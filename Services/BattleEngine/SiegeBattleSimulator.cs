using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 턴제 시뮬레이터 (3단계까지: 턴 진행 + 데미지 계산 + 타겟팅).
    /// 데이터·규칙은 메모리 siege-simulation-rules / battle-time-model.
    ///
    /// 구현: 선공 결정 → 스킬턴 사이클(0턴부터 2턴마다 선/후 번갈아) → 기본공격(양팀 통합 속공순) →
    ///   70턴 / 라운드 전환(R1·R2 적 전멸) → 점수 누적. 데미지는 DamageCalculator(공성전 감쇄·디버프 반영),
    ///   타겟=약점(최저HP·동률 앞열, R3 보스). 행동 소요시간만큼 전체 유닛 쿨다운 감소.
    /// 후속: 적 행동(아군 피격·생존)=4번, DoT/상태이상·결과 정교화=5번.
    /// </summary>
    public class SiegeBattleSimulator
    {
        private readonly BattleSimulator _baseSim = new();   // 아군 스탯 초기화 재사용
        private readonly DamageCalculator _damageCalc = new();
        private readonly Random _rng = new();

        public SiegeBattleResult Simulate(SiegeBattleConfig config)
        {
            var state = Initialize(config);
            RunTurnLoop(config, state);
            return BuildResult(state);
        }

        #region 초기화

        private SiegeBattleState Initialize(SiegeBattleConfig config)
        {
            var state = new SiegeBattleState
            {
                SiegeStage = config.SiegeStage,
                MaxTurns = config.MaxTurns,
            };

            // 아군 스탯 — 기존 BattleSimulator.InitializeCharacterState 재사용 (TargetEnemy 불필요)
            var tempConfig = new BattleConfig
            {
                AllyParty = config.AllyParty,
                AllyPet = config.AllyPet,
                PetStar = config.PetStar,
                PetOptionAtkRate = config.PetOptionAtkRate,
                PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate,
                FormationName = config.FormationName,   // PVE: 빈 값 → 진형 효과 미적용
            };
            for (int i = 0; i < config.AllyParty.Count; i++)
                state.AllyStates.Add(_baseSim.InitializeCharacterState(tempConfig, config.AllyParty[i], i));

            // 1라운드 적 생성
            state.InitializeRound(1);

            // 선공 결정 (팀 총 속공치, 동률 랜덤)
            double allySpd = state.AllyTotalSpd, enemySpd = state.EnemyTotalSpd;
            state.AllyFirst = allySpd > enemySpd || (allySpd == enemySpd && _rng.Next(2) == 0);

            return state;
        }

        #endregion

        #region 턴 루프

        private void RunTurnLoop(SiegeBattleConfig config, SiegeBattleState state)
        {
            // 행동순서는 라운드당 1회 구성(라운드 내 고정). 동속공 랜덤도 이때 1회 결정 — 속공 불변이라 유지.
            var order = BuildActionOrder(state);
            int cursor = 0;
            int t = 0;

            while (t < state.MaxTurns)
            {
                state.CurrentTurn = t;

                // 스킬턴: 0턴부터 2턴마다, 선/후공 번갈아 (턴 미소모, 기본공격 안 함)
                if (t % 2 == 0)
                {
                    bool firstSideTurn = (t / 2) % 2 == 0;                 // 이번 스킬턴이 선공팀 차례인지
                    bool skillByAlly = firstSideTurn ? state.AllyFirst : !state.AllyFirst;
                    state.IsSkillTurn = true;
                    ProcessSkillTurn(config, state, skillByAlly);
                    state.IsSkillTurn = false;
                }

                // 기본공격: 양팀 통합 속공순 다음 유닛 (턴 소모)
                if (order.Count > 0)
                {
                    var actor = order[cursor % order.Count];
                    cursor++;
                    ExecuteBasicAttack(state, actor);
                }
                t++;

                // 턴 경과: 적 DoT 틱(점수 누적) + 아군 CC/상태이상·면역 잔여턴 감소
                TickEnemyDots(state);
                TickAllyStatus(state);

                // 라운드 전환 (R1/R2: 적 전멸 시 다음 라운드, 턴 이어짐)
                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    state.CurrentRound++;
                    state.InitializeRound(state.CurrentRound);
                    order = BuildActionOrder(state);   // 적 교체 → 행동순 재구성
                    cursor = 0;
                }
            }

            state.CurrentTurn = t;
        }

        /// <summary>양팀(아군+적) 통합 속공 내림차순. 같은 팀 동속공은 자리순, 다른 팀 동속공은 랜덤(1회 결정).</summary>
        private List<SiegeActor> BuildActionOrder(SiegeBattleState state)
        {
            var actors = new List<SiegeActor>();
            foreach (var a in state.AllyStates)
                actors.Add(new SiegeActor { IsAlly = true, Ally = a, Spd = a.FinalSpd, Position = a.PartyIndex });
            foreach (var e in state.Enemies)
                actors.Add(new SiegeActor { IsAlly = false, Enemy = e, Spd = e.FinalSpd, Position = e.Position });

            return actors
                .GroupBy(x => x.Spd)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => OrderSameSpeed(g.ToList()))
                .ToList();
        }

        /// <summary>동속공 그룹: 같은 팀은 자리(Position)순, 팀 간 순서는 랜덤 인터리브.</summary>
        private List<SiegeActor> OrderSameSpeed(List<SiegeActor> group)
        {
            if (group.Count <= 1) return group;
            var allies = group.Where(x => x.IsAlly).OrderBy(x => x.Position).ToList();
            var enemies = group.Where(x => !x.IsAlly).OrderBy(x => x.Position).ToList();
            var result = new List<SiegeActor>();
            int ai = 0, ei = 0;
            while (ai < allies.Count || ei < enemies.Count)
            {
                bool pickAlly = ei >= enemies.Count || (ai < allies.Count && _rng.Next(2) == 0);
                if (pickAlly) result.Add(allies[ai++]);
                else result.Add(enemies[ei++]);
            }
            return result;
        }

        #endregion

        #region 행동

        /// <summary>스킬턴 처리 (해당 팀에 사용 가능한 스킬이 있으면 사용; 없으면 스킵).</summary>
        private void ProcessSkillTurn(SiegeBattleConfig config, SiegeBattleState state, bool byAlly)
        {
            if (byAlly)
            {
                // 아군: 쿨 충족 스킬 1개 (속공 높은 순으로 첫 사용가능). CC(행동불가) 아군은 제외. 한 명만.
                foreach (var ally in state.AllyStates
                             .Where(a => !a.IsDead && !a.Effects.HasActionBlockingCC())
                             .OrderByDescending(a => a.FinalSpd))
                {
                    var skill = PickAllySkill(ally);
                    if (skill == null) continue;

                    var target = PickTarget(state);
                    if (target == null) return;

                    double dmg = CalcDamageToEnemy(ally, target, skill);
                    ApplyDamage(state, ally, target, dmg, skill.Name, isSkill: true);
                    RegisterDotToEnemy(state, ally, target, skill);   // 스킬의 DoT(화상·출혈 등) 등록

                    double cd = skill.GetCooldown(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel);
                    if (cd > 0) ally.SkillCooldowns[skill.SkillType] = cd;

                    // 스킬 소요시간만큼 전체 쿨다운 감소 (쿨=실시간, 1초당 1)
                    AdvanceTime(state, GetActionDuration(skill));
                    return;
                }
            }
            else
            {
                // 적 스킬턴: SkillPriority 순위대로(앞쪽 우선) 쿨 충족된 첫 스킬 1개 사용
                foreach (var pri in state.CurrentSkillPriority)
                {
                    var enemy = state.Enemies.FirstOrDefault(e => e.Source.Id == pri.EnemyId);
                    if (enemy == null || !enemy.IsSkillReady(pri.SkillType)) continue;
                    var skill = enemy.Source.Skills?.FirstOrDefault(s => s.SkillType == pri.SkillType);
                    if (skill == null) continue;

                    // 광역은 타겟수만큼, 단일은 1명 — 모두 랜덤 아군
                    int targetCount = Math.Max(1, skill.GetTargetCount(false, 0));
                    foreach (var target in PickRandomAllies(state, targetCount))
                    {
                        double dmg = CalcDamageToAlly(enemy, target, skill);
                        ApplyDamageToAlly(state, enemy, target, dmg, skill.Name);
                        ApplyEnemyStatusToAlly(state, target, skill);
                    }

                    double cd = skill.GetCooldown(false, 0);
                    if (cd > 0) enemy.SkillCooldowns[pri.SkillType] = cd;
                    AdvanceTime(state, GetActionDuration(skill));
                    return;
                }
                // 사용 가능한 스킬이 없으면 스킬턴 스킵
            }
        }

        /// <summary>기본공격 (턴 소모).</summary>
        private void ExecuteBasicAttack(SiegeBattleState state, SiegeActor actor)
        {
            if (actor.IsAlly)
            {
                var ally = actor.Ally;
                // 사망·CC(빙결·기절 등 행동불가) 상태면 기본공격 스킵 (차례는 지나감 = 딜 기회 상실)
                if (!ally.IsDead && !ally.Effects.HasActionBlockingCC())
                {
                    var normal = ally.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                    var target = PickTarget(state);
                    if (normal != null && target != null)
                    {
                        double dmg = CalcDamageToEnemy(ally, target, normal);
                        ApplyDamage(state, ally, target, dmg, normal.Name, isSkill: false);
                        RegisterDotToEnemy(state, ally, target, normal);   // 평타의 DoT(화상 등) 등록
                        TriggerAllyImmunity(state, ally);   // 기본공격 발동 → 면역 패시브 트리거(턴제 면역 갱신)
                    }
                }
            }
            else
            {
                // 적 기본공격 → 랜덤 아군 1명 피격 + 상태이상 부여
                var enemy = actor.Enemy;
                var normal = enemy.Source.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                var target = PickRandomAlly(state);
                if (normal != null && target != null)
                {
                    double dmg = CalcDamageToAlly(enemy, target, normal);
                    ApplyDamageToAlly(state, enemy, target, dmg, normal.Name);
                    ApplyEnemyStatusToAlly(state, target, normal);
                }
            }

            // 기본공격 소요시간(평타 2초)만큼 전체 쿨다운 감소
            AdvanceTime(state, GetActionDuration(null));
        }

        /// <summary>아군이 적에게 데미지 적용 + 점수·로그.</summary>
        private void ApplyDamage(SiegeBattleState state, CharacterBattleState ally, SiegeEnemyState target,
            double dmg, string label, bool isSkill)
        {
            if (dmg <= 0) return;

            target.CurrentHp -= dmg;            // HP 0 이하 허용 (무사망)
            target.TotalDamageTaken += dmg;
            ally.TotalDamageDealt += dmg;
            state.TotalScore += dmg;
            state.RoundScore[state.CurrentRound] = state.RoundScore.GetValueOrDefault(state.CurrentRound) + dmg;

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = ally.Source.Character.Name,
                IsAlly = true,
                ActionType = isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                SkillName = label,
                DamageDealt = dmg,
                Description = $"{ally.Source.Character.Name} → {target.Source.Name}: {dmg:N0}",
            });
        }

        #endregion

        #region 타겟팅 / 데미지 계산

        /// <summary>
        /// 타겟 선정: 약점공격(시뮬은 결정론적으로 항상 발동) = 생명력 최저 적, 동률이면 앞열(Position 낮은 순).
        /// 라운드3에서는 약점공격 대상이 항상 보스(3보스 중 최저 HP).
        /// </summary>
        private SiegeEnemyState PickTarget(SiegeBattleState state)
        {
            IEnumerable<SiegeEnemyState> candidates = state.CurrentRound >= 3
                ? state.Enemies.Where(e => e.IsBoss)
                : state.Enemies;
            var list = candidates.ToList();
            if (list.Count == 0) list = state.Enemies;
            // 최저 HP → 동률 시 앞열(Position 낮은 자리)
            return list.OrderBy(e => e.CurrentHp).ThenBy(e => e.Position).FirstOrDefault();
        }

        /// <summary>아군 → 적 데미지 (DamageCalculator). 공성전 감쇄(물/마·타겟수)·디버프 반영. 시뮬은 치명·약점 항상 발동.</summary>
        private double CalcDamageToEnemy(CharacterBattleState ally, SiegeEnemyState target, Skill skill)
        {
            var battleChar = ally.Source;
            var character = battleChar.Character;
            var enemy = target.Source;
            var baseStats = character.GetBaseStats();

            int targetCount = skill.GetTargetCount(battleChar.IsSkillEnhanced, battleChar.TranscendLevel);
            double targetReduction = GetTargetReduction(enemy, targetCount);
            // 공성전 감쇄: 캐릭터 공격속성에 따라 물리/마법 받피감
            double elemReduction = character.AttackType == AttackType.Magic
                ? enemy.MagicReduction
                : enemy.PhysicalReduction;

            var debuffs = target.Effects.GetTotalDebuffs();

            var input = new DamageCalculator.DamageInput
            {
                Character = character,
                Skill = skill,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                TranscendLevel = battleChar.TranscendLevel,
                FinalAtk = ally.FinalAtk,
                FinalDef = ally.FinalDef,
                FinalHp = ally.MaxHp,
                CritDamage = baseStats.Cri_Dmg,
                WeakpointDmg = baseStats.Wek_Dmg,
                BossDef = enemy.Stats.Def,
                // 공성전 감쇄(물/마·타겟수)는 합연산이 아니라 곱연산으로 후처리 (아래) — DamageCalculator엔 0으로
                BossDmgReduction = 0,
                BossTargetReduction = 0,
                BossHp = target.MaxHp,
                TargetHp = target.MaxHp,
                TargetCurrentHp = target.CurrentHp,
                DefReduction = debuffs.Def_Reduction,
                DmgTakenIncrease = debuffs.GetEffectiveDmgTakenIncrease(character.AttackType),
                Vulnerability = debuffs.Vulnerability,
                BossVulnerability = debuffs.Boss_Vulnerability,
                IsCritical = true,
                IsWeakpoint = true,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = target.IsBoss,
                SelfMaxHp = ally.MaxHp,
            };
            double raw = _damageCalc.Calculate(input).FinalDamage;

            // 공성전 감쇄 = 곱연산 (받는 물/마 피해 N% 감소 × 타겟수별 감소). 합연산 시 180%↑ 차감으로 음수가 되므로.
            double reductionMult = (1 - elemReduction / 100.0) * (1 - targetReduction / 100.0);
            return raw * Math.Max(0, reductionMult);
        }

        /// <summary>타겟 수에 따른 공성전 감쇄(1인/3인/5인기).</summary>
        private double GetTargetReduction(Enemy enemy, int targetCount) => targetCount switch
        {
            1 => enemy.SingleTargetReduction,
            3 => enemy.TripleTargetReduction,
            >= 5 => enemy.MultiTargetReduction,
            _ => 0
        };

        /// <summary>적 → 아군 데미지 (적 공격력 vs 아군 방어/받피감). 적은 치확·약확 0이라 비치명·비약점 기본.</summary>
        private double CalcDamageToAlly(SiegeEnemyState enemy, CharacterBattleState ally, Skill enemySkill)
        {
            var e = enemy.Source;
            // 아군 받피감(자버프) + 아군에게 걸린 받피증/취약(적 디버프)
            var (perm, timed, pet) = ally.Effects.GetSeparatedBuffs();
            double allyDmgRdc = perm.Dmg_Rdc + timed.Dmg_Rdc + pet.Dmg_Rdc;
            var allyDebuffs = ally.Effects.GetTotalDebuffs();

            var input = new DamageCalculator.DamageInput
            {
                Character = null,                       // 적은 Character 모델 없음
                Skill = enemySkill,
                IsSkillEnhanced = false,
                TranscendLevel = 0,
                FinalAtk = enemy.FinalAtk,
                FinalDef = enemy.FinalDef,
                CritDamage = e.Stats.Cri_Dmg,
                BossDef = ally.FinalDef,                // 의미상 target(아군) 방어
                BossDmgReduction = allyDmgRdc,          // 아군 받피감 (합연산 차감, 보통 작아 음수 없음)
                BossHp = ally.MaxHp,
                TargetHp = ally.MaxHp,
                TargetCurrentHp = ally.CurrentHp,
                DmgTakenIncrease = allyDebuffs.Dmg_Taken_Increase,
                Vulnerability = allyDebuffs.Vulnerability,
                IsCritical = e.Stats.Cri >= 100,        // 적 치확(보통 0)
                IsWeakpoint = false,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = false,                   // 아군은 보스 아님
                SelfMaxHp = enemy.MaxHp,
            };
            return _damageCalc.Calculate(input).FinalDamage;
        }

        /// <summary>적이 아군을 공격 → 피해 적용. (생존 메카닉: 부활·면역·권능은 후속 단계)</summary>
        private void ApplyDamageToAlly(SiegeBattleState state, SiegeEnemyState enemy, CharacterBattleState ally,
            double dmg, string label)
        {
            if (dmg <= 0 || ally.IsDead) return;

            // 피해 무효화 (피격 N회 / N턴) — 피해 자체를 0으로
            if ((ally.NullifyHitsRemaining > 0 || ally.NullifyTurnsRemaining > 0) && ally.NullifyType == DamageNullType.All)
            {
                if (ally.NullifyHitsRemaining > 0) ally.NullifyHitsRemaining--;
                state.TurnLogs.Add(new BattleTurnLog
                {
                    Turn = state.CurrentTurn, ActorName = enemy.Source.Name, IsAlly = false,
                    ActionType = ActionType.BuffApplied, SkillName = "피해 무효화", DamageDealt = 0,
                    Description = $"{ally.Source.Character.Name} 피격 무효화 (잔여 {ally.NullifyHitsRemaining}회)",
                });
                return;
            }

            ally.CurrentHp -= dmg;
            string outcome = "";
            if (ally.CurrentHp <= 0)
            {
                // 생존 판정: 불굴/불사 → 권능 → 부활 → 사망 (공유 SurvivalResolver)
                var r = SurvivalResolver.ResolveLethal(ally);
                if (ally.IsDead) ally.CurrentHp = 0;
                outcome = $" ({r.Label})";
            }

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = enemy.Source.Name,
                IsAlly = false,
                ActionType = state.IsSkillTurn ? ActionType.SkillAttack : ActionType.NormalAttack,
                SkillName = label,
                DamageDealt = dmg,
                Description = $"{enemy.Source.Name} → {ally.Source.Character.Name}: {dmg:N0}{outcome}",
            });
        }

        /// <summary>살아있는 아군 중 랜덤 1명 (없으면 null).</summary>
        private CharacterBattleState PickRandomAlly(SiegeBattleState state)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            return alive.Count == 0 ? null : alive[_rng.Next(alive.Count)];
        }

        /// <summary>살아있는 아군 중 랜덤 N명 (중복 없이; 부족하면 가능한 만큼).</summary>
        private List<CharacterBattleState> PickRandomAllies(SiegeBattleState state, int count)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            if (alive.Count <= count) return alive;
            return alive.OrderBy(_ => _rng.Next()).Take(count).ToList();
        }

        #endregion

        #region 상태이상 / 면역 (CC)

        /// <summary>면역 패시브(기본공격 트리거) 발동 — 대상에 상태이상 면역 턴제버프 갱신.</summary>
        private void TriggerAllyImmunity(SiegeBattleState state, CharacterBattleState ally)
        {
            var passive = ally.Source.Character.Passive;
            if (passive == null) return;
            foreach (var e in GetPassiveEffects(passive, ally))
            {
                if (e.Type != PersistentEffectType.Immunity) continue;
                if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.NormalOnly) continue;
                if (e.StatusImmunity?.Types == null) continue;

                IEnumerable<CharacterBattleState> targets = e.Target == EffectTarget.Party
                    ? state.AllyStates
                    : new[] { ally };
                foreach (var tgt in targets)
                    foreach (var type in e.StatusImmunity.Types)
                        tgt.StatusImmunityTurns[type] =
                            Math.Max(tgt.StatusImmunityTurns.GetValueOrDefault(type), e.StatusImmunity.Duration);
            }
        }

        /// <summary>적 스킬/평타의 상태이상을 아군에 부여 (면역 보유 타입은 무효).</summary>
        private void ApplyEnemyStatusToAlly(SiegeBattleState state, CharacterBattleState ally, Skill enemySkill)
        {
            if (ally.IsDead) return;
            var statuses = enemySkill.GetLevelData(false)?.StatusEffects;   // 적 스킬은 단일 티어
            if (statuses == null) return;
            foreach (var se in statuses)
            {
                if (ally.StatusImmunityTurns.GetValueOrDefault(se.Type) > 0) continue;   // 면역 → 무효
                var baseEffect = StatusEffectDb.Get(se.Type);
                if (baseEffect == null) continue;
                ally.Effects.AddEffect(new BattleEffect
                {
                    Id = $"siege_cc:{ally.PartyIndex}:{se.Type}",
                    SourceName = $"siege_cc:{ally.PartyIndex}:{se.Type}",
                    StatusType = se.Type,
                    StatusData = StatusEffectData.FromDbEffect(baseEffect),
                    RemainingTurns = se.Duration > 0 ? se.Duration : baseEffect.Duration,
                    IsPermanent = false,
                    MergeStrategy = MergeStrategy.Stack,
                });
            }
        }

        /// <summary>턴 경과: 아군 상태이상(CC 등) + 면역 잔여턴 1 감소.</summary>
        private void TickAllyStatus(SiegeBattleState state)
        {
            foreach (var ally in state.AllyStates)
            {
                ally.Effects.TickTurn();
                foreach (var k in ally.StatusImmunityTurns.Keys.ToList())
                    ally.StatusImmunityTurns[k] = Math.Max(0, ally.StatusImmunityTurns[k] - 1);
                // 턴 기반 생존(피해무효화[N턴]·불사[N턴]) 잔여 턴 감소
                if (ally.NullifyTurnsRemaining > 0) ally.NullifyTurnsRemaining--;
                if (ally.ImmortalTurnsRemaining > 0) ally.ImmortalTurnsRemaining--;
            }
        }

        /// <summary>패시브 효과(레벨+초월) 합산.</summary>
        private IEnumerable<PersistentEffect> GetPassiveEffects(Passive passive, CharacterBattleState ally)
        {
            var list = new List<PersistentEffect>();
            var lvl = passive.GetLevelData(ally.Source.IsSkillEnhanced);
            if (lvl?.Effects != null) list.AddRange(lvl.Effects);
            var tr = passive.GetTranscendBonus(ally.Source.TranscendLevel);
            if (tr?.Effects != null) list.AddRange(tr.Effects);
            return list;
        }

        #endregion

        #region DoT (아군 → 적 지속피해)

        /// <summary>
        /// 아군 스킬/평타의 DoT(화상·출혈 등 공격력 비례)를 적에 등록. 틱당 데미지는 등록 시점에
        /// 시전자 공격력·공성전 감쇄(시전자 공격속성 기준)를 반영해 계산해 둔다.
        /// (HP 비례 DoT인 중독·즉사 등은 후속.)
        /// </summary>
        private void RegisterDotToEnemy(SiegeBattleState state, CharacterBattleState ally, SiegeEnemyState target, Skill skill)
        {
            var statuses = skill.GetLevelData(ally.Source.IsSkillEnhanced)?.StatusEffects;
            if (statuses == null) return;
            foreach (var se in statuses)
            {
                var baseEffect = StatusEffectDb.Get(se.Type);
                if (baseEffect == null) continue;
                double atkRatio = (se.CustomAtkRatio ?? baseEffect.AtkRatio) / 100.0;
                if (atkRatio <= 0) continue;   // 공격력 비례 DoT만 (HP비례는 후속)

                double elemReduction = ally.Source.Character.AttackType == AttackType.Magic
                    ? target.Source.MagicReduction
                    : target.Source.PhysicalReduction;
                double tick = ally.FinalAtk * atkRatio * Math.Max(0, 1 - elemReduction / 100.0);
                int dur = se.Duration > 0 ? se.Duration : baseEffect.Duration;

                target.ActiveDots.Add(new SiegeDot
                {
                    Type = se.Type,
                    TickDamage = tick,
                    RemainingTurns = dur,
                    SourceName = ally.Source.Character.Name,
                });
            }
        }

        /// <summary>매 턴 각 적의 DoT 틱 → 점수 누적 + 캐릭별 기여 반영, 잔여턴 감소.</summary>
        private void TickEnemyDots(SiegeBattleState state)
        {
            foreach (var enemy in state.Enemies)
            {
                foreach (var dot in enemy.ActiveDots)
                {
                    if (dot.TickDamage > 0)
                    {
                        enemy.CurrentHp -= dot.TickDamage;
                        enemy.TotalDamageTaken += dot.TickDamage;
                        state.TotalScore += dot.TickDamage;
                        state.RoundScore[state.CurrentRound] =
                            state.RoundScore.GetValueOrDefault(state.CurrentRound) + dot.TickDamage;

                        var src = state.AllyStates.FirstOrDefault(a => a.Source.Character.Name == dot.SourceName);
                        if (src != null) src.TotalDamageDealt += dot.TickDamage;

                        var dotName = StatusEffectDb.Get(dot.Type)?.Name ?? dot.Type.ToString();
                        state.TurnLogs.Add(new BattleTurnLog
                        {
                            Turn = state.CurrentTurn, ActorName = dot.SourceName, IsAlly = true,
                            ActionType = ActionType.DoTDamage, SkillName = dotName, DamageDealt = dot.TickDamage,
                            Description = $"[DoT] {dotName} → {enemy.Source.Name}: {dot.TickDamage:N0}",
                        });
                    }
                    dot.RemainingTurns--;
                }
                enemy.ActiveDots.RemoveAll(d => d.RemainingTurns <= 0);
            }
        }

        #endregion

        #region 시간 / 쿨다운

        /// <summary>행동 소요시간(초): 평타 2 / 1스킬 4 / 2스킬(컷신) 5 (battle-time-model).</summary>
        private double GetActionDuration(Skill skill)
        {
            if (skill == null) return 2.0;                       // 기본공격
            return skill.SkillType switch
            {
                SkillType.Normal or SkillType.Normal2 => 2.0,
                SkillType.Skill2 => 5.0,                          // 컷신 2스킬
                _ => 4.0,                                         // 그 외 스킬
            };
        }

        /// <summary>행동 소요시간만큼 경과 + 전체 유닛(아군+적) 쿨다운 감소 (쿨=실시간 1초당 1).</summary>
        private void AdvanceTime(SiegeBattleState state, double seconds)
        {
            if (seconds <= 0) return;
            state.ElapsedSeconds += seconds;
            foreach (var a in state.AllyStates)
                foreach (var k in a.SkillCooldowns.Keys.ToList())
                    a.SkillCooldowns[k] = Math.Max(0, a.SkillCooldowns[k] - seconds);
            foreach (var e in state.Enemies)
                foreach (var k in e.SkillCooldowns.Keys.ToList())
                    e.SkillCooldowns[k] = Math.Max(0, e.SkillCooldowns[k] - seconds);
        }

        #endregion

        #region 보조

        /// <summary>R1/R2 적이 모두 HP0 이하인지 (라운드 클리어 판정).</summary>
        private bool AllEnemiesDown(SiegeBattleState state)
            => state.Enemies.Count > 0 && state.Enemies.All(e => e.CurrentHp <= 0);

        private Skill PickAllySkill(CharacterBattleState ally)
        {
            // 간략: 쿨 충족 스킬 중 SkillType 높은 순 (궁→4→3→2→1). 4번에서 로테이션 정교화.
            return ally.Source.Character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .Where(s => ally.IsSkillReady(s.SkillType))
                .OrderByDescending(s => s.SkillType)
                .FirstOrDefault();
        }

        #endregion

        #region 결과

        private SiegeBattleResult BuildResult(SiegeBattleState state)
        {
            var result = new SiegeBattleResult
            {
                TotalScore = state.TotalScore,
                RoundScore = state.RoundScore,
                TotalTurns = state.CurrentTurn,
                RoundsCleared = state.CurrentRound - 1,
                TurnLogs = state.TurnLogs,
            };
            foreach (var ally in state.AllyStates)
            {
                result.CharacterResults.Add(new SiegeCharacterResult
                {
                    CharacterName = ally.Source.Character.Name,
                    PartyIndex = ally.PartyIndex,
                    TotalDamage = ally.TotalDamageDealt,
                    DamageShare = state.TotalScore > 0 ? ally.TotalDamageDealt / state.TotalScore * 100 : 0,
                });
            }
            return result;
        }

        #endregion
    }

    /// <summary>행동 순서 정렬용 유닛 래퍼 (아군 또는 적).</summary>
    internal class SiegeActor
    {
        public bool IsAlly;
        public CharacterBattleState Ally;
        public SiegeEnemyState Enemy;
        public double Spd;
        public int Position;
    }
}

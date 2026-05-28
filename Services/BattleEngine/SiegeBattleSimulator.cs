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
        private readonly Random _rng;
        private readonly int? _seed;

        /// <summary>seed 지정 시 결정론적 RNG — 장비 후보를 같은 조건으로 공정 비교할 때 사용.</summary>
        public SiegeBattleSimulator(int? seed = null) { _seed = seed; _rng = seed.HasValue ? new Random(seed.Value) : new Random(); }

        /// <summary>[진단] 특정 스킬(아군→적) 타격 시 데미지 입력 구성요소를 덤프 (예: 죽음의 무도 보스 갭 추적).</summary>
        public string DiagSkillName;          // 추적할 스킬명 (null이면 비활성)
        public System.Text.StringBuilder DiagLog = new();
        private int _diagCount;

        public SiegeBattleResult Simulate(SiegeBattleConfig config) => Simulate(config, computeWeights: true);

        private SiegeBattleResult Simulate(SiegeBattleConfig config, bool computeWeights)
        {
            var state = Initialize(config);
            if (computeWeights)
            {
                // 2-패스 버프 타게팅: 스카우팅 sim(raw-atk 타게팅)으로 실제 캐릭별 누적딜을 얻어,
                // 그걸 DamageWeight로 써서 비스킷 버프·라이언 쿨감을 "실제 딜 1위 딜러"에게 배분.
                // 비딜러(지원/방어형)는 0으로 제외. (1타 추정이 아닌 실제 기여 기반)
                var byName = new SiegeBattleSimulator(_seed).Simulate(config, computeWeights: false)
                    .CharacterResults.ToDictionary(c => c.CharacterName, c => c.TotalDamage);
                foreach (var a in state.AllyStates)
                    a.DamageWeight = (a.Source?.Character?.Type != null && DealerRoles.Contains(a.Source.Character.Type)
                        && byName.TryGetValue(a.Source.Character.Name, out var d)) ? d : 0;
            }
            // computeWeights=false(스카우팅): DamageWeight=0 → raw FinalAtk 타게팅(원래 동작)
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
                PetEnhance = config.PetEnhance,
                PetOptionAtkRate = config.PetOptionAtkRate,
                PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate,
                FormationName = config.FormationName,   // 아군은 진형효과 적용 (적군만 미적용)
            };
            for (int i = 0; i < config.AllyParty.Count; i++)
                state.AllyStates.Add(_baseSim.InitializeCharacterState(tempConfig, config.AllyParty[i], i));
            // DamageWeight는 Simulate의 2-패스에서 설정 (스카우팅 누적딜 기반). 스카우팅 패스에선 0(raw-atk 타게팅).

            // 1라운드 적 생성
            state.InitializeRound(1);
            ApplyStandingEnemyDebuffs(state, config);   // 아군 패시브 방깎·펫 보스취약 등 상시 디버프

            // 선공 결정 (팀 총 속공치, 동률 랜덤)
            double allySpd = state.AllyTotalSpd, enemySpd = state.EnemyTotalSpd;
            state.AllyFirst = allySpd > enemySpd || (allySpd == enemySpd && _rng.Next(2) == 0);

            return state;
        }

        #endregion

        #region 턴 루프

        private void RunTurnLoop(SiegeBattleConfig config, SiegeBattleState state)
        {
            int cursor = 0;
            int t = 0;
            int roundTurn = 0;     // 현재 라운드의 평타 턴 수 (2턴마다 라운드 내 스킬턴)
            int inRoundSkill = 0;  // 라운드 시작 선공 스킬 이후 후공→선공 교대 카운터

            // 배틀 시작 = 라운드1 진입. 선공(속공 빠른 쪽) 스킬턴(0턴). 0턴 스킬로 라운드 연쇄 클리어 시 다음 라운드도 0턴 선공 스킬턴.
            state.CurrentTurn = 0;
            EnterRound(config, state);
            var order = BuildActionOrder(state);

            while (t < state.MaxTurns)
            {
                t++;
                roundTurn++;
                state.CurrentTurn = t;

                // 비트리거 상시 면역(예: 풍연 빙결 면역) 매 턴 갱신 — 시전자 생존 동안 유지
                ApplyStandingImmunities(state);

                // 기본공격: 양팀 통합 속공순 다음 유닛 (턴 소모)
                if (order.Count > 0)
                {
                    var actor = order[cursor % order.Count];
                    cursor++;
                    ExecuteBasicAttack(state, actor);
                }

                // 라운드 내 스킬턴: 2턴마다, 라운드 시작 선공 스킬 이후 후공→선공 교대 (턴 미소모)
                if (roundTurn % 2 == 0)
                {
                    bool skillByAlly = (inRoundSkill % 2 == 0) ? !state.AllyFirst : state.AllyFirst;
                    inRoundSkill++;
                    state.IsSkillTurn = true;
                    ProcessSkillTurn(config, state, skillByAlly);
                    state.IsSkillTurn = false;
                }

                // 턴 경과: 적 DoT 틱(점수 누적) + 아군 CC/상태이상·면역 잔여턴 감소
                TickEnemyDots(state);
                TickAllyStatus(state);

                // 라운드 전환: 적 전멸 시 다음 라운드 진입 → 선공 스킬턴(0턴, 연쇄)
                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    EnterRound(config, state);
                    order = BuildActionOrder(state);   // 적 교체 → 행동순 재구성
                    cursor = 0; roundTurn = 0; inRoundSkill = 0;
                }
            }

            state.CurrentTurn = t;
        }

        /// <summary>
        /// 라운드 진입: 속공 빠른 쪽(선공)을 재계산하고 선공 스킬턴(0턴, 평타 없음)을 발동.
        /// 그 0턴 스킬로 라운드가 클리어되면 다음 라운드로 넘어가 또 선공 스킬턴을 발동(연쇄). R3는 클리어 없음.
        /// </summary>
        private void EnterRound(SiegeBattleConfig config, SiegeBattleState state)
        {
            while (true)
            {
                DetermineFirst(state);   // 이 라운드 적 기준 선공 재계산
                state.IsSkillTurn = true;
                ProcessSkillTurn(config, state, byAlly: state.AllyFirst);   // 선공 스킬턴
                state.IsSkillTurn = false;

                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    Log(state, "시스템", true, ActionType.BuffApplied, "라운드 전환", 0,
                        $"R{state.CurrentRound} 클리어 → R{state.CurrentRound + 1} 진입 (선공 스킬턴)");
                    state.CurrentRound++;
                    state.InitializeRound(state.CurrentRound);
                    ApplyStandingEnemyDebuffs(state, config);
                    continue;   // 다음 라운드도 0턴 선공 스킬턴
                }
                break;
            }
        }

        /// <summary>선공(속공 빠른 쪽) 결정 — 현재 라운드 적 총 속공 기준. 동률 랜덤.</summary>
        private void DetermineFirst(SiegeBattleState state)
        {
            double a = state.AllyTotalSpd, e = state.EnemyTotalSpd;
            state.AllyFirst = a > e || (a == e && _rng.Next(2) == 0);
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
                int stIdx = state.AllySkillTurnIndex++;   // 이 아군 스킬턴의 발생 순서 인덱스

                // 행동 후보 수집(살아있고 CC 아닌 아군 × 준비된 비평타 스킬, + Hold) — 빔서치 기록/플랜 폴백용
                if (config.RecordDecisionPoints)
                    state.DecisionPoints.Add(new RotationDecisionPoint
                    {
                        SkillTurnIndex = stIdx,
                        Turn = state.CurrentTurn,
                        Choices = CollectAllyChoices(state),
                    });

                // 플랜이 이 스킬턴을 지정하면 그대로 시전(또는 홀드). 무효/초과면 자동(라운드로빈) 폴백.
                var plan = config.RotationPlan;
                if (plan != null && stIdx < plan.Count)
                {
                    var dec = plan[stIdx];
                    if (dec.Hold) return;   // 홀드: 스킬턴 스킵
                    if (dec.HeroIndex >= 0 && dec.HeroIndex < state.AllyStates.Count)
                    {
                        var a = state.AllyStates[dec.HeroIndex];
                        var sk = a.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == dec.Skill);
                        if (!a.IsDead && !a.Effects.HasActionBlockingCC() && sk != null && a.IsSkillReady(dec.Skill))
                        {
                            ExecuteAllySkill(state, dec.HeroIndex, sk);
                            return;
                        }
                    }
                    // 무효 → 자동 폴백
                }

                // 자동: 라운드로빈(AllyRotationCursor)으로 순회 → 첫 시전 가능한 아군이 PickAllySkill 시전.
                int n = state.AllyStates.Count;
                for (int k = 0; k < n; k++)
                {
                    int idx = (state.AllyRotationCursor + k) % n;
                    var ally = state.AllyStates[idx];
                    if (ally.IsDead || ally.Effects.HasActionBlockingCC()) continue;
                    var skill = PickAllySkill(ally);
                    if (skill == null) continue;
                    ExecuteAllySkill(state, idx, skill);
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

                    // 적 진영 전체 피해 면역 부여 (화 R3 룩 등)
                    int imm = skill.GetLevelData(false)?.GrantEnemyImmunityTurns ?? 0;
                    if (imm > 0)
                    {
                        state.EnemyImmunityTurns = Math.Max(state.EnemyImmunityTurns, imm);
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"적 진영 피해 면역[{imm}턴]");
                    }

                    double cd = skill.GetCooldown(false, 0);
                    if (cd > 0) enemy.SkillCooldowns[pri.SkillType] = cd;
                    // 보스 자기 보호막 생성 (예: 루디 방어 준비 = 방어력 100배). 아군 피해를 흡수(점수 미집계)·버프해제로 제거.
                    var sld = skill.GetLevelData(false);
                    if (sld != null && sld.SelfShieldDefRatio > 0)
                    {
                        enemy.Shield = enemy.Source.Stats.Def * sld.SelfShieldDefRatio / 100.0;
                        enemy.ShieldTurns = sld.SelfShieldTurns > 0 ? sld.SelfShieldTurns : 99;
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"보호막 {enemy.Shield:N0} 생성 [{enemy.ShieldTurns}턴] (버프해제/소진 시 제거)");
                    }
                    // 상대(보스) 스킬 사용 → 아군 전원 스킬 쿨다운 5초 감소 (메인 sim과 동일, 5초 이하 잔여 미적용)
                    foreach (var a in state.AllyStates)
                        if (!a.IsDead) a.ReduceCooldowns(5);
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
                    if (normal != null)
                    {
                        int tc = System.Math.Max(1, normal.GetTargetCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel));
                        var targets = PickTargets(state, tc);
                        foreach (var target in targets)
                        {
                            double dmg = CalcDamageToEnemy(ally, target, normal);
                            ApplyDamage(state, ally, target, dmg, normal.Name, isSkill: false);
                            RegisterDotToEnemy(state, ally, target, normal);   // 평타의 DoT(화상 등) 등록
                        }
                        if (targets.Count > 0)
                        {
                            ProcessAttackStacks(state, ally, isSkill: false);   // 공격 발동형 스택 — 평타 2회마다 1회
                            TriggerAllyImmunity(state, ally);   // 기본공격 발동 → 면역 패시브 트리거(턴제 면역 갱신)
                            ApplyBasicAttackCdReduction(state, ally, normal);   // 평타 쿨감(예: 라이언 자신+최고공격력 아군 9초)
                        }
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

            // 적 진영 피해 면역 (화 R3 룩 스킬 등) — 피해 0
            if (state.EnemyImmunityTurns > 0)
            {
                Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                    label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 피해 면역 (무효)");
                ApplyHitCdReduce(state, target);   // 피격 자체는 발생 → 일 쿨감 패시브는 트리거
                return;
            }

            // 보호막 흡수: 보호막이 있으면 그만큼 먼저 깎이고 점수에 집계되지 않음. 파괴되면 잔여만 HP로(점수 집계).
            if (target.Shield > 0)
            {
                double absorbed = Math.Min(target.Shield, dmg);
                target.Shield -= absorbed;
                dmg -= absorbed;
                bool broke = target.Shield <= 0;
                Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                    label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 보호막 흡수 {absorbed:N0}{(broke ? " (보호막 파괴)" : $" (잔여 {target.Shield:N0})")} — 점수 미집계");
                ApplyHitCdReduce(state, target);
                if (dmg <= 0) return;   // 전부 보호막에 흡수 → HP 피해 없음
                // 파괴 후 잔여 피해는 HP로 (아래 점수 집계). def0 기준이라 파괴 1타는 약간 과대(근사).
            }

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

            ApplyHitCdReduce(state, target);   // 피격 시 쿨감 패시브(일요일 몹)
        }

        /// <summary>일요일 몹 패시브: 피격된 적이 쿨감 패시브 보유 시, 공격력 최고 적의 스킬 쿨 N초 감소.</summary>
        private void ApplyHitCdReduce(SiegeBattleState state, SiegeEnemyState hit)
        {
            double red = hit.Source?.SiegeHitCdReduce ?? 0;
            if (red <= 0) return;
            var top = state.Enemies.OrderByDescending(e => e.FinalAtk).FirstOrDefault();
            if (top == null) return;
            foreach (var k in top.SkillCooldowns.Keys.ToList())
                top.SkillCooldowns[k] = Math.Max(0, top.SkillCooldowns[k] - red);
        }

        #endregion

        #region 타겟팅 / 데미지 계산

        /// <summary>
        /// 타겟 선정: 약점공격(시뮬은 결정론적으로 항상 발동) = 생명력 최저 적, 동률이면 앞열(Position 낮은 순).
        /// 라운드3에서는 약점공격 대상이 항상 보스(3보스 중 최저 HP).
        /// </summary>
        /// <summary>스킬 타겟 수만큼 적 선정 (최저 HP 우선·동률 앞열). 광역기는 여러 적을 친다. R3는 보스만.</summary>
        private List<SiegeEnemyState> PickTargets(SiegeBattleState state, int count)
        {
            IEnumerable<SiegeEnemyState> candidates = state.CurrentRound >= 3
                ? state.Enemies.Where(e => e.IsBoss)
                : state.Enemies;
            var list = candidates.ToList();
            if (list.Count == 0) list = state.Enemies;
            return list.OrderBy(e => e.CurrentHp).ThenBy(e => e.Position).Take(System.Math.Max(1, count)).ToList();
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

            // 시전자 데미지 스탯 = 전투시작 표시스탯 스냅샷(DisplayStats: gear/세트/초월/버프/패시브 포함)
            //   + 전투 중 추가된 스킬 버프(Effects 델타). 치명·약점은 확률 기반 기댓값으로 계산.
            var ds = ally.DisplayStats ?? new BaseStatSet();
            var midBuffs = ally.Effects.GetTotalBuffs();

            // 아군에 걸린 공격력 감소 디버프(예: 보스 레이첼 불새 공감24/마공감24) → 실효 공격력 차감.
            var allyDebuffs = ally.Effects.GetTotalDebuffs();
            double atkRed = character.AttackType == AttackType.Magic
                ? allyDebuffs.MagicAtk_Reduction : allyDebuffs.Atk_Reduction;
            double effAtk = ally.FinalAtk * System.Math.Max(0, 1 - atkRed / 100.0);

            var input = new DamageCalculator.DamageInput
            {
                Character = character,
                Skill = skill,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                TranscendLevel = battleChar.TranscendLevel,
                FinalAtk = effAtk,
                FinalDef = ally.FinalDef,
                FinalHp = ally.MaxHp,
                // 치명·약점: 100% 가정이 아니라 실제 확률로 기대 계수 (치확/약확 옵션이 의미를 갖도록)
                ExpectedCritWeak = true,
                CritChance = ds.Cri + midBuffs.Cri,
                CritDamage = ds.Cri_Dmg + midBuffs.Cri_Dmg,
                WeakChance = ds.Wek + midBuffs.Wek,
                WeakpointDmg = ds.Wek_Dmg + midBuffs.Wek_Dmg,
                DmgDealt = ds.Dmg_Dealt + midBuffs.Dmg_Dealt,
                DmgDealtType = ds.Dmg_Dealt_Type + midBuffs.Dmg_Dealt_Type + midBuffs.Mark_Energeia + midBuffs.Mark_Purify,
                DmgDealtBoss = ds.Dmg_Dealt_Bos + midBuffs.Dmg_Dealt_Bos,
                Dmg1to3 = ds.Dmg_Dealt_1to3 + midBuffs.Dmg_Dealt_1to3,
                Dmg4to5 = ds.Dmg_Dealt_4to5 + midBuffs.Dmg_Dealt_4to5,
                ArmorPen = ds.Arm_Pen + midBuffs.Arm_Pen,
                BossDef = enemy.Stats.Def,   // 보호막 흡수는 ApplyDamage에서 처리(보호막량만 미집계, 점수 일관성 위해 def 유지)
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
                // 잃은HP 비례 보너스(예: 광풍참 +50%) — 대상 실제 잔여HP%로 비례. R3 보스 음수HP면 0%잔여→풀보너스.
                IsLostHpConditionMet = true,
                LostHpActualRemainingPct = target.MaxHp > 0 ? target.CurrentHp / target.MaxHp * 100.0 : 0,
                Mode = BattleMode.Boss,
                IsTargetBoss = target.IsBoss,
                SelfMaxHp = ally.MaxHp,
            };
            var dr = _damageCalc.Calculate(input);
            double raw = dr.FinalDamage;

            // 공성전 감쇄 = 곱연산 (받는 물/마 피해 N% 감소 × 타겟수별 감소). 합연산 시 180%↑ 차감으로 음수가 되므로.
            double reductionMult = (1 - elemReduction / 100.0) * (1 - targetReduction / 100.0);
            double final = raw * Math.Max(0, reductionMult);

            // [진단] 추적 스킬 타격 시 입력 구성요소 덤프
            if (DiagSkillName != null && skill.Name == DiagSkillName && _diagCount < 8)
            {
                _diagCount++;
                DiagLog.AppendLine($"───────── {ally.Source.Character.Name} {skill.Name} → {target.Source.Name}(보스={target.IsBoss}, HP {target.CurrentHp:N0}/{target.MaxHp:N0}) ─────────");
                DiagLog.AppendLine($"  FinalAtk={input.FinalAtk:N0} 치피={input.CritDamage} 약피={input.WeakpointDmg}");
                DiagLog.AppendLine($"  [아군버프] 피증={input.DmgDealt} 타입피증={input.DmgDealtType} 보스피증={input.DmgDealtBoss} 3인기={input.Dmg1to3} 방관={input.ArmorPen}");
                DiagLog.AppendLine($"  [적디버프] 방깎={input.DefReduction} 취약={input.Vulnerability} 받피증={input.DmgTakenIncrease} 보스취약={input.BossVulnerability}");
                DiagLog.AppendLine($"  [조건] 조건충족={input.IsSkillConditionMet}(현HP%={(input.TargetHp>0?input.TargetCurrentHp/input.TargetHp*100:0):F0}) 방무(스킬초월포함)→ 방어계수={dr.DefCoefficient:F3} 치명계수={dr.CritMultiplier:F3} 약점계수={dr.WeakpointMultiplier:F3}");
                DiagLog.AppendLine($"  raw={raw:N0} × 감쇄{reductionMult:F3}(물마{elemReduction}/타겟{targetReduction}) = {final:N0}");
            }
            return final;
        }

        private static readonly System.Collections.Generic.HashSet<string> DealerRoles = new() { "공격형", "마법형", "만능형" };

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

                var names = string.Join("·", e.StatusImmunity.Types.Select(t => StatusEffectDb.Get(t)?.Name ?? t.ToString()));
                string scope = e.Target == EffectTarget.Party ? "전체 아군" : ally.Source.Character.Name;
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "면역", 0,
                    $"{scope} {names} 면역 [{e.StatusImmunity.Duration}턴] (평타 트리거)");
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
                var baseEffect = StatusEffectDb.Get(se.Type);
                string stName = baseEffect?.Name ?? se.Type.ToString();
                if (ally.StatusImmunityTurns.GetValueOrDefault(se.Type) > 0)
                {
                    // 면역 → 무효 (로그로 표기)
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "면역", 0,
                        $"{stName} 무효 (면역)");
                    continue;
                }
                if (baseEffect == null) continue;
                int dur = se.Duration > 0 ? se.Duration : baseEffect.Duration;
                ally.Effects.AddEffect(new BattleEffect
                {
                    Id = $"siege_cc:{ally.PartyIndex}:{se.Type}",
                    SourceName = $"siege_cc:{ally.PartyIndex}:{se.Type}",
                    StatusType = se.Type,
                    StatusData = StatusEffectData.FromDbEffect(baseEffect),
                    RemainingTurns = dur,
                    IsPermanent = false,
                    MergeStrategy = MergeStrategy.Stack,
                });
                Log(state, "적", false, ActionType.DebuffApplied, stName, 0,
                    $"{ally.Source.Character.Name} {stName} 부여 [{dur}턴]");
            }

            // 적 스킬의 스탯 디버프(공격력/방어력 감소 등)를 아군에 적용 (보스 관점 Target=Enemy = 아군).
            // 예: 레이첼 불새 → 아군 공감24·마공감24·방깎36 [5턴]. 미호 1스킬 등으로 해제 가능.
            var effects = enemySkill.GetLevelData(false)?.Effects;
            if (effects != null)
            {
                foreach (var e in effects)
                {
                    if (e.Type != SkillEffectType.Debuff || e.Debuff == null) continue;
                    int dur = e.Duration > 0 ? e.Duration : 99;
                    ally.Effects.AddEffect(new BattleEffect
                    {
                        Id = $"siege_enemydebuff:{ally.PartyIndex}:{enemySkill.Name}",
                        SourceName = $"siege_enemydebuff:{ally.PartyIndex}:{enemySkill.Name}",
                        Category = EffectCategory.ActiveDebuff,
                        Target = EffectTarget.Self,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = dur,
                        DebuffValues = e.Debuff.Clone(),
                    });
                    Log(state, "적", false, ActionType.DebuffApplied, enemySkill.Name, 0,
                        $"{ally.Source.Character.Name} {SummarizeDebuff(e.Debuff)} [{dur}턴]");
                }
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

        /// <summary>라운드 시작 시: 아군 패시브의 적 대상 디버프 + 펫 디버프를 라운드 내 전체 적에 상시 적용.</summary>
        private void ApplyStandingEnemyDebuffs(SiegeBattleState state, SiegeBattleConfig config)
        {
            var summaries = new List<string>();
            foreach (var enemy in state.Enemies)
            {
                // 아군 패시브 적 디버프 (예: 비스킷 방깎)
                foreach (var ally in state.AllyStates.Where(a => !a.IsDead))
                {
                    var passive = ally.Source.Character.Passive;
                    if (passive == null) continue;
                    foreach (var e in GetPassiveEffects(passive, ally))
                    {
                        if (e.Type != PersistentEffectType.Debuff || e.Debuff == null) continue;
                        if (e.Target != EffectTarget.Enemy && e.Target != EffectTarget.AllEnemies) continue;
                        if (e.ApplyMode == ApplyMode.Triggered) continue;   // 공격 시 발동형(예: 타카 취약 스택)은 상시 아님 → 제외
                        AddEnemyDebuff(enemy, e.Debuff, 99, $"siege_pasdebuff:{ally.PartyIndex}:{enemy.Position}");
                    }
                }
                // 펫 디버프 (예: 윈디 보스취약)
                if (config.AllyPet != null)
                {
                    var pd = config.AllyPet.GetSkillDebuff(config.PetStar, config.PetEnhance);
                    if (pd != null && !string.IsNullOrEmpty(SummarizeDebuff(pd)))
                        AddEnemyDebuff(enemy, pd, 99, $"siege_petdebuff:{enemy.Position}");
                }
            }
            // 로그 (라운드당 1회): 적1 기준 디버프 요약
            var first = state.Enemies.FirstOrDefault();
            if (first != null)
            {
                var s = SummarizeDebuff(first.Effects.GetTotalDebuffs());
                if (!string.IsNullOrEmpty(s))
                    Log(state, "시스템", true, ActionType.DebuffApplied, "상시 디버프", 0,
                        $"R{state.CurrentRound} 적 상시 디버프: {s}");
            }
        }

        /// <summary>비트리거 패시브 면역(예: 풍연 빙결 면역[상시])을 매 턴 갱신 — 시전자 생존 동안 유지.</summary>
        private void ApplyStandingImmunities(SiegeBattleState state)
        {
            foreach (var ally in state.AllyStates.Where(a => !a.IsDead))
            {
                var passive = ally.Source.Character.Passive;
                if (passive == null) continue;
                foreach (var e in GetPassiveEffects(passive, ally))
                {
                    if (e.Type != PersistentEffectType.Immunity || e.StatusImmunity?.Types == null) continue;
                    if (e.ApplyMode == ApplyMode.Triggered) continue;   // 평타 트리거형은 TriggerAllyImmunity가 처리
                    IEnumerable<CharacterBattleState> targets =
                        e.Target == EffectTarget.Party ? state.AllyStates : new[] { ally };
                    foreach (var tgt in targets)
                        foreach (var type in e.StatusImmunity.Types)
                            tgt.StatusImmunityTurns[type] = Math.Max(
                                tgt.StatusImmunityTurns.GetValueOrDefault(type),
                                Math.Max(1, e.StatusImmunity.Duration));
                }
            }
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

        /// <summary>매 턴 각 적의 DoT 틱 → 점수 누적 + 캐릭별 기여 반영, 잔여턴 감소. 적 면역 중엔 DoT도 무효.</summary>
        private void TickEnemyDots(SiegeBattleState state)
        {
            bool immune = state.EnemyImmunityTurns > 0;
            if (state.EnemyImmunityTurns > 0) state.EnemyImmunityTurns--;   // 면역 잔여 턴 감소

            foreach (var enemy in state.Enemies)
            {
                // 보호막 지속턴 경과 (소진 전이라도 만료되면 소멸)
                if (enemy.ShieldTurns > 0 && --enemy.ShieldTurns <= 0) enemy.Shield = 0;

                foreach (var dot in enemy.ActiveDots)
                {
                    if (dot.TickDamage > 0 && !immune)
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

        /// <summary>
        /// 평타 발동형 쿨감 적용 (예: 라이언 평타 "자신과 공격력 최고 아군 스킬 쿨 9초 감소").
        /// 평타 스킬의 Effects 중 Buff.Cooldown_Reduction>0을 찾아 대상(Self / SelfAndHighestAtkAlly)의 쿨 감소.
        /// </summary>
        private void ApplyBasicAttackCdReduction(SiegeBattleState state, CharacterBattleState ally, Skill normal)
        {
            var lvl = normal?.GetLevelData(ally.Source.IsSkillEnhanced);
            if (lvl?.Effects == null) return;
            foreach (var e in lvl.Effects)
            {
                double cdr = e.Buff?.Cooldown_Reduction ?? 0;
                if (cdr <= 0) continue;

                var targets = new List<CharacterBattleState> { ally };   // 항상 자신 포함
                if (e.Target == EffectTarget.SelfAndHighestAtkAlly || e.TargetSelector == TargetSelector.HighestAtkAlly
                    || e.Target == EffectTarget.Party)
                {
                    // "자신과 공격력 최고 아군" = 자신 + 자신 제외 실효딜 최고 아군 (raw atk 아닌 데미지 가중치)
                    var top = state.AllyStates.Where(a => !a.IsDead && a != ally)
                        .OrderByDescending(a => a.DamageWeight).ThenByDescending(a => a.FinalAtk).FirstOrDefault();
                    if (top != null) targets.Add(top);
                }
                foreach (var t in targets) t.ReduceCooldowns(cdr);
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, normal.Name, 0,
                    $"평타 쿨감 {cdr:0}초: {string.Join(",", targets.Select(t => t.Source.Character.Name))}");
            }
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
            // 쿨 충족 스킬 중 기본 쿨타임이 긴 핵심 스킬 우선 (버퍼의 버프 스킬이 쿨이 길어 우선 시전됨).
            // 동률은 SkillType 높은 순(궁→…→1). 평타 제외.
            bool enh = ally.Source.IsSkillEnhanced;
            int tr = ally.Source.TranscendLevel;
            return ally.Source.Character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .Where(s => ally.IsSkillReady(s.SkillType))
                .OrderByDescending(s => s.GetCooldown(enh, tr))
                .ThenByDescending(s => s.SkillType)
                .FirstOrDefault();
        }

        /// <summary>한 아군이 스킬 1개를 시전 (데미지·DoT·스택·버프/디버프·쿨·시간경과). 스킬턴 본체.</summary>
        private void ExecuteAllySkill(SiegeBattleState state, int allyIdx, Skill skill)
        {
            var ally = state.AllyStates[allyIdx];
            int tc = System.Math.Max(1, skill.GetTargetCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel));
            var targets = PickTargets(state, tc);
            if (targets.Count == 0) return;
            foreach (var target in targets)
            {
                double dmg = CalcDamageToEnemy(ally, target, skill);
                ApplyDamage(state, ally, target, dmg, skill.Name, isSkill: true);
                RegisterDotToEnemy(state, ally, target, skill);   // 스킬의 DoT(화상·출혈 등) 등록
            }
            ProcessAttackStacks(state, ally, isSkill: true);   // 공격 발동형 스택(타카 EagleClaw) — 스킬 발동 시 1회
            ApplySkillEffects(state, ally, skill, targets);    // 스킬 버프(아군)/디버프(적) 적용 + 로그
            state.AllyRotationCursor = (allyIdx + 1) % state.AllyStates.Count;   // 다음 자동 스킬턴은 다음 아군부터

            double cd = skill.GetCooldown(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel);
            if (cd > 0) ally.SkillCooldowns[skill.SkillType] = cd;
            AdvanceTime(state, GetActionDuration(skill));   // 스킬 소요시간만큼 전체 쿨다운 감소
        }

        /// <summary>이번 아군 스킬턴에 가능한 행동 후보(살아있고 CC 아닌 아군 × 준비된 비평타 스킬) + Hold. (빔서치용)</summary>
        private List<RotationDecision> CollectAllyChoices(SiegeBattleState state)
        {
            var choices = new List<RotationDecision>();
            for (int ai = 0; ai < state.AllyStates.Count; ai++)
            {
                var a = state.AllyStates[ai];
                if (a.IsDead || a.Effects.HasActionBlockingCC()) continue;
                foreach (var s in a.Source.Character.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (s.SkillType == SkillType.Normal || s.SkillType == SkillType.Normal2) continue;
                    if (!a.IsSkillReady(s.SkillType)) continue;
                    choices.Add(new RotationDecision { HeroIndex = ai, Skill = s.SkillType });
                }
            }
            choices.Add(new RotationDecision { Hold = true });
            return choices;
        }

        // ===== 로깅 보조 =====

        private void Log(SiegeBattleState state, string actor, bool isAlly, ActionType type,
            string skillName, double dmg, string desc)
            => state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn, ActorName = actor, IsAlly = isAlly,
                ActionType = type, SkillName = skillName, DamageDealt = dmg, Description = desc,
            });

        /// <summary>BuffSet의 0이 아닌 필드를 짧은 한글 문자열로.</summary>
        private static string SummarizeBuff(BuffSet b)
        {
            if (b == null) return "";
            var p = new List<string>();
            void A(double v, string k) { if (v != 0) p.Add($"{k}{v:0.#}"); }
            A(b.Atk_Rate, "공%"); A(b.MagicAtk_Rate, "마공%"); A(b.Def_Rate, "방%"); A(b.Hp_Rate, "체%");
            A(b.Cri, "치확"); A(b.Cri_Dmg, "치피"); A(b.Wek, "약확"); A(b.Wek_Dmg, "약피");
            A(b.Dmg_Dealt, "피증"); A(b.Dmg_Dealt_Type, "타입피증"); A(b.Dmg_Dealt_Bos, "보스피증");
            A(b.Dmg_Dealt_1to3, "1-3인기"); A(b.Dmg_Dealt_4to5, "4-5인기");
            A(b.Arm_Pen, "방관"); A(b.Dmg_Rdc, "받피감"); A(b.Eff_Hit, "효적"); A(b.Eff_Res, "효저");
            A(b.Heal_Bonus, "받회"); A(b.Blk, "막기"); A(b.Blessing, "축복"); A(b.Cooldown_Reduction, "쿨감");
            return string.Join(" ", p);
        }

        /// <summary>DebuffSet의 0이 아닌 필드를 짧은 한글 문자열로.</summary>
        private static string SummarizeDebuff(DebuffSet d)
        {
            if (d == null) return "";
            var p = new List<string>();
            void A(double v, string k) { if (v != 0) p.Add($"{k}{v:0.#}"); }
            A(d.Def_Reduction, "방깎"); A(d.Vulnerability, "취약"); A(d.Boss_Vulnerability, "보스취약");
            A(d.Dmg_Taken_Increase, "받피증"); A(d.Phys_Dmg_Taken_Increase, "받물피증"); A(d.Mag_Dmg_Taken_Increase, "받마피증");
            A(d.Atk_Reduction, "공감"); A(d.MagicAtk_Reduction, "마공감"); A(d.Dmg_Reduction, "피감");
            A(d.Heal_Reduction, "받회감"); A(d.Cri_Reduction, "치확감"); A(d.Wek_Reduction, "약확감");
            A(d.Spd_Reduction, "속감"); A(d.Cooldown_Increase, "쿨증"); A(d.Eff_Red, "효저깎");
            return string.Join(" ", p);
        }

        /// <summary>
        /// 아군 스킬이 선언한 버프(아군)·디버프(적)를 실제 적용하고 로그를 남긴다.
        /// 버프: Self→시전자, Party→전체(HighestAtkAlly 셀렉터면 공격력 상위 N명). 디버프: 피격 적(hitEnemies).
        /// 새 Effects 리스트 + 레거시 SelfBuff/PartyBuff/DebuffEffect + 초월 보너스 모두 처리.
        /// </summary>
        private void ApplySkillEffects(SiegeBattleState state, CharacterBattleState ally, Skill skill,
            List<SiegeEnemyState> hitEnemies)
        {
            bool enh = ally.Source.IsSkillEnhanced;
            string actor = ally.Source.Character.Name;
            var lvl = skill.GetLevelData(enh);
            if (lvl == null) return;

            // 버프를 대상 아군들에 적용
            void ApplyBuff(EffectTarget target, TargetSelector? selector, int tgtCount, BuffSet b, int dur)
            {
                var s = SummarizeBuff(b);
                if (string.IsNullOrEmpty(s)) return;
                var targets = ResolveAllyBuffTargets(state, ally, target, selector, tgtCount);
                int d = dur > 0 ? dur : 99;
                foreach (var t in targets)
                    t.Effects.AddEffect(new BattleEffect
                    {
                        Id = $"siege_skbuff:{actor}:{skill.Name}:{t.PartyIndex}",
                        SourceName = $"siege_skbuff:{actor}:{skill.Name}:{t.PartyIndex}",
                        Category = EffectCategory.ActiveSelfBuff,
                        Target = EffectTarget.Self,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = d,
                        BuffValues = b.Clone(),
                    });
                string names = string.Join(",", targets.Select(t => t.Source.Character.Name));
                Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0, $"버프 {names}: {s} [{d}턴]");
            }

            // 디버프를 피격 적들에 적용 (없으면 적용 안 함)
            void ApplyDebuff(DebuffSet dd, int dur, EffectTarget target)
            {
                var s = SummarizeDebuff(dd);
                if (string.IsNullOrEmpty(s)) return;
                var targets = target == EffectTarget.AllEnemies ? state.Enemies : hitEnemies;
                int d = dur > 0 ? dur : 99;
                foreach (var en in targets)
                    AddEnemyDebuff(en, dd, d, $"siege_skdebuff:{actor}:{skill.Name}:{en.Position}");
                if (targets.Count > 0)
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"디버프 적{targets.Count}: {s} [{d}턴]");
            }

            // 초월 보너스 (대상수 변경 등). 6초월 비스킷 장비강화 = 버프 대상 2명 등.
            var tr = skill.GetTranscendBonus(ally.Source.TranscendLevel);
            int? buffTgtOverride = tr?.TargetCountOverride;   // HighestAtkAlly 버프 대상 수 오버라이드

            void HandleEffects(List<SkillEffect> effects)
            {
                if (effects == null) return;
                foreach (var e in effects)
                {
                    if (e.Type == SkillEffectType.Buff && e.Buff != null)
                    {
                        int tc = (e.TargetSelector == TargetSelector.HighestAtkAlly && buffTgtOverride.HasValue)
                            ? buffTgtOverride.Value : e.TargetCount;
                        ApplyBuff(e.Target, e.TargetSelector, tc, e.Buff, e.Duration);
                    }
                    else if (e.Type == SkillEffectType.Debuff && e.Debuff != null)
                        ApplyDebuff(e.Debuff, e.Duration, e.Target);
                    else if (e.Type == SkillEffectType.DebuffCleanse && e.DispelDebuffCount > 0)
                    {
                        // 아군 후열 디버프 해제 (예: 미호 초월2). 보스의 공감 등 디버프를 제거 → 딜 회복.
                        foreach (var a in state.AllyStates.Where(x => !x.IsDead && x.Source.IsBackPosition))
                        {
                            int removed = a.Effects.RemoveDebuffs(e.DispelDebuffCount);
                            if (removed > 0)
                                Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                    $"{a.Source.Character.Name} 디버프 {removed}개 해제");
                        }
                    }
                    else if (e.Type == SkillEffectType.BuffDispel && e.DispelBuffCount > 0)
                    {
                        // 적 버프 해제 — 보스 보호막(예: 루디) 즉시 제거 → 딜이 점수로 들어가기 시작.
                        foreach (var en in hitEnemies.Where(x => x.Shield > 0))
                        {
                            Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                                $"{en.Source.Name} 보호막 {en.Shield:N0} 버프해제로 제거");
                            en.Shield = 0; en.ShieldTurns = 0;
                        }
                    }
                }
            }

            HandleEffects(lvl.Effects);
            // 레거시 필드
            if (lvl.SelfBuff != null) ApplyBuff(EffectTarget.Self, null, 1, lvl.SelfBuff, lvl.EffectDuration);
            if (lvl.PartyBuff != null) ApplyBuff(EffectTarget.Party, null, 0, lvl.PartyBuff, lvl.EffectDuration);
            if (lvl.DebuffEffect != null) ApplyDebuff(lvl.DebuffEffect, lvl.EffectDuration, EffectTarget.Enemy);

            if (tr != null)
            {
                HandleEffects(tr.Effects);
                if (tr.PartyBuff != null) ApplyBuff(EffectTarget.Party, null, 0, tr.PartyBuff, 0);
                if (tr.Debuff != null) ApplyDebuff(tr.Debuff, 0, EffectTarget.Enemy);
            }
        }

        /// <summary>버프 대상 아군 선정 (Self / Party / 공격력 상위 N명).</summary>
        private List<CharacterBattleState> ResolveAllyBuffTargets(SiegeBattleState state, CharacterBattleState caster,
            EffectTarget target, TargetSelector? selector, int tgtCount)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            if (target == EffectTarget.Self || target == EffectTarget.SingleAlly)
                return new List<CharacterBattleState> { caster };
            if (selector == TargetSelector.HighestAtkAlly)
                // 실효딜 가중치 상위 N명(딜러 우선). raw FinalAtk가 아니라 대표 스킬 데미지로 랭킹 →
                // 공%로 atk만 뻥튀기한 캐릭(예: 레이첼)이 버프를 가로채는 문제 방지. 비딜러(=0)는 후순위.
                return alive.OrderByDescending(a => a.DamageWeight).ThenByDescending(a => a.FinalAtk)
                    .Take(System.Math.Max(1, tgtCount)).ToList();
            return alive;   // Party 전체
        }

        /// <summary>
        /// 아군 공격 발동형 스택 디버프 처리 (예: 타카 EagleClaw — 스택당 취약4%·받물3%, 최대 8).
        /// **트리거: 기본공격(평타) 2회마다 1회 OR 스킬 발동 시 즉시 1회**(타겟 수 무관).
        /// 트리거 시 **적군 3명**(R3=보스3, 그 외 최저HP 3)에게 +StacksPerTrigger. 라운드별 리셋, 적별 누적.
        /// </summary>
        private void ProcessAttackStacks(SiegeBattleState state, CharacterBattleState ally, bool isSkill)
        {
            var passive = ally.Source.Character.Passive;
            var lvl = passive?.GetLevelData(ally.Source.IsSkillEnhanced);
            if (lvl?.Effects == null) return;
            var trEffects = passive.GetTranscendBonus(ally.Source.TranscendLevel)?.Effects;

            foreach (var baseEffect in lvl.Effects)
            {
                // 초월이 같은 StatusType을 정의하면 override (단일보스 sim과 동일). 초월 데이터에 둘 다 명시됨.
                var effect = baseEffect;
                if (trEffects != null)
                {
                    var ov = trEffects.FirstOrDefault(e => e.StatusType == baseEffect.StatusType && e.StatusType != StatusEffectType.None);
                    if (ov != null) effect = ov;
                }
                if (effect.ApplyMode != ApplyMode.Triggered || effect.MaxStacks <= 0) continue;
                if (effect.Type != PersistentEffectType.Debuff || effect.Debuff == null) continue;
                if (effect.Target != EffectTarget.Enemy && effect.Target != EffectTarget.AllEnemies) continue;
                bool matches = effect.TriggerCondition switch
                {
                    TriggerCondition.AllAttack => true,
                    TriggerCondition.SkillOnly => isSkill,
                    TriggerCondition.NormalOnly => !isSkill,
                    _ => false,
                };
                if (!matches) continue;

                var perStack = effect.Debuff;

                // 트리거 판정: 스킬 발동 시 즉시 1회. 평타는 TriggerCount회(=2)마다 1회. 카운터는 평타에만, 라운드별 리셋.
                if (!isSkill)
                {
                    string ckey = $"siege_stackcnt:{ally.PartyIndex}:{effect.StatusType}:R{state.CurrentRound}";
                    ally.StackTriggerCounters.TryGetValue(ckey, out int cnt);
                    cnt++;
                    if (cnt < System.Math.Max(1, effect.TriggerCount)) { ally.StackTriggerCounters[ckey] = cnt; continue; }
                    ally.StackTriggerCounters[ckey] = 0;
                }

                // 발동 시 적군 3명에게 스택 부여 (보스 포함). 적별로 누적.
                foreach (var en in PickTargets(state, 3))
                {
                    string skey = $"siege_stack:{ally.PartyIndex}:{effect.StatusType}:R{state.CurrentRound}:{en.Position}";
                    ally.CurrentStacks.TryGetValue(skey, out int st);
                    int newStacks = System.Math.Min(st + effect.StacksPerTrigger, effect.MaxStacks);
                    ally.CurrentStacks[skey] = newStacks;

                    var scaled = ScaleDebuff(perStack, newStacks);
                    AddEnemyDebuff(en, scaled, 99, skey);   // 같은 key 갱신(스택 증가분 반영)
                    Log(state, ally.Source.Character.Name, true, ActionType.DebuffApplied,
                        StatusEffectDb.Get(effect.StatusType)?.Name ?? effect.StatusType.ToString(), 0,
                        $"{en.Source.Name} {effect.StatusType} {newStacks}스택: {SummarizeDebuff(scaled)}");
                }
            }
        }

        /// <summary>디버프를 스택 수만큼 합산 (스택당 효과).</summary>
        private static DebuffSet ScaleDebuff(DebuffSet b, int stacks)
        {
            var r = new DebuffSet();
            for (int i = 0; i < System.Math.Max(1, stacks); i++) r.Add(b);
            return r;
        }

        /// <summary>적 1명에 디버프 누적(중복 방지: 같은 Id 갱신).</summary>
        private void AddEnemyDebuff(SiegeEnemyState enemy, DebuffSet d, int dur, string id)
        {
            enemy.Effects.RemoveBySource(id);
            enemy.Effects.AddEffect(new BattleEffect
            {
                Id = id,
                SourceName = id,
                Category = EffectCategory.ActiveDebuff,
                Target = EffectTarget.Enemy,
                MergeStrategy = MergeStrategy.MaxMerge,
                IsPermanent = dur >= 99,
                RemainingTurns = dur,
                DebuffValues = d.Clone(),
            });
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
                ElapsedSeconds = state.ElapsedSeconds,
                TurnLogs = state.TurnLogs,
                DecisionPoints = state.DecisionPoints,
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

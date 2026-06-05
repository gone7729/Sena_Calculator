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
        public string DiagSkillName;          // 추적할 스킬명 1개 (null이면 비활성, 후방 호환)
        public List<string> DiagSkillNames;   // 추적할 스킬명 여러개 (null이면 비활성)
        public System.Text.StringBuilder DiagLog = new();
        private int _diagCount;

        // [진단] true면 메인 패스 DamageWeight 확정 직후 각 아군의 raw FinalAtk·DamageWeight·MaxHp를 DiagLog에 덤프.
        //   라이언쿨감·비스킷 버프가 누구(=DamageWeight 1위)에게 가는지, raw atk 순위와 어긋나는지 확인용.
        public bool DiagAllyStats;

        // [빌드 실행가능성 계측] config.RecordFeasibility=true(최종 빌드 재생)일 때만 동작 — 점수/전투 불변.
        //   _readySince[(아군PartyIndex, 스킬)] = 그 스킬이 마지막 시전 후 쿨 0에 도달한 경과초(준비완료시각).
        //   double.PositiveInfinity = 아직 쿨 중(준비 전). 시전 전 한번도 안 쓴 스킬은 dict에 없음 = 0초부터 준비.
        private bool _recordFeasibility;
        private readonly Dictionary<(int Idx, SkillType Skill), double> _readySince = new();
        private readonly List<BuildStepFeasibility> _feasLog = new();

        // [반격 오버라이드] config.CounterattackChanceOverride. null=보스 정의값(25%), 0=OFF.
        //   빔서치는 0(반격 RNG·시간경과 무의존)으로 로테 산출, 최종 점수·기어·생존반지는 null(ON)로 평가.
        //   금요일(제이브) 외 보스는 Counterattack=null이라 무관(월화수목토 무영향).
        private double? _counterChanceOverride;
        private bool IsDiagSkill(string name)
            => (DiagSkillName != null && name == DiagSkillName)
            || (DiagSkillNames != null && DiagSkillNames.Contains(name));

        public SiegeBattleResult Simulate(SiegeBattleConfig config) => Simulate(config, computeWeights: true);

        private SiegeBattleResult Simulate(SiegeBattleConfig config, bool computeWeights)
        {
            // 실행가능성 계측은 최종(메인) 패스에만 — 스카우팅 서브시뮬(computeWeights=false)은 결과 폐기되므로 제외.
            _recordFeasibility = config.RecordFeasibility && computeWeights;
            _counterChanceOverride = config.CounterattackChanceOverride;   // 반격 확률 오버라이드(빔=0, 최종=null)
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

                if (DiagAllyStats)
                {
                    // raw FinalAtk 1위 vs DamageWeight 1위 — 둘이 다르면 버프 배분(인게임=raw atk, 시뮬=DamageWeight)이 어긋남.
                    var rawTop = state.AllyStates.OrderByDescending(a => a.FinalAtk).FirstOrDefault()?.Source.Character.Name;
                    var dwTop = state.AllyStates.OrderByDescending(a => a.DamageWeight).FirstOrDefault()?.Source.Character.Name;
                    DiagLog.AppendLine($"───── [아군 스탯 스냅샷] rawAtk 1위={rawTop} / DamageWeight 1위={dwTop} {(rawTop == dwTop ? "(일치)" : "★불일치")} ─────");
                    foreach (var a in state.AllyStates.OrderByDescending(a => a.DamageWeight))
                        DiagLog.AppendLine($"  {a.Source.Character.Name,-6}({a.Source.Character.Type}) FinalAtk={a.FinalAtk,9:N0}  DamageWeight(스카우팅딜)={a.DamageWeight,12:N0}  MaxHp={a.MaxHp,9:N0}");
                }
            }
            // computeWeights=false(스카우팅): DamageWeight=0 → raw FinalAtk 타게팅(원래 동작)
            RunTurnLoop(config, state);
            if (_recordFeasibility && config.RotationPlan != null)
                AppendUnreachedFeasSteps(config.RotationPlan, state);
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

                // 효과 잔여턴/DoT는 각 캐릭터의 행동 직후에 처리(per-character-action 모델).
                //   글로벌 sim-turn tick이 아니라, 행동자(actor)만 tick — n턴 지속 = "부여받은 캐릭의 n번 행동".
                //   이렇게 해야 따뜻한울림 3턴(보스 3번 행동≈24 sim-turn) 같은 셋업이 후속 nuke까지 닿는다.
                // 전역 효과(EnemyImmunityTurns: 적 진영 피해 면역)만 매 sim-turn 글로벌 감소.
                if (state.EnemyImmunityTurns > 0) state.EnemyImmunityTurns--;

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

            var ordered = actors
                .GroupBy(x => x.Spd)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => OrderSameSpeed(g.ToList()))
                .ToList();

            // 속공 순서 로그 — 누가 어떤 속공으로 이 순서가 됐는지 (라운드별 행동 순서)
            Log(state, "행동순서", true, ActionType.BuffApplied, "속공순", 0,
                $"R{state.CurrentRound} 기본공격 순서: " + string.Join(" > ", ordered.Select(o =>
                    $"{(o.IsAlly ? o.Ally.Source.Character.Name : o.Enemy.Source.Name)}(속공{o.Spd:F0}{(o.IsAlly ? "" : "·적")})")));
            return ordered;
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
                    if (dec.Hold)
                    {
                        if (_recordFeasibility) RecordFeasStep(state, stIdx, dec, null, null, true, null, 0, 0);
                        return;   // 홀드: 스킬턴 스킵
                    }
                    if (dec.HeroIndex >= 0 && dec.HeroIndex < state.AllyStates.Count)
                    {
                        var a = state.AllyStates[dec.HeroIndex];
                        var sk = a.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == dec.Skill);
                        bool ready = sk != null && a.IsSkillReady(dec.Skill);
                        if (!a.IsDead && !a.Effects.HasActionBlockingCC() && ready)
                        {
                            if (_recordFeasibility)
                            {
                                // 재시전(쿨 제약 받음)만 slack이 의미. 첫 시전은 readySince 키가 없음(쿨 무관).
                                bool gated = _readySince.TryGetValue((dec.HeroIndex, dec.Skill), out var rs)
                                             && !double.IsPositiveInfinity(rs);
                                double slack = gated ? state.ElapsedSeconds - rs : 0;
                                RecordFeasStep(state, stIdx, dec, a, sk, true, null, 0, slack, gated);
                            }
                            ExecuteAllySkill(state, dec.HeroIndex, sk);
                            return;
                        }
                        if (_recordFeasibility)
                        {
                            // 폴백 사유 판정 (우선순위: 사망 → CC → 스킬없음 → 쿨 미충족).
                            double cdRem = sk != null ? a.SkillCooldowns.GetValueOrDefault(dec.Skill) : 0;
                            string reason = a.IsDead ? "시전자 사망"
                                : a.Effects.HasActionBlockingCC() ? "행동불가 CC"
                                : sk == null ? "스킬 없음"
                                : $"쿨 {cdRem:0.#}초 남음";
                            RecordFeasStep(state, stIdx, dec, a, sk, false, reason, cdRem, 0);
                        }
                    }
                    else if (_recordFeasibility)
                        RecordFeasStep(state, stIdx, dec, null, null, false, "잘못된 영웅 인덱스", 0, 0);
                    // 무효 → 자동 폴백
                }

                // 자동: 라운드로빈(AllyRotationCursor)으로 순회 → 첫 시전 가능한 아군이 PickAllySkill 시전.
                int n = state.AllyStates.Count;
                for (int k = 0; k < n; k++)
                {
                    int idx = (state.AllyRotationCursor + k) % n;
                    var ally = state.AllyStates[idx];
                    if (ally.IsDead || ally.Effects.HasActionBlockingCC()) continue;
                    var skill = PickAllySkill(ally, state);
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
                    var recast = skill.GetLevelData(false)?.OnKillRecast;
                    foreach (var target in PickRandomAllies(state, targetCount))
                    {
                        bool wasAlive = !target.IsDead;
                        double dmg = CalcDamageToAlly(enemy, target, skill);
                        ApplyDamageToAlly(state, enemy, target, dmg, skill.Name);
                        ApplyEnemyStatusToAlly(state, target, skill);

                        // OnKillRecast: 직접 피해로 아군 처치 시 RatioPercent% 위력으로 1회 재시전(연쇄 1회 한도).
                        if (recast != null && recast.RatioPercent > 0 && wasAlive && target.IsDead)
                        {
                            var rt = PickRandomAlly(state);
                            if (rt != null)
                            {
                                double rdmg = CalcDamageToAlly(enemy, rt, skill) * recast.RatioPercent / 100.0;
                                ApplyDamageToAlly(state, enemy, rt, rdmg, skill.Name + "(처치 재시전)");
                                ApplyEnemyStatusToAlly(state, rt, skill);
                            }
                        }
                    }

                    // 적 진영 전체 피해 면역 부여 (화 R3 룩 등)
                    int imm = skill.GetLevelData(false)?.GrantEnemyImmunityTurns ?? 0;
                    if (imm > 0)
                    {
                        state.EnemyImmunityTurns = Math.Max(state.EnemyImmunityTurns, imm);
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"적 진영 피해 면역[{imm}턴]");
                    }

                    // 보스 자기 보호막 생성 (예: 루디 방어 준비 = 방어력 100배). 아군 피해를 흡수(점수 미집계)·버프해제로 제거.
                    var sld = skill.GetLevelData(false);
                    if (sld != null && sld.SelfShieldDefRatio > 0)
                    {
                        enemy.Shield = enemy.Source.Stats.Def * sld.SelfShieldDefRatio / 100.0;
                        enemy.ShieldTurns = sld.SelfShieldTurns > 0 ? sld.SelfShieldTurns : 99;
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"보호막 {enemy.Shield:N0} 생성 [{enemy.ShieldTurns}턴] (버프해제/소진 시 제거)");
                    }
                    // 상대(보스) 스킬 사용 → 그 스킬 소요시간(2스킬 5초·1스킬 4초)만큼 시간 경과 → 전체(아군+적) 쿨 감소.
                    //   [정정] 이전엔 별도 ReduceCooldowns(5)까지 더해 이중 감소 → 교만 등 쿨이 실시간보다 빨리 회복됐음.
                    //   시간경과(AdvanceTime)만으로 충분(쿨=실시간). 별도 -5초 메커니즘 제거.
                    double enemyCd = GetActionDuration(skill);
                    Log(state, enemy.Source.Name, false, ActionType.BuffApplied, "쿨감", 0,
                        $"적 {skill.Name}({skill.SkillType}) 시전 → 시간경과 {enemyCd:F0}초 → 전체 쿨 -{enemyCd:F0}초 (경과 {state.ElapsedSeconds:F0}초)");
                    AdvanceTime(state, GetActionDuration(skill));
                    // 자기 쿨은 시전 완료 시점부터 카운트(자기 컷신 시간엔 자기 쿨 안 깎임) → AdvanceTime 後 set.
                    double cd = skill.GetCooldown(false, 0);
                    if (cd > 0) enemy.SkillCooldowns[pri.SkillType] = cd;

                    // 스킬턴은 턴을 소모하지 않으므로 버프/디버프/DoT 턴 감소(tick) 없음 (게임 규칙 2-3).
                    //   효과 턴 감소는 기본공격(턴 소모) 시에만 — ExecuteBasicAttack의 TickEnemyAfterAction.
                    //   쿨다운(초 단위)은 위 AdvanceTime으로 스킬턴에도 정상 감소.
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
                            double dmg = CalcDamageToEnemy(ally, target, normal, state);
                            ApplyDamage(state, ally, target, dmg, normal.Name, isSkill: false);
                            RegisterDotToEnemy(state, ally, target, normal);   // 평타의 DoT(화상 등) 등록
                            MaybeEnemyCounter(state, target, normal.GetLevelData(ally.Source.IsSkillEnhanced)?.AtkCount ?? 1);
                        }
                        if (targets.Count > 0)
                        {
                            ProcessAttackStacks(state, ally, isSkill: false);   // 공격 발동형 스택 — 평타 2회마다 1회
                            TriggerAllyImmunity(state, ally);   // 기본공격 발동 → 면역 패시브 트리거(턴제 면역 갱신)
                            ApplyBasicAttackCdReduction(state, ally, normal);   // 평타 쿨감(예: 라이언 자신+최고공격력 아군 9초)
                            ApplySkillHeal(state, ally, normal);   // 평타 회복(예: 리나 강화평타 최저HP 아군 7%)
                        }
                    }
                }
            }
            else
            {
                // 적 기본공격 → 랜덤 아군 1명 피격 + 상태이상 부여
                var enemy = actor.Enemy;
                // 실명(라이언 강자사냥 등): 적 기본공격은 반드시 빗나감(피해·상태이상 없음). 스킬은 적중.
                //   기본공격 1회로 실명 1턴 소모 (per-action 모델). HP와 무관하게 차감.
                if (enemy.BlindTurnsRemaining > 0)
                {
                    enemy.BlindTurnsRemaining--;
                    Log(state, enemy.Source.Name, false, ActionType.NormalAttack, "기본공격", 0,
                        $"{enemy.Source.Name} 기본공격 빗나감 (실명, 잔여 {enemy.BlindTurnsRemaining}턴)");
                }
                else
                {
                    var normal = enemy.Source.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                    var target = PickRandomAlly(state);
                    if (normal != null && target != null)
                    {
                        double dmg = CalcDamageToAlly(enemy, target, normal);
                        ApplyDamageToAlly(state, enemy, target, dmg, normal.Name);
                        ApplyEnemyStatusToAlly(state, target, normal);
                    }
                }
            }

            AdvanceTime(state, GetActionDuration(null));
            // [보류] 약점공격 발동 시 평타 시간 -0.5초 단축(게임 규칙) → 1-per-turn 시뮬에선 다른 cd가 추가로 진행되는 효과.
            //   단축 시간(0.5초)이 정밀하게 측정된 값이 아니라 잠정 — 정확한 수치 확정 후 재활성화.
            // if (actor.IsAlly && actor.Ally != null)
            // {
            //     var allyDs = actor.Ally.DisplayStats;
            //     var allyBuffs = actor.Ally.Effects.GetTotalBuffs();
            //     double pw = System.Math.Max(0, System.Math.Min(100, (allyDs?.Wek ?? 0) + allyBuffs.Wek)) / 100.0;
            //     if (pw > 0) AdvanceTime(state, 0.5 * pw);
            // }

            // 행동자(actor)만 효과 tick (per-character-action 모델)
            if (actor.IsAlly && actor.Ally != null)
                TickAllyAfterAction(state, actor.Ally);
            else if (!actor.IsAlly && actor.Enemy != null && actor.Enemy.CurrentHp > 0)
                TickEnemyAfterAction(state, actor.Enemy);
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

            // 보스 피해 무효화 (델론즈 「죽음의 경계」: 아군 사망 시 부여) — 직격 1회 무효·1 차감, 점수 미집계.
            if (target.NullifyHitsRemaining > 0)
            {
                target.NullifyHitsRemaining--;
                Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                    label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 피해 무효 (죽음의 경계, 잔여 {target.NullifyHitsRemaining}회)");
                ApplyHitCdReduce(state, target);
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

            // R1/R2: 적 최대체력만큼만 점수 누적(오버킬 미집계). R3: 적 미사망이라 오버킬 누적.
            double scoredDmg = (state.CurrentRound < 3)
                ? System.Math.Max(0, System.Math.Min(dmg, target.CurrentHp))
                : dmg;
            target.CurrentHp -= dmg;            // HP 0 이하 허용 (무사망)
            target.TotalDamageTaken += dmg;
            ally.TotalDamageDealt += scoredDmg;
            state.TotalScore += scoredDmg;
            state.RoundScore[state.CurrentRound] = state.RoundScore.GetValueOrDefault(state.CurrentRound) + scoredDmg;

            // 흡혈: 활성 중이면 적에 준 피해의 LifestealRatio%만큼 시전자 회복 (미호 파티 흡혈 등)
            if (ally.LifestealTurns > 0 && ally.LifestealRatio > 0)
                HealAlly(state, ally, dmg * ally.LifestealRatio / 100.0, ally.Source.Character.Name, "흡혈");

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
        private double CalcDamageToEnemy(CharacterBattleState ally, SiegeEnemyState target, Skill skill, SiegeBattleState state = null)
        {
            var battleChar = ally.Source;
            var character = battleChar.Character;
            var enemy = target.Source;
            var baseStats = character.GetBaseStats();

            int targetCount = skill.GetTargetCount(battleChar.IsSkillEnhanced, battleChar.TranscendLevel);
            // 평타는 n인기 스킬이 아니므로 단일/광역 감쇄가 적용되지 않는다.
            double targetReduction = skill.SkillType == SkillType.Normal ? 0 : GetTargetReduction(enemy, targetCount);
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
            // 전투 중 부여된 공격%버프(예: 클로에 청소시간 마공20%)를 실효 공격력에 반영.
            //   ally.FinalAtk = 전투시작 스냅샷(상시 패시브·파티버프 포함, 액티브 스킬버프 제외).
            //   midBuffs는 전투 중 액티브 버프만 담으므로 스냅샷과 겹치지 않음 → 곱연산으로 stage-3 적용.
            double midAtkRate = character.AttackType == AttackType.Magic
                ? midBuffs.MagicAtk_Rate : midBuffs.Atk_Rate;
            double effAtk = ally.FinalAtk * (1 + midAtkRate / 100.0) * System.Math.Max(0, 1 - atkRed / 100.0);

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
            if (IsDiagSkill(skill.Name) && _diagCount < 200)
            {
                _diagCount++;
                int diagTurn = state?.CurrentTurn ?? -1;
                int diagRound = state?.CurrentRound ?? -1;
                DiagLog.AppendLine($"───────── T{diagTurn,2} R{diagRound} {ally.Source.Character.Name} {skill.Name} → {target.Source.Name}(보스={target.IsBoss}, HP {target.CurrentHp:N0}/{target.MaxHp:N0}) ─────────");
                DiagLog.AppendLine($"  FinalAtk(공감반영후)={input.FinalAtk:N0} (공감={atkRed:F0}%, 스냅FinalAtk={ally.FinalAtk:N0}) 치피={input.CritDamage} 약피={input.WeakpointDmg}");
                DiagLog.AppendLine($"  [아군버프] 피증={input.DmgDealt} 타입피증={input.DmgDealtType} 보스피증={input.DmgDealtBoss} 3인기={input.Dmg1to3} 방관={input.ArmorPen}");
                DiagLog.AppendLine($"  [적디버프] 방깎={input.DefReduction} 취약={input.Vulnerability} 받피증={input.DmgTakenIncrease} 보스취약={input.BossVulnerability}");
                DiagLog.AppendLine($"  [조건] 조건충족={input.IsSkillConditionMet}(현HP%={(input.TargetHp>0?input.TargetCurrentHp/input.TargetHp*100:0):F0}) 방무(스킬초월포함)→ 방어계수={dr.DefCoefficient:F3} 치명계수={dr.CritMultiplier:F3} 약점계수={dr.WeakpointMultiplier:F3}");
                string lostHpFlag = dr.LostHpMultiplier > 1 ? "적용" : "미발동/0";
                DiagLog.AppendLine($"  [잃은HP] 잔여HP%={input.LostHpActualRemainingPct:F1} → 보너스배수 ×{dr.LostHpMultiplier:F4} ({lostHpFlag})");
                DiagLog.AppendLine($"  raw={raw:N0} × 감쇄{reductionMult:F3}(물마{elemReduction}/타겟{targetReduction}) = {final:N0} [기대값: 치명{dr.CritMultiplier:F3}×약점{dr.WeakpointMultiplier:F3}]");
                // ★엑셀 "치명·약점" 칸과 사과:사과 비교용 — 치명+약점 둘다 발동한 결정값.
                input.ExpectedCritWeak = false;
                var drDet = _damageCalc.Calculate(input);
                input.ExpectedCritWeak = true;
                double finalDet = drDet.FinalDamage * System.Math.Max(0, reductionMult);
                DiagLog.AppendLine($"  ★결정값(치명+약점 둘다 발동)={finalDet:N0} [치명{drDet.CritMultiplier:F3}×약점{drDet.WeakpointMultiplier:F3}]");
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

        /// <summary>적 → 아군 데미지 (적 공격력 vs 아군 방어/받피감). 적은 치확·약확 0이라 비치명·비약점 기본.
        /// forceCrit/critDmgOverride: 반격 등 강제 치명(제이브 반격 치확100·치피650)용.</summary>
        private double CalcDamageToAlly(SiegeEnemyState enemy, CharacterBattleState ally, Skill enemySkill,
            bool forceCrit = false, double critDmgOverride = -1)
        {
            var e = enemy.Source;
            // 아군 받피감(자버프) + 아군에게 걸린 받피증/취약(적 디버프)
            var (perm, timed, pet) = ally.Effects.GetSeparatedBuffs();
            double allyDmgRdc = perm.Dmg_Rdc + timed.Dmg_Rdc + pet.Dmg_Rdc;
            // 5인기 받피감(Dmg_Rdc_Multi, 예: 비스킷 패시브 20%)은 적의 광역(4~5인) 공격에만 적용.
            int enemyTgtCount = enemySkill?.GetTargetCount(false, 0) ?? 1;
            if (enemyTgtCount >= 4)
                allyDmgRdc += perm.Dmg_Rdc_Multi + timed.Dmg_Rdc_Multi + pet.Dmg_Rdc_Multi;
            var allyDebuffs = ally.Effects.GetTotalDebuffs();
            // 적에 부여된 디버프 반영 (그간 enemy.Effects가 CalcDamageToAlly에서 무시됐음):
            //  - Atk_Reduction: 적 공격력 감소
            //  - Dmg_Reduction: 적이 입히는 피해 감소(피감, 비스킷 평타 강화 6%)
            //  - allyDebuffs.Def_Reduction: 적이 ally에 부여한 방깎(불새 방깎36) → ally 방어 감소
            var enemyDebuffs = enemy.Effects.GetTotalDebuffs();
            double effEnemyAtk = enemy.FinalAtk * System.Math.Max(0, 1 - enemyDebuffs.Atk_Reduction / 100.0);
            bool enemyCrit = forceCrit || e.Stats.Cri >= 100;        // 적 치확(보통 0; 반격은 강제)
            double critDmg = critDmgOverride >= 0 ? critDmgOverride : e.Stats.Cri_Dmg;
            // 탄성(전용무기): 치명타 공격 피격 시 받는 피해 % 감소. 받피감과 동일 채널로 합산.
            double tanseong = enemyCrit ? (ally.DisplayStats?.CritDmg_Taken_Reduction ?? 0) : 0;
            double effDmgRdc = allyDmgRdc + enemyDebuffs.Dmg_Reduction + tanseong;

            var input = new DamageCalculator.DamageInput
            {
                Character = null,                       // 적은 Character 모델 없음
                Skill = enemySkill,
                IsSkillEnhanced = false,
                TranscendLevel = 0,
                FinalAtk = effEnemyAtk,                 // 적 공격력 감소 디버프 반영
                FinalDef = enemy.FinalDef,
                CritDamage = critDmg,
                BossDef = ally.FinalDef,                // 의미상 target(아군) 방어
                DefReduction = allyDebuffs.Def_Reduction,  // 적이 ally에 부여한 방깎(불새 36 등) → ally 방어 감소
                BossDmgReduction = effDmgRdc,           // 아군 받피감 + 적 출력감소(피감) + 탄성 합산
                BossHp = ally.MaxHp,
                TargetHp = ally.MaxHp,
                TargetCurrentHp = ally.CurrentHp,
                DmgTakenIncrease = allyDebuffs.Dmg_Taken_Increase,
                Vulnerability = allyDebuffs.Vulnerability,
                IsCritical = enemyCrit,
                IsWeakpoint = false,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = false,                   // 아군은 보스 아님
                SelfMaxHp = enemy.MaxHp,
            };
            return _damageCalc.Calculate(input).FinalDamage;
        }

        /// <summary>
        /// 보스 반격(제이브 「복수의 갑옷」) 판정 — 아군이 이 적을 1회 공격(스킬/평타)할 때 호출.
        /// 게임 규칙대로 <b>피격 hit당</b>(AtkCount 횟수) Chance%로 롤 → 발동마다 ExecuteEnemyCounter.
        /// Counterattack 미보유 적(제이브 외 전부)은 즉시 반환 → RNG 무소비(비-금요일 회귀 안전).
        /// </summary>
        private void MaybeEnemyCounter(SiegeBattleState state, SiegeEnemyState hitEnemy, int hits)
        {
            var ca = hitEnemy.Source?.Counterattack;
            if (ca == null) return;
            // 오버라이드(빔=0) 우선. 0이면 RNG 무소비로 즉시 반환 → 빔 로테 평가가 반격에 무의존(결정론).
            double chance = _counterChanceOverride ?? ca.Chance;
            if (chance <= 0) return;
            for (int h = 0; h < Math.Max(1, hits); h++)
                if (_rng.Next(100) < chance)
                    ExecuteEnemyCounter(state, hitEnemy, ca);
        }

        /// <summary>
        /// 반격 1회 발동 — 무작위 아군 TargetCount명(도발 시 도발자 우선)에게 물리 Ratio%[강제치명·치피 CritDamage].
        /// 실명 중이면 빗나감(0뎀·시간 미소모; 실명 턴은 기본공격만 소모). 발동 시 ActionSeconds초 경과(전체 쿨 감소).
        /// 라이언 물리면역(도발+면역) / 보호막 / 생존판정은 ApplyDamageToAlly가 자동 처리.
        /// (용염=아군 화상 DoT는 아군 DoT 데미지 모델 후속 — 현재 직격만. 화상면역은 라이언 패시브로 이미 적용.)
        /// </summary>
        private void ExecuteEnemyCounter(SiegeBattleState state, SiegeEnemyState enemy, Models.SiegeCounterattack ca)
        {
            if (enemy.BlindTurnsRemaining > 0)
            {
                Log(state, enemy.Source.Name, false, ActionType.SkillAttack, "반격", 0,
                    $"{enemy.Source.Name} 반격 빗나감 (실명)");
                return;   // 실명 → 무효, 시간 미소모
            }
            var counterSkill = new Skill
            {
                Name = "반격", SkillType = SkillType.Skill1,
                LevelData = new Dictionary<int, SkillLevelData>
                { [0] = new SkillLevelData { Ratio = ca.Ratio, AtkCount = 1, TargetCount = 1 } },
            };
            var targets = PickRandomAllies(state, ca.TargetCount);   // 도발 우선 → 라이언 도발 시 라이언에게(물리면역=0뎀)
            foreach (var t in targets)
            {
                double dmg = CalcDamageToAlly(enemy, t, counterSkill, forceCrit: true, critDmgOverride: ca.CritDamage);
                ApplyDamageToAlly(state, enemy, t, dmg, "반격");
            }
            Log(state, enemy.Source.Name, false, ActionType.SkillAttack, "반격", 0,
                $"{enemy.Source.Name} 반격 발동 → 아군 {targets.Count}명 (물리 {ca.Ratio:0}%·치명 치피{ca.CritDamage:0}%)");
            AdvanceTime(state, ca.ActionSeconds);   // 반격 3초 → 전체(아군+적) 쿨 감소
        }

        /// <summary>적이 아군을 공격 → 피해 적용. (생존 메카닉: 부활·면역·권능은 후속 단계)</summary>
        private void ApplyDamageToAlly(SiegeBattleState state, SiegeEnemyState enemy, CharacterBattleState ally,
            double dmg, string label)
        {
            if (dmg <= 0 || ally.IsDead) return;

            // 피해 무효화 (피격 N회 / N턴) — 피해 자체를 0으로. 타입 한정(물리/마법) 면역은 적 공격 타입과 일치할 때만.
            var incomingType = enemy.Source.AttackType == AttackType.Magic ? DamageNullType.Magic : DamageNullType.Physical;
            if ((ally.NullifyHitsRemaining > 0 || ally.NullifyTurnsRemaining > 0)
                && (ally.NullifyType == DamageNullType.All || ally.NullifyType == incomingType))
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

            // 아군 보호막 흡수 (Shield_HpRatio 버프). 흡수분은 HP 피해가 아님. 전부 막으면 HP 피해 없음.
            if (ally.Shield > 0)
            {
                double absorbed = Math.Min(ally.Shield, dmg);
                ally.Shield -= absorbed;
                dmg -= absorbed;
                if (dmg <= 0)
                {
                    state.TurnLogs.Add(new BattleTurnLog
                    {
                        Turn = state.CurrentTurn, ActorName = enemy.Source.Name, IsAlly = false,
                        ActionType = ActionType.BuffApplied, SkillName = "보호막", DamageDealt = 0,
                        Description = $"{ally.Source.Character.Name} 보호막 흡수 {absorbed:N0} (잔여 {ally.Shield:N0})",
                    });
                    return;
                }
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

            // 아군 사망 → 「죽음의 경계」 보유 보스(델론즈)에 피해무효화[N회] 부여 (이후 아군 직격 N회 무효).
            if (ally.IsDead)
                foreach (var en in state.Enemies)
                    if (en.Source.OnAllyDeathNullifyHits > 0)
                    {
                        en.NullifyHitsRemaining = en.Source.OnAllyDeathNullifyHits;
                        Log(state, en.Source.Name, false, ActionType.BuffApplied, "죽음의 경계", 0,
                            $"{ally.Source.Character.Name} 사망 → {en.Source.Name} 피해무효화 [{en.NullifyHitsRemaining}회]");
                    }

            CheckHpThresholdNullify(state, ally);   // 생명력 임계 피해무효(나타 50% 등) 트리거
        }

        /// <summary>살아있는 아군 중 랜덤 1명 (없으면 null). 도발 중인 아군이 있으면 그 중에서 우선 선택.</summary>
        private CharacterBattleState PickRandomAlly(SiegeBattleState state)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            if (alive.Count == 0) return null;
            var taunters = alive.Where(a => a.TauntTurnsRemaining > 0).ToList();
            var pool = taunters.Count > 0 ? taunters : alive;
            return pool[_rng.Next(pool.Count)];
        }

        /// <summary>살아있는 아군 중 랜덤 N명 (중복 없이; 부족하면 가능한 만큼).
        /// 도발 중인 아군이 있으면 우선 타겟 — 단일(N=1)은 도발자, 광역은 도발자 포함 후 나머지 랜덤.</summary>
        private List<CharacterBattleState> PickRandomAllies(SiegeBattleState state, int count)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            var taunters = alive.Where(a => a.TauntTurnsRemaining > 0).ToList();
            if (taunters.Count > 0)
            {
                if (count <= taunters.Count) return taunters.OrderBy(_ => _rng.Next()).Take(count).ToList();
                var rest = alive.Where(a => a.TauntTurnsRemaining <= 0).OrderBy(_ => _rng.Next()).Take(count - taunters.Count);
                return taunters.Concat(rest).ToList();
            }
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

        /// <summary>
        /// 기본공격(턴 소모) 직후 호출 — 행동한 아군 한 명의 효과/잔여턴/재생/보호막을 한 번씩 tick.
        /// 게임 규칙(2-3): "n턴 지속" = 부여받은 캐릭이 <b>기본공격</b>을 n번 하는 동안 유효.
        /// 스킬턴(턴 미소모)에는 tick하지 않는다 → 셋업(따뜻한울림 3턴)이 보스의 기본공격 3회
        /// (그 사이 스킬턴 다수 포함) 동안 유효해 후속 버스트에 닿는다. 쿨다운(초)만 스킬턴에도 감소.
        /// </summary>
        private void TickAllyAfterAction(SiegeBattleState state, CharacterBattleState ally)
        {
            if (ally.IsDead) return;
            ally.Effects.TickTurn();
            foreach (var k in ally.StatusImmunityTurns.Keys.ToList())
                ally.StatusImmunityTurns[k] = Math.Max(0, ally.StatusImmunityTurns[k] - 1);
            // 턴 기반 생존(피해무효화[N턴]·불사[N턴]) 잔여 턴 감소
            if (ally.NullifyTurnsRemaining > 0) ally.NullifyTurnsRemaining--;
            if (ally.ImmortalTurnsRemaining > 0) ally.ImmortalTurnsRemaining--;
            if (ally.TauntTurnsRemaining > 0) ally.TauntTurnsRemaining--;   // 도발 잔여 턴 감소
            if (ally.LifestealTurns > 0) ally.LifestealTurns--;   // 흡혈 잔여 턴 감소

            // 지속 회복(재생) 틱 + 잔여 턴 감소 (예: 리나 행진가 매턴 시전자 최대HP 15%)
            if (ally.Regens.Count > 0)
            {
                foreach (var rg in ally.Regens)
                {
                    HealAlly(state, ally, rg.PerTurn, rg.SourceName, "재생");
                    rg.RemainingTurns--;
                }
                ally.Regens.RemoveAll(r => r.RemainingTurns <= 0);
            }
            // 보호막 잔여 턴 (만료 시 소멸)
            if (ally.ShieldTurns > 0 && --ally.ShieldTurns <= 0) ally.Shield = 0;
        }

        /// <summary>아군 회복 (MaxHp 상한). 점수와 무관 — 로그만 남긴다.</summary>
        private void HealAlly(SiegeBattleState state, CharacterBattleState target, double amount, string srcName, string label)
        {
            if (target == null || target.IsDead || amount <= 0) return;
            double before = target.CurrentHp;
            target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + amount);
            double healed = target.CurrentHp - before;
            if (healed <= 0) return;
            Log(state, srcName, true, ActionType.BuffApplied, label, 0,
                $"{target.Source.Character.Name} 회복 +{healed:N0} (HP {target.CurrentHp:N0}/{target.MaxHp:N0})");
        }

        /// <summary>
        /// 스킬/평타의 직접 회복(HealHpRatio = 시전자 최대HP 비례)을 대상 아군에 적용.
        /// 대상: LowestHpAlly 셀렉터 → 최저HP 아군(자신 제외) / TargetCount≥파티수 → 전체 / 그 외 → 자신.
        /// </summary>
        private void ApplySkillHeal(SiegeBattleState state, CharacterBattleState caster, Skill skill)
        {
            var lvl = skill.GetLevelData(caster.Source.IsSkillEnhanced);
            if (lvl == null) return;
            double ratio = lvl.HealHpRatio + (skill.GetTranscendBonus(caster.Source.TranscendLevel)?.HealHpRatio ?? 0);
            if (ratio <= 0) return;
            double amount = caster.MaxHp * ratio / 100.0;

            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            List<CharacterBattleState> targets;
            if (lvl.TargetSelector == TargetSelector.LowestHpAlly)
                targets = alive.Where(a => a != caster)
                    .OrderBy(a => a.MaxHp > 0 ? a.CurrentHp / a.MaxHp : 1).Take(1).ToList();
            else if (lvl.TargetCount >= state.AllyStates.Count)
                targets = alive;   // 전체 회복 (예: 리나 행진가 TargetCount=5)
            else
                targets = new List<CharacterBattleState> { caster };
            foreach (var t in targets) HealAlly(state, t, amount, caster.Source.Character.Name, skill.Name);
        }

        /// <summary>
        /// 아군 스킬 시전 시 발동하는 파티/자기 패시브(시전자 대상). 패시브 보유 아군이 살아있어야 발동.
        ///  - 나타 2초월: [모든 아군] 자신 스킬 발동 시 시전자 마공 55% 보호막[2턴] (Target=Party)
        ///  - 미호: [모든 아군] 자신 스킬 2회 발동 시 흡혈[2턴] (Target=Party)
        ///  - 라이언 강화: [자신] 스킬 1회 발동 시 모든 피해 무효화[피격 1회] (Target=Self)
        /// </summary>
        private void ApplyPartyOnSkillCastPassives(SiegeBattleState state, CharacterBattleState caster)
        {
            caster.SkillCastCount++;
            foreach (var owner in state.AllyStates.Where(a => !a.IsDead))
            {
                var passive = owner.Source.Character.Passive;
                if (passive == null) continue;
                foreach (var e in GetPassiveEffects(passive, owner))
                {
                    if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.SkillOnly) continue;
                    // Party: 패시브 보유자가 누구든 시전자에게 적용. Self: 패시브 보유자가 자기 스킬 시전 시만 자기에게.
                    if (e.Target != EffectTarget.Party && e.Target != EffectTarget.Self) continue;
                    if (e.Target == EffectTarget.Self && owner != caster) continue;

                    // 보호막 (시전자 공격력 비례) — 더 큰 값으로만 갱신
                    if (e.Type == PersistentEffectType.Buff && (e.Buff?.Shield_AtkRatio ?? 0) > 0)
                    {
                        double shield = caster.FinalAtk * e.Buff.Shield_AtkRatio / 100.0;
                        int dur = e.Duration > 0 ? e.Duration : 2;
                        if (shield > caster.Shield) { caster.Shield = shield; caster.ShieldTurns = Math.Max(caster.ShieldTurns, dur); }
                        Log(state, owner.Source.Character.Name, true, ActionType.BuffApplied, "보호막", 0,
                            $"{caster.Source.Character.Name} 보호막 {shield:N0} [{dur}턴]");
                    }

                    // 흡혈 (N회 발동마다 활성화)
                    if (e.Type == PersistentEffectType.Lifesteal && e.LifestealRatio > 0)
                    {
                        int need = Math.Max(1, e.TriggerCount);
                        if (caster.SkillCastCount % need == 0)
                        {
                            int dur = e.Duration > 0 ? e.Duration : 2;
                            caster.LifestealTurns = Math.Max(caster.LifestealTurns, dur);
                            caster.LifestealRatio = e.LifestealRatio;
                            Log(state, owner.Source.Character.Name, true, ActionType.BuffApplied, "흡혈", 0,
                                $"{caster.Source.Character.Name} 흡혈 {e.LifestealRatio:0}% [{dur}턴]");
                        }
                    }

                    // 피해 무효화 (예: 타카 강화 - 스킬 1회 발동 시 피격 N회 무효)
                    if (e.Type == PersistentEffectType.DamageNullification && e.DamageNullification != null)
                    {
                        var n = e.DamageNullification;
                        if (n.HitCount > 0)
                            caster.NullifyHitsRemaining = Math.Max(caster.NullifyHitsRemaining, n.HitCount);
                        if (n.Duration > 0)
                            caster.NullifyTurnsRemaining = Math.Max(caster.NullifyTurnsRemaining, n.Duration);
                        caster.NullifyType = n.Type;
                        Log(state, owner.Source.Character.Name, true, ActionType.BuffApplied, "피해 무효화", 0,
                            $"{caster.Source.Character.Name} 피해 무효 [피격 {n.HitCount}회{(n.Duration > 0 ? $"/{n.Duration}턴" : "")}]");
                    }
                }
            }
        }

        /// <summary>생명력 임계 트리거 피해무효 (예: 나타 생명력 50% 이하 → 모든 피해 면역 2턴, 전투당 1회).</summary>
        private void CheckHpThresholdNullify(SiegeBattleState state, CharacterBattleState ally)
        {
            if (ally.IsDead || ally.HpThresholdNullifyUsed || ally.MaxHp <= 0) return;
            double hpPct = ally.CurrentHp / ally.MaxHp * 100.0;
            var passive = ally.Source.Character.Passive;
            if (passive == null) return;
            foreach (var e in GetPassiveEffects(passive, ally))
            {
                if (e.Type != PersistentEffectType.DamageNullification || e.DamageNullification == null) continue;
                if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.OnHpBelow) continue;
                if (hpPct > e.TriggerHpThreshold) continue;

                ally.HpThresholdNullifyUsed = true;
                ally.NullifyType = e.DamageNullification.Type;
                if (e.DamageNullification.Duration > 0)
                    ally.NullifyTurnsRemaining = Math.Max(ally.NullifyTurnsRemaining, e.DamageNullification.Duration);
                if (e.DamageNullification.HitCount > 0)
                    ally.NullifyHitsRemaining = Math.Max(ally.NullifyHitsRemaining, e.DamageNullification.HitCount);
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "피해 면역", 0,
                    $"{ally.Source.Character.Name} 생명력 {hpPct:0}%↓ → 모든 피해 면역 [{e.DamageNullification.Duration}턴]");
                break;
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

        /// <summary>
        /// 행동 직후 호출 — 행동한 적 한 명의 Effects/보호막/DoT를 한 번씩 tick.
        /// per-character-action 모델: 적에 걸린 아군 디버프(따뜻한울림 방깎/살육의춤 마법취약)도
        /// "그 적의 N번 행동" 동안 유효 → 사이클이 짧은 셋업도 후속 nuke까지 닿음.
        /// </summary>
        private void TickEnemyAfterAction(SiegeBattleState state, SiegeEnemyState enemy)
        {
            enemy.Effects.TickTurn();
            // 보호막 지속턴 경과 (소진 전이라도 만료되면 소멸)
            if (enemy.ShieldTurns > 0 && --enemy.ShieldTurns <= 0) enemy.Shield = 0;

            // DoT 데미지 + 잔여 턴 감소 (적 진영 피해 면역 / 보스 무효화 중엔 무효).
            //   델론즈 무효화는 DoT도 막지만 횟수는 차감하지 않음(직격만 차감) → 여기선 무효 처리만.
            bool immune = state.EnemyImmunityTurns > 0 || enemy.NullifyHitsRemaining > 0;
            foreach (var dot in enemy.ActiveDots)
            {
                if (dot.TickDamage > 0 && !immune)
                {
                    // R1/R2: 오버킬 미집계, R3: 누적
                    double scoredTick = (state.CurrentRound < 3)
                        ? System.Math.Max(0, System.Math.Min(dot.TickDamage, enemy.CurrentHp))
                        : dot.TickDamage;
                    enemy.CurrentHp -= dot.TickDamage;
                    enemy.TotalDamageTaken += dot.TickDamage;
                    state.TotalScore += scoredTick;
                    state.RoundScore[state.CurrentRound] =
                        state.RoundScore.GetValueOrDefault(state.CurrentRound) + scoredTick;

                    var src = state.AllyStates.FirstOrDefault(a => a.Source.Character.Name == dot.SourceName);
                    if (src != null) src.TotalDamageDealt += scoredTick;

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
                // [실행가능성] 쿨감으로 즉시 0에 도달한 스킬의 준비완료시각 = 지금(이 행동 시점).
                if (_recordFeasibility)
                    foreach (var t in targets)
                        foreach (var k in t.SkillCooldowns.Keys.ToList())
                            if (t.SkillCooldowns[k] <= 0
                                && _readySince.TryGetValue((t.PartyIndex, k), out var rs) && double.IsPositiveInfinity(rs))
                                _readySince[(t.PartyIndex, k)] = state.ElapsedSeconds;
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
                {
                    double before = a.SkillCooldowns[k];
                    double after = Math.Max(0, before - seconds);
                    a.SkillCooldowns[k] = after;
                    // [실행가능성] 쿨이 이 구간에서 0에 도달 → 정확한 준비완료 경과초 기록(구간 내 보간).
                    if (_recordFeasibility && before > 0 && after <= 0
                        && _readySince.TryGetValue((a.PartyIndex, k), out var rs) && double.IsPositiveInfinity(rs))
                        _readySince[(a.PartyIndex, k)] = (state.ElapsedSeconds - seconds) + before;
                }
            foreach (var e in state.Enemies)
                foreach (var k in e.SkillCooldowns.Keys.ToList())
                    e.SkillCooldowns[k] = Math.Max(0, e.SkillCooldowns[k] - seconds);
        }

        #endregion

        #region 보조

        /// <summary>R1/R2 적이 모두 HP0 이하인지 (라운드 클리어 판정).</summary>
        private bool AllEnemiesDown(SiegeBattleState state)
            => state.Enemies.Count > 0 && state.Enemies.All(e => e.CurrentHp <= 0);

        private Skill PickAllySkill(CharacterBattleState ally, SiegeBattleState state)
        {
            // 쿨 충족 스킬 중 기본 쿨타임이 긴 핵심 스킬 우선 (버퍼의 버프 스킬이 쿨이 길어 우선 시전됨).
            // 동률은 SkillType 높은 순(궁→…→1). 평타 제외.
            // 단 어떤 아군이 60%HP 미만이면 힐 스킬(HealHpRatio>0) 우선 — 실측 리나가 행진가로 팀을 살림.
            bool enh = ally.Source.IsSkillEnhanced;
            int tr = ally.Source.TranscendLevel;
            var ready = ally.Source.Character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .Where(s => ally.IsSkillReady(s.SkillType))
                .ToList();
            if (ready == null || ready.Count == 0) return null;
            bool teamNeedsHeal = state.AllyStates.Any(a => !a.IsDead && a.MaxHp > 0 && a.CurrentHp < a.MaxHp * 0.6);
            if (teamNeedsHeal)
            {
                var heal = ready.FirstOrDefault(s => (s.GetLevelData(enh)?.HealHpRatio ?? 0) > 0);
                if (heal != null) return heal;
            }
            return ready
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
            // 툴팁 순서: 적 대상 효과(디버프·턴감소·버프해제)를 피해 前에 적용 → 같은 스킬 피해가 증폭/면역관통/보호막관통.
            ApplySkillEffects(state, ally, skill, targets, preDamage: true);
            foreach (var target in targets)
            {
                double dmg = CalcDamageToEnemy(ally, target, skill, state);
                ApplyDamage(state, ally, target, dmg, skill.Name, isSkill: true);
                RegisterDotToEnemy(state, ally, target, skill);   // 스킬의 DoT(화상·출혈 등) 등록
                MaybeEnemyCounter(state, target, skill.GetLevelData(ally.Source.IsSkillEnhanced)?.AtkCount ?? 1);
            }
            ProcessAttackStacks(state, ally, isSkill: true);   // 공격 발동형 스택(타카 EagleClaw) — 스킬 발동 시 1회
            ApplySkillEffects(state, ally, skill, targets, preDamage: false);   // 아군 버프·아군 디버프해제 (피해 後)
            ApplySkillHeal(state, ally, skill);   // 스킬 직접 회복(HealHpRatio, 예: 리나 행진가 전체회복 24%)
            ApplyPartyOnSkillCastPassives(state, ally);   // 스킬 시전 트리거(나타 보호막·미호 흡혈)
            state.AllyRotationCursor = (allyIdx + 1) % state.AllyStates.Count;   // 다음 자동 스킬턴은 다음 아군부터

            // 시전 소요시간 경과: 다른 쿨다운은 이 시간만큼 감소하되, 방금 시전한 스킬 자신의 쿨은
            //   "시전 완료 시점부터" 카운트(자기 컷신 시간엔 자기 쿨 안 깎임) → AdvanceTime을 쿨 set보다 먼저.
            //   (이전 버그: set(105) 후 AdvanceTime(5)이 자기 쿨까지 깎아 105→100 = 자기 시전시간만큼 조기회복.)
            AdvanceTime(state, GetActionDuration(skill));   // 스킬 소요시간만큼 전체 쿨다운 감소(자기 제외 효과)
            double cd = skill.GetCooldown(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel);
            if (cd > 0) ally.SkillCooldowns[skill.SkillType] = cd;
            // [실행가능성] 방금 시전 → 이 스킬은 쿨 중(준비 전). 다시 0 도달 시점까지 pending(∞) 마킹.
            if (_recordFeasibility && cd > 0) _readySince[(allyIdx, skill.SkillType)] = double.PositiveInfinity;
            // [쿨추적] 시전 시점 경과초 + 이 스킬 쿨 설정값 (게임 실측과 쿨회복 속도 대조용)
            Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "쿨", 0,
                $"{skill.Name} 시전 — 쿨 {cd:F0}초 설정 (경과 {state.ElapsedSeconds:F0}초)");

            // [실험] 스킬턴은 효과 tick하지 않음 — 함수 설계(815-817줄: "n턴 지속=기본공격 n회").
            //   스킬턴 tick 시 메인딜러 셋업 버프(따뜻한울림/청소)가 버스트 전 조기만료됨.
            // TickAllyAfterAction(state, ally);
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
            List<SiegeEnemyState> hitEnemies, bool preDamage)
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
                {
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
                    // 보호막(Shield_HpRatio) → 흡수 풀 생성 (대상 최대HP 비례). 더 큰 값으로만 갱신.
                    if (b.Shield_HpRatio > 0)
                    {
                        double shield = t.MaxHp * b.Shield_HpRatio / 100.0;
                        if (shield > t.Shield) { t.Shield = shield; t.ShieldTurns = Math.Max(t.ShieldTurns, d); }
                    }
                }
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

            // 적 피해면역(EnemyImmunityTurns)을 턴감소만큼 깎기 (미호 2스킬 등). 피해 전 적용 시 피해가 막히지 않음.
            void ReduceEnemyImmunity(int turns)
            {
                if (turns <= 0 || state.EnemyImmunityTurns <= 0) return;
                int before = state.EnemyImmunityTurns;
                state.EnemyImmunityTurns = System.Math.Max(0, state.EnemyImmunityTurns - turns);
                Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                    $"적 피해면역 {before}→{state.EnemyImmunityTurns}턴 (턴감소 {turns})");
            }

            // 적 보호막(루디 등) 버프해제로 제거 → 이후 피해가 점수로 집계.
            void DispelEnemyShield()
            {
                foreach (var en in hitEnemies.Where(x => x.Shield > 0))
                {
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"{en.Source.Name} 보호막 {en.Shield:N0} 버프해제로 제거");
                    en.Shield = 0; en.ShieldTurns = 0;
                }
            }

            // 효과는 툴팁 위→아래 순서대로 적용된다. 각 효과의 PreDamage 플래그가 이 단계(피해 前/後)와 일치할 때만 처리.
            // 예: 레이첼 불새 방깎/취약(PreDamage)→피해 증폭, 미호 턴감소(PreDamage)→면역관통, 오를리 유성 피해→버프해제(後).
            void HandleEffects(List<SkillEffect> effects)
            {
                if (effects == null) return;
                foreach (var e in effects)
                {
                    if (e.PreDamage != preDamage) continue;   // 이 단계에 맞는 효과만
                    switch (e.Type)
                    {
                        case SkillEffectType.Buff when e.Buff != null:
                        {
                            int tc = (e.TargetSelector == TargetSelector.HighestAtkAlly && buffTgtOverride.HasValue)
                                ? buffTgtOverride.Value : e.TargetCount;
                            ApplyBuff(e.Target, e.TargetSelector, tc, e.Buff, e.Duration);
                            break;
                        }
                        case SkillEffectType.Debuff when e.Debuff != null:
                            ApplyDebuff(e.Debuff, e.Duration, e.Target);
                            break;
                        case SkillEffectType.BuffTurnReduction
                            when e.TurnReduction > 0 && (e.Target == EffectTarget.Enemy || e.Target == EffectTarget.AllEnemies):
                            ReduceEnemyImmunity(e.TurnReduction);
                            break;
                        case SkillEffectType.BuffDispel when e.DispelBuffCount > 0:
                            DispelEnemyShield();
                            break;
                        case SkillEffectType.StatusAilment when e.StatusType == StatusEffectType.Regeneration:
                        {
                            // 지속 회복(재생): 매 턴 시전자 최대HP × CustomHpRatio% 회복. 대상 아군에 등록.
                            double rr = e.CustomHpRatio ?? 0;
                            if (rr <= 0) break;
                            double perTurn = ally.MaxHp * rr / 100.0;
                            int turns = e.Duration > 0 ? e.Duration : 1;
                            IEnumerable<CharacterBattleState> rt = e.Target == EffectTarget.Party
                                ? state.AllyStates.Where(x => !x.IsDead)
                                : (e.TargetSelector == TargetSelector.LowestHpAlly
                                    ? state.AllyStates.Where(x => !x.IsDead).OrderBy(x => x.MaxHp > 0 ? x.CurrentHp / x.MaxHp : 1).Take(1)
                                    : new[] { ally });
                            foreach (var t in rt.ToList())
                                t.Regens.Add(new SiegeAllyRegen { PerTurn = perTurn, RemainingTurns = turns, SourceName = actor });
                            Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                $"지속회복 +{perTurn:N0}/턴 [{turns}턴]");
                            break;
                        }
                        case SkillEffectType.DebuffCleanse when e.DispelDebuffCount > 0:
                            // 아군 후열 디버프 해제 (예: 미호 초월2). 보스의 공감 등 디버프를 제거 → 딜 회복.
                            foreach (var a in state.AllyStates.Where(x => !x.IsDead && x.Source.IsBackPosition))
                            {
                                int removed = a.Effects.RemoveDebuffs(e.DispelDebuffCount);
                                if (removed > 0)
                                    Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                        $"{a.Source.Character.Name} 디버프 {removed}개 해제");
                            }
                            break;
                        case SkillEffectType.DamageNullification when e.DamageNullification != null:
                        {
                            // 시전자 피해 무효(강자사냥 물리면역[2턴] 등) — 적 공격 타입과 일치하면 ApplyDamageToAlly에서 0뎀.
                            var n = e.DamageNullification;
                            if (n.Duration > 0) ally.NullifyTurnsRemaining = Math.Max(ally.NullifyTurnsRemaining, n.Duration);
                            if (n.HitCount > 0) ally.NullifyHitsRemaining = Math.Max(ally.NullifyHitsRemaining, n.HitCount);
                            ally.NullifyType = n.Type;
                            Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                $"{actor} {n.Type} 피해 면역 [{(n.Duration > 0 ? n.Duration + "턴" : n.HitCount + "회")}]");
                            break;
                        }
                        case SkillEffectType.StatusAilment when e.StatusType == StatusEffectType.Taunt:
                        {
                            // 자신 도발(강자사냥[2턴]) — 시전자가 적 공격을 유도. PickRandomAll*에서 우선 타겟됨.
                            int td = e.Duration > 0 ? e.Duration : 1;
                            ally.TauntTurnsRemaining = Math.Max(ally.TauntTurnsRemaining, td);
                            Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0, $"{actor} 도발 [{td}턴]");
                            break;
                        }
                        case SkillEffectType.StatusAilment when e.StatusType == StatusEffectType.Blind:
                        {
                            // 적 실명(라이언 강자사냥[2턴] 등) — 부여 적의 기본공격이 반드시 빗나감(스킬은 적중).
                            //   확률: 0 또는 ≥100 = 무롤 확정(라이언 강자사냥=명시 100%). 0<c<100일 때만 RNG 롤
                            //   (Ryan은 0이라 무롤 → RNG 시퀀스 무교란 = 비-실명 결과 회귀 안전).
                            int bd = e.Duration > 0 ? e.Duration : 1;
                            bool apply = e.Chance <= 0 || e.Chance >= 100 || _rng.Next(100) < e.Chance;
                            if (!apply) break;
                            var blindTargets = e.Target == EffectTarget.AllEnemies ? state.Enemies : hitEnemies;
                            foreach (var en in blindTargets)
                                en.BlindTurnsRemaining = Math.Max(en.BlindTurnsRemaining, bd);
                            if (blindTargets.Count > 0)
                                Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                                    $"적{blindTargets.Count} 실명 [{bd}턴] (기본공격 빗나감)");
                            break;
                        }
                    }
                }
            }

            // base + 초월 Effects를 필드별 오버라이드로 병합한 유효 리스트 1회 적용(최종값 컨벤션).
            //   예: 리나 울림 방깎 base34 + 초월41 → 41 (이전엔 base 후 초월을 같은 id로 덮어써 7만 남았음).
            HandleEffects(skill.GetEffectiveEffects(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel));
            // 레거시 필드는 PreDamage 플래그가 없으므로 피해 後(기본)로 처리.
            if (!preDamage)
            {
                if (lvl.DebuffEffect != null) ApplyDebuff(lvl.DebuffEffect, lvl.EffectDuration, EffectTarget.Enemy);
                if (lvl.SelfBuff != null) ApplyBuff(EffectTarget.Self, null, 1, lvl.SelfBuff, lvl.EffectDuration);
                if (lvl.PartyBuff != null) ApplyBuff(EffectTarget.Party, null, 0, lvl.PartyBuff, lvl.EffectDuration);
            }

            // 초월 Effects는 위 GetEffectiveEffects에 병합됨. 레거시 초월 필드(tr.Debuff/tr.PartyBuff)만 별도 처리.
            if (tr != null)
            {
                if (!preDamage && tr.Debuff != null) ApplyDebuff(tr.Debuff, 0, EffectTarget.Enemy);
                if (!preDamage && tr.PartyBuff != null) ApplyBuff(EffectTarget.Party, null, 0, tr.PartyBuff, 0);
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

        /// <summary>[실행가능성] 빌드 1스텝 결과 기록 (계획대로 시전/폴백/홀드).</summary>
        private void RecordFeasStep(SiegeBattleState state, int stepIdx, RotationDecision dec,
            CharacterBattleState ally, Skill sk, bool executed, string reason, double cdRem, double slack, bool gated = false)
        {
            _feasLog.Add(new BuildStepFeasibility
            {
                StepIndex = stepIdx,
                Turn = state.CurrentTurn,
                Elapsed = state.ElapsedSeconds,
                Hold = dec.Hold,
                HeroName = dec.Hold ? "(홀드)" : (ally?.Source.Character.Name ?? ResolveHeroName(state, dec.HeroIndex)),
                SkillName = dec.Hold ? "" : (sk?.Name ?? dec.Skill.ToString()),
                ExecutedAsPlanned = executed,
                FallbackReason = reason,
                CooldownRemaining = cdRem,
                Slack = slack,
                CooldownGated = gated,
            });
        }

        private static string ResolveHeroName(SiegeBattleState state, int idx)
            => idx >= 0 && idx < state.AllyStates.Count ? state.AllyStates[idx].Source.Character.Name : $"H{idx}";

        /// <summary>[실행가능성] 전투 중 도달 못 한 플랜 스텝(전투 조기종료/스킬턴 부족)을 미도달로 채우고 정렬.</summary>
        private void AppendUnreachedFeasSteps(List<RotationDecision> plan, SiegeBattleState state)
        {
            var seen = _feasLog.Select(f => f.StepIndex).ToHashSet();
            for (int i = 0; i < plan.Count; i++)
            {
                if (seen.Contains(i)) continue;
                var dec = plan[i];
                string skName = "";
                if (!dec.Hold)
                {
                    var hero = dec.HeroIndex >= 0 && dec.HeroIndex < state.AllyStates.Count
                        ? state.AllyStates[dec.HeroIndex] : null;
                    skName = hero?.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == dec.Skill)?.Name
                             ?? dec.Skill.ToString();
                }
                _feasLog.Add(new BuildStepFeasibility
                {
                    StepIndex = i, Reached = false, Hold = dec.Hold,
                    HeroName = dec.Hold ? "(홀드)" : ResolveHeroName(state, dec.HeroIndex),
                    SkillName = skName, ExecutedAsPlanned = false,
                    FallbackReason = "미도달(전투 종료/스킬턴 부족)",
                });
            }
            _feasLog.Sort((a, b) => a.StepIndex.CompareTo(b.StepIndex));
        }

        private SiegeBattleResult BuildResult(SiegeBattleState state)
        {
            var result = new SiegeBattleResult
            {
                TotalScore = state.TotalScore,
                RoundScore = state.RoundScore,
                TotalTurns = state.CurrentTurn,
                RoundsCleared = state.CurrentRound - 1,
                ElapsedSeconds = state.ElapsedSeconds,
                AlliesAlive = state.AllyStates.Count(a => !a.IsDead),
                TurnLogs = state.TurnLogs,
                DecisionPoints = state.DecisionPoints,
                Feasibility = _feasLog,
            };
            foreach (var ally in state.AllyStates)
            {
                result.CharacterResults.Add(new SiegeCharacterResult
                {
                    CharacterName = ally.Source.Character.Name,
                    PartyIndex = ally.PartyIndex,
                    TotalDamage = ally.TotalDamageDealt,
                    DamageShare = state.TotalScore > 0 ? ally.TotalDamageDealt / state.TotalScore * 100 : 0,
                    Died = ally.IsDead,
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

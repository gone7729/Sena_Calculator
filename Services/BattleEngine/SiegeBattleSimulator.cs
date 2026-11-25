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
        // [누수 계측] config.RecordLeak=true(시도 재생)일 때만 non-null — 점수/전투 불변. SiegeLeakLedger.cs 참고.
        private SiegeLeakRecorder _leak;
        private readonly Dictionary<(int Idx, SkillType Skill), double> _readySince = new();
        private readonly List<BuildStepFeasibility> _feasLog = new();
        // 직전 아군 스킬 1회 시전이 부여한 아군 버프 수령자 이름(스킬 순서 로그의 [대상] 표기용). ExecuteAllySkill마다 초기화.
        private readonly List<string> _curCastAllyBuffTargets = new();
        private Func<int, string, string, IReadOnlyList<string>, bool> _buffGate;   // [실험] SiegeBattleConfig.HighestAtkBuffGate
        private readonly Dictionary<int, double> _atkForBuffExtend = new();   // 아군별 공격 횟수 누적(턴제 버프 증가 패시브용)
        // 직전 시전이 적에 부여한 디버프 대상(예: 레이첼 불새 → [스파이크,룩,챈슬러]) / 버프해제한 적(예: 리프어택 → [스파이크]).
        private readonly List<string> _curCastDebuffTargets = new();
        private readonly List<string> _curCastDispelTargets = new();
        // 아군 사망 이벤트 (턴·경과초·사망 영웅·가해 적/스킬). 최종 빌드 재생 시 사망 로그로 출력.
        private readonly List<SiegeDeathEvent> _deaths = new();

        // [반격 오버라이드] config.CounterattackChanceOverride. null=보스 정의값(25%), 0=OFF.
        //   빔서치는 0(반격 RNG·시간경과 무의존)으로 로테 산출, 최종 점수·기어·생존반지는 null(ON)로 평가.
        //   금요일(제이브) 외 보스는 Counterattack=null이라 무관(월화수목토 무영향).
        private double? _counterChanceOverride;
        // [사망 페널티] config.AllyDeathPenalty. RankScore(랭킹용)=TotalScore − 페널티×사망수. 보고 점수는 불변.
        private double _allyDeathPenalty;
        // [풀버프 정렬] config.BuffFirstAuto. 자동 로테가 파티버프 셋업 스킬을 딜러 핵보다 먼저 시전.
        private bool _buffFirstAuto;
        // [광폭화] 광폭화 패시브 보유 적(요일 보스)만, 룩·챈슬러 친위대 제외(e.HasEnrage). 누적 턴(게임 70턴 시즈 카운터=state.CurrentTurn)에 따라 적이 주는 피해 증폭.
        //   30턴 +50% / 40턴 +100% / 50턴 +150% / 55턴 +200% / 60턴 +500% (SiegeBossSkillDb 원문).
        //   CalcDamageToAlly(적→아군)에만 곱연산. 아군→적(점수)엔 무관. _activeState로 현재 턴 참조.
        private SiegeBattleState _activeState;
        private int _lastEnrageTier = -1;   // 광폭화 단계 변화 로그용
        private bool IsDiagSkill(string name)
            => (DiagSkillName != null && name == DiagSkillName)
            || (DiagSkillNames != null && DiagSkillNames.Contains(name));

        public SiegeBattleResult Simulate(SiegeBattleConfig config) => Simulate(config, computeWeights: true);

        private SiegeBattleResult Simulate(SiegeBattleConfig config, bool computeWeights)
        {
            // 실행가능성 계측은 최종(메인) 패스에만 — 스카우팅 서브시뮬(computeWeights=false)은 결과 폐기되므로 제외.
            _recordFeasibility = config.RecordFeasibility && computeWeights;
            _leak = config.RecordLeak && computeWeights ? new SiegeLeakRecorder(_damageCalc) : null;
            _counterChanceOverride = config.CounterattackChanceOverride;   // 반격 확률 오버라이드(빔=0, 최종=null)
            _allyDeathPenalty = config.AllyDeathPenalty;   // 사망 페널티(RankScore용)
            _buffFirstAuto = config.BuffFirstAuto;         // 풀버프 정렬(셋업 버프 우선)
            _buffGate = config.HighestAtkBuffGate;         // [실험] 공격력 최고 아군 버프 시전 게이트
            var state = Initialize(config);
            _activeState = state;   // 광폭화 턴 참조용
            // 인게임 타게팅은 DamageWeight를 쓰지 않으므로 스카우팅 패스 생략(진단 덤프 시만 유지).
            if (computeWeights && (!config.InGameBuffTargeting || DiagAllyStats))
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

        private Random _critRng;   // [표본 모드] 치명·약점 추첨용(타게팅 난수 _rng와 분리 — 기댓값 판과 같은 타게팅을 유지)

        private SiegeBattleState Initialize(SiegeBattleConfig config)
        {
            _atkForBuffExtend.Clear();
            _critRng = config.CritSampleSeed.HasValue ? new Random(config.CritSampleSeed.Value) : null;
            var state = new SiegeBattleState
            {
                SiegeStage = config.SiegeStage,
                MaxTurns = config.MaxTurns,
                ForceFullBuffUptime = config.ForceFullBuffUptime,
                HighestAtkAllyOrder = config.HighestAtkAllyOrder,
                HighestAtkAllyOverrideCdrOnly = config.HighestAtkAllyOverrideCdrOnly,
                InGameBuffTargeting = config.InGameBuffTargeting,
                DiagAlliesImmortal = config.DiagAlliesImmortal,
                DiagImmortalAllies = config.DiagImmortalAllies,
                DiagForceCritWeak = config.DiagForceCritWeak,
                AllyCooldownMargin = config.AllyCooldownMargin,
                BasicAttackSeconds = config.BasicAttackSeconds,
                CoopAttackSeconds = config.CoopAttackSeconds,
                AllySkill1Seconds = config.AllySkill1Seconds, AllySkill2Seconds = config.AllySkill2Seconds,
                EnemySkill1Seconds = config.EnemySkill1Seconds, EnemySkill2Seconds = config.EnemySkill2Seconds,
                EnemySkillReadyAtTurnStart = config.EnemySkillReadyAtTurnStart,
                OwnCooldownFromCastStart = config.OwnCooldownFromCastStart,
                TimeLedger = config.DiagTimeLedger ? new List<(int, double, string, double)>() : null,
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
            ApplyInitialPassiveStacks(state);   // 전투 시작 추가 중첩 (쥬다스: 자신 제외 아군 만능형 수만큼 물공 중첩)
            // DamageWeight는 Simulate의 2-패스에서 설정 (스카우팅 누적딜 기반). 스카우팅 패스에선 0(raw-atk 타게팅).

            // 1라운드 적 생성
            state.InitializeRound(1);
            ApplyStandingEnemyDebuffs(state, config);   // 아군 패시브 방깎·펫 보스취약 등 상시 디버프
            ApplyRoundStartPassives(state);             // 라운드 진입 패시브 (동영 2초월 동일 열 마법형 피해 면역)
            ApplyEntryImmunities(state);                // 상태이상 면역 패시브: 전투 진입 시 [2턴] 부여 (이후 보유자 평타마다 갱신)

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
                _leak?.OnTurn(state.AllyStates.Where(a => a.IsDead).Select(a => a.Source.Character.Name));
                LogEnrageTransition(state);   // 광폭화 단계 진입 시 1회 로그
                if (config.DiagForceLethal != null)
                    foreach (var (fn, ft) in config.DiagForceLethal)
                    {
                        if (ft != t) continue;
                        var fa = state.AllyStates.FirstOrDefault(a => a.Source.Character.Name == fn && !a.IsDead);
                        if (fa == null) continue;
                        fa.CurrentHp = 0;
                        var fr = SurvivalResolver.ResolveLethal(fa);
                        if (fa.IsDead) fa.CurrentHp = 0;
                        Log(state, fn, true, ActionType.BuffApplied, "강제 치명(진단)", 0, $"{fn} 강제 치명 → {fr.Label} (HP {Math.Max(0, fa.CurrentHp):N0}/{fa.MaxHp:N0})");
                        if (fr.Survived && fr.Label == "부활") OnAllyRevived(state, fa);
                        if (fa.IsDead) _deaths.Add(new SiegeDeathEvent { Turn = t, Elapsed = state.ElapsedSeconds, AllyName = fn, Cause = "강제 치명(진단)" });
                    }

                // 적 스킬 사용 판정용 쿨 스냅샷(턴 시작 = 평타 前). 스킬턴 처리 후 해제.
                state.EnemyCdAtTurnStart = state.EnemySkillReadyAtTurnStart
                    ? state.Enemies.SelectMany(e => e.SkillCooldowns.Select(kv => (Key: (e.Source.Id, kv.Key), kv.Value)))
                        .GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First().Value)
                    : null;

                // 기본공격: 양팀 통합 속공순 다음 유닛 (턴 소모)
                if (order.Count > 0)
                {
                    var actor = order[cursor % order.Count];
                    cursor++;
                    ExecuteBasicAttack(state, actor);
                }

                // 라운드 내 스킬턴: 2턴마다, 라운드 시작 선공 스킬 이후 후공→선공 교대 (턴 미소모)
                //   ★스킬턴은 "뒤에 턴이 남아야" 나오는 특수턴(유저 실측) → 마지막 턴(t==MaxTurns)엔 평타만 진행되고
                //     끝나므로 스킬턴 미발동. (이전: T70에 룩 1스킬이 발동해 비스킷 사망 = 과도한 막판 적 스킬턴.)
                if (roundTurn % 2 == 0 && t < state.MaxTurns)
                {
                    bool skillByAlly = (inRoundSkill % 2 == 0) ? !state.AllyFirst : state.AllyFirst;
                    inRoundSkill++;
                    state.IsSkillTurn = true;
                    ProcessSkillTurn(config, state, skillByAlly);
                    state.IsSkillTurn = false;
                }
                state.EnemyCdAtTurnStart = null;   // 라운드 진입 스킬턴(EnterRound)은 현재 쿨로 판정

                // 효과 잔여턴/DoT는 각 캐릭터의 행동 직후에 처리(per-character-action 모델).
                //   글로벌 sim-turn tick이 아니라, 행동자(actor)만 tick — n턴 지속 = "부여받은 캐릭의 n번 행동".
                //   이렇게 해야 따뜻한울림 3턴(보스 3번 행동≈24 sim-turn) 같은 셋업이 후속 nuke까지 닿는다.
                //   적 피해 면역(ImmunityTurns)도 이 모델을 따른다 → TickEnemyAfterAction에서 적별 차감.
                //   (구버그: 글로벌 sim-turn마다 감소해 「피해면역[2턴]」이 ~2 sim-turn 만에 만료 → 거의 무효 → 시뮬 과대산출.)

                // 라운드 전환: 적 전멸 시 다음 라운드 진입 → 선공 스킬턴(0턴, 연쇄)
                //   이 시점의 전멸은 위 라운드 내 스킬턴(또는 평타)이 깬 것 → 먼저 다음 라운드로 전환한 뒤
                //   새 라운드의 신선한 적에게 진입 스킬을 시전한다(EnterRound). 죽은 라운드에 스킬을
                //   공짜로 한 번 더 시전하던 버그 정정 — 데미지 없는 버프/디버프가 라운드를 못 넘기게.
                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    TransitionToNextRound(config, state);
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

                // 이 진입 스킬이 (신선한) 적을 전멸시킨 경우에만 다음 라운드로 연쇄.
                //   데미지 없는 버프/디버프 진입 스킬은 적을 못 죽이므로 여기서 끊기고 평타로 진행한다.
                if (state.CurrentRound < 3 && AllEnemiesDown(state))
                {
                    TransitionToNextRound(config, state);
                    continue;   // 다음 라운드의 신선한 적에게 0턴 선공 스킬턴
                }
                break;
            }
        }

        /// <summary>현재 라운드(전멸 상태)를 닫고 다음 라운드의 신선한 적을 세팅. 사망모션 1초 경과(쿨 −1초).</summary>
        private void TransitionToNextRound(SiegeBattleConfig config, SiegeBattleState state)
        {
            // 적 사망 모션: 라운드 클리어 시 1초 경과(쿨 −1초), 그 후 전환 동안 쿨 동결(추가 시간 없음). (실측 2026-06-08)
            AdvanceTime(state, 1.0, "라운드 전환(사망모션)");
            Log(state, "시스템", true, ActionType.BuffApplied, "라운드 전환", 0,
                $"R{state.CurrentRound} 클리어 → R{state.CurrentRound + 1} 진입 (사망모션 1초 경과·선공 스킬턴)");
            state.CurrentRound++;
            state.InitializeRound(state.CurrentRound);
            ApplyStandingEnemyDebuffs(state, config);
            ApplyRoundStartPassives(state);
        }

        /// <summary>라운드 진입 시 발동 패시브: 패시브 상시 보호막(전투 시작/라운드당) + TriggerCondition.OnRoundStart 피해 면역.
        ///   동영 2초월: 자신과 동일 열 아군(마법형) 모든 피해 면역[1턴] — 매 라운드 진입마다. 보유자 생존 시에만.</summary>
        private void ApplyRoundStartPassives(SiegeBattleState state)
        {
            foreach (var owner in state.AllyStates.Where(a => !a.IsDead).ToList())
            {
                var passive = owner.Source.Character.Passive;
                if (passive == null) continue;
                foreach (var e in GetPassiveEffects(passive, owner))
                {
                    // 패시브 상시 보호막 (트리거 없는 Self 보호막 버프) — 전투 시작 1회(R1 진입) 또는 매 라운드(RefreshEachRound).
                    //   예) 크리스·니아·이브·젠이츠·윤건·쥬다스 = 전투 시작, 리나·엘리시아·녹스 각성·메이 2초월 = 라운드당 1회.
                    //   IsConditional(노호 "스킬 발동 시" 등)은 제외. 지속 턴 미입력은 위키 표기 3턴.
                    if (e.Type == PersistentEffectType.Buff && e.Buff != null && e.ApplyMode != ApplyMode.Triggered
                        && e.Target == EffectTarget.Self && (e.RefreshEachRound || (!e.IsConditional && state.CurrentRound == 1))
                        && (e.Buff.Shield_AtkRatio > 0 || e.Buff.Shield_HpRatio > 0))
                    {
                        double shield = owner.FinalAtk * e.Buff.Shield_AtkRatio / 100.0 + owner.MaxHp * e.Buff.Shield_HpRatio / 100.0;
                        int sd = e.Duration > 0 ? e.Duration : 3;
                        if (shield > owner.Shield) { owner.Shield = shield; owner.ShieldTurns = Math.Max(owner.ShieldTurns, sd); }
                        Log(state, owner.Source.Character.Name, true, ActionType.BuffApplied, "보호막", 0,
                            $"R{state.CurrentRound} {(e.RefreshEachRound ? "라운드 진입" : "전투 시작")}: {owner.Source.Character.Name} 보호막 {shield:N0} [{sd}턴]");
                        continue;
                    }
                    if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.OnRoundStart) continue;
                    if (e.Type != PersistentEffectType.DamageNullification || e.DamageNullification == null) continue;
                    IEnumerable<CharacterBattleState> targets = e.Target == EffectTarget.Self
                        ? new[] { owner }
                        : state.AllyStates.Where(a => !a.IsDead);
                    if (e.SameRowOnly) targets = targets.Where(a => a.Source.IsBackPosition == owner.Source.IsBackPosition);
                    if (e.TargetClasses != null && e.TargetClasses.Length > 0)
                        targets = targets.Where(a => e.TargetClasses.Contains(a.Source.Character.Type));
                    var n = e.DamageNullification;
                    var list = targets.ToList();
                    foreach (var t in list)
                    {
                        if (n.Duration > 0) t.NullifyTurnsRemaining = Math.Max(t.NullifyTurnsRemaining, n.Duration);
                        if (n.HitCount > 0) t.NullifyHitsRemaining = Math.Max(t.NullifyHitsRemaining, n.HitCount);
                        t.NullifyType = n.Type;
                    }
                    if (list.Count > 0)
                        Log(state, owner.Source.Character.Name, true, ActionType.BuffApplied, passive.Name, 0,
                            $"R{state.CurrentRound} 진입: {string.Join(",", list.Select(t => t.Source.Character.Name))} {n.Type} 피해 면역 [{(n.Duration > 0 ? n.Duration + "턴" : n.HitCount + "회")}]");
                }
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
            if (_leak != null) _leak.CurStep = byAlly ? state.AllySkillTurnIndex : -1;
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
                        bool ready = sk != null && a.IsSkillReady(dec.Skill) && PassesBuffGate(state, a, sk);
                        // 전원 풀피 시 회복 스킬은 발동 불가 → 플랜 무시하고 자동(다음 스킬)으로 폴백(예약 다음 순번 발동).
                        //   (빔은 풀피 회복을 후보에서 제외하므로 보통 비발동; 시드/HP변동 대비 방어 게이트.)
                        bool healGated = sk != null && IsHealSkill(sk, a.Source.IsSkillEnhanced) && AllAlliesFullHp(state);
                        if (!healGated && !a.IsDead && !a.Effects.HasActionBlockingCC() && ready)
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
                            // 방금 시전이 부여/적용한 대상을 이 스텝에 기록(스킬 순서 [대상] 표기).
                            if (_recordFeasibility && _feasLog.Count > 0)
                            {
                                if (_curCastAllyBuffTargets.Count > 0)
                                    _feasLog[^1].BuffTargets = _curCastAllyBuffTargets.Distinct().ToList();
                                if (_curCastDebuffTargets.Count > 0)
                                    _feasLog[^1].DebuffTargets = _curCastDebuffTargets.Distinct().ToList();
                                if (_curCastDispelTargets.Count > 0)
                                    _feasLog[^1].DispelTargets = _curCastDispelTargets.Distinct().ToList();
                            }
                            return;
                        }
                        // healGated면 폴백 사유 미기록 — 자동 섹션이 다음 스킬을 시전.
                        if (!healGated && _recordFeasibility)
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

                // [풀버프 정렬] 셋업(파티버프) 스킬이 준비됐으면 딜러 핵보다 먼저 시전 — "버프 깔고 버스트".
                //   쿨 긴 핵심 버프 우선. 파티버프 스킬은 쿨이 길어 매 턴 나오지 않으니 자연히 간격이 벌어짐.
                if (_buffFirstAuto)
                {
                    int bn = state.AllyStates.Count;
                    (int idx, Skill sk, double cd) bestSetup = (-1, null, -1);
                    for (int k = 0; k < bn; k++)
                    {
                        int idx = (state.AllyRotationCursor + k) % bn;
                        var a = state.AllyStates[idx];
                        if (a.IsDead || a.Effects.HasActionBlockingCC()) continue;
                        foreach (var s in a.Source.Character.Skills ?? Enumerable.Empty<Skill>())
                        {
                            if (s.SkillType == SkillType.Normal || s.SkillType == SkillType.Normal2 || (s.IsAwakenOnly && !a.Source.IsAwakened)) continue;
                            if (!a.IsSkillReady(s.SkillType) || !IsPartyBuffSetup(s, a.Source.IsSkillEnhanced)) continue;
                            if (!PassesBuffGate(state, a, s)) continue;
                            double cd = s.GetCooldown(a.Source.IsSkillEnhanced, a.Source.TranscendLevel, a.Source.IsAwakened);
                            if (cd > bestSetup.cd) bestSetup = (idx, s, cd);
                        }
                    }
                    if (bestSetup.sk != null) { ExecuteAllySkill(state, bestSetup.idx, bestSetup.sk); RecordAutoFeasStep(state, stIdx, bestSetup.idx, bestSetup.sk); return; }
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
                    RecordAutoFeasStep(state, stIdx, idx, skill);
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
                    // 턴 시작 판정: 평타 전엔 쿨이 남아 있었으면 이번 스킬턴엔 안 씀 (EnemySkillReadyAtTurnStart)
                    if (state.EnemyCdAtTurnStart != null
                        && state.EnemyCdAtTurnStart.TryGetValue((enemy.Source.Id, pri.SkillType), out var cd0) && cd0 > 0) continue;
                    var skill = enemy.Source.Skills?.FirstOrDefault(s => s.SkillType == pri.SkillType);
                    if (skill == null) continue;

                    // 광역은 타겟수만큼, 단일은 1명 — 모두 랜덤 아군
                    int targetCount = Math.Max(1, skill.GetTargetCount(false, 0));
                    var recast = skill.GetLevelData(false)?.OnKillRecast;
                    var mainTargets = PickRandomAllies(state, targetCount);
                    var primaryTarget = mainTargets.FirstOrDefault();   // 도발 우선 타겟(라이언 등) — 동일 열 추가타 기준
                    foreach (var target in mainTargets)
                    {
                        bool wasAlive = !target.IsDead;
                        double dmg = CalcDamageToAlly(enemy, target, skill);
                        ApplyDamageToAlly(state, enemy, target, dmg, skill.Name);
                        ApplyEnemyStatusToAlly(state, target, skill, enemy);

                        // OnKillRecast: 직접 피해로 아군 처치 시 RatioPercent% 위력으로 1회 재시전(연쇄 1회 한도).
                        if (recast != null && recast.RatioPercent > 0 && wasAlive && target.IsDead)
                        {
                            var rt = PickRandomAlly(state);
                            if (rt != null)
                            {
                                double rdmg = CalcDamageToAlly(enemy, rt, skill) * recast.RatioPercent / 100.0;
                                ApplyDamageToAlly(state, enemy, rt, rdmg, skill.Name + "(처치 재시전)");
                                ApplyEnemyStatusToAlly(state, rt, skill, enemy);
                            }
                        }
                    }

                    // 추가타(예: 스파이크 혹한의 일격 "동일 열 75% 1회"). 빙결 무관 직접딜 → 면역으로 못 막음.
                    //   챈슬러 버프 시 추가타도 치명타(CalcDamageToAlly가 enemy.EnemyBuff 반영). 빙결 상태는 메인 skill로 부여(풍연 면역 차단).
                    var lvld = skill.GetLevelData(false);
                    if (lvld != null && lvld.EnemyExtraHitRatio > 0 && lvld.EnemyExtraHitTargets > 0 && primaryTarget != null)
                    {
                        var extraSkill = new Skill
                        {
                            Name = skill.Name + "(추가타)", SkillType = skill.SkillType,
                            LevelData = new Dictionary<int, SkillLevelData>
                            { [0] = new SkillLevelData { Ratio = lvld.EnemyExtraHitRatio, AtkCount = 1, TargetCount = 1 } },
                        };
                        // ★동일 열: 주 대상(도발타겟)과 같은 열(IsBackPosition) 아군 전원에게 추가타.
                        //   밸런스(딜러 2명 같은 후열)면 비탱 딜러(타카)가 휩쓸려 사망, 보호(딜러 다른 열)면 회피.
                        //   (이전: PickRandomAllies=랜덤 근사라 같은 열 사망을 못 잡아 밸런스를 과대평가했음.)
                        foreach (var t in state.AllyStates.Where(a => !a.IsDead
                            && a.Source.IsBackPosition == primaryTarget.Source.IsBackPosition).ToList())
                        {
                            double dmg = CalcDamageToAlly(enemy, t, extraSkill);
                            ApplyDamageToAlly(state, enemy, t, dmg, extraSkill.Name);
                            ApplyEnemyStatusToAlly(state, t, skill, enemy);   // 동일 열 빙결[3턴] (풍연 면역이 차단)
                        }
                    }

                    // 적 진영 전체 피해 면역 부여 (화 R3 룩: 「모든 아군(보스측) 피해 면역[2턴]」) — 적 전체에 부여.
                    //   적별 ImmunityTurns로 저장 → 각 적이 행동할 때마다 1턴 차감(EnemyBuffTurns와 동일 모델).
                    int imm = skill.GetLevelData(false)?.GrantEnemyImmunityTurns ?? 0;
                    if (imm > 0)
                    {
                        foreach (var en in state.Enemies)
                            en.ImmunityTurns = Math.Max(en.ImmunityTurns, imm);
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"적 진영 전체 피해 면역[{imm}턴] (룩·챈슬러·보스 모두 — 버프해제 불가, 턴제버프감소로만 해제)");
                    }

                    // 적→아군(같은 진영) 버프: 공격력 최고 적에게 부여 (토요일 챈슬러 분쇄 → 스파이크 치확100·치피+500[5턴]).
                    //   미해제 시 그 적이 치명타 학살기 → 아군 전멸. 비스킷 리프어택 버프해제로 제거해야 함(필수 기믹).
                    var allyBuff = skill.GetLevelData(false)?.GrantHighestAtkAllyBuff;
                    if (allyBuff != null)
                    {
                        // 공격력 최고 적 = 스파이크(3181) > 챈슬러(1754) > 룩(1502). 시즈 적은 HP 음수여도 행동(스펀지)이라 HP 필터 X.
                        var tgt = state.Enemies.OrderByDescending(e => e.FinalAtk).FirstOrDefault() ?? enemy;
                        tgt.EnemyBuff = allyBuff.Clone();
                        tgt.EnemyBuffTurns = Math.Max(tgt.EnemyBuffTurns, skill.GetLevelData(false)?.GrantHighestAtkAllyBuffTurns ?? 0);
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"{tgt.Source.Name}에게 버프 [{tgt.EnemyBuffTurns}턴]: {SummarizeBuff(tgt.EnemyBuff)} (버프해제 시 제거)");
                    }

                    // 보스 자기 보호막 생성 (예: 루디 방어 준비 = 방어력 100배). 아군 피해를 흡수(점수 미집계)·버프해제로 제거.
                    var sld = skill.GetLevelData(false);
                    if (sld != null && sld.SelfShieldDefRatio > 0)
                    {
                        enemy.Shield = enemy.Source.Stats.Def * sld.SelfShieldDefRatio / 100.0;
                        enemy.ShieldTurns = sld.SelfShieldTurns > 0 ? sld.SelfShieldTurns : 99;
                        Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                            $"보호막 {enemy.Shield:N0} 생성 [{enemy.ShieldTurns}턴] (버프해제/소진 시 제거)");
                        // 링크: 보스측 모든 적에게 [N턴] + 받는 피해 감소 (SelfShieldDmgReduction = 링크 감소율)
                        if (sld.SelfShieldDmgReduction > 0)
                        {
                            foreach (var le in state.Enemies)
                            {
                                le.LinkTurns = enemy.ShieldTurns;
                                le.LinkDmgRdc = sld.SelfShieldDmgReduction;
                            }
                            Log(state, enemy.Source.Name, false, ActionType.BuffApplied, skill.Name, 0,
                                $"링크 [{enemy.ShieldTurns}턴]: {string.Join(",", state.Enemies.Select(x => x.Source.Name))} 받피감 {sld.SelfShieldDmgReduction:0}% (버프해제 시 제거)");
                        }
                    }
                    // 상대(보스) 스킬 사용 → 그 스킬 소요시간(1스킬 3초·2스킬 4초)만큼 시간 경과 → 전체(아군+적) 쿨 감소.
                    //   [정정] 이전엔 별도 ReduceCooldowns(5)까지 더해 이중 감소 → 교만 등 쿨이 실시간보다 빨리 회복됐음.
                    //   시간경과(AdvanceTime)만으로 충분(쿨=실시간). 별도 -5초 메커니즘 제거.
                    double enemyCd = GetActionDuration(skill, enemy: true);
                    Log(state, enemy.Source.Name, false, ActionType.BuffApplied, "쿨감", 0,
                        $"적 {skill.Name}({skill.SkillType}) 시전 → 시간경과 {enemyCd:0.##}초 → 전체 쿨 -{enemyCd:0.##}초 (경과 {state.ElapsedSeconds:F0}초)");
                    AdvanceTime(state, enemyCd, $"[적스킬] {enemy.Source.Name} {skill.Name}");
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
            if (_leak != null) _leak.CurStep = -1;
            if (actor.IsAlly)
            {
                var ally = actor.Ally;
                // 사망·CC(빙결·기절 등 행동불가) 상태면 기본공격 스킵 (차례는 지나감 = 딜 기회 상실)
                if (!ally.IsDead && !ally.Effects.HasActionBlockingCC())
                {
                    var normal = ally.Source.Character.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                    if (normal != null)
                    {
                        int tc = System.Math.Max(1, normal.GetTargetCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened));
                        var targets = PickTargets(state, tc, ally);
                        foreach (var target in targets)
                        {
                            bool pen = normal.PiercesImmunity(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened);
                            if (tc <= 1)
                                ApplySingleTargetHit(state, ally, target, normal, isSkill: false, pen);   // 약점→최저HP / 미발동→무작위(기댓값)
                            else
                                ApplyDamage(state, ally, target, CalcDamageToEnemy(ally, target, normal, state), normal.Name, isSkill: false, penetrate: pen, hitCount: HitCountOf(ally, normal));
                            RegisterDotToEnemy(state, ally, target, normal);   // 평타의 DoT(화상 등) 등록
                            RegisterRingStatusToEnemy(ally, target);           // 상태이상 반지(가시·샐러맨더·독사) — 기본 공격 시 확률 부여
                            MaybeEnemyCounter(state, target, normal.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened)?.AtkCount ?? 1);
                        }
                        if (targets.Count > 0)
                        {
                            ProcessAttackStacks(state, ally, isSkill: false);   // 공격 발동형 스택 — 평타 2회마다 1회
                            ProcessAttackBuffs(state, ally, isSkill: false);    // 공격 발동형 아군 버프(지크 물공증) — 평타 2회마다
                            ProcessTriggeredFixedDamage(state, ally, isSkill: false);   // N회 공격마다 추가 피해(세인·여포·발리스타 등)
                            CountAttackForBuffExtend(state, ally);   // 공격 N회마다 자신의 턴제 버프 +1턴(쥬리)
                            TriggerAllyImmunity(state, ally);   // 기본공격 발동 → 면역 패시브 트리거(턴제 면역 갱신)
                            ApplyBasicAttackCdReduction(state, ally, normal);   // 평타 쿨감(예: 라이언 자신+최고공격력 아군 9초)
                            ApplySkillHeal(state, ally, normal);   // 평타 회복(예: 리나 강화평타 최저HP 아군 7%)
                            ProcessCoopAttacks(state, ally, normal, targets[0], tc);   // 다른 아군의 협공(발리스타·쥬리)
                        }
                    }
                }
            }
            else
            {
                // 적 기본공격 → 랜덤 아군 1명 피격 + 상태이상 부여
                var enemy = actor.Enemy;
                // 실명(라이언 강자사냥 등): 적 기본공격·반격은 반드시 "빗나감". 스킬은 영향 없음.
                //   빗나감 = 타격 판정이 사라지는 게 아니다(유저 확인 2026-11-23): 피해 20% 감소 · 치명타 미적용 · 디버프(상태이상) 무시.
                //   타격 자체는 들어가므로 피격 횟수형 피해 무효화(타카 등)를 소모하고 피해도 입힌다.
                //   (공격자의 약점 공격이 발동하면 빗나감을 무시하지만 공성 적은 약점 공격이 없어 해당 없음.)
                //   이전엔 "0뎀·무타격"으로 계산해 실명 중 평타가 무효화를 깎지 않았다 — 토요일 T62 타카 생존(실제는 사망)의 원인.
                //   기본공격 1회로 실명 1턴 소모 (per-action 모델). HP와 무관하게 차감.
                {
                    bool missed = enemy.BlindTurnsRemaining > 0;
                    if (missed) enemy.BlindTurnsRemaining--;
                    var normal = enemy.Source.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
                    var target = PickRandomAlly(state);
                    if (normal != null && target != null)
                    {
                        double dmg = CalcDamageToAlly(enemy, target, normal, noCrit: missed) * (missed ? MissDamageMultiplier : 1.0);
                        if (missed)
                            Log(state, enemy.Source.Name, false, ActionType.NormalAttack, "기본공격", 0,
                                $"{enemy.Source.Name} 기본공격 빗나감 → {target.Source.Character.Name} (실명: 피해 -20%·치명 없음·상태이상 없음, 잔여 {enemy.BlindTurnsRemaining}턴)");
                        ApplyDamageToAlly(state, enemy, target, dmg, missed ? normal.Name + "(빗나감)" : normal.Name);
                        if (!missed) ApplyEnemyStatusToAlly(state, target, normal, enemy);
                    }
                }
            }

            AdvanceTime(state, GetActionDuration(null),
                $"평타 {(actor.IsAlly ? actor.Ally.Source.Character.Name : actor.Enemy.Source.Name)}"
                + ((actor.IsAlly && (actor.Ally.IsDead || actor.Ally.Effects.HasActionBlockingCC())) ? "(행동불가·스킵)" : ""));

            // 행동자(actor)만 효과 tick (per-character-action 모델)
            //   ★R3 적은 스펀지(HP≤0여도 행동 지속)다. 옛 `CurrentHp > 0` 가드는 음수HP가 된 R3 적의
            //     디버프 잔여턴을 영영 안 깎아(불새 [4턴]이 70턴 내내 유지) 디버프 갱신을 무의미하게 만들고
            //     시뮬을 과대산출시켰다. 행동한 적은 HP 무관하게 tick한다(R1/R2 사망 적은 행동 자체를 안 함).
            if (actor.IsAlly && actor.Ally != null)
                TickAllyAfterAction(state, actor.Ally);
            else if (!actor.IsAlly && actor.Enemy != null)
                TickEnemyAfterAction(state, actor.Enemy);
        }

        /// <summary>적에게 걸린 디버프 종류 수 — 스탯 디버프(필드별 1개) + 상태이상 종류.
        ///   공성 DoT(화상·출혈·중독)는 ActiveDots에 따로 있어 종류별로 더한다(기댓값 DoT는 걸려 있을 확률 50%↑만).
        ///   실명도 1개로 센다.</summary>
        private static int CountEnemyDebuffs(SiegeEnemyState target)
        {
            int n = target.Effects.GetActiveDebuffCount();
            foreach (var g in target.ActiveDots.Where(d => d.RemainingTurns > 0).GroupBy(d => d.Type))
            {
                double none = 1.0;
                foreach (var d in g) none *= 1 - Math.Clamp(d.Probability, 0, 1);
                if (1 - none >= 0.5) n++;
            }
            if (target.BlindTurnsRemaining > 0) n++;
            return n;
        }

        /// <summary>
        /// N회 공격마다 추가 피해(TriggeredFixedDamage) — 세인(모든 공격 3회마다 물공 100%)·여포·발리스타·윤건·카일·플라튼·겔리두스.
        ///   단일보스 시뮬(BattleSimulator.ProcessTriggeredFixedDamage)과 같은 규칙: 평타 1회·스킬 1회를 각각 공격 1회로 센다(타수 무관).
        ///   합성 스킬로 일반 피해 경로(방어·치명/약점 기댓값·피증·공성 감쇄)를 탄다. 카운터는 라운드를 넘어 유지.
        ///   발동 공격의 버프 해제(플라튼 30%)는 미구현. 협공·패시브 발동 시전은 공격 횟수에 넣지 않는다.
        /// </summary>
        private void ProcessTriggeredFixedDamage(SiegeBattleState state, CharacterBattleState ally, bool isSkill)
        {
            var passive = ally.Source.Character.Passive;
            if (passive == null || ally.IsDead) return;
            // 같은 트리거가 base/초월 양쪽에 있으면 뒤(높은 초월) 값이 이김
            var byTrigger = new Dictionary<TriggerCondition, TriggeredFixedDamage>();
            foreach (var e in GetPassiveEffects(passive, ally))
                if (e.Type == PersistentEffectType.TriggeredFixedDamage && e.TriggeredFixedDamage != null)
                    byTrigger[e.TriggeredFixedDamage.TriggerOn] = e.TriggeredFixedDamage;
            foreach (var tfd in byTrigger.Values)
            {
                bool matches = tfd.TriggerOn switch
                {
                    TriggerCondition.AllAttack => true,
                    TriggerCondition.SkillOnly => isSkill,
                    TriggerCondition.NormalOnly => !isSkill,
                    _ => false,
                };
                if (!matches) continue;
                string key = $"siege_tfd:{ally.PartyIndex}:{tfd.TriggerOn}";
                ally.StackTriggerCounters.TryGetValue(key, out int cnt);
                cnt++;
                if (cnt < Math.Max(1, tfd.TriggerCount)) { ally.StackTriggerCounters[key] = cnt; continue; }
                ally.StackTriggerCounters[key] = 0;

                int tc = Math.Max(1, tfd.TargetCount);
                var sk = new Skill
                {
                    Name = (passive.Name ?? "패시브") + "(추가공격)",
                    SkillType = SkillType.Skill1,
                    LevelData = new Dictionary<int, SkillLevelData>
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = tfd.AtkRatio, DefRatio = tfd.DefRatio, FixedDamage = tfd.FixedDamage,
                            TargetCount = tc, AtkCount = Math.Max(1, tfd.HitCount),
                        }
                    },
                };
                var targets = PickTargets(state, tc, ally);
                if (targets.Count == 0) continue;
                Log(state, ally.Source.Character.Name, true, ActionType.SkillAttack, sk.Name, 0,
                    $"공격 {Math.Max(1, tfd.TriggerCount)}회 누적 → 추가 피해 (적{targets.Count})");
                foreach (var t in targets)
                {
                    if (tc <= 1) ApplySingleTargetHit(state, ally, t, sk, isSkill: true, pen: false);
                    else ApplyDamage(state, ally, t, CalcDamageToEnemy(ally, t, sk, state), sk.Name, isSkill: true, hitCount: HitCountOf(ally, sk));
                }
            }
        }

        /// <summary>
        /// 협공(CoopAttack) — 유저 확정 규칙(2026-11-18): **아군 평타에만**, 평타 1회당 판정 1번.
        ///   대상 = 그 평타가 공격한 적. 대상 수 N(쥬리 3)이면 공격받은 적 1 + 나머지 중 무작위 N−1.
        ///   확률 = TriggerChance + 초월 CoopChanceBonus + 협공확률 버프(Coop_Chance). 랜덤 대신 기댓값(피해 × 확률 × 피격확률).
        ///   피해 = 계산기 모델(SYSTEM 5.4): 공/방계수 × 배율 + 대상최대HP비례(공격력 상한), 피증·받피증·공성 감쇄 적용, 치명·약점 미적용.
        ///   단일 평타의 "공격받은 적"은 ApplySingleTargetHit과 같은 분포(약점→최저HP, 미발동→무작위).
        ///   자기 평타에는 자기 협공 미발동(가정). 협공 보유자가 사망·행동불가면 미발동.
        /// </summary>
        private void ProcessCoopAttacks(SiegeBattleState state, CharacterBattleState attacker, Skill normal,
            SiegeEnemyState primary, int basicTargetCount)
        {
            foreach (var owner in state.AllyStates.ToList())
            {
                if (owner == attacker || owner.IsDead || owner.Effects.HasActionBlockingCC()) continue;
                var passive = owner.Source.Character.Passive;
                if (passive == null) continue;
                var coop = GetPassiveEffects(passive, owner)
                    .FirstOrDefault(e => e.Type == PersistentEffectType.CoopAttack && e.CoopAttack != null)?.CoopAttack;
                if (coop == null) continue;

                double chance = coop.TriggerChance + owner.Effects.GetTotalBuffs().Coop_Chance;
                double ratioBonus = 0, hpBonus = 0;
                if (passive.TranscendBonuses != null)
                    foreach (var kv in passive.TranscendBonuses.Where(t => t.Key <= owner.Source.TranscendLevel))
                    {
                        chance += kv.Value.CoopChanceBonus;
                        ratioBonus += kv.Value.CoopRatioBonus;
                        hpBonus += kv.Value.CoopHpRatioBonus;
                    }
                double p = Math.Clamp(chance / 100.0, 0, 1);
                if (p <= 0) continue;

                // 평타가 공격한 적의 분포
                var cands = SingleTargetCandidates(state);
                if (!cands.Contains(primary)) cands.Add(primary);
                int n = cands.Count;
                double pw = basicTargetCount <= 1 ? WeakProb(attacker, normal) : 1.0;
                if (n <= 1) pw = 1.0;
                // 협공 추가 대상(무작위 N−1)에 뽑힐 확률
                int tcCoop = Math.Max(1, coop.TargetCount);
                double q = n > 1 ? Math.Min(1.0, (tcCoop - 1) / (double)(n - 1)) : 0;

                var sk = new Skill
                {
                    Name = (passive.Name ?? "패시브") + "(협공)",
                    SkillType = SkillType.Skill1,
                    LevelData = new Dictionary<int, SkillLevelData>
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = coop.Ratio + ratioBonus,
                            AtkCount = Math.Max(1, coop.AtkCount),
                            TargetCount = Math.Min(tcCoop, Math.Max(1, n)),
                            TargetMaxHpRatio = coop.TargetMaxHpRatio > 0 ? coop.TargetMaxHpRatio + hpBonus : 0,
                            AtkCap = coop.AtkCap,
                        }
                    },
                };

                var hits = new List<(SiegeEnemyState T, double Dmg, double W)>();
                foreach (var t in cands)
                {
                    double wAtk = t == primary ? pw + (1 - pw) / n : (1 - pw) / n;   // 이 적이 평타 대상일 확률
                    double w = p * (wAtk + (1 - wAtk) * q);
                    if (w <= 0) continue;
                    var input = BuildDamageInputToEnemy(owner, t, sk, state, out double redMult, out _, out _, out _);
                    // 협공도 치명·약점 적용 (2026-11-19 영상: 치명·약점 아이콘 + 약점 배율 1.53 확인).
                    double dmg = _damageCalc.Calculate(input).FinalDamage * Math.Max(0, redMult);
                    double leakPre = dmg;
                    if (coop.CurrentHpBonusDmgMax > 0 && t.MaxHp > 0)   // 대상 현재 생명력 비례 최대 N% 피증(쥬리 50) — 협공 피해 전체에 곱
                    {
                        // R3 불멸 적은 생명력이 바닥나지 않는다: 영상 역산으로 보스 20.0%, 친위대(룩·챈슬러) 24.4%에서 고정
                        //   (6턴 이후 협공이 69턴까지 같은 값 — 레이첼 ×1.1000, 룩·챈슬러 ×1.1218). 친위대 값이 20%가 아닌 이유는 미확인.
                        double hpFloor = state.CurrentRound >= 3 ? (t.Source.HasEnrage ? 0.20 : 0.2437) : 0;
                        dmg *= 1 + coop.CurrentHpBonusDmgMax / 100.0 * Math.Clamp(t.CurrentHp / t.MaxHp, hpFloor, 1);
                    }
                    if (_leak != null) LeakOnCalc(state, owner, t, sk, input, redMult, 1.0, leakPre > 0 ? dmg / leakPre : 1.0, dmg);
                    hits.Add((t, w * dmg, w));
                }
                if (hits.Count == 0) continue;
                {
                    double eDmg = hits.Sum(h => h.Dmg);   // 이 판정의 기댓값 pD → 분산 p(1−p)D² = (pD)²(1−p)/p
                    state.CoopJudgments++; state.CoopExpectedProcs += p; state.CoopExpectedDamage += eDmg;
                    state.CoopVariance += eDmg * eDmg * (1 - p) / p;
                }
                Log(state, owner.Source.Character.Name, true, ActionType.SkillAttack, sk.Name, 0,
                    $"협공 (확률 {p * 100:0}% 기댓값) ← {attacker.Source.Character.Name} 평타, 적{hits.Count}");
                foreach (var h in hits.OrderBy(h => h.T == primary ? 1 : 0))
                    ApplyDamage(state, owner, h.T, h.Dmg, sk.Name, isSkill: true, hitWeight: h.W, spill: h.T != primary, hitCount: HitCountOf(owner, sk));
                CountAttackForBuffExtend(state, owner, p);   // 협공도 "모든 공격"에 포함(기댓값 가중)
                AdvanceTime(state, p * state.CoopAttackSeconds, $"[협공] {owner.Source.Character.Name}");   // 협공 연출 시간(기댓값)
            }
        }

        /// <summary>
        /// 「자신의 모든 공격 N회 발동 시 턴제 버프 +1턴(본래 턴 초과 불가)」 — 쥬리 신의 저울.
        /// 평타·스킬·협공을 각각 공격 1회로 센다(협공은 발동 확률만큼 가중). 스탯 버프(BuffValues)만 대상.
        /// </summary>
        private void CountAttackForBuffExtend(SiegeBattleState state, CharacterBattleState ally, double weight = 1.0)
        {
            if (ally == null || ally.IsDead || weight <= 0) return;
            int n = ally.Source.Character.Passive?.GetLevelData(ally.Source.IsSkillEnhanced)?.TimedBuffExtendEveryAttacks ?? 0;
            if (n <= 0) return;
            _atkForBuffExtend.TryGetValue(ally.PartyIndex, out double c);
            c += weight;
            while (c >= n - 1e-9)
            {
                c -= n;
                int ext = 0;
                var extNames = new List<string>();
                foreach (var e in ally.Effects.GetAllEffects())
                    if (!e.IsPermanent && e.BuffValues != null && e.MaxDuration > 0
                        && e.RemainingTurns > 0 && e.RemainingTurns < e.MaxDuration)
                    { e.RemainingTurns++; ext++; extNames.Add($"{e.SourceName ?? "?"} {e.RemainingTurns}/{e.MaxDuration}턴"); }
                if (ext > 0)
                    Log(state, ally.Source.Character.Name, true, ActionType.SkillAttack, "턴제 버프 증가", 0,
                        $"공격 {n}회 → 자신의 턴제 버프 {ext}개 +1턴 [{string.Join(", ", extNames)}]");
            }
            _atkForBuffExtend[ally.PartyIndex] = c;
        }

        /// <summary>아군이 적에게 데미지 적용 + 점수·로그.</summary>
        private void ApplyDamage(SiegeBattleState state, CharacterBattleState ally, SiegeEnemyState target,
            double dmg, string label, bool isSkill, bool penetrate = false, double hitWeight = 1.0, bool spill = false,
            int hitCount = 1)
        {
            if (dmg <= 0) return;
            object lh = _leak?.BeginHit(ally, target, ally.Source.Character.Name, label, state.CurrentTurn);   // [누수 계측]
            bool leakCapped = false;

            // 피격 시 상태이상 부여 패시브(레이첼 「화염의 힘」) — 면역·무효·보호막과 무관하게 "적중" 자체로 발동.
            ApplyEnemyOnHitStatus(state, target, hitCount * hitWeight);

            // R3 시스템 기믹(일요일 룩·챈슬러): 피해 1 고정 — 보스(크리스)에게만 유효타가 들어가도록 시스템이 강제.
            //   패시브/버프 아닌 고정 속성(SiegeDamageCapToOne)이라 버프해제·턴제버프감소로 풀 수 없음.
            //   단일 타격은 약점 미발동 시 무작위 적 → 확률(1−약확)×2/3로 룩·챈에 빠진다(ApplySingleTargetHit).
            //   기댓값 분산 타격(hitWeight<1)은 "1 × 가중치".
            if (target.Source?.SiegeDamageCapToOne == true && dmg > hitWeight)
            {
                if (lh != null) { SiegeLeakRecorder.Lose(lh, "피해1고정", dmg - hitWeight); leakCapped = true; }
                dmg = hitWeight;
            }

            // 피해 면역 (화 R3 룩 스킬: 적 전체 [2턴]) — 이 대상이 면역 중이면 피해 0. 단 관통(IgnoresTurnDamageImmunity)이면 면역 무시.
            if (target.ImmunityTurns > 0 && !penetrate)
            {
                SiegeLeakRecorder.Lose(lh, "면역", dmg);
                if (!spill)
                    Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                        label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 피해 면역 (무효)");
                ApplyHitCdReduce(state, target, hitWeight);   // 피격 자체는 발생 → 일 쿨감 패시브는 트리거
                return;
            }

            // 보스 피해 무효화 (델론즈 「죽음의 경계」: 아군 사망 시 부여) — 직격 1회 무효·1 차감, 점수 미집계.
            //   기댓값 분산분(spill)은 횟수를 소모하지 않고 무효만(근사).
            if (target.NullifyHitsRemaining > 0)
            {
                SiegeLeakRecorder.Lose(lh, "무효", dmg);
                if (spill) return;
                target.NullifyHitsRemaining--;
                Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                    label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 피해 무효 (죽음의 경계, 잔여 {target.NullifyHitsRemaining}회)");
                ApplyHitCdReduce(state, target, hitWeight);
                return;
            }

            // 받피감 버프(루디 방어 준비 = 데미지감소 10%): 루디는 「보호막[5턴]」「데미지감소[5턴]」 2개의 독립 버프를
            //   동시에 얻는다(유저 확정). 받피감은 대체HP 풀과 별개라 ★데미지로 보호막 풀을 깨도 사라지지 않는다 →
            //   ShieldTurns(버프 지속턴)로만 게이팅(Shield 잔량 무관). 흡수 後 HP딜(점수 집계분)에도 곱연산 → 점수 직접 영향.
            //   루디 대체HP(방어력×100=9.3만)는 첫 타에 깨져도 -10%는 5턴 내내 유효. 비스킷 리프어택(쿨60s)이
            //   버프해제로 둘 다 지워야(DispelBuffCount=2=정확히 이 2개) -10%가 풀린다 → 재해제가 점수에 유의미.
            //   [2026-10 실측 정정] 이 받피감은 루디 전용이 아니라 「링크」 — 룩·챈슬러 포함 적 전원에게 걸린다 → LinkTurns/LinkDmgRdc.
            if (lh != null && target.LinkTurns > 0 && target.LinkDmgRdc > 0)
                SiegeLeakRecorder.Lose(lh, "링크", dmg * target.LinkDmgRdc / 100.0);
            if (target.LinkTurns > 0 && target.LinkDmgRdc > 0)
                dmg *= 1.0 - target.LinkDmgRdc / 100.0;

            // 보호막 흡수: 보호막이 있으면 그만큼 먼저 깎이고 점수에 집계되지 않음. 파괴되면 잔여만 HP로(점수 집계).
            if (target.Shield > 0)
            {
                double absorbed = Math.Min(target.Shield, dmg);
                target.Shield -= absorbed;
                dmg -= absorbed;
                SiegeLeakRecorder.Lose(lh, "보호막", absorbed);
                bool broke = target.Shield <= 0;
                Log(state, ally.Source.Character.Name, true, isSkill ? ActionType.SkillAttack : ActionType.NormalAttack,
                    label, 0, $"{ally.Source.Character.Name} → {target.Source.Name}: 보호막 흡수 {absorbed:N0}{(broke ? " (보호막 파괴)" : $" (잔여 {target.Shield:N0})")} — 점수 미집계");
                ApplyHitCdReduce(state, target, hitWeight);
                if (dmg <= 0) return;   // 전부 보호막에 흡수 → HP 피해 없음
                // 파괴 후 잔여 피해는 HP로 (아래 점수 집계). dmg는 이미 적 방어력 반영(post-def, BossDef=enemy.Stats.Def)이라
                //   보호막 흡수분·HP 잔여분 모두 방어력이 포함된 값이다.
            }

            // R1/R2: 적 최대체력만큼만 점수 누적(오버킬 미집계). R3: 적 미사망이라 오버킬 누적.
            double scoredDmg = (state.CurrentRound < 3)
                ? System.Math.Max(0, System.Math.Min(dmg, target.CurrentHp))
                : dmg;
            if (lh != null)
            {
                SiegeLeakRecorder.Lose(lh, "오버킬", dmg - scoredDmg);
                SiegeLeakRecorder.Scored(lh, scoredDmg, leakCapped || scoredDmg < dmg);
            }
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

            ApplyHitCdReduce(state, target, hitWeight);   // 피격 시 쿨감 패시브(일요일 몹) — 분산 타격은 가중치만큼
        }

        /// <summary>일요일 몹 패시브: 피격된 적이 쿨감 패시브 보유 시, 공격력 최고 적의 스킬 쿨 N초 감소.</summary>
        private void ApplyHitCdReduce(SiegeBattleState state, SiegeEnemyState hit, double weight = 1.0)
        {
            double red = (hit.Source?.SiegeHitCdReduce ?? 0) * weight;
            if (red <= 0) return;
            var top = state.Enemies.OrderByDescending(e => e.FinalAtk).FirstOrDefault();
            if (top == null) return;
            foreach (var k in top.SkillCooldowns.Keys.ToList())
                top.SkillCooldowns[k] = Math.Max(0, top.SkillCooldowns[k] - red);
        }

        #endregion

        #region 타겟팅 / 데미지 계산

        /// <summary>
        /// 타겟 선정(타겟팅): 약점공격 = 생명력 최저 적, 동률이면 앞열(Position 낮은 순).
        ///   ※ 약점 "데미지 계수"는 항상 100%가 아니라 실제 약확 기댓값(CalcDamageToEnemy의 ExpectedCritWeak). 여기선 타겟팅만.
        /// 라운드3: 약확 100%↑ 단일기는 보스(크리스) 강제 집중(친위대 회피), 미만이면 최저 HP(룩/챈슬러로 분산).
        /// </summary>
        /// <summary>스킬 타겟 수만큼 적 선정 (최저 HP 우선·동률 앞열). 광역기는 여러 적을 친다. R3는 보스만.
        /// R3 약점공격 집중(일요일 핵심 기믹): 공격자 약점확률 100%↑이면 단일 타격을 전열 보스(자리2=크리스)로 집중 →
        /// 룩/챈슬러(쿨감 패시브 보유 친위대)를 안 때려 보스 쿨타임 감소를 차단. 100% 미만이면 기존 최저HP(룩/챈슬러로 분산).</summary>
        private List<SiegeEnemyState> PickTargets(SiegeBattleState state, int count, CharacterBattleState attacker = null)
        {
            // (구 "약확 100%↑면 R3 요일보스 고정" 분기는 제거 — 단일 타격의 약점/무작위 분산은 SingleTargetCandidates +
            //  ApplySingleTargetHit가 담당. 여기 반환 [0] = 약점 대상(최저 HP) = 부가효과(디버프·DoT·반격) 대상.)
            // R1/R2: 살아있는 적(HP>0)만 타겟 — 죽은 적은 HP 음수로 영원히 "최저HP"라 단일 평타가
            //   시체만 도그파일하고 옆 적이 안 죽어 라운드가 안 깨지던 버그 수정(R2 16턴 → 즉시).
            //   R3 보스는 무사망(HP≤0 허용)이라 IsBoss 필터만(HP 무관) — 보스를 계속 때려 점수 누적.
            IEnumerable<SiegeEnemyState> candidates = state.CurrentRound >= 3
                ? state.Enemies.Where(e => e.IsBoss)
                : state.Enemies.Where(e => e.CurrentHp > 0);
            var list = candidates.ToList();
            if (list.Count == 0) list = state.Enemies;   // 전멸 직전 폴백(이 행동 후 라운드 전환)
            return list.OrderBy(e => e.CurrentHp).ThenBy(e => e.Position).Take(System.Math.Max(1, count)).ToList();
        }

        /// <summary>단일 타격이 갈 수 있는 적 후보(PickTargets와 같은 풀): R3=IsBoss 전원, R1/R2=생존 적.</summary>
        private List<SiegeEnemyState> SingleTargetCandidates(SiegeBattleState state)
        {
            var list = (state.CurrentRound >= 3
                ? state.Enemies.Where(e => e.IsBoss)
                : state.Enemies.Where(e => e.CurrentHp > 0)).ToList();
            return list.Count == 0 ? state.Enemies.ToList() : list;
        }

        /// <summary>약점 발동 확률 = 공격자 약확(표시스탯+전투중 버프+스킬 내장 약확), 0~1.</summary>
        private static double WeakProb(CharacterBattleState ally, Skill skill)
        {
            var ds = ally.DisplayStats ?? new BaseStatSet();
            double skillWek = skill?.GetTotalBonus(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel)?.Wek ?? 0;
            return Math.Clamp((ds.Wek + ally.Effects.GetTotalBuffs().Wek + skillWek) / 100.0, 0, 1);
        }

        /// <summary>
        /// 단일 타격(타겟 1명) 1회 — 인게임 타게팅 규칙(유저 확정 2026-11-16):
        ///   약점 발동(확률 pw = 약확) → 최저 HP 적(primary) / 미발동(1−pw) → 후보 중 무작위 1명(primary 포함, 균등).
        /// 랜덤 대신 기댓값으로 분산: primary는 pw×D(약점) + (1−pw)/n×D(비약점), 나머지는 각 (1−pw)/n×D(비약점).
        ///   피해 1 고정(일 R3 룩·챈)·피격 쿨감 패시브(일 몹 크리스 쿨−15초)도 가중치만큼 기댓값 적용.
        ///   (빔서치가 운 좋은 시드에 과적합하지 않도록 결정적 기댓값. 디버프·DoT·반격은 primary에만 — 근사.)
        /// 약확 100%↑이면 전부 primary(일요일 크리스 집중 기믹), 후보 1명이면 기존과 동일.
        /// </summary>
        private void ApplySingleTargetHit(SiegeBattleState state, CharacterBattleState ally, SiegeEnemyState primary,
            Skill skill, bool isSkill, bool pen)
        {
            var cands = SingleTargetCandidates(state);
            if (!cands.Contains(primary)) cands.Add(primary);
            double pw = WeakProb(ally, skill);
            if (cands.Count <= 1 || pw >= 1.0)
            {
                ApplyDamage(state, ally, primary, CalcDamageToEnemy(ally, primary, skill, state), skill.Name, isSkill, pen, hitCount: HitCountOf(ally, skill));
                return;
            }
            double wRand = (1 - pw) / cands.Count;
            // 피해를 먼저 전부 계산한 뒤 적용(앞 적용이 뒤 계산의 잃은HP 등에 섞이지 않게).
            var hits = new List<(SiegeEnemyState T, double Dmg, double W)>();
            foreach (var t in cands)
            {
                double w = wRand, dmg = wRand * CalcDamageToEnemy(ally, t, skill, state, forceWeak: false);
                if (t == primary && pw > 0)
                {
                    dmg += pw * CalcDamageToEnemy(ally, t, skill, state, forceWeak: true);
                    w += pw;
                }
                hits.Add((t, dmg, w));
            }
            foreach (var h in hits.OrderBy(h => h.T == primary ? 1 : 0))   // 분산분 먼저, primary 마지막
                ApplyDamage(state, ally, h.T, h.Dmg, skill.Name, isSkill, pen, hitWeight: h.W, spill: h.T != primary, hitCount: HitCountOf(ally, skill));
        }

        /// <summary>아군 → 적 데미지 (DamageCalculator). 공성전 감쇄(물/마·타겟수)·디버프 반영.
        ///   치명·약점은 실제 확률 기댓값(ExpectedCritWeak=true) — 치확/약확 기어 옵션이 딜에 실제로 반영된다.</summary>
        private double CalcDamageToEnemy(CharacterBattleState ally, SiegeEnemyState target, Skill skill, SiegeBattleState state = null,
            bool? forceWeak = null)
        {
            var input = BuildDamageInputToEnemy(ally, target, skill, state,
                out double reductionMult, out double elemReduction, out double targetReduction, out double atkRed);
            // 약점 발동/미발동 고정(치명은 기댓값 유지) — 단일 타격 약점·무작위 분산(ApplySingleTargetHit)용. 내부 pw가 0~1로 clamp.
            if (forceWeak == true) input.WeakChance = 1e6;
            else if (forceWeak == false) input.WeakChance = -1e6;
            var dr = _damageCalc.Calculate(input);
            double raw = dr.FinalDamage;
            // 조건이 확률적(대상 상태이상이 기댓값 DoT로만 걸려 있음)이면 충족/미충족 피해를 확률 가중 — 예: 샤오 출혈 조건.
            double pCond = SkillConditionEvaluator.MetProbability(
                skill.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened)?.Condition, ally, target, state);
            if (pCond > 0 && pCond < 1)
            {
                input.IsSkillConditionMet = false;
                double rawUnmet = _damageCalc.Calculate(input).FinalDamage;
                input.IsSkillConditionMet = true;
                raw = pCond * raw + (1 - pCond) * rawUnmet;
            }
            double final = raw * Math.Max(0, reductionMult);
            if (_leak != null && state != null) LeakOnCalc(state, ally, target, skill, input, reductionMult, pCond, 1.0, final);

            // [진단] 추적 스킬 타격 시 입력 구성요소 덤프
            if (IsDiagSkill(skill.Name) && _diagCount < 200)
            {
                _diagCount++;
                int diagTurn = state?.CurrentTurn ?? -1;
                int diagRound = state?.CurrentRound ?? -1;
                DiagLog.AppendLine($"───────── T{diagTurn,2} R{diagRound} {ally.Source.Character.Name} {skill.Name} → {target.Source.Name}(보스={target.IsBoss}, HP {target.CurrentHp:N0}/{target.MaxHp:N0}) ─────────");
                DiagLog.AppendLine($"  FinalAtk(공감반영후)={input.FinalAtk:N0} (공감={atkRed:F0}%, 스냅FinalAtk={ally.FinalAtk:N0}) 치피={input.CritDamage} 약피={input.WeakpointDmg}");
                DiagLog.AppendLine($"  [아군버프] 피증={input.DmgDealt} 타입피증={input.DmgDealtType} 보스피증={input.DmgDealtBoss} 3인기={input.Dmg1to3} 방관={input.ArmorPen}"
                    + (input.PerDebuffBonusPercent > 0 ? $" 디버프당={input.PerDebuffBonusPercent}%×{System.Math.Min(input.TargetDebuffCount, input.PerDebuffBonusMaxStacks)}개(적 디버프 {input.TargetDebuffCount})" : ""));
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

        // ── 누수 계측 (config.RecordLeak) — 읽기 전용. 전투 상태를 바꾸지 않는다. ──
        /// <summary>피해 계산 1건을 누수 기록기에 넘긴다(기준 상태 표본도 함께 갱신).</summary>
        private void LeakOnCalc(SiegeBattleState state, CharacterBattleState ally, SiegeEnemyState target, Skill skill,
            DamageCalculator.DamageInput input, double reductionMult, double pCond, double extraMult, double final)
        {
            LeakSample(state, skill);
            bool magic = ally.Source.Character.AttackType == AttackType.Magic;
            var mid = ally.Effects.GetTotalBuffs();
            CurrentEffAtk(ally, mid, out double atkRed);
            _leak.OnCalc(ally, target, ally.PartyIndex, target.IsBoss, magic, input, reductionMult, pCond, extraMult, final,
                magic ? mid.MagicAtk_Rate : mid.Atk_Rate, atkRed, ally.Stage3AtkRate);
        }

        /// <summary>지금 이 순간 아군 전원의 버프 상태·적 전원의 디버프 상태를 "그 전투의 최고 상태" 기준에 반영.</summary>
        private void LeakSample(SiegeBattleState state, Skill skill)
        {
            var anyEnemy = state.Enemies.FirstOrDefault();
            if (anyEnemy == null || skill == null) return;
            foreach (var a in state.AllyStates)
            {
                if (a.IsDead) continue;
                // 공격자 쪽 필드(치명·약점·피증·방무)는 스킬·대상과 무관 → 아무 스킬·아무 적으로 입력을 만들어 읽는다.
                var inp = BuildDamageInputToEnemy(a, anyEnemy, skill, state, out _, out _, out _, out double red);
                var mid = a.Effects.GetTotalBuffs();
                bool magic = a.Source.Character.AttackType == AttackType.Magic;
                _leak.SampleAlly(a.PartyIndex, a.Source.Character.Name, inp, magic ? mid.MagicAtk_Rate : mid.Atk_Rate, red);
            }
            foreach (var e in state.Enemies)
            {
                var d = e.Effects.GetTotalDebuffs();
                _leak.SampleEnemy(e.IsBoss, d.Def_Reduction, d.Vulnerability, d.Boss_Vulnerability,
                    d.GetEffectiveDmgTakenIncrease(AttackType.Physical), d.GetEffectiveDmgTakenIncrease(AttackType.Magic),
                    CountEnemyDebuffs(e));
            }
        }

        /// <summary>아군의 현재 실효 공격력 (자기 공격 속성 기준) — 데미지 계산·인게임 버프 타게팅 공용.
        ///   ally.FinalAtk = 전투시작 스냅샷(상시 패시브·파티버프 포함, 액티브 스킬버프 제외).
        ///   전투 중 부여된 공격%버프(예: 클로에 청소시간 마공20%)는 stage-3 곱연산으로 반영(스냅샷과 겹치지 않음).
        ///   아군에 걸린 공격력 감소 디버프(예: 보스 레이첼 불새 공감24/마공감24)는 실효 공격력에서 차감.</summary>
        private static double CurrentEffAtk(CharacterBattleState ally, BuffSet midBuffs, out double atkRed)
        {
            bool magic = ally.Source.Character.AttackType == AttackType.Magic;
            var allyDebuffs = ally.Effects.GetTotalDebuffs();
            atkRed = magic ? allyDebuffs.MagicAtk_Reduction : allyDebuffs.Atk_Reduction;
            double midAtkRate = magic ? midBuffs.MagicAtk_Rate : midBuffs.Atk_Rate;
            // 공격력 감소는 별도 곱(×0.76)이 아니라 3단계 공격력 버프 합에서 뺀다: (1 + 버프합 − 감소) / (1 + 버프합).
            //   2026-11-19 영상: 불새 공감 24% 상태의 미호 살육의 춤 80,692 = 공감 없는 값 101,975 × (1.15 − 0.24)/1.15 (윈디 15%).
            //   ×0.76이면 77,501로 4.1% 낮다.
            double s3 = 1 + ally.Stage3AtkRate / 100.0;
            double redMult = s3 > 0 ? System.Math.Max(0, s3 - atkRed / 100.0) / s3 : System.Math.Max(0, 1 - atkRed / 100.0);
            return ally.FinalAtk * (1 + midAtkRate / 100.0) * redMult;
        }

        private static double CurrentEffAtk(CharacterBattleState ally)
            => CurrentEffAtk(ally, ally.Effects.GetTotalBuffs(), out _);

        /// <summary>"공격력이 가장 높은 아군" 순위 정렬. 강제 순위(HighestAtkAllyOrder)가 항상 최우선.
        ///   InGameBuffTargeting=true → 시전 순간 현재 공격력 순(인게임 규칙).
        ///   false → 스카우팅 누적딜(DamageWeight) 순, 동률은 스냅샷 공격력(기존 낙관 모델).</summary>
        private static IOrderedEnumerable<CharacterBattleState> OrderByAtkRank(
            IEnumerable<CharacterBattleState> allies, SiegeBattleState state, bool forCdr)
        {
            var o = allies.OrderBy(a => state.AtkRankKey(a.PartyIndex, forCdr));
            return state.InGameBuffTargeting
                ? o.ThenByDescending(a => CurrentEffAtk(a)).ThenBy(a => a.PartyIndex)
                : o.ThenByDescending(a => a.DamageWeight).ThenByDescending(a => a.FinalAtk);
        }

        /// <summary>아군→적 DamageInput 구성 (CalcDamageToEnemy·진단 공용). 공성 감쇄(곱연산)는 reductionMult로 반환.</summary>
        private DamageCalculator.DamageInput BuildDamageInputToEnemy(CharacterBattleState ally, SiegeEnemyState target, Skill skill,
            SiegeBattleState state, out double reductionMult, out double elemReduction, out double targetReduction, out double atkRed)
        {
            var battleChar = ally.Source;

            var character = battleChar.Character;
            var enemy = target.Source;
            var baseStats = character.GetBaseStats();

            int targetCount = skill.GetTargetCount(battleChar.IsSkillEnhanced, battleChar.TranscendLevel);
            // 평타도 1인 공격 → 1인기 감쇄 적용 (2026-10 연습전투 실측: 나타·미호 평타 3건 일치. 이전엔 평타 면제).
            targetReduction = GetTargetReduction(enemy, targetCount);
            // 공성전 감쇄: 캐릭터 공격속성에 따라 물리/마법 받피감
            elemReduction = character.AttackType == AttackType.Magic
                ? enemy.MagicReduction
                : enemy.PhysicalReduction;

            var debuffs = target.Effects.GetTotalDebuffs();

            // 시전자 데미지 스탯 = 전투시작 표시스탯 스냅샷(DisplayStats: gear/세트/초월/버프/패시브 포함)
            //   + 전투 중 추가된 스킬 버프(Effects 델타). 치명·약점은 확률 기반 기댓값으로 계산.
            var ds = ally.DisplayStats ?? new BaseStatSet();
            var midBuffs = ally.Effects.GetTotalBuffs();

            double effAtk = CurrentEffAtk(ally, midBuffs, out atkRed);

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
                DmgAmp = ds.Dmg_Amp,   // 피해증폭 = 최종 별도 곱
                DmgDealtType = ds.Dmg_Dealt_Type + midBuffs.Dmg_Dealt_Type + midBuffs.Mark_Energeia + midBuffs.Mark_Purify,
                DmgDealtBoss = ds.Dmg_Dealt_Bos + midBuffs.Dmg_Dealt_Bos,
                Dmg1to3 = ds.Dmg_Dealt_1to3 + midBuffs.Dmg_Dealt_1to3,
                Dmg1 = ds.Dmg_Dealt_1 + midBuffs.Dmg_Dealt_1,
                Dmg3 = ds.Dmg_Dealt_3 + midBuffs.Dmg_Dealt_3,
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
                // 조건부 추가피해의 발동 조건을 실제 전투 상태로 판정 (조건 미입력 스킬은 항상 true).
                //   예: 타카 죽음의 무도 +260%는 대상 HP 30% 미만일 때만.
                IsSkillConditionMet = SkillConditionEvaluator.IsMet(
                    skill.GetLevelData(battleChar.IsSkillEnhanced, battleChar.IsAwakened)?.Condition, ally, target, state),
                // 잃은HP 비례 보너스(예: 광풍참 +50%) — 대상 실제 잔여HP%로 비례. R3 보스 음수HP면 0%잔여→풀보너스.
                IsLostHpConditionMet = true,
                LostHpActualRemainingPct = target.MaxHp > 0 ? target.CurrentHp / target.MaxHp * 100.0 : 0,
                Mode = BattleMode.Boss,
                IsTargetBoss = target.IsBoss,
                IsTargetHighestMaxHp = IsHighestMaxHpEnemy(state, target),
                SelfMaxHp = ally.MaxHp,
            };
            // 적 디버프 1개당 피해량 증가 (세인 2초월 20%×최대4, 레긴레이프·탄지로) — 단일보스 시뮬과 같은 집계.
            var perDebuff = PerDebuffBonusExtractor.From(character, skill, battleChar.IsSkillEnhanced, battleChar.TranscendLevel);
            if (perDebuff.IsActive)
            {
                input.TargetDebuffCount = CountEnemyDebuffs(target);
                input.PerDebuffBonusPercent = perDebuff.PercentPerDebuff;
                input.PerDebuffBonusMaxStacks = perDebuff.MaxStacks;
            }
            if (state?.DiagForceCritWeak == true) input.ExpectedCritWeak = false;   // [진단] 치명·약점 항상 발동
            else if (_critRng != null && state != null) input.SampleRng = _critRng;      // [표본 모드] 타격마다 추첨
            // 공성전 감쇄 = 곱연산 (받는 물/마 피해 N% 감소 × 타겟수별 감소). 합연산 시 180%↑ 차감으로 음수가 되므로.
            // 대상수 감쇄(1인 70%·5인 90% 등)는 별도 곱이 아니라 "받는 피해 증가"(취약·보스취약·받피증)와 합산
            //   → 취약계수 = 1 + (취약+보스취약+받피증 − 대상수감쇄)/100 (0 하한). 실측: 보스취약30·1인70 → 0.6배.
            //   물/마 감쇄(물리 90% 등)는 아직 미검증 → 기존 곱연산 유지.
            input.TargetCountReduction = targetReduction;
            reductionMult = 1 - elemReduction / 100.0;
            return input;
        }

        /// <summary>대상이 "최대 생명력이 가장 높은 적군"인가 — 생존 적(현재HP>0, 대상 자신은 항상 포함) 중 최대HP 최고.
        ///   쥬다스 최후의 권좌 171% 추가 피해 판정. state 없으면(단발 평가) true.</summary>
        private static bool IsHighestMaxHpEnemy(SiegeBattleState state, SiegeEnemyState target)
        {
            if (state?.Enemies == null || target == null) return true;
            foreach (var e in state.Enemies)
                if (e != target && e.CurrentHp > 0 && e.MaxHp > target.MaxHp) return false;
            return true;
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

        /// <summary>[허수아비 DPS] 스쿼드 풀버프를 dealerIndex 딜러에 적용하고 보스(HP=0=잃은체력 최대)에서 nukeSkill
        ///   단타 데미지를 계산. 로테/cadence와 무관한 정적 딜 지표 — 딜러 기어 비교 + 딜비중 우선순위용.
        ///   상시 패시브·파티버프는 FinalAtk 스냅샷에 이미 포함(아래 CalcDamageToEnemy 주석), 여기선 턴제 스킬버프·
        ///   스킬 적디버프만 풀업(99턴) 적용 → CalcDamageToEnemy가 합산(이중계산 없음). 패시브 적디버프·펫은
        ///   Initialize→ApplyStandingEnemyDebuffs가 이미 적용. "받을 수 있는 버프 다 켠" 천장 지표.</summary>
        public double EvaluateDummyNuke(SiegeBattleConfig config, int dealerIndex, Skill nukeSkill)
        {
            if (nukeSkill == null) return 0;
            var state = Initialize(config);   // FinalAtk(상시+파티 perm 포함) + 상시 적디버프(패시브·펫) 적용됨
            if (dealerIndex < 0 || dealerIndex >= state.AllyStates.Count) return 0;
            var dealer = state.AllyStates[dealerIndex];

            // 스쿼드 전원의 비평타 스킬이 거는 턴제 파티버프 + 스킬 적디버프를 풀업으로 적용(같은 종류는 MaxMerge 병합).
            foreach (var ally in state.AllyStates)
                foreach (var sk in ally.Source.Character.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (sk.SkillType == SkillType.Normal || sk.SkillType == SkillType.Normal2 || (sk.IsAwakenOnly && !ally.Source.IsAwakened)) continue;
                    ApplySkillEffects(state, ally, sk, state.Enemies, preDamage: true);
                    ApplySkillEffects(state, ally, sk, state.Enemies, preDamage: false);
                }

            // 보스(주 점수 타깃) = 최대HP 적. HP=0 → 잃은체력비례 최대(R3 넉 레짐).
            var boss = state.Enemies.OrderByDescending(e => e.MaxHp).FirstOrDefault();
            if (boss == null) return 0;
            boss.CurrentHp = 0;

            // 공격 발동형 스택 디버프(타카 매의발톱 EagleClaw 등)를 ★max 스택으로 선적용 — 스쿼드가 유지 가능한 디버프.
            //   단발 평가라 트리거가 안 돌므로 명시 적용(스택당 받물피증 등 × MaxStacks). ProcessAttackStacks와 동일 분류.
            foreach (var ally in state.AllyStates)
            {
                var pas = ally.Source.Character.Passive;
                var plvl = pas?.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
                if (plvl?.Effects == null) continue;
                var trEff = pas.GetTranscendBonus(ally.Source.TranscendLevel)?.Effects;
                foreach (var be in plvl.Effects)
                {
                    var ef = be;
                    if (trEff != null)
                    {
                        var ov = trEff.FirstOrDefault(e => e.StatusType == be.StatusType && e.StatusType != StatusEffectType.None);
                        if (ov != null) ef = ov;
                    }
                    if (ef.ApplyMode != ApplyMode.Triggered || ef.MaxStacks <= 0) continue;
                    if (ef.Type != PersistentEffectType.Debuff || ef.Debuff == null) continue;
                    if (ef.Target != EffectTarget.Enemy && ef.Target != EffectTarget.AllEnemies) continue;
                    AddEnemyDebuff(boss, ScaleDebuff(ef.Debuff, ef.MaxStacks), 99,
                        $"dummy_stack:{ally.PartyIndex}:{ef.StatusType}", EffectCategory.StackDebuff);
                }
            }

            // 감사용: 허수아비 보스에 실제로 적용된 디버프(방깎·취약·받피증) 요약 — 디버퍼 디버프 반영 확인.
            LastDummyDebuffSummary = SummarizeDebuff(boss.Effects.GetTotalDebuffs());
            LastDummyBuffSummary = SummarizeBuff(dealer.Effects.GetTotalBuffs());
            return CalcDamageToEnemy(dealer, boss, nukeSkill, state);
        }

        /// <summary>[진단] 1타 데미지 분해 — 연습전투 실측 대조용. round 라운드의 targetName 적(HP를 targetHpPct%로 설정)에게
        ///   dealerIndex 아군이 skill 시전 시, activeSkills로 지정한 아군 액티브 스킬의 버프/디버프만 적용(상시 패시브·펫은
        ///   Initialize·ApplyStandingEnemyDebuffs로 항상 포함). 치명/약점 4조합의 확정 데미지 + 입력 요약을 반환.
        ///   보스 보호막·링크(받피감)는 ApplyDamage 단계라 여기엔 미반영 = "링크 해제 상태" 데미지.</summary>
        public HitBreakdown EvaluateHitBreakdown(SiegeBattleConfig config, int dealerIndex, Skill skill, int round,
            string targetName, double targetHpPct, IEnumerable<(int AllyIndex, SkillType Skill)> activeSkills,
            IEnumerable<(int AllyIndex, int Triggers)> attackStackTriggers = null)
        {
            var state = Initialize(config);
            if (round > 1)
            {
                state.CurrentRound = round;
                state.InitializeRound(round);
                ApplyStandingEnemyDebuffs(state, config);
            }
            foreach (var (ai, st) in activeSkills ?? Enumerable.Empty<(int, SkillType)>())
            {
                var a = state.AllyStates[ai];
                var sk = a.Source.Character.Skills.FirstOrDefault(x => x.SkillType == st);
                if (sk == null) continue;
                ApplySkillEffects(state, a, sk, state.Enemies, preDamage: true);
                ApplySkillEffects(state, a, sk, state.Enemies, preDamage: false);
            }
            // 공격 발동형 중첩 디버프(타카 매의 발톱: 스킬 1회 = 2중첩)를 지정 횟수만큼 발동 — "타카 6중첩" 같은 상태 재현용.
            foreach (var (ai, n) in attackStackTriggers ?? Enumerable.Empty<(int, int)>())
                for (int k = 0; k < n; k++) ProcessAttackStacks(state, state.AllyStates[ai], isSkill: true);
            var dealer = state.AllyStates[dealerIndex];
            var target = state.Enemies.FirstOrDefault(e => e.Source.Name == targetName) ?? state.Enemies.First();
            target.CurrentHp = target.MaxHp * targetHpPct / 100.0;

            var input = BuildDamageInputToEnemy(dealer, target, skill, state,
                out double redMult, out _, out _, out _);
            var hb = new HitBreakdown
            {
                Input = input, ReductionMult = redMult,
                DealerBuffs = SummarizeBuff(dealer.Effects.GetTotalBuffs()),
                TargetDebuffs = SummarizeDebuff(target.Effects.GetTotalDebuffs()),
                TargetDef = target.Source.Stats.Def,
                Logs = state.TurnLogs.Select(l => $"{l.ActorName}/{l.SkillName}: {l.Description}").ToList(),
            };
            input.ExpectedCritWeak = true;
            hb.Expected = _damageCalc.Calculate(input).FinalDamage * redMult;
            input.ExpectedCritWeak = false;
            foreach (var (c, w) in new[] { (true, true), (true, false), (false, true), (false, false) })
            {
                input.IsCritical = c; input.IsWeakpoint = w;
                var dr = _damageCalc.Calculate(input);
                hb.Combos[(c, w)] = dr.FinalDamage * redMult;
                if (c && w) hb.Detail = dr;
            }
            return hb;
        }

        /// <summary>[감사] 마지막 EvaluateDummyNuke에서 보스에 적용된 디버프 / 딜러에 적용된 액티브 버프 요약.</summary>
        public string LastDummyDebuffSummary { get; private set; }
        public string LastDummyBuffSummary { get; private set; }

        /// <summary>적 → 아군 데미지 (적 공격력 vs 아군 방어/받피감). 적은 치확·약확 0이라 비치명·비약점 기본.
        /// forceCrit/critDmgOverride: 반격 등 강제 치명(제이브 반격 치확100·치피650)용.</summary>
        private double CalcDamageToAlly(SiegeEnemyState enemy, CharacterBattleState ally, Skill enemySkill,
            bool forceCrit = false, double critDmgOverride = -1, bool noCrit = false)
        {
            var e = enemy.Source;
            // 아군 받피감(자버프) + 아군에게 걸린 받피증/취약(적 디버프)
            var (perm, timed, pet) = ally.Effects.GetSeparatedBuffs();
            double allyDmgRdc = perm.Dmg_Rdc + timed.Dmg_Rdc + pet.Dmg_Rdc;
            // 영웅 자신의 받는 피해 감소 스탯(장비 메인옵 받피감·초월 등). 그간 버프만 합산해 장비 받피감이 통째로 빠져 있었음.
            //   2026-11-19 영상(불새): 비스킷 10% → 581(계산 580), 라이언 32%+막기 → 172(계산 171.6).
            allyDmgRdc += ally.StatDmgRdc;
            // 물리/마법 받피감(타입별) — 적 공격 속성에 맞는 것만 적용. (그간 누락돼 탱킹/유틸 패시브의
            //   물리감소·마법감소가 무시됨 → 전열 탱킹 가치 저평가로 자리 배치 왜곡됐음.)
            bool enemyMagic = e.AttackType == AttackType.Magic;
            allyDmgRdc += enemyMagic
                ? perm.Mag_Dmg_Rdc + timed.Mag_Dmg_Rdc + pet.Mag_Dmg_Rdc
                : perm.Phys_Dmg_Rdc + timed.Phys_Dmg_Rdc + pet.Phys_Dmg_Rdc;
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
            // 적 진영 버프(챈슬러 분쇄 → 스파이크 치확100·치피+500). 미해제 시 치명타 학살.
            double buffCri = enemy.HasEnemyBuff ? enemy.EnemyBuff.Cri : 0;
            double buffCriDmg = enemy.HasEnemyBuff ? enemy.EnemyBuff.Cri_Dmg : 0;
            bool enemyCrit = !noCrit && (forceCrit || (e.Stats.Cri + buffCri) >= 100);   // 적 치확(보통 0; 반격·버프는 강제). DoT 틱은 치명 없음
            double critDmg = critDmgOverride >= 0 ? critDmgOverride : (e.Stats.Cri_Dmg + buffCriDmg);
            // 적 치명타 피해 상한(실측 2026-11-25 토요일 영상): 분쇄 버프(치피 150 + 500 = 650%) 지진이 실제로는 치피 400% 값으로 들어온다
            //   (T6 레이첼 1,932·비스킷 1,081 — 650%면 3,140·1,757). 툴팁은 +500% 그대로라 "과치피는 효율이 떨어진다"로 해석(유저 추정).
            //   아군 쪽 치피는 310%까지만 실측 일치를 확인했으므로 상한을 적→아군에만 둔다.
            //   반격 등 치피를 직접 지정한 경로(critDmgOverride, 금요일 제이브 650%)는 실측이 없어 그대로 둔다.
            if (critDmgOverride < 0) critDmg = System.Math.Min(critDmg, MaxEnemyCritDamagePct);
            // 탄성(전용무기): 치명타 공격 피격 시 받는 피해 % 감소. 받피감과 동일 채널로 합산.
            double tanseong = enemyCrit ? (ally.DisplayStats?.CritDmg_Taken_Reduction ?? 0) : 0;
            // 받는 쪽 감소(아군 받피감·탄성)와 주는 쪽 증감(적 피해량 감소 디버프·광폭화)은 서로 다른 곱이다 — 아래 enemyOut 참고.
            double effDmgRdc = allyDmgRdc + tanseong;

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
            // 아군 막기(기댓값): 막기 성공 시 피해 50% + 막기 피해 감소율(수문장 4세트 10%) 감소.
            //   (2026-11 수요일 녹화 대조 전까지 아군 피격엔 막기가 아예 없어 막기 98% 라이언 등 탱커가 과잉 사망.)
            //   아군에 걸린 막기 확률 감소 디버프(DebuffSet.Blk_Red)만큼 확률 차감. DoT 등 별도 경로는 무관.
            double blkChance = Math.Clamp((ally.DisplayStats?.Blk ?? 0) - allyDebuffs.Blk_Red, 0, 100) / 100.0;
            double blkCut = Math.Min(1.0, 0.5 + (ally.DisplayStats?.Blk_Red ?? 0) / 100.0);
            double blockMult = 1 - blkChance * blkCut;
            // 광폭화는 패시브 보유 적(요일 보스)만. 룩·챈슬러 친위대는 보스취급이나 광폭화 없음 → ×1.0.
            // 적이 주는 피해 증감 = 1 + 광폭화% − 피해량 감소 디버프% (한 괄호 안에서 합산).
            //   실측(2026-11-25 토요일 영상, 혹한의 지진 1타): 레이첼 T6 1,932 → T30 2,132 → T54 3,736, 비스킷 1,081 → 1,193.
            //   T54/T30 = 1.752 = (1+1.5−0.17)/(1+0.5−0.17) — 광폭화와 염화 피감 17이 합산. 곱이면 2.5/1.5 = 1.667.
            //   T30/T6 = 1.1035 = (1.33 × 공감 0.78)/(1−0.06) — 받피감 0인 레이첼과 26%인 비스킷이 같은 비 → 아군 받피감은 별도 곱.
            double enemyOut = 1.0 + (e.HasEnrage ? EnrageMultiplier() - 1.0 : 0.0) - enemyDebuffs.Dmg_Reduction / 100.0;
            return _damageCalc.Calculate(input).FinalDamage * blockMult * System.Math.Max(0, enemyOut);
        }

        /// <summary>적→아군 치명타 피해 상한(%). 토요일 분쇄 버프 실측으로 정함 — CalcDamageToAlly 주석 참고.</summary>
        public const double MaxEnemyCritDamagePct = 400;

        /// <summary>
        /// 광폭화 배수 (광폭화 패시브 보유 적=요일 보스만; 룩·챈슬러 친위대 제외 — e.HasEnrage로 게이팅):
        /// 누적 턴(state.CurrentTurn = 게임 70턴 시즈 카운터)에 따라
        /// 적이 주는 피해 증폭. 30턴 +50% / 40턴 +100% / 50턴 +150% / 55턴 +200% / 60턴 +500%.
        /// 아군→적(점수)엔 적용하지 않고 적→아군 피해에만 곱연산 — 후반 생존 현실성 반영.
        /// </summary>
        private double EnrageMultiplier()
        {
            int t = _activeState?.CurrentTurn ?? 0;
            return t >= 60 ? 6.0
                 : t >= 55 ? 3.0
                 : t >= 50 ? 2.5
                 : t >= 40 ? 2.0
                 : t >= 30 ? 1.5
                 : 1.0;
        }

        /// <summary>광폭화 단계가 올라가는 턴에 1회 로그(적 피해 증폭 진입 표기).</summary>
        private void LogEnrageTransition(SiegeBattleState state)
        {
            double m = EnrageMultiplier();
            int tier = m >= 6 ? 5 : m >= 3 ? 4 : m >= 2.5 ? 3 : m >= 2 ? 2 : m >= 1.5 ? 1 : 0;
            if (tier <= _lastEnrageTier) return;
            _lastEnrageTier = tier;
            if (tier == 0) return;
            Log(state, "시스템", false, ActionType.BuffApplied, "광폭화", 0,
                $"광폭화 진입 (T{state.CurrentTurn}) — 적이 주는 피해 ×{m:F2} (+{(m - 1) * 100:F0}%)");
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
        /// 실명 중이면 빗나감(피해 -20%·치명 없음·상태이상 없음 — 타격은 들어감; 실명 턴은 기본공격만 소모). 발동 시 ActionSeconds초 경과(전체 쿨 감소).
        /// 라이언 물리면역(도발+면역) / 보호막 / 생존판정은 ApplyDamageToAlly가 자동 처리.
        /// 반격 대상에 용염(화상, 매 턴 공격력 120%)[2턴] 부여 — 틱 피해는 ProcessEnemyDotsOnAlly. 화상 면역이면 무효.
        /// </summary>
        private void ExecuteEnemyCounter(SiegeBattleState state, SiegeEnemyState enemy, Models.SiegeCounterattack ca)
        {
            // 실명 중 반격 = 빗나감: 타격은 들어가되 피해 -20%·치명 없음(강제 치명도 꺼짐)·상태이상(용염) 없음. 실명 턴은 기본공격만 소모.
            bool missed = enemy.BlindTurnsRemaining > 0;
            var counterSkill = new Skill
            {
                Name = "반격", SkillType = SkillType.Skill1,
                LevelData = new Dictionary<int, SkillLevelData>
                { [0] = new SkillLevelData { Ratio = ca.Ratio, AtkCount = 1, TargetCount = 1 } },
            };
            var targets = PickRandomAllies(state, ca.TargetCount);   // 도발 우선 → 라이언 도발 시 라이언에게(물리면역=0뎀)
            foreach (var t in targets)
            {
                double dmg = missed
                    ? CalcDamageToAlly(enemy, t, counterSkill, noCrit: true) * MissDamageMultiplier
                    : CalcDamageToAlly(enemy, t, counterSkill, forceCrit: true, critDmgOverride: ca.CritDamage);
                ApplyDamageToAlly(state, enemy, t, dmg, missed ? "반격(빗나감)" : "반격");
                if (!missed && ca.Status.HasValue)   // 용염[100%/2턴] — 화상 면역이면 무효
                    AddEnemyStatusToAlly(state, enemy, t, ca.Status.Value, ca.StatusDuration, 1, ca.StatusAtkRatio);
            }
            Log(state, enemy.Source.Name, false, ActionType.SkillAttack, "반격", 0,
                missed ? $"{enemy.Source.Name} 반격 빗나감 → 아군 {targets.Count}명 (실명: 피해 -20%·치명 없음·상태이상 없음)"
                       : $"{enemy.Source.Name} 반격 발동 → 아군 {targets.Count}명 (물리 {ca.Ratio:0}%·치명 치피{ca.CritDamage:0}%)");
            AdvanceTime(state, ca.ActionSeconds, "반격");   // 반격 3초 → 전체(아군+적) 쿨 감소
        }

        /// <summary>적이 아군을 공격 → 피해 적용. (생존 메카닉: 부활·면역·권능은 후속 단계)</summary>
        /// <param name="dotSource">null이 아니면 DoT 틱(화상·용염) — 직격이 아니라서 물리/마법 한정 면역은 못 막고(모든 피해 무효만),
        ///   피격 횟수형 무효화도 차감하지 않는다(적 쪽 델론즈 무효화와 같은 규칙). enemy는 null 가능.</param>
        private void ApplyDamageToAlly(SiegeBattleState state, SiegeEnemyState enemy, CharacterBattleState ally,
            double dmg, string label, string dotSource = null)
        {
            if (dmg <= 0 || ally.IsDead) return;
            if (state.DiagAlliesImmortal) return;   // [진단] 불사 실험 — 피격 자체를 무시
            if (state.DiagImmortalAllies != null && state.DiagImmortalAllies.Contains(ally.Source.Character.Name)) return;   // 부분 불사
            bool isDot = dotSource != null;
            string srcName = isDot ? dotSource : enemy.Source.Name;

            // 피해 무효화 (피격 N회 / N턴) — 피해 자체를 0으로. 타입 한정(물리/마법) 면역은 적 공격 타입과 일치할 때만.
            bool nullMatch = ally.NullifyType == DamageNullType.All
                || (!isDot && ally.NullifyType == (enemy.Source.AttackType == AttackType.Magic ? DamageNullType.Magic : DamageNullType.Physical));
            if ((ally.NullifyHitsRemaining > 0 || ally.NullifyTurnsRemaining > 0) && nullMatch)
            {
                if (!isDot && ally.NullifyHitsRemaining > 0) ally.NullifyHitsRemaining--;
                state.TurnLogs.Add(new BattleTurnLog
                {
                    Turn = state.CurrentTurn, ActorName = srcName, IsAlly = false,
                    ActionType = ActionType.BuffApplied, SkillName = "피해 무효화", DamageDealt = 0,
                    Description = isDot
                        ? $"{ally.Source.Character.Name} {label} 틱 무효화"
                        : $"{ally.Source.Character.Name} 피격 무효화 (잔여 {ally.NullifyHitsRemaining}회)",
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
                        Turn = state.CurrentTurn, ActorName = srcName, IsAlly = false,
                        ActionType = ActionType.BuffApplied, SkillName = "보호막", DamageDealt = 0,
                        Description = $"{ally.Source.Character.Name} 보호막 흡수 {absorbed:N0} (잔여 {ally.Shield:N0})",
                    });
                    return;
                }
            }

            ally.CurrentHp -= dmg;
            if (ally.CurrentHp > 0 && ally.MaxHp > 0)   // [생존 여유] 피격 직후 최저 HP%
                ally.MinHpPctAfterHit = Math.Min(ally.MinHpPctAfterHit, 100.0 * ally.CurrentHp / ally.MaxHp);
            string outcome = "";
            if (ally.CurrentHp <= 0)
            {
                // 생존 판정: 불굴/불사 → 권능 → 부활 → 사망 (공유 SurvivalResolver)
                var r = SurvivalResolver.ResolveLethal(ally);
                if (ally.IsDead) ally.CurrentHp = 0;
                outcome = $" ({r.Label})";
                if (r.Survived) ally.LethalSaves++;   // [생존 여유] 치사 피해를 생존 수단으로 넘김
                if (r.Survived && r.Label == "부활") OnAllyRevived(state, ally);   // 부활 연계 (아일린 청천벽력·진 발동·피해 면역)
            }

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = srcName,
                IsAlly = false,
                ActionType = isDot ? ActionType.DoTDamage : state.IsSkillTurn ? ActionType.SkillAttack : ActionType.NormalAttack,
                SkillName = label,
                DamageDealt = dmg,
                Description = isDot
                    ? $"[DoT] {label}({srcName}) → {ally.Source.Character.Name}: {dmg:N0}{outcome} (HP {Math.Max(0, ally.CurrentHp):N0}/{ally.MaxHp:N0})"
                    : $"{enemy.Source.Name} → {ally.Source.Character.Name}: {dmg:N0}{outcome} (HP {Math.Max(0, ally.CurrentHp):N0}/{ally.MaxHp:N0})",
            });

            // 사망 이벤트 기록 (사망 로그용): 이번 피격으로 처음 사망(부활 못 함). 권능=생존이라 미기록.
            if (ally.IsDead)
                _deaths.Add(new SiegeDeathEvent
                {
                    Turn = state.CurrentTurn, Elapsed = state.ElapsedSeconds,
                    AllyName = ally.Source.Character.Name, Cause = $"{srcName} {label}",
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
            CheckHpThresholdHeal(state, ally);      // 생명력 임계 자힐(샤오 50%↓ 등) 트리거
        }

        /// <summary>살아있는 아군 중 랜덤 1명 (없으면 null). 도발 중인 아군이 있으면 그 중에서 우선 선택.</summary>
        /// <summary>빗나감(실명) 시 피해 배수 — 피해 20% 감소(유저 확인 2026-11-23). 치명 미적용·디버프 무시는 호출부에서 처리.</summary>
        private const double MissDamageMultiplier = 0.8;

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

        /// <summary>면역 패시브 보유자의 기본공격 → 대상의 상태이상 면역 턴제버프 갱신.
        /// (인게임 규칙·유저 확인 2026-11-18: 면역 패시브는 전투 진입 시 [2턴] 부여되고 보유자가 평타를 칠 때마다 갱신.
        ///  DB의 ApplyMode 표기(Triggered/상시)와 무관하게 모든 면역 패시브가 같은 방식.)</summary>
        private void TriggerAllyImmunity(SiegeBattleState state, CharacterBattleState ally)
        {
            var passive = ally.Source.Character.Passive;
            if (passive == null) return;
            foreach (var e in GetPassiveEffects(passive, ally))
            {
                if (e.Type != PersistentEffectType.Immunity) continue;
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
                    $"{scope} {names} 면역 [{e.StatusImmunity.Duration}턴] (평타 갱신)");
            }
        }

        /// <summary>적 스킬/평타의 상태이상을 아군에 부여 (면역 보유 타입은 무효).</summary>
        private void ApplyEnemyStatusToAlly(SiegeBattleState state, CharacterBattleState ally, Skill enemySkill,
            SiegeEnemyState enemy = null)
        {
            if (ally.IsDead) return;
            var statuses = enemySkill.GetLevelData(false)?.StatusEffects;   // 적 스킬은 단일 티어
            if (statuses == null) return;
            foreach (var se in statuses)
            {
                if (!AddEnemyStatusToAlly(state, enemy, ally, se.Type, se.Duration, se.Stacks, se.CustomAtkRatio)) continue;

                // 즉사 2중첩 시 즉시 사망 (크리스 어둠의 일격 Stacks=2 또는 누적 2회). 면역은 위에서 이미 차단됨.
                if (se.Type == StatusEffectType.InstantDeath && !ally.IsDead
                    && ally.Effects.GetTotalStacks(StatusEffectType.InstantDeath) >= 2)
                    KillAllyByInstantDeath(state, ally, "즉사 2중첩");
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
        /// 적→아군 DoT 1틱 피해(가중치 1 기준) — 직격과 같은 식(CalcDamageToAlly)에 배율만 화상 80% / 용염 120%.
        /// 유저 확인(2026-11-18): 화상 피해에도 방어력·받피감 등이 모두 적용된다 → 방어계수·받피감(공용+물/마)·받피증·취약·막기·광폭화 반영.
        /// 치명은 없음(noCrit). 틱 시점의 아군 방어 상태·적 공격력 감소 디버프로 계산한다(스냅샷 아님).
        /// </summary>
        private double EnemyDotTick(SiegeEnemyState enemy, CharacterBattleState ally, double atkRatio)
        {
            if (enemy == null || atkRatio <= 0) return 0;
            var dotSkill = new Skill
            {
                Name = "DoT", SkillType = SkillType.Skill1,
                LevelData = new Dictionary<int, SkillLevelData>
                { [0] = new SkillLevelData { Ratio = atkRatio, AtkCount = 1, TargetCount = 1 } },
            };
            return CalcDamageToAlly(enemy, ally, dotSkill, noCrit: true);
        }

        private static int HitCountOf(CharacterBattleState ally, Skill skill)
            => Math.Max(1, skill.GetAtkCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened));

        /// <summary>
        /// 적이 아군 한 명에게 상태이상 1건 부여 (면역 보유 타입은 무효 → false).
        /// 공격력 비례 DoT(화상 80%·용염 120%)면 건 적(TickSource)과 가중치(TickWeight = 중첩 × weight)를 효과에 담는다
        ///   → 그 아군의 기본공격(턴) 직후마다 ProcessEnemyDotsOnAlly가 틱 피해를 계산해 차감. 갱신형 아님: 부여마다 자기 지속 턴을 가진 새 인스턴스.
        /// weight ≠ 1 = 기댓값 분산 부여(피격 화상: 확률 × 타수 × 대상으로 뽑힐 확률) — 같은 적·같은 잔여 턴 인스턴스에 가중치를 합산.
        /// </summary>
        private bool AddEnemyStatusToAlly(SiegeBattleState state, SiegeEnemyState enemy, CharacterBattleState ally,
            StatusEffectType type, int duration, int stacks, double? customAtkRatio, double weight = 1.0, bool log = true)
        {
            if (ally.IsDead || weight <= 0) return false;
            var baseEffect = StatusEffectDb.Get(type);
            string stName = baseEffect?.Name ?? type.ToString();
            if (ally.StatusImmunityTurns.GetValueOrDefault(type) > 0)
            {
                // 면역 → 무효 (로그로 표기)
                if (log)
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "면역", 0,
                        $"{stName} 무효 (면역)");
                return false;
            }
            if (baseEffect == null) return false;
            int dur = duration > 0 ? duration : baseEffect.Duration;
            stacks = Math.Max(1, stacks);
            double atkRatio = customAtkRatio ?? baseEffect.AtkRatio;
            bool isDot = enemy != null && atkRatio > 0;
            double tickW = isDot ? stacks * weight : 0;
            double tick = isDot ? EnemyDotTick(enemy, ally, atkRatio) * tickW : 0;   // 로그용 추정치(실제 차감은 틱 시점 재계산)
            bool ev = weight != 1.0;
            string id = $"siege_cc:{ally.PartyIndex}:{type}{(ev ? $":ev:{enemy?.Source.Id}:{atkRatio:0}" : "")}";
            if (ev)
            {
                if (!isDot) return false;   // 기댓값 분산은 DoT만 (CC를 확률로 쪼갤 수 없음)
                var same = ally.Effects.GetAllEffects().FirstOrDefault(e => e.Id == id && !e.IsExpired && e.RemainingTurns == dur
                    && ReferenceEquals(e.TickSource, enemy));
                if (same != null) { same.TickWeight += tickW; return true; }
            }
            var data = StatusEffectData.FromDbEffect(baseEffect);
            if (customAtkRatio.HasValue) data.AtkRatio = customAtkRatio.Value;
            ally.Effects.AddEffect(new BattleEffect
            {
                Id = id,
                SourceName = id,
                StatusType = type,
                StatusData = data,
                RemainingTurns = dur,
                Stacks = stacks,
                IsPermanent = false,
                MergeStrategy = MergeStrategy.Stack,
                TickWeight = tickW,
                TickSource = isDot ? enemy : null,
                TickSourceName = enemy?.Source.Name,
            });
            if (log)
                Log(state, enemy?.Source.Name ?? "적", false, ActionType.DebuffApplied, stName, 0,
                    $"{ally.Source.Character.Name} {stName} 부여 [{dur}턴]{(stacks > 1 ? $" ×{stacks}중첩" : "")}"
                    + (tick > 0 ? $" (틱 {tick:N0})" : ""));
            return true;
        }

        /// <summary>
        /// 피격 시 상태이상 부여 패시브 — 레이첼 「화염의 힘」: 적의 공격 1회 적중당할 시 적군 3명 화상[100%/2턴].
        /// hits = 이 적이 맞은 타수(기댓값 분산 타격은 가중치 포함). 무작위 3명 대신 기댓값: 생존 아군 각자 min(1, 3/N) 확률로 부여
        ///   → 틱 피해에 확률 × 타수를 곱해 한 인스턴스로 합산(빔 평가가 RNG에 의존하지 않도록 결정적). 화상 면역 아군은 제외.
        /// </summary>
        private void ApplyEnemyOnHitStatus(SiegeBattleState state, SiegeEnemyState hitEnemy, double hits)
        {
            var oh = hitEnemy.Source?.OnHitStatus;
            if (oh == null || hits <= 0) return;
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            if (alive.Count == 0) return;
            double p = Math.Min(1.0, oh.TargetCount / (double)alive.Count) * Math.Clamp(oh.Chance / 100.0, 0, 1);
            int applied = 0;
            foreach (var a in alive)
                if (AddEnemyStatusToAlly(state, hitEnemy, a, oh.Type, oh.Duration, 1, oh.AtkRatio, weight: p * hits, log: false))
                    applied++;
            string stName = StatusEffectDb.Get(oh.Type)?.Name ?? oh.Type.ToString();
            if (applied > 0)
                Log(state, hitEnemy.Source.Name, false, ActionType.DebuffApplied, stName, 0,
                    $"{hitEnemy.Source.Name} 피격 {hits:0.##}회 → 아군 {applied}명 {stName} [{oh.Duration}턴] (1인당 기댓값 ×{p * hits:0.##}"
                    + (applied < alive.Count ? $", 면역 {alive.Count - applied}명" : "") + ")");
        }

        /// <summary>
        /// 아군에게 걸린 적 DoT(화상·용염) 틱 — TickAllyAfterAction에서만 호출 = 그 아군의 기본공격(턴 소모) 직후 1회.
        /// 인스턴스별 틱 피해(EnemyDotTick × 가중치)를 건 적별로 합쳐 한 번에 차감. 방어력·받피감·막기·광폭화 적용, 보호막은 흡수,
        ///   모든 피해 무효화 중이면 무효(횟수 미차감). 치사량이면 생존 판정(불사/권능/부활) 후 사망 기록.
        /// </summary>
        private void ProcessEnemyDotsOnAlly(SiegeBattleState state, CharacterBattleState ally)
        {
            var dots = ally.Effects.GetAllEffects()
                .Where(e => e.TickWeight > 0 && !e.IsExpired && e.TickSource is SiegeEnemyState).ToList();
            if (dots.Count == 0) return;
            foreach (var g in dots.GroupBy(e => (e.StatusType, e.TickSource)))
            {
                if (ally.IsDead) return;
                var first = g.First();
                string stName = first.StatusData?.AtkRatio == 120 && first.StatusType == StatusEffectType.Burn ? "용염"
                    : StatusEffectDb.Get(first.StatusType ?? StatusEffectType.None)?.Name ?? "DoT";
                var src = (SiegeEnemyState)first.TickSource;
                double dmg = g.GroupBy(e => e.StatusData?.AtkRatio ?? 0)
                    .Sum(r => EnemyDotTick(src, ally, r.Key) * r.Sum(e => e.TickWeight));
                ApplyDamageToAlly(state, null, ally, dmg, stName, dotSource: first.TickSourceName ?? "적");
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
            ProcessInstantDeathOnAlly(state, ally);   // 즉사 DoT(현재HP%) + 마지막 턴 사망 (일요일 크리스)
            if (ally.IsDead) return;
            ProcessEnemyDotsOnAlly(state, ally);      // 화상·용염 틱 (수 레이첼 / 금 제이브·챈슬러)
            if (ally.IsDead) return;
            if (!state.ForceFullBuffUptime) ally.Effects.TickTurn();   // [진단] 풀uptime이면 아군 버프 만료 스킵
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

        /// <summary>
        /// 즉사(일요일 크리스) — TickAllyAfterAction에서만 호출 = 즉사 보유 아군의 **기본공격(평타·턴 소모) 직후**에만 tick.
        ///   스킬턴(턴 미소모)엔 발동 안 함. 매 평타마다 현재 생명력 N%(기본 20%) 피해,
        /// 그 즉사 효과의 마지막 턴(이번 tick에 만료)이면 즉시 사망. 면역 보유 시엔 애초에 부여되지 않음.
        /// 여러 즉사 인스턴스가 겹쳐도 DoT는 1회(최대 비율)만 — "매 평타 현재 HP N%"는 상태당이 아닌 대상당.
        /// </summary>
        private void ProcessInstantDeathOnAlly(SiegeBattleState state, CharacterBattleState ally)
        {
            var deaths = ally.Effects.GetStatusEffectsOfType(StatusEffectType.InstantDeath);
            if (deaths.Count == 0) return;

            // DoT: 현재 HP의 최대 비율% (보통 20%) — 보호막/무효화 무시(HP 직접 차감 DoT).
            double ratio = deaths.Max(e => e.StatusData?.TargetCurrentHpRatio ?? 0) / 100.0;
            if (ratio > 0 && ally.CurrentHp > 0)
            {
                double dmg = ally.CurrentHp * ratio;
                ally.CurrentHp -= dmg;
                Log(state, "적", false, ActionType.SkillAttack, "즉사", dmg,
                    $"{ally.Source.Character.Name} 즉사 피해 {dmg:N0} (HP {Math.Max(0, ally.CurrentHp):N0}/{ally.MaxHp:N0})");
                if (ally.CurrentHp <= 0)
                {
                    var r = SurvivalResolver.ResolveLethal(ally);
                    if (ally.IsDead) { ally.CurrentHp = 0; RecordInstantDeath(state, ally, "즉사 피해"); return; }
                    Log(state, "적", false, ActionType.BuffApplied, "즉사", 0,
                        $"{ally.Source.Character.Name} 즉사 피해 치명 — {r.Label}로 생존");
                }
            }

            // 마지막 턴(이번 tick에 RemainingTurns가 0이 됨) 즉시 사망.
            if (deaths.Any(e => e.RemainingTurns <= 1))
                KillAllyByInstantDeath(state, ally, "즉사 만료");
        }

        /// <summary>즉사 즉시 사망 처리 (2중첩·마지막 턴). 생존 패시브(불사/권능/부활)도 즉사엔 무력 — 무조건 사망.</summary>
        private void KillAllyByInstantDeath(SiegeBattleState state, CharacterBattleState ally, string cause)
        {
            if (ally.IsDead) return;
            ally.CurrentHp = 0;
            ally.IsDead = true;
            Log(state, "적", false, ActionType.SkillAttack, "즉사", 0,
                $"{ally.Source.Character.Name} {cause}로 즉시 사망");
            RecordInstantDeath(state, ally, cause);
        }

        /// <summary>즉사 사망 이벤트 기록 + 「죽음의 경계」 등 사망 트리거(델론즈 외 일요일엔 무관).</summary>
        private void RecordInstantDeath(SiegeBattleState state, CharacterBattleState ally, string cause)
        {
            _deaths.Add(new SiegeDeathEvent
            {
                Turn = state.CurrentTurn, Elapsed = state.ElapsedSeconds,
                AllyName = ally.Source.Character.Name, Cause = cause,
            });
            foreach (var en in state.Enemies)
                if (en.Source.OnAllyDeathNullifyHits > 0)
                    en.NullifyHitsRemaining = en.Source.OnAllyDeathNullifyHits;
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
            var lvl = skill.GetLevelData(caster.Source.IsSkillEnhanced, caster.Source.IsAwakened);
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
            FireAllySkillTriggeredCasts(state);   // 아군 스킬 N회마다 패시브 시전 (동영)
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

                    // 트리거 회복 (예: 손오공 미후분신술 — 자신의 스킬 1회 발동 시 모든 아군이 시전자 마법공 35% 회복).
                    //   위 보호막·흡혈은 "스킬을 쓴 당사자가 받는" 효과라 caster에만 적용하지만, 이건 패시브 보유자가
                    //   시전했을 때 아군 전체가 받는 회복이다 → owner==caster일 때만, 회복량은 보유자 스탯 기준.
                    if (e.Type == PersistentEffectType.TriggeredHeal && owner == caster)
                    {
                        double flat = owner.FinalAtk * e.TriggeredHealAtkRatio / 100.0
                                    + owner.FinalDef * e.TriggeredHealDefRatio / 100.0;
                        var healTargets = e.Target == EffectTarget.Party
                            ? state.AllyStates.Where(a => !a.IsDead).ToList()
                            : new List<CharacterBattleState> { caster };
                        foreach (var t in healTargets)
                        {
                            double amount = flat + t.MaxHp * e.TriggeredHealHpRatio / 100.0;
                            if (amount > 0) HealAlly(state, t, amount, owner.Source.Character.Name, "패시브 회복(스킬 시전)");
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

        /// <summary>생명력 임계 회복 (전투당 1회). 피격/DoT 직후 호출.
        ///   ① 자기 패시브 자힐 — 예: 샤오 HP50%↓ → 자기 물공 40~45% 회복
        ///   ② 다른 아군의 파티 패시브 — 예: 손오공 2초월 "[모든 아군] HP50%↓ 시 시전자 마법공 95% 회복"
        ///      (회복량이 보유자 스탯 기준이라 ①의 자기-스탯 계산으론 표현이 안 된다)</summary>
        private void CheckHpThresholdHeal(SiegeBattleState state, CharacterBattleState ally)
        {
            if (ally.IsDead || ally.HpThresholdHealUsed || ally.MaxHp <= 0) return;
            double hpPct = ally.CurrentHp / ally.MaxHp * 100.0;

            // 자기 패시브(Self/미지정) 우선, 없으면 아군의 Party 패시브를 찾는다.
            foreach (var owner in state.AllyStates.Where(a => !a.IsDead))
            {
                var passive = owner.Source.Character.Passive;
                if (passive == null) continue;
                bool isSelf = owner == ally;
                foreach (var e in GetPassiveEffects(passive, owner))
                {
                    if (e.Type != PersistentEffectType.TriggeredHeal) continue;
                    if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.OnHpBelow) continue;
                    // 남의 패시브는 Party 대상일 때만 이 아군에게 적용된다.
                    if (!isSelf && e.Target != EffectTarget.Party) continue;
                    if (hpPct > e.TriggerHpThreshold) continue;

                    ally.HpThresholdHealUsed = true;
                    double amount = owner.FinalAtk * e.TriggeredHealAtkRatio / 100.0
                                  + owner.FinalDef * e.TriggeredHealDefRatio / 100.0
                                  + ally.MaxHp * e.TriggeredHealHpRatio / 100.0;
                    HealAlly(state, ally, amount, owner.Source.Character.Name, $"임계 회복(HP{e.TriggerHpThreshold:0}%↓)");
                    return;
                }
            }
        }

        /// <summary>패시브 효과(레벨+초월) 합산.</summary>
        private IEnumerable<PersistentEffect> GetPassiveEffects(Passive passive, CharacterBattleState ally)
        {
            var list = new List<PersistentEffect>();
            var lvl = passive.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
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
                        AddEnemyDebuff(enemy, e.Debuff, 99, $"siege_pasdebuff:{ally.PartyIndex}:{enemy.Position}",
                            EffectCategory.PassiveDebuff);   // 상시 패시브(비스킷 방깎 등) — 턴제와 다른 카테고리(Add)
                    }
                }
                // 펫 디버프 (예: 윈디 보스취약)
                if (config.AllyPet != null)
                {
                    var pd = config.AllyPet.GetSkillDebuff(config.PetStar, config.PetEnhance);
                    if (pd != null && !string.IsNullOrEmpty(SummarizeDebuff(pd)))
                        AddEnemyDebuff(enemy, pd, 99, $"siege_petdebuff:{enemy.Position}",
                            EffectCategory.PetDebuff);   // 펫 디버프 — 별개 카테고리(항상 Add)
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

        /// <summary>전투 진입 시 면역 패시브 부여 — 모든 상태이상 면역 패시브가 진입 즉시 [지속턴]만큼 걸린다.
        /// 이후 갱신은 보유자 기본공격(TriggerAllyImmunity)뿐 → 보유자가 죽거나 행동불가면 지속턴 뒤 소멸.</summary>
        private void ApplyEntryImmunities(SiegeBattleState state)
        {
            foreach (var ally in state.AllyStates.Where(a => !a.IsDead))
            {
                var passive = ally.Source.Character.Passive;
                if (passive == null) continue;
                foreach (var e in GetPassiveEffects(passive, ally))
                {
                    if (e.Type != PersistentEffectType.Immunity || e.StatusImmunity?.Types == null) continue;
                    IEnumerable<CharacterBattleState> targets =
                        e.Target == EffectTarget.Party ? state.AllyStates : new[] { ally };
                    foreach (var tgt in targets)
                        foreach (var type in e.StatusImmunity.Types)
                            tgt.StatusImmunityTurns[type] = Math.Max(
                                tgt.StatusImmunityTurns.GetValueOrDefault(type),
                                Math.Max(1, e.StatusImmunity.Duration));
                    var names = string.Join("·", e.StatusImmunity.Types.Select(t => StatusEffectDb.Get(t)?.Name ?? t.ToString()));
                    string scope = e.Target == EffectTarget.Party ? "전체 아군" : ally.Source.Character.Name;
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "면역", 0,
                        $"{scope} {names} 면역 [{Math.Max(1, e.StatusImmunity.Duration)}턴] (전투 진입)");
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
            var statuses = skill.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened)?.StatusEffects;
            if (statuses == null) return;
            foreach (var se in statuses)
            {
                var baseEffect = StatusEffectDb.Get(se.Type);
                if (baseEffect == null) continue;
                double tick = DotTickDamage(ally, target, se.Type, se.CustomAtkRatio, se.CustomTargetMaxHpRatio, se.CustomAtkCap);
                if (tick <= 0) continue;
                int dur = se.Duration > 0 ? se.Duration : baseEffect.Duration;

                // 세나 규칙(2026-11-17 유저 확인): 상태이상은 갱신형이 아니다 — 부여될 때마다 자기 지속 턴을 가진 새 인스턴스 추가.
                //   부여 횟수 = 툴팁: "각 공격마다"(PerHit)면 타수만큼 개별 롤, 아니면 스킬당 1회. × Stacks.
                //   확률은 기댓값: 인스턴스 틱 × p (StatusChance 참조).
                int hits = se.PerHit ? Math.Max(1, skill.GetAtkCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened)) : 1;
                int count = hits * Math.Max(1, se.Stacks);
                double p = StatusChance(ally, se.Chance);
                if (p <= 0) continue;
                for (int k = 0; k < count; k++)
                    target.ActiveDots.Add(new SiegeDot
                    {
                        Type = se.Type,
                        TickDamage = tick * p,
                        RemainingTurns = dur,
                        SourceName = ally.Source.Character.Name,
                        Probability = p,
                    });
            }
        }

        /// <summary>DoT 1틱 피해(확률 반영 전) — 시전자 공격력 × 비율(화상 80%·출혈 60%), 중독은 대상 최대HP 비례(공격력 상한). 공성 감쇄(시전자 공격속성) 적용.</summary>
        private static double DotTickDamage(CharacterBattleState ally, SiegeEnemyState target, StatusEffectType type,
            double? customAtkRatio = null, double? customMaxHpRatio = null, double? customAtkCap = null)
        {
            var baseEffect = StatusEffectDb.Get(type);
            if (baseEffect == null) return 0;
            double atkRatio = (customAtkRatio ?? baseEffect.AtkRatio) / 100.0;
            // 대상 최대HP 비례 DoT(중독 6%·동영 극독 30%) — 시전자 공격력 AtkCap% 상한. 보스는 HP가 커서 대개 상한 적용.
            //   중독만 매 턴 DoT. 빙결·마력역류·사슬피해의 최대HP%는 피격/공격 시 발동이라 여기서 제외.
            double maxHpRatio = type == StatusEffectType.Poison
                ? (customMaxHpRatio ?? baseEffect.TargetMaxHpRatio) / 100.0 : 0;
            if (atkRatio <= 0 && maxHpRatio <= 0) return 0;

            double elemReduction = ally.Source.Character.AttackType == AttackType.Magic
                ? target.Source.MagicReduction
                : target.Source.PhysicalReduction;
            double raw = ally.FinalAtk * atkRatio;
            if (maxHpRatio > 0)
            {
                double hpDmg = target.MaxHp * maxHpRatio;
                double cap = (customAtkCap ?? baseEffect.AtkCap) / 100.0;
                if (cap > 0) hpDmg = Math.Min(hpDmg, ally.FinalAtk * cap);
                raw += hpDmg;
            }
            return raw * Math.Max(0, 1 - elemReduction / 100.0);
        }

        /// <summary>상태이상이 걸릴 확률 = 툴팁 확률 × (1 + 효과 적용 확률/100) × (1 + 효과 적중/100), 0~1.
        ///   효과 적용 확률(주술사 4세트 +10)은 스킬의 n% 상태이상 확률을 1.1배로 올린다(2026-11-25 유저 확인 — 효과 적중과 다른 옵션, 곱연산).
        ///   반지의 상태이상에도 같은 배수를 적용했다(유저 설명은 "캐릭터 스킬의 cc기" — 반지 적용 여부는 미확인).
        ///   효과 적중 항은 기존 모델 그대로(적 효과 저항 0 가정 — 공성 적 효저 데이터 없음). 미확정: 실제로는 효저를 상쇄하는 값일 수 있다.</summary>
        private static double StatusChance(CharacterBattleState ally, double chance)
        {
            if (chance <= 0) return 0;
            double effHit = (ally.DisplayStats?.Eff_Hit ?? 0) + ally.Effects.GetTotalBuffs().Eff_Hit;
            double effAcc = ally.DisplayStats?.Eff_Acc ?? 0;
            return Math.Clamp(chance / 100.0 * (1 + effAcc / 100.0) * (1 + effHit / 100.0), 0, 1);
        }

        /// <summary>상태이상 반지 — 기본 공격 시 확률로 적에게 상태이상(Accessory.GetNormalAttackStatusEffects: 본 반지 + 세공 칸).
        ///   공성 적은 보스 취급이라 행동불가·즉사는 무효 → 지속 피해형(출혈·화상·중독)만 등록한다. 틱 피해는 착용자 공격력 기준(스킬 DoT와 같은 식),
        ///   부여마다 새 인스턴스(기댓값: 틱 × 확률). 실명(공포의 반지)·버프 해제·턴 감소 반지는 미모델.</summary>
        private void RegisterRingStatusToEnemy(CharacterBattleState ally, SiegeEnemyState target)
        {
            var acc = ally.Source.Equipment?.Accessory;
            if (acc == null || string.IsNullOrEmpty(acc.RingName) && string.IsNullOrEmpty(acc.SubOption)) return;
            foreach (var e in acc.GetNormalAttackStatusEffects())
            {
                if (e.StatusType != StatusEffectType.Bleeding && e.StatusType != StatusEffectType.Burn && e.StatusType != StatusEffectType.Poison) continue;
                double tick = DotTickDamage(ally, target, e.StatusType);
                double p = StatusChance(ally, e.Chance);
                if (tick <= 0 || p <= 0) continue;
                target.ActiveDots.Add(new SiegeDot
                {
                    Type = e.StatusType,
                    TickDamage = tick * p,
                    RemainingTurns = Math.Max(1, e.Duration),
                    SourceName = ally.Source.Character.Name,
                    Probability = p,
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
            // 보호막 지속턴 경과 (소진 전이라도 만료되면 소멸 — 받피감도 함께 소멸)
            if (enemy.ShieldTurns > 0 && --enemy.ShieldTurns <= 0) { enemy.Shield = 0; enemy.ShieldDmgRdc = 0; }
            if (enemy.LinkTurns > 0 && --enemy.LinkTurns <= 0) enemy.LinkDmgRdc = 0;   // 링크 지속턴 경과
            // 적 진영 버프(챈슬러 분쇄 → 스파이크 치확/치피) 지속턴 경과. 챈슬러가 재시전하면 갱신됨.
            if (enemy.EnemyBuffTurns > 0 && --enemy.EnemyBuffTurns <= 0) enemy.EnemyBuff = null;
            // 피해 면역(화 R3 룩) 지속턴 경과 — 이 적의 행동마다 1턴 차감(per-action). 룩이 재시전하면 갱신됨.
            if (enemy.ImmunityTurns > 0) enemy.ImmunityTurns--;

            // DoT 데미지 + 잔여 턴 감소 (피해 면역 / 보스 무효화 중엔 무효).
            //   델론즈 무효화는 DoT도 막지만 횟수는 차감하지 않음(직격만 차감) → 여기선 무효 처리만.
            bool immune = enemy.ImmunityTurns > 0 || enemy.NullifyHitsRemaining > 0;
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
                    _leak?.OnDot(dot.SourceName, dotName, scoredTick, 0);
                    state.TurnLogs.Add(new BattleTurnLog
                    {
                        Turn = state.CurrentTurn, ActorName = dot.SourceName, IsAlly = true,
                        ActionType = ActionType.DoTDamage, SkillName = dotName, DamageDealt = dot.TickDamage,
                        Description = $"[DoT] {dotName} → {enemy.Source.Name}: {dot.TickDamage:N0}",
                    });
                }
                else if (_leak != null && dot.TickDamage > 0)
                    _leak.OnDot(dot.SourceName, StatusEffectDb.Get(dot.Type)?.Name ?? dot.Type.ToString(), 0, dot.TickDamage);
                dot.RemainingTurns--;
            }
            enemy.ActiveDots.RemoveAll(d => d.RemainingTurns <= 0);
        }

        #endregion

        #region 시간 / 쿨다운

        /// <summary>행동 소요시간(초): config 값(기본 평타 1.85 / 아군 1스킬 3.3·2스킬 4.25 / 적 1스킬 3.1·2스킬 4.0 — 2026-11 수요일 녹화 게임 시계 실측).</summary>
        private double GetActionDuration(Skill skill, bool enemy = false)
        {
            var s = _activeState;
            if (skill == null) return s?.BasicAttackSeconds ?? 2.0;   // 기본공격 (config.BasicAttackSeconds)
            if (skill.ActionSeconds is double fixedSec) return fixedSec;   // 스킬별 실측값이 있으면 그 값
            return skill.SkillType switch
            {
                SkillType.Normal or SkillType.Normal2 => s?.BasicAttackSeconds ?? 2.0,
                SkillType.Skill1 => enemy ? s?.EnemySkill1Seconds ?? 3.0 : s?.AllySkill1Seconds ?? 3.0,   // 1스킬
                _ => enemy ? s?.EnemySkill2Seconds ?? 4.0 : s?.AllySkill2Seconds ?? 4.0,                  // 2스킬(컷신) 등
            };
        }

        /// <summary>
        /// 평타 발동형 쿨감 적용 (예: 라이언 평타 "자신과 공격력 최고 아군 스킬 쿨 9초 감소").
        /// 평타 스킬의 Effects 중 Buff.Cooldown_Reduction>0을 찾아 대상(Self / SelfAndHighestAtkAlly)의 쿨 감소.
        /// </summary>
        private void ApplyBasicAttackCdReduction(SiegeBattleState state, CharacterBattleState ally, Skill normal)
        {
            var lvl = normal?.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
            if (lvl?.Effects == null) return;
            foreach (var e in lvl.Effects)
            {
                double cdr = e.Buff?.Cooldown_Reduction ?? 0;
                if (cdr <= 0) continue;

                var targets = new List<CharacterBattleState> { ally };   // 항상 자신 포함
                if (e.Target == EffectTarget.SelfAndHighestAtkAlly || e.TargetSelector == TargetSelector.HighestAtkAlly
                    || e.Target == EffectTarget.Party)
                {
                    // "자신과 공격력 최고 아군" = 자신 + 자신 제외 공격력 최고 아군 (순위 기준은 OrderByAtkRank 참조)
                    //   config.HighestAtkAllyOrder 지정 시 그 순위 우선(유저 빌드 재현: 리나 공격세트 → 쿨감 수령).
                    var top = OrderByAtkRank(state.AllyStates.Where(a => !a.IsDead && a != ally), state, forCdr: true)
                        .FirstOrDefault();
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
        private void AdvanceTime(SiegeBattleState state, double seconds, string why = null)
        {
            if (seconds <= 0) return;
            state.TimeLedger?.Add((state.CurrentTurn, seconds, why ?? "?", state.ElapsedSeconds + seconds));
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

        /// <summary>살아있는 아군이 전부 생명력 100%인지. 회복 스킬 게이팅 판정용.</summary>
        private static bool AllAlliesFullHp(SiegeBattleState state)
            => state.AllyStates.Where(a => !a.IsDead && a.MaxHp > 0).All(a => a.CurrentHp >= a.MaxHp);

        /// <summary>순수 회복 스킬인지 (회복 출력 있고 직접 피해 없음 — 리나 행진가 등).
        /// 전원 풀피 시 게임에선 발동되지 않고 다음 예약 스킬이 나간다.</summary>
        private static bool IsHealSkill(Skill skill, bool enh)
        {
            var ld = skill.GetLevelData(enh);
            if (ld == null) return false;
            bool heals = ld.HealHpRatio > 0 || ld.HealAtkRatio > 0 || ld.HealDefRatio > 0;
            bool damages = ld.Ratio > 0 || ld.DefRatio > 0 || ld.HpRatio > 0 || ld.TargetMaxHpRatio > 0;
            return heals && !damages;
        }

        /// <summary>순수 파티버프 셋업 스킬인지 (데미지 0 + 파티/자기 Buff 효과). 청소시간·장비강화·따뜻한울림 등.
        /// 풀버프 정렬에서 딜러 핵보다 먼저 시전할 대상. 데미지 스킬(핵)은 Ratio>0이라 제외.</summary>
        private static bool IsPartyBuffSetup(Skill skill, bool enh)
        {
            var lvl = skill.GetLevelData(enh);
            if (lvl == null || lvl.Ratio > 0 || lvl.Effects == null) return false;
            return lvl.Effects.Any(e => e.Type == SkillEffectType.Buff
                && (e.Target == EffectTarget.Party || e.Target == EffectTarget.Self
                    || e.Target == EffectTarget.SelfAndHighestAtkAlly));
        }

        private Skill PickAllySkill(CharacterBattleState ally, SiegeBattleState state)
        {
            // 쿨 충족 스킬 중 기본 쿨타임이 긴 핵심 스킬 우선 (버퍼의 버프 스킬이 쿨이 길어 우선 시전됨).
            // 동률은 SkillType 높은 순(궁→…→1). 평타 제외.
            // 단 어떤 아군이 60%HP 미만이면 힐 스킬(HealHpRatio>0) 우선 — 실측 리나가 행진가로 팀을 살림.
            bool enh = ally.Source.IsSkillEnhanced;
            int tr = ally.Source.TranscendLevel;
            var ready = ally.Source.Character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .Where(s => !s.IsAwakenOnly || ally.Source.IsAwakened)   // 각성 전용 스킬은 각성 상태에서만
                .Where(s => ally.IsSkillReady(s.SkillType))
                .Where(s => PassesBuffGate(state, ally, s))
                .ToList();
            if (ready == null || ready.Count == 0) return null;
            // 전원 풀피면 회복 스킬은 발동되지 않고 다음 예약(여기선 다음 우선) 스킬이 나간다(게임 규칙).
            if (AllAlliesFullHp(state))
            {
                ready = ready.Where(s => !IsHealSkill(s, enh)).ToList();
                if (ready.Count == 0) return null;
            }
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
            _curCastAllyBuffTargets.Clear();   // 이 시전이 부여한 아군 버프 수령자 누적(스킬 순서 [대상] 표기용)
            _curCastDebuffTargets.Clear();     // 디버프 대상(적) 누적
            _curCastDispelTargets.Clear();     // 버프해제 대상(적) 누적
            int tc = System.Math.Max(1, skill.GetTargetCount(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened));
            var targets = PickTargets(state, tc, ally);
            if (targets.Count == 0) return;
            // 툴팁 순서: 적 대상 효과(디버프·턴감소·버프해제)를 피해 前에 적용 → 같은 스킬 피해가 증폭/면역관통/보호막관통.
            ApplySkillEffects(state, ally, skill, targets, preDamage: true);
            foreach (var target in targets)
            {
                bool pen = skill.PiercesImmunity(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened);   // 레벨 or 초월 관통
                if (tc <= 1)
                    ApplySingleTargetHit(state, ally, target, skill, isSkill: true, pen);   // 약점→최저HP / 미발동→무작위(기댓값)
                else
                    ApplyDamage(state, ally, target, CalcDamageToEnemy(ally, target, skill, state), skill.Name, isSkill: true, penetrate: pen, hitCount: HitCountOf(ally, skill));
                RegisterDotToEnemy(state, ally, target, skill);   // 스킬의 DoT(화상·출혈 등) 등록
                MaybeEnemyCounter(state, target, skill.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened)?.AtkCount ?? 1);
            }
            ProcessAttackStacks(state, ally, isSkill: true);   // 공격 발동형 스택(타카 EagleClaw) — 스킬 발동 시 1회
            ProcessAttackBuffs(state, ally, isSkill: true);    // 공격 발동형 아군 버프(지크 물공증) — 스킬 발동 시 1회
            ProcessTriggeredFixedDamage(state, ally, isSkill: true);   // N회 공격마다 추가 피해(세인·여포·발리스타 등)
            CountAttackForBuffExtend(state, ally);   // 공격 N회마다 자신의 턴제 버프 +1턴(쥬리)
            ApplySkillEffects(state, ally, skill, targets, preDamage: false);   // 아군 버프·아군 디버프해제 (피해 後)
            if (_leak != null) LeakSample(state, skill);   // 피해 뒤에 걸린 버프·디버프도 "최고 상태" 기준에 반영
            ApplySkillHeal(state, ally, skill);   // 스킬 직접 회복(HealHpRatio, 예: 리나 행진가 전체회복 24%)
            ApplyPartyOnSkillCastPassives(state, ally);   // 스킬 시전 트리거(나타 보호막·미호 흡혈)
            state.AllyRotationCursor = (allyIdx + 1) % state.AllyStates.Count;   // 다음 자동 스킬턴은 다음 아군부터

            // 시전 소요시간 경과: 방금 시전한 스킬 자신의 쿨도 "시전 시작 시점부터" 카운트 → 쿨 set 後 AdvanceTime.
            //   2026-10 월요일 연습전투 실측: 나타 혼천릉파(R1 0턴→R3 16턴)·미호 교만(8턴→24턴)이 인게임에선 시전됐으나
            //   "완료 시점부터" 모델은 둘 다 쿨 3초 부족(2스킬 시전 4초분) → 시작 시점 모델로 아군 20스텝·적 13회 턴 전부 일치.
            //   (적 스킬 쿨은 별도 경로 — 기존 "완료 시점부터" 유지, 적 타이밍은 실측과 일치 확인.)
            double cd = skill.GetCooldown(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened);
            if (!state.OwnCooldownFromCastStart)   // 실측 기본: 시전 완료 후부터 자기 쿨 카운트 → 시간 먼저 흘리고 쿨 set
                AdvanceTime(state, GetActionDuration(skill), $"[아군스킬] {ally.Source.Character.Name} {skill.Name}");
            if (cd > 0) ally.SkillCooldowns[skill.SkillType] = cd + state.AllyCooldownMargin;   // 안전 여유(기본 0)
            // [실행가능성] 방금 시전 → 이 스킬은 쿨 중(준비 전). 다시 0 도달 시점까지 pending(∞) 마킹.
            if (_recordFeasibility && cd > 0) _readySince[(allyIdx, skill.SkillType)] = double.PositiveInfinity;
            if (state.OwnCooldownFromCastStart)
                AdvanceTime(state, GetActionDuration(skill), $"[아군스킬] {ally.Source.Character.Name} {skill.Name}");   // 스킬 소요시간만큼 전체 쿨다운 감소(자기 쿨 포함 = 시전 시작부터 카운트)

            // 지정 스킬 쿨타임 초기화 (파스칼 어둠의 문 → 파괴의 거인). 자기 쿨 set 後 적용 → 다음 스킬턴 즉시 준비.
            var resetTarget = skill.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened)?.ResetsCooldownOf;
            if (resetTarget.HasValue)
            {
                ally.SkillCooldowns[resetTarget.Value] = 0;
                if (_recordFeasibility) _readySince[(allyIdx, resetTarget.Value)] = state.ElapsedSeconds;
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "쿨초기화", 0,
                    $"{skill.Name} → {resetTarget.Value} 쿨타임 초기화 (즉시 준비)");
            }
            // [쿨추적] 시전 시점 경과초 + 이 스킬 쿨 설정값 (게임 실측과 쿨회복 속도 대조용)
            Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "쿨", 0,
                $"{skill.Name} 시전 — 쿨 {cd:F0}초 설정 (경과 {state.ElapsedSeconds:F0}초)");

            // [실험] 스킬턴은 효과 tick하지 않음 — 함수 설계(TickAllyAfterAction XML doc: "n턴 지속=기본공격 n회").
            //   스킬턴 tick 시 메인딜러 셋업 버프(따뜻한울림/청소)가 버스트 전 조기만료됨.
            // TickAllyAfterAction(state, ally);
        }

        /// <summary>이번 아군 스킬턴에 가능한 행동 후보(살아있고 CC 아닌 아군 × 준비된 비평타 스킬) + Hold. (빔서치용)</summary>
        private List<RotationDecision> CollectAllyChoices(SiegeBattleState state)
        {
            var choices = new List<RotationDecision>();
            bool allFull = AllAlliesFullHp(state);   // 전원 풀피면 회복 스킬은 후보에서 제외(게임 규칙)
            for (int ai = 0; ai < state.AllyStates.Count; ai++)
            {
                var a = state.AllyStates[ai];
                if (a.IsDead || a.Effects.HasActionBlockingCC()) continue;
                foreach (var s in a.Source.Character.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (s.SkillType == SkillType.Normal || s.SkillType == SkillType.Normal2) continue;
                    if (s.IsAwakenOnly && !a.Source.IsAwakened) continue;   // 각성 전용 스킬은 각성 상태에서만
                    if (!a.IsSkillReady(s.SkillType)) continue;
                    if (!PassesBuffGate(state, a, s)) continue;
                    if (allFull && IsHealSkill(s, a.Source.IsSkillEnhanced)) continue;   // 풀피 → 회복 스킬 미발동
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
            A(b.Dmg_Dealt_1to3, "1-3인기"); A(b.Dmg_Dealt_1, "1인기"); A(b.Dmg_Dealt_3, "3인기"); A(b.Dmg_Dealt_4to5, "4-5인기");
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
                _curCastAllyBuffTargets.AddRange(targets.Select(t => t.Source.Character.Name));   // 스킬 순서 [대상] 표기용
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
                        MaxDuration = d,
                        BuffValues = b.Clone(),
                    });
                    // 보호막(Shield_HpRatio) → 흡수 풀 생성 (대상 최대HP 비례). 더 큰 값으로만 갱신.
                    if (b.Shield_HpRatio > 0)
                    {
                        double shield = t.MaxHp * b.Shield_HpRatio / 100.0;
                        if (shield > t.Shield) { t.Shield = shield; t.ShieldTurns = Math.Max(t.ShieldTurns, d); }
                    }
                    // 보호막(Shield_AtkRatio) → 시전자 공격력 비례 (예: 샤오 정기흡공 모든 아군 물공 160%[2턴]). 더 큰 값으로만 갱신.
                    if (b.Shield_AtkRatio > 0)
                    {
                        double shield = ally.FinalAtk * b.Shield_AtkRatio / 100.0;
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
                _curCastDebuffTargets.AddRange(targets.Select(en => en.Source.Name));   // 스킬 순서 [디버프 대상] 표기용
                if (targets.Count > 0)
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"디버프 [{string.Join(",", targets.Select(en => en.Source.Name))}]: {s} [{d}턴]");
            }

            // 중첩형 디버프 (아일린 "각 공격마다 방어력 감소 8% 1중첩[최대 4중첩]").
            //   타수(PerHit)마다 StacksPerTrigger씩 쌓여 MaxStacks 상한. StackDebuff 카테고리 = 상시·턴제와 별개 묶음(합산).
            //   중첩 풀은 (시전자, StackKey)별 — 같은 키를 쓰는 다른 스킬과 공유. 라운드가 바뀌면(새 적) 자연 소멸.
            //   근사: 스킬 피해 後에 일괄 적용(같은 스킬의 뒤쪽 타격이 앞 타격 중첩의 이득을 보는 부분은 미반영 — 보수적).
            void ApplyStackDebuff(SkillEffect e)
            {
                int hits = e.PerHit ? System.Math.Max(1, skill.GetAtkCount(enh, ally.Source.TranscendLevel, ally.Source.IsAwakened)) : 1;
                int add = hits * System.Math.Max(1, e.StacksPerTrigger);
                string key = e.StackKey ?? skill.Name;
                var targets = e.Target == EffectTarget.AllEnemies ? state.Enemies : hitEnemies;
                int dur = e.Duration > 0 ? e.Duration : 99;
                foreach (var en in targets)
                {
                    string skey = $"siege_stackdebuff:{ally.PartyIndex}:{key}:R{state.CurrentRound}:{en.Position}";
                    ally.CurrentStacks.TryGetValue(skey, out int st);
                    int ns = System.Math.Min(st + add, e.MaxStacks);
                    ally.CurrentStacks[skey] = ns;
                    AddEnemyDebuff(en, ScaleDebuff(e.Debuff, ns), dur, skey, EffectCategory.StackDebuff);
                }
                _curCastDebuffTargets.AddRange(targets.Select(en => en.Source.Name));
                if (targets.Count > 0)
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"중첩 디버프 [{string.Join(",", targets.Select(en => en.Source.Name))}] +{add}중첩(최대 {e.MaxStacks}): {SummarizeDebuff(e.Debuff)}/중첩");
            }

            // 초월 보너스 (대상수 변경 등). 6초월 비스킷 장비강화 = 버프 대상 2명 등.
            var tr = skill.GetTranscendBonus(ally.Source.TranscendLevel);
            int? buffTgtOverride = tr?.TargetCountOverride;   // HighestAtkAlly 버프 대상 수 오버라이드

            // 턴제 버프 감소(미호 교만 등): 적의 턴제 버프 지속턴을 깎는다.
            //   ★피해 면역(ImmunityTurns)도 「[2턴]」 턴제 버프이므로 감소 대상 → 미호 교만으로 해제 가능.
            //     (비스킷 리프어택은 "버프 해제"라 피해 면역 불가 — 그쪽만 DispelEnemyShield에서 면역 제외.)
            //   교만은 피해 前(PreDamage) 적용 → 면역을 깎은 뒤 자기 피해가 들어간다.
            void ReduceEnemyTimedBuffs(int turns)
            {
                if (turns <= 0) return;
                foreach (var en in state.Enemies)
                {
                    if (en.ImmunityTurns > 0)
                    {
                        int b = en.ImmunityTurns;
                        en.ImmunityTurns = System.Math.Max(0, en.ImmunityTurns - turns);
                        Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                            $"{en.Source.Name} 피해 면역 {b}→{en.ImmunityTurns}턴 (턴감소 {turns})");
                    }
                    if (en.EnemyBuffTurns > 0)
                    {
                        int b = en.EnemyBuffTurns;
                        en.EnemyBuffTurns = System.Math.Max(0, en.EnemyBuffTurns - turns);
                        if (en.EnemyBuffTurns <= 0) en.EnemyBuff = null;
                        Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                            $"{en.Source.Name} 버프 {b}→{en.EnemyBuffTurns}턴 (턴감소 {turns})");
                    }
                }
            }

            // 적 버프해제: ①보호막(루디 등) 제거 → 이후 피해 점수 집계, ②적 진영 버프(챈슬러→스파이크 치확/치피) 제거.
            //   보호막은 피격 적(hitEnemies)에서, 적 진영 버프는 버프 보유 적 전체에서 제거(MostBuffsEnemy 근사 — 보통 1명=스파이크).
            //   토요일 필수 기믹: 챈슬러 버프를 매번 해제해야 스파이크 치명타 학살을 막아 아군 생존·점수 유지.
            void DispelEnemyShield()
            {
                // 보호막 버프 활성(ShieldTurns>0)이면 해제 — 대체HP(루디 9.3만)는 첫 타에 깨져 Shield=0이 돼도
                //   받피감 버프는 ShieldTurns로 남으므로 Shield>0가 아닌 ShieldTurns>0로 판별해야 -10%까지 제거된다.
                //   링크도 버프 → 맞은 적 각각의 링크 해제 (실측: 리프어택 1회로 루디·룩·챈슬러 링크 + 루디 보호막 전부 제거).
                foreach (var en in hitEnemies.Where(x => x.ShieldTurns > 0 || x.Shield > 0 || x.LinkTurns > 0))
                {
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"{en.Source.Name}{(en.Shield > 0 || en.ShieldTurns > 0 ? $" 보호막{(en.Shield > 0 ? $" {en.Shield:N0}" : "")}" : "")}{(en.LinkTurns > 0 ? $" 링크(받피감{en.LinkDmgRdc:0}%)" : "")} 버프해제로 제거");
                    en.Shield = 0; en.ShieldTurns = 0; en.ShieldDmgRdc = 0;
                    en.LinkTurns = 0; en.LinkDmgRdc = 0;
                    _curCastDispelTargets.Add(en.Source.Name);
                }
                foreach (var en in state.Enemies.Where(x => x.HasEnemyBuff))
                {
                    Log(state, actor, true, ActionType.DebuffApplied, skill.Name, 0,
                        $"{en.Source.Name} 적 버프 해제 ({SummarizeBuff(en.EnemyBuff)})");
                    en.EnemyBuff = null; en.EnemyBuffTurns = 0;
                    _curCastDispelTargets.Add(en.Source.Name);
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
                        case SkillEffectType.Debuff when e.Debuff != null && e.MaxStacks > 0:
                            ApplyStackDebuff(e);
                            break;
                        case SkillEffectType.Debuff when e.Debuff != null:
                            ApplyDebuff(e.Debuff, e.Duration, e.Target);
                            break;
                        case SkillEffectType.BuffTurnReduction
                            when e.TurnReduction > 0 && (e.Target == EffectTarget.Enemy || e.Target == EffectTarget.AllEnemies):
                            ReduceEnemyTimedBuffs(e.TurnReduction);   // 면역 포함 턴제 버프 감소 (미호 교만은 면역 해제 가능)
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
                            // 아군 디버프 해제: 후열셀렉터면 후열만(미호 초월2), 아니면 Party=전체 아군(헬레니아 등).
                            //   보스의 공감 등 디버프 제거 → 딜 회복.
                            var cleanseTargets = e.TargetSelector == TargetSelector.BackRowAlly
                                ? state.AllyStates.Where(x => !x.IsDead && x.Source.IsBackPosition)
                                : state.AllyStates.Where(x => !x.IsDead);
                            foreach (var a in cleanseTargets)
                            {
                                int removed = a.Effects.RemoveDebuffs(e.DispelDebuffCount);
                                if (removed > 0)
                                    Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                        $"{a.Source.Character.Name} 디버프 {removed}개 해제");
                            }
                            break;
                        case SkillEffectType.Revive when e.ReviveHpPercent > 0:
                        {
                            // 사망 아군 TargetCount명을 생명력 ReviveHpPercent%로 부활 (먼저 죽은 순).
                            int reviveCount = e.TargetCount > 0 ? e.TargetCount : 1;
                            // 영멸 보유 아군은 부활 불가.
                            var dead = state.AllyStates
                                .Where(x => x.IsDead && x.Effects.GetStatusEffectsOfType(StatusEffectType.Annihilation).Count == 0)
                                .Take(reviveCount).ToList();
                            foreach (var a in dead)
                            {
                                a.IsDead = false;
                                a.CurrentHp = System.Math.Max(1, a.MaxHp * e.ReviveHpPercent / 100.0);
                                Log(state, actor, true, ActionType.BuffApplied, skill.Name, 0,
                                    $"{a.Source.Character.Name} 부활 (생명력 {e.ReviveHpPercent:0.##}%)");
                            }
                            break;
                        }
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
            HandleEffects(skill.GetEffectiveEffects(ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel, ally.Source.IsAwakened));
            // 초월 Effects는 위 GetEffectiveEffects에 병합됨. 초월 전용 필드(tr.Debuff/tr.PartyBuff)만 별도 처리.
            //   ※ 이 둘은 아직 DB에서 쓰인다(에반·유진호 PartyBuff, 샤오 Debuff). Effects로 옮기면
            //     GetEffectiveEffects가 같은 (Target,Type)의 base 효과와 병합해 지속시간이 달라지므로 별도 유지.
            if (tr != null)
            {
                if (!preDamage && tr.Debuff != null) ApplyDebuff(tr.Debuff, 0, EffectTarget.Enemy);
                if (!preDamage && tr.PartyBuff != null) ApplyBuff(EffectTarget.Party, null, 0, tr.PartyBuff, 0);
            }
        }

        /// <summary>[실험] 공격력 최고 아군 버프 게이트(config.HighestAtkBuffGate) — 이 스킬에 HighestAtkAlly 버프가 있으면
        /// 지금 시전할 때 뽑힐 수령자를 미리 계산해 게이트에 묻는다. 게이트 미지정·해당 버프 없음이면 항상 통과.</summary>
        private bool PassesBuffGate(SiegeBattleState state, CharacterBattleState a, Skill sk)
        {
            if (_buffGate == null || sk == null) return true;
            var eff = sk.GetLevelData(a.Source.IsSkillEnhanced)?.Effects?.FirstOrDefault(e =>
                e.Type == SkillEffectType.Buff && e.Buff != null && e.TargetSelector == TargetSelector.HighestAtkAlly);
            if (eff == null) return true;
            int tc = sk.GetTranscendBonus(a.Source.TranscendLevel)?.TargetCountOverride ?? eff.TargetCount;
            var targets = ResolveAllyBuffTargets(state, a, eff.Target, eff.TargetSelector, tc)
                .Select(t => t.Source.Character.Name).ToList();
            return _buffGate(state.CurrentRound, a.Source.Character.Name, sk.Name, targets);
        }

        /// <summary>버프 대상 아군 선정 (Self / Party / 공격력 상위 N명).</summary>
        private List<CharacterBattleState> ResolveAllyBuffTargets(SiegeBattleState state, CharacterBattleState caster,
            EffectTarget target, TargetSelector? selector, int tgtCount)
        {
            var alive = state.AllyStates.Where(a => !a.IsDead).ToList();
            if (target == EffectTarget.Self || target == EffectTarget.SingleAlly)
                return new List<CharacterBattleState> { caster };
            if (selector == TargetSelector.HighestAtkAlly)
                // 기본: 실효딜 가중치 상위 N명(딜러 우선) — 공%로 atk만 뻥튀기한 캐릭이 버프를 가로채지 않는 낙관 모델.
                // InGameBuffTargeting: 시전 순간 현재 공격력 상위 N명(인게임 규칙 — 가로채기·순위 역전도 그대로 발생).
                //   config.HighestAtkAllyOrder(쿨감 전용이 아닐 때) 지정 시 그 순위 우선.
                return OrderByAtkRank(alive, state, forCdr: false)
                    .Take(System.Math.Max(1, tgtCount)).ToList();
            if (selector == TargetSelector.BackRowAlly)   // 후열 아군 한정 (지크 물공증, 미호 디버프해제와 동일 패턴)
                return alive.Where(a => a.Source.IsBackPosition).ToList();
            if (selector == TargetSelector.FrontRowAlly)  // 전열 아군 한정
                return alive.Where(a => !a.Source.IsBackPosition).ToList();
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
            var lvl = passive?.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
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

                // 트리거 판정: 스킬 발동 시 즉시 1회. 평타는 TriggerCount회(=2)마다 1회. 카운터는 라운드별.
                //   ★스킬이 트리거하면 평타 누적 카운터도 리셋한다(게임 규칙). 즉 평타→스킬(부여)→평타→평타(부여)이지,
                //     평타→스킬(부여)→평타(부여)가 아니다. (구버그: 스킬이 카운터를 안 건드려 스킬 직후 평타1회로 재부여 → 과다부여.)
                string ckey = $"siege_stackcnt:{ally.PartyIndex}:{effect.StatusType}:R{state.CurrentRound}";
                if (!isSkill)
                {
                    ally.StackTriggerCounters.TryGetValue(ckey, out int cnt);
                    cnt++;
                    if (cnt < System.Math.Max(1, effect.TriggerCount)) { ally.StackTriggerCounters[ckey] = cnt; continue; }
                    ally.StackTriggerCounters[ckey] = 0;
                }
                else
                {
                    ally.StackTriggerCounters[ckey] = 0;   // 스킬 트리거 → 평타 누적 리셋
                }

                // 발동 시 적군 3명에게 스택 부여 (보스 포함). 적별로 누적.
                foreach (var en in PickTargets(state, 3))
                {
                    string skey = $"siege_stack:{ally.PartyIndex}:{effect.StatusType}:R{state.CurrentRound}:{en.Position}";
                    ally.CurrentStacks.TryGetValue(skey, out int st);
                    int newStacks = System.Math.Min(st + effect.StacksPerTrigger, effect.MaxStacks);
                    ally.CurrentStacks[skey] = newStacks;

                    var scaled = ScaleDebuff(perStack, newStacks);
                    // 타카 EagleClaw 등 스택형 취약은 스택 시 상시 적용 → PassiveDebuff(턴제 레이첼 취약과 다른 카테고리=Add).
                    // 타카 EagleClaw 등 스택형 = 중첩형 카테고리(상시·턴제와 별도 합산 — 2026-11-17 유저 확인. 이전엔 상시로 분류).
                    AddEnemyDebuff(en, scaled, 99, skey, EffectCategory.StackDebuff);   // 같은 key 갱신(스택 증가분 반영)
                    Log(state, ally.Source.Character.Name, true, ActionType.DebuffApplied,
                        StatusEffectDb.Get(effect.StatusType)?.Name ?? effect.StatusType.ToString(), 0,
                        $"{en.Source.Name} {effect.StatusType} {newStacks}스택: {SummarizeDebuff(scaled)}");
                }
            }
        }

        /// <summary>아군 공격 발동형 버프 처리 (예: 지크 "나만 믿어" — 모든 공격 2회마다 후열 아군 물공증[2초월 3턴]).
        /// 트리거: 평타 TriggerCount회마다 1회 OR 스킬 발동 시 즉시 1회. 대상은 effect.TargetSelector(BackRowAlly 등).
        /// ProcessAttackStacks(적 디버프)의 아군 버프 버전 — 적 디버프는 거기서, 아군 버프는 여기서 처리.</summary>
        private void ProcessAttackBuffs(SiegeBattleState state, CharacterBattleState ally, bool isSkill)
        {
            var passive = ally.Source.Character.Passive;
            var lvl = passive?.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
            if (lvl?.Effects == null) return;

            for (int ei = 0; ei < lvl.Effects.Count; ei++)
            {
                var effect = lvl.Effects[ei];
                if (effect.ApplyMode != ApplyMode.Triggered) continue;
                if (effect.Type != PersistentEffectType.Buff || effect.Buff == null) continue;
                if (effect.Target != EffectTarget.Party && effect.Target != EffectTarget.Self) continue;
                bool matches = effect.TriggerCondition switch
                {
                    TriggerCondition.AllAttack => true,
                    TriggerCondition.SkillOnly => isSkill,
                    TriggerCondition.NormalOnly => !isSkill,
                    _ => false,
                };
                if (!matches) continue;

                // 트리거 판정: 스킬 즉시 1회, 평타는 TriggerCount회마다 1회. 카운터는 평타에만, 라운드별 리셋.
                if (!isSkill)
                {
                    string ckey = $"siege_buffcnt:{ally.PartyIndex}:{ei}:R{state.CurrentRound}";
                    ally.StackTriggerCounters.TryGetValue(ckey, out int cnt);
                    cnt++;
                    if (cnt < System.Math.Max(1, effect.TriggerCount)) { ally.StackTriggerCounters[ckey] = cnt; continue; }
                    ally.StackTriggerCounters[ckey] = 0;
                }

                int dur = effect.Duration > 0 ? effect.Duration : 99;

                // 중첩형 자기 버프 (쥬다스 "모든 공격 2회 발동 시 물공 증가 1중첩[최대 15]", 중첩당 값 × 현재 중첩).
                if (effect.IsPerStack && effect.MaxStacks > 0 && effect.Target == EffectTarget.Self)
                {
                    string sk = $"siege_buffstack:{ally.PartyIndex}:{ei}";
                    ally.CurrentStacks.TryGetValue(sk, out int st);
                    int ns = System.Math.Min(st + System.Math.Max(1, effect.StacksPerTrigger), effect.MaxStacks);
                    ally.CurrentStacks[sk] = ns;
                    ApplyStackBuff(ally, sk, effect.Buff, ns, effect.Duration);
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, passive.Name, 0,
                        $"{ally.Source.Character.Name} {SummarizeBuff(effect.Buff)} {ns}중첩");
                    continue;
                }

                // 공격 발동형 보호막 (쥬다스 "모든 공격 2회 발동 시 시전자 물공 110% 보호막[2턴]").
                //   스킬 시전 트리거(SkillOnly: 나타·소교)는 ApplyPartyOnSkillCastPassives가 처리 → 여기선 제외(이중 부여 방지).
                if (effect.Buff.Shield_AtkRatio > 0 && effect.TriggerCondition != TriggerCondition.SkillOnly)
                {
                    double shield = ally.FinalAtk * effect.Buff.Shield_AtkRatio / 100.0;
                    int sd = effect.Duration > 0 ? effect.Duration : 2;
                    foreach (var t in ResolveAllyBuffTargets(state, ally, effect.Target, effect.TargetSelector, state.AllyStates.Count))
                        if (shield > t.Shield) { t.Shield = shield; t.ShieldTurns = Math.Max(t.ShieldTurns, sd); }
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "보호막", 0,
                        $"{ally.Source.Character.Name} 보호막 {shield:N0} [{sd}턴]");
                    continue;
                }

                var targets = ResolveAllyBuffTargets(state, ally, effect.Target, effect.TargetSelector, state.AllyStates.Count);
                foreach (var t in targets)
                {
                    string id = $"siege_atkbuff:{ally.PartyIndex}:{ei}:{t.PartyIndex}";
                    t.Effects.AddEffect(new BattleEffect
                    {
                        Id = id, SourceName = id,
                        Category = EffectCategory.ActiveSelfBuff,
                        Target = EffectTarget.Self,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = dur,
                        MaxDuration = dur,
                        BuffValues = effect.Buff.Clone(),
                    });
                }
                if (targets.Count > 0)
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, passive.Name, 0,
                        $"{string.Join(",", targets.Select(t => t.Source.Character.Name))} {SummarizeBuff(effect.Buff)} [{dur}턴]");
            }
        }

        /// <summary>중첩형 버프 적용 (StackBuff 카테고리 — 상시·턴제와 별개 묶음). 같은 키 갱신.</summary>
        private static void ApplyStackBuff(CharacterBattleState ally, string key, BuffSet perStack, int stacks, int duration)
        {
            var total = new BuffSet();
            for (int i = 0; i < System.Math.Max(0, stacks); i++) total.Add(perStack);
            ally.Effects.RemoveBySource(key);
            ally.Effects.AddEffect(new BattleEffect
            {
                Id = key, SourceName = key,
                Category = EffectCategory.StackBuff,
                Target = EffectTarget.Self,
                MergeStrategy = MergeStrategy.MaxMerge,
                IsPermanent = duration <= 0,
                RemainingTurns = duration > 0 ? duration : 99,
                BuffValues = total,
            });
        }

        /// <summary>전투 시작 시 추가 중첩 — InitialStacksPerAllyClass(자신 제외 아군 중 해당 직업군 수).
        ///   쥬다스: 아군 만능형 1명당 물공 중첩 +1. 키는 ProcessAttackBuffs와 동일(이후 트리거 중첩이 이어서 쌓임).</summary>
        private void ApplyInitialPassiveStacks(SiegeBattleState state)
        {
            foreach (var ally in state.AllyStates)
            {
                var passive = ally.Source.Character.Passive;
                var lvl = passive?.GetLevelData(ally.Source.IsSkillEnhanced, ally.Source.IsAwakened);
                if (lvl?.Effects == null) continue;
                for (int ei = 0; ei < lvl.Effects.Count; ei++)
                {
                    var e = lvl.Effects[ei];
                    if (string.IsNullOrEmpty(e.InitialStacksPerAllyClass) || !e.IsPerStack || e.MaxStacks <= 0 || e.Buff == null) continue;
                    int n = state.AllyStates.Count(a => a != ally && a.Source.Character.Type == e.InitialStacksPerAllyClass);
                    if (n <= 0) continue;
                    string sk = $"siege_buffstack:{ally.PartyIndex}:{ei}";
                    int ns = System.Math.Min(n, e.MaxStacks);
                    ally.CurrentStacks[sk] = ns;
                    ApplyStackBuff(ally, sk, e.Buff, ns, e.Duration);
                    Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, passive.Name, 0,
                        $"전투 시작: 아군 {e.InitialStacksPerAllyClass} {n}명 → {SummarizeBuff(e.Buff)} {ns}중첩");
                }
            }
        }

        /// <summary>트리거 스킬 시전 효과 선택 — base·초월에 같은 TriggerOn이 있으면 마지막(높은 초월)이 이김(최종값 컨벤션).</summary>
        private TriggeredSkillCast GetTriggeredCast(CharacterBattleState owner, TriggerCondition on)
        {
            var passive = owner.Source.Character.Passive;
            if (passive == null) return null;
            return GetPassiveEffects(passive, owner)
                .Where(e => e.Type == PersistentEffectType.TriggeredSkillCast && e.TriggeredSkillCast != null
                            && e.TriggeredSkillCast.TriggerOn == on)
                .Select(e => e.TriggeredSkillCast).LastOrDefault();
        }

        /// <summary>아군 스킬 시전 N회마다 발동하는 패시브 시전 (동영 「황천의 동행자」: 아군 스킬 2회 → 모든 적 마법 50% + 극독 등).
        ///   카운터는 패시브 보유자별·전투 누적(라운드 무관). 보유자가 사망 중이면 누적·발동 안 함.
        ///   트리거 시전 자체는 "스킬 발동"으로 세지 않는다(연쇄 없음).</summary>
        private void FireAllySkillTriggeredCasts(SiegeBattleState state)
        {
            foreach (var owner in state.AllyStates.Where(a => !a.IsDead).ToList())
            {
                var tsc = GetTriggeredCast(owner, TriggerCondition.AllySkill);
                if (tsc == null) continue;
                string ck = $"siege_tsc_allyskill:{owner.PartyIndex}";
                owner.StackTriggerCounters.TryGetValue(ck, out int c);
                c++;
                if (c < System.Math.Max(1, tsc.TriggerCount)) { owner.StackTriggerCounters[ck] = c; continue; }
                owner.StackTriggerCounters[ck] = 0;
                ExecuteTriggeredCast(state, owner, tsc);
            }
        }

        /// <summary>아군 부활 직후 연계: TriggeredSkillCast(OnRevival) 시전 + 부활 시 피해 면역(DamageNullification, OnRevival).
        ///   아일린 각성 「불꽃의 화신」: 부활 시 [청천벽력·진] 발동(본래 스킬과 별개) + 모든 피해 면역[3턴].</summary>
        private void OnAllyRevived(SiegeBattleState state, CharacterBattleState ally)
        {
            // 부활하면 부여돼 있던 버프는 유지되지 않고 사라지고, 디버프·상태이상도 같이 풀린다(2026-11-24 유저 확인)
            //   — 지속 턴이 있는 효과 전부 + 보호막·도발 제거. (권능은 죽지 않고 버티는 것이라 그대로 남는다. 상시 패시브·펫 버프는 유지.)
            var lost = ally.Effects.RemoveAllTimed();
            ally.Shield = 0;
            ally.TauntTurnsRemaining = 0;
            if (lost.Count > 0)
            {
                int nBuff = lost.Count(e => e.IsStatBuff && !e.IsStatDebuff);
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, "부활", 0,
                    $"부활 → {ally.Source.Character.Name} 버프 {nBuff}개·디버프/상태이상 {lost.Count - nBuff}개 소멸 ({string.Join(", ", lost.Select(e => e.SourceName).Distinct())})");
            }
            var passive = ally.Source.Character.Passive;
            if (passive == null) return;
            foreach (var e in GetPassiveEffects(passive, ally))
            {
                if (e.Type != PersistentEffectType.DamageNullification || e.DamageNullification == null) continue;
                if (e.ApplyMode != ApplyMode.Triggered || e.TriggerCondition != TriggerCondition.OnRevival) continue;
                var n = e.DamageNullification;
                if (n.Duration > 0) ally.NullifyTurnsRemaining = Math.Max(ally.NullifyTurnsRemaining, n.Duration);
                if (n.HitCount > 0) ally.NullifyHitsRemaining = Math.Max(ally.NullifyHitsRemaining, n.HitCount);
                ally.NullifyType = n.Type;
                Log(state, ally.Source.Character.Name, true, ActionType.BuffApplied, passive.Name, 0,
                    $"부활 → {ally.Source.Character.Name} {n.Type} 피해 면역 [{(n.Duration > 0 ? n.Duration + "턴" : n.HitCount + "회")}]");
            }
            var tsc = GetTriggeredCast(ally, TriggerCondition.OnRevival);
            if (tsc != null) ExecuteTriggeredCast(state, ally, tsc);
        }

        /// <summary>패시브 트리거 시전 실행 — 쿨타임·시간 경과·스킬 시전 카운트 없이 피해 + 부가효과만.
        ///   피해 계산은 일반 스킬과 동일(DamageCalculator, 대상수 감쇄 포함), 공격력은 보유자 현재 버프 기준.</summary>
        private void ExecuteTriggeredCast(SiegeBattleState state, CharacterBattleState owner, TriggeredSkillCast tsc)
        {
            if (owner.IsDead) return;
            var lvl = new SkillLevelData
            {
                Ratio = tsc.Ratio,
                AtkCount = System.Math.Max(1, tsc.AtkCount),
                TargetCount = System.Math.Max(1, tsc.TargetCount),
                Effects = tsc.Effects,
                IgnoresTurnDamageImmunity = tsc.IgnoresTurnDamageImmunity,
            };
            var sk = new Skill
            {
                Name = tsc.Name ?? (owner.Source.Character.Passive?.Name + "(발동)"),
                SkillType = SkillType.Skill1,
                LevelData = new Dictionary<int, SkillLevelData> { [0] = lvl },
            };
            var targets = PickTargets(state, lvl.TargetCount, owner);
            if (targets.Count == 0) return;
            Log(state, owner.Source.Character.Name, true, ActionType.SkillAttack, sk.Name, 0,
                $"패시브 발동 → 적{targets.Count} 공격력 {tsc.Ratio:0}% {lvl.AtkCount}회");
            ApplySkillEffects(state, owner, sk, targets, preDamage: true);
            foreach (var t in targets)
            {
                if (tsc.Ratio > 0)
                    ApplyDamage(state, owner, t, CalcDamageToEnemy(owner, t, sk, state), sk.Name, isSkill: true,
                        penetrate: tsc.IgnoresTurnDamageImmunity, hitCount: HitCountOf(owner, sk));
                RegisterDotToEnemy(state, owner, t, sk);
            }
            ApplySkillEffects(state, owner, sk, targets, preDamage: false);
        }

        /// <summary>디버프를 스택 수만큼 합산 (스택당 효과).</summary>
        private static DebuffSet ScaleDebuff(DebuffSet b, int stacks)
        {
            var r = new DebuffSet();
            for (int i = 0; i < System.Math.Max(1, stacks); i++) r.Add(b);
            return r;
        }

        /// <summary>적 1명에 디버프 누적(중복 방지: 같은 Id 갱신).
        /// category로 상시(PassiveDebuff)/턴제(ActiveDebuff)/펫(PetDebuff)을 구분 — GetTotalDebuffs가 카테고리 내 MaxMerge,
        /// 카테고리 간 Add. 같은 종류·같은 타입은 최댓값만(풍연 방깎24[턴제]+레이첼 방깎36[턴제]=36), 타입 다르면 합산
        /// (비스킷 방깎24[상시]+레이첼 방깎36[턴제]=60, 타카 취약[상시]+레이첼 취약[턴제]=합산).</summary>
        private void AddEnemyDebuff(SiegeEnemyState enemy, DebuffSet d, int dur, string id,
            EffectCategory category = EffectCategory.ActiveDebuff)
        {
            enemy.Effects.RemoveBySource(id);
            enemy.Effects.AddEffect(new BattleEffect
            {
                Id = id,
                SourceName = id,
                Category = category,
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
            CharacterBattleState ally, Skill sk, bool executed, string reason, double cdRem, double slack,
            bool gated = false, bool isAuto = false)
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
                IsAuto = isAuto,
                FallbackReason = reason,
                CooldownRemaining = cdRem,
                Slack = slack,
                CooldownGated = gated,
            });
        }

        /// <summary>자동 폴백(플랜 범위 밖) 시전을 실행가능성 로그에 기록 + 시전 대상(버프/디버프/해제) 표기.</summary>
        private void RecordAutoFeasStep(SiegeBattleState state, int stepIdx, int heroIdx, Skill sk)
        {
            if (!_recordFeasibility) return;
            var a = state.AllyStates[heroIdx];
            RecordFeasStep(state, stepIdx, new RotationDecision { HeroIndex = heroIdx, Skill = sk.SkillType },
                a, sk, executed: true, reason: null, cdRem: 0, slack: 0, gated: false, isAuto: true);
            if (_feasLog.Count > 0)
            {
                if (_curCastAllyBuffTargets.Count > 0) _feasLog[^1].BuffTargets = _curCastAllyBuffTargets.Distinct().ToList();
                if (_curCastDebuffTargets.Count > 0) _feasLog[^1].DebuffTargets = _curCastDebuffTargets.Distinct().ToList();
                if (_curCastDispelTargets.Count > 0) _feasLog[^1].DispelTargets = _curCastDispelTargets.Distinct().ToList();
            }
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
                CoopJudgments = state.CoopJudgments, CoopExpectedProcs = state.CoopExpectedProcs,
                CoopExpectedDamage = state.CoopExpectedDamage, CoopStdDev = Math.Sqrt(state.CoopVariance),
                AlliesAlive = state.AllyStates.Count(a => !a.IsDead),
                TurnLogs = state.TurnLogs,
                TimeLedger = state.TimeLedger,
                DecisionPoints = state.DecisionPoints,
                Feasibility = _feasLog,
                Deaths = _deaths,
            };
            // 랭킹용 점수: 실제 딜 − 사망 페널티×(전투 종료 시 사망 아군 수). 보고 점수(TotalScore)는 불변.
            int deadCount = state.AllyStates.Count(a => a.IsDead);
            result.RankScore = result.TotalScore - _allyDeathPenalty * deadCount;
            if (_leak != null)
            {
                result.Leak = _leak.Finish(state.TotalScore,
                    state.AllyStates.GroupBy(a => a.Source.Character.Name).ToDictionary(g => g.Key, g => g.Sum(a => a.TotalDamageDealt)));
                result.Leak.AddFeasibility(_feasLog);
            }
            foreach (var ally in state.AllyStates)
            {
                result.CharacterResults.Add(new SiegeCharacterResult
                {
                    CharacterName = ally.Source.Character.Name,
                    PartyIndex = ally.PartyIndex,
                    TotalDamage = ally.TotalDamageDealt,
                    DamageShare = state.TotalScore > 0 ? ally.TotalDamageDealt / state.TotalScore * 100 : 0,
                    Died = ally.IsDead,
                    MinHpPct = ally.MinHpPctAfterHit,
                    LethalSaves = ally.LethalSaves,
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
    /// <summary>[진단] EvaluateHitBreakdown 결과.</summary>
    public class HitBreakdown
    {
        public DamageCalculator.DamageInput Input { get; set; }
        public DamageCalculator.DamageResult Detail { get; set; }   // 치명+약점 조합의 세부 계수
        public double ReductionMult { get; set; }
        public double TargetDef { get; set; }
        public string DealerBuffs { get; set; }
        public string TargetDebuffs { get; set; }
        public double Expected { get; set; }                         // 치명·약점 확률 기댓값(시뮬 실제 사용값)
        public Dictionary<(bool Crit, bool Weak), double> Combos { get; } = new();
        public List<string> Logs { get; set; }                       // 적용된 버프/디버프 로그(감사용)
    }
}

using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.Optimizer;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>공성전 탐색 입력.</summary>
    public class SiegeOptimizerConfig
    {
        public List<BattleCharacter> FixedMembers { get; set; } = new();   // 필수 포함 영웅(버퍼·면역·디버퍼 등)
        public List<BattleCharacter> Candidates { get; set; } = new();     // 나머지 후보 풀
        public Stage SiegeStage { get; set; }
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public int PetEnhance { get; set; }   // 펫 스킬강화 (0=미강화, 1~3)
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }
        public int MaxTurns { get; set; } = 70;
        public int PartySize { get; set; } = 5;

        // 장비 미지정 영웅에게 장비 옵티마이저로 최적 장비를 자동 장착할지 (R3 보스 기준 1회)
        public bool AutoEquip { get; set; } = true;

        // 전용무기 조율 4슬롯(전설 가정)을 영웅별로 풀시뮬 점수로 탐색할지. (메인옵/세공은 현 모델 미포함)
        //   기본 OFF(기존 동작 불변). ON이면 AutoEquip 기어 선택 후 영웅별 조율을 그리디 탐색.
        public bool SearchExclusiveWeapon { get; set; } = false;

        // 저점-우선 기어: 치확/약확을 (파티 버스트 버프 포함) 100%까지만 채우고 나머지는 치피/공%로.
        // 버프-정렬 로테이션 전제(가 가정)라 정렬 안 되면 평균 점수 소폭 하락 가능 → 기본 OFF, Phase 2와 함께 평가.
        public bool FloorFirstGear { get; set; } = false;

        // 최종 best config(진형·자리·기어)에 스킬 로테이션 빔서치를 적용해 로테이션 최적 점수·플랜 산출
        public bool OptimizeRotation { get; set; } = true;
        public int RotationBeamWidth { get; set; } = 10;
        // 70턴 시즈의 아군 스킬턴(라운드 연쇄 포함 ~20개)을 전부 덮어야 막판 스킬턴까지 빔이 최적화한다.
        //   빔은 결정점 소진 시 조기종료(!extended)하므로 이 값은 상한일 뿐 — 넉넉히 둬도 낭비 없음.
        //   (이전 18은 막판 ~2 스킬턴을 자동 폴백=버프우선에 넘겨 끝물 공격스킬 채택을 놓쳤음)
        public int RotationMaxDepth { get; set; } = 28;

        // [반격 평균 탐색] >1이면 빔/채택 평가를 반격 ON(보스 정의 25%) N시드 평균으로 — 로테 선택이 반격 RNG에
        //   과적합되지 않게 평균점으로 고른다. 금요일(제이브)만 의미. 1이면 기존(빔=반격 OFF, 채택=ON 단일시드).
        //   플랜 구조(Dps)는 OFF로 결정론 유지하고 점수만 ON N시드 평균(RotationBeamSearch).
        public int CounterattackSearchSeeds { get; set; } = 1;

        // [허수아비 딜러 기어] ON이면 딜러(공격/마법/만능형)의 세트·메인부옵·전용조율을 "허수아비 단타 DPS"
        //   (스쿼드 풀버프 + 보스 HP0=잃은체력 최대)로 전수 탐색. 풀시뮬 좌표상승 프록시의 그리디·uptime 노이즈를
        //   제거하고 R3 넉 레짐을 직접 최대화. 탱·서포터는 기존 FullScore 유지. OFF면 전부 기존 동작(무회귀).
        public bool DummyGearForDealers { get; set; }

        // 진형 단일 강제 (실측 비교용). null이면 전 진형 탐색.
        public string ForcedFormation { get; set; }

        // [공성 진형 규칙] 기본진형(3후열) 포함 여부. 기본 OFF — 비스킷 장비강화 버프가 2명에게만 들어가 메인딜러는
        //   최대 2명이므로, 3후열은 약한 3번째 딜러에 후열공%를 분산해 비효율(유저 확정). 탐색은 밸런스(2)·보호(1)만.
        //   비교·디버그용으로만 true.
        public bool IncludeBasicFormation { get; set; }

        // [후열 = dummy DPS 상위 N딜러] 기본 ON. 후열마스크 전수탐색이 cadence 아티팩트(후열 누구냐가 속공/턴순서를
        //   바꿔 점수 ±흔들림)로 최고딜러를 전열에 버리는 오선택을 막는다. 도발은 열 무관(라이언 후열서도 탱킹)이라
        //   최고딜러는 후열에 둬야 공%버프까지 받아 무조건 이득. 후열 = 허수아비 단타 상위 requiredBack명으로 강제.
        //   false면 기존 전수 마스크 탐색(비교·디버그용).
        public bool DummyBackRow { get; set; } = true;

        // 후열 강제 (실측 비교용). null이면 전 마스크 탐색. 캐릭터 이름 리스트.
        public List<string> ForcedBackRow { get; set; }

        // 캐릭터별 강제 세트(실측 비교용). Key=Character.Id, Value=세트명("수문장","성기사" 등).
        // EquipCandidates에서 해당 캐릭터는 이 세트 후보만 사용 — 메인/부옵은 기존 로직대로 최적.
        public Dictionary<int, string> ForcedSetByCharId { get; set; } = new();

        // 캐릭터별 강제 메인옵션(실측 비교용). Key=Character.Id, Value=(무기메인, 방어구메인).
        // 지정 시 EquipCandidates는 해당 메인옵만 후보로 사용.
        public Dictionary<int, (string WeaponMain, string ArmorMain)> ForcedMainByCharId { get; set; } = new();

        // 기어 선정(세트·메인·부옵·전용) 평가에 쓸 정렬 로테이션. null이면 자동 로테(기존). 등록 시 기어가
        //   "정렬된 버스트" 기준으로 선정돼 버프-캡 스탯(약확/치확)이 치피/공%로 자동 재배분됨(메인옵 포함).
        //   HeroIndex = FixedMembers+Candidates 순서 기준.
        public List<RotationDecision> GearEvalRotation { get; set; }

        // 기어 선정 평가 진형 (null이면 "기본 진형"). 최종이 밸런스면 밸런스로 평가해야 메인옵 등 정합.
        public string GearEvalFormation { get; set; }
        // 기어 선정 평가 시 후열로 둘 영웅 이름(밸런스 등 진형 보너스 정합용). null이면 전부 전열.
        public List<string> GearEvalBackRow { get; set; }

        // 아군 사망 1명당 랭킹 페널티(딜). 보고 점수(TotalScore)는 불변, RankScore에만 반영. 0이면 페널티 없음.
        //   작게(20만): 막판 희생 같은 최적 공격빌드는 허용(>1등), 다수 조기사망 파괴적 전멸만 차단(안전망).
        //   큰 값(예 2M)은 0사망을 강요해 최고스펙 점수를 1등 미만으로 깎으므로 지양.
        public double AllyDeathPenalty { get; set; } = 200_000;

        // 생존(권능) 반지 후처리 사용 여부. 기본 ON(기존 동작). OFF면 ApplySurvivalRings 미실행 —
        //   6초월 실측 프로필처럼 권능반지를 끼지 않는 계정 가정 탐색에 사용.
        public bool EnableSurvivalRings { get; set; } = true;

        // 좌표상승(기어↔로테/진형) 2패스 탐색. 기본 OFF(기존 1패스 동작·무회귀).
        //   ON이면 1패스(기어=프록시 레짐)로 진형·자리·빔로테를 찾은 뒤, 그 최종 레짐을 GearEval*에 피드백해
        //   기어를 재최적화하고 다시 탐색 → 기어 선정 레짐 = 최종 평가 레짐 정합(2단계 근사 함정 교정).
        //   딜러 메인옵을 사람이 ForcedMain으로 박지 않아도 시뮬이 스스로 찾게 하는 게 목적.
        //   조합탐색(Candidates) 시엔 GearEvalRotation HeroIndex 정합이 깨질 수 있어 고정팀(Candidates 없음)에서만 적용.
        public bool CoordinateAscentGear { get; set; }

        // 외부 시드 로테이션 — 빔 교차수분(crossPlans)에 추가로 투입할 (HeroIndex,Skill) 플랜들. 예: 다른 초월
        //   프로필(6↔12초월)의 빔 결과. 빔이 이 config에서 못 찾은 더 높은 로테를 교차평가로 회수(무회귀).
        //   HeroIndex는 동일 팀 순서 기준 — 고정팀에서만 의미.
        public List<List<RotationDecision>> SeedRotations { get; set; }
    }

    /// <summary>공성전 탐색 결과 (최고딜 팀 + 진형).</summary>
    public class SiegeOptimizerResult
    {
        public List<BattleCharacter> BestParty { get; set; } = new();
        public string BestFormation { get; set; }
        public double BestScore { get; set; }              // 보고용 실제 딜(TotalScore)
        public double BestRankScore { get; set; }          // 선택용 랭킹 점수(생존 페널티 반영). 비교는 이 값으로.
        public SiegeBattleResult BestResult { get; set; }
        public List<string> BestBackRow { get; set; } = new();   // 최적 후열 배치 영웅 이름
        internal int BestMask { get; set; }                       // 최적 후열 배치 비트마스크 (재적용용)
        internal List<Accessory> RingedAcc { get; set; }          // 생존반지 적용 후 장신구 구성(후보별 격리 평가용, team 공유라 원복 후 재적용)
        public List<RotationDecision> BestRotationPlan { get; set; } = new();   // 빔서치 최적 스킬 로테이션
        public double AutoRotationScore { get; set; }             // 자동 로테이션 점수 (빔서치 전, 비교용)
        public int EvaluatedCount { get; set; }     // 평가한 (조합 × 진형 × 자리) 수
        public List<string> GearLog { get; set; } = new();   // 자동 장착된 영웅별 메인옵/부옵 값 로그
        public List<SiegeEvalEntry> EvalLog { get; set; } = new();   // 탐색 중 평가한 (팀×진형)별 점수
    }

    /// <summary>탐색 평가 1건 (팀 조합 × 진형 → 점수).</summary>
    public class SiegeEvalEntry
    {
        public string Formation { get; set; }
        public List<string> Party { get; set; } = new();
        public List<string> BackRow { get; set; } = new();   // 후열 배치 영웅
        public double Score { get; set; }
        public Dictionary<int, double> RoundScore { get; set; } = new();
    }

    /// <summary>
    /// 공성전 탐색: 영웅 조합 전수탐색 × 아군 진형 3종(기본/밸런스/보호) 순회 × 공성전 시뮬 → 최고딜 팀.
    /// 후보가 5~8명이라 C(N,5)×3 수준이라 전수탐색이 현실적([[simulator-optimizer-plan]]).
    /// 장비는 입력 BattleCharacter 그대로 사용 — 장비 옵티마이저(영웅×진형 캐싱) 연동은 후속.
    /// </summary>
    public class SiegeOptimizer
    {
        private readonly SiegeBattleSimulator _sim = new(GearCompareSeed);   // 고정 시드 — 배치/진형 탐색 재현성

        // 공성전 아군 순회 진형 (공격 진형 제외 — 기획)
        private static readonly string[] Formations = { "기본 진형", "밸런스 진형", "보호 진형" };

        /// <summary>
        /// 공성 탐색 진입점. 좌표상승 OFF면 1패스(OptimizeOnce) 그대로. ON이면 2패스:
        ///   1패스 결과(최종 진형·자리·빔로테)를 GearEval*에 피드백해 기어를 최종 레짐에서 재최적화하고 재탐색,
        ///   무회귀(IsBetterPick)로 더 나은 패스 채택. 공유 BattleCharacter 객체라 1패스 기어를 스냅샷해 복원한다.
        /// </summary>
        public SiegeOptimizerResult Optimize(SiegeOptimizerConfig config)
        {
            // 좌표상승 적용 대상 = 시작 시점 장비 미지정(자동장착) 영웅. 조합탐색 시엔 로테 HeroIndex 정합이 깨져 제외.
            bool canAscend = config.CoordinateAscentGear && config.AutoEquip
                             && (config.Candidates == null || config.Candidates.Count == 0);
            var autoTargets = canAscend
                ? config.FixedMembers.Concat(config.Candidates ?? new List<BattleCharacter>())
                    .Where(bc => bc != null && bc.Equipment == null).ToList()
                : new List<BattleCharacter>();

            var r1 = OptimizeOnce(config);
            if (!canAscend || r1?.BestParty == null || autoTargets.Count == 0) return r1;

            // 1패스 기어 스냅샷(공유객체 보존) + 최종 레짐을 기어평가에 피드백.
            var p1Gear = autoTargets.Select(t => t.Equipment).ToList();
            config.GearEvalFormation = r1.BestFormation;
            config.GearEvalBackRow = r1.BestBackRow != null ? new List<string>(r1.BestBackRow) : null;
            config.GearEvalRotation = (r1.BestRotationPlan != null && r1.BestRotationPlan.Count > 0)
                ? new List<RotationDecision>(r1.BestRotationPlan) : null;

            // 재장착 유도 후 2패스.
            foreach (var t in autoTargets) t.Equipment = null;
            var r2 = OptimizeOnce(config);

            // 무회귀: 2패스가 더 나으면 채택, 아니면 1패스 기어·자리를 공유객체에 복원해 반환.
            if (r2?.BestParty != null && IsBetterPick(r2.BestScore, r2.BestRankScore, r1.BestScore, r1.BestRankScore))
                return r2;
            for (int i = 0; i < autoTargets.Count; i++) autoTargets[i].Equipment = p1Gear[i];
            for (int i = 0; i < r1.BestParty.Count; i++)
                r1.BestParty[i].IsBackPosition = (r1.BestMask & (1 << i)) != 0;
            if (r1.RingedAcc != null)
                for (int i = 0; i < r1.RingedAcc.Count && i < r1.BestParty.Count; i++)
                    if (r1.BestParty[i].Equipment != null) r1.BestParty[i].Equipment.Accessory = r1.RingedAcc[i];
            return r1;
        }

        private SiegeOptimizerResult OptimizeOnce(SiegeOptimizerConfig config)
        {
            int remaining = config.PartySize - config.FixedMembers.Count;
            if (remaining < 0) remaining = 0;

            // 장비 자동 장착 (R3 보스 기준, 영웅별 1회) — 팀 탐색 전에 끝내 재사용
            var gearLog = config.AutoEquip ? EquipCandidates(config) : new List<string>();

            SiegeOptimizerResult best = null;
            // (B) 메인딜러 후열 휴리스틱 후보: 자동로테 점수가 가장 높은 "최고딜 캐릭이 후열인" config.
            //   2-stage 근사 보정용 — cleanse→버스트 시너지는 자동로테 점수에 안 잡혀 우승 마스크에서
            //   누락되기 쉬우므로, 빔서치 후보에 항상 함께 포함한다(공성전 한정).
            SiegeOptimizerResult bestDealerBack = null;
            // (B2) 위 (B)는 "1위 딜러"만 본다 — 밸런스(후열2)에서 1위가 이미 후열이면 (B)가 best와 같아져
            //   2위 딜러-후열 config가 빔평가에서 누락된다(목요일 라이언). 보완: 후열이 데미지 상위
            //   requiredBack명과 정확히 일치하는("상위 N딜러 전원 후열") config를 진형별로 추적해 빔 후보에
            //   추가 → 2위 딜러 후열도 공정 비교. Key=진형, Value=그 진형의 최고 자동로테 config.
            var bestDealerBackFull = new Dictionary<string, SiegeOptimizerResult>();
            int evaluated = 0;
            var evalLog = new List<SiegeEvalEntry>();

            foreach (var combo in Combinations(config.Candidates, remaining))
            {
                var team = new List<BattleCharacter>(config.FixedMembers);
                team.AddRange(combo);
                if (team.Count == 0) continue;

                int n = team.Count;

                // [후열 = dummy DPS 상위] 팀별 허수아비 단타 순위 1회 계산 → 진형별 top-requiredBack을 후열로 강제.
                int[] dummyRank = null;
                if (config.DummyBackRow && config.ForcedBackRow == null)
                {
                    var dsim = new SiegeBattleSimulator(GearCompareSeed);
                    var dcfg = new SiegeBattleConfig
                    {
                        AllyParty = team, FormationName = "밸런스 진형", SiegeStage = config.SiegeStage,
                        AllyPet = config.AllyPet, PetStar = config.PetStar, PetEnhance = config.PetEnhance,
                        PetOptionAtkRate = config.PetOptionAtkRate, PetOptionDefRate = config.PetOptionDefRate,
                        PetOptionHpRate = config.PetOptionHpRate, MaxTurns = config.MaxTurns,
                    };
                    var dps = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        double bd = -1;
                        foreach (var sk in team[i].Character?.Skills ?? Enumerable.Empty<Skill>())
                        {
                            if (sk.SkillType == SkillType.Normal || sk.SkillType == SkillType.Normal2) continue;
                            double d = dsim.EvaluateDummyNuke(dcfg, i, sk);
                            if (d > bd) bd = d;
                        }
                        dps[i] = bd;
                    }
                    dummyRank = Enumerable.Range(0, n).OrderByDescending(i => dps[i]).ToArray();
                }
                // 진형 강제 옵션 (실측 비교): null이면 탐색. 기본진형은 비스킷 2명 버프 한계로 제외(밸런스·보호만),
                //   IncludeBasicFormation=true면 기본까지 포함(비교·디버그용).
                var searchFormations = config.IncludeBasicFormation
                    ? Formations
                    : Formations.Where(f => f != "기본 진형").ToArray();
                var formationsToTry = string.IsNullOrEmpty(config.ForcedFormation)
                    ? searchFormations
                    : new[] { config.ForcedFormation };
                foreach (var formation in formationsToTry)
                {
                    // 진형별 후열 인원 고정(기본3·밸런스2·공격4·보호1). 후열공% × 인원 = 42 일정.
                    int requiredBack = GameDamageCalculator.Database.StatTable.FormationDb.Formations.TryGetValue(formation, out var fb)
                        ? System.Math.Min(fb.BackRowCount, n) : n;

                    // 후열 강제 옵션 (실측 비교): 캐릭 이름으로 후열 마스크 고정.
                    int? forcedMask = null;
                    if (config.ForcedBackRow != null)
                    {
                        int fm = 0;
                        for (int i = 0; i < n; i++)
                            if (config.ForcedBackRow.Contains(team[i].Character?.Name)) fm |= (1 << i);
                        forcedMask = fm;
                    }
                    else if (dummyRank != null)
                    {
                        // 후열 = 허수아비 단타 상위 requiredBack명 강제 (cadence 노이즈 무시, 도발 열무관).
                        int fm = 0;
                        for (int k = 0; k < requiredBack && k < dummyRank.Length; k++) fm |= (1 << dummyRank[k]);
                        forcedMask = fm;
                    }

                    // 자리(전/후열) 배치 탐색: 후열 인원이 정확히 requiredBack인 마스크만.
                    //   누구를 후열에 둘지가 핵심 변수 (보호진형 후열 1 → 메인딜러 1명).
                    //   이 (조합×진형)의 전 마스크 결과를 모아 메인딜러-후열(B2) 판정에 재사용한다.
                    var group = new List<(int Mask, SiegeBattleResult Result, List<string> BackRow)>();
                    for (int mask = 0; mask < (1 << n); mask++)
                    {
                        if (forcedMask.HasValue && mask != forcedMask.Value) continue;
                        if (System.Numerics.BitOperations.PopCount((uint)mask) != requiredBack) continue;
                        for (int i = 0; i < n; i++) team[i].IsBackPosition = (mask & (1 << i)) != 0;

                        var simConfig = new SiegeBattleConfig
                        {
                            AllyParty = team,
                            FormationName = formation,
                            SiegeStage = config.SiegeStage,
                            AllyPet = config.AllyPet,
                            PetStar = config.PetStar,
                            PetEnhance = config.PetEnhance,
                            PetOptionAtkRate = config.PetOptionAtkRate,
                            PetOptionDefRate = config.PetOptionDefRate,
                            PetOptionHpRate = config.PetOptionHpRate,
                            MaxTurns = config.MaxTurns,
                        };
                        // 매 마스크 새 시뮬 인스턴스 — 공유 인스턴스는 시뮬 간 상태(쿨/카운터 등) 누적 오염 위험
                        //   (같은 config가 자리탐색 경로에 따라 다른 자동로테 점수를 내던 문제 방지).
                        var result = new SiegeBattleSimulator(GearCompareSeed).Simulate(simConfig);
                        evaluated++;
                        var backRow = team.Where(c => c.IsBackPosition).Select(c => c.Character.Name).ToList();
                        evalLog.Add(new SiegeEvalEntry
                        {
                            Formation = formation,
                            Party = team.Select(c => c.Character.Name).ToList(),
                            BackRow = backRow,
                            Score = result.TotalScore,
                            RoundScore = new Dictionary<int, double>(result.RoundScore),
                        });
                        group.Add((mask, result, backRow));

                        if (best == null || IsBetterPick(result.TotalScore, result.RankScore, best.BestScore, best.BestRankScore))
                        {
                            best = new SiegeOptimizerResult
                            {
                                BestParty = team,
                                BestFormation = formation,
                                BestScore = result.TotalScore,
                                BestRankScore = result.RankScore,
                                BestResult = result,
                                BestBackRow = backRow,
                                BestMask = mask,
                            };
                        }

                        // (B) 데미지 1위 캐릭이 후열인 config (자동로테 최고점, 전역). 기존 동작 유지.
                        var topDealer = result.CharacterResults.Count > 0
                            ? result.CharacterResults.OrderByDescending(c => c.TotalDamage).First() : null;
                        if (topDealer != null && backRow.Contains(topDealer.CharacterName)
                            && (bestDealerBack == null || IsBetterPick(result.TotalScore, result.RankScore, bestDealerBack.BestScore, bestDealerBack.BestRankScore)))
                            bestDealerBack = new SiegeOptimizerResult
                            {
                                BestParty = team, BestFormation = formation, BestScore = result.TotalScore,
                                BestRankScore = result.RankScore,
                                BestResult = result, BestBackRow = backRow, BestMask = mask,
                            };
                    }

                    // (B2) 이 (조합×진형)의 "상위 N딜러 전원 후열" config를 빔 후보로 추가(진형별 최고점).
                    //   딜러 식별을 config별 자동로테 데미지로 하면 자기모순 — 자동로테는 딜러-후열 시너지를
                    //   과소평가하므로 정작 후열딜러 config에서 그 딜러가 저평가돼 상위에서 빠진다(목요일 라이언).
                    //   대신 이 그룹 내 캐릭별 '최대' 데미지(전열 등 최선 배치에서 찍은 값)로 딜러를 가린 뒤,
                    //   그 상위 N명이 전원 후열인 config를 찾는다. requiredBack==1이면 (B)와 동일 → 제외(중복 빔 방지).
                    if (requiredBack >= 2 && group.Count > 0)
                    {
                        var heroMax = new Dictionary<string, double>();
                        foreach (var g in group)
                            foreach (var cr in g.Result.CharacterResults)
                                heroMax[cr.CharacterName] = System.Math.Max(
                                    heroMax.TryGetValue(cr.CharacterName, out var v) ? v : 0, cr.TotalDamage);
                        // 딜러 식별 = 역할(공격형/마법형) 우선, 그 안에서 데미지. 데미지(heroMax)만 쓰면
                        //   전열 배치에서 후열한정 버프(지크 물공증) 못 받아 딜 낮은 딜러(금요일 라이언)가
                        //   상위에서 빠져 후열 후보에서 누락됨 → 라이언 후열(17.5M)을 못 찾던 버그.
                        bool IsDealerRole(string nm)
                        {
                            var ty = team.FirstOrDefault(x => x.Character?.Name == nm)?.Character?.Type;
                            return ty == "공격형" || ty == "마법형";
                        }
                        // 역할이 딜러여도 그 요일 딜이 미미하면(그룹 1위의 5% 미만) 유틸로 취급해 후열 강제 후보에서 제외.
                        //   (수요일 라이언: 공격형이지만 마법덱에선 쿨감·면역 유틸이라 딜 0.3% → 후열 자리 낭비.)
                        double maxDmgG = heroMax.Values.DefaultIfEmpty(0).Max();
                        bool IsActiveDealer(string nm) => IsDealerRole(nm) && heroMax.GetValueOrDefault(nm) >= maxDmgG * 0.05;
                        var carries = heroMax.OrderByDescending(kv => IsActiveDealer(kv.Key) ? 1 : 0)
                            .ThenByDescending(kv => kv.Value).Take(requiredBack)
                            .Select(kv => kv.Key).ToHashSet();
                        foreach (var (m, r, br) in group)
                            if (br.Count == carries.Count && br.All(carries.Contains)
                                && (!bestDealerBackFull.TryGetValue(formation, out var cf) || IsBetterPick(r.TotalScore, r.RankScore, cf.BestScore, cf.BestRankScore)))
                                bestDealerBackFull[formation] = new SiegeOptimizerResult
                                {
                                    BestParty = team, BestFormation = formation, BestScore = r.TotalScore,
                                    BestRankScore = r.RankScore,
                                    BestResult = r, BestBackRow = br, BestMask = m,
                                };
                    }
                }
            }

            if (best != null)
            {
                // 빔 후보들의 우승 로테 모음 — 생존반지 후처리(ApplySurvivalRings) 後 재평가에 재사용.
                var crossPlans = new List<List<RotationDecision>>();
                // 최종 best config(진형·자리·기어)에 스킬 로테이션 빔서치 → 로테이션 최적 점수·플랜.
                // 진형·자리·기어 탐색은 자동 로테이션 점수로 했으므로 2단계 근사. 단, 자동로테는
                // cleanse→버스트 시너지(메인딜러 후열 가치)를 과소평가 → 메인딜러-후열 후보(B)를
                // 빔서치 후보에 함께 넣고, 빔 점수가 더 높은 쪽을 채택해 근사 오류를 보정한다.
                if (config.OptimizeRotation)
                {
                    // 후보군: 자동로테 우승 + 메인딜러-후열(B) + 상위N딜러-전원후열(B2, 진형별).
                    //   (팀·진형·마스크)가 같은 config는 중복 빔서치 방지로 제외.
                    var beamCands = new List<SiegeOptimizerResult> { best };
                    void AddCand(SiegeOptimizerResult c)
                    {
                        if (c == null) return;
                        bool dup = beamCands.Any(b => ReferenceEquals(b.BestParty, c.BestParty)
                            && b.BestFormation == c.BestFormation && b.BestMask == c.BestMask);
                        if (!dup) beamCands.Add(c);
                    }
                    AddCand(bestDealerBack);
                    foreach (var full in bestDealerBackFull.Values) AddCand(full);

                    foreach (var cand in beamCands)
                    {
                        // team 객체가 탐색·다른 후보 빔으로 변형되므로 이 후보 자리 배치를 재적용 후 빔.
                        for (int i = 0; i < cand.BestParty.Count; i++)
                            cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                        cand.AutoRotationScore = cand.BestScore;
                        var beam = new RotationBeamSearch(GearCompareSeed).Search(
                            BuildSimConfig(config, cand.BestParty, cand.BestFormation),
                            config.RotationBeamWidth, config.RotationMaxDepth, config.CounterattackSearchSeeds);
                        // 빔은 반격 OFF(0%)로 로테를 골랐다 → 채택·비교 점수는 반격 ON(실전·단일시드)으로 재평가.
                        //   금요일(제이브)만 OFF≠ON; 그 외 보스는 반격 없어 동일(회귀 없음). 비교는 RankScore(생존 페널티).
                        //   raw 트랙(Plan)·생존 트랙(RankPlan) 둘 다 평가해 IsBetterPick(생존 우선)으로 채택.
                        var beamPlans = new List<List<RotationDecision>> { beam.Plan };
                        if (beam.RankPlan != null && !ReferenceEquals(beam.RankPlan, beam.Plan))
                            beamPlans.Add(beam.RankPlan);
                        foreach (var bp in beamPlans)
                        {
                            var beamOn = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, bp);
                            if (IsBetterPick(beamOn.TotalScore, beamOn.RankScore, cand.BestScore, cand.BestRankScore))
                            {
                                cand.BestScore = beamOn.TotalScore;
                                cand.BestRankScore = beamOn.RankScore;
                                cand.BestResult = beamOn;
                                cand.BestRotationPlan = bp;
                            }
                        }
                    }

                    // 교차수분: 각 후보의 빔 플랜을 다른 후보 config에도 평가해, 빔이 그 config에서 못 찾은
                    //   더 높은 로테(다른 자리/진형의 우승 로테)를 채택한다. 로테는 (HeroIndex,Skill) 리스트라
                    //   자리·진형 무관 → 교차적용 가능. (목요일: 전열 우승로테를 라이언-후열 자리에 적용하면
                    //   후열 자체 빔보다 높음 — 빔이 후열을 지역최적으로 저평가한 것을 교정.) 무회귀(더 높을 때만).
                    crossPlans = beamCands
                        .Where(c => c.BestRotationPlan != null && c.BestRotationPlan.Count > 0)
                        .Select(c => c.BestRotationPlan).ToList();
                    // 외부 시드 로테 주입(프로필 간 교차수분 등) — 빔이 못 찾은 더 높은 로테를 교차평가로 회수.
                    if (config.SeedRotations != null)
                        crossPlans.AddRange(config.SeedRotations.Where(p => p != null && p.Count > 0));
                    foreach (var cand in beamCands)
                    {
                        for (int i = 0; i < cand.BestParty.Count; i++)
                            cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                        foreach (var plan in crossPlans)
                        {
                            if (ReferenceEquals(plan, cand.BestRotationPlan)) continue;   // 자기 플랜은 이미 반영됨
                            var r = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, plan);
                            if (IsBetterPick(r.TotalScore, r.RankScore, cand.BestScore, cand.BestRankScore))
                            {
                                cand.BestScore = r.TotalScore;
                                cand.BestRankScore = r.RankScore;
                                cand.BestResult = r;
                                cand.BestRotationPlan = plan;
                            }
                        }
                    }
                    // 각 빔 후보에 생존반지+로테재최적화를 적용한 뒤 best 선정 — 딜러 후열 빌드가
                    //   "생존반지 전 점수"로 탈락하던 버그 수정. (라이언/타카 후열은 생존반지로 살리면 딜이
                    //   두 배인데, 1차빔은 사망 페널티로 RankScore가 낮아 기본진형에 밀려 후처리를 못 받았음.)
                    //   team 공유 객체라 cand마다 장신구를 백업→적용→평가→원복하고, best의 반지 구성은 RingedAcc에 저장.
                    if (config.AutoEquip && config.EnableSurvivalRings)
                    {
                        foreach (var cand in beamCands)
                        {
                            for (int i = 0; i < cand.BestParty.Count; i++)
                                cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                            var accBak = cand.BestParty.Select(p => p.Equipment?.Accessory).ToList();
                            if (ApplySurvivalRings(config, cand) && config.OptimizeRotation)
                                ReoptimizeRotationAfterRings(config, cand, crossPlans);
                            cand.RingedAcc = cand.BestParty.Select(p => p.Equipment?.Accessory).ToList();
                            for (int i = 0; i < accBak.Count; i++)
                                if (cand.BestParty[i].Equipment != null) cand.BestParty[i].Equipment.Accessory = accBak[i];
                        }
                    }
                    // 생존(RankScore) 최고 후보 채택, 동률이면 raw(보고 점수) 높은 쪽 — IsBetterPick과 동일 기준.
                    best = beamCands.OrderByDescending(c => c.BestRankScore).ThenByDescending(c => c.BestScore).First();
                }

                best.EvaluatedCount = evaluated;
                best.GearLog = gearLog;
                best.EvalLog = evalLog;
                // team은 공유·변형 객체이므로 최종 채택 config의 자리 배치 + 생존반지 구성을 마지막에 확정 재적용.
                for (int i = 0; i < best.BestParty.Count; i++)
                    best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
                if (best.RingedAcc != null)
                    for (int i = 0; i < best.RingedAcc.Count && i < best.BestParty.Count; i++)
                        if (best.BestParty[i].Equipment != null) best.BestParty[i].Equipment.Accessory = best.RingedAcc[i];

                // OptimizeRotation=false(빔 미사용)면 빔 후보 경로를 안 타므로 여기서 생존반지 후처리(기존 동작).
                if (config.AutoEquip && config.EnableSurvivalRings && !config.OptimizeRotation)
                    ApplySurvivalRings(config, best);
            }
            return best ?? new SiegeOptimizerResult { EvaluatedCount = 0, GearLog = gearLog, EvalLog = evalLog };
        }

        // 장비 후보(세트) 비교용 풀시뮬 고정 시드 — 후보 간 동일 RNG로 공정 비교.
        private const int GearCompareSeed = 777;

        /// <summary>선택 비교: 생존(RankScore=raw−사망페널티) 우선, 사실상 동률(±eps)일 때만 raw로 tie-break.
        ///   사망 빌드가 최종 채택되려면 사망당 20만 이상의 raw 우위가 필요 → 산발 사망 고점은 허용하되
        ///   "후반 줄사망" 빌드(예: 토요일 4사망 12.10M가 생존 빌드를 밀어내던 문제)는 걸러진다.
        ///   고점 라인 탐색 자체는 빔이 raw 트랙(Plan)과 생존 트랙(RankPlan)을 모두 유지하므로 가려지지 않는다.</summary>
        private static bool IsBetterPick(double total, double rank, double bestTotal, double bestRank)
        {
            const double eps = 1.0;   // 동률 간주 한계(딜)
            if (rank > bestRank + eps) return true;
            if (rank < bestRank - eps) return false;
            return total > bestTotal;   // rank 동률 → raw(보고 점수) 높은 쪽
        }

        /// <summary>
        /// 장비 미지정 영웅에게 최적 장비 장착. 영웅별로 허용 세트마다 후보(메인·부옵 프록시 최적)를 만들고,
        /// 그 세트 선택을 공성전 풀시뮬 팀 점수(고정 시드·기본 진형, 좌표상승)로 비교해 정한다.
        /// </summary>
        private List<string> EquipCandidates(SiegeOptimizerConfig config)
        {
            var log = new List<string>();
            var boss = ResolveBoss(config.SiegeStage);
            if (boss == null) return log;

            var optimizer = new EquipmentOptimizer { JointAccessory = config.CoordinateAscentGear };
            var targets = config.FixedMembers.Concat(config.Candidates).Where(bc => bc != null && bc.Equipment == null).ToList();
            var team = config.FixedMembers.Concat(config.Candidates).ToList();   // 풀시뮬 평가 팀

            // 1) 영웅별 세트 후보 생성 (메인·부옵은 프록시 데미지로 최적). 기준선 = 첫 후보.
            var cands = new Dictionary<BattleCharacter, List<(string Set, EquipmentLoadout Lo)>>();
            // 딜러별 풀파티 버스트 크리/약점 floor (SoloConfig엔 파티버프가 없으므로 풀팀에서 따로 계산해 주입)
            // FloorFirstGear OFF면 default(0,0) → 기존 동작 그대로(회귀 없음).
            (double Cri, double Wek) Floor(BattleCharacter bc) =>
                config.FloorFirstGear ? optimizer.PartyBuffCritWeak(team, team.IndexOf(bc), bc.Character.Type) : default;

            // 강제 옵션을 GearConstraints에 반영하는 헬퍼 — BuildSetCandidates와 OptimizeForSetFull에서 공통 사용.
            GearConstraints GcForChar(BattleCharacter bc)
            {
                var gc = GetGearConstraints(bc, config.SiegeStage);
                if (config.ForcedSetByCharId != null && bc.Character != null
                    && config.ForcedSetByCharId.TryGetValue(bc.Character.Id, out var fs)
                    && !string.IsNullOrEmpty(fs))
                {
                    gc = new GearConstraints
                    {
                        AllowedSets = new[] { fs },
                        WeaponMains = gc?.WeaponMains, ArmorMains = gc?.ArmorMains, SubOptions = gc?.SubOptions,
                    };
                }
                if (config.ForcedMainByCharId != null && bc.Character != null
                    && config.ForcedMainByCharId.TryGetValue(bc.Character.Id, out var fm))
                {
                    gc = new GearConstraints
                    {
                        AllowedSets = gc?.AllowedSets,
                        WeaponMains = string.IsNullOrEmpty(fm.WeaponMain) ? gc?.WeaponMains : new[] { fm.WeaponMain },
                        ArmorMains = string.IsNullOrEmpty(fm.ArmorMain) ? gc?.ArmorMains : new[] { fm.ArmorMain },
                        SubOptions = gc?.SubOptions,
                    };
                }
                return gc;
            }

            foreach (var bc in targets)
            {
                cands[bc] = optimizer.BuildSetCandidates(bc, SoloConfig(config, boss, bc), 0, GcForChar(bc), Floor(bc));
                if (cands[bc].Count > 0) bc.Equipment = cands[bc][0].Lo;
            }

            // 2) 좌표상승: 영웅별로 각 세트 후보를 풀시뮬 팀 점수(고정 시드·기본 진형)로 비교해 최적 세트 선택
            //   GearEvalRotation 등록 시 그 정렬 로테로 평가 → 버프-캡 스탯이 치피/공%로 자동 재배분(메인옵 포함).
            double FullScore()
            {
                // 기어평가 진형·후열(최종 정합용). 기본은 "기본 진형"·전부 전열(기존 동작).
                if (config.GearEvalBackRow != null)
                    foreach (var t in team)
                        t.IsBackPosition = t.Character != null && config.GearEvalBackRow.Contains(t.Character.Name);
                var sc = BuildSimConfig(config, team, config.GearEvalFormation ?? "기본 진형");
                if (config.GearEvalRotation != null && config.GearEvalRotation.Count > 0)
                    sc.RotationPlan = config.GearEvalRotation;
                return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc).TotalScore;
            }

            // [허수아비 딜러 기어] 딜러 판별 + 대표 넉 + 허수아비 단타 DPS (스쿼드 풀버프 + 보스 HP0=잃은체력 최대).
            bool IsDealerChar(BattleCharacter b) => config.DummyGearForDealers && b.Character != null
                && (b.Character.Type == "공격형" || b.Character.Type == "마법형" || b.Character.Type == "만능형");
            // 넉 = 최대 배율이 아니라 ★최대 실제 허수아비 단타로 선정 — 죽음의무도(배율 낮아도 잃은체력 조건 260%·
            //   타수)처럼 S2가 메인딜인 경우를 올바로 잡는다. 스쿼드 풀버프+보스HP0 기준.
            Skill DealerNuke(BattleCharacter b)
            {
                Skill nuke = null; double best = -1;
                foreach (var sk in b.Character.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (sk.SkillType == SkillType.Normal || sk.SkillType == SkillType.Normal2) continue;
                    double d = DummyScore(b, sk);
                    if (d > best) { best = d; nuke = sk; }
                }
                return nuke;
            }
            double DummyScore(BattleCharacter b, Skill nuke)
            {
                if (config.GearEvalBackRow != null)
                    foreach (var t in team)
                        t.IsBackPosition = t.Character != null && config.GearEvalBackRow.Contains(t.Character.Name);
                var dsc = BuildSimConfig(config, team, config.GearEvalFormation ?? "기본 진형");
                return new SiegeBattleSimulator(GearCompareSeed).EvaluateDummyNuke(dsc, team.IndexOf(b), nuke);
            }

            foreach (var bc in targets)
            {
                // [허수아비] 딜러는 세트·메인부옵·장신구·전용을 허수아비 단타 DPS로 전수탐색 (4세트 한정 v1).
                //   풀버프가 치확/약확 캡을 직접 반영 → Floor 프록시 불필요(default). 탱·서포터는 아래 기존 경로.
                if (IsDealerChar(bc))
                {
                    var nuke = DealerNuke(bc);
                    if (nuke != null)
                    {
                        var allowed = GcForChar(bc)?.AllowedSets ?? new[] { "복수자", "암살자", "추적자", "선봉장" };
                        EquipmentLoadout best = null; double bestS = -1; string bestSet = null; var per = new List<string>();
                        foreach (var setName in allowed)
                        {
                            var lo = optimizer.OptimizeForSetFull(bc, SoloConfig(config, boss, bc), 0, setName,
                                GcForChar(bc), l => { bc.Equipment = l; return DummyScore(bc, nuke); });
                            if (lo == null) continue;
                            bc.Equipment = lo; double s = DummyScore(bc, nuke);
                            per.Add($"{setName}={s:N0}");
                            if (s > bestS) { bestS = s; best = lo; bestSet = setName; }
                        }
                        if (best != null) bc.Equipment = best;
                        log.Add($"[{bc.Character.Name}] 세트 선택(허수아비 {nuke.Name} 단타 {bestS:N0}): {bestSet}  ← {string.Join(" / ", per)}");
                        log.Add(FormatGear(bc));
                        continue;
                    }
                }

                var list = cands[bc];
                string chosenSet = list.Count > 0 ? list[0].Set : null;
                if (list.Count > 1)
                {
                    EquipmentLoadout bestLo = bc.Equipment; double bestScore = -1;
                    var perSet = new List<string>();
                    foreach (var (setName, lo) in list)
                    {
                        bc.Equipment = lo;
                        double sc = FullScore();
                        perSet.Add($"{setName}={sc:N0}");
                        if (sc > bestScore) { bestScore = sc; bestLo = lo; chosenSet = setName; }
                    }
                    bc.Equipment = bestLo;
                    log.Add($"[{bc.Character.Name}] 세트 선택(풀시뮬): {chosenSet}  ← {string.Join(" / ", perSet)}");
                }

                // 메인·부옵·장신구도 풀시뮬 점수로 재최적화 (선택된 세트 안에서, 강제 옵션 반영)
                if (chosenSet != null)
                {
                    var refined = optimizer.OptimizeForSetFull(bc, SoloConfig(config, boss, bc), 0, chosenSet,
                        GcForChar(bc), lo => { bc.Equipment = lo; return FullScore(); }, Floor(bc));
                    if (refined != null) bc.Equipment = refined;
                }
                log.Add(FormatGear(bc));
            }

            // 3) 전용무기 조율 4슬롯 탐색 (전설 가정) — 기어 확정 후 영웅별 그리디(풀시뮬 점수).
            if (config.SearchExclusiveWeapon)
            {
                foreach (var bc in targets)
                {
                    if (bc.Character == null) continue;
                    bool magic = bc.Character.AttackType == AttackType.Magic;
                    // 딜러(공격/마법/만능형) = 공격조율만. 지원/방어형만 생존조율 후보 포함.
                    bool isDealer = bc.Character.Type is "공격형" or "마법형" or "만능형";
                    // [허수아비] 딜러 전용조율도 허수아비 단타 DPS로 (탱·서포터는 FullScore).
                    var dummyNuke = IsDealerChar(bc) ? DealerNuke(bc) : null;
                    ExclusiveWeapon bestW = bc.Character.ExclusiveWeapon; double bestS = -1;
                    foreach (var w in ExclusiveTuningCandidates(magic, bc.Character.Id, isDealer))
                    {
                        bc.Character.ExclusiveWeapon = w;
                        double s = dummyNuke != null ? DummyScore(bc, dummyNuke) : FullScore();
                        if (s > bestS) { bestS = s; bestW = w; }
                    }
                    bc.Character.ExclusiveWeapon = bestW;
                    log.Add($"[{bc.Character.Name}] 전용조율: {DescribeTuning(bestW)}");
                }
            }
            return log;
        }

        /// <summary>
        /// 생존반지 후처리 — 최종 best config(진형·자리·로테이션 고정)에서 죽는(부활 못 한) 캐릭에
        /// 권능/부활/불사 반지를 같은 조건으로 재시뮬해 비교, 점수 개선 시 채택한다.
        /// 사망캐 게이팅 + 캐스케이드(한 명 살리면 새로 죽는 캐릭) 대응으로 반복(상한 5). 매 회 최선의
        /// (캐릭×반지) 1개만 그리디 채택. 스탯(등급/메인/부옵)은 기존 장신구 그대로, 생존효과만 부여.
        /// </summary>
        /// <returns>생존반지를 1개 이상 채택했으면 true (호출부가 로테 재최적화 트리거).</returns>
        private bool ApplySurvivalRings(SiegeOptimizerConfig config, SiegeOptimizerResult best)
        {
            SiegeBattleResult SimBest() => new SiegeBattleSimulator(GearCompareSeed).Simulate(new SiegeBattleConfig
            {
                AllyParty = best.BestParty, FormationName = best.BestFormation, SiegeStage = config.SiegeStage,
                AllyPet = config.AllyPet, PetStar = config.PetStar, PetEnhance = config.PetEnhance,
                PetOptionAtkRate = config.PetOptionAtkRate, PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate, MaxTurns = config.MaxTurns,
                RotationPlan = best.BestRotationPlan,
            });

            var ringed = new HashSet<BattleCharacter>();
            for (int guard = 0; guard < 5; guard++)
            {
                var cur = SimBest();
                var dyers = cur.CharacterResults.Where(c => c.Died).Select(c => c.CharacterName).ToHashSet();
                if (dyers.Count == 0) break;

                // 채택 기준 생존(RankScore) 우선(동률 시 raw) — 사망을 막으면 rank +20만이라 raw를 약간
                //   낮추는 반지도 채택될 수 있다(생존 우선 철학). 표시는 실제 딜(TotalScore).
                BattleCharacter pick = null; Accessory pickAcc = null;
                double pickRank = cur.RankScore; double pickTotal = cur.TotalScore;
                foreach (var bc in best.BestParty)
                {
                    if (bc.Character == null || bc.Equipment?.Accessory == null) continue;
                    if (ringed.Contains(bc) || !dyers.Contains(bc.Character.Name)) continue;
                    var orig = bc.Equipment.Accessory;
                    foreach (var ring in SurvivalRingCandidates(orig))
                    {
                        bc.Equipment.Accessory = ring;
                        var rr = SimBest();
                        if (IsBetterPick(rr.TotalScore, rr.RankScore, pickTotal, pickRank)) { pickRank = rr.RankScore; pickTotal = rr.TotalScore; pick = bc; pickAcc = ring; }
                    }
                    bc.Equipment.Accessory = orig;   // 원복 (채택은 1회 1개만)
                }

                if (pick == null) break;             // 개선되는 생존반지 없음 → 종료
                double before = cur.TotalScore;
                pick.Equipment.Accessory = pickAcc;
                ringed.Add(pick);
                best.GearLog.Add($"[{pick.Character.Name}] 생존반지 채택: {pickAcc.RingName} (사망→생존, 팀점수 {before:N0}→{pickTotal:N0})");
            }

            if (ringed.Count > 0)
            {
                var final = SimBest();
                best.BestScore = final.TotalScore;
                best.BestRankScore = final.RankScore;
                best.BestResult = final;
            }
            return ringed.Count > 0;
        }

        /// <summary>
        /// 생존반지 채택 後 로테이션 재최적화 — 반지가 살린 캐릭의 버프 유지로 더 높은 로테가 가능해진다.
        /// 반지-장착 best config에서 (1) 빔 재탐색 + (2) 기존 crossPlans 재평가 → 가장 높은 로테로 갱신(무회귀).
        /// 자기 플랜(현 BestRotationPlan)도 후보에 포함해 절대 퇴보하지 않게 한다.
        /// </summary>
        private void ReoptimizeRotationAfterRings(SiegeOptimizerConfig config, SiegeOptimizerResult best,
            List<List<RotationDecision>> crossPlans)
        {
            // 자리 재적용 (team은 공유·변형 객체).
            for (int i = 0; i < best.BestParty.Count; i++)
                best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

            // (1) 반지-장착 config에서 빔 재탐색 — 생존이 열어준 새 로테 공간 탐색.
            var beam = new RotationBeamSearch(GearCompareSeed).Search(
                BuildSimConfig(config, best.BestParty, best.BestFormation),
                config.RotationBeamWidth, config.RotationMaxDepth, config.CounterattackSearchSeeds);

            // (2) 후보 플랜 = 새 빔 + 기존 crossPlans + 현 채택 플랜. 반지-장착 config로 동일 시드 재평가.
            var candPlans = new List<List<RotationDecision>>();
            if (beam.Plan != null && beam.Plan.Count > 0) candPlans.Add(beam.Plan);
            if (beam.RankPlan != null && beam.RankPlan.Count > 0) candPlans.Add(beam.RankPlan);
            candPlans.AddRange(crossPlans);
            if (best.BestRotationPlan != null && best.BestRotationPlan.Count > 0) candPlans.Add(best.BestRotationPlan);

            double before = best.BestScore;
            foreach (var plan in candPlans)
            {
                var r = ScoreOnPlan(config, best.BestParty, best.BestFormation, plan);
                if (IsBetterPick(r.TotalScore, r.RankScore, best.BestScore, best.BestRankScore))
                {
                    best.BestScore = r.TotalScore;
                    best.BestRankScore = r.RankScore;
                    best.BestResult = r;
                    best.BestRotationPlan = plan;
                }
            }
            if (best.BestScore > before)
                best.GearLog.Add($"[로테 재최적화] 생존반지 後 천장 회수: {before:N0} → {best.BestScore:N0}");
        }

        /// <summary>생존반지 후보 — 기존 장신구의 스탯(등급/메인/부옵)은 유지하고 권능 효과만 부여.
        /// 공성전은 **권능의 반지만** 탐색한다: 권능은 사망하지 않고(생명력1로) 생존 → 버프 유지.
        /// 부활/불사는 사망 후 부활이라 보유 버프가 해제돼 저딜 서포터 생존 가치가 떨어짐(제외).</summary>
        private static IEnumerable<Accessory> SurvivalRingCandidates(Accessory baseAcc)
        {
            yield return new Accessory
            {
                Grade = baseAcc.Grade, MainOption = baseAcc.MainOption,
                SubOption = baseAcc.SubOption, RingName = "권능의 반지",
            };
        }

        /// <summary>전용무기 조율 후보(전설 4슬롯) — 딜러/탱커 공통 소수 조합만(8^4 전수 대신).
        /// 전용장비는 실게임에서 영웅당 1개뿐 → 요일별로 못 바꾼다. 따라서 딜러는 방어조율(탄성·생명력·방어력)을
        /// 후보에서 제외하고 공격조율(모든공격력·피해증폭)만 둔다. 탄성은 적 치확100 구간(토요일 챈슬러 버프)에서만
        /// 작동하는데 그건 비스킷 버프해제로 처리할 기믹이라, 딜러가 탄성을 끼면 상시 딜 손실이 된다(사용자 지적).
        /// 지원/방어형만 생존조율 후보를 갖는다.</summary>
        private static List<ExclusiveWeapon> ExclusiveTuningCandidates(bool magic, int charId, bool isDealer)
        {
            ExclusiveWeapon W(params TuningOption[] opts) => new()
            {
                Name = "조율탐색", OwnerCharacterId = charId, Atk = 247, IsMagic = magic,
                Tuning = opts.Select(o => new TuningSlot { Option = o, Grade = ExclusiveWeaponGrade.전설 }).ToList(),
            };
            var A = TuningOption.모든공격력; var D = TuningOption.피해증폭;
            var T = TuningOption.탄성; var H = TuningOption.생명력; var Df = TuningOption.방어력;
            var cands = new List<ExclusiveWeapon>
            {
                W(A, A, A, A),   // 모든공격력 몰빵
                W(A, A, A, D),
                W(A, A, D, D),
                W(A, D, D, D),
                W(D, D, D, D),   // 피해증폭 몰빵
            };
            if (isDealer) return cands;   // 딜러: 공격조율만 (방어조율은 전용장비 1개 제약상 상시 딜손실)
            // 지원/방어형: 생존조율도 후보
            cands.Add(W(A, A, T, T));   // 공격 + 생존(탄성)
            cands.Add(W(T, T, T, T));   // 생존
            cands.Add(W(H, H, Df, Df)); // 탱커
            return cands;
        }

        /// <summary>조율 구성 요약 문자열.</summary>
        private static string DescribeTuning(ExclusiveWeapon w)
        {
            if (w == null || w.Tuning == null || w.Tuning.Count == 0) return "(없음)";
            return string.Join("+", w.Tuning.GroupBy(t => t.Option)
                .Select(g => $"{g.Key}×{g.Count()}"));
        }

        /// <summary>장비 프록시 평가용 단일 영웅 BattleConfig (R3 보스 타깃).</summary>
        private static BattleConfig SoloConfig(SiegeOptimizerConfig config, Enemy boss, BattleCharacter bc) => new()
        {
            AllyParty = new List<BattleCharacter> { bc },
            TargetEnemy = boss,
            FormationName = "기본 진형",
            AllyPet = config.AllyPet,
            PetStar = config.PetStar,
            PetEnhance = config.PetEnhance,
            PetOptionAtkRate = config.PetOptionAtkRate,
            PetOptionDefRate = config.PetOptionDefRate,
            PetOptionHpRate = config.PetOptionHpRate,
        };

        /// <summary>빔이 고른 로테를 반격 ON(실전·기본 25%)으로 재평가 — 채택 점수 일관성.
        ///   CounterattackSearchSeeds>1(금요일)이면 N시드 평균(TotalScore·RankScore)으로 — 반격 RNG 과적합 방지.
        ///   반환 result는 첫 시드 것(라운드/기여/로테 표시용)에 헤드라인 점수만 평균으로 덮어씀.</summary>
        private static SiegeBattleResult ScoreOnPlan(SiegeOptimizerConfig config, List<BattleCharacter> team,
            string formation, List<RotationDecision> plan)
        {
            var sc = BuildSimConfig(config, team, formation);   // override null = 반격 ON
            sc.RotationPlan = plan;
            int n = System.Math.Max(1, config.CounterattackSearchSeeds);
            if (n <= 1) return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
            double sumT = 0, sumR = 0;
            SiegeBattleResult first = null;
            for (int s = 0; s < n; s++)
            {
                var r = new SiegeBattleSimulator(GearCompareSeed + s).Simulate(sc);
                sumT += r.TotalScore; sumR += r.RankScore;
                first ??= r;
            }
            first.TotalScore = sumT / n;
            first.RankScore = sumR / n;
            return first;
        }

        /// <summary>공성전 풀시뮬 SiegeBattleConfig 구성.</summary>
        private static SiegeBattleConfig BuildSimConfig(SiegeOptimizerConfig config, List<BattleCharacter> team, string formation) => new()
        {
            AllyParty = team,
            FormationName = formation,
            SiegeStage = config.SiegeStage,
            AllyPet = config.AllyPet,
            PetStar = config.PetStar,
            PetEnhance = config.PetEnhance,
            PetOptionAtkRate = config.PetOptionAtkRate,
            PetOptionDefRate = config.PetOptionDefRate,
            PetOptionHpRate = config.PetOptionHpRate,
            MaxTurns = config.MaxTurns,
            AllyDeathPenalty = config.AllyDeathPenalty,   // 생존 우선(전멸 빌드 회피) — 보고 점수 불변, RankScore에만 반영
        };

        /// <summary>장착된 장비의 세트·메인옵 값·부옵 값을 사람이 읽기 쉬운 문자열로.</summary>
        private static string FormatGear(BattleCharacter bc)
        {
            var lo = bc.Equipment;
            if (lo == null) return $"{bc.Character.Name}: 장비 없음";

            var sets = lo.GetActiveSets()?.Select(s => $"{s.SetName}{s.PieceCount}");
            var sb = new System.Text.StringBuilder();
            sb.Append($"[{bc.Character.Name}] 세트 {(sets != null ? string.Join("+", sets) : "")}");
            foreach (var e in lo.GetEquipments())
            {
                var subs = e.SubSlots
                    .Where(s => !string.IsNullOrEmpty(s.StatName))
                    .Select(s => $"{s.StatName} {s.DisplayValue}");
                sb.Append($"\n  {e.Name}: 메인 {e.MainStatName} {e.MainStatValue} | 부옵 {string.Join(", ", subs)}");
            }
            // 장신구
            if (lo.Accessory != null)
                sb.Append($"\n  장신구: {(string.IsNullOrEmpty(lo.Accessory.RingName) ? "" : $"《{lo.Accessory.RingName}》 ")}{lo.Accessory.Grade}성 메인 {lo.Accessory.MainOption}{(string.IsNullOrEmpty(lo.Accessory.SubOption) ? "" : $" / 부 {lo.Accessory.SubOption}")}");
            return sb.ToString();
        }

        // 딜러 = 공격형·마법형·만능형 / 딜러제외(서포터·탱커) = 지원형·방어형
        private static bool IsDealer(BattleCharacter bc)
        {
            var t = bc.Character.Type;
            return t == "공격형" || t == "마법형" || t == "만능형";
        }

        /// <summary>
        /// 역할 기반 장비 탐색 제약. 3-tier 분류:
        ///  · 효과적 딜러: DPS 기어
        ///  · 면역/유틸리티(라이언 등 — 면역 패시브 보유 + 효과적 비딜러): 방어 전용 기어 (생존 우선, 죽으면 utility 손실)
        ///  · 서포터/버퍼(비스킷·리나 등): 혼합 (데미지 + 방어)
        /// </summary>
        private static GearConstraints GetGearConstraints(BattleCharacter bc, Stage stage = null)
        {
            if (IsEffectiveDealer(bc, stage))
                return new GearConstraints
                {
                    AllowedSets = new[] { "복수자", "암살자", "추적자", "선봉장" },
                    WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                    ArmorMains = new[] { "공격력%" },
                    SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력" },
                };
            // 면역 패시브 보유자(라이언 화상면역 등) — 죽으면 utility(쿨감·면역) 손실이 크므로 방어 전용.
            if (HasImmunityPassive(bc))
                return new GearConstraints
                {
                    AllowedSets = new[] { "수문장", "복수자" },
                    WeaponMains = new[] { "생명력%", "방어력%" },
                    ArmorMains = new[] { "생명력%", "방어력%", "받피감%" },
                    SubOptions = new[] { "생명력%", "방어력%", "생명력", "방어력", "막기확률%" },
                };
            return new GearConstraints
            {
                AllowedSets = new[] { "복수자", "수문장" },
                WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                ArmorMains = new[] { "공격력%", "받피감%" },
                SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력", "생명력%", "방어력%" },
            };
        }

        /// <summary>면역 패시브(StatusImmunity) 보유 여부 — 라이언(화상)·풍연(빙결) 등. 죽으면 효과 손실.</summary>
        private static bool HasImmunityPassive(BattleCharacter bc)
        {
            var passive = bc.Character.Passive;
            if (passive == null) return false;
            bool Has(System.Collections.Generic.IEnumerable<Models.Effects.PersistentEffect> effs) =>
                effs != null && effs.Any(e => e.Type == Models.Effects.PersistentEffectType.Immunity);
            return Has(passive.GetLevelData(bc.IsSkillEnhanced)?.Effects)
                || Has(passive.GetTranscendBonus(bc.TranscendLevel)?.Effects);
        }

        /// <summary>
        /// 효과적 딜러 판정: 타입이 딜러여도 요일 reduction으로 데미지가 90% 이상 차단되면 유틸 취급.
        /// 예) 라이언(공격형)이 수요일(Phys90) 채용 시 → false → 생존 세팅 탐색.
        /// </summary>
        private static bool IsEffectiveDealer(BattleCharacter bc, Stage stage)
        {
            if (!IsDealer(bc)) return false;
            if (stage == null) return true;
            // 무대의 아무 적이나 골라 reduction 프로필 확인 (요일 reduction은 동일)
            var anyEnemyId = stage.Waves?.FirstOrDefault()?.Enemies?.FirstOrDefault()?.EnemyId ?? 0;
            var enemy = EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == anyEnemyId);
            if (enemy == null) return true;
            double red = bc.Character.AttackType == AttackType.Physical ? enemy.PhysicalReduction : enemy.MagicReduction;
            return red < 90;   // 90% 이상이면 효과적 비딜러
        }

        /// <summary>
        /// 스테이지 마지막 라운드(R3)의 보스 적을 EnemyDb에서 조회 (장비 평가 타깃).
        /// Enemy.IsBoss=true인 적(예: 스파이크)을 우선 — 보스피증(복수자 등)이 평가에 반영되도록.
        /// (룩/챈슬러 친위대는 R3에서 보스 취급이나 Enemy.IsBoss=false라 그대로 쓰면 보스피증이 빠짐.)
        /// </summary>
        private static Enemy ResolveBoss(Stage stage)
        {
            var wave = stage?.Waves?.OrderByDescending(w => w.WaveNumber).FirstOrDefault();
            if (wave?.Enemies == null || wave.Enemies.Count == 0) return null;

            Enemy Lookup(StageEnemy se) => se == null ? null : EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == se.EnemyId);

            // 1순위: StageEnemy.IsBoss이면서 Enemy.IsBoss=true (실제 보스)
            foreach (var se in wave.Enemies.Where(e => e.IsBoss))
            {
                var en = Lookup(se);
                if (en != null && en.IsBoss) return en;
            }
            // 2순위: 보스 취급 적, 3순위: 아무 적
            return Lookup(wave.Enemies.FirstOrDefault(e => e.IsBoss) ?? wave.Enemies.FirstOrDefault());
        }

        /// <summary>pool에서 k개 조합 (C(N,k)). k=0이면 빈 조합 하나.</summary>
        private static IEnumerable<List<BattleCharacter>> Combinations(List<BattleCharacter> pool, int k)
        {
            if (k <= 0) { yield return new List<BattleCharacter>(); yield break; }
            if (pool == null || pool.Count < k) yield break;

            int n = pool.Count;
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = i;

            while (true)
            {
                yield return idx.Select(i => pool[i]).ToList();

                int p = k - 1;
                while (p >= 0 && idx[p] == n - k + p) p--;
                if (p < 0) yield break;
                idx[p]++;
                for (int i = p + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
            }
        }
    }
}

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

        // 진형 단일 강제 (실측 비교용). null이면 전 진형 탐색.
        public string ForcedFormation { get; set; }

        // 후열 강제 (실측 비교용). null이면 전 마스크 탐색. 캐릭터 이름 리스트.
        public List<string> ForcedBackRow { get; set; }

        // 캐릭터별 강제 세트(실측 비교용). Key=Character.Id, Value=세트명("수문장","성기사" 등).
        // EquipCandidates에서 해당 캐릭터는 이 세트 후보만 사용 — 메인/부옵은 기존 로직대로 최적.
        public Dictionary<int, string> ForcedSetByCharId { get; set; } = new();

        // 캐릭터별 강제 메인옵션(실측 비교용). Key=Character.Id, Value=(무기메인, 방어구메인).
        // 지정 시 EquipCandidates는 해당 메인옵만 후보로 사용.
        public Dictionary<int, (string WeaponMain, string ArmorMain)> ForcedMainByCharId { get; set; } = new();
    }

    /// <summary>공성전 탐색 결과 (최고딜 팀 + 진형).</summary>
    public class SiegeOptimizerResult
    {
        public List<BattleCharacter> BestParty { get; set; } = new();
        public string BestFormation { get; set; }
        public double BestScore { get; set; }
        public SiegeBattleResult BestResult { get; set; }
        public List<string> BestBackRow { get; set; } = new();   // 최적 후열 배치 영웅 이름
        internal int BestMask { get; set; }                       // 최적 후열 배치 비트마스크 (재적용용)
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

        public SiegeOptimizerResult Optimize(SiegeOptimizerConfig config)
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
                // 진형 강제 옵션 (실측 비교): null이면 전체 탐색.
                var formationsToTry = string.IsNullOrEmpty(config.ForcedFormation)
                    ? Formations
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
                        var result = _sim.Simulate(simConfig);
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

                        if (best == null || result.TotalScore > best.BestScore)
                        {
                            best = new SiegeOptimizerResult
                            {
                                BestParty = team,
                                BestFormation = formation,
                                BestScore = result.TotalScore,
                                BestResult = result,
                                BestBackRow = backRow,
                                BestMask = mask,
                            };
                        }

                        // (B) 데미지 1위 캐릭이 후열인 config (자동로테 최고점, 전역). 기존 동작 유지.
                        var topDealer = result.CharacterResults.Count > 0
                            ? result.CharacterResults.OrderByDescending(c => c.TotalDamage).First() : null;
                        if (topDealer != null && backRow.Contains(topDealer.CharacterName)
                            && (bestDealerBack == null || result.TotalScore > bestDealerBack.BestScore))
                            bestDealerBack = new SiegeOptimizerResult
                            {
                                BestParty = team, BestFormation = formation, BestScore = result.TotalScore,
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
                        var carries = heroMax.OrderByDescending(kv => kv.Value).Take(requiredBack)
                            .Select(kv => kv.Key).ToHashSet();
                        foreach (var (m, r, br) in group)
                            if (br.Count == carries.Count && br.All(carries.Contains)
                                && (!bestDealerBackFull.TryGetValue(formation, out var cf) || r.TotalScore > cf.BestScore))
                                bestDealerBackFull[formation] = new SiegeOptimizerResult
                                {
                                    BestParty = team, BestFormation = formation, BestScore = r.TotalScore,
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
                            config.RotationBeamWidth, config.RotationMaxDepth);
                        // 빔은 반격 OFF(0%)로 로테를 골랐다 → 채택·비교 점수는 반격 ON(실전·단일시드)으로 재평가.
                        //   금요일(제이브)만 OFF≠ON; 그 외 보스는 반격 없어 동일(회귀 없음).
                        var beamOn = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, beam.Plan);
                        if (beamOn.TotalScore > cand.BestScore)
                        {
                            cand.BestScore = beamOn.TotalScore;
                            cand.BestResult = beamOn;
                            cand.BestRotationPlan = beam.Plan;
                        }
                    }

                    // 교차수분: 각 후보의 빔 플랜을 다른 후보 config에도 평가해, 빔이 그 config에서 못 찾은
                    //   더 높은 로테(다른 자리/진형의 우승 로테)를 채택한다. 로테는 (HeroIndex,Skill) 리스트라
                    //   자리·진형 무관 → 교차적용 가능. (목요일: 전열 우승로테를 라이언-후열 자리에 적용하면
                    //   후열 자체 빔보다 높음 — 빔이 후열을 지역최적으로 저평가한 것을 교정.) 무회귀(더 높을 때만).
                    crossPlans = beamCands
                        .Where(c => c.BestRotationPlan != null && c.BestRotationPlan.Count > 0)
                        .Select(c => c.BestRotationPlan).ToList();
                    foreach (var cand in beamCands)
                    {
                        for (int i = 0; i < cand.BestParty.Count; i++)
                            cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                        foreach (var plan in crossPlans)
                        {
                            if (ReferenceEquals(plan, cand.BestRotationPlan)) continue;   // 자기 플랜은 이미 반영됨
                            var sc = BuildSimConfig(config, cand.BestParty, cand.BestFormation);
                            sc.RotationPlan = plan;
                            var r = new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
                            if (r.TotalScore > cand.BestScore)
                            {
                                cand.BestScore = r.TotalScore;
                                cand.BestResult = r;
                                cand.BestRotationPlan = plan;
                            }
                        }
                    }
                    // 빔 점수가 가장 높은 후보 채택.
                    best = beamCands.OrderByDescending(c => c.BestScore).First();
                }

                best.EvaluatedCount = evaluated;
                best.GearLog = gearLog;
                best.EvalLog = evalLog;
                // team은 공유·변형 객체이므로 최종 채택 config의 자리 배치를 마지막에 확정 재적용.
                for (int i = 0; i < best.BestParty.Count; i++)
                    best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

                // 생존반지 후처리 — 최종 config(진형·자리·로테이션 확정)에서 죽는 캐릭에 생존반지 부여.
                //   여기서 사망 감지를 하므로 전열 가정 과탐지 없이 "실제로 죽는" 캐릭만 대상.
                if (config.AutoEquip)
                {
                    bool ringsApplied = ApplySurvivalRings(config, best);
                    // 반지 채택 → 저딜 서포터가 생존하며 버프를 유지 → 더 높은 로테 천장이 열린다.
                    //   기존 BestRotationPlan은 반지 前 고정값이라 그 천장을 모름(목요일 후열 ~0.58M 손실).
                    //   반지-장착 config에서 빔 재탐색 + crossPlans 재평가로 천장을 회수(무회귀, 더 높을 때만).
                    if (ringsApplied && config.OptimizeRotation)
                        ReoptimizeRotationAfterRings(config, best, crossPlans);
                }
            }
            return best ?? new SiegeOptimizerResult { EvaluatedCount = 0, GearLog = gearLog, EvalLog = evalLog };
        }

        // 장비 후보(세트) 비교용 풀시뮬 고정 시드 — 후보 간 동일 RNG로 공정 비교.
        private const int GearCompareSeed = 777;

        /// <summary>
        /// 장비 미지정 영웅에게 최적 장비 장착. 영웅별로 허용 세트마다 후보(메인·부옵 프록시 최적)를 만들고,
        /// 그 세트 선택을 공성전 풀시뮬 팀 점수(고정 시드·기본 진형, 좌표상승)로 비교해 정한다.
        /// </summary>
        private List<string> EquipCandidates(SiegeOptimizerConfig config)
        {
            var log = new List<string>();
            var boss = ResolveBoss(config.SiegeStage);
            if (boss == null) return log;

            var optimizer = new EquipmentOptimizer();
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
            double FullScore() => new SiegeBattleSimulator(GearCompareSeed)
                .Simulate(BuildSimConfig(config, team, "기본 진형")).TotalScore;

            foreach (var bc in targets)
            {
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
                    ExclusiveWeapon bestW = bc.Character.ExclusiveWeapon; double bestS = -1;
                    foreach (var w in ExclusiveTuningCandidates(magic, bc.Character.Id))
                    {
                        bc.Character.ExclusiveWeapon = w;
                        double s = FullScore();
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

                BattleCharacter pick = null; Accessory pickAcc = null; double pickScore = cur.TotalScore;
                foreach (var bc in best.BestParty)
                {
                    if (bc.Character == null || bc.Equipment?.Accessory == null) continue;
                    if (ringed.Contains(bc) || !dyers.Contains(bc.Character.Name)) continue;
                    var orig = bc.Equipment.Accessory;
                    foreach (var ring in SurvivalRingCandidates(orig))
                    {
                        bc.Equipment.Accessory = ring;
                        double s = SimBest().TotalScore;
                        if (s > pickScore) { pickScore = s; pick = bc; pickAcc = ring; }
                    }
                    bc.Equipment.Accessory = orig;   // 원복 (채택은 1회 1개만)
                }

                if (pick == null) break;             // 개선되는 생존반지 없음 → 종료
                double before = cur.TotalScore;
                pick.Equipment.Accessory = pickAcc;
                ringed.Add(pick);
                best.GearLog.Add($"[{pick.Character.Name}] 생존반지 채택: {pickAcc.RingName} (사망→생존, 팀점수 {before:N0}→{pickScore:N0})");
            }

            if (ringed.Count > 0)
            {
                var final = SimBest();
                best.BestScore = final.TotalScore;
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
                config.RotationBeamWidth, config.RotationMaxDepth);

            // (2) 후보 플랜 = 새 빔 + 기존 crossPlans + 현 채택 플랜. 반지-장착 config로 동일 시드 재평가.
            var candPlans = new List<List<RotationDecision>>();
            if (beam.Plan != null && beam.Plan.Count > 0) candPlans.Add(beam.Plan);
            candPlans.AddRange(crossPlans);
            if (best.BestRotationPlan != null && best.BestRotationPlan.Count > 0) candPlans.Add(best.BestRotationPlan);

            double before = best.BestScore;
            foreach (var plan in candPlans)
            {
                var sc = BuildSimConfig(config, best.BestParty, best.BestFormation);
                sc.RotationPlan = plan;
                var r = new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
                if (r.TotalScore > best.BestScore)
                {
                    best.BestScore = r.TotalScore;
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

        /// <summary>전용무기 조율 후보(전설 4슬롯) — 딜러/탱커 공통 소수 조합만(8^4 전수 대신).</summary>
        private static List<ExclusiveWeapon> ExclusiveTuningCandidates(bool magic, int charId)
        {
            ExclusiveWeapon W(params TuningOption[] opts) => new()
            {
                Name = "조율탐색", OwnerCharacterId = charId, Atk = 247, IsMagic = magic,
                Tuning = opts.Select(o => new TuningSlot { Option = o, Grade = ExclusiveWeaponGrade.전설 }).ToList(),
            };
            var A = TuningOption.모든공격력; var D = TuningOption.피해증폭;
            var T = TuningOption.탄성; var H = TuningOption.생명력; var Df = TuningOption.방어력;
            return new List<ExclusiveWeapon>
            {
                W(A, A, A, A),   // 딜러: 모든공격력 몰빵
                W(A, A, A, D),
                W(A, A, D, D),
                W(A, D, D, D),
                W(D, D, D, D),   // 피해증폭 몰빵
                W(A, A, T, T),   // 공격 + 생존(탄성)
                W(T, T, T, T),   // 생존
                W(H, H, Df, Df), // 탱커
            };
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

        /// <summary>빔이 OFF로 고른 로테를 반격 ON(실전·기본 25%)으로 재평가 — 채택 점수 일관성.</summary>
        private static SiegeBattleResult ScoreOnPlan(SiegeOptimizerConfig config, List<BattleCharacter> team,
            string formation, List<RotationDecision> plan)
        {
            var sc = BuildSimConfig(config, team, formation);   // override null = 반격 ON
            sc.RotationPlan = plan;
            return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
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

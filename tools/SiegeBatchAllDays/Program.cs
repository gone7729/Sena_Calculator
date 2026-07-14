using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 월~일 요일별 팀 고정 + 진형·기어·스킬순서 탐색 → JSON + TXT 저장.
//   기본 6초월·잠재0/0/0, 인자 "12초월잠재3" 지정 시 12초월·잠재3/3/3. 스킬강화 / 펫 윈디 6성 강화+3·
//   펫잠재 모공%72(18×4) / 전용무기 없음은 공통. 출력 파일은 프로필 접미사로 분리(siege_{요일}_{프로필}).
//   탐색: 진형 3종(기본/밸런스/보호)×자리 + AutoEquip(세트/메인/부옵/장신구) + 빔 로테이션.
// ============================================================================

// 요일별 팀 (영웅 id). 월요일 5번째 = 지크(303). (대안: 에반 453)
var DAYS = new (string Day, int[] Ids)[]
{
    ("월요일", new[] { 118, 103, 202, 201, 303 }),   // [리나테스트] 나타·미호·리나(오를리203→202)·비스킷·지크
    ("화요일", new[] { 118, 103, 202, 201, 255 }),   // 나타·미호·리나·비스킷·클로에
    ("수요일", new[] { 118, 103, 202, 201, 2   }),   // 나타·미호·리나·비스킷·라이언
    ("목요일", new[] { 2,   1,   301, 201, 15  }),   // 라이언·타카·레이첼·비스킷·돼오
    ("금요일", new[] { 2,   1,   301, 201, 303 }),   // 라이언·타카·레이첼·비스킷·지크
    ("토요일", new[] { 2,   1,   301, 201, 51  }),   // 라이언·타카·레이첼·비스킷·풍연
    ("일요일", new[] { 101, 117, 201, 57,  103 }),   // 파스칼·소교·비스킷·샤오(즉사면역)·미호
};

// 요일별 전문가(공개 고점) 로테 시드 — 빔과 함께 평가해 더 높은 쪽(max) 정식 채택.
//   비스킷S1=장비강화·S2=리프어택 / 나타S1=화첨·S2=혼천 / 클로에S1=고양이·S2=청소 / 리나S2=따뜻한울림 / 미호S1=살육·S2=교만.
var expertByDay = new Dictionary<string, (string Name, SkillType Skill)[]>
{
    // 화요일 공개 고점 빌드(유저 제공, 전용 전 10.18M / 시뮬 전용 포함 11.2M, 빔 10.68M 상회).
    ["화요일"] = new (string, SkillType)[]
    {
        ("비스킷", SkillType.Skill1), ("나타", SkillType.Skill1),                       // R1
        ("클로에", SkillType.Skill2), ("비스킷", SkillType.Skill2),                     // R2
        ("리나", SkillType.Skill2), ("미호", SkillType.Skill1), ("나타", SkillType.Skill2),
        ("미호", SkillType.Skill2), ("나타", SkillType.Skill1), ("미호", SkillType.Skill1),
        ("클로에", SkillType.Skill2), ("비스킷", SkillType.Skill1), ("나타", SkillType.Skill2),
        ("나타", SkillType.Skill1), ("미호", SkillType.Skill2), ("리나", SkillType.Skill2),
        ("클로에", SkillType.Skill2), ("미호", SkillType.Skill1), ("나타", SkillType.Skill2),
        ("나타", SkillType.Skill1),
    },
    // 목요일 공개 고점 빌드(유저 제공, 전용 전 12.47M, 라이언 53%·타카 38%).
    //   라이언 강자사냥=S1·광풍참=S2 / 타카 바람의칼날=S1·죽음의무도=S2 / 레이첼 염화=S1·불새=S2 / 비스킷 장비강화=S1 / 돼오 룰렛맨=S1.
    ["목요일"] = new (string, SkillType)[]
    {
        ("레이첼", SkillType.Skill1), ("라이언", SkillType.Skill1), ("비스킷", SkillType.Skill1), ("돼오", SkillType.Skill1),
        ("타카", SkillType.Skill2), ("라이언", SkillType.Skill2), ("타카", SkillType.Skill1), ("라이언", SkillType.Skill1),
        ("레이첼", SkillType.Skill1), ("레이첼", SkillType.Skill2), ("비스킷", SkillType.Skill1),
        ("타카", SkillType.Skill2), ("라이언", SkillType.Skill2), ("타카", SkillType.Skill1), ("라이언", SkillType.Skill1),
        ("돼오", SkillType.Skill1), ("레이첼", SkillType.Skill2), ("타카", SkillType.Skill2), ("라이언", SkillType.Skill2),
    },
};

// 프로필 = 4개 탐색 루트. 인자로 (초월 프로필) × (펫) 선택. 펫 6성·강화3·모공%76 공통.
//   ▸ "6초월"  → 6초월·잠재0·전용장비X·권능반지X  (사용자 실측 계정 가정).
//   ▸ 미지정    → 12초월·잠재3·전용장비O·권능반지O  (고스펙 천장).
//   펫: "델로"(치확·치피) / "리첼"(약확·약공증) / 미지정="윈디"(공%·보스취약).
//   4루트 = {6초월×델로, 6초월×리첼, 6초월×윈디, 12초월×윈디}. 출력 접미사 = "{초월}초월_{펫}".
bool isLowSpec = args.Contains("6초월");
int TRANS = isLowSpec ? 6 : 12;
int POT = isLowSpec ? 0 : 3;
// [스펙 override] 인자 "초월N"/"잠재N"으로 초월·잠재 레벨 맞춤(유저 실측 스펙 대조용). 예: 초월9 잠재3.
var transArg = args.FirstOrDefault(a => a.StartsWith("초월") && int.TryParse(a.Substring("초월".Length), out _));
if (transArg != null) TRANS = int.Parse(transArg.Substring("초월".Length));
var potArg = args.FirstOrDefault(a => a.StartsWith("잠재") && int.TryParse(a.Substring("잠재".Length), out _));
if (potArg != null) POT = int.Parse(potArg.Substring("잠재".Length));
bool searchExclusive = !isLowSpec;       // 6초월=전용장비 미장착, 12초월=전설 4슬롯 조율 탐색
bool enableRings = !isLowSpec;           // 6초월=권능반지 제외, 12초월=생존(권능)반지 후처리 포함
string petName = args.Contains("델로") ? "델로"
               : args.Contains("리첼") ? "리첼"
               : "윈디";
string SUFFIX = $"{TRANS}초월_{petName}";

// 인자로 요일 지정 시 해당 요일만 탐색 (예: dotnet run -- 수요일). 미지정이면 전 요일. (프로필 토큰은 요일 아님 → 무시)
var dayArgs = args.Where(a => DAYS.Any(d => d.Day == a)).ToArray();
if (dayArgs.Length > 0)
    DAYS = DAYS.Where(d => dayArgs.Contains(d.Day)).ToArray();

// 사망 페널티 오버라이드(튜닝/검증): 인자 "페널티N" (예: 페널티0). 미지정이면 기본 100만.
//   딜러 공격조율(탄성 제외) 후 생존을 빔이 주기적 버프해제 로테로 확보하도록 유도하는 값(토요일 4→1사망, 해제 3회).
//   20만(구값)은 사망 빌드가 너무 쉽게 채택돼 후반 줄사망 허용 → 100만으로 상향.
double deathPenalty = 1_000_000;
var penArg = args.FirstOrDefault(a => a.StartsWith("페널티"));
if (penArg != null && double.TryParse(penArg.Substring("페널티".Length), out var pv)) deathPenalty = pv;

BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;   // null로 시작 → SearchExclusiveWeapon이 전설 4슬롯 조율을 탐색해 장착
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = TRANS,
        PotentialAtkLevel = POT, PotentialDefLevel = POT, PotentialHpLevel = POT,
        Equipment = null,       // AutoEquip이 기어 탐색
    };
}

// 커밋된 결과 JSON의 skillOrder(hero·skill 이름)를 RotationDecision으로 매핑 — 빔 교차수분 시드용. 없으면 null.
//   플랜 인덱스 = 전역 스킬턴 인덱스이고 홀드도 한 스텝을 차지하는데, skillOrder는 홀드를 빼고 저장한다
//   (feas.Where(!f.Hold)). 그래서 순서대로 이어붙이면 홀드 뒤의 모든 결정이 한 칸씩 당겨진 '다른 로테'가 된다.
//   → 각 항목의 step(1-based 스킬턴 인덱스)으로 원위치에 놓고, 빈 칸은 홀드로 복원한다. (스키마 불변 = 옛 JSON도 그대로 복원)
List<RotationDecision> LoadRot(string path, List<BattleCharacter> team)
{
    if (!System.IO.File.Exists(path)) return null;
    try
    {
        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("skillOrder", out var so)) return null;
        var byStep = new Dictionary<int, RotationDecision>();
        int maxStep = -1;
        foreach (var s in so.EnumerateArray())
        {
            if (s.TryGetProperty("isAuto", out var au) && au.GetBoolean()) continue;   // 빔 결정만(자동 꼬리 제외)
            string hn = s.GetProperty("hero").GetString();
            string sn = s.GetProperty("skill").GetString();
            int hi = team.FindIndex(t => t.Character?.Name == hn);
            var sk = hi >= 0 ? team[hi].Character.Skills.FirstOrDefault(k => k.Name == sn) : null;
            if (hi < 0 || sk == null) continue;
            int idx = s.TryGetProperty("step", out var st) ? st.GetInt32() - 1 : byStep.Count;
            if (idx < 0 || byStep.ContainsKey(idx)) continue;
            byStep[idx] = new RotationDecision { HeroIndex = hi, Skill = sk.SkillType };
            if (idx > maxStep) maxStep = idx;
        }
        if (maxStep < 0) return new List<RotationDecision>();
        var plan = new List<RotationDecision>(maxStep + 1);
        for (int i = 0; i <= maxStep; i++)
            plan.Add(byStep.TryGetValue(i, out var d) ? d : new RotationDecision { Hold = true });   // 빈 칸 = 홀드
        return plan;
    }
    catch { return null; }
}

// 시드 변형: 홀드를 모두 제거해 "쉬지 않고 계속 시전"하는 로테로 압축한다. 원본과 다른 별개의 유효 전략이며
//   (버스트를 늦추지 않고 앞당김), 다른 프로필/자리에선 이쪽이 더 높은 경우가 있다. 후보로만 쓰이므로(무회귀) 안전.
//   ※ 옛 LoadRot의 홀드 유실 버그가 사실상 이 변형을 만들어냈고, 월·화·수 12초월 시드(홀드 7~8칸)에선
//     그게 6초월 config의 고점이었다. 충실 복원만 남기면 그 고점을 잃으므로 둘 다 시드로 넣는다.
List<RotationDecision> StripHolds(List<RotationDecision> plan) =>
    plan?.Where(d => !d.Hold).ToList();

string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
// 출력은 results/siege/ 아래로 모은다 (json/txt/battlelog). 웹 반영은 web/scripts/sync-siege.mjs.
string outDir = System.IO.Path.Combine(repoRoot, "results", "siege");
System.IO.Directory.CreateDirectory(outDir);
var jsonOpts = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

foreach (var (day, ids) in DAYS)
{
    if (!EnemyDb.SiegeStages.TryGetValue(day, out var stage))
    {
        Console.WriteLine($"[skip] {day} 데이터 없음"); continue;
    }
    var team = ids.Select(Hero).ToList();
    var cfg = new SiegeOptimizerConfig
    {
        FixedMembers = team,
        Candidates = new List<BattleCharacter>(),
        PartySize = 5,
        SiegeStage = stage,
        AllyPet = PetDb.GetByName(petName),
        PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76,
        MaxTurns = 70,
        AutoEquip = true,
        SearchExclusiveWeapon = searchExclusive,   // 12초월=전설 4슬롯 조율, 6초월=미장착
        EnableSurvivalRings = enableRings,          // 12초월=권능반지 포함, 6초월=제외
        FloorFirstGear = true,          // 파티버프(비스킷 약확54 등) 반영해 치확/약확 100% 캡 — 과배분 방지
        AllyDeathPenalty = deathPenalty,
        OptimizeRotation = true,
        RotationBeamWidth = 10,
        RotationMaxDepth = 36,          // 70턴 전 스킬턴(~18~20) 커버 + 후반 버스트까지 플랜에 포함
        // 진형·자리 전체 탐색 (Forced* 미지정)
    };
    // [좌표상승+조인트 기본 ON] 기어↔로테/진형 2패스 + 메인·부옵·장신구 조인트 최적화. 7요일 무회귀 검증 완료
    //   (평균 +2.7%, 일 +10.1%). 딜러 메인옵(라이언 치피·소교 치확 등)을 사람이 박지 않아도 시뮬이 스스로 찾으므로
    //   옛 라이언 ForcedMain(목/금) 제거. 비교·디버그용으론 "노좌표상승" 인자로 옛 1패스 동작 복원.
    if (!args.Contains("노좌표상승")) cfg.CoordinateAscentGear = true;

    // [허수아비 딜러 기어] 인자 "허수아비기어" — 딜러 세트·메인부옵·전용을 허수아비 단타 DPS(스쿼드 풀버프+보스HP0)로
    //   전수탐색. 탱·서포터는 기존 풀시뮬. 기어→로테 단방향(좌표상승 딜러 우회). 기본 OFF(무회귀).
    if (args.Contains("허수아비기어")) cfg.DummyGearForDealers = true;

    // [탐색예산 override] 천장진단용 — 인자 "빔폭N"/"빔깊이N"으로 로테 빔 넓이·깊이 키워 탐색 부족 여부 판별.
    //   점수가 크게 오르면 탐색 부족(빔 협소), 거의 안 오르면 모델 천장(uptime 등). 미지정이면 기본(10/36).
    var bwArg = args.FirstOrDefault(a => a.StartsWith("빔폭"));
    if (bwArg != null && int.TryParse(bwArg.Substring("빔폭".Length), out var bw) && bw > 0) cfg.RotationBeamWidth = bw;
    var bdArg = args.FirstOrDefault(a => a.StartsWith("빔깊이"));
    if (bdArg != null && int.TryParse(bdArg.Substring("빔깊이".Length), out var bd) && bd > 0) cfg.RotationMaxDepth = bd;

    // [빔 평균 시드] 빔 로테 선택을 N시드 평균으로 → 빔이 RNG(반격·아군타겟) 과적합 없이 기대점 최고 로테 발굴.
    //   금요일(제이브 반격) 10시드, 일요일(적 타겟 랜덤→사망 변수 큼) 4시드, 그 외 1. 인자 "반격시드N"으로 조절.
    int caSeeds = day == "금요일" ? 10 : day == "일요일" ? 4 : 1;
    var caArg = args.FirstOrDefault(a => a.StartsWith("반격시드"));
    if (caArg != null && int.TryParse(caArg.Substring("반격시드".Length), out var cav) && cav > 0) caSeeds = cav;
    cfg.CounterattackSearchSeeds = caSeeds;

    // [채택 평가 평균 시드] 적의 아군 타겟이 랜덤(_rng)이라 단일 시드면 "누가 죽느냐"가 과적합됨.
    //   채택/비교·리포트 점수(ScoreOnPlan)를 N시드 평균으로 → 기대 점수·사망수 반영(빔보다 큰 N로 정확히).
    //   일요일만 기본 16, 그 외 1. 인자 "평가시드N"으로 조절. 빔=caSeeds(속도), 채택=Max(caSeeds,evalSeeds).
    int evalSeeds = day == "일요일" ? 16 : 1;
    var esArg = args.FirstOrDefault(a => a.StartsWith("평가시드"));
    if (esArg != null && int.TryParse(esArg.Substring("평가시드".Length), out var esv) && esv > 0) evalSeeds = esv;
    cfg.EvalSeeds = evalSeeds;

    // [일요일 최고점 정책] 적 타겟이 랜덤이라 N시드 평균이면 사망의 딜 손실이 TotalScore에 이미 반영됨(죽으면
    //   누적딜 멈춤). 인위적 사망 페널티는 이중계상 → 0으로 두고 "기대 TotalScore 최고점"으로 선택(사용자 '최고점').
    //   파국적 전멸 시드는 평균을 끌어내려 자연히 회피되고, 무해한 막판 사망은 허용된다. 인자 "페널티N"이 우선.
    if (day == "일요일" && penArg == null) cfg.AllyDeathPenalty = 0;

    // [기어=정렬 기준 선정] (실험·opt-in "기어정렬") expert 로테로 기어 선정. ★검증결과 역효과(목요일 14.87→13.92M):
    //   기본진형 단발 평가가 최종 빔과 불일치해 전체 기어가 나빠짐 → 기본 비활성. 라이언 치피 메인은 ForcedMain으로 별도 처리 권장.
    if (args.Contains("기어정렬") && expertByDay.TryGetValue(day, out var gearRot))
    {
        var teamNames = team.Select(t => t.Character.Name).ToList();
        cfg.GearEvalRotation = gearRot
            .Select(s => new RotationDecision { HeroIndex = teamNames.FindIndex(n => n == s.Name), Skill = s.Skill })
            .Where(d => d.HeroIndex >= 0).ToList();
        cfg.GearEvalFormation = "밸런스 진형";                                   // 최종 진형 정합
        cfg.GearEvalBackRow = day == "목요일" ? new List<string> { "라이언", "타카" } : null;  // 최종 후열 정합
    }

    // [검증] 인자 "강제딜러후열": 밸런스 진형에 딜러 2명(요일별)을 후열 강제 — 자동 자리탐색과 점수 비교용.
    //   사용자 가설: 딜러 후열(물공증·딜보존) + 비딜러 전열(반격 탱킹)이 빔 자동(딜러 전열)보다 높다.
    if (args.Contains("강제딜러후열"))
    {
        var dealerBack = new Dictionary<string, List<string>>
        {
            ["금요일"] = new() { "라이언", "타카" },
            ["목요일"] = new() { "라이언", "타카" },
            ["토요일"] = new() { "라이언", "타카" },
        };
        if (dealerBack.TryGetValue(day, out var db))
        {
            cfg.ForcedFormation = "밸런스 진형";
            cfg.ForcedBackRow = db;
        }
    }

    // [교차시드] 같은 요일의 기존 커밋된 로테(6초월·12초월 양쪽)를 빔 교차수분 시드로 투입 — 빔이 이 프로필에서
    //   못 찾은 더 높은 로테를 회수(목요일 검증: 6초월 로테가 12초월 빌드에서 +10%). 무회귀(더 높을 때만 채택).
    //   "노교차시드"로 비활성. (시드는 검증된 유효 로테라 stale이어도 후보로만 쓰여 안전.)
    if (!args.Contains("노교차시드"))
    {
        var seeds = new List<List<RotationDecision>>();
        foreach (var sp in new[] { "6초월", "12초월" })
        {
            var rot = LoadRot(System.IO.Path.Combine(outDir, $"siege_{day}_{sp}_윈디.json"), team);
            if (rot == null || rot.Count == 0) continue;
            seeds.Add(rot);                                   // 충실 복원(홀드 포함)
            var packed = StripHolds(rot);                     // 변형: 홀드 제거(쉬지 않고 시전)
            if (packed != null && packed.Count > 0 && packed.Count != rot.Count) seeds.Add(packed);
        }
        if (seeds.Count > 0) cfg.SeedRotations = seeds;
    }

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var res = new SiegeOptimizer().Optimize(cfg);
    sw.Stop();

    // ── [허수아비] 딜러별 풀버프 단타 DPS 지표 (인자 "허수아비") — 기어 평가/딜비중 우선순위 검증용.
    //   각 딜러의 최대배율 비평타 스킬(넉)을 보스 HP0(잃은체력 최대)+스쿼드 풀버프에서 단타 계산.
    if (args.Contains("허수아비"))
    {
        for (int i = 0; i < res.BestParty.Count; i++)
            res.BestParty[i].IsBackPosition = res.BestBackRow != null
                && res.BestParty[i].Character != null && res.BestBackRow.Contains(res.BestParty[i].Character.Name);
        var dummyCfg = new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
        };
        var dummySim = new SiegeBattleSimulator(777);
        Console.WriteLine($"\n══════ [허수아비 단타 DPS] {day} {SUFFIX} (보스 HP0 + 스쿼드 풀버프) ══════");
        for (int i = 0; i < res.BestParty.Count; i++)
        {
            var bc = res.BestParty[i];
            // 모든 비평타 스킬의 허수아비 단타를 평가해 ★최대를 넉으로 (max 배율 아님 — 죽음의무도 등 S2 메인딜 포착).
            var sks = (bc.Character.Skills ?? Enumerable.Empty<Skill>())
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2).ToList();
            if (sks.Count == 0) { Console.WriteLine($"  {bc.Character.Name,-6}: (넉 없음)"); continue; }
            Skill bestNuke = null; double bestDmg = -1; var parts = new List<string>();
            foreach (var sk in sks)
            {
                double d = dummySim.EvaluateDummyNuke(dummyCfg, i, sk);
                parts.Add($"{sk.Name}={d:N0}");
                if (d > bestDmg) { bestDmg = d; bestNuke = sk; }
            }
            Console.WriteLine($"  {bc.Character.Name,-6}: ★{bestNuke.Name}({bestDmg:N0})  ←  {string.Join(" / ", parts)}");
        }
        Console.WriteLine($"  [감사] 보스 적용 디버프: {dummySim.LastDummyDebuffSummary}");
        Console.WriteLine($"  [감사] 딜러 액티브버프: {dummySim.LastDummyBuffSummary}");
        Console.WriteLine("══════════════════════════════════\n");

        // [강제 풀uptime] 최종 빌드를 아군버프 만료 없이 재생 → "넉↔버프 정렬/uptime이 갭의 전부인가" 못박기.
        for (int i = 0; i < res.BestParty.Count; i++)
            res.BestParty[i].IsBackPosition = res.BestBackRow != null
                && res.BestParty[i].Character != null && res.BestBackRow.Contains(res.BestParty[i].Character.Name);
        var fullUpCfg = new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
            RotationPlan = res.BestRotationPlan, ForceFullBuffUptime = true,
        };
        var fullUp = new SiegeBattleSimulator(777).Simulate(fullUpCfg);
        Console.WriteLine($"══════ [강제 풀uptime] {day} {SUFFIX} ══════");
        Console.WriteLine($"  일반(채택 로테)   : {res.BestScore,14:N0}");
        Console.WriteLine($"  강제 풀uptime     : {fullUp.TotalScore,14:N0}  ({(fullUp.TotalScore/Math.Max(1,res.BestScore)-1)*100:+0.0;-0.0}%)");
        foreach (var c in fullUp.CharacterResults.OrderByDescending(c => c.TotalDamage))
            Console.WriteLine($"    {c.CharacterName,-6}: {c.TotalDamage,12:N0}");
        Console.WriteLine("══════════════════════════════════\n");
    }

    // [기어 스탯 출력] 각 영웅 기어 블록 끝에 "기어만"(기본+장비+세트+초월+잠재+전용) 치확/약확/치피 삽입.
    //   StatCalculator는 본인 상시 패시브 자버프(예: 나타 치확33)를 DisplayStats에 항상 더하므로,
    //   "기어만" 표시를 위해 GetTotalSelfBuff의 치확/약확/치피를 빼준다. (파티버프는 애초에 미전달이라 제외됨)
    {
        var statCalc = new StatCalculator();
        string GearStat(BattleCharacter bc)
        {
            var lo = bc.Equipment;
            var ds = statCalc.Calculate(new StatCalculationInput
            {
                Character = bc.Character, TranscendLevel = bc.TranscendLevel, IsSkillEnhanced = bc.IsSkillEnhanced,
                Equipments = lo?.GetEquipments(), Accessory = lo?.Accessory,
                EquipSets = lo?.GetActiveSets(),
                PotentialAtkLevel = bc.PotentialAtkLevel, PotentialDefLevel = bc.PotentialDefLevel, PotentialHpLevel = bc.PotentialHpLevel,
                ExclusiveWeapon = bc.Character?.ExclusiveWeapon,
                Formation = new Formation { Name = res.BestFormation, IsBackPosition = bc.IsBackPosition },
                Pet = cfg.AllyPet, PetStar = cfg.PetStar, PetOptionAtkRate = cfg.PetOptionAtkRate,
            }).DisplayStats ?? new BaseStatSet();
            // 본인 상시 패시브 자버프 제외(기어만 표시). GetTotalSelfBuff = 본인 Self+Party 상시 버프.
            var pas = new BuffSet();
            var selfPas = bc.Character?.Passive?.GetTotalSelfBuff(bc.IsSkillEnhanced, bc.TranscendLevel);
            if (selfPas != null) pas.Add(selfPas);
            double cri = ds.Cri - pas.Cri, wek = ds.Wek - pas.Wek, criDmg = ds.Cri_Dmg - pas.Cri_Dmg;
            return $"[{bc.Character.Name}] 기어스탯(버프전): 치확 {cri:F0}% · 약확 {wek:F0}% · 치피 {criDmg:F0}%";
        }
        // 각 영웅의 마지막 "[이름]" 기어 로그 줄 뒤에 스탯 줄 삽입.
        var augmented = new List<string>(res.GearLog);
        foreach (var bc in res.BestParty)
        {
            string tag = $"[{bc.Character.Name}]";
            int lastIdx = augmented.FindLastIndex(l => l.StartsWith(tag));
            if (lastIdx >= 0) augmented.Insert(lastIdx + 1, GearStat(bc));
            else augmented.Add(GearStat(bc));
        }
        res.GearLog = augmented;
    }

    var nm = res.BestParty.Select(b => b.Character.Name).ToList();

    // [전문가 로테 시드] 등록된 요일이면 공개 로테를 같은 기어/진형으로 평가해 빔보다 높으면 정식 채택(무회귀).
    if (expertByDay.TryGetValue(day, out var expert))
    {
        var expertPlan = expert
            .Select(s => new RotationDecision { HeroIndex = nm.FindIndex(n => n == s.Name), Skill = s.Skill })
            .Where(d => d.HeroIndex >= 0).ToList();
        var eRes = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
            RotationPlan = expertPlan, AllyDeathPenalty = deathPenalty,
        });
        int eDeaths = eRes.Deaths?.Count ?? 0;
        Console.WriteLine($"  [전문가시드] {day}: 빔 Total {res.BestScore:N0}/Rank {res.BestRankScore:N0}  vs  전문가 Total {eRes.TotalScore:N0}/Rank {eRes.RankScore:N0} (사망 {eDeaths})");
        // 생존(RankScore) 우선 채택, 사실상 동률(±1)일 때만 raw로 tie-break — 옵티마이저 IsBetterPick과 동일 철학.
        bool expertBetter = eRes.RankScore > res.BestRankScore + 1.0
            || (System.Math.Abs(eRes.RankScore - res.BestRankScore) <= 1.0 && eRes.TotalScore > res.BestScore);
        if (expertBetter)
        {
            Console.WriteLine($"  [전문가시드 채택] {day}: 빔 {res.BestScore:N0} → 전문가 {eRes.TotalScore:N0}");
            res.BestScore = eRes.TotalScore; res.BestRankScore = eRes.RankScore;
            res.BestResult = eRes; res.BestRotationPlan = expertPlan;
        }
    }

    var party = (res.BestResult?.CharacterResults ?? new()).OrderByDescending(c => c.TotalDamage).ToList();

    // ── 빌드 실행가능성 검증 ──
    //   폴백/쿨여유는 반격 OFF(0%)로 검사 — 빔이 가정한 무반격 베이스라인에서 로테가 실제 실행가능한지.
    //     (금요일 제이브 반격의 3초 시간경과가 쿨을 흔들어 생기는 우발 폴백은 점수에만 반영, 실행가능성엔 제외.)
    //   점수 재현(scoreReproduced)은 반격 ON(최종 점수와 동일 조건)으로 별도 확인. 금요일만 OFF≠ON.
    var feasRes = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
    {
        AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
        AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
        PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
        PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
        RotationPlan = res.BestRotationPlan, RecordFeasibility = true,
        CounterattackChanceOverride = 0,   // 0% 기준 폴백검사
    });
    var feas = feasRes.Feasibility;
    // 스킬 빌드 = 실제 시전 순서(빔 플랜 + 플랜 범위 밖 자동 시전 포함, 70턴 전체). isAuto로 빔 명시 최적화 여부 구분.
    var skillOrder = feas.Where(f => f.Reached && !f.Hold).Select(f => new
    {
        step = f.StepIndex + 1,
        hero = f.HeroName,
        skill = f.SkillName,
        isAuto = f.IsAuto,
    }).ToList();
    int feasFallbacks = feas.Count(f => !f.ExecutedAsPlanned && !f.Hold && f.Reached);
    int feasUnreached = feas.Count(f => !f.Reached);
    // 최소 쿨여유 = 재시전(쿨 제약 받은 캐스트)만 — 첫 시전(쿨 무관)은 견고성 지표에서 제외.
    double feasMinSlack = feas.Where(f => f.ExecutedAsPlanned && !f.Hold && f.CooldownGated)
        .Select(f => f.Slack).DefaultIfEmpty(double.NaN).Min();
    string feasMinSlackStr = double.IsNaN(feasMinSlack) ? "n/a(재시전 없음)" : $"{feasMinSlack:F1}s";
    double ceiling0 = feasRes.TotalScore;   // 0% 천장(반격 0회 발동 시 점수)
    double score25 = res.BestScore;         // 25% 실전 점수(금요일=N시드 평균, 그 외=단일 시드)
    // 점수 재현: 최종 플랜을 반격 ON으로 재생해 res.BestScore와 일치하는지(플랜→점수 정합성).
    //   금요일(평균 탐색)이면 동일 N시드 평균으로 재현(res.BestScore도 평균이므로).
    SiegeBattleConfig OnReplayCfg() => new()
    {
        AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
        AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
        PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
        PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
        RotationPlan = res.BestRotationPlan,   // 반격 ON(기본)
    };
    // 재생 시드 = Max(반격시드, 평가시드). N시드 평균 점수 + 시드별 사망수 분포 수집(타겟 RNG 과적합 진단).
    int replaySeeds = Math.Max(Math.Max(caSeeds, evalSeeds), 1);
    SiegeBattleResult onReplay; double onReplayScore;
    var deathCounts = new List<int>();
    if (replaySeeds <= 1) { onReplay = new SiegeBattleSimulator(777).Simulate(OnReplayCfg()); onReplayScore = onReplay.TotalScore; deathCounts.Add(onReplay.Deaths?.Count ?? 0); }
    else
    {
        double sumOn = 0; onReplay = null;
        for (int s = 0; s < replaySeeds; s++) { var r = new SiegeBattleSimulator(777 + s).Simulate(OnReplayCfg()); sumOn += r.TotalScore; deathCounts.Add(r.Deaths?.Count ?? 0); onReplay ??= r; }
        onReplayScore = sumOn / replaySeeds;
    }
    double avgDeaths = deathCounts.Count > 0 ? deathCounts.Average() : 0;
    int seedsWithDeath = deathCounts.Count(d => d > 0);
    bool feasScoreMatch = Math.Abs(onReplayScore - res.BestScore) < Math.Max(1.0, replaySeeds > 1 ? res.BestScore * 0.005 : 1.0);
    bool feasOk = feasFallbacks == 0 && feasUnreached == 0;

    // ── [정렬로테] 유저 지정 전문가 로테(이름·스킬)를 같은 기어/진형으로 평가 → 빔과 비교 (인자 "정렬로테") ──
    //   목적: 빔이 정렬을 못 짠 건지(고정 로테>빔) 빔이 이미 최적인지(≤빔) 가르기. 일요일 파스칼 버스트 정렬 검증.
    if (args.Contains("정렬로테"))
    {
        // 요일별 전문가 정렬 로테(셋업 버프 → 버스트). 핵심: 큰 핵(2스킬)을 버프 윈도우 안에서 시전.
        var sundayAligned = new (string Name, SkillType Skill)[]
        {
            ("미호", SkillType.Skill2), ("미호", SkillType.Skill1),
            ("비스킷", SkillType.Skill1), ("샤오", SkillType.Skill1),
            ("소교", SkillType.Skill2),                                    // 호접지몽 셋업
            ("파스칼", SkillType.Skill2), ("파스칼", SkillType.Skill1), ("파스칼", SkillType.Skill2),
            ("소교", SkillType.Skill1), ("파스칼", SkillType.Skill1), ("파스칼", SkillType.Skill2),
            ("비스킷", SkillType.Skill1), ("샤오", SkillType.Skill1), ("소교", SkillType.Skill2),
            ("파스칼", SkillType.Skill2), ("파스칼", SkillType.Skill1), ("파스칼", SkillType.Skill2),
            ("소교", SkillType.Skill1), ("샤오", SkillType.Skill1),
        };
        // 화요일: R1/R2는 살육(S1)·화첨(S1)으로 클리어 → 큰 핵(교만 S2·혼천 S2)은 R3 버프 윈도우로 미룸.
        //   리나따뜻한울림(S2)+클로에청소시간(S2,+20%공+27%치확)+비스킷장비강화(S1) 깔고 → 교만·혼천 버스트.
        var tuesdayAligned = new (string Name, SkillType Skill)[]
        {
            ("미호", SkillType.Skill1),                                    // 1 살육의춤 — R1 클리어
            ("나타", SkillType.Skill1),                                    // 2 화첨창술 — R2 클리어
            ("리나", SkillType.Skill2),                                    // 3 따뜻한울림(버프+디버프)
            ("클로에", SkillType.Skill2),                                  // 4 청소시간(+20%공·+27%치확)
            ("비스킷", SkillType.Skill1),                                  // 5 장비강화(보스피증·약확)
            ("미호", SkillType.Skill2),                                    // 6 교만의일격 — 버프된 핵
            ("나타", SkillType.Skill2),                                    // 7 혼천릉파 — 버프된 핵
            ("비스킷", SkillType.Skill2),                                  // 8 리프어택(버프해제)
            ("클로에", SkillType.Skill1),                                  // 9 고양이은혜(필러·회복불가)
            ("나타", SkillType.Skill1),                                    // 10 화첨창술
            ("클로에", SkillType.Skill2),                                  // 11 청소시간 재셋업
            ("리나", SkillType.Skill2),                                    // 12 따뜻한울림 재셋업
            ("미호", SkillType.Skill2),                                    // 13 교만 — 버프된 핵
            ("나타", SkillType.Skill2),                                    // 14 혼천 — 버프된 핵
            ("미호", SkillType.Skill1),                                    // 15 살육
            ("나타", SkillType.Skill1),                                    // 16 화첨
            ("비스킷", SkillType.Skill1),                                  // 17 장비강화
            ("클로에", SkillType.Skill2),                                  // 18 청소시간
            ("미호", SkillType.Skill2), ("나타", SkillType.Skill2),       // 19-20 핵
        };
        // 화요일 공개 고점 빌드(유저 제공, 전용 전 10,176,300점). 적 행동 제외한 아군 스킬턴 20개.
        //   비스킷S1=장비강화·S2=리프어택 / 나타S1=화첨·S2=혼천 / 클로에S1=고양이·S2=청소 / 리나S2=따뜻한울림 / 미호S1=살육·S2=교만.
        var tuesdayPublic = new (string Name, SkillType Skill)[]
        {
            ("비스킷", SkillType.Skill1),                                  // R1
            ("나타", SkillType.Skill1),                                    // R1
            ("클로에", SkillType.Skill2),                                  // R2 청소시간
            ("비스킷", SkillType.Skill2),                                  // R2 리프어택
            ("리나", SkillType.Skill2),                                    // R3 따뜻한울림
            ("미호", SkillType.Skill1),                                    // 살육
            ("나타", SkillType.Skill2),                                    // 혼천
            ("미호", SkillType.Skill2),                                    // 교만(면역버프턴감)
            ("나타", SkillType.Skill1),                                    // 화첨
            ("미호", SkillType.Skill1),                                    // 살육
            ("클로에", SkillType.Skill2),                                  // 청소시간
            ("비스킷", SkillType.Skill1),                                  // 장비강화
            ("나타", SkillType.Skill2),                                    // 혼천
            ("나타", SkillType.Skill1),                                    // 화첨
            ("미호", SkillType.Skill2),                                    // 교만(면역버프턴감)
            ("리나", SkillType.Skill2),                                    // 따뜻한울림
            ("클로에", SkillType.Skill2),                                  // 청소시간
            ("미호", SkillType.Skill1),                                    // 살육
            ("나타", SkillType.Skill2),                                    // 혼천
            ("나타", SkillType.Skill1),                                    // 화첨
        };
        // 월요일: 빔(2해제) 플랜에 3번째 리프어택(루디 3번째 방어준비 T54 직후)을 슬롯17에 끼우고
        //   나머지 버스트(혼천/교만/화첨)를 한 슬롯씩 밀어 평가 → "3해제 > 2해제"인지 검증.
        var mondayThreeDispel = new (string Name, SkillType Skill)[]
        {
            ("미호", SkillType.Skill1),   // 1 살육
            ("비스킷", SkillType.Skill1), // 2 장비강화
            ("나타", SkillType.Skill1),   // 3 화첨
            ("나타", SkillType.Skill2),   // 4 혼천
            ("비스킷", SkillType.Skill2), // 5 리프 (해제1)
            ("리나", SkillType.Skill2),   // 6 따뜻한울림
            ("리나", SkillType.Skill1),   // 7 행진가
            ("미호", SkillType.Skill1),   // 8 살육
            ("나타", SkillType.Skill1),   // 9 화첨
            ("나타", SkillType.Skill2),   // 10 혼천
            ("비스킷", SkillType.Skill2), // 11 리프 (해제2)
            ("비스킷", SkillType.Skill1), // 12 장비강화
            ("미호", SkillType.Skill2),   // 13 교만
            ("나타", SkillType.Skill1),   // 14 화첨
            ("미호", SkillType.Skill1),   // 15 살육
            ("리나", SkillType.Skill2),   // 16 따뜻한울림
            ("비스킷", SkillType.Skill2), // 17 리프 (해제3 — NEW)
            ("나타", SkillType.Skill2),   // 18 혼천
            ("미호", SkillType.Skill2),   // 19 교만
            ("나타", SkillType.Skill1),   // 20 화첨
        };
        var alignedByName = (day == "화요일" && args.Contains("공개빌드")) ? tuesdayPublic
                          : day == "화요일" ? tuesdayAligned
                          : day == "월요일" ? mondayThreeDispel : sundayAligned;
        var alignedPlan = alignedByName
            .Select(s => new RotationDecision { HeroIndex = nm.FindIndex(n => n == s.Name), Skill = s.Skill })
            .Where(d => d.HeroIndex >= 0).ToList();

        // [결합검증] "치피기어": 딜러(나타·미호) 치확 부옵 → 치피로 재배분(청소시간 치확버프 전제). 정렬로테와 함께 평가.
        if (args.Contains("치피기어"))
        {
            int swapped = 0;
            foreach (var bc in res.BestParty.Where(b => b.Character.Name == "나타" || b.Character.Name == "미호"))
                foreach (var eq in bc.Equipment?.GetEquipments() ?? Enumerable.Empty<Equipment>())
                    foreach (var sub in eq.SubSlots)
                        if (sub.StatName != null && sub.StatName.Contains("치명타확률"))
                        { sub.StatName = "치명타피해%"; swapped++; }
            Console.WriteLine($"  [치피기어] 나타·미호 치확 부옵 {swapped}개 → 치피로 재배분");
        }
        var aRes = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
            RotationPlan = alignedPlan, RecordFeasibility = true, AllyDeathPenalty = deathPenalty,
        });
        var af = aRes.Feasibility;
        int aFallback = af.Count(f => !f.ExecutedAsPlanned && !f.Hold && f.Reached);
        Console.WriteLine($"\n══════ [정렬로테] {day} ══════");
        Console.WriteLine($"  빔 점수        : {res.BestScore,14:N0}");
        Console.WriteLine($"  정렬로테 점수  : {aRes.TotalScore,14:N0}  ({aRes.TotalScore/res.BestScore*100:F1}% of 빔)  폴백 {aFallback}개");
        Console.WriteLine($"  라운드별: {string.Join(", ", aRes.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))}");
        foreach (var c in aRes.CharacterResults.OrderByDescending(c => c.TotalDamage))
            Console.WriteLine($"    {c.CharacterName,-6}: {c.TotalDamage,12:N0}");
        Console.WriteLine("  ── 실행 로그(폴백 표기) ──");
        for (int i = 0; i < af.Count; i++)
        {
            var f = af[i];
            string who = f.Hold ? "(홀드)" : $"{f.HeroName} {f.SkillName}";
            string st = !f.Reached ? "미도달" : f.ExecutedAsPlanned ? "✓" : $"폴백({f.FallbackReason})";
            Console.WriteLine($"    {i+1,2}. {who,-22} {st}");
        }
        // 전투로그 인자 시: 이 정렬/공개 로테의 적 스킬 시전 순서 덤프 (유저 로그와 대조용).
        //   적 스킬 시전은 쿨감 로그("적 X(스킬) 시전 → 시간경과…")로 남으므로 그 라인에서 순서 추출.
        if (args.Contains("전투로그"))
        {
            Console.WriteLine("  ── 적 스킬 시전 순서 (공개 로테) ──");
            int ei = 0;
            foreach (var l in aRes.TurnLogs.Where(l => l.Description != null && l.Description.Contains("시전 →")))
                Console.WriteLine($"    {++ei,2}. T{l.Turn,2} {l.Description.Split('→')[0].Replace("적 ", "").Trim()}");
        }
        Console.WriteLine("══════════════════════════════════\n");
    }

    // ── [버프우선] 풀버프 정렬 자동로테(BuffFirstAuto) 평가 → 빔과 비교 (인자 "버프우선") ──
    //   파티버프 셋업 스킬을 딜러 핵보다 먼저 시전하는 전략. 같은 기어/진형으로 평가해 max 채택 여부 확인.
    if (args.Contains("버프우선"))
    {
        var bfRes = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
            BuffFirstAuto = true, AllyDeathPenalty = deathPenalty,
        });
        Console.WriteLine($"\n══════ [버프우선] {day} ══════");
        Console.WriteLine($"  빔 점수        : {res.BestScore,14:N0}");
        Console.WriteLine($"  버프우선 점수  : {bfRes.TotalScore,14:N0}  ({bfRes.TotalScore/res.BestScore*100:F1}% of 빔)");
        Console.WriteLine($"  채택(max)      : {Math.Max(res.BestScore, bfRes.TotalScore),14:N0}  ({(bfRes.TotalScore>res.BestScore?"버프우선 우세":"빔 우세")})");
        foreach (var c in bfRes.CharacterResults.OrderByDescending(c => c.TotalDamage))
            Console.WriteLine($"    {c.CharacterName,-6}: {c.TotalDamage,12:N0}");
        Console.WriteLine("══════════════════════════════════\n");
    }

    // ── [딜 진단] 라이언/타카 raw atk·DamageWeight·버프 수령자·딜기 가동 (콘솔 전용, 파일 불변) ──
    //   인게임 라이언쿨감·비스킷은 "공격력 최고 아군"(raw atk)에게, 시뮬은 DamageWeight 최고에게 → 둘이 어긋나는지 실측.
    if (args.Contains("딜진단"))
    {
        var diagSim = new SiegeBattleSimulator(777)
        {
            DiagAllyStats = true,
            DiagSkillNames = new List<string> { "광풍참", "죽음의 무도", "바람의 칼날", "강자 사냥" },
        };
        var diagRes = diagSim.Simulate(new SiegeBattleConfig
        {
            AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
            AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
            PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
            PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
            RotationPlan = res.BestRotationPlan,
        });
        Console.WriteLine($"\n══════ [딜 진단] {day} {SUFFIX} ══════");
        // (1) 아군 스탯 스냅샷 — DiagLog에서 per-cast 덤프(───────── T...) 이전 줄까지.
        foreach (var line in diagSim.DiagLog.ToString().Split('\n'))
        {
            if (line.StartsWith("───────── T")) break;
            if (line.Trim().Length > 0) Console.WriteLine(line.TrimEnd());
        }
        // (2) 딜기 가동 집계 (캐스트 수 + 합계 데미지) — TurnLogs 기반.
        var skillNames = new[] { "광풍참", "죽음의 무도", "바람의 칼날", "강자 사냥" };
        Console.WriteLine("  ── 딜기 가동(캐스트수 · 합계딜) ──");
        foreach (var g in diagRes.TurnLogs
            .Where(l => l.IsAlly && skillNames.Contains(l.SkillName) && l.DamageDealt > 0)
            .GroupBy(l => (l.ActorName, l.SkillName)))
            Console.WriteLine($"    {g.Key.ActorName,-6} {g.Key.SkillName,-8}: {g.Count(),3}회 · 합계 {g.Sum(l => l.DamageDealt),14:N0}");
        // (3) 라이언 평타쿨감 수령자 집계 (라이언 외 1명 = DamageWeight 1위).
        var cdRecip = new Dictionary<string, int>();
        foreach (var l in diagRes.TurnLogs.Where(l => l.Description != null && l.Description.Contains("평타 쿨감")))
        {
            int idx = l.Description.IndexOf("초: ");
            if (idx < 0) continue;
            foreach (var rn in l.Description.Substring(idx + 3).Split(','))
                cdRecip[rn.Trim()] = cdRecip.GetValueOrDefault(rn.Trim()) + 1;
        }
        Console.WriteLine("  ── 라이언 평타쿨감 수령자 (자신 포함) ──");
        foreach (var kv in cdRecip.OrderByDescending(k => k.Value))
            Console.WriteLine($"    {kv.Key,-6}: {kv.Value,3}회");
        Console.WriteLine("══════════════════════════════════\n");
    }

    // ── [전투로그] 턴별 전체 액션(아군+적) 덤프 — xlsx 실게임 빌드와 대조·챈슬러 재시전/사망 추적용 (인자 "전투로그") ──
    if (args.Contains("전투로그"))
    {
        var blsb = new StringBuilder();
        blsb.AppendLine($"=== {day} {SUFFIX} 전투로그 (반격 0% 재생, {feasRes.TurnLogs.Count} entries) ===");
        foreach (var l in feasRes.TurnLogs)
            blsb.AppendLine($"T{l.Turn,2} [{(l.IsAlly ? "아" : "적")}] {l.ActorName,-6} {l.SkillName,-10} {(l.DamageDealt > 0 ? $"{l.DamageDealt,12:N0}" : "".PadLeft(12))}  {l.Description}");
        string blPath = System.IO.Path.Combine(outDir, $"battlelog_{day}_{SUFFIX}.txt");
        System.IO.File.WriteAllText(blPath, blsb.ToString(), Encoding.UTF8);
        Console.WriteLine($"  [전투로그] {blPath}");
    }

    // 반격 기여 = 25% 평균 − 0회 보장. 금요일만 ≠0. 양수=반격이 점수↑(3초 시간경과로 아군 쿨 동반감소),
    //   음수=반격이 점수↓(아군 사망 손실 우세). 0회 보장(ceiling0)은 반격 0회 발동 시의 견고한 바닥.
    double counterDelta = score25 - ceiling0;

    // ── JSON ──
    var jsonObj = new
    {
        day,
        boss = stage.Name,
        score = Math.Round(res.BestScore),
        autoRotationScore = Math.Round(res.AutoRotationScore),
        formation = res.BestFormation,
        backRow = res.BestBackRow,
        roundsCleared = res.BestResult?.RoundsCleared ?? 0,
        totalTurns = res.BestResult?.TotalTurns ?? 0,
        roundScore = (res.BestResult?.RoundScore ?? new()).OrderBy(k => k.Key)
            .ToDictionary(k => k.Key.ToString(), k => Math.Round(k.Value)),
        team = res.BestParty.Select((b, i) => new
        {
            id = b.Character.Id,
            name = b.Character.Name,
            role = b.Character.Type,
            transcend = b.TranscendLevel,
            isBackRow = res.BestBackRow?.Contains(b.Character.Name) ?? false,
            totalDamage = Math.Round(party.FirstOrDefault(c => c.CharacterName == b.Character.Name)?.TotalDamage ?? 0),
            damageShare = Math.Round(party.FirstOrDefault(c => c.CharacterName == b.Character.Name)?.DamageShare ?? 0, 1),
        }),
        gear = res.GearLog,
        skillOrder,
        feasibility = new
        {
            ok = feasOk,
            fallbacks = feasFallbacks,
            unreached = feasUnreached,
            minSlackSec = double.IsNaN(feasMinSlack) ? (double?)null : Math.Round(feasMinSlack, 1),
            scoreReproduced = feasScoreMatch,
            // 반격 모델: 폴백/쿨여유는 0%(빔 베이스라인), 점수는 25%(실전). 금요일만 두 값이 갈림.
            score25 = Math.Round(score25),          // 25% 평균 점수(= 헤드라인 score)
            floor0 = Math.Round(ceiling0),          // 0회 발동 보장 점수(견고한 바닥)
            counterattackDelta = Math.Round(counterDelta),   // 25% 평균 − 0회 보장 (양수=반격이 점수↑)
            steps = feas.Select(f => new
            {
                step = f.StepIndex + 1,
                turn = f.Turn,
                elapsedSec = Math.Round(f.Elapsed, 0),
                hero = f.HeroName,
                skill = f.SkillName,
                status = f.Hold ? "hold" : !f.Reached ? "unreached" : f.ExecutedAsPlanned ? "ok" : "fallback",
                firstUse = f.ExecutedAsPlanned && !f.Hold && !f.CooldownGated,
                slackSec = f.ExecutedAsPlanned && !f.Hold && f.CooldownGated ? Math.Round(f.Slack, 1) : (double?)null,
                reason = f.ExecutedAsPlanned ? null : f.FallbackReason,
                cooldownRemainingSec = !f.ExecutedAsPlanned && f.CooldownRemaining > 0 ? Math.Round(f.CooldownRemaining, 1) : (double?)null,
                buffTargets = f.BuffTargets,       // 아군 버프 수령자(예: 비스킷 장비강화 → [타카,라이언])
                debuffTargets = f.DebuffTargets,   // 적 디버프 대상(예: 레이첼 불새 → [스파이크,룩,챈슬러])
                dispelTargets = f.DispelTargets,   // 버프해제된 적(예: 비스킷 리프어택 → [스파이크])
            }),
            deaths = feasRes.Deaths.Select(d => new
            {
                turn = d.Turn, elapsedSec = Math.Round(d.Elapsed, 0), ally = d.AllyName, cause = d.Cause,
            }),
        },
    };
    string jsonPath = System.IO.Path.Combine(outDir, $"siege_{day}_{SUFFIX}.json");
    System.IO.File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonObj, jsonOpts), Encoding.UTF8);

    // ── TXT ──
    var sb = new StringBuilder();
    string specDesc = isLowSpec
        ? $"{TRANS}초월·잠재0·전용X·권능반지X·펫{petName}76"
        : $"{TRANS}초월·잠재{POT}·전용전설·권능반지O·펫{petName}76";
    sb.AppendLine($"════════ {day} 공성전 — {specDesc} / 진형·기어·전용조율·스킬순서 탐색 ════════");
    sb.AppendLine($"보스: {stage.Name}");
    sb.AppendLine($"팀: {string.Join(", ", nm)}");
    sb.AppendLine($"총점: {res.BestScore:N0}   [자동로테 {res.AutoRotationScore:N0} → 빔 {res.BestScore:N0}]");
    sb.AppendLine($"진형: {res.BestFormation} / 후열: {string.Join(",", res.BestBackRow)} / 탐색 {sw.ElapsedMilliseconds / 1000.0:F0}s");
    // 자리(진형 번호 1~5) — 행동순서 = 속공 내림차순, 동속공이면 자리 낮은 순. (라운드 시작 선공도 이 순서)
    sb.AppendLine($"자리: {string.Join(" · ", res.BestParty.Select((p, i) => $"{i + 1}.{p.Character?.Name}({(p.IsBackPosition ? "후열" : "전열")})"))}");
    if (res.BestResult != null)
        sb.AppendLine($"라운드별: {string.Join(", ", res.BestResult.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
    sb.AppendLine("\n──── 캐릭터별 기여 ────");
    foreach (var c in party) sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
    sb.AppendLine("\n──── 자동 장착 기어 ────");
    foreach (var g in res.GearLog) sb.AppendLine("  " + g);
    sb.AppendLine("\n──── 최적 스킬 순서 + 실행가능성 ────");
    sb.AppendLine($"  실행가능성(반격 0% 기준): {(feasOk ? "가능 ✓" : "불가 ✗")}  (폴백 {feasFallbacks} · 미도달 {feasUnreached} · 최소 쿨여유 {feasMinSlackStr} · 점수재현 {(feasScoreMatch ? "일치" : $"불일치!{onReplayScore:N0}")})");
    if (Math.Abs(counterDelta) > 1.0)
        sb.AppendLine($"  반격 모델: 25% 평균 {score25:N0}  ·  0회 보장 {ceiling0:N0}  ·  반격 기여 {counterDelta:+#,0;-#,0}");
    if (replaySeeds > 1)
        sb.AppendLine($"  사망 분포({replaySeeds}시드): 평균 {avgDeaths:F2}명 · 사망발생 {seedsWithDeath}/{replaySeeds}시드 · 시드별 [{string.Join(",", deathCounts)}]");
    foreach (var f in feas)
    {
        string status = f.Hold ? "홀드"
            : !f.Reached ? "✗ 미도달"
            : f.IsAuto ? "○ 자동"
            : f.ExecutedAsPlanned ? (f.CooldownGated ? $"✓ 여유 {f.Slack,5:F1}s" : "✓ 첫시전")
            : $"✗ 폴백({f.FallbackReason})";
        string when = (f.Reached && !f.Hold) ? $"T{f.Turn,2} {f.Elapsed,4:F0}s  " : "            ";
        // 적용 대상 표기 (비스킷 버프 로그처럼): 버프[수령아군]·디버프[대상적]·해제[버프해제된적]
        var anno = new List<string>();
        if (f.BuffTargets != null && f.BuffTargets.Count > 0) anno.Add($"버프[{string.Join(",", f.BuffTargets)}]");
        if (f.DebuffTargets != null && f.DebuffTargets.Count > 0) anno.Add($"디버프[{string.Join(",", f.DebuffTargets)}]");
        if (f.DispelTargets != null && f.DispelTargets.Count > 0) anno.Add($"해제[{string.Join(",", f.DispelTargets)}]");
        string annoStr = anno.Count > 0 ? " " + string.Join(" ", anno) : "";
        string act = f.Hold ? "(홀드)" : $"{f.HeroName} → {f.SkillName}{annoStr}";
        sb.AppendLine($"  {f.StepIndex + 1,2}. {when}{act,-52} [{status}]");
    }
    // ── 아군 사망 로그 (실행가능성 0% 기준 재생) ──
    if (feasRes.Deaths != null && feasRes.Deaths.Count > 0)
    {
        sb.AppendLine("\n──── 아군 사망 로그 ────");
        foreach (var d in feasRes.Deaths)
            sb.AppendLine($"  T{d.Turn,2} {d.Elapsed,4:F0}s  {d.AllyName} 사망 ← {d.Cause}");
    }
    string txtPath = System.IO.Path.Combine(outDir, $"siege_{day}_{SUFFIX}.txt");
    System.IO.File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);

    string deathDist = replaySeeds > 1 ? $" · 사망 평균 {avgDeaths:F2}({seedsWithDeath}/{replaySeeds}시드)" : "";
    Console.WriteLine($"[{day}] 총점 {res.BestScore:N0} · {res.BestFormation} · 실행가능 {(feasOk ? "✓" : $"✗(폴백{feasFallbacks}/미도달{feasUnreached})")} 최소여유 {feasMinSlackStr}{deathDist} · {sw.ElapsedMilliseconds / 1000.0:F0}s → JSON+TXT 저장");
}

Console.WriteLine("=== 전체 완료 ===");

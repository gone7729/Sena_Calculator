using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 월~토(일 제외) 요일별 팀 고정 + 진형·기어·스킬순서 탐색 → JSON + TXT 저장.
//   전원 6초월·잠재 0/0/0·스킬강화 / 펫 윈디 6성 강화+3·펫잠재 모공%72(18×4) / 전용무기 없음.
//   탐색: 진형 3종(기본/밸런스/보호)×자리 + AutoEquip(세트/메인/부옵/장신구) + 빔 로테이션.
// ============================================================================

// 요일별 팀 (영웅 id). 월요일 5번째 = 지크(303). (대안: 에반 453)
var DAYS = new (string Day, int[] Ids)[]
{
    ("월요일", new[] { 118, 103, 203, 201, 303 }),   // 나타·미호·오를리·비스킷·지크
    ("화요일", new[] { 118, 103, 202, 201, 255 }),   // 나타·미호·리나·비스킷·클로에
    ("수요일", new[] { 118, 103, 202, 201, 2   }),   // 나타·미호·리나·비스킷·라이언
    ("목요일", new[] { 2,   1,   301, 201, 15  }),   // 라이언·타카·레이첼·비스킷·돼오
    ("금요일", new[] { 2,   1,   301, 201, 303 }),   // 라이언·타카·레이첼·비스킷·지크
    ("토요일", new[] { 2,   1,   301, 201, 51  }),   // 라이언·타카·레이첼·비스킷·풍연
};

// 인자로 요일 지정 시 해당 요일만 탐색 (예: dotnet run -- 수요일). 미지정이면 전 요일.
if (args.Length > 0)
    DAYS = DAYS.Where(d => args.Contains(d.Day)).ToArray();

static BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;   // 전용무기 없음
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = 6,
        PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
        Equipment = null,       // AutoEquip이 기어 탐색
    };
}

string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
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
        AllyPet = PetDb.GetByName("윈디"),
        PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
        MaxTurns = 70,
        AutoEquip = true,
        SearchExclusiveWeapon = false,
        OptimizeRotation = true,
        RotationBeamWidth = 10,
        RotationMaxDepth = 28,
        // 진형·자리 전체 탐색 (Forced* 미지정)
    };

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var res = new SiegeOptimizer().Optimize(cfg);
    sw.Stop();

    var nm = res.BestParty.Select(b => b.Character.Name).ToList();
    string SkNm(int hi, SkillType st) =>
        res.BestParty[hi].Character.Skills?.FirstOrDefault(s => s.SkillType == st)?.Name ?? st.ToString();

    var party = (res.BestResult?.CharacterResults ?? new()).OrderByDescending(c => c.TotalDamage).ToList();
    var skillOrder = (res.BestRotationPlan ?? new()).Select((d, i) => new
    {
        step = i + 1,
        hero = d.Hold ? "(홀드)" : nm[d.HeroIndex],
        skill = d.Hold ? "" : SkNm(d.HeroIndex, d.Skill),
    }).ToList();

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
    };
    string jsonPath = System.IO.Path.Combine(repoRoot, $"siege_{day}_6초월잠재0.json");
    System.IO.File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonObj, jsonOpts), Encoding.UTF8);

    // ── TXT ──
    var sb = new StringBuilder();
    sb.AppendLine($"════════ {day} 공성전 — 6초월·잠재0·전용없음 / 진형·기어·스킬순서 탐색 ════════");
    sb.AppendLine($"보스: {stage.Name}");
    sb.AppendLine($"팀: {string.Join(", ", nm)}");
    sb.AppendLine($"총점: {res.BestScore:N0}   [자동로테 {res.AutoRotationScore:N0} → 빔 {res.BestScore:N0}]");
    sb.AppendLine($"진형: {res.BestFormation} / 후열: {string.Join(",", res.BestBackRow)} / 탐색 {sw.ElapsedMilliseconds / 1000.0:F0}s");
    if (res.BestResult != null)
        sb.AppendLine($"라운드별: {string.Join(", ", res.BestResult.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
    sb.AppendLine("\n──── 캐릭터별 기여 ────");
    foreach (var c in party) sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
    sb.AppendLine("\n──── 자동 장착 기어 ────");
    foreach (var g in res.GearLog) sb.AppendLine("  " + g);
    sb.AppendLine("\n──── 최적 스킬 순서 ────");
    foreach (var s in skillOrder) sb.AppendLine($"  {s.step,2}. " + (string.IsNullOrEmpty(s.skill) ? "(홀드)" : $"{s.hero} → {s.skill}"));
    string txtPath = System.IO.Path.Combine(repoRoot, $"siege_{day}_6초월잠재0.txt");
    System.IO.File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);

    Console.WriteLine($"[{day}] 총점 {res.BestScore:N0} · {res.BestFormation} · {sw.ElapsedMilliseconds / 1000.0:F0}s → JSON+TXT 저장");
}

Console.WriteLine("=== 전체 완료 ===");

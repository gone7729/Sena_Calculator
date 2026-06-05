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
//   기본 6초월·잠재0/0/0, 인자 "12초월잠재3" 지정 시 12초월·잠재3/3/3. 스킬강화 / 펫 윈디 6성 강화+3·
//   펫잠재 모공%72(18×4) / 전용무기 없음은 공통. 출력 파일은 프로필 접미사로 분리(siege_{요일}_{프로필}).
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

// 프로필: 인자에 "12초월잠재3" 포함 시 12초월·잠재3/3/3, 아니면 기본 6초월·잠재0.
//   (펫 윈디 잠재 모공%72=18×4·전용무기X·스킬강화o는 두 프로필 공통.)
bool hiProfile = args.Contains("12초월잠재3");
int TRANS = hiProfile ? 12 : 6;
int POT = hiProfile ? 3 : 0;
string SUFFIX = hiProfile ? "12초월잠재3" : "6초월잠재0";

// 인자로 요일 지정 시 해당 요일만 탐색 (예: dotnet run -- 수요일). 미지정이면 전 요일. (프로필 토큰은 요일 아님 → 무시)
var dayArgs = args.Where(a => DAYS.Any(d => d.Day == a)).ToArray();
if (dayArgs.Length > 0)
    DAYS = DAYS.Where(d => dayArgs.Contains(d.Day)).ToArray();

BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;   // 전용무기 없음
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = TRANS,
        PotentialAtkLevel = POT, PotentialDefLevel = POT, PotentialHpLevel = POT,
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
    int feasFallbacks = feas.Count(f => !f.ExecutedAsPlanned && !f.Hold && f.Reached);
    int feasUnreached = feas.Count(f => !f.Reached);
    // 최소 쿨여유 = 재시전(쿨 제약 받은 캐스트)만 — 첫 시전(쿨 무관)은 견고성 지표에서 제외.
    double feasMinSlack = feas.Where(f => f.ExecutedAsPlanned && !f.Hold && f.CooldownGated)
        .Select(f => f.Slack).DefaultIfEmpty(double.NaN).Min();
    string feasMinSlackStr = double.IsNaN(feasMinSlack) ? "n/a(재시전 없음)" : $"{feasMinSlack:F1}s";
    double ceiling0 = feasRes.TotalScore;   // 0% 천장(반격 0회 발동 시 점수)
    double score25 = res.BestScore;         // 25% 실전 점수(최종, 단일 시드)
    // 점수 재현: 최종 플랜을 반격 ON으로 재생해 res.BestScore와 일치하는지(플랜→점수 정합성).
    var onReplay = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
    {
        AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
        AllyPet = cfg.AllyPet, PetStar = cfg.PetStar, PetEnhance = cfg.PetEnhance,
        PetOptionAtkRate = cfg.PetOptionAtkRate, PetOptionDefRate = cfg.PetOptionDefRate,
        PetOptionHpRate = cfg.PetOptionHpRate, MaxTurns = cfg.MaxTurns,
        RotationPlan = res.BestRotationPlan,   // 반격 ON(기본)
    });
    bool feasScoreMatch = Math.Abs(onReplay.TotalScore - res.BestScore) < 1.0;
    bool feasOk = feasFallbacks == 0 && feasUnreached == 0;

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
            }),
        },
    };
    string jsonPath = System.IO.Path.Combine(repoRoot, $"siege_{day}_{SUFFIX}.json");
    System.IO.File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonObj, jsonOpts), Encoding.UTF8);

    // ── TXT ──
    var sb = new StringBuilder();
    sb.AppendLine($"════════ {day} 공성전 — {TRANS}초월·잠재{POT}·전용없음 / 진형·기어·스킬순서 탐색 ════════");
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
    sb.AppendLine("\n──── 최적 스킬 순서 + 실행가능성 ────");
    sb.AppendLine($"  실행가능성(반격 0% 기준): {(feasOk ? "가능 ✓" : "불가 ✗")}  (폴백 {feasFallbacks} · 미도달 {feasUnreached} · 최소 쿨여유 {feasMinSlackStr} · 점수재현 {(feasScoreMatch ? "일치" : $"불일치!{onReplay.TotalScore:N0}")})");
    if (Math.Abs(counterDelta) > 1.0)
        sb.AppendLine($"  반격 모델: 25% 평균 {score25:N0}  ·  0회 보장 {ceiling0:N0}  ·  반격 기여 {counterDelta:+#,0;-#,0}");
    foreach (var f in feas)
    {
        string status = f.Hold ? "홀드"
            : !f.Reached ? "✗ 미도달"
            : f.ExecutedAsPlanned ? (f.CooldownGated ? $"✓ 여유 {f.Slack,5:F1}s" : "✓ 첫시전")
            : $"✗ 폴백({f.FallbackReason})";
        string when = (f.Reached && !f.Hold) ? $"T{f.Turn,2} {f.Elapsed,4:F0}s  " : "            ";
        string act = f.Hold ? "(홀드)" : $"{f.HeroName} → {f.SkillName}";
        sb.AppendLine($"  {f.StepIndex + 1,2}. {when}{act,-28} [{status}]");
    }
    string txtPath = System.IO.Path.Combine(repoRoot, $"siege_{day}_{SUFFIX}.txt");
    System.IO.File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);

    Console.WriteLine($"[{day}] 총점 {res.BestScore:N0} · {res.BestFormation} · 실행가능 {(feasOk ? "✓" : $"✗(폴백{feasFallbacks}/미도달{feasUnreached})")} 최소여유 {feasMinSlackStr} · {sw.ElapsedMilliseconds / 1000.0:F0}s → JSON+TXT 저장");
}

Console.WriteLine("=== 전체 완료 ===");

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 요일별 공성전 풀시뮬 로그 (나타 투입 테스트). 각 요일 후보풀에서 최적 5인+진형+로테이션 탐색 → 로그 저장.
// 펫: 윈디 6성 강화3 / 펫옵 72 (기본값 — 팀에 맞게 조정 가능).

Console.WriteLine("사용 가능한 시즈 요일: " + string.Join(", ", EnemyDb.SiegeStages.Keys));

var pools = new (string Day, string[] Heroes)[]
{
    ("월요일", new[] { "나타", "루리", "미호", "오를리", "비스킷", "리나", "지크", "에반" }),
    ("화요일", new[] { "나타", "루리", "미호", "오를리", "비스킷", "리나", "클로에" }),
    ("수요일", new[] { "나타", "루리", "미호", "오를리", "비스킷", "리나", "클로에", "라이언", "아리엘" }),
};

foreach (var (day, heroes) in pools)
{
    Console.WriteLine($"\n========== {day} ==========");
    if (!EnemyDb.SiegeStages.TryGetValue(day, out var stage))
    {
        Console.WriteLine($"[skip] {day} 시즈 데이터 없음");
        continue;
    }

    var candidates = new List<BattleCharacter>();
    var missing = new List<string>();
    foreach (var name in heroes)
    {
        var ch = CharacterDb.Characters.FirstOrDefault(c => c.Name == name);
        if (ch == null) { missing.Add(name); continue; }
        candidates.Add(new BattleCharacter { Character = ch, IsSkillEnhanced = true, TranscendLevel = 12 });
    }
    if (missing.Count > 0) Console.WriteLine($"[경고] 미구현 영웅 제외: {string.Join(", ", missing)}");
    if (candidates.Count < 5) { Console.WriteLine($"[skip] 후보 부족({candidates.Count}명)"); continue; }

    var config = new SiegeOptimizerConfig
    {
        Candidates = candidates,
        SiegeStage = stage,
        MaxTurns = 70,
        AutoEquip = true,
        AllyPet = PetDb.GetByName("윈디"),
        PetStar = 6,
        PetEnhance = 3,
        PetOptionAtkRate = 72,
    };

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var result = new SiegeOptimizer().Optimize(config);
    sw.Stop();
    var r = result.BestResult;

    var sb = new StringBuilder();
    sb.AppendLine($"================ {day} 공성전 풀시뮬 (나타 투입 테스트) ================");
    sb.AppendLine($"보스: {stage.Name}");
    sb.AppendLine($"후보풀: {string.Join(", ", heroes)}");
    sb.AppendLine("세팅: 전원 12초월·스킬강화 / 펫 윈디 6성 강화3 / 펫옵 72 / AutoEquip");
    sb.AppendLine($"선택 팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))}");
    sb.AppendLine($"진형: {result.BestFormation} / 후열: {(result.BestBackRow.Count > 0 ? string.Join(",", result.BestBackRow) : "없음")}");
    sb.AppendLine($"총점: {result.BestScore:N0}  (도달턴 {r?.TotalTurns}, 클리어라운드 {r?.RoundsCleared}, 탐색 {result.EvaluatedCount}회, {sw.ElapsedMilliseconds:N0}ms)");
    if (r != null)
        sb.AppendLine($"라운드별: {string.Join(", ", r.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
    sb.AppendLine("\n캐릭터별 기여:");
    foreach (var c in (r?.CharacterResults ?? new()).OrderByDescending(c => c.TotalDamage))
        sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
    sb.AppendLine("\n자동 장착 장비:");
    foreach (var line in result.GearLog ?? new()) sb.AppendLine(line);
    sb.AppendLine($"\n턴별 행동 로그 (총 {(r?.TurnLogs.Count ?? 0)}개):");
    foreach (var log in r?.TurnLogs ?? new())
        sb.AppendLine($"T{log.Turn,2} [{(log.IsAlly ? "아군" : "적 ")}] {log.SkillName}: {log.Description}");

    string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", $"siege_{day}_나타테스트.txt"));
    System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
    Console.WriteLine($"선택팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))} / 총점 {result.BestScore:N0} → {outPath}");
}
Console.WriteLine("\n완료.");

using System;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 공성전 시뮬/옵티마이저 동작 검증용 콘솔 (빠른 실행). 샘플 팀으로 토요일 공성전 탐색.
var candidates = CharacterDb.Characters.Take(6)
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 6 })
    .ToList();
Console.WriteLine($"후보 {candidates.Count}명: {string.Join(", ", candidates.Select(c => c.Character.Name))}");

var stage = EnemyDb.SiegeStages.TryGetValue("토요일", out var s) ? s : null;
Console.WriteLine($"토요일 Stage: {stage?.Name ?? "없음"} (라운드 {stage?.Waves.Count ?? 0})");

var config = new SiegeOptimizerConfig { Candidates = candidates, SiegeStage = stage, MaxTurns = 70 };
var sw = System.Diagnostics.Stopwatch.StartNew();
var result = new SiegeOptimizer().Optimize(config);
sw.Stop();

Console.WriteLine($"\n=== 탐색 결과 ===");
Console.WriteLine($"평가 {result.EvaluatedCount}회 ({sw.ElapsedMilliseconds}ms)");
Console.WriteLine($"최고점수: {result.BestScore:N0}");
Console.WriteLine($"진형: {result.BestFormation}");
Console.WriteLine($"팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))}");
if (result.BestResult != null)
{
    var r = result.BestResult;
    Console.WriteLine($"라운드별: {string.Join(", ", r.RoundScore.OrderBy(kv => kv.Key).Select(kv => $"R{kv.Key}={kv.Value:N0}"))}");
    Console.WriteLine($"캐릭별: {string.Join(", ", r.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName}={c.TotalDamage:N0}({c.DamageShare:F0}%)"))}");
    Console.WriteLine($"도달턴 {r.TotalTurns}, 클리어 라운드 {r.RoundsCleared}, 턴로그 {r.TurnLogs.Count}개");
    Console.WriteLine("\n[턴로그 앞 12개]");
    foreach (var log in r.TurnLogs.Take(12))
        Console.WriteLine($"  T{log.Turn} {(log.IsAlly ? "아군" : "적")} {log.SkillName}: {log.Description}");
}

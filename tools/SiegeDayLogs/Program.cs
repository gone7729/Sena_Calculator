using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 화요일 실측 비교 (공성전_화요일.xlsx 기반). 실측 총점 6,862,365 (R1 25,170 + R2 31,450 + R3 6,805,745).
// 멤버·세팅은 시트별 정리값.

Console.WriteLine("사용 가능한 시즈 요일: " + string.Join(", ", EnemyDb.SiegeStages.Keys));

const string DAY = "화요일";
// (이름, 초월, 잠재공, 잠재방, 잠재생). 스킬강화 전부 o.
var teamSpec = new (string Name, int Trans, int Pa, int Pd, int Ph)[]
{
    ("나타",   4, 1, 1, 1),
    ("미호",   9, 3, 3, 3),
    ("비스킷", 9, 0, 0, 0),
    ("클로에", 12, 0, 0, 0),
    ("리나",   10, 0, 0, 0),
};

if (!EnemyDb.SiegeStages.TryGetValue(DAY, out var stage))
{
    Console.WriteLine($"[skip] {DAY} 시즈 데이터 없음"); return;
}

var candidates = new List<BattleCharacter>();
foreach (var (name, t, pa, pd, ph) in teamSpec)
{
    var ch = CharacterDb.Characters.FirstOrDefault(c => c.Name == name);
    if (ch == null) { Console.WriteLine($"[경고] 미구현: {name}"); continue; }
    // 화요일 실측: 나타만 공용 전용무기 (조율 없음, 마공 247) 장착. 나머지 4명은 미장착.
    if (name == "나타")
        ch.ExclusiveWeapon = ExclusiveWeaponDb.Universal(magic: true);
    else
        ch.ExclusiveWeapon = null;
    candidates.Add(new BattleCharacter
    {
        Character = ch,
        IsSkillEnhanced = true,
        TranscendLevel = t,
        PotentialAtkLevel = pa,
        PotentialDefLevel = pd,
        PotentialHpLevel = ph,
    });
}
if (candidates.Count < 5) { Console.WriteLine("[skip] 후보 부족"); return; }

// 실측 강제: 진형=밸런스, 후열=미호·나타, 클로에=수문장 / 리나=성기사 셋업 고정.
var forcedSets = new Dictionary<int, string>
{
    [255] = "수문장", // 클로에
    [202] = "성기사", // 리나
};

// 실측 메인옵 강제 (탱커/힐러는 HP/방어 우선 — 옵티마이저는 default가 딜러용이라 잘못 박는다).
var forcedMains = new Dictionary<int, (string WeaponMain, string ArmorMain)>
{
    [255] = ("방어력%", "막기확률%"),  // 클로에: 무기 방어력% / 방어구 막기%
    [202] = ("생명력%", "생명력%"),    // 리나: 모두 생명력%
};

var config = new SiegeOptimizerConfig
{
    Candidates = candidates,
    SiegeStage = stage,
    MaxTurns = 70,
    AutoEquip = true,
    RotationBeamWidth = 30,
    RotationMaxDepth = 40,
    AllyPet = PetDb.GetByName("윈디"),
    PetStar = 6,
    PetEnhance = 3,
    PetOptionAtkRate = 72,
    ForcedFormation = "밸런스 진형",
    ForcedBackRow = new List<string> { "미호", "나타" },
    ForcedSetByCharId = forcedSets,
    ForcedMainByCharId = forcedMains,
};

var sw = System.Diagnostics.Stopwatch.StartNew();
var result = new SiegeOptimizer().Optimize(config);
sw.Stop();
var r = result.BestResult;
const double REAL_SCORE = 6_862_365.0;

var sb = new StringBuilder();
sb.AppendLine($"================ {DAY} 공성전 풀시뮬 (실측 비교) ================");
sb.AppendLine($"보스: {stage.Name}");
sb.AppendLine($"멤버 세팅: " + string.Join(" | ", teamSpec.Select(s => $"{s.Name}(T{s.Trans}/P{s.Pa}{s.Pd}{s.Ph})")));
sb.AppendLine($"실측 총점: {REAL_SCORE:N0}  (R1=25,170 R2=31,450 R3=6,805,745)");
sb.AppendLine();
sb.AppendLine($"선택 팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))}");
sb.AppendLine($"진형: {result.BestFormation} / 후열: {(result.BestBackRow.Count > 0 ? string.Join(",", result.BestBackRow) : "없음")}");
sb.AppendLine($"시뮬 총점: {result.BestScore:N0}  ({result.BestScore/REAL_SCORE*100:F1}%)  (도달턴 {r?.TotalTurns}, 클리어 R{r?.RoundsCleared}, 탐색 {result.EvaluatedCount}, {sw.ElapsedMilliseconds:N0}ms)");
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
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", $"siege_{DAY}_실측비교.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"시뮬 총점 {result.BestScore:N0} ({result.BestScore/REAL_SCORE*100:F1}% of 실측) → {outPath}");
Console.WriteLine("\n완료.");

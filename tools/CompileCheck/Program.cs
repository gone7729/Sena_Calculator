using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// [진단] 수요일 빔 최적 플랜의 나타 혼천릉파 per-hit 분해. 실측 494k 대비 어디서 부족한가.
const string DAY = "수요일";
int[] teamIds = { 118, 103, 201, 202, 2 };   // 나타, 미호, 비스킷, 리나, 라이언
var team = teamIds
    .Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 12,
        PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3 })
    .ToList();

var optCfg = new SiegeOptimizerConfig
{
    Candidates = team, SiegeStage = EnemyDb.SiegeStages[DAY], MaxTurns = 70,
    AutoEquip = true, RotationBeamWidth = 30, RotationMaxDepth = 40,
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
};
var result = new SiegeOptimizer().Optimize(optCfg);
var r = result.BestResult;

var sb = new StringBuilder();
sb.AppendLine($"================ 수요일 빔 최적 플랜: 나타 혼천릉파 per-hit 분해 ================");
sb.AppendLine($"총점 {result.BestScore:N0} / 실측 10,659,698 대비 {result.BestScore/10659698.0*100:F1}%");
sb.AppendLine($"라운드: {string.Join(", ", r.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))}");
sb.AppendLine($"진형: {result.BestFormation}/후열: {string.Join(",", result.BestBackRow)}");
foreach (var c in r.CharacterResults.OrderByDescending(c=>c.TotalDamage))
    sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");

// 빔 최적 플랜 재생 + 나타 혼천 진단
var diagSim = new SiegeBattleSimulator(777) { DiagSkillName = "혼천릉파" };
var diagCfg = new SiegeBattleConfig
{
    AllyParty = result.BestParty, FormationName = result.BestFormation,
    SiegeStage = EnemyDb.SiegeStages[DAY],
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, RotationPlan = result.BestRotationPlan,
};
diagSim.Simulate(diagCfg);
sb.AppendLine("\n[나타 혼천릉파 per-hit 분해]");
sb.AppendLine(diagSim.DiagLog.ToString());
sb.AppendLine("\n실측 영상 비교: ~494k/타격 (3타겟 ×3) (t=220 프레임)");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_수요일_혼천perhit.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"DONE 총점 {result.BestScore:N0} → {outPath}");

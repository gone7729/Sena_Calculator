using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// [수요일 진단] 빔 plan vs 강제 ST0 변형 A/B 비교
// 빔 plan ST0=라이언 강자사냥(물리 6k, R1 못 클리어) → 라운드 연쇄 손실 의심.
// 강제 plan: ST0=나타 혼천릉파(마법 ~200k, R1 즉시 전멸) → R2/R3 0턴 연쇄 기대.

const string DAY = "수요일";
int[] teamIds = { 118, 103, 201, 202, 2 };   // 나타, 미호, 비스킷, 리나, 라이언 (SiegeDayLogs 순서)
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
var beamScore = result.BestScore;
var beamR = result.BestResult;

// 캐릭터 인덱스 (BestParty 순서)
int idxNata = result.BestParty.FindIndex(c => c.Character.Name == "나타");
int idxMiho = result.BestParty.FindIndex(c => c.Character.Name == "미호");
int idxBis  = result.BestParty.FindIndex(c => c.Character.Name == "비스킷");
int idxLina = result.BestParty.FindIndex(c => c.Character.Name == "리나");
int idxRyan = result.BestParty.FindIndex(c => c.Character.Name == "라이언");

SiegeBattleConfig MakeCfg(List<RotationDecision> plan) => new SiegeBattleConfig
{
    AllyParty = result.BestParty, FormationName = result.BestFormation,
    SiegeStage = EnemyDb.SiegeStages[DAY],
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, RotationPlan = plan,
};

// 후보 plan들
var plans = new List<(string Name, List<RotationDecision> Plan)>
{
    ("A. 빔 plan (baseline)", result.BestRotationPlan),
    ("B. ST0=나타 혼천릉파 (R1 강타) + 나머지 auto",
        new List<RotationDecision> { new() { HeroIndex = idxNata, Skill = SkillType.Skill2 } }),
    ("C. ST0=나타 혼천, ST1=미호 살육의춤 (R2 연쇄 기대)",
        new List<RotationDecision> {
            new() { HeroIndex = idxNata, Skill = SkillType.Skill2 },
            new() { HeroIndex = idxMiho, Skill = SkillType.Skill1 },
        }),
    ("D. ST0=나타 혼천, ST1=나타 화첨, ST2=미호 살육의춤 (강AoE 3연타)",
        new List<RotationDecision> {
            new() { HeroIndex = idxNata, Skill = SkillType.Skill2 },
            new() { HeroIndex = idxNata, Skill = SkillType.Skill1 },
            new() { HeroIndex = idxMiho, Skill = SkillType.Skill1 },
        }),
    ("E. 실측 추정 사이클: 비스킷강화→리나울림→미호취약→나타혼천→나타화첨 반복",
        BuildRealCycle(idxBis, idxLina, idxMiho, idxNata)),
};

var sb = new StringBuilder();
sb.AppendLine("================ 수요일 빔 vs 강제 plan A/B 비교 ================");
sb.AppendLine($"팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))}");
sb.AppendLine($"진형: {result.BestFormation} / 후열: {string.Join(",", result.BestBackRow)}");
sb.AppendLine($"빔 plan ST0: {result.BestParty[result.BestRotationPlan[0].HeroIndex].Character.Name} {result.BestRotationPlan[0].Skill}");
sb.AppendLine();

foreach (var (name, plan) in plans)
{
    var rr = new SiegeBattleSimulator(777).Simulate(MakeCfg(plan));
    int r1Turn = rr.TurnLogs.FirstOrDefault(l => l.Description != null && l.Description.Contains("R1 클리어"))?.Turn ?? -1;
    int r2Turn = rr.TurnLogs.FirstOrDefault(l => l.Description != null && l.Description.Contains("R2 클리어"))?.Turn ?? -1;
    int deaths = rr.TurnLogs.Count(l => l.Description != null && l.Description.Contains("(사망)"));
    sb.AppendLine($"{name}");
    sb.AppendLine($"  총점 {rr.TotalScore:N0}  | R1클리어 T{r1Turn} R2클리어 T{r2Turn} | 사망 {deaths}명 | 라운드: {string.Join(", ", rr.RoundScore.OrderBy(k=>k.Key).Select(k=>$"R{k.Key}={k.Value:N0}"))}");
    sb.AppendLine($"  vs 빔: {(rr.TotalScore - beamScore):+#,0;-#,0;0} ({(rr.TotalScore/beamScore-1)*100:+0.0;-0.0;0.0}%)");
    sb.AppendLine();
}

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_수요일_AB.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"DONE 빔={beamScore:N0} → {outPath}");
foreach (var (name, plan) in plans)
{
    var rr = new SiegeBattleSimulator(777).Simulate(MakeCfg(plan));
    Console.WriteLine($"  {name}: {rr.TotalScore:N0}");
}

static List<RotationDecision> BuildRealCycle(int bis, int lina, int miho, int nata)
{
    var p = new List<RotationDecision>();
    // 5회 사이클
    for (int i = 0; i < 5; i++)
    {
        p.Add(new() { HeroIndex = bis,  Skill = SkillType.Skill2 });   // 비스킷 장비강화
        p.Add(new() { HeroIndex = lina, Skill = SkillType.Skill2 });   // 리나 따뜻한울림
        p.Add(new() { HeroIndex = miho, Skill = SkillType.Skill1 });   // 미호 살육의춤
        p.Add(new() { HeroIndex = nata, Skill = SkillType.Skill2 });   // 나타 혼천릉파
        p.Add(new() { HeroIndex = nata, Skill = SkillType.Skill1 });   // 나타 화첨창술
    }
    return p;
}

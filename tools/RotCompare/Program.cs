using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 수요일(레이첼) — 유저 실제 로테(20스텝) vs 옵티마이저 로테.
// 유저 빌드 전제: 리나가 "평타 쿨감"을 받는 세팅(리나 공격세트). 시뮬은 DamageWeight 타게팅이라 리나(지원형)가
// 절대 못 받으므로 HighestAtkAllyOrder로 순위를 강제해 비교.
//   파트1: 옵티마이저 기어(밸런스) 위에서 로테만 교체.
//   파트2: 유저 로테·보호진형(나타 후열)에 맞춰 기어를 다시 탐색한 뒤 비교.
Console.OutputEncoding = Encoding.UTF8;
const string DAY = "수요일";
int TR = args.Length > 1 ? int.Parse(args[1]) : 6;   // 초월 (2번째 인자)
Console.WriteLine($"초월 {TR}");
int[] ids = { 118, 103, 202, 201, 2 };  // 나타 미호 리나 비스킷 라이언
List<BattleCharacter> Fresh() => ids.Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = TR }).ToList();

// 유저 로테(원문 그대로): 비스킷2 미호2 비스킷1 나타2 미호1 나타1 리나2 나타2 미호2 나타1 미호1 나타2 비스킷1 나타1 리나2 나타2 미호1 나타1 미호2 나타2
var steps = new (string hero, SkillType skill)[]
{
    ("비스킷", SkillType.Skill2), ("미호", SkillType.Skill2), ("비스킷", SkillType.Skill1), ("나타", SkillType.Skill2),
    ("미호", SkillType.Skill1), ("나타", SkillType.Skill1), ("리나", SkillType.Skill2), ("나타", SkillType.Skill2),
    ("미호", SkillType.Skill2), ("나타", SkillType.Skill1), ("미호", SkillType.Skill1), ("나타", SkillType.Skill2),
    ("비스킷", SkillType.Skill1), ("나타", SkillType.Skill1), ("리나", SkillType.Skill2), ("나타", SkillType.Skill2),
    ("미호", SkillType.Skill1), ("나타", SkillType.Skill1), ("미호", SkillType.Skill2), ("나타", SkillType.Skill2),
};

SiegeOptimizerConfig BaseOpt(List<BattleCharacter> team, bool asCandidates) => new()
{
    FixedMembers = asCandidates ? new List<BattleCharacter>() : team,
    Candidates = asCandidates ? team : new List<BattleCharacter>(),
    SiegeStage = EnemyDb.SiegeStages[DAY], MaxTurns = 70,
    AutoEquip = true, AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76,
    FloorFirstGear = true, OptimizeRotation = true, RotationBeamWidth = 10, RotationMaxDepth = 36,
    AllyDeathPenalty = 1_000_000, EnableSurvivalRings = false, SearchExclusiveWeapon = false,
};

void PrintPlan(string title, List<BattleCharacter> party, List<RotationDecision> plan)
{
    Console.WriteLine($"\n=== {title} ===");
    for (int i = 0; i < plan.Count; i++)
        Console.WriteLine($"  {i,2}: {(plan[i].Hold ? "Hold" : $"{party[plan[i].HeroIndex].Character.Name} {party[plan[i].HeroIndex].Character.Skills.First(s => s.SkillType == plan[i].Skill).Name}")}");
}

bool IMMORTAL = false;   // p6: 아군 불사 진단
void RunAndReport(string label, List<BattleCharacter> party, string formation, List<RotationDecision> plan, List<int> order, bool cdrOnly)
{
    var cfg = new SiegeBattleConfig
    {
        AllyParty = party, FormationName = formation, SiegeStage = EnemyDb.SiegeStages[DAY],
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
        RotationPlan = plan, AllyDeathPenalty = 1_000_000, RecordFeasibility = true,
        HighestAtkAllyOrder = order, HighestAtkAllyOverrideCdrOnly = cdrOnly, DiagAlliesImmortal = IMMORTAL,
    };
    var r = new SiegeBattleSimulator(777).Simulate(cfg);
    Console.WriteLine($"\n[{label}] 총점 {r.TotalScore:N0} · {r.RoundsCleared}R · 경과 {r.ElapsedSeconds:N0}초 · 사망 {r.Deaths.Count}"
        + (r.Deaths.Count > 0 ? " (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")" : ""));
    Console.WriteLine("  라운드: " + string.Join(" / ", r.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key} {k.Value:N0}")));
    Console.WriteLine("  캐릭별: " + string.Join(" / ", r.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName} {c.TotalDamage:N0}")));

    // 평타 쿨감 수령자 집계 (시전자별)
    var cdr = r.TurnLogs.Where(l => (l.Description ?? "").Contains("평타 쿨감")).ToList();
    var bySrc = cdr.GroupBy(l => l.ActorName ?? "?").Select(g =>
    {
        var to = new Dictionary<string, int>();
        foreach (var l in g) foreach (var n in l.Description.Split(':').Last().Split(',').Select(s => s.Trim())) to[n] = to.GetValueOrDefault(n) + 1;
        return $"{g.Key} 평타{g.Count()}회→[{string.Join(",", to.Select(k => $"{k.Key}{k.Value}"))}]";
    });
    Console.WriteLine("  평타쿨감: " + string.Join(" / ", bySrc));

    var casts = r.Feasibility.Where(f => f.Reached && !f.Hold).ToList();
    var byHeroSkill = casts.Where(f => f.ExecutedAsPlanned || f.IsAuto)
        .GroupBy(f => $"{f.HeroName} {f.SkillName}").OrderByDescending(g => g.Count());
    Console.WriteLine("  시전: " + string.Join(" / ", byHeroSkill.Select(g => $"{g.Key}×{g.Count()}")));
    var enh = casts.Where(f => f.SkillName == "장비 강화" && f.BuffTargets != null).Select(f => $"T{f.Turn}[{string.Join(",", f.BuffTargets)}]");
    Console.WriteLine("  장비강화 수령: " + string.Join(" ", enh));

    var bad = r.Feasibility.Where(f => !f.IsAuto && !f.Hold && (!f.Reached || !f.ExecutedAsPlanned)).ToList();
    Console.WriteLine($"  플랜 스텝 {plan.Count} · 폴백 {bad.Count}건" + (bad.Count == 0 ? "" : ":"));
    foreach (var f in bad)
        Console.WriteLine($"    #{f.StepIndex} T{f.Turn} {f.HeroName} {f.SkillName}: {(f.Reached ? f.FallbackReason : "미도달")}");
    int autoN = r.Feasibility.Count(f => f.IsAuto);
    if (autoN > 0) Console.WriteLine($"  플랜 이후 자동 {autoN}스텝: " + string.Join(", ", r.Feasibility.Where(f => f.IsAuto).Select(f => $"{f.HeroName} {f.SkillName}")));
}

// 실행 모드: p1(옵티 기어 위 로테 교체) / p2(유저로테 기준 기어 재탐색) / p3(유저 세트 강제+권능반지 허용) / p4(현행 배치+권능반지 허용)
var mode = args.Length > 0 ? args[0] : "p1";

if (mode == "p5" || mode == "p7")
{
    // p5: p3 기어(유저 세트 강제·유저 로테 기준·보호진형) + 유저 실제 권능반지(리나·비스킷) 수동 장착 → 유저 로테 생존/점수 + 리나 피격 로그.
    var team5 = Fresh();
    int Idx5(string n) => team5.FindIndex(c => c.Character.Name == n);
    var planU5 = steps.Select(s => new RotationDecision { HeroIndex = Idx5(s.hero), Skill = s.skill }).ToList();
    var ordNL5 = new List<int> { Idx5("나타"), Idx5("리나") };
    var c5 = BaseOpt(team5, asCandidates: false);
    c5.OptimizeRotation = false; c5.CoordinateAscentGear = false; c5.EnableSurvivalRings = false;
    c5.ForcedFormation = "보호 진형"; c5.ForcedBackRow = new List<string> { "나타" };
    c5.GearEvalFormation = "보호 진형"; c5.GearEvalBackRow = new List<string> { "나타" };
    c5.GearEvalRotation = planU5; c5.HighestAtkAllyOrder = ordNL5; c5.HighestAtkAllyOverrideCdrOnly = true;
    c5.ForcedSetByCharId = new Dictionary<int, string> { { 202, "선봉장" }, { 2, "수문장" }, { 118, "복수자" }, { 103, "복수자" }, { 201, "복수자" } };
    var opt5 = new SiegeOptimizer().Optimize(c5);
    var party5 = opt5.BestParty;
    foreach (var g in opt5.GearLog.Where(g => g.Contains("세트 ") && !g.Contains("풀시뮬"))) Console.WriteLine("  " + g);
    foreach (var n in new[] { "리나", "비스킷" })
    {
        var bc = party5[party5.FindIndex(c => c.Character.Name == n)];
        var a = bc.Equipment.Accessory;
        bc.Equipment.Accessory = new Accessory { Grade = a.Grade, MainOption = a.MainOption, SubOption = a.SubOption, RingName = "권능의 반지" };
    }
    Console.WriteLine("리나·비스킷 권능의 반지 장착");
    RunAndReport("p5-U4 유저 로테 · 권능(리나·비스킷) · 나타1위·리나2위 쿨감만", party5, "보호 진형", planU5, ordNL5, true);
    RunAndReport("p5-U0 유저 로테 · 권능(리나·비스킷) · 기본 타게팅", party5, "보호 진형", planU5, null, false);

    // 리나 피격/회복 로그 (T0~T40)
    var cfg5 = new SiegeBattleConfig
    {
        AllyParty = party5, FormationName = "보호 진형", SiegeStage = EnemyDb.SiegeStages[DAY],
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
        RotationPlan = planU5, AllyDeathPenalty = 1_000_000, HighestAtkAllyOrder = ordNL5, HighestAtkAllyOverrideCdrOnly = true,
    };
    var sim5 = new SiegeBattleSimulator(777) { DiagAllyStats = true };
    var r5 = sim5.Simulate(cfg5);
    Console.WriteLine("\n--- 아군 스탯 스냅샷 (raw FinalAtk / DamageWeight / MaxHp) ---");
    Console.Write(sim5.DiagLog.ToString());
    Console.WriteLine("\n--- 리나 관련 로그 (피격·보호막·회복·사망) T0~T40 ---");
    foreach (var l in r5.TurnLogs.Where(l => l.Turn <= 40 && (l.Description ?? "").Contains("리나") && !l.IsAlly))
        Console.WriteLine($"  T{l.Turn} [{l.ActorName}/{l.SkillName}] {l.Description}");
    if (mode == "p5") return;
    // p7: 적→아군 전체 피격/회복/보호막/광폭화 로그 + 사망 (생존 모델 검증용)
    Console.WriteLine("\n--- 적→아군 피격·회복·보호막·광폭화 전체 로그 ---");
    foreach (var l in r5.TurnLogs.Where(l => (!l.IsAlly && l.DamageDealt > 0) || (l.Description ?? "").Contains("회복") || (l.SkillName ?? "").Contains("보호막") || (l.SkillName ?? "").Contains("광폭") || (l.Description ?? "").Contains("광폭화") || (l.Description ?? "").Contains("사망")))
        Console.WriteLine($"  T{l.Turn,2} [{l.ActorName}/{l.SkillName}] {l.Description}");
    return;
}

if (mode == "p6")
{
    // p6: 아군 불사(생존 제약 제거) — 유저 세트·보호진형·나타 후열에서 빔 로테 vs 유저 로테의 "순수 로테 품질" 비교.
    IMMORTAL = true;
    var team6 = Fresh();
    int Idx6(string n) => team6.FindIndex(c => c.Character.Name == n);
    var planU6 = steps.Select(s => new RotationDecision { HeroIndex = Idx6(s.hero), Skill = s.skill }).ToList();
    var ordNL6 = new List<int> { Idx6("나타"), Idx6("리나") };
    var c6 = BaseOpt(team6, asCandidates: false);
    c6.CoordinateAscentGear = false; c6.DiagAlliesImmortal = true; c6.EnableSurvivalRings = false;
    c6.ForcedFormation = "보호 진형"; c6.ForcedBackRow = new List<string> { "나타" };
    c6.GearEvalFormation = "보호 진형"; c6.GearEvalBackRow = new List<string> { "나타" };
    c6.GearEvalRotation = planU6; c6.SeedRotations = new List<List<RotationDecision>> { planU6 };
    c6.HighestAtkAllyOrder = ordNL6; c6.HighestAtkAllyOverrideCdrOnly = true;
    c6.ForcedSetByCharId = new Dictionary<int, string> { { 202, "선봉장" }, { 2, "수문장" }, { 118, "복수자" }, { 103, "복수자" }, { 201, "복수자" } };
    var opt6 = new SiegeOptimizer().Optimize(c6);
    var party6 = opt6.BestParty;
    Console.WriteLine($"[{DAY}] p6(불사) 옵티 채택 총점 {opt6.BestScore:N0} · 진형 {opt6.BestFormation}");
    foreach (var g in opt6.GearLog.Where(g => g.Contains("세트 ") && !g.Contains("풀시뮬"))) Console.WriteLine("  " + g);
    var planD = opt6.BestRotationPlan.Select(d => new RotationDecision { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold }).ToList();
    PrintPlan("로테 D (p6 불사 빔)", party6, planD);
    RunAndReport("p6-D  불사 · 빔 로테 · 나타1위·리나2위 쿨감만", party6, "보호 진형", planD, ordNL6, true);
    RunAndReport("p6-U4 불사 · 유저 로테 · 나타1위·리나2위 쿨감만", party6, "보호 진형", planU6, ordNL6, true);
    RunAndReport("p6-U3 불사 · 유저 로테 · 나타1위·리나2위 전부", party6, "보호 진형", planU6, ordNL6, false);
    RunAndReport("p6-U0 불사 · 유저 로테 · 기본 타게팅", party6, "보호 진형", planU6, null, false);
    return;
}

if (mode == "p3" || mode == "p4")
{
    // p3: 유저 세트(리나 선봉장·라이언 수문장·나머지 복수자) 강제 + 권능반지 허용 + 보호진형(나타 후열) + 유저 로테 기준 기어.
    // p4: 현행 배치 설정 그대로 + 권능반지만 허용(6초월 무권능 가정 해제) — 옵티마이저 자율 결과.
    var team3 = Fresh();
    int Idx3(string n) => team3.FindIndex(c => c.Character.Name == n);
    var planU3 = steps.Select(s => new RotationDecision { HeroIndex = Idx3(s.hero), Skill = s.skill }).ToList();
    var ordNL3 = new List<int> { Idx3("나타"), Idx3("리나") };
    var c3 = BaseOpt(team3, asCandidates: mode == "p4");
    c3.EnableSurvivalRings = true;
    if (mode == "p3")
    {
        c3.CoordinateAscentGear = false;
        c3.ForcedFormation = "보호 진형"; c3.ForcedBackRow = new List<string> { "나타" };
        c3.GearEvalFormation = "보호 진형"; c3.GearEvalBackRow = new List<string> { "나타" };
        c3.GearEvalRotation = planU3; c3.SeedRotations = new List<List<RotationDecision>> { planU3 };
        c3.HighestAtkAllyOrder = ordNL3; c3.HighestAtkAllyOverrideCdrOnly = true;
        c3.ForcedSetByCharId = new Dictionary<int, string> { { 202, "선봉장" }, { 2, "수문장" }, { 118, "복수자" }, { 103, "복수자" }, { 201, "복수자" } };
    }
    else c3.CoordinateAscentGear = true;
    var opt3 = new SiegeOptimizer().Optimize(c3);
    var party3 = opt3.BestParty;
    int IdxP3(string n) => party3.FindIndex(c => c.Character.Name == n);
    var planU3b = steps.Select(s => new RotationDecision { HeroIndex = IdxP3(s.hero), Skill = s.skill }).ToList();
    var ordNL3b = new List<int> { IdxP3("나타"), IdxP3("리나") };
    Console.WriteLine($"[{DAY}] {mode} 옵티 채택 총점 {opt3.BestScore:N0} · 진형 {opt3.BestFormation}");
    Console.WriteLine("파티: " + string.Join(" / ", party3.Select((c, i) => $"{i}:{c.Character.Name}({(c.IsBackPosition ? "후" : "전")})")));
    foreach (var g in opt3.GearLog) Console.WriteLine("  " + g);
    var planC = opt3.BestRotationPlan.Select(d => new RotationDecision { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold }).ToList();
    PrintPlan($"로테 ({mode} 옵티 채택)", party3, planC);
    RunAndReport($"{mode}-C  옵티 채택 로테 · 기본 타게팅", party3, opt3.BestFormation, planC, null, false);
    RunAndReport($"{mode}-C' 옵티 채택 로테 · 나타1위·리나2위 쿨감만", party3, opt3.BestFormation, planC, ordNL3b, true);
    RunAndReport($"{mode}-U4 유저 로테 · 나타1위·리나2위 쿨감만", party3, opt3.BestFormation, planU3b, ordNL3b, true);
    RunAndReport($"{mode}-U3 유저 로테 · 나타1위·리나2위 전부", party3, opt3.BestFormation, planU3b, ordNL3b, false);
    RunAndReport($"{mode}-U0 유저 로테 · 기본 타게팅", party3, opt3.BestFormation, planU3b, null, false);
    return;
}

// ───────── 파트1: 옵티마이저 기어(현행 배치 설정) 위에서 로테만 교체 ─────────
var team1 = Fresh();
var c1 = BaseOpt(team1, asCandidates: true); c1.CoordinateAscentGear = true;
var opt1 = new SiegeOptimizer().Optimize(c1);
var party1 = opt1.BestParty;
int Idx1(string n) => party1.FindIndex(c => c.Character.Name == n);
Console.WriteLine($"[{DAY}] 파트1 옵티마이저 총점 {opt1.BestScore:N0} · 진형 {opt1.BestFormation}");
Console.WriteLine("파티: " + string.Join(" / ", party1.Select((c, i) => $"{i}:{c.Character.Name}({(c.IsBackPosition ? "후" : "전")})")));
foreach (var g in opt1.GearLog) Console.WriteLine("  " + g);
var planA = opt1.BestRotationPlan.Select(d => new RotationDecision { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold }).ToList();
var planU1 = steps.Select(s => new RotationDecision { HeroIndex = Idx1(s.hero), Skill = s.skill }).ToList();
PrintPlan("로테 A (옵티마이저)", party1, planA);
PrintPlan("로테 U (유저 20스텝)", party1, planU1);

var ordNL1 = new List<int> { Idx1("나타"), Idx1("리나") };
var ordL1 = new List<int> { Idx1("리나") };
RunAndReport("1-A  옵티 로테 · 기본 타게팅", party1, opt1.BestFormation, planA, null, false);
RunAndReport("1-U0 유저 로테 · 기본 타게팅", party1, opt1.BestFormation, planU1, null, false);
RunAndReport("1-U3 유저 로테 · 나타1위·리나2위 (라이언쿨감→나타, 나타쿨감→리나, 장비강화→나타+리나)", party1, opt1.BestFormation, planU1, ordNL1, false);
RunAndReport("1-U4 유저 로테 · 나타1위·리나2위 쿨감만 (장비강화→나타+미호)", party1, opt1.BestFormation, planU1, ordNL1, true);
RunAndReport("1-U1 유저 로테 · 리나1위 전부", party1, opt1.BestFormation, planU1, ordL1, false);

if (mode != "p2") return;
// ───────── 파트2: 유저 로테·보호진형(나타 후열)에 맞춰 기어 재탐색 ─────────
var team2 = Fresh();
int Idx2(string n) => team2.FindIndex(c => c.Character.Name == n);
var planU2 = steps.Select(s => new RotationDecision { HeroIndex = Idx2(s.hero), Skill = s.skill }).ToList();
var ordNL2 = new List<int> { Idx2("나타"), Idx2("리나") };
var c2 = BaseOpt(team2, asCandidates: false);
c2.CoordinateAscentGear = false;   // GearEval*을 직접 고정(좌표상승은 GearEvalRotation을 덮어씀)
c2.ForcedFormation = "보호 진형"; c2.ForcedBackRow = new List<string> { "나타" };
c2.GearEvalFormation = "보호 진형"; c2.GearEvalBackRow = new List<string> { "나타" };
c2.GearEvalRotation = planU2; c2.SeedRotations = new List<List<RotationDecision>> { planU2 };
c2.HighestAtkAllyOrder = ordNL2; c2.HighestAtkAllyOverrideCdrOnly = true;
var opt2 = new SiegeOptimizer().Optimize(c2);
var party2 = opt2.BestParty;
Console.WriteLine($"\n\n[{DAY}] 파트2 (유저 로테 기준 기어·보호진형·나타 후열·나타1위리나2위 쿨감) 옵티 채택 총점 {opt2.BestScore:N0} · 진형 {opt2.BestFormation}");
Console.WriteLine("파티: " + string.Join(" / ", party2.Select((c, i) => $"{i}:{c.Character.Name}({(c.IsBackPosition ? "후" : "전")})")));
foreach (var g in opt2.GearLog) Console.WriteLine("  " + g);
var planB = opt2.BestRotationPlan.Select(d => new RotationDecision { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold }).ToList();
PrintPlan("로테 B (파트2 옵티 채택)", party2, planB);
RunAndReport("2-B  옵티 채택 로테 · 나타1위·리나2위 쿨감만", party2, opt2.BestFormation, planB, ordNL2, true);
RunAndReport("2-U4 유저 로테 · 나타1위·리나2위 쿨감만", party2, opt2.BestFormation, planU2, ordNL2, true);
RunAndReport("2-U3 유저 로테 · 나타1위·리나2위 전부(장비강화→나타+리나)", party2, opt2.BestFormation, planU2, ordNL2, false);
RunAndReport("2-U0 유저 로테 · 기본 타게팅", party2, opt2.BestFormation, planU2, null, false);

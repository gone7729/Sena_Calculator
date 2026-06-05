using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 작업1: 라이언-후열 빔 아티팩트 확정 실험 (목요일)
//   고정: 밸런스 진형 / 후열 타카·라이언 / 전열 돼오·레이첼·비스킷. 6초월·잠재0·전용없음.
//   절차: 강제 config를 AutoEquip+빔으로 산출(기어·빔점수) → 같은 기어로 doc의 실측 19스킬
//         로테이션을 홀드 프리픽스 0~6으로 강제시뮬, 최선 정렬 점수가 빔/전열을 넘는지 본다.
//   판정: 실측로테(후열) > 라이언-전열 빔이면 → 빔이 갇힌 것 = 라이언-후열 옳음(아티팩트 확정).
// ============================================================================

const string DAY = "목요일";

static BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = 6,
        PotentialAtkLevel = 0, PotentialDefLevel = 0, PotentialHpLevel = 0,
        Equipment = null,
    };
}

if (!EnemyDb.SiegeStages.TryGetValue(DAY, out var stage))
{
    Console.WriteLine($"[skip] {DAY} 시즈 데이터 없음"); return;
}

// 자리 인덱스: 라이언(0)·타카(1)·레이첼(2)·비스킷(3)·돼오(4) — FixedMembers 순서 = AllyParty 순서.
int[] Ids() => new[] { 2, 1, 301, 201, 15 };

SiegeOptimizerConfig Cfg(List<BattleCharacter> team, List<string> backRow) => new()
{
    FixedMembers = team, Candidates = new(), PartySize = 5, SiegeStage = stage,
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, AutoEquip = true, SearchExclusiveWeapon = false,
    OptimizeRotation = true, RotationBeamWidth = 10, RotationMaxDepth = 28,
    ForcedFormation = "밸런스 진형", ForcedBackRow = backRow,
};

SiegeBattleConfig SimCfg(List<BattleCharacter> team, List<RotationDecision> plan, bool recordDp = false) => new()
{
    AllyParty = team, FormationName = "밸런스 진형", SiegeStage = stage,
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, RotationPlan = plan, RecordDecisionPoints = recordDp,
};

// 이름→SkillType (공백 무시 매칭). 없으면 명확히 throw.
SkillType Sk(BattleCharacter bc, string name)
{
    string norm = name.Replace(" ", "");
    var sk = bc.Character.Skills?.FirstOrDefault(s => (s.Name ?? "").Replace(" ", "") == norm);
    if (sk == null) throw new Exception($"스킬 '{name}' 을(를) {bc.Character.Name} 에서 못 찾음");
    return sk.SkillType;
}

var sb = new StringBuilder();
void Both(string s) { Console.WriteLine(s); sb.AppendLine(s); }

Both("════════ 작업1: 라이언-후열 빔 아티팩트 확정 실험 (목요일, 6초월·잠재0·전용없음) ════════");
Both($"보스: {stage.Name}\n");

// ── (A) 라이언-후열 (후열 타카·라이언) 강제 — AutoEquip+빔 ──
var teamBack = Ids().Select(Hero).ToList();
var swB = System.Diagnostics.Stopwatch.StartNew();
var resBack = new SiegeOptimizer().Optimize(Cfg(teamBack, new List<string> { "타카", "라이언" }));
swB.Stop();
Both($"[A] 라이언-후열 (후열 {string.Join("·", resBack.BestBackRow)}) — AutoEquip+빔 {swB.ElapsedMilliseconds / 1000.0:F0}s");
Both($"    자동로테 {resBack.AutoRotationScore:N0} → 빔(+생존반지) {resBack.BestScore:N0}");
foreach (var c in resBack.BestResult.CharacterResults.OrderByDescending(c => c.TotalDamage))
    Both($"      {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%){(c.Died ? " ☠사망" : "")}");

// 빔 플랜 결정점·라운드 구조 덤프 (홀드 프리픽스 결정 참고용)
var dpRes = new SiegeBattleSimulator(777).Simulate(SimCfg(teamBack, resBack.BestRotationPlan, recordDp: true));
Both($"\n    [빔 플랜 결정점 {dpRes.DecisionPoints.Count}개] 스킬턴 인덱스→턴:");
Both("      " + string.Join("  ", dpRes.DecisionPoints.Select(d => $"#{d.SkillTurnIndex}=T{d.Turn}")));
Both("    [라운드 전환 로그]");
foreach (var log in dpRes.TurnLogs.Where(l => (l.SkillName ?? "").Contains("라운드") || (l.Description ?? "").Contains("클리어")))
    Both($"      T{log.Turn}: {log.Description}");

string[] heroNm = { "라이언", "타카", "레이첼", "비스킷", "돼오" };
string PlanStr(List<RotationDecision> p) => string.Join("\n", p.Select((d, i) =>
    $"      {i + 1,2}. " + (d.Hold ? "(홀드)" : $"{heroNm[d.HeroIndex]} → {teamBack[d.HeroIndex].Character.Skills.First(s => s.SkillType == d.Skill).Name}")));
Both($"\n    [빔 최적 스킬순서 {resBack.BestRotationPlan.Count}개]");
Both(PlanStr(resBack.BestRotationPlan));

// ── (B) doc 실측 19스킬 로테이션 (이름→SkillType, 자리 인덱스로) ──
var R = teamBack;  // 인덱스 별칭
(int h, string nm)[] measuredSpec =
{
    (1, "바람의 칼날"), (0, "강자 사냥"), (2, "불새"),     (3, "장비 강화"), (1, "죽음의 무도"),
    (0, "광풍참"),     (4, "룰렛맨"),   (1, "바람의 칼날"), (0, "강자 사냥"), (2, "불새"),
    (1, "죽음의 무도"), (0, "광풍참"),   (3, "장비 강화"),   (4, "룰렛맨"),   (0, "강자 사냥"),
    (1, "죽음의 무도"), (1, "바람의 칼날"), (2, "불새"),     (0, "광풍참"),
};
var measured = measuredSpec.Select(m => new RotationDecision { HeroIndex = m.h, Skill = Sk(R[m.h], m.nm) }).ToList();
Both($"\n[B] doc 실측 19스킬 로테이션 (광풍참 {measuredSpec.Count(m => m.nm == "광풍참")}회):");
Both(PlanStr(measured));

// 홀드 프리픽스 0~6 브루트포스 — R1/R2 0턴 선공 스킬턴 홀드 정렬 탐색
Both("\n[C] 실측 로테 강제시뮬 — 홀드 프리픽스 0~6 (R1/R2 통과 정렬):");
double bestM = -1; int bestPre = -1; SiegeBattleResult bestMRes = null;
for (int hold = 0; hold <= 6; hold++)
{
    var plan = Enumerable.Repeat(new RotationDecision { Hold = true }, hold).Concat(measured).ToList();
    var r = new SiegeBattleSimulator(777).Simulate(SimCfg(teamBack, plan));
    Both($"    홀드×{hold}: 총점 {r.TotalScore:N0}  (R{string.Join("/", r.RoundScore.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value / 1e6:F2}M"))}, 생존 {r.AlliesAlive}/5)");
    if (r.TotalScore > bestM) { bestM = r.TotalScore; bestPre = hold; bestMRes = r; }
}
Both($"  → 실측 로테 최선: 홀드×{bestPre}, 총점 {bestM:N0}");
if (bestMRes != null)
    foreach (var c in bestMRes.CharacterResults.OrderByDescending(c => c.TotalDamage))
        Both($"      {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%){(c.Died ? " ☠사망" : "")}");

// ── (D) 라이언-전열 (후열 타카·레이첼) 강제 — 비교 기준 ──
var teamFront = Ids().Select(Hero).ToList();
var swF = System.Diagnostics.Stopwatch.StartNew();
var resFront = new SiegeOptimizer().Optimize(Cfg(teamFront, new List<string> { "타카", "레이첼" }));
swF.Stop();
Both($"\n[D] 라이언-전열 (후열 {string.Join("·", resFront.BestBackRow)}) — AutoEquip+빔 {swF.ElapsedMilliseconds / 1000.0:F0}s");
Both($"    자동로테 {resFront.AutoRotationScore:N0} → 빔(+생존반지) {resFront.BestScore:N0}");

// ── 판정 ──
Both("\n════════ 판정 ════════");
Both($"  라이언-후열 빔        : {resBack.BestScore:N0}");
Both($"  라이언-후열 실측로테  : {bestM:N0}  (홀드×{bestPre})");
Both($"  라이언-전열 빔        : {resFront.BestScore:N0}");
double vsFront = (bestM - resFront.BestScore) / resFront.BestScore * 100;
double vsBeam = (bestM - resBack.BestScore) / resBack.BestScore * 100;
Both($"  실측로테(후열) vs 전열빔 : {bestM - resFront.BestScore:+#,##0;-#,##0} ({vsFront:+0.00;-0.00}%)");
Both($"  실측로테(후열) vs 후열빔 : {bestM - resBack.BestScore:+#,##0;-#,##0} ({vsBeam:+0.00;-0.00}%)");
Both(bestM > resFront.BestScore
    ? "  ✅ 실측로테(후열) > 전열빔 → 빔이 라이언-후열 최적을 못 찾음 = 아티팩트 확정. 라이언-후열이 옳음."
    : "  ❌ 실측로테(후열) ≤ 전열빔 → 빔 아티팩트 아님. 라이언-전열이 실제로 우위(또는 동급).");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_목요일_라이언후열_실험.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"\n→ {outPath}");

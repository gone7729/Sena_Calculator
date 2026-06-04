using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 화요일: 영웅·펫만 고정 + 기어(템세팅)·스킬 로테이션·진형(+자리) 모두 탐색.
//   영웅(초월·잠재·스강·전용무기)·펫(윈디6성+3·72%)만 실측 고정.
//   Equipment=null로 비워 AutoEquip이 세트→메인옵→부옵을 탐색,
//   진형 3종(기본/밸런스/보호) × 자리(전/후열) 순회, 빔서치가 스킬순서를 탐색.
//   실측 총점 6,862,365 대비 "이 영웅구성의 최적 기어+진형+플레이 천장"을 본다.
// ============================================================================

const string DAY = "목요일";
const double REAL_SCORE = 12_470_914.0;   // 목요일 직전 배치 결과(비교 기준)

static BattleCharacter Hero(int id, int tr, int pa, int pd, int ph, bool magicExclusive = false)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = magicExclusive ? ExclusiveWeaponDb.Universal(magic: true) : null;
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = tr,
        PotentialAtkLevel = pa, PotentialDefLevel = pd, PotentialHpLevel = ph,
        Equipment = null,   // ← 비움 = AutoEquip이 기어 탐색
    };
}

// 영웅 5인 — 실측 조건: 전원 6초월·잠재 3/3/3·전용무기 없음
var ryan   = Hero(2,   6, 0, 0, 0);   // 라이언
var taka   = Hero(1,   6, 0, 0, 0);   // 타카
var rachel = Hero(301, 6, 0, 0, 0);   // 레이첼
var biskit = Hero(201, 6, 0, 0, 0);   // 비스킷
var dweo   = Hero(15,  6, 0, 0, 0);   // 돼오
var team = new List<BattleCharacter> { ryan, taka, rachel, biskit, dweo };

if (!EnemyDb.SiegeStages.TryGetValue(DAY, out var stage))
{
    Console.WriteLine($"[skip] {DAY} 시즈 데이터 없음"); return;
}

var cfg = new SiegeOptimizerConfig
{
    FixedMembers = team,
    Candidates = new List<BattleCharacter>(),
    PartySize = 5,
    SiegeStage = stage,
    AllyPet = PetDb.GetByName("윈디"),
    PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70,
    AutoEquip = true,            // ← 기어 탐색
    SearchExclusiveWeapon = false,// ← 전용무기 없음(유저 지정)
    OptimizeRotation = true,     // ← 스킬 로테이션 빔서치
    RotationBeamWidth = 10,
    RotationMaxDepth = 28,
    // ForcedFormation/ForcedBackRow 미지정 = 진형 3종 × 자리배치 전체 탐색
};

var sw = System.Diagnostics.Stopwatch.StartNew();
var res = new SiegeOptimizer().Optimize(cfg);
sw.Stop();

// ── per-hit 진단: best 플랜으로 최종 1회 시뮬 (나타 화첨/혼천 덤프) → 실측 영상(화첨255k·혼천494k) 대조
var diagSim = new SiegeBattleSimulator(777) { DiagSkillNames = new List<string> { "화첨창술", "혼천릉파" } };
var diagResult = diagSim.Simulate(new SiegeBattleConfig
{
    AllyParty = res.BestParty, FormationName = res.BestFormation, SiegeStage = stage,
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, RotationPlan = res.BestRotationPlan,
});

var sb = new StringBuilder();
sb.AppendLine("════════ 목요일: 라이언·타카·레이첼·비스킷·돼오 전원6초월·잠재0 전용없음 — 기어·진형·로테이션 탐색 (델론즈 재시전·죽음의경계 검증) ════════");
sb.AppendLine($"보스: {stage.Name}");
sb.AppendLine($"실측 총점: {REAL_SCORE:N0}");
sb.AppendLine($"최적 총점: {res.BestScore:N0}  (실측의 {res.BestScore / REAL_SCORE * 100:F1}%)   [자동로테 {res.AutoRotationScore:N0} → 빔 {res.BestScore:N0}]");
sb.AppendLine($"진형: {res.BestFormation} / 후열: {string.Join(",", res.BestBackRow)} / 탐색 {sw.ElapsedMilliseconds / 1000.0:F0}s");
if (res.BestResult != null)
    sb.AppendLine($"라운드별: {string.Join(", ", res.BestResult.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}"))}");
sb.AppendLine();

sb.AppendLine("──── 캐릭터별 기여 ────");
if (res.BestResult != null)
    foreach (var c in res.BestResult.CharacterResults.OrderByDescending(c => c.TotalDamage))
        sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");

sb.AppendLine("\n──── 자동 장착 기어 (탐색 결과) ────");
foreach (var g in res.GearLog) sb.AppendLine("  " + g);

sb.AppendLine("\n──── 최적 스킬 순서 ────");
var nm = res.BestParty.Select(b => b.Character.Name).ToList();
string SkNm(int hi, SkillType st) =>
    res.BestParty[hi].Character.Skills?.FirstOrDefault(s => s.SkillType == st)?.Name ?? st.ToString();
if (res.BestRotationPlan != null && res.BestRotationPlan.Count > 0)
    for (int i = 0; i < res.BestRotationPlan.Count; i++)
    {
        var d = res.BestRotationPlan[i];
        sb.AppendLine($"  {i + 1,2}. " + (d.Hold ? "(홀드)" : $"{nm[d.HeroIndex]} → {SkNm(d.HeroIndex, d.Skill)}"));
    }
else
    sb.AppendLine("  (빔이 자동로테를 못 넘어 플랜 비어있음)");

sb.AppendLine("\n──── 속공순서 · 쿨다운 추적 · 불새↔살육 cleanse 턴로그 ────");
foreach (var log in diagResult.TurnLogs)
{
    var dsc = log.Description ?? "";
    var sk = log.SkillName ?? "";
    bool keep = sk == "죽음의 경계" || sk.Contains("죽음의 일격") || (log.Description??"").Contains("재시전") || (log.Description??"").Contains("사망") || (log.Description??"").Contains("무효 (죽음") || sk == "쿨감"
        || sk == "불새" || sk == "살육의 춤" || sk == "교만의 일격" || sk == "혼천릉파" || sk == "화첨창술"
        || (dsc.Contains("디버프") && dsc.Contains("해제"));
    if (keep)
        sb.AppendLine($"T{log.Turn,2} [{(log.IsAlly ? "아군" : "적 ")}] {sk}: {dsc}");
}

sb.AppendLine("\n──── 나타 화첨창술·혼천릉파 per-hit (실측 영상: 화첨≈255k·혼천≈494k/타격) ────");
sb.AppendLine(diagSim.DiagLog.ToString());

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_목요일_델론즈검증_6초월잠재0_perhit.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine(sb.ToString());
Console.WriteLine($"→ {outPath}");

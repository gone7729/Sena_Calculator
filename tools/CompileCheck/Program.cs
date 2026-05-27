using System;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 수요일 공성전 교차검증: 루리·미호·오를리·비스킷·라이언. 오를리 패시브(마법형 아군 치확17/치피25)
// 적용 검증 포함. 전원 12초월·스킬강화, 펫 윈디 6성 강화+3, 펫 잠재 공옵 72%(18×4).
const string DAY = "수요일";
int[] teamIds = { 102, 103, 203, 201, 2 };   // 루리, 미호, 오를리, 비스킷, 라이언
var team = teamIds
    .Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 12 })
    .ToList();

var config = new SiegeOptimizerConfig
{
    Candidates = team,
    SiegeStage = EnemyDb.SiegeStages[DAY],
    MaxTurns = 70,
    AutoEquip = true,                       // 세트·메인·부옵·장신구 풀시뮬 최적화
    AllyPet = PetDb.GetByName("윈디"),
    PetStar = 6,
    PetEnhance = 3,
    PetOptionAtkRate = 72,                   // 펫 잠재 공옵 18×4
};

var sw = System.Diagnostics.Stopwatch.StartNew();
var result = new SiegeOptimizer().Optimize(config);
sw.Stop();

var r = result.BestResult;
var sb = new StringBuilder();
sb.AppendLine($"================ {DAY} 공성전 풀시뮬 (시뮬 모드) ================");
sb.AppendLine($"팀: {string.Join(", ", result.BestParty.Select(c => c.Character.Name))}");
sb.AppendLine("세팅: 전원 12초월·스킬강화 / 펫 윈디 6성 강화+3 / 펫 잠재 공옵 72%(18×4)");
sb.AppendLine($"진형: {result.BestFormation} / 후열: {(result.BestBackRow.Count > 0 ? string.Join(",", result.BestBackRow) : "없음(전원 전열)")}");
sb.AppendLine($"총점: {result.BestScore:N0}");
sb.AppendLine($"라운드별: {string.Join(", ", r.RoundScore.OrderBy(kv => kv.Key).Select(kv => $"R{kv.Key}={kv.Value:N0}"))}");
sb.AppendLine($"도달턴: {r.TotalTurns}, 클리어 라운드: {r.RoundsCleared}");
sb.AppendLine($"평가 {result.EvaluatedCount}회 (탐색 소요 {sw.ElapsedMilliseconds:N0}ms)");
sb.AppendLine();
sb.AppendLine("캐릭터별 기여:");
foreach (var c in r.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine();
sb.AppendLine("================ 스탯 스냅샷 (오를리 패시브 검증: 마법형만 치확+17/치피+25) ================");
{
    var bsim = new BattleSimulator();
    var bcfg = new BattleConfig
    {
        AllyParty = result.BestParty, FormationName = result.BestFormation,
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    };
    for (int i = 0; i < result.BestParty.Count; i++)
    {
        var bc = result.BestParty[i];
        var cs = bsim.InitializeCharacterState(bcfg, bc, i);
        var ds = cs.DisplayStats;
        sb.AppendLine($"  {bc.Character.Name} ({bc.Character.Type}/{bc.Character.AttackType}, {(bc.IsBackPosition ? "후열" : "전열")}): 공격력 {cs.FinalAtk:N0}, 치확 {ds?.Cri:F0}, 치피 {ds?.Cri_Dmg:F0}, 약확 {ds?.Wek:F0}, Spd {cs.FinalSpd}");
    }

    // [통제 비교] 오를리 패시브(마법형 치확+17/치피+25) 적용 검증: 오를리 있을 때 vs 없을 때 크릿 델타.
    sb.AppendLine();
    sb.AppendLine("  [오를리 패시브 통제검증] 오를리 포함 vs 제외 시 크릿 변화 (마법형만 +17/+25 기대):");
    var partyNoOrli = result.BestParty.Where(c => c.Character.Name != "오를리").ToList();
    var cfgNo = new BattleConfig { AllyParty = partyNoOrli, FormationName = result.BestFormation,
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72 };
    foreach (var name in new[] { "루리", "라이언" })
    {
        int iFull = result.BestParty.FindIndex(c => c.Character.Name == name);
        int iNo = partyNoOrli.FindIndex(c => c.Character.Name == name);
        if (iFull < 0 || iNo < 0) continue;
        var full = bsim.InitializeCharacterState(bcfg, result.BestParty[iFull], iFull).DisplayStats;
        var no = bsim.InitializeCharacterState(cfgNo, partyNoOrli[iNo], iNo).DisplayStats;
        sb.AppendLine($"    {name}: 치확 {no?.Cri:F0}→{full?.Cri:F0} (Δ{full?.Cri - no?.Cri:+0;-0;0}), 치피 {no?.Cri_Dmg:F0}→{full?.Cri_Dmg:F0} (Δ{full?.Cri_Dmg - no?.Cri_Dmg:+0;-0;0})");
    }
}
sb.AppendLine();
sb.AppendLine($"================ 탐색 평가 목록 (점수순 상위 25 / 총 {result.EvalLog.Count}건) ================");
int rank = 1;
foreach (var e in result.EvalLog.OrderByDescending(x => x.Score).Take(25))
{
    string rounds = string.Join(", ", e.RoundScore.OrderBy(kv => kv.Key).Select(kv => $"R{kv.Key}={kv.Value:N0}"));
    string mark = (Math.Abs(e.Score - result.BestScore) < 1) ? " ★최고" : "";
    string back = e.BackRow.Count > 0 ? string.Join(",", e.BackRow) : "없음";
    sb.AppendLine($"  {rank++}. [{e.Formation}] 후열({back}) = {e.Score:N0} ({rounds}){mark}");
}
sb.AppendLine();
sb.AppendLine("================ 템세팅 (풀시뮬 자동 최적) ================");
foreach (var line in result.GearLog)
    sb.AppendLine(line);
sb.AppendLine();
sb.AppendLine("================ [통제] 빠른클리어(루리/미호 강AoE 선두) vs 빔서치 ================");
{
    int iLuri = result.BestParty.FindIndex(c => c.Character.Name == "루리");
    int iMiho = result.BestParty.FindIndex(c => c.Character.Name == "미호");
    // R1/R2 진입에 강 AoE를 몰아 0턴 연쇄로 R3 조기 진입 시도
    var fastPlan = new System.Collections.Generic.List<RotationDecision>
    {
        new() { HeroIndex = iLuri, Skill = SkillType.Skill2 },
        new() { HeroIndex = iMiho, Skill = SkillType.Skill2 },
        new() { HeroIndex = iLuri, Skill = SkillType.Skill1 },
        new() { HeroIndex = iMiho, Skill = SkillType.Skill1 },
    };
    var fastCfg = new SiegeBattleConfig
    {
        AllyParty = result.BestParty, FormationName = result.BestFormation,
        SiegeStage = EnemyDb.SiegeStages[DAY],
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
        MaxTurns = 70, RotationPlan = fastPlan,
    };
    var fastR = new SiegeBattleSimulator(777).Simulate(fastCfg);
    int r3start = fastR.TurnLogs.FirstOrDefault(l => l.Description != null && l.Description.Contains("R2 클리어"))?.Turn ?? -1;
    sb.AppendLine($"빔서치 점수: {result.BestScore:N0}");
    sb.AppendLine($"빠른클리어 점수: {fastR.TotalScore:N0} (R2클리어 T{r3start}, R3진입)");
    sb.AppendLine($"라운드별(빠른클리어): {string.Join(", ", fastR.RoundScore.OrderBy(kv=>kv.Key).Select(kv=>$"R{kv.Key}={kv.Value:N0}"))}");
}
sb.AppendLine("================ [진단] 죽음의 무도 보스/잡몹 데미지 입력 분해 ================");
sb.AppendLine("실측 T12: 스파이크(보스) 285,884 / 294,630 (조건O) | 룩(잡몹) 113,024 (조건X). 정답 보스 ≈290,500.");
{
    var diagSim = new SiegeBattleSimulator(777) { DiagSkillName = null };   // 수요일 팀엔 죽음의무도 없음 — 진단 비활성
    var diagCfg = new SiegeBattleConfig
    {
        AllyParty = result.BestParty,          // BestMask(자리) 이미 재적용됨
        FormationName = result.BestFormation,
        SiegeStage = EnemyDb.SiegeStages[DAY],
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
        MaxTurns = 70,
    };
    diagSim.Simulate(diagCfg);
    sb.AppendLine(diagSim.DiagLog.ToString());
}
sb.AppendLine("================ [빔서치] 스킬 로테이션 (옵티마이저 통합) ================");
sb.AppendLine($"자동 로테이션 점수: {result.AutoRotationScore:N0}");
sb.AppendLine($"빔서치 최적(=총점): {result.BestScore:N0} (개선 {(result.AutoRotationScore>0? (result.BestScore/result.AutoRotationScore-1)*100:0):F1}%)");
sb.AppendLine("최적 플랜(스킬턴별):");
for (int i = 0; i < result.BestRotationPlan.Count; i++)
{
    var dec = result.BestRotationPlan[i];
    string s = dec.Hold ? "홀드" : $"{result.BestParty[dec.HeroIndex].Character.Name} {dec.Skill}";
    sb.AppendLine($"  ST{i,2}: {s}");
}
sb.AppendLine();
sb.AppendLine($"================ 턴별 행동 로그 (빔서치 최적 플랜, 총 {r.TurnLogs.Count}개) ================");
foreach (var log in r.TurnLogs)
    sb.AppendLine($"T{log.Turn,2} [{(log.IsAlly ? "아군" : "적 ")}] {log.SkillName}: {log.Description}");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", $"siege_{DAY}_풀시뮬.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"DONE 출력: {outPath} (총점 {result.BestScore:N0}, 턴로그 {r.TurnLogs.Count}, {sw.ElapsedMilliseconds:N0}ms)");

using System;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 토요일 공성전 풀시뮬 (시뮬 모드 세팅): 전원 12초월·스킬강화, 펫 윈디 6성 강화+3, 펫 잠재 공옵 72%(18×4).
// 템세팅(세트·메인·부옵·장신구 전부 풀시뮬)·스킬순서 탐색 → 결과를 텍스트 파일로.
int[] teamIds = { 1, 2, 201, 301, 51 };   // 타카, 라이언, 비스킷, 레이첼, 풍연
var team = teamIds
    .Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 12 })
    .ToList();

var config = new SiegeOptimizerConfig
{
    Candidates = team,
    SiegeStage = EnemyDb.SiegeStages["토요일"],
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
sb.AppendLine("================ 토요일 공성전 풀시뮬 (시뮬 모드) ================");
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
sb.AppendLine("================ 스탯 스냅샷 (FinalAtk — 비스킷 버프 대상 판정 기준) ================");
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
        sb.AppendLine($"  {bc.Character.Name} ({bc.Character.Type}/{bc.Character.AttackType}, {(bc.IsBackPosition ? "후열" : "전열")}): 인게임공격력(진형포함) {cs.FinalAtk:N0}, Spd {cs.FinalSpd}");
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
sb.AppendLine($"================ 턴별 행동 로그 (총 {r.TurnLogs.Count}개) ================");
foreach (var log in r.TurnLogs)
    sb.AppendLine($"T{log.Turn,2} [{(log.IsAlly ? "아군" : "적 ")}] {log.SkillName}: {log.Description}");

string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "siege_토요일_풀시뮬.txt"));
System.IO.File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"DONE 출력: {outPath} (총점 {result.BestScore:N0}, 턴로그 {r.TurnLogs.Count}, {sw.ElapsedMilliseconds:N0}ms)");

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 공성 빌드 정밀 탐색 + 저장/불러오기.
//   탐색은 한 번에 1시간 안팎 걸리지만 찾은 빌드는 results/siege/build_*.json 에 구조화해 저장하므로
//   (장비·장신구·전용장비·자리·로테) 다음부터는 "불러오기"로 탐색 없이 재생한다.
//   저장 파일은 "지금까지 찾은 최고 빌드"다 — 다시 탐색해도 현재 엔진으로 재평가한 저장 빌드보다 높을 때만 바뀐다.
//   그래서 출발 순서를 바꿔 여러 번 돌리면 그중 최고가 남는다(탐색의 출발 순서 의존 대응).
//
// 사용: SiegeBuildSearch <요일> 팀=a,b,c,d,e [진형=밸런스 진형] [후열=x,y] [불러오기] [재탐색=N]
//        [6초월] [델로|리첼] [노각성] [dw] [quick] [nospd] [noorder] [nopolish] [nosurv] [norank]
//        [페널티N] [반격시드N] [평가시드N]
//   팀 순서 = 탐색 출발 순서(같은 속공이면 앞에서부터 행동). 저장 키는 팀 구성(순서 무관)이다.
//   재탐색=N: 탐색이 끝난 뒤 최종 순서에서 다시 출발하는 탐색을 최대 N번 더 한다.
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;
string day = args.FirstOrDefault(a => EnemyDb.SiegeStages.ContainsKey(a));
string teamArg = args.FirstOrDefault(a => a.StartsWith("팀="))?.Substring(2);
if (day == null || teamArg == null)
{
    Console.WriteLine("사용: SiegeBuildSearch <요일> 팀=a,b,c,d,e [진형=밸런스 진형] [후열=x,y] [불러오기] [재탐색=N] [6초월] [델로|리첼] [노각성] [dw] [quick]");
    return;
}
var teamNames = teamArg.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
var unknown = teamNames.Where(n => !CharacterDb.Characters.Any(c => c.Name == n)).ToList();
if (unknown.Count > 0 || teamNames.Count != 5)
{
    Console.WriteLine($"팀은 DB에 있는 영웅 5명이어야 합니다. (없는 이름: {string.Join(",", unknown)})");
    return;
}

string Val(string key) => args.FirstOrDefault(a => a.StartsWith(key))?.Substring(key.Length);
bool quick = args.Contains("quick");
bool loadOnly = args.Contains("불러오기");
bool lowSpec = args.Contains("6초월");
bool ingame = !args.Contains("dw");
bool awaken = !args.Contains("노각성") && (!lowSpec || args.Contains("각성"));
int TRANS = lowSpec ? 6 : 12, POT = lowSpec ? 0 : 3;
string petName = args.Contains("델로") ? "델로" : args.Contains("리첼") ? "리첼" : "윈디";
string formationArg = Val("진형=");
var backArg = Val("후열=")?.Split(',').Select(x => x.Trim()).ToList();
int passes = int.TryParse(Val("재탐색="), out var pz) ? Math.Max(1, pz + 1) : 1;
double penalty = double.TryParse(Val("페널티"), out var pv) ? pv : (day == "일요일" ? 0 : 1_000_000);
int caSeeds = int.TryParse(Val("반격시드"), out var cs) && cs > 0 ? cs : day == "금요일" ? 10 : day == "일요일" ? 4 : 1;
int evalSeeds = int.TryParse(Val("평가시드"), out var es) && es > 0 ? es : day == "일요일" ? 16 : 1;

var stage = EnemyDb.SiegeStages[day];
var pet = PetDb.GetByName(petName);
string profile = $"{TRANS}초월_{petName}" + (awaken == !lowSpec ? "" : awaken ? "_각성" : "_노각성") + (ingame ? "" : "_dw") + (quick ? "_quick" : "");
string teamKey = string.Join("-", teamNames.Select(n => CharacterDb.Characters.First(c => c.Name == n)).OrderBy(c => c.Id).Select(c => c.Name));
string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string outDir = System.IO.Path.Combine(repoRoot, "results", "siege");
string storePath = System.IO.Path.Combine(outDir, $"build_{day}_{profile}_{teamKey}.json");
string txtPath = System.IO.Path.ChangeExtension(storePath, ".txt");

BattleCharacter Hero(string name)
{
    var c = CharacterDb.Characters.First(x => x.Name == name);
    c.ExclusiveWeapon = quick && !lowSpec ? ExclusiveWeaponDb.Universal(magic: c.AttackType == AttackType.Magic) : null;
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = TRANS, IsAwakened = awaken && c.HasAwakening,
        PotentialAtkLevel = POT, PotentialDefLevel = POT, PotentialHpLevel = POT, Equipment = null,
    };
}

SiegeOptimizerConfig MakeCfg(List<BattleCharacter> team, List<List<RotationDecision>> seeds)
{
    var cfg = new SiegeOptimizerConfig
    {
        FixedMembers = team, Candidates = new List<BattleCharacter>(), PartySize = 5, SiegeStage = stage,
        AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
        AutoEquip = true, SearchExclusiveWeapon = !quick && !lowSpec, EnableSurvivalRings = !quick && !lowSpec,
        FloorFirstGear = true, AllyDeathPenalty = penalty, OptimizeRotation = true, InGameBuffTargeting = ingame,
        ForcedFormation = formationArg, ForcedBackRow = backArg,
        CounterattackSearchSeeds = caSeeds, EvalSeeds = evalSeeds,
        SeedRotations = seeds != null && seeds.Count > 0 ? seeds : null,
    };
    if (quick)
    {
        cfg.RotationBeamWidth = 4; cfg.RotationMaxDepth = 28; cfg.QuickGear = true;
        cfg.PolishRotation = true; cfg.SearchAtkRank = !args.Contains("norank");
    }
    else
    {
        cfg.RotationBeamWidth = 10; cfg.RotationMaxDepth = 36; cfg.CoordinateAscentGear = true;
        cfg.PolishRotation = !args.Contains("nopolish");
        cfg.SearchAggressiveSurvival = !args.Contains("nosurv");
        cfg.SearchAtkRank = !args.Contains("norank");
        cfg.SearchActionOrder = !args.Contains("noorder");
        cfg.SearchSpeedLead = !args.Contains("nospd");
    }
    return cfg;
}

// ── 엔진 소스 해시(저장 당시 코드와 지금 코드가 같은지 표시용) ──
string EngineHash()
{
    try
    {
        char sep = System.IO.Path.DirectorySeparatorChar;
        var files = new[] { "Services", "Models", "DB" }.Select(d => System.IO.Path.Combine(repoRoot, d))
            .Where(System.IO.Directory.Exists)
            .SelectMany(d => System.IO.Directory.EnumerateFiles(d, "*.cs", System.IO.SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}"))
            .OrderBy(f => f, StringComparer.Ordinal).ToList();
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var ms = new System.IO.MemoryStream();
        foreach (var f in files)
        {
            var hdr = Encoding.UTF8.GetBytes(System.IO.Path.GetRelativePath(repoRoot, f).Replace('\\', '/') + "\n");
            ms.Write(hdr, 0, hdr.Length);
            var body = System.IO.File.ReadAllBytes(f);
            ms.Write(body, 0, body.Length);
        }
        ms.Position = 0;
        return "sha256:" + Convert.ToHexString(sha.ComputeHash(ms)).ToLowerInvariant()[..16];
    }
    catch { return null; }
}
string GitHead()
{
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", "rev-parse --short HEAD")
        { WorkingDirectory = repoRoot, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        using var p = System.Diagnostics.Process.Start(psi);
        string o = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit();
        return p.ExitCode == 0 ? o : null;
    }
    catch { return null; }
}
string engineHash = EngineHash();

// 저장된 빌드를 지금 엔진·지금 조건으로 다시 점수 낸다. (전용장비를 DB 영웅에 다시 꽂으므로 탐색 전후 각각 호출)
(SiegeBattleResult Res, List<BattleCharacter> Party, List<RotationDecision> Plan) Evaluate(SiegeSavedBuild b)
{
    var party = SiegeBuildStore.ToParty(b);
    var plan = SiegeBuildStore.ToPlan(b, party);
    var res = SiegeOptimizer.EvaluateBuild(MakeCfg(party, null), party, b.Formation, plan);
    return (res, party, plan);
}

Console.WriteLine($"═══ {day} [{string.Join(",", teamNames)}] {profile} — 저장 파일 {System.IO.Path.GetFileName(storePath)}");

// ── 1) 저장된 빌드 확인 ──
var saved = SiegeBuildStore.Load(storePath);
double savedTotal = double.NegativeInfinity, savedRank = double.NegativeInfinity;
if (saved != null)
{
    var ev = Evaluate(saved);
    savedTotal = ev.Res.TotalScore; savedRank = ev.Res.RankScore;
    string fresh = saved.EngineHash == engineHash ? "같은 엔진" : "엔진 변경됨";
    Console.WriteLine($"[저장된 빌드] 저장 당시 {saved.Score:N0} → 현재 재생 {savedTotal:N0} (랭크 {savedRank:N0}, 사망 {ev.Res.Deaths?.Count ?? 0}) · {fresh} · {saved.SavedAt} · 출발 {saved.StartOrder}");
    if (saved.InGameBuffTargeting != ingame) Console.WriteLine($"  ⚠ 저장 당시 타게팅({(saved.InGameBuffTargeting ? "인게임" : "DW")})과 지금 설정이 다릅니다.");
}
else Console.WriteLine("[저장된 빌드] 없음");

if (loadOnly && saved == null)
{
    Console.WriteLine("불러올 빌드가 없습니다. '불러오기' 없이 실행해 먼저 탐색하세요.");
    return;
}

// ── 2) 탐색 (불러오기면 생략) ──
SiegeSavedBuild best = saved;
double bestTotal = savedTotal, bestRank = savedRank;
bool bestIsNew = false;
if (!loadOnly)
{
    var order = teamNames.ToList();
    var seedBuilds = new List<SiegeSavedBuild>();
    if (saved != null) seedBuilds.Add(saved);
    for (int pass = 1; pass <= passes; pass++)
    {
        var team = order.Select(Hero).ToList();
        var seeds = seedBuilds.Select(b => SiegeBuildStore.ToPlan(b, team)).Where(p => p.Count > 0).ToList();
        var cfg = MakeCfg(team, seeds);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var o = new SiegeOptimizer().Optimize(cfg);
        sw.Stop();
        var b = SiegeBuildStore.Capture(o.BestParty, o.BestFormation, o.BestBackRow, o.BestRotationPlan);
        b.Day = day; b.Profile = profile; b.Score = Math.Round(o.BestScore); b.RankScore = Math.Round(o.BestRankScore);
        b.Deaths = o.BestResult?.Deaths?.Count ?? 0; b.InGameBuffTargeting = ingame;
        b.EngineHash = engineHash; b.GitCommit = GitHead(); b.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        b.StartOrder = string.Join(",", order); b.SearchMinutes = Math.Round(sw.Elapsed.TotalMinutes, 1);
        b.Pet = new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 76 };
        b.Log = o.GearLog.ToList();
        string finalOrder = string.Join(",", o.BestParty.Select(x => x.Character.Name));
        Console.WriteLine($"[탐색 {pass}/{passes}] 출발 {b.StartOrder} → {o.BestScore:N0} (랭크 {o.BestRankScore:N0}, 사망 {b.Deaths}) · {o.BestFormation} 후열 {string.Join(",", o.BestBackRow)} · 최종 순서 {finalOrder} · {sw.Elapsed.TotalMinutes:F1}분");
        if (SiegeOptimizer.IsBetterBuild(o.BestScore, o.BestRankScore, bestTotal, bestRank))
        {
            best = b; bestTotal = o.BestScore; bestRank = o.BestRankScore; bestIsNew = true;
        }
        seedBuilds.Add(b);
        if (finalOrder == string.Join(",", order)) break;   // 순서가 그대로면 다시 출발해도 같은 탐색
        order = o.BestParty.Select(x => x.Character.Name).ToList();
    }
}

// ── 3) 최고 빌드를 되살려 재현 확인 → 저장 ──
var fin = Evaluate(best);
bool reproduced = Math.Abs(fin.Res.TotalScore - bestTotal) < 1.0;
if (!loadOnly)
{
    if (bestIsNew)
    {
        if (!reproduced) Console.WriteLine($"  ⚠ 저장 전 재현 불일치: 탐색 {bestTotal:N0} vs 재생 {fin.Res.TotalScore:N0}");
        SiegeBuildStore.Save(storePath, best);
        Console.WriteLine(saved == null ? $"[저장] 새 빌드 {bestTotal:N0}" : $"[저장] 갱신 {savedTotal:N0} → {bestTotal:N0}");
    }
    else
    {
        Console.WriteLine($"[유지] 저장된 빌드 {savedTotal:N0}가 이번 탐색 결과 이상 — 빌드를 바꾸지 않습니다.");
        if (saved.EngineHash != engineHash)
        {
            // 빌드는 그대로, 점수만 현재 엔진 값으로 갱신
            saved.Score = Math.Round(savedTotal); saved.RankScore = Math.Round(savedRank); saved.Deaths = fin.Res.Deaths?.Count ?? 0;
            saved.EngineHash = engineHash;
            SiegeBuildStore.Save(storePath, saved);
        }
    }
}

// ── 4) 리포트 ──
var rep = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
{
    AllyParty = fin.Party, FormationName = best.Formation, SiegeStage = stage,
    AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
    RotationPlan = fin.Plan, AllyDeathPenalty = penalty, RecordFeasibility = true, InGameBuffTargeting = ingame,
});
var bcfg = new BattleConfig { AllyParty = fin.Party, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, FormationName = best.Formation };
var bsim = new BattleSimulator();
var sb = new StringBuilder();
sb.AppendLine($"════════ {day} 공성전 빌드 — {profile} · 타게팅 {(ingame ? "인게임(현재 공격력)" : "DW")} ════════");
sb.AppendLine($"[저장] {best.SavedAt} · engineHash={best.EngineHash} · git={best.GitCommit} · 탐색 {best.SearchMinutes:F1}분 · 출발 순서 {best.StartOrder}");
sb.AppendLine($"보스: {stage.Name}");
sb.AppendLine($"총점: {fin.Res.TotalScore:N0} (랭크 {fin.Res.RankScore:N0}) · {rep.RoundsCleared}R 클리어 · 사망 {rep.Deaths.Count}"
    + (rep.Deaths.Count > 0 ? " (" + string.Join(", ", rep.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")" : ""));
sb.AppendLine($"진형: {best.Formation} / 후열: {string.Join(",", best.BackRow)}");
sb.AppendLine("자리: " + string.Join(" · ", fin.Party.Select((p, i) => $"{i + 1}.{p.Character.Name}({(p.IsBackPosition ? "후열" : "전열")})")));
sb.AppendLine();
sb.AppendLine("──── 캐릭터별 기여 ────");
foreach (var c in rep.CharacterResults.OrderByDescending(c => c.TotalDamage))
    sb.AppendLine($"  {c.CharacterName}: {c.TotalDamage:N0} ({c.DamageShare:F1}%)");
sb.AppendLine();
sb.AppendLine("──── 공격력 · 속공 ────");
for (int i = 0; i < fin.Party.Count; i++)
{
    var st = bsim.InitializeCharacterState(bcfg, fin.Party[i], i);
    sb.AppendLine($"  {fin.Party[i].Character.Name}: 공격력 {st.FinalAtk:N0} · 속공 {st.FinalSpd:N0}");
}
sb.AppendLine();
sb.AppendLine("──── 장비 ────");
foreach (var bc in fin.Party)
{
    var lo = bc.Equipment;
    sb.AppendLine($"[{bc.Character.Name}] 세트 {string.Join("+", lo.GetActiveSets().Select(x => $"{x.SetName}{x.PieceCount}"))}");
    foreach (var e in lo.GetEquipments())
        sb.AppendLine($"  {e.Name}: 메인 {e.MainStatName} {e.MainStatValue} | 부옵 {string.Join(", ", e.SubSlots.Where(x => !string.IsNullOrEmpty(x.StatName)).Select(x => $"{x.StatName} {x.DisplayValue}({x.Tier}티어)"))}");
    if (lo.Accessory != null) sb.AppendLine($"  장신구: {lo.Accessory.Grade}성 {lo.Accessory.RingName ?? lo.Accessory.MainOption} / 부 {lo.Accessory.SubOption ?? "-"}");
    var ex = bc.Character.ExclusiveWeapon;
    if (ex != null && ex.Tuning.Count > 0) sb.AppendLine($"  전용장비: {string.Join(", ", ex.Tuning.Select(t => $"{t.Option}({t.Grade})"))}");
}
sb.AppendLine();
sb.AppendLine("──── 스킬 순서 ────");
foreach (var f in rep.Feasibility.Where(f => f.Reached))
{
    string status = f.Hold ? "홀드" : f.IsAuto ? "자동" : f.ExecutedAsPlanned ? (f.CooldownGated ? $"여유 {f.Slack:F1}s" : "첫 시전") : $"폴백({f.FallbackReason})";
    var anno = new List<string>();
    if (f.BuffTargets != null && f.BuffTargets.Count > 0) anno.Add($"버프[{string.Join(",", f.BuffTargets)}]");
    if (f.DebuffTargets != null && f.DebuffTargets.Count > 0) anno.Add($"디버프[{string.Join(",", f.DebuffTargets)}]");
    if (f.DispelTargets != null && f.DispelTargets.Count > 0) anno.Add($"해제[{string.Join(",", f.DispelTargets)}]");
    string act = f.Hold ? "(홀드)" : $"{f.HeroName} → {f.SkillName}" + (anno.Count > 0 ? " " + string.Join(" ", anno) : "");
    sb.AppendLine($"  {f.StepIndex + 1,2}. T{f.Turn,2} {f.Elapsed,4:F0}s  {act}  [{status}]");
}
string summary = sb.ToString();
if (best.Log != null && best.Log.Count > 0)
{
    sb.AppendLine();
    sb.AppendLine("──── 탐색 로그 ────");
    foreach (var l in best.Log) sb.AppendLine("  " + l);
}
if (!loadOnly) System.IO.File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(true));

// 콘솔에는 탐색 로그를 뺀 요약만
Console.WriteLine();
Console.Write(summary);
Console.WriteLine($"점수 재현: {(reproduced ? "일치 ✓" : $"불일치 ✗ (기준 {bestTotal:N0} vs 재생 {fin.Res.TotalScore:N0})")}");
if (!loadOnly) Console.WriteLine($"→ {storePath}\n→ {txtPath}");

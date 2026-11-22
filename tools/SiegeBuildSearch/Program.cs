using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 공성 빌드 정밀 탐색 + 저장/불러오기 — 탐색 경로 2종.
//   탐색은 한 번에 1시간 안팎 걸리지만 찾은 빌드는 results/siege/build_*.json 에 구조화해 저장하므로
//   (장비·장신구·전용장비·자리·로테) 다음부터는 "불러오기"로 탐색 없이 재생한다.
//   저장 파일은 "지금까지 찾은 최고 빌드"다 — 다시 탐색해도 현재 엔진으로 재평가한 저장 빌드보다 높을 때만 바뀐다.
//
//   [고점 경로] (기본, `고점=<유저 최고점>`) — 속공 순서·공격력 순위(쿨감·버프 수령자)처럼 템 옵션을 따로 손봐야 하는 수까지
//       전부 탐색한다. 유저 최고점을 넣으면 출발 순서를 바꿔 가며 여러 번 탐색해(출발=N) 그 점수가 어느 단계의 손질로
//       나오는지(정석 → 공격력 순위 → 행동 순서 → 속공 선행 사다리), 더 높은 점수가 있는지를 본다.
//   [정석 경로] (`정석`) — 위 손질 없이 기본 탐색 + 로테 다듬기 + 생존 보강까지만. 따라 하기 쉬운 빌드.
//       고점 탐색의 앞부분이 곧 정석이라, 고점 탐색을 돌리면 정석 빌드도 함께 저장된다(파일 이름에 _정석).
//
// 사용: SiegeBuildSearch <요일> 팀=a,b,c,d,e [고점=13090000 | 정석] [출발=N] [다시] [진형=밸런스 진형] [후열=x,y] [불러오기] [연장끔] [턴로그] [각성기끔] [시도]
//        [6초월] [델로|리첼] [노각성] [dw] [quick] [nospd] [noorder] [nopolish] [nosurv] [norank]
//        [페널티N] [반격시드N] [평가시드N]
//   팀 순서 = 첫 탐색의 출발 순서(같은 속공이면 앞에서부터 행동). 저장 키는 팀 구성(순서 무관)이다.
//   출발=N: 출발 순서 N개(입력 순서 → 뒤집은 순서 → 한 칸씩 돌린 순서)로 탐색. 고점=점수를 주면 기본 3, 아니면 1.
//           같은 엔진에서 이미 탐색한 출발 순서는 건너뛴다(`다시`로 강제).
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;
string day = args.FirstOrDefault(a => EnemyDb.SiegeStages.ContainsKey(a));
string teamArg = args.FirstOrDefault(a => a.StartsWith("팀="))?.Substring(2);
if (day == null || teamArg == null)
{
    Console.WriteLine("사용: SiegeBuildSearch <요일> 팀=a,b,c,d,e [고점=<유저 최고점> | 정석] [출발=N] [다시] [진형=밸런스 진형] [후열=x,y] [불러오기] [연장끔] [6초월] [델로|리첼] [노각성] [dw] [quick]");
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
double? ParseScore(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return null;
    s = s.Replace(",", "").Replace("_", "").Trim();
    double mul = 1;
    if (s.EndsWith("M", StringComparison.OrdinalIgnoreCase)) { mul = 1e6; s = s[..^1]; }
    else if (s.EndsWith("억")) { mul = 1e8; s = s[..^1]; }
    else if (s.EndsWith("만")) { mul = 1e4; s = s[..^1]; }
    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0 ? v * mul : null;
}
bool quick = args.Contains("quick");
bool noExtend = args.Contains("연장끔");   // [진단] 「공격 N회마다 턴제 버프 +1턴」(쥬리 신의 저울)을 끄고 재생 — 저장 안 함
bool loadOnly = args.Contains("불러오기") || noExtend;
bool turnLog = args.Contains("턴로그");     // [진단] 턴별 행동 로그를 build_*_턴로그.txt 로 따로 저장
bool noAwkSkill = args.Contains("각성기끔"); // 각성 게이지 미모델 → 각성기(쿨 0)를 빼고 탐색(각성기 보유 영웅에겐 하한). 저장 프로필 분리(_각성기끔)
if (noAwkSkill)
    foreach (var c in CharacterDb.Characters) c.Skills?.RemoveAll(s => s.SkillType == SkillType.Awaken);
if (noExtend)
    foreach (var c in CharacterDb.Characters.Where(c => teamNames.Contains(c.Name) && c.Passive?.LevelData != null))
        foreach (var ld in c.Passive.LevelData.Values) ld.TimedBuffExtendEveryAttacks = 0;
bool lowSpec = args.Contains("6초월");
bool ingame = !args.Contains("dw");
bool awaken = !args.Contains("노각성") && (!lowSpec || args.Contains("각성"));
bool force = args.Contains("다시");
var route = args.Contains("정석") ? SiegeSearchRoute.Standard : SiegeSearchRoute.Peak;
double? targetArg = ParseScore(Val("고점=") ?? Val("목표="));
int TRANS = lowSpec ? 6 : 12, POT = lowSpec ? 0 : 3;
string petName = args.Contains("델로") ? "델로" : args.Contains("리첼") ? "리첼" : "윈디";
string formationArg = Val("진형=");
var backArg = Val("후열=")?.Split(',').Select(x => x.Trim()).ToList();
int starts = int.TryParse(Val("출발="), out var sn) && sn > 0 ? sn : (route == SiegeSearchRoute.Peak && targetArg != null ? 3 : 1);
double penalty = double.TryParse(Val("페널티"), out var pv) ? pv : (day == "일요일" ? 0 : 1_000_000);
int caSeeds = int.TryParse(Val("반격시드"), out var cs) && cs > 0 ? cs : day == "금요일" ? 10 : day == "일요일" ? 4 : 1;
int evalSeeds = int.TryParse(Val("평가시드"), out var es) && es > 0 ? es : day == "일요일" ? 16 : 1;

var stage = EnemyDb.SiegeStages[day];
var pet = PetDb.GetByName(petName);
string profile = $"{TRANS}초월_{petName}" + (awaken == !lowSpec ? "" : awaken ? "_각성" : "_노각성") + (ingame ? "" : "_dw") + (noAwkSkill ? "_각성기끔" : "") + (quick ? "_quick" : "");
string teamKey = string.Join("-", teamNames.Select(n => CharacterDb.Characters.First(c => c.Name == n)).OrderBy(c => c.Id).Select(c => c.Name));
string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string outDir = System.IO.Path.Combine(repoRoot, "results", "siege");
string RouteName(SiegeSearchRoute r) => r == SiegeSearchRoute.Standard ? "정석" : "고점";
string PathFor(SiegeSearchRoute r) => System.IO.Path.Combine(outDir, $"build_{day}_{profile}{(r == SiegeSearchRoute.Standard ? "_정석" : "")}_{teamKey}.json");

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
        // 동작 확인용 축소 탐색 — 정석 = 로테 다듬기까지, 고점 = + 공격력 순위
        cfg.RotationBeamWidth = 4; cfg.RotationMaxDepth = 28; cfg.QuickGear = true;
        cfg.PolishRotation = true;
        cfg.SearchAtkRank = route == SiegeSearchRoute.Peak && !args.Contains("norank");
    }
    else
    {
        cfg.RotationBeamWidth = 10; cfg.RotationMaxDepth = 36; cfg.CoordinateAscentGear = true;
        cfg.ApplyRoute(route);
        if (args.Contains("nopolish")) cfg.PolishRotation = false;
        if (args.Contains("nosurv")) cfg.SearchAggressiveSurvival = false;
        if (args.Contains("norank")) cfg.SearchAtkRank = false;
        if (args.Contains("noorder")) cfg.SearchActionOrder = false;
        if (args.Contains("nospd")) cfg.SearchSpeedLead = false;
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
string gitHead = GitHead();

// ── 시도 기록 (SiegeTrialStore) — 탐색이 평가한 후보를 도태된 것까지 전부 trials_*.jsonl 에 덧붙인다 ──
string trialPath = System.IO.Path.Combine(outDir, $"trials_{day}_{profile}_{teamKey}.jsonl");
if (args.Contains("시도"))
{
    var all = SiegeTrialStore.Load(trialPath);
    Console.WriteLine($"═══ 시도 기록 {day} [{teamKey}] {profile} — {all.Count}줄");
    Console.WriteLine($"    {trialPath}");
    if (all.Count == 0) { Console.WriteLine("기록이 없습니다. 먼저 탐색을 돌리세요."); return; }
    string Line(SiegeTrialRecord t) => $"{t.Score,12:N0} (랭크 {t.RankScore,12:N0}, 사망 {t.DeathCount:0.#}) · {t.Formation?.Replace(" 진형", "")}·후열 {string.Join("+", t.BackRow)} · 순서 {string.Join(",", t.Order)} · {t.Phase}{(t.ForcedBuffOrder != null ? $" [가정: 버프 수령 {string.Join(">", t.ForcedBuffOrder)}]" : "")} · 출발 {t.StartOrder}";
    double Median(List<double> v) { v.Sort(); return v.Count == 0 ? 0 : v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2; }
    var uniq = all.GroupBy(t => t.EngineHash + t.Fingerprint).Select(g => g.OrderByDescending(t => t.Adopted).First()).ToList();
    Console.WriteLine($"서로 다른 빌드 {uniq.Count}개 (그중 버프 수령자 강제 가정 {uniq.Count(t => t.ForcedBuffOrder != null)}개) · 단계 채택 기록 {all.Count(t => t.Adopted)}줄 · 엔진 {string.Join(", ", all.GroupBy(t => t.EngineHash).Select(g => $"{g.Key}×{g.Count()}"))}");
    int mismatch = all.Count(t => t.ReplayScore != null && Math.Abs(t.ReplayScore.Value - t.Score) > 1);
    if (mismatch > 0) Console.WriteLine($"  ⚠ 재생 점수가 기록 점수와 다른 줄 {mismatch}개 (N시드 평균 요일이면 정상)");
    Console.WriteLine();
    Console.WriteLine("──── 출발 순서별 ────");
    foreach (var g in all.GroupBy(t => t.StartOrder))
        Console.WriteLine($"  {g.Key}: {g.Count()}줄 · 랭크 최저 {g.Min(t => t.RankScore):N0} / 중앙 {Median(g.Select(t => t.RankScore).ToList()):N0} / 최고 {g.Max(t => t.RankScore):N0}");
    Console.WriteLine();
    Console.WriteLine("──── 단계별 (평가된 후보) ────");
    foreach (var g in all.Where(t => !t.Adopted).GroupBy(t => t.Phase))
        Console.WriteLine($"  {g.Key}: {g.Count()}건 · 랭크 최저 {g.Min(t => t.RankScore):N0} / 중앙 {Median(g.Select(t => t.RankScore).ToList()):N0} / 최고 {g.Max(t => t.RankScore):N0}");
    Console.WriteLine();
    Console.WriteLine("──── 랭크 상위 5 ────");
    foreach (var t in uniq.OrderByDescending(t => t.RankScore).Take(5)) Console.WriteLine("  " + Line(t));
    Console.WriteLine("──── 랭크 하위 5 ────");
    foreach (var t in uniq.OrderBy(t => t.RankScore).Take(5)) Console.WriteLine("  " + Line(t));
    if (args.Contains("검증"))
    {
        // 기록에서 빌드를 되살려 다시 돌렸을 때 기록된 점수가 그대로 나오는지 — 나중에 누수 분석이 이 기록을 재생해서 쓸 수 있는지 확인
        Console.WriteLine();
        Console.WriteLine("──── 재생 검증 (지금 엔진으로 기록된 시도만) ────");
        int ok = 0, bad = 0;
        foreach (var t in uniq.Where(t => t.EngineHash == engineHash))
        {
            var party = SiegeBuildStore.ToParty(t.Build);
            var plan = SiegeBuildStore.ToPlan(t.Build, party);
            var c = MakeCfg(party, null);
            if (t.ForcedBuffOrder != null)
            {
                c.HighestAtkAllyOrder = t.ForcedBuffOrder.Select(n => party.FindIndex(p => p.Character.Name == n)).ToList();
                c.HighestAtkAllyOverrideCdrOnly = t.ForcedCdrOnly;
            }
            var r = SiegeOptimizer.EvaluateBuild(c, party, t.Build.Formation, plan);
            if (Math.Abs(r.TotalScore - t.Score) <= 1) ok++;
            else { bad++; Console.WriteLine($"  ✗ {t.Phase} #{t.Seq} (출발 {t.StartOrder}): 기록 {t.Score:N0} → 재생 {r.TotalScore:N0}"); }
        }
        Console.WriteLine($"  일치 {ok}건 · 불일치 {bad}건" + (ok + bad < uniq.Count ? $" · 이전 엔진 기록 {uniq.Count - ok - bad}건은 건너뜀" : ""));
    }
    return;
}
// 같은 엔진에서 이미 기록한 빌드(지문)는 다시 적지 않는다
var seenTrials = SiegeTrialStore.Load(trialPath).Where(t => t.EngineHash == engineHash && !t.Adopted).Select(t => t.Fingerprint).ToHashSet();

// 저장된 빌드를 지금 엔진·지금 조건으로 다시 점수 낸다. (전용장비를 DB 영웅에 다시 꽂으므로 탐색 전후 각각 호출)
(SiegeBattleResult Res, List<BattleCharacter> Party, List<RotationDecision> Plan) Evaluate(SiegeSavedBuild b)
{
    var party = SiegeBuildStore.ToParty(b);
    var plan = SiegeBuildStore.ToPlan(b, party);
    var res = SiegeOptimizer.EvaluateBuild(MakeCfg(party, null), party, b.Formation, plan);
    return (res, party, plan);
}

// ── 1) 저장된 빌드 확인 (경로별 저장 파일) ──
Slot LoadSlot(SiegeSearchRoute r)
{
    var s = new Slot { Route = r, Name = RouteName(r), Path = PathFor(r) };
    s.Saved = SiegeBuildStore.Load(s.Path);
    if (s.Saved == null) { Console.WriteLine($"[저장된 빌드·{s.Name}] 없음"); return s; }
    var ev = Evaluate(s.Saved);
    s.SavedTotal = ev.Res.TotalScore; s.SavedRank = ev.Res.RankScore;
    int deaths = ev.Res.Deaths?.Count ?? 0;
    string fresh = s.Saved.EngineHash == engineHash ? "같은 엔진" : "엔진 변경됨";
    Console.WriteLine($"[저장된 빌드·{s.Name}] 저장 당시 {s.Saved.Score:N0} → 현재 재생 {s.SavedTotal:N0} (랭크 {s.SavedRank:N0}, 사망 {deaths}) · {fresh} · {s.Saved.SavedAt} · 출발 {s.Saved.StartOrder}");
    if (s.Saved.InGameBuffTargeting != ingame) Console.WriteLine($"  ⚠ 저장 당시 타게팅({(s.Saved.InGameBuffTargeting ? "인게임" : "DW")})과 지금 설정이 다릅니다.");
    s.History = s.Saved.History ?? new();
    if (s.History.Count == 0 && s.Saved.StartOrder != null)   // 기록 필드가 생기기 전에 저장된 파일
        s.History.Add(new SiegeSavedBuild.RunDto
        {
            StartOrder = s.Saved.StartOrder, FinalOrder = string.Join(",", s.Saved.Heroes.OrderBy(h => h.Slot).Select(h => h.Name)),
            Score = s.Saved.Score, RankScore = s.Saved.RankScore, Deaths = s.Saved.Deaths, Minutes = s.Saved.SearchMinutes,
            At = s.Saved.SavedAt, EngineHash = s.Saved.EngineHash,
        });
    // 빌드는 그대로 두고 점수만 현재 엔진 값으로 (파일에는 탐색 모드에서 저장할 때 반영)
    s.Saved.Score = Math.Round(s.SavedTotal); s.Saved.RankScore = Math.Round(s.SavedRank); s.Saved.Deaths = deaths; s.Saved.EngineHash = engineHash;
    s.Saved.Route ??= s.Name;
    s.Best = s.Saved; s.BestTotal = s.SavedTotal; s.BestRank = s.SavedRank;
    return s;
}

Console.WriteLine($"═══ {day} [{string.Join(",", teamNames)}] {profile} — {RouteName(route)} 경로");
var std = LoadSlot(SiegeSearchRoute.Standard);
var peak = route == SiegeSearchRoute.Peak ? LoadSlot(SiegeSearchRoute.Peak) : null;
var primary = peak ?? std;
double? target = route == SiegeSearchRoute.Peak ? targetArg ?? peak.Saved?.TargetScore : null;
if (target != null) Console.WriteLine($"[유저 최고점] {target:N0}");

if (loadOnly && primary.Saved == null)
{
    Console.WriteLine("불러올 빌드가 없습니다. '불러오기' 없이 실행해 먼저 탐색하세요.");
    return;
}

string Gap(double score, double basis) => $"{(score - basis) / basis * 100:+0.0;-0.0}%";
// 정규 근사로 "기댓값보다 zσ 이상 잘 나오는 판"이 몇 판 중 1판인지 (Abramowitz–Stegun erfc 근사)
string OneIn(double z)
{
    double x = Math.Abs(z) / Math.Sqrt(2), t = 1 / (1 + 0.3275911 * x);
    double erfc = t * (0.254829592 + t * (-0.284496736 + t * (1.421413741 + t * (-1.453152027 + t * 1.061405429)))) * Math.Exp(-x * x);
    double pUp = z >= 0 ? erfc / 2 : 1 - erfc / 2;
    return pUp <= 0 ? "∞" : Math.Max(1, Math.Round(1 / pUp)).ToString("N0");
}

// ── 2) 탐색 (불러오기면 생략) ──
void Offer(Slot slot, SiegeSavedBuild b, double total, double rank, SiegeSavedBuild.RunDto run)
{
    slot.History.Add(run);
    if (slot.History.Count > 40) slot.History.RemoveRange(0, slot.History.Count - 40);
    if (SiegeOptimizer.IsBetterBuild(total, rank, slot.BestTotal, slot.BestRank))
    {
        Console.WriteLine(slot.Best == null ? $"  [저장·{slot.Name}] 새 빌드 {total:N0}" : $"  [저장·{slot.Name}] 갱신 {slot.BestTotal:N0} → {total:N0}");
        slot.Best = b; slot.BestTotal = total; slot.BestRank = rank; slot.BestIsNew = true;
    }
    else
    {
        Console.WriteLine($"  [유지·{slot.Name}] 저장된 빌드 {slot.BestTotal:N0}가 이번 결과 {total:N0} 이상 — 빌드를 바꾸지 않습니다.");
        // 같은 점수로 다시 찾았는데 저장 빌드에 단계별 점수가 없으면(예전 파일) 이번 탐색의 것을 채운다
        if (Math.Abs(total - slot.BestTotal) < 1 && (slot.Best.Stages == null || slot.Best.Stages.Count == 0)) slot.Best.Stages = b.Stages;
    }
    slot.Best.History = slot.History;
    if (slot.Route == SiegeSearchRoute.Peak && target != null) slot.Best.TargetScore = target;
    SiegeBuildStore.Save(slot.Path, slot.Best);   // 출발 순서 하나가 끝날 때마다 저장 — 중간에 끊겨도 그때까지의 최고가 남는다
}

if (!loadOnly)
{
    // 출발 순서 후보: 입력 순서 → 뒤집은 순서 → 한 칸씩 돌린 순서. 같은 엔진에서 이미 돌린 순서는 건너뛴다.
    var cands = new List<List<string>> { teamNames.ToList(), Enumerable.Reverse(teamNames).ToList() };
    for (int r = 1; r < teamNames.Count; r++) cands.Add(teamNames.Skip(r).Concat(teamNames.Take(r)).ToList());
    cands = cands.GroupBy(o => string.Join(",", o)).Select(g => g.First()).ToList();
    var tried = primary.History.Where(h => h.EngineHash == engineHash).GroupBy(h => h.StartOrder).ToDictionary(g => g.Key, g => g.Max(h => h.Score));
    var todo = new List<List<string>>();
    foreach (var o in cands)
    {
        if (todo.Count >= starts) break;
        string key = string.Join(",", o);
        if (!force && tried.TryGetValue(key, out var prev)) { Console.WriteLine($"[건너뜀] 출발 {key} — 이 엔진에서 이미 탐색함({prev:N0}). 다시 돌리려면 '다시'."); continue; }
        todo.Add(o);
    }
    if (todo.Count == 0) Console.WriteLine("탐색할 새 출발 순서가 없습니다 — 저장된 빌드를 그대로 보여 줍니다. (팀= 순서를 바꾸거나 '다시')");

    var found = new List<SiegeSavedBuild>();
    for (int si = 0; si < todo.Count; si++)
    {
        var order = todo[si];
        string startOrder = string.Join(",", order);
        var team = order.Select(Hero).ToList();
        var seedBuilds = new[] { peak?.Best, std.Best }.Where(b => b != null).Concat(found).Distinct().ToList();
        var seeds = seedBuilds.Select(b => SiegeBuildStore.ToPlan(b, team)).Where(p => p.Count > 0).ToList();
        var cfg = MakeCfg(team, seeds);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var stages = new List<SiegeSavedBuild.StageDto>();

        SiegeSavedBuild Snap(SiegeOptimizerResult r, string routeName)
        {
            var b = SiegeBuildStore.Capture(r.BestParty, r.BestFormation, r.BestBackRow, r.BestRotationPlan);
            b.Day = day; b.Profile = profile; b.Route = routeName;
            b.Score = Math.Round(r.BestScore); b.RankScore = Math.Round(r.BestRankScore);
            b.Deaths = r.BestResult?.Deaths?.Count ?? 0; b.InGameBuffTargeting = ingame;
            b.EngineHash = engineHash; b.GitCommit = gitHead; b.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            b.StartOrder = startOrder; b.SearchMinutes = Math.Round(sw.Elapsed.TotalMinutes, 1);
            b.Pet = new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 76 };
            b.Log = r.GearLog.ToList();
            b.Stages = stages.Select(s => new SiegeSavedBuild.StageDto { Name = s.Name, Score = s.Score, RankScore = s.RankScore, Deaths = s.Deaths, Order = s.Order, Standard = s.Standard }).ToList();
            return b;
        }
        SiegeSavedBuild.RunDto Run(SiegeSavedBuild b) => new()
        {
            StartOrder = startOrder, FinalOrder = string.Join(",", b.Heroes.OrderBy(h => h.Slot).Select(h => h.Name)),
            Score = b.Score, RankScore = b.RankScore, Deaths = b.Deaths, Minutes = b.SearchMinutes, At = b.SavedAt, EngineHash = engineHash,
        };

        // 시도 기록 — 평가된 후보(OnTrial)와 단계별 채택 빌드(OnStage)를 그 자리에서 복사해 trials_*.jsonl 에 덧붙인다.
        //   party는 탐색이 계속 고쳐 쓰는 공유 객체라 콜백 안에서 바로 옮겨 적는다. 스킬 순서는 같은 빌드를 한 번 더 돌려 얻는다.
        int trialSeq = 0, trialNew = 0;
        void RecordTrial(string phase, bool adopted, List<BattleCharacter> party, string formation, List<RotationDecision> plan, SiegeBattleResult r)
        {
            if (r == null || party == null || party.Count == 0) return;
            try
            {
                var back = party.Where(p => p.IsBackPosition).Select(p => p.Character.Name).ToList();
                // 버프 수령자 강제(공격력 순위 탐색의 가정 평가) 중이면 그 순서도 지문·기록에 넣는다 — 빌드만으로는 재현되지 않는 점수
                var forced = cfg.HighestAtkAllyOrder?.Where(i => i >= 0 && i < party.Count).Select(i => party[i].Character.Name).ToList();
                string fp = SiegeTrialStore.Fingerprint(SiegeBuildStore.Capture(party, formation, back, plan))
                    + (forced != null ? "|강제:" + string.Join(">", forced) + (cfg.HighestAtkAllyOverrideCdrOnly ? "(쿨감만)" : "") : "");
                if (!adopted && !seenTrials.Add(fp)) return;   // 같은 빌드를 다시 평가한 것 — 이미 기록됨
                var replay = plan != null && plan.Count > 0 ? SiegeOptimizer.ReplayWithCasts(cfg, party, formation, plan) : null;
                var rec = SiegeTrialStore.Make(party, formation, plan, r, replay, penalty);
                rec.At = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); rec.Day = day; rec.Profile = profile; rec.TeamKey = teamKey;
                rec.StartOrder = startOrder; rec.Phase = phase; rec.Seq = trialSeq++; rec.Adopted = adopted;
                rec.EngineHash = engineHash; rec.GitCommit = gitHead;
                rec.ForcedBuffOrder = forced; rec.ForcedCdrOnly = forced != null && cfg.HighestAtkAllyOverrideCdrOnly;
                rec.Fingerprint = fp;
                SiegeTrialStore.Append(trialPath, rec);
                trialNew++;
            }
            catch (Exception ex) { Console.WriteLine($"    [시도 기록 실패] {phase}: {ex.Message}"); }
        }
        cfg.OnTrial = t => RecordTrial(t.Phase, false, t.Team, t.Formation, t.Plan, t.Result);

        // 정석 경로가 끝난 시점의 결과를 그 자리에서 복사해 둔다(이후 고점 단계가 같은 객체를 고쳐 쓴다).
        SiegeSavedBuild stdSnap = null; double stdTotal = 0, stdRank = 0;
        cfg.OnStage = (name, r) =>
        {
            if (name != SiegeOptimizerConfig.StandardStage)
            {
                // 채택 빌드의 자리(후열)를 파티에 맞춰 놓고 기록한 뒤 되돌린다 — 직전 후보 평가의 자리 표시가 남아 있을 수 있다.
                var flags = r.BestParty.Select(p => p.IsBackPosition).ToList();
                var backSet = new HashSet<string>(r.BestBackRow ?? new List<string>());
                foreach (var p in r.BestParty) p.IsBackPosition = backSet.Contains(p.Character.Name);
                RecordTrial(name, true, r.BestParty, r.BestFormation, r.BestRotationPlan, r.BestResult);
                for (int i = 0; i < flags.Count; i++) r.BestParty[i].IsBackPosition = flags[i];
            }
            if (name == SiegeOptimizerConfig.StandardStage)
            {
                foreach (var s in stages) s.Standard = true;
                stdSnap = Snap(r, "정석"); stdTotal = r.BestScore; stdRank = r.BestRankScore;
                return;
            }
            var st = new SiegeSavedBuild.StageDto
            {
                Name = name, Score = Math.Round(r.BestScore), RankScore = Math.Round(r.BestRankScore),
                Deaths = r.BestResult?.Deaths?.Count ?? 0, Order = string.Join(",", r.BestParty.Select(x => x.Character.Name)),
            };
            stages.Add(st);
            Console.WriteLine($"    · {name}: {st.Score:N0} (사망 {st.Deaths}){(target != null ? $" · 유저 대비 {Gap(st.Score, target.Value)}" : "")} · {sw.Elapsed.TotalMinutes:F1}분");
        };

        Console.WriteLine($"[탐색 {si + 1}/{todo.Count}] 출발 {startOrder}");
        var o2 = new SiegeOptimizer().Optimize(cfg);
        sw.Stop();
        string finalOrder = string.Join(",", o2.BestParty.Select(x => x.Character.Name));
        Console.WriteLine($"  → {o2.BestScore:N0} (랭크 {o2.BestRankScore:N0}, 사망 {o2.BestResult?.Deaths?.Count ?? 0}) · {o2.BestFormation} 후열 {string.Join(",", o2.BestBackRow)} · 최종 순서 {finalOrder} · {sw.Elapsed.TotalMinutes:F1}분"
            + (target != null ? $" · 유저 최고점 대비 {Gap(o2.BestScore, target.Value)}" : ""));
        Console.WriteLine($"  [시도 기록] {trialNew}건 추가 → {System.IO.Path.GetFileName(trialPath)}");
        if (stdSnap != null) { Offer(std, stdSnap, stdTotal, stdRank, Run(stdSnap)); found.Add(stdSnap); }
        if (peak != null)
        {
            var b = Snap(o2, "고점");
            Offer(peak, b, o2.BestScore, o2.BestRankScore, Run(b));
            found.Add(b);
        }
    }
}

// ── 3) 리포트 (경로별: 최고 빌드를 되살려 재현 확인) ──
string SetStr(SiegeSavedBuild.HeroDto h) => string.Join("+", h.Gear.GroupBy(g => g.Set ?? "-").OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => $"{g.Key}{g.Count()}"));
string MainStr(SiegeSavedBuild.HeroDto h) => string.Join("/", h.Gear.Select(g => g.Main));
Dictionary<string, int> SubSum(SiegeSavedBuild.HeroDto h) => h.Gear.SelectMany(g => g.Subs).GroupBy(s => s.Stat).ToDictionary(g => g.Key, g => g.Sum(s => s.Tier));
string AccStr(SiegeSavedBuild.HeroDto h) => h.Accessory == null ? "-" : $"{h.Accessory.Grade}성 {h.Accessory.Ring ?? h.Accessory.Main}/{h.Accessory.Sub ?? "-"}";
string ExStr(SiegeSavedBuild.HeroDto h) => h.Exclusive == null || h.Exclusive.Tuning.Count == 0 ? "-" : string.Join(",", h.Exclusive.Tuning.Select(t => $"{t.Option}({t.Grade})"));
string OrderOf(SiegeSavedBuild b) => string.Join(" · ", b.Heroes.OrderBy(h => h.Slot).Select(h => h.Name));

// 정석 빌드에서 고점 빌드로 가려면 무엇을 손봐야 하는지 (자리 순서 · 영웅별 장비 차이)
List<string> DiffLines(SiegeSavedBuild a, SiegeSavedBuild b)
{
    var lines = new List<string>();
    if (a.Formation != b.Formation) lines.Add($"진형: {a.Formation} → {b.Formation}");
    if (OrderOf(a) != OrderOf(b)) lines.Add($"진형 번호 순(=같은 속공일 때 행동 순서): {OrderOf(a)} → {OrderOf(b)}");
    if (!a.BackRow.OrderBy(x => x).SequenceEqual(b.BackRow.OrderBy(x => x))) lines.Add($"후열: {string.Join(",", a.BackRow)} → {string.Join(",", b.BackRow)}");
    foreach (var hb in b.Heroes.OrderBy(h => h.Slot))
    {
        var ha = a.Heroes.FirstOrDefault(h => h.Id == hb.Id);
        if (ha == null) continue;
        var d = new List<string>();
        if (SetStr(ha) != SetStr(hb)) d.Add($"세트 {SetStr(ha)} → {SetStr(hb)}");
        if (MainStr(ha) != MainStr(hb)) d.Add($"메인옵 {MainStr(ha)} → {MainStr(hb)}");
        var sa = SubSum(ha); var sb2 = SubSum(hb);
        var subDiff = sa.Keys.Union(sb2.Keys).Select(k => (K: k, A: sa.GetValueOrDefault(k), B: sb2.GetValueOrDefault(k))).Where(x => x.A != x.B)
            .OrderByDescending(x => x.B - x.A).Select(x => $"{x.K} {x.A}→{x.B}티어").ToList();
        if (subDiff.Count > 0) d.Add("부옵 " + string.Join(", ", subDiff));
        if (AccStr(ha) != AccStr(hb)) d.Add($"장신구 {AccStr(ha)} → {AccStr(hb)}");
        if (ExStr(ha) != ExStr(hb)) d.Add($"전용장비 {ExStr(ha)} → {ExStr(hb)}");
        if (d.Count > 0) lines.Add($"[{hb.Name}] " + string.Join(" · ", d));
    }
    bool sameRot = a.Rotation.Count == b.Rotation.Count && a.Rotation.Zip(b.Rotation, (x, y) => x.Hold == y.Hold && x.Hero == y.Hero && x.Skill == y.Skill).All(x => x);
    if (!sameRot) lines.Add("스킬 순서가 다름 (각 빌드의 스킬 순서 참조)");
    if (lines.Count == 0) lines.Add("차이 없음 — 이 팀은 정석 빌드가 곧 고점 빌드입니다.");
    return lines;
}

(string Summary, bool Reproduced) Report(Slot slot, Slot other)
{
    var best = slot.Best;
    var fin = Evaluate(best);
    bool reproduced = Math.Abs(fin.Res.TotalScore - slot.BestTotal) < 1.0;
    var rep = new SiegeBattleSimulator(777).Simulate(new SiegeBattleConfig
    {
        AllyParty = fin.Party, FormationName = best.Formation, SiegeStage = stage,
        AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
        RotationPlan = fin.Plan, AllyDeathPenalty = penalty, RecordFeasibility = true, InGameBuffTargeting = ingame,
    });
    var bcfg = new BattleConfig { AllyParty = fin.Party, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, FormationName = best.Formation };
    var bsim = new BattleSimulator();
    var sb = new StringBuilder();
    sb.AppendLine($"════════ {day} 공성전 빌드 [{slot.Name} 경로] — {profile} · 타게팅 {(ingame ? "인게임(현재 공격력)" : "DW")} ════════");
    sb.AppendLine(slot.Route == SiegeSearchRoute.Standard
        ? "정석 경로: 속공 순서·공격력 순위를 맞추려고 템 옵션을 따로 손보지 않는 빌드 (기본 탐색 + 로테 다듬기 + 생존 보강)"
        : "고점 경로: 공격력 순위(버프·쿨감 수령자)·행동 순서·속공 선행까지 손본 빌드");
    sb.AppendLine($"[저장] {best.SavedAt} · engineHash={best.EngineHash} · git={best.GitCommit} · 탐색 {best.SearchMinutes:F1}분 · 출발 순서 {best.StartOrder}");
    sb.AppendLine($"보스: {stage.Name}");
    sb.AppendLine($"총점: {fin.Res.TotalScore:N0} (랭크 {fin.Res.RankScore:N0}) · {rep.RoundsCleared}R 클리어 · 사망 {rep.Deaths.Count}"
        + (rep.Deaths.Count > 0 ? " (" + string.Join(", ", rep.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")" : ""));
    if (other?.Best != null)
        sb.AppendLine($"{other.Name} 경로 저장 빌드: {other.BestTotal:N0} ({slot.Name} 대비 {Gap(other.BestTotal, fin.Res.TotalScore)})");
    double? tgt = slot.Route == SiegeSearchRoute.Peak ? target ?? best.TargetScore : null;
    if (tgt != null)
    {
        double diff = fin.Res.TotalScore - tgt.Value;
        sb.AppendLine($"유저 최고점: {tgt:N0} → 탐색 최고 {fin.Res.TotalScore:N0} ({Gap(fin.Res.TotalScore, tgt.Value)}, {diff:+#,0;-#,0})");
        var hit = best.Stages.FirstOrDefault(s => s.Score >= tgt.Value);
        if (diff >= 0 && hit != null)
            sb.AppendLine(hit.Standard
                ? $"  ▶ 유저 최고점은 정석 경로('{hit.Name}' 단계)만으로 나옵니다. 고점 경로는 그보다 {Gap(fin.Res.TotalScore, tgt.Value)} 높습니다."
                : $"  ▶ 유저 최고점은 '{hit.Name}' 단계에서 처음 넘습니다 — 그 단계까지의 손질이 들어간 빌드로 보이며, 끝까지 손보면 {Gap(fin.Res.TotalScore, tgt.Value)} 더 낼 수 있습니다.");
        else if (diff >= 0)
            sb.AppendLine("  ▶ 저장된 빌드가 유저 최고점보다 높습니다.");
        else if (rep.CoopStdDev > 0 && -diff <= 3 * rep.CoopStdDev)
            sb.AppendLine($"  ▶ 기댓값으로는 미달이지만 협공 운으로 닿는 범위입니다 — 이 빌드에서 협공이 +{-diff / rep.CoopStdDev:0.0}σ 터진 판(약 {OneIn(-diff / rep.CoopStdDev)}판 중 1판)의 점수입니다.");
        else
            sb.AppendLine("  ▶ 미달 — 운(협공)으로도 설명되지 않는 차이입니다. 출발 순서를 늘려 더 탐색하거나(출발=N), 미모델 요소를 확인해야 합니다.");
    }
    if (rep.CoopJudgments > 0 && rep.CoopStdDev > 0)
    {
        // 시뮬 점수는 협공을 기댓값으로 넣은 값 — 실제 한 판은 협공이 몇 번 터지느냐로 흔들린다(유저 최고점은 잘 터진 판).
        double total = fin.Res.TotalScore, sd = rep.CoopStdDev;
        sb.AppendLine($"협공 운: 판정 {rep.CoopJudgments}회 · 기대 발동 {rep.CoopExpectedProcs:F1}회 · 협공 기댓값 {rep.CoopExpectedDamage:N0} (총점의 {rep.CoopExpectedDamage / total * 100:F1}%)");
        sb.AppendLine($"  한 판의 흔들림 1σ = ±{sd:N0} ({sd / total * 100:F1}%) → 잘 터진 판 +1σ {total + sd:N0} · +2σ {total + 2 * sd:N0} · +3σ {total + 3 * sd:N0}");
    }
    {
        // 턴제 버프 증가(쥬리 「신의 저울」: 자신의 모든 공격 3회마다 자신의 턴제 버프 +1턴) — 몇 번, 어떤 버프가 늘었는지
        var ext = rep.TurnLogs.Where(l => l.SkillName == "턴제 버프 증가").ToList();
        if (noExtend) sb.AppendLine("턴제 버프 증가: [진단] 꺼 둔 상태로 재생한 점수입니다.");
        else if (ext.Count > 0)
        {
            sb.AppendLine($"턴제 버프 증가: {ext.Count}회 (" + string.Join(", ", ext.GroupBy(l => l.ActorName).Select(g => $"{g.Key} {g.Count()}회")) + ")");
            foreach (var l in ext) sb.AppendLine($"  T{l.Turn} {l.ActorName}: {l.Description}");
        }
    }
    sb.AppendLine($"진형: {best.Formation} / 후열: {string.Join(",", best.BackRow)}");
    sb.AppendLine("자리: " + string.Join(" · ", fin.Party.Select((p, i) => $"{i + 1}.{p.Character.Name}({(p.IsBackPosition ? "후열" : "전열")})")));
    if (best.Stages.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("──── 탐색 단계별 점수 ────");
        bool lineDrawn = false;
        foreach (var s in best.Stages)
        {
            if (!s.Standard && !lineDrawn) { sb.AppendLine("  ─ 여기까지 정석 / 아래는 고점 경로 전용 ─"); lineDrawn = true; }
            sb.AppendLine($"  {s.Name}: {s.Score:N0} (사망 {s.Deaths})" + (tgt != null ? $" · 유저 대비 {Gap(s.Score, tgt.Value)}" : "") + $" · 순서 {s.Order}");
        }
    }
    if (slot.Route == SiegeSearchRoute.Peak && other?.Best != null)
    {
        sb.AppendLine();
        sb.AppendLine("──── 정석 빌드에서 손봐야 하는 것 ────");
        foreach (var l in DiffLines(other.Best, best)) sb.AppendLine("  " + l);
    }
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
    if (slot.History.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("──── 탐색 기록 (출발 순서별) ────");
        foreach (var h in slot.History)
            sb.AppendLine($"  {h.At}  출발 {h.StartOrder} → {h.Score:N0} (사망 {h.Deaths}) · 최종 순서 {h.FinalOrder} · {h.Minutes:F1}분" + (h.EngineHash == engineHash ? "" : " · 이전 엔진"));
    }
    string summary = sb.ToString();
    if (best.Log != null && best.Log.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("──── 탐색 로그 ────");
        foreach (var l in best.Log) sb.AppendLine("  " + l);
    }
    if (!loadOnly) System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(slot.Path, ".txt"), sb.ToString(), new UTF8Encoding(true));
    if (turnLog)
    {
        var tl = new StringBuilder(summary);
        tl.AppendLine();
        tl.AppendLine($"──── 턴별 행동 로그 (총 {rep.TurnLogs.Count}개) ────");
        foreach (var l in rep.TurnLogs)
            tl.AppendLine($"T{l.Turn,2} [{(l.IsAlly ? "아군" : "적 ")}] {l.ActorName} · {l.SkillName}: {l.Description}");
        string tlPath = System.IO.Path.ChangeExtension(slot.Path, null) + "_턴로그.txt";
        System.IO.File.WriteAllText(tlPath, tl.ToString(), new UTF8Encoding(true));
        Console.WriteLine($"[턴로그] {tlPath}");
    }
    return (summary, reproduced);
}

// 주 경로가 아닌 쪽(고점 탐색 때 함께 저장된 정석 빌드)은 파일만 쓰고, 콘솔에는 주 경로 요약을 보여 준다.
var secondary = primary == peak ? std : null;
if (secondary?.Best != null && !loadOnly)
{
    var r2 = Report(secondary, primary);
    Console.WriteLine($"[{secondary.Name} 빌드] {secondary.BestTotal:N0} · 점수 재현 {(r2.Reproduced ? "일치 ✓" : "불일치 ✗")} → {System.IO.Path.ChangeExtension(secondary.Path, ".txt")}");
}
if (primary.Best == null) { Console.WriteLine("저장된 빌드가 없습니다."); return; }
var r1 = Report(primary, primary == peak ? std : null);
Console.WriteLine();
Console.Write(r1.Summary);
Console.WriteLine($"점수 재현: {(r1.Reproduced ? "일치 ✓" : $"불일치 ✗ (기준 {primary.BestTotal:N0})")}");
if (!loadOnly) Console.WriteLine($"→ {primary.Path}\n→ {System.IO.Path.ChangeExtension(primary.Path, ".txt")}");

// 경로별 저장 슬롯 — 파일에 있던 빌드와 이번 실행에서 찾은 최고 빌드
class Slot
{
    public SiegeSearchRoute Route;
    public string Name;
    public string Path;
    public SiegeSavedBuild Saved;
    public SiegeSavedBuild Best;
    public double SavedTotal = double.NegativeInfinity, SavedRank = double.NegativeInfinity;
    public double BestTotal = double.NegativeInfinity, BestRank = double.NegativeInfinity;
    public bool BestIsNew;
    public List<SiegeSavedBuild.RunDto> History = new();
}

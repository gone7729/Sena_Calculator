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
//        [6초월] [델로|리첼] [노각성] [dw] [quick] [nospd] [noorder] [nopolish] [nosurv] [noedit] [norank]
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
// 불사탐색: [진단] 아군이 죽지 않는다고 놓고 탐색 — 이 편성이 낼 수 있는 순수 딜 천장(생존 제약 없음). 저장 프로필 분리(_불사).
//   유저 최고점이 이 천장보다 높으면 탐색이 아니라 모델(횟수·시간·미모델 효과)이 모자란 것이다.
bool immortalSearch = args.Contains("불사탐색");
// 프로필=이름: 저장 프로필에 꼬리표를 붙여 따로 저장 — 실험용 빌드(예: 불사 빌드를 복사해 필멸 조건으로 손보기)가 정식 저장 빌드를 덮지 않게.
string profTag = args.FirstOrDefault(a => a.StartsWith("프로필="))?.Substring(4);
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
// 펫잠재=66: 펫 잠재 모든 공격력% 합(기본 72 = 18×4). 유저 빌드 대조용.
double PETPOT = double.TryParse(args.FirstOrDefault(a => a.StartsWith("펫잠재="))?.Substring(4), out var pp0) ? pp0 : 72;
string petName = args.Contains("델로") ? "델로" : args.Contains("리첼") ? "리첼" : "윈디";
string formationArg = Val("진형=");
var backArg = Val("후열=")?.Split(',').Select(x => x.Trim()).ToList();
int starts = int.TryParse(Val("출발="), out var sn) && sn > 0 ? sn : (route == SiegeSearchRoute.Peak && targetArg != null ? 3 : 1);
double penalty = double.TryParse(Val("페널티"), out var pv) ? pv : (day == "일요일" ? 0 : 1_000_000);
int caSeeds = int.TryParse(Val("반격시드"), out var cs) && cs > 0 ? cs : day == "금요일" ? 10 : day == "일요일" ? 4 : 1;
int evalSeeds = int.TryParse(Val("평가시드"), out var es) && es > 0 ? es : day == "일요일" ? 16 : 1;

var stage = EnemyDb.SiegeStages[day];
var pet = PetDb.GetByName(petName);
string profile = $"{TRANS}초월_{petName}" + (awaken == !lowSpec ? "" : awaken ? "_각성" : "_노각성") + (ingame ? "" : "_dw") + (noAwkSkill ? "_각성기끔" : "") + (immortalSearch ? "_불사" : "") + (profTag != null ? "_" + profTag : "") + (quick ? "_quick" : "");
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
        AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, MaxTurns = 70,
        AutoEquip = true, SearchExclusiveWeapon = !quick && !lowSpec, EnableSurvivalRings = !quick && !lowSpec,
        FloorFirstGear = true, AllyDeathPenalty = penalty, OptimizeRotation = true, InGameBuffTargeting = ingame,
        ForcedFormation = formationArg, ForcedBackRow = backArg,
        CounterattackSearchSeeds = caSeeds, EvalSeeds = evalSeeds,
        SeedRotations = seeds != null && seeds.Count > 0 ? seeds : null,
        DiagAlliesImmortal = immortalSearch,
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
        if (args.Contains("noedit")) cfg.SearchEditSurvival = false;
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
// ── 누수 장부·반사실 공용 ──
int W(string s) => s.Sum(ch => ch >= 0x1100 ? 2 : 1);
string PadL(string s, int w) => new string(' ', Math.Max(0, w - W(s))) + s;
string PadR(string s, int w) => s + new string(' ', Math.Max(0, w - W(s)));

// 기록된 시도를 빌드(지문)당 한 번씩 누수 장부를 켜고 다시 돌린다 — 채택 기록·최근 기록 우선
List<(SiegeTrialRecord T, SiegeLeakRecord R)> BuildLeakRecs(List<SiegeTrialRecord> trials, out int recalcBad)
{
    var uniq = trials.GroupBy(t => t.Fingerprint).Select(g => g.OrderByDescending(t => t.Adopted).ThenByDescending(t => t.At).First()).ToList();
    var adoptedFp = trials.Where(t => t.Adopted).Select(t => t.Fingerprint).ToHashSet();
    var finalFp = trials.Where(t => t.Adopted).GroupBy(t => t.EngineHash + "|" + t.StartOrder)
        .Select(g => g.OrderByDescending(t => t.At).ThenByDescending(t => t.Seq).First().Fingerprint).ToHashSet();
    var list = new List<(SiegeTrialRecord, SiegeLeakRecord)>();
    recalcBad = 0;
    foreach (var t in uniq)
    {
        var party = SiegeBuildStore.ToParty(t.Build);
        var plan = SiegeBuildStore.ToPlan(t.Build, party);
        var c = MakeCfg(party, null);
        if (t.ForcedBuffOrder != null)
        {
            c.HighestAtkAllyOrder = t.ForcedBuffOrder.Select(n => party.FindIndex(p => p.Character.Name == n)).ToList();
            c.HighestAtkAllyOverrideCdrOnly = t.ForcedCdrOnly;
        }
        var r = SiegeOptimizer.ReplayWithCasts(c, party, t.Build.Formation, plan, leak: true);
        recalcBad += r.Leak.RecalcMismatch;
        list.Add((t, new SiegeLeakRecord
        {
            Fingerprint = t.Fingerprint, Score = t.Score, RankScore = t.RankScore, DeathCount = t.DeathCount,
            Phase = t.Phase, StartOrder = t.StartOrder, Adopted = adoptedFp.Contains(t.Fingerprint), Final = finalFp.Contains(t.Fingerprint),
            ForcedBuffOrder = t.ForcedBuffOrder, Formation = t.Formation, BackRow = t.BackRow, Order = t.Order,
            ReplayOk = Math.Abs(r.TotalScore - (t.ReplayScore ?? t.Score)) <= 1, ReplayScore = Math.Round(r.TotalScore), Leak = r.Leak,
        }));
    }
    return list;
}

// 한 전투의 누수 장부 상세(기준 상태 · 계수 누수 · 깎인 딜 · 사망 · 행별 누수)
void PrintLeakDetail(SiegeLeakLedger L)
{
    string P(double v) => L.Score > 0 ? $"{v / L.Score * 100:0.0}%" : "-";
    Console.WriteLine($"  점수 {L.Score:N0} = 타격 {L.HitScored:N0} + DoT {L.DotScored:N0} · 아군 스킬턴 {L.SkillTurns}회(폴백 {L.Fallbacks} · 홀드 {L.Holds})");
    Console.WriteLine("  기준이 된 최고 상태(전투 중 값이 변한 것만): " + (L.Best.Count == 0 ? "없음" : string.Join(" · ", L.Best.Select(kv => $"{kv.Key} {kv.Value:0.#}"))));
    Console.WriteLine("  계수 누수: " + string.Join(" · ", L.Factor.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:N0}({P(kv.Value)})"))
        + $" │ 동시 {L.AllBest:N0}({P(L.AllBest)})");
    if (L.Lost.Count > 0) Console.WriteLine("  깎인 딜: " + string.Join(" · ", L.Lost.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:N0}({P(kv.Value)})")));
    if (L.DeadTurns.Count > 0) Console.WriteLine("  사망: " + string.Join(" · ", L.DeadTurns.Select(kv => $"{kv.Key} {kv.Value}턴")) + $" → 추정 손실 {L.DeathLossEst:N0}({P(L.DeathLossEst)})");
    if (L.IdleSec.Count > 0) Console.WriteLine("  쿨이 돈 뒤 대기(초, 참고): " + string.Join(" · ", L.IdleSec.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:0.#}")));
    Console.WriteLine();
    Console.WriteLine("  " + PadR("스텝", 5) + PadR("턴", 4) + PadR("영웅", 10) + PadR("스킬", 26) + PadL("점수", 12) + PadL("동시 누수", 12) + "  누수 큰 계수 / 깎인 딜");
    foreach (var row in L.Rows)
    {
        string fs = string.Join(" · ", row.Factor.Where(kv => kv.Value >= 1).OrderByDescending(kv => kv.Value).Take(4).Select(kv => $"{kv.Key} {kv.Value:N0}"));
        string ls = row.Lost == null ? "" : string.Join(" · ", row.Lost.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} −{kv.Value:N0}"));
        if (row.Targets != null) ls = ls.Length > 0 ? $"{ls} │ {row.Targets}" : row.Targets;
        Console.WriteLine("  " + PadR(row.Step >= 0 ? row.Step.ToString() : "-", 5) + PadR(row.Turn >= 0 ? $"T{row.Turn}" : "-", 4) + PadR(row.Hero ?? "", 10) + PadR(row.Skill ?? "", 26)
            + PadL(row.Scored.ToString("N0"), 12) + PadL(row.AllBest.ToString("N0"), 12) + "  " + fs + (fs.Length > 0 && ls.Length > 0 ? " │ " : "") + ls);
    }
}

// ── 누수 장부: 기록된 시도를 다시 돌려 "그 전투의 최고 상태 대비 덜 들어간 딜"을 계수별로 계산 (SiegeLeakLedger) ──
//   `누수` = 구간 평균 + 상·하위 시도 + 최고 빌드 상세.  `상세=N` = 랭크 N위 시도의 상세.
string leakPath = System.IO.Path.Combine(outDir, $"leaks_{day}_{profile}_{teamKey}.jsonl");
if (args.Contains("누수"))
{
    var all = SiegeTrialStore.Load(trialPath);
    Console.WriteLine($"═══ 누수 장부 {day} [{teamKey}] {profile}");
    if (all.Count == 0) { Console.WriteLine($"시도 기록이 없습니다({trialPath}). 먼저 탐색을 돌리세요."); return; }
    var recs = BuildLeakRecs(all, out int recalcBad).Select(x => x.R).ToList();
    SiegeLeakStore.Save(leakPath, recs);

    var good = recs.Where(x => x.ReplayOk).OrderByDescending(x => x.RankScore).ToList();
    int bad = recs.Count - good.Count;
    Console.WriteLine($"시도 {recs.Count}개 재생 · 점수 재현 {good.Count}개" + (bad > 0 ? $" · 재현 안 됨 {bad}개(다른 엔진의 기록 — 아래 표에서 제외)" : "")
        + (recalcBad > 0 ? $" · ⚠ 재계산이 원래 계산과 어긋난 타격 {recalcBad}건" : " · 타격 재계산 전부 일치"));
    Console.WriteLine($"    → {leakPath}");
    if (good.Count == 0) return;
    Console.WriteLine();
    Console.WriteLine("값은 전부 그 시도 점수 대비 %.");
    Console.WriteLine("  계수 누수 = 그 계수 하나만 그 전투의 최고 상태였다면 더 들어갔을 딜(타격별 재계산). '동시' = 전 계수가 함께 최고였다면.");
    Console.WriteLine("  깎인 딜  = 계산된 피해 중 점수가 되지 못한 몫(면역·무효·링크·보호막·오버킬).");
    Console.WriteLine("  사망     = 죽어 있던 턴 합 / 추정 손실(그 영웅의 생존 중 턴당 딜 × 죽은 턴 — 지원 영웅의 버프 손실은 빠짐).");

    string[] lostKeys = { "면역", "무효", "링크", "보호막", "오버킬", "피해1고정" };
    var useF = SiegeLeakRecorder.Factors.Where(f => good.Any(x => x.Leak.Factor.GetValueOrDefault(f) > 0)).ToArray();
    var useL = lostKeys.Where(l => good.Any(x => x.Leak.Lost.GetValueOrDefault(l) > 0)).ToArray();
    double Pct(SiegeLeakRecord x, double v) => x.Leak.Score > 0 ? v / x.Leak.Score * 100 : 0;
    // 한 시도의 열 값(%): 계수들 | 동시 | 깎인 딜들 | 사망턴 | 사망손실 | 폴백 | 홀드
    double[] Cols(SiegeLeakRecord x) => useF.Select(f => Pct(x, x.Leak.Factor.GetValueOrDefault(f)))
        .Append(Pct(x, x.Leak.AllBest))
        .Concat(useL.Select(l => Pct(x, x.Leak.Lost.GetValueOrDefault(l))))
        .Append(x.Leak.DeadTurns.Values.Sum()).Append(Pct(x, x.Leak.DeathLossEst))
        .Append(x.Leak.Fallbacks).Append(x.Leak.Holds).ToArray();
    int nF = useF.Length, nL = useL.Length;
    string Header(string first) => PadR(first, 34) + PadL("점수", 12) + PadL("사망", 5) + " │" + string.Concat(useF.Select(f => PadL(f, 9))) + " │" + PadL("동시", 7)
        + " │" + string.Concat(useL.Select(l => PadL(l, 8))) + " │" + PadL("사망턴", 7) + PadL("사망손실", 9) + " │" + PadL("폴백", 5) + PadL("홀드", 5);
    string Fmt(string first, double score, double deaths, double[] v)
    {
        var sb = new StringBuilder();
        sb.Append(PadR(first, 34)).Append(PadL(score.ToString("N0"), 12)).Append(PadL(deaths.ToString("0.#"), 5)).Append(" │");
        for (int i = 0; i < nF; i++) sb.Append(PadL(v[i].ToString("0.0"), 9));
        sb.Append(" │").Append(PadL(v[nF].ToString("0.0"), 7)).Append(" │");
        for (int i = 0; i < nL; i++) sb.Append(PadL(v[nF + 1 + i].ToString("0.0"), 8));
        sb.Append(" │").Append(PadL(v[nF + 1 + nL].ToString("0.#"), 7)).Append(PadL(v[nF + 2 + nL].ToString("0.0"), 9));
        sb.Append(" │").Append(PadL(v[nF + 3 + nL].ToString("0.#"), 5)).Append(PadL(v[nF + 4 + nL].ToString("0.#"), 5));
        return sb.ToString();
    }
    string Tag(SiegeLeakRecord x) => (x.ForcedBuffOrder != null ? "가정" : x.Final ? "최종" : x.Adopted ? "채택" : "후보");

    // 구간 평균 — 실제 빌드(버프 수령자 강제 가정이 아닌 것)만. 수가 적으면 전부.
    var real = good.Where(x => x.ForcedBuffOrder == null).ToList();
    var pool = real.Count >= 8 ? real : good;
    Console.WriteLine();
    Console.WriteLine($"──── 구간 평균 (랭크 순 · {(pool == real ? $"실제 빌드 {real.Count}개, 가정 평가 {good.Count - real.Count}개 제외" : $"전체 {good.Count}개")}) ────");
    Console.WriteLine(Header("구간"));
    int q = Math.Max(1, pool.Count / 4);
    var groups = new (string Name, List<SiegeLeakRecord> Items)[]
    {
        ($"상위 25% ({q})", pool.Take(q).ToList()),
        ($"중간 ({pool.Count - 2 * q})", pool.Skip(q).Take(Math.Max(0, pool.Count - 2 * q)).ToList()),
        ($"하위 25% ({q})", pool.Skip(Math.Max(0, pool.Count - q)).ToList()),
    };
    double[] Avg(List<SiegeLeakRecord> items)
    {
        var rows = items.Select(Cols).ToList();
        return Enumerable.Range(0, rows[0].Length).Select(i => rows.Average(r => r[i])).ToArray();
    }
    double[] topAvg = null, botAvg = null;
    foreach (var g in groups.Where(g => g.Items.Count > 0))
    {
        var a = Avg(g.Items);
        if (topAvg == null) topAvg = a;
        botAvg = a;
        Console.WriteLine(Fmt(g.Name, g.Items.Average(x => x.Score), g.Items.Average(x => x.DeathCount), a));
    }
    if (topAvg != null && botAvg != null && pool.Count >= 4)
        Console.WriteLine(Fmt("하위 − 상위", groups[2].Items.Average(x => x.Score) - groups[0].Items.Average(x => x.Score),
            groups[2].Items.Average(x => x.DeathCount) - groups[0].Items.Average(x => x.DeathCount),
            botAvg.Zip(topAvg, (b, a) => b - a).ToArray()));

    Console.WriteLine();
    int show = Math.Min(8, good.Count);
    Console.WriteLine($"──── 랭크 상위 {show} ────");
    Console.WriteLine(Header("구분·단계"));
    foreach (var x in good.Take(show)) Console.WriteLine(Fmt($"{Tag(x)} {x.Phase}", x.Score, x.DeathCount, Cols(x)));
    if (good.Count > show)
    {
        Console.WriteLine($"──── 랭크 하위 {show} ────");
        foreach (var x in good.Skip(Math.Max(show, good.Count - show))) Console.WriteLine(Fmt($"{Tag(x)} {x.Phase}", x.Score, x.DeathCount, Cols(x)));
    }

    // 상세 — 기본은 실제 빌드 중 랭크 1위
    int detailRank = int.TryParse(Val("상세="), out var dr) && dr >= 1 && dr <= good.Count ? dr : 0;
    var d = detailRank > 0 ? good[detailRank - 1] : (real.FirstOrDefault() ?? good[0]);
    Console.WriteLine();
    Console.WriteLine($"──── 상세: 랭크 {good.IndexOf(d) + 1}위 · {d.Score:N0} (사망 {d.DeathCount:0.#}) · {Tag(d)} {d.Phase} · {d.Formation?.Replace(" 진형", "")}·후열 {string.Join("+", d.BackRow ?? new())} · 순서 {string.Join(",", d.Order ?? new())}"
        + (d.ForcedBuffOrder != null ? $" · [가정: 버프 수령 {string.Join(">", d.ForcedBuffOrder)}]" : "") + " ────");
    PrintLeakDetail(d.Leak);
    return;
}

// ── 반사실 확인: 한 시도의 스킬 순서에서 행동 하나만 바꿔(장비·자리 그대로) 다시 돌린다 (SiegeCounterfactual) ──
//   누수 장부가 가리킨 누수가 실제로 되찾을 수 있는 딜인지 확인한다. Δ점수 = Δ상한 − Δ누수 로 분해.
//   `반사실` = 실제 빌드 랭크 1위 기준.  `상세=N` = 랭크 N위 시도 기준.  `연쇄=N` = 가장 좋은 편집을 최대 N번 이어 적용(기본 6, 0=안 함).
//   `표시=N` = 편집 목록 줄 수(기본 12).  `저장` = 연쇄 결과가 저장된 고점 빌드보다 높으면 build_*.json 을 그 빌드로 바꾼다.
//   `불사` = [진단] 아군이 피해를 받지 않는 조건에서 비교(기준도 같은 조건) — 생존을 빼고 "딜만 보면 되찾을 수 있는가"를 본다.
//   `보강[=N]` = 순서를 고정하고 실제 조건에서 생존 반지·생존 장비를 다시 맞춘 뒤(SiegeOptimizer.RefitSurvival) 기준과 비교.
//                `불사 보강` = 불사 연쇄로 찾은 순서 하나.  `보강`만 = 점수는 오르는데 사망이 늘어 진 단일 편집 상위 N개(기본 3).
//                기준보다 나으면 counterfactual_*.json 으로 남긴다(`저장`이면 고점 빌드도 갱신).
if (args.Contains("반사실"))
{
    var all = SiegeTrialStore.Load(trialPath);
    Console.WriteLine($"═══ 반사실 확인 {day} [{teamKey}] {profile}");
    if (all.Count == 0) { Console.WriteLine($"시도 기록이 없습니다({trialPath}). 먼저 탐색을 돌리세요."); return; }
    var pairs = BuildLeakRecs(all, out _);
    var good = pairs.Where(x => x.R.ReplayOk).OrderByDescending(x => x.R.RankScore).ToList();   // `누수`의 랭크 번호와 같은 순서
    if (good.Count == 0) { Console.WriteLine("지금 엔진으로 점수가 재현되는 시도가 없습니다. 탐색을 다시 돌리세요."); return; }
    int pickRank = int.TryParse(Val("상세="), out var pr0) && pr0 >= 1 && pr0 <= good.Count ? pr0 : 0;
    var pick = pickRank > 0 ? good[pickRank - 1] : (good.FirstOrDefault(x => x.R.ForcedBuffOrder == null).T != null ? good.First(x => x.R.ForcedBuffOrder == null) : good[0]);
    int chainMax = int.TryParse(Val("연쇄="), out var cm) && cm >= 0 ? cm : 6;
    int showN = int.TryParse(Val("표시="), out var sn2) && sn2 > 0 ? sn2 : 12;
    var bt = pick.T;

    // 기준 시도를 되살린다 (전용장비를 DB 영웅에 다시 꽂으므로 다른 빌드를 되살린 뒤에는 다시 불러야 한다)
    var party = SiegeBuildStore.ToParty(bt.Build);
    var basePlan = SiegeBuildStore.ToPlan(bt.Build, party);
    var cfgCf = MakeCfg(party, null);
    if (bt.ForcedBuffOrder != null)
    {
        cfgCf.HighestAtkAllyOrder = bt.ForcedBuffOrder.Select(n => party.FindIndex(p => p.Character.Name == n)).ToList();
        cfgCf.HighestAtkAllyOverrideCdrOnly = bt.ForcedCdrOnly;
    }
    bool immortal = args.Contains("불사");
    cfgCf.DiagAlliesImmortal = immortal;
    string formation = bt.Build.Formation;
    var swCf = System.Diagnostics.Stopwatch.StartNew();
    var baseRun = SiegeCounterfactual.Run(cfgCf, party, formation, basePlan);
    string Sg(double v) => v.ToString("+#,0;-#,0;0");
    string PctOf(double v, double of) => of > 0 ? $"{v / of * 100:0.0}%" : "-";

    Console.WriteLine($"기준: 랭크 {good.IndexOf(pick) + 1}위 · {bt.Phase} · {bt.Formation?.Replace(" 진형", "")}·후열 {string.Join("+", bt.BackRow)} · 순서 {string.Join(",", bt.Order)}"
        + (bt.ForcedBuffOrder != null ? $" · [가정: 버프 수령 {string.Join(">", bt.ForcedBuffOrder)}]" : ""));
    if (immortal) Console.WriteLine($"  [진단: 불사] 아군이 피해를 받지 않는 조건 — 기록된 점수 {bt.Score:N0}(사망 {bt.DeathCount:0.#})와 다른 값이며, 아래 비교는 전부 이 조건 안에서의 차이다.");
    Console.WriteLine($"  점수 {baseRun.Score:N0} (랭크 {baseRun.Rank:N0}, 사망 {baseRun.Deaths}) · 동시 누수 {baseRun.AllBest:N0}({PctOf(baseRun.AllBest, baseRun.Score)}) · 상한 {baseRun.Ceiling:N0}");
    Console.WriteLine();
    Console.WriteLine("스킬 순서에서 행동 하나만 바꾸고 장비·자리는 그대로 둔 채 다시 돌린 결과.");
    Console.WriteLine("  상한 = 점수 + 동시 누수 = 그 전투의 모든 타격이 그 전투의 최고 상태로 들어갔을 때의 점수.");
    Console.WriteLine("  Δ점수 = Δ상한 − Δ누수.  Δ누수 < 0 = 누수를 막음.  Δ상한 < 0 = 얻을 수 있던 딜 자체가 줄어듦(버스트 횟수·최고 상태·사망).");
    Console.WriteLine("  편집 표기의 #번호·턴은 그 줄이 출발한 순서(기준 또는 연쇄의 직전 단계) 기준.");

    // 한 플랜에서 나오는 단일 편집 전부를 돌린다
    (List<SiegeCounterfactualRun> Runs, Func<int, string> Label) Sweep(SiegeCounterfactualRun from)
    {
        var turnOf = from.Leak.Rows.Where(r => r.Step >= 0).GroupBy(r => r.Step).ToDictionary(g => g.Key, g => g.First().Turn);
        string Label(int i) => i < from.Plan.Count
            ? $"#{i}{(turnOf.TryGetValue(i, out var tn) ? $" T{tn}" : "")} {SiegeCounterfactual.CastName(from.Plan[i], party)}" : $"#{i}";
        var edits = SiegeCounterfactual.SingleEdits(from.Plan, from.Leak.SkillTurns, party);
        var runs = edits.Select(e => SiegeCounterfactual.Run(cfgCf, party, formation, e.Plan, e)).ToList();
        return (runs, Label);
    }
    bool Better(SiegeCounterfactualRun a, SiegeCounterfactualRun b) => SiegeOptimizer.IsBetterBuild(a.Score, a.Rank, b.Score, b.Rank);
    string EditLine(SiegeCounterfactualRun v, SiegeCounterfactualRun from, Func<int, string> label) =>
        PadL(Sg(v.Score - from.Score), 11) + PadL(Sg(v.Rank - from.Rank), 11) + PadL(v.Deaths.ToString(), 5)
        + PadL(Sg(v.AllBest - from.AllBest), 11) + PadL(Sg(v.Ceiling - from.Ceiling), 11) + PadL(v.Leak.Fallbacks.ToString(), 5)
        + "  " + SiegeCounterfactual.Describe(v.Edit, party, label);
    string EditHeader() => PadL("Δ점수", 11) + PadL("Δ랭크", 11) + PadL("사망", 5) + PadL("Δ누수", 11) + PadL("Δ상한", 11) + PadL("폴백", 5) + "  편집";

    var (runs0, label0) = Sweep(baseRun);
    var ranked0 = runs0.OrderByDescending(v => v.Rank).ThenByDescending(v => v.Score).ToList();
    Console.WriteLine();
    Console.WriteLine($"──── 한 행동만 바꾼 편집 {runs0.Count}개 ({swCf.Elapsed.TotalSeconds:F0}초) ────");
    // 결과가 기준과 같은 편집 = 바꾼 칸이 어차피 폴백(쿨)·자동으로 같은 시전이 된 경우 — 실제로는 아무것도 안 바뀐 것
    bool NoOp(SiegeCounterfactualRun v, SiegeCounterfactualRun from) => Math.Abs(v.Score - from.Score) < 1 && Math.Abs(v.AllBest - from.AllBest) < 1 && v.Deaths == from.Deaths;
    Console.WriteLine("  " + PadR("종류", 8) + PadL("개수", 6) + PadL("결과 같음", 10) + PadL("나음", 6) + PadL("사망 늘어남", 12) + PadL("최고 Δ랭크", 13) + PadL("중앙 Δ랭크", 13) + PadL("최고 Δ점수", 13));
    foreach (var g in runs0.GroupBy(v => v.Edit.Kind))
    {
        var live = g.Where(v => !NoOp(v, baseRun)).ToList();
        var ds = live.Select(v => v.Rank - baseRun.Rank).OrderBy(x => x).ToList();
        Console.WriteLine("  " + PadR(g.Key, 8) + PadL(g.Count().ToString(), 6) + PadL((g.Count() - live.Count).ToString(), 10) + PadL(live.Count(v => Better(v, baseRun)).ToString(), 6)
            + PadL(live.Count(v => v.Deaths > baseRun.Deaths).ToString(), 12)
            + PadL(ds.Count > 0 ? Sg(ds[^1]) : "-", 13) + PadL(ds.Count > 0 ? Sg(ds[ds.Count / 2]) : "-", 13) + PadL(live.Count > 0 ? Sg(live.Max(v => v.Score - baseRun.Score)) : "-", 13));
    }
    var live0 = ranked0.Where(v => !NoOp(v, baseRun)).ToList();
    Console.WriteLine($"  결과 같음 = 바꾼 칸이 폴백·자동으로 같은 시전이 되어 실제로는 아무것도 안 바뀐 편집(아래 목록에서 제외).");
    Console.WriteLine($"  로테 다듬기가 보는 수 = 교체 · 3칸 이내 맞바꿈. 이동·끼움·빼기·먼 맞바꿈은 탐색이 본 적 없는 수.");
    if (!immortal && live0.Count > 0)
        Console.WriteLine($"  실제로 바뀐 편집 {live0.Count}개 중 사망이 늘어난 것 {live0.Count(v => v.Deaths > baseRun.Deaths)}개 (평균 사망 {live0.Average(v => v.Deaths):0.0}) — 장비의 생존 여유가 이 스킬 순서에 맞춰져 있다는 뜻. 딜만 보려면 `불사`.");
    Console.WriteLine();
    Console.WriteLine($"──── 랭크 점수가 가장 오른 편집 {Math.Min(showN, live0.Count)}개 ────");
    Console.WriteLine(EditHeader());
    foreach (var v in live0.Take(showN)) Console.WriteLine(EditLine(v, baseRun, label0));

    // 누수 가설 확인: 누수가 큰 시전마다 "그 앞에 상태를 더 만들어 두는" 편집만 모아 본다
    var leakRows = baseRun.Leak.Rows.Where(r => r.Step >= 0 && r.AllBest >= baseRun.Score * 0.01).OrderByDescending(r => r.AllBest).Take(6).ToList();
    Console.WriteLine();
    Console.WriteLine("──── 누수 가설 확인: 누수가 큰 시전 앞에 뒤의 시전을 당겨 오거나(이동·맞바꿈) 새 시전을 끼우는 편집 ────");
    if (leakRows.Count == 0) Console.WriteLine("  점수의 1% 이상 새는 시전이 없습니다.");
    foreach (var row in leakRows)
    {
        var touching = runs0.Where(v => !NoOp(v, baseRun) && v.Edit.PutsSomethingBefore(row.Step)).OrderByDescending(v => v.Rank).ThenByDescending(v => v.Score).ToList();
        // 그 시전 자체가 더 세게 들어간 편집(+2% 이상)만이 "이 누수를 막은" 편집이다 — 총점까지 올라야 되찾은 것
        var stronger = touching.Where(v => { var mm = SiegeCounterfactual.MatchRow(baseRun.Leak, row, v.Leak); return mm != null && mm.Scored > row.Scored * 1.02; }).ToList();
        int nBetter = stronger.Count(v => Better(v, baseRun));
        Console.WriteLine($"  #{row.Step} T{row.Turn} {row.Hero} {row.Skill} — 점수 {row.Scored:N0} · 누수 {row.AllBest:N0}"
            + $" · 해당 편집 {touching.Count}개 · 그 시전이 더 세게 들어간 것 {stronger.Count}개 · 그중 총점(랭크)도 오른 것 {nBetter}개"
            + (stronger.Count == 0 ? " → 한 행동으로는 그 시전을 더 세게 만들지 못함"
               : nBetter > 0 ? " → 한 행동으로 되찾을 수 있음" : $" → 그 시전은 세져도 총점은 안 오름(가장 나은 것 Δ랭크 {Sg(stronger[0].Rank - baseRun.Rank)})"));
        foreach (var v in (stronger.Count > 0 ? stronger : touching).Take(3))
        {
            var m = SiegeCounterfactual.MatchRow(baseRun.Leak, row, v.Leak);
            string hit = m == null ? "그 시전이 없어짐" : $"그 시전 {row.Scored:N0} → {m.Scored:N0}(T{m.Turn}, 누수 {m.AllBest:N0})";
            Console.WriteLine($"      Δ점수 {Sg(v.Score - baseRun.Score)} = Δ상한 {Sg(v.Ceiling - baseRun.Ceiling)} − Δ누수 {Sg(v.AllBest - baseRun.AllBest)} · 사망 {v.Deaths} · 폴백 {v.Leak.Fallbacks} · {hit}");
            Console.WriteLine($"        {SiegeCounterfactual.Describe(v.Edit, party, label0)}");
        }
    }

    // 연쇄: 가장 좋은 편집을 적용하고 거기서 다시 — 한 행동씩 얼마나 되찾아지는지
    var cur = baseRun;
    var chain = new List<string>();
    if (chainMax > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"──── 연쇄: 가장 좋은 편집을 적용한 뒤 거기서 다시 (최대 {chainMax}번) ────");
        var runsK = runs0; var labelK = label0;
        for (int k = 1; k <= chainMax; k++)
        {
            var bestV = runsK.Where(v => Better(v, cur)).OrderByDescending(v => v.Rank).ThenByDescending(v => v.Score).FirstOrDefault();
            if (bestV == null) { Console.WriteLine($"  {k}. 더 나은 편집 없음 — 여기서 멈춤"); break; }
            string desc = SiegeCounterfactual.Describe(bestV.Edit, party, labelK);
            Console.WriteLine($"  {k}. {desc}");
            Console.WriteLine($"       {cur.Score:N0} → {bestV.Score:N0} ({Sg(bestV.Score - cur.Score)}) · 랭크 {bestV.Rank:N0} · 사망 {bestV.Deaths} · 동시 누수 {PctOf(cur.AllBest, cur.Score)} → {PctOf(bestV.AllBest, bestV.Score)} · 상한 {Sg(bestV.Ceiling - cur.Ceiling)} · 폴백 {bestV.Leak.Fallbacks}");
            chain.Add($"{desc} : {cur.Score:N0} → {bestV.Score:N0}");
            cur = bestV;
            if (k < chainMax) (runsK, labelK) = Sweep(cur);
        }
    }

    string DeathStr(SiegeBattleResult r) => r.Deaths == null || r.Deaths.Count == 0 ? "사망 0" : $"사망 {r.Deaths.Count} (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")";
    bool chained = !ReferenceEquals(cur, baseRun);
    if (chained)
    {
        Console.WriteLine();
        Console.WriteLine($"──── 연쇄 결과: {baseRun.Score:N0} → {cur.Score:N0} ({Sg(cur.Score - baseRun.Score)}, {(cur.Score - baseRun.Score) / baseRun.Score * 100:+0.0;-0.0}%) · 사망 {baseRun.Deaths} → {cur.Deaths}"
            + $" · 동시 누수 {baseRun.AllBest:N0}({PctOf(baseRun.AllBest, baseRun.Score)}) → {cur.AllBest:N0}({PctOf(cur.AllBest, cur.Score)}) · 상한 {baseRun.Ceiling:N0} → {cur.Ceiling:N0} · {swCf.Elapsed.TotalSeconds:F0}초 ────");
        PrintLeakDetail(cur.Leak);
    }

    // 여기부터는 실제 조건(피해 받음) · 탐색의 채택 기준(EvaluateBuild)
    cfgCf.DiagAlliesImmortal = false;
    var realBase = SiegeOptimizer.EvaluateBuild(cfgCf, party, formation, basePlan);
    SiegeBattleResult final = null; List<RotationDecision> finalPlan = null;
    if (chained && !immortal)
    {
        final = SiegeOptimizer.EvaluateBuild(cfgCf, party, formation, cur.Plan); finalPlan = cur.Plan;
        Console.WriteLine();
        Console.WriteLine($"탐색 채택 기준 재평가: {final.TotalScore:N0} (랭크 {final.RankScore:N0}) {(Math.Abs(final.TotalScore - cur.Score) <= 1 ? "일치 ✓" : "단일 시드 값과 다름(N시드 평균 요일)")}");
    }
    if (chained && immortal)
    {
        // 딜만 보면 나은 순서가 지금 장비의 생존 여유로는 얼마나 버티는지
        var realNew = SiegeOptimizer.EvaluateBuild(cfgCf, party, formation, cur.Plan);
        Console.WriteLine();
        Console.WriteLine("──── 같은 순서를 실제 조건(피해 받음)·지금 장비로 돌리면 ────");
        Console.WriteLine($"  기준 순서: {realBase.TotalScore:N0} (랭크 {realBase.RankScore:N0}) · {DeathStr(realBase)}");
        Console.WriteLine($"  연쇄 순서: {realNew.TotalScore:N0} (랭크 {realNew.RankScore:N0}) · {DeathStr(realNew)}");
        Console.WriteLine($"  → 점수 {Sg(realNew.TotalScore - realBase.TotalScore)} · 랭크 {Sg(realNew.RankScore - realBase.RankScore)}. 불사에서의 이득 {Sg(cur.Score - baseRun.Score)}이 실제로 남으려면 이 순서에 맞춘 생존 장비가 따로 필요하다(`보강`).");
    }

    // 보강: 순서를 고정하고 생존 반지·생존 장비를 다시 맞춘 뒤 기준과 비교 — "순서 편집 + 그 순서에 맞는 생존"까지가 공정한 반사실.
    //   불사: 연쇄로 찾은 순서 하나.  실제 조건: 점수는 오르는데 사망이 늘어 랭크에서 진 단일 편집 상위 N개(`보강=N`, 기본 3).
    //   죽는 영웅에게 생존을 더하기만 하므로(필요 없어진 생존을 공격으로 되돌리지 않음) 새 순서 쪽에 불리한 하한 비교다.
    if (args.Any(a => a == "보강" || a.StartsWith("보강=")))
    {
        int refitN = int.TryParse(Val("보강="), out var rn) && rn > 0 ? rn : 3;
        var cands = new List<(string Desc, List<RotationDecision> Plan)>();
        if (immortal) { if (chained) cands.Add(("불사 연쇄 순서", cur.Plan)); }
        else
            foreach (var v in runs0.Where(v => !NoOp(v, baseRun) && v.Score > baseRun.Score && v.Deaths > baseRun.Deaths).OrderByDescending(v => v.Score).Take(refitN))
                cands.Add(($"{SiegeCounterfactual.Describe(v.Edit, party, label0)} (점수 {Sg(v.Score - baseRun.Score)} · 사망 {v.Deaths})", v.Plan));
        Console.WriteLine();
        Console.WriteLine($"──── 보강: 순서를 고정하고 생존 반지·생존 장비를 다시 맞춘 뒤 기준({realBase.TotalScore:N0}, 사망 {realBase.Deaths?.Count ?? 0})과 비교 — 후보 {cands.Count}개 ────");
        if (cands.Count == 0) Console.WriteLine(immortal ? "  불사에서도 더 나은 순서가 없어 보강할 것이 없습니다." : "  점수는 오르는데 사망이 늘어 진 편집이 없습니다.");
        SiegeBattleResult bestR = null; List<RotationDecision> bestPlan = null; string bestDesc = null;
        List<(EquipmentLoadout Eq, Accessory Acc)> bestGear = null;
        foreach (var cd in cands)
        {
            var snap = party.Select(p => (Eq: p.Equipment, Acc: p.Equipment?.Accessory)).ToList();
            var refitLog = new List<string>();
            var swR = System.Diagnostics.Stopwatch.StartNew();
            var before = SiegeOptimizer.EvaluateBuild(cfgCf, party, formation, cd.Plan);
            var refit = new SiegeOptimizer().RefitSurvival(cfgCf, party, formation, cd.Plan, refitLog);
            Console.WriteLine($"  ▷ {cd.Desc}");
            Console.WriteLine($"      보강 전: {before.TotalScore:N0} (랭크 {before.RankScore:N0}) · {DeathStr(before)}");
            foreach (var l in refitLog.Where(l => l.StartsWith("["))) Console.WriteLine("      " + l);
            Console.WriteLine($"      보강 후: {refit.TotalScore:N0} (랭크 {refit.RankScore:N0}) · {DeathStr(refit)} · {swR.Elapsed.TotalMinutes:F1}분");
            bool wins = SiegeOptimizer.IsBetterBuild(refit.TotalScore, refit.RankScore, realBase.TotalScore, realBase.RankScore);
            Console.WriteLine($"      기준 대비 점수 {Sg(refit.TotalScore - realBase.TotalScore)} · 랭크 {Sg(refit.RankScore - realBase.RankScore)} → {(wins ? "기준보다 나음" : "기준보다 낫지 않음")}");
            if (wins && (bestR == null || SiegeOptimizer.IsBetterBuild(refit.TotalScore, refit.RankScore, bestR.TotalScore, bestR.RankScore)))
            {
                bestR = refit; bestPlan = cd.Plan; bestDesc = cd.Desc;
                bestGear = party.Select(p => (p.Equipment, p.Equipment?.Accessory)).ToList();
            }
            for (int i = 0; i < party.Count; i++) { party[i].Equipment = snap[i].Eq; if (snap[i].Eq != null) snap[i].Eq.Accessory = snap[i].Acc; }
        }
        if (bestR != null)
        {
            for (int i = 0; i < party.Count; i++) { party[i].Equipment = bestGear[i].Eq; if (bestGear[i].Eq != null) bestGear[i].Eq.Accessory = bestGear[i].Acc; }
            final = bestR; finalPlan = bestPlan;
            chain.Add($"{bestDesc} + 생존 재보강 : {realBase.TotalScore:N0} → {bestR.TotalScore:N0}");
            Console.WriteLine();
            Console.WriteLine($"  → 순서 편집 + 생존 재보강으로 실제 조건에서 {realBase.TotalScore:N0} → {bestR.TotalScore:N0} ({Sg(bestR.TotalScore - realBase.TotalScore)}) · {DeathStr(bestR)}");
            PrintLeakDetail(SiegeOptimizer.ReplayWithCasts(cfgCf, party, formation, bestPlan, leak: true).Leak);
        }
        else if (cands.Count > 0)
            Console.WriteLine("  → 기준보다 나은 것이 없음: 순서를 바꿔 얻는 딜보다 그 순서를 버티는 데 드는 생존 값이 크다(더하기만 한 하한 비교).");
    }

    if (final != null && SiegeOptimizer.IsBetterBuild(final.TotalScore, final.RankScore, realBase.TotalScore, realBase.RankScore))
    {
        // 파생 자료로 빌드를 남긴다(저장된 최고 빌드는 `저장`을 줄 때만 바꾼다)
        var back = party.Where(p => p.IsBackPosition).Select(p => p.Character.Name).ToList();
        var nb = SiegeBuildStore.Capture(party, formation, back, finalPlan);
        nb.Day = day; nb.Profile = profile; nb.Route = "고점";
        nb.Score = Math.Round(final.TotalScore); nb.RankScore = Math.Round(final.RankScore); nb.Deaths = final.Deaths?.Count ?? 0;
        nb.InGameBuffTargeting = ingame; nb.EngineHash = engineHash; nb.GitCommit = gitHead; nb.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        nb.StartOrder = bt.StartOrder;
        nb.Pet = new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
        nb.Log = new List<string> { $"[반사실 편집] 기준 {bt.Phase} {realBase.TotalScore:N0} (지문 {bt.Fingerprint})" }.Concat(chain.Select(c => "  [반사실 편집] " + c)).ToList();
        string cfPath = System.IO.Path.Combine(outDir, $"counterfactual_{day}_{profile}_{teamKey}.json");
        SiegeBuildStore.Save(cfPath, nb);
        Console.WriteLine();
        Console.WriteLine($"→ {cfPath}");
        if (args.Contains("저장") && bt.ForcedBuffOrder == null)
        {
            string peakPath = PathFor(SiegeSearchRoute.Peak);
            var saved = SiegeBuildStore.Load(peakPath);
            double sTot = double.NegativeInfinity, sRank = double.NegativeInfinity;
            if (saved != null) { var ev = Evaluate(saved); sTot = ev.Res.TotalScore; sRank = ev.Res.RankScore; }
            if (SiegeOptimizer.IsBetterBuild(final.TotalScore, final.RankScore, sTot, sRank))
            {
                if (saved != null)
                {
                    nb.History = saved.History; nb.TargetScore = saved.TargetScore;
                    nb.Stages = (saved.Stages ?? new()).ToList();
                    nb.Log = (saved.Log ?? new()).Concat(nb.Log).ToList();
                }
                nb.Stages.Add(new SiegeSavedBuild.StageDto { Name = "반사실 편집(스킬 순서)", Score = nb.Score, RankScore = nb.RankScore, Deaths = nb.Deaths, Order = string.Join(",", party.Select(p => p.Character.Name)) });
                SiegeBuildStore.Save(peakPath, nb);
                Console.WriteLine($"[저장·고점] {(saved == null ? "새 빌드" : $"갱신 {sTot:N0} →")} {final.TotalScore:N0} → {peakPath}");
            }
            else Console.WriteLine($"[유지·고점] 저장된 빌드 {sTot:N0}가 반사실 결과 {final.TotalScore:N0} 이상 — 바꾸지 않습니다.");
        }
    }
    return;
}
// ── 부옵이동: 저장된 빌드에서 한 영웅의 부옵 티어를 A → B로 옮겼을 때 점수 (같은 스킬 순서 재생, 저장 안 함) ──
//   `부옵이동=타카:치명타피해%>약점공격확률%:5` (티어 이동) / `부옵이동=타카:약점공격확률%>공격력:교체` (칸 교체) — "약확을 더 챙기면 오르나?" 같은 가정 확인용. 1티어씩 옮기며 매 단계 점수를 찍는다.
string subMoveArg = Val("부옵이동=");
if (subMoveArg != null)
{
    var mp = subMoveArg.Split(':');
    var st2 = mp.Length >= 2 ? mp[1].Split('>') : new string[0];
    int mvN = mp.Length >= 3 && int.TryParse(mp[2], out var mvn) ? mvn : 1;
    var sbM = SiegeBuildStore.Load(PathFor(route));
    if (sbM == null || st2.Length != 2) { Console.WriteLine("사용: 부옵이동=영웅:출발스탯>도착스탯:티어수 (저장된 빌드 필요)"); return; }
    var partyM = SiegeBuildStore.ToParty(sbM);
    var planM = SiegeBuildStore.ToPlan(sbM, partyM);
    var cfgM = MakeCfg(partyM, null);
    var heroM = partyM.FirstOrDefault(x => x.Character.Name == mp[0]);
    if (heroM?.Equipment == null) { Console.WriteLine($"영웅 없음: {mp[0]}"); return; }
    var bsimM = new BattleSimulator();
    var bcfgM = new BattleConfig { AllyParty = partyM, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, FormationName = sbM.Formation };
    string StatLine()
    {
        var stM = bsimM.InitializeCharacterState(bcfgM, heroM, partyM.IndexOf(heroM));
        var d = stM.DisplayStats;
        return $"공 {stM.FinalAtk:N0} · 치확 {d.Cri:0.#} · 치피 {d.Cri_Dmg:0.#} · 약확 {d.Wek:0.#} · 약피 {d.Wek_Dmg:0.#}";
    }
    // `:세트=복수자` (마지막 인자) — 먼저 그 영웅의 4부위 세트를 바꾼 뒤 기준을 잡는다(세트 A vs B를 같은 약확에서 비교할 때).
    string setSwap = mp.FirstOrDefault(x => x.StartsWith("세트="))?.Substring(3);
    if (setSwap != null)
    {
        var rOrig = SiegeOptimizer.EvaluateBuild(cfgM, partyM, sbM.Formation, planM);
        Console.WriteLine($"  세트 교체 전({string.Join("+", heroM.Equipment.GetActiveSets().Select(x => $"{x.SetName}{x.PieceCount}"))}): {rOrig.TotalScore:N0} (사망 {rOrig.Deaths?.Count ?? 0}) · {mp[0]} {rOrig.CharacterResults.FirstOrDefault(c => c.CharacterName == mp[0])?.TotalDamage ?? 0:N0} · {StatLine()}");
        foreach (var e in heroM.Equipment.GetEquipments()) e.SetName = setSwap;
    }
    var r0 = SiegeOptimizer.EvaluateBuild(cfgM, partyM, sbM.Formation, planM);
    // `운` — 매 단계 "치명·약점이 전부 터진 판"(DiagForceCritWeak) 점수도 같이 낸다. 유저는 여러 판 중 최고점을 노리므로
    //   치확·약확을 덜고 치피·공격력을 챙긴다 — 기댓값은 내려가도 최고점은 오르는지 본다.
    bool luckM = args.Contains("운");
    var cfgML = MakeCfg(partyM, null); cfgML.DiagForceCritWeak = true;
    string Luck() => (luckM ? $" · 전부 터진 판 {SiegeOptimizer.EvaluateBuild(cfgML, partyM, sbM.Formation, planM).TotalScore:N0}" : "") + Dist();
    // `표본=K` — 치명·약점을 실제로 추첨한 판을 K번 돌려 분포를 낸다. `판=N`(기본 5) = 하루 도전 횟수 → "N판 중 최고점의 기대치".
    int sampK = int.TryParse(Val("표본="), out var sk0) ? sk0 : 0;
    int triesN = int.TryParse(Val("판="), out var tn0) ? tn0 : 5;
    string Dist()
    {
        if (sampK <= 0) return "";
        var cfgS = MakeCfg(partyM, null);
        var xs = new List<double>();
        for (int i = 1; i <= sampK; i++) { cfgS.CritSampleSeed = i; xs.Add(SiegeOptimizer.EvaluateBuild(cfgS, partyM, sbM.Formation, planM).TotalScore); }
        xs.Sort();
        double emax = 0;   // E[N판 중 최고] = Σ x(i) × [(i/K)^N − ((i−1)/K)^N]
        for (int i = 1; i <= sampK; i++) emax += xs[i - 1] * (Math.Pow((double)i / sampK, triesN) - Math.Pow((double)(i - 1) / sampK, triesN));
        double Q(double q) => xs[Math.Min(sampK - 1, (int)(q * sampK))];
        return $"\n      표본 {sampK}판: 평균 {xs.Average():N0} · 중앙 {Q(0.5):N0} · 하위10% {Q(0.1):N0} · 상위10% {Q(0.9):N0} · 최고 {xs[sampK - 1]:N0} · ★{triesN}판 중 최고 기대 {emax:N0}";
    }
    Console.WriteLine($"═══ 부옵이동 {day} [{teamKey}] {RouteName(route)} — {mp[0]} {st2[0]} → {st2[1]} (같은 스킬 순서 재생, 저장 안 함)");
    Console.WriteLine($"  기준: {r0.TotalScore:N0} (사망 {r0.Deaths?.Count ?? 0}) · {mp[0]} {r0.CharacterResults.FirstOrDefault(c => c.CharacterName == mp[0])?.TotalDamage ?? 0:N0} · {StatLine()}{Luck()}");
    // `:교체` — 티어 이동 대신 칸 자체를 바꾼다(부위마다 출발 스탯 칸을 도착 스탯으로, 티어 유지. 도착 스탯이 이미 있는 부위는 건너뜀).
    if (mp.Length >= 3 && mp[2] == "교체")
    {
        int ren = 0;
        foreach (var e in heroM.Equipment.GetEquipments())
        {
            var from = e.SubSlots.FirstOrDefault(x => x.StatName == st2[0]);
            if (from == null || e.SubSlots.Any(x => x.StatName == st2[1])) continue;
            from.StatName = st2[1]; ren++;
            var rr = SiegeOptimizer.EvaluateBuild(cfgM, partyM, sbM.Formation, planM);
            Console.WriteLine($"  {ren}부위 교체: {rr.TotalScore:N0} ({rr.TotalScore - r0.TotalScore:+#,0;-#,0}, 사망 {rr.Deaths?.Count ?? 0}) · {mp[0]} {rr.CharacterResults.FirstOrDefault(c => c.CharacterName == mp[0])?.TotalDamage ?? 0:N0} · {StatLine()}");
        }
        if (ren == 0) Console.WriteLine("  바꿀 칸 없음");
        return;
    }
    for (int k = 1; k <= mvN; k++)
    {
        bool moved = false;
        foreach (var e in heroM.Equipment.GetEquipments())
        {
            var from = e.SubSlots.FirstOrDefault(x => x.StatName == st2[0] && x.Tier > 1);
            var to = e.SubSlots.FirstOrDefault(x => x.StatName == st2[1] && x.Tier < 6);
            if (from == null || to == null) continue;
            from.Tier--; to.Tier++; moved = true; break;
        }
        if (!moved) { Console.WriteLine($"  {k}티어째: 옮길 칸 없음(출발 스탯 2티어 이상·도착 스탯 6티어 미만인 부위가 없음)"); break; }
        var rk = SiegeOptimizer.EvaluateBuild(cfgM, partyM, sbM.Formation, planM);
        Console.WriteLine($"  {k}티어 이동: {rk.TotalScore:N0} ({rk.TotalScore - r0.TotalScore:+#,0;-#,0}, 사망 {rk.Deaths?.Count ?? 0}) · {mp[0]} {rk.CharacterResults.FirstOrDefault(c => c.CharacterName == mp[0])?.TotalDamage ?? 0:N0} · {StatLine()}{Luck()}");
    }
    return;
}

// ── 단계: 다시 탐색하지 않고, 저장된 빌드에 탐색의 "순서 편집+생존 보강" 단계만 돌린다 (SiegeOptimizer.RunEditSurvival) ──
//   이미 찾아 둔 빌드를 새로 생긴 후처리 단계로 손볼 때. 정석·고점 저장 빌드를 각각 처리하고(`정석`이면 정석만),
// ── 상한: 저장된 빌드가 유저 최고점에 못 미칠 때 "어디서 더 나올 수 있는가"를 잰다 (저장 안 함) ──
//   ① 같은 스킬 순서 재생: 기준 / 치명·약점 항상 발동(운 상한) / 불사(생존 제약 없음) / 둘 다
//   ② 같은 장비로 스킬 순서만 다시 탐색: 쿨 여유(초)를 0, -2, -4 … 로 줘 가며 — 스킬턴 격자(4턴)에 몇 초 차이로 걸려
//      주력기가 한 번 덜 들어가는 구조인지 본다. `상한=0,-2,-4,-8`로 여유 목록 지정, `불사`를 붙이면 ②도 불사로.
// ── 벗기기=아일린:방어구1[+방어구2+무기1+무기2+장신구][,영웅:부위…]: 저장된 빌드에서 장비 부위를 빼고 같은 스킬 순서로 재생 (저장 안 함) ──
//   "일부러 빨리 죽게 만든다" 같은 수가 시뮬에서도 이득인지 확인용. 실제 점수(누적 딜)와 랭크 점수(사망 페널티 포함)를 같이 낸다.
string stripArg = Val("벗기기=");
if (stripArg != null)
{
    var sbS = SiegeBuildStore.Load(PathFor(route));
    if (sbS == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    string DS(SiegeBattleResult r) => r.Deaths == null || r.Deaths.Count == 0 ? "사망 0" : $"사망 {r.Deaths.Count} (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")";
    void Show(string title, SiegeBattleResult r)
    {
        Console.WriteLine($"▷ {title}: {r.TotalScore:N0} (랭크 {r.RankScore:N0}) · {DS(r)}");
        Console.WriteLine("    기여: " + string.Join(" / ", r.CharacterResults.OrderByDescending(x => x.TotalDamage).Select(x => $"{x.CharacterName} {x.TotalDamage:N0}")));
        Console.WriteLine("    시전: " + string.Join(" | ", r.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨").Select(l => $"T{l.Turn} {l.ActorName} {(l.Description ?? "").Split(" 시전")[0]}")));
        foreach (var l in r.TurnLogs.Where(l => ((l.Description ?? "") + (l.SkillName ?? "")).Contains("부활") || (l.SkillName ?? "").Contains("발동")))
            Console.WriteLine($"    T{l.Turn} [{(l.IsAlly ? "아군" : "적")}] {l.ActorName} · {l.SkillName}: {l.Description}");
        foreach (var g in r.TurnLogs.Where(l => l.IsAlly && l.ActorName == "라이언" && l.SkillName == "광풍참").GroupBy(l => l.Turn))
            Console.WriteLine($"    T{g.Key} 광풍참: " + string.Join(" / ", g.Select(l => (l.Description ?? "").Split("→ ").Last())));
    }
    Console.WriteLine($"═══ 벗기기 {day} [{teamKey}] {profile} · {RouteName(route)} (저장 안 함)");
    {
        var pt = SiegeBuildStore.ToParty(sbS); var pl = SiegeBuildStore.ToPlan(sbS, pt);
        Show("기준", SiegeOptimizer.EvaluateBuild(MakeCfg(pt, null), pt, sbS.Formation, pl));
    }
    foreach (string variant in stripArg.Split('/', StringSplitOptions.RemoveEmptyEntries))
    {
        var pt = SiegeBuildStore.ToParty(sbS); var pl = SiegeBuildStore.ToPlan(sbS, pt);
        foreach (string item in variant.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = item.Split(':'); var h = pt.First(x => x.Character.Name == kv[0]);
            foreach (string part in kv[1].Split('+'))
                switch (part)
                {
                    case "방어구1": h.Equipment.Armor1 = null; break;
                    case "방어구2": h.Equipment.Armor2 = null; break;
                    case "무기1": h.Equipment.Weapon1 = null; break;
                    case "무기2": h.Equipment.Weapon2 = null; break;
                    case "장신구": h.Equipment.Accessory = null; break;
                    default: throw new ArgumentException($"부위 없음: {part}");
                }
        }
        Show(variant + " 제거", SiegeOptimizer.EvaluateBuild(MakeCfg(pt, null), pt, sbS.Formation, pl));
    }
    return;
}

// ── 강제사망=아일린:2/아일린:6/…: 저장된 빌드에서 지정 턴에 그 영웅이 치명 판정을 받는다고 놓고 (저장 안 함) ──
//   ① 같은 스킬 순서 재생 ② 같은 장비로 스킬 순서만 재탐색. `불사`를 붙이면 그 외 피해는 받지 않는 조건.
string lethalArg = Val("강제사망=");
if (lethalArg != null)
{
    var sbL = SiegeBuildStore.Load(PathFor(route));
    if (sbL == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    bool immL = args.Contains("불사");
    string DSL(SiegeBattleResult r) => r.Deaths == null || r.Deaths.Count == 0 ? "사망 0" : $"사망 {r.Deaths.Count} (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")";
    void ShowL(string title, SiegeBattleResult r, double baseScore)
    {
        Console.WriteLine($"▷ {title}: {r.TotalScore:N0} ({r.TotalScore - baseScore:+#,0;-#,0}) · {DSL(r)}");
        Console.WriteLine("    기여: " + string.Join(" / ", r.CharacterResults.OrderByDescending(x => x.TotalDamage).Select(x => $"{x.CharacterName} {x.TotalDamage:N0}")));
        Console.WriteLine("    시전: " + string.Join(" | ", r.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨").Select(l => $"T{l.Turn} {l.ActorName} {(l.Description ?? "").Split(" 시전")[0]}")));
        foreach (var l in r.TurnLogs.Where(l => ((l.Description ?? "") + (l.SkillName ?? "")).Contains("부활") || (l.SkillName ?? "").Contains("강제 치명")))
            Console.WriteLine($"    T{l.Turn} {l.ActorName} · {l.SkillName}: {l.Description}");
        foreach (var g in r.TurnLogs.Where(l => l.IsAlly && l.ActorName == "라이언" && l.SkillName == "광풍참").GroupBy(l => l.Turn))
            Console.WriteLine($"    T{g.Key} 광풍참: " + string.Join(" / ", g.Select(l => (l.Description ?? "").Split("→ ").Last())));
    }
    SiegeOptimizerConfig CfgL(List<BattleCharacter> pt, List<(string, int)> lethal, List<RotationDecision> seed)
    {
        var c = MakeCfg(pt, seed != null ? new List<List<RotationDecision>> { seed } : null);
        c.DiagAlliesImmortal = immL || immortalSearch; c.DiagForceLethal = lethal;
        return c;
    }
    Console.WriteLine($"═══ 강제사망 {day} [{teamKey}] {profile} · {RouteName(route)}{(immL ? " · 불사" : "")} (저장 안 함)");
    double baseL;
    {
        var pt = SiegeBuildStore.ToParty(sbL); var pl = SiegeBuildStore.ToPlan(sbL, pt);
        var r0 = SiegeOptimizer.EvaluateBuild(CfgL(pt, null, null), pt, sbL.Formation, pl);
        baseL = r0.TotalScore; ShowL("기준", r0, baseL);
    }
    foreach (string variant in lethalArg.Split('/', StringSplitOptions.RemoveEmptyEntries))
    {
        var lethal = variant.Split(',').Select(x => x.Split(':')).Select(x => (x[0], int.Parse(x[1]))).ToList();
        var pt = SiegeBuildStore.ToParty(sbL); var pl = SiegeBuildStore.ToPlan(sbL, pt);
        ShowL($"{variant} · 같은 순서", SiegeOptimizer.EvaluateBuild(CfgL(pt, lethal, null), pt, sbL.Formation, pl), baseL);
        if (args.Contains("재탐색"))
        {
            var c = CfgL(pt, lethal, pl);
            c.AutoEquip = false; c.SearchExclusiveWeapon = false; c.CoordinateAscentGear = false; c.EnableSurvivalRings = false;
            c.SearchAggressiveSurvival = false; c.SearchEditSurvival = false; c.SearchAtkRank = false; c.SearchActionOrder = false; c.SearchSpeedLead = false;
            c.ForcedFormation = sbL.Formation; c.ForcedBackRow = pt.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
            var o = new SiegeOptimizer().Optimize(c);
            ShowL($"{variant} · 순서 재탐색", o.BestResult, baseL);
        }
    }
    return;
}

string ceilArg = args.FirstOrDefault(a => a == "상한" || a.StartsWith("상한="));
if (ceilArg != null)
{
    var sbC = SiegeBuildStore.Load(PathFor(route));
    if (sbC == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    var margins = (ceilArg.Length > 3 ? ceilArg.Substring(3) : "0,-2,-4,-8").Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToList();
    bool imm = args.Contains("불사");
    Console.WriteLine($"═══ 상한 {day} [{teamKey}] {RouteName(route)} (저장 안 함)");
    SiegeBattleResult Rep(bool luck, bool immortal)
    {
        var pt = SiegeBuildStore.ToParty(sbC); var pl = SiegeBuildStore.ToPlan(sbC, pt);
        var c = MakeCfg(pt, null); c.DiagForceCritWeak = luck; c.DiagAlliesImmortal = immortal;
        return SiegeOptimizer.EvaluateBuild(c, pt, sbC.Formation, pl);
    }
    string Casts(SiegeBattleResult r) => string.Join(" · ", r.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨")
        .GroupBy(l => l.ActorName + " " + (l.Description ?? "").Split(" 시전")[0]).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
        .Select(g => $"{g.Key}×{g.Count()}"));
    string Shares(SiegeBattleResult r) => string.Join(" / ", r.CharacterResults.OrderByDescending(x => x.TotalDamage).Take(3).Select(x => $"{x.CharacterName} {x.TotalDamage:N0}"));
    var r00 = Rep(false, false);
    Console.WriteLine("① 같은 스킬 순서 재생");
    foreach (var (nm, lk, im) in new[] { ("기준", false, false), ("치명·약점 항상 발동", true, false), ("불사", false, true), ("치명·약점 항상 + 불사", true, true) })
    {
        var r = nm == "기준" ? r00 : Rep(lk, im);
        Console.WriteLine($"  {nm}: {r.TotalScore:N0} ({r.TotalScore - r00.TotalScore:+#,0;-#,0}, 사망 {r.Deaths?.Count ?? 0}) · {Shares(r)}");
    }
    Console.WriteLine($"  시전: {Casts(r00)}");
    // `순서=라이언1,풍연1,비스킷1,아일린1,…` — 손으로 적은 스킬 순서를 같은 장비로 재생(영웅+스킬 번호, 스킬턴 순). 탐색이 놓친 순서 확인용.
    string orderArg = Val("순서=");
    if (orderArg != null)
    {
        var pt = SiegeBuildStore.ToParty(sbC);
        var hand = orderArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t =>
        {
            string nm = t.TrimEnd('1', '2'); int idx = pt.FindIndex(x => x.Character.Name == nm);
            if (idx < 0) throw new ArgumentException($"영웅 없음: {t}");
            return new RotationDecision { HeroIndex = idx, Skill = t.EndsWith("2") ? SkillType.Skill2 : SkillType.Skill1 };
        }).ToList();
        foreach (double mg in margins)
        {
            var c = MakeCfg(pt, null); c.AllyCooldownMargin = mg; c.DiagAlliesImmortal = imm;
            var r = SiegeOptimizer.EvaluateBuild(c, pt, sbC.Formation, hand);
            Console.WriteLine($"③ 손 순서 (쿨 여유 {mg:+0.#;-0.#;0}초): {r.TotalScore:N0} ({r.TotalScore - r00.TotalScore:+#,0;-#,0}, 사망 {r.Deaths?.Count ?? 0}) · {Shares(r)}");
            Console.WriteLine($"      시전: {Casts(r)}");
            foreach (var l in r.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨"))
                Console.Write($" T{l.Turn} {l.ActorName} {(l.Description ?? "").Split(" 시전")[0]} |");
            Console.WriteLine();
            foreach (var dth in r.Deaths ?? new()) Console.WriteLine($"      사망: T{dth.Turn} {dth.AllyName} ({dth.Cause})");
        }
        return;
    }
    Console.WriteLine($"② 같은 장비·스킬 순서만 재탐색 (쿨 여유별{(imm ? ", 불사" : "")})");
    foreach (double mg in margins)
    {
        var pt = SiegeBuildStore.ToParty(sbC); var pl = SiegeBuildStore.ToPlan(sbC, pt);
        var c = MakeCfg(pt, new List<List<RotationDecision>> { pl });
        c.AutoEquip = false; c.SearchExclusiveWeapon = false; c.CoordinateAscentGear = false; c.EnableSurvivalRings = false;
        c.SearchAggressiveSurvival = false; c.SearchEditSurvival = false; c.SearchAtkRank = false; c.SearchActionOrder = false; c.SearchSpeedLead = false;
        c.ForcedFormation = sbC.Formation; c.ForcedBackRow = pt.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
        c.AllyCooldownMargin = mg; c.DiagAlliesImmortal = imm;
        var swC = System.Diagnostics.Stopwatch.StartNew();
        var same = SiegeOptimizer.EvaluateBuild(c, pt, sbC.Formation, pl);
        var o = new SiegeOptimizer().Optimize(c);
        var br = o.BestResult;
        Console.WriteLine($"  쿨 여유 {mg:+0.#;-0.#;0}초: 같은 순서 {same.TotalScore:N0} → 재탐색 {br.TotalScore:N0} (사망 {br.Deaths?.Count ?? 0}) · {Shares(br)} · {swC.Elapsed.TotalMinutes:F1}분");
        Console.WriteLine($"      시전: {Casts(br)}");
        if (o.BestRotationPlan != null)
            Console.WriteLine("      순서: " + string.Join(" ", o.BestRotationPlan.Select(d => SiegeCounterfactual.CastName(d, o.BestParty))));
    }
    return;
}

// ── 보강: 저장된 빌드의 스킬 순서를 그대로 두고 생존 반지·생존 장비만 다시 맞춘다 (RefitSurvival) ──
//   불사탐색으로 찾은 순서(딜 천장)를 `프로필=`로 복사해 온 뒤 "이 순서를 실제로 버티려면 얼마가 드는가"를 잴 때 쓴다.
//   더 높아지면 그 프로필에 저장한다.
if (args.Contains("보강"))
{
    string rpath = PathFor(route);
    var sbR = SiegeBuildStore.Load(rpath);
    if (sbR == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    var rparty = SiegeBuildStore.ToParty(sbR); var rplan = SiegeBuildStore.ToPlan(sbR, rparty);
    var rcfg = MakeCfg(rparty, null);
    string DStr(SiegeBattleResult r) => r.Deaths == null || r.Deaths.Count == 0 ? "사망 0" : $"사망 {r.Deaths.Count} (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")";
    string Top(SiegeBattleResult r) => string.Join(" / ", r.CharacterResults.OrderByDescending(x => x.TotalDamage).Select(x => $"{x.CharacterName} {x.TotalDamage:N0}"));
    Console.WriteLine($"═══ 보강(순서 고정·생존만 다시) {day} [{teamKey}] {profile} · {RouteName(route)}");
    var rb = SiegeOptimizer.EvaluateBuild(rcfg, rparty, sbR.Formation, rplan);
    Console.WriteLine($"  보강 전: {rb.TotalScore:N0} (랭크 {rb.RankScore:N0}) · {DStr(rb)}");
    var rlog = new List<string>(); var swR2 = System.Diagnostics.Stopwatch.StartNew();
    var ra = new SiegeOptimizer().RefitSurvival(rcfg, rparty, sbR.Formation, rplan, rlog);
    foreach (var l in rlog.Where(l => l.StartsWith("["))) Console.WriteLine("    " + l);
    Console.WriteLine($"  보강 후: {ra.TotalScore:N0} (랭크 {ra.RankScore:N0}) · {DStr(ra)} · {swR2.Elapsed.TotalMinutes:F1}분");
    Console.WriteLine($"    기여: {Top(ra)}");
    foreach (var l in ra.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨")) Console.Write($" T{l.Turn} {l.ActorName} {(l.Description ?? "").Split(" 시전")[0]} |");
    Console.WriteLine();
    foreach (var h in rparty)
        Console.WriteLine($"    [{h.Character.Name}] {string.Join("+", h.Equipment.GetActiveSets().Select(x => $"{x.SetName}{x.PieceCount}"))} · 장신구 {h.Equipment?.Accessory?.RingName ?? h.Equipment?.Accessory?.MainOption}/{h.Equipment?.Accessory?.SubOption}");
    if (SiegeOptimizer.IsBetterBuild(ra.TotalScore, ra.RankScore, rb.TotalScore, rb.RankScore))
    {
        var rback = rparty.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
        var nbR = SiegeBuildStore.Capture(rparty, sbR.Formation, rback, rplan);
        nbR.Day = day; nbR.Profile = profile; nbR.Route = RouteName(route);
        nbR.Score = Math.Round(ra.TotalScore); nbR.RankScore = Math.Round(ra.RankScore); nbR.Deaths = ra.Deaths?.Count ?? 0;
        nbR.InGameBuffTargeting = ingame; nbR.EngineHash = engineHash; nbR.GitCommit = gitHead; nbR.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        nbR.StartOrder = sbR.StartOrder; nbR.SearchMinutes = Math.Round(sbR.SearchMinutes + swR2.Elapsed.TotalMinutes, 1);
        nbR.Pet = sbR.Pet ?? new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
        nbR.History = sbR.History; nbR.TargetScore = sbR.TargetScore; nbR.Stages = (sbR.Stages ?? new()).ToList();
        SiegeBuildStore.Save(rpath, nbR);
        Console.WriteLine($"  [저장] {rb.TotalScore:N0} → {ra.TotalScore:N0} → {rpath}");
    }
    return;
}

// ── 생존조합: 불사탐색으로 찾은 빌드(프로필 `_불사`)에서 출발해 실제 조건의 빌드로 내려온다 (RelaxImmortality) ──
//   먼저 `불사탐색`으로 딜 천장 빌드를 저장해 둬야 한다. 결과는 `_생존조합` 프로필에 저장하고,
//   저장된 고점 빌드보다 높으면 고점 빌드도 갱신한다(최고 빌드만 유지).
if (args.Contains("생존조합"))
{
    string immPath = System.IO.Path.Combine(outDir, $"build_{day}_{profile}_불사_{teamKey}.json");
    var sbI = SiegeBuildStore.Load(immPath);
    if (sbI == null) { Console.WriteLine($"불사 빌드 없음 — 먼저 `불사탐색`을 붙여 탐색하세요. ({immPath})"); return; }
    var ptI = SiegeBuildStore.ToParty(sbI); var plI = SiegeBuildStore.ToPlan(sbI, ptI);
    var cfgI = MakeCfg(ptI, null);
    string DSI(SiegeBattleResult r) => r.Deaths == null || r.Deaths.Count == 0 ? "사망 0" : $"사망 {r.Deaths.Count} (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}←{d.Cause}")) + ")";
    Console.WriteLine($"═══ 생존 조합 {day} [{teamKey}] {profile} — 불사 빌드 {sbI.Score:N0}에서 출발");
    var swI = System.Diagnostics.Stopwatch.StartNew();
    var (rI, pI, logI) = new SiegeOptimizer().RelaxImmortality(cfgI, ptI, sbI.Formation, plI);
    foreach (var l in logI) Console.WriteLine("  " + l);
    Console.WriteLine($"  결과: {rI.TotalScore:N0} (랭크 {rI.RankScore:N0}) · {DSI(rI)} · {swI.Elapsed.TotalMinutes:F1}분");
    Console.WriteLine("    기여: " + string.Join(" / ", rI.CharacterResults.OrderByDescending(x => x.TotalDamage).Select(x => $"{x.CharacterName} {x.TotalDamage:N0}")));
    Console.WriteLine("    시전: " + string.Join(" | ", rI.TurnLogs.Where(l => l.IsAlly && l.SkillName == "쿨").Select(l => $"T{l.Turn} {l.ActorName} {(l.Description ?? "").Split(" 시전")[0]}")));
    var backI = ptI.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
    var nbI = SiegeBuildStore.Capture(ptI, sbI.Formation, backI, pI);
    nbI.Day = day; nbI.Profile = profile; nbI.Route = "고점";
    nbI.Score = Math.Round(rI.TotalScore); nbI.RankScore = Math.Round(rI.RankScore); nbI.Deaths = rI.Deaths?.Count ?? 0;
    nbI.InGameBuffTargeting = ingame; nbI.EngineHash = engineHash; nbI.GitCommit = gitHead; nbI.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    nbI.StartOrder = sbI.StartOrder; nbI.SearchMinutes = Math.Round(sbI.SearchMinutes + swI.Elapsed.TotalMinutes, 1);
    nbI.Pet = sbI.Pet ?? new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
    nbI.TargetScore = sbI.TargetScore;
    nbI.Log = logI.ToList();
    foreach (var l in DiffLines(sbI, nbI)) Console.WriteLine("    불사 빌드에서 바뀐 것: " + l);
    string relPath = System.IO.Path.Combine(outDir, $"build_{day}_{profile}_생존조합_{teamKey}.json");
    SiegeBuildStore.Save(relPath, nbI);
    Console.WriteLine($"  [저장] {relPath}");
    string peakPathI = PathFor(SiegeSearchRoute.Peak);
    var savedI = SiegeBuildStore.Load(peakPathI);
    double sTotI = double.NegativeInfinity, sRankI = double.NegativeInfinity;
    if (savedI != null) { var ev = Evaluate(savedI); sTotI = ev.Res.TotalScore; sRankI = ev.Res.RankScore; }
    if (SiegeOptimizer.IsBetterBuild(rI.TotalScore, rI.RankScore, sTotI, sRankI))
    {
        if (savedI != null)
        {
            nbI.History = savedI.History; nbI.TargetScore = savedI.TargetScore;
            nbI.Stages = (savedI.Stages ?? new()).ToList();
            nbI.Log = (savedI.Log ?? new()).Concat(logI).ToList();
        }
        nbI.Stages.Add(new SiegeSavedBuild.StageDto { Name = "생존 조합(불사 순서에서 출발)", Score = nbI.Score, RankScore = nbI.RankScore, Deaths = nbI.Deaths, Order = string.Join(",", ptI.Select(x => x.Character.Name)) });
        SiegeBuildStore.Save(peakPathI, nbI);
        Console.WriteLine($"  [저장·고점] {(savedI == null ? "새 빌드" : $"갱신 {sTotI:N0} →")} {rI.TotalScore:N0} → {peakPathI}");
    }
    else Console.WriteLine($"  [유지·고점] 저장된 빌드 {sTotI:N0}(랭크 {sRankI:N0})가 생존 조합 결과 {rI.TotalScore:N0}(랭크 {rI.RankScore:N0}) 이상 — 바꾸지 않습니다.");
    return;
}

// ── 장신구비교=라이언: 저장된 빌드에서 그 영웅의 6성 장신구 옵션 2개 조합을 전부 바꿔 끼워 본다 (저장 안 함) ──
//   조합마다 그 영웅의 부옵 티어(치확·약확 ↔ 치피·공격력%)를 ① 평균 최대로 다시 맞춘 점수와 ② 거기서 N판 기대 최대로 맞춘 점수를 낸다
//   — 장신구가 치확을 주면 부옵의 치확을 치피로 돌릴 수 있으므로, 부옵을 다시 맞춰야 공정한 비교가 된다.
string accCmp = Val("장신구비교=");
if (accCmp != null)
{
    var sbA = SiegeBuildStore.Load(PathFor(route));
    if (sbA == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    int triesA = int.TryParse(Val("판="), out var ta) ? ta : 5;
    int sampA = int.TryParse(Val("표본="), out var sa) ? sa : 300;
    var ptA = SiegeBuildStore.ToParty(sbA); var plA = SiegeBuildStore.ToPlan(sbA, ptA);
    var cfgA = MakeCfg(ptA, null);
    var hA = ptA.FirstOrDefault(x => x.Character.Name == accCmp);
    if (hA?.Equipment?.Accessory == null) { Console.WriteLine($"영웅 없음: {accCmp}"); return; }
    var origAcc = hA.Equipment.Accessory;
    var slotsA = hA.Equipment.GetEquipments().SelectMany(e => e.SubSlots).ToList();
    var tiers0 = slotsA.Select(x => x.Tier).ToList();
    // `옵션=보피증%,1-3인기%,치명타확률%` — 비교할 옵션을 줄인다(기본 5종 = 10조합)
    string[] opts = Val("옵션=")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "보피증%", "1-3인기%", "피증%", "치명타확률%", "약점공격확률%" };
    string[] chanceA = { "치명타확률%", "약점공격확률%" }; string[] powerA = { "치명타피해%", "공격력%" };
    var pairsA = chanceA.SelectMany(c => powerA.SelectMany(w => new[] { (From: c, To: w), (From: w, To: c) })).ToList();
    string StatA()
    {
        var bs = new BattleSimulator();
        var bc = new BattleConfig { AllyParty = ptA, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, FormationName = sbA.Formation };
        var stt = bs.InitializeCharacterState(bc, hA, ptA.IndexOf(hA)); var dd = stt.DisplayStats;
        return $"치확 {dd.Cri:0.#}·치피 {dd.Cri_Dmg:0.#}·약확 {dd.Wek:0.#}";
    }
    Console.WriteLine($"═══ 장신구 비교 {day} [{teamKey}] {profile} · {RouteName(route)} — {accCmp} (현재 {origAcc.RingName ?? origAcc.MainOption}/{origAcc.SubOption}) · 부옵 재조정 포함, 저장 안 함");
    var rows = new List<(string Name, double Ev, string EvStat, double Mean, double B5, string B5Stat)>();
    for (int i = 0; i < opts.Length; i++)
        for (int j = i + 1; j < opts.Length; j++)
        {
            for (int k = 0; k < slotsA.Count; k++) slotsA[k].Tier = tiers0[k];
            hA.Equipment.Accessory = new Accessory { Grade = 6, MainOption = opts[i], SubOption = opts[j] };
            // ① 평균(기댓값) 최대로 부옵 재조정
            double cur = SiegeOptimizer.EvaluateBuild(cfgA, ptA, sbA.Formation, plA).RankScore;
            for (bool moved = true; moved;)
            {
                moved = false; GameDamageCalculator.Models.SubStatSlot ba = null, bb = null; double bv = cur;
                foreach (var e in hA.Equipment.GetEquipments())
                    foreach (var (f, t) in pairsA)
                    {
                        var a = e.SubSlots.FirstOrDefault(x => x.StatName == f && x.Tier > 1);
                        var b = e.SubSlots.FirstOrDefault(x => x.StatName == t && x.Tier < 6);
                        if (a == null || b == null) continue;
                        a.Tier--; b.Tier++;
                        double v = SiegeOptimizer.EvaluateBuild(cfgA, ptA, sbA.Formation, plA).RankScore;
                        a.Tier++; b.Tier--;
                        if (v > bv + 1) { bv = v; ba = a; bb = b; }
                    }
                if (ba != null) { ba.Tier--; bb.Tier++; cur = bv; moved = true; }
            }
            string evStat = StatA();
            // ② 거기서 N판 기대 최대로
            var (_, aft) = new SiegeOptimizer().TuneBestOfN(cfgA, ptA, sbA.Formation, plA, null, triesA, sampA, 16, 0.0, new[] { accCmp });
            rows.Add(($"{opts[i]} + {opts[j]}", cur, evStat, aft.Mean, aft.BestOfN, StatA()));
            Console.WriteLine($"  {opts[i]} + {opts[j]}: 평균 최대 {cur:N0} ({evStat}) · {triesA}판 기대 최대 {aft.BestOfN:N0} (그때 평균 {aft.Mean:N0}, {StatA()})");
        }
    for (int k = 0; k < slotsA.Count; k++) slotsA[k].Tier = tiers0[k];
    hA.Equipment.Accessory = origAcc;
    Console.WriteLine("  ── 평균 순 ──");
    foreach (var r in rows.OrderByDescending(r => r.Ev)) Console.WriteLine($"    {r.Ev,12:N0}  {r.Name}");
    Console.WriteLine($"  ── {triesA}판 기대 순 ──");
    foreach (var r in rows.OrderByDescending(r => r.B5)) Console.WriteLine($"    {r.B5,12:N0}  {r.Name}");
    return;
}

// ── 다섯판: 저장된 빌드(평균 최대)를 "하루 N판 중 최고점 기대"가 최대가 되게 부옵 티어를 손본다 (TuneBestOfN) ──
//   `다섯판 [판=5] [표본=300]`. 결과는 프로필 꼬리표 `_N판`으로 따로 저장 — 원래 빌드는 그대로. 확인: `프로필=5판 불러오기`.
if (args.Contains("다섯판"))
{
    var sbT = SiegeBuildStore.Load(PathFor(route));
    if (sbT == null) { Console.WriteLine("저장된 빌드 없음"); return; }
    int triesT = int.TryParse(Val("판="), out var tt) ? tt : 5;
    int sampT = int.TryParse(Val("표본="), out var st) ? st : 300;
    var ptT = SiegeBuildStore.ToParty(sbT); var plT = SiegeBuildStore.ToPlan(sbT, ptT);
    var cfgT = MakeCfg(ptT, null);
    Console.WriteLine($"═══ {triesT}판 기대 손질 {day} [{teamKey}] {profile} · {RouteName(route)}");
    var ev0 = SiegeOptimizer.EvaluateBuild(cfgT, ptT, sbT.Formation, plT);
    var big0 = SiegeOptimizer.ScoreDistribution(cfgT, ptT, sbT.Formation, plT, 2000, triesT);
    string DistStr(SiegeOptimizer.ScoreDist d) => $"평균 {d.Mean:N0} · 하위10% {d.P10:N0} · 중앙 {d.P50:N0} · 상위10% {d.P90:N0} · 최고 {d.Max:N0} · ★{d.Tries}판 중 최고 기대 {d.BestOfN:N0}";
    string StatsOf(BattleCharacter h)
    {
        var bs = new BattleSimulator();
        var bc = new BattleConfig { AllyParty = ptT, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, FormationName = sbT.Formation };
        var stt = bs.InitializeCharacterState(bc, h, ptT.IndexOf(h)); var dd = stt.DisplayStats;
        return $"공 {stt.FinalAtk:N0} · 치확 {dd.Cri:0.#} · 치피 {dd.Cri_Dmg:0.#} · 약확 {dd.Wek:0.#}";
    }
    Console.WriteLine($"  손질 전: 기댓값 {ev0.TotalScore:N0} (사망 {ev0.Deaths?.Count ?? 0})");
    Console.WriteLine($"    2000판: {DistStr(big0)}");
    foreach (var h in ptT) Console.WriteLine($"    [{h.Character.Name}] {StatsOf(h)}");
    var tlog = new List<string>();
    // 약확은 기본으로 건드리지 않는다(약점은 항상 터져야 한다는 지침). `약확허용`을 붙이면 약확도 옮겨 본다.
    new SiegeOptimizer().TuneBestOfN(cfgT, ptT, sbT.Formation, plT, tlog, triesT, sampT, lockWeak: !args.Contains("약확허용"));
    foreach (var l in tlog) Console.WriteLine("    " + l);
    var ev1 = SiegeOptimizer.EvaluateBuild(cfgT, ptT, sbT.Formation, plT);
    var big1 = SiegeOptimizer.ScoreDistribution(cfgT, ptT, sbT.Formation, plT, 2000, triesT);
    Console.WriteLine($"  손질 후: 기댓값 {ev1.TotalScore:N0} ({ev1.TotalScore - ev0.TotalScore:+#,0;-#,0}, 사망 {ev1.Deaths?.Count ?? 0})");
    Console.WriteLine($"    2000판: {DistStr(big1)}");
    Console.WriteLine($"    {triesT}판 중 최고 기대 {big0.BestOfN:N0} → {big1.BestOfN:N0} ({big1.BestOfN - big0.BestOfN:+#,0;-#,0})");
    foreach (var h in ptT) Console.WriteLine($"    [{h.Character.Name}] {StatsOf(h)}");
    var backT = ptT.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
    var nbT = SiegeBuildStore.Capture(ptT, sbT.Formation, backT, plT);
    nbT.Day = day; nbT.Profile = profile + $"_{triesT}판"; nbT.Route = RouteName(route);
    nbT.Score = Math.Round(ev1.TotalScore); nbT.RankScore = Math.Round(ev1.RankScore); nbT.Deaths = ev1.Deaths?.Count ?? 0;
    nbT.InGameBuffTargeting = ingame; nbT.EngineHash = engineHash; nbT.GitCommit = gitHead; nbT.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    nbT.StartOrder = sbT.StartOrder; nbT.SearchMinutes = sbT.SearchMinutes;
    nbT.Pet = sbT.Pet ?? new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
    nbT.TargetScore = sbT.TargetScore;
    nbT.Log = tlog.Concat(new[] { $"[{triesT}판 기대] 손질 전 2000판: {DistStr(big0)}", $"[{triesT}판 기대] 손질 후 2000판: {DistStr(big1)}" }).ToList();
    foreach (var l in DiffLines(sbT, nbT)) Console.WriteLine("    바뀐 것: " + l);
    string tpath = System.IO.Path.Combine(outDir, $"build_{day}_{profile}_{triesT}판{(route == SiegeSearchRoute.Standard ? "_정석" : "")}_{teamKey}.json");
    SiegeBuildStore.Save(tpath, nbT);
    Console.WriteLine($"  [저장] {tpath}");
    return;
}

//   더 높아지면 그 빌드로 저장한 뒤 아래 리포트로 이어진다(탐색은 건너뜀).
bool stageOnly = args.Contains("단계");
if (stageOnly)
{
    Console.WriteLine($"═══ 단계(순서 편집+생존 보강) {day} [{teamKey}] {profile}");
    foreach (var rt in route == SiegeSearchRoute.Standard ? new[] { SiegeSearchRoute.Standard } : new[] { SiegeSearchRoute.Standard, SiegeSearchRoute.Peak })
    {
        string spath = PathFor(rt);
        var sb0 = SiegeBuildStore.Load(spath);
        if (sb0 == null) { Console.WriteLine($"[{RouteName(rt)}] 저장된 빌드 없음"); continue; }
        var sparty = SiegeBuildStore.ToParty(sb0);
        var splan = SiegeBuildStore.ToPlan(sb0, sparty);
        var scfg = MakeCfg(sparty, null);
        var swS = System.Diagnostics.Stopwatch.StartNew();
        var before = SiegeOptimizer.EvaluateBuild(scfg, sparty, sb0.Formation, splan);
        Console.WriteLine($"[{RouteName(rt)}] 시작 {before.TotalScore:N0} (랭크 {before.RankScore:N0}, 사망 {before.Deaths?.Count ?? 0})");
        var (sr, sp, slog) = new SiegeOptimizer().RunEditSurvival(scfg, sparty, sb0.Formation, splan);
        foreach (var l in slog) Console.WriteLine("    " + l);
        if (!SiegeOptimizer.IsBetterBuild(sr.TotalScore, sr.RankScore, before.TotalScore, before.RankScore))
        {
            Console.WriteLine($"[{RouteName(rt)}] 더 나은 빌드 없음 — 저장된 빌드를 그대로 둡니다. ({swS.Elapsed.TotalMinutes:F1}분)");
            continue;
        }
        var chk = SiegeOptimizer.EvaluateBuild(scfg, sparty, sb0.Formation, sp);
        var sback = sparty.Where(x => x.IsBackPosition).Select(x => x.Character.Name).ToList();
        var nb2 = SiegeBuildStore.Capture(sparty, sb0.Formation, sback, sp);
        nb2.Day = day; nb2.Profile = profile; nb2.Route = RouteName(rt);
        nb2.Score = Math.Round(chk.TotalScore); nb2.RankScore = Math.Round(chk.RankScore); nb2.Deaths = chk.Deaths?.Count ?? 0;
        nb2.InGameBuffTargeting = ingame; nb2.EngineHash = engineHash; nb2.GitCommit = gitHead; nb2.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        nb2.StartOrder = sb0.StartOrder; nb2.SearchMinutes = Math.Round(sb0.SearchMinutes + swS.Elapsed.TotalMinutes, 1);
        nb2.Pet = sb0.Pet ?? new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
        nb2.History = sb0.History; nb2.TargetScore = sb0.TargetScore;
        nb2.Stages = (sb0.Stages ?? new()).ToList();
        nb2.Stages.Add(new SiegeSavedBuild.StageDto
        {
            Name = "순서 편집+생존 보강(단계)", Score = nb2.Score, RankScore = nb2.RankScore, Deaths = nb2.Deaths,
            Order = string.Join(",", sparty.Select(x => x.Character.Name)), Standard = rt == SiegeSearchRoute.Standard,
        });
        nb2.Log = (sb0.Log ?? new()).Concat(slog).ToList();
        SiegeBuildStore.Save(spath, nb2);
        Console.WriteLine($"[저장·{RouteName(rt)}] {before.TotalScore:N0} → {chk.TotalScore:N0} ({chk.TotalScore - before.TotalScore:+#,0;-#,0}, 사망 {nb2.Deaths}) · {swS.Elapsed.TotalMinutes:F1}분 → {spath}");
    }
    Console.WriteLine();
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

if (!loadOnly && !stageOnly)
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
            b.Pet = new SiegeSavedBuild.PetDto { Name = petName, Star = 6, Enhance = 3, PotentialAtkRate = 72 };
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
        AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, MaxTurns = 70,
        RotationPlan = fin.Plan, AllyDeathPenalty = penalty, RecordFeasibility = true, InGameBuffTargeting = ingame,
        DiagAlliesImmortal = immortalSearch,   // 불사탐색 프로필은 리포트·턴로그도 같은 조건으로 재생
    });
    var bcfg = new BattleConfig { AllyParty = fin.Party, AllyPet = pet, PetStar = 6, PetEnhance = 3, PetOptionAtkRate = PETPOT, FormationName = best.Formation };
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
    sb.AppendLine("──── 공격력 · 속공 · 치명/약점 (전투 시작 시점 — 상시 패시브 포함, 턴제 버프 제외) ────");
    for (int i = 0; i < fin.Party.Count; i++)
    {
        var st = bsim.InitializeCharacterState(bcfg, fin.Party[i], i);
        var ds = st.DisplayStats;
        sb.AppendLine($"  {fin.Party[i].Character.Name}: 공격력 {st.FinalAtk:N0} · 속공 {st.FinalSpd:N0} · 치확 {ds.Cri:0.#}% · 치피 {ds.Cri_Dmg:0.#}% · 약확 {ds.Wek:0.#}% · 약피 {ds.Wek_Dmg:0.#}%");
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
        // 생존 여유 — 영웅별 피격 직후 최저 HP(시점·원인)와 무효화·부활로 넘긴 횟수. 인게임 검증 전에 얇은 여유를 먼저 본다.
        tl.AppendLine();
        tl.AppendLine("──── 생존 여유 (피격 직후 최저 HP) ────");
        var hpRx = new System.Text.RegularExpressions.Regex(@"→ (\S+?): [\d,]+(?: \((\S+?)\))? \(HP ([\d,]+)/([\d,]+)\)");
        var low = new Dictionary<string, (double Pct, int Turn, string Cause, int Hits)>();
        var saved = new Dictionary<string, List<string>>();
        foreach (var l in rep.TurnLogs.Where(l => !l.IsAlly))
        {
            var m = hpRx.Match(l.Description ?? "");
            if (m.Success)
            {
                string who = m.Groups[1].Value;
                double pct = 100.0 * double.Parse(m.Groups[3].Value.Replace(",", "")) / double.Parse(m.Groups[4].Value.Replace(",", ""));
                var cur = low.TryGetValue(who, out var c) ? c : (Pct: 101.0, Turn: 0, Cause: "", Hits: 0);
                // 부활·권능 등으로 넘긴 치명타는 HP가 다시 차므로 최저값이 아니라 "넘김" 목록에 따로 적는다
                if (m.Groups[2].Success)
                {
                    if (!saved.TryGetValue(who, out var sl)) saved[who] = sl = new List<string>();
                    sl.Add($"T{l.Turn} {m.Groups[2].Value}({l.ActorName} {l.SkillName})");
                    low[who] = (cur.Pct, cur.Turn, cur.Cause, cur.Hits + 1);
                }
                else low[who] = pct < cur.Pct ? (pct, l.Turn, $"{l.ActorName} {l.SkillName}", cur.Hits + 1) : (cur.Pct, cur.Turn, cur.Cause, cur.Hits + 1);
            }
        }
        var nulls = rep.TurnLogs.Where(l => !l.IsAlly && l.SkillName == "피해 무효화")
            .GroupBy(l => (l.Description ?? "").Split(' ')[0]).ToDictionary(g => g.Key, g => g.Count());
        foreach (var name in low.Keys.Union(nulls.Keys).Union(saved.Keys))
        {
            var parts = new List<string>();
            if (low.TryGetValue(name, out var v) && v.Pct <= 100) parts.Add($"최저 {v.Pct:F0}% (T{v.Turn} {v.Cause}) · 피격 {v.Hits}회");
            else if (low.ContainsKey(name)) parts.Add($"피격 {low[name].Hits}회");
            if (nulls.TryGetValue(name, out int nc)) parts.Add($"무효화 {nc}회");
            if (saved.TryGetValue(name, out var sv)) parts.Add("치명 넘김: " + string.Join(", ", sv));
            tl.AppendLine($"  {name}: {string.Join(" · ", parts)}");
        }
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

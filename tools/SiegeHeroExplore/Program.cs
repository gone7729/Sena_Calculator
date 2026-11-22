using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;
using GameDamageCalculator.Services.BattleEngine;

// ============================================================================
// 공성 영웅 DB 탐색 (12초월·잠재3·윈디 — SiegeBatchAllDays 고스펙 프로필과 동일 스펙).
//   전수 조합(125C5 ≈ 2.3억)은 불가 → 단계형:
//   [A] 기준 팀(results/siege/siege_{요일}_12초월_윈디.json의 팀) 기어를 1회 잡고,
//       한 자리씩 DB 전 영웅으로 교체 → 교체 영웅만 기어 탐색 + 짧은 빔(진형·후열은 기준 고정) = 스크리닝 점수.
//   [B] 상위 교체들을 서로 다른 자리끼리 2명 교체로 조합해 같은 방식으로 재평가.
//   [C] 상위 팀만 배치와 같은 정식 탐색(전원 기어·진형·자리·빔·좌표상승) — "정식N" 인자로 개수 지정.
//   각성 보유 영웅은 각성 상태(IsAwakened)로 평가 (고스펙 가정).
//   후보 필터(기본 ON, "전체" 인자로 끔): 요일 감쇄 90%↑ 공격 속성의 딜러 타입(공격형·마법형·만능형)은 후보에서 제외.
//     단 그 요일 적 상태이상의 파티 면역 보유자(월 지크·수 라이언 등)는 유지. 지원형·방어형은 딜이 본업이 아니라 유지(목~토 비스킷).
// 사용: dotnet run -c Release -- 화요일 [limitN] [정식N] [전체] [각성기끔]
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;
string day = args.FirstOrDefault(a => a.EndsWith("요일")) ?? "화요일";
int limit = int.TryParse(args.FirstOrDefault(a => a.StartsWith("limit"))?.Substring(5), out var lv) ? lv : int.MaxValue;
int fullN = int.TryParse(args.FirstOrDefault(a => a.StartsWith("정식"))?.Substring(2), out var fv) ? fv : 3;
const int TRANS = 12, POT = 3;

string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string resDir = System.IO.Path.Combine(repoRoot, "results", "siege");
string basePath = System.IO.Path.Combine(resDir, $"siege_{day}_12초월_윈디.json");
var stage = EnemyDb.SiegeStages[day];
// [각성기끔] 각성기(SkillType.Awaken)를 전 영웅에서 뺀다 — 각성 게이지가 미모델이라 시뮬은 각성기를 쿨 0·비용 0으로
//   스킬 턴마다 쓸 수 있다(쥬다스 「심판의 날」 연속 시전으로 팀 점수의 70%+). 게이지 모델 전까지의 하한 평가용.
bool noAwkSkill = args.Contains("각성기끔");
if (noAwkSkill)
    foreach (var c in CharacterDb.Characters) c.Skills?.RemoveAll(s => s.SkillType == SkillType.Awaken);

BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = TRANS, IsAwakened = c.HasAwakening,
        PotentialAtkLevel = POT, PotentialDefLevel = POT, PotentialHpLevel = POT, Equipment = null,
    };
}

// 스크리닝용 — 전용무기를 null 거치지 않고 바로 범용 장착(같은 영웅을 여러 병렬 평가가 공유하므로 null 순간이 생기면 경합)
BattleCharacter HeroS(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = ExclusiveWeaponDb.Universal(magic: c.AttackType == AttackType.Magic);
    return new BattleCharacter
    {
        Character = c, IsSkillEnhanced = true, TranscendLevel = TRANS, IsAwakened = c.HasAwakening,
        PotentialAtkLevel = POT, PotentialDefLevel = POT, PotentialHpLevel = POT, Equipment = null,
    };
}

SiegeOptimizerConfig Cfg(List<BattleCharacter> team, bool full, string formation = null, List<string> back = null, bool beam = true)
{
    var cfg = new SiegeOptimizerConfig
    {
        FixedMembers = team, Candidates = new List<BattleCharacter>(), PartySize = 5, SiegeStage = stage,
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76, MaxTurns = 70,
        AutoEquip = true, SearchExclusiveWeapon = true, FloorFirstGear = true, AllyDeathPenalty = 1_000_000,
        OptimizeRotation = true,
    };
    if (full)
    {
        // SiegeBatchAllDays 12초월 기본과 동일 (진형·자리 전체 탐색, 좌표상승, 권능반지)
        cfg.EnableSurvivalRings = true; cfg.RotationBeamWidth = 10; cfg.RotationMaxDepth = 36; cfg.CoordinateAscentGear = true;
        cfg.CounterattackSearchSeeds = day == "금요일" ? 10 : day == "일요일" ? 4 : 1;
        cfg.EvalSeeds = day == "일요일" ? 16 : 1;
        if (day == "일요일") cfg.AllyDeathPenalty = 0;
    }
    else
    {
        // 스크리닝: 진형·후열 고정, 좁은 빔, 프록시 기어(QuickGear), 전용은 범용 장착(조율 탐색 생략), 반지·좌표상승 없음
        cfg.EnableSurvivalRings = false; cfg.RotationBeamWidth = 4; cfg.RotationMaxDepth = 28;
        cfg.QuickGear = true; cfg.SearchExclusiveWeapon = false;
        cfg.OptimizeRotation = beam;   // false = 자동 로테 점수만 (1차 거름용)
        foreach (var b in team.Where(b => b.Equipment == null && b.Character.ExclusiveWeapon == null))
            b.Character.ExclusiveWeapon = ExclusiveWeaponDb.Universal(magic: b.Character.AttackType == AttackType.Magic);
        cfg.ForcedFormation = formation; cfg.ForcedBackRow = back;
    }
    return cfg;
}

// ---- 기준 팀
int[] baseIds; string jForm; List<string> jBack;
using (var doc = JsonDocument.Parse(System.IO.File.ReadAllText(basePath)))
{
    baseIds = doc.RootElement.GetProperty("team").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToArray();
    jForm = doc.RootElement.GetProperty("formation").GetString();
    jBack = doc.RootElement.GetProperty("backRow").EnumerateArray().Select(x => x.GetString()).ToList();
}
var sw = Stopwatch.StartNew();
var baseTeam = baseIds.Select(Hero).ToList();
// quickbase(측정용): 기준 팀도 저예산(기존 결과의 진형·후열 고정)으로 — 정식은 20분+ 소요
var baseRes = new SiegeOptimizer().Optimize(args.Contains("quickbase") ? Cfg(baseTeam, full: false, jForm, jBack) : Cfg(baseTeam, full: true));
string baseForm = baseRes.BestFormation;
var baseBack = baseRes.BestBackRow.ToList();
var baseParty = baseRes.BestParty;   // 기어 장착된 객체 (이후 교체 평가에서 재사용)
var log = new StringBuilder();
void Out(string s) { lock (log) { Console.WriteLine(s); log.AppendLine(s); } }
Out($"═══ {day} 영웅 DB 탐색 (12초월·잠재3·윈디, 각성 보유 영웅은 각성{(noAwkSkill ? " · 각성기 끔" : "")}) ═══");
Out($"[기준·정식] {string.Join(",", baseParty.Select(b => b.Character.Name))} = {baseRes.BestScore:N0} ({baseForm}, 후열 {string.Join(",", baseBack)}) — {sw.Elapsed.TotalMinutes:F1}분");

// 평가마다 BattleCharacter 얕은 복제 — 옵티마이저가 IsBackPosition 등을 바꾸므로 병렬 평가 간 공유 금지.
//   Equipment(기어 객체)는 기준 멤버가 이미 장착 → 읽기만 함(QuickGear·전용 탐색 OFF라 기준 멤버 변경 없음).
static BattleCharacter Clone(BattleCharacter b)
{
    var c = new BattleCharacter();
    foreach (var pi in typeof(BattleCharacter).GetProperties())
        if (pi.CanRead && pi.CanWrite) pi.SetValue(c, pi.GetValue(b));
    return c;
}
var po = new ParallelOptions { MaxDegreeOfParallelism = 10 };

// 스크리닝 기준점: 같은 저예산 파이프라인으로 기준 팀 재평가(공정 비교)
//   free = 진형·후열을 고정하지 않고 스크리닝이 스스로 고른다(밸런스·보호 × 후열 배치). 교체 영웅이 들어오면 최적 진형이
//   달라질 수 있어(토요일: 타카→아일린이면 밸런스 후열 2명 → 보호 진형 라이언 단독 후열) [A2]·[B]는 free로 잰다.
//   [A1] 자동로테 1차 거름만 기준 진형·후열 고정(건수가 많아서).
double Screen(List<BattleCharacter> team, List<string> back, out SiegeOptimizerResult r, bool beam = true, bool free = false)
{
    r = new SiegeOptimizer().Optimize(Cfg(team, full: false, free ? null : baseForm, free ? null : back, beam));
    return r.BestScore;
}
string Seat(SiegeOptimizerResult r) => r == null ? "" : $"{r.BestFormation?.Replace(" 진형", "")}·후열 {string.Join("+", r.BestBackRow ?? new List<string>())}";
var t0 = Stopwatch.StartNew();
double baseScreen = Screen(baseParty.Select(Clone).ToList(), baseBack, out var baseScreenRes, free: true);
Out($"[기준·스크리닝] {baseScreen:N0} ({Seat(baseScreenRes)}) ({t0.Elapsed.TotalSeconds:F0}s)");

// ---- [A] 1자리 교체
var inTeam = new HashSet<int>(baseIds);
// 후보 필터: 요일 감쇄 속성 딜러 제외 (그 요일 상태이상 파티 면역 보유자는 유지)
var dayStatuses = new HashSet<StatusEffectType>();
double physRed = 0, magRed = 0;
foreach (var w in stage.Waves)
    foreach (var se in w.Enemies)
    {
        var en = EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == se.EnemyId);
        if (en == null) continue;
        physRed = Math.Max(physRed, en.PhysicalReduction); magRed = Math.Max(magRed, en.MagicReduction);
        foreach (var sk in en.Skills ?? new())
            foreach (var st in sk.GetLevelData(false)?.StatusEffects ?? new())
                dayStatuses.Add(st.Type);
    }
AttackType? reducedType = physRed >= 90 ? AttackType.Physical : magRed >= 90 ? AttackType.Magic : (AttackType?)null;
bool GrantsDayImmunity(Character c)
{
    if (c.Passive == null) return false;
    var effs = c.Passive.LevelData.Values.SelectMany(l => l.Effects ?? new())
        .Concat((c.Passive.TranscendBonuses ?? new()).Values.SelectMany(t => t.Effects ?? new()));
    return effs.Any(e => e.Type == PersistentEffectType.Immunity && e.Target == EffectTarget.Party
        && e.StatusImmunity?.Types != null && e.StatusImmunity.Types.Any(dayStatuses.Contains));
}
var dealerTypes = new HashSet<string> { "공격형", "마법형", "만능형" };
bool filterOn = !args.Contains("전체") && reducedType != null;
var others = CharacterDb.Characters.Where(c => !inTeam.Contains(c.Id)).ToList();
var kept = others.Where(c => !filterOn || c.AttackType != reducedType || !dealerTypes.Contains(c.Type) || GrantsDayImmunity(c)).ToList();
var pool = kept.Select(c => c.Id).ToList();
if (filterOn)
{
    var exempt = others.Where(c => c.AttackType == reducedType && dealerTypes.Contains(c.Type) && GrantsDayImmunity(c)).Select(c => c.Name).ToList();
    Out($"[후보 필터] {(reducedType == AttackType.Physical ? "물리" : "마법")} 감쇄 90% 요일 — 해당 속성 딜러 {others.Count - kept.Count}명 제외, 후보 {kept.Count}명"
        + $" (적 상태이상 {string.Join("·", dayStatuses)} 면역이라 유지: {(exempt.Count > 0 ? string.Join(", ", exempt) : "없음")})");
}
else Out($"[후보 필터] 없음 — 후보 {kept.Count}명");
// [A1] 전 영웅 × 전 자리 — 자동 로테 점수(빔 없음)로 1차 거름. [A2] 자리별 상위 TOP_AUTO명만 짧은 빔으로 재평가.
const int TOP_AUTO = 12;
double baseAuto = Screen(baseParty.Select(Clone).ToList(), baseBack, out _, beam: false);
Out($"[기준·자동로테] {baseAuto:N0}");
var autoBag = new System.Collections.Concurrent.ConcurrentBag<(int Slot, int HeroId, string Name, double Score)>();
int done = 0;
var ta = Stopwatch.StartNew();
var jobs = Enumerable.Range(0, baseParty.Count).SelectMany(slot => pool.Select(hid => (slot, hid))).Take(limit).ToList();
List<BattleCharacter> SwapTeam(int slot, int hid, out List<string> back)
{
    var team = baseParty.Select(Clone).ToList();
    var nh = HeroS(hid);
    team[slot] = nh;
    back = baseBack.Select(n => n == baseParty[slot].Character.Name ? nh.Character.Name : n).ToList();
    return team;
}
Parallel.ForEach(jobs, po, job =>
{
    var (slot, hid) = job;
    var team = SwapTeam(slot, hid, out var back);
    try { autoBag.Add((slot, hid, team[slot].Character.Name, Screen(team, back, out _, beam: false))); }
    catch (Exception ex) { lock (log) Out($"  [오류] {team[slot].Character.Name}: {ex.Message}"); }
    int d = System.Threading.Interlocked.Increment(ref done);
    if (d % 50 == 0) Console.WriteLine($"  … 자동 {d}/{jobs.Count}건 ({ta.Elapsed.TotalMinutes:F1}분)");
});
Out($"\n[A1] 자동로테 거름 {autoBag.Count}건 — {ta.Elapsed.TotalMinutes:F1}분");
for (int slot = 0; slot < baseParty.Count; slot++)
    Out($"  {baseParty[slot].Character.Name} 자리 자동 상위: " + string.Join(" / ", autoBag.Where(x => x.Slot == slot)
        .OrderByDescending(x => x.Score).Take(TOP_AUTO).Select(x => $"{x.Name} {x.Score / baseAuto * 100:F0}%")));
var beamJobs = Enumerable.Range(0, baseParty.Count)
    .SelectMany(slot => autoBag.Where(x => x.Slot == slot).OrderByDescending(x => x.Score).Take(TOP_AUTO)).ToList();
var single = new System.Collections.Concurrent.ConcurrentBag<(int Slot, int HeroId, string Name, double Score, string Seat)>();
done = 0; ta.Restart();
Parallel.ForEach(beamJobs, po, job =>
{
    var team = SwapTeam(job.Slot, job.HeroId, out var back);
    try { double sc = Screen(team, back, out var sr, free: true); single.Add((job.Slot, job.HeroId, job.Name, sc, Seat(sr))); }
    catch (Exception ex) { lock (log) Out($"  [오류] {job.Name}: {ex.Message}"); }
    int d = System.Threading.Interlocked.Increment(ref done);
    if (d % 10 == 0) Console.WriteLine($"  … 빔 {d}/{beamJobs.Count}건 ({ta.Elapsed.TotalMinutes:F1}분)");
});
Out($"\n[A2] 1자리 교체 빔 재평가 {single.Count}건 — {ta.Elapsed.TotalMinutes:F1}분 (건당 {ta.Elapsed.TotalSeconds / Math.Max(1, single.Count):F1}s)");
for (int slot = 0; slot < baseParty.Count; slot++)
{
    var top = single.Where(x => x.Slot == slot).OrderByDescending(x => x.Score).Take(6).ToList();
    Out($"  {baseParty[slot].Character.Name} 자리: " + string.Join(" / ", top.Select(x => $"{x.Name} {x.Score / baseScreen * 100:F1}%")));
}
var improving = single.Where(x => x.Score > baseScreen).OrderByDescending(x => x.Score).ToList();
Out($"  기준 초과 교체 {improving.Count}건: " + string.Join(", ", improving.Take(15).Select(x => $"{baseParty[x.Slot].Character.Name}→{x.Name} {x.Score:N0} [{x.Seat}]")));
if (limit != int.MaxValue) { Save(); return; }

// ---- [B] 2자리 교체 (서로 다른 자리, 상위 교체끼리)
var topPerSlot = Enumerable.Range(0, baseParty.Count)
    .SelectMany(s => single.Where(x => x.Slot == s && x.Score > baseScreen * 0.97).OrderByDescending(x => x.Score).Take(3)).ToList();
var pairs = new List<(int S1, int H1, int S2, int H2)>();
for (int i = 0; i < topPerSlot.Count; i++)
    for (int j = i + 1; j < topPerSlot.Count; j++)
        if (topPerSlot[i].Slot != topPerSlot[j].Slot && topPerSlot[i].HeroId != topPerSlot[j].HeroId)
            pairs.Add((topPerSlot[i].Slot, topPerSlot[i].HeroId, topPerSlot[j].Slot, topPerSlot[j].HeroId));
var tb = Stopwatch.StartNew();
var doubles = new System.Collections.Concurrent.ConcurrentBag<(List<int> Ids, double Score, string Seat)>();
Parallel.ForEach(pairs, po, pr =>
{
    var (s1, h1, s2, h2) = pr;
    var team = baseParty.Select(Clone).ToList();
    var a = HeroS(h1); var b = HeroS(h2);
    team[s1] = a; team[s2] = b;
    var back = baseBack.Select(n => n == baseParty[s1].Character.Name ? a.Character.Name
                                  : n == baseParty[s2].Character.Name ? b.Character.Name : n).ToList();
    try { double sc = Screen(team, back, out var sr, free: true); doubles.Add((team.Select(t => t.Character.Id).ToList(), sc, Seat(sr))); }
    catch (Exception ex) { lock (log) Out($"  [오류] {a.Character.Name}+{b.Character.Name}: {ex.Message}"); }
});
Out($"\n[B] 2자리 교체 {doubles.Count}건 — {tb.Elapsed.TotalMinutes:F1}분");
foreach (var d in doubles.OrderByDescending(d => d.Score).Take(8))
    Out($"  {string.Join(",", d.Ids.Select(i => CharacterDb.Characters.First(c => c.Id == i).Name))} = {d.Score:N0} ({d.Score / baseScreen * 100:F1}%) [{d.Seat}]");

// ---- [C] 상위 팀 정식 탐색
var cands = single.Select(x => { var ids = baseIds.ToList(); ids[x.Slot] = x.HeroId; return (Ids: ids, x.Score); })
    .Concat(doubles.Select(d => (d.Ids, d.Score)))
    .OrderByDescending(x => x.Score)   // 기준 초과 필터 없음: 스크리닝은 교체 영웅에 범용 전용·프록시 기어라 불리 → 상위 N팀을 정식으로 재판정
    .GroupBy(x => string.Join(",", x.Ids.OrderBy(i => i))).Select(g => g.First())
    .Take(fullN).ToList();
Out($"\n[C] 정식 탐색 {cands.Count}팀 (기준 정식 {baseRes.BestScore:N0})");
// 정식 탐색은 전용무기 조율이 Character(정적 DB 객체)를 바꾸므로, 같은 영웅이 여러 팀에 있으면 병렬 시 경합.
//   → 영웅이 겹치지 않는 팀끼리만 동시에, 겹치면 순차(간단히: 팀별 순차 실행 대신 겹침 없는 묶음 단위 병렬).
var finalsBag = new System.Collections.Concurrent.ConcurrentBag<(string Team, double Score, SiegeOptimizerResult R, double Min)>();
var remainingC = cands.ToList();
while (remainingC.Count > 0)
{
    var batch = new List<(List<int> Ids, double Score)>(); var used = new HashSet<int>();
    foreach (var c in remainingC.ToList())
        if (!c.Ids.Any(used.Contains)) { batch.Add(c); foreach (var i in c.Ids) used.Add(i); remainingC.Remove(c); }
    Parallel.ForEach(batch, po, c =>
    {
        var tc = Stopwatch.StartNew();
        var team = c.Ids.Select(Hero).ToList();
        var r = new SiegeOptimizer().Optimize(Cfg(team, full: true));
        finalsBag.Add((string.Join(",", r.BestParty.Select(b => b.Character.Name)), r.BestScore, r, tc.Elapsed.TotalMinutes));
    });
}
var finals = finalsBag.OrderByDescending(f => f.Score).ToList();
foreach (var (nm, sc, r, min) in finals)
{
    Out($"  {nm} = {sc:N0} ({sc / baseRes.BestScore * 100:F1}% of 기준) {r.BestFormation} 후열 {string.Join(",", r.BestBackRow)} — {min:F1}분");
    foreach (var cr in r.BestResult?.CharacterResults.OrderByDescending(x => x.TotalDamage) ?? Enumerable.Empty<SiegeCharacterResult>())
        Out($"      {cr.CharacterName}: {cr.TotalDamage:N0} ({cr.DamageShare:F1}%){(cr.Died ? " 사망" : "")}");
}
var best = finals.FirstOrDefault();
if (best.R != null && best.Score > baseRes.BestScore)
{
    Out($"\n★ 최고: {best.Team} = {best.Score:N0} (기준 대비 {(best.Score / baseRes.BestScore - 1) * 100:+0.0;-0.0}%)");
    Out("  기어: "); foreach (var g in best.R.GearLog) Out("    " + g);
}
else Out($"\n★ 기준 팀({baseRes.BestScore:N0})을 넘는 팀 없음");
Out($"총 소요 {sw.Elapsed.TotalMinutes:F1}분");
Save();

void Save()
{
    string outPath = System.IO.Path.Combine(resDir, $"explore_{day}_12초월_윈디{(noAwkSkill ? "_각성기끔" : "")}.txt");
    System.IO.File.WriteAllText(outPath, log.ToString(), Encoding.UTF8);
    Console.WriteLine($"→ {outPath}");
}

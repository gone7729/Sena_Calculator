using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 버프 타게팅 모델 비교 — "공격력이 가장 높은 아군" 버프(비스킷 장비강화)·평타 쿨감(라이언·나타) 대상 선정.
//   DW  = 기존: 스카우팅 누적딜(DamageWeight) 순 → 버프가 항상 이상적인 딜러에게 가는 낙관 모델
//   ING = 인게임: 시전 순간 현재 공격력(스냅샷 × 전투 중 공%버프 × (1-공감)) 순
//
// 실행: dotnet run -c Release --project tools/TargetingCompare -- [요일=수요일] [초월=6] [opt-ingame]
//   1) 기존(DW) 모드로 옵티마이저 탐색 → 그 빌드·로테를 DW / ING 두 모드로 재시뮬해 점수·버프 수령자 비교
//   2) opt-ingame 인자 시: ING 모드로 옵티마이저를 다시 돌려, 인게임 규칙에서의 최적 빌드가 무엇인지 확인
Console.OutputEncoding = Encoding.UTF8;
string DAY = args.FirstOrDefault(a => a.EndsWith("요일")) ?? "수요일";
int TR = args.Select(a => int.TryParse(a, out var v) ? v : 0).FirstOrDefault(v => v > 0);
if (TR == 0) TR = 6;
bool optIngame = args.Contains("opt-ingame");

var TEAMS = new Dictionary<string, int[]>
{
    ["수요일"] = new[] { 118, 103, 202, 201, 2 },   // 나타·미호·리나·비스킷·라이언 (SiegeBatchAllDays와 동일)
};
if (!TEAMS.TryGetValue(DAY, out var ids)) { Console.WriteLine($"{DAY} 팀 미정의"); return; }

BattleCharacter Hero(int id)
{
    var c = CharacterDb.Characters.First(x => x.Id == id);
    c.ExclusiveWeapon = null;
    return new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = TR, Equipment = null };
}

SiegeOptimizerConfig OptCfg(bool ingame) => new()
{
    FixedMembers = ids.Select(Hero).ToList(), Candidates = new List<BattleCharacter>(), PartySize = 5,
    SiegeStage = EnemyDb.SiegeStages[DAY], AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
    MaxTurns = 70, AutoEquip = true, SearchExclusiveWeapon = TR >= 12, EnableSurvivalRings = TR >= 12,
    FloorFirstGear = true, AllyDeathPenalty = 1_000_000, OptimizeRotation = true, RotationBeamWidth = 10, RotationMaxDepth = 36,
    CoordinateAscentGear = true, InGameBuffTargeting = ingame,
};

SiegeBattleResult Sim(SiegeOptimizerResult o, bool ingame, out string diag)
{
    var cfg = new SiegeBattleConfig
    {
        AllyParty = o.BestParty, FormationName = o.BestFormation, SiegeStage = EnemyDb.SiegeStages[DAY],
        AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72, MaxTurns = 70,
        RotationPlan = o.BestRotationPlan, AllyDeathPenalty = 1_000_000, RecordFeasibility = true,
        CounterattackChanceOverride = 0, InGameBuffTargeting = ingame,
    };
    var sim = new SiegeBattleSimulator(777) { DiagAllyStats = true };
    var r = sim.Simulate(cfg);
    diag = sim.DiagLog.ToString();
    return r;
}

void Report(string label, SiegeBattleResult r)
{
    Console.WriteLine($"\n[{label}] 총점 {r.TotalScore:N0} · {r.RoundsCleared}R · 사망 {r.Deaths.Count}"
        + (r.Deaths.Count > 0 ? " (" + string.Join(", ", r.Deaths.Select(d => $"T{d.Turn} {d.AllyName}")) + ")" : ""));
    Console.WriteLine("  캐릭별: " + string.Join(" / ", r.CharacterResults.OrderByDescending(c => c.TotalDamage)
        .Select(c => $"{c.CharacterName} {c.TotalDamage:N0}")));
    var cdr = r.TurnLogs.Where(l => (l.Description ?? "").Contains("평타 쿨감")).ToList();
    var bySrc = cdr.GroupBy(l => l.ActorName ?? "?").Select(g =>
    {
        var to = new Dictionary<string, int>();
        foreach (var l in g) foreach (var n in l.Description.Split(':').Last().Split(',').Select(s => s.Trim()))
            if (n != g.Key) to[n] = to.GetValueOrDefault(n) + 1;
        return $"{g.Key} 평타{g.Count()}회→[{string.Join(",", to.Select(k => $"{k.Key}{k.Value}"))}]";
    });
    Console.WriteLine("  평타쿨감 수령(자신 제외): " + string.Join(" / ", bySrc));
    var enh = r.Feasibility.Where(f => f.Reached && !f.Hold && f.SkillName == "장비 강화" && f.BuffTargets != null)
        .Select(f => $"T{f.Turn}[{string.Join(",", f.BuffTargets)}]");
    Console.WriteLine("  장비강화 수령: " + string.Join(" ", enh));
}

void Compare(string title, SiegeOptimizerResult o)
{
    Console.WriteLine($"\n════════ {title} ════════");
    Console.WriteLine($"옵티 보고 점수 {o.BestScore:N0} · 진형 {o.BestFormation} · 후열 {string.Join(",", o.BestBackRow)}");
    foreach (var g in o.GearLog.Where(g => g.Contains("기어스탯"))) Console.WriteLine("  " + g.Trim());
    var dw = Sim(o, false, out var diag);
    Console.Write(diag);
    Report("DW 모드 재시뮬", dw);
    var ing = Sim(o, true, out _);
    Report("ING 모드 재시뮬", ing);
    Console.WriteLine($"\n  → 같은 빌드·로테, 타게팅만 인게임 규칙: {dw.TotalScore:N0} → {ing.TotalScore:N0} ({(ing.TotalScore / Math.Max(1, dw.TotalScore) - 1) * 100:+0.0;-0.0}%)");
}

var sw = System.Diagnostics.Stopwatch.StartNew();
var oDw = new SiegeOptimizer().Optimize(OptCfg(false));
Console.WriteLine($"(DW 탐색 {sw.Elapsed.TotalSeconds:N0}s)");
Compare($"{DAY} {TR}초월 — 기존(DW) 옵티마이저 빌드", oDw);

if (optIngame)
{
    sw.Restart();
    var oIng = new SiegeOptimizer().Optimize(OptCfg(true));
    Console.WriteLine($"\n(ING 탐색 {sw.Elapsed.TotalSeconds:N0}s)");
    Compare($"{DAY} {TR}초월 — 인게임(ING) 규칙 옵티마이저 빌드", oIng);
    Console.WriteLine($"\n★ 인게임 규칙 기준 최적: DW빌드 {Sim(oDw, true, out _).TotalScore:N0} vs ING빌드 {Sim(oIng, true, out _).TotalScore:N0}");
}

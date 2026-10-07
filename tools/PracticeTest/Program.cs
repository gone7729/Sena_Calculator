using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;

// 연습전투 테스트: sheet_to_json.py가 만든 team.json → 영웅별 스탯창(펫·진형·버프 제외) 계산.
//   인게임 스탯창과 1:1 비교해 스탯 도출을 먼저 검증한다. (시뮬 전투 비교는 팀 5명·펫·자리 입력 후 추가)
// 사용법: dotnet run --project tools/PracticeTest -- tools/PracticeTest/team.json [요일=월요일] [후열=나타] [펫성급=6] [sim]
//   sim 인자 시: 시트 기어 고정 + 보호진형으로 공성 시뮬(빔 로테) → results/practice/ 에 전투로그 저장.
Console.OutputEncoding = Encoding.UTF8;
var path = args.Length > 0 ? args[0] : "tools/PracticeTest/team.json";
string day = args.Length > 1 ? args[1] : "월요일";
var backRow = (args.Length > 2 ? args[2] : "나타").Split(',').ToList();
int petStar = args.Length > 3 ? int.Parse(args[3]) : 6;
bool runSim = args.Contains("sim");
var team = new List<BattleCharacter>();   // 시트 순서 = 진형번호 오름차순 = 동속공 행동 순서
using var doc = JsonDocument.Parse(File.ReadAllText(path));

// 시트 오타 정정 (사용자 확인: "포식자" 세트는 없음 → 복수자)
var setAlias = new Dictionary<string, string> { ["포식자"] = "복수자" };

foreach (var h in doc.RootElement.GetProperty("heroes").EnumerateArray())
{
    string name = h.GetProperty("name").GetString();
    var ch = CharacterDb.Characters.FirstOrDefault(c => c.Name == name);
    if (ch == null) { Console.WriteLine($"[{name}] DB에 없음"); continue; }
    int tr = h.GetProperty("transcend").GetInt32();
    bool enh = h.GetProperty("enhanced").GetProperty("skill1").GetBoolean();
    var pot = h.GetProperty("potential");

    // 장비 4부위
    var pieces = new List<Equipment>();
    var fixedNotes = new HashSet<string>();
    foreach (var g in h.GetProperty("gear").EnumerateArray())
    {
        string set = g.GetProperty("set").GetString();
        if (setAlias.TryGetValue(set, out var fixedSet)) { if (fixedNotes.Add(set)) Console.WriteLine($"  (세트 '{set}' → '{fixedSet}' 정정)"); set = fixedSet; }
        if (!EquipmentDb.SetEffects.ContainsKey(set)) Console.WriteLine($"  ⚠ 세트 '{set}' DB에 없음");
        var e = new Equipment { Name = g.GetProperty("name").GetString(), Slot = g.GetProperty("slot").GetString(),
                                SetName = set, MainStatName = g.GetProperty("main").GetString() };
        int i = 0;
        foreach (var s in g.GetProperty("subs").EnumerateArray())
        {
            e.SubSlots[i].StatName = s.GetProperty("stat").GetString();
            e.SubSlots[i].Tier = s.GetProperty("tier").GetInt32();
            i++;
        }
        pieces.Add(e);
    }

    // 장신구 (반지면 메인옵 자리를 반지 효과가 대신)
    var a = h.GetProperty("accessory");
    string Str(JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    var acc = new Accessory
    {
        Grade = a.GetProperty("grade").GetInt32(),
        MainOption = Str(a, "main"), SubOption = Str(a, "sub"), RingName = Str(a, "ring"),
    };

    // 전용장비 (없으면 null)
    var exEl = h.GetProperty("exclusive");
    ExclusiveWeapon exw = exEl.ValueKind == JsonValueKind.Null ? null : new ExclusiveWeapon
    {
        Name = "시트", OwnerCharacterId = ch.Id, Atk = exEl.GetProperty("atk").GetDouble(),
        Hp = exEl.GetProperty("hp").GetDouble(), Def = exEl.GetProperty("def").GetDouble(),
        IsMagic = ch.AttackType == AttackType.Magic,
        Tuning = exEl.GetProperty("tuning").EnumerateArray().Select(t => new TuningSlot
        {
            Option = Enum.Parse<TuningOption>(t.GetProperty("option").GetString()),
            Grade = Enum.Parse<ExclusiveWeaponGrade>(t.GetProperty("grade").GetString()),
        }).ToList(),
    };

    var loadout = new EquipmentLoadout { Weapon1 = pieces[0], Weapon2 = pieces[1], Armor1 = pieces[2], Armor2 = pieces[3], Accessory = acc };
    var input = new StatCalculationInput
    {
        Character = ch, TranscendLevel = tr, IsSkillEnhanced = enh,
        Equipments = loadout.GetEquipments(), EquipSets = loadout.GetActiveSets(),
        PotentialAtkLevel = pot.GetProperty("atk").GetInt32(), PotentialDefLevel = pot.GetProperty("def").GetInt32(),
        PotentialHpLevel = pot.GetProperty("hp").GetInt32(),
        Accessory = acc, ExclusiveWeapon = exw,
        // 스탯창 = 펫·진형·파티버프 없음
    };
    var r = new StatCalculator().Calculate(input);
    ch.ExclusiveWeapon = exw;   // 시뮬은 Character.ExclusiveWeapon을 읽는다 (없으면 null = 미장착)
    team.Add(new BattleCharacter
    {
        Character = ch, TranscendLevel = tr, IsSkillEnhanced = enh, Equipment = loadout,
        PotentialAtkLevel = input.PotentialAtkLevel, PotentialDefLevel = input.PotentialDefLevel, PotentialHpLevel = input.PotentialHpLevel,
        IsBackPosition = backRow.Contains(name),
    });
    var d = r.DisplayStats;
    var self = ch.Passive?.GetTotalSelfBuff(enh, tr) ?? new PermanentBuff();

    // 인게임 스탯창 = 패시브 자버프 + 장신구 옵션 줄(메인/부옵) 제외. 장신구 성급 보너스(공·방·생%)는 포함.
    //   실측: 나타 치확 63 = 전투 96 − 패시브 33 / 비스킷 치확 89 = 전투 99 − 장신구 부옵 10
    var accAll = acc.GetTotalStats();
    var accGrade = new Accessory { Grade = acc.Grade }.GetTotalStats();
    var accOpt = new BaseStatSet { Cri = accAll.Cri - accGrade.Cri, Cri_Dmg = accAll.Cri_Dmg - accGrade.Cri_Dmg,
                                   Wek = accAll.Wek - accGrade.Wek, Blk = accAll.Blk - accGrade.Blk };
    self.Cri += accOpt.Cri; self.Cri_Dmg += accOpt.Cri_Dmg; self.Wek += accOpt.Wek;
    string slot = h.TryGetProperty("slot", out var sl) ? sl.ToString() : "?";
    string P(double total, double pas) => pas > 0 ? $"   (전투값 {total:0.#} = + 패시브·장신구옵션 {pas:0.#})" : "";
    double selfAtk = ch.AttackType == AttackType.Magic ? self.MagicAtk_Rate : self.Atk_Rate;
    Console.WriteLine($"\n===== [진형번호{slot}] {name} ({ch.Type}) 초월{tr} · 세트 {string.Join("+", loadout.GetActiveSets().Select(s => $"{s.SetName}{s.PieceCount}"))}"
        + $" · 장신구 {acc.Grade}성 {acc.RingName ?? acc.MainOption}/{acc.SubOption ?? "-"} =====");
    Console.WriteLine($"  공격력   {r.FinalAtk / (1 + selfAtk / 100.0),8:N0}" + (selfAtk > 0 ? $"   (패시브 공% {selfAtk} 적용 시 {r.FinalAtk:N0})" : ""));
    Console.WriteLine($"  방어력   {r.FinalDef,8:N0}");
    Console.WriteLine($"  생명력   {r.FinalHp,8:N0}");
    Console.WriteLine($"  속공     {r.FinalSpd,8:N0}");
    Console.WriteLine($"  치명확률 {d.Cri - self.Cri,7:0.#}%" + P(d.Cri, self.Cri));
    Console.WriteLine($"  치명피해 {d.Cri_Dmg - self.Cri_Dmg,7:0.#}%" + P(d.Cri_Dmg, self.Cri_Dmg));
    Console.WriteLine($"  약점확률 {d.Wek - self.Wek,7:0.#}%" + P(d.Wek, self.Wek));
    Console.WriteLine($"  약점피해 {d.Wek_Dmg - self.Wek_Dmg,7:0.#}%" + P(d.Wek_Dmg, self.Wek_Dmg));
    Console.WriteLine($"  막기확률 {d.Blk - accOpt.Blk,7:0.#}%" + P(d.Blk, accOpt.Blk));
    double amp = (exw?.Tuning ?? new List<TuningSlot>()).Where(t => t.Option == TuningOption.피해증폭).Sum(t => ExclusiveWeapon.GetTuningValue(t.Option, t.Grade));
    Console.WriteLine($"  피해증폭 {amp,7:0.#}%");
    Console.WriteLine($"  [스탯창 외] 피증 {d.Dmg_Dealt:0.#}%(피해증폭 합산 모델) · 보스피증 {d.Dmg_Dealt_Bos:0.#}% · 1-3인기 {d.Dmg_Dealt_1to3:0.#}%");
}

if (!runSim) return;

// ===================== 공성 시뮬 (시트 기어 고정) =====================
var petEl = doc.RootElement.GetProperty("pet");
var pet = PetDb.GetByName(petEl.GetProperty("name").GetString());
int petEnh = petEl.GetProperty("enhance").GetInt32();
double petAtk = petEl.GetProperty("potentialAtkRate").GetDouble();
const string FORMATION = "보호 진형";
var stage = EnemyDb.SiegeStages[day];
Console.WriteLine($"\n\n########## {day} 시뮬 · {FORMATION} · 후열 {string.Join(",", backRow)} · 펫 {pet?.Name} {petStar}성 강화{petEnh} 잠재공{petAtk}% ##########");
Console.WriteLine("파티(동속공 행동순): " + string.Join(" / ", team.Select((b, i) => $"{i + 1}:{b.Character.Name}{(b.IsBackPosition ? "(후)" : "")}")));

SiegeBattleConfig Cfg(List<RotationDecision> plan) => new()
{
    AllyParty = team, FormationName = FORMATION, SiegeStage = stage,
    AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk, MaxTurns = 70,
    RotationPlan = plan, AllyDeathPenalty = 1_000_000, RecordFeasibility = plan != null,
};

var opt = new SiegeOptimizer().Optimize(new SiegeOptimizerConfig
{
    FixedMembers = team, Candidates = new List<BattleCharacter>(), SiegeStage = stage, MaxTurns = 70,
    AutoEquip = false, AllyPet = pet, PetStar = petStar, PetEnhance = petEnh, PetOptionAtkRate = petAtk,
    ForcedFormation = FORMATION, ForcedBackRow = backRow, DummyBackRow = false,
    OptimizeRotation = true, RotationBeamWidth = 10, RotationMaxDepth = 36,
    AllyDeathPenalty = 1_000_000, EnableSurvivalRings = false, SearchExclusiveWeapon = false,
});
// 옵티마이저가 자리 플래그를 덮어쓸 수 있으니 재확정
foreach (var b in team) b.IsBackPosition = backRow.Contains(b.Character.Name);

void Report(string label, SiegeBattleResult r)
{
    Console.WriteLine($"\n[{label}] 총점 {r.TotalScore:N0} · {r.RoundsCleared}R 클리어 · 경과 {r.ElapsedSeconds:N0}초 · 사망 {r.Deaths.Count}"
        + (r.Deaths.Count > 0 ? " (" + string.Join(", ", r.Deaths.Select(x => $"T{x.Turn} {x.AllyName}←{x.Cause}")) + ")" : ""));
    Console.WriteLine("  라운드: " + string.Join(" / ", r.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key} {k.Value:N0}")));
    Console.WriteLine("  영웅별: " + string.Join(" / ", r.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName} {c.TotalDamage:N0}")));
}

var auto = new SiegeBattleSimulator(777).Simulate(Cfg(null));
Report("자동 로테(궁→2→1 우선)", auto);

var plan = opt.BestRotationPlan.Select(x => new RotationDecision { HeroIndex = x.HeroIndex, Skill = x.Skill, Hold = x.Hold }).ToList();
var best = new SiegeBattleSimulator(777).Simulate(Cfg(plan));
Report("빔 로테", best);
Console.WriteLine("  스킬 순서: " + string.Join(" → ", best.Feasibility.Where(f => f.Reached && !f.Hold && (f.ExecutedAsPlanned || f.IsAuto))
    .Select(f => $"{f.HeroName} {f.SkillName}")));

// 아군 피격 요약 (광폭화 검증용): 적 스킬별·턴별 아군이 받은 피해
Console.WriteLine("\n  -- 아군 피격 (광폭화 검증용: 같은 적 스킬의 턴별 피해 비교) --");
foreach (var l in best.TurnLogs.Where(l => !l.IsAlly && l.DamageDealt > 0 && (l.Description ?? "").Contains("→")))
    Console.WriteLine($"    T{l.Turn,2} {l.ActorName}/{l.SkillName}: {l.Description}");

var dir = Path.Combine("results", "practice");
Directory.CreateDirectory(dir);
var logPath = Path.Combine(dir, $"battlelog_{day}_연습전투_시뮬.txt");
var sb = new StringBuilder();
sb.AppendLine($"=== {day} 연습전투 시뮬 (시트 기어 고정 · {FORMATION} · 후열 {string.Join(",", backRow)} · 총점 {best.TotalScore:N0}) ===");
foreach (var l in best.TurnLogs)
    sb.AppendLine($"T{l.Turn,2} [{(l.IsAlly ? "아" : "적")}] {l.ActorName,-6} {l.SkillName,-10} {(l.DamageDealt > 0 ? $"{l.DamageDealt,12:N0}" : "".PadLeft(12))}  {l.Description}");
File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"\n  [전투로그] {logPath}");

using System;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;

// 영상 실측(2026-05-26, T12) 타카 죽음의 무도(2스킬,강화,초월12) 검증.
// 목적: FinalAtk가 진형/펫/펫잠재/초월12/공격력%/장비를 전부 거친 실제 파이프라인 값인지 확인 + 레이어 분해.

// 1) 옵티마이저로 팀·기어 세팅(시뮬 모드와 동일) → 타카 기어 확보
int[] teamIds = { 1, 2, 201, 301, 51 };   // 타카, 라이언, 비스킷, 레이첼, 풍연
var team = teamIds.Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 12 }).ToList();
var optCfg = new SiegeOptimizerConfig
{
    Candidates = team, SiegeStage = EnemyDb.SiegeStages["토요일"], MaxTurns = 70,
    AutoEquip = true, AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72,
};
// 토요일 A/B: FloorFirstGear OFF vs ON — 총점·타카 기어 비교
double TakaCriDmg(SiegeOptimizerResult r)
{
    var t = r.BestParty.First(c => c.Character.Id == 1);
    var sc = new StatCalculator();
    var lo = t.Equipment; var bigSet = lo?.GetActiveSets().OrderByDescending(s => s.PieceCount).FirstOrDefault();
    var si = new StatCalculationInput {
        Character = t.Character, TranscendLevel = 12, IsSkillEnhanced = true,
        Equipments = lo?.GetEquipments(), Accessory = lo?.Accessory,
        EquipSetName = bigSet?.SetName ?? "", EquipSetCount = bigSet?.PieceCount ?? 0,
        Formation = new Formation { Name = "밸런스 진형", IsBackPosition = r.BestBackRow.Contains("타카") },
        Pet = PetDb.GetByName("윈디"), PetStar = 6, PetOptionAtkRate = 72, PartyPetBuffs = new BuffSet { Atk_Rate = 15 },
    };
    return sc.Calculate(si).DisplayStats?.Cri_Dmg ?? 0;
}
optCfg.FloorFirstGear = false;
var off = new SiegeOptimizer().Optimize(optCfg);
Console.WriteLine($"[토요일] 총점 {off.BestScore:N0} (실측 ~9M)  타카 치피 {TakaCriDmg(off)}");

// ===== C 측정: 레이첼 무기메인 공%(현재) vs 치피 → 타카가 top-1(평타쿨감)되는지 + 팀 총점 =====
SiegeBattleConfig SimCfg() => new() {
    AllyParty = off.BestParty, FormationName = off.BestFormation,
    SiegeStage = EnemyDb.SiegeStages["토요일"], AllyPet = PetDb.GetByName("윈디"),
    PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 72, MaxTurns = 70,
};
string CdrTarget(SiegeBattleResult r) => r.TurnLogs.FirstOrDefault(l => (l.Description ?? "").Contains("평타 쿨감"))?.Description ?? "(없음)";
var rachel = off.BestParty.First(c => c.Character.Id == 301);
string rwMain0 = string.Join("/", rachel.Equipment.GetEquipments().Where(e => e.Slot == "무기").Select(e => e.MainStatName));
var baseR = new SiegeBattleSimulator(777).Simulate(SimCfg());
foreach (var eq in rachel.Equipment.GetEquipments().Where(e => e.Slot == "무기")) eq.MainStatName = "치명타피해%";
var newR = new SiegeBattleSimulator(777).Simulate(SimCfg());
Console.WriteLine($"[C] 레이첼 무기메인 {rwMain0}(현재): auto총점 {baseR.TotalScore:N0}, 평타쿨감→[{CdrTarget(baseR)}]");
Console.WriteLine($"[C] 레이첼 무기메인 치피: auto총점 {newR.TotalScore:N0}, 평타쿨감→[{CdrTarget(newR)}]");
var rr = off.BestResult;
Console.WriteLine($"도달턴 {rr.TotalTurns}, 클리어라운드 {rr.RoundsCleared}, 총 경과게임시간 {rr.ElapsedSeconds:N0}초 ({rr.ElapsedSeconds / Math.Max(1, rr.TotalTurns):F1}초/턴)");
int deathCount = rr.TurnLogs.Count(l => l.IsAlly && l.SkillName == "죽음의 무도" && (l.Description ?? "").Contains("스파이크"));
Console.WriteLine($"죽음의무도 보스 타격 {deathCount}회 / 70턴 (실측 자동AI 5회). 쿨70초 → 게임시간 {rr.ElapsedSeconds:N0}초면 순수쿨로 {rr.ElapsedSeconds / 70:F1}회 + 적스킬-5초·평타쿨감");
Console.WriteLine("라운드별: " + string.Join(", ", rr.RoundScore.OrderBy(k => k.Key).Select(k => $"R{k.Key}={k.Value:N0}")));
Console.WriteLine("캐릭별 딜: " + string.Join(", ", rr.CharacterResults.OrderByDescending(c => c.TotalDamage).Select(c => $"{c.CharacterName}={c.TotalDamage:N0}")));
Console.WriteLine("아군 스킬 시전 횟수:");
foreach (var g in rr.TurnLogs.Where(l => l.IsAlly).GroupBy(l => l.SkillName).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key}: {g.Count()}회");
Console.WriteLine($"적 행동 로그 총 {rr.TurnLogs.Count(l => !l.IsAlly)}건 (스킬 사용 시 아군 쿨 -5초):");
foreach (var g in rr.TurnLogs.Where(l => !l.IsAlly).GroupBy(l => l.SkillName).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key}: {g.Count()}회");
int cdrLogs = rr.TurnLogs.Count(l => (l.Description ?? "").Contains("평타 쿨감"));
int cdrToTaka = rr.TurnLogs.Count(l => (l.Description ?? "").Contains("평타 쿨감") && (l.Description ?? "").Contains("타카"));
Console.WriteLine($"평타 쿨감 이벤트 총 {cdrLogs}건, 그중 타카 포함 {cdrToTaka}건 (각 -9초 → 타카 죽음의무도 쿨 펌핑)");
Console.WriteLine("버프 타게팅 로그 (장비강화/평타쿨감 — 누가 받나):");
foreach (var l in rr.TurnLogs.Where(l => (l.Description ?? "").Contains("장비 강화") || (l.Description ?? "").Contains("평타 쿨감") || ((l.SkillName ?? "").Contains("장비 강화"))).Take(12))
    Console.WriteLine($"  T{l.Turn} {l.SkillName}: {l.Description}");
Console.WriteLine("R3 일부 턴로그 (T34~T40):");
foreach (var l in rr.TurnLogs.Where(l => l.Turn >= 34 && l.Turn <= 40))
    Console.WriteLine($"  T{l.Turn} [{(l.IsAlly ? "아" : "적")}] {l.SkillName}: {l.Description}");
Console.WriteLine("상위 데미지 턴로그 10:");
foreach (var l in rr.TurnLogs.Where(l => l.IsAlly).OrderByDescending(l => ParseDmg(l.Description)).Take(10))
    Console.WriteLine($"  T{l.Turn} {l.SkillName}: {l.Description}");
static double ParseDmg(string d) { var m = System.Text.RegularExpressions.Regex.Match(d ?? "", @"([\d,]{4,})"); return m.Success ? double.Parse(m.Value.Replace(",", "")) : 0; }

var optResult = off;
var taka = optResult.BestParty.First(c => c.Character.Id == 1);
bool takaBack = optResult.BestBackRow.Contains("타카");

// 2) 타카 FinalAtk를 실제 StatCalculator로 계산 + 레이어 로그
var statCalc = new StatCalculator();
var loadout = taka.Equipment;
var sets = loadout?.GetActiveSets() ?? new System.Collections.Generic.List<EquipmentSet>();
var bigSet = sets.OrderByDescending(s => s.PieceCount).FirstOrDefault();
var statInput = new StatCalculationInput
{
    Character = taka.Character, TranscendLevel = 12, IsSkillEnhanced = true,
    Equipments = loadout?.GetEquipments(), Accessory = loadout?.Accessory,
    EquipSetName = bigSet?.SetName ?? "", EquipSetCount = bigSet?.PieceCount ?? 0,
    Formation = new Formation { Name = "밸런스 진형", IsBackPosition = takaBack },
    Pet = PetDb.GetByName("윈디"), PetStar = 6, PetOptionAtkRate = 72,
    PartyPetBuffs = new BuffSet { Atk_Rate = 15 },   // 윈디 6성 강화3 공격력버프 (펫버프 레이어)
};
var sr = statCalc.Calculate(statInput);

Console.WriteLine("================ 타카 FinalAtk 분해 (실제 StatCalculator) ================");
Console.WriteLine($"기초공(전설 공격형 Lv30+5) = {taka.Character.GetBaseStats().Atk}");
Console.WriteLine($"BaseAtk(장비/세트/초월/장신구 atk% 포함, 진형·펫·버프 前) = {sr.BaseAtk:N0}");
Console.WriteLine($"FinalAtk(진형+펫잠재+펫버프 모두 적용) = {sr.FinalAtk:N0}");
Console.WriteLine($"치피 = {sr.DisplayStats?.Cri_Dmg}, 후열={takaBack}, 세트={bigSet?.SetName} {bigSet?.PieceCount}\n");
Console.WriteLine("---- StatCalculator DebugLog (레이어별) ----");
Console.WriteLine(sr.DebugLog.ToString());

// 3) 그 FinalAtk로 죽음의 무도 데미지
var skill = taka.Character.Skills.First(s => s.Name == "죽음의 무도");
var dmg = new DamageCalculator();
const double BOSS_DEF = 1323;
// 실제 DB값: 피증=복수자45 / 라이언3인기31 / 보스피증(복수자40+토벌10+비스킷40=90)
//           취약·받피증=윈디보스30 + EagleClaw(받물12+취약16) + 레이첼28
//           방깎=비스킷24+레이첼36=60, 방무40=스킬초월6(자동), 약확=비스킷54+레이첼27→약점
double Run(double atk, bool isBoss, bool cond, bool weak) => dmg.Calculate(new DamageCalculator.DamageInput
{
    Character = taka.Character, Skill = skill, IsSkillEnhanced = true, TranscendLevel = 12,
    FinalAtk = atk, CritDamage = sr.DisplayStats?.Cri_Dmg ?? 294, WeakpointDmg = 130,
    DmgDealt = 45, Dmg1to3 = 31,
    DmgDealtBoss = isBoss ? 90 : 0,
    BossVulnerability = isBoss ? 30 : 0,
    DmgTakenIncrease = 12 + 28,   // EagleClaw 받물12 + 레이첼 28
    Vulnerability = 16,           // EagleClaw 초월 물리취약(4스택×4)
    DefReduction = 60,            // 비스킷24 + 레이첼36
    BossDef = BOSS_DEF, IsCritical = true, IsWeakpoint = weak, IsSkillConditionMet = cond,
    Mode = BattleMode.Boss, IsTargetBoss = isBoss,
    TargetHp = 1_000_000, TargetCurrentHp = cond ? 100_000 : 800_000, SelfMaxHp = 8124,
}).FinalDamage;

Console.WriteLine("\n================ 죽음의 무도 1타 (위 FinalAtk 사용) ================");
Console.WriteLine($"실측: 스파이크 285,884 / 294,630 (조건O) | 룩 113,024 (조건X)");
Console.WriteLine($"시뮬 FinalAtk {sr.FinalAtk:N0}:");
Console.WriteLine($"  스파이크(조건O,약점O) = {Run(sr.FinalAtk, true, true, true):N0}");
Console.WriteLine($"  룩(조건X,약점O)       = {Run(sr.FinalAtk, false, false, true):N0}");
Console.WriteLine($"  룩(조건X,약점X)       = {Run(sr.FinalAtk, false, false, false):N0}");

// ===== 라이언 광풍참 1타 (실측 T20: 440554/454031, 체력비례 최대, EagleClaw 4스택) =====
var ryan = optResult.BestParty.First(c => c.Character.Id == 2);
var rlo = ryan.Equipment; var rSet = rlo?.GetActiveSets().OrderByDescending(s => s.PieceCount).FirstOrDefault();
var rsr = statCalc.Calculate(new StatCalculationInput {
    Character = ryan.Character, TranscendLevel = 12, IsSkillEnhanced = true,
    Equipments = rlo?.GetEquipments(), Accessory = rlo?.Accessory,
    EquipSetName = rSet?.SetName ?? "", EquipSetCount = rSet?.PieceCount ?? 0,
    Formation = new Formation { Name = "밸런스 진형", IsBackPosition = optResult.BestBackRow.Contains("라이언") },
    Pet = PetDb.GetByName("윈디"), PetStar = 6, PetOptionAtkRate = 72, PartyPetBuffs = new BuffSet { Atk_Rate = 15 },
});
var gp = ryan.Character.Skills.First(s => s.Name == "광풍참");
double Gp(double remPct) => dmg.Calculate(new DamageCalculator.DamageInput {
    Character = ryan.Character, Skill = gp, IsSkillEnhanced = true, TranscendLevel = 12,
    FinalAtk = rsr.FinalAtk, CritDamage = rsr.DisplayStats?.Cri_Dmg ?? 294, WeakpointDmg = 130,
    DmgDealt = 45, Dmg1to3 = 31, DmgDealtBoss = 90,
    BossVulnerability = 30, DmgTakenIncrease = 12 + 28, Vulnerability = 16, DefReduction = 60,
    BossDef = BOSS_DEF, IsCritical = true, IsWeakpoint = true, IsSkillConditionMet = true,
    IsLostHpConditionMet = true, LostHpActualRemainingPct = remPct,
    Mode = BattleMode.Boss, IsTargetBoss = true, TargetHp = 1_000_000, TargetCurrentHp = remPct * 10_000, SelfMaxHp = 7944,
}).FinalDamage;
Console.WriteLine($"\n================ 라이언 광풍참 1타 (실측 440554/454031, 체력비례 최대) ================");
Console.WriteLine($"라이언 FinalAtk {rsr.FinalAtk:N0}, 치피 {rsr.DisplayStats?.Cri_Dmg}");
Console.WriteLine($"  광풍참 (잃은HP 100%, 4스택) = {Gp(0):N0}  | 잃은HP 0%(풀피) = {Gp(100):N0}");

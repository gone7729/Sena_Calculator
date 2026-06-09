using System;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;

// 전용장비 조율 비교: 나타 혼천릉파 1타 — 모든공격력×4(+48% 공%) vs 피해증폭×4(+16% 피증).
// 조건: 확정 치명 + 약점 + 비스킷 버프(보스피증40·약확54). 기어 고정(전용 없이 최적화), 전용만 교체.

var nataChar = CharacterDb.Characters.First(c => c.Id == 118);
nataChar.ExclusiveWeapon = null;

// 1) 화요일 기어 최적화(전용 탐색 OFF → 전용 무관 기어 확보)
var team = new[] { 118, 103, 202, 201, 255 }
    .Select(id => CharacterDb.Characters.First(c => c.Id == id))
    .Select(c => { c.ExclusiveWeapon = null; return new BattleCharacter { Character = c, IsSkillEnhanced = true, TranscendLevel = 12, PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3 }; })
    .ToList();
var opt = new SiegeOptimizer().Optimize(new SiegeOptimizerConfig
{
    Candidates = team, SiegeStage = EnemyDb.SiegeStages["화요일"], MaxTurns = 70,
    AutoEquip = true, SearchExclusiveWeapon = false, OptimizeRotation = false,
    AllyPet = PetDb.GetByName("윈디"), PetStar = 6, PetEnhance = 3, PetOptionAtkRate = 76,
});
var nata = opt.BestParty.First(c => c.Character.Id == 118);
var loadout = nata.Equipment;
var bigSet = loadout?.GetActiveSets().OrderByDescending(s => s.PieceCount).FirstOrDefault();
bool nataBack = opt.BestBackRow.Contains("나타");
var hon = nataChar.Skills.First(s => s.Name == "혼천릉파");
var enemy = EnemyDb.Bosses.First(b => b.Name == "아일린");
var calc = new DamageCalculator();
var sc = new StatCalculator();

double Compute(TuningOption opt4, string label)
{
    nataChar.ExclusiveWeapon = new ExclusiveWeapon
    {
        Name = "나타 전용", OwnerCharacterId = 118, Atk = 247, IsMagic = true,
        Tuning = Enumerable.Range(0, 4).Select(_ => new TuningSlot { Option = opt4, Grade = ExclusiveWeaponGrade.전설 }).ToList(),
    };
    var ds = sc.Calculate(new StatCalculationInput
    {
        Character = nataChar, TranscendLevel = 12, IsSkillEnhanced = true,
        Equipments = loadout?.GetEquipments(), Accessory = loadout?.Accessory,
        EquipSetName = bigSet?.SetName ?? "", EquipSetCount = bigSet?.PieceCount ?? 0,
        PotentialAtkLevel = 3, PotentialDefLevel = 3, PotentialHpLevel = 3,
        ExclusiveWeapon = nataChar.ExclusiveWeapon,
        Formation = new Formation { Name = opt.BestFormation, IsBackPosition = nataBack },
        Pet = PetDb.GetByName("윈디"), PetStar = 6, PetOptionAtkRate = 76,
        PartyPetBuffs = new BuffSet { Atk_Rate = 15 },
    });
    var st = ds.DisplayStats;
    double finalAtk = ds.FinalAtk;
    // 비스킷 장비강화 버프: 보스피증 +40, 약확 +54 (약점 발동은 강제). 청소시간/따뜻한울림 등은 제외(조율 단독 비교).
    var input = new DamageCalculator.DamageInput
    {
        Character = nataChar, Skill = hon, IsSkillEnhanced = true, TranscendLevel = 12,
        FinalAtk = finalAtk,
        CritChance = 100, CritDamage = st.Cri_Dmg, WeakChance = 100, WeakpointDmg = st.Wek_Dmg,
        DmgDealt = st.Dmg_Dealt, DmgDealtType = st.Dmg_Dealt_Type, DmgDealtBoss = st.Dmg_Dealt_Bos + 40,
        Dmg1to3 = st.Dmg_Dealt_1to3, Dmg4to5 = st.Dmg_Dealt_4to5, ArmorPen = st.Arm_Pen,
        BossDef = enemy.Stats.Def, BossHp = enemy.Stats.Hp, TargetHp = enemy.Stats.Hp, TargetCurrentHp = 1,
        IsCritical = true, IsWeakpoint = true, IsSkillConditionMet = true,
        Mode = BattleMode.Boss, IsTargetBoss = true, SelfMaxHp = ds.FinalHp,
    };
    double dmg = calc.Calculate(input).FinalDamage;
    Console.WriteLine($"  {label,-14}: FinalAtk {finalAtk,8:N0} · 치피 {st.Cri_Dmg,3:F0} · 약피 {st.Wek_Dmg,3:F0} · 피증 {st.Dmg_Dealt,3:F0} · 타입피증 {st.Dmg_Dealt_Type,3:F0} → 혼천릉파 {dmg,12:N0}");
    return dmg;
}

Console.WriteLine($"\n═══ 나타 혼천릉파 1타 조율 비교 (확정치명·약점·비스킷버프 / 기어 {bigSet?.SetName}4 고정·{(nataBack?"후열":"전열")}) ═══");
double a = Compute(TuningOption.모든공격력, "모든공격력×4");
double b = Compute(TuningOption.피해증폭, "피해증폭×4");
Console.WriteLine($"\n  → 모든공격력 / 피해증폭 = {a/b*100:F1}%  ({(a>b?"모든공격력":"피해증폭")} 우세, 차이 {Math.Abs(a-b):N0} / {Math.Abs(a-b)/Math.Min(a,b)*100:F1}%)");

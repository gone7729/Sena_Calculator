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
var optResult = new SiegeOptimizer().Optimize(optCfg);
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

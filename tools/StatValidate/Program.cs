using System;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;

// 파스칼 12초월 — 사용자 제공 스탯 수치로 스탯창공/인게임공 도출 + 파스칼 base 확인.
var pascal = CharacterDb.Characters.First(c => c.Name == "파스칼");
var sc = new StatCalculator();

// (1) 순수 base(12초월, 장비/펫/버프 없음) — 기초공·치피·약피 base 확인
var bare = sc.Calculate(new StatCalculationInput { Character = pascal, TranscendLevel = 12, IsSkillEnhanced = true });
var gbs = pascal.GetBaseStats();
var t12 = pascal.GetTranscendStats(12);
double baseAtk = gbs.Atk + t12.Atk;   // 기초공 = 캐릭터 base + 12초월 flat
Console.WriteLine("===== 파스칼 12초월 순수 base =====");
Console.WriteLine($"기초공(baseAtk) = GetBaseStats {gbs.Atk} + t12flat {t12.Atk} = {baseAtk:N0}");
Console.WriteLine($"t12 Atk_Rate = {t12.Atk_Rate}%   t12 Cri_Dmg = {t12.Cri_Dmg}   t12 Wek_Dmg = {t12.Wek_Dmg}");
Console.WriteLine($"bare.BaseAtk(초월%·무기flat 포함) = {bare.BaseAtk:N0}   bare.FinalAtk = {bare.FinalAtk:N0}");
Console.WriteLine($"base 치명타피해 = {bare.DisplayStats.Cri_Dmg}%   base 약점피해 = {bare.DisplayStats.Wek_Dmg}%   base 약점확률 = {bare.DisplayStats.Wek}%   base 치명확률 = {bare.DisplayStats.Cri}%");

// (2) 사용자 제공 수치
double flatAtk = 604 + 370 + 150 + 564 + 247;  // 무기604 + 잠재370 + 부옵150 + 펫564 + 전용247
double totalAtkRate = 113 + t12.Atk_Rate;        // 스탯창%: 메인+부옵+장신구+전용 + 초월%
double formation = 42, petPot = 69;              // 진형(보호) 42, 펫잠재 모든공 69
double buffAtk = 33 + 31 + 15;                   // 지속 마공33 + 턴제 마공31 + 펫 15

double statScreenAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
double sepBonus = baseAtk * (formation + petPot) / 100.0;
double preBuff = statScreenAtk + sepBonus;
double finalAtk = preBuff * (1 + buffAtk / 100.0);

Console.WriteLine("\n===== 공격력 도출 (StatCalculator 공식) =====");
Console.WriteLine($"flatAtk = 604(무기)+370(잠재)+150(부옵)+564(펫)+247(전용) = {flatAtk:N0}");
Console.WriteLine($"스탯창 공격력 = 기초공{baseAtk} × (1+{totalAtkRate}%) + flat{flatAtk} = {statScreenAtk:N0}");
Console.WriteLine($"별도보너스 = 기초공{baseAtk} × (진형{formation}+펫잠재{petPot})% = {sepBonus:N0}");
Console.WriteLine($"버프전(preBuff) = 스탯창{statScreenAtk:N0} + 별도{sepBonus:N0} = {preBuff:N0}");
Console.WriteLine($"버프배수 = 1 + (지속33+턴제31+펫15)/100 = {1 + buffAtk / 100.0:F2}x");
Console.WriteLine($"★ 인게임 공격력(FinalAtk) = preBuff{preBuff:N0} × {1 + buffAtk / 100.0:F2} = {finalAtk:N0}");

// ===== 파괴의 거인 데미지 (크리스 대상) =====
var skill = pascal.Skills.First(s => s.Name.Contains("파괴"));
var dmg = new DamageCalculator();
const double CHRIS_DEF = 1625;   // 일요일 크리스 방어력(EnemyDB)

// 사용자 전투 스탯 (스탯창치피 324 + 상시40 + 턴제46 = 410 등)
double critDmg = 324 + 40 + 46;             // 치명타피해 총합
double wekDmg  = 130 + 23;                   // base130(=100+유저30) + 상시 약피증23
double defRed  = 24 + 39;                    // 방깎: 상시24 + 턴제39
double magVuln = 26;                          // 마법취약(받마피증) 26
double bossVuln= 30;                          // 펫 보스취약 30
double dmgBoss = 12 + 40;                     // 보스피증: 장신구12 + 턴제40
double dmgN    = 10;                          // n인기 피증(장신구)

double Run(double dmgDealtExtra, string label)
{
    var r = dmg.Calculate(new DamageCalculator.DamageInput
    {
        Character = pascal, Skill = skill, IsSkillEnhanced = true, TranscendLevel = 12,
        FinalAtk = finalAtk,
        ExpectedCritWeak = true,
        CritChance = 0, CritDamage = critDmg,        // 확정치명은 스킬 내장(Bonus Cri50 + 초월100)
        WeakChance = 55 + 54, WeakpointDmg = wekDmg, // 약확 55 + 비스킷54
        DmgDealt = dmgDealtExtra,                    // 피해증폭을 피증에 합칠 때만 사용
        Dmg1to3 = dmgN,
        DmgDealtBoss = dmgBoss,
        BossVulnerability = bossVuln,
        DmgTakenIncrease = magVuln,                  // 마법취약(파스칼=마법)
        DefReduction = defRed,
        BossDef = CHRIS_DEF,
        IsCritical = true, IsWeakpoint = true, IsTargetBoss = true,
        IsSkillConditionMet = true, IsLostHpConditionMet = false,
        Mode = BattleMode.Boss,
    });
    Console.WriteLine($"  {label}: {r.FinalDamage:N0}");
    return r.FinalDamage;
}

Console.WriteLine("\n===== 파괴의 거인 데미지 (FinalAtk 13,452 · 크리스) =====");
Console.WriteLine($"계수470 · 확정치명(치피{critDmg}) · 약점(약피{wekDmg}=1.53배) · 방깎{defRed}·방무65(스킬) · 보스피증{dmgBoss}·n인기{dmgN} · 마법취약{magVuln}·보스취약{bossVuln}");
double dA = Run(0, "① 피해증폭 미포함 (base)");
double dB = Run(8.8, "② 피해증폭 8.8%를 피증에 합산");
double dC = dA * 1.088;
Console.WriteLine($"  ③ 피해증폭 8.8%를 최종 별도 곱: {dC:N0}");
Console.WriteLine($"\n(로그 6초월값 930,862와는 스펙 다름 — 12초월 기준 도출값)");

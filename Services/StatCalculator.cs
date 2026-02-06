using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 스탯 계산 입력 데이터
    /// </summary>
    public class StatCalculationInput
    {
        // 캐릭터
        public Character Character { get; set; }
        public int TranscendLevel { get; set; }
        public bool IsSkillEnhanced { get; set; }
        public bool IsPassiveConditionMet { get; set; }

        // 장비
        public IEnumerable<Equipment> Equipments { get; set; }
        public string EquipSetName { get; set; }
        public int EquipSetCount { get; set; } = 4;

        // 잠재능력
        public int PotentialAtkLevel { get; set; }
        public int PotentialDefLevel { get; set; }
        public int PotentialHpLevel { get; set; }

        // 장신구
        public Accessory Accessory { get; set; }

        // 진형
        public Formation Formation { get; set; }

        // 펫
        public Pet Pet { get; set; }
        public int PetStar { get; set; }
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }

        // 버프/디버프
        public BuffSet TotalBuffs { get; set; }
        public DebuffSet TotalDebuffs { get; set; }

        // 분리된 파티버프 (지속/턴제/펫)
        public BuffSet PartyPermanentBuffs { get; set; }
        public BuffSet PartyTimedBuffs { get; set; }
        public BuffSet PartyPetBuffs { get; set; }
    }

    /// <summary>
    /// 스탯 계산 결과
    /// </summary>
    public class StatCalculationResult
    {
        // 기본 스탯 (버프 전)
        public double BaseAtk { get; set; }
        public double BaseDef { get; set; }
        public double BaseHp { get; set; }

        // 최종 스탯 (버프 후)
        public double FinalAtk { get; set; }
        public double FinalDef { get; set; }
        public double FinalHp { get; set; }
        public double FinalSpd { get; set; }

        // 기타 스탯
        public BaseStatSet DisplayStats { get; set; } = new BaseStatSet();

        // 디버프 (UI 표시용)
        public DebuffSet CurrentDebuffs { get; set; }

        // 디버깅용 로그
        public StringBuilder DebugLog { get; set; } = new();
    }

    /// <summary>
    /// 스탯 계산 서비스
    /// </summary>
    public class StatCalculator
    {
        /// <summary>
        /// 전체 스탯 계산
        /// </summary>
        public StatCalculationResult Calculate(StatCalculationInput input)
        {
            var result = new StatCalculationResult();
            result.CurrentDebuffs = input.TotalDebuffs ?? new DebuffSet();

            // ========== 캐릭터 기본 스탯 ==========
            BaseStatSet characterStats = input.Character?.GetBaseStats() ?? new BaseStatSet();
            double baseAtk = characterStats.Atk;
            double baseDef = characterStats.Def;
            double baseHp = characterStats.Hp;

            // ========== 캐릭터 패시브 자버프 (지속/턴제 분리) ==========
            var (selfPermanentBuff, selfTimedBuff) = GetCharacterPassiveBuffSeparated(input);

            // ========== 파티버프 (지속/턴제/펫 분리) ==========
            BuffSet partyPermanentBuff = input.PartyPermanentBuffs ?? new BuffSet();
            BuffSet partyTimedBuff = input.PartyTimedBuffs ?? new BuffSet();
            BuffSet partyPetBuff = input.PartyPetBuffs ?? new BuffSet();

            // 호환성을 위한 통합 버프 (로그용)
            BuffSet characterPassiveBuff = new BuffSet();
            characterPassiveBuff.Add(selfPermanentBuff);
            characterPassiveBuff.Add(selfTimedBuff);

            // ========== 각종 스탯 소스 ==========
            var potentialStats = GetPotentialStats(input);
            var equipmentStats = GetEquipmentStats(input.Equipments);
            var accessoryStats = input.Accessory?.GetTotalStats() ?? new BaseStatSet();
            var transcendStats = input.Character?.GetTranscendStats(input.TranscendLevel) ?? new BaseStatSet();
            var setBonus = GetSetBonus(input.EquipSetName, input.EquipSetCount);
            var petBaseStats = input.Pet?.GetBaseStats(input.PetStar) ?? new BaseStatSet();

            // ========== 버프 ==========
            BuffSet totalBuffs = input.TotalBuffs ?? new BuffSet();

            // ========== 디버그 로그: 피증 출처별 상세 ==========
            result.DebugLog.AppendLine("══════════ 스탯 출처별 상세 ══════════");
            result.DebugLog.AppendLine($"캐릭터: {input.Character?.Name}, 초월: {input.TranscendLevel}, 스강: {input.IsSkillEnhanced}");
            result.DebugLog.AppendLine($"세트: {input.EquipSetName} {input.EquipSetCount}세트");
            
            result.DebugLog.AppendLine("\n[피증% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Dmg_Dealt}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Dmg_Dealt}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Dmg_Dealt}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Dmg_Dealt}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Dmg_Dealt}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Dmg_Dealt}%");
            double totalDmgDealt = characterStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt 
                + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt + characterPassiveBuff.Dmg_Dealt;
            result.DebugLog.AppendLine($"  ★ 합계: {totalDmgDealt}%");

            result.DebugLog.AppendLine("\n[보피증% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Dmg_Dealt_Bos}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Dmg_Dealt_Bos}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Dmg_Dealt_Bos}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Dmg_Dealt_Bos}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Dmg_Dealt_Bos}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Dmg_Dealt_Bos}%");
            double totalDmgDealtBos = characterStats.Dmg_Dealt_Bos + transcendStats.Dmg_Dealt_Bos 
                + setBonus.Dmg_Dealt_Bos + accessoryStats.Dmg_Dealt_Bos 
                + totalBuffs.Dmg_Dealt_Bos + characterPassiveBuff.Dmg_Dealt_Bos;
            result.DebugLog.AppendLine($"  ★ 합계: {totalDmgDealtBos}%");

            result.DebugLog.AppendLine("\n[타입피증% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Dmg_Dealt_Type}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Dmg_Dealt_Type}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Dmg_Dealt_Type}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Dmg_Dealt_Type}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Dmg_Dealt_Type}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Dmg_Dealt_Type}%");
            if (totalBuffs.Mark_Energeia > 0 || characterPassiveBuff.Mark_Energeia > 0)
                result.DebugLog.AppendLine($"  표식(에네르게이아): 버프{totalBuffs.Mark_Energeia}% + 패시브{characterPassiveBuff.Mark_Energeia}%");
            if (totalBuffs.Mark_Purify > 0 || characterPassiveBuff.Mark_Purify > 0)
                result.DebugLog.AppendLine($"  마력 정화: 버프{totalBuffs.Mark_Purify}% + 패시브{characterPassiveBuff.Mark_Purify}%");
            double totalDmgDealtType = characterStats.Dmg_Dealt_Type + transcendStats.Dmg_Dealt_Type
                + setBonus.Dmg_Dealt_Type + accessoryStats.Dmg_Dealt_Type
                + totalBuffs.Dmg_Dealt_Type + characterPassiveBuff.Dmg_Dealt_Type
                + totalBuffs.Mark_Energeia + characterPassiveBuff.Mark_Energeia
                + totalBuffs.Mark_Purify + characterPassiveBuff.Mark_Purify;
            result.DebugLog.AppendLine($"  ★ 합계: {totalDmgDealtType}%");

            result.DebugLog.AppendLine("\n[1-3인기% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Dmg_Dealt_1to3}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Dmg_Dealt_1to3}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Dmg_Dealt_1to3}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Dmg_Dealt_1to3}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Dmg_Dealt_1to3}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Dmg_Dealt_1to3}%");
            double totalDmg1to3 = characterStats.Dmg_Dealt_1to3 + transcendStats.Dmg_Dealt_1to3 
                + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3 
                + totalBuffs.Dmg_Dealt_1to3 + characterPassiveBuff.Dmg_Dealt_1to3;
            result.DebugLog.AppendLine($"  ★ 합계: {totalDmg1to3}%");

            result.DebugLog.AppendLine("\n[4-5인기% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Dmg_Dealt_4to5}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Dmg_Dealt_4to5}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Dmg_Dealt_4to5}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Dmg_Dealt_4to5}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Dmg_Dealt_4to5}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Dmg_Dealt_4to5}%");
            double totalDmg4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 
                + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 
                + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5;
            result.DebugLog.AppendLine($"  ★ 합계: {totalDmg4to5}%");

            result.DebugLog.AppendLine("\n[방무% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Arm_Pen}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Arm_Pen}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Arm_Pen}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Arm_Pen}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Arm_Pen}%");
            double totalArmPen = characterStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen 
                + totalBuffs.Arm_Pen + characterPassiveBuff.Arm_Pen;
            result.DebugLog.AppendLine($"  ★ 합계: {totalArmPen}%");

            result.DebugLog.AppendLine("\n[치피% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  장비 메인: {equipmentStats.MainStats.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  장비 부옵: {equipmentStats.SubStats.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  장신구: {accessoryStats.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Cri_Dmg}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Cri_Dmg}%");
            double totalCriDmg = characterStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg 
                + equipmentStats.SubStats.Cri_Dmg + equipmentStats.MainStats.Cri_Dmg 
                + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + characterPassiveBuff.Cri_Dmg;
            result.DebugLog.AppendLine($"  ★ 합계: {totalCriDmg}%");

            result.DebugLog.AppendLine("\n[약피% 출처]");
            result.DebugLog.AppendLine($"  캐릭터 기본: {characterStats.Wek_Dmg}%");
            result.DebugLog.AppendLine($"  초월: {transcendStats.Wek_Dmg}%");
            result.DebugLog.AppendLine($"  세트: {setBonus.Wek_Dmg}%");
            result.DebugLog.AppendLine($"  버프: {totalBuffs.Wek_Dmg}%");
            result.DebugLog.AppendLine($"  패시브 자버프: {characterPassiveBuff.Wek_Dmg}%");
            double totalWekDmg = characterStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg 
                + totalBuffs.Wek_Dmg + characterPassiveBuff.Wek_Dmg;
            result.DebugLog.AppendLine($"  ★ 합계: {totalWekDmg}%");

            // ========== 깡스탯 합계 ==========
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def * 2;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            double flatAtk = equipFlatAtk + potentialStats.Atk + equipmentStats.SubStats.Atk 
                           + petBaseStats.Atk + equipmentStats.MainStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + equipmentStats.SubStats.Def 
                           + petBaseStats.Def + equipmentStats.MainStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + equipmentStats.SubStats.Hp 
                          + petBaseStats.Hp + equipmentStats.MainStats.Hp;

            // ========== 속공 계산 ==========
            double totalSpd = characterStats.Spd + equipmentStats.SubStats.Spd;

            // ========== 합연산% (스탯창에 포함되는 것들) ==========
            double formationAtkRate = input.Formation?.GetAtkRate() ?? 0;
            double formationDefRate = input.Formation?.GetDefRate() ?? 0;

            // 스탯창 공격력%: 진형, 펫잠재 제외 (이것들은 기초공에만 적용)
            double totalAtkRate = transcendStats.Atk_Rate
                + setBonus.Atk_Rate + equipmentStats.SubStats.Atk_Rate
                + accessoryStats.Atk_Rate
                + equipmentStats.MainStats.Atk_Rate;

            double totalDefRate = transcendStats.Def_Rate
                + setBonus.Def_Rate + equipmentStats.SubStats.Def_Rate
                + accessoryStats.Def_Rate
                + equipmentStats.MainStats.Def_Rate;

            double totalHpRate = transcendStats.Hp_Rate
                + setBonus.Hp_Rate + equipmentStats.SubStats.Hp_Rate
                + accessoryStats.Hp_Rate + input.PetOptionHpRate
                + equipmentStats.MainStats.Hp_Rate;

            // ========== 버프% (지속끼리 Max, 턴제+펫 합연산) ==========
            result.DebugLog.AppendLine("\n[버프% 중복 처리 - 지속/턴제+펫 분리]");

            // 공격력%
            result.DebugLog.AppendLine($"  [공%] 파티지속: {partyPermanentBuff.Atk_Rate}%, 자버프지속: {selfPermanentBuff.Atk_Rate}%");
            result.DebugLog.AppendLine($"  [공%] 파티턴제: {partyTimedBuff.Atk_Rate}%, 자버프턴제: {selfTimedBuff.Atk_Rate}%");
            result.DebugLog.AppendLine($"  [공%] 펫스킬: {partyPetBuff.Atk_Rate}%");
            double permanentAtkRate = Math.Max(partyPermanentBuff.Atk_Rate, selfPermanentBuff.Atk_Rate)
                + partyPermanentBuff.FoolhardyBravery;
            double timedAtkRate = Math.Max(partyTimedBuff.Atk_Rate, selfTimedBuff.Atk_Rate);
            double petAtkRate = partyPetBuff.Atk_Rate;
            double totalBuffAtkRate = permanentAtkRate + timedAtkRate + petAtkRate;
            double atkMultiplier = 1 + totalBuffAtkRate / 100.0;
            result.DebugLog.AppendLine($"  ★ 지속: {permanentAtkRate}% + 턴제: {timedAtkRate}% + 펫: {petAtkRate}% = {totalBuffAtkRate}% → {atkMultiplier:F4}x");

            // 방어력%
            result.DebugLog.AppendLine($"  [방%] 파티지속: {partyPermanentBuff.Def_Rate}%, 자버프지속: {selfPermanentBuff.Def_Rate}%");
            result.DebugLog.AppendLine($"  [방%] 파티턴제: {partyTimedBuff.Def_Rate}%, 자버프턴제: {selfTimedBuff.Def_Rate}%");
            result.DebugLog.AppendLine($"  [방%] 펫스킬: {partyPetBuff.Def_Rate}%");
            double permanentDefRate = Math.Max(partyPermanentBuff.Def_Rate, selfPermanentBuff.Def_Rate);
            double timedDefRate = Math.Max(partyTimedBuff.Def_Rate, selfTimedBuff.Def_Rate);
            double petDefRate = partyPetBuff.Def_Rate;
            double totalBuffDefRate = permanentDefRate + timedDefRate + petDefRate;
            double defMultiplier = 1 + totalBuffDefRate / 100.0;
            result.DebugLog.AppendLine($"  ★ 지속: {permanentDefRate}% + 턴제: {timedDefRate}% + 펫: {petDefRate}% = {totalBuffDefRate}% → {defMultiplier:F4}x");

            // 체력%
            result.DebugLog.AppendLine($"  [체%] 파티지속: {partyPermanentBuff.Hp_Rate}%, 자버프지속: {selfPermanentBuff.Hp_Rate}%");
            result.DebugLog.AppendLine($"  [체%] 파티턴제: {partyTimedBuff.Hp_Rate}%, 자버프턴제: {selfTimedBuff.Hp_Rate}%");
            result.DebugLog.AppendLine($"  [체%] 펫스킬: {partyPetBuff.Hp_Rate}%");
            double permanentHpRate = Math.Max(partyPermanentBuff.Hp_Rate, selfPermanentBuff.Hp_Rate);
            double timedHpRate = Math.Max(partyTimedBuff.Hp_Rate, selfTimedBuff.Hp_Rate);
            double petHpRate = partyPetBuff.Hp_Rate;
            double totalBuffHpRate = permanentHpRate + timedHpRate + petHpRate;
            double hpMultiplier = 1 + totalBuffHpRate / 100.0;
            result.DebugLog.AppendLine($"  ★ 지속: {permanentHpRate}% + 턴제: {timedHpRate}% + 펫: {petHpRate}% = {totalBuffHpRate}% → {hpMultiplier:F4}x");

            // ========== 기본 스탯 (스탯창 = 진형/펫잠재 제외) ==========
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp  = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // ========== 별도 보너스 (기초공에만 적용: 진형 + 펫잠재공%) ==========
            double separateBonusAtk = baseAtk * (formationAtkRate + input.PetOptionAtkRate) / 100.0;
            double separateBonusDef = baseDef * (formationDefRate + input.PetOptionDefRate) / 100.0;
            result.DebugLog.AppendLine($"\n[별도 보너스 (기초공에만 적용)]");
            result.DebugLog.AppendLine($"  진형공%: {formationAtkRate}%, 펫잠재공%: {input.PetOptionAtkRate}%");
            result.DebugLog.AppendLine($"  → 기초공{baseAtk} × ({formationAtkRate} + {input.PetOptionAtkRate})% = {separateBonusAtk:F0}");

            // ========== 스탯 비례 증가 ==========
            var scalingBonus = CalculateStatScaling(input, baseStatAtk + separateBonusAtk, baseStatDef + separateBonusDef, baseStatHp, totalSpd);
            baseStatAtk += scalingBonus.Atk;
            baseStatDef += scalingBonus.Def;
            baseStatHp += scalingBonus.Hp;

            // ========== 버프 전 최종 (스탯창 + 별도보너스) ==========
            double preBuffAtk = baseStatAtk + separateBonusAtk;
            double preBuffDef = baseStatDef + separateBonusDef;

            // ========== 최종 스탯 (버프 적용 후 - 곱연산) ==========
            // 공식: 최종공격력 = (스탯공격력 + 펫깡공 + 기초공 * (진형 + 펫잠재%)) * (1+지속%) * (1+턴제%) * (1+펫%)
            result.FinalAtk = preBuffAtk * atkMultiplier;
            result.FinalDef = preBuffDef * defMultiplier;
            result.FinalHp  = baseStatHp * hpMultiplier;
            result.FinalSpd = totalSpd;

            // ========== 순수 기본 스탯 ==========
            double flatAtkBase = equipFlatAtk + potentialStats.Atk + equipmentStats.SubStats.Atk + equipmentStats.MainStats.Atk;
            double flatDefBase = equipFlatDef + potentialStats.Def + equipmentStats.SubStats.Def + equipmentStats.MainStats.Def;
            double flatHpBase  = equipFlatHp  + potentialStats.Hp  + equipmentStats.SubStats.Hp  + equipmentStats.MainStats.Hp;
            result.BaseAtk = baseAtk * (1 + (equipmentStats.MainStats.Atk_Rate + equipmentStats.SubStats.Atk_Rate 
                + transcendStats.Atk_Rate + accessoryStats.Atk_Rate + setBonus.Atk_Rate) / 100) + flatAtkBase;
            result.BaseDef = baseDef * (1 + (equipmentStats.MainStats.Def_Rate + equipmentStats.SubStats.Def_Rate 
                + transcendStats.Def_Rate + accessoryStats.Def_Rate + setBonus.Def_Rate) / 100) + flatDefBase;
            result.BaseHp  = baseHp  * (1 + (equipmentStats.MainStats.Hp_Rate + equipmentStats.SubStats.Hp_Rate 
                + transcendStats.Hp_Rate  + accessoryStats.Hp_Rate  + setBonus.Hp_Rate) / 100)  + flatHpBase;

            // ========== 기타 스탯 ==========
            result.DisplayStats = CalculateDisplayStats(
                characterStats, transcendStats, setBonus,
                equipmentStats, accessoryStats,
                totalBuffs, characterPassiveBuff,
                totalAtkRate, scalingBonus.Cri, scalingBonus.DmgRdc,
                permanentAtkRate, timedAtkRate, petAtkRate
            );

            // 디버그 출력 (파일)
            System.Diagnostics.Debug.WriteLine(result.DebugLog.ToString());
            try { System.IO.File.WriteAllText("stat_debug.txt", result.DebugLog.ToString(), Encoding.UTF8); } catch { }

            return result;
        }

        #region Private Helper Methods

        /// <summary>
        /// 캐릭터 패시브 자버프를 지속/턴제 분리하여 반환
        /// </summary>
        private (BuffSet Permanent, BuffSet Timed) GetCharacterPassiveBuffSeparated(StatCalculationInput input)
        {
            BuffSet permanent = new BuffSet();
            BuffSet timed = new BuffSet();

            if (input.Character?.Passive == null) return (permanent, timed);

            // 상시 자버프 (지속)
            var permanentBuff = input.Character.Passive.GetTotalSelfBuff(
                input.IsSkillEnhanced, input.TranscendLevel);
            if (permanentBuff != null) permanent.Add(permanentBuff);

            // 턴제 자버프 (조건 충족 시)
            if (input.IsPassiveConditionMet)
            {
                var timedBuff = input.Character.Passive.GetConditionalSelfBuff(
                    input.IsSkillEnhanced, input.TranscendLevel);
                if (timedBuff != null) timed.Add(timedBuff);
            }

            return (permanent, timed);
        }

        private BaseStatSet GetPotentialStats(StatCalculationInput input)
        {
            BaseStatSet stats = new BaseStatSet();

            string grade = input.Character?.Grade ?? "전설";
            var potentialStats = StatTable.PotentialDb.GetStats(grade);

            if (input.PotentialAtkLevel > 0)
                stats.Atk = potentialStats["공격력"][input.PotentialAtkLevel - 1];
            if (input.PotentialDefLevel > 0)
                stats.Def = potentialStats["방어력"][input.PotentialDefLevel - 1];
            if (input.PotentialHpLevel > 0)
                stats.Hp = potentialStats["생명력"][input.PotentialHpLevel - 1];

            return stats;
        }

        private (BaseStatSet MainStats, BaseStatSet SubStats) GetEquipmentStats(IEnumerable<Equipment> equipments)
        {
            BaseStatSet mainStats = new BaseStatSet();
            BaseStatSet subStats = new BaseStatSet();

            if (equipments == null) return (mainStats, subStats);

            foreach (var equip in equipments)
            {
                mainStats.Add(equip.GetMainStats());
                subStats.Add(equip.GetSubStats());
            }

            return (mainStats, subStats);
        }

        private BaseStatSet GetSetBonus(string setName, int setCount)
        {
            BaseStatSet total = new BaseStatSet();

            if (string.IsNullOrEmpty(setName) || setName == "없음")
                return total;

            if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
            {
                // 4세트면 4세트만, 2세트면 2세트만 적용 (중복 X)
                if (setCount >= 4 && setData.TryGetValue(4, out var bonus4))
                {
                    total.Add(bonus4);
                }
                else if (setCount >= 2 && setData.TryGetValue(2, out var bonus2))
                {
                    total.Add(bonus2);
                }
            }

            return total;
        }

        private (double Atk, double Def, double Hp, double Cri, double Blk, double DmgRdc) CalculateStatScaling(
            StatCalculationInput input, double baseAtk, double baseDef, double baseHp, double spd)
        {
            double scalingAtk = 0, scalingDef = 0, scalingHp = 0, scalingCri = 0, scalingBlk = 0, scalingDmgRdc = 0;

            if (input.Character?.Passive == null) return (0, 0, 0, 0, 0, 0);

            var passiveData = input.Character.Passive.GetLevelData(input.IsSkillEnhanced);
            if (passiveData?.StatScalings == null) return (0, 0, 0, 0, 0, 0);

            foreach (var scaling in passiveData.StatScalings)
            {
                double sourceValue = scaling.SourceStat switch
                {
                    StatType.Spd => spd,
                    StatType.Hp => baseHp,
                    StatType.Def => baseDef,
                    StatType.Atk => baseAtk,
                    StatType.Blk => scalingBlk,
                    StatType.Dmg_Rdc => scalingDmgRdc,
                    _ => 0
                };

                double bonus = CalcScalingBonus(sourceValue, scaling);

                switch (scaling.TargetStat)
                {
                    case StatType.Atk: scalingAtk += bonus; break;
                    case StatType.Def: scalingDef += bonus; break;
                    case StatType.Hp: scalingHp += bonus; break;
                    case StatType.Cri: scalingCri += bonus; break;
                    case StatType.Blk: scalingBlk += bonus; break;
                    case StatType.Dmg_Rdc: scalingDmgRdc += bonus; break;
                }
            }

            return (scalingAtk, scalingDef, scalingHp, scalingCri, scalingBlk, scalingDmgRdc);
        }

        private double CalcScalingBonus(double sourceValue, StatScaling scaling)
        {
            if (scaling == null || scaling.SourceUnit <= 0) return 0;
            double multiplier = Math.Floor(sourceValue / scaling.SourceUnit);
            double bonus = multiplier * scaling.PerUnit;
            return Math.Min(bonus, scaling.MaxValue);
        }

        private BaseStatSet CalculateDisplayStats(
            BaseStatSet characterStats, BaseStatSet transcendStats, BaseStatSet setBonus,
            (BaseStatSet MainStats, BaseStatSet SubStats) equipmentStats, BaseStatSet accessoryStats,
            BuffSet totalBuffs, BuffSet characterPassiveBuff,
            double totalAtkRate, double scalingCri, double scalingDmgRdc,
            double buffPermanentAtkRate, double buffTimedAtkRate, double buffPetAtkRate)
        {
            return new BaseStatSet
            {
                Cri = characterStats.Cri + transcendStats.Cri + setBonus.Cri
                    + equipmentStats.SubStats.Cri + equipmentStats.MainStats.Cri
                    + accessoryStats.Cri + characterPassiveBuff.Cri + totalBuffs.Cri + scalingCri,

                Cri_Dmg = characterStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg
                    + equipmentStats.SubStats.Cri_Dmg + equipmentStats.MainStats.Cri_Dmg
                    + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + characterPassiveBuff.Cri_Dmg,

                Wek = characterStats.Wek + transcendStats.Wek + setBonus.Wek
                    + equipmentStats.SubStats.Wek + equipmentStats.MainStats.Wek
                    + accessoryStats.Wek + totalBuffs.Wek + characterPassiveBuff.Wek,

                Wek_Dmg = characterStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg
                    + totalBuffs.Wek_Dmg + characterPassiveBuff.Wek_Dmg,

                Dmg_Dealt = characterStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt
                    + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt + characterPassiveBuff.Dmg_Dealt,

                Dmg_Dealt_Bos = characterStats.Dmg_Dealt_Bos + transcendStats.Dmg_Dealt_Bos
                    + setBonus.Dmg_Dealt_Bos + accessoryStats.Dmg_Dealt_Bos
                    + totalBuffs.Dmg_Dealt_Bos + characterPassiveBuff.Dmg_Dealt_Bos,

                Dmg_Dealt_Type = characterStats.Dmg_Dealt_Type + transcendStats.Dmg_Dealt_Type
                    + setBonus.Dmg_Dealt_Type + accessoryStats.Dmg_Dealt_Type
                    + totalBuffs.Dmg_Dealt_Type + characterPassiveBuff.Dmg_Dealt_Type
                    + totalBuffs.Mark_Energeia + characterPassiveBuff.Mark_Energeia
                    + totalBuffs.Mark_Purify + characterPassiveBuff.Mark_Purify,

                Arm_Pen = characterStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen
                    + totalBuffs.Arm_Pen + characterPassiveBuff.Arm_Pen,

                Blk = characterStats.Blk + transcendStats.Blk + setBonus.Blk
                    + equipmentStats.SubStats.Blk + equipmentStats.MainStats.Blk + accessoryStats.Blk,

                Eff_Hit = characterStats.Eff_Hit + transcendStats.Eff_Hit + setBonus.Eff_Hit
                    + equipmentStats.SubStats.Eff_Hit + equipmentStats.MainStats.Eff_Hit
                    + accessoryStats.Eff_Hit + totalBuffs.Eff_Hit + characterPassiveBuff.Eff_Hit,

                Eff_Res = characterStats.Eff_Res + transcendStats.Eff_Res + setBonus.Eff_Res
                    + equipmentStats.SubStats.Eff_Res + equipmentStats.MainStats.Eff_Res
                    + accessoryStats.Eff_Res + totalBuffs.Eff_Res + characterPassiveBuff.Eff_Res,

                Eff_Acc = characterStats.Eff_Acc + transcendStats.Eff_Acc + setBonus.Eff_Acc,

                Dmg_Rdc = characterStats.Dmg_Rdc + transcendStats.Dmg_Rdc + setBonus.Dmg_Rdc
                    + equipmentStats.MainStats.Dmg_Rdc + totalBuffs.Dmg_Rdc + characterPassiveBuff.Dmg_Rdc
                    + scalingDmgRdc,

                Dmg_Dealt_1to3 = characterStats.Dmg_Dealt_1to3 + transcendStats.Dmg_Dealt_1to3
                    + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3
                    + totalBuffs.Dmg_Dealt_1to3 + characterPassiveBuff.Dmg_Dealt_1to3,

                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5
                    + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5
                    + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5,

                // 버프%: Max 처리된 지속+턴제+펫 값 사용
                Atk_Rate = totalAtkRate + buffPermanentAtkRate + buffTimedAtkRate + buffPetAtkRate
            };

        }

        #endregion
    }
}

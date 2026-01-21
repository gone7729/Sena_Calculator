using System;
using System.Collections.Generic;
using System.Linq;
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

            // ========== 캐릭터 패시브 자버프 ==========
            BuffSet characterPassiveBuff = GetCharacterPassiveBuff(input);

            // ========== 각종 스탯 소스 ==========
            var potentialStats = GetPotentialStats(input);
            var equipmentStats = GetEquipmentStats(input.Equipments);
            var accessoryStats = input.Accessory?.GetTotalStats() ?? new BaseStatSet();
            var transcendStats = input.Character?.GetTranscendStats(input.TranscendLevel) ?? new BaseStatSet();
            var setBonus = GetSetBonus(input.EquipSetName, input.EquipSetCount);
            var petBaseStats = input.Pet?.GetBaseStats(input.PetStar) ?? new BaseStatSet();

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

            // ========== 합연산% ==========
            double formationAtkRate = input.Formation?.GetAtkRate() ?? 0;
            double formationDefRate = input.Formation?.GetDefRate() ?? 0;

            double totalAtkRate = transcendStats.Atk_Rate + formationAtkRate
    + setBonus.Atk_Rate + equipmentStats.SubStats.Atk_Rate
    + accessoryStats.Atk_Rate 
    + equipmentStats.MainStats.Atk_Rate;

            double totalDefRate = transcendStats.Def_Rate + formationDefRate
                + setBonus.Def_Rate + equipmentStats.SubStats.Def_Rate
                + accessoryStats.Def_Rate + input.PetOptionDefRate
                + equipmentStats.MainStats.Def_Rate;

            double totalHpRate = transcendStats.Hp_Rate
                + setBonus.Hp_Rate + equipmentStats.SubStats.Hp_Rate
                + accessoryStats.Hp_Rate + input.PetOptionHpRate
                + equipmentStats.MainStats.Hp_Rate;

            // ========== 버프% ==========
            BuffSet totalBuffs = input.TotalBuffs ?? new BuffSet();
            double buffAtkRate =  input.PetOptionAtkRate + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate =  totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate =  totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            // ========== 기본 스탯 (버프 적용 전) ==========
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // ========== 스탯 비례 증가 ==========
            var scalingBonus = CalculateStatScaling(input, baseStatAtk, baseStatDef, baseStatHp, totalSpd);
            baseStatAtk += scalingBonus.Atk;
            baseStatDef += scalingBonus.Def;
            baseStatHp += scalingBonus.Hp;

            // ========== 최종 스탯 (버프 적용 후) ==========
            result.FinalAtk = baseStatAtk * (1 + buffAtkRate / 100.0);
            result.FinalDef = baseStatDef * (1 + buffDefRate / 100.0);
            result.FinalHp = baseStatHp * (1 + buffHpRate / 100.0);
            result.FinalSpd = totalSpd;

            System.Diagnostics.Debug.WriteLine($"=== FinalAtk 계산 ===");
System.Diagnostics.Debug.WriteLine($"baseStatAtk: {baseStatAtk}");
System.Diagnostics.Debug.WriteLine($"buffAtkRate: {buffAtkRate}");
System.Diagnostics.Debug.WriteLine($"계산: {baseStatAtk} × {1 + buffAtkRate / 100.0} = {baseStatAtk * (1 + buffAtkRate / 100.0)}");
System.Diagnostics.Debug.WriteLine($"result.FinalAtk: {result.FinalAtk}");

            // ========== 순수 기본 스탯 ==========
            double flatAtkBase = equipFlatAtk + potentialStats.Atk + equipmentStats.SubStats.Atk + equipmentStats.MainStats.Atk;
            double flatDefBase = equipFlatDef + potentialStats.Def + equipmentStats.SubStats.Def + equipmentStats.MainStats.Def;
            double flatHpBase = equipFlatHp + potentialStats.Hp + equipmentStats.SubStats.Hp + equipmentStats.MainStats.Hp;
            result.BaseAtk = baseAtk * (1 + (equipmentStats.MainStats.Atk_Rate + equipmentStats.SubStats.Atk_Rate 
                + transcendStats.Atk_Rate + accessoryStats.Atk_Rate + setBonus.Atk_Rate) / 100) + flatAtkBase;
            result.BaseDef = baseDef * (1 + (equipmentStats.MainStats.Def_Rate + equipmentStats.SubStats.Def_Rate 
                + transcendStats.Def_Rate + accessoryStats.Def_Rate + setBonus.Def_Rate) / 100) + flatDefBase;
            result.BaseHp = baseHp * (1 + (equipmentStats.MainStats.Hp_Rate + equipmentStats.SubStats.Hp_Rate 
                + transcendStats.Hp_Rate + accessoryStats.Hp_Rate + setBonus.Hp_Rate) / 100) + flatHpBase;

            // ========== 기타 스탯 ==========
            result.DisplayStats = CalculateDisplayStats(
                characterStats, transcendStats, setBonus, 
                equipmentStats, accessoryStats, 
                totalBuffs, characterPassiveBuff,
                totalAtkRate, scalingBonus.Cri
            );

            // 라인 136 근처에 추가
System.Diagnostics.Debug.WriteLine($"=== TotalBuffs 상세 ===");
System.Diagnostics.Debug.WriteLine($"totalBuffs.Atk_Rate: {totalBuffs.Atk_Rate}");
System.Diagnostics.Debug.WriteLine($"totalBuffs.Dmg_Dealt: {totalBuffs.Dmg_Dealt}");
System.Diagnostics.Debug.WriteLine($"totalBuffs.Dmg_Dealt_Boss: {totalBuffs.Dmg_Dealt_Bos}");
System.Diagnostics.Debug.WriteLine($"characterPassiveBuff.Atk_Rate: {characterPassiveBuff.Atk_Rate}");
System.Diagnostics.Debug.WriteLine($"characterPassiveBuff.Atk_Rate: {characterPassiveBuff.Dmg_Dealt_Type}");


            return result;
        }

        #region Private Helper Methods

        private BuffSet GetCharacterPassiveBuff(StatCalculationInput input)
        {
            BuffSet buff = new BuffSet();
            if (input.Character?.Passive == null) return buff;

            // 상시 자버프
            var permanentBuff = input.Character.Passive.GetTotalSelfBuff(
                input.IsSkillEnhanced, input.TranscendLevel);
            if (permanentBuff != null) buff.Add(permanentBuff);

            // 턴제 자버프 (조건 충족 시)
            if (input.IsPassiveConditionMet)
            {
                var timedBuff = input.Character.Passive.GetConditionalSelfBuff(
                    input.IsSkillEnhanced, input.TranscendLevel);
                if (timedBuff != null) buff.Add(timedBuff);
            }

            return buff;
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
                // 2세트 효과
                if (setCount >= 2 && setData.TryGetValue(2, out var bonus2))
                    total.Add(bonus2);
                
                // 4세트 효과
                if (setCount >= 4 && setData.TryGetValue(4, out var bonus4))
                    total.Add(bonus4);
            }

            return total;
        }

        private (double Atk, double Def, double Hp, double Cri, double Blk) CalculateStatScaling(
            StatCalculationInput input, double baseAtk, double baseDef, double baseHp, double spd)
        {
            double scalingAtk = 0, scalingDef = 0, scalingHp = 0, scalingCri = 0, scalingBlk = 0;

            if (input.Character?.Passive == null) return (0, 0, 0, 0, 0);

            var passiveData = input.Character.Passive.GetLevelData(input.IsSkillEnhanced);
            if (passiveData?.StatScalings == null) return (0, 0, 0, 0, 0);

            foreach (var scaling in passiveData.StatScalings)
            {
                double sourceValue = scaling.SourceStat switch
                {
                    StatType.Spd => spd,
                    StatType.Hp => baseHp,
                    StatType.Def => baseDef,
                    StatType.Atk => baseAtk,
                    StatType.Blk => scalingBlk,
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
                }
            }

            return (scalingAtk, scalingDef, scalingHp, scalingCri, scalingBlk);
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
            double totalAtkRate, double scalingCri)
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
                    + totalBuffs.Dmg_Dealt_Type + characterPassiveBuff.Dmg_Dealt_Type,
                
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
                    + equipmentStats.MainStats.Dmg_Rdc + totalBuffs.Dmg_Rdc + characterPassiveBuff.Dmg_Rdc,
                    
                Dmg_Dealt_1to3 = characterStats.Dmg_Dealt_1to3 + transcendStats.Dmg_Dealt_1to3 
                    + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3 
                    + totalBuffs.Dmg_Dealt_1to3 + characterPassiveBuff.Dmg_Dealt_1to3,
                    
                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 
                    + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 
                    + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5,
                    
                Atk_Rate = totalAtkRate + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate
            };
            
        }

        #endregion
    }
}
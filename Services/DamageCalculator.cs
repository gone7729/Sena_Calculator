using System;
using System.Collections.Generic;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 데미지 계산 서비스
    /// </summary>
    public class DamageCalculator
    {
        /// <summary>
        /// 데미지 계산 입력값
        /// </summary>
        public class DamageInput
        {
            // 캐릭터 관련
            public Character Character { get; set; }
            public Skill Skill { get; set; }
            public bool IsSkillEnhanced { get; set; }
            public int TranscendLevel { get; set; }
            
            // 장비 관련
            public BaseStatSet EquipmentStats { get; set; } = new BaseStatSet();
            public BaseStatSet SetBonusStats { get; set; } = new BaseStatSet();
            
            // 펫/진형
            public double PetAtk { get; set; }
            public double FormationBonus { get; set; }  // 진형 공격력 보너스 (0.4 = 40%)
            
            // 버프/디버프
            public double AtkBuffPercent { get; set; }      // 공격력 증가 버프
            public double DefDebuffPercent { get; set; }    // 적 방어력 감소
            public double DmgTakenDebuff { get; set; }      // 적 받는 피해 증가
            
            // 타겟
            public Boss Target { get; set; }
            public bool IsTargetConditionMet { get; set; }  // 보스 조건 충족 여부
            public bool IsSkillConditionMet { get; set; }   // 스킬 조건 충족 여부 (체력 30% 미만 등)
            
            // 크리티컬
            public bool IsCritical { get; set; }
        }

        /// <summary>
        /// 데미지 계산 결과
        /// </summary>
        public class DamageResult
        {
            public double FinalAtk { get; set; }            // 최종 공격력
            public double FinalDef { get; set; }            // 적 최종 방어력
            public double DefCoefficient { get; set; }      // 방어 계수
            public double SkillRatio { get; set; }          // 스킬 배율
            public double CritMultiplier { get; set; }      // 치명타 배율
            public double DamageMultiplier { get; set; }    // 피해량 배율
            public double TotalArmorPen { get; set; }       // 총 방무
            public double FinalDamage { get; set; }         // 최종 데미지
            
            // 상세 정보
            public string Details { get; set; }
        }

        /// <summary>
        /// 데미지 계산 메인 함수
        /// </summary>
        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult();

            // 1. 캐릭터 기본 스탯 + 초월 스탯
            input.Character.TranscendLevel = input.TranscendLevel;
            var baseStats = input.Character.GetBaseStats();

            // 2. 장비 스탯 합산
            var totalStats = baseStats.Clone();
            totalStats.Add(input.EquipmentStats);
            totalStats.Add(input.SetBonusStats);

            // 3. 최종 공격력 계산 (정수% → /100.0)
            double baseAtk = baseStats.Atk;
            double statAtk = totalStats.Atk;
            double atkRate = totalStats.Atk_Rate + input.AtkBuffPercent;

            result.FinalAtk = (statAtk + input.PetAtk + baseAtk * input.FormationBonus / 100.0) * (1 + atkRate / 100.0);

            // 4. 방무 계산 (정수% → /100.0, 최대 100%)
            double skillArmorPen = input.Skill.GetArmorPen(input.IsSkillEnhanced);
            result.TotalArmorPen = Math.Min((totalStats.Arm_Pen + skillArmorPen) / 100.0, 1.0);

            // 5. 적 최종 방어력 계산
            double targetBaseDef = input.Target.GetEffectiveDefense(input.IsTargetConditionMet);
            double defReduction = input.DefDebuffPercent / 100.0;
            result.FinalDef = Math.Max(targetBaseDef * (1 - defReduction) * (1 - result.TotalArmorPen), 0);

            // 6. 방어 계수
            result.DefCoefficient = 1 + result.FinalDef * 0.00214;

            // 7. 스킬 배율
            result.SkillRatio = input.Skill.GetRatio(input.IsSkillEnhanced);

            // 조건부 추가 피해
            if (input.IsSkillConditionMet)
            {
                result.SkillRatio += input.Skill.GetConditionalDmg(input.IsSkillEnhanced);
            }

            // 8. 치명타 배율 (정수% → /100.0)
            if (input.IsCritical)
            {
                double baseCritDmg = totalStats.Cri_Dmg / 100.0;  // 150 → 1.5
                double bonusCritDmg = input.Skill.GetBonusCriDmg(input.IsSkillEnhanced);
                result.CritMultiplier = baseCritDmg + bonusCritDmg;
            }
            else
            {
                result.CritMultiplier = 1.0;
            }

            // 9. 피해량 배율 (정수% → /100.0)
            result.DamageMultiplier = 1 + totalStats.Dmg_Dealt / 100.0 + input.DmgTakenDebuff / 100.0;

            // 보스 대상 추가 피해
            if (input.Target.BossType != BossType.Other)
            {
                result.DamageMultiplier += totalStats.Dmg_Dealt_Bos / 100.0;
            }

            // 10. 피해 감쇄 적용
            double reduction = 0;
            double reductionMult = 1 - reduction / 100.0;

            // 11. 최종 데미지 계산
            result.FinalDamage = (result.FinalAtk * result.SkillRatio * result.CritMultiplier * result.DamageMultiplier * reductionMult) / result.DefCoefficient;

            result.Details = GenerateDetails(input, result);

            return result;
        }

        public double QuickCalculate(
            double finalAtk,
            double skillRatio,
            double critDmg,       // 정수 (150 = 150%)
            bool isCrit,
            double armorPen,      // 정수 (40 = 40%)
            double dmgDealt,      // 정수 (30 = 30%)
            double bossDmg,       // 정수 (40 = 40%)
            bool isBoss,
            double targetDef,
            double defReduction)  // 정수 (20 = 20%)
        {
            // 방어력 계산
            double effectiveDef = Math.Max(targetDef * (1 - defReduction / 100.0) * (1 - armorPen / 100.0), 0);
            double defCoef = 1 + effectiveDef * 0.00214;

            // 치명타 배율
            double critMult = isCrit ? critDmg / 100.0 : 1.0;

            // 피해량 배율
            double dmgMult = 1 + dmgDealt / 100.0 + (isBoss ? bossDmg / 100.0 : 0);

            // 최종 데미지
            return (finalAtk * skillRatio * critMult * dmgMult) / defCoef;
        }

        /// <summary>
        /// 상세 정보 문자열 생성
        /// </summary>
        private string GenerateDetails(DamageInput input, DamageResult result)
        {
            return $@"
═══════════════════════════════════════════════════
📊 스탯 계산
───────────────────────────────────────────────────
▶ 최종 공격력: {result.FinalAtk:N0}
▶ 총 방무: {result.TotalArmorPen * 100:F1}%
▶ 적 최종 방어력: {result.FinalDef:N0}
▶ 방어 계수: {result.DefCoefficient:F4}

📈 배율 계산
───────────────────────────────────────────────────
▶ 스킬 배율: {result.SkillRatio:F2}x
▶ 치명타 배율: {result.CritMultiplier:F2}x {(input.IsCritical ? "(치명타!)" : "(일반)")}
▶ 피해량 배율: {result.DamageMultiplier:F2}x

═══════════════════════════════════════════════════
💥 최종 데미지: {result.FinalDamage:N0}
═══════════════════════════════════════════════════";
        }
    }
}

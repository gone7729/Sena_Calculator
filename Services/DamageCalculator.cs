using System;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services
{
    public class DamageCalculator
    {
        #region Input/Output 클래스

        public class DamageInput
        {
            // 캐릭터/스킬
            public Character Character { get; set; }
            public Skill Skill { get; set; }
            public bool IsSkillEnhanced { get; set; }
            public int TranscendLevel { get; set; }

            // 최종 스탯 (UI에서 계산된 값, 정수%)
            public double FinalAtk { get; set; }
            public double FinalDef { get; set; }
            public double FinalHp { get; set; }
            public double CritDamage { get; set; }        // 치명타 피해%
            public double DmgDealt { get; set; }          // 주는 피해%
            public double DmgDealtBoss { get; set; }      // 보스 피해%
            public double ArmorPen { get; set; }          // 방무%
            public double WeakpointDmg { get; set; }      // 약점공격 피해%

            // 디버프 (정수%)
            public double DefReduction { get; set; }      // 방깎%
            public double DmgTakenIncrease { get; set; }  // 받피증%
            public double Vulnerability { get; set; }     // 취약%

            // 보스 정보
            public double BossDef { get; set; }
            public double BossDefIncrease { get; set; }   // 보스 방증%
            public double BossDmgReduction { get; set; }  // 보스 받피감%
            public double BossTargetReduction { get; set; } // n인기별 감소%

            // 전투 옵션
            public bool IsCritical { get; set; }
            public bool IsWeakpoint { get; set; }
            public bool IsBlocked { get; set; }
            public bool IsSkillConditionMet { get; set; }

            // 힐 관련
            public double HealReduction { get; set; }
        }

        public class DamageResult
        {
            // 스탯
            public double FinalAtk { get; set; }
            public double TotalArmorPen { get; set; }
            public double EffectiveBossDef { get; set; }
            public double DefCoefficient { get; set; }

            // 배율
            public double SkillRatio { get; set; }
            public double CritMultiplier { get; set; }
            public double WeakpointMultiplier { get; set; }
            public double DamageMultiplier { get; set; }

            // 타수
            public int AtkCount { get; set; }

            // 결과 (1타 기준)
            public double BaseDamage { get; set; }
            public double ExtraDamage { get; set; }       // 조건부 추가 피해
            public double WekBonusDmg { get; set; }       // 약점 추가 피해
            public double DamagePerHit { get; set; }      // 1타당 데미지
            public double FinalDamage { get; set; }       // 총 데미지 (타수 적용)

            // 회복
            public double HealAmount { get; set; }
            public string HealSource { get; set; }

            public double BonusDamage { get; set; }        // 별도 피해 (출혈 폭발 등)

            public string Details { get; set; }
        }

        #endregion

        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult { FinalAtk = input.FinalAtk };

            // 스킬 데이터 가져오기
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            var skillBonus = input.Skill?.GetTotalBonus(input.IsSkillEnhanced, input.TranscendLevel) ?? new BuffSet();
            result.AtkCount = input.Skill?.Atk_Count ?? 1;

            // 1. 스킬 배율
            result.SkillRatio = levelData?.Ratio ?? 1.0;

            // 2. 방어 관통 (캐릭터 스탯 + 스킬 보너스, 최대 100%)
            result.TotalArmorPen = Math.Min((input.ArmorPen + skillBonus.Arm_Pen) / 100.0, 1.0);

            // 3. 방어 계수
            result.DefCoefficient = CalcDefCoefficient(input, result.TotalArmorPen, out double effectiveDef);
            result.EffectiveBossDef = effectiveDef;

            // 4. 치명타 계수
            result.CritMultiplier = input.IsCritical 
                ? (input.CritDamage + skillBonus.Cri_Dmg) / 100.0 
                : 1.0;

            // 5. 약점 계수
            result.WeakpointMultiplier = input.IsWeakpoint 
                ? input.WeakpointDmg / 100.0 
                : 1.0;

            // 6. 피해 증가 계수
            result.DamageMultiplier = CalcDamageMultiplier(input);

            // 7. 기본 데미지 (1타)
            double atkOverDef = result.FinalAtk / result.DefCoefficient;
            result.BaseDamage = atkOverDef
                              * result.SkillRatio
                              * result.CritMultiplier
                              * result.WeakpointMultiplier
                              * result.DamageMultiplier;

            // 8. 조건부 추가 피해 (1타)
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                result.ExtraDamage = atkOverDef * levelData.ConditionalExtraDmg * result.DamageMultiplier;
            }

            // 9. 약점 추가 피해 (1타, 치명/약점 계수 미적용)
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && skillBonus.WekBonusDmg > 0)
            {
                result.WekBonusDmg = atkOverDef * skillBonus.WekBonusDmg * result.DamageMultiplier;
            }

            // 10. 1타당 데미지
            result.DamagePerHit = result.BaseDamage + result.ExtraDamage + result.WekBonusDmg;

            // 11. 막기 시 50% 감소
            if (input.IsBlocked)
            {
                result.DamagePerHit *= 0.5;
                result.BaseDamage *= 0.5;
                result.ExtraDamage *= 0.5;
                result.WekBonusDmg *= 0.5;
            }

            // 12. 별도 피해 (출혈 폭발 등, 치명타/약점 미적용)
            result.BonusDamage = 0;
            if (levelData?.BonusDmgRatio > 0)
            {
                var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
                double totalBonusRatio = levelData.BonusDmgRatio + skillTranscend.BonusDmgRatio;
                int stacks = levelData.BonusDmgMaxStacks;

                // 치명타/약점 미적용, 피증만 적용
                result.BonusDamage = atkOverDef * totalBonusRatio * result.DamageMultiplier * stacks;

                // 막기 시 50% 감소
                if (input.IsBlocked)
                {
                    result.BonusDamage *= 0.5;
                }
            }

            // 13. 총 데미지 (타수 적용)
            result.FinalDamage = result.DamagePerHit * result.AtkCount;

            // 14. 회복량 계산
            CalcHeal(input, levelData, result);

            // 15. 상세 정보
            result.Details = GenerateDetails(input, result);

            return result;
        }

        #region 계산 헬퍼

        /// <summary>
        /// 방어 계수 계산
        /// = 1 + 방어력 × (1 + 방증% - 방깎%) × (1 - 방무%) × 0.00214
        /// </summary>
        private double CalcDefCoefficient(DamageInput input, double armorPen, out double effectiveDef)
        {
            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            effectiveDef = input.BossDef * defModifier * armorPenModifier;
            return 1 + effectiveDef * 0.00214;
        }

        /// <summary>
        /// 피해 증가 계수 계산
        /// = 1 + (주는피해 + 보스피해 + 받피증 + 취약 - 받피감 - 인기감소) / 100
        /// </summary>
        private double CalcDamageMultiplier(DamageInput input)
        {
            // 스킬 초월 조건부 피해 보너스
            double conditionalDmgBonus = 0;
            if (input.Skill != null)
            {
                var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
                conditionalDmgBonus = skillTranscend.ConditionalDmgBonus;
            }

            double increase = input.DmgDealt + input.DmgDealtBoss + input.DmgTakenIncrease 
                            + input.Vulnerability + conditionalDmgBonus;
            double reduction = input.BossDmgReduction + input.BossTargetReduction;
            return 1 + (increase - reduction) / 100.0;
        }

        /// <summary>
        /// 회복량 계산
        /// </summary>
        private void CalcHeal(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            result.HealAmount = 0;
            result.HealSource = "";

            if (levelData == null) return;

            double baseHeal = 0;

            if (levelData.HealAtkRatio > 0)
            {
                baseHeal = input.FinalAtk * levelData.HealAtkRatio;
                result.HealSource = "공격력";
            }
            else if (levelData.HealDefRatio > 0)
            {
                baseHeal = input.FinalDef * levelData.HealDefRatio;
                result.HealSource = "방어력";
            }
            else if (levelData.HealHpRatio > 0)
            {
                baseHeal = input.FinalHp * levelData.HealHpRatio;
                result.HealSource = "최대체력";
            }

            if (baseHeal > 0)
            {
                result.HealAmount = baseHeal * (1 - input.HealReduction / 100.0);
            }
        }

        #endregion

        #region 출력

        private string GenerateDetails(DamageInput input, DamageResult result)
        {
            string critInfo = input.IsCritical ? "(치명타!)" : "(일반)";

            string wekInfo = input.IsWeakpoint ? "(약점!)" : "";

            string blockInfo = input.IsBlocked ? " (막기 -50%)" : "";

            string bonusDmgInfo = result.BonusDamage > 0
            ? $"\n  ├ 별도 피해: {result.BonusDamage:N0}"
            : "";

            string conditionalInfo = "";
            if (input.IsSkillConditionMet && input.Skill != null)
            {
                var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
                if (skillTranscend.ConditionalDmgBonus > 0)
                {
                    conditionalInfo = $"\n  스킬 조건부 피해: +{skillTranscend.ConditionalDmgBonus}%";
                }
            }

            string extraInfo = result.ExtraDamage > 0
                ? $"\n  ├ 조건부 추가: {result.ExtraDamage:N0}"
                : "";

            string wekBonusInfo = result.WekBonusDmg > 0
                ? $"\n  ├ 약점 추가: {result.WekBonusDmg:N0}"
                : "";

            string atkCountInfo = result.AtkCount > 1
                ? $"\n  └ {result.AtkCount}타 = {result.DamagePerHit:N0} × {result.AtkCount}"
                : "";

            string healInfo = result.HealAmount > 0
                ? $"\n\n💚 회복량: {result.HealAmount:N0} ({result.HealSource} 기준)"
                : "";

            return $@"═══════════════════════════════════════════════════
📊 스탯 정보
───────────────────────────────────────────────────
  최종 공격력: {result.FinalAtk:N0}
  총 방무: {result.TotalArmorPen * 100:F1}%
  보스 실효 방어력: {result.EffectiveBossDef:N0}
  방어 계수: {result.DefCoefficient:F4}

📈 배율 정보
───────────────────────────────────────────────────
  스킬 배율: {result.SkillRatio:F2}x
  치명 계수: {result.CritMultiplier:F2}x {critInfo}
  약공 계수: {result.WeakpointMultiplier:F2}x {wekInfo}
  피증 계수: {result.DamageMultiplier:F2}x{conditionalInfo}

═══════════════════════════════════════════════════
💥 최종 데미지: {result.FinalDamage:N0}{blockInfo}{extraInfo}{wekBonusInfo}{bonusDmgInfo}{atkCountInfo}{healInfo}
═══════════════════════════════════════════════════";
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services
{
    public class DamageCalculator
    {
        public class DamageInput
        {
            // 캐릭터/스킬
            public Character Character { get; set; }
            public Skill Skill { get; set; }
            public bool IsSkillEnhanced { get; set; }
            public int TranscendLevel { get; set; }

            // 최종 스탯 (UI에서 계산된 값, 정수%)
            public double FinalAtk { get; set; }
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
            public double BossDef { get; set; }           // 보스 방어력
            public double BossDefIncrease { get; set; }   // 보스 방증%
            public double BossDmgReduction { get; set; }  // 보스 받피감%
            public double BossTargetReduction { get; set; } // n인기별 감소%

            // 전투 옵션
            public bool IsCritical { get; set; }
            public bool IsWeakpoint { get; set; }
            public bool IsBlocked { get; set; }

            // 조건
            public bool IsSkillConditionMet { get; set; }

            public double FinalDef { get; set; }      // 최종 방어력 (힐 계산용)
            public double FinalHp { get; set; }       // 최종 체력 (힐 계산용)
            public double HealReduction { get; set; } // 회복량 감소%
        }

        public class DamageResult
        {
            public double FinalAtk { get; set; }
            public double DefCoefficient { get; set; }
            public double SkillRatio { get; set; }
            public double CritMultiplier { get; set; }
            public double WeakpointMultiplier { get; set; }
            public double DamageMultiplier { get; set; }
            public double TotalArmorPen { get; set; }
            public double EffectiveBossDef { get; set; }
            public double FinalDamage { get; set; }
            public double ExtraDamage { get; set; }
            public string Details { get; set; }
            public double WekBonusDmg { get; set; }
            public double HealAmount { get; set; }    // 회복량
            public string HealSource { get; set; }    // 회복 기준 (공격력/방어력/체력)
        }

        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult();
            result.FinalAtk = input.FinalAtk;

            // 1. 스킬 배율
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            result.SkillRatio = levelData?.Ratio ?? 1.0;

            // 스킬 초월 보너스
            double skillArmorPen = 0;
            double skillBonusCriDmg = 0;
            if (input.Skill != null)
            {
                var transcendBonus = input.Skill.GetTranscendBonus(input.TranscendLevel);
                skillArmorPen = transcendBonus.ArmorPen;
                skillBonusCriDmg = transcendBonus.BonusCriDmg;
            }

            // 2. 총 방무% (캐릭터 + 스킬 초월, 최대 100%)
            result.TotalArmorPen = Math.Min((input.ArmorPen + skillArmorPen) / 100.0, 1.0);

            // 3. 방어계수 = 1 + 방어력 × (1 + 방증% - 방깎%) × (1 - 방무%) × 0.00214
            double defModifier = 1 + (input.BossDefIncrease - input.DefReduction) / 100.0;
            defModifier = Math.Max(defModifier, 0);  // 음수 방지
            double armorPenModifier = 1 - result.TotalArmorPen;
            result.EffectiveBossDef = input.BossDef * defModifier * armorPenModifier;
            result.DefCoefficient = 1 + result.EffectiveBossDef * 0.00214;

            // 4. 치명계수 = 치명타 피해% (+ 스킬 추가 치피)
            if (input.IsCritical)
            {
                result.CritMultiplier = (input.CritDamage + skillBonusCriDmg) / 100.0;
            }
            else
            {
                result.CritMultiplier = 1.0;
            }

            // 5. 약공계수 = 약점공격 피해% (스탯에서 가져옴)
            if (input.IsWeakpoint)
            {
                result.WeakpointMultiplier = input.WeakpointDmg / 100.0;
            }
            else
            {
                result.WeakpointMultiplier = 1.0;
            }

            // 6. 피증계수 = 1 + (주는피해 + 보스피해 + 받피증 + 취약 - 받피감 - 인기감소) / 100
            double dmgIncrease = input.DmgDealt + input.DmgDealtBoss + input.DmgTakenIncrease + input.Vulnerability;
            double dmgReduction = input.BossDmgReduction + input.BossTargetReduction;
            result.DamageMultiplier = 1 + (dmgIncrease - dmgReduction) / 100.0;

            // 7. 최종 데미지 = (최종공격력 / 방어계수) × 스킬계수 × 치명계수 × 약공계수 × 피증계수
            double baseDamage = (result.FinalAtk / result.DefCoefficient)
                              * result.SkillRatio
                              * result.CritMultiplier
                              * result.WeakpointMultiplier
                              * result.DamageMultiplier;

            // 8. 조건부 추가 피해 (체력 30% 미만 등)
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData != null && levelData.ConditionalExtraDmg > 0)
            {
                double extraRatio = levelData.ConditionalExtraDmg;
                result.ExtraDamage = (result.FinalAtk / result.DefCoefficient)
                                   * extraRatio
                                   * result.DamageMultiplier;
                baseDamage += result.ExtraDamage;
            }

            // 9. 약점 공격 성공 시 추가 피해 (순수 공격력 배율, 치명타/약점피해 계수 미적용)
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && levelData != null && levelData.WekBonusDmg > 0)
            {
                result.WekBonusDmg = (result.FinalAtk / result.DefCoefficient)
                                   * levelData.WekBonusDmg
                                   * result.DamageMultiplier;
                baseDamage += result.WekBonusDmg;
            }

            // 10. 막기 시 50% 감소
            if (input.IsBlocked)
            {
                baseDamage *= 0.5;
                result.ExtraDamage *= 0.5;
            }

            // 11. 회복량 계산
            result.HealAmount = 0;
            result.HealSource = "";
            if (levelData != null)
            {
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

            result.FinalDamage = baseDamage;
            result.Details = GenerateDetails(input, result);

            return result;
        }

        private string GenerateDetails(DamageInput input, DamageResult result)
        {
            string extraDmgInfo = result.ExtraDamage > 0
                ? $"\n▶ 조건부 추가 피해: {result.ExtraDamage:N0}"
                : "";

            string wekBonusInfo = result.WekBonusDmg > 0
                ? $"\n▶ 약점 추가 피해: {result.WekBonusDmg:N0}"
                : "";

            string healInfo = result.HealAmount > 0
            ? $"\n\n💚 회복량: {result.HealAmount:N0} ({result.HealSource} 기준)"
            : "";

            string blockInfo = input.IsBlocked ? " (막기 -50%)" : "";
            string critInfo = input.IsCritical ? "(치명타!)" : "(일반)";
            string wekInfo = input.IsWeakpoint ? "(약점!)" : "";

            return $@"═══════════════════════════════════════════════════
📊 스탯 정보
───────────────────────────────────────────────────
▶ 최종 공격력: {result.FinalAtk:N0}
▶ 총 방무: {result.TotalArmorPen * 100:F1}%
▶ 보스 실효 방어력: {result.EffectiveBossDef:N0}
▶ 방어 계수: {result.DefCoefficient:F4}

📈 배율 정보
───────────────────────────────────────────────────
▶ 스킬 배율: {result.SkillRatio:F2}x
▶ 치명 계수: {result.CritMultiplier:F2}x {critInfo}
▶ 약공 계수: {result.WeakpointMultiplier:F2}x {wekInfo}
▶ 피증 계수: {result.DamageMultiplier:F2}x

═══════════════════════════════════════════════════
💥 최종 데미지: {result.FinalDamage:N0}{blockInfo}{extraDmgInfo}
═══════════════════════════════════════════════════";
        }
    }
}
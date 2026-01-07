using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            public double WeakpointDmgBuff { get; set; }  // 버프 약피% (미호 등)

            // 인기별 피해량 증가 (NEW)
            public double Dmg1to3 { get; set; }           // 1-3인기 피해%
            public double Dmg4to5 { get; set; }           // 4-5인기 피해%

            // 디버프 (정수%)
            public double DefReduction { get; set; }      // 방깎%
            public double DmgTakenIncrease { get; set; }  // 받피증%
            public double Vulnerability { get; set; }     // 취약%

            // 보스 정보
            public double BossDef { get; set; }
            public double BossDefIncrease { get; set; }   // 보스 방증%
            public double BossDmgReduction { get; set; }  // 보스 받피감%
            public double BossTargetReduction { get; set; } // n인기별 감소%

            // HP 비례 피해용
            public double TargetHp { get; set; }          // 대상 최대 체력
            public double TargetCurrentHp { get; set; }   // 대상 현재 체력

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

            public double BonusDamage { get; set; }        // 별도 피해 총합

            // HP 비례 피해
            public double HpRatioDamage { get; set; }

            // 상태이상별 피해 상세 (치명타/약점 미적용)
            public Dictionary<string, double> BonusDamageDetails { get; set; } = new Dictionary<string, double>();

            public string Details { get; set; }
        }

        #endregion

        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult 
            { 
                FinalAtk = input.FinalAtk
            };

            // 스킬 데이터 가져오기
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            var skillBonus = input.Skill?.GetTotalBonus(input.IsSkillEnhanced, input.TranscendLevel) ?? new BuffSet();
            result.AtkCount = input.Skill?.Atk_Count ?? 1;

            // 1. 스킬 배율 (정수% → 소수 변환)
            result.SkillRatio = (levelData?.Ratio ?? 100) / 100.0;

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
                ? (input.WeakpointDmg / 100.0) * (1 + input.WeakpointDmgBuff / 100.0)
                : 1.0;

            // 6. 피해 증가 계수 (★ 모드에 따라 보스피해 적용 여부 결정)
            result.DamageMultiplier = CalcDamageMultiplier(input);

            // 7. 기본 데미지 (1타)
            double atkOverDef = result.FinalAtk / result.DefCoefficient;
            result.BaseDamage = atkOverDef
                              * result.SkillRatio
                              * result.CritMultiplier
                              * result.WeakpointMultiplier
                              * result.DamageMultiplier;

            // 8. 조건부 추가 피해 (1타, 정수% 변환)
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                result.ExtraDamage = atkOverDef * (levelData.ConditionalExtraDmg / 100.0) * result.DamageMultiplier;
            }

            // 9. 약점 추가 피해 (1타, 치명/약점 계수 미적용, 정수% 변환)
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && skillBonus.WekBonusDmg > 0)
            {
                result.WekBonusDmg = atkOverDef * (skillBonus.WekBonusDmg / 100.0) * result.DamageMultiplier;
            }

            // 10. HP 비례 피해
            result.HpRatioDamage = CalcHpRatioDamage(input, levelData, result.DamageMultiplier);

            // 11. 1타당 데미지
            result.DamagePerHit = result.BaseDamage + result.ExtraDamage + result.WekBonusDmg;

            // 12. 별도 피해 (HP비례 + 상태이상, 치명타/약점 미적용)
            CalcBonusDamage(input, levelData, atkOverDef, result);

            // 13. 막기 시 50% 감소
            if (input.IsBlocked)
            {
                result.DamagePerHit *= 0.5;
                result.BaseDamage *= 0.5;
                result.ExtraDamage *= 0.5;
                result.WekBonusDmg *= 0.5;
                result.HpRatioDamage *= 0.5;
                result.BonusDamage *= 0.5;
                
                // 상세 내역도 감소
                var keys = new List<string>(result.BonusDamageDetails.Keys);
                foreach (var key in keys)
                {
                    result.BonusDamageDetails[key] *= 0.5;
                }
            }

            // 14. 총 데미지 (타수 적용)
            result.FinalDamage = (result.DamagePerHit + result.BonusDamage) * result.AtkCount;

            // 15. 회복량 계산
            CalcHeal(input, levelData, result);

            // 16. 상세 정보
            result.Details = GenerateDetails(input, result);

            return result;
        }

        #region 계산 헬퍼

        /// <summary>
        /// 별도 피해 계산 (HP비례 + 상태이상, 치명타/약점 미적용)
        /// Dictionary에 상세 내역 저장
        /// </summary>
        private void CalcBonusDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
        {
            result.BonusDamage = 0;
            result.BonusDamageDetails.Clear();

            if (levelData == null) return;

            // === 1. HP 비례 피해 (카일 등) ===
            if (result.HpRatioDamage > 0)
            {
                result.BonusDamageDetails["HP비례"] = result.HpRatioDamage;
                result.BonusDamage += result.HpRatioDamage;
            }

            // === 2. 상태이상 피해 ===
            if (levelData.StatusEffects != null && levelData.StatusEffects.Count > 0)
            {
                // 초월 보너스 가져오기
                var skillTranscend = input.Skill?.GetTranscendBonus(input.TranscendLevel);

                foreach (var effect in levelData.StatusEffects)
                {
                    // 초월 오버라이드가 있으면 해당 값 사용
                    var effectToUse = skillTranscend?.StatusEffects?
                        .FirstOrDefault(e => e.Type == effect.Type) ?? effect;

                    var baseEffect = StatusEffectDb.Get(effectToUse.Type);
                    if (baseEffect == null) continue;

                    // 커스텀 값 또는 기본값
                    double atkRatio = (effectToUse.CustomAtkRatio ?? baseEffect.AtkRatio) / 100.0;
                    double hpRatio = (effectToUse.CustomHpRatio ?? baseEffect.TargetMaxHpRatio) / 100.0;
                    double currentHpRatio = baseEffect.TargetCurrentHpRatio / 100.0;
                    double atkCap = (effectToUse.CustomAtkCap ?? baseEffect.AtkCap) / 100.0;
                    double armorPen = (effectToUse.CustomArmorPen ?? baseEffect.ArmorPen) / 100.0;
                    int stacks = effectToUse.Stacks > 0 ? effectToUse.Stacks : (baseEffect.MaxStacks > 0 ? baseEffect.MaxStacks : 1);

                    double damage = 0;

                    // 공격력 비례 (화상, 감전, 석화, 폭탄, 출혈폭발 등)
                    if (atkRatio > 0)
                    {
                        // 방무 적용된 atkOverDef 또는 별도 계산
                        double effectiveAtkOverDef = atkOverDef;
                        if (armorPen > 0)
                        {
                            // 상태이상 고유 방무가 있으면 재계산
                            double newArmorPen = Math.Min(input.ArmorPen / 100.0 + armorPen, 1.0);
                            double defCoef = CalcDefCoefficientSimple(input, newArmorPen);
                            effectiveAtkOverDef = input.FinalAtk / defCoef;
                        }
                        damage += effectiveAtkOverDef * atkRatio * stacks;
                    }

                    // 대상 최대 HP 비례 (빙결, 빙극, 마력역류 등)
                    if (hpRatio > 0 && input.TargetHp > 0)
                    {
                        double hpDamage = input.TargetHp * hpRatio;
                        if (atkCap > 0)
                        {
                            hpDamage = Math.Min(hpDamage, input.FinalAtk * atkCap);
                        }
                        damage += hpDamage * stacks;
                    }

                    // 대상 현재 HP 비례 (즉사 등)
                    if (currentHpRatio > 0 && input.TargetCurrentHp > 0)
                    {
                        double hpDamage = input.TargetCurrentHp * currentHpRatio;
                        if (atkCap > 0)
                        {
                            hpDamage = Math.Min(hpDamage, input.FinalAtk * atkCap);
                        }
                        damage += hpDamage * stacks;
                    }

                    // 고정 피해 (수정 결정)
                    double fixedDmg = effectToUse.CustomFixedDamage ?? baseEffect.FixedDamage;
                    if (fixedDmg > 0)
                    {
                        damage = fixedDmg * stacks;
                    }

                    // 피해 증가 계수 적용
                    damage *= result.DamageMultiplier;

                    if (damage > 0)
                    {
                        string effectName = baseEffect.Name;
                        if (result.BonusDamageDetails.ContainsKey(effectName))
                        {
                            result.BonusDamageDetails[effectName] += damage;
                        }
                        else
                        {
                            result.BonusDamageDetails[effectName] = damage;
                        }
                        result.BonusDamage += damage;
                    }
                }
            }
        }

        /// <summary>
        /// 방어 계수 간단 계산 (상태이상 방무용)
        /// </summary>
        private double CalcDefCoefficientSimple(DamageInput input, double armorPen)
        {
            const double DEF_CONSTANT = 0.00214;
            const double THRESHOLD = 3125.0;

            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            double effectiveDef = input.BossDef * defModifier * armorPenModifier;

            if (effectiveDef <= THRESHOLD)
            {
                return 1 + effectiveDef * DEF_CONSTANT;
            }
            else
            {
                double baseCoef = 1 + THRESHOLD * DEF_CONSTANT;
                double extraDef = effectiveDef - THRESHOLD;
                return baseCoef + extraDef * DEF_CONSTANT * 0.5;
            }
        }

        /// <summary>
        /// HP 비례 피해 계산
        /// - 대상 최대 HP의 N% 피해
        /// - 공격력 제한 적용 (AtkCap)
        /// - 피해 증가 계수 적용
        /// </summary>
        private double CalcHpRatioDamage(DamageInput input, SkillLevelData levelData, double damageMultiplier)
        {
            if (levelData == null) return 0;

            double totalHpDamage = 0;

            // 대상 최대 HP 비례 피해
            if (levelData.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                // 기본 피해 = 대상 최대 HP × 비율%
                double rawDamage = input.TargetHp * (levelData.TargetMaxHpRatio / 100.0);

                // 공격력 제한 적용
                if (levelData.AtkCap > 0)
                {
                    double cap = input.FinalAtk * (levelData.AtkCap / 100.0);
                    rawDamage = Math.Min(rawDamage, cap);
                }

                // 피해 증가 계수 적용
                totalHpDamage += rawDamage * damageMultiplier;
            }

            // 대상 현재 HP 비례 피해 (즉사 등)
            if (levelData.TargetCurrentHpRatio > 0 && input.TargetCurrentHp > 0)
            {
                double rawDamage = input.TargetCurrentHp * (levelData.TargetCurrentHpRatio / 100.0);

                // 공격력 제한 적용
                if (levelData.AtkCap > 0)
                {
                    double cap = input.FinalAtk * (levelData.AtkCap / 100.0);
                    rawDamage = Math.Min(rawDamage, cap);
                }

                totalHpDamage += rawDamage * damageMultiplier;
            }

            return totalHpDamage;
        }

        /// <summary>
        /// 방어 계수 계산
        /// = 1 + 방어력 × (1 + 방증% - 방깎%) × (1 - 방무%) × 0.00214
        /// </summary>
        private double CalcDefCoefficient(DamageInput input, double armorPen, out double effectiveDef)
        {
            const double DEF_CONSTANT = 0.00214;
            const double THRESHOLD = 3125.0;

            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            effectiveDef = input.BossDef * defModifier * armorPenModifier;

            if (effectiveDef <= THRESHOLD)
            {
                return 1 + effectiveDef * DEF_CONSTANT;
            }
            else
            {
                double baseCoef = 1 + THRESHOLD * DEF_CONSTANT;
                double extraDef = effectiveDef - THRESHOLD;
                return baseCoef + extraDef * DEF_CONSTANT * 0.5;
            }
        }

        /// <summary>
        /// 피해 증가 계수 계산
        /// PVE: 1 + (주는피해 + 보스피해 + 받피증 + 취약 - 받피감 - 인기감소) / 100
        /// PVP: 1 + (주는피해 + 받피증 + 취약 - 받피감 - 인기감소) / 100 (보스피해 제외)
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

            // 인기별 피해량 증가 (스킬 타겟 수에 따라)
            double targetTypeDmg = 0;
            if (input.Skill != null)
            {
                int targetCount = input.Skill.GetTargetCount(input.TranscendLevel);
                if (targetCount >= 1 && targetCount <= 3)
                {
                    targetTypeDmg = input.Dmg1to3;
                }
                else if (targetCount >= 4)
                {
                    targetTypeDmg = input.Dmg4to5;
                }
            }

            // 피해 증가 합계 (PVE: 보스피해 포함)
            double increase = input.DmgDealt + input.DmgDealtBoss + input.DmgTakenIncrease 
                            + input.Vulnerability + conditionalDmgBonus + targetTypeDmg;

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

            // 별도 피해 상세 내역 생성
            var bonusDmgBuilder = new StringBuilder();
            if (result.BonusDamageDetails != null && result.BonusDamageDetails.Count > 0)
            {
                foreach (var kvp in result.BonusDamageDetails)
                {
                    bonusDmgBuilder.Append($"\n  ├ {kvp.Key}: {kvp.Value:N0}");
                }
            }
            string bonusDmgInfo = bonusDmgBuilder.ToString();

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
                ? $"\n  └ {result.AtkCount}타 = {(result.DamagePerHit + result.BonusDamage):N0} × {result.AtkCount}"
                : "";

            string healInfo = result.HealAmount > 0
                ? $"\n\n💚 회복량: {result.HealAmount:N0} ({result.HealSource} 기준)"
                : "";

            return $@"═══════════════════════════════════════════════════
🎯 PVE (보스전)
═══════════════════════════════════════════════════

📊 스탯 정보
───────────────────────────────────────────────────
  최종 공격력: {result.FinalAtk:N0}
  총 방무: {result.TotalArmorPen * 100:F1}%
  실효 방어력: {result.EffectiveBossDef:N0}
  방어 계수: {result.DefCoefficient:F4}

📈 배율 정보
───────────────────────────────────────────────────
  스킬 배율: {result.SkillRatio:F2}x
  치명 계수: {result.CritMultiplier:F2}x {critInfo}
  약공 계수: {result.WeakpointMultiplier:F2}x {wekInfo}
  피증 계수: {result.DamageMultiplier:F2}x{conditionalInfo}

═══════════════════════════════════════════════════
💥 최종 데미지: {result.FinalDamage:N0}{blockInfo}{extraInfo}{wekBonusInfo}{bonusDmgInfo}{atkCountInfo}{healInfo}
═══════════════════════════════════════════════════

---디버깅용---
  [캐릭터 스탯]
  공격력: {input.FinalAtk:N0}
  치피%: {input.CritDamage}
  약피%: {input.WeakpointDmg}
  주피%: {input.DmgDealt}
  보피%: {input.DmgDealtBoss}
  방무%: {input.ArmorPen}

  [디버프]
  방깎%: {input.DefReduction}
  받피증%: {input.DmgTakenIncrease}
  취약%: {input.Vulnerability}

  [보스]
  방어력: {input.BossDef}
  방증%: {input.BossDefIncrease}
  받피감%: {input.BossDmgReduction}
  인기감%: {input.BossTargetReduction}

  [전투옵션]
  치명타: {input.IsCritical}
  약점: {input.IsWeakpoint}
  막기: {input.IsBlocked}
";
        }

        #endregion
    }
}
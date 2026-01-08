using System;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services
{
    public class PvpDamageCalculator
    {
        #region Input/Output 클래스

        public class PvpDamageInput
        {
            // 내 캐릭터/스킬
            public Character Character { get; set; }
            public Skill Skill { get; set; }
            public bool IsSkillEnhanced { get; set; }
            public int TranscendLevel { get; set; }

            // 내 최종 스탯
            public double FinalAtk { get; set; }
            public double FinalDef { get; set; }
            public double FinalHp { get; set; }
            public double CritDamage { get; set; }        // 치명타 피해%
            public double WeakpointDmg { get; set; }      // 약점공격 피해%
            public double WeakpointDmgBuff { get; set; }  // 버프 약피% (미호 등)
            public double DmgDealt { get; set; }          // 주는 피해%
            public double ArmorPen { get; set; }          // 방무%
            public double Dmg1to3 { get; set; }           // 1-3인기 피해%
            public double Dmg4to5 { get; set; }           // 4-5인기 피해%

            // 내가 건 디버프 (상대에게 적용)
            public double DefReduction { get; set; }      // 방깎%
            public double DmgTakenIncrease { get; set; }  // 받피증%
            public double Vulnerability { get; set; }     // 취약%

            // 상대 캐릭터 정보
            public double TargetDef { get; set; }         // 상대 최종 방어력
            public double TargetDefIncrease { get; set; } // 상대 방증% (진형 등)
            public double TargetDmgRdc { get; set; }      // 상대 받피감%
            public double TargetHp { get; set; }          // 상대 최종 체력

            // 인기별 피해 감소 (상대)
            public double Target1to3Rdc { get; set; }     // 1-3인기 받피감%
            public double Target4to5Rdc { get; set; }     // 4-5인기 받피감%

            // 전투 옵션
            public bool IsCritical { get; set; }
            public bool IsWeakpoint { get; set; }
            public bool IsBlocked { get; set; }
            public bool IsSkillConditionMet { get; set; }
        }

        public class PvpDamageResult
        {
            // 스탯
            public double FinalAtk { get; set; }
            public double TotalArmorPen { get; set; }
            public double EffectiveTargetDef { get; set; }
            public double DefCoefficient { get; set; }

            // 배율
            public double SkillRatio { get; set; }
            public double CritMultiplier { get; set; }
            public double WeakpointMultiplier { get; set; }
            public double DamageMultiplier { get; set; }

            // 타수
            public int AtkCount { get; set; }

            // 결과
            public double BaseDamage { get; set; }
            public double ExtraDamage { get; set; }
            public double WekBonusDmg { get; set; }
            public double DamagePerHit { get; set; }
            public double FinalDamage { get; set; }
            public double BonusDamage { get; set; }

            // PVP 전용
            public double DamagePercent { get; set; }     // 상대 체력 대비 %

            public string Details { get; set; }
        }

        #endregion

        // PVP 방어 상수 (PVE: 0.00214)
        private const double PVP_DEF_CONSTANT = 0.00319;
        private const double DEF_THRESHOLD = 3125.0;

        public PvpDamageResult Calculate(PvpDamageInput input)
        {
            var result = new PvpDamageResult
            {
                FinalAtk = input.FinalAtk
            };

            // 스킬 데이터
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            var skillBonus = input.Skill?.GetTotalBonus(input.IsSkillEnhanced, input.TranscendLevel) ?? new BuffSet();
            result.AtkCount = input.Skill?.Atk_Count ?? 1;

            // 1. 스킬 배율
            result.SkillRatio = levelData?.Ratio ?? 1.0;

            // 2. 방어 관통 (최대 100%)
            result.TotalArmorPen = Math.Min((input.ArmorPen + skillBonus.Arm_Pen) / 100.0, 1.0);

            // 3. 방어 계수 (PVP 전용 상수)
            result.DefCoefficient = CalcDefCoefficient(input, result.TotalArmorPen, out double effectiveDef);
            result.EffectiveTargetDef = effectiveDef;

            // 4. 치명타 계수
            result.CritMultiplier = input.IsCritical
                ? 1 + (input.CritDamage + skillBonus.Cri_Dmg) / 100.0
                : 1.0;

            // 5. 약점 계수
            result.WeakpointMultiplier = input.IsWeakpoint
                ? 1 + (input.WeakpointDmg / 100.0) * (1 + input.WeakpointDmgBuff / 100.0)
                : 1.0;

            // 6. 피해 증가 계수 (보스피해 미적용)
            result.DamageMultiplier = CalcDamageMultiplier(input);

            // 7. 기본 데미지 (1타)
            double atkOverDef = result.FinalAtk / result.DefCoefficient;
            result.BaseDamage = atkOverDef
                              * result.SkillRatio
                              * result.CritMultiplier
                              * result.WeakpointMultiplier
                              * result.DamageMultiplier;

            // 8. 조건부 추가 피해
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                result.ExtraDamage = atkOverDef * levelData.ConditionalExtraDmg * result.DamageMultiplier;
            }

            // 9. 약점 추가 피해 (추적자 등)
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && skillBonus.WekBonusDmg > 0)
            {
                result.WekBonusDmg = atkOverDef * (skillBonus.WekBonusDmg / 100.0) * result.DamageMultiplier;
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

            // 13. 총 데미지
            result.FinalDamage = result.DamagePerHit * result.AtkCount;

            // 14. 상대 체력 대비 %
            if (input.TargetHp > 0)
            {
                result.DamagePercent = (result.FinalDamage / input.TargetHp) * 100;
            }

            // 15. 상세 정보
            result.Details = GenerateDetails(input, result);

            return result;
        }

        #region 계산 헬퍼

        private double CalcDefCoefficient(PvpDamageInput input, double armorPen, out double effectiveDef)
        {
            // 방어력 증감
            double defModifier = Math.Max(1 + (input.TargetDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            effectiveDef = input.TargetDef * defModifier * armorPenModifier;

            // PVP 방어 계수 공식
            if (effectiveDef <= DEF_THRESHOLD)
            {
                return 1 + effectiveDef * PVP_DEF_CONSTANT;
            }
            else
            {
                double baseCoef = 1 + DEF_THRESHOLD * PVP_DEF_CONSTANT;
                double extraDef = effectiveDef - DEF_THRESHOLD;
                return baseCoef + extraDef * PVP_DEF_CONSTANT * 0.5;
            }
        }

        private double CalcDamageMultiplier(PvpDamageInput input)
        {
            // 스킬 초월 조건부 피해 보너스
            double conditionalDmgBonus = 0;
            if (input.Skill != null)
            {
                var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
                conditionalDmgBonus = skillTranscend.ConditionalDmgBonus;
            }

            // 인기별 피해량 증가
            double targetTypeDmg = 0;
            double targetTypeRdc = 0;
            if (input.Skill != null)
            {
                int targetCount = input.Skill.GetTargetCount(input.TranscendLevel);
                if (targetCount >= 1 && targetCount <= 3)
                {
                    targetTypeDmg = input.Dmg1to3;
                    targetTypeRdc = input.Target1to3Rdc;
                }
                else if (targetCount >= 4)
                {
                    targetTypeDmg = input.Dmg4to5;
                    targetTypeRdc = input.Target4to5Rdc;
                }
            }

            // 피해 증가 합계 (보스피해 제외!)
            double increase = input.DmgDealt + input.DmgTakenIncrease
                            + input.Vulnerability + conditionalDmgBonus + targetTypeDmg;

            // 피해 감소 합계
            double reduction = input.TargetDmgRdc + targetTypeRdc;

            return 1 + (increase - reduction) / 100.0;
        }

        #endregion

        #region 출력

        private string GenerateDetails(PvpDamageInput input, PvpDamageResult result)
        {
            string critInfo = input.IsCritical ? "(치명타!)" : "(일반)";
            string wekInfo = input.IsWeakpoint ? "(약점!)" : "";
            string blockInfo = input.IsBlocked ? " [막기 -50%]" : "";

            string bonusDmgInfo = result.BonusDamage > 0
                ? $"\n  ├ 별도 피해: {result.BonusDamage:N0}"
                : "";

            string extraInfo = result.ExtraDamage > 0
                ? $"\n  ├ 조건부 추가: {result.ExtraDamage:N0}"
                : "";

            string wekBonusInfo = result.WekBonusDmg > 0
                ? $"\n  ├ 약점 추가: {result.WekBonusDmg:N0}"
                : "";

            string atkCountInfo = result.AtkCount > 1
                ? $"\n  └ {result.AtkCount}타 = {result.DamagePerHit:N0} × {result.AtkCount}"
                : "";

            string hpPercentInfo = input.TargetHp > 0
                ? $"\n  📊 상대 체력 대비: {result.DamagePercent:F1}%"
                : "";

            return $@"═══════════════════════════════════════════════════
⚔️ PVP (대인전)
═══════════════════════════════════════════════════

📊 스탯 정보
───────────────────────────────────────────────────
  최종 공격력: {result.FinalAtk:N0}
  총 방무: {result.TotalArmorPen * 100:F1}%
  상대 실효 방어력: {result.EffectiveTargetDef:N0}
  방어 계수: {result.DefCoefficient:F4}

📈 배율 정보
───────────────────────────────────────────────────
  스킬 배율: {result.SkillRatio:F2}x
  치명 계수: {result.CritMultiplier:F2}x {critInfo}
  약공 계수: {result.WeakpointMultiplier:F2}x {wekInfo}
  피증 계수: {result.DamageMultiplier:F2}x

═══════════════════════════════════════════════════
💥 최종 데미지: {result.FinalDamage:N0}{blockInfo}{hpPercentInfo}{extraInfo}{wekBonusInfo}{bonusDmgInfo}{atkCountInfo}
═══════════════════════════════════════════════════

---디버깅용---
  [내 캐릭터]
  공격력: {input.FinalAtk:N0}
  치피%: {input.CritDamage}
  약피%: {input.WeakpointDmg}
  주피%: {input.DmgDealt}
  방무%: {input.ArmorPen}
  1-3인기%: {input.Dmg1to3}
  4-5인기%: {input.Dmg4to5}

  [내가 건 디버프]
  방깎%: {input.DefReduction}
  받피증%: {input.DmgTakenIncrease}
  취약%: {input.Vulnerability}

  [상대 캐릭터]
  방어력: {input.TargetDef}
  방증%: {input.TargetDefIncrease}
  받피감%: {input.TargetDmgRdc}
  체력: {input.TargetHp:N0}
  1-3인기 받피감%: {input.Target1to3Rdc}
  4-5인기 받피감%: {input.Target4to5Rdc}

  [전투옵션]
  치명타: {input.IsCritical}
  약점: {input.IsWeakpoint}
  막기: {input.IsBlocked}

  [PVP 방어상수: {PVP_DEF_CONSTANT}]
";
        }

        #endregion
    }
}

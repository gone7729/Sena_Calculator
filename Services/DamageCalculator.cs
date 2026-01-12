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
            public double EffResReduction { get; set; }  // 대상 효과 저항 감소%

            // 보스 정보
            public double BossDef { get; set; }
            public double BossDefIncrease { get; set; }   // 보스 방증%
            public double BossDmgReduction { get; set; }  // 보스 받피감%
            public double BossTargetReduction { get; set; } // n인기별 감소%
            public double BossHp { get; set; } // 보스 생명력

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

            public double EffHit { get; set; }         // 효과 적중%
            public double TargetEffRes { get; set; }   // 대상 효과 저항%
            public bool ForceStatusEffect { get; set; } // 상태이상 100% 적용 체크
            public int TargetStackCount { get; set; } = 0;
            public bool IsLostHpConditionMet { get; set; }  // 체력조건 체크
            public double BlessingCap { get; set; }  // 축복 피해 제한 (최대 HP%)
            public double SelfMaxHp { get; set; }    // 자신 최대 HP
            public double TargetBlessing { get; set; }  // 대상 축복 피해 제한%
            public double AtkBuff { get; set; } // 현재 적용 중인 공격력 버프%
            public BattleMode Mode { get; set; } = BattleMode.Boss;
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
            public double CriBonusDmg { get; set; }       // 치명타 추가 피해
            public double DamagePerHit { get; set; }      // 1타당 데미지
            public double FinalDamage { get; set; }       // 총 데미지 (타수 적용)
            public double ConsumeExtraDmg { get; set; }       // 총 데미지 (타수 적용)

            // 회복
            public double HealAmount { get; set; }
            public string HealSource { get; set; }

            public double BonusDamage { get; set; }        // 별도 피해 총합

            // HP 비례 피해
            public double HpRatioDamage { get; set; }

            // 상태이상별 피해 상세 (치명타/약점 미적용)
            public Dictionary<string, double> BonusDamageDetails { get; set; } = new Dictionary<string, double>();

            public string Details { get; set; }
            public List<StatusEffectResult> StatusEffectResults { get; set; } = new List<StatusEffectResult>();
            public double SkillDamage { get; set; }    // 스킬 피해 (상태이상 제외)
            public double StatusDamage { get; set; }   // 상태이상 피해

            public double CoopDamage { get; set; }       // 협공 피해
            public double CoopHpDamage { get; set; }     // 협공 HP비례 피해
            public bool CoopTriggered { get; set; }      // 협공 발동 여부
            public double CoopChance { get; set; }       // 협공 확률
            public double DamageBeforeBlessing { get; set; }  // 축복 적용 전 피해
            public bool BlessingApplied { get; set; }         // 축복 적용 여부

            public double HpConversionValue { get; set; }  // 전환 후 HP
            public double HpConversionLoss { get; set; }   // HP 감소량
            public bool HasHpConversion { get; set; }
            public double HealFromDamage { get; set; }  // 피해량 비례 회복량
            
        }

        public class StatusEffectResult
        {
            public string Name { get; set; }           // 상태이상 이름
            public double ApplyChance { get; set; }    // 적용 확률%
            public double ExpectedStacks { get; set; } // 기대 스택
            public int MaxStacks { get; set; }         // 최대 스택
            public bool IsForced { get; set; }         // 강제 적용 여부
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

            // 스킬 발동 전 버프 (기존 버프보다 높을 때만)
            double preCastAtkBonus = levelData?.PreCastBuff?.Atk_Rate ?? 0;
            if (preCastAtkBonus > input.AtkBuff)
            {
                double additionalBonus = preCastAtkBonus - input.AtkBuff;
                result.FinalAtk = input.FinalAtk * (1 + additionalBonus / 100.0);
            }
            else
            {
                result.FinalAtk = input.FinalAtk;
            }

            result.AtkCount = input.Skill?.Atk_Count ?? 1;

            // 1. 스킬 배율 (정수% → 소수 변환)
            result.SkillRatio = (levelData?.Ratio ?? 100) / 100.0;

            // 2. 방어 관통 (캐릭터 스탯 + 스킬 보너스, 최대 100%)
            result.TotalArmorPen = Math.Min((input.ArmorPen + skillBonus.Arm_Pen) / 100.0, 1.0);

            // 3. 방어 계수 계산 - 스킬 방깎 vs 기존 방깎 비교 후 더 높은 값 적용
            double skillDefReduction = levelData?.DispelDefReduction ?? 0;
            double effectiveDefReduction = Math.Max(input.DefReduction, skillDefReduction);

            // 임시로 DefReduction 교체
            double originalDefReduction = input.DefReduction;
            input.DefReduction = effectiveDefReduction;

            result.DefCoefficient = CalcDefCoefficient(input, result.TotalArmorPen, out double effectiveDef);
            result.EffectiveBossDef = effectiveDef;

            // 원래 값 복원
            input.DefReduction = originalDefReduction;

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

            // 6-1. 잃은 HP 비례 피해 증가 추가 ← 이거 추가!
            double lostHpBonus = CalcLostHpBonusDmg(input, levelData);
            if (lostHpBonus > 0)
            {
                result.DamageMultiplier *= (1 + lostHpBonus / 100.0);
            }

            // 7. 기본 데미지 (1타)
            double atkOverDef = result.FinalAtk / result.DefCoefficient;

            // 공격력 비례 피해
            double atkDamage = atkOverDef * result.SkillRatio;
            double fixedDamage = levelData?.FixedDamage ?? 0;  // 고정 피해

            // 방어력 비례 피해
            double defDamage = 0;
            if (levelData?.DefRatio > 0 && input.FinalDef > 0)
            {
                double defOverDef = input.FinalDef / result.DefCoefficient;
                defDamage = defOverDef * (levelData.DefRatio / 100.0);
            }

            // 생명력 비례 피해
            double hpDamage = 0;
            if (levelData?.HpRatio > 0 && input.FinalHp > 0)
            {
                double hpOverDef = input.FinalHp / result.DefCoefficient;
                hpDamage = hpOverDef * (levelData.HpRatio / 100.0);
            }

            result.BaseDamage = (atkDamage + defDamage + hpDamage)
                              * result.CritMultiplier
                              * result.WeakpointMultiplier
                              * result.DamageMultiplier;
            
            result.BaseDamage += fixedDamage;

            // 8. 조건부 추가 피해 (1타, 정수% 변환)
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                double extraDmg = atkOverDef * (levelData.ConditionalExtraDmg / 100.0) * result.DamageMultiplier;

                if (levelData.ConditionalExtraDmgPerHit)
                {
                    extraDmg *= result.AtkCount;
                }

                result.ExtraDamage = extraDmg;
            }

            // 9. 약점 추가 피해 (1타, 치명/약점 계수 미적용, 정수% 변환)
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && skillBonus.WekBonusDmg > 0)
            {
                result.WekBonusDmg = atkOverDef * (skillBonus.WekBonusDmg / 100.0) * result.DamageMultiplier;
            }

            // 10. 치명타 추가 피해 (NEW)
            result.CriBonusDmg = 0;
            if (input.IsCritical && skillBonus.CriBonusDmg > 0)
            {
                double criBonus = atkOverDef * (skillBonus.CriBonusDmg / 100.0) * result.DamageMultiplier;

                // 타격당 적용 여부
                if (skillBonus.CriBonusDmgPerHit)
                {
                    criBonus *= result.AtkCount;
                }

                result.CriBonusDmg = criBonus;
            }

            // 11. HP 비례 피해
            result.HpRatioDamage = CalcHpRatioDamage(input, levelData, result.DamageMultiplier);

            // 12. 스택 소모형 추가 피해
            result.ConsumeExtraDmg = 0;
            if (levelData?.ConsumeExtra != null)
            {
                var consumeExtra = levelData.ConsumeExtra;
                double damage = 0;

                // HP 비례 피해
                if (consumeExtra.TargetMaxHpRatio > 0 && input.TargetHp > 0)
                {
                    damage = input.TargetHp * (consumeExtra.TargetMaxHpRatio / 100.0);

                    // 공격력 제한
                    if (consumeExtra.AtkCap > 0)
                    {
                        double cap = input.FinalAtk * (consumeExtra.AtkCap / 100.0);
                        damage = Math.Min(damage, cap);
                    }
                }

                // 공격력 비례 피해
                if (consumeExtra.AtkRatio > 0)
                {
                    damage += atkOverDef * (consumeExtra.AtkRatio / 100.0);
                }

                result.ConsumeExtraDmg = damage * result.DamageMultiplier;
            }

            // 13. 1타당 데미지
            result.DamagePerHit = result.BaseDamage + result.ExtraDamage + result.WekBonusDmg + result.CriBonusDmg + result.ConsumeExtraDmg;

            // 14. 별도 피해 (HP비례 + 상태이상, 치명타/약점 미적용)
            CalcBonusDamage(input, levelData, atkOverDef, result);

            // 15. 막기 시 50% 감소
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

            // 16. 축복 적용
            result.DamagePerHit = ApplyBlessing(result.DamagePerHit, input, result);

            // 17. 총 데미지 (타수 적용) - 상태이상 제외!
            result.SkillDamage = result.DamagePerHit * result.AtkCount;  // 스킬 피해만
            result.StatusDamage = result.BonusDamage * result.AtkCount;  // 상태이상 피해
            result.FinalDamage = result.SkillDamage + result.StatusDamage;  // 총합

            // 18. 회복량 계산
            CalcHeal(input, levelData, result);

            result.HealFromDamage = 0;
            if (levelData?.HealDmgRatio > 0)
            {
                result.HealFromDamage = result.FinalDamage * (levelData.HealDmgRatio / 100.0);
            }

            // 19. 협공 피해 계산
            CalcCoopDamage(input, result);

            // 20. 상세 정보
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
           result.StatusEffectResults.Clear();  // 추가!

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
               var skillTranscend = input.Skill?.GetTranscendBonus(input.TranscendLevel);

               foreach (var effect in levelData.StatusEffects)
               {
                   var effectToUse = skillTranscend?.StatusEffects?
                       .FirstOrDefault(e => e.Type == effect.Type) ?? effect;

                   var baseEffect = StatusEffectDb.Get(effectToUse.Type);
                   if (baseEffect == null) continue;

                   // 적용 확률 계산
                   double applyChance = CalcStatusEffectChance(input, effectToUse);

                   // 독립시행 기대 스택 계산 (효과 적중/저항 포함)
                   int atkCount = input.Skill?.Atk_Count ?? 1;
                   int maxStacks = baseEffect.MaxStacks > 0 ? baseEffect.MaxStacks : 99;
                   double expectedStacks = CalcExpectedStacks(input, effectToUse, atkCount, maxStacks);

                   // 상태이상 결과 저장 (성공/실패 무관하게 저장!)
                   result.StatusEffectResults.Add(new StatusEffectResult
                   {
                       Name = baseEffect.Name,
                       ApplyChance = applyChance * 100,
                       ExpectedStacks = expectedStacks,
                       MaxStacks = maxStacks,
                       IsForced = input.ForceStatusEffect
                   });

                   if (expectedStacks <= 0) continue;  // 피해 계산은 스킵

                   // ★ HP 전환 처리 (피해 아님)
                    if (baseEffect.IsHpConversion)
                    {
                        double conversionRatio = (effectToUse.CustomHpConversionRatio ?? 0) / 100.0;

                        if (conversionRatio > 0 && input.TargetCurrentHp > 0)
                        {
                            double newHp = input.TargetCurrentHp * conversionRatio;
                            double hpLoss = input.TargetCurrentHp - newHp;

                            result.HpConversionValue = newHp;
                            result.HpConversionLoss = hpLoss;
                            result.HasHpConversion = true;

                            result.BonusDamageDetails["HP전환"] = hpLoss;
                        }

                        continue;  // 피해 계산 스킵
                    }

                    // 커스텀 값 또는 기본값
                    double atkRatio = (effectToUse.CustomAtkRatio ?? baseEffect.AtkRatio) / 100.0;
                    double hpRatio = (effectToUse.CustomHpRatio ?? baseEffect.TargetMaxHpRatio) / 100.0;
                    double currentHpRatio = baseEffect.TargetCurrentHpRatio / 100.0;
                    double atkCap = (effectToUse.CustomAtkCap ?? baseEffect.AtkCap) / 100.0;
                    double armorPen = (effectToUse.CustomArmorPen ?? baseEffect.ArmorPen) / 100.0;
                    int stacks = effectToUse.Stacks > 0 ? effectToUse.Stacks : (baseEffect.MaxStacks > 0 ? baseEffect.MaxStacks : 1);

                    double damage = 0;

                    // ★ 소모형 효과 (폭탄 폭파, 출혈 폭발 등)
                    if (baseEffect.ConsumeType.HasValue && baseEffect.MaxConsume > 0)
                    {
                        int consumeCount = Math.Min(input.TargetStackCount, baseEffect.MaxConsume);
                        if (consumeCount <= 0) continue;

                        // 방무 적용
                        double effectiveAtkOverDef = atkOverDef;
                        if (armorPen > 0)
                        {
                            double newArmorPen = Math.Min(input.ArmorPen / 100.0 + armorPen, 1.0);
                            double defCoef = CalcDefCoefficientSimple(input, newArmorPen);
                            effectiveAtkOverDef = input.FinalAtk / defCoef;
                        }

                        // 남은 턴 (기본값 1)
                        int remainingTurns = baseEffect.DefaultRemainingTurns > 0 ? baseEffect.DefaultRemainingTurns : 1;

                        // 피해 = 공격력비례 × 남은 턴 × 스택 수
                        // 출혈 폭발: 120% × 2턴 × 2스택 = 480%
                        damage = effectiveAtkOverDef * atkRatio * remainingTurns * consumeCount;
                    }
                    else
                    {
                        // 기존 로직: 공격력 비례 (화상, 감전, 석화 등)
                        if (atkRatio > 0)
                        {
                            double effectiveAtkOverDef = atkOverDef;
                            if (armorPen > 0)
                            {
                                double newArmorPen = Math.Min(input.ArmorPen / 100.0 + armorPen, 1.0);
                                double defCoef = CalcDefCoefficientSimple(input, newArmorPen);
                                effectiveAtkOverDef = input.FinalAtk / defCoef;
                            }
                            damage += effectiveAtkOverDef * atkRatio * expectedStacks;
                        }

                        // 대상 최대 HP 비례 (빙결, 빙극, 마력역류 등)
                        if (hpRatio > 0 && input.TargetHp > 0)
                        {
                            double hpDamage = input.TargetHp * hpRatio;
                            if (atkCap > 0)
                            {
                                hpDamage = Math.Min(hpDamage, input.FinalAtk * atkCap);
                            }
                            damage += hpDamage * expectedStacks;
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
        /// 상태이상 적용 확률 계산
        /// </summary>
        private double CalcStatusEffectChance(DamageInput input, SkillStatusEffect effect)
        {
            // 체크박스 강제 적용
            if (input.ForceStatusEffect)
                return 1.0;
            
            // 기본 확률 (SkillStatusEffect.Chance 사용)
            double baseChance = effect.Chance / 100.0;
            
            // 효과 저항 = 기본 저항 - 저항 감소
            double effectiveEffRes = Math.Max(0, input.TargetEffRes - input.EffResReduction);

            // 효과 적중/저항 반영
            double effModifier = 1 + (input.EffHit - effectiveEffRes) / 100.0;

            double actualChance = baseChance * effModifier;
            return Math.Clamp(actualChance, 0, 1);
        }

        private static Random _random = new Random();

        /// <summary>
        /// 독립시행 기대 스택 계산 (효과 적중/저항 반영)
        /// </summary>
        private double CalcExpectedStacks(DamageInput input, SkillStatusEffect effect, int atkCount, int maxStacks)
        {
            int stacksPerHit = effect.Stacks > 0 ? effect.Stacks : 1;  // 한 번만 선언!

            // 강제 적용
            if (input.ForceStatusEffect)
            {
                int maxPossible = atkCount * stacksPerHit;
                return Math.Min(maxPossible, maxStacks);
            }

            // 확률 계산
            double applyChance = CalcStatusEffectChance(input, effect);
            if (applyChance <= 0) return 0;

            int successCount = 0;

            // 각 타격마다 확률 굴리기
            for (int i = 0; i < atkCount; i++)
            {
                double roll = _random.NextDouble();
                if (roll < applyChance)
                {
                    successCount += stacksPerHit;
                }
            }

            // 최대 스택 제한
            return Math.Min(successCount, maxStacks);
        }

        /// <summary>
        /// HP 비례 피해 계산 - 스킬
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
        /// 잃은 HP 비례 피해 증가 계산
        /// 체크 시 최대치 적용 (대상 HP 30% 이하 가정)
        /// </summary>
        private double CalcLostHpBonusDmg(DamageInput input, SkillLevelData levelData)
        {
            if (levelData == null || levelData.LostHpBonusDmgMax <= 0) return 0;

            // 체력조건 체크 시 최대치 적용
            if (input.IsLostHpConditionMet)
            {
                return levelData.LostHpBonusDmgMax;
            }

            return 0;
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
        /// Boss: 보스피해 포함
        /// Mob: 보스피해 제외
        /// PvP: 보스피해 제외
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

            // 보스 피해 (보스전만 적용)
            double bossDmg = input.Mode == BattleMode.Boss ? input.DmgDealtBoss : 0;

            // 피해 증가 합계
            double increase = input.DmgDealt + bossDmg + input.DmgTakenIncrease 
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
                baseHeal = input.FinalAtk * (levelData.HealAtkRatio / 100.0);
                result.HealSource = "공격력";
            }
            else if (levelData.HealDefRatio > 0)
            {
                baseHeal = input.FinalDef * (levelData.HealDefRatio / 100.0);
                result.HealSource = "방어력";
            }
            else if (levelData.HealHpRatio > 0)
            {
                baseHeal = input.FinalHp * (levelData.HealHpRatio / 100.0);
                result.HealSource = "최대체력";
            }

            if (baseHeal > 0)
            {
                result.HealAmount = baseHeal * (1 - input.HealReduction / 100.0);
            }
        }

        /// <summary>
        /// 협공 피해 계산
        /// </summary>
        private void CalcCoopDamage(DamageInput input, DamageResult result)
        {
            result.CoopDamage = 0;
            result.CoopHpDamage = 0;

            // Character에서 협공 데이터 가져오기
            if (input.Character?.Passive == null) return;

            var passiveData = input.Character.Passive.GetLevelData(input.IsSkillEnhanced);
            var coopAttack = passiveData?.CoopAttack;
            if (coopAttack == null) return;

            // 스킬 피해
            if (coopAttack.Ratio > 0)
            {
                double atkOverDef = input.FinalAtk / result.DefCoefficient;
                result.CoopDamage = atkOverDef * (coopAttack.Ratio / 100.0) * result.DamageMultiplier;
            }

            // HP 비례 피해
            if (coopAttack.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                double hpDamage = input.TargetHp * (coopAttack.TargetMaxHpRatio / 100.0);

                // 공격력 제한
                if (coopAttack.AtkCap > 0)
                {
                    double cap = input.FinalAtk * (coopAttack.AtkCap / 100.0);
                    hpDamage = Math.Min(hpDamage, cap);
                }

                result.CoopHpDamage = hpDamage * result.DamageMultiplier;
            }

            // 타수 적용
            result.CoopDamage *= coopAttack.AtkCount;
            result.CoopHpDamage *= coopAttack.AtkCount;
        }

        /// <summary>
        /// 축복 피해 제한 적용
        /// </summary>
        private double ApplyBlessing(double damage, DamageInput input, DamageResult result)
        {
            result.DamageBeforeBlessing = damage;
            result.BlessingApplied = false;

            if (input.TargetBlessing <= 0 || input.TargetHp <= 0)
                return damage;

            double maxDamage = input.TargetHp * (input.TargetBlessing / 100.0);

            if (damage > maxDamage)
            {
                result.BlessingApplied = true;
                return maxDamage;
            }

            return damage;
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

            // 상태이상 결과 생성
            var statusBuilder = new StringBuilder();
            if (result.StatusEffectResults != null && result.StatusEffectResults.Count > 0)
            {
                statusBuilder.Append("\n\n🔥 상태이상");
                statusBuilder.Append("\n───────────────────────────────────────────────────");

                foreach (var se in result.StatusEffectResults)
                {
                    if (se.IsForced)
                    {
                        statusBuilder.Append($"\n  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 (강제 적용)");
                    }
                    else if (se.ExpectedStacks > 0)
                    {
                        statusBuilder.Append($"\n  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 성공! ({se.ApplyChance:N0}% 확률)");
                    }
                    else
                    {
                        statusBuilder.Append($"\n  ✗ {se.Name}: 실패 ({se.ApplyChance:N0}% 확률)");
                    }
                }

                // 상태이상 피해 상세
                if (result.BonusDamageDetails != null && result.BonusDamageDetails.Count > 0)
                {
                    statusBuilder.Append("\n  [피해]");
                    foreach (var kvp in result.BonusDamageDetails)
                    {
                        statusBuilder.Append($"\n  ├ {kvp.Key}: {kvp.Value:N0}");
                    }
                    statusBuilder.Append($"\n  └ 총 상태이상 피해: {result.StatusDamage:N0}");
                }
            }
            string statusInfo = statusBuilder.ToString();

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

            string criBonusInfo = result.CriBonusDmg > 0
                ? $"\n  ├ 치명타 추가: {result.CriBonusDmg:N0}"
                : "";

            string atkCountInfo = result.AtkCount > 1
                ? $"\n  └ {result.AtkCount}타 = {result.DamagePerHit:N0} × {result.AtkCount}"
                : "";

            string healInfo = result.HealAmount > 0
                ? $"\n\n💚 회복량: {result.HealAmount:N0} ({result.HealSource} 기준)"
                : "";

            string consumeExtraInfo = result.ConsumeExtraDmg > 0
                ? $"\n  ├ 스택 소모 추가: {result.ConsumeExtraDmg:N0}"
                : "";
            
            string blessingInfo = "";
                if (result.BlessingApplied)
                {
                    blessingInfo = $"\n\n🛡️ 축복: {result.DamageBeforeBlessing:N0} → {result.DamagePerHit:N0} (HP {input.TargetBlessing}% 제한)";
                }

            string healFromDmgInfo = result.HealFromDamage > 0
                ? $"\n\n💚 피해 흡수: {result.HealFromDamage:N0}"
                : "";
            
            // 협공 결과
            string coopInfo = "";
            double totalCoopDmg = result.CoopDamage + result.CoopHpDamage;
            if (totalCoopDmg > 0)
            {
                coopInfo = $@"
            
            ⚔️ 협공 피해: {totalCoopDmg:N0}
              ├ 스킬: {result.CoopDamage:N0}
              └ HP비례: {result.CoopHpDamage:N0}";
            }

    return $@"═══════════════════════════════════════════════════
🎯 PVE (보스전)
═══════════════════════════════════════════════════
💥 스킬 데미지: {result.SkillDamage:N0}{blockInfo}{extraInfo}{wekBonusInfo}{criBonusInfo}{consumeExtraInfo}{atkCountInfo}{coopInfo}{blessingInfo}{healFromDmgInfo}
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
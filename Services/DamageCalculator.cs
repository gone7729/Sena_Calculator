using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services
{
    public class DamageCalculator
    {
        #region Input/Output 클래스

        public class DamageInput
        {
            // ===== 캐릭터/스킬 =====
            public Character Character { get; set; }
            public Skill Skill { get; set; }
            public bool IsSkillEnhanced { get; set; }
            public int TranscendLevel { get; set; }

            // ===== 최종 스탯 =====
            public double FinalAtk { get; set; }
            public double FinalDef { get; set; }
            public double FinalHp { get; set; }
            public double CritDamage { get; set; }
            public double DmgDealt { get; set; }
            public double DmgDealtType { get; set; }
            public double DmgDealtBoss { get; set; }
            public double ArmorPen { get; set; }
            public double WeakpointDmg { get; set; }
            public double WeakpointDmgBuff { get; set; }
            public double Dmg1to3 { get; set; }
            public double Dmg4to5 { get; set; }

            // ===== 디버프 =====
            public double DefReduction { get; set; }
            public double DmgTakenIncrease { get; set; }
            public double Vulnerability { get; set; }
            public double BossVulnerability { get; set; }
            public double EffResReduction { get; set; }
            public double HealReduction { get; set; }

            // ===== 보스/대상 정보 =====
            public double BossDef { get; set; }
            public double BossDefIncrease { get; set; }
            public double BossDmgReduction { get; set; }
            public double BossTargetReduction { get; set; }
            public double BossHp { get; set; }
            public double TargetHp { get; set; }
            public double TargetCurrentHp { get; set; }

            // ===== 전투 옵션 =====
            public bool IsCritical { get; set; }
            public bool IsWeakpoint { get; set; }

            // 확률 기반 기댓값 크리/약점 (true면 100% 가정 대신 실제 확률로 기대 계수 계산).
            // 메인옵·부옵 탐색(치확/약확)이 의미를 가지려면 이 모드를 써야 함.
            public bool ExpectedCritWeak { get; set; }
            public double CritChance { get; set; }   // 치명타확률% (0~100+, 100 초과는 100으로 캡)
            public double WeakChance { get; set; }   // 약점공격확률%
            public bool IsBlocked { get; set; }
            public bool IsSkillConditionMet { get; set; }
            public bool IsLostHpConditionMet { get; set; }
            // 시즈: 대상 실제 잔여HP%(0~100). 지정 시 잃은HP 보너스를 이 값으로 비례 계산(LostHpAssumedRemaining 대신).
            public double? LostHpActualRemainingPct { get; set; }

            // ===== 상태이상 =====
            public double EffHit { get; set; }
            public double TargetEffRes { get; set; }
            public bool ForceStatusEffect { get; set; }
            public int TargetStackCount { get; set; } = 0;

            // ===== 축복/버프 =====
            public double BlessingCap { get; set; }
            public double SelfMaxHp { get; set; }
            public double TargetBlessing { get; set; }
            public double AtkBuff { get; set; }

            // ===== 모드 =====
            public BattleMode Mode { get; set; } = BattleMode.Boss;
            public bool IsTargetBoss { get; set; } = true;

            // ===== 스택소모 스킬용 =====
            // 자버프 타입피증 (스택소모 시에만 적용, 스킬피해에는 미적용)
            public double SelfBuffTypeDmg { get; set; }

            // ===== 적 디버프당 동적 피증 (PerEnemyDebuffDmgBonus) =====
            // 적이 보유한 디버프(상태이상 포함) 개수
            public int TargetDebuffCount { get; set; }
            // 디버프 1개당 피증% (패시브+스킬 합산값)
            public double PerDebuffBonusPercent { get; set; }
            // 카운트 상한 (모든 효과 중 최대)
            public int PerDebuffBonusMaxStacks { get; set; }
        }

        public class DamageResult
        {
            public double FinalAtk { get; set; }
            public double TotalArmorPen { get; set; }
            public double EffectiveBossDef { get; set; }
            public double DefCoefficient { get; set; }

            public double SkillRatio { get; set; }
            public double CritMultiplier { get; set; }
            public double WeakpointMultiplier { get; set; }
            public double DamageMultiplier { get; set; }
            public double SkillDmgMultiplier { get; set; } // 스킬피해용 (자버프 타입피증 제외)
            public double ExtraDmgMultiplier { get; set; } // 조건부 추가피해용 (n인기피증 제외)
            public double LostHpMultiplier { get; set; } = 1.0; // 잃은HP 보너스 배수 (스택소모에서는 제거하여 적용)

            public int AtkCount { get; set; }

            public double BaseDamage { get; set; }
            public double ExtraDamage { get; set; }
            public double WekBonusDmg { get; set; }
            public double CriBonusDmg { get; set; }
            public double DamagePerHit { get; set; }
            public double ConsumeExtraDmg { get; set; }

            public double FinalDamage { get; set; }
            public double SkillDamage { get; set; }
            public double StatusDamage { get; set; }
            public double BonusDamage { get; set; }
            public double HpRatioDamage { get; set; }

            public double CoopDamage { get; set; }
            public double CoopHpDamage { get; set; }
            public bool CoopTriggered { get; set; }
            public double CoopChance { get; set; }

            public double MarkDamage { get; set; }
            public double MarkHpDamage { get; set; }

            public double HealAmount { get; set; }
            public string HealSource { get; set; }
            public double HealFromDamage { get; set; }

            public double DamageBeforeBlessing { get; set; }
            public bool BlessingApplied { get; set; }

            public double HpConversionValue { get; set; }
            public double HpConversionLoss { get; set; }
            public bool HasHpConversion { get; set; }

            public Dictionary<string, double> BonusDamageDetails { get; set; } = new();
            public List<StatusEffectResult> StatusEffectResults { get; set; } = new();
            public string Details { get; set; }

            // ===== 디버깅용 =====
            public StringBuilder DebugLog { get; set; } = new();
            public bool WriteDebugFile { get; set; }
        }

        public class StatusEffectResult
        {
            public string Name { get; set; }
            public double ApplyChance { get; set; }
            public double ExpectedStacks { get; set; }
            public int MaxStacks { get; set; }
            public bool IsForced { get; set; }
        }

        #endregion

        #region 메인 계산

        public DamageResult Calculate(DamageInput input, bool writeDebugFile = false)
        {
            var result = new DamageResult { FinalAtk = input.FinalAtk };
            result.WriteDebugFile = writeDebugFile;
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            var skillBonus = input.Skill?.GetTotalBonus(input.IsSkillEnhanced, input.TranscendLevel) ?? new BuffSet();
            // 초월 배율 오버라이드(선언값 = 최종값). 예: 에반 연속공격 2초월 물리 170%/방어 200%.
            var txOverride = input.Skill?.GetTranscendBonus(input.TranscendLevel);
            double effRatio = txOverride?.RatioOverride ?? levelData?.Ratio ?? 100;
            // (방어비례는 CalcBaseDamage에서 별도 계산 — 그쪽 스코프에서 초월 오버라이드 적용)

            // ===== 디버깅 로그 =====
            result.DebugLog.AppendLine("══════════ 데미지 계산 디버그 ══════════");
            result.DebugLog.AppendLine($"[입력] 캐릭터: {input.Character?.Name}, 스킬: {input.Skill?.Name}");
            result.DebugLog.AppendLine($"[입력] 스강: {input.IsSkillEnhanced}, 초월: {input.TranscendLevel}");

            // 1. 스킬 발동 전 버프
            ApplyPreCastBuff(input, levelData, result);
            result.AtkCount = input.Skill?.GetAtkCount(input.IsSkillEnhanced, input.TranscendLevel) ?? 1;
            result.DebugLog.AppendLine($"\n[2] PreCast 후 공격력: {result.FinalAtk:N0}, 타수: {result.AtkCount}");

            // 2. 스킬 배율
            result.SkillRatio = effRatio / 100.0;
            result.DebugLog.AppendLine($"[3] 스킬배율: {effRatio}% = {result.SkillRatio:F4}x");

            // 3. 방어 관통
            double inputArmorPen = input.ArmorPen;
            double skillArmorPen = skillBonus.Arm_Pen;
            result.TotalArmorPen = Math.Min((inputArmorPen + skillArmorPen) / 100.0, 1.0);
            result.DebugLog.AppendLine($"[4] 방무: 입력{inputArmorPen}% + 스킬{skillArmorPen}% = {result.TotalArmorPen * 100:F2}%");

            // 4. 방어 계수
            CalcDefenseCoefficient(input, levelData, result);
            result.DebugLog.AppendLine($"[5] 방어계수: 보스방{input.BossDef:N0} × (1+{input.BossDefIncrease}-{input.DefReduction})% × (1-{result.TotalArmorPen:F2})");
            result.DebugLog.AppendLine($"    실효방어: {result.EffectiveBossDef:N2}, 방어계수: {result.DefCoefficient:F4}");

            // 5. 치명타 계수 (기댓값 모드면 확률로 가중)
            double inputCritDmg = input.CritDamage;
            double skillCritDmg = skillBonus.Cri_Dmg;
            double critFull = (inputCritDmg + skillCritDmg) / 100.0;
            // 스킬 자체 내장 치확/약확(예: 파스칼 파괴의거인 6초월 확정치명 Cri=100)도 발동 확률에 합산.
            //   안 그러면 기댓값 모드가 이미 확정인 치확/약확을 과소평가 → 옵티마이저가 치확/약확 기어를 낭비 배분.
            double effCritChance = input.CritChance + skillBonus.Cri;
            double effWeakChance = input.WeakChance + skillBonus.Wek;
            if (!input.IsCritical)
                result.CritMultiplier = 1.0;
            else if (input.ExpectedCritWeak)
            {
                double pc = System.Math.Min(1.0, System.Math.Max(0, effCritChance) / 100.0);
                result.CritMultiplier = 1.0 + pc * (critFull - 1.0);
            }
            else result.CritMultiplier = critFull;
            result.DebugLog.AppendLine($"[6] 치명계수: {(input.ExpectedCritWeak ? $"기댓값(확률{effCritChance}%)" : input.IsCritical ? "발동" : "미발동")} → {result.CritMultiplier:F4}x");

            // 6. 약점 계수 (기댓값 모드면 확률로 가중). 약피% / 100 곱연산.
            double weakFull = (input.WeakpointDmg + input.WeakpointDmgBuff) / 100.0;
            if (!input.IsWeakpoint)
                result.WeakpointMultiplier = 1.0;
            else if (input.ExpectedCritWeak)
            {
                double pw = System.Math.Min(1.0, System.Math.Max(0, effWeakChance) / 100.0);
                result.WeakpointMultiplier = 1.0 + pw * (weakFull - 1.0);
            }
            else result.WeakpointMultiplier = weakFull;
            result.DebugLog.AppendLine($"[7] 약점계수: {(input.ExpectedCritWeak ? $"기댓값(확률{effWeakChance}%)" : input.IsWeakpoint ? "발동" : "미발동")} → {result.WeakpointMultiplier:F4}x");

            // 7. 피해 증가 계수
            result.DamageMultiplier = CalcDamageMultiplier(input, levelData, result);

            // 8. 잃은 HP 비례 피해 증가 (스택소모에는 미적용 — LostHpMultiplier로 분리 보관)
            double lostHpBonus = CalcLostHpBonusDmg(input, levelData);
            if (lostHpBonus > 0)
            {
                double lostHpMul = 1 + lostHpBonus / 100.0;
                result.LostHpMultiplier = lostHpMul;
                result.DebugLog.AppendLine($"[9] 잃은HP 보너스: +{lostHpBonus}% (×{lostHpMul:F4})");
                result.DamageMultiplier *= lostHpMul;
                result.SkillDmgMultiplier *= lostHpMul;
            }

            // 9. 기본 데미지
            double atkOverDef = result.FinalAtk / result.DefCoefficient;
            result.DebugLog.AppendLine($"\n[10] 기본데미지: 공{result.FinalAtk:N0} / 방계수{result.DefCoefficient:F4} = {atkOverDef:N2}");
            CalcBaseDamage(input, levelData, atkOverDef, result);

            // 10. 추가 피해
            CalcExtraDamage(input, levelData, skillBonus, atkOverDef, result);

            // 11. HP 비례 피해
            result.HpRatioDamage = CalcHpRatioDamage(input, levelData, result);

            // 12. 스택 소모형 추가 피해
            CalcConsumeExtraDamage(input, levelData, atkOverDef, result);

            // 13. 1타당 데미지
            result.DamagePerHit = result.BaseDamage + result.ExtraDamage + result.WekBonusDmg + result.CriBonusDmg;
            result.DebugLog.AppendLine($"\n[14] 1타당합계: 기본{result.BaseDamage:N0} + 조건{result.ExtraDamage:N0} + 약추{result.WekBonusDmg:N0} + 치추{result.CriBonusDmg:N0} = {result.DamagePerHit:N0}");

            // 14. 별도 피해
            CalcBonusDamage(input, levelData, atkOverDef, result);

            // 15. 막기
            ApplyBlock(input, result);

            // 16. 축복
            result.DamagePerHit = ApplyBlessing(result.DamagePerHit, input, result);

            // 17. 최종 데미지
            double vulnerabilityTotal = 1 + (input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease) / 100;
            result.SkillDamage = result.DamagePerHit * result.AtkCount;
            result.StatusDamage = result.BonusDamage * result.AtkCount;
            result.FinalDamage = (result.SkillDamage + result.HpRatioDamage + result.ConsumeExtraDmg) * vulnerabilityTotal;

            result.DebugLog.AppendLine($"\n[17] 최종계산:");
            result.DebugLog.AppendLine($"    취약계수: 1 + ({input.Vulnerability} + {input.BossVulnerability} + {input.DmgTakenIncrease})/100 = {vulnerabilityTotal:F4}x");
            result.DebugLog.AppendLine($"    스킬피해: {result.DamagePerHit:N0} × {result.AtkCount}타 = {result.SkillDamage:N0}");
            result.DebugLog.AppendLine($"    상태이상: {result.StatusDamage:N0}, HP비례: {result.HpRatioDamage:N0}, 스택소모: {result.ConsumeExtraDmg:N0}");
            result.DebugLog.AppendLine($"    취약전합계: {result.SkillDamage + result.HpRatioDamage + result.ConsumeExtraDmg:N0}");
            result.DebugLog.AppendLine($"    ★ 최종: {result.FinalDamage:N0}");

            // 18~20
            CalcHeal(input, levelData, result);
            if (levelData?.HealDmgRatio > 0)
                result.HealFromDamage = result.FinalDamage * (levelData.HealDmgRatio / 100.0);
            CalcCoopDamage(input, result);
            CalcMarkDamage(input, result);

            // 21. 상세 정보
            result.Details = GenerateDetails(input, result);

            // ===== 디버그 출력 (첫번째 계산만) =====
            if (result.WriteDebugFile)
            {
                System.Diagnostics.Debug.WriteLine(result.DebugLog.ToString());
                try { System.IO.File.WriteAllText("damage_debug.txt", result.DebugLog.ToString(), Encoding.UTF8); } catch { }
            }

            return result;
        }

        #endregion

        #region 방어/방무 계산

        private void CalcDefenseCoefficient(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            double skillDefReduction = levelData?.DispelDefReduction ?? 0;
            double effectiveDefReduction = Math.Max(input.DefReduction, skillDefReduction);
            double originalDefReduction = input.DefReduction;
            input.DefReduction = effectiveDefReduction;
            result.DefCoefficient = CalcDefCoefficient(input, result.TotalArmorPen, out double effectiveDef);
            result.EffectiveBossDef = effectiveDef;
            input.DefReduction = originalDefReduction;
        }

        private double CalcDefCoefficient(DamageInput input, double armorPen, out double effectiveDef)
        {
            const double DEF_CONSTANT = 467.0;
            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            effectiveDef = input.BossDef * defModifier * armorPenModifier;
            return 1 + effectiveDef / DEF_CONSTANT;
        }

        private double CalcDefCoefficientSimple(DamageInput input, double armorPen)
        {
            const double DEF_CONSTANT = 467.0;
            const double THRESHOLD = 3125.0;
            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            double effectiveDef = input.BossDef * defModifier * armorPenModifier;

            if (effectiveDef <= THRESHOLD)
                return 1 + effectiveDef / DEF_CONSTANT;

            double rate1 = DEF_CONSTANT / (DEF_CONSTANT + effectiveDef);
            double rate2 = DEF_CONSTANT / (DEF_CONSTANT + THRESHOLD);
            return 1 / ((rate1 + rate2) / 2);
        }

        #endregion

        #region 피해 배율 계산

        private double CalcDamageMultiplier(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            var debugLog = result.DebugLog;
            debugLog.AppendLine($"\n[8] 피증계수 계산 (합연산):");

            // 조건부 피증
            double conditionalDmgBonus = 0;
            if (input.Skill != null)
            {
                var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
                conditionalDmgBonus = skillTranscend.ConditionalDmgBonus;
            }

            // 적 디버프당 동적 피증 (PerEnemyDebuffDmgBonus)
            double perDebuffBonus = 0;
            if (input.PerDebuffBonusPercent > 0 && input.PerDebuffBonusMaxStacks > 0 && input.TargetDebuffCount > 0)
            {
                int effectiveCount = Math.Min(input.TargetDebuffCount, input.PerDebuffBonusMaxStacks);
                perDebuffBonus = input.PerDebuffBonusPercent * effectiveCount;
                conditionalDmgBonus += perDebuffBonus;
            }

            // 인기 피증
            double targetTypeDmg = 0;
            int targetCount = 0;
            if (input.Skill != null)
            {
                targetCount = input.Skill.GetTargetCount(input.IsSkillEnhanced, input.TranscendLevel);
                targetTypeDmg = (targetCount >= 1 && targetCount <= 3) ? input.Dmg1to3 : (targetCount >= 4) ? input.Dmg4to5 : 0;
            }

            // 보스 피증
            double bossDmg = input.IsTargetBoss ? input.DmgDealtBoss : 0;

            // 피감
            double reductionTotal = input.BossDmgReduction + input.BossTargetReduction;

            // ===== 합연산 방식 =====
            // 모든 피증을 합산 후 한번에 계산
            double totalDmgBonus = input.DmgDealt + input.DmgDealtType + conditionalDmgBonus + bossDmg + targetTypeDmg;
            double multiplier = 1 + (totalDmgBonus - reductionTotal) / 100.0;

            debugLog.AppendLine($"    기본피증: {input.DmgDealt}%");
            debugLog.AppendLine($"    타입피증: {input.DmgDealtType}%");
            debugLog.AppendLine($"    조건부: {conditionalDmgBonus}%"
                + (perDebuffBonus > 0
                    ? $" (디버프당 {input.PerDebuffBonusPercent}% × {Math.Min(input.TargetDebuffCount, input.PerDebuffBonusMaxStacks)}개 = {perDebuffBonus}% 포함)"
                    : ""));
            debugLog.AppendLine($"    보스피증: {bossDmg}%");
            debugLog.AppendLine($"    {targetCount}인기피증: {targetTypeDmg}%");
            debugLog.AppendLine($"    피감합계: {reductionTotal}% (보스{input.BossDmgReduction}% + 인기{input.BossTargetReduction}%)");
            debugLog.AppendLine($"    ★ 피증합계: {totalDmgBonus}% - {reductionTotal}% = {totalDmgBonus - reductionTotal}%");
            debugLog.AppendLine($"    ★ 피증계수: 1 + {totalDmgBonus - reductionTotal}/100 = {multiplier:F4}x");

            // ===== 스택소모 스킬용: 스킬피해 배율 분리 =====
            // 스택을 소모하는 스킬(ConsumeExtra)은 자버프 타입피증이 스킬피해에는 미적용
            result.SkillDmgMultiplier = multiplier;
            if (levelData?.ConsumeExtra != null && input.SelfBuffTypeDmg > 0)
            {
                // 스킬피해에서 자버프 타입피증 제외
                double skillDmgBonus = totalDmgBonus - input.SelfBuffTypeDmg;
                result.SkillDmgMultiplier = 1 + (skillDmgBonus - reductionTotal) / 100.0;
                debugLog.AppendLine($"\n    [스택소모] 자버프 타입피증 {input.SelfBuffTypeDmg}% 분리:");
                debugLog.AppendLine($"    스킬피해용 피증: {skillDmgBonus}% → {result.SkillDmgMultiplier:F4}x");
                debugLog.AppendLine($"    스택소모용 피증: {totalDmgBonus}% → {multiplier:F4}x (자버프 포함)");
            }

            // ===== 조건부 추가피해용 =====
            result.ExtraDmgMultiplier = multiplier;

            return multiplier;
        }

        private double CalcLostHpBonusDmg(DamageInput input, SkillLevelData levelData)
        {
            if (levelData == null || levelData.LostHpBonusDmgMax <= 0) return 0;
            if (!input.IsLostHpConditionMet) return 0;
            // 시즈 등: 대상 실제 잔여HP%가 주어지면 그 값으로 비례 (음수HP 스펀지면 0% 잔여 → 풀 보너스)
            if (input.LostHpActualRemainingPct.HasValue)
            {
                double rem = System.Math.Max(0, System.Math.Min(100, input.LostHpActualRemainingPct.Value));
                return levelData.LostHpBonusDmgMax * (100.0 - rem) / 100.0;
            }
            // 잔여HP% 기준이 있으면 비례 계산 (예: 30% 남음 → 70% 손실 → 50% × 0.7 = 35%)
            if (levelData.LostHpAssumedRemaining > 0)
            {
                double lostPct = (100.0 - levelData.LostHpAssumedRemaining) / 100.0;
                return levelData.LostHpBonusDmgMax * lostPct;
            }
            return levelData.LostHpBonusDmgMax;
        }

        #endregion

        #region 기본/추가 피해 계산

        private void ApplyPreCastBuff(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            // 물리/마법 공격력% 합산 (한 캐릭터는 둘 중 하나만 보유하므로 안전)
            double preCastAtkBonus = (levelData?.PreCastBuff?.Atk_Rate ?? 0) + (levelData?.PreCastBuff?.MagicAtk_Rate ?? 0);
            if (preCastAtkBonus > input.AtkBuff)
            {
                double additionalBonus = preCastAtkBonus - input.AtkBuff;
                result.FinalAtk = input.FinalAtk * (1 + additionalBonus / 100.0);
            }
            else
            {
                result.FinalAtk = input.FinalAtk;
            }
        }

        private void CalcBaseDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
        {
            double atkDamage = atkOverDef * result.SkillRatio;
            double fixedDamage = levelData?.FixedDamage ?? 0;
            double defDamage = 0;
            double hpDamage = 0;

            // 초월 방어비례 오버라이드(에반·유진호 2초월 방어 200%) 우선, 없으면 레벨 선언값.
            double defRatio = input.Skill?.GetTranscendBonus(input.TranscendLevel)?.DefRatioOverride
                              ?? levelData?.DefRatio ?? 0;
            if (defRatio > 0 && input.FinalDef > 0)
            {
                double defOverDef = input.FinalDef / result.DefCoefficient;
                defDamage = defOverDef * (defRatio / 100.0);
            }

            if (levelData?.HpRatio > 0 && input.FinalHp > 0)
            {
                double hpOverDef = input.FinalHp / result.DefCoefficient;
                hpDamage = hpOverDef * (levelData.HpRatio / 100.0);
            }

            double rawDamage = atkDamage + defDamage + hpDamage;
            // 스킬피해에는 SkillDmgMultiplier 사용 (스택소모 스킬은 자버프 타입피증 제외됨)
            result.BaseDamage = rawDamage * result.CritMultiplier * result.WeakpointMultiplier * result.SkillDmgMultiplier + fixedDamage;

            result.DebugLog.AppendLine($"    공격력피해: {atkOverDef:N2} × {result.SkillRatio:F4} = {atkDamage:N2}");
            if (defDamage > 0) result.DebugLog.AppendLine($"    방어력피해: {defDamage:N2}");
            if (hpDamage > 0) result.DebugLog.AppendLine($"    HP피해: {hpDamage:N2}");
            result.DebugLog.AppendLine($"    원시합계: {rawDamage:N2}");
            result.DebugLog.AppendLine($"    × 치명{result.CritMultiplier:F4} × 약점{result.WeakpointMultiplier:F4} × 피증{result.SkillDmgMultiplier:F4}");
            result.DebugLog.AppendLine($"    + 고정피해: {fixedDamage:N0}");
            result.DebugLog.AppendLine($"    = 기본데미지: {result.BaseDamage:N0}");
        }

        private void CalcExtraDamage(DamageInput input, SkillLevelData levelData, BuffSet skillBonus, double atkOverDef, DamageResult result)
        {
            result.DebugLog.AppendLine($"\n[11] 추가피해:");
            result.ExtraDamage = 0;

            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                // 조건부 추가피해: 방어계수 적용 (스킬 방무 포함)
                double extraRatio = levelData.ConditionalExtraDmg / 100.0;

                // 피증 구성요소 개별 출력 (소거법용)
                int targetCount = input.Skill?.GetTargetCount(input.IsSkillEnhanced, input.TranscendLevel) ?? 0;
                double targetTypeDmg = (targetCount >= 1 && targetCount <= 3) ? input.Dmg1to3 : (targetCount >= 4) ? input.Dmg4to5 : 0;
                double bossDmg = input.IsTargetBoss ? input.DmgDealtBoss : 0;
                result.DebugLog.AppendLine($"    [피증분석] 기본:{input.DmgDealt}% 타입:{input.DmgDealtType}% 보스:{bossDmg}% {targetCount}인기:{targetTypeDmg}%");
                result.DebugLog.AppendLine($"    [피증비교] 전체:{result.DamageMultiplier:F4} 스킬:{result.SkillDmgMultiplier:F4} 추가:{result.ExtraDmgMultiplier:F4}");

                double extraDmg = atkOverDef * extraRatio * result.SkillDmgMultiplier * result.CritMultiplier * result.WeakpointMultiplier;
                if (levelData.ConditionalExtraDmgPerHit) extraDmg *= result.AtkCount;
                result.ExtraDamage = extraDmg;
                result.DebugLog.AppendLine($"    조건부: 공/방{atkOverDef:N2} × 배율{extraRatio:F2} × 피증{result.SkillDmgMultiplier:F4} × 치명{result.CritMultiplier:F4} × 약점{result.WeakpointMultiplier:F4}");
                result.DebugLog.AppendLine($"           = {result.ExtraDamage:N0}");
            }

            if (levelData?.ConditionalExtraDmgSelfHpRatio > 0)
            {
                double selfHpExtraDmg = input.SelfMaxHp * (levelData.ConditionalExtraDmgSelfHpRatio / 100.0) * result.CritMultiplier * result.WeakpointMultiplier;
                result.ExtraDamage += selfHpExtraDmg;
                result.DebugLog.AppendLine($"    시전자HP비례: {selfHpExtraDmg:N0}");
            }

            // 조건부 추가피해(약점/치명 발동 시)는 발동 확률만큼만 기여.
            //   기댓값 모드(시뮬): 약점추가 = 약점확률 × 약점full, 치명추가 = 치명확률 × 치명full.
            //   비-기댓값(계산기 max-damage, IsWeakpoint/IsCritical=true): 기존대로 full 계수 그대로(무변경).
            double critFull = (input.CritDamage + skillBonus.Cri_Dmg) / 100.0;
            double weakFull = (input.WeakpointDmg + input.WeakpointDmgBuff) / 100.0;
            // 스킬 내장 치확/약확(확정치명 등) 합산 — 위 치명/약점 계수와 동일 기준.
            double pc = System.Math.Min(1.0, System.Math.Max(0, input.CritChance + skillBonus.Cri) / 100.0);
            double pw = System.Math.Min(1.0, System.Math.Max(0, input.WeakChance + skillBonus.Wek) / 100.0);

            result.WekBonusDmg = 0;
            result.DebugLog.AppendLine($"    [약점추가 조건] IsWeakpoint:{input.IsWeakpoint}, 기댓값:{input.ExpectedCritWeak}, WekBonusDmg:{skillBonus.WekBonusDmg}%");
            if (skillBonus.WekBonusDmg > 0 && (input.IsWeakpoint || input.ExpectedCritWeak))
            {
                // 약점추가는 약점 발동 시에만 → 기댓값 모드면 (약점확률 × 약점full), 치명은 독립(기댓값 계수).
                double weakFactor = input.ExpectedCritWeak ? pw * weakFull : result.WeakpointMultiplier;
                double wekBonus = atkOverDef * (skillBonus.WekBonusDmg / 100.0) * result.SkillDmgMultiplier * result.CritMultiplier * weakFactor;
                if (skillBonus.WekBonusDmgPerHit) wekBonus *= result.AtkCount;
                result.WekBonusDmg = wekBonus;
                result.DebugLog.AppendLine($"    약점추가: {result.WekBonusDmg:N0} (약점계수 {weakFactor:F3})");
            }

            result.CriBonusDmg = 0;
            if (skillBonus.CriBonusDmg > 0 && (input.IsCritical || input.ExpectedCritWeak))
            {
                double critFactor = input.ExpectedCritWeak ? pc * critFull : result.CritMultiplier;
                double criBonus = atkOverDef * (skillBonus.CriBonusDmg / 100.0) * result.SkillDmgMultiplier * critFactor * result.WeakpointMultiplier;
                if (skillBonus.CriBonusDmgPerHit) criBonus *= result.AtkCount;
                result.CriBonusDmg = criBonus;
                result.DebugLog.AppendLine($"    치명추가: {result.CriBonusDmg:N0} (치명계수 {critFactor:F3})");
            }
        }

        private void CalcConsumeExtraDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
        {
            result.ConsumeExtraDmg = 0;
            if (levelData?.ConsumeExtra == null) return;

            result.DebugLog.AppendLine($"\n[13] 스택소모:");
            var consumeExtra = levelData.ConsumeExtra;
            
            double totalHpRatio = consumeExtra.TargetMaxHpRatio;
            double totalAtkCap = consumeExtra.AtkCap;
            double totalAtkRatio = consumeExtra.AtkRatio;
            
            var transcendBonus = input.Skill?.GetTranscendBonus(input.TranscendLevel);
            if (transcendBonus?.ConsumeExtra != null)
            {
                totalHpRatio += transcendBonus.ConsumeExtra.TargetMaxHpRatio;
                totalAtkRatio += transcendBonus.ConsumeExtra.AtkRatio;
                totalAtkCap += transcendBonus.ConsumeExtra.AtkCap;
            }
            
            double damage = 0;
            if (totalHpRatio > 0 && input.TargetHp > 0)
            {
                damage = input.TargetHp * (totalHpRatio / 100.0);
                if (totalAtkCap > 0)
                {
                    double cap = input.FinalAtk * (totalAtkCap / 100.0);
                    damage = Math.Min(damage, cap);
                }
            }
            if (totalAtkRatio > 0)
                damage += atkOverDef * (totalAtkRatio / 100.0);

            // 스택소모 HP비례 피해: 피증/치명/약점 적용, 잃은HP 미적용.
            //   취약은 이 단계에선 빠지지만 최종조립(FinalDamage = (… + ConsumeExtraDmg) × 취약)에서 일괄 적용됨.
            double damageMulNoLostHp = result.DamageMultiplier / result.LostHpMultiplier;
            double fullMultiplier = damageMulNoLostHp * result.CritMultiplier * result.WeakpointMultiplier;
            result.ConsumeExtraDmg = damage * fullMultiplier;
            result.DebugLog.AppendLine($"    HP비례{totalHpRatio}% 공비례{totalAtkRatio}% 공제한{totalAtkCap}%");
            result.DebugLog.AppendLine($"    × 피증{damageMulNoLostHp:F4} × 치명{result.CritMultiplier:F4} × 약점{result.WeakpointMultiplier:F4}");
            result.DebugLog.AppendLine($"    = {result.ConsumeExtraDmg:N0} (취약·잃은HP 미적용)");
        }

        #endregion

        #region HP 비례 피해 계산

        private double CalcHpRatioDamage(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            if (levelData == null) return 0;
            
            result.DebugLog.AppendLine($"\n[12] HP비례:");
            double totalHpDamage = 0;

            if (levelData.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                double rawDamage = input.TargetHp * (levelData.TargetMaxHpRatio / 100.0);
                if (levelData.AtkCap > 0)
                    rawDamage = Math.Min(rawDamage, input.FinalAtk * (levelData.AtkCap / 100.0));
                totalHpDamage += rawDamage * result.DamageMultiplier * result.CritMultiplier * result.WeakpointMultiplier;
                result.DebugLog.AppendLine($"    최대HP: {input.TargetHp:N0} × {levelData.TargetMaxHpRatio}% = {totalHpDamage:N0}");
            }

            if (levelData.TargetCurrentHpRatio > 0 && input.TargetCurrentHp > 0)
            {
                double rawDamage = input.TargetCurrentHp * (levelData.TargetCurrentHpRatio / 100.0);
                if (levelData.AtkCap > 0)
                    rawDamage = Math.Min(rawDamage, input.FinalAtk * (levelData.AtkCap / 100.0));
                totalHpDamage += rawDamage * result.DamageMultiplier * result.CritMultiplier * result.WeakpointMultiplier;
            }

            return totalHpDamage;
        }

        #endregion

        #region 상태이상/별도 피해 계산

        private static Random _random = new Random();

        private void CalcBonusDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
        {
            result.BonusDamage = 0;
            result.BonusDamageDetails.Clear();
            result.StatusEffectResults.Clear();
            if (levelData == null) return;

            if (result.HpRatioDamage > 0)
            {
                result.BonusDamageDetails["HP비례"] = result.HpRatioDamage;
                result.BonusDamage += result.HpRatioDamage;
            }

            CalcStatusEffectDamage(input, levelData, atkOverDef, result);
        }

        private void CalcStatusEffectDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
        {
            var skillTranscend = input.Skill?.GetTranscendBonus(input.TranscendLevel);

            // base + 초월 상태이상을 타입별 병합 (초월이 같은 타입은 override, 없는 타입은 추가).
            // 이렇게 해야 ① 동일 상태이상 이중 적용 방지 ② 초월 전용 상태이상(생명력전환·중독 등) 누락 방지.
            var merged = new Dictionary<StatusEffectType, SkillStatusEffect>();
            if (levelData.StatusEffects != null)
                foreach (var e in levelData.StatusEffects) merged[e.Type] = e;
            if (skillTranscend?.StatusEffects != null)
                foreach (var e in skillTranscend.StatusEffects) merged[e.Type] = e;
            if (merged.Count == 0) return;

            foreach (var effectToUse in merged.Values)
            {
                var baseEffect = StatusEffectDb.Get(effectToUse.Type);
                if (baseEffect == null) continue;

                double applyChance = CalcStatusEffectChance(input, effectToUse);
                int atkCount = input.Skill?.GetAtkCount(input.IsSkillEnhanced, input.TranscendLevel) ?? 1;
                int maxStacks = baseEffect.MaxStacks > 0 ? baseEffect.MaxStacks : 99;
                double expectedStacks = CalcExpectedStacks(input, effectToUse, atkCount, maxStacks);

                result.StatusEffectResults.Add(new StatusEffectResult
                {
                    Name = baseEffect.Name,
                    ApplyChance = applyChance * 100,
                    ExpectedStacks = expectedStacks,
                    MaxStacks = maxStacks,
                    IsForced = input.ForceStatusEffect
                });

                if (expectedStacks <= 0) continue;

                if (baseEffect.IsHpConversion)
                {
                    CalcHpConversion(input, effectToUse, result);
                    continue;
                }

                double damage = CalcSingleStatusEffectDamage(input, effectToUse, baseEffect, atkOverDef, expectedStacks, result);
                if (damage > 0)
                    AddBonusDamageDetail(result, baseEffect.Name, damage);
            }
        }

        private double CalcSingleStatusEffectDamage(DamageInput input, SkillStatusEffect effectToUse, StatusEffect baseEffect, double atkOverDef, double expectedStacks, DamageResult result)
        {
            double atkRatio = (effectToUse.CustomAtkRatio ?? baseEffect.AtkRatio) / 100.0;
            double hpRatio = (effectToUse.CustomHpRatio ?? baseEffect.TargetMaxHpRatio) / 100.0;
            double currentHpRatio = baseEffect.TargetCurrentHpRatio / 100.0;
            double atkCap = (effectToUse.CustomAtkCap ?? baseEffect.AtkCap) / 100.0;
            double armorPen = (effectToUse.CustomArmorPen ?? baseEffect.ArmorPen) / 100.0;
            int stacks = effectToUse.Stacks > 0 ? effectToUse.Stacks : (baseEffect.MaxStacks > 0 ? baseEffect.MaxStacks : 1);

            double damage = 0;

            if (baseEffect.ConsumeType.HasValue && baseEffect.MaxConsume > 0)
            {
                damage = CalcConsumeStatusEffectDamage(input, baseEffect, atkOverDef, atkRatio, armorPen);
            }
            else
            {
                if (atkRatio > 0)
                {
                    double effectiveAtkOverDef = GetEffectiveAtkOverDef(input, atkOverDef, armorPen);
                    damage += effectiveAtkOverDef * atkRatio * expectedStacks;
                }

                if (hpRatio > 0 && input.TargetHp > 0)
                {
                    double hpDamage = input.TargetHp * hpRatio;
                    if (atkCap > 0) hpDamage = Math.Min(hpDamage, input.FinalAtk * atkCap);
                    damage += hpDamage * expectedStacks;
                }

                if (currentHpRatio > 0 && input.TargetCurrentHp > 0)
                {
                    double hpDamage = input.TargetCurrentHp * currentHpRatio;
                    if (atkCap > 0) hpDamage = Math.Min(hpDamage, input.FinalAtk * atkCap);
                    damage += hpDamage * stacks;
                }

                double fixedDmg = effectToUse.CustomFixedDamage ?? baseEffect.FixedDamage;
                if (fixedDmg > 0) damage = fixedDmg * stacks;
            }

            return damage * result.DamageMultiplier;
        }

        private double CalcConsumeStatusEffectDamage(DamageInput input, StatusEffect baseEffect, double atkOverDef, double atkRatio, double armorPen)
        {
            int consumeCount = Math.Min(input.TargetStackCount, baseEffect.MaxConsume);
            if (consumeCount <= 0) return 0;

            double effectiveAtkOverDef = GetEffectiveAtkOverDef(input, atkOverDef, armorPen);
            int remainingTurns = baseEffect.DefaultRemainingTurns > 0 ? baseEffect.DefaultRemainingTurns : 1;
            return effectiveAtkOverDef * atkRatio * remainingTurns * consumeCount;
        }

        private double GetEffectiveAtkOverDef(DamageInput input, double atkOverDef, double armorPen)
        {
            if (armorPen <= 0) return atkOverDef;
            double newArmorPen = Math.Min(input.ArmorPen / 100.0 + armorPen, 1.0);
            double defCoef = CalcDefCoefficientSimple(input, newArmorPen);
            return input.FinalAtk / defCoef;
        }

        private void CalcHpConversion(DamageInput input, SkillStatusEffect effectToUse, DamageResult result)
        {
            double conversionRatio = (effectToUse.CustomHpConversionRatio ?? 0) / 100.0;
            if (conversionRatio > 0 && input.TargetCurrentHp > 0)
            {
                double newHp = input.TargetCurrentHp * conversionRatio;
                result.HpConversionValue = newHp;
                result.HpConversionLoss = input.TargetCurrentHp - newHp;
                result.HasHpConversion = true;
                result.BonusDamageDetails["HP전환"] = result.HpConversionLoss;
            }
        }

        private void AddBonusDamageDetail(DamageResult result, string effectName, double damage)
        {
            if (result.BonusDamageDetails.ContainsKey(effectName))
                result.BonusDamageDetails[effectName] += damage;
            else
                result.BonusDamageDetails[effectName] = damage;
            result.BonusDamage += damage;
        }

        private double CalcStatusEffectChance(DamageInput input, SkillStatusEffect effect)
        {
            if (input.ForceStatusEffect) return 1.0;
            double baseChance = effect.Chance / 100.0;
            double effectiveEffRes = Math.Max(0, input.TargetEffRes - input.EffResReduction);
            double effModifier = 1 + (input.EffHit - effectiveEffRes) / 100.0;
            return Math.Clamp(baseChance * effModifier, 0, 1);
        }

        private double CalcExpectedStacks(DamageInput input, SkillStatusEffect effect, int atkCount, int maxStacks)
        {
            int stacksPerHit = effect.Stacks > 0 ? effect.Stacks : 1;
            if (input.ForceStatusEffect)
                return Math.Min(atkCount * stacksPerHit, maxStacks);

            double applyChance = CalcStatusEffectChance(input, effect);
            if (applyChance <= 0) return 0;

            int successCount = 0;
            for (int i = 0; i < atkCount; i++)
            {
                if (_random.NextDouble() < applyChance)
                    successCount += stacksPerHit;
            }
            return Math.Min(successCount, maxStacks);
        }

        #endregion

        #region 회복 계산

        private void CalcHeal(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
            result.HealAmount = 0;
            result.HealSource = "";
            result.HealFromDamage = 0;
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
                result.HealAmount = baseHeal * (1 - input.HealReduction / 100.0);
        }

        #endregion

        #region 협공 계산

        private void CalcCoopDamage(DamageInput input, DamageResult result)
        {
            result.CoopDamage = 0;
            result.CoopHpDamage = 0;
            if (input.Character?.Passive == null) return;

            var passiveData = input.Character.Passive.GetLevelData(input.IsSkillEnhanced);
            var coopAttack = passiveData?.CoopAttack;
            if (coopAttack == null) return;

            if (coopAttack.Ratio > 0)
            {
                double atkOverDef = input.FinalAtk / result.DefCoefficient;
                result.CoopDamage = atkOverDef * (coopAttack.Ratio / 100.0) * result.DamageMultiplier;
            }

            if (coopAttack.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                double hpDamage = input.TargetHp * (coopAttack.TargetMaxHpRatio / 100.0);
                if (coopAttack.AtkCap > 0)
                    hpDamage = Math.Min(hpDamage, input.FinalAtk * (coopAttack.AtkCap / 100.0));
                result.CoopHpDamage = hpDamage * result.DamageMultiplier;
            }

            result.CoopDamage *= coopAttack.AtkCount;
            result.CoopHpDamage *= coopAttack.AtkCount;
        }

        #endregion

        #region 표식 계산

        private void CalcMarkDamage(DamageInput input, DamageResult result)
        {
            result.MarkDamage = 0;
            result.MarkHpDamage = 0;
            if (input.Character?.Passive == null) return;

            var passiveData = input.Character.Passive.GetLevelData(input.IsSkillEnhanced);
            var markAttack = passiveData?.MarkAttack;
            if (markAttack == null) return;

            if (markAttack.Ratio > 0)
            {
                double atkOverDef = input.FinalAtk / result.DefCoefficient;
                result.MarkDamage = atkOverDef * (markAttack.Ratio / 100.0) * result.DamageMultiplier * markAttack.AtkCount;
            }

            if (markAttack.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                result.MarkHpDamage = input.TargetHp * (markAttack.TargetMaxHpRatio / 100.0) * result.DamageMultiplier;
            }
        }

        #endregion

        #region 막기/축복 처리

        private void ApplyBlock(DamageInput input, DamageResult result)
        {
            if (!input.IsBlocked) return;
            result.DebugLog.AppendLine($"\n[15] 막기 적용 (-50%)");
            result.DamagePerHit *= 0.5;
            result.BaseDamage *= 0.5;
            result.ExtraDamage *= 0.5;
            result.WekBonusDmg *= 0.5;
            result.HpRatioDamage *= 0.5;
            result.BonusDamage *= 0.5;
            foreach (var key in result.BonusDamageDetails.Keys.ToList())
                result.BonusDamageDetails[key] *= 0.5;
        }

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
                result.DebugLog.AppendLine($"\n[16] 축복 적용: {damage:N0} → {maxDamage:N0}");
                return maxDamage;
            }
            return damage;
        }

        #endregion

        #region 결과 출력

        /// <summary>
        /// 4가지 시나리오 비교 결과 생성 (치명+약점, 치명만, 약점만, 일반)
        /// </summary>
        public string GenerateComparisonDetails(
            DamageResult critWek,
            DamageResult critOnly,
            DamageResult wekOnly,
            DamageResult normal,
            DamageInput baseInput)
        {
            var sb = new StringBuilder();

            // ===== 데미지 비교 테이블 =====
            sb.AppendLine("════════════════════════════════════════");
            sb.AppendLine("🎯 데미지 비교");
            sb.AppendLine("────────────────────────────────────────");

            // 최대값 찾기
            double max = Math.Max(Math.Max(critWek.FinalDamage, critOnly.FinalDamage),
                                  Math.Max(wekOnly.FinalDamage, normal.FinalDamage));

            string GetMarker(double val) => val == max ? " ← 최대" : "";

            sb.AppendLine($"  치명+약점: {critWek.FinalDamage,12:N0}{GetMarker(critWek.FinalDamage)}");
            sb.AppendLine($"  치명만:    {critOnly.FinalDamage,12:N0}{GetMarker(critOnly.FinalDamage)}");
            sb.AppendLine($"  약점만:    {wekOnly.FinalDamage,12:N0}{GetMarker(wekOnly.FinalDamage)}");
            sb.AppendLine($"  일반:      {normal.FinalDamage,12:N0}{GetMarker(normal.FinalDamage)}");
            sb.AppendLine("════════════════════════════════════════");

            // ===== 막기 표시 =====
            if (baseInput.IsBlocked)
            {
                sb.AppendLine("⚠️ 막기 적용됨 (-50%)");
                sb.AppendLine("");
            }

            // ===== 최대 데미지 기준 상세 정보 (치명+약점) =====
            var result = critWek;
            var input = baseInput;
            input.IsCritical = true;
            input.IsWeakpoint = true;

            // 취약/받피증 계수
            double vulBonus = input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease;
            double vulMult = 1 + vulBonus / 100.0;

            // 스킬 데미지 (취약 적용)
            double finalSkillDmg = result.SkillDamage * vulMult;
            sb.AppendLine($"\n💥 스킬 데미지: {finalSkillDmg:N0}");
            if (result.AtkCount > 1)
            {
                double finalPerHit = result.DamagePerHit * vulMult;
                sb.AppendLine($"   ({finalPerHit:N0} × {result.AtkCount}타)");
            }
            if (vulBonus > 0)
                sb.AppendLine($"   (취약/받피증 +{vulBonus:F0}% 적용됨)");

            // 스킬 데미지 내역 (취약 적용)
            if (result.ExtraDamage > 0 || result.WekBonusDmg > 0 || result.CriBonusDmg > 0)
            {
                sb.AppendLine($"\n📌 스킬 데미지 내역");
                sb.AppendLine($"  기본 피해: {result.BaseDamage * vulMult:N0}");
                if (result.ExtraDamage > 0)
                    sb.AppendLine($"  조건부 추가: {result.ExtraDamage * vulMult:N0}");
                if (result.WekBonusDmg > 0)
                    sb.AppendLine($"  약점 추가: {result.WekBonusDmg * vulMult:N0}");
                if (result.CriBonusDmg > 0)
                    sb.AppendLine($"  치명 추가: {result.CriBonusDmg * vulMult:N0}");
            }

            // 별도 피해 (취약 적용)
            if (result.HpRatioDamage > 0 || result.ConsumeExtraDmg > 0 ||
                result.StatusDamage > 0 || result.CoopDamage > 0 || result.CoopHpDamage > 0 ||
                result.MarkDamage > 0 || result.MarkHpDamage > 0)
            {
                sb.AppendLine("\n📌 별도 피해");
                if (result.HpRatioDamage > 0)
                    sb.AppendLine($"  HP비례: {result.HpRatioDamage * vulMult:N0}");
                if (result.ConsumeExtraDmg > 0)
                    sb.AppendLine($"  스택소모: {result.ConsumeExtraDmg * vulMult:N0}");
                if (result.StatusDamage > 0)
                    sb.AppendLine($"  상태이상: {result.StatusDamage * vulMult:N0}");
                double totalCoopDmg = (result.CoopDamage + result.CoopHpDamage) * vulMult;
                if (totalCoopDmg > 0)
                    sb.AppendLine($"  협공: {totalCoopDmg:N0}");
                double totalMarkDmg = (result.MarkDamage + result.MarkHpDamage) * vulMult;
                if (totalMarkDmg > 0)
                    sb.AppendLine($"  표식: {totalMarkDmg:N0}");
            }

            // 축복/흡수
            if (result.BlessingApplied)
                sb.AppendLine($"\n🛡️ 축복 적용: {result.DamageBeforeBlessing:N0} → {result.DamagePerHit:N0}");
            if (result.HealFromDamage > 0)
                sb.AppendLine($"\n💚 피해 흡수: {result.HealFromDamage:N0}");

            // 상태이상 상세
            if (result.StatusEffectResults?.Count > 0)
            {
                sb.AppendLine("\n🔥 상태이상");
                sb.AppendLine("─────────────────");
                foreach (var se in result.StatusEffectResults)
                {
                    if (se.IsForced)
                        sb.AppendLine($"  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 (강제)");
                    else if (se.ExpectedStacks > 0)
                        sb.AppendLine($"  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 ({se.ApplyChance:N0}%)");
                    else
                        sb.AppendLine($"  ✗ {se.Name}: 실패 ({se.ApplyChance:N0}%)");
                }
            }

            // 스탯 정보
            sb.AppendLine("\n📊 스탯 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  최종 공격력: {result.FinalAtk:N0}");
            sb.AppendLine($"  방어 계수: {result.DefCoefficient:F4}");

            // 배율 정보
            sb.AppendLine("\n📈 배율 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  스킬 배율: {result.SkillRatio:F2}x");
            sb.AppendLine($"  치명 계수: {result.CritMultiplier:F2}x");
            sb.AppendLine($"  약점 계수: {result.WeakpointMultiplier:F2}x");
            sb.AppendLine($"  피증 계수: {result.DamageMultiplier:F2}x");

            // 보스 정보
            sb.AppendLine("\n👹 보스 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  실효 방어력: {result.EffectiveBossDef:N0}");
            sb.AppendLine($"  총 방무: {result.TotalArmorPen * 100:F1}%");
            if (input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease > 0)
            {
                double vulTotal = input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease;
                sb.AppendLine($"  취약 합계: {vulTotal:F0}%");
            }
            if (input.BossDmgReduction + input.BossTargetReduction > 0)
            {
                double redTotal = input.BossDmgReduction + input.BossTargetReduction;
                sb.AppendLine($"  피감 합계: {redTotal:F0}%");
            }

            sb.AppendLine("\n════════════════════════════════════════");

            return sb.ToString();
        }

        private string GenerateDetails(DamageInput input, DamageResult result)
        {
            var sb = new StringBuilder();

            // ===== 1. 최종 데미지 =====
            string blockInfo = input.IsBlocked ? " (막기 -50%)" : "";
            sb.AppendLine("════════════════════════════════════════");
            sb.AppendLine($"🎯 최종 데미지: {result.FinalDamage:N0}{blockInfo}");
            sb.AppendLine("════════════════════════════════════════");

            // ===== 2. 스킬 데미지 =====
            sb.AppendLine($"\n💥 스킬 데미지: {result.SkillDamage:N0}");
            if (result.AtkCount > 1)
                sb.AppendLine($"   ({result.DamagePerHit:N0} × {result.AtkCount}타)");

            // ===== 3. 스킬 데미지 내역 (이미 포함됨) =====
            if (result.ExtraDamage > 0 || result.WekBonusDmg > 0 || result.CriBonusDmg > 0)
            {
                sb.AppendLine($"\n📌 스킬 데미지 내역");
                sb.AppendLine($"  기본 피해: {result.BaseDamage:N0}");
                if (result.ExtraDamage > 0)
                    sb.AppendLine($"  조건부 추가: {result.ExtraDamage:N0}");
                if (result.WekBonusDmg > 0)
                    sb.AppendLine($"  약점 추가: {result.WekBonusDmg:N0}");
                if (result.CriBonusDmg > 0)
                    sb.AppendLine($"  치명 추가: {result.CriBonusDmg:N0}");
            }

            // ===== 4. 별도 피해 (스킬 데미지 외 추가) =====
            if (result.HpRatioDamage > 0 || result.ConsumeExtraDmg > 0 ||
                result.StatusDamage > 0 || result.CoopDamage > 0 || result.CoopHpDamage > 0 ||
                result.MarkDamage > 0 || result.MarkHpDamage > 0)
            {
                sb.AppendLine("\n📌 별도 피해");
                if (result.HpRatioDamage > 0)
                    sb.AppendLine($"  HP비례: {result.HpRatioDamage:N0}");
                if (result.ConsumeExtraDmg > 0)
                    sb.AppendLine($"  스택소모: {result.ConsumeExtraDmg:N0}");
                if (result.StatusDamage > 0)
                    sb.AppendLine($"  상태이상: {result.StatusDamage:N0}");
                double totalCoopDmg = result.CoopDamage + result.CoopHpDamage;
                if (totalCoopDmg > 0)
                    sb.AppendLine($"  협공: {totalCoopDmg:N0}");
                double totalMarkDmg = result.MarkDamage + result.MarkHpDamage;
                if (totalMarkDmg > 0)
                    sb.AppendLine($"  표식: {totalMarkDmg:N0}");
            }

            // 축복/흡수
            if (result.BlessingApplied)
                sb.AppendLine($"\n🛡️ 축복 적용: {result.DamageBeforeBlessing:N0} → {result.DamagePerHit:N0}");
            if (result.HealFromDamage > 0)
                sb.AppendLine($"\n💚 피해 흡수: {result.HealFromDamage:N0}");

            // 상태이상 상세
            if (result.StatusEffectResults?.Count > 0)
            {
                sb.AppendLine("\n🔥 상태이상");
                sb.AppendLine("─────────────────");
                foreach (var se in result.StatusEffectResults)
                {
                    if (se.IsForced)
                        sb.AppendLine($"  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 (강제)");
                    else if (se.ExpectedStacks > 0)
                        sb.AppendLine($"  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 ({se.ApplyChance:N0}%)");
                    else
                        sb.AppendLine($"  ✗ {se.Name}: 실패 ({se.ApplyChance:N0}%)");
                }
            }

            // ===== 4. 스탯 정보 =====
            string critInfo = input.IsCritical ? "(치명타)" : "(일반)";
            string wekInfo = input.IsWeakpoint ? "(약점)" : "";
            sb.AppendLine("\n📊 스탯 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  최종 공격력: {result.FinalAtk:N0}");
            sb.AppendLine($"  방어 계수: {result.DefCoefficient:F4}");

            // ===== 5. 배율 정보 =====
            sb.AppendLine("\n📈 배율 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  스킬 배율: {result.SkillRatio:F2}x");
            sb.AppendLine($"  치명 계수: {result.CritMultiplier:F2}x {critInfo}");
            sb.AppendLine($"  약점 계수: {result.WeakpointMultiplier:F2}x {wekInfo}");
            sb.AppendLine($"  피증 계수: {result.DamageMultiplier:F2}x");

            // ===== 6. 상세 정보 (계산 과정) =====
            sb.AppendLine("\n📝 상세 계산");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  기초 피해: {result.FinalAtk:N0} / {result.DefCoefficient:F4} = {result.FinalAtk / result.DefCoefficient:N0}");
            sb.AppendLine($"  스킬 적용: × {result.SkillRatio:F2} = {(result.FinalAtk / result.DefCoefficient) * result.SkillRatio:N0}");
            sb.AppendLine($"  계수 적용: × 치명{result.CritMultiplier:F2} × 약점{result.WeakpointMultiplier:F2} × 피증{result.DamageMultiplier:F2}");
            sb.AppendLine($"  1타 피해: {result.DamagePerHit:N0}");

            // ===== 7. 보스 정보 =====
            sb.AppendLine("\n👹 보스 정보");
            sb.AppendLine("─────────────────");
            sb.AppendLine($"  실효 방어력: {result.EffectiveBossDef:N0}");
            sb.AppendLine($"  총 방무: {result.TotalArmorPen * 100:F1}%");
            if (input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease > 0)
            {
                double vulTotal = input.Vulnerability + input.BossVulnerability + input.DmgTakenIncrease;
                sb.AppendLine($"  취약 합계: {vulTotal:F0}%");
            }
            if (input.BossDmgReduction + input.BossTargetReduction > 0)
            {
                double redTotal = input.BossDmgReduction + input.BossTargetReduction;
                sb.AppendLine($"  피감 합계: {redTotal:F0}%");
            }

            sb.AppendLine("\n════════════════════════════════════════");

            // 디버그 로그 (개발용)
            sb.AppendLine("\n[DEBUG LOG]");
            sb.Append(result.DebugLog);

            return sb.ToString();
        }

        #endregion
    }
}

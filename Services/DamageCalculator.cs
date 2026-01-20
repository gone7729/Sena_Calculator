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

        /// <summary>
        /// 데미지 계산 입력 데이터
        /// </summary>
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
            public double BossVulnerability { get; set; }   // 보스 취약
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
            public bool IsBlocked { get; set; }
            public bool IsSkillConditionMet { get; set; }
            public bool IsLostHpConditionMet { get; set; }

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
        }

        /// <summary>
        /// 데미지 계산 결과
        /// </summary>
        public class DamageResult
        {
            // ===== 스탯 =====
            public double FinalAtk { get; set; }
            public double TotalArmorPen { get; set; }
            public double EffectiveBossDef { get; set; }
            public double DefCoefficient { get; set; }

            // ===== 배율 =====
            public double SkillRatio { get; set; }
            public double CritMultiplier { get; set; }
            public double WeakpointMultiplier { get; set; }
            public double DamageMultiplier { get; set; }

            // ===== 타수 =====
            public int AtkCount { get; set; }

            // ===== 피해 (1타 기준) =====
            public double BaseDamage { get; set; }
            public double ExtraDamage { get; set; }
            public double WekBonusDmg { get; set; }
            public double CriBonusDmg { get; set; }
            public double DamagePerHit { get; set; }
            public double ConsumeExtraDmg { get; set; }

            // ===== 최종 피해 =====
            public double FinalDamage { get; set; }
            public double SkillDamage { get; set; }
            public double StatusDamage { get; set; }
            public double BonusDamage { get; set; }
            public double HpRatioDamage { get; set; }

            // ===== 협공 =====
            public double CoopDamage { get; set; }
            public double CoopHpDamage { get; set; }
            public bool CoopTriggered { get; set; }
            public double CoopChance { get; set; }

            // ===== 회복 =====
            public double HealAmount { get; set; }
            public string HealSource { get; set; }
            public double HealFromDamage { get; set; }

            // ===== 축복 =====
            public double DamageBeforeBlessing { get; set; }
            public bool BlessingApplied { get; set; }

            // ===== HP 전환 =====
            public double HpConversionValue { get; set; }
            public double HpConversionLoss { get; set; }
            public bool HasHpConversion { get; set; }

            // ===== 상세 정보 =====
            public Dictionary<string, double> BonusDamageDetails { get; set; } = new();
            public List<StatusEffectResult> StatusEffectResults { get; set; } = new();
            public string Details { get; set; }
        }

        /// <summary>
        /// 상태이상 적용 결과
        /// </summary>
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

        /// <summary>
        /// 데미지 계산 메인 메서드
        /// </summary>
        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult { FinalAtk = input.FinalAtk };
            var levelData = input.Skill?.GetLevelData(input.IsSkillEnhanced);
            var skillBonus = input.Skill?.GetTotalBonus(input.IsSkillEnhanced, input.TranscendLevel) ?? new BuffSet();

            // 1. 스킬 발동 전 버프
            ApplyPreCastBuff(input, levelData, result);
            result.AtkCount = input.Skill?.Atk_Count ?? 1;

            // 2. 스킬 배율
            result.SkillRatio = (levelData?.Ratio ?? 100) / 100.0;

            // 3. 방어 관통
            result.TotalArmorPen = Math.Min((input.ArmorPen + skillBonus.Arm_Pen) / 100.0, 1.0);

            // 4. 방어 계수
            CalcDefenseCoefficient(input, levelData, result);

            // 5. 치명타 계수
            result.CritMultiplier = input.IsCritical
                ? (input.CritDamage + skillBonus.Cri_Dmg) / 100.0
                : 1.0;

            // 6. 약점 계수
            result.WeakpointMultiplier = input.IsWeakpoint
    ? input.WeakpointDmg / 100.0  // 버프 제거
    : 1.0;

            // 7. 피해 증가 계수
            result.DamageMultiplier = CalcDamageMultiplier(input);

            // 8. 잃은 HP 비례 피해 증가
            double lostHpBonus = CalcLostHpBonusDmg(input, levelData);
            if (lostHpBonus > 0)
                result.DamageMultiplier *= (1 + lostHpBonus / 100.0);

            // 9. 기본 데미지
            double atkOverDef = result.FinalAtk / result.DefCoefficient;
            CalcBaseDamage(input, levelData, atkOverDef, result);

            // 10. 추가 피해
            CalcExtraDamage(input, levelData, skillBonus, atkOverDef, result);

            // 11. HP 비례 피해
            result.HpRatioDamage = CalcHpRatioDamage(input, levelData, result.DamageMultiplier);

            // 12. 스택 소모형 추가 피해
            CalcConsumeExtraDamage(input, levelData, atkOverDef, result);

            // 13. 1타당 데미지
            result.DamagePerHit = result.BaseDamage + result.ExtraDamage + result.WekBonusDmg
                    + result.CriBonusDmg;

            // 14. 별도 피해
            CalcBonusDamage(input, levelData, atkOverDef, result);

            // 15. 막기
            ApplyBlock(input, result);

            // 16. 축복
            result.DamagePerHit = ApplyBlessing(result.DamagePerHit, input, result);

            // 17. 최종 데미지
            result.SkillDamage = result.DamagePerHit * result.AtkCount;
            result.StatusDamage = result.BonusDamage * result.AtkCount;
            result.FinalDamage = result.SkillDamage + result.StatusDamage 
                                + result.HpRatioDamage + result.ConsumeExtraDmg;

            // 18. 회복량
            CalcHeal(input, levelData, result);

            // 19. 피해량 비례 회복
            if (levelData?.HealDmgRatio > 0)
                result.HealFromDamage = result.FinalDamage * (levelData.HealDmgRatio / 100.0);

            // 20. 협공 피해
            CalcCoopDamage(input, result);

            // 21. 상세 정보
            result.Details = GenerateDetails(input, result);

System.Diagnostics.Debug.WriteLine($"=== 전체 검증 ===");
System.Diagnostics.Debug.WriteLine($"FinalAtk: {input.FinalAtk}");
System.Diagnostics.Debug.WriteLine($"DefCoef: {result.DefCoefficient}");
System.Diagnostics.Debug.WriteLine($"AtkOverDef: {result.FinalAtk / result.DefCoefficient}");
System.Diagnostics.Debug.WriteLine($"SkillRatio: {result.SkillRatio}");
System.Diagnostics.Debug.WriteLine($"CritMult: {result.CritMultiplier}");
System.Diagnostics.Debug.WriteLine($"WeakMult: {result.WeakpointMultiplier}");
System.Diagnostics.Debug.WriteLine($"DmgMult: {result.DamageMultiplier}");
System.Diagnostics.Debug.WriteLine($"전체배율: {result.CritMultiplier * result.WeakpointMultiplier * result.DamageMultiplier}");
System.Diagnostics.Debug.WriteLine($"=== 추가 피해 ===");
System.Diagnostics.Debug.WriteLine($"BaseDamage: {result.BaseDamage:N0}");
System.Diagnostics.Debug.WriteLine($"ExtraDamage: {result.ExtraDamage:N0}");
System.Diagnostics.Debug.WriteLine($"WekBonusDmg: {result.WekBonusDmg:N0}");
System.Diagnostics.Debug.WriteLine($"CriBonusDmg: {result.CriBonusDmg:N0}");
System.Diagnostics.Debug.WriteLine($"DamagePerHit: {result.DamagePerHit:N0}");

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
            const double DEF_CONSTANT = 0.00214;
            const double THRESHOLD = 3125.0;

            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            effectiveDef = input.BossDef * defModifier * armorPenModifier;

            if (effectiveDef <= THRESHOLD)
                return 1 + effectiveDef * DEF_CONSTANT;

            double baseCoef = 1 + THRESHOLD * DEF_CONSTANT;
            double extraDef = effectiveDef - THRESHOLD;
            return baseCoef + extraDef * DEF_CONSTANT * 0.5;
        }

        private double CalcDefCoefficientSimple(DamageInput input, double armorPen)
        {
            const double DEF_CONSTANT = 0.00214;
            const double THRESHOLD = 3125.0;

            double defModifier = Math.Max(1 + (input.BossDefIncrease - input.DefReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            double effectiveDef = input.BossDef * defModifier * armorPenModifier;

            if (effectiveDef <= THRESHOLD)
                return 1 + effectiveDef * DEF_CONSTANT;

            double baseCoef = 1 + THRESHOLD * DEF_CONSTANT;
            double extraDef = effectiveDef - THRESHOLD;
            return baseCoef + extraDef * DEF_CONSTANT * 0.5;
        }

        #endregion

        #region 피해 배율 계산

        private double CalcDamageMultiplier(DamageInput input)
{
    // === 조건부 피해 보너스 (스킬 초월) ===
    double conditionalDmgBonus = 0;
    if (input.Skill != null)
    {
        var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
        conditionalDmgBonus = skillTranscend.ConditionalDmgBonus;
    }

    // === 타겟 수 기반 피해 ===
    double targetTypeDmg = 0;
    if (input.Skill != null)
    {
        int targetCount = input.Skill.GetTargetCount(input.TranscendLevel);
        targetTypeDmg = (targetCount >= 1 && targetCount <= 3) ? input.Dmg1to3
                      : (targetCount >= 4) ? input.Dmg4to5 : 0;
    }

    // === 보스 모드 전용 스탯 ===
    double bossDmg = input.Mode == BattleMode.Boss ? input.DmgDealtBoss : 0;
    double bossVuln = input.Mode == BattleMode.Boss ? input.BossVulnerability : 0;

    // === 피증 계열 합산 (덧셈으로 먼저 합침) ===
    // 피증 + 보피증 + 조건부 피해 보너스 + 타겟 수 피해
    double dmgDealtTotal = input.DmgDealt + bossDmg + conditionalDmgBonus + targetTypeDmg;

    // === 취약 계열 합산 ===
    // 취약 + 보스취약 + 받피증
    double vulnerabilityTotal = input.Vulnerability + bossVuln + input.DmgTakenIncrease;

    // === 피해 감소 계열 ===
    double reductionTotal = input.BossDmgReduction + input.BossTargetReduction;

    // === 최종 배율: 곱셈 구조 ===
    // (1 + 피증계열) × (1 + 취약계열) × (1 - 감소계열)
    double dmgDealtMult = 1 + dmgDealtTotal / 100.0;
    double vulnerabilityMult = 1 + vulnerabilityTotal / 100.0;
    double reductionMult = 1 - reductionTotal / 100.0;

     // === 디버그 출력 추가 ===
    System.Diagnostics.Debug.WriteLine($"=== CalcDamageMultiplier ===");
    System.Diagnostics.Debug.WriteLine($"피증: {input.DmgDealt}, 보피증: {bossDmg}, 조건부: {conditionalDmgBonus}, 타겟: {targetTypeDmg}");
    System.Diagnostics.Debug.WriteLine($"취약: {input.Vulnerability}, 보스취약: {bossVuln}, 받피증: {input.DmgTakenIncrease}");
    System.Diagnostics.Debug.WriteLine($"감소: {reductionTotal}");
    System.Diagnostics.Debug.WriteLine($"dmgDealtMult: {dmgDealtMult}, vulnerabilityMult: {vulnerabilityMult}, reductionMult: {reductionMult}");
    System.Diagnostics.Debug.WriteLine($"최종 DamageMultiplier: {dmgDealtMult * vulnerabilityMult * reductionMult}");

    return dmgDealtMult * vulnerabilityMult * reductionMult;
}

        private double CalcLostHpBonusDmg(DamageInput input, SkillLevelData levelData)
        {
            if (levelData == null || levelData.LostHpBonusDmgMax <= 0) return 0;
            return input.IsLostHpConditionMet ? levelData.LostHpBonusDmgMax : 0;
        }

        #endregion

        #region 기본/추가 피해 계산

        private void ApplyPreCastBuff(DamageInput input, SkillLevelData levelData, DamageResult result)
        {
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
        }

        private void CalcBaseDamage(DamageInput input, SkillLevelData levelData, double atkOverDef, DamageResult result)
{
    double atkDamage = atkOverDef * result.SkillRatio;
    double fixedDamage = levelData?.FixedDamage ?? 0;

    double defDamage = 0;
    if (levelData?.DefRatio > 0 && input.FinalDef > 0)
    {
        double defOverDef = input.FinalDef / result.DefCoefficient;
        defDamage = defOverDef * (levelData.DefRatio / 100.0);
    }

    double hpDamage = 0;
    if (levelData?.HpRatio > 0 && input.FinalHp > 0)
    {
        double hpOverDef = input.FinalHp / result.DefCoefficient;
        hpDamage = hpOverDef * (levelData.HpRatio / 100.0);
    }

    result.BaseDamage = (atkDamage + defDamage + hpDamage)
                      * result.CritMultiplier
                      * result.WeakpointMultiplier
                      * result.DamageMultiplier
                      + fixedDamage;

    // === 디버그 출력 (계산 후) ===
    System.Diagnostics.Debug.WriteLine($"=== CalcBaseDamage ===");
    System.Diagnostics.Debug.WriteLine($"atkOverDef: {atkOverDef:N2}, SkillRatio: {result.SkillRatio:N2}");
    System.Diagnostics.Debug.WriteLine($"atkDamage(raw): {atkDamage:N2}");
    System.Diagnostics.Debug.WriteLine($"CritMult: {result.CritMultiplier}, WeakMult: {result.WeakpointMultiplier}, DmgMult: {result.DamageMultiplier:N4}");
    System.Diagnostics.Debug.WriteLine($"전체배율: {result.CritMultiplier * result.WeakpointMultiplier * result.DamageMultiplier:N2}");
    System.Diagnostics.Debug.WriteLine($"BaseDamage: {result.BaseDamage:N0}");
}
        private void CalcExtraDamage(DamageInput input, SkillLevelData levelData, BuffSet skillBonus,
                                     double atkOverDef, DamageResult result)
        {
            // 조건부 추가 피해
            result.ExtraDamage = 0;
            if (input.IsSkillConditionMet && levelData?.ConditionalExtraDmg > 0)
            {
                double extraDmg = atkOverDef * (levelData.ConditionalExtraDmg / 100.0) * result.DamageMultiplier;
                if (levelData.ConditionalExtraDmgPerHit)
                    extraDmg *= result.AtkCount;
                result.ExtraDamage = extraDmg;
            }

            // 시전자 생명력 비례 추가 피해
            if (levelData.ConditionalExtraDmgSelfHpRatio > 0)
            {
                double selfHpExtraDmg = input.SelfMaxHp * (levelData.ConditionalExtraDmgSelfHpRatio / 100.0);
                result.ExtraDamage += selfHpExtraDmg;
            }

            // 약점 추가 피해
            result.WekBonusDmg = 0;
            if (input.IsWeakpoint && skillBonus.WekBonusDmg > 0)
                result.WekBonusDmg = atkOverDef * (skillBonus.WekBonusDmg / 100.0) * result.DamageMultiplier;

            // 치명타 추가 피해
            result.CriBonusDmg = 0;
            if (input.IsCritical && skillBonus.CriBonusDmg > 0)
            {
                double criBonus = atkOverDef * (skillBonus.CriBonusDmg / 100.0) * result.DamageMultiplier;
                if (skillBonus.CriBonusDmgPerHit)
                    criBonus *= result.AtkCount;
                result.CriBonusDmg = criBonus;
            }
        }

        private void CalcConsumeExtraDamage(DamageInput input, SkillLevelData levelData,
                                    double atkOverDef, DamageResult result)
{
    result.ConsumeExtraDmg = 0;
    if (levelData?.ConsumeExtra == null) return;

    var consumeExtra = levelData.ConsumeExtra;
    
    // ✅ 초월 보너스 합산
    double totalHpRatio = consumeExtra.TargetMaxHpRatio;
    double totalAtkCap = consumeExtra.AtkCap;
    double totalAtkRatio = consumeExtra.AtkRatio;
    
    var transcendBonus = input.Skill?.GetTranscendBonus(input.TranscendLevel);
    if (transcendBonus?.ConsumeExtra != null)
    {
        totalHpRatio += transcendBonus.ConsumeExtra.TargetMaxHpRatio;
        totalAtkRatio += transcendBonus.ConsumeExtra.AtkRatio;
    }
    
    double damage = 0;

    if (totalHpRatio > 0 && input.TargetHp > 0)
    {
        damage = input.TargetHp * (totalHpRatio / 100.0);
        System.Diagnostics.Debug.WriteLine($"HP 비례 raw: {damage:N0}");
        
        if (totalAtkCap > 0)
        {
            double cap = input.FinalAtk * (totalAtkCap / 100.0);
            System.Diagnostics.Debug.WriteLine($"Cap 값: {cap:N0}");
            damage = Math.Min(damage, cap);
            System.Diagnostics.Debug.WriteLine($"Cap 적용 후: {damage:N0}");
        }
    }

    if (totalAtkRatio > 0)
        damage += atkOverDef * (totalAtkRatio / 100.0);

    double fullMultiplier = result.DamageMultiplier 
                      * result.CritMultiplier 
                      * result.WeakpointMultiplier;
result.ConsumeExtraDmg = damage * fullMultiplier;

System.Diagnostics.Debug.WriteLine($"fullMultiplier: {fullMultiplier:N2} (DmgMult:{result.DamageMultiplier:N2} × Crit:{result.CritMultiplier} × Weak:{result.WeakpointMultiplier})");
System.Diagnostics.Debug.WriteLine($"최종 ConsumeExtraDmg: {result.ConsumeExtraDmg:N0}");
}

        #endregion

        #region HP 비례 피해 계산

        private double CalcHpRatioDamage(DamageInput input, SkillLevelData levelData, double damageMultiplier)
        {

            if (levelData == null) return 0;
            double totalHpDamage = 0;

            if (levelData.TargetMaxHpRatio > 0 && input.TargetHp > 0)
            {
                double rawDamage = input.TargetHp * (levelData.TargetMaxHpRatio / 100.0);
                if (levelData.AtkCap > 0)
                    rawDamage = Math.Min(rawDamage, input.FinalAtk * (levelData.AtkCap / 100.0));
                totalHpDamage += rawDamage * damageMultiplier;
            }

            if (levelData.TargetCurrentHpRatio > 0 && input.TargetCurrentHp > 0)
            {
                double rawDamage = input.TargetCurrentHp * (levelData.TargetCurrentHpRatio / 100.0);
                if (levelData.AtkCap > 0)
                    rawDamage = Math.Min(rawDamage, input.FinalAtk * (levelData.AtkCap / 100.0));
                totalHpDamage += rawDamage * damageMultiplier;
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

        private void CalcStatusEffectDamage(DamageInput input, SkillLevelData levelData,
                                            double atkOverDef, DamageResult result)
        {
            if (levelData.StatusEffects == null || levelData.StatusEffects.Count == 0) return;

            var skillTranscend = input.Skill?.GetTranscendBonus(input.TranscendLevel);

            foreach (var effect in levelData.StatusEffects)
            {
                var effectToUse = skillTranscend?.StatusEffects?
                    .FirstOrDefault(e => e.Type == effect.Type) ?? effect;

                var baseEffect = StatusEffectDb.Get(effectToUse.Type);
                if (baseEffect == null) continue;

                double applyChance = CalcStatusEffectChance(input, effectToUse);
                int atkCount = input.Skill?.Atk_Count ?? 1;
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

                double damage = CalcSingleStatusEffectDamage(input, effectToUse, baseEffect,
                                                            atkOverDef, expectedStacks, result);
                if (damage > 0)
                    AddBonusDamageDetail(result, baseEffect.Name, damage);
            }
        }

        private double CalcSingleStatusEffectDamage(DamageInput input, SkillStatusEffect effectToUse,
                                                    StatusEffect baseEffect, double atkOverDef,
                                                    double expectedStacks, DamageResult result)
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

        private double CalcConsumeStatusEffectDamage(DamageInput input, StatusEffect baseEffect,
                                                     double atkOverDef, double atkRatio, double armorPen)
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

        #region 막기/축복 처리

        private void ApplyBlock(DamageInput input, DamageResult result)
        {
            if (!input.IsBlocked) return;

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
                return maxDamage;
            }
            return damage;
        }

        #endregion

        #region 결과 출력

        private string GenerateDetails(DamageInput input, DamageResult result)
{
    string critInfo = input.IsCritical ? "(치명타!)" : "(일반)";
    string wekInfo = input.IsWeakpoint ? "(약점!)" : "";
    string blockInfo = input.IsBlocked ? " (막기 -50%)" : "";

    var statusBuilder = new StringBuilder();
    if (result.StatusEffectResults?.Count > 0)
    {
        statusBuilder.Append("\n\n🔥 상태이상\n───────────────────────────────────────────────────");
        foreach (var se in result.StatusEffectResults)
        {
            if (se.IsForced)
                statusBuilder.Append($"\n  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 (강제 적용)");
            else if (se.ExpectedStacks > 0)
                statusBuilder.Append($"\n  ✓ {se.Name}: {se.ExpectedStacks:N0}스택 성공! ({se.ApplyChance:N0}% 확률)");
            else
                statusBuilder.Append($"\n  ✗ {se.Name}: 실패 ({se.ApplyChance:N0}% 확률)");
        }

        if (result.BonusDamageDetails?.Count > 0)
        {
            statusBuilder.Append("\n  [피해]");
            foreach (var kvp in result.BonusDamageDetails)
                statusBuilder.Append($"\n  ├ {kvp.Key}: {kvp.Value:N0}");
            statusBuilder.Append($"\n  └ 총 상태이상 피해: {result.StatusDamage:N0}");
        }
    }

    string conditionalInfo = "";
    if (input.IsSkillConditionMet && input.Skill != null)
    {
        var skillTranscend = input.Skill.GetTranscendBonus(input.TranscendLevel);
        if (skillTranscend.ConditionalDmgBonus > 0)
            conditionalInfo = $"\n  스킬 조건부 피해: +{skillTranscend.ConditionalDmgBonus}%";
    }

    string extraInfo = result.ExtraDamage > 0 ? $"\n  ├ 조건부 추가: {result.ExtraDamage:N0}" : "";
    string wekBonusInfo = result.WekBonusDmg > 0 ? $"\n  ├ 약점 추가: {result.WekBonusDmg:N0}" : "";
    string criBonusInfo = result.CriBonusDmg > 0 ? $"\n  ├ 치명타 추가: {result.CriBonusDmg:N0}" : "";
    string consumeExtraInfo = result.ConsumeExtraDmg > 0 ? $"\n  ├ 스택 소모 추가: {result.ConsumeExtraDmg:N0}" : "";
    
    // ✅ HP 비례 피해 표시 추가
    string hpRatioInfo = result.HpRatioDamage > 0 ? $"\n  ├ HP비례 추가: {result.HpRatioDamage:N0}" : "";
    
    string atkCountInfo = result.AtkCount > 1 ? $"\n  └ {result.AtkCount}타 = {result.DamagePerHit:N0} × {result.AtkCount}" : "";
    string healFromDmgInfo = result.HealFromDamage > 0 ? $"\n\n💚 피해 흡수: {result.HealFromDamage:N0}" : "";
    string blessingInfo = result.BlessingApplied ? $"\n\n🛡️ 축복: {result.DamageBeforeBlessing:N0} → {result.DamagePerHit:N0} (HP {input.TargetBlessing}% 제한)" : "";

    string coopInfo = "";
    double totalCoopDmg = result.CoopDamage + result.CoopHpDamage;
    if (totalCoopDmg > 0)
        coopInfo = $"\n\n⚔️ 협공 피해: {totalCoopDmg:N0}\n  ├ 스킬: {result.CoopDamage:N0}\n  └ HP비례: {result.CoopHpDamage:N0}";

    // ✅ 최종 피해 합계 표시 (HP 비례 포함)
    string totalDamageInfo = "";
    if (result.HpRatioDamage > 0 || result.StatusDamage > 0)
    {
        totalDamageInfo = $"\n───────────────────────────────────────────────────\n🎯 총 피해: {result.FinalDamage:N0}";
    }

    return $@"═══════════════════════════════════════════════════
🎯 PVE (보스전)
═══════════════════════════════════════════════════
💥 스킬 데미지: {result.SkillDamage:N0}{blockInfo}{extraInfo}{wekBonusInfo}{criBonusInfo}{consumeExtraInfo}{hpRatioInfo}{atkCountInfo}{coopInfo}{blessingInfo}{healFromDmgInfo}{statusBuilder}{totalDamageInfo}
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
  최대HP: {input.TargetHp:N0}

  [전투옵션]
  치명타: {input.IsCritical}
  약점: {input.IsWeakpoint}
  막기: {input.IsBlocked}
";
}

        #endregion
    }
}
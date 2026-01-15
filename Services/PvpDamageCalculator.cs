using System;
using System.Text;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// PVP 양방향 데미지 계산기
    /// PVE와 차이점: 방어계수 임계값 없음, 보스피해증가/인기수피해 미적용
    /// </summary>
    public class PvpDamageCalculator
    {
        #region 공용 상수

        // PVP 방어 계수 (PVE와 동일하지만 임계값 없음)
        private const double DEF_COEFFICIENT = 0.00214;

        #endregion

        #region 메인 계산

        /// <summary>
        /// PVP 양방향 데미지 계산
        /// </summary>
        public PvpResult Calculate(PvpInput input)
        {
            var result = new PvpResult();

            // 내가 적에게 주는 피해
            result.MyDamageToEnemy = CalculateDamage(
                input.MyStats,
                input.EnemyStats,
                input.MyDebuffsOnEnemy,
                input.MySkill,
                input.MyOptions,
                input.MySkillEnhanced,
                input.MyTranscendLevel);

            // 적이 나에게 주는 피해
            result.EnemyDamageToMe = CalculateDamage(
                input.EnemyStats,
                input.MyStats,
                input.EnemyDebuffsOnMe,
                input.EnemySkill,
                input.EnemyOptions,
                input.EnemySkillEnhanced,
                input.EnemyTranscendLevel);

            return result;
        }

        /// <summary>
        /// 단방향 데미지 계산
        /// </summary>
        private PvpDamageResult CalculateDamage(PvpCharacterStats attacker, PvpCharacterStats defender,
            PvpDebuffs debuffs, Skill skill, PvpCombatOptions options, 
            bool isSkillEnhanced, int transcendLevel)
        {
            var result = new PvpDamageResult();

            if (skill == null)
            {
                result.FinalDamage = 0;
                result.Details = "스킬이 선택되지 않았습니다.";
                return result;
            }

            var levelData = skill.GetLevelData(isSkillEnhanced);
            var skillBonus = skill.GetTotalBonus(isSkillEnhanced, transcendLevel) ?? new BuffSet();

            // 1. 방어 관통 (스킬 보너스 포함)
            result.TotalArmorPen = Math.Min((attacker.ArmorPen + skillBonus.Arm_Pen) / 100.0, 1.0);

            // 2. 방어 계수 계산 (방깎 디버프 적용)
            result.DefCoefficient = CalculateDefenseCoefficient(
                defender.Def,
                defender.DefIncrease,
                debuffs?.DefReduction ?? 0,
                result.TotalArmorPen);
            result.EffectiveDef = CalculateEffectiveDef(
                defender.Def,
                defender.DefIncrease,
                debuffs?.DefReduction ?? 0,
                result.TotalArmorPen);

            // 3. 기본 피해 = 공격력 / 방어계수
            double atkOverDef = attacker.Atk / result.DefCoefficient;
            result.BaseDamage = atkOverDef;

            // 4. 스킬 배율
            result.SkillRatio = (levelData?.Ratio ?? 100) / 100.0;

            // 5. 타수
            result.AtkCount = skill.Atk_Count;

            // 6. 치명타 배율 (스킬 보너스 포함)
            result.CritMultiplier = options.IsCritical
                ? (attacker.CritDamage + skillBonus.Cri_Dmg) / 100.0
                : 1.0;

            // 7. 약점 배율 (약점 피해 증가 버프 적용)
            result.WeakpointMultiplier = options.IsWeakpoint
                ? (attacker.WeakDamage / 100.0) * (1 + attacker.WeakDamageBuff / 100.0)
                : 1.0;

            // 8. 피해 증가 배율 (PVP: 보스피해/인기수피해 제외)
            // 피증 + 받피증(디버프) + 취약(디버프) - 피감(상대)
            double dmgIncrease = attacker.DamageDealt 
                               + (debuffs?.DmgTakenIncrease ?? 0) 
                               + (debuffs?.Vulnerability ?? 0);
            double dmgReduction = defender.DamageReduction;
            result.DamageMultiplier = 1 + (dmgIncrease - dmgReduction) / 100.0;
            result.DamageMultiplier = Math.Max(0, result.DamageMultiplier); // 음수 방지

            // 9. 막기 배율
            double blockMultiplier = options.IsBlocked ? 0.5 : 1.0;

            // 10. 1타당 피해 계산
            double damagePerHit = atkOverDef * result.SkillRatio * result.CritMultiplier 
                                * result.WeakpointMultiplier * result.DamageMultiplier * blockMultiplier;
            result.DamagePerHit = damagePerHit;

            // 11. 최종 피해 (타수 적용)
            double totalDamage = damagePerHit * result.AtkCount;

            // 12. 축복 적용
            if (defender.Blessing > 0)
            {
                result.BlessingAbsorbed = Math.Min(defender.Blessing, totalDamage);
                totalDamage = Math.Max(0, totalDamage - defender.Blessing);
            }

            // 13. 고통 감내 적용
            ApplyPainEndurance(totalDamage, defender, result);

            // 14. 상세 정보 생성
            result.Details = GenerateDetails(attacker, defender, debuffs, skill, options, result);

            return result;
        }

        /// <summary>
        /// 고통 감내 적용
        /// </summary>
        private void ApplyPainEndurance(double damage, PvpCharacterStats defender, PvpDamageResult result)
        {
            result.FinalDamage = damage;
            result.ImmediateDamage = damage;
            
            if (defender.PainEndurance == null || defender.Hp <= 0)
                return;

            var pe = defender.PainEndurance;
            double threshold = defender.Hp * (pe.Threshold / 100.0);

            // 피해가 기준 이상일 때만 발동
            if (damage >= threshold && pe.ReductionRate > 0 && pe.Duration > 0)
            {
                result.PainEnduranceTriggered = true;
                
                // 즉시 받는 피해 = (100 - 분산비율)%
                double immediateRate = 1 - (pe.ReductionRate / 100.0);
                result.ImmediateDamage = damage * immediateRate;
                
                // 분산 피해 = 분산비율% / 턴수
                double deferredDamage = damage * (pe.ReductionRate / 100.0);
                result.DotDamagePerTurn = deferredDamage / pe.Duration;
                result.DotDuration = pe.Duration;
                
                // 최종 피해는 총합 (즉시 + 분산 전체)
                result.FinalDamage = damage;  // 총 피해량은 동일
            }
        }

        /// <summary>
        /// 방어 계수 계산 (PVP: 임계값 없음)
        /// </summary>
        private double CalculateDefenseCoefficient(double defense, double defenseIncrease, 
            double defenseReduction, double armorPen)
        {
            // 방어 계수 = 1 + 방어력 × (1 + 방증% - 방깎%) × (1 - 방무%) × 0.00214
            double defModifier = Math.Max(1 + (defenseIncrease - defenseReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            
            double coefficient = 1 + defense * defModifier * armorPenModifier * DEF_COEFFICIENT;
            return Math.Max(1, coefficient); // 최소 1
        }

        /// <summary>
        /// 유효 방어력 계산
        /// </summary>
        private double CalculateEffectiveDef(double defense, double defenseIncrease,
            double defenseReduction, double armorPen)
        {
            double defModifier = Math.Max(1 + (defenseIncrease - defenseReduction) / 100.0, 0);
            double armorPenModifier = 1 - armorPen;
            return defense * defModifier * armorPenModifier;
        }

        /// <summary>
        /// 상세 정보 생성
        /// </summary>
        private string GenerateDetails(PvpCharacterStats attacker, PvpCharacterStats defender,
            PvpDebuffs debuffs, Skill skill, PvpCombatOptions options, PvpDamageResult result)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"[스킬: {skill.Name}]");
            sb.AppendLine($"스킬 배율: {result.SkillRatio * 100:F0}%");
            sb.AppendLine($"타수: {result.AtkCount}회");
            sb.AppendLine();
            
            sb.AppendLine("[공격자]");
            sb.AppendLine($"공격력: {attacker.Atk:N0}");
            sb.AppendLine($"방무: {result.TotalArmorPen * 100:F1}%");
            sb.AppendLine($"치피: {attacker.CritDamage:F1}%");
            sb.AppendLine($"피증: {attacker.DamageDealt:F1}%");
            sb.AppendLine();
            
            sb.AppendLine("[방어자]");
            sb.AppendLine($"방어력: {defender.Def:N0}");
            sb.AppendLine($"방증: {defender.DefIncrease:F1}%");
            sb.AppendLine($"피감: {defender.DamageReduction:F1}%");
            sb.AppendLine();
            
            if (debuffs != null && (debuffs.DefReduction > 0 || debuffs.DmgTakenIncrease > 0 || debuffs.Vulnerability > 0))
            {
                sb.AppendLine("[디버프]");
                if (debuffs.DefReduction > 0) sb.AppendLine($"방깎: {debuffs.DefReduction:F1}%");
                if (debuffs.DmgTakenIncrease > 0) sb.AppendLine($"받피증: {debuffs.DmgTakenIncrease:F1}%");
                if (debuffs.Vulnerability > 0) sb.AppendLine($"취약: {debuffs.Vulnerability:F1}%");
                sb.AppendLine();
            }
            
            sb.AppendLine("[계산 결과]");
            sb.AppendLine($"유효 방어력: {result.EffectiveDef:N0}");
            sb.AppendLine($"방어 계수: {result.DefCoefficient:F4}");
            sb.AppendLine($"공/방계수: {result.BaseDamage:N0}");
            sb.AppendLine();
            
            sb.AppendLine("[배율]");
            if (options.IsCritical) sb.AppendLine($"치명타: {result.CritMultiplier * 100:F1}%");
            if (options.IsWeakpoint) sb.AppendLine($"약점: {result.WeakpointMultiplier * 100:F1}%");
            sb.AppendLine($"피해 배율: {result.DamageMultiplier * 100:F1}%");
            if (options.IsBlocked) sb.AppendLine("막기: 50%");
            sb.AppendLine();
            
            sb.AppendLine($"[1타 피해: {result.DamagePerHit:N0}]");
            sb.AppendLine($"[스킬 피해: {result.DamagePerHit * result.AtkCount:N0}] ({result.AtkCount}타)");
            
            // 축복 정보
            if (result.BlessingAbsorbed > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"[축복 흡수: {result.BlessingAbsorbed:N0}]");
            }
            
            // 고통 감내 정보
            if (result.PainEnduranceTriggered)
            {
                sb.AppendLine();
                sb.AppendLine("[고통 감내 발동]");
                sb.AppendLine($"즉시 피해: {result.ImmediateDamage:N0}");
                sb.AppendLine($"분산 피해: {result.DotDamagePerTurn:N0}/턴 × {result.DotDuration}턴");
            }
            
            sb.AppendLine();
            sb.AppendLine($"[최종 피해: {result.FinalDamage:N0}]");

            return sb.ToString();
        }

        #endregion
    }

    #region 입출력 클래스

    /// <summary>
    /// PVP 계산 입력
    /// </summary>
    public class PvpInput
    {
        // 스탯
        public PvpCharacterStats MyStats { get; set; }
        public PvpCharacterStats EnemyStats { get; set; }
        
        // 스킬
        public Skill MySkill { get; set; }
        public Skill EnemySkill { get; set; }
        public bool MySkillEnhanced { get; set; }
        public bool EnemySkillEnhanced { get; set; }
        public int MyTranscendLevel { get; set; }
        public int EnemyTranscendLevel { get; set; }
        
        // 전투 옵션
        public PvpCombatOptions MyOptions { get; set; }
        public PvpCombatOptions EnemyOptions { get; set; }
        
        // 디버프 (상대에게 건 디버프)
        public PvpDebuffs MyDebuffsOnEnemy { get; set; }
        public PvpDebuffs EnemyDebuffsOnMe { get; set; }
    }

    /// <summary>
    /// PVP 캐릭터 스탯
    /// </summary>
    public class PvpCharacterStats
    {
        // 기본 스탯
        public double Atk { get; set; }
        public double Def { get; set; }
        public double Hp { get; set; }

        // 공격 관련
        public double CritRate { get; set; }
        public double CritDamage { get; set; }
        public double WeakRate { get; set; }
        public double WeakDamage { get; set; }
        public double WeakDamageBuff { get; set; }  // 약점 피해 증가 버프
        public double DamageDealt { get; set; }
        public double ArmorPen { get; set; }

        // 방어 관련
        public double DefIncrease { get; set; }
        public double DamageReduction { get; set; }
        public double BlockRate { get; set; }
        
        // 특수 방어
        public double Blessing { get; set; }                    // 축복량
        public PainEndurance PainEndurance { get; set; }        // 고통 감내

        // 효과
        public double EffHit { get; set; }
        public double EffRes { get; set; }
    }

    /// <summary>
    /// 고통 감내 (큰 피해를 분산해서 받음)
    /// </summary>
    public class PainEndurance
    {
        public double Threshold { get; set; }      // 발동 기준 (최대 HP의 %)
        public double ReductionRate { get; set; }  // 분산 비율%
        public int Duration { get; set; }          // 분산 턴 수
    }

    /// <summary>
    /// PVP 디버프 (상대에게 적용)
    /// </summary>
    public class PvpDebuffs
    {
        public double DefReduction { get; set; }      // 방깎
        public double DmgTakenIncrease { get; set; }  // 받피증
        public double Vulnerability { get; set; }     // 취약
        public double EffResReduction { get; set; }   // 효저깎
    }

    /// <summary>
    /// PVP 전투 옵션
    /// </summary>
    public class PvpCombatOptions
    {
        public bool IsCritical { get; set; } = true;
        public bool IsWeakpoint { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// PVP 계산 결과
    /// </summary>
    public class PvpResult
    {
        public PvpDamageResult MyDamageToEnemy { get; set; } = new PvpDamageResult();
        public PvpDamageResult EnemyDamageToMe { get; set; } = new PvpDamageResult();
    }

    /// <summary>
    /// 단일 방향 피해 결과
    /// </summary>
    public class PvpDamageResult
    {
        // 최종
        public double FinalDamage { get; set; }
        public double DamagePerHit { get; set; }
        public int AtkCount { get; set; }
        
        // 중간 계산값
        public double BaseDamage { get; set; }         // 공/방계수
        public double EffectiveDef { get; set; }       // 유효 방어력
        public double TotalArmorPen { get; set; }      // 총 방무
        
        // 계수/배율
        public double DefCoefficient { get; set; }     // 방어 계수
        public double SkillRatio { get; set; }         // 스킬 배율
        public double CritMultiplier { get; set; }     // 치명타 배율
        public double WeakpointMultiplier { get; set; } // 약점 배율
        public double DamageMultiplier { get; set; }   // 피해 증가 배율
        
        // 축복/고통 감내
        public double BlessingAbsorbed { get; set; }   // 축복으로 흡수된 피해
        public double ImmediateDamage { get; set; }    // 즉시 받는 피해 (고통 감내 적용 후)
        public double DotDamagePerTurn { get; set; }   // 턴당 분산 피해
        public int DotDuration { get; set; }           // 분산 턴 수
        public bool PainEnduranceTriggered { get; set; } // 고통 감내 발동 여부
        
        // 상세 정보
        public string Details { get; set; }
    }

    #endregion
}
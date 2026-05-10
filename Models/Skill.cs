using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Normal,     // 평타
        Normal2,    // 평타 2
        Skill1,     // 스킬 1
        Skill2,     // 스킬 2
        Skill3,     // 스킬 3
        Skill4,     // 스킬 4
        Ultimate    // 궁극기
    }

    /// <summary>
    /// 스킬
    /// </summary>
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SkillType SkillType { get; set; }
        public int TargetCount { get; set; } = 1;
        public int Atk_Count { get; set; } = 1;
        public int Ticks { get; set; } = 1;
        public double CooldownSeconds { get; set; }     // 쿨다운 (초)
        public double ActionDuration { get; set; }      // 행동 소요시간 (초), 0이면 기본값 사용

        /// <summary>
        /// 행동 소요시간 반환 (0이면 SkillType별 기본값)
        /// </summary>
        public double GetActionDuration()
        {
            if (ActionDuration > 0) return ActionDuration;
            return SkillType switch
            {
                SkillType.Normal or SkillType.Normal2 => 2.0,
                SkillType.Skill1 or SkillType.Skill2 => 4.0,
                SkillType.Skill3 or SkillType.Skill4 => 4.0,
                SkillType.Ultimate => 4.0,
                _ => 2.0
            };
        }

        // 레벨별 데이터 (0=기본, 1=강화)
        public Dictionary<int, SkillLevelData> LevelData { get; set; } = new();

        // 초월별 추가 효과
        public Dictionary<int, SkillTranscend> TranscendBonuses { get; set; } = new();

        // === 헬퍼 메서드 ===

        public SkillLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new SkillLevelData();
        }

        public SkillTranscend GetTranscendBonus(int level)
{
    var result = new SkillTranscend();
    if (TranscendBonuses == null) return result;

    foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
    {
        result.Bonus.Add(kvp.Value.Bonus);
        result.Debuff.Add(kvp.Value.Debuff);
        
        if (kvp.Value.TargetCountOverride.HasValue)
            result.TargetCountOverride = kvp.Value.TargetCountOverride;
        
        result.Effect = kvp.Value.Effect;
        
        // ✅ ConsumeExtra 합산 추가
        if (kvp.Value.ConsumeExtra != null)
        {
            if (result.ConsumeExtra == null)
            {
                result.ConsumeExtra = new ConsumeExtraDamage();
            }
            result.ConsumeExtra.TargetMaxHpRatio += kvp.Value.ConsumeExtra.TargetMaxHpRatio;
            result.ConsumeExtra.AtkRatio += kvp.Value.ConsumeExtra.AtkRatio;
            result.ConsumeExtra.ConsumeCount += kvp.Value.ConsumeExtra.ConsumeCount;
            // AtkCap은 덮어쓰기 (보통 변하지 않음)
            if (kvp.Value.ConsumeExtra.AtkCap > 0)
                result.ConsumeExtra.AtkCap = kvp.Value.ConsumeExtra.AtkCap;
        }
    }
    return result;
}

        public int GetTargetCount(int transcendLevel)
        {
            var bonus = GetTranscendBonus(transcendLevel);
            return bonus.TargetCountOverride ?? TargetCount;
        }

        /// <summary>
        /// 스킬 레벨 + 초월 보너스 합산된 BuffSet 반환 (스킬 계산 전용)
        /// </summary>
        public BuffSet GetTotalBonus(bool isEnhanced, int transcendLevel)
        {
            var result = new BuffSet();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.Bonus != null) result.Add(levelData.Bonus);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Bonus != null) result.Add(transcend.Bonus);

            return result;
        }

        public ConsumeExtraDamage GetTotalConsumeExtra(bool isEnhanced, int transcendLevel)
{
    var levelData = GetLevelData(isEnhanced);
    var transcend = GetTranscendBonus(transcendLevel);

    // 둘 다 없으면 null
    if (levelData?.ConsumeExtra == null && transcend?.ConsumeExtra == null)
        return null;

    var result = new ConsumeExtraDamage();
    
    // 레벨 데이터가 있으면 복사
    if (levelData?.ConsumeExtra != null)
    {
        result.ConsumeCount = levelData.ConsumeExtra.ConsumeCount;
        result.TargetMaxHpRatio = levelData.ConsumeExtra.TargetMaxHpRatio;
        result.AtkCap = levelData.ConsumeExtra.AtkCap;
        result.AtkRatio = levelData.ConsumeExtra.AtkRatio;
    }

    // 초월 보너스 합산
    if (transcend?.ConsumeExtra != null)
    {
        result.TargetMaxHpRatio += transcend.ConsumeExtra.TargetMaxHpRatio;
        result.AtkRatio += transcend.ConsumeExtra.AtkRatio;
    }

    return result;
}
    }

    /// <summary>
    /// 스킬이 부여하는 상태이상 인스턴스
    /// </summary>
    public class SkillStatusEffect
    {
        public StatusEffectType Type { get; set; }
        public int MaxConsume { get; set; } = 0;            // 최대 소모 개수
        public int Duration { get; set; }                   // 지속 턴
        public int Stacks { get; set; } = 1;                // 부여 스택
        public double Chance { get; set; } = 100;           // 부여 확률%
        public string Condition { get; set; }               // 부여 조건

        // 커스텀 값 (기본값 오버라이드)
        public double? CustomAtkRatio { get; set; }
        public double? CustomHpRatio { get; set; }
        public double? CustomAtkCap { get; set; }
        public double? CustomArmorPen { get; set; }
        public double? CustomFixedDamage { get; set; }
        public double? CustomTargetMaxHpRatio { get; set; }
        public double? CustomTargetCurrentHpRatio { get; set; }
        public double? CustomHpConversionRatio { get; set; }
        public double? CustomTriggerCount { get; set; }
    }

    /// <summary>
    /// 스킬 레벨별 데이터 (0=기본, 1=강화)
    /// </summary>
    public class SkillLevelData
    {
        // ===== 배율 =====
        public double Ratio { get; set; }               // 공격력 비례
        public double DefRatio { get; set; }            // 방어력 비례
        public double HpRatio { get; set; }             // 생명력 비례
        public double SpdRatio { get; set; }            // 속공 비례

        // ===== 조건부 효과 =====
        public double ConditionalRatioBonus { get; set; }
        public double ConditionalExtraDmg { get; set; }
        public double ConditionalExtraDmgSelfHpRatio { get; set; }
        public string ConditionalDesc { get; set; }
        public bool ConditionalExtraDmgPerHit { get; set; }
        public double ConditionalDmgBonus { get; set; }

        // ===== 스킬 자체 보너스 (해당 스킬 계산에만 적용) =====
        public BuffSet Bonus { get; set; } = new BuffSet();
        public BuffSet PreCastBuff { get; set; }        // 스킬 발동 전 적용, 스킬 끝나면 소멸

        // ===== 턴제 버프 (파티/본인에게 부여) =====
        public TimedBuff SelfBuff { get; set; }           // 본인 전용 버프
        public TimedBuff PartyBuff { get; set; }          // 아군 전체 버프

        // ===== 턴제 디버프 =====
        public TimedDebuff DebuffEffect { get; set; }
        public int EffectDuration { get; set; }
        public double EffectChance { get; set; } = 0;
        public double DispelDefReduction { get; set; }  // 버프 해제 연계 방깎%

        // ===== 회복 =====
        public double HealAtkRatio { get; set; }
        public double HealDefRatio { get; set; }
        public double HealHpRatio { get; set; }
        public double HealDmgRatio { get; set; }        // 피해량 비례 회복%

        // ===== 생명력 비례 피해 =====
        public double TargetMaxHpRatio { get; set; }
        public double TargetCurrentHpRatio { get; set; }
        public double AtkCap { get; set; }

        // ===== 잃은 HP 비례 =====
        public double LostHpBonusDmgMax { get; set; }
        public double LostHpAssumedRemaining { get; set; } // 특정조건 시 대상 잔여HP% (예: 30 = 30%남음 → 70%손실)

        // ===== 상태이상 =====
        // 새 Effects가 있으면 자동 변환, 없으면 레거시 필드 사용
        private List<SkillStatusEffect> _statusEffects = new List<SkillStatusEffect>();
        public List<SkillStatusEffect> StatusEffects
        {
            get
            {
                if (_statusEffects.Count > 0) return _statusEffects;
                if (Effects == null || Effects.Count == 0) return _statusEffects;
                // Effects에서 상태이상만 추출하여 레거시 형식으로 변환
                var converted = new List<SkillStatusEffect>();
                foreach (var e in Effects)
                {
                    if (e.Type == SkillEffectType.StatusAilment)
                    {
                        converted.Add(new SkillStatusEffect
                        {
                            Type = e.StatusType,
                            Stacks = e.Stacks,
                            Chance = e.Chance,
                            Duration = e.Duration,
                            MaxConsume = e.MaxConsume,
                            CustomAtkRatio = e.CustomAtkRatio,
                            CustomHpRatio = e.CustomHpRatio,
                            CustomAtkCap = e.CustomAtkCap,
                            CustomArmorPen = e.CustomArmorPen,
                            CustomFixedDamage = e.CustomFixedDamage,
                            CustomTargetMaxHpRatio = e.CustomTargetMaxHpRatio,
                            CustomTargetCurrentHpRatio = e.CustomTargetCurrentHpRatio,
                            CustomHpConversionRatio = e.CustomHpConversionRatio,
                            CustomTriggerCount = e.CustomTriggerCount,
                        });
                    }
                }
                return converted;
            }
            set => _statusEffects = value ?? new List<SkillStatusEffect>();
        }

        // ===== 통합 효과 리스트 (새 방식) =====
        public List<SkillEffect> Effects { get; set; }

        // ===== 기타 =====
        public string Effect { get; set; }
        public int? TargetCountOverride { get; set; }
        public ConsumeExtraDamage ConsumeExtra { get; set; }
        public double FixedDamage { get; set; }

        // 처치 시 같은 스킬 1회 재시전 (모든 효과 재적용, 연쇄 없음)
        public OnKillRecast OnKillRecast { get; set; }
    }

    /// <summary>
    /// 스킬 초월 보너스
    /// </summary>
    public class SkillTranscend
    {
        // 스킬 자체 보너스 (해당 스킬 계산에만 적용)
        public BuffSet Bonus { get; set; } = new BuffSet();
        
        // 턴제 버프/디버프
        public TimedBuff PartyBuff { get; set; } = new TimedBuff();
        public TimedDebuff Debuff { get; set; } = new TimedDebuff();
        public int? TargetCountOverride { get; set; }

        // 조건부 효과
        public double ConditionalDmgBonus { get; set; }
        public double ConditionalExtraDmg { get; set; }
        public double ConditionalExtraDmgSelfHpRatio { get; set; }

        // 상태이상
        private List<SkillStatusEffect> _statusEffects = new List<SkillStatusEffect>();
        public List<SkillStatusEffect> StatusEffects
        {
            get
            {
                if (_statusEffects.Count > 0) return _statusEffects;
                if (Effects == null || Effects.Count == 0) return _statusEffects;
                var converted = new List<SkillStatusEffect>();
                foreach (var e in Effects)
                {
                    if (e.Type == SkillEffectType.StatusAilment)
                    {
                        converted.Add(new SkillStatusEffect
                        {
                            Type = e.StatusType,
                            Stacks = e.Stacks,
                            Chance = e.Chance,
                            Duration = e.Duration,
                            MaxConsume = e.MaxConsume,
                            CustomAtkRatio = e.CustomAtkRatio,
                            CustomHpRatio = e.CustomHpRatio,
                            CustomAtkCap = e.CustomAtkCap,
                            CustomArmorPen = e.CustomArmorPen,
                            CustomFixedDamage = e.CustomFixedDamage,
                            CustomTargetMaxHpRatio = e.CustomTargetMaxHpRatio,
                            CustomTargetCurrentHpRatio = e.CustomTargetCurrentHpRatio,
                            CustomHpConversionRatio = e.CustomHpConversionRatio,
                            CustomTriggerCount = e.CustomTriggerCount,
                        });
                    }
                }
                return converted;
            }
            set => _statusEffects = value ?? new List<SkillStatusEffect>();
        }
        public ConsumeExtraDamage ConsumeExtra { get; set; }

        // 통합 효과 리스트 (새 방식)
        public List<SkillEffect> Effects { get; set; }

        // 생명력 비례 피해
        public double TargetMaxHpRatio { get; set; }
        public double TargetCurrentHpRatio { get; set; }
        public double AtkCap { get; set; }
        public double HealHpRatio { get; set; }

        public double HealAtkRatio { get; set; }
        public string Effect { get; set; }

        // 처치 시 같은 스킬 1회 재시전 (모든 효과 재적용, 연쇄 없음)
        public OnKillRecast OnKillRecast { get; set; }
    }

    /// <summary>
    /// 처치 시 같은 스킬을 n% 위력으로 1회 추가 발동
    /// 적 종류(일반몹/영웅/보스) 무관하게 트리거. 재발동된 스킬의 처치로는 재시전되지 않음(연쇄 없음).
    /// 데미지·버프·디버프·상태이상 등 스킬의 모든 효과가 다시 적용됨.
    /// </summary>
    public class OnKillRecast
    {
        public double RatioPercent { get; set; } = 100;   // 원본 배율 대비 % (70 = 0.7배)
        public double Chance { get; set; } = 100;         // 발동 확률%
    }

    /// <summary>
    /// 스택 소모형 추가 피해
    /// </summary>
    public class ConsumeExtraDamage
    {
        public int ConsumeCount { get; set; }           // 소모 개수 (4)
        public double AtkRatio { get; set; }            // 공격력 비례% (39)
        public double DefRatio { get; set; }            // 방어력 비례% (45)
        public double Arm_Pen { get; set; }             // 관통 여부
        public double TargetMaxHpRatio { get; set; }    // 대상 최대 HP%
        public double AtkCap { get; set; }              // 공격력 제한%
    }
}
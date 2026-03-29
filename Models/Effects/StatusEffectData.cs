namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// StatusEffect의 해석된 스냅샷 (DB 기본값 + 스킬 커스텀 오버라이드 반영)
    /// StatusEffect(DB 정의)와 달리, Name/Description 없이 순수 수치만 보관
    /// </summary>
    public class StatusEffectData
    {
        // 피해 관련
        public double AtkRatio { get; set; }
        public double TargetMaxHpRatio { get; set; }
        public double TargetCurrentHpRatio { get; set; }
        public double AtkCap { get; set; }
        public double ArmorPen { get; set; }
        public double FixedDamage { get; set; }
        public double ChainDamage { get; set; }

        // 추가 효과
        public double HealRatio { get; set; }
        public bool IsGuaranteedCrit { get; set; }
        public double MissChanceIncrease { get; set; }
        public double DamageReduction { get; set; }
        public bool BlocksBlock { get; set; }
        public bool BlocksHeal { get; set; }
        public bool BlocksAction { get; set; }
        public bool BlocksActiveSkill { get; set; }
        public double WakeUpThreshold { get; set; }

        // 생명력 전환
        public double HpConversionAmount { get; set; }
        public bool IsHpConversion { get; set; }

        // 소모형 효과
        public StatusEffectType? ConsumeType { get; set; }
        public int MaxConsume { get; set; }
        public int DefaultRemainingTurns { get; set; } = 2;
        public int TriggerCount { get; set; }

        /// <summary>
        /// StatusEffect DB 정의에서 생성
        /// </summary>
        public static StatusEffectData FromDbEffect(StatusEffect dbEffect)
        {
            if (dbEffect == null) return new StatusEffectData();
            return new StatusEffectData
            {
                AtkRatio = dbEffect.AtkRatio,
                TargetMaxHpRatio = dbEffect.TargetMaxHpRatio,
                TargetCurrentHpRatio = dbEffect.TargetCurrentHpRatio,
                AtkCap = dbEffect.AtkCap,
                ArmorPen = dbEffect.ArmorPen,
                FixedDamage = dbEffect.FixedDamage,
                ChainDamage = dbEffect.ChainDamage,
                HealRatio = dbEffect.HealRatio,
                IsGuaranteedCrit = dbEffect.IsGuaranteedCrit,
                MissChanceIncrease = dbEffect.MissChanceIncrease,
                DamageReduction = dbEffect.DamageReduction,
                BlocksBlock = dbEffect.BlocksBlock,
                BlocksHeal = dbEffect.BlocksHeal,
                BlocksAction = dbEffect.BlocksAction,
                BlocksActiveSkill = dbEffect.BlocksActiveSkill,
                WakeUpThreshold = dbEffect.WakeUpThreshold,
                HpConversionAmount = dbEffect.HpConversionAmount,
                IsHpConversion = dbEffect.IsHpConversion,
                ConsumeType = dbEffect.ConsumeType,
                MaxConsume = dbEffect.MaxConsume,
                DefaultRemainingTurns = dbEffect.DefaultRemainingTurns,
                TriggerCount = dbEffect.TriggerCount,
            };
        }
    }
}

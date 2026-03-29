namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// 스킬이 발동하는 턴제 효과 (버프/디버프/상태이상)
    /// SkillLevelData.Effects, SkillTranscend.Effects에서 사용
    /// </summary>
    public class SkillEffect
    {
        // === 대상 ===
        public EffectTarget Target { get; set; }     // Self, Party, Enemy
        public int TargetCount { get; set; } = 0;    // 0이면 스킬 기본 대상 수 사용

        // === 유형 ===
        public SkillEffectType Type { get; set; }

        // === 지속 ===
        public int Duration { get; set; }            // 지속 턴 (0이면 기본값 사용)
        public double Chance { get; set; } = 100;    // 적용 확률%

        // === 버프/디버프 스탯 (Type = Buff 또는 Debuff일 때) ===
        public BuffSet Buff { get; set; }
        public DebuffSet Debuff { get; set; }

        // === 상태이상 (Type = StatusAilment일 때) ===
        public StatusEffectType StatusType { get; set; }
        public int Stacks { get; set; } = 1;

        // 상태이상 커스텀 오버라이드
        public double? CustomAtkRatio { get; set; }
        public double? CustomHpRatio { get; set; }
        public double? CustomAtkCap { get; set; }
        public double? CustomArmorPen { get; set; }
        public double? CustomFixedDamage { get; set; }
        public double? CustomTargetMaxHpRatio { get; set; }
        public double? CustomTargetCurrentHpRatio { get; set; }
        public double? CustomHpConversionRatio { get; set; }
        public double? CustomTriggerCount { get; set; }
        public int MaxConsume { get; set; }
    }

    /// <summary>
    /// 스킬 효과 유형
    /// </summary>
    public enum SkillEffectType
    {
        Buff,           // 버프 (아군 스탯 증가)
        Debuff,         // 디버프 (적 스탯 감소)
        StatusAilment,  // 상태이상 (화상, 기절 등)
    }
}

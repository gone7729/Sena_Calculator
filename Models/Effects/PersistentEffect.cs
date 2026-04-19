namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// 패시브/지속 효과 (항상 적용되거나 조건 충족 시 적용)
    /// Passive.Effects에서 사용
    /// </summary>
    public class PersistentEffect
    {
        // === 대상 ===
        public EffectTarget Target { get; set; }     // Self, Party, Enemy

        // === 유형 ===
        public PersistentEffectType Type { get; set; }

        // === 조건 ===
        public bool IsConditional { get; set; }      // 조건부 여부
        public string Condition { get; set; }        // 조건 설명 (UI 표시용)

        // === 적용 모드 & 스택 트리거 ===
        public ApplyMode ApplyMode { get; set; } = ApplyMode.Immediate;
        public TriggerCondition TriggerCondition { get; set; }  // Triggered일 때 조건
        public int TriggerCount { get; set; } = 1;              // 트리거에 필요한 횟수
        public int StacksPerTrigger { get; set; } = 1;          // 트리거당 부여 스택 수
        public int MaxStacks { get; set; }                      // 최대 스택 (0이면 무제한)

        // === 버프/디버프 스탯 (Type = Buff 또는 Debuff일 때) ===
        public BuffSet Buff { get; set; }
        public DebuffSet Debuff { get; set; }

        // === 상태이상 (Type = StatusAilment일 때) ===
        public StatusEffectType StatusType { get; set; }
        public int Stacks { get; set; } = 1;
        public double Chance { get; set; } = 100;

        // 상태이상 커스텀 오버라이드 (SkillEffect와 동일)
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

        // === 협공 (Type = CoopAttack일 때) ===
        public CoopAttack CoopAttack { get; set; }

        // === 표식 (Type = MarkAttack일 때) ===
        public MarkAttack MarkAttack { get; set; }

        // === 스탯 스케일링 (Type = StatScaling일 때) ===
        public StatScaling StatScaling { get; set; }

        // === 고통 인내 (Type = PainEndurance일 때) ===
        public PainEndurance PainEndurance { get; set; }

        // === 고정 스탯 보너스 (Type = FlatBonus일 때) ===
        public BaseStatSet FlatBonus { get; set; }
    }

    /// <summary>
    /// 지속 효과 유형
    /// </summary>
    public enum PersistentEffectType
    {
        Buff,           // 패시브 버프 (공격력%, 피해량% 등)
        Debuff,         // 패시브 디버프 (방깎, 받피증 등)
        StatusAilment,  // 패시브 상태이상 (카일 체인데미지 등)
        CoopAttack,     // 협공
        MarkAttack,     // 표식
        StatScaling,    // 스탯 변환 (속공→공격력 등)
        PainEndurance,  // 고통 인내
        FlatBonus,      // 고정 스탯 보너스
    }
}

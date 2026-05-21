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

        // === PerEnemyDebuffDmgBonus 전용 (Type = PerEnemyDebuffDmgBonus일 때) ===
        // 적 디버프 1개당 피증%, 카운트 상한
        public double PercentPerDebuff { get; set; }
        public int MaxDebuffStacks { get; set; }

        // === 트리거형 고정 데미지 (Type = TriggeredFixedDamage일 때) ===
        public TriggeredFixedDamage TriggeredFixedDamage { get; set; }

        // === 피해 무효화 (Type = DamageNullification일 때) ===
        public DamageNullification DamageNullification { get; set; }

        // === 상태이상 면역 (Type = Immunity일 때) ===
        public StatusImmunity StatusImmunity { get; set; }

        // === 트리거 회복 (ApplyMode = Triggered, 시전자 공격력 비례 회복%) ===
        public double TriggeredHealAtkRatio { get; set; }
    }

    /// <summary>
    /// 지속 효과 유형
    ///
    /// 두 그룹이 한 enum에 모여 있다:
    ///   (1) BattleEffect 추상화에 들어가는 것 — Buff / Debuff / StatusAilment.
    ///       EffectManager가 BuffSet/DebuffSet/StatusType으로 집계.
    ///   (2) BattleEffect로 표현하기에 본질이 다른 특수 메카닉 — 추가 공격, 스탯 변환,
    ///       피해 분산, 깡스탯 보너스. EffectConverter.FromPersistentEffects가 이들은
    ///       BattleEffect로 변환하지 않고 (continue), Passive.LevelData의 fallback
    ///       getter(_coopAttack, _markAttack, _statScalings, _painEndurance, _flatBonus)
    ///       를 통해 DamageCalculator/StatCalculator가 직접 소비한다.
    ///   같은 enum 안에 둔 이유: CharacterDB 데이터를 한 List(Effects)로 통일하기 위함.
    /// </summary>
    public enum PersistentEffectType
    {
        // (1) BattleEffect로 변환되는 효과
        Buff,           // 패시브 버프 (공격력%, 피해량% 등)
        Debuff,         // 패시브 디버프 (방깎, 받피증 등)
        StatusAilment,  // 패시브 상태이상 (카일 체인데미지 등)

        // (2) BattleEffect로 변환되지 않는 특수 메카닉 (별도 채널 소비)
        CoopAttack,     // 협공 — DamageCalculator가 직접 소비
        MarkAttack,     // 표식 — DamageCalculator가 직접 소비
        StatScaling,    // 스탯 변환 (속공→공격력 등) — StatCalculator가 직접 소비
        PainEndurance,  // 고통 인내 (받피해 분산)
        FlatBonus,      // 고정 스탯 보너스 — StatCalculator가 직접 소비
        PerEnemyDebuffDmgBonus, // 적 디버프 1개당 피증 — DamageCalculator가 직접 소비 (동적)
        TriggeredFixedDamage,   // N회 공격마다 적군 N명에게 고정 데미지 (발리스타 등)
        DamageNullification,    // 피해 무효화 (피격 N회 / N턴 / 물·마 한정)
        Immunity,               // 상태이상 면역 (화상 면역 등)
        TriggeredHeal,          // 트리거 시 시전자 공격력 비례 회복 (TriggeredHealAtkRatio)
    }

    /// <summary>
    /// N회 공격마다 적군 N명에게 추가 피해를 주는 패시브 메카닉
    /// 피해는 고정값(FixedDamage) 또는 공격력 비례(AtkRatio) 중 한 가지를 사용.
    ///   예) 발리스타 - 모든 공격 3회 발동 시 적군 3명에게 1285 고정 데미지 (FixedDamage 사용)
    ///   예) 여포 - 기본공격 2회마다 1명에게 공격력 45% 추가 피해 (AtkRatio 사용)
    /// 동일 효과가 base/초월 양쪽에 정의된 경우 가장 나중 값(높은 초월)이 이김.
    /// </summary>
    public class TriggeredFixedDamage
    {
        public int TriggerCount { get; set; }                                          // 발동에 필요한 횟수 (3 = 3회마다)
        public TriggerCondition TriggerOn { get; set; } = TriggerCondition.AllAttack;  // 무엇을 카운트하는지
        public double FixedDamage { get; set; }                                        // 1회 발동 시 고정 데미지값
        public double AtkRatio { get; set; }                                           // 1회 발동 시 시전자 공격력 비례% (FixedDamage 대신 사용 가능)
        public int TargetCount { get; set; } = 1;                                      // 적 명수
        public int HitCount { get; set; } = 1;                                         // 발동당 타격 횟수
    }

    /// <summary>
    /// 피해 무효화 — 피격 N회 또는 N턴 동안 (특정 피해 타입 한정 가능)
    ///   예) 타카 - 스킬 1회 발동 시 모든 피해 무효화[피격 1회]  (HitCount=1, Type=All, 트리거)
    ///   예) 델론즈 - 자신 모든 피해 무효화[피격 3회]            (HitCount=3, Type=All)
    ///   예) 라이언 - 물리 피해 면역[2턴]                        (Duration=2, Type=Physical)
    /// </summary>
    public class DamageNullification
    {
        public int HitCount { get; set; }                              // 무효화 피격 횟수 (0이면 턴제만)
        public int Duration { get; set; }                              // 지속 턴 (0이면 횟수형)
        public DamageNullType Type { get; set; } = DamageNullType.All; // 무효화 대상 피해 타입
    }

    /// <summary>
    /// 상태이상 면역 — 지정 상태이상에 N턴 동안 면역
    ///   예) 라이언 - 아군 화상 면역[2턴]  (Types=[Burn], Duration=2)
    /// </summary>
    public class StatusImmunity
    {
        public StatusEffectType[] Types { get; set; }   // 면역 대상 상태이상
        public int Duration { get; set; }               // 지속 턴
    }
}

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
        public TargetSelector? TargetSelector { get; set; }  // 대상 선정 기준 (열 우선·생명력 낮은 아군·공격력 높은 아군 등). null이면 기본
        // 직업군 제한 (Party 대상일 때만 의미) — null/빈 배열이면 전체 아군. "공격형","만능형" 등
        public string[] TargetClasses { get; set; }

        // === 유형 ===
        public SkillEffectType Type { get; set; }

        // === 적용 순서 (툴팁 위→아래) ===
        // true면 이 효과를 스킬 피해 前에 적용 (예: 레이첼 불새 방깎→피해, 미호 턴감소→피해 = 피해 증폭/면역관통).
        // 기본 false = 피해 後 적용 (예: 오를리 유성 피해→버프해제, 레이첼 염화 피해→공감).
        public bool PreDamage { get; set; }

        // === 지속 ===
        public int Duration { get; set; }            // 지속 턴 (0이면 기본값 사용)
        public double Chance { get; set; } = 100;    // 적용 확률%

        // === 턴제 버프 감소 (Type = BuffTurnReduction일 때) ===
        public int TurnReduction { get; set; }       // 대상의 활성 턴제 버프 잔여 턴을 N턴 감소

        // === 버프 해제 (Type = BuffDispel일 때) — 대상(적)의 버프 N개 제거 ===
        // 적용 순서대로(먼저 부여된 것부터) 해제. 피해 면역/피해 무효화/권능 효과는 해제 불가(스킵).
        public int DispelBuffCount { get; set; }

        // === 디버프 해제 (Type = DebuffCleanse일 때) — 대상(아군)의 디버프 N개 제거 (예: 미호 초월2 아군 후열 해제) ===
        public int DispelDebuffCount { get; set; }

        // === 적용 모드 & 스택 트리거 ===
        public ApplyMode ApplyMode { get; set; } = ApplyMode.Immediate;
        public TriggerCondition TriggerCondition { get; set; }  // Triggered일 때 조건
        public int TriggerCount { get; set; } = 1;              // 트리거에 필요한 횟수
        public int StacksPerTrigger { get; set; } = 1;          // 트리거당 부여 스택 수
        public int MaxStacks { get; set; }                      // 최대 스택 (0이면 무제한)

        // === 발동 조건: 대상이 특정 상태이상일 때만 적용 (예: 상대 출혈 시 확정 출혈) ===
        public StatusEffectType? RequiresStatusType { get; set; }

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

        // === PerEnemyDebuffDmgBonus 전용 (Type = PerEnemyDebuffDmgBonus일 때) ===
        // 적 디버프 1개당 피증%, 카운트 상한
        public double PercentPerDebuff { get; set; }
        public int MaxDebuffStacks { get; set; }

        // === 피해 무효화 (Type = DamageNullification일 때) ===
        public DamageNullification DamageNullification { get; set; }

        // === 상태이상 면역 (Type = Immunity일 때) ===
        public StatusImmunity StatusImmunity { get; set; }

        // === 부활 (Type = Revive일 때) — 사망 아군 TargetCount명을 생명력 ReviveHpPercent%로 부활 ===
        public double ReviveHpPercent { get; set; }
    }

    /// <summary>
    /// 스킬 효과 유형
    /// </summary>
    public enum SkillEffectType
    {
        Buff,           // 버프 (아군 스탯 증가)
        Debuff,         // 디버프 (적 스탯 감소)
        StatusAilment,  // 상태이상 (화상, 기절 등)
        PerEnemyDebuffDmgBonus, // 적 디버프 1개당 피해량 증가 (동적 피증)
        DamageNullification,    // 피해 무효화 (피격 N회 / N턴 / 물·마 한정)
        Immunity,               // 상태이상 면역 (화상 면역 등)
        BuffTurnReduction,      // 대상의 활성 턴제 버프 잔여 턴 감소 (TurnReduction)
        BuffDispel,             // 대상(적)의 버프 N개 해제 (DispelBuffCount, 적용순·면역/무효화/권능 제외)
        DebuffCleanse,          // 대상(아군)의 디버프 N개 해제 (DispelDebuffCount)
        Revive,                 // 사망 아군 부활 (TargetCount명, 생명력 ReviveHpPercent%)
    }
}

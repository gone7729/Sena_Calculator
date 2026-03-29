namespace GameDamageCalculator.Models.Effects
{
    /// <summary>
    /// 효과 대상
    /// </summary>
    public enum EffectTarget
    {
        Self,           // 시전자 본인
        Party,          // 아군 전체
        SingleAlly,     // 아군 1명
        Enemy,          // 적 1명
        AllEnemies      // 적 전체
    }

    /// <summary>
    /// 효과 카테고리 (병합 그룹 결정)
    /// 같은 카테고리 내에서는 MaxMerge, 카테고리 간에는 Add
    /// </summary>
    public enum EffectCategory
    {
        // === 버프 ===
        PassiveSelfBuff,        // 패시브 자버프 (상시)
        PassivePartyBuff,       // 패시브 파티버프 (상시)
        ConditionalSelfBuff,    // 조건부 패시브 자버프 (턴제)
        ConditionalPartyBuff,   // 조건부 패시브 파티버프 (턴제)
        ActiveSelfBuff,         // 스킬 자버프 (턴제)
        ActivePartyBuff,        // 스킬 파티버프 (턴제)
        PetBuff,                // 펫 버프 (항상 합산)
        SkillBonus,             // 스킬 보너스 (해당 스킬 계산에만 적용)

        // === 디버프 ===
        PassiveDebuff,          // 패시브 디버프 (상시)
        ConditionalDebuff,      // 조건부 패시브 디버프 (턴제)
        ActiveDebuff,           // 스킬 디버프 (턴제)
        PetDebuff,              // 펫 디버프 (항상 합산)

        // === 상태이상 ===
        CrowdControl,           // CC (기절, 빙결 등)
        DamageOverTime,         // DoT (화상, 출혈, 중독 등)
        SpecialStatus,          // 특수 (폭탄, 수정, HP전환 등)
    }

    /// <summary>
    /// 병합 전략
    /// </summary>
    public enum MergeStrategy
    {
        MaxMerge,   // 같은 카테고리 내 각 속성 최대값 (기본)
        Additive,   // 항상 합산 (펫, 카테고리 간)
        Stack,      // 스택 증가 (상태이상)
        Replace,    // 새 효과가 기존 대체
    }
}

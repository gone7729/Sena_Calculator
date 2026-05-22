namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 상태이상 타입
    /// </summary>
    public enum StatusEffectType
    {
        None,

        // === CC (행동 불가) ===
        Stun,           // 기절
        Silence,        // 침묵
        Freeze,         // 빙결
        Petrify,        // 석화
        IceExtreme,     // 빙극
        Paralysis,      // 마비
        Shock,          // 감전
        Sleep,          // 수면
        Confusion,      // 혼란
        Concussion,     // 진탕
        Blind,          // 실명 (공격 빗나감 유발)
        Taunt,          // 도발 (시전자만 공격 대상이 됨)

        // === DoT (지속 피해) ===
        Burn,           // 화상
        InstantDeath,   // 즉사
        ManaBackflow,   // 마력 역류
        Bleeding,       // 출혈
        Poison,         // 중독
        ChainDamage,    // 카일꺼

        // === 패시브 스택형 ===
        EagleClaw,      // 매의 발톱 (타카)

        // === 특수 ===
        Bomb,             // 폭탄
        BombDetonation,   // 폭탄 폭파
        BleedExplosion,   // 출혈 폭발
        Crystal,          // 수정 결정
        CrystalResonance, // 수정 공명
        Miss,             // 빗나감
        HealBlock,        // 회복 불가
        HpConversion,     // 생명력 전환
        Regeneration,     // 재생 (턴제 회복)
        Disguise,         // 위장 (1인 공격 비대상 + 위장 중 피격 확정 빗나감)
    }
}

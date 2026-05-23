using System.Collections.Generic;

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
        // 직업군 제한 (Party 대상일 때만 의미) — null/빈 배열이면 전체 아군.
        // Character.Type 문자열 매칭 (예: "공격형", "만능형", "마법형", "지원형", "방어형")
        public string[] TargetClasses { get; set; }
        public TargetSelector? TargetSelector { get; set; }  // 대상 선정 기준 (공격력 높은 아군·생명력 낮은 아군 등). null이면 기본

        // === 유형 ===
        public PersistentEffectType Type { get; set; }

        // === 조건 ===
        public bool IsConditional { get; set; }      // 조건부 여부
        public string Condition { get; set; }        // 조건 설명 (UI 표시용)

        // === 적용 모드 & 스택 트리거 ===
        public ApplyMode ApplyMode { get; set; } = ApplyMode.Immediate;
        public TriggerCondition TriggerCondition { get; set; }  // Triggered일 때 조건
        public int TriggerCount { get; set; } = 1;              // 트리거에 필요한 횟수
        public double TriggerHpThreshold { get; set; }          // OnHpBelow 트리거 임계 생명력% (예: 70 = 70% 이하)
        public bool OncePerBattle { get; set; }                 // 트리거 효과를 전투당 1회만 발동
        public int MaxTriggersPerBattle { get; set; }           // 트리거 효과 전투당 최대 발동 횟수 (0=무제한, OncePerBattle보다 우선)
        public int StacksPerTrigger { get; set; } = 1;          // 트리거당 부여 스택 수
        public int MaxStacks { get; set; }                      // 최대 스택 (0이면 무제한)
        public bool IsPerStack { get; set; }                    // true면 Buff/Debuff가 "스택당" 값 (런타임에서 현재 스택수만큼 배수 적용). 사냥술·레벨업·신성 등
        public int Duration { get; set; }                       // 부여 효과 지속 턴 (트리거 디버프/버프, 0이면 무기한)

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

        // === 트리거 회복 (ApplyMode = Triggered, 시전자 스탯 비례 회복%) ===
        public double TriggeredHealAtkRatio { get; set; }   // 시전자 공격력 비례 회복%
        public double TriggeredHealDefRatio { get; set; }   // 시전자 방어력 비례 회복%
        public double TriggeredHealHpRatio { get; set; }    // 대상(자신) 최대 HP 비례 회복%

        // === 흡혈 (상시, 자신의 공격이 준 피해량 비례 회복%) — Type = Lifesteal ===
        public double LifestealRatio { get; set; }

        // === 불굴/부활 (Type = Revival일 때) ===
        public Revival Revival { get; set; }

        // === 디버프 해제 (Type = DebuffCleanse일 때) — 대상의 디버프 N개 제거 ===
        public int DispelDebuffCount { get; set; }

        // === 버프 해제 (Type = BuffDispel일 때) — 대상(적)의 버프 N개 제거 ===
        // 적용 순서대로(먼저 부여된 것부터) 해제. 피해 면역/피해 무효화/권능 효과는 해제 불가(스킵).
        public int DispelBuffCount { get; set; }

        // === 트리거 스킬 발동 (Type = TriggeredSkillCast일 때) — 적군 사망 등 트리거로 스킬 시전 ===
        public TriggeredSkillCast TriggeredSkillCast { get; set; }

        // === 쿨타임 초기화 (Type = CooldownReset일 때) — 불사 발동 등 트리거로 쿨타임 초기화 ===
        public CooldownReset CooldownReset { get; set; }

        // === 강자주시 (Type = FocusTarget일 때) — 특정 적군 고정 타게팅 ===
        public FocusTarget FocusTarget { get; set; }

        // === 권능 (Type = Authority일 때) — 치명적 피해 시 1회 생존 ===
        public Authority Authority { get; set; }
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
        TriggeredHeal,          // 트리거 시 시전자 스탯 비례 회복 (TriggeredHeal Atk/Def/HpRatio)
        Lifesteal,              // 흡혈 (상시, 준 피해량 비례 회복 — LifestealRatio)
        Revival,                // 불굴/부활 (사망 시 부활 + 피격 N회 또는 N턴 사망 무효)
        DebuffCleanse,          // 디버프 해제 (대상 디버프 N개 제거, DispelDebuffCount)
        TriggeredSkillCast,     // 트리거 시 스킬 시전 (적군 N명 사망 → 별개 스킬 발동, TriggeredSkillCast)
        CooldownReset,          // 트리거 시 쿨타임 초기화/감소 (불사 발동 등, CooldownReset)
        BuffDispel,             // 대상(적)의 버프 N개 해제 (DispelBuffCount, 적용순·면역/무효화/권능 제외)
        FocusTarget,            // 강자주시 — 특정 적군 고정 타게팅 (FocusTarget)
        Authority,              // 권능 — 현재 생명력 이상 피해 시 생명력 1로 1회 생존 (Authority)
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
        public int DispelBuffCount { get; set; }                                       // 발동 공격 시 대상 버프 해제 개수 (0이면 없음)
        public double DispelBuffChance { get; set; } = 100;                            // 버프 해제 확률%
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

    /// <summary>
    /// 불굴/불사/부활 — 사망 시 일정 생명력으로 부활하여 일정 동안 사망하지 않음.
    /// 무적 지속 방식 두 가지 (둘 중 하나 사용):
    ///   - HitCount > 0    : 피격 N회 동안 사망 무효 (불굴). 예) 카구라 「팔사의 저주」 [피격 8회]
    ///   - ImmortalTurns>0 : N턴 동안 사망 무효 (불사). 예) 태오 「까마귀 눈동자」 [2턴]
    /// OncePerBattle = true 이면 전투당 1회만 발동한다. 한 번 소비된 뒤에는 다른 부활 수단으로
    /// 다시 살아난 후 또 사망하더라도 이 효과는 재발동하지 않는다 (전투 단위로 1회 소진).
    /// 런타임 동작(사망 감지·부활·카운트/턴 소비·적사망 시 증가)은 추후 구현.
    /// </summary>
    public class Revival
    {
        public int HitCount { get; set; }                  // 부활 후 사망 무효 피격 횟수 (불굴 = 8, 0이면 턴제)
        public int ImmortalTurns { get; set; }             // 부활 후 사망 무효 지속 턴 (불사 = 2, 0이면 횟수형)
        public double ReviveHp { get; set; } = 1;          // 부활 시 생명력 (고정값, ReviveHpPercent=0일 때 사용)
        public double ReviveHpPercent { get; set; }        // 부활 시 생명력 최대HP% (예: 55 = 55%, 0이면 ReviveHp 고정값 사용)
        public bool OncePerBattle { get; set; } = true;    // 전투당 1회만 발동
        public int HitCountGainOnEnemyDeath { get; set; }  // 적군 사망 시 잔여 피격 횟수 증가량 (0이면 없음)
        public int MaxHitCount { get; set; }               // 잔여 피격 횟수 상한 (0이면 HitCount와 동일)
    }

    /// <summary>
    /// 트리거 스킬 발동 — 특정 트리거(예: 적군 N명 사망) 시 별개의 스킬을 시전한다.
    ///   예) 태오 「까마귀 눈동자」 - 적군 4명 사망 시 2스킬과 같은 효과의 스킬 발동(라운드당 1회).
    /// 본래 스킬과 별개 취급: 쿨타임을 공유하지 않으며, 본래 스킬의 강화로 오른 공격력 보너스를
    /// 물려받지 않는다. 단, 패시브를 강화하면 본래 스킬 강화와 같은 수치(배율 등)를 갖는다 →
    /// 이를 위해 base/enhanced PassiveLevelData에 각각 해당 티어 수치로 선언한다.
    /// 런타임 동작(트리거 카운트·스킬 시전·쿨타임 분리)은 추후 구현.
    /// </summary>
    public class TriggeredSkillCast
    {
        public TriggerCondition TriggerOn { get; set; } = TriggerCondition.EnemyDeath; // 트리거 조건
        public int TriggerCount { get; set; }              // 발동에 필요한 누적 횟수 (적군 4명 사망 = 4)
        public bool OncePerRound { get; set; } = true;     // 라운드당 1회만 발동
        public double Ratio { get; set; }                  // 시전 스킬 배율 (공격력 비례%)
        public int AtkCount { get; set; } = 1;             // 타수
        public int TargetCount { get; set; } = 1;          // 대상 수
        public List<SkillEffect> Effects { get; set; }     // 시전 스킬의 부가 효과 (턴제 버프 감소 등)
    }

    /// <summary>
    /// 쿨타임 초기화/감소 — 트리거 발동 시 시전자(또는 대상)의 스킬 쿨타임을 조정한다.
    ///   예) 태오 「까마귀 눈동자」 2초월 - 불사 발동 시 스킬 쿨타임 초기화 (AllSkills=true, ReduceSeconds=0)
    /// ReduceSeconds=0이면 전체 초기화(잔여 쿨 0), >0이면 해당 초만큼만 감소.
    /// 대상 스킬은 SkillType으로 한정 가능(null이면 평타 제외 전 스킬). 런타임 동작은 추후 구현.
    /// </summary>
    public class CooldownReset
    {
        public bool AllSkills { get; set; } = true;        // 평타 제외 모든 스킬 대상
        public SkillType? OnlySkillType { get; set; }      // 특정 스킬만 대상 (null이면 AllSkills 기준)
        public double ReduceSeconds { get; set; }          // 감소 초 (0이면 전체 초기화)
    }

    /// <summary>
    /// 강자주시 — 시전자가 특정 적군을 주시(고정 타게팅)하여 우선 공격하고, 지정 스킬 사용 시
    /// 그 대상에게 추가 피해를 준다(추가 피해 자체는 스킬의 ConditionalExtraDmg로 표현).
    ///   예) 카일 「소검쌍무」 - 공격력이 가장 높은 적군을 강자주시. 라운드당 한 대상 고정,
    ///       강자주시 대상 사망 시 다른 대상으로 전이하지 않음. 1·2스킬은 그 대상에 추가 피해.
    /// 런타임 동작(대상 선정·우선 공격·라운드 고정·비전이)은 추후 구현.
    /// </summary>
    public class FocusTarget
    {
        public TargetSelector Selector { get; set; } = TargetSelector.HighestAtkEnemy; // 대상 선정 기준
        public bool PriorityAttack { get; set; } = true;    // 시전자가 그 대상을 우선 공격
        public bool LockPerRound { get; set; } = true;      // 라운드 내 한 대상에만 고정
        public bool NoTransferOnDeath { get; set; } = true; // 대상 사망 시 다른 대상으로 전이 안 함
    }

    /// <summary>대상 선정 기준 (강자주시 / 스킬 타게팅 / 버프 대상 공용). 런타임 선정은 추후.</summary>
    public enum TargetSelector
    {
        // === 적 선정 ===
        HighestAtkEnemy,    // 공격력이 가장 높은 적군
        HighestDefEnemy,    // 방어력이 가장 높은 적군
        LowestHpEnemy,      // 현재 생명력이 가장 낮은 적군
        MostBuffsEnemy,     // 버프가 가장 많은 적군 (버프 해제 대상)
        FrontRowEnemy,      // 전열 우선 적군
        BackRowEnemy,       // 후열 우선 적군

        // === 아군 선정 ===
        HighestAtkAlly,     // 공격력이 가장 높은 아군
        LowestHpAlly,       // 현재 생명력이 가장 낮은 아군
        FrontRowAlly,       // 전열 우선 아군
        BackRowAlly,        // 후열 우선 아군
    }

    /// <summary>
    /// 권능 — 현재 생명력 이상의 피해를 입었을 때 생명력 ReviveHp로 1회 생존(전투당 1회).
    /// 불굴/불사(Revival, 사망 후 부활)와 달리 사망을 막아(치명타 방지) 즉시 생존시킨다.
    /// 버프 해제로 제거되지 않는다(해제 불가). [[reference-buff-dispel-rules]]
    ///   예) 콜트 2초월 - 권능(전투당 1회) + 발동 시 시전자 물공 155% 비례 보호막[3턴].
    /// 발동 연계 보호막은 ShieldAtkRatio/ShieldDuration으로 함께 표현. 런타임 동작은 추후 구현.
    /// </summary>
    public class Authority
    {
        public double ReviveHp { get; set; } = 1;        // 생존 시 남는 생명력 (고정값)
        public bool OncePerBattle { get; set; } = true;  // 전투당 1회
        public double ShieldAtkRatio { get; set; }       // 발동 시 시전자 공격력 비례 보호막% (0이면 없음)
        public int ShieldDuration { get; set; }          // 보호막 지속 턴
    }
}

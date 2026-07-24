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
        Ultimate,   // 궁극기
        Awaken      // 각성 전용 스킬 (각성 상태에서만 사용 가능)
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
                SkillType.Skill1 => 3.0,                 // 1스킬 3초 (실측 2026-06-08)
                SkillType.Skill2 => 4.0,                 // 2스킬(컷신) 4초
                SkillType.Skill3 or SkillType.Skill4 => 4.0,
                SkillType.Ultimate => 4.0,
                _ => 2.0
            };
        }

        // 레벨별 데이터 (0=기본, 1=강화, 2=각성)
        //   각성은 강화 위에 얹히는 증분이 아니라 수치를 통째로 덮어쓰는 상위 사양이라 독립 티어로 둔다.
        //   (예: 헤브니아 검의심판 120/강화145 → 각성 160, 위키에서 강화 표기 자체가 사라짐)
        public Dictionary<int, SkillLevelData> LevelData { get; set; } = new();

        /// <summary>이 스킬이 각성 전용인지 (각성 상태에서만 사용 가능).</summary>
        public bool IsAwakenOnly => SkillType == SkillType.Awaken;

        // 초월별 추가 효과
        public Dictionary<int, SkillTranscend> TranscendBonuses { get; set; } = new();

        // === 헬퍼 메서드 ===

        /// <summary>
        /// 티어별 스킬 수치. 우선순위: 각성(2) > 강화(1) > 기본(0).
        /// 각성 데이터가 없는 스킬은 강화/기본으로 폴백하므로, 각성 미입력 영웅은 종전 동작 그대로다.
        /// </summary>
        public SkillLevelData GetLevelData(bool isEnhanced, bool isAwakened = false)
        {
            if (isAwakened && LevelData.TryGetValue(2, out var awaken)) return awaken;
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new SkillLevelData();
        }

        public SkillTranscend GetTranscendBonus(int level)
{
    var result = new SkillTranscend();
    if (TranscendBonuses == null) return result;

    foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
    {
        // 초월 Bonus는 필드별 override (겹치는 필드는 초월값으로 덮어씀, 안 겹치면 유지). 높은 초월이 이김.
        result.Bonus.Override(kvp.Value.Bonus);
        result.Debuff.Add(kvp.Value.Debuff);
        
        if (kvp.Value.TargetCountOverride.HasValue)
            result.TargetCountOverride = kvp.Value.TargetCountOverride;

        if (kvp.Value.AtkCountOverride.HasValue)
            result.AtkCountOverride = kvp.Value.AtkCountOverride;

        if (kvp.Value.Cooldown > 0)
            result.Cooldown = kvp.Value.Cooldown;

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

        // 초월 Effects(SkillEffect 리스트) 누적 — 예: 미호 초월2 아군 디버프 해제. (기존 미병합 버그)
        if (kvp.Value.Effects != null && kvp.Value.Effects.Count > 0)
        {
            result.Effects ??= new List<SkillEffect>();
            result.Effects.AddRange(kvp.Value.Effects);
        }
    }
    return result;
}

        /// <summary>
        /// base 레벨 Effects + 초월 Effects를 "필드 단위 오버라이드"로 병합한 유효 Effects 반환.
        /// 초월 컨벤션 = 최종값 선언(오버라이드). 같은 종류(Target·Type, 상태이상은 StatusType)의 효과가
        /// base에 있으면 초월의 Buff/Debuff 0 아닌 필드로 덮어쓰고(예: 리나 울림 방깎 34→41), 없으면 추가한다.
        /// 비중첩 필드는 Override가 곧 추가와 동일하므로 안전(BuffSet/DebuffSet.Override = 0 아닌 필드만 대체).
        /// 양 경로(EffectConverter·SiegeBattleSimulator)가 이 메서드로 동일 병합을 공유한다.
        /// </summary>
        public List<SkillEffect> GetEffectiveEffects(bool isEnhanced, int transcendLevel, bool isAwakened = false)
        {
            var result = new List<SkillEffect>();
            var baseEffects = GetLevelData(isEnhanced, isAwakened)?.Effects;
            if (baseEffects != null)
                foreach (var e in baseEffects) result.Add(CloneSkillEffect(e));

            var txEffects = GetTranscendBonus(transcendLevel)?.Effects;
            if (txEffects == null) return result;

            foreach (var tx in txEffects)
            {
                // 같은 종류의 base 효과를 찾아 필드별 오버라이드 (없으면 추가)
                var match = result.FirstOrDefault(b =>
                    b.Target == tx.Target && b.Type == tx.Type &&
                    (tx.Type != SkillEffectType.StatusAilment || b.StatusType == tx.StatusType));
                if (match != null)
                {
                    if (tx.Buff != null) { match.Buff ??= new BuffSet(); match.Buff.Override(tx.Buff); }
                    if (tx.Debuff != null) { match.Debuff ??= new DebuffSet(); match.Debuff.Override(tx.Debuff); }
                    if (tx.Duration > 0) match.Duration = tx.Duration;
                }
                else
                {
                    result.Add(CloneSkillEffect(tx));
                }
            }
            return result;
        }

        /// <summary>SkillEffect 얕은 복제(+ Buff/Debuff 깊은 복제) — DB 정적 객체 변경 방지.</summary>
        private static SkillEffect CloneSkillEffect(SkillEffect e) => new SkillEffect
        {
            Target = e.Target, TargetCount = e.TargetCount, TargetSelector = e.TargetSelector,
            TargetClasses = e.TargetClasses, Type = e.Type, PreDamage = e.PreDamage,
            Duration = e.Duration, Chance = e.Chance, TurnReduction = e.TurnReduction,
            DispelBuffCount = e.DispelBuffCount, DispelDebuffCount = e.DispelDebuffCount,
            ApplyMode = e.ApplyMode, TriggerCondition = e.TriggerCondition, TriggerCount = e.TriggerCount,
            StacksPerTrigger = e.StacksPerTrigger, MaxStacks = e.MaxStacks, RequiresStatusType = e.RequiresStatusType,
            Buff = e.Buff?.Clone(), Debuff = e.Debuff?.Clone(),
            StatusType = e.StatusType, Stacks = e.Stacks,
            CustomAtkRatio = e.CustomAtkRatio, CustomHpRatio = e.CustomHpRatio, CustomAtkCap = e.CustomAtkCap,
            CustomArmorPen = e.CustomArmorPen, CustomFixedDamage = e.CustomFixedDamage,
            CustomTargetMaxHpRatio = e.CustomTargetMaxHpRatio, CustomTargetCurrentHpRatio = e.CustomTargetCurrentHpRatio,
            CustomHpConversionRatio = e.CustomHpConversionRatio, CustomTriggerCount = e.CustomTriggerCount,
            MaxConsume = e.MaxConsume, PercentPerDebuff = e.PercentPerDebuff, MaxDebuffStacks = e.MaxDebuffStacks,
            DamageNullification = e.DamageNullification, StatusImmunity = e.StatusImmunity,
        };

        /// <summary>
        /// 티어별 대상 수. 우선순위: 초월 오버라이드 > 레벨별 > Skill.TargetCount
        /// </summary>
        public int GetTargetCount(bool isEnhanced, int transcendLevel, bool isAwakened = false)
        {
            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.TargetCountOverride.HasValue) return transcend.TargetCountOverride.Value;

            var levelData = GetLevelData(isEnhanced, isAwakened);
            if (levelData.TargetCount > 0) return levelData.TargetCount;

            return TargetCount;
        }

        /// <summary>
        /// 티어별 공격 횟수. 우선순위: 초월 오버라이드 > 레벨별 > Skill.Atk_Count
        /// </summary>
        public int GetAtkCount(bool isEnhanced, int transcendLevel, bool isAwakened = false)
        {
            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.AtkCountOverride.HasValue) return transcend.AtkCountOverride.Value;

            var levelData = GetLevelData(isEnhanced, isAwakened);
            if (levelData.AtkCount > 0) return levelData.AtkCount;

            return Atk_Count;
        }

        /// <summary>
        /// 티어별 쿨타임 반환. 우선순위: 초월 오버라이드 > 레벨별 쿨타임 > Skill.CooldownSeconds 폴백
        /// </summary>
        public double GetCooldown(bool isEnhanced, int transcendLevel, bool isAwakened = false)
        {
            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Cooldown > 0) return transcend.Cooldown;

            var levelData = GetLevelData(isEnhanced, isAwakened);
            if (levelData.Cooldown > 0) return levelData.Cooldown;

            // 강화 레벨에 쿨 미입력(0)이면 기본 레벨 쿨로 폴백 (강화가 쿨을 0으로 만들지 않음 — 데이터 누락 방어)
            if (isEnhanced)
            {
                var baseLevel = GetLevelData(false);
                if (baseLevel.Cooldown > 0) return baseLevel.Cooldown;
            }

            return CooldownSeconds;
        }

        /// <summary>
        /// 스킬 레벨 보너스에 초월 보너스를 필드별 override한 BuffSet 반환 (스킬 계산 전용)
        /// 겹치는 필드는 초월값이 기본값을 덮어씀(예: 기본 Cri 30 → 초월 Cri 100 = 100), 안 겹치면 유지.
        /// </summary>
        public BuffSet GetTotalBonus(bool isEnhanced, int transcendLevel, bool isAwakened = false)
        {
            var result = new BuffSet();

            var levelData = GetLevelData(isEnhanced, isAwakened);
            if (levelData.Bonus != null) result.Add(levelData.Bonus);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Bonus != null) result.Override(transcend.Bonus);

            return result;
        }

        public ConsumeExtraDamage GetTotalConsumeExtra(bool isEnhanced, int transcendLevel, bool isAwakened = false)
{
    var levelData = GetLevelData(isEnhanced, isAwakened);
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

        // ===== 쿨타임 (티어별, 0이면 Skill.CooldownSeconds로 폴백) =====
        public double Cooldown { get; set; }            // 쿨다운 (초)

        // ===== 대상 수 / 공격 횟수 (티어별, 0이면 Skill 기본값으로 폴백) =====
        public int TargetCount { get; set; }            // 대상 수
        public int AtkCount { get; set; }               // 공격 횟수(타수)

        // ===== 조건부 효과 =====
        // 아래 값들은 Condition이 충족될 때만 적용된다.
        //   Condition이 null이면 조건 없이 항상 발동 (아직 조건을 입력하지 않은 스킬 = 종전 동작).
        public SkillCondition Condition { get; set; }
        public double ConditionalRatioBonus { get; set; }
        public double ConditionalExtraDmg { get; set; }
        public double ConditionalExtraDmgSelfHpRatio { get; set; }
        public string ConditionalDesc { get; set; }
        public bool ConditionalExtraDmgPerHit { get; set; }
        public double ConditionalDmgBonus { get; set; }

        // ===== 스킬 자체 보너스 (해당 스킬 계산에만 적용) =====
        public BuffSet Bonus { get; set; } = new BuffSet();
        public BuffSet PreCastBuff { get; set; }        // 스킬 발동 전 적용, 스킬 끝나면 소멸

        // 버프/디버프/상태이상은 Effects(SkillEffect) 리스트로 표현한다 — 대상·종류를 항목마다 명시.
        //   ※ 옛 전용 필드(SelfBuff/PartyBuff/DebuffEffect)는 제거됨: DB가 전부 Effects로 이관됐고,
        //     한 레벨에 두 방식을 같이 쓰면 한쪽이 조용히 무시되는 구조였다.
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

        // ===== 현재 HP 비례 피증 (잃은HP의 대칭 — HP 높을수록 증가) =====
        public double CurrentHpBonusDmgMax { get; set; }   // 대상/자신 현재 생명력 100%일 때 최대 피증%

        // ===== 대상 수 감소 시 피증 (피해 대상 1명 줄어들 때마다 +N%) =====
        public double DmgBonusPerMissingTarget { get; set; }

        // ===== 상태이상 해제 시 폭발 피해 (빙결/석화 해제 시 등) =====
        public CleanseExplosion CleanseExplosion { get; set; }

        // ===== 상태이상 (Effects에서 뽑아낸 읽기 전용 뷰) =====
        // 상태이상의 원본 저장소는 Effects 하나뿐이다. 여기서는 상태이상 항목만 골라
        // SkillStatusEffect 형태로 투영해 준다 — 저장소가 아니라서 대입할 수 없다.
        public List<SkillStatusEffect> StatusEffects
        {
            get
            {
                var converted = new List<SkillStatusEffect>();
                if (Effects == null || Effects.Count == 0) return converted;
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
        }

        // ===== 통합 효과 리스트 (효과의 단일 저장소) =====
        public List<SkillEffect> Effects { get; set; }

        // ===== 관통 =====
        // 대상의 피해 무효화[N턴](DamageNullification.Duration형)을 무시하고 피해를 입힌다.
        // 피격 횟수형 피해 무효화(DamageNullification.HitCount형)는 무시하지 못한다.
        public bool IgnoresTurnDamageImmunity { get; set; }

        // ===== 대상 선정 =====
        // 기본(앞열 우선) 대신 특정 기준으로 대상 선정 (예: 방어력 최고 적군). null이면 기본.
        public TargetSelector? TargetSelector { get; set; }

        // ===== 기타 =====
        public string Effect { get; set; }
        public int? TargetCountOverride { get; set; }
        public ConsumeExtraDamage ConsumeExtra { get; set; }
        public double FixedDamage { get; set; }

        // 공성전: 이 스킬 사용 시 적(보스) 진영 전체가 N턴간 모든 피해 면역 (화 R3 룩 등). 0이면 없음.
        public int GrantEnemyImmunityTurns { get; set; }

        // 공성전 보스 자기 보호막 (예: 루디 방어 준비). 보호막량 = 시전자 방어력 × SelfShieldDefRatio/100.
        // (10000 = 방어력 100배). 0이면 없음. 지속턴 = SelfShieldTurns.
        public double SelfShieldDefRatio { get; set; }
        public int SelfShieldTurns { get; set; }
        // 보호막 버프가 동반하는 받는 피해 감소 %(예: 루디 방어 준비 = 10%). 보호막 지속 동안 시전자가 받는
        //   모든 피해를 이 비율만큼 감소(대체HP 흡수와 별개). 버프해제(리프어택)/만료로 보호막이 사라지면 함께 소멸.
        public int SelfShieldDmgReduction { get; set; }

        // 이 스킬 시전 시 시전자의 지정 스킬 쿨타임을 0으로 초기화 (예: 파스칼 어둠의 문 → 파괴의 거인).
        //   null이면 없음. 시전 직후(자기 쿨 set 後) 적용 → 다음 스킬턴에 그 스킬이 곧바로 준비됨.
        public SkillType? ResetsCooldownOf { get; set; }

        // 공성전: 적 시전 시 "공격력 최고 아군(=같은 적 진영 최고공격 적, 보통 보스)"에게 부여하는 버프.
        //   예) 토요일 챈슬러 분쇄 → 스파이크 혹한의 기운/숨결(치확100·치피+500)[5턴]. 비스킷 리프어택 등 버프해제로 제거.
        //   미해제 시 그 적이 치명타 학살기가 됨(아군 전멸 유도) → 필수 버프해제 기믹. null이면 없음.
        public BuffSet GrantHighestAtkAllyBuff { get; set; }
        public int GrantHighestAtkAllyBuffTurns { get; set; }

        // 공성전: 적 스킬의 주 타격과 별개로 추가 아군에게 들어가는 추가타(예: 스파이크 혹한의 일격 "동일 열 75% 1회").
        //   빙결 무관 직접딜이라 면역으로 못 막음. 챈슬러 버프 시 추가타도 치명타로 들어감. 0이면 없음.
        public double EnemyExtraHitRatio { get; set; }
        public int EnemyExtraHitTargets { get; set; }

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
        public int? AtkCountOverride { get; set; }      // 초월 시 공격 횟수 변경
        // 초월 시 배율 변경 (선언값 = 최종값). 예: 에반 연속공격 2초월 "물리 170% / 방어 200%"
        public double? RatioOverride { get; set; }      // 공격력 비례 배율
        public double? DefRatioOverride { get; set; }   // 방어력 비례 배율

        // 쿨타임 (초월 시 변경, 0이면 강화 레벨 쿨타임 유지)
        public double Cooldown { get; set; }

        // 조건부 효과
        public double ConditionalDmgBonus { get; set; }
        public double ConditionalExtraDmg { get; set; }
        public double ConditionalExtraDmgSelfHpRatio { get; set; }

        // 상태이상 (Effects에서 뽑아낸 읽기 전용 뷰 — 저장소는 Effects 하나뿐)
        public List<SkillStatusEffect> StatusEffects
        {
            get
            {
                var converted = new List<SkillStatusEffect>();
                if (Effects == null || Effects.Count == 0) return converted;
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

    /// <summary>
    /// 상태이상 해제 시 폭발 피해 (예: 빙결/석화 해제 시 대상 최대 HP 비례 방어무시 피해).
    /// 런타임 동작(해제 감지·피해 적용)은 추후.
    /// </summary>
    public class CleanseExplosion
    {
        public StatusEffectType TargetStatus { get; set; }  // 해제 시 폭발하는 상태이상 (Freeze, Petrify 등)
        public double AtkRatio { get; set; }                // 공격력 비례 피해%
        public double TargetMaxHpRatio { get; set; }        // 대상 최대 HP 비례 피해%
        public double AtkCap { get; set; }                  // HP비례 피해의 공격력 상한%
        public double ArmorPen { get; set; }                // 방어 무시%
    }
}
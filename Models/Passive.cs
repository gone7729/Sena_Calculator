using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 패시브
    /// </summary>
    public class Passive
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxStacks { get; set; } = 1;

        public Dictionary<int, PassiveLevelData> LevelData { get; set; } = new();
        public Dictionary<int, PassiveTranscend> TranscendBonuses { get; set; } = new();

        public PassiveLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new PassiveLevelData();
        }

        public PassiveTranscend GetTranscendBonus(int level)
        {
            var result = new PassiveTranscend();
            if (TranscendBonuses == null) return result;

            foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
            {
                // 상시 자/파티 버프는 필드별 override (초월 선언값 = 최종값). 높은 초월 단계가 이김.
                result.SelfBuff.Override(kvp.Value.SelfBuff);
                result.PartyBuff.Override(kvp.Value.PartyBuff);
                result.Debuff.Add(kvp.Value.Debuff);
                result.ConditionalSelfBuff.Add(kvp.Value.ConditionalSelfBuff);
                result.ConditionalPartyBuff.Add(kvp.Value.ConditionalPartyBuff);
                result.ConditionalDebuff.Add(kvp.Value.ConditionalDebuff);
                result.Effect = kvp.Value.Effect;

                if (kvp.Value.MaxStacksOverride.HasValue)
                    result.MaxStacksOverride = kvp.Value.MaxStacksOverride;

                if (kvp.Value.Effects != null && kvp.Value.Effects.Count > 0)
                {
                    result.Effects ??= new List<Effects.PersistentEffect>();
                    result.Effects.AddRange(kvp.Value.Effects);
                    // 새 Effects 방식의 "상시(Immediate)" 비조건부 Self/Party 버프만 초월 상시 버프로 override 반영
                    // (기존엔 transcend.Effects 버프가 GetTotalSelfBuff/GetPartyBuff에서 누락되던 갭 수정)
                    // ApplyMode.Triggered(아군 사망 시 등)는 상시 버프가 아니므로 제외
                    foreach (var e in kvp.Value.Effects)
                    {
                        if (e.Type == Effects.PersistentEffectType.Buff && !e.IsConditional
                            && e.ApplyMode == Effects.ApplyMode.Immediate && e.Buff != null)
                        {
                            if (e.Target == Effects.EffectTarget.Self) result.SelfBuff.Override(e.Buff);
                            else if (e.Target == Effects.EffectTarget.Party) result.PartyBuff.Override(e.Buff);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 티어별 최대 스택. 우선순위: 초월 오버라이드 > 레벨별 > Passive.MaxStacks
        /// </summary>
        public int GetMaxStacks(bool isEnhanced, int transcendLevel)
        {
            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.MaxStacksOverride.HasValue) return transcend.MaxStacksOverride.Value;

            var levelData = GetLevelData(isEnhanced);
            if (levelData.MaxStacks > 0) return levelData.MaxStacks;

            return MaxStacks;
        }

        /// <summary>
        /// 본인 전용 상시 버프 (새 Effects + 레거시 필드 모두 확인)
        /// </summary>
        public PermanentBuff GetTotalSelfBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new PermanentBuff();
            var levelData = GetLevelData(isEnhanced);

            // 새 Effects에서 Self + Party 상시 버프 추출
            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Buff && !e.IsConditional
                        && (e.Target == Effects.EffectTarget.Self || e.Target == Effects.EffectTarget.Party)
                        && e.Buff != null)
                        result.Add(e.Buff);
                }
            }
            else
            {
                if (levelData.SelfBuff != null) result.Add(levelData.SelfBuff);
                if (levelData.PartyBuff != null) result.Add(levelData.PartyBuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            // 초월 버프는 override (선언값 = 최종값, 예: 기본 Cri 20 → 초월 Cri 100)
            if (transcend.SelfBuff != null) result.Override(transcend.SelfBuff);
            if (transcend.PartyBuff != null) result.Override(transcend.PartyBuff);

            return result;
        }

        /// <summary>
        /// 본인 전용 턴제 버프 (조건부)
        /// </summary>
        public TimedBuff GetConditionalSelfBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new TimedBuff();
            var levelData = GetLevelData(isEnhanced);

            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Buff && e.IsConditional
                        && (e.Target == Effects.EffectTarget.Self || e.Target == Effects.EffectTarget.Party)
                        && e.Buff != null)
                        result.Add(e.Buff);
                }
            }
            else
            {
                if (levelData.ConditionalSelfBuff != null) result.Add(levelData.ConditionalSelfBuff);
                if (levelData.ConditionalPartyBuff != null) result.Add(levelData.ConditionalPartyBuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.ConditionalSelfBuff != null) result.Add(transcend.ConditionalSelfBuff);
            if (transcend.ConditionalPartyBuff != null) result.Add(transcend.ConditionalPartyBuff);

            return result;
        }

        /// <summary>
        /// 아군용 상시 버프 (PartyBuff만)
        /// </summary>
        public PermanentBuff GetPartyBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new PermanentBuff();
            var levelData = GetLevelData(isEnhanced);

            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Buff && !e.IsConditional
                        && e.Target == Effects.EffectTarget.Party && e.Buff != null)
                        result.Add(e.Buff);
                }
            }
            else
            {
                if (levelData.PartyBuff != null) result.Add(levelData.PartyBuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            // 초월 버프는 override (선언값 = 최종값)
            if (transcend.PartyBuff != null) result.Override(transcend.PartyBuff);

            return result;
        }

        /// <summary>
        /// 아군용 턴제 버프 (조건부 PartyBuff)
        /// </summary>
        public TimedBuff GetConditionalPartyBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new TimedBuff();
            var levelData = GetLevelData(isEnhanced);

            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Buff && e.IsConditional
                        && e.Target == Effects.EffectTarget.Party && e.Buff != null)
                        result.Add(e.Buff);
                }
            }
            else
            {
                if (levelData.ConditionalPartyBuff != null) result.Add(levelData.ConditionalPartyBuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.ConditionalPartyBuff != null) result.Add(transcend.ConditionalPartyBuff);

            return result;
        }

        /// <summary>
        /// 상시 디버프
        /// </summary>
        public PermanentDebuff GetDebuff(bool isEnhanced, int transcendLevel)
        {
            var result = new PermanentDebuff();
            var levelData = GetLevelData(isEnhanced);

            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Debuff && !e.IsConditional
                        && e.Debuff != null)
                        result.Add(e.Debuff);
                }
            }
            else
            {
                if (levelData.Debuff != null) result.Add(levelData.Debuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Debuff != null) result.Add(transcend.Debuff);

            return result;
        }

        /// <summary>
        /// 턴제 디버프 (조건부)
        /// </summary>
        public TimedDebuff GetConditionalDebuff(bool isEnhanced, int transcendLevel)
        {
            var result = new TimedDebuff();
            var levelData = GetLevelData(isEnhanced);

            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                foreach (var e in levelData.Effects)
                {
                    if (e.Type == Effects.PersistentEffectType.Debuff && e.IsConditional
                        && e.Debuff != null)
                        result.Add(e.Debuff);
                }
            }
            else
            {
                if (levelData.ConditionalDebuff != null) result.Add(levelData.ConditionalDebuff);
            }

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.ConditionalDebuff != null) result.Add(transcend.ConditionalDebuff);

            return result;
        }
    }

    /// <summary>
    /// 패시브 레벨별 데이터
    /// </summary>
    public class PassiveLevelData
    {
        // 최대 스택 (0이면 Passive.MaxStacks 폴백)
        public int MaxStacks { get; set; }

        // ===== 상시 버프/디버프 =====
        public PermanentBuff SelfBuff { get; set; } = new PermanentBuff();       // 본인 전용 상시
        public PermanentBuff PartyBuff { get; set; } = new PermanentBuff();      // 아군 전체 상시
        public PermanentDebuff Debuff { get; set; } = new PermanentDebuff();     // 상시 디버프

        // ===== 턴제 버프/디버프 (조건부) =====
        public TimedBuff ConditionalSelfBuff { get; set; } = new TimedBuff();    // 본인 전용 조건부
        public TimedBuff ConditionalPartyBuff { get; set; } = new TimedBuff();   // 아군 전체 조건부
        public TimedDebuff ConditionalDebuff { get; set; } = new TimedDebuff();  // 조건부 디버프

        // 상태이상 부여 (레거시)
        public List<SkillStatusEffect> StatusEffects { get; set; } = new List<SkillStatusEffect>();
        public string Effect { get; set; }

        // ===== 특수 메카닉 (레거시 set + Effects 리스트 fallback) =====
        // set은 레거시 방식, get은 레거시 우선 후 Effects 리스트에서 동일 타입 검색
        private List<StatScaling> _statScalings;
        public List<StatScaling> StatScalings
        {
            get
            {
                if (_statScalings != null && _statScalings.Count > 0) return _statScalings;
                if (Effects == null) return _statScalings;
                var list = new List<StatScaling>();
                foreach (var e in Effects)
                    if (e.Type == PersistentEffectType.StatScaling && e.StatScaling != null)
                        list.Add(e.StatScaling);
                return list.Count > 0 ? list : _statScalings;
            }
            set => _statScalings = value;
        }

        private CoopAttack _coopAttack;
        public CoopAttack CoopAttack
        {
            get
            {
                if (_coopAttack != null) return _coopAttack;
                if (Effects == null) return null;
                foreach (var e in Effects)
                    if (e.Type == PersistentEffectType.CoopAttack) return e.CoopAttack;
                return null;
            }
            set => _coopAttack = value;
        }

        private MarkAttack _markAttack;
        public MarkAttack MarkAttack
        {
            get
            {
                if (_markAttack != null) return _markAttack;
                if (Effects == null) return null;
                foreach (var e in Effects)
                    if (e.Type == PersistentEffectType.MarkAttack) return e.MarkAttack;
                return null;
            }
            set => _markAttack = value;
        }

        private PainEndurance _painEndurance;
        public PainEndurance PainEndurance
        {
            get
            {
                if (_painEndurance != null) return _painEndurance;
                if (Effects == null) return null;
                foreach (var e in Effects)
                    if (e.Type == PersistentEffectType.PainEndurance) return e.PainEndurance;
                return null;
            }
            set => _painEndurance = value;
        }

        private BaseStatSet _flatBonus;
        public BaseStatSet FlatBonus
        {
            get
            {
                if (_flatBonus != null) return _flatBonus;
                if (Effects == null) return null;
                foreach (var e in Effects)
                    if (e.Type == PersistentEffectType.FlatBonus) return e.FlatBonus;
                return null;
            }
            set => _flatBonus = value;
        }

        // ===== 통합 효과 리스트 (새 방식) =====
        public List<PersistentEffect> Effects { get; set; }
    }

    /// <summary>
    /// 협공 데이터
    /// </summary>
    public class CoopAttack
    {
        public double TriggerChance { get; set; }       // 발동 확률%
        public int TargetCount { get; set; }            // 대상 수
        public int AtkCount { get; set; } = 1;          // 타수
        public double Ratio { get; set; }               // 공격력 배율%
        public double TargetMaxHpRatio { get; set; }    // 대상 최대 HP%
        public double AtkCap { get; set; }              // 공격력 제한%
    }

    /// <summary>
    /// 표식 공격 데이터
    /// </summary>
    public class MarkAttack
    {
        public int MaxStacks { get; set; }               // 최대 중첩 수
        public int AtkCount { get; set; } = 1;           // 타수 (최대 중첩 발동 시)
        public double Ratio { get; set; }                // 공격력 배율% (최대 중첩 발동 시)
        public double TargetMaxHpRatio { get; set; }     // 최대 중첩 시 대상 최대 HP%
        public double AtkCap { get; set; }               // 공격력 제한% (HP비례 피해 상한, 0이면 무제한)

        // === 스택 누적 트리거 (각 0이면 해당 채널 비활성) ===
        public int StackOnNormalEvery { get; set; }      // 기본공격 N회마다 1중첩 부여 (예: 여포 = 2)
        public int StackOnSkillEvery { get; set; }       // 스킬 N회마다 1중첩 부여 (예: 여포 = 1)

        // === 최대 중첩 발동 시 본인 자버프 ===
        public BuffSet OnMaxStackSelfBuff { get; set; }  // 부여할 자버프 스탯
        public int OnMaxStackSelfBuffDuration { get; set; } // 자버프 지속 턴 (0이면 자버프 없음)
    }

    /// <summary>
    /// 고통 감내 데이터
    /// </summary>
    public class PainEndurance
    {
        public double Threshold { get; set; }      // 발동 기준 (최대 HP의 %)
        public double ReductionRate { get; set; }  // 분산 비율%
        public int Duration { get; set; }          // 분산 턴 수
    }

    /// <summary>
    /// 패시브 초월 보너스
    /// </summary>
    public class PassiveTranscend
    {
        // 최대 스택 오버라이드 (초월로 스택 수 변경 시)
        public int? MaxStacksOverride { get; set; }

        // ===== 상시 버프/디버프 =====
        public PermanentBuff SelfBuff { get; set; } = new PermanentBuff();       // 본인 전용 상시
        public PermanentBuff PartyBuff { get; set; } = new PermanentBuff();      // 아군 전체 상시
        public PermanentDebuff Debuff { get; set; } = new PermanentDebuff();     // 상시 디버프

        // ===== 턴제 버프/디버프 (조건부) =====
        public TimedBuff ConditionalSelfBuff { get; set; } = new TimedBuff();    // 본인 전용 조건부
        public TimedBuff ConditionalPartyBuff { get; set; } = new TimedBuff();   // 아군 전체 조건부
        public TimedDebuff ConditionalDebuff { get; set; } = new TimedDebuff();  // 조건부 디버프

        public List<StatScaling> StatScalings { get; set; } = new List<StatScaling>();

        // 상태이상 부여
        public List<SkillStatusEffect> StatusEffects { get; set; } = new List<SkillStatusEffect>();
        public string Effect { get; set; }

        // 협공 강화
        public double CoopChanceBonus { get; set; }     // 협공 확률 증가%
        public double CoopRatioBonus { get; set; }      // 협공 배율 증가%
        public double CoopHpRatioBonus { get; set; }    // 협공 HP비례 증가%

        // 통합 효과 리스트 (새 방식 - 기존 Effects를 오버라이드)
        public List<Effects.PersistentEffect> Effects { get; set; }
    }

    /// <summary>
    /// 스탯 스케일링 (속공→공격력 등)
    /// </summary>
    public class StatScaling
    {
        public StatType SourceStat { get; set; }        // 기준 스탯
        public StatType TargetStat { get; set; }        // 증가할 스탯
        public double PerUnit { get; set; }             // 단위당 증가량
        public double SourceUnit { get; set; }          // 기준 스탯 단위
        public double MaxValue { get; set; }            // 최대 증가량
    }

    /// <summary>
    /// 스탯 타입
    /// </summary>
    public enum StatType
    {
        None,
        Atk,        // 공격력
        Def,        // 방어력
        Hp,         // 생명력
        Spd,        // 속공
        Cri,        // 치명타 확률
        Cri_Dmg,    // 치명타 피해
        Eff_Hit,    // 효과 적중
        Eff_Res,    // 효과 저항
        Blk,        // 막기
        Dmg_Rdc     // 받는 피해 감소
    }
}
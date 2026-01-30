using System.Collections.Generic;
using System.Linq;

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
                result.SelfBuff.Add(kvp.Value.SelfBuff);
                result.PartyBuff.Add(kvp.Value.PartyBuff);
                result.Debuff.Add(kvp.Value.Debuff);
                result.ConditionalSelfBuff.Add(kvp.Value.ConditionalSelfBuff);
                result.ConditionalPartyBuff.Add(kvp.Value.ConditionalPartyBuff);
                result.ConditionalDebuff.Add(kvp.Value.ConditionalDebuff);
                result.Effect = kvp.Value.Effect;
            }
            return result;
        }

        /// <summary>
        /// 본인 전용 상시 버프
        /// </summary>
        public PermanentBuff GetTotalSelfBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new PermanentBuff();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.SelfBuff != null) result.Add(levelData.SelfBuff);
            if (levelData.PartyBuff != null) result.Add(levelData.PartyBuff);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.SelfBuff != null) result.Add(transcend.SelfBuff);
            if (transcend.PartyBuff != null) result.Add(transcend.PartyBuff);

            return result;
        }

        /// <summary>
        /// 본인 전용 턴제 버프 (조건부)
        /// </summary>
        public TimedBuff GetConditionalSelfBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new TimedBuff();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.ConditionalSelfBuff != null) result.Add(levelData.ConditionalSelfBuff);
            if (levelData.ConditionalPartyBuff != null) result.Add(levelData.ConditionalPartyBuff);

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
            if (levelData.PartyBuff != null) result.Add(levelData.PartyBuff);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.PartyBuff != null) result.Add(transcend.PartyBuff);

            return result;
        }

        /// <summary>
        /// 아군용 턴제 버프 (조건부 PartyBuff)
        /// </summary>
        public TimedBuff GetConditionalPartyBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new TimedBuff();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.ConditionalPartyBuff != null) result.Add(levelData.ConditionalPartyBuff);

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
            if (levelData.Debuff != null) result.Add(levelData.Debuff);

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
            if (levelData.ConditionalDebuff != null) result.Add(levelData.ConditionalDebuff);

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
        // ===== 상시 버프/디버프 =====
        public PermanentBuff SelfBuff { get; set; } = new PermanentBuff();       // 본인 전용 상시
        public PermanentBuff PartyBuff { get; set; } = new PermanentBuff();      // 아군 전체 상시
        public PermanentDebuff Debuff { get; set; } = new PermanentDebuff();     // 상시 디버프

        // ===== 턴제 버프/디버프 (조건부) =====
        public TimedBuff ConditionalSelfBuff { get; set; } = new TimedBuff();    // 본인 전용 조건부
        public TimedBuff ConditionalPartyBuff { get; set; } = new TimedBuff();   // 아군 전체 조건부
        public TimedDebuff ConditionalDebuff { get; set; } = new TimedDebuff();  // 조건부 디버프

        // 상태이상 부여
        public List<SkillStatusEffect> StatusEffects { get; set; } = new List<SkillStatusEffect>();
        public string Effect { get; set; }
        public List<StatScaling> StatScalings { get; set; } = new List<StatScaling>();
        public CoopAttack CoopAttack { get; set; }
        public PainEndurance PainEndurance { get; set; }
        public BaseStatSet FlatBonus { get; set; }
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
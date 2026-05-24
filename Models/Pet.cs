using System.Collections.Generic;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 펫 스킬 (버프 + 디버프)
    /// </summary>
    public class PetSkill
    {
        public BuffSet Buff { get; set; }     // BaseStatSet → BuffSet
        public DebuffSet Debuff { get; set; }
    }

    /// <summary>
    /// 펫 모델
    /// </summary>
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Rarity { get; set; }
        public Dictionary<int, PetSkill> Skills { get; set; }

        /// <summary>
        /// 스킬강화 효과 (절대값 "변경"). 키 = 강화 레벨 (1~3), 0강(미강화)은 항목 없음.
        /// 값 = 해당 강화 레벨에서 스킬값이 변경되는 최종 절대치 (예: 보스취약 26% → 30%로 변경).
        /// GetSkillBuff/Debuff에서 0이 아닌 필드만 성급값을 Override(대체)한다. 가산 아님.
        /// (게임 표기가 "X%로 변경"이라 덮어쓰기. 강화는 보통 6성에서만 가능.)
        /// </summary>
        public Dictionary<int, PetSkill> EnhanceBonus { get; set; }

        /// <summary>
        /// 성급별 기본 스탯 가져오기
        /// </summary>
        public BaseStatSet GetBaseStats(int star)
        {
            if (StatTable.PetStatTable.GradeStats.TryGetValue(Rarity, out var rarityStats))
            {
                if (rarityStats.TryGetValue(star, out var stats))
                {
                    return stats.Clone();
                }
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 성급별 스킬 버프 가져오기 (강화 0 = 성급값 그대로).
        /// </summary>
        public BuffSet GetSkillBuff(int star, int enhance = 0)
        {
            var result = Skills != null && Skills.TryGetValue(star, out var skill)
                ? (skill.Buff?.Clone() ?? new BuffSet())
                : new BuffSet();

            // 강화 효과: 0이 아닌 필드만 절대값으로 변경(Override)
            if (enhance > 0 && EnhanceBonus != null
                && EnhanceBonus.TryGetValue(enhance, out var bonus) && bonus.Buff != null)
            {
                result.Override(bonus.Buff);
            }
            return result;
        }

        /// <summary>
        /// 성급별 스킬 디버프 가져오기 (강화 0 = 성급값 그대로).
        /// </summary>
        public DebuffSet GetSkillDebuff(int star, int enhance = 0)
        {
            var result = Skills != null && Skills.TryGetValue(star, out var skill)
                ? (skill.Debuff?.Clone() ?? new DebuffSet())
                : new DebuffSet();

            if (enhance > 0 && EnhanceBonus != null
                && EnhanceBonus.TryGetValue(enhance, out var bonus) && bonus.Debuff != null)
            {
                result.Override(bonus.Debuff);
            }
            return result;
        }

        // ✅ 새 버전
        public BaseStatSet GetTotalStats(int star)
        {
            return GetBaseStats(star);  // 기본 스탯만 반환
        }
    }
}
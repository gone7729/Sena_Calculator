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
        /// 성급별 스킬 버프 가져오기
        /// </summary>
        public BuffSet GetSkillBuff(int star)
        {
            if (Skills != null && Skills.TryGetValue(star, out var skill))
            {
                return skill.Buff?.Clone() ?? new BuffSet();
            }
            return new BuffSet();
        }

        /// <summary>
        /// 성급별 스킬 디버프 가져오기
        /// </summary>
        public DebuffSet GetSkillDebuff(int star)
        {
            if (Skills != null && Skills.TryGetValue(star, out var skill))
            {
                return skill.Debuff?.Clone() ?? new DebuffSet();
            }
            return new DebuffSet();
        }

        // ✅ 새 버전
        public BaseStatSet GetTotalStats(int star)
        {
            return GetBaseStats(star);  // 기본 스탯만 반환
        }
    }
}
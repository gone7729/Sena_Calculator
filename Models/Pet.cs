using System.Collections.Generic;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 펫 모델
    /// </summary>
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Rarity { get; set; }
        public Dictionary<int, BaseStatSet> Skills { get; set; }

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
        /// 성급별 스킬 보너스 가져오기
        /// </summary>
        public BaseStatSet GetSkillBonus(int star)
        {
            if (Skills != null && Skills.TryGetValue(star, out var bonus))
            {
                return bonus.Clone();
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 전체 스탯 (기본 + 스킬 보너스)
        /// </summary>
        public BaseStatSet GetTotalStats(int star)
        {
            var total = GetBaseStats(star);
            total.Add(GetSkillBonus(star));
            return total;
        }
    }
}
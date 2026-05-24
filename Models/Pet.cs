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
        /// 스킬강화 보너스 (성급값에 가산). 키 = 강화 레벨 (1~3), 0강(미강화)은 항목 없음.
        /// 값 = 해당 강화 레벨에서 성급 스킬값 위에 더해지는 누적 보너스(절대치).
        /// 즉 최종값 = 성급 스킬값 + EnhanceBonus[enhance]. 성급과 무관한 독립 가산.
        /// (데이터 미입력 상태 — 강화 수치 확보 시 펫별로 채운다.)
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

            // 강화 보너스 가산 (성급과 독립)
            if (enhance > 0 && EnhanceBonus != null
                && EnhanceBonus.TryGetValue(enhance, out var bonus) && bonus.Buff != null)
            {
                result.Add(bonus.Buff);
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
                result.Add(bonus.Debuff);
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
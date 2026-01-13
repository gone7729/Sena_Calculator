using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 캐릭터
    /// </summary>
    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }       // 전설, 영웅
        public string Type { get; set; }        // 공격형, 마법형, 만능형, 지원형, 방어형

        public List<Skill> Skills { get; set; } = new List<Skill>();
        public Passive Passive { get; set; }
        public List<TranscendBonus> TranscendBonuses { get; set; } = new List<TranscendBonus>();

        // 런타임 상태
        public int TranscendLevel { get; set; } = 0;
        public bool IsSkillEnhanced { get; set; } = false;

        /// <summary>
        /// 등급/타입에 따른 기본 스탯 가져오기
        /// </summary>
        public BaseStatSet GetBaseStats()
        {
            if (StatTable.AllBaseStats.TryGetValue(Grade, out var gradeStats))
            {
                if (gradeStats.TryGetValue(Type, out var typeStats))
                {
                    return typeStats.Clone();
                }
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 초월 레벨에 따른 스탯 보너스 가져오기
        /// </summary>
        public BaseStatSet GetTranscendStats(int level)
        {
            var result = new BaseStatSet();
            if (level <= 0) return result;

            // 캐릭터 고유 초월 (1~6)
            foreach (var bonus in TranscendBonuses.Where(b => b.Level <= level))
            {
                result.Add(bonus.BonusStats);
            }

            // 공통 초월 (7~12)
            foreach (var bonus in StatTable.TranscendDb.CommonBonuses.Where(b => b.Level <= level))
            {
                result.Add(bonus.BonusStats);
            }

            return result;
        }

        /// <summary>
        /// 이름으로 스킬 찾기
        /// </summary>
        public Skill GetSkillByName(string skillName)
        {
            return Skills.FirstOrDefault(s => s.Name == skillName);
        }

        /// <summary>
        /// 타입으로 스킬 찾기
        /// </summary>
        public Skill GetSkillByType(SkillType skillType)
        {
            return Skills.FirstOrDefault(s => s.SkillType == skillType);
        }
    }

    /// <summary>
    /// 캐릭터 초월 스탯 보너스 (1~12레벨)
    /// </summary>
    public class TranscendBonus
    {
        public int Level { get; set; }
        public BaseStatSet BonusStats { get; set; } = new BaseStatSet();
        public string SpecialEffect { get; set; }
    }
}

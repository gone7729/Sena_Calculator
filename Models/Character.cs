using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 초월 타입
    /// </summary>
    public enum TranscendType
    {
        AtkCri,      // 공격 + 치확
        AtkCriDmg,   // 공격 + 치피
        AtkWek,      // 공격 + 약점
        AtkEff,      // 공격 + 효적
        AtkDmgRdc,   // 공격 + 피감
        DefBlk,      // 방어 + 막기
        DefDmgRdc    // 방어 + 피감
    }

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
        
        // 초월 타입 (TranscendDb에서 보너스 가져옴)
        public TranscendType TranscendType { get; set; } = TranscendType.AtkCri;

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
        /// 초월 타입에 따른 고유 보너스 리스트 (1~6) - 등급별 분기
        /// </summary>
        private List<TranscendBonus> GetUniqueBonuses()
        {
            // 전설 등급
            if (Grade == "전설")
            {
                return TranscendType switch
                {
                    TranscendType.AtkCri => StatTable.TranscendDb.Legendary.AtkCriBonuses,
                    TranscendType.AtkCriDmg => StatTable.TranscendDb.Legendary.AtkCriDmgBonuses,
                    TranscendType.AtkWek => StatTable.TranscendDb.Legendary.AtkWekBonuses,
                    TranscendType.AtkEff => StatTable.TranscendDb.Legendary.AtkEffBonuses,
                    TranscendType.AtkDmgRdc => StatTable.TranscendDb.Legendary.AtkDmgRdcBonuses,
                    TranscendType.DefBlk => StatTable.TranscendDb.Legendary.DefBlkBonuses,
                    TranscendType.DefDmgRdc => StatTable.TranscendDb.Legendary.DefDmgRdcBonuses,
                    _ => StatTable.TranscendDb.Legendary.AtkCriBonuses
                };
            }
            // 영웅 등급
            else if (Grade == "영웅")
            {
                return TranscendType switch
                {
                    TranscendType.AtkCri => StatTable.TranscendDb.Hero.AtkCriBonuses,
                    TranscendType.AtkCriDmg => StatTable.TranscendDb.Hero.AtkCriDmgBonuses,
                    TranscendType.AtkWek => StatTable.TranscendDb.Hero.AtkWekBonuses,
                    TranscendType.AtkEff => StatTable.TranscendDb.Hero.AtkEffBonuses,
                    TranscendType.AtkDmgRdc => StatTable.TranscendDb.Hero.AtkDmgRdcBonuses,
                    TranscendType.DefBlk => StatTable.TranscendDb.Hero.DefBlkBonuses,
                    TranscendType.DefDmgRdc => StatTable.TranscendDb.Hero.DefDmgRdcBonuses,
                    _ => StatTable.TranscendDb.Hero.AtkCriBonuses
                };
            }
            // 기본값 (전설)
            return StatTable.TranscendDb.Legendary.AtkCriBonuses;
        }

        /// <summary>
        /// 공통 보너스 리스트 (7~12) - 등급별 분기
        /// </summary>
        private List<TranscendBonus> GetCommonBonuses()
        {
            if (Grade == "영웅")
                return StatTable.TranscendDb.Hero.CommonBonuses;
            return StatTable.TranscendDb.Legendary.CommonBonuses;
        }

        /// <summary>
        /// 초월 타입에 따른 전체 보너스 리스트 (1~12)
        /// </summary>
        public List<TranscendBonus> GetTranscendBonuses()
        {
            var uniqueBonuses = GetUniqueBonuses();
            var commonBonuses = GetCommonBonuses();
            
            var full = new List<TranscendBonus>(uniqueBonuses);
            full.AddRange(commonBonuses);
            return full;
        }

        /// <summary>
        /// 초월 레벨에 따른 스탯 보너스 가져오기
        /// </summary>
        public BaseStatSet GetTranscendStats(int level)
        {
            var result = new BaseStatSet();
            if (level <= 0) return result;

            var allBonuses = GetTranscendBonuses();
            foreach (var bonus in allBonuses.Where(b => b.Level <= level))
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
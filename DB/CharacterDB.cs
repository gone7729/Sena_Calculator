using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 캐릭터 데이터베이스
    /// </summary>
    public static class CharacterDb
    {
        public static readonly List<Character> Characters = new List<Character>
        {
            // ===== 타카 =====
            new Character
            {
                Id = 1,
                Name = "타카",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        Ratio = 1.0,
                        RatioEnhanced = 1.2,
                        TargetCount = 1
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "바람의 칼날",
                        SkillType = SkillType.Skill1,
                        Ratio = 1.45,
                        RatioEnhanced = 1.70,
                        BonusCriDmg = 0.37,
                        BonusCriDmgEnhanced = 0.46,
                        ConditionalDmg = 0.45,
                        ConditionalDmgEnhanced = 0.55,
                        ConditionalDesc = "체력 30% 미만",
                        TargetCount = 3
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 무도",
                        SkillType = SkillType.Skill2,
                        Ratio = 1.45,
                        RatioEnhanced = 1.70,
                        ArmorPen = 0,
                        ArmorPenEnhanced = 0.40,     // 6초월 시 방무 40%
                        ConditionalDmg = 2.60,
                        ConditionalDmgEnhanced = 2.60,
                        ConditionalDesc = "체력 30% 미만",
                        TargetCount = 3
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 발톱",
                    Description = "적에게 받는 물리 피해 증가 디버프 (최대 8중첩)",
                    MaxStacks = 8,
                    DebuffToEnemy = new BaseStatSet { /* 받는 물리 피해 3% */ },
                    EnhancedBonusStats = new BaseStatSet { /* 강화 시 4% */ }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 0.12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 0.06 }, SpecialEffect = "매의 발톱 받는 물리 피해 3% → 4%" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 0.18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 0.18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 0.12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 0.06 }, SpecialEffect = "죽음의 무도 방어력 40% 무시" },
                    new TranscendBonus { Level = 7, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } },
                    new TranscendBonus { Level = 8, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } },
                    new TranscendBonus { Level = 9, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } },
                    new TranscendBonus { Level = 10, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } },
                    new TranscendBonus { Level = 11, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } },
                    new TranscendBonus { Level = 12, BonusStats = new BaseStatSet { Atk_Rate = 0.02, Def_Rate = 0.02, Hp_Rate = 0.02 } }
                }
            },

            // ===== 아래에 캐릭터 추가 =====
            // new Character { Id = 2, Name = "파스칼", ... },
        };

        /// <summary>
        /// 이름으로 캐릭터 찾기
        /// </summary>
        public static Character GetByName(string name)
        {
            return Characters.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// ID로 캐릭터 찾기
        /// </summary>
        public static Character GetById(int id)
        {
            return Characters.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 등급별 캐릭터 목록
        /// </summary>
        public static List<Character> GetByGrade(string grade)
        {
            return Characters.Where(c => c.Grade == grade).ToList();
        }

        /// <summary>
        /// 타입별 캐릭터 목록
        /// </summary>
        public static List<Character> GetByType(string type)
        {
            return Characters.Where(c => c.Type == type).ToList();
        }
    }
}

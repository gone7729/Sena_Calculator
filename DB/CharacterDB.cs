using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 캐릭터 데이터베이스
    /// 세븐나이츠 리버스 영웅 목록
    /// 등급: 전설, 희귀
    /// 유형: 공격형, 마법형, 방어형, 지원형, 만능형
    /// </summary>
    public static class CharacterDb
    {
        public static readonly List<Character> Characters = new List<Character>
        {
            // 전설 - 공격형 1~

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
                        TargetCount = 1,
                        Atk_Count = 1,
                        ConditionalDesc = "체력 30% 미만",
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.0, ConditionalExtraDmg = 0.45 } },
                            { 1, new SkillLevelData { Ratio = 1.2, ConditionalExtraDmg = 0.55 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "바람의 칼날",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        ConditionalDesc = "치명타 피해 37% 추가 적용",
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, BonusCriDmg = 37 } },
                            { 1, new SkillLevelData { Ratio = 1.70, BonusCriDmg = 46 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 무도",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        ConditionalDesc = "체력 30% 미만",
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, ConditionalExtraDmg = 2.60 } },
                            { 1, new SkillLevelData { Ratio = 1.70, ConditionalExtraDmg = 2.60 } }
                        },
                        TranscendBonuses = new Dictionary<int, TranscendEffect>
                        {
                            { 6, new TranscendEffect { ArmorPen = 40, Effect = "방어력 40% 무시" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 발톱",
                    MaxStacks = 8,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        // 0 = 기본 (스킬강화 X): 취약 3% × 8스택 = 24%
                        { 0, new PassiveLevelData { 
                            DebuffModifier = new DebuffSet { Vulnerability = 24 }
                        }},
                        // 1 = 강화 (스킬강화 O): 취약 4% × 8스택 = 32%
                        { 1, new PassiveLevelData { 
                            DebuffModifier = new DebuffSet { Vulnerability = 32 }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, TranscendEffect>
                    {
                        // 2초월: 받피증 추가
                        { 2, new TranscendEffect { 
                            DebuffModifier = new DebuffSet { Dmg_Taken_Increase = 24 }
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "매의 발톱 물리 취약 3% → 4%" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "죽음의 무도 방어력 40% 무시" }
                }
            },

            // ===== 라이언 =====
            new Character
            {
                Id = 2,
                Name = "라이언",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.0 } },
                            { 1, new SkillLevelData { Ratio = 1.2 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "강자 사냥",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.30 } },
                            { 1, new SkillLevelData { Ratio = 1.55 } }
                        },
                        TranscendBonuses = new Dictionary<int, TranscendEffect>
                        {
                            { 2, new TranscendEffect { ArmorPen = 40, Effect = "방어력 40% 무시" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "광풍참",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        ConditionalDesc = "잃은 생명력 비례 최대 50% 피해량 증가, 약점 공격 시 추가 피해",
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 1.45, 
                                ConditionalExtraDmg = 0.50,
                                WekBonusDmg = 2.30
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 1.70, 
                                ConditionalExtraDmg = 0.50,
                                WekBonusDmg = 2.70
                            }}
                        },
                        TranscendBonuses = new Dictionary<int,  TranscendEffect>
                        {
                            { 6, new TranscendEffect { ArmorPen = 40, Effect = "방어력 40% 무시" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "쾌속의 마검사",
                    MaxStacks = 1,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            BuffModifier = new BuffSet { Dmg_Dealt_1to3 = 25 },
                            Effect = "모든 아군 3인 공격기 피해량 25% 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            BuffModifier = new BuffSet { Dmg_Dealt_1to3 = 31 },
                            Effect = "모든 아군 3인 공격기 피해량 31% 증가"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "강자 사냥 방어력 40% 무시" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "광풍참 방어력 40% 무시" }
                }
            },
             // 전설 - 지원형 101~

            // ===== 비스킷 =====
            new Character
            {
                Id = 101,
                Name = "비스킷",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 0.5, DefRatio = 0.6 } },
                            { 1, new SkillLevelData { Ratio = 0.5, DefRatio = 0.6, DebuffEffect = new DebuffSet { Dmg_Reduction = 6 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "장비 강화",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                BuffEffect = new BuffSet { Dmg_Dealt_Bos = 33, Wek = 44 },
                                EffectDuration = 5
                            }},
                            { 1, new SkillLevelData { 
                                BuffEffect = new BuffSet { Dmg_Dealt_Bos = 40, Wek = 54 },
                                EffectDuration = 5
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, TranscendEffect>
                        {
                            { 6, new TranscendEffect { Effect = "아군 2명" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "리프 어택",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1, DefRatio = 1.15, ArmorPen = 40 } },
                            { 1, new SkillLevelData { Ratio = 1.15, DefRatio = 1.35, ArmorPen = 40 } }
                        },
                    }
                },
                Passive = new Passive
                {
                    Name = "대장장이의 강화",
                    MaxStacks = 1,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            DebuffModifier = new DebuffSet { Def_Reduction = 20 },
                            Effect = "적 방어력 20% 감소"
                        }},
                        { 1, new PassiveLevelData { 
                            DebuffModifier = new DebuffSet { Def_Reduction = 24 },
                            Effect = "적 방어력 24% 감소"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, TranscendEffect>
                    {
                        { 2, new TranscendEffect { 
                            BuffModifier = new BuffSet { Dmg_Rdc_Multi = 20 },
                            Effect = "아군 5인 공격기 받는 피해 20% 감소"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "5인 감쇄 20%" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Dmg_Rdc = 10 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "아군 2명" }
                }
            },

            // ===== 리나 =====
            new Character
            {
                Id = 101,
                Name = "리나",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1 } },
                            { 1, new SkillLevelData { Ratio = 1.2 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "행진가",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { HealHpRatio = 21 } },
                            { 1, new SkillLevelData { HealHpRatio = 24 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "따뜻한 울림",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                BuffEffect = new BuffSet { Dmg_Dealt = 23 }, 
                                DebuffEffect = new DebuffSet { Def_Reduction = 34 }
                            } },
                            { 1, new SkillLevelData { 
                                BuffEffect = new BuffSet { Dmg_Dealt = 28 }, 
                                DebuffEffect = new DebuffSet { Def_Reduction = 34 }
                            } },
                        },
                        TranscendBonuses = new Dictionary<int, TranscendEffect>
                        {
                            { 2, new TranscendEffect { 
                                DebuffModifier = new DebuffSet { Def_Reduction = 41 },
                                Effect = "방어력 41% 감소"
                            }}
                        }
                        
                    }
                },
                Passive = new Passive
                {
                    Name = "불협화음",
                    MaxStacks = 1,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            BuffModifier = new BuffSet { Eff_Res = 28, Shield_HpRatio = 39 },
                            Effect = "효과 저항 28% 증가, 보호막 최대체력 39%"
                        }},
                        { 1, new PassiveLevelData { 
                            BuffModifier = new BuffSet { Eff_Res = 34, Shield_HpRatio = 45 },
                            Effect = "효과 저항 34% 증가, 보호막 최대체력 45%"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, TranscendEffect>
                    {
                        { 2, new TranscendEffect { 
                            BuffModifier = new BuffSet { Cri_Dmg = 34 },
                            Effect = "모든 아군 치명타 피해 34% 증가"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "5인 감쇄 20%" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Dmg_Rdc = 10 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "아군 2명" }
                }
            },
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

        /// <summary>
        /// 등급+타입으로 캐릭터 목록
        /// </summary>
        public static List<Character> GetByGradeAndType(string grade, string type)
        {
            return Characters.Where(c => c.Grade == grade && c.Type == type).ToList();
        }

        /// <summary>
        /// 캐릭터 이름 목록 (콤보박스용)
        /// </summary>
        public static List<string> GetAllNames()
        {
            return Characters.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// 등급별 캐릭터 이름 목록
        /// </summary>
        public static List<string> GetNamesByGrade(string grade)
        {
            return Characters.Where(c => c.Grade == grade).Select(c => c.Name).ToList();
        }
    }
}

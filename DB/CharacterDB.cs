using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    public static class CharacterDb
    {
        public static readonly List<Character> Characters = new List<Character>
        {
            #region 전설 - 공격형 1~

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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.0, ConditionalExtraDmg = 0.45, ConditionalDesc = "체력 30% 미만" } },
                            { 1, new SkillLevelData { Ratio = 1.2, ConditionalExtraDmg = 0.55, ConditionalDesc = "체력 30% 미만" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "바람의 칼날",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, Bonus = new BuffSet { Cri_Dmg = 37 } } },
                            { 1, new SkillLevelData { Ratio = 1.70, Bonus = new BuffSet { Cri_Dmg = 46 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 무도",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, ConditionalExtraDmg = 2.60, ConditionalDesc = "체력 30% 미만" } },
                            { 1, new SkillLevelData { Ratio = 1.70, ConditionalExtraDmg = 2.60, ConditionalDesc = "체력 30% 미만" } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 }, Effect = "방어력 40% 무시" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 발톱",
                    MaxStacks = 8,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { Debuff = new DebuffSet { Vulnerability = 24 } } },
                        { 1, new PassiveLevelData { Debuff = new DebuffSet { Vulnerability = 32 } } }
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Debuff = new DebuffSet { Dmg_Taken_Increase = 24 } } }
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
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 }, Effect = "방어력 40% 무시" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "광풍참",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 1.45, 
                                ConditionalExtraDmg = 0.50,
                                ConditionalDesc = "잃은 생명력 비례 최대 50%",
                                Bonus = new BuffSet { WekBonusDmg = 2.30 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 1.70, 
                                ConditionalExtraDmg = 0.50,
                                ConditionalDesc = "잃은 생명력 비례 최대 50%",
                                Bonus = new BuffSet { WekBonusDmg = 2.70 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 }, Effect = "방어력 40% 무시" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "쾌속의 마검사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt_1to3 = 25 },
                            Effect = "모든 아군 3인 공격기 피해량 25% 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt_1to3 = 31 },
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

            // ===== 델론즈 =====
            new Character
            {
                Id = 3,
                Name = "델론즈",
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
                        Name = "사신강림",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.15 } },
                            { 1, new SkillLevelData { Ratio = 1.35 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 }, Effect = "방어력 40% 무시" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 일격",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.19 }},
                            { 1, new SkillLevelData { Ratio = 1.58 }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "처치 시 100% 위력으로 연속 발동" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "죽음의 경계",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt = 17 }
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt = 20 }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 6, new PassiveTranscend { Effect = "아군 죽음 내가 쌔짐 ㄹㅇ ㅇㅇ" } }
                        }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "처치 시 100% 위력으로 연속 발동" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "아군 죽음 내가 쌔짐 ㄹㅇ ㅇㅇ" }
                }
            },

            // 세인
            new Character
            {
                Id = 4,
                Name = "세인",
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
                        Name = "귀신 베기",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 5.7 } },
                            { 1, new SkillLevelData { Ratio = 6.9 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { ConditionalDmgBonus = 2, Effect = "디버프 1개당 피해량 증가(최대 4개)" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "악마의 힘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Rdc = 24, Cri = 35 }
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Rdc = 29, Cri = 41 }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Buff = new BuffSet{ Dmg_Dealt = 80 }, Effect = "디버프당 피해량 증가 20%" } }
                        }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "처치 시 100% 위력으로 연속 발동" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "아군 죽음 내가 쌔짐 ㄹㅇ ㅇㅇ" }
                }
            },


            // 비담
            new Character
            {
                Id = 5,
                Name = "비담",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        TargetCount = 2,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 0.55 } },
                            { 1, new SkillLevelData { Ratio = 0.65 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "거궁신포",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.15 } },
                            { 1, new SkillLevelData { Ratio = 1.35 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "쿨 55초 변경, 상대 출혈 시 확정 출혈" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "태전포화",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.75, BonusDmgRatio = 1.2, BonusDmgMaxStacks = 3 }},
                            { 1, new SkillLevelData { Ratio = 2.05, BonusDmgRatio = 1.2, BonusDmgMaxStacks = 3 }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { BonusDmgRatio = 0.3, Effect = "출혈폭발 댐증" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "화랑의 후예",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt = 29 }
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Dmg_Dealt = 35 }
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "처치 시 100% 위력으로 연속 발동" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "아군 죽음 내가 쌔짐 ㄹㅇ ㅇㅇ" }
                }
            },

            // 카구라


            // 태오


            // 클라한


            // 카일


            // 콜트


            // 아멜리아


            // 발리스타



            #endregion

            #region 전설 - 마법형 101~

            // ===== 파스칼 =====
            new Character
            {
                Id = 101,
                Name = "파스칼",
                Grade = "전설",
                Type = "마법형",
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
                        Name = "어둠의 문",
                        SkillType = SkillType.Skill1,
                        TargetCount = 0,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Effect = "파괴의 거인 쿨초기화, 피해무효 1회" } },
                            { 1, new SkillLevelData { Effect = "파괴의 거인 쿨초기화, 피해무효 2회" } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "쿨타임 40초 감소" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "파괴의 거인",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 3.90, Bonus = new BuffSet { Cri = 50 } } },
                            { 1, new SkillLevelData { Ratio = 4.70, Bonus = new BuffSet { Cri = 50 } } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 65, Cri = 50 },
                                Effect = "치명타 확정, 방어력 65% 무시" 
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "천재의 권능",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Atk_Rate = 27, Cri_Dmg = 40 },
                            Effect = "자신 공격력 27% 증가, 치피 40% 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Atk_Rate = 33, Cri_Dmg = 40 },
                            Effect = "자신 공격력 33% 증가, 치피 40% 증가"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "어둠의 문 쿨타임 40초 감소" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri_Dmg = 24 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "파괴의 거인 치명타 확정, 방무 65%" }
                }
            },

            // ===== 루리 =====
            new Character
            {
                Id = 102,
                Name = "루리",
                Grade = "전설",
                Type = "마법형",
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
                        Name = "근거리 연사",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 0.75, Bonus = new BuffSet { WekBonusDmg = 0.35 } } },
                            { 1, new SkillLevelData { Ratio = 0.9, Bonus = new BuffSet { WekBonusDmg = 0.42 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "폭격 지원",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, Bonus = new BuffSet { WekBonusDmg = 1.3 } } },
                            { 1, new SkillLevelData { Ratio = 1.7, Bonus = new BuffSet { WekBonusDmg = 1.55 } } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = "방어력 40% 무시" 
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "뒷거래",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { Buff = new BuffSet { Atk_Rate = 24 } } },
                        { 1, new PassiveLevelData { Buff = new BuffSet { Atk_Rate = 29 } } }
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Buff = new BuffSet { Wek = 39 },
                            Effect = "약확 39% 증가" 
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "약확 39% 증가" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Wek = 20 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "방어력 40% 무시" }
                }
            },

            // ===== 미호 =====
            new Character
            {
                Id = 103,
                Name = "미호",
                Grade = "전설",
                Type = "마법형",
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
                        Name = "살율의 춤",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.45, DebuffEffect = new DebuffSet { Vulnerability = 22 } } },
                            { 1, new SkillLevelData { Ratio = 1.7, DebuffEffect = new DebuffSet { Vulnerability = 22 } } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { WekBonusDmg = 0.75 }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "교만의 일격",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 1.6 } },
                            { 1, new SkillLevelData { Ratio = 1.85 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { WekBonusDmg = 0.85 }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "호시탐탐",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { Buff = new BuffSet { Wek_Dmg = 23 } } },
                        { 1, new PassiveLevelData { Buff = new BuffSet { Wek_Dmg = 23 } } }
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "디버프 해제" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Wek = 20 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "약점 공격 시 추가 피해" }
                }
            },

            // 연희


            // 쥬리


            // 벨리카


            // 에스파다


            // 데이지


            // 바네사


            // 키리엘


            // 멜키르


            // 실베스타


            // 프레이야


            // 린


            // 밀리아


            // 유신

            #endregion

            #region 전설 - 지원형 201~

            // ===== 비스킷 =====
            new Character
            {
                Id = 201,
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
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effect = "아군 2명" } }
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
                            { 0, new SkillLevelData { Ratio = 1, DefRatio = 1.15, Bonus = new BuffSet { Arm_Pen = 40 } } },
                            { 1, new SkillLevelData { Ratio = 1.15, DefRatio = 1.35, Bonus = new BuffSet { Arm_Pen = 40 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "대장장이의 강화",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Debuff = new DebuffSet { Def_Reduction = 20 },
                            Effect = "적 방어력 20% 감소"
                        }},
                        { 1, new PassiveLevelData { 
                            Debuff = new DebuffSet { Def_Reduction = 24 },
                            Effect = "적 방어력 24% 감소"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Buff = new BuffSet { Dmg_Rdc_Multi = 20 },
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
                Id = 202,
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
                            }},
                            { 1, new SkillLevelData { 
                                BuffEffect = new BuffSet { Dmg_Dealt = 28 }, 
                                DebuffEffect = new DebuffSet { Def_Reduction = 34 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Debuff = new DebuffSet { Def_Reduction = 7 },
                                Effect = "방어력 41% 감소"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "불협화음",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Eff_Res = 28, Shield_HpRatio = 39 },
                            Effect = "효과 저항 28% 증가, 보호막 최대체력 39%"
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Eff_Res = 34, Shield_HpRatio = 45 },
                            Effect = "효과 저항 34% 증가, 보호막 최대체력 45%"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Buff = new BuffSet { Cri_Dmg = 34 },
                            Effect = "모든 아군 치명타 피해 34% 증가"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "아군 치피증" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Dmg_Rdc = 10 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "적군 방어력 감소 증가" }
                }
            },

            // 오를리


            // 플라튼


            // 로지


            // 엘리스

            #endregion

            #region 전설 - 만능형 301~

            // ===== 레이첼 =====
            new Character
            {
                Id = 301,
                Name = "레이첼",
                Grade = "전설",
                Type = "만능형",
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
                        Name = "염화",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 1.02,
                                DebuffEffect = new DebuffSet { 
                                    Atk_Reduction = 22,
                                    Dmg_Reduction = 17
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 1.22,
                                DebuffEffect = new DebuffSet { 
                                    Atk_Reduction = 22,
                                    Dmg_Reduction = 17
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                TargetCountOverride = 3,
                                Effect = "공격 대상 증가"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "불새",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 1.6,
                                DebuffEffect = new DebuffSet { Def_Reduction = 29, Vulnerability = 22 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 1.6,
                                DebuffEffect = new DebuffSet { Def_Reduction = 36, Vulnerability = 28 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "턴 증가" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "화염의 힘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Buff = new BuffSet { Wek = 22 },
                            Effect = "아군 약공 확률 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            Buff = new BuffSet { Wek = 27 },
                            Effect = "아군 약공 확률 증가"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "턴 증가" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Wek = 20 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "대상 증가" }
                }
            },

            // 아일린


            // 지크


            // 에이스


            // 엘리시아


            // 크리스


            // 제이브


            // 트루드


            // 카르마


            // 스파이크


            // 손오공


            // 챈슬러


            // 니아

            #endregion

            #region 전설 - 방어형 401~

            // 루디


            // 아킬라


            // 녹스


            // 아라곤


            // 룩


            #endregion
        };

        // ===== 헬퍼 메서드 =====

        public static Character GetByName(string name) 
            => Characters.FirstOrDefault(c => c.Name == name);

        public static Character GetById(int id) 
            => Characters.FirstOrDefault(c => c.Id == id);

        public static List<Character> GetByGrade(string grade) 
            => Characters.Where(c => c.Grade == grade).ToList();

        public static List<Character> GetByType(string type) 
            => Characters.Where(c => c.Type == type).ToList();

        public static List<Character> GetByGradeAndType(string grade, string type) 
            => Characters.Where(c => c.Grade == grade && c.Type == type).ToList();

        public static List<string> GetAllNames() 
            => Characters.Select(c => c.Name).ToList();

        public static List<string> GetNamesByGrade(string grade) 
            => Characters.Where(c => c.Grade == grade).Select(c => c.Name).ToList();
    }
}

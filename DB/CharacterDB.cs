using System;
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

            // 타카
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
                            { 0, new SkillLevelData { Ratio = 100, ConditionalExtraDmg = 45, ConditionalDesc = "체력 30% 미만" } },
                            { 1, new SkillLevelData { Ratio = 120, ConditionalExtraDmg = 55, ConditionalDesc = "체력 30% 미만" } }
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
                            { 0, new SkillLevelData { Ratio = 145, Bonus = new BuffSet { Cri_Dmg = 37 } } },
                            { 1, new SkillLevelData { Ratio = 170, Bonus = new BuffSet { Cri_Dmg = 46 } } }
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
                            { 0, new SkillLevelData { Ratio = 145, ConditionalExtraDmg = 260, ConditionalDesc = "체력 30% 미만" } },
                            { 1, new SkillLevelData { Ratio = 170, ConditionalExtraDmg = 260, ConditionalDesc = "체력 30% 미만" } }
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

            // 라이언
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 130 } },
                            { 1, new SkillLevelData { Ratio = 155 } }
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
                                Ratio = 145, 
                                LostHpBonusDmgMax = 50,
                                ConditionalDesc = "잃은 생명력 비례 최대 50%",
                                Bonus = new BuffSet { WekBonusDmg = 230 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 170, 
                                LostHpBonusDmgMax  = 50,
                                ConditionalDesc = "잃은 생명력 비례 최대 50%",
                                Bonus = new BuffSet { WekBonusDmg = 270 }
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
                            PartyBuff = new BuffSet { Dmg_Dealt_1to3 = 25 },
                            Effect = "모든 아군 3인 공격기 피해량 25% 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Dmg_Dealt_1to3 = 31 },
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

            // 델론즈
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 115 } },
                            { 1, new SkillLevelData { Ratio = 135 } }
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
                            { 0, new SkillLevelData { Ratio = 119 }},
                            { 1, new SkillLevelData { Ratio = 158 }}
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
                            PartyBuff = new BuffSet { Dmg_Dealt = 17 }
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Dmg_Dealt = 20 }
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 570 } },
                            { 1, new SkillLevelData { Ratio = 690 } }
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
                            SelfBuff = new BuffSet { Dmg_Rdc = 24, Cri = 35 }
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Dmg_Rdc = 29, Cri = 41 }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { SelfBuff = new BuffSet{ Dmg_Dealt = 80 }, Effect = "디버프당 피해량 증가 20%" } }
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
                            { 0, new SkillLevelData { Ratio = 55 } },
                            { 1, new SkillLevelData { Ratio = 65 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "거궁신포",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 72,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bleeding,
                                        Stacks = 1,
                                        Chance = 55
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 85,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bleeding,
                                        Stacks = 1,
                                        Chance = 60
                                    }
                                }
                            } },
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
                            { 0, new SkillLevelData { 
                                Ratio = 175, 
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.BleedExplosion
                                    }
                                }
                                }},
                            { 1, new SkillLevelData { 
                                Ratio = 205, 
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.BleedExplosion
                                    }
                                }
                                }},
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                StatusEffects = new List<SkillStatusEffect> {
                                    new SkillStatusEffect { Type = StatusEffectType.BleedExplosion, CustomAtkRatio = 150 }
                                },
                                Effect = "출혈폭발 댐증" 
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "화랑의 후예",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Dmg_Dealt = 29 }
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Dmg_Dealt = 35 }
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
            new Character
            {
                Id = 6,
                Name = "카구라-한번두진상태",
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "해방-팔사검",
                        SkillType = SkillType.Skill1,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 62, DebuffEffect = new DebuffSet { Heal_Reduction = 52 } } },
                            { 1, new SkillLevelData { Ratio = 62, DebuffEffect = new DebuffSet { Heal_Reduction = 68 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "해방-뱀 사냥",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 57 }},
                            { 1, new SkillLevelData { Ratio = 57 }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "팔사의 저주",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Cri_Dmg = 28 },
                            SelfBuff = new BuffSet { Dmg_Dealt = 20 }
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Cri_Dmg = 34 },
                            SelfBuff = new BuffSet { Dmg_Dealt = 20 }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "뒤지면 파티 힐" } },
                        { 6, new PassiveTranscend { Debuff = new DebuffSet { Vulnerability = 24 }, Effect = "스킬 발동 시 취약" } }
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "뒤지면 파티 힐" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Eff_Hit = 24 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "스킬 발동 시 취약" }
                }
            },

            // 태오
            new Character
            {
                Id = 7,
                Name = "태오",
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "관-통-데미지!",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 130, Bonus = new BuffSet { Cri = 30 } } },
                            { 1, new SkillLevelData { Ratio = 170 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Cri = 100 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "까악-까악-",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 65, Bonus = new BuffSet { Cri = 30 } }},
                            { 1, new SkillLevelData { Ratio = 85 }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Cri = 100 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "까마귀 눈동자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "피해무효화 3회, 불사 3턴"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "피해무효화 3회, 불사 3턴",
                            ConditionalSelfBuff = new BuffSet { Atk_Rate = 39 }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "불사 쿨초" } }
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "뒤지면 파티 힐" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "스킬 발동 시 취약" }
                }
            },

            // 클라한
            new Character
            {
                Id = 8,
                Name = "클라한",
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "먼저쓰면안됨",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 115, ConditionalExtraDmg = 100 } },
                            { 1, new SkillLevelData { Ratio = 150, ConditionalExtraDmg = 100 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { ConditionalExtraDmg = 115 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "먼저쓰셈",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 115, ConditionalExtraDmg = 125 } },
                            { 1, new SkillLevelData { Ratio = 165, ConditionalExtraDmg = 125 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { ConditionalExtraDmg = 135 } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "호랑이의 용맹",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            SelfBuff = new BuffSet{ Atk_Rate = 27 }
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet{ Atk_Rate = 33 }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { SelfBuff = new BuffSet{ Dmg_Dealt = 33 }, Effect = "피증" } }
                        }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "피증" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "조건충족뎀지증가" }
                }
            },

            // 카일
            new Character
            {
                Id = 9,
                Name = "카일",
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
                            { 0, new SkillLevelData { Ratio = 100, ConditionalExtraDmg = 130 } },
                            { 1, new SkillLevelData { Ratio = 130, ConditionalExtraDmg = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "철쇄폭압",
                        SkillType = SkillType.Skill1,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 60, TargetMaxHpRatio = 8,  AtkCap = 75, ConditionalExtraDmg = 65 } },
                            { 1, new SkillLevelData { Ratio = 72, TargetMaxHpRatio = 10, AtkCap = 75, ConditionalExtraDmg = 65 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet{ Arm_Pen = 40 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "사슬의 무덤",
                        SkillType = SkillType.Skill2,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 70, ConditionalExtraDmg = 65 } },
                            { 1, new SkillLevelData { Ratio = 90, ConditionalExtraDmg = 65 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet{ Arm_Pen = 40 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "소검쌍무",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            SelfBuff = new BuffSet{ Cri = 27 },
                            StatusEffects = new List<SkillStatusEffect> { new SkillStatusEffect{ Type = StatusEffectType.ChainDamage } }
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet{ Cri = 27 },
                            StatusEffects = new List<SkillStatusEffect> { new SkillStatusEffect{ Type = StatusEffectType.ChainDamage, CustomTargetMaxHpRatio = 32 } }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Effect = "잉 때리면 또 버티기" } }
                        }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "잉 때리면 또 버티기" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "스킬에 방무" }
                }
            },

            // 콜트
            new Character
            {
                Id = 10,
                Name = "콜트",
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
                            { 0, new SkillLevelData { Ratio = 55 } },
                            { 1, new SkillLevelData { Ratio = 70 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "빠르고, 멋있게",
                        SkillType = SkillType.Skill1,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 40,  
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bomb,
                                        Stacks = 1,
                                        Chance = 35
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 50,  
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bomb,
                                        Stacks = 1,
                                        Chance = 45
                                    }
                                }
                            } },
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어떄, 화려하지?",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 125,  
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bomb,
                                        Stacks = 1,
                                        Chance = 50
                                    },
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.BombDetonation
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 150,  
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Bomb,
                                        Stacks = 1,
                                        Chance = 50
                                    },
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.BombDetonation
                                    }
                                },
                                DebuffEffect = new DebuffSet{ Dmg_Reduction = 13 }
                            } },
                        }
                    },
                },
                Passive = new Passive
                {
                    Name = "방랑의 사냥꾼",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Spd,      
                                    TargetStat = StatType.Atk,      
                                    PerUnit = 120,                  
                                    SourceUnit = 7,                 
                                    MaxValue = 1080                 
                                }
                            },
                            PartyBuff = new BuffSet{ Eff_Hit = 28 }
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Spd,      
                                    TargetStat = StatType.Atk,      
                                    PerUnit = 120,                  
                                    SourceUnit = 7,                 
                                    MaxValue = 1320                 
                                }
                            },
                            PartyBuff = new BuffSet{ Eff_Hit = 34 }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Effect = "권능" } },
                            { 6, new PassiveTranscend { Effect = "적군 처치 쿨감" } },
                        }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "권능" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "적군 처치 쿨감" }
                }
            },

            // 아멜리아
            new Character
            {
                Id = 11,
                Name = "아멜리아",
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "통행금지",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 60,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Concussion,
                                        Stacks = 1,
                                        Chance = 60
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 70,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Concussion,
                                        Stacks = 1,
                                        Chance = 60
                                    }
                                }
                            } },
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "위기대응",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 135, Effect = "회복불가(미구현)" } },
                            { 1, new SkillLevelData { Ratio = 165, Effect = "회복불가(미구현)" }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { TargetCountOverride = 2 } },
                            { 6, new SkillTranscend { Effect = "생명력전환 55%"} }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "첨단 나노 슈트",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Eff_Hit = 40 }
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Eff_Hit = 49 }
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

            // 발리스타
            new Character
            {
                Id = 12,
                Name = "발리스타",
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "죽음의 그림자",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 80, Effect="처치시 70%위력 함더" } },
                            { 1, new SkillLevelData { Ratio = 80, Effect="처치시 100%위력 함더" } },
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "사신의 딸",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            CoopAttack = new CoopAttack
                            {
                              TriggerChance = 25,
                              AtkCount = 1,
                              Ratio = 100,
                              TargetCount = 1 
                            },
                            Effect ="위장, 협공, 4회 공격 시 1100 고정뎀"
                        }},
                        { 1, new PassiveLevelData { 
                            CoopAttack = new CoopAttack
                            {
                              TriggerChance = 30,
                              AtkCount = 1,
                              Ratio = 100,
                              TargetCount = 1 
                            },
                            Effect ="위장, 협공확률 증가, 4회 공격 시 1100 고정뎀"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "고정 피해량 1485" } },
                        { 6, new PassiveTranscend { Effect = "4회 공격 시 모든 피해 무효 1회" } },
                    }
                    
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "고정 피해량 1485" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "4회 공격 시 모든 피해 무효 1회" }
                }
            },

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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 390, Bonus = new BuffSet { Cri = 50 } } },
                            { 1, new SkillLevelData { Ratio = 470, Bonus = new BuffSet { Cri = 50 } } }
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
                            SelfBuff = new BuffSet { Atk_Rate = 27, Cri_Dmg = 40 },
                            Effect = "자신 공격력 27% 증가, 치피 40% 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            SelfBuff = new BuffSet { Atk_Rate = 33, Cri_Dmg = 40 },
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 75, Bonus = new BuffSet { WekBonusDmg = 35 } } },
                            { 1, new SkillLevelData { Ratio = 90, Bonus = new BuffSet { WekBonusDmg = 42 } } }
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
                            { 0, new SkillLevelData { Ratio = 145, Bonus = new BuffSet { WekBonusDmg = 130 } } },
                            { 1, new SkillLevelData { Ratio = 170, Bonus = new BuffSet { WekBonusDmg = 155 } } }
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
                        { 0, new PassiveLevelData { SelfBuff = new BuffSet { Atk_Rate = 24 } } },
                        { 1, new PassiveLevelData { SelfBuff = new BuffSet { Atk_Rate = 29 } } }
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            SelfBuff = new BuffSet { Wek = 39 },
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            { 0, new SkillLevelData { Ratio = 145, DebuffEffect = new DebuffSet { Vulnerability = 22 } } },
                            { 1, new SkillLevelData { Ratio = 170, DebuffEffect = new DebuffSet { Vulnerability = 22 } } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { WekBonusDmg = 75 }
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
                            { 0, new SkillLevelData { Ratio = 160 } },
                            { 1, new SkillLevelData { Ratio = 185 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { WekBonusDmg = 85 }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "호시탐탐",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { PartyBuff = new BuffSet { Wek_Dmg = 23 } } },
                        { 1, new PassiveLevelData { PartyBuff = new BuffSet { Wek_Dmg = 23 } } }
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
            new Character
            {
                Id = 104,
                Name = "연희",
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
                            { 0, new SkillLevelData { 
                                Ratio = 100,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Sleep,
                                        Stacks = 1,
                                        Chance = 40
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 130,
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Sleep,
                                        Stacks = 1,
                                        Chance = 40
                                    }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                TargetCountOverride = 2
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "파괴의 손짓",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 43, TargetMaxHpRatio = 6, AtkCap = 100 } },
                            { 1, new SkillLevelData { Ratio = 51, TargetMaxHpRatio = 7, AtkCap = 100 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "종말의 영면",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 41, Bonus = new BuffSet { Arm_Pen = 40 },
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Sleep,
                                        Stacks = 1,
                                        Chance = 50
                                    }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 55, Bonus = new BuffSet { Arm_Pen = 40 },
                                StatusEffects = new List<SkillStatusEffect>
                                {
                                    new SkillStatusEffect 
                                    { 
                                        Type = StatusEffectType.Sleep,
                                        Stacks = 1,
                                        Chance = 50
                                    }
                                }
                            } },
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "혜안",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Atk_Rate = 19 }
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Atk_Rate = 25 }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            SelfBuff = new BuffSet { Dmg_Dealt = 35 }
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "평타 적군 2명" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri_Dmg = 24 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "자신 마피증 35%" }
                }
            },

            // 쥬리
            new Character
            {
                Id = 105,
                Name = "쥬리",
                Grade = "전설",
                Type = "마법형",
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
                            { 0, new SkillLevelData { 
                                Ratio = 55,
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 70,
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "천상의 심판",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 65, DebuffEffect = new DebuffSet{ Def_Reduction = 20 } } },
                            { 1, new SkillLevelData { Ratio = 77, DebuffEffect = new DebuffSet{ Def_Reduction = 29 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "최고 판결",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70, Effect = "영멸(님 부활못함ㅋ)",
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 80, Effect = "영멸(님 부활못함ㅋ)",
                            } },
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "신의 저울",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            CoopAttack = new CoopAttack
                            {
                                TriggerChance = 25,
                                TargetCount = 3,
                                AtkCount = 1,
                                Ratio = 40,
                                TargetMaxHpRatio = 7,
                                AtkCap = 300
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            CoopAttack = new CoopAttack
                            {
                                TriggerChance = 25,
                                TargetCount = 3,
                                AtkCount = 1,
                                Ratio = 50,
                                TargetMaxHpRatio = 9,
                                AtkCap = 300
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            CoopChanceBonus = 10
                        }},
                        { 6, new PassiveTranscend { 
                            Effect = "불사 시 행동 제어 면역"
                        }}
                    }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "협공 확률 증" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri_Dmg = 24 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "불사 시 행동 제어 면역" }
                }
            },

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
                            { 0, new SkillLevelData { Ratio = 50, DefRatio = 60 } },
                            { 1, new SkillLevelData { Ratio = 50, DefRatio = 60, DebuffEffect = new DebuffSet { Dmg_Reduction = 6 } } }
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
                            { 0, new SkillLevelData { Ratio = 100, DefRatio = 115, Bonus = new BuffSet { Arm_Pen = 40 } } },
                            { 1, new SkillLevelData { Ratio = 115, DefRatio = 135, Bonus = new BuffSet { Arm_Pen = 40 } } }
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
                            PartyBuff = new BuffSet { Dmg_Rdc_Multi = 20 },
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                            PartyBuff = new BuffSet { Eff_Res = 28 },
                            SelfBuff = new BuffSet { Shield_HpRatio = 39 },
                            Effect = "효과 저항 28% 증가, 보호막 최대체력 39%"
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Eff_Res = 34 },
                            SelfBuff = new BuffSet { Shield_HpRatio = 45 },
                            Effect = "효과 저항 34% 증가, 보호막 최대체력 45%"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            PartyBuff = new BuffSet { Cri_Dmg = 34 },
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
                            { 0, new SkillLevelData { Ratio = 100 } },
                            { 1, new SkillLevelData { Ratio = 120 } }
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
                                Ratio = 102,
                                DebuffEffect = new DebuffSet { 
                                    Atk_Reduction = 22,
                                    Dmg_Reduction = 17
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 122,
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
                                Ratio = 160,
                                DebuffEffect = new DebuffSet { Def_Reduction = 29, Vulnerability = 22 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 160,
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
                            PartyBuff = new BuffSet { Wek = 22 },
                            Effect = "아군 약공 확률 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            PartyBuff = new BuffSet { Wek = 27 },
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

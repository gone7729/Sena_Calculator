using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

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
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.Debuff,
                                    StatusType = StatusEffectType.EagleClaw,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,           // 2회 공격 시
                                    StacksPerTrigger = 2,       // 2스택 부여
                                    MaxStacks = 8,              // 최대 8스택
                                    Debuff = new DebuffSet { Dmg_Taken_Increase = 3 }  // 스택당 받피증 3%
                                    // 타카의 모든 공격이 2회 적중 시 매의 발톱 디버프를 2스택 부여한다. 스킬 공격은 2회 공격으로 판정한다.
                                }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.Debuff,
                                    StatusType = StatusEffectType.EagleClaw,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,
                                    StacksPerTrigger = 2,
                                    MaxStacks = 8,
                                    Debuff = new DebuffSet { Dmg_Taken_Increase = 3 }
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.Debuff,
                                    StatusType = StatusEffectType.EagleClaw,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,
                                    StacksPerTrigger = 2,
                                    MaxStacks = 8,
                                    Debuff = new DebuffSet { Dmg_Taken_Increase = 4 }  // 초월 시 스택당 3% → 4%
                                }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
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
                                LostHpAssumedRemaining = 0,
                                ConditionalDesc = "잃은 생명력 비례 최대 50%",
                                Bonus = new BuffSet { WekBonusDmg = 230 }
                            }},
                            { 1, new SkillLevelData {
                                Ratio = 170,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
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
                            Effect = "모든 아군 3인 공격기 피해량 25% 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 25 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "모든 아군 3인 공격기 피해량 31% 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 31 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
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
                            { 2, new SkillTranscend { OnKillRecast = new OnKillRecast { RatioPercent = 100 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "죽음의 경계",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Party, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Dmg_Dealt_Type = 17 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Party, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 6, new PassiveTranscend { Effect = "아군 사망 시 모든 피해 무효화 피격 1회, 아군 사망 시 물리 공격력 증가 39%(2턴)" } }
                        }
                },
                TranscendType = TranscendType.AtkCri
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
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect {
                                        Target = EffectTarget.Self,
                                        Type = SkillEffectType.PerEnemyDebuffDmgBonus,
                                        PercentPerDebuff = 50,
                                        MaxDebuffStacks = 4
                                    }
                                },
                                Effect = "상대 디버프 1개당 피해량 증가 50%(최대 4개)"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "악마의 힘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 24, Cri = 35 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 29, Cri = 41 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.PerEnemyDebuffDmgBonus,
                                    PercentPerDebuff = 20,
                                    MaxDebuffStacks = 4
                                }
                            },
                            Effect = "상대 디버프 1개당 피해량 증가 20%(최대 4개)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 55 }
                                }
                            } },
                            { 1, new SkillLevelData {
                                Ratio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 60 }
                                }
                            } }
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BleedExplosion }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Ratio = 205,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BleedExplosion }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BleedExplosion, CustomAtkRatio = 150 }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 29 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 35 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 카구라
            new Character
            {
                Id = 6,
                Name = "카구라",
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
                            { 0, new SkillLevelData { Ratio = 62, Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 52 } }
                            }}},
                            { 1, new SkillLevelData { Ratio = 62, Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 68 } }
                            }}}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 24 } }
                            }}}
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
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 24 } }
                            }}}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "팔사의 저주",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 28 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 34 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "뒤지면 파티 힐" } },
                        { 6, new PassiveTranscend { Effect = "스킬 발동 시 취약" } }
                    }
                },
                TranscendType = TranscendType.AtkEff
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Atk_Rate = 39 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "불사 쿨초" } }
                    }
                },
                TranscendType = TranscendType.AtkCriDmg
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 27 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 33 } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Dmg_Dealt_Type = 33 } }
            }, Effect = "피증" } }
                        }
                },
                TranscendType = TranscendType.AtkCri
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.ChainDamage }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.ChainDamage, CustomTargetMaxHpRatio = 32 }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Effect = "잉 때리면 또 버티기" } }
                        }
                },
                TranscendType = TranscendType.AtkCri
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 35 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 50,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 45 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어때, 화려하지?",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 125,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BombDetonation }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BombDetonation },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 13 } }
                                }
                            } }
                        }
                    }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28 } }
                            }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34 } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { Effect = "권능" } },
                            { 6, new PassiveTranscend { Effect = "적군 처치 쿨감" } }
                        }
                },
                TranscendType = TranscendType.AtkEff
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Concussion, Stacks = 1, Chance = 60 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Concussion, Stacks = 1, Chance = 60 }
                                }
                            } }
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
                            { 0, new SkillLevelData {
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 2, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData {
                                Ratio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 2, Stacks = 1, Chance = 100 }
                                }
                            } }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 49 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
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
                            { 0, new SkillLevelData { Ratio = 95, OnKillRecast = new OnKillRecast { RatioPercent = 100 } } },
                            { 1, new SkillLevelData {
                                Ratio = 110,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect
                                    {
                                        Target = EffectTarget.Self,
                                        Type = SkillEffectType.Buff,
                                        Duration = 3,
                                        Buff = new BuffSet { Coop_Chance = 28 }
                                    }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "사신의 딸",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.CoopAttack,
                                    CoopAttack = new CoopAttack
                                    {
                                        TriggerChance = 25,
                                        AtkCount = 1,
                                        Ratio = 100,
                                        TargetCount = 1
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 3,
                                        TriggerOn = TriggerCondition.AllAttack,
                                        FixedDamage = 1285,
                                        TargetCount = 3,
                                        HitCount = 1
                                    }
                                }
                            },
                            Effect = "위장, 협공"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.CoopAttack,
                                    CoopAttack = new CoopAttack
                                    {
                                        TriggerChance = 30,
                                        AtkCount = 1,
                                        Ratio = 100,
                                        TargetCount = 1
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 3,
                                        TriggerOn = TriggerCondition.AllAttack,
                                        FixedDamage = 1285,
                                        TargetCount = 3,
                                        HitCount = 1
                                    }
                                }
                            },
                            Effect = "위장, 협공확률 증가"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 3,
                                        TriggerOn = TriggerCondition.AllAttack,
                                        FixedDamage = 1485,
                                        TargetCount = 3,
                                        HitCount = 1
                                    }
                                }
                            }
                        } },
                        { 6, new PassiveTranscend { Effect = "2회 공격 시 모든 피해 무효 1회 (미확립)" } }
                    }
                    
                },
                TranscendType = TranscendType.AtkCri
            },

            // 여포
            new Character
            {
                Id = 13,
                Name = "여포",
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
                            { 0, new SkillLevelData { Ratio = 80 } },
                            { 1, new SkillLevelData { Ratio = 100 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "혈풍벽파",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 57,
                                Bonus = new BuffSet { WekBonusDmg = 25, WekBonusDmgPerHit = true },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                }
                            }},
                            { 1, new SkillLevelData { Ratio = 67,
                                Bonus = new BuffSet { WekBonusDmg = 25, WekBonusDmgPerHit = true },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 55 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 29 } }
                            }}},
                            { 6, new SkillTranscend { HealAtkRatio = 40, Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Arm_Pen = 10 } }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "적토질풍격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 52,
                                Bonus = new BuffSet { WekBonusDmg = 27, WekBonusDmgPerHit = true }
                            }},
                            { 1, new SkillLevelData { Ratio = 60,
                                Bonus = new BuffSet { WekBonusDmg = 32, WekBonusDmgPerHit = true }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 29 } }
                            }}},
                            { 6, new SkillTranscend { HealAtkRatio = 40, Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Arm_Pen = 10 } }
                            }}}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "초인적인 힘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 40 } },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.MarkAttack,
                                    MarkAttack = new MarkAttack
                                    {
                                        MaxStacks = 2,
                                        AtkCount = 1,
                                        TargetMaxHpRatio = 16,
                                        StackOnNormalEvery = 2,
                                        StackOnSkillEvery = 1,
                                        OnMaxStackSelfBuff = new BuffSet { Wek_Dmg = 28 },
                                        OnMaxStackSelfBuffDuration = 5
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 2,
                                        TriggerOn = TriggerCondition.NormalOnly,
                                        AtkRatio = 45,
                                        TargetCount = 1,
                                        HitCount = 1
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 1,
                                        TriggerOn = TriggerCondition.SkillOnly,
                                        AtkRatio = 45,
                                        TargetCount = 1,
                                        HitCount = 1
                                    }
                                }
                            },
                            Effect = "축복 40%, 표식:방천화극의 분노"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.MarkAttack,
                                    MarkAttack = new MarkAttack
                                    {
                                        MaxStacks = 2,
                                        AtkCount = 1,
                                        TargetMaxHpRatio = 16,
                                        StackOnNormalEvery = 2,
                                        StackOnSkillEvery = 1,
                                        OnMaxStackSelfBuff = new BuffSet { Wek_Dmg = 28 },
                                        OnMaxStackSelfBuffDuration = 5
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 2,
                                        TriggerOn = TriggerCondition.NormalOnly,
                                        AtkRatio = 45,
                                        TargetCount = 1,
                                        HitCount = 1
                                    }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage
                                    {
                                        TriggerCount = 1,
                                        TriggerOn = TriggerCondition.SkillOnly,
                                        AtkRatio = 45,
                                        TargetCount = 1,
                                        HitCount = 1
                                    }
                                }
                            },
                            Effect = "축복 25%, 표식:방천화극의 분노"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 백룡
            new Character
            {
                Id = 14,
                Name = "백룡",
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
                        Name = "섬광십무",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 340, 
                                Bonus = new BuffSet{ Arm_Pen = 40 }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 410, 
                                Bonus = new BuffSet{ Arm_Pen = 40 }
                             } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ HealAtkRatio = 30 }},
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Arm_Pen = 25 } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "쌍절태풍격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 475, 
                                Bonus = new BuffSet{ Arm_Pen = 40 } 
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 575, 
                                Bonus = new BuffSet{ Arm_Pen = 40 } 
                              } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Arm_Pen = 25 } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "전설의 부활",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27, Arm_Pen = 5 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 33, Arm_Pen = 10 } }
                            }
                        }}
                    }
                    
                },
                TranscendType = TranscendType.AtkWek
            },

            #endregion

            #region 영웅 - 공격형 51~

            // 풍연
            new Character
            {
                Id = 51,
                Name = "풍연",
                Grade = "영웅",
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
                        Name = "구음검격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 155  } },
                            { 1, new SkillLevelData { Ratio = 190 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } }
            } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "이기어검",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 175 } },
                            { 1, new SkillLevelData { Ratio = 210 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet{ Arm_Pen = 40 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "검의 결계",
                    MaxStacks = 8,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { Effect = "빙결 면역, 피면" } },
                        { 1, new PassiveLevelData { Effect = "빙결 면역, 피면",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 40 } }
                            }
                        } }
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 쥬피
            new Character
            {
                Id = 52,
                Name = "쥬피",
                Grade = "영웅",
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
                            { 0, new SkillLevelData { 
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 50 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "연사",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 370 } },
                            { 1, new SkillLevelData { Ratio = 440 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "저격",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 315, 
                                ConditionalExtraDmg = 155
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 375, 
                                ConditionalExtraDmg = 155
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { ConditionalExtraDmg = 30 } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 눈",
                    MaxStacks = 8,
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Cri = 37 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 40 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Cri = 45 } }
                            }
                        } }
                    }
                },
                TranscendType = TranscendType.AtkCri
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
                AttackType = AttackType.Magic,
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
                            Effect = "자신 공격력 27% 증가, 치피 40% 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 27, Cri_Dmg = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 공격력 33% 증가, 치피 40% 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 33, Cri_Dmg = 40 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCriDmg
            },

            // ===== 루리 =====
            new Character
            {
                Id = 102,
                Name = "루리",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 29 } }
                            }
                        } }
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 39 } }
            },
                            Effect = "약확 39% 증가" 
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // ===== 미호 =====
            new Character
            {
                Id = 103,
                Name = "미호",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                            { 0, new SkillLevelData { Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                }
                            } },
                            { 1, new SkillLevelData { Ratio = 170,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                }
                            } }
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
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } }
                            }
                        } }
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 연희
            new Character
            {
                Id = 104,
                Name = "연희",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 40 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 40 }
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 50 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 55, Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 50 }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "혜안",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 35 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 쥬리
            new Character
            {
                Id = 105,
                Name = "쥬리",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 55
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 70
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
                            { 0, new SkillLevelData { Ratio = 65,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                }
                            } },
                            { 1, new SkillLevelData { Ratio = 77,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 29 } }
                                }
                            } }
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
                                Ratio = 70, Effect = "영멸(님 부활못함ㅋ)"
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 80, Effect = "영멸(님 부활못함ㅋ)"
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "신의 저울",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.CoopAttack,
                                    CoopAttack = new CoopAttack
                                    {
                                        TriggerChance = 25,
                                        TargetCount = 3,
                                        AtkCount = 1,
                                        Ratio = 40,
                                        TargetMaxHpRatio = 7,
                                        AtkCap = 300
                                    }
                                }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.CoopAttack,
                                    CoopAttack = new CoopAttack
                                    {
                                        TriggerChance = 25,
                                        TargetCount = 3,
                                        AtkCount = 1,
                                        Ratio = 50,
                                        TargetMaxHpRatio = 9,
                                        AtkCap = 300
                                    }
                                }
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
                TranscendType = TranscendType.AtkCri
            },

            // 벨리카
            new Character
            {
                Id = 106,
                Name = "벨리카",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "메테오",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 105, Bonus = new BuffSet{ CriBonusDmg = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 60 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120, Bonus = new BuffSet{ CriBonusDmg = 50 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 60 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어둠의 환영",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 105, Bonus = new BuffSet{ CriBonusDmg = 55 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120, Bonus = new BuffSet{ CriBonusDmg = 55 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 60 }
                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "흑월의 축복",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "시탑, 무탑에서 마법피해량증가 28%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "시탑, 무탑에서 마법피해량증가 31%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 33 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "보호막"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 에스파다
            new Character
            {
                Id = 107,
                Name = "에스파다",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정화탄",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 26 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 33 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "신의 심판",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 425,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    ConsumeCount = 4,
                                    TargetMaxHpRatio = 26,
                                    AtkCap = 1300
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 515,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    ConsumeCount = 4,
                                    TargetMaxHpRatio = 26,
                                    AtkCap = 1300
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    TargetMaxHpRatio = 6,
                                    AtkCap = 300
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "마력 정화",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "마력 정화 중첩(4스택)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Mark_Purify = 32, Cri = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "마력 정화 중첩(4스택)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Mark_Purify = 40, Cri = 40 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 29 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 데이지
            new Character
            {
                Id = 108,
                Name = "데이지",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "비화선",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 270, TargetMaxHpRatio = 20, AtkCap = 350
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 330, TargetMaxHpRatio = 20, AtkCap = 350
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                TargetMaxHpRatio = 6, AtkCap = 350
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "불나비",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 27 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 155,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 33 } }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "기민한 움직임",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "공격력 높은 아군 2명",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 21 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "공격력 높은 아군 2명",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 27 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "아군 보호막"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 바네사
            new Character
            {
                Id = 109,
                Name = "바네사",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 130
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "모래열풍",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 95, Effect = "70%확률 쿨타임 증가 19초"
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 110, Effect = "70%확률 쿨타임 증가 23초"
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "버프 해제 2개"
                            }},
                            {6, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 24 } }
            } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "메마른 해일",
                        SkillType = SkillType.Skill2,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 82,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 50 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                           {6, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 24 } }
            } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "사막의 마법사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "스킬 시 취약"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 키리엘
            new Character
            {
                Id = 110,
                Name = "키리엘",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 35 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "레굴루스",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 220, ConditionalDmgBonus = 160
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 267, ConditionalDmgBonus = 160
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "유성우",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 30 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 67,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 35 }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "별의 축제",
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 17 } }
                            }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "행동 제어 면역, 보호막량 상승"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 멜키르
            new Character
            {
                Id = 111,
                Name = "멜키르",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 45 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50, CustomTargetMaxHpRatio = 15 }
                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "어둠 강탈",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 55 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "금지된 실험",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 52,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50, CustomTargetMaxHpRatio = 15 }
                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "타락한 마법사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 40 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 28 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "마법형 3명일 시 공격 3회 시 적군 3명 100% 마력 역류"
                        }},
                        { 6, new PassiveTranscend { 
                            Effect = "마력 역류 15%로 변경"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 실베스타
            new Character
            {
                Id = 112,
                Name = "실베스타",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 130
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정의의 단죄",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Ratio = 72,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 }
                            } },
                            { 1, new SkillLevelData {
                                Ratio = 85,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "정화의 일격",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 60
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 75
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "기사의 의지",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "권능 발동 시 모든 디버프 해제, 체력 100% 회복"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 프레이야
            new Character
            {
                Id = 113,
                Name = "프레이야",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 50 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "고결한 금풍",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 57,
                                DispelDefReduction = 22,
                                PreCastBuff = new BuffSet{ MagicAtk_Rate = 10 },
                                Effect = "버프 2개 해제, 해제한 버프 개수 만큼 방어력 감소(2중첩)"
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 67,
                                DispelDefReduction = 22,
                                PreCastBuff = new BuffSet { MagicAtk_Rate = 10 },
                                Effect = "버프 2개 해제, 해제한 버프 개수 만큼 방어력 감소(2중첩)",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 75 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "금빛 검우",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 60,
                                Effect = "항상 4중첩 유지",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 52 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 80,
                                Effect = "항상 4중첩 유지",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 39 }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "아름다운 지배자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 40 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 31 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 37 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "전투 시작 시 4중첩"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 린
            new Character
            {
                Id = 114,
                Name = "린",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 45 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 50 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "용린성하",
                        SkillType = SkillType.Skill1,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 62
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 62,
                                Bonus = new BuffSet { CriBonusDmg = 74, CriBonusDmgPerHit = true }
                                
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effect = "처치 시 쿨초"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "용제의 진노",
                        SkillType = SkillType.Skill2,
                        TargetCount = 4,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70,
                                ConditionalExtraDmg = 30,
                                ConditionalExtraDmgPerHit = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 90,
                                ConditionalExtraDmg = 42,
                                ConditionalExtraDmgPerHit = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 100 }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "홍린포",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Cri,      
                                    PerUnit = 350,                  
                                    SourceUnit = 3,                 
                                    MaxValue = 27                 
                                }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Cri,      
                                    PerUnit = 350,                  
                                    SourceUnit = 3,                 
                                    MaxValue = 33                 
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "부활 시 피해 무효 2회"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 밀리아
            new Character
            {
                Id = 115,
                Name = "밀리아",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 30, DefRatio = 30,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 30 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 30, DefRatio = 30,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, CustomTriggerCount = 5 }
                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "수정룡의 거울",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 30, DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 30, DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 65 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, CustomTriggerCount = 5 }
                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "수정의 숨결",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 30, DefRatio = 32,
                                Effect = "빗나감 확률 36%",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 30, DefRatio = 32,
                                Effect = "빗나감 확률 48%",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 45 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "도발 2턴"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "광채의 수정비늘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            FlatBonus = new BaseStatSet { Def = 1009, Hp = 3929},
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            FlatBonus = new BaseStatSet { Def = 1009, Hp = 3929},
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 20 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 유신
            new Character
            {
                Id = 116,
                Name = "유신",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 13 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 16 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "뇌운 흑영랑",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 92,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "아군 디버프 2개 해제"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "번뇌",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 45 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 31 } }
            }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "신선의 가르침",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 소교
            new Character
            {
                Id = 117,
                Name = "소교",
                Grade = "전설",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "우후죽순",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 270,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 }
                                
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 330,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "아군 디버프 2개 해제"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "호접지몽",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 26 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 26 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 31 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 46 } }
            }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "전장의 공주",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 29 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 39 } }
            } }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            #endregion

            #region 영웅 - 마법형 151~

            // 유리
            new Character
            {
                Id = 151,
                Name = "유리",
                Grade = "영웅",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정기 흡수",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "스킬 사용 시 주피증 29%" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "마력의 구슬",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 285, ConditionalExtraDmg = 170 } },
                            { 1, new SkillLevelData { Ratio = 340, ConditionalExtraDmg = 170 } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "여우의 매력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 21 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 21 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Dmg_Dealt_Type = 29 } }
            } }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 아리엘
            new Character
            {
                Id = 152,
                Name = "아리엘",
                Grade = "영웅",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "빛의 심판",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 75 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 75 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, Chance = 25 }
                }  
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "눈부신 빛",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                }
                            } },
                            { 1, new SkillLevelData { Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } }
            } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "신의 은총",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 11 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 15 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Dmg_Dealt_Type = 29 } }
            } }}
                    }
                },
                TranscendType = TranscendType.AtkCriDmg
            },

            //노호
            new Character
            {
                Id = 153,
                Name = "노호",
                Grade = "영웅",
                Type = "마법형",
                AttackType = AttackType.Magic,
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
                                Ratio = 50, HpRatio = 12
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, HpRatio = 14
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "잘못된 기록",
                        SkillType = SkillType.Skill1,
                        TargetCount = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 35 }
                                }
                            } },
                            { 1, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, Chance = 25 }
                }  
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "파멸의 고서",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 80, HpRatio = 19 } },
                            { 1, new SkillLevelData { Ratio = 95, HpRatio = 22 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 16 } }
            } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "칼보다 강한 펜",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { MagicAtk_Rate = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { MagicAtk_Rate = 21 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {6, new PassiveTranscend{ Effect = "보호막" }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            #endregion

            #region 전설 - 지원형 - 마법 201~

            // ===== 비스킷 =====
            new Character
            {
                Id = 201,
                Name = "비스킷",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 1, new SkillLevelData { Ratio = 50, DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
                                }
                            } }
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
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Bos = 33, Wek = 44 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Bos = 40, Wek = 54 } }
                                }
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
                            Effect = "적 방어력 20% 감소",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "적 방어력 24% 감소",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc_Multi = 20 } }
            },
                            Effect = "아군 5인 공격기 받는 피해 20% 감소"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // ===== 리나 =====
            new Character
            {
                Id = 202,
                Name = "리나",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 23 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 28 } }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 7 } }
            },
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
                            Effect = "효과 저항 28% 증가, 보호막 최대체력 39%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 39 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "효과 저항 34% 증가, 보호막 최대체력 45%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 45 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 34 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 34 } }
            },
                            Effect = "모든 아군 치명타 피해 34% 증가"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 오를리
            new Character
            {
                Id = 203,
                Name = "오를리",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 0, new SkillLevelData { Ratio = 50, HpRatio = 12 } },
                            { 1, new SkillLevelData { 
                                Ratio = 50, HpRatio = 12,
                                HealHpRatio = 15,
                                Effect = "생명력 회복 15% 지속 1턴"
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "고결한 유성",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 60, HpRatio = 14 } },
                            { 1, new SkillLevelData { Ratio = 60, HpRatio = 14,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 36 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "빛의 일갈",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 20, HpRatio = 5
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 22, HpRatio = 6
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "디버프 해제 2개"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "통찰의 빛",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 17, Cri_Dmg = 25 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "보호막량 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 17, Cri_Dmg = 25 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "스킬 발동 시 아군 마법형 모든 피해 무효화 1회"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 플라튼
            new Character
            {
                Id = 204,
                Name = "플라튼",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 0, new SkillLevelData { Ratio = 50, DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 7 } }
                                }
                            } },
                            { 1, new SkillLevelData { Ratio = 60, DefRatio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 9 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "심판대행",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { Ratio = 42, DefRatio = 47, HealDmgRatio = 32 } },
                            { 1, new SkillLevelData { Ratio = 50, DefRatio = 55, HealDmgRatio = 43 } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "이계의 빛",
                        SkillType = SkillType.Skill2,
                        TargetCount = 2,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 32 }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "네스트라의 가호",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "방어력의 165% 회복, 생명력 회복 2회"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "방어력의 190% 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "생명력 회복 3회로 증가"
                        }},
                        { 6, new PassiveTranscend { 
                            Effect = "고정피해1485"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 로지
            new Character
            {
                Id = 205,
                Name = "로지",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 0, new SkillLevelData { Ratio = 30, HpRatio = 7 } },
                            { 1, new SkillLevelData { 
                                Ratio = 30, HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 16 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "달빛의 심판",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 30, HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 37, HpRatio = 10,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "성령의 기도",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Effect = "아군 2명 부활, 적군 턴감 2턴"
                            }},
                            { 1, new SkillLevelData { 
                                Effect = "아군 2명 부활, 적군 턴감 2턴, 부활 시 권능 2턴"
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effect = "권능 아군 전체"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "망자의 축복",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Hp,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 100,                  
                                    SourceUnit = 1000,                 
                                    MaxValue = 900                 
                                }
                            },
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Hp,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 100,                  
                                    SourceUnit = 1000,                 
                                    MaxValue = 900                 
                                }
                            },
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 6 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 엘리스
            new Character
            {
                Id = 206,
                Name = "엘리스",
                Grade = "전설",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 1, new SkillLevelData { 
                                Ratio = 120
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                HealHpRatio = 15
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "비밀의 문",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                HealHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 31 } }
                                }
                            } },
                            { 1, new SkillLevelData {
                                HealHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 39 } }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Heal_Bonus = 36 } }
            }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "내 카드 속엔?",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                HealHpRatio = 29
                            }},
                            { 1, new SkillLevelData { 
                                HealHpRatio = 33
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "나, 너, 로맨틱",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Def_Rate = 31 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Def_Rate = 31 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            #endregion

            #region 영웅 - 지원형 - 마법 251~

            // 유이
            new Character
            {
                Id = 251,
                Name = "유이",
                Grade = "영웅",
                Type = "지원형",
                AttackType = AttackType.Magic,
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
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 11 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "축복의 선율",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { HealHpRatio = 15 } },
                            { 1, new SkillLevelData { HealHpRatio = 15 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 19 } }
                            }}},
                            {6, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24 } }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "전장의 선율",
                        SkillType = SkillType.Skill2,
                        TargetCount = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Effect = "두명 부활"
                            }},
                            { 1, new SkillLevelData { 
                                Effect = "두명 부활"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "전장의 마에스트로",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "화상면역"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "화상면역"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            #endregion

            #region 영웅 - 지원형 - 물리 276~

            // 카론
            new Character
            {
                Id = 276,
                Name = "카론",
                Grade = "영웅",
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
                        Name = "자연의 회복",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { HealAtkRatio = 180 } },
                            { 1, new SkillLevelData { HealAtkRatio = 230 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect { TargetCount = 3 }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "자연의 숨결",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { HealAtkRatio = 105 } },
                            { 1, new SkillLevelData { HealAtkRatio = 125 } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect {  }
                            }}}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "자연의 보호",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet {  } }
                            } ,
                            Effect = "출혈면역"
                        }},
                        { 1, new PassiveLevelData { 
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet {  } }
                            },
                            Effect = "출혈면역"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            #endregion

            #region 전설 - 만능형 - 물리 301~

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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 22,
                                    Dmg_Reduction = 17 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 122,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 22,
                                    Dmg_Reduction = 17 } }
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 29, Vulnerability = 22 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 36, Vulnerability = 28 } }
                                }
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
                            Effect = "아군 약공 확률 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 22 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "아군 약공 확률 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 27 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 아일린
            new Character
            {
                Id = 302,
                Name = "아일린",
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
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "뇌제의 분노",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 113,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 136,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 45 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effect = "관통 추가"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "청천벽력",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 87,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "관통 추가" } },
                            { 6, new SkillTranscend { 
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Chance = 10 }
                } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "포디나의 분노",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 25 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 지크
            new Character
            {
                Id = 303,
                Name = "지크",
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
                        Name = "부숴버려!",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 17 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 23 } }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "봉인해제!",
                        SkillType = SkillType.Skill2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 545,
                                HealAtkRatio = 25
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 655,
                                HealAtkRatio = 30
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 } } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "나만 믿어",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Atk_Rate = 23 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Atk_Rate = 27 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 에이스
            new Character
            {
                Id = 304,
                Name = "에이스",
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
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "달빛 베기",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70,
                                Bonus = new BuffSet { Arm_Pen = 40 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 85,
                                Bonus = new BuffSet { Arm_Pen = 40 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 56 } }
            }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "일도천화엽",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 140,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17, Blk_Red = 25 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 140,
                                Effect = "아군 디버프 해제",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17, Blk_Red = 33 } }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "불굴의 지휘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20, Heal_Reduction = 44 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24, Heal_Reduction = 44 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 64 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 엘리시아
            new Character
            {
                Id = 305,
                Name = "엘리시아",
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
                            { 1, new SkillLevelData { Ratio = 130 } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "천공의 빛",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 80, FixedDamage = 775,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 50 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 80, FixedDamage = 1285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 60 }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "별빛 낙하",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Effect = "턴제 감소 3턴으로 증가"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "혁명단의 인도자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "생명력 50% 되면 공격력 비례 힐"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 크리스
            new Character
            {
                Id = 306,
                Name = "크리스",
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
                            { 1, new SkillLevelData { Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 20 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "어둠의 일격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 305,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 50 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 370,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 60 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend{ Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Chance = 30 }
                } } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어둠의 속삭임",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "영혼 흡수",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "불사, 적 사망 시 공격력 비례 회복(아군)"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "불사 턴 증가, 적 사망 시 공격력 비례 회복(아군)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "불사 시 스킬 쿨초"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 제이브
            new Character
            {
                Id = 307,
                Name = "제이브",
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
                            { 0, new SkillLevelData { Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40 }
                                }
                            } },
                            { 1, new SkillLevelData { Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 50 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "분노의 일격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "용의 분노",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 55 }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "복수의 갑옷",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 125,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 1125                 
                                }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 125,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 1375                 
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "행동 제어 3턴"
                        }},
                        { 6, new PassiveTranscend { 
                            Effect = "화상 용염으로 변경(추후), 반격도 추후"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 트루드
            new Character
            {
                Id = 308,
                Name = "트루드",
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
                            { 0, new SkillLevelData { Ratio = 100
                             } },
                            { 1, new SkillLevelData { Ratio = 120, HealAtkRatio = 20
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "한 방이면 끝!",
                        SkillType = SkillType.Skill1,
                        TargetCount = 4,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 125
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 150, Effect="행동제어면역3턴"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "놀아 볼까!",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Ratio = 58,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 28 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Ratio = 68,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 37 } }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "전투의 희열",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 50 } },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.PainEndurance,
                                    PainEndurance = new PainEndurance
                                    {
                                        Threshold = 10,       // 10%
                                        ReductionRate = 75,   // 75%
                                        Duration = 5          // 5턴
                                    }
                                }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 60 } },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.PainEndurance,
                                    PainEndurance = new PainEndurance
                                    {
                                        Threshold = 10,       // 10%
                                        ReductionRate = 75,   // 75%
                                        Duration = 5          // 5턴
                                    }
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 100 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            
            // 스파이크
            new Character
            {
                Id = 309,
                Name = "스파이크",
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
                            { 0, new SkillLevelData { Ratio = 50, HpRatio = 12
                             } },
                            { 1, new SkillLevelData { Ratio = 65, HpRatio = 15
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "혹한의 일격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 195, HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 195, HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ HealHpRatio = 40 }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "혹한의 지진",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 32, HpRatio = 8,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 40 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 40, HpRatio = 9,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 50 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ HealHpRatio = 40 }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "혹한의 심장",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Hp,      
                                    TargetStat = StatType.Atk,      
                                    PerUnit = 120,                  
                                    SourceUnit = 1000,                 
                                    MaxValue = 1080                 
                                }
                            },
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28, Eff_Res = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Hp,      
                                    TargetStat = StatType.Atk,      
                                    PerUnit = 120,                  
                                    SourceUnit = 1000,                 
                                    MaxValue = 1080                 
                                }
                            },
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28, Eff_Res = 28 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "권능"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            
            // 챈슬러
            new Character
            {
                Id = 310,
                Name = "챈슬러",
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
                            { 0, new SkillLevelData { 
                                Ratio = 50, DefRatio = 60
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, DefRatio = 70
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "분쇄",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Ratio = 120, DefRatio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Ratio = 145, DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 44 } }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "대지 파괴",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 75, DefRatio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 85, DefRatio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 24 } }
            } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "위압감",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 11 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 13 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "피면 2턴"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            
            // 겔리두스
            new Character
            {
                Id = 311,
                Name = "겔리두스",
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
                            { 0, new SkillLevelData { 
                                Ratio = 50, DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 45 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, DefRatio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 50 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend
                            {
                                Effect = "빙극 대상 3명"
                            }
                            }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "패왕의 기억",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 65, DefRatio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 75, DefRatio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70 }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "창공의 패왕",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 30, DefRatio = 33,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 35, DefRatio = 38,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70 }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "얼음 여왕의 가호",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Def,      
                                    TargetStat = StatType.Blk,      
                                    PerUnit = 3,                  
                                    SourceUnit = 150,                 
                                    MaxValue = 27                
                                }
                            },
                            Effect = "n회 공격 시 피해 추후 추가 예정",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Def,      
                                    TargetStat = StatType.Blk,      
                                    PerUnit = 3,                  
                                    SourceUnit = 150,                 
                                    MaxValue = 33                
                                }
                            },
                            Effect = "n회 공격 시 피해 추후 추가 예정",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 6 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 팔라누스
            new Character
            {
                Id = 312,
                Name = "팔라누스",
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
                            { 0, new SkillLevelData { 
                                Ratio = 100, HealDmgRatio = 17,
                                Bonus = new BuffSet{ Arm_Pen = 65 }
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 100, HealDmgRatio = 17,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = "아군 방어형, 지원형 1명씩 있을 시 50%확률로 표식 1중첩"
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "강고한 검격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 57, HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 67, HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Wek = 100 } }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "작전명 : 섬멸",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 77, HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 90, HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Wek = 100 } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "지휘관의 저력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Dmg_Rdc,      
                                    PerUnit = 1.5,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 13.5                
                                }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Dmg_Rdc,      
                                    PerUnit = 1.5,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 13.5                
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Heal_Bonus = 28 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 관우
            new Character
            {
                Id = 313,
                Name = "관우",
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
                            { 0, new SkillLevelData { 
                                Ratio = 100
                                
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 100,
                                Effect = "회복불가 추가",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Unrecover = 1 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "청룡월파참",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 390 
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 470
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "진두지휘",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Effect = "아군 디버프 해제 2개, 해제한 디버프 1개당 지속 회복(2턴), 해제가능한 디버프가 있어야 사용가능"
                            }},
                            { 1, new SkillLevelData { 
                                Effect = "아군 디버프 해제 2개, 해제한 디버프 1개당 지속 회복(2턴), 해제가능한 디버프가 있어야 사용가능, 모든피해면역(1턴)"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "만인지적",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect="감전면역, 2턴",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect="감전면역, 물피증 3턴",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 40 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            #endregion

            #region 전설 - 만능형 - 마법 326~

            // 카르마
            new Character
            {
                Id = 326,
                Name = "카르마",
                Grade = "전설",
                Type = "만능형",
                AttackType = AttackType.Magic,
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
                            { 0, new SkillLevelData { Ratio = 50, DefRatio = 60
                             } },
                            { 1, new SkillLevelData { Ratio = 50, DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "과오의 화옥",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 25, DefRatio = 28, Bonus = new BuffSet{Arm_Pen = 40}
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 28, DefRatio = 33, Bonus = new BuffSet{Arm_Pen = 40}
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "절망의 폭풍",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 20, DefRatio = 23, HealDmgRatio = 31,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 39,
                                    Arm_Pen = 45,
                                    ConsumeCount = 4
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 23, DefRatio = 26, HealDmgRatio = 37,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 39,
                                    Arm_Pen = 45,
                                    ConsumeCount = 4
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ TargetCountOverride = 5 }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "타락한 신선",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 12, Dmg_Rdc = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 15, Dmg_Rdc = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 33 } }
            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 손오공
            new Character
            {
                Id = 327,
                Name = "손오공",
                Grade = "전설",
                Type = "만능형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "평타-분신",
                        SkillType = SkillType.Normal2,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                }
                            } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 50 } }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "대봉승타격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 53, HealAtkRatio = 35
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 62, HealAtkRatio = 35
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 4,
                        Name = "여의난참무",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 43, HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 11 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 43, HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 15 } }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 5,
                        Name = "환.대봉승타격",
                        SkillType = SkillType.Skill3,
                        TargetCount = 3,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 86, TargetMaxHpRatio = 6, AtkCap = 100, HealAtkRatio = 35
                            }},
                            { 1, new SkillLevelData { 
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 102, TargetMaxHpRatio = 7, AtkCap = 100, HealAtkRatio = 35
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 50 } }}
                        }
                    },
                    new Skill
                    {
                        Id = 6,
                        Name = "환.여의난참무",
                        SkillType = SkillType.Skill4,
                        TargetCount = 5,
                        Atk_Count = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 }, Ratio = 68, HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Blk_Red = 25 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 }, Ratio = 82, HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Blk_Red = 33 } }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 50 } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "미후분신술",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 125,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 1125                
                                }
                            },
                            Effect = "권능"
                        }},
                        { 1, new PassiveLevelData { 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Atk,      
                                    TargetStat = StatType.Def,      
                                    PerUnit = 125,                  
                                    SourceUnit = 300,                 
                                    MaxValue = 1125                
                                }
                            },
                            Effect = "권능"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effect = "체 50% 아군 힐"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 니아
            new Character
            {
                Id = 328,
                Name = "니아",
                Grade = "전설",
                Type = "만능형",
                AttackType = AttackType.Magic,
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                }
                            } },
                            { 1, new SkillLevelData { 
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35 }
                                }
                            } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "100만 볼트",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 40 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 50 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend
                            {
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, Chance = 10 }
                }
                            }
                            }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "시간 조작",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 115,
                                Effect = "쿨증, 턴감"
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 135,
                                Effect = "쿨증, 턴감"
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Effect = "쿨증 확률증" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "예비 전력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 49 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { 
                            Effect = "피면 2턴"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            #endregion

            #region 영웅 - 만능형 - 물리 351~

            

            #endregion

            #region 영웅 - 만능형 - 마법 376~

            // 빅토리아
            new Character
            {
                Id = 376,
                Name = "빅토리아",
                Grade = "희귀",
                Type = "만능형",
                AttackType = AttackType.Magic,
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
                                Ratio = 100
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 120
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "권총 사격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "사기 진작",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 29 } }
                                }
                            }},
                            { 1, new SkillLevelData {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 35 } }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Effect = "1턴증가" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "해적왕의 위엄",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "마비 면역"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "마비 면역, 스킬 사용 시 체력 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { 
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 22 } }
            },
                            Effect = "아군 후열"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 라니아

            #endregion

            #region 전설 - 방어형 - 물리 401~

            // 루디
            new Character
            {
                Id = 401,
                Name = "루디",
                Grade = "전설",
                Type = "방어형",
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
                                Ratio = 50, DefRatio = 60
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 65, DefRatio = 75
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "돌격",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 170, DefRatio = 195,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 205, DefRatio = 235,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100 }
                                }
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "방어 준비",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Effect = "링크"
                            }},
                            { 1, new SkillLevelData { 
                                Effect = "링크",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 10 } }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Effect = "cc면역" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "견고한 방패",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 20 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 24 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 아킬라
            new Character
            {
                Id = 402,
                Name = "아킬라",
                Grade = "전설",
                Type = "방어형",
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
                                Ratio = 50, HpRatio = 12
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, HpRatio = 14
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "잔혹한 폭풍",
                        SkillType = SkillType.Skill1,
                        TargetCount = 5,
                        Atk_Count = 2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 15, DefRatio = 4,
                                Effect = "상대 약확 22% 감소, 즉사 턴감"
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 15, DefRatio = 4,
                                Effect = "상대 약확 30% 감소, 즉사 턴감"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "칠흑의 장막",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 35, HpRatio = 9,
                                Effect = "링크",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 35, HpRatio = 9,
                                Effect = "링크",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 10 } }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Effect = "cc면역" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "어둠의 인도자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 34 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {6, new PassiveTranscend{ Effect = "적군 사망 시 10초쿨감" }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 녹스
            new Character
            {
                Id = 403,
                Name = "녹스",
                Grade = "전설",
                Type = "방어형",
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
                                Ratio = 50, HpRatio = 12
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, HpRatio = 14
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "지옥의 방패",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                HealHpRatio = 15,
                                Effect = "도발, 피면, 아군 지속 힐"
                            }},
                            { 1, new SkillLevelData { 
                                HealHpRatio = 20,
                                Effect = "도발, 피면, 아군 지속 힐"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "지옥의 일격",
                        SkillType = SkillType.Skill2,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 80, HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 37 } }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 80, HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 37 } }
                                }
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "지옥의 기사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "아군 즉사 효과 적용 확률 증가"
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "아군 즉사 효과 적용 확률 증가"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 34 } }
            }}},
                        {6, new PassiveTranscend{ Effect = "" }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 아라곤
            new Character
            {
                Id = 404,
                Name = "아라곤",
                Grade = "전설",
                Type = "방어형",
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
                                Ratio = 50, DefRatio = 60
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 50, DefRatio = 60,
                                Effect = "평타 시 9초쿨감"
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "포격 지원",
                        SkillType = SkillType.Skill1,
                        TargetCount = 3,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 110, DefRatio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60 }
                                }
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 130, DefRatio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60 }
                                }
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Cri_Dmg_Reduction = 40 } }
            } }},
                            {6, new SkillTranscend{ Effect = "턴감" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "노장의 위엄",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "피격 시 25% 확률 힐",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 13 } }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "피격 시 25% 확률 힐 증가",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 13 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 룩
            new Character
            {
                Id = 405,
                Name = "룩",
                Grade = "전설",
                Type = "방어형",
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
                                Ratio = 50, HpRatio = 12
                             } },
                            { 1, new SkillLevelData { 
                                Ratio = 60, HpRatio = 14
                             } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "투창",
                        SkillType = SkillType.Skill1,
                        TargetCount = 1,
                        Atk_Count = 1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Ratio = 315, HpRatio = 75
                            }},
                            { 1, new SkillLevelData { 
                                Ratio = 380, HpRatio = 91
                            }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "진형 굳히기",
                        SkillType = SkillType.Skill2,
                        TargetCount = 5,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                Effect = "파티 보호막"
                            }},
                            { 1, new SkillLevelData { 
                                Effect = "파티 보호막"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "공성 방패",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 24 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 24 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 23 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {6, new PassiveTranscend{ 
                            StatScalings = new List<StatScaling>
                            {
                                new StatScaling
                                {
                                    SourceStat = StatType.Hp,      
                                    TargetStat = StatType.Blk,      
                                    PerUnit = 1,                  
                                    SourceUnit = 550,                 
                                    MaxValue = 20                
                                }
                            }
                         }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            #endregion

            #region 전설 - 방어형 - 마법 426~

            #endregion
        
            #region 영웅 - 방어형 -물리 451~
    
            #endregion

            #region 영웅 - 방어형 - 마법 476~
    
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

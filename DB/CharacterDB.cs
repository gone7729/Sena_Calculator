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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                ConditionalExtraDmg = 45,
                                Effect = "대상 현재 생명력 30% 이하일 경우 공격력45% 추가피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                ConditionalExtraDmg = 55,
                                Effect = "대상 현재 생명력 30% 이하일 경우 공격력55% 추가피해"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "바람의 칼날",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { 
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 145, 
                                Bonus = new BuffSet { Cri_Dmg = 37 }, 
                                Effect = "치피 37% 추가 적용" 
                                } },
                            { 1, new SkillLevelData { 
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 170, 
                                Bonus = new BuffSet { Cri_Dmg = 46 }, 
                                Effect = "치피 46% 추가 적용" 
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 무도",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                ConditionalExtraDmg = 260,
                                ConditionalDesc = "체력 30% 미만",
                                Effect = "체력 30% 미만일 경우 추가 피해 260%"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 170,
                                ConditionalExtraDmg = 260,
                                ConditionalDesc = "체력 30% 미만",
                                Effect = "체력 30% 미만일 경우 추가 피해 260%"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 }, 
                                Effect = "방어력 40% 무시" 
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 발톱",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            MaxStacks = 8,
                            Effect = "공격 당한 적군 - 모든 공격 2회 시 2스택 부여",
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
                                    Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 3 }, 
                                }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            MaxStacks = 8,
                            Effect = "공격 당한 적군 - 모든 공격 2회 시 2스택 부여[상시], 자신의 스킬 1회 발동 시 모든 피해 무효화[피격 1회]",
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
                                    Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 3 }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SkillOnly,
                                    TriggerCount = 1,
                                    DamageNullification = new DamageNullification { HitCount = 1, Type = DamageNullType.All }
                                }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "공격 당한 적군 - 초월 시 스택 당 물리 취약 4%[상시]",
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
                                    Debuff = new DebuffSet { Vulnerability = 4 } 
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = "자신과 공격력이 가장 높은 아군 스킬 쿨타임 8초 감소",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.SelfAndHighestAtkAlly, Type = SkillEffectType.Buff, Buff = new BuffSet { Cooldown_Reduction = 8 } }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "자신과 공격력이 가장 높은 아군 스킬 쿨타임 9초 감소",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.SelfAndHighestAtkAlly, Type = SkillEffectType.Buff, Buff = new BuffSet { Cooldown_Reduction = 9 } }
                                }
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "강자 사냥",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 130,
                                Effect = "실명(100%)[2턴], 자신 - 도발(100%)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 155,
                                Effect = "실명(100%)[2턴], 자신 - 도발(100%)[2턴], 물리 피해 면역[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Physical } }
                                }
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 }, 
                                Effect = "방어력 40% 무시" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "광풍참",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 105,
                                Ratio = 145,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Bonus = new BuffSet { WekBonusDmg = 230 },
                                Effect = "잃은 체력 비례 최대 50% 피해량 증가, 약점 공격 시 물공 230%만큼 추가 피해(대상 잃체비 최대 50% 피해량 증가)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 170,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Bonus = new BuffSet { WekBonusDmg = 270 },
                                Effect = "잃은 체력 비례 최대 50% 피해량 증가, 약점 공격 시 물공 230%만큼 추가 피해(대상 잃체비 최대 50% 피해량 증가)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                Bonus = new BuffSet { Arm_Pen = 40 }, 
                                Effect = "방어력 40% 무시" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "쾌속의 마검사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "모든 아군 3인 공격기 피해량 25% 증가[상시], 아군 화상 면역[2턴], 자신의 기본 공격 1회 발동 시 아군 화상 면역[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 25 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.NormalOnly, TriggerCount = 1, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "모든 아군 3인 공격기 피해량 31% 증가[상시], 아군 화상 면역[2턴], 자신의 기본 공격 1회 발동 시 아군 화상 면역[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 31 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.NormalOnly, TriggerCount = 1, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "침묵(45%)[1턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Chance = 45, Duration = 1 }
                                }
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "사신강림",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 115,
                                Effect = "침묵(50%)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Chance = 50, Duration = 2 }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 135,
                                Effect = "침묵(60%)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Chance = 60, Duration = 2 }
                                }
                                } }
                        },
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 일격",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 5,
                                Cooldown = 108,
                                Ratio = 119,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 5,
                                Cooldown = 108,
                                Ratio = 158,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                OnKillRecast = new OnKillRecast { 
                                    RatioPercent = 100 } 
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "죽음의 경계",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신 - 모든 피해 무효화[피격 3회], 아군 - 물피증 17%[상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Party, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Dmg_Dealt_Type = 17 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 - 모든 피해 무효화[피격 4회], 아군 - 물피증 20%[상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Party, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 4, Type = DamageNullType.All } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 6, new PassiveTranscend { 
                                Effect = "자신 - 아군 사망 시 모든 피해 무효화 [피격 1회], 아군 사망 시 물리 공격력 증가 39%[2턴]",
                                Effects = new List<PersistentEffect>
                                {
                                    new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllyDeath, TriggerCount = 1, DamageNullification = new DamageNullification { HitCount = 1, Type = DamageNullType.All } },
                                    new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllyDeath, Buff = new BuffSet { Atk_Rate = 39 } }
                                } } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "귀신 베기",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 38,
                                Ratio = 570,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 38,
                                Ratio = 690,
                                Effect = ""
                                } }
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
                            Effect = "자신 -마법 감쇄24%[상시], 치확 증가35%[상시], 자신의 모든 공격 3회 발동 시 물공 100% 1회 피해",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Mag_Dmg_Rdc = 24, Cri = 35 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage, TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 3, TriggerOn = TriggerCondition.AllAttack, AtkRatio = 100, TargetCount = 1, HitCount = 1 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 - 마법 감쇄29%[상시], 치확 증가41%[상시], 자신의 모든 공격 3회 발동 시 물공 100% 1회 피해",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Mag_Dmg_Rdc = 29, Cri = 41 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage, TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 3, TriggerOn = TriggerCondition.AllAttack, AtkRatio = 100, TargetCount = 1, HitCount = 1 } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 55,
                                Effect = "출혈(35%)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Chance = 35, Duration = 2 }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 65,
                                Effect = "출혈(40%)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Chance = 40, Duration = 2 }
                                }
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "거궁신포",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 72,
                                Effect = "각 공격마다 출혈(55%)[3턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.StatusAilment, 
                                        StatusType = StatusEffectType.Bleeding, 
                                        Stacks = 1, 
                                        Chance = 55 }
                                },
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 85,
                                Effect = "각 공격마다 출혈(60%)[3턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.StatusAilment, 
                                        StatusType = StatusEffectType.Bleeding, 
                                        Stacks = 1, 
                                        Chance = 60 }
                                },
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { 
                                Cooldown = 55,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Chance = 100, Duration = 3, RequiresStatusType = StatusEffectType.Bleeding }
                                },
                                Effect = "쿨 55초 변경, 상대 출혈 시 출혈(100%)[3턴]" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "태전포화",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 175,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.StatusAilment, 
                                        StatusType = StatusEffectType.BleedExplosion }
                                },
                                Effect = "대상에게 적용된 출혈 효과를 폭발시켜 피해를 입힙니다. 출혈 효과의 개수와 남은 턴에 비례하여 피해를 입히며 효과는 소모됩니다."
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 205,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BleedExplosion }
                                },
                                Effect = "대상에게 적용된 출혈 효과를 폭발시켜 피해를 입힙니다. 출혈 효과의 개수와 남은 턴에 비례하여 피해를 입히며 효과는 소모됩니다."
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.StatusAilment, 
                                        StatusType = StatusEffectType.BleedExplosion, 
                                        CustomAtkRatio = 150 }
                                },
                                Effect = "출혈폭발 대미지 120% -> 150%"
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
                            Effect = "자신 - 자신의 스킬 1회 발동 시 자신에게 물피증 29%(3턴), 모든 공격 3회 발동 후 시전자 물공 50%만큼 생명력 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, Buff = new BuffSet { Dmg_Dealt_Type = 29 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllAttack, TriggerCount = 3, TriggeredHealAtkRatio = 50 }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 - 자신의 스킬 1회 발동 시 자신에게 물피증 35%(3턴), 모든 공격 3회 발동 후 시전자 물공 55%만큼 생명력 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, Buff = new BuffSet { Dmg_Dealt_Type = 35 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllAttack, TriggerCount = 3, TriggeredHealAtkRatio = 55 }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "해방-팔사검",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 62,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.Debuff, 
                                        Debuff = new DebuffSet { Heal_Reduction = 52 } }
                                },
                                Effect = "받는회복량감소52%[3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 62,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 68 } }
                                },
                                Effect = "받는회복량감소68%[3턴]"
                                } }
                        },
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "해방-뱀 사냥",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 57,
                                Effect = "석화(45%)[2턴]",
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 57,
                                Effect = "석화(55%)[2턴]"
                                } }
                        },
                    }
                },
                Passive = new Passive
                {
                    Name = "팔사의 저주",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신 - 사망시 불굴(전투당 1회)[피격 8회], 아군(공격형,만능형) - 치피증28%, 아군(공격형,만능형) 불굴 발동 시 디버프 해제(2개) 및 물피증20%(3턴)",
                            Effects = new List<PersistentEffect>
                            {
                                // 아군(공격형,만능형) 치피증 28%
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.Buff,
                                    Buff = new BuffSet { Cri_Dmg = 28 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    IsConditional = true,
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } },
                                // 불굴: 사망 시 생명력 1로 부활, 피격 8회까지 사망 무효 (전투당 1회). 적군 사망 시 잔여 피격 횟수 +1 (8 상한)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival {
                                        HitCount = 8, ReviveHp = 1,
                                        OncePerBattle = true,
                                        HitCountGainOnEnemyDeath = 1,
                                        MaxHitCount = 8 } },
                                // 불굴 발동 시 아군(공격형,만능형): 디버프 2개 해제
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.DebuffCleanse,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    DispelDebuffCount = 2 },
                                // 불굴 발동 시 아군(공격형,만능형): 물피증 20% [3턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    Duration = 3,
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 - 사망시 불굴(전투당 1회)[피격 8회], 아군(공격형,만능형) - 치피증34%, 아군(공격형,만능형) 불굴 발동 시 디버프 해제(2개) 및 물피증20%(3턴)",
                            Effects = new List<PersistentEffect>
                            {
                                // 아군(공격형,만능형) 치피증 34%
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.Buff,
                                    Buff = new BuffSet { Cri_Dmg = 34 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    IsConditional = true,
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } },
                                // 불굴: 사망 시 생명력 1로 부활, 피격 8회까지 사망 무효 (전투당 1회). 적군 사망 시 잔여 피격 횟수 +1 (8 상한)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival {
                                        HitCount = 8, ReviveHp = 1,
                                        OncePerBattle = true,
                                        HitCountGainOnEnemyDeath = 1,
                                        MaxHitCount = 8 } },
                                // 불굴 발동 시 아군(공격형,만능형): 디버프 2개 해제
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.DebuffCleanse,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    DispelDebuffCount = 2 },
                                // 불굴 발동 시 아군(공격형,만능형): 물피증 20% [3턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    Duration = 3,
                                    Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "아군(공격형, 만능형) - 불굴 발동 시 시전자 물공 65%만큼 피회복",
                            Effects = new List<PersistentEffect>
                            {
                                // 불굴 발동 시 아군(공격형,만능형): 시전자 물공 65% 비례 피회복
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetClasses = new[] { "공격형", "만능형" },
                                    Type = PersistentEffectType.TriggeredHeal,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    TriggeredHealAtkRatio = 65 }
                            }
                        } },
                        { 6, new PassiveTranscend {
                            Effect = "스킬 1회 발동 시 물리 취약24%[2턴] 모든 적군",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.AllEnemies, Type = PersistentEffectType.Debuff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, Chance = 100, Duration = 2, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 24 } }
                            }
                        } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "관-통-데미지!",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 150,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 170,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "까악-까악-",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 77,
                                Effect = "대상 턴제 버프 감소(100% 확률)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 90,
                                Effect = "대상 턴제 버프 감소(100% 확률)[2턴]",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                }
                                } }
                        },
                    }
                },
                Passive = new Passive
                {
                    Name = "까마귀 눈동자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData { 
                            Effect = "자신 - 피해면역[피격 3회], 사망 시 불사 상태로 부활[2턴](전투당1회), 치명타확률 증가(27%)[상시], 적군 4명 사망 시 2스킬 발동 -> 대상 턴제 버프 감소[2턴], Ratio=77%, AtkCount=2 해당 스킬은 본래의 스킬과 별개 취급(라운드당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                // 치명타확률 증가 27% [상시]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    Buff = new BuffSet { Cri = 27 } },
                                // 피해면역[피격 3회]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    DamageNullification = new DamageNullification { HitCount = 3 } },
                                // 사망 시 불사[2턴]로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 2, OncePerBattle = true } },
                                // 적군 4명 사망 시 2스킬(까악-까악-) 발동: 대상 턴제 버프 감소[2턴]. 본래 스킬과 별개(쿨 미공유), 라운드당 1회
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.TriggeredSkillCast,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.EnemyDeath,
                                    TriggeredSkillCast = new TriggeredSkillCast {
                                        TriggerOn = TriggerCondition.EnemyDeath, TriggerCount = 4, OncePerRound = true,
                                        Ratio = 77, AtkCount = 2, TargetCount = 5,
                                        Effects = new List<SkillEffect>
                                        {
                                            new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                        } } }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "자신 - 피해면역[피격 3회], 사망 시 불사 상태로 부활[2턴](전투당1회), 치명타확률 증가(27%)[상시], 적군 4명 사망 시 2스킬 발동 -> 대상 턴제 버프 감소[2턴], Ratio=90%, AtkCount=2 해당 스킬은 본래의 스킬과 별개 취급(라운드당 1회) 불사 발동 시 물공증(39%)[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                // 치명타확률 증가 27% [상시]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    Buff = new BuffSet { Cri = 27 } },
                                // 피해면역[피격 3회]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    DamageNullification = new DamageNullification { HitCount = 3 } },
                                // 사망 시 불사[2턴]로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 2, OncePerBattle = true } },
                                // 불사 발동 시 물공증 39% [2턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    Duration = 2,
                                    Buff = new BuffSet { Atk_Rate = 39 } },
                                // 적군 4명 사망 시 2스킬(까악-까악-) 발동: 대상 턴제 버프 감소[2턴]. 본래 스킬과 별개(쿨 미공유), 라운드당 1회
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.TriggeredSkillCast,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.EnemyDeath,
                                    TriggeredSkillCast = new TriggeredSkillCast {
                                        TriggerOn = TriggerCondition.EnemyDeath, TriggerCount = 4, OncePerRound = true,
                                        Ratio = 90, AtkCount = 2, TargetCount = 5,
                                        Effects = new List<SkillEffect>
                                        {
                                            new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                        } } }
                            },
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "자신 - 불사 발동 시 스킬 쿨타임 초기화",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.CooldownReset,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    CooldownReset = new CooldownReset { AllSkills = true } }
                            }
                        } },
                        { 6, new PassiveTranscend { 
                            Effect = "자신 - 치명타 확률 증가100%[상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Cri = 100 } },
                            },
                             } },
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "먼저쓰면안됨",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Ratio = 115,
                                ConditionalExtraDmg = 100,
                                Effect = "관통, 대상 현재 생명력이 50% 이하일 시 물공 100%만큼 추가 관통 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 76,
                                Ratio = 150,
                                ConditionalExtraDmg = 100,
                                Effect = "관통, 대상 현재 생명력이 50% 이하일 시 물공 100%만큼 추가 관통 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { 
                                ConditionalExtraDmg = 115,
                                Effect = "관통, 대상 현재 생명력이 50% 이하일 시 물공 115%만큼 추가 관통 피해" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "먼저쓰셈",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Ratio = 125,
                                ConditionalExtraDmg = 115,
                                Effect = "관통, 대상 현재 생명력이 50% 이상일 시 물공 115%만큼 추가 관통 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Ratio = 165,
                                ConditionalExtraDmg = 115,
                                Effect = "관통, 대상 현재 생명력이 50% 이상일 시 물공 115%만큼 추가 관통 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { ConditionalExtraDmg = 135, 
                            Effect = "관통, 대상 현재 생명력이 50% 이상일 시 물공 135%만큼 추가 관통 피해" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "호랑이의 용맹",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "아군 - 물공증27% [상시], 자신 - 모든 피해 면역[2턴], 적 사망 시 시전자 물공30%만큼 생명력 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Atk_Rate = 27 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "아군 - 물공증33% [상시], 자신 - 모든 피해 면역[2턴], 적 사망 시 시전자 물공30%만큼 생명력 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Atk_Rate = 33 } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend { 
                                Effects = new List<PersistentEffect> {
                                    new PersistentEffect { 
                                        Target = EffectTarget.Self, 
                                        Type = PersistentEffectType.Buff, 
                                        IsConditional = true, 
                                        Buff = new BuffSet { Dmg_Dealt_Type = 33 } }
                            }, 
                            Effect = "자신 - 물피증33%[3턴]" } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = "자신의 기본 공격 2회 발동 시 물공130%만큼 1회 추가 공격, (강자주시)추가 공격 시 버프해제 1개(30%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = "자신의 기본 공격 2회 발동 시 물공130%만큼 1회 추가 공격, (강자주시)추가 공격 시 버프해제 1개(30%)"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "철쇄폭압",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 90,
                                Ratio = 60,
                                TargetMaxHpRatio = 8,
                                AtkCap = 75,
                                LostHpBonusDmgMax = 50,
                                ConditionalExtraDmg = 65,
                                Effect = "대상 최대 생명력8%만큼 2회 피해, 대상이  잃은 생명력에 비례해 최대 50% 피해증가, 생명력 비례 피해는 시전자 공75%제한, 단일 적군(강자주시) 물공65%만큼 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 90,
                                Ratio = 72,
                                TargetMaxHpRatio = 10,
                                AtkCap = 75,
                                LostHpBonusDmgMax = 50,
                                ConditionalExtraDmg = 65,
                                Effect = "대상 최대 생명력10%만큼 2회 피해, 대상이  잃은 생명력에 비례해 최대 50% 피해증가, 생명력 비례 피해는 시전자 공75%제한, 단일 적군(강자주시) 물공65%만큼 추가 피해"
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 94,
                                Ratio = 70,
                                IgnoresTurnDamageImmunity = true,
                                ConditionalExtraDmg = 65,
                                Effect = "버프해제(100%)[2개], 관통, 단일적군(강자주시) 물공65%만큼 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 94,
                                Ratio = 90,
                                IgnoresTurnDamageImmunity = true,
                                ConditionalExtraDmg = 65,
                                Effect = "버프해제(100%)[2개], 관통, 단일적군(강자주시) 물공65%만큼 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                }
                                } }
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
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Cri = 27 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.StatusAilment,
                                    StatusType = StatusEffectType.ChainDamage },
                                // 강자주시: 공격력 최고 적군 고정 타게팅 (라운드 고정, 대상 사망 시 비전이)
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.FocusTarget,
                                    FocusTarget = new FocusTarget { Selector = FocusTargetSelector.HighestAtkEnemy } },
                                // 기본공격 2회마다 물공 130% 추가공격, 추가공격 시 버프해제 1개(30%)
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 2, TriggerOn = TriggerCondition.NormalOnly, AtkRatio = 130, TargetCount = 1, DispelBuffCount = 1, DispelBuffChance = 30 } },
                                // 전투 시작 시 아군 공격형 3명 이상이면 모든 피해 무효화[피격 3회]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    IsConditional = true,
                                    Condition = "공격형 3명 이상 편성",
                                    DamageNullification = new DamageNullification { HitCount = 3 } }
                            },
                            Effect = "자신 - 치명타확률 증가27%[상시], 전투 시작 시 아군에 공격형 영웅이 3명 이상일 시, 모든 피해 무효화[피격3회], 적군 - 강자주시(공격력이 가장 높은 적군) 카일이 대상을 주시해 우선 공격, 추가로 1스킬, 2스킬 사용 시 대상에겐 추가 피해, 강자주시는 각 라운드에서 한 대상에게만 고정(강자주시된 대상 사망 시 다른 대상은 강자주시가 적용되지 않음), 모든 적군 - 자신의 모든 공격 2회 발동 시 대상 최대 생명력의 23%만큼 1회 방어 무시 피해(시전자 공100%cap)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { 
                                    Target = EffectTarget.Self, 
                                    Type = PersistentEffectType.Buff, 
                                    Buff = new BuffSet { Cri = 27 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.StatusAilment,
                                    StatusType = StatusEffectType.ChainDamage,
                                    CustomTargetMaxHpRatio = 32 },
                                // 강자주시: 공격력 최고 적군 고정 타게팅 (라운드 고정, 대상 사망 시 비전이)
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.FocusTarget,
                                    FocusTarget = new FocusTarget { Selector = FocusTargetSelector.HighestAtkEnemy } },
                                // 기본공격 2회마다 물공 130% 추가공격, 추가공격 시 버프해제 1개(30%)
                                new PersistentEffect {
                                    Target = EffectTarget.Enemy,
                                    Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 2, TriggerOn = TriggerCondition.NormalOnly, AtkRatio = 130, TargetCount = 1, DispelBuffCount = 1, DispelBuffChance = 30 } },
                                // 전투 시작 시 아군 공격형 3명 이상이면 모든 피해 무효화[피격 4회]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    IsConditional = true,
                                    Condition = "공격형 3명 이상 편성",
                                    DamageNullification = new DamageNullification { HitCount = 4 } }
                            },
                            Effect = "자신 - 치명타확률 증가27%[상시], 전투 시작 시 아군에 공격형 영웅이 3명 이상일 시, 모든 피해 무효화[피격4회], 적군 - 강자주시(공격력이 가장 높은 적군) 카일이 대상을 주시해 우선 공격, 추가로 1스킬, 2스킬 사용 시 대상에겐 추가 피해, 강자주시는 각 라운드에서 한 대상에게만 고정(강자주시된 대상 사망 시 다른 대상은 강자주시가 적용되지 않음), 모든 적군 - 자신의 모든 공격 2회 발동 시 대상 최대 생명력의 32%만큼 1회 방어 무시 피해(시전자 공100%cap)"
                        
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend {
                                Effect = "자신 - 생명력 70% 이하가 되면 모든피해무효화[피격3회](전투당 1회)(공격형 3명 이상 편성 시)",
                                Effects = new List<PersistentEffect>
                                {
                                    new PersistentEffect {
                                        Target = EffectTarget.Self,
                                        Type = PersistentEffectType.DamageNullification,
                                        ApplyMode = ApplyMode.Triggered,
                                        TriggerCondition = TriggerCondition.OnHpBelow,
                                        TriggerHpThreshold = 70,
                                        OncePerBattle = true,
                                        IsConditional = true,
                                        Condition = "공격형 3명 이상 편성",
                                        DamageNullification = new DamageNullification { HitCount = 3 } }
                                }
                            } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 55,
                                Effect = "두명공격"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 70,
                                Effect = "두명공격"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "빠르고, 멋있게",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 72,
                                Ratio = 40,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 35 }
                                },
                                Effect = "공격마다 폭탄(35%)[3턴] "
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 72,
                                Ratio = 50,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 45 }
                                },
                                Effect = "공격마다 폭탄(45%)[3턴] "
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어때, 화려하지?",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 125,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BombDetonation },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 13 } }
                                },
                                Effect = "폭탄(50%)[3턴], 150%위력으로 폭탄 폭파[최대3개], 모든 공격력 감소13%(100%)[3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bomb, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.BombDetonation },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 18 } }
                                },
                                Effect = "폭탄(50%)[3턴], 150%위력으로 폭탄 폭파[최대3개], 모든 공격력 감소18%(100%)[3턴]"
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
                            Effect = "자신 - 위장[2턴], 속공 비례 공격력 증가[상시](최대 증가 수치 1080), 아군 - 효과적중증가28%[상시]",
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
                                // 자신 위장[2턴] (1인 공격 비대상 + 확정 빗나감)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData { 
                            Effect = "자신 - 위장[2턴], 속공 비례 공격력 증가[상시](최대 증가 수치 1320), 아군 - 34%[상시]",
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
                                // 자신 위장[2턴] (1인 공격 비대상 + 확정 빗나감)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34 } }
                            }
                        }}
                    },
                        TranscendBonuses = new Dictionary<int, PassiveTranscend>
                        {
                            { 2, new PassiveTranscend {
                                Effect = "자신 - 권능(전투당 1회 발동) 현재 생명력 이상의 피해를 입었을 때 생명력 1로 1회 생존, 권능 발동 시 시전자 물공 155%만큼 보호막[3턴]",
                                Effects = new List<PersistentEffect>
                                {
                                    new PersistentEffect {
                                        Target = EffectTarget.Self,
                                        Type = PersistentEffectType.Authority,
                                        Authority = new Authority { ReviveHp = 1, OncePerBattle = true, ShieldAtkRatio = 155, ShieldDuration = 3 } }
                                }
                            } },
                            { 6, new PassiveTranscend {
                                Effect = "적군 사망 후 스킬 쿨타임 감소 23초",
                                Effects = new List<PersistentEffect>
                                {
                                    new PersistentEffect {
                                        Target = EffectTarget.Self,
                                        Type = PersistentEffectType.CooldownReset,
                                        ApplyMode = ApplyMode.Triggered,
                                        TriggerCondition = TriggerCondition.EnemyDeath,
                                        CooldownReset = new CooldownReset { AllSkills = true, ReduceSeconds = 23 } }
                                }
                            } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "통행금지",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Shield_AtkRatio = 55 } },
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.StatusAilment, 
                                        StatusType = StatusEffectType.Concussion, Stacks = 1, Chance = 60 }
                                },
                                Effect = "자신 - 시전자 물공55%만큼 보호막[3턴], 진탕(60%)[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Shield_AtkRatio = 55 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Concussion, Stacks = 1, Chance = 60 }
                                },
                                Effect = "자신 - 시전자 물공55%만큼 보호막[3턴], 진탕(60%)[2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "위기대응",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 45,
                                Ratio = 135,
                                TargetSelector = FocusTargetSelector.HighestDefEnemy,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 2, Stacks = 1, Chance = 100 }
                                },
                                Effect = "방어력이 가장 높은 적군 대상"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 45,
                                Ratio = 165,
                                TargetSelector = FocusTargetSelector.HighestDefEnemy,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 2, Stacks = 1, Chance = 100 }
                                },
                                Effect = "방어력이 가장 높은 적군 대상"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { TargetCountOverride = 2, Effect= "대상 2명" } },
                            { 6, new SkillTranscend {
                                Effect = "생명력전환 55% 추가",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, CustomHpConversionRatio = 55 }
                                }
                            } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "첨단 나노 슈트",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신 - 효과적중증가40%[상시], 사망 시 생명력 55% 부활(전투당 1회), 아군(자신 제외) - 자신 사망 시 시전자 물공90% 보호막[2턴](전투당 2회), 자신 사망 시 디버프해제[3개](전투당 2회) ",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40 } },
                                // 사망 시 생명력 55% 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 55, OncePerBattle = true } },
                                // 자신 사망 시 아군(자신 제외) 보호막 시전자 물공 90%[2턴] (전투당 2회)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Duration = 2, MaxTriggersPerBattle = 2, Condition = "자신 제외", Buff = new BuffSet { Shield_AtkRatio = 90 } },
                                // 자신 사망 시 아군(자신 제외) 디버프 해제 3개 (전투당 2회)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.DebuffCleanse, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, MaxTriggersPerBattle = 2, Condition = "자신 제외", DispelDebuffCount = 3 }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 - 효과적중증가49%[상시], 사망 시 생명력 55% 부활(전투당 1회), 아군(자신 제외) - 자신 사망 시 시전자 물공90% 보호막[2턴](전투당 2회), 자신 사망 시 디버프해제[3개](전투당 2회) ",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 49 } },
                                // 사망 시 생명력 55% 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 55, OncePerBattle = true } },
                                // 자신 사망 시 아군(자신 제외) 보호막 시전자 물공 90%[2턴] (전투당 2회)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Duration = 2, MaxTriggersPerBattle = 2, Condition = "자신 제외", Buff = new BuffSet { Shield_AtkRatio = 90 } },
                                // 자신 사망 시 아군(자신 제외) 디버프 해제 3개 (전투당 2회)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.DebuffCleanse, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, MaxTriggersPerBattle = 2, Condition = "자신 제외", DispelDebuffCount = 3 }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "죽음의 그림자",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 60,
                                Ratio = 95,
                                IgnoresTurnDamageImmunity = true,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effect = "관통, 처치 시 100% 위력으로 재발동(재발동으로는 재발동 트리거x)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 60,
                                Ratio = 110,
                                IgnoresTurnDamageImmunity = true,
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
                                },
                                Effect = "관통, 처치 시 100% 위력으로 재발동(재발동으로는 재발동 트리거x), 자신 - 협공확률28%[3턴]"
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
                                // 자신 위장 (1인 공격 비대상 + 확정 빗나감)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise },
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
                            Effect = "자신 - 위장, 협공25%, 적군 - 협공 발동 시 단일 적군 물공100% 1회 피해, 자신의 모든 공격 3회 발동 시 1285만큼 1회 고정 피해"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 자신 위장 (1인 공격 비대상 + 확정 빗나감)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise },
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
                            Effect = "자신 - 위장, 협공30%, 적군 - 협공 발동 시 단일 적군 물공100% 1회 피해, 자신의 모든 공격 3회 발동 시 1285 1회 고정 피해"
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
                            },
                            Effect = "공격 3회 시 고정 관통 피해 1485로 상승"
                        } },
                        { 6, new PassiveTranscend {
                            Effect = "자신 - 2회 공격 시 모든 피해 무효 1회",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,
                                    DamageNullification = new DamageNullification { HitCount = 1 } }
                            }
                        } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 80,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "혈풍벽파",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 57,
                                Bonus = new BuffSet { WekBonusDmg = 25, WekBonusDmgPerHit = true },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 67,
                                Bonus = new BuffSet { WekBonusDmg = 25, WekBonusDmgPerHit = true },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 55 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 112,
                                Ratio = 52,
                                Bonus = new BuffSet { Arm_Pen = 40, WekBonusDmg = 27, WekBonusDmgPerHit = true },
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effect = "방어 무시, 직접 피해로 처치 시 100% 위력 연속 발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 112,
                                Ratio = 60,
                                Bonus = new BuffSet { Arm_Pen = 40, WekBonusDmg = 32, WekBonusDmgPerHit = true },
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effect = "방어 무시, 직접 피해로 처치 시 100% 위력 연속 발동"
                                } }
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
                                // 용이무모: 물리 공격력 31% 증가, 마법 공격력 90% 감소 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 31, MagicAtk_Rate = -90 } },
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
                                },
                                // 사망 시 생명력 80%로 부활 (전투당 1회) + 부활 발동 시 스킬 쿨타임 초기화
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.CooldownReset,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    CooldownReset = new CooldownReset { AllSkills = true } }
                            },
                            Effect = "축복 40%, 표식:방천화극의 분노"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 용이무모: 물리 공격력 31% 증가, 마법 공격력 90% 감소 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 31, MagicAtk_Rate = -90 } },
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
                                },
                                // 사망 시 생명력 100%로 부활 (전투당 1회) + 부활 발동 시 스킬 쿨타임 초기화
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.CooldownReset,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    CooldownReset = new CooldownReset { AllSkills = true } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "섬광십무",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 81,
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effect = "방어 무시 (방어력 40% 무시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 81,
                                Ratio = 410,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effect = "방어 무시 (방어력 40% 무시)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ HealAtkRatio = 30, Effect = "모든 아군 공격력 30% 비례 회복" }},
                            // 6초월: 방어 무시 효율 증가 65% (방어 무시 효율 증가, 모델엔 추가 Arm_Pen으로 근사)
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Arm_Pen = 25 }, Effect = "방어 무시 효율 증가 65%" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "쌍절태풍격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 475,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effect = "단일 대상, 방어 무시 (방어력 40% 무시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 575,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effect = "단일 대상, 방어 무시 (방어력 40% 무시)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 방어 무시 효율 증가 65% (모델엔 추가 Arm_Pen으로 근사)
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Arm_Pen = 25 }, Effect = "방어 무시 효율 증가 65%" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "전설의 부활",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 무도의 경지: 받는 물리 피해 29% 감소, 받는 마법 피해 90% 증가
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27, Arm_Pen = 5, Phys_Dmg_Rdc = 29, Mag_Dmg_Rdc = -90 } }
                            },
                            Effect = "치명타 확률 27%, 모든 피해 방어 5% 무시, 무도의 경지(물리 받피감 29% / 마법 받피증 90%)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 33, Arm_Pen = 10, Phys_Dmg_Rdc = 29, Mag_Dmg_Rdc = -90 } }
                            },
                            Effect = "치명타 확률 33%, 모든 피해 방어 10% 무시, 무도의 경지(물리 받피감 29% / 마법 받피증 90%)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "구음검격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 155,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 190,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 }, Duration = 3, Chance = 100 }
            }, Effect = "방어력 24% 감소 [3턴]" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "이기어검",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 175,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 210,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet{ Arm_Pen = 40 }, Effect = "방어 무시 (방어력 40% 무시)" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "검의 결계",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신 모든 피해 면역[2턴], 모든 아군 빙결 면역[2턴] + 기본공격 1회 발동 시 빙결 면역[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Freeze }, Duration = 2 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effect = "자신 모든 피해 면역[2턴], 모든 아군 빙결 면역[2턴] + 기본공격 1회 발동 시 빙결 면역[2턴], 자신 치명타 피해 40% 증가[상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Freeze }, Duration = 2 } },
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "중독 [40% 확률] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "중독 [50% 확률] [2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        // [저격 자세]→[연사] 변환 스킬. 저격 자세 발동(쿨60) 시 치명타 확률 증가·위장 부여, 연사(쿨66)로 변환되어 단일 피해.
                        // 계산기에서는 연사(단일 피해) 기준으로 모델링하고, 저격 자세의 자버프는 SelfBuff로 표현.
                        Id = 2,
                        Name = "연사",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 370,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri = 37 }, Duration = 6 }
                                },
                                Effect = "저격 자세[6턴]→연사 변환, 치명타 확률 37%[6턴], 위장[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 440,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri = 45 }, Duration = 6 }
                                },
                                Effect = "저격 자세[6턴]→연사 변환, 치명타 확률 45%[6턴], 위장[2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 저격 자세 / 치명타 확률 증가 지속 12턴
                            { 6, new SkillTranscend { Effect = "저격 자세·치명타 확률 증가 지속 12턴" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "저격",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 315,
                                ConditionalExtraDmg = 155,
                                ConditionalDesc = "[저격 자세] 상태일 경우 공격력 155% 추가 피해",
                                Effect = "[저격 자세] 시 공격력 155% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 375,
                                ConditionalExtraDmg = 155,
                                ConditionalDesc = "[저격 자세] 상태일 경우 공격력 155% 추가 피해",
                                Effect = "[저격 자세] 시 공격력 155% 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: [저격 자세] 상태일 경우 추가 피해 185% (기본 155 + 30)
                            { 2, new SkillTranscend { ConditionalExtraDmg = 30, Effect = "[저격 자세] 시 추가 피해 185%로 상승" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 눈",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "치명타 피해 31% 증가 [상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 31 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effect = "치명타 피해 40% 증가 [상시]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 40 } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "어둠의 문",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 0,
                                AtkCount = 1,
                                Cooldown = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 1, Type = DamageNullType.All } }
                                },
                                Effect = "[파괴의 거인] 쿨타임 초기화, 모든 피해 무효화[피격 1회]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 0,
                                AtkCount = 1,
                                Cooldown = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } }
                                },
                                Effect = "[파괴의 거인] 쿨타임 초기화, 모든 피해 무효화[피격 2회]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 스킬 쿨타임 40초로 감소
                            { 2, new SkillTranscend { Cooldown = 40, Effect = "스킬 쿨타임 40초" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "파괴의 거인",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 150,
                                Ratio = 390,
                                Bonus = new BuffSet { Arm_Pen = 65, Cri = 50 },
                                Effect = "방어 무시 (방어력 65% 무시), 치명타 확률 50% 추가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 150,
                                Ratio = 470,
                                Bonus = new BuffSet { Arm_Pen = 65, Cri = 50 },
                                Effect = "방어 무시 (방어력 65% 무시), 치명타 확률 50% 추가"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 100% 추가 적용 (확정 치명타)
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Cri = 100 },
                                Effect = "치명타 확률 100% 추가 적용 (확정 치명타)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "근거리 연사",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 75,
                                Bonus = new BuffSet { WekBonusDmg = 35 },
                                Effect = "약점 공격 시 공격력 35% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 90,
                                Bonus = new BuffSet { WekBonusDmg = 42 },
                                Effect = "약점 공격 시 공격력 42% 추가 피해"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "폭격 지원",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Bonus = new BuffSet { WekBonusDmg = 130 },
                                Effect = "약점 공격 시 공격력 130% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 170,
                                Bonus = new BuffSet { WekBonusDmg = 155 },
                                Effect = "약점 공격 시 공격력 155% 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = "방어 무시 (방어력 40% 무시)"
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
                            // 마법 피해량 24%[상시], 약점 공격 확률 32%[상시], 스킬 1회 발동 시 공격력 95% 비례 회복
                            Effect = "마법 피해량 24% 증가, 약점 공격 확률 32% 증가 [상시], 스킬 발동 시 공격력 95% 비례 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24, Wek = 32 } }
                            }
                        } },
                        { 1, new PassiveLevelData {
                            Effect = "마법 피해량 29% 증가, 약점 공격 확률 39% 증가 [상시], 스킬 발동 시 공격력 95% 비례 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 29, Wek = 39 } }
                            }
                        } }
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 스킬 1회 발동 시 약점 공격 피해량 48% 증가 [5턴] (조건부)
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect> {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 5, Buff = new BuffSet { Wek_Dmg = 48 } }
                            },
                            Effect = "스킬 발동 시 약점 공격 피해량 48% 증가 [5턴]"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "살육의 춤",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    // 마법 취약: 받는 마법 피해 22% 증가 [100% 확률] [3턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 170,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 아군 후열 디버프 해제 2개 (모델 없음 → Effect 텍스트)
                            { 2, new SkillTranscend {
                                Effect = "2초월: 아군 후열 디버프 해제 2개"
                            }},
                            // 6초월: 약점 공격 피해 발생 시 마법 공격력의 75% 추가 피해
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    // 대상의 턴제 버프 2턴 감소 [100% 확률]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 185,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 약점 공격 피해 발생 시 마법 공격력의 85% 추가 피해
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
                            // 약점 공격 피해량 23% 증가 [상시] (스킬 2회 발동 시 흡혈[2턴]은 회복 메카닉, 모델 없음 → Effect 텍스트)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } }
                            },
                            Effect = "스킬 2회 발동 시 흡혈[2턴] (피해량 20% 회복)"
                        } },
                        { 1, new PassiveLevelData {
                            // 강화: 자신 권능(전투당 1회) + 권능 발동 시 최대 생명력 30% 회복
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnHpBelow,
                                    Authority = new Authority { ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "스킬 2회 발동 시 흡혈[2턴] (피해량 20% 회복), 권능(전투당 1회) + 발동 시 최대 생명력 30% 회복"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 91,
                                Ratio = 43,
                                TargetMaxHpRatio = 6,
                                AtkCap = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 91,
                                Ratio = 51,
                                TargetMaxHpRatio = 7,
                                AtkCap = 100,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "종말의 영면",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 120,
                                Ratio = 41,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 120,
                                Ratio = 55,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = ""
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
                                // 모든 아군 마법 공격력 19% 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 19 } },
                                // 자신 모든 피해 무효화 [피격 3회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 마법 공격력 25% 증가, 피해 무효화 횟수 4회
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 4, Type = DamageNullType.All } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 55,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 70,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "천상의 심판",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 65,
                                Effects = new List<SkillEffect>
                                {
                                    // 방어력 20% 감소 [100% 확률] [3턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 77,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "최고 판결",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 70,
                                Effect = "영멸 [60% 확률] [2턴]: 보유 상태로 사망 시 부활/불사/불굴 효과 미발동 (모델 없음)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 80,
                                Effect = "영멸 [70% 확률] [2턴]: 보유 상태로 사망 시 부활/불사/불굴 효과 미발동 (모델 없음)"
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
                                },
                                // 자신 모든 피해 무효화 [피격 2회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } },
                                // 사망 시 불사 상태로 부활 [2턴] (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 2, ReviveHp = 1, OncePerBattle = true } }
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
                                },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } },
                                // 강화: 불사 지속 3턴
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 3, ReviveHp = 1, OncePerBattle = true } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "메테오",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 105,
                                // 치명타 발생 시 마법 공격력의 40% 추가 피해
                                Bonus = new BuffSet{ CriBonusDmg = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    // 화상 [60% 확률] [2턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 120,
                                Bonus = new BuffSet{ CriBonusDmg = 50 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "어둠의 환영",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 105,
                                // 치명타 발생 시 마법 공격력의 55% 추가 피해
                                Bonus = new BuffSet{ CriBonusDmg = 55 },
                                Effects = new List<SkillEffect>
                                {
                                    // 마법 취약: 받는 마법 피해 17% 증가 [100% 확률] [3턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 17 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 120,
                                Bonus = new BuffSet{ CriBonusDmg = 55 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 침묵 [60% 확률] [2턴]
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                {
                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 60, Duration = 2 }
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
                            // 자신 치명타 확률 27% 증가 [상시]. 보호막(마공 160%, 라운드당 1회) + 시탑/무탑 마법피해량 증가 28%는 모델 없음 → Effect 텍스트
                            Effect = "마법 공격력 160% 보호막[3턴](라운드당 1회), 무한의탑·시련의탑 마법피해량 증가 28%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 치명타 확률 33%, 마법피해량 증가 31%
                            Effect = "마법 공격력 160% 보호막[3턴](라운드당 1회), 무한의탑·시련의탑 마법피해량 증가 31%",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 33 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "2초월: 보호막 흡수량 마법 공격력 185%, 생명력 50% 이하 시 마법 공격력 185% 보호막[3턴](전투당 1회)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정화탄",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    // 마법 취약: 받는 마법 피해 26% 증가 [100% 확률] [5턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 26 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    // 강화: 마법 취약 효과 33%
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 33 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "신의 심판",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 425,
                                // 마력 정화 4개 소모 → 대상 최대 생명력 26% 추가 피해 (공격력 1,300% 제한)
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    ConsumeCount = 4,
                                    TargetMaxHpRatio = 26,
                                    AtkCap = 1300
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 515,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    ConsumeCount = 4,
                                    TargetMaxHpRatio = 26,
                                    AtkCap = 1300
                                },
                                Effect = ""
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
                            // 적 1명 사망 시 마력 정화 1중첩 [최대 4중첩]. 중첩당 마법피해량 +8%, 치명타 확률 +10%
                            // → 4중첩 만재 가정: 마법피증 32%, 치명 40%
                            Effect = "마력 정화: 적 처치 시 1중첩[최대 4중첩], 중첩당 마법피해량 8%·치명타 확률 10% 증가 (4중첩 만재 가정)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Mark_Purify = 32, Cri = 40 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 마법 피해량 증가 효과 10% (중첩당) → 4중첩 만재 40%
                            Effect = "마력 정화: 적 처치 시 1중첩[최대 4중첩], 중첩당 마법피해량 10%·치명타 확률 10% 증가 (4중첩 만재 가정)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Mark_Purify = 40, Cri = 40 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 마법 감쇄 [상시] — 받는 마법 피해량 29% 감소
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Mag_Dmg_Rdc = 29 } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "비화선",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 270,
                                TargetMaxHpRatio = 20,
                                AtkCap = 350,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 330,
                                TargetMaxHpRatio = 20,
                                AtkCap = 350,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                TargetMaxHpRatio = 26, AtkCap = 700
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "불나비",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 27 }, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 155,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 33 }, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 1, Chance = 100 }
                                },
                                Effect = ""
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
                            Effect = "마법 공격력 증가는 공격력 높은 아군 2명 대상 [상시]. 모든 아군 석화 면역[2턴] + 기본공격 1회 발동 시 석화 면역[2턴].",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 21 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Petrify }, Duration = 2 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "마법 공격력 증가는 공격력 높은 아군 2명 대상 [상시]. 모든 아군 석화 면역[2턴] + 기본공격 1회 발동 시 석화 면역[2턴].",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 27 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Petrify }, Duration = 2 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "스킬 2회 발동 시 시전자 마법 공격력의 55%만큼 보호막[2턴] (모든 아군)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "모래열풍",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 95,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통(피해 면역 무시), 스킬 쿨타임 증가 19초 [70% 확률]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 110,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통(피해 면역 무시), 스킬 쿨타임 증가 23초 [70% 확률]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "메마른 해일",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "석화 해제 시 시전자 공격력의 120% 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 82,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "석화 해제 시 시전자 공격력의 120% 피해"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "사막의 마법사",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신 모든 피해 면역[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 모든 피해 면역[3턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 3, Type = DamageNullType.All } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend {
                            Effect = "자신의 스킬 1회 발동 시 모든 적군 마법 취약(받는 마법 피해 24% 증가) [100% 확률] [2턴]"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 35, Duration = 1 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "레굴루스",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 75,
                                Ratio = 220,
                                ConditionalDmgBonus = 160,
                                ConditionalDesc = "자신의 모든 디버프 해제, 해제한 디버프 개수만큼 주는 피해량 40% 증가 1중첩(최대 4중첩=160%)",
                                Effect = "자신 모든 디버프 해제 후 해제 개수만큼 주는 피해량 증가(최대 4중첩)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 75,
                                Ratio = 267,
                                ConditionalDmgBonus = 160,
                                ConditionalDesc = "자신의 모든 디버프 해제, 해제한 디버프 개수만큼 주는 피해량 40% 증가 1중첩(최대 4중첩=160%)",
                                Effect = "자신 모든 디버프 해제 후 해제 개수만큼 주는 피해량 증가(최대 4중첩)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 92,
                                Ratio = 57,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 30, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시), 각 공격마다 마비"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 92,
                                Ratio = 67,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 35, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시), 각 공격마다 마비"
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
                            Effect = "자신 모든 피해 무효화[피격 3회], 권능(전투당 1회) + 발동 시 마법 공격력의 135% 보호막[3턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 17 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 135, ShieldDuration = 3 } }
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
                            Effect = "자신 모든 피해 무효화[피격 3회], 권능(전투당 1회) + 발동 시 마법 공격력의 135% 보호막[3턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 135, ShieldDuration = 3 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend {
                            Effect = "권능 보호막 흡수량 마법 공격력의 155%로 상승, 자신 행동 제어 면역[3턴]"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 45, Duration = 2 }
                                },
                                Effect = "마력 역류는 전투 시작 시 아군 마법형 3명 이상일 때만 발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "마력 역류는 전투 시작 시 아군 마법형 3명 이상일 때만 발동"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 40 }, Duration = 2 }
                                },
                                Effect = "보호막은 모든 마법형 아군 대상 (시전자 마법 공격력의 40%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 55, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 40 }, Duration = 2 }
                                },
                                Effect = "보호막은 모든 마법형 아군 대상 (시전자 마법 공격력의 40%)"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "금지된 실험",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 52,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 }, Duration = 3 }
                                },
                                Effect = "마력 역류는 전투 시작 시 아군 마법형 3명 이상일 때 각 공격마다 발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.ManaBackflow, Stacks = 1, Chance = 50, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 }, Duration = 3 }
                                },
                                Effect = "마력 역류는 전투 시작 시 아군 마법형 3명 이상일 때 각 공격마다 발동"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정의의 단죄",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 72,
                                HealDmgRatio = 21,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effect = "자신 흡혈[3턴] (피해량의 21% 회복), 직접 피해로 처치 시 100% 위력 연속 발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 85,
                                HealDmgRatio = 24,
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effect = "자신 흡혈[3턴] (피해량의 24% 회복), 직접 피해로 처치 시 100% 위력 연속 발동"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = ""
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
                            Effect = "자신 권능(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 40 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 권능(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend {
                            Effect = "권능 발동 시 모든 디버프 해제 + 최대 생명력의 100% 회복"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "고결한 금풍",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 57,
                                DispelDefReduction = 22,
                                PreCastBuff = new BuffSet{ MagicAtk_Rate = 10 },
                                Effect = "버프 2개 해제, 해제한 버프 개수 만큼 방어력 감소(2중첩)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 67,
                                DispelDefReduction = 22,
                                PreCastBuff = new BuffSet { MagicAtk_Rate = 10 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 75 }
                                },
                                Effect = "버프 2개 해제, 해제한 버프 개수 만큼 방어력 감소(2중첩)"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 52 }
                                },
                                Effect = "항상 4중첩 유지"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 39 }
                                },
                                Effect = "항상 4중첩 유지"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "용린성하",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 62,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 62,
                                Bonus = new BuffSet { CriBonusDmg = 74, CriBonusDmgPerHit = true },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 70,
                                ConditionalExtraDmg = 30,
                                ConditionalExtraDmgPerHit = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 90,
                                ConditionalExtraDmg = 42,
                                ConditionalExtraDmgPerHit = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 30,
                                DefRatio = 30,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 30,
                                DefRatio = 30,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 65 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = "빗나감 확률 36%"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 45 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = "빗나감 확률 48%"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 13 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 16 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "뇌운 흑영랑",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 92,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 45 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "우후죽순",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 270,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 330,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 26 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 26 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 31 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정기 흡수",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 285,
                                ConditionalExtraDmg = 170,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 340,
                                ConditionalExtraDmg = 170,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "빛의 심판",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 75 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 75 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                HpRatio = 14,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "잘못된 기록",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 35 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 80,
                                HpRatio = 19,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 95,
                                HpRatio = 22,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "장비 강화",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Bos = 33, Wek = 44 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Bos = 40, Wek = 54 } }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 100,
                                DefRatio = 115,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                DefRatio = 135,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "행진가",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 21,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 24,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "따뜻한 울림",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 23 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 28 } }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 60,
                                HpRatio = 14,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 60,
                                HpRatio = 14,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 36 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "빛의 일갈",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 20,
                                HpRatio = 5,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 22,
                                HpRatio = 6,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 7 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                DefRatio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 9 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "심판대행",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 42,
                                DefRatio = 47,
                                HealDmgRatio = 32,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 50,
                                DefRatio = 55,
                                HealDmgRatio = 43,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "이계의 빛",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 32 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 30,
                                HpRatio = 7,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 30,
                                HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 16 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "달빛의 심판",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 37,
                                HpRatio = 10,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "성령의 기도",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Effect = "아군 2명 부활, 적군 턴감 2턴"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Effect = "아군 2명 부활, 적군 턴감 2턴, 부활 시 권능 2턴"
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 31 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 39 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 29,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 33,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 11 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "축복의 선율",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 15,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 15,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effect = "두명 부활"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effect = "두명 부활"
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "자연의 회복",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealAtkRatio = 180,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealAtkRatio = 230,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealAtkRatio = 105,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealAtkRatio = 125,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "염화",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 102,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.Debuff, 
                                        Debuff = new DebuffSet { 
                                            Atk_Reduction = 22,
                                            Dmg_Reduction = 17 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 122,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.Debuff, 
                                        Debuff = new DebuffSet { 
                                            Atk_Reduction = 22,
                                            Dmg_Reduction = 17 } }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { 
                                        Target = EffectTarget.Enemy, 
                                        Type = SkillEffectType.Debuff, 
                                        Debuff = new DebuffSet { 
                                            Def_Reduction = 29, 
                                            Vulnerability = 22 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 36, Vulnerability = 28 } }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "뇌제의 분노",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 113,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 136,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 87,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "부숴버려!",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 17 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 23 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "봉인해제!",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 545,
                                HealAtkRatio = 25,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 655,
                                HealAtkRatio = 30,
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "달빛 베기",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 70,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 85,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 140,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17, Blk_Red = 25 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 140,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 17, Blk_Red = 33 } }
                                },
                                Effect = "아군 디버프 해제"
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "천공의 빛",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 80,
                                FixedDamage = 775,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 80,
                                FixedDamage = 1285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "별빛 낙하",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 20 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "어둠의 일격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 305,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 370,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "분노의 일격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "용의 분노",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 55 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                HealAtkRatio = 20,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "한 방이면 끝!",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 125,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 150,
                                Effect = "행동제어면역3턴"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "놀아 볼까!",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 58,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 28 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 68,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 37 } }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 65,
                                HpRatio = 15,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "혹한의 일격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 195,
                                HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 195,
                                HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 32,
                                HpRatio = 8,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 40,
                                HpRatio = 9,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
                                } }
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                DefRatio = 70,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "분쇄",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 120,
                                DefRatio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 34 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 44 } }
                                },
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "대지 파괴",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 75,
                                DefRatio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 85,
                                DefRatio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 45 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                DefRatio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 65,
                                DefRatio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 75,
                                DefRatio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70 }
                                },
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "창공의 패왕",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 30,
                                DefRatio = 33,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 35,
                                DefRatio = 38,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                HealDmgRatio = 17,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                HealDmgRatio = 17,
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 57,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 67,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 77,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 90,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Unrecover = 1 } }
                                },
                                Effect = "회복불가 추가"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "청룡월파참",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 390,
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 470,
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effect = "아군 디버프 해제 2개, 해제한 디버프 1개당 지속 회복(2턴), 해제가능한 디버프가 있어야 사용가능"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "과오의 화옥",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 25,
                                DefRatio = 28,
                                Bonus = new BuffSet{Arm_Pen = 40},
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 28,
                                DefRatio = 33,
                                Bonus = new BuffSet{Arm_Pen = 40},
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "절망의 폭풍",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 20,
                                DefRatio = 23,
                                HealDmgRatio = 31,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 39,
                                    Arm_Pen = 45,
                                    ConsumeCount = 4
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 23,
                                DefRatio = 26,
                                HealDmgRatio = 37,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 39,
                                    Arm_Pen = 45,
                                    ConsumeCount = 4
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "평타-분신",
                        SkillType = SkillType.Normal2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 53,
                                HealAtkRatio = 35,
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 62,
                                HealAtkRatio = 35,
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 4,
                        Name = "여의난참무",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 43,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 11 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Ratio = 43,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 15 } }
                                },
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 5,
                        Name = "환.대봉승타격",
                        SkillType = SkillType.Skill3,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 86,
                                TargetMaxHpRatio = 6,
                                AtkCap = 100,
                                HealAtkRatio = 35,
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 0,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 102,
                                TargetMaxHpRatio = 7,
                                AtkCap = 100,
                                HealAtkRatio = 35,
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 0,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 68,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Blk_Red = 25 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 0,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 82,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Blk_Red = 33 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "100만 볼트",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 40 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 50 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 115,
                                Effect = "쿨증, 턴감"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "권총 사격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Vulnerability = 22 } }
                                },
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "사기 진작",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 29 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 35 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 65,
                                DefRatio = 75,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "돌격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 170,
                                DefRatio = 195,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 205,
                                DefRatio = 235,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100 }
                                },
                                Effect = ""
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "방어 준비",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effect = "링크"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 10 } }
                                },
                                Effect = "링크"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                HpRatio = 14,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "잔혹한 폭풍",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 15,
                                DefRatio = 4,
                                Effect = "상대 약확 22% 감소, 즉사 턴감"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 15,
                                DefRatio = 4,
                                Effect = "상대 약확 30% 감소, 즉사 턴감"
                                }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "칠흑의 장막",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 35,
                                HpRatio = 9,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 }
                                },
                                Effect = "링크"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 35,
                                HpRatio = 9,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 10 } }
                                },
                                Effect = "링크"
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                HpRatio = 14,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "지옥의 방패",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                HealHpRatio = 15,
                                Effect = "도발, 피면, 아군 지속 힐"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 80,
                                HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 37 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 80,
                                HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 37 } }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effect = "평타 시 9초쿨감"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "포격 지원",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 110,
                                DefRatio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 130,
                                DefRatio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60 }
                                },
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                HpRatio = 12,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                HpRatio = 14,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "투창",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 315,
                                HpRatio = 75,
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 0,
                                Ratio = 380,
                                HpRatio = 91,
                                Effect = ""
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
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
                                Effect = "파티 보호막"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 0,
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

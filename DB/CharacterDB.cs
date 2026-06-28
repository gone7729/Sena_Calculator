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
                                    // 초월: 스택당 물리취약 4% (+ 기본 받물피증 3% 유지). override 방식이라 둘 다 여기에 명시.
                                    Debuff = new DebuffSet { Vulnerability = 4, Phys_Dmg_Taken_Increase = 3 }
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
                                Cooldown = 105,   // 강화 시에도 쿨 동일 (이전 0은 데이터 누락 → 무한시전 버그)
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
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Chance = 45, Duration = 2 } },
                                Effect = "석화(45%)[2턴]",
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 0,
                                Ratio = 57,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Chance = 55, Duration = 2 } },
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, Chance = 100, DispelBuffCount = 2 }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, Chance = 100, DispelBuffCount = 2 }
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
                                    FocusTarget = new FocusTarget { Selector = TargetSelector.HighestAtkEnemy } },
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
                                    FocusTarget = new FocusTarget { Selector = TargetSelector.HighestAtkEnemy } },
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
                                TargetSelector = TargetSelector.HighestDefEnemy,
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
                                TargetSelector = TargetSelector.HighestDefEnemy,
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

            // 돼오
            new Character
            {
                Id = 15,
                Name = "돼오",
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "출혈 [40% 확률] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "출혈 [50% 확률] [2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "룰렛맨",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 0,
                                Cooldown = 80,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Cri_Dmg = 40 } }
                                },
                                Effect = "비피해 스킬: 모든 아군 치명타 피해 증가 40% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 0,
                                Cooldown = 80,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Cri_Dmg = 49 } }
                                },
                                Effect = "비피해 스킬: 모든 아군 치명타 피해 증가 49% [3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Atk_Reduction = 30 } }
                            }, Effect = "적군 3명 모든 공격력 감소 30% [100% 확률] [3턴]" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "진수성참",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 145,
                                Bonus = new BuffSet { CriBonusDmg = 65 },
                                HealAtkRatio = 55,
                                Effect = "치명타 발생 시 물리 공격력 65% 추가 피해, 모든 아군 시전자 물리 공격력 55% 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 170,
                                Bonus = new BuffSet { CriBonusDmg = 65 },
                                HealAtkRatio = 65,
                                Effect = "치명타 발생 시 물리 공격력 65% 추가 피해, 모든 아군 시전자 물리 공격력 65% 회복"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "상회대표",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 7첩 반상: 모든 공격 1회 발동 시 1중첩 [최대 7중첩], 중첩당 받는 물리 피해 4% 감소
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Phys_Dmg_Rdc = 28 } }
                            },
                            Effect = "[상시] 침묵 면역[2턴], 기본 공격 1회 발동 시 침묵 면역[2턴]. 7첩 반상: 모든 공격 1회 발동 시 1중첩(최대 7), 중첩당 받는 물리 피해량 4% 감소(최대 7×4=28%)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 7첩 반상 강화: 중첩당 받는 물리 피해 5% 감소
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Phys_Dmg_Rdc = 35 } }
                            },
                            Effect = "[상시] 침묵 면역[2턴], 기본 공격 1회 발동 시 침묵 면역[2턴]. 7첩 반상: 모든 공격 1회 발동 시 1중첩(최대 7), 중첩당 받는 물리 피해량 5% 감소(최대 7×5=35%)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend { Effect = "자신의 스킬 1회 발동 시 시전자 물리 공격력의 55%만큼 보호막 [2턴]" } }
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 란드그리드
            new Character
            {
                Id = 16,
                Name = "란드그리드",
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
                        Name = "약속된 전장",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 105,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { WekBonusDmg = 31 } }
                                },
                                Effect = "자신 약점 공격 피해량 증가 31% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 105,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { WekBonusDmg = 31 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Blk_Red = 25 } }
                                },
                                Effect = "자신 약점 공격 피해량 증가 31% [3턴], 적군 3명 막기 확률 감소 25% [100%] [3턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "절대 창의 주스트",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 4,
                                Cooldown = 108,
                                Ratio = 76,
                                Bonus = new BuffSet { Arm_Pen = 100 },
                                ConditionalExtraDmg = 91,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 공격력이 자신보다 낮을 경우 각 공격마다 물리 공격력 91% 관통 추가 피해",
                                TargetSelector = TargetSelector.FrontRowEnemy,  // 전열 우선
                                Effect = "전열 우선, 4회 관통(피해 면역 무시). 전투 시작 시 아군에 공격형 5명 이상 시 생명력 전환 32%. 대상 공격력이 자신보다 낮으면 각 공격마다 91% 관통 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 4,
                                Cooldown = 108,
                                Ratio = 101,
                                Bonus = new BuffSet { Arm_Pen = 100 },
                                ConditionalExtraDmg = 91,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 공격력이 자신보다 낮을 경우 각 공격마다 물리 공격력 91% 관통 추가 피해",
                                TargetSelector = TargetSelector.FrontRowEnemy,  // 전열 우선
                                Effect = "전열 우선, 4회 관통(피해 면역 무시). 전투 시작 시 아군에 공격형 5명 이상 시 생명력 전환 32%. 대상 공격력이 자신보다 낮으면 각 공격마다 91% 관통 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effect = "해당 스킬의 직접 피해로 적군 처치 시 [절대 창의 주스트] 쿨타임 초기화" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "영웅 무쌍",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 적군 방어력 감소 + 막기 확률 감소 [상시]
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20, Blk_Red = 19 } },
                                // 사망 시 불굴 상태로 부활 [피격 8회] (전투당 1회), 스킬 1회 발동 시 피격 횟수 1 증가(최대 8), 적군 사망 시도 1 증가
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { HitCount = 8, ReviveHp = 1, OncePerBattle = true, HitCountGainOnEnemyDeath = 1, MaxHitCount = 8 } }
                            },
                            Effect = "[상시] 자신 모든 피해 면역[2턴]. 적군 방어력 감소 20%, 막기 확률 감소 19%. 사망 시 불굴[피격 8회] 부활(전투당 1회), 스킬 1회 발동/적군 사망 시 불굴 피격 횟수 1 증가(최대 8). 자신 행동 제어 시 아군 전체 행동 제어 디버프 해제(전투당 1회) + 행동 제어 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 적군 방어력 감소 24%, 막기 확률 감소 23%
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24, Blk_Red = 23 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { HitCount = 8, ReviveHp = 1, OncePerBattle = true, HitCountGainOnEnemyDeath = 1, MaxHitCount = 8 } }
                            },
                            Effect = "[상시] 자신 모든 피해 면역[2턴]. 적군 방어력 감소 24%, 막기 확률 감소 23%. 사망 시 불굴[피격 8회] 부활(전투당 1회), 스킬 1회 발동/적군 사망 시 불굴 피격 횟수 1 증가(최대 8). 자신 행동 제어 시 아군 전체 행동 제어 디버프 해제(전투당 1회) + 행동 제어 면역[2턴]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 자신 약점 공격 확률 증가 39% [상시]
                        { 2, new PassiveTranscend { SelfBuff = new PermanentBuff { Wek = 39 }, Effect = "자신 약점 공격 확률 증가 39% [상시]" } }
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 브란즈&브란셀
            new Character
            {
                Id = 17,
                Name = "브란즈&브란셀",
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
                                AtkCount = 2,
                                Ratio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Ratio = 75,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "괴력의 난동",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 105,
                                Ratio = 38,
                                Bonus = new BuffSet { Arm_Pen = 100 },
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 30, Duration = 2 }
                                },
                                Effect = "3회 관통(피해 면역 무시), 각 공격마다 출혈 [30%] [2턴], 직접 피해로 처치 시 100% 위력 연속 발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 105,
                                Ratio = 45,
                                Bonus = new BuffSet { Arm_Pen = 100 },
                                OnKillRecast = new OnKillRecast { RatioPercent = 100 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 35, Duration = 2 }
                                },
                                Effect = "3회 관통(피해 면역 무시), 각 공격마다 출혈 [35%] [2턴], 직접 피해로 처치 시 100% 위력 연속 발동"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "구원의 일격",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 247,
                                TargetSelector = TargetSelector.BackRowEnemy,  // 후열 우선
                                Effect = "후열 우선, 생명력 24% 이하 적 즉시 처형(현재 생명력이 시전자 공격력 120% 초과 시 미적용)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 297,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 3 }
                                },
                                TargetSelector = TargetSelector.BackRowEnemy,  // 후열 우선
                                Effect = "후열 우선, 생명력 24% 이하 적 즉시 처형(현재 생명력이 시전자 공격력 120% 초과 시 미적용), 턴제 버프 감소 3턴 [100%]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { HealAtkRatio = 110, Effect = "자신 물리 공격력 110% 만큼 생명력 회복" }},
                            { 6, new SkillTranscend { Effect = "단일 아군: 해당 스킬 직접 피해로 처치 시 사망한 대상을 생명력 40%로 부활(공격형 3명 이상 편성 시), 부활 시 모든 피해 무효화 [피격 2회]" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "선악의 권능",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 권능 (전투당 1회): 현재 생명력 이상 피해 시 생명력 1로 1회 생존
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "[상시] 권능(전투당 1회), 자신 모든 피해 면역[2턴]. 아군 사망 시 모든 아군 물리 공격력 증가 25%[2턴](공격형 3명 이상), 적군 사망 시 모든 아군 시전자 물리 공격력 45% 보호막[2턴](공격형 3명 이상)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "[상시] 권능(전투당 1회) + 발동 시 모든 피해 면역[2턴], 자신 모든 피해 면역[2턴]. 아군 사망 시 모든 아군 물리 공격력 증가 25%[2턴](공격형 3명 이상), 적군 사망 시 모든 아군 시전자 물리 공격력 45% 보호막[2턴](공격형 3명 이상)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 차해인
            new Character
            {
                Id = 18,
                Name = "차해인",
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
                        Name = "무희",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 105,
                                Ratio = 62,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55, Duration = 2 }
                                },
                                Effect = "모든 아군 디버프 해제 2개, 마비 [55%] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 105,
                                Ratio = 72,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55, Duration = 2 }
                                },
                                Effect = "모든 아군 디버프 해제 2개, 마비 [55%] [2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "검무",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 120,
                                Ratio = 62,
                                Bonus = new BuffSet { CriBonusDmg = 27, CriBonusDmgPerHit = true },
                                DmgBonusPerMissingTarget = 7,
                                Effect = "치명타 발생 시 각 공격마다 물리 공격력 27% 추가 피해, 피해 대상 1명 줄어들 때마다 각 공격마다 7% 피해량 증가, 직접 피해로 처치 시 [검무] 쿨타임 감소 60초"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 120,
                                Ratio = 75,
                                Bonus = new BuffSet { CriBonusDmg = 27, CriBonusDmgPerHit = true },
                                DmgBonusPerMissingTarget = 7,
                                Effect = "치명타 발생 시 각 공격마다 물리 공격력 27% 추가 피해, 피해 대상 1명 줄어들 때마다 각 공격마다 7% 피해량 증가, 직접 피해로 처치 시 [검무] 쿨타임 감소 60초"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effect = "해당 스킬 직접 피해로 적군 처치 시 [검무] 쿨타임 초기화" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "마나 감지",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "사망 시 생명력 80%로 부활(전투당 1회). [무한의 탑/시련의 탑 한정] 모든 아군 효과 적용 확률 증가 28% [상시], 스킬 1회 발동 시 스킬 쿨타임 감소 15초"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 부활 시 생명력 100%
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
                            },
                            Effect = "사망 시 생명력 100%로 부활(전투당 1회). [무한의 탑/시련의 탑 한정] 모든 아군 효과 적용 확률 증가 32% [상시], 스킬 1회 발동 시 스킬 쿨타임 감소 15초"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "[모든 아군] 자신의 스킬 1회 발동 시 시전자 물리 공격력의 40%만큼 생명력 회복" } }
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 파이
            new Character
            {
                Id = 19,
                Name = "파이",
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 35, Duration = 2 }
                                },
                                Effect = "적군 3명 화상 [35%] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 35, Duration = 2 }
                                },
                                Effect = "적군 3명 화상 [35%] [2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "아그니아의 창",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = "모든 적군, 각 공격마다 화상 [60%] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 70, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } }
                                },
                                Effect = "모든 적군, 각 공격마다 화상 [70%] [2턴], 방어력 감소 29% [100%] [3턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "염룡의 연주",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Ratio = 340,
                                TargetMaxHpRatio = 20,
                                AtkCap = 300,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "단일 적군: 버프 해제 3개, 화상 [100%] [3턴], 대상 최대 생명력 20% 비례 피해(시전자 공격력 300% 제한), 생명력 24% 이하 적 즉시 처형(현재 생명력이 시전자 공격력 120% 초과 시 미적용). 주 대상 동일열 적군: 화상 [60%] [2턴], 물리 공격력 120% + 대상 최대 생명력 14% 비례 피해(시전자 공격력 150% 제한)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Ratio = 410,
                                TargetMaxHpRatio = 26,
                                AtkCap = 300,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "단일 적군: 버프 해제 3개, 화상 [100%] [3턴], 대상 최대 생명력 26% 비례 피해(시전자 공격력 300% 제한), 생명력 24% 이하 적 즉시 처형(현재 생명력이 시전자 공격력 120% 초과 시 미적용). 주 대상 동일열 적군: 화상 [60%] [2턴], 물리 공격력 120% + 대상 최대 생명력 14% 비례 피해(시전자 공격력 150% 제한)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "처형 조건 생명력 30%" }},
                            { 6, new SkillTranscend { Effect = "처형 대상 동일열 전체로 확장" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "솟구치는 불꽃",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 적군 물리 취약 [상시] 17%
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 17 } },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "[상시] 모든 적군 물리 취약 17%. 사망 시 생명력 80%로 부활(전투당 1회) + 부활 시 물리 공격력 135% 보호막[3턴]. [화상] 상태 대상 공격 시 물리 공격력 70% 추가 피해"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 물리 취약 20%, 부활 시 생명력 100%
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 20 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
                            },
                            Effect = "[상시] 모든 적군 물리 취약 20%. 사망 시 생명력 100%로 부활(전투당 1회) + 부활 시 물리 공격력 135% 보호막[3턴]. [화상] 상태 대상 공격 시 물리 공격력 70% 추가 피해"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 칼 헤론
            new Character
            {
                Id = 20,
                Name = "칼 헤론",
                Grade = "전설",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 1, Chance = 20 }
                                },
                                Effect = "단일 적군(전뢰의 표식 우선). [전뢰의 표식] 대상에게 400 추가 고정 피해 + 감전[20%][1턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 1, Chance = 20 }
                                },
                                Effect = "단일 적군(전뢰의 표식 우선). [전뢰의 표식] 대상에게 400 추가 고정 피해 + 감전[20%][1턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 1, Chance = 30 }
                            }, Effect = "2초월: [전뢰의 표식] 대상 감전 확률 30%" }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "뇌광포",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 84,
                                Ratio = 53,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 22 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 30 }
                                },
                                Effect = "적군 3명(전뢰의 표식 우선), 관통 피해. 물리 취약[100%][3턴](받피 +22%). [전뢰의 표식] 대상에게 1,700 추가 관통 고정 피해 + 감전[30%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 84,
                                Ratio = 53,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 28 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 30 }
                                },
                                Effect = "강화: 물리 취약 28%. 적군 3명(전뢰의 표식 우선), 관통 피해. [전뢰의 표식] 대상에게 1,700 추가 관통 고정 피해 + 감전[30%][2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 50 }
                            }, Effect = "2초월: [전뢰의 표식] 대상 감전 확률 50%" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "청천벽력·개(改)",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 30 }
                                },
                                Effect = "모든 적군, 각 공격마다 감전[30%][2턴]. [전뢰의 표식] 대상에게 1,700 추가 고정 피해 + 감전[30%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 35 }
                                },
                                Effect = "강화: 감전 확률 35%. 모든 적군, 각 공격마다 감전. [전뢰의 표식] 대상에게 1,700 추가 고정 피해 + 감전[35%][2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Duration = 2, Chance = 50 }
                            }, Effect = "2초월: [전뢰의 표식] 대상 감전 확률 50%" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "쌍창의 달인",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 자신: 모든 피해 면역[2턴], 사망 시 생명력 80% 부활(전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } },
                                // 아군(공격형): 치명타 피해 31% 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "공격형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 31 } },
                                // 모든 아군: 물리 피해량 19% 증가[3턴], 효과 저항 31% 증가[3턴]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 19, Eff_Res = 31 } },
                                // 모든 적군: 받는 회복량 44% 감소 [상시] (모델 필드 없음 → Effect 텍스트)
                            },
                            Effect = "[상시] 자신 모든 피해 면역[2턴], 사망 시 생명력 80%로 부활(전투당 1회). 공격형 아군 치명타 피해 +31%. 모든 아군 물리 피해량 +19%[3턴]·효과 저항 +31%[3턴]. 모든 적군 받는 회복량 44% 감소. 전투 시작 시 공격형 영웅 3명 이상일 때 공격력 최고 적군에 [전뢰의 표식](치확·약확 50% 감소)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } },
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "공격형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 31 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 19, Eff_Res = 31 } },
                                // 6초월: 부활 시 모든 피해 무효화[피격 2회] + 디버프 해제 2개 + 물리 피해량 +19%[3턴] (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    OncePerBattle = true,
                                    DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DebuffCleanse,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    OncePerBattle = true,
                                    DispelDebuffCount = 2 },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    OncePerBattle = true,
                                    Duration = 3,
                                    Buff = new BuffSet { Dmg_Dealt_Type = 19 } },
                            },
                            Effect = "강화: 부활 시 생명력 100%, 받는 회복량 감소 52%. 6초월: 부활 시 모든 피해 무효화[피격 2회] + 디버프 해제 2개 + 물리 피해량 +19%[3턴] (전투당 1회). 모든 적군 받는 회복량 52% 감소"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            #endregion

            #region 희귀 - 공격형 51~

            // 풍연
            new Character
            {
                Id = 51,
                Name = "풍연",
                Grade = "희귀",
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
                Grade = "희귀",
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
                                ResetsCooldownOf = SkillType.Skill2,   // 파괴의 거인 쿨타임 초기화
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
                                ResetsCooldownOf = SkillType.Skill2,   // 파괴의 거인 쿨타임 초기화
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
                                    // 마법 취약: 받는 마법 피해 22% 증가 [100% 확률] [3턴] — 툴팁상 피해 前(증폭)
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, PreDamage = true, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, PreDamage = true, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 아군 후열 디버프 해제 2개
                            { 2, new SkillTranscend {
                                Effect = "2초월: 아군 후열 디버프 해제 2개",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.BackRowAlly, Type = SkillEffectType.DebuffCleanse, DispelDebuffCount = 2 }
                                }
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
                                    // 대상의 턴제 버프 2턴 감소 [100% 확률] — 툴팁상 피해 前(피해면역 관통)
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, PreDamage = true, TurnReduction = 2 }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, PreDamage = true, TurnReduction = 2 }
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
                            // 약점 공격 피해량 23% 증가 [상시]. 모든 아군: 자신 스킬 2회 발동 시 흡혈[2턴](피해량 20% 회복)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Lifesteal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 2, Duration = 2, LifestealRatio = 20 }
                            },
                            Effect = "스킬 2회 발동 시 흡혈[2턴] (피해량 20% 회복)"
                        } },
                        { 1, new PassiveLevelData {
                            // 강화: 자신 권능(전투당 1회) + 권능 발동 시 최대 생명력 30% 회복
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Lifesteal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 2, Duration = 2, LifestealRatio = 20 },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnHpBelow,
                                    Authority = new Authority { ReviveHpPercent = 30, OncePerBattle = true } }
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
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.AllEnemies, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Annihilation, Chance = 60, Duration = 2 } },
                                Effect = "영멸 [60% 확률] [2턴]: 보유 상태로 사망 시 부활/불사/불굴 효과 미발동"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 80,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.AllEnemies, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Annihilation, Chance = 70, Duration = 2 } },
                                Effect = "영멸 [70% 확률] [2턴]: 보유 상태로 사망 시 부활/불사/불굴 효과 미발동"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 1, Chance = 100 }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 1, Chance = 100 }
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 70, Debuff = new DebuffSet { Cooldown_Increase = 19 } }
                                },
                                Effect = "관통(피해 면역 무시), 스킬 쿨타임 증가 19초 [70% 확률]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 110,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 70, Debuff = new DebuffSet { Cooldown_Increase = 23 } }
                                },
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
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
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
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
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
                                    new SkillEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 40 }, Duration = 2 }
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
                                    new SkillEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 40 }, Duration = 2 }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 50, Duration = 2 }
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
                                Cooldown = 112,
                                Ratio = 57,
                                DispelDefReduction = 11,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 2, Chance = 100 }
                                },
                                Effect = "버프 2개 해제, 해제한 버프 개수만큼 방어력 11% 감소(최대 4중첩)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 112,
                                Ratio = 67,
                                DispelDefReduction = 11,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 75, Duration = 2 }
                                },
                                Effect = "버프 2개 해제, 해제한 버프 개수만큼 방어력 11% 감소(최대 4중첩)"
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
                                Cooldown = 120,
                                Ratio = 70,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 52 }
                                },
                                Effect = "신성 4개 우선 소모하여 생명력 전환, 방어 무시 40%"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 120,
                                Ratio = 80,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 39 }
                                },
                                Effect = "신성 4개 우선 소모하여 생명력 전환, 방어 무시 40%"
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
                                // 마법형 영웅 3명 이상 편성 시 효과 적중 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Condition = "마법형 영웅 3명 이상", Buff = new BuffSet { Eff_Hit = 31 } },
                                // 모든 피해 무효화 [피격 3회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } },
                                // 신성: 모든 공격 1회 발동 시 1중첩 [최대 6중첩], 중첩당 마법 공격력 +5%
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllAttack, StacksPerTrigger = 1, MaxStacks = 6, IsPerStack = true, Buff = new BuffSet { MagicAtk_Rate = 5 } }
                            },
                            Effect = "축복 40%, 무효화[피격3회], 생명력 50% 이하 시 마법공격력 160% 보호막[3턴](전투당1회), 모든 공격 1회당 신성 1중첩(최대6, 중첩당 마법공격력 +5%)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blessing = 25 } },
                                // 마법형 영웅 3명 이상 편성 시 효과 적중 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Condition = "마법형 영웅 3명 이상", Buff = new BuffSet { Eff_Hit = 37 } },
                                // 모든 피해 무효화 [피격 3회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } },
                                // 신성: 모든 공격 1회 발동 시 1중첩 [최대 6중첩], 중첩당 마법 공격력 +5%
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.AllAttack, StacksPerTrigger = 1, MaxStacks = 6, IsPerStack = true, Buff = new BuffSet { MagicAtk_Rate = 5 } }
                            },
                            Effect = "축복 25%(피격 제한 최대 생명력 25%), 무효화[피격3회], 생명력 50% 이하 시 마법공격력 160% 보호막[3턴](전투당1회), 모든 공격 1회당 신성 1중첩(최대6, 중첩당 마법공격력 +5%)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "전투 시작 시 신성 4중첩 (최대 6중첩)"
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
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 55,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 2, Chance = 45 }
                                },
                                Effect = "혼란 [타격 2회]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 65,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 2, Chance = 50 }
                                },
                                Effect = "혼란 [타격 2회]"
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
                                Cooldown = 80,
                                Ratio = 62,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "대상의 턴제 버프 2턴 감소"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 62,
                                Bonus = new BuffSet { WekBonusDmg = 37, WekBonusDmgPerHit = true },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "대상의 턴제 버프 2턴 감소, 약점 공격 피해 발생 시 각 공격마다 마법공격력 37% 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effect = "직접 피해로 적군 처치 시 [용린성하] 쿨타임 초기화 (모델 미지원)"
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
                                Cooldown = 96,
                                Ratio = 70,
                                ConditionalExtraDmg = 42,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 현재 생명력 50% 이상 시 각 공격마다 마법공격력 42% 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 3, Chance = 100 }
                                },
                                Effect = "혼란 [타격 3회]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 90,
                                ConditionalExtraDmg = 42,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 현재 생명력 50% 이상 시 각 공격마다 마법공격력 42% 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Confusion, Stacks = 3, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Wek_Reduction = 30 } }
                                },
                                Effect = "혼란 [타격 3회], 약점 공격 확률 감소 30% [100%][3턴]"
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
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 아군 약점 공격 피해량 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 18 } },
                                // 모든 적군 약점 공격 확률 감소 18% [상시]
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Wek_Reduction = 18 } },
                                // 모든 피해 무효화 [피격 2회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "무효화[피격2회], 부활 80%, 스킬 1회당 무효화[피격2회] 증가(최대6회), 적군 약점 공격 확률 18% 감소[상시]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek_Dmg = 23 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Wek_Reduction = 22 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "무효화[피격2회], 부활 80%, 스킬 1회당 무효화[피격2회] 증가(최대6회), 적군 약점 공격 확률 22% 감소[상시]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                // 2초월: 부활 시 생명력 100%
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } },
                                // 2초월: 자신 부활 시 모든 피해 무효화 [피격 2회]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnRevival, DamageNullification = new DamageNullification { HitCount = 2, Type = DamageNullType.All } }
                            },
                            Effect = "부활 시 생명력 100%, 부활 시 무효화[피격2회]"
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
                                DefRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 30 }
                                },
                                Effect = "[마력 결속-3명] 수정 결정 [피격 6회]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Ratio = 30,
                                DefRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 }
                                },
                                Effect = "[마력 결속-3명] 수정 결정 [피격 6회]"
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
                                Cooldown = 100,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 50 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                },
                                Effect = "[마력 결속-3명] 수정 결정 [피격6회], 수정 공명 [피격 4회 감소]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 100,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 65 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.CrystalResonance, Stacks = 1, Chance = 100 }
                                },
                                Effect = "[마력 결속-3명] 수정 결정 [피격6회], 수정 공명 [피격 4회 감소]"
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
                                Cooldown = 104,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "빗나감 확률 증가 36% [3턴], [마력 결속-3명] 각 공격마다 수정 결정 [피격4회]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 30,
                                DefRatio = 32,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Crystal, Stacks = 1, Chance = 45 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "빗나감 확률 증가 48% [3턴], [마력 결속-3명] 각 공격마다 수정 결정 [피격4회]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Stacks = 1, Chance = 100, Duration = 2 }
                                },
                                Effect = "자신 도발 [2턴]"
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
                            // 신체 강화: 수정룡 - 기본 방어력/생명력 상승 [상시]
                            FlatBonus = new BaseStatSet { Def = 1009, Hp = 3929},
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 적군 마법 취약 [상시]
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 17 } },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "수정룡(깡 방어/생명), 부활 80%, [마력결속-4명] 행동제어 면역[3턴], [마력결속-5명] 마법형 아군 공격 1회당 시전자 방어력 45% 보호막[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            FlatBonus = new BaseStatSet { Def = 1009, Hp = 3929},
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 20 } },
                                // 사망 시 생명력 100%로 부활 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
                            },
                            Effect = "수정룡(깡 방어/생명), 부활 100%, [마력결속-4명] 행동제어 면역[3턴], [마력결속-5명] 마법형 아군 공격 1회당 시전자 방어력 45% 보호막[2턴]"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 2, Debuff = new DebuffSet { Eff_Red = 13 } }
                                },
                                Effect = "효과 저항 감소 [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 2, Debuff = new DebuffSet { Eff_Red = 16 } }
                                },
                                Effect = "효과 저항 감소 [2턴]"
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
                                Cooldown = 81,
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "각 공격마다 마비 [40%] [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 81,
                                Ratio = 92,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "각 공격마다 마비 [40%] [2턴]"
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
                                Cooldown = 88,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 45, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                },
                                Effect = "마비 [45%] [2턴], 받는 회복량 감소 44% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Heal_Reduction = 44 } }
                                },
                                Effect = "마비 [55%] [2턴], 받는 회복량 감소 44% [3턴]"
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
                                // 모든 아군 효과 적중 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28 } },
                                // 자신 모든 피해 면역 [2턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            },
                            Effect = "자신 모든 피해 면역 [2턴], 아군 효과 적중 증가 [상시]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34 } },
                                // 자신 모든 피해 면역 [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { Duration = 3, Type = DamageNullType.All } }
                            },
                            Effect = "자신 모든 피해 면역 [3턴], 아군 효과 적중 증가 [상시]"
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
                                Cooldown = 84,
                                Ratio = 270,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 },
                                Effect = "방어 무시 40%, 약점 공격 피해 발생 시 마법공격력 215% 추가 방어 무시 피해, 모든 아군 디버프 2개 해제"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 330,
                                Bonus = new BuffSet{ Arm_Pen = 40, WekBonusDmg = 215 },
                                Effect = "방어 무시 40%, 약점 공격 피해 발생 시 마법공격력 215% 추가 방어 무시 피해, 모든 아군 디버프 2개 해제"
                                } }
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
                                Cooldown = 108,
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 5, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 26 } },
                                    // 공격력이 가장 높은 아군 2명 마법공격력 증가 [3턴]
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.HighestAtkAlly, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { MagicAtk_Rate = 25 } }
                                },
                                Effect = "방어 무시 40%, 마법 취약 26% [5턴], 공격력 높은 아군 2명 마법공격력 증가 25% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 108,
                                Ratio = 340,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 5, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 26 } },
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.HighestAtkAlly, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { MagicAtk_Rate = 31 } }
                                },
                                Effect = "방어 무시 40%, 마법 취약 26% [5턴], 공격력 높은 아군 2명 마법공격력 증가 31% [3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                // 6초월: 공격력 높은 아군 2명 치명타 피해 증가 46% [3턴]
                                Effects = new List<SkillEffect> {
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.HighestAtkAlly, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Cri_Dmg = 46 } }
                                },
                                Effect = "공격력 높은 아군 2명 치명타 피해 증가 46% [3턴]"
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
                                // 자신 마법 피해량 증가 [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24 } },
                                // 모든 아군 마비 면역 [2턴]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Paralysis }, Duration = 2 } },
                                // 스킬 1회당 시전자 마법공격력 45% 보호막 [2턴] (나타 2초월과 동일 패턴: 모든 아군 스킬 시전 시 시전자 보호막)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 45 } }
                            },
                            Effect = "아군 마비 면역[2턴], 기본공격 1회당 마비 면역[2턴], 스킬 1회당 시전자 마법공격력 45% 보호막[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 29 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Paralysis }, Duration = 2 } },
                                // 스킬 1회당 시전자 마법공격력 45% 보호막 [2턴]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 45 } }
                            },
                            Effect = "아군 마비 면역[2턴], 기본공격 1회당 마비 면역[2턴], 스킬 1회당 시전자 마법공격력 45% 보호막[2턴]"
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

            // 나타
            new Character
            {
                Id = 118,
                Name = "나타",
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
                                Effect = "자신·공격력이 가장 높은 아군 스킬 쿨타임 감소 8초",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.SelfAndHighestAtkAlly, Type = SkillEffectType.Buff, Buff = new BuffSet { Cooldown_Reduction = 8 } }
                                }
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effect = "자신·공격력이 가장 높은 아군 스킬 쿨타임 감소 8초",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.SelfAndHighestAtkAlly, Type = SkillEffectType.Buff, Buff = new BuffSet { Cooldown_Reduction = 8 } }
                                }
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "화첨창술",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 175,
                                // 대상이 잃은 생명력에 비례해 최대 50% 피해량 증가
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Effect = "대상이 잃은 생명력에 비례해 최대 50% 피해량 증가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 225,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Effect = "대상이 잃은 생명력에 비례해 최대 50% 피해량 증가"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "혼천릉파",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 260,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Effect = "대상이 잃은 생명력에 비례해 최대 50% 피해량 증가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 335,
                                LostHpBonusDmgMax = 50,
                                LostHpAssumedRemaining = 0,
                                Effect = "대상이 잃은 생명력에 비례해 최대 50% 피해량 증가"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 방어 무시 40%
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = "6초월: 방어 무시 40%"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "연꽃의 보호",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신 치명타 확률 27% 증가[상시]. 아군(마법형) 3인 공격기 피해량 증가 28%[상시]
                            // 생명력 50% 이하 시 모든 피해 면역[2턴](전투당 1회)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } },
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 28 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, OncePerBattle = true, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            },
                            Effect = "[자신] 생명력 50% 이하 시 모든 피해 면역[2턴](전투당 1회), 치명타 확률 27% 증가[상시]. [아군(마법형)] 3인 공격기 피해량 증가 28%[상시]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 치명타 확률 33%, 3인 공격기 피해량 증가 33%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 33 } },
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 33 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, OncePerBattle = true, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            },
                            Effect = "강화: [자신] 치명타 확률 33% 증가, 생명력 50% 이하 시 모든 피해 면역[2턴](전투당 1회). [아군(마법형)] 3인 공격기 피해량 증가 33%[상시]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 모든 아군 스킬 1회 발동 시 시전자 마법 공격력 55% 보호막[2턴]
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 55 } }
                            },
                            Effect = "2초월: [모든 아군] 스킬 1회 발동 시 시전자 마법 공격력 55% 보호막[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 레긴레이프
            new Character
            {
                Id = 119,
                Name = "레긴레이프",
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
                                // 마법 취약 [100%] 1중첩 [최대 4중첩] (마법형 5명 이상 편성 시) — 받는 마법 피해 10%/중첩
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 10 } }
                                },
                                Effect = "마법 취약[100%] 1중첩[최대 4중첩, 중첩당 받는 마법 피해 10%](마법형 5명 이상 편성 시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 10 } }
                                },
                                Effect = "마법 취약[100%] 1중첩[최대 4중첩, 중첩당 받는 마법 피해 10%](마법형 5명 이상 편성 시)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 대상 수 적군 2명
                            { 2, new SkillTranscend {
                                TargetCountOverride = 2,
                                Effect = "2초월: 대상 수 적군 2명"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "광명의 도래",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 65,
                                // 대상 보유 디버프 1개당 각 공격마다 마법 공격력 9% 추가 피해[최대 8개]. 치명타 확률 30% 추가 적용
                                Bonus = new BuffSet { Cri = 30 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.PerEnemyDebuffDmgBonus, PercentPerDebuff = 9, MaxDebuffStacks = 8 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 20 } }
                                },
                                Effect = "대상 보유 디버프 1개당 각 공격마다 마법 공격력 9% 추가 피해[최대 8개], 치명타 확률 30% 추가 적용, 마법 취약 2중첩[최대 4중첩](마법형 5명 이상)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 75,
                                Bonus = new BuffSet { Cri = 30 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.PerEnemyDebuffDmgBonus, PercentPerDebuff = 9, MaxDebuffStacks = 8 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 20 } }
                                },
                                Effect = "강화: 피해량 상승. 대상 보유 디버프 1개당 각 공격마다 마법 공격력 9% 추가 피해[최대 8개], 치명타 확률 30% 추가 적용"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 100% 추가 적용
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Cri = 100 },
                                Effect = "6초월: 치명타 확률 100% 추가 적용"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "우주의 균열",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 120,
                                Ratio = 42,
                                // 대상 최대 생명력 5% (시전자 공격력 50% 제한). 치명타 확률 30% 추가 적용
                                TargetMaxHpRatio = 5,
                                AtkCap = 50,
                                Bonus = new BuffSet { Cri = 30 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 20 } }
                                },
                                Effect = "마법 공격력 42% + 대상 최대 생명력 5%(공격력 50% 제한) 3회 피해, 치명타 확률 30% 추가, 마법 취약 2중첩[최대 4중첩](마법형 5명 이상)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 120,
                                Ratio = 42,
                                TargetMaxHpRatio = 5,
                                AtkCap = 50,
                                Bonus = new BuffSet { Cri = 30 },
                                // 강화: 마비 [55%] [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 20 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Paralysis, Stacks = 1, Chance = 55, Duration = 2 }
                                },
                                Effect = "강화: 마비[55%][2턴] 추가. 치명타 확률 30% 추가 적용"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 100% 추가 적용
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Cri = 100 },
                                Effect = "6초월: 치명타 확률 100% 추가 적용"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "조화의 비원",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 적군 받는 회복량 감소 44%[상시]. 사망 시 불사[2턴] 부활(전투당 1회). 생명력 70% 이하 시 모든 피해 면역[2턴](전투당 1회)
                            // 평타2회/스킬1회마다 마법형 5명 이상 시 회복·디버프 해제 → 모델 없음 → Effect 텍스트
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Revival = new Revival { ImmortalTurns = 2, OncePerBattle = true } }
                            },
                            Effect = "[자신] 생명력 70% 이하 시 모든 피해 면역[2턴](전투당 1회), 사망 시 불사 부활[2턴](전투당 1회), 불사 시 최후의 심판[2턴](방무 40%·효저 49%·쿨 초기화). [모든 아군] 평타2회/스킬1회마다 시전자 마법공격력 32% 회복 + 디버프 해제 1개(마법형 5명 이상). [모든 적군] 받는 회복량 감소 44%[상시]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 모든 적군 방어력 감소 24%[상시] 추가
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Heal_Reduction = 44, Def_Reduction = 24 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Revival = new Revival { ImmortalTurns = 2, OncePerBattle = true } }
                            },
                            Effect = "강화: [모든 적군] 방어력 감소 24%[상시] 추가. 받는 회복량 감소 44%[상시]. [자신] 불사 부활[2턴]+최후의 심판. 평타2회/스킬1회마다 회복+디버프 해제(마법형 5명 이상)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 성진우
            new Character
            {
                Id = 120,
                Name = "성진우",
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
                                // 대상이 [방어력 증가] 상태일 경우 마법 공격력 45% 추가 피해 (조건부)
                                ConditionalExtraDmg = 45,
                                ConditionalDesc = "대상이 [방어력 증가] 상태일 경우 마법 공격력 45% 추가 피해",
                                Effect = "대상이 [방어력 증가] 상태일 경우 마법 공격력 45% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                ConditionalExtraDmg = 45,
                                ConditionalDesc = "대상이 [방어력 증가] 상태일 경우 마법 공격력 45% 추가 피해",
                                Effect = "대상이 [방어력 증가] 상태일 경우 마법 공격력 45% 추가 피해"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "급소 찌르기",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 57,
                                // 대상 최대 생명력 7% (시전자 공격력 75% 제한). 관통: 피해 면역 무시
                                TargetMaxHpRatio = 7,
                                AtkCap = 75,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "마법 공격력 57% + 대상 최대 생명력 7%(공격력 75% 제한) 2회 관통 피해 (피해 면역 무시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 67,
                                TargetMaxHpRatio = 9,
                                AtkCap = 75,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "강화: 마법 공격력 67% + 대상 최대 생명력 9%(공격력 75% 제한) 2회 관통 피해 (피해 면역 무시)"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "그림자 소환",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 45,
                                // 방어 무시 40% 피해. 대상 속공이 자신보다 낮을 경우 각 공격마다 마법 공격력 62% 추가 방어 무시 피해 (조건부, 타격당)
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                ConditionalExtraDmg = 62,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 속공이 자신보다 낮을 경우 각 공격마다 마법 공격력 62% 추가 방어 무시 피해",
                                Effect = "마법 공격력 45% 2회 방어 무시(40%) 피해. 대상 속공이 자신보다 낮을 경우 각 공격마다 마법 공격력 62% 추가 방어 무시 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 55,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                ConditionalExtraDmg = 62,
                                ConditionalExtraDmgPerHit = true,
                                ConditionalDesc = "대상 속공이 자신보다 낮을 경우 각 공격마다 마법 공격력 62% 추가 방어 무시 피해",
                                Effect = "강화: 마법 공격력 55% 2회 방어 무시(40%) 피해. 대상 속공이 자신보다 낮을 경우 각 공격마다 마법 공격력 62% 추가 방어 무시 피해"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "레벨업",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 아군 마법 피해량 증가 17%[상시]. 자신 모든 피해 무효화[피격 3회], 효과 저항 증가 40%[상시], 사망 시 생명력 80% 부활(전투당 1회)
                            // 레벨업 스택(공격력 3%/레벨, 최대 30레벨)·역경을 이겨낸 자(스테이지 단계비례 피증 2%)는 동적 → Effect 텍스트
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 17 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 40 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "[자신] 레벨업: 공격 2회/스킬 1회당 1레벨, 적 사망 시 2레벨(최대 30레벨, 레벨당 모든 공격력 3%). 역경을 이겨낸 자: 스테이지 단계 비례 피해량 2%/단계. 사망 시 생명력 80% 부활(전투당 1회). [모든 아군] 마법 피해량 증가 17%[상시]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 마법 피해량 증가 20%, 부활 시 생명력 100%, 효과 저항 증가 49%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 49 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, DamageNullification = new DamageNullification { HitCount = 3, Type = DamageNullType.All } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
                            },
                            Effect = "강화: [모든 아군] 마법 피해량 증가 20%[상시]. [자신] 부활 시 생명력 100%, 효과 저항 증가 49%[상시], 모든 피해 무효화[피격 3회]. 레벨업·역경을 이겨낸 자(동적)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: [레벨업] 최대 40레벨. 6초월: 부활 시 모든 아군 모든 피해 무효화[피격 1회], 자신 모든 피해 무효화[피격 3회] + 10레벨 증가
                        { 2, new PassiveTranscend {
                            Effect = "2초월: [자신] 레벨업 최대 40레벨"
                        }},
                        { 6, new PassiveTranscend {
                            Effect = "6초월: 자신 부활 시 [모든 아군] 모든 피해 무효화[피격 1회], [자신] 모든 피해 무효화[피격 3회] + 10레벨 증가(최대 40레벨)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 오목
            new Character
            {
                Id = 121,
                Name = "오목",
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
                                Ratio = 100,
                                // 강화: 아군 2명(자신 제외) 마법 피해량 증가 10% [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Dmg_Dealt_Type = 10 } }
                                },
                                Effect = "강화: 아군 2명(자신 제외) 마법 피해량 증가 10% [2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "맹호의 무도",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 98,
                                Ratio = 305,
                                // 방어력이 가장 높은 적군 대상. 생명력 전환 32%. 아군(마법형) 수호령[2턴]
                                TargetSelector = TargetSelector.HighestDefEnemy,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 32 }
                                },
                                Effect = "방어력이 가장 높은 적군 대상, 생명력 전환 32%. [아군(마법형)] 수호령[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 98,
                                Ratio = 305,
                                TargetSelector = TargetSelector.HighestDefEnemy,
                                // 강화: 아군(마법형) 디버프 해제 2개
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 32 }
                                },
                                Effect = "강화: [아군(마법형)] 수호령[2턴] + 디버프 해제 2개. 방어력이 가장 높은 적군 대상, 생명력 전환 32%"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "만월화차 맹호전",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 136,
                                Ratio = 70,
                                // 대상 최대 생명력 7% (시전자 공격력 75% 제한). 대상 현재 생명력 비례 최대 50% 피해량 증가
                                TargetMaxHpRatio = 7,
                                AtkCap = 75,
                                CurrentHpBonusDmgMax = 50,
                                Effect = "마법 공격력 70% + 대상 최대 생명력 7%(공격력 75% 제한) 2회 피해. 대상 현재 생명력 비례 최대 50% 피해량 증가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 136,
                                Ratio = 80,
                                TargetMaxHpRatio = 9,
                                AtkCap = 75,
                                CurrentHpBonusDmgMax = 50,
                                Effect = "강화: 마법 공격력 80% + 대상 최대 생명력 9%(공격력 75% 제한) 2회 피해. 대상 현재 생명력 비례 최대 50% 피해량 증가"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 방어 무시 65%
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { Arm_Pen = 65 },
                                Effect = "6초월: 방어 무시 65%"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "신목의 가호",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 아군 마법 공격력 증가 19%[상시]. 자신 위장[2턴]. 자신 사망 시 모든 적군 약점 공격 확률 감소 27%·방어력 감소 24%[100%][3턴](전투당 1회)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 19 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Chance = 100, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 24 } }
                            },
                            Effect = "[자신] 위장[2턴]. [모든 아군] 마법 공격력 증가 19%[상시]. [모든 적군] 자신 사망 시 약점 공격 확률 감소 27%·방어력 감소 24%[100%][3턴](전투당 1회)"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 마법 공격력 증가 25%, 방어력 감소 34%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SelfDeath, OncePerBattle = true, Chance = 100, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 34 } }
                            },
                            Effect = "강화: [모든 아군] 마법 공격력 증가 25%[상시]. [모든 적군] 자신 사망 시 약점 공격 확률 감소 27%·방어력 감소 34%[100%][3턴](전투당 1회). [자신] 위장[2턴]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 권능(전투당 1회) + 발동 시 시전자 마법 공격력 155% 보호막[3턴]
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 155, ShieldDuration = 3 } }
                            },
                            Effect = "2초월: [자신] 권능(전투당 1회) + 발동 시 시전자 마법 공격력 155% 보호막[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            #endregion

            #region 희귀 - 마법형 151~

            // 유리
            new Character
            {
                Id = 151,
                Name = "유리",
                Grade = "희귀",
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
                                    // 치명타 발생 시 화상 [100% 확률] [2턴 지속]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 2 }
                                },
                                Effect = "치명타 발생 시 화상"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 2 }
                                },
                                Effect = "치명타 발생 시 화상"
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
                                Cooldown = 60,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 자신 마법 피해량 증가 29% [5턴 지속]
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Dmg_Dealt_Type = 29 } }
                            }}}
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
                                Cooldown = 72,
                                Ratio = 285,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                ConditionalExtraDmg = 170,
                                ConditionalDesc = "대상이 화상 상태일 경우 마법 공격력의 170% 추가 피해",
                                Effect = "방어 무시"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 340,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                ConditionalExtraDmg = 170,
                                ConditionalDesc = "대상이 화상 상태일 경우 마법 공격력의 170% 추가 피해",
                                Effect = "방어 무시"
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
                                // 치명타 확률 증가 21% [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 21 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 치명타 확률 증가 27% [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: 사망 시 생명력 80%로 부활
                        {6, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                            new PersistentEffect {
                                Target = EffectTarget.Self,
                                Type = PersistentEffectType.Revival,
                                ApplyMode = ApplyMode.Triggered,
                                TriggerCondition = TriggerCondition.SelfDeath,
                                Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                        },
                            Effect = "사망 시 생명력 80%로 부활"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 아리엘
            new Character
            {
                Id = 152,
                Name = "아리엘",
                Grade = "희귀",
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
                                Cooldown = 70,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    // 실명 [75% 확률] [2턴 지속] — 기본/반격/협공이 반드시 빗나감
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Stacks = 1, Chance = 75, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Stacks = 1, Chance = 75, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 빗나감 확률 증가 44% [100% 확률][3턴 지속] — 대상 공격이 빗나갈 확률 증가. 모델 필드 없음 → 텍스트.
                            {6, new SkillTranscend{
                                Effect = "빗나감 확률 증가 44% [100% 확률][3턴 지속]"
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
                                Cooldown = 70,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    // 방어력 감소 20% [100% 확률][3턴 지속]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 20 } }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 방어력 감소 20% → 24% (delta +4, Debuff.Add 합산)
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 4 } }
                            },
                                Effect = "방어력 감소 24%"
                            }}
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
                                // 마법 피해량 증가 11% [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 11 } },
                                // 화상 면역 [2턴] (전투 시작/기본공격 1회 발동 시)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } }
                            },
                            Effect = "화상 면역[2턴], 마법 피해량 증가 11%"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 15 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } }
                            },
                            Effect = "화상 면역[2턴], 마법 피해량 증가 15%"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCriDmg
            },

            //노호
            new Character
            {
                Id = 153,
                Name = "노호",
                Grade = "희귀",
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
                                Cooldown = 77,
                                Effects = new List<SkillEffect>
                                {
                                    // 생명력 전환 35% (현재 생명력보다 높게는 전환 안 됨)
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 35 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 77,
                                Effects = new List<SkillEffect>
                                {
                                    // 강화: 생명력 전환 25%
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                },
                                Effect = ""
                                } }
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
                                Cooldown = 70,
                                Ratio = 80,
                                HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    // 적군 3명(버프 많은 순)에게 버프 해제 2개 [100% 확률]
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "대상 선정: 버프가 많은 순"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 95,
                                HpRatio = 22,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "대상 선정: 버프가 많은 순"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 모든 공격력 감소 16% [100% 확률][3턴 지속]
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Atk_Reduction = 16 } }
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
                                // 모든 아군 마법 공격력 증가 19% [2턴 지속]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 2, Buff = new BuffSet { MagicAtk_Rate = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 마법 공격력 증가 23%
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 2, Buff = new BuffSet { MagicAtk_Rate = 23 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: 자신 스킬 1회 발동 시 최대 생명력 32% 보호막 [3턴 지속]
                        {6, new PassiveTranscend{
                            Effects = new List<PersistentEffect> {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 3, Buff = new BuffSet { Shield_HpRatio = 32 } }
                            },
                            Effect = "스킬 1회 발동 시 최대 생명력 32% 보호막[3턴]"
                        }}
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
                                    // 강화: 피해량 감소 6% [100% 확률][2턴 지속]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 2, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
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
                                Cooldown = 120,
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    // 대상: 공격력이 가장 높은 아군 1명 — 보스 피해량 증가 33% + 약점 확률 증가 44% [5턴] (6초월 시 2명)
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.HighestAtkAlly, TargetCount = 1, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Dmg_Dealt_Bos = 33, Wek = 44 } }
                                },
                                Effect = "대상: 공격력이 가장 높은 아군 1명"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 120,
                                EffectDuration = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetSelector = TargetSelector.HighestAtkAlly, TargetCount = 1, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Dmg_Dealt_Bos = 40, Wek = 54 } }
                                },
                                Effect = "대상: 공격력이 가장 높은 아군 1명"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 장비 강화 버프 대상 1명 → 2명 (공격력 최고 아군)
                            { 6, new SkillTranscend { TargetCountOverride = 2, Effect = "대상 수 변경: 공격력이 가장 높은 아군 2명" } }
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
                                Cooldown = 60,
                                Ratio = 100,
                                DefRatio = 115,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    // 적군 3명(버프 많은 순) 버프 해제 2개 [100% 확률] — 툴팁상 피해 前(보호막 관통)
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, PreDamage = true, DispelBuffCount = 2 }
                                },
                                Effect = "방어 무시, 대상 선정: 버프가 많은 순"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 115,
                                DefRatio = 135,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, PreDamage = true, DispelBuffCount = 2 }
                                },
                                Effect = "방어 무시, 대상 선정: 버프가 많은 순"
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
                                HealHpRatio = 7,
                                TargetSelector = TargetSelector.LowestHpAlly,  // 현재 생명력 가장 낮은 아군 (자신 제외 단서는 텍스트 유지)
                                Effect = "강화: 현재 생명력 가장 낮은 아군(자신 제외)에게 시전자 최대 생명력 7% 회복 추가"
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
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                HealHpRatio = 21,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "모든 아군 회복, 지속 회복[2턴] (매턴 시전자 최대 생명력 15% 회복)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                HealHpRatio = 24,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "모든 아군 회복, 지속 회복[2턴] (매턴 시전자 최대 생명력 15% 회복)"
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
                                Cooldown = 120,
                                Effects = new List<SkillEffect>
                                {
                                    // 적군 전열 방어력 감소 34% [100% 확률][3턴], 모든 아군 피해량 증가 23% [3턴]
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Dmg_Dealt = 23 } }
                                },
                                Effect = "디버프 대상: 적군 전열"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 34 } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Dmg_Dealt = 28 } }
                                },
                                Effect = "디버프 대상: 적군 전열"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 디버프 대상 적군 전체로 변경 + 방어력 감소 34% → 41% (오버라이드: 최종값 41 선언)
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect> {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Def_Reduction = 41 } }
                                },
                                Effect = "대상 수 변경: 적군 전체, 방어력 감소 41%"
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
                            Effect = "효과 저항 28% 증가[상시], 자신 보호막 최대체력 39%[3턴, 라운드당 1회]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 3, Buff = new BuffSet { Shield_HpRatio = 39 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 28 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "효과 저항 34% 증가[상시], 자신 보호막 최대체력 45%[3턴, 라운드당 1회]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Duration = 3, Buff = new BuffSet { Shield_HpRatio = 45 } },
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
                                Cooldown = 70,
                                Ratio = 60,
                                HpRatio = 14,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 60,
                                HpRatio = 14,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 },
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
                                Cooldown = 104,
                                Ratio = 20,
                                HpRatio = 5,
                                Effects = new List<SkillEffect>
                                {
                                    // 대상: 자신 + 마법형 아군 (오를리는 지원형이므로 자신 포함은 텍스트로 유지, 직업군 제한만 필드화)
                                    new SkillEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 19 }, Duration = 2 }
                                },
                                Effect = "보호막 대상: 자신, 마법형 아군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 22,
                                HpRatio = 6,
                                Effects = new List<SkillEffect>
                                {
                                    // 대상: 자신 + 마법형 아군 (오를리는 지원형이므로 자신 포함은 텍스트로 유지, 직업군 제한만 필드화)
                                    new SkillEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 23 }, Duration = 2 }
                                },
                                Effect = "보호막 대상: 자신, 마법형 아군"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effect = "[자신/마법형 아군] 디버프 해제 2개"
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
                            Effect = "[자신] 최대 생명력 39% 보호막[3턴] (라운드당 1회). 치명타 확률/피해 증가 대상: 자신, 마법형 아군",
                            Effects = new List<PersistentEffect>
                            {
                                // 대상: 자신 + 마법형 아군 (오를리는 지원형 → 자신 포함은 텍스트로 유지, 직업군 제한만 필드화)
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 17, Cri_Dmg = 25 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "[자신] 최대 생명력 45% 보호막[3턴] (라운드당 1회). 치명타 확률/피해 증가 대상: 자신, 마법형 아군",
                            Effects = new List<PersistentEffect>
                            {
                                // 대상: 자신 + 마법형 아군 (오를리는 지원형 → 자신 포함은 텍스트로 유지, 직업군 제한만 필드화)
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "마법형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 17, Cri_Dmg = 25 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.DamageNullification, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, DamageNullification = new DamageNullification { HitCount = 1, Type = DamageNullType.All } }
                            },
                            Effect = "[자신/마법형 아군] 스킬 1회 발동 시 모든 피해 무효화[피격 1회]"
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
                                Cooldown = 96,
                                Ratio = 42,
                                DefRatio = 47,
                                HealDmgRatio = 32,
                                Effect = "[자신] 피해량의 32% 생명력 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 50,
                                DefRatio = 55,
                                HealDmgRatio = 43,
                                Effect = "[자신] 피해량의 43% 생명력 회복"
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
                                Cooldown = 91,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 32 }
                                },
                                Effect = "대상: 적군 2명 (버프 많은 순)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 91,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 25 }
                                },
                                Effect = "대상: 적군 2명 (버프 많은 순)"
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, MaxTriggersPerBattle = 2, TriggeredHealDefRatio = 165 },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 4, TriggerOn = TriggerCondition.OnHit, FixedDamage = 1100, TargetCount = 5, HitCount = 1 } }
                            },
                            Effect = "[자신] 생명력 50% 이하 시 방어력 165% 회복 (전투당 2회). 적 4회 피격당 시 적군 전체 1100 고정피해"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, MaxTriggersPerBattle = 2, TriggeredHealDefRatio = 190 },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 4, TriggerOn = TriggerCondition.OnHit, FixedDamage = 1100, TargetCount = 5, HitCount = 1 } }
                            },
                            Effect = "[자신] 생명력 50% 이하 시 방어력 190% 회복 (전투당 2회). 적 4회 피격당 시 적군 전체 1100 고정피해"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "[자신] 생명력 회복 전투당 3회로 증가"
                        }},
                        { 6, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 4, TriggerOn = TriggerCondition.OnHit, FixedDamage = 1485, TargetCount = 5, HitCount = 1 } }
                            },
                            Effect = "적 4회 피격당 시 적군 전체 고정피해 1485"
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
                                Cooldown = 80,
                                Ratio = 30,
                                HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 37,
                                HpRatio = 10,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35, Duration = 3 }
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
                                AtkCount = 1,
                                Cooldown = 112,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.AllEnemies, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "[아군 2명] 사망한 대상을 생명력 40%로 부활. [적군 전체] 턴제 버프 2턴 감소"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 112,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.AllEnemies, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "[아군 2명] 사망한 대상을 생명력 55%로 부활 + 권능[2턴]. [적군 전체] 턴제 버프 2턴 감소"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effect = "부활 대상 수: 아군 전체로 확장"
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
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "방어형", "만능형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 20 } }
                            },
                            Effect = "[자신] 생명력 50% 이하 시 모든 피해 면역[2턴, 해제 불가]. 효과 적용 확률 증가 대상: 방어형/만능형 아군"
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
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "방어형", "만능형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 20 } }
                            },
                            Effect = "[자신] 생명력 50% 이하 시 모든 피해 면역[3턴, 해제 불가]. 효과 적용 확률 증가 대상: 방어형/만능형 아군"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "방어형", "만능형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 26 } }
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.SingleAlly, TargetSelector = TargetSelector.LowestHpAlly, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 1 }
                                },
                                Effect = "2초월: 현재 생명력 가장 낮은 아군(자신 제외) 지속 회복[1턴] (매턴 시전자 최대 생명력 15%)"
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
                                Cooldown = 104,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 31 }, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "[모든 아군] 방어력 증가[3턴] + 지속 회복(최대 생명력 15%)[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 39 }, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "[모든 아군] 방어력 증가[3턴] + 지속 회복(최대 생명력 15%)[2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Heal_Bonus = 36 }, Duration = 3 }
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
                                Cooldown = 109,
                                HealHpRatio = 29,
                                Effect = "[모든 아군] 최대 생명력 29% 즉시 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 109,
                                HealHpRatio = 33,
                                Effect = "[모든 아군] 최대 생명력 33% 즉시 회복"
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
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Def_Rate = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, Chance = 25, TriggeredHealHpRatio = 9 }
                            },
                            Effect = "[모든 아군] 전투 시작 시 방어력 31% 증가[1턴]. [자신] 피격 시 25% 확률 최대 생명력 9% 회복"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, IsConditional = true, Buff = new BuffSet { Def_Rate = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, Chance = 25, TriggeredHealHpRatio = 10 }
                            },
                            Effect = "[모든 아군] 전투 시작 시 방어력 31% 증가[2턴]. [자신] 피격 시 25% 확률 최대 생명력 10% 회복"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            // 초선
            new Character
            {
                Id = 207,
                Name = "초선",
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
                                // 현재 생명력이 가장 낮은 아군(자신 제외) 시전자 최대 생명력 6% 회복
                                HealHpRatio = 6,
                                TargetSelector = TargetSelector.LowestHpAlly,  // 현재 생명력 가장 낮은 아군 (자신 제외 단서는 텍스트 유지)
                                Effect = "현재 생명력이 가장 낮은 아군(자신 제외) 시전자 최대 생명력 6% 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                HealHpRatio = 7,
                                TargetSelector = TargetSelector.LowestHpAlly,  // 현재 생명력 가장 낮은 아군 (자신 제외 단서는 텍스트 유지)
                                Effect = "강화: 현재 생명력이 가장 낮은 아군(자신 제외) 시전자 최대 생명력 7% 회복"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "진군가",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 85,
                                // 아군 3명 시전자 최대 생명력 25% 회복
                                HealHpRatio = 25,
                                Effect = "아군 3명 시전자 최대 생명력 25% 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 85,
                                HealHpRatio = 29,
                                Effect = "강화: 아군 3명 시전자 최대 생명력 29% 회복"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 아군 2명(자신 제외) 대상이 공격형/마법형일 경우 스킬 쿨타임 초기화
                            { 2, new SkillTranscend {
                                Effect = "2초월: 아군 2명(자신 제외) 대상이 공격형/마법형일 경우 스킬 쿨타임 초기화"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "보호의 노래",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                // 아군 2명(자신 제외) 치명타 피해 증가 43% [4턴], 적군 3명 턴제 버프 감소 2턴 + 수면 85% [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 4, Buff = new BuffSet { Cri_Dmg = 43 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 85, Duration = 2 }
                                },
                                Effect = "아군 2명(자신 제외) 치명타 피해 증가 43% [4턴], 적군 3명 턴제 버프 감소 2턴 [100%] + 수면 85% [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                // 강화: 수면 확률 100%, 아군 2명(자신 제외) 모든 공격력 증가 30% [4턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetCount = 2, Type = SkillEffectType.Buff, Duration = 4, Buff = new BuffSet { Cri_Dmg = 43, Atk_Rate = 30, MagicAtk_Rate = 30 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Sleep, Stacks = 1, Chance = 100, Duration = 2 }
                                },
                                Effect = "강화: 수면 확률 100%, 아군 2명(자신 제외) 모든 공격력 증가 30% [4턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 적군 3명 받는 회복량 감소 78% [100%] [3턴]
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Heal_Reduction = 78 } }
                                },
                                Effect = "6초월: 적군 3명 받는 회복량 감소 78% [100%] [3턴]"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "폐월의 미소",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신: 최대 생명력 1000마다 효과 적중 5% 증가(최대 45%), 시전자 최대 생명력 39% 보호막[3턴](라운드당 1회)
                            // 아군 2명(자신 제외, 공격형/마법형): 기본공격 1회당 시전자 최대 생명력 20% 보호막[2턴] + 행동 제어 면역[2턴]
                            Effect = "[자신] 최대 생명력 1000마다 효과 적중 5% 증가(최대 45%), 시전자 최대 생명력 39% 보호막[3턴](라운드당 1회). [아군 2명(자신 제외, 공격형/마법형)] 기본공격 1회당 시전자 최대 생명력 20% 보호막[2턴] + 행동 제어 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 자신 보호막 흡수량 최대 생명력 45%, 아군 보호막 흡수량 최대 생명력 22%
                            Effect = "강화: [자신] 보호막 흡수량 최대 생명력 45%[3턴]. [아군 2명(자신 제외, 공격형/마법형)] 기본공격 1회당 최대 생명력 22% 보호막[2턴] + 행동 제어 면역[2턴]. 최대 생명력 1000마다 효과 적중 5% 증가(최대 45%)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 아군(자신, 방어형, 지원형) 받는 회복량 증가 28% [상시]
                        { 2, new PassiveTranscend {
                            Effect = "2초월: [아군(자신/방어형/지원형)] 받는 회복량 증가 28% [상시]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            #endregion

            #region 희귀 - 지원형 - 마법 251~

            // 유이
            new Character
            {
                Id = 251,
                Name = "유이",
                Grade = "희귀",
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { MagicAtk_Reduction = 9 }, Duration = 2, Chance = 100 }
                                },
                                Effect = "마법 공격력 감소 9%[100%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { MagicAtk_Reduction = 11 }, Duration = 2, Chance = 100 }
                                },
                                Effect = "마법 공격력 감소 11%[100%][2턴]"
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
                                Cooldown = 124,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "[모든 아군] 지속 회복(최대 생명력 15%)[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 124,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 3 }
                                },
                                Effect = "[모든 아군] 지속 회복(최대 생명력 15%)[3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 19 }, Duration = 3 }
                            }}},
                            {6, new SkillTranscend{Effects = new List<SkillEffect> {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 24 }, Duration = 3 }
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
                                Cooldown = 112,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Revive, TargetCount = 2, ReviveHpPercent = 35 } },
                                Effect = "[아군 2명] 사망한 대상을 생명력 35%로 부활"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 2,
                                AtkCount = 1,
                                Cooldown = 112,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Revive, TargetCount = 2, ReviveHpPercent = 45 } },
                                Effect = "[아군 2명] 사망한 대상을 생명력 45%로 부활"
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } }
                            },
                            Effect = "[모든 아군] 화상 면역[2턴] + 자신 기본 공격 1회 발동 시 화상 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Burn }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, TriggeredHealHpRatio = 22 }
                            },
                            Effect = "[모든 아군] 화상 면역[2턴] + 자신 기본 공격 1회 발동 시 화상 면역[2턴]. [자신] 스킬 1회 발동 시 최대 생명력 22% 회복"
                        }}
                    }
                },
                TranscendType = TranscendType.DefDmgRdc
            },

            #endregion

            #region 희귀 - 지원형 - 물리 276~

            // 카론
            new Character
            {
                Id = 276,
                Name = "카론",
                Grade = "희귀",
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
                                Cooldown = 98,
                                HealAtkRatio = 180,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 98,
                                HealAtkRatio = 230,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ TargetCountOverride = 3, Effect = "대상 수 변경: 아군 3명" }}
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
                                Cooldown = 124,
                                HealAtkRatio = 105,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 124,
                                HealAtkRatio = 125,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Cooldown = 91, Effect = "쿨타임 감소: 91초" }}
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
                                // 모든 아군 - 기본 공격 1회 발동 시 출혈 면역 [2턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.Immunity,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.NormalOnly,
                                    TriggerCount = 1,
                                    StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Bleeding }, Duration = 2 } },
                                // 자신 - 스킬 1회 발동 시 마법 공격력 115% 보호막 [3턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SkillOnly,
                                    TriggerCount = 1,
                                    Duration = 3,
                                    Buff = new BuffSet { Shield_AtkRatio = 115 } }
                            } ,
                            Effect = "출혈 면역[2턴], 스킬 시 마공 115% 보호막[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.Immunity,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.NormalOnly,
                                    TriggerCount = 1,
                                    StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Bleeding }, Duration = 2 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Buff,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SkillOnly,
                                    TriggerCount = 1,
                                    Duration = 3,
                                    Buff = new BuffSet { Shield_AtkRatio = 135 } }
                            },
                            Effect = "출혈 면역[2턴], 스킬 시 마공 135% 보호막[3턴]"
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 50, Duration = 2 }
                                },
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
                                Cooldown = 84,
                                Ratio = 102,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect {
                                        Target = EffectTarget.Enemy,
                                        Type = SkillEffectType.Debuff,
                                        Duration = 5,
                                        Debuff = new DebuffSet {
                                            Atk_Reduction = 22,
                                            Dmg_Reduction = 17 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 84,
                                Ratio = 122,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect {
                                        Target = EffectTarget.Enemy,
                                        Type = SkillEffectType.Debuff,
                                        Duration = 5,
                                        Debuff = new DebuffSet {
                                            Atk_Reduction = 22,
                                            Dmg_Reduction = 17 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 100, Duration = 3 }
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
                                Cooldown = 84,
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect {
                                        Target = EffectTarget.Enemy,
                                        Type = SkillEffectType.Debuff,
                                        Duration = 3,
                                        Debuff = new DebuffSet {
                                            Def_Reduction = 29,
                                            Phys_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 160,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 4, PreDamage = true, Debuff = new DebuffSet { Def_Reduction = 36, Phys_Dmg_Taken_Increase = 28 } }   // 2초월 턴상승 반영(3→4턴, 시뮬 항상 2초월+). 툴팁: 방깎/취약 먼저 → 그 피해 증폭
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "턴 상승: 방어력 감소/물리 취약 지속 4턴" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "화염의 힘",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "아군 약공 확률 증가, 사망 시 생명력 80% 부활(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 22 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "아군 약공 확률 증가, 사망 시 생명력 100% 부활(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 27 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
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
                                ConditionalExtraDmg = 45,
                                ConditionalDesc = "대상이 [감전] 상태일 경우 물리 공격력의 45% 추가 피해",
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                ConditionalExtraDmg = 55,
                                ConditionalDesc = "대상이 [감전] 상태일 경우 물리 공격력의 55% 추가 피해",
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
                                Cooldown = 60,
                                Ratio = 113,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 60,
                                Ratio = 136,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 45, Duration = 3 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effect = "효과 추가: 관통(대상 피해 면역 무시)"
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
                                Cooldown = 80,
                                Ratio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 87,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "효과 추가: 관통(대상 피해 면역 무시)" } },
                            // 6초월: 감전 확률 40%로 상승 (기본 30 → 최종 40)
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 40, Duration = 2 }
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
                            Effect = "아군 물리 공격력 증가, 사망 시 생명력 80% 부활(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 19 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "아군 물리 공격력 증가, 사망 시 생명력 100% 부활(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 25 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
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
                                Cooldown = 78,
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Debuff = new DebuffSet { Dmg_Reduction = 17 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 78,
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Debuff = new DebuffSet { Dmg_Reduction = 23 } }
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
                                Cooldown = 72,
                                Ratio = 545,
                                HealAtkRatio = 25,
                                Effect = "모든 아군 시전자 물공 25% 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 655,
                                HealAtkRatio = 30,
                                Effect = "모든 아군 시전자 물공 30% 회복"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Bonus = new BuffSet { Arm_Pen = 40 }, Effect = "효과 추가: 방어 무시 40%" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "나만 믿어",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "아군 기절 면역[2턴]+기본공격 시 기절 면역[1턴], 후열 아군 물공 증가(모든 공격 2회 발동 시)[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 아군 - 기절 면역 [2턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.Immunity,
                                    StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Stun }, Duration = 2 } },
                                // 후열 아군 - 모든 공격 2회 발동 시 물리 공격력 증가 [2턴] (2초월 3턴은 전열 가정 시드라 base는 2 유지)
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetSelector = TargetSelector.BackRowAlly,   // 후열 아군 한정 (ProcessAttackBuffs가 처리)
                                    Type = PersistentEffectType.Buff,
                                    IsConditional = true,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,
                                    Duration = 2,
                                    Buff = new BuffSet { Atk_Rate = 23 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "아군 기절 면역[2턴]+기본공격 시 기절 면역[1턴], 후열 아군 물공 증가(모든 공격 2회 발동 시)[2턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.Immunity,
                                    StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Stun }, Duration = 2 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    TargetSelector = TargetSelector.BackRowAlly,   // 후열 아군 한정 (ProcessAttackBuffs가 처리)
                                    Type = PersistentEffectType.Buff,
                                    IsConditional = true,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.AllAttack,
                                    TriggerCount = 2,
                                    Duration = 3,   // 시뮬은 항상 2초월+ → 턴 상승(2→3) 반영
                                    Buff = new BuffSet { Atk_Rate = 27 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend { Effect = "턴 상승: 물리 공격력 증가 지속 3턴" } }
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
                                Cooldown = 70,
                                Ratio = 70,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "방어 무시, 대상 턴제 버프 2턴 감소"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 85,
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "방어 무시, 대상 턴제 버프 2턴 감소"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect> {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Heal_Reduction = 56 } }
                                },
                                Effect = "효과 추가: 받는 회복량 감소 56%[3턴]"
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
                                Cooldown = 104,
                                Ratio = 140,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 17, Blk_Red = 25 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 140,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 17, Blk_Red = 33 } }
                                },
                                Effect = "강화: 아군 3명 디버프 해제 2개 (SkillEffect 디버프해제 모델 미지원 → 텍스트)"
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
                            Effect = "모든 적군 방깎/받는회복량감소[상시], 자신 권능(전투당 1회)+발동 시 물공 135% 보호막[3턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20, Heal_Reduction = 44 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 135, ShieldDuration = 3 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "모든 적군 방깎/받는회복량감소[상시], 자신 권능(전투당 1회)+발동 시 물공 155% 보호막[3턴]",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24, Heal_Reduction = 44 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 155, ShieldDuration = 3 } }
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
                                Cooldown = 104,
                                Ratio = 80,
                                FixedDamage = 775,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "침묵 [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 80,
                                FixedDamage = 1285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = "침묵 [2턴]"
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
                                Cooldown = 104,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "턴제 버프 감소 2턴, 빗나감 확률 증가 36% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "턴제 버프 감소 2턴, 빗나감 확률 증가 48% [3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 3, Chance = 100 }
                                },
                                Effect = "턴제 버프 감소 3턴으로 증가"
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
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 } },
                                // 자신: 물공 160% 보호막 [3턴] (라운드당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 160 } },
                                // 생명력 50% 이하 시 모든 피해 면역 [2턴] (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnHpBelow,
                                    TriggerHpThreshold = 50,
                                    OncePerBattle = true,
                                    DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                // 자신의 스킬 1회 발동 시 아군 전체 디버프 해제 2개
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.DebuffCleanse,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SkillOnly,
                                    DispelDebuffCount = 2 }
                            },
                            Effect = "[상시] 적 방깎 20%, 자신 물공 160% 보호막[3턴](라운드당 1회), HP50%↓ 모든피해면역[2턴](전투당1회), 스킬 발동 시 아군 디버프 해제 2개"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 160 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.DamageNullification,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnHpBelow,
                                    TriggerHpThreshold = 50,
                                    OncePerBattle = true,
                                    DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.DebuffCleanse,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SkillOnly,
                                    DispelDebuffCount = 2 }
                            },
                            Effect = "[상시] 적 방깎 24%, 자신 물공 160% 보호막[3턴](라운드당 1회), HP50%↓ 모든피해면역[2턴](전투당1회), 스킬 발동 시 아군 디버프 해제 2개"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                // HP 50% 이하 시 물공 165% 비례 회복 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.TriggeredHeal,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnHpBelow,
                                    TriggerHpThreshold = 50,
                                    OncePerBattle = true,
                                    TriggeredHealAtkRatio = 165 }
                            },
                            Effect = "생명력 50% 이하 시 물공 165% 비례 회복 (전투당 1회)"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 20, Duration = 3 }
                                },
                                Effect = "즉사 [3턴]"
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
                                Cooldown = 60,
                                Ratio = 305,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 50, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 135 } }
                                },
                                Effect = "즉사 [3턴], 처치 시 물공 135% 보호막[3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 370,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 60, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 135 } }
                                },
                                Effect = "즉사 [3턴], 처치 시 물공 135% 보호막[3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend{ Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 30, Duration = 3 }
                                },
                                Effect = "즉사 [30% 확률] 추가 부여 (별도 판정)"
                            } }
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
                                Cooldown = 80,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 25, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 135 } }
                                },
                                Effect = "즉사 [3턴], 처치 시 물공 135% 보호막[3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 30, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 135 } }
                                },
                                Effect = "즉사 [3턴], 처치 시 물공 135% 보호막[3턴]"
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
                            Effects = new List<PersistentEffect>
                            {
                                // 자신: 물공 160% 보호막 [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 160 } },
                                // 사망 시 불사 상태로 부활 [2턴] (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 2, ReviveHp = 1, OncePerBattle = true } },
                                // 적 사망 후 아군 전체 시전자 물공 20% 회복
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.TriggeredHeal,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.EnemyDeath,
                                    TriggeredHealAtkRatio = 20 }
                            },
                            Effect = "[상시] 자신 물공 160% 보호막[3턴], 사망 시 불사[2턴] 부활(전투당1회), 적 사망 후 아군 물공 20% 회복"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 160 } },
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ImmortalTurns = 3, ReviveHp = 1, OncePerBattle = true } },
                                new PersistentEffect {
                                    Target = EffectTarget.Party,
                                    Type = PersistentEffectType.TriggeredHeal,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.EnemyDeath,
                                    TriggeredHealAtkRatio = 20 }
                            },
                            Effect = "[상시] 자신 물공 160% 보호막[3턴], 사망 시 불사[3턴] 부활(전투당1회), 적 사망 후 아군 물공 20% 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                // 불사 발동 시 스킬 쿨타임 초기화
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.CooldownReset,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.OnRevival,
                                    CooldownReset = new CooldownReset { AllSkills = true } }
                            },
                            Effect = "불사 발동 시 스킬 쿨타임 초기화"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "화상 [2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                Effect = "화상 [2턴]"
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
                                Cooldown = 104,
                                Ratio = 115,
                                DmgBonusPerMissingTarget = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45, Duration = 2 }
                                },
                                Effect = "기절 [2턴], 피해 대상 1명 줄어들 때마다 피해량 15% 증가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 150,
                                DmgBonusPerMissingTarget = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 45, Duration = 2 }
                                },
                                Effect = "기절 [2턴], 피해 대상 1명 줄어들 때마다 피해량 15% 증가"
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
                                Cooldown = 104,
                                Ratio = 70,
                                IgnoresTurnDamageImmunity = true,
                                DmgBonusPerMissingTarget = 8,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시), 각 공격마다 화상[2턴], 피해 대상 1명 줄어들 때마다 피해량 8% 증가"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 80,
                                IgnoresTurnDamageImmunity = true,
                                DmgBonusPerMissingTarget = 8,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 55, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시), 각 공격마다 화상[2턴], 피해 대상 1명 줄어들 때마다 피해량 8% 증가"
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
                            },
                            Effect = "[상시] 공격력 비례 방어력(300당 125, 최대 1125), 반격 38% — 반격 시 적 3명 물공 40% 피해 + 화상 35%[2턴] (반격은 모델 미지원)"
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
                            },
                            Effect = "[상시] 공격력 비례 방어력(300당 125, 최대 1375), 반격 38% — 반격 시 적 3명 물공 50% 피해 + 화상 40%[2턴] (반격은 모델 미지원)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                // 행동 제어 면역 [3턴]
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Immunity,
                                    StatusImmunity = new StatusImmunity {
                                        Types = new[] { StatusEffectType.Stun, StatusEffectType.Silence, StatusEffectType.Freeze, StatusEffectType.Petrify, StatusEffectType.Paralysis, StatusEffectType.Sleep, StatusEffectType.Confusion, StatusEffectType.Concussion },
                                        Duration = 3 }
                                }
                            },
                            Effect = "행동 제어 면역 [3턴]"
                        }},
                        { 6, new PassiveTranscend {
                            Effect = "화상이 [용염](턴마다 공격력 120% 피해, 화상으로 간주)으로 변경 — 모델 미지원"
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
                                Cooldown = 80,
                                Ratio = 125,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Immunity, StatusImmunity = new StatusImmunity {
                                        Types = new[] { StatusEffectType.Stun, StatusEffectType.Silence, StatusEffectType.Freeze, StatusEffectType.Petrify, StatusEffectType.Paralysis, StatusEffectType.Sleep, StatusEffectType.Confusion, StatusEffectType.Concussion },
                                        Duration = 3 } }
                                },
                                Effect = "행동 제어 면역 [3턴]"
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
                                Cooldown = 76,
                                Ratio = 58,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 28 } }
                                },
                                Effect = "치명타 피해 증가 28% [3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 76,
                                Ratio = 68,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 37 } }
                                },
                                Effect = "치명타 피해 증가 37% [3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Shield_AtkRatio = 155 } }
                                },
                                Effect = "물공 155% 보호막 [3턴]"
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
                                },
                                // 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
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
                                },
                                // 사망 시 생명력 100%로 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true } }
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
                                Cooldown = 72,
                                Ratio = 195,
                                HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "빙결[3턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한). 주 대상과 동일 열 적군: 물공 65%+최대HP15% 1회, 빙결 60%[3턴](동일 열 추가 대상은 모델 미지원 — 단일 대상만 계산)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 195,
                                HpRatio = 47,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "빙결[3턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한). 주 대상과 동일 열 적군: 물공 75%+최대HP18% 1회, 빙결 70%[3턴](동일 열 추가 대상은 모델 미지원 — 단일 대상만 계산)"
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
                                Cooldown = 80,
                                Ratio = 32,
                                HpRatio = 8,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "각 공격마다 빙결[2턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 40,
                                HpRatio = 9,
                                ConditionalExtraDmgSelfHpRatio = 15,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "각 공격마다 빙결[2턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
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
                            },
                            Effect = "[상시] 최대HP 비례 공격력(1000당 120, 최대 1080), 빙결 대상 공격 시 시전자 최대HP 15% 추가 피해, 아군 효과적중 28%/효과저항 28%"
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
                                    MaxValue = 1320
                                }
                            },
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34, Eff_Res = 28 } }
                            },
                            Effect = "[상시] 최대HP 비례 공격력(1000당 120, 최대 1320), 빙결 대상 공격 시 시전자 최대HP 15% 추가 피해, 아군 효과적중 34%/효과저항 28%"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                // 권능 (전투당 1회) — 발동 시 최대HP 60% 회복은 모델 미지원(텍스트 기재)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    Authority = new Authority { ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "권능 (전투당 1회), 권능 발동 시 최대 생명력 60% 회복(회복은 모델 미지원)"
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
                                Cooldown = 60,
                                Ratio = 120,
                                DefRatio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Debuff = new DebuffSet { Def_Reduction = 34 } }
                                },
                                Effect = "버프 해제 3개"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Debuff = new DebuffSet { Def_Reduction = 44 } }
                                },
                                Effect = "버프 해제 3개"
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
                                Cooldown = 70,
                                Ratio = 75,
                                DefRatio = 85,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "빗나감 확률 48% 증가"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 85,
                                DefRatio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = "빗나감 확률 60% 증가"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Atk_Reduction = 24 } }
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
                            Effect = "자신 생명력 50% 이하 시 시전자 방어력의 165% 생명력 회복 (전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 11 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, OncePerBattle = true, TriggeredHealDefRatio = 165 },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 3, TriggerOn = TriggerCondition.AllAttack, FixedDamage = 1285, TargetCount = 3, HitCount = 1 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 생명력 50% 이하 시 시전자 방어력의 165% 생명력 회복 (전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 13 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, OncePerBattle = true, TriggeredHealDefRatio = 165 },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 3, TriggerOn = TriggerCondition.AllAttack, FixedDamage = 1930, TargetCount = 3, HitCount = 1 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 6, new PassiveTranscend {
                            Effect = "자신 생명력 50% 이하 시 모든 피해 면역 2턴 (전투당 1회)"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 45, Duration = 1 }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 50, Duration = 1 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend
                            {
                                Effect = "빙극 효과 대상 적군 3명"
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
                                Cooldown = 75,
                                Ratio = 65,
                                DefRatio = 70,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 75,
                                Ratio = 75,
                                DefRatio = 85,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
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
                                Cooldown = 88,
                                Ratio = 30,
                                DefRatio = 33,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 88,
                                Ratio = 35,
                                DefRatio = 38,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.IceExtreme, Stacks = 1, Chance = 70, Duration = 2 }
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
                            Effect = "전투시작 시 만능형 3명 이상이면 생명력 50% 이하 시 시전자 방어력 165% 회복(전투당 2회). 모든 공격 2회 발동 시 적군 3명 방어력 65% 비례 피해 + 빙극 75% 2턴",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 20 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 2, TriggerOn = TriggerCondition.AllAttack, AtkRatio = 60, TargetCount = 3, HitCount = 1 } }
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
                            Effect = "전투시작 시 만능형 3명 이상이면 생명력 50% 이하 시 시전자 방어력 165% 회복(전투당 2회). 모든 공격 2회 발동 시 적군 3명 방어력 65% 비례 피해 + 빙극 75% 2턴",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 20 } },
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.TriggeredFixedDamage,
                                    TriggeredFixedDamage = new TriggeredFixedDamage { TriggerCount = 2, TriggerOn = TriggerCondition.AllAttack, AtkRatio = 60, TargetCount = 3, HitCount = 1 } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Hit_Red = 26 } }
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
                                Cooldown = 96,
                                Ratio = 57,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "관통(피해 면역 무시). 표식 2중첩 이상이면 방어 무시 전환. 표식 발동 시 자신 물리피증 1중첩(최대5)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 67,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
                                },
                                Effect = "관통(피해 면역 무시). 표식 2중첩 이상이면 방어 무시 전환. 표식 발동 시 자신 물리피증 1중첩(최대5)"
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
                                Cooldown = 96,
                                Ratio = 77,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = "표식 2중첩 이상이면 방어 무시 전환. 표식 발동 시 자신 물리피증 1중첩(최대5)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 96,
                                Ratio = 90,
                                HealDmgRatio = 24,
                                Bonus = new BuffSet{ Arm_Pen = 65 },
                                Effect = "표식 2중첩 이상이면 방어 무시 전환. 표식 발동 시 자신 물리피증 1중첩(최대5)"
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
                            },
                            Effect = "권능(전투당 1회) 발동 시 현재 생명력 가장 높은 적군과 생명력 교환. 생명력 30% 이하 시 모든 디버프 해제(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true } }
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
                            },
                            Effect = "권능(전투당 1회) 발동 시 현재 생명력 가장 높은 적군과 생명력 교환 + 모든 피해 면역 2턴. 생명력 30% 이하 시 모든 디버프 해제(전투당 1회)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Authority, Authority = new Authority { OncePerBattle = true } }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        { 2, new PassiveTranscend {
                            Effect = "받는 회복량 증가 28% [상시] (자신, 방어형, 지원형)",
                            Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "방어형", "지원형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Heal_Bonus = 28 } }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 30, Duration = 2, Debuff = new DebuffSet { Unrecover = 1 } }
                                },
                                Effect = "회복불가 [30% 확률] [2턴]"
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
                                Cooldown = 68,
                                Ratio = 390,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 3, Chance = 100 }
                                },
                                Effect = "버프 해제 3개"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 68,
                                Ratio = 470,
                                Bonus = new BuffSet{ Arm_Pen = 40 },
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, DispelBuffCount = 3, Chance = 100 }
                                },
                                Effect = "버프 해제 3개"
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
                                Cooldown = 96,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "아군 디버프 해제 2개, 해제한 디버프 1개당 지속 회복(매턴 시전자 최대HP 15%, 2턴, 해제 개수만큼 중첩), 해제가능한 디버프가 있어야 사용가능"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "아군 디버프 해제 2개, 해제한 디버프 1개당 지속 회복(매턴 시전자 최대HP 15%, 2턴, 해제 개수만큼 중첩), 해제가능한 디버프가 있어야 사용가능, 모든피해면역(1턴)"
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
                            Effect="모든 아군 감전 면역 2턴(기본공격 1회 시 갱신). 아군 후열 물리피증 20% 2턴(모든 공격 2회 시 갱신)",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 20 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect="모든 아군 감전 면역 2턴(기본공격 1회 시 갱신). 아군 후열 물리피증 20% 3턴(모든 공격 2회 시 갱신)",
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
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Chance = 100, Duration = 2, Debuff = new DebuffSet { Dmg_Reduction = 6 } }
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
                                Cooldown = 105,
                                Ratio = 25,
                                DefRatio = 28,
                                Bonus = new BuffSet{Arm_Pen = 40},
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 105,
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
                                Cooldown = 105,
                                Ratio = 20,
                                DefRatio = 23,
                                HealDmgRatio = 31,
                                IgnoresTurnDamageImmunity = true,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 45,
                                    ConsumeCount = 4
                                },
                                Effect = "관통(피해 면역 무시). 인고 4개 소모 시 각 공격마다 마공39%/방45% 관통 추가 피해"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 105,
                                Ratio = 23,
                                DefRatio = 26,
                                HealDmgRatio = 37,
                                IgnoresTurnDamageImmunity = true,
                                ConsumeExtra = new ConsumeExtraDamage
                                {
                                    AtkRatio = 39,
                                    DefRatio = 45,
                                    ConsumeCount = 4
                                },
                                Effect = "관통(피해 면역 무시). 인고 4개 소모 시 각 공격마다 마공39%/방45% 관통 추가 피해"
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
                            Effect = "자신 피격 1회 시 인고 1중첩(최대4, 중첩당 방어력 13% 증가). 모든 아군 감쇄(받피감 20%)[피격 3회], 모든 공격 2회 발동 시 갱신",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "만능형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 12 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, TriggerCount = 1, StacksPerTrigger = 1, MaxStacks = 4, Buff = new BuffSet { Def_Rate = 13 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "자신 피격 1회 시 인고 1중첩(최대4, 중첩당 방어력 13% 증가). 모든 아군 감쇄(받피감 20%)[피격 3회], 모든 공격 2회 발동 시 갱신",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, TargetClasses = new[] { "만능형" }, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt = 15 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 20 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, TriggerCount = 1, StacksPerTrigger = 1, MaxStacks = 4, Buff = new BuffSet { Def_Rate = 13 } }
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30, Duration = 1 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "석화 해제 시 시전자 공격력 120% 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 130,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30, Duration = 1 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "석화 해제 시 시전자 공격력 120% 피해"
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
                                AtkCount = 2,
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30, Duration = 1 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "치명타 확률 50% 추가 적용"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Bonus = new BuffSet{ Cri = 50 },
                                Ratio = 75,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 30, Duration = 1 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "치명타 확률 50% 추가 적용"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 추가 적용 50% → 100% (Override = 최종값)
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }}
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
                                Cooldown = 84,
                                Ratio = 53,
                                HealAtkRatio = 35,
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 84,
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
                                Cooldown = 91,
                                Ratio = 43,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Dmg_Rdc = 11 } }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "모든 아군 감쇄[받피감11%,3턴], 자신 분신[5턴]+행동제어면역[3턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 91,
                                Ratio = 43,
                                HealAtkRatio = 35,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Dmg_Rdc = 15 } }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "모든 아군 감쇄[받피감15%,3턴], 자신 분신[5턴]+행동제어면역[3턴]"
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
                                Cooldown = 84,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 86,
                                TargetMaxHpRatio = 6,
                                AtkCap = 100,
                                HealAtkRatio = 35,
                                DmgBonusPerMissingTarget = 10,
                                Effect = "치명타 확률 50% 추가 적용, 피해 대상 1명 줄 때마다 피해량 10% 증가"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 84,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 102,
                                TargetMaxHpRatio = 7,
                                AtkCap = 100,
                                HealAtkRatio = 35,
                                DmgBonusPerMissingTarget = 10,
                                Effect = "치명타 확률 50% 추가 적용, 피해 대상 1명 줄 때마다 피해량 10% 증가"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 추가 적용 50% → 100% (Override = 최종값)
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }}
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
                                Cooldown = 91,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 68,
                                HealAtkRatio = 35,
                                DmgBonusPerMissingTarget = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Blk_Red = 25 } }
                                },
                                Effect = "치명타 확률 50% 추가 적용, 피해 대상 1명 줄 때마다 피해량 5% 증가"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 3,
                                Cooldown = 91,
                                Bonus = new BuffSet{ Cri = 50, Arm_Pen = 40 },
                                Ratio = 82,
                                HealAtkRatio = 35,
                                DmgBonusPerMissingTarget = 5,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Blk_Red = 33 } }
                                },
                                Effect = "치명타 확률 50% 추가 적용, 피해 대상 1명 줄 때마다 피해량 5% 증가"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 추가 적용 50% → 100% (Override = 최종값)
                            {6, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "미후분신술",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 공격력 비례 방어력 증가 [상시] : 공격력 300마다 방어력 125, 최대 1125
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.StatScaling,
                                    StatScaling = new StatScaling { SourceStat = StatType.Atk, TargetStat = StatType.Def, PerUnit = 125, SourceUnit = 300, MaxValue = 1125 }
                                },
                                // 권능 (전투당 1회) + 발동 시 마법 공격력 135% 비례 보호막 [3턴]
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 135, ShieldDuration = 3 }
                                }
                            },
                            Effect = "권능(전투당1회)+보호막 마법공135%[3턴], 스킬발동 시 아군 마법공35% 회복, 피격 시 스킬쿨 10초 감소"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.StatScaling,
                                    StatScaling = new StatScaling { SourceStat = StatType.Atk, TargetStat = StatType.Def, PerUnit = 125, SourceUnit = 300, MaxValue = 1125 }
                                },
                                // 권능 (전투당 1회) + 발동 시 마법 공격력 155% 비례 보호막 [3턴]
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Authority,
                                    Authority = new Authority { OncePerBattle = true, ShieldAtkRatio = 155, ShieldDuration = 3 }
                                }
                            },
                            Effect = "권능(전투당1회)+보호막 마법공155%[3턴], 스킬발동 시 아군 마법공55% 회복, 피격 시 스킬쿨 10초 감소"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 아군 생명력 50% 이하 시 시전자 마법공 95% 회복 (전투당 1회) — 회복은 계산기 미반영, Effect 텍스트로만
                        { 2, new PassiveTranscend {
                            Effect = "[모든 아군] 생명력 50% 이하 시 시전자 마법공 95% 회복(전투당1회)"
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 30, Duration = 1 }
                                },
                                Effect = "감전: 피격 시 시전자 공격력 40% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 35, Duration = 1 }
                                },
                                Effect = "감전: 피격 시 시전자 공격력 40% 추가 피해"
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
                                Cooldown = 80,
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "감전: 피격 시 시전자 공격력 40% 추가 피해"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Shock, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "감전: 피격 시 시전자 공격력 40% 추가 피해"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 감전 확률 40% → 50% (SkillEffect는 append되므로 base 확률을 직접 못 덮어씀 → Effect 텍스트로만 기록)
                            {2, new SkillTranscend { Effect = "2초월: 감전 확률 50%" }}
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
                                Cooldown = 104,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 60, Debuff = new DebuffSet { Cooldown_Increase = 16 } }
                                },
                                Effect = "턴제 버프 2턴 감소[100%], 스킬 쿨타임 증가 16초[60%]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 104,
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 60, Debuff = new DebuffSet { Cooldown_Increase = 19 } }
                                },
                                Effect = "턴제 버프 2턴 감소[100%], 스킬 쿨타임 증가 19초[60%]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 쿨타임 증가 발동 확률 60% → 70% (쿨증은 계산기 미반영, Effect 텍스트로만)
                            {6, new SkillTranscend{ Effect = "6초월: 스킬 쿨타임 증가 확률 70%" }}
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
                                // 효과 적중 증가 [상시] + 마법 공격력 160% 비례 보호막 [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40, Shield_AtkRatio = 160 } }
                            },
                            Effect = "보호막 마법공 160%[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 49, Shield_AtkRatio = 200 } }
                            },
                            Effect = "보호막 마법공 200%[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 미스트
            new Character
            {
                Id = 329,
                Name = "미스트",
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
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Fear, Chance = 40, Duration = 1 } },
                                Effect = "공포[40%,1턴] (행동불가+효저0% 고정)"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "깨어난 야수",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35, Duration = 3 }
                                },
                                Effect = "즉사[35%,3턴] (공포 상태 대상은 해제불가 즉사)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 67,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40, Duration = 3 }
                                },
                                Effect = "즉사[40%,3턴] (공포 상태 대상은 해제불가 즉사)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 대상 수 변경 → 적군 전체
                            {6, new SkillTranscend{ TargetCountOverride = 5 }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 안개",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35, Duration = 3 }
                                },
                                Effect = "턴제버프 2턴 감소[100%], 공포[50%,2턴], 대상 공격력이 자신보다 낮을 시 즉사[35%,3턴](공포 시 해제불가 즉사). 공포는 모델 미지원"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 67,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 45, Duration = 3 }
                                },
                                Effect = "턴제버프 2턴 감소[100%], 공포[50%,2턴], 대상 공격력이 자신보다 낮을 시 즉사[45%,3턴](공포 시 해제불가 즉사). 공포는 모델 미지원"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: [자신] 모든 피해 면역[2턴]
                            {2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            }}}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "파멸의 안개",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 자신: 공격력 비례 약점 공격 확률 증가 [상시] (공격력 300마다 약점 3%, 최대 27%)
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.StatScaling,
                                    StatScaling = new StatScaling { SourceStat = StatType.Atk, TargetStat = StatType.Cri, PerUnit = 0, SourceUnit = 300, MaxValue = 0 }
                                },
                                // 자신: 사망 시 생명력 80%로 부활 (전투당 1회)
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true }
                                },
                                // 모든 아군: 효과 적중 증가 [상시] 28%
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 28 } }
                            },
                            Effect = "[상시][자신] 약점공 = 공격력 300마다 3%(최대 27%, StatScaling은 Wek 미지원으로 수치 미반영), 사망 시 80% 부활(전투당1회). [상시][모든 아군] 효과적중 28%, 자신 스킬 2회 발동 시 행동제어 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.StatScaling,
                                    StatScaling = new StatScaling { SourceStat = StatType.Atk, TargetStat = StatType.Cri, PerUnit = 0, SourceUnit = 300, MaxValue = 0 }
                                },
                                new PersistentEffect
                                {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    Revival = new Revival { ReviveHpPercent = 100, OncePerBattle = true }
                                },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 34 } }
                            },
                            Effect = "[상시][자신] 약점공 = 공격력 300마다 3%(최대 27%, StatScaling은 Wek 미지원으로 수치 미반영), 사망 시 100% 부활(전투당1회). [상시][모든 아군] 효과적중 34%, 자신 스킬 2회 발동 시 행동제어 면역[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 백각
            new Character
            {
                Id = 330,
                Name = "백각",
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = "강화: 출혈[40%,2턴] 추가"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "백각환독",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 60,
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 60, Duration = 3 }
                                },
                                Effect = "각 공격마다 중독[60%,3턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 60,
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 60, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Stacks = 1, Chance = 100, Duration = 4 }
                                },
                                Effect = "각 공격마다 중독[60%,3턴], 강화: 회복불가[100%,4턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 중독 확률 60% → 70% (SkillEffect는 append되므로 base 확률을 못 덮어씀 → Effect 텍스트로만)
                            {6, new SkillTranscend{ Effect = "6초월: 중독 확률 70%" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "마신주령",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 60,
                                Ratio = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 60, Duration = 3 }
                                },
                                Effect = "각 공격마다 출혈[60%,3턴], 모든 아군 디버프 해제 1개 (아군 디버프 해제는 모델 미지원)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 60,
                                Ratio = 110,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Stacks = 1, Chance = 60, Duration = 3 }
                                },
                                Effect = "각 공격마다 출혈[60%,3턴], 모든 아군 디버프 해제 1개 (아군 디버프 해제는 모델 미지원)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 출혈 확률 60% → 70% (SkillEffect는 append되므로 base 확률을 못 덮어씀 → Effect 텍스트로만)
                            {6, new SkillTranscend{ Effect = "6초월: 출혈 확률 70%" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "어둠의 주술",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신: 모든 피해 면역[2턴], 모든 아군: 자신 모든 공격 2회 발동 시 마법공 32% 회복 (회복은 계산기 미반영)
                            Effect = "[자신] 모든 피해 면역[2턴], [모든 아군] 자신 모든 공격 2회 발동 시 시전자 마법공 32% 회복"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "[자신] 모든 피해 면역[3턴], [모든 아군] 자신 모든 공격 2회 발동 시 시전자 마법공 32% 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: [자신] 효과 적용 확률 증가[상시] 22% = 효과 적중
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 22 } }
                            }
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            #endregion

            #region 희귀 - 만능형 - 물리 351~

            

            #endregion

            #region 희귀 - 만능형 - 마법 376~

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
                                Cooldown = 60,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 5, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = "마법 취약[받는 마법 피해 22% 증가, 5턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 5, Debuff = new DebuffSet { Mag_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = "마법 취약[받는 마법 피해 22% 증가, 5턴]"
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
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { MagicAtk_Rate = 29 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { MagicAtk_Rate = 35 } }
                                },
                                Effect = ""
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 마법 공격력 증가 지속 3턴 → 4턴 (지속 턴 변경은 모델 미지원 → Effect 텍스트로만)
                            {6, new SkillTranscend{ Effect = "6초월: 마법 공격력 증가 지속 4턴" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "해적왕의 위엄",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "모든 아군 마비 면역[2턴]+기본공격 시 마비 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "모든 아군 마비 면역[2턴], 스킬 1회 발동 시 마법공 90% 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 아군 후열 물리 감쇄[상시] = 받는 물리 피해 22% 감소
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect> {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Phys_Dmg_Rdc = 22 } }
                            },
                            Effect = "[아군 후열] 물리 감쇄[받는 물리 피해 22% 감소, 상시]"
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
                                Cooldown = 78,
                                Ratio = 170,
                                DefRatio = 195,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85, Duration = 3 }
                                },
                                TargetSelector = TargetSelector.MostBuffsEnemy,  // 버프가 많은 순
                                Effect = "대상 선정: 버프가 많은 적군"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 78,
                                Ratio = 205,
                                DefRatio = 235,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                TargetSelector = TargetSelector.MostBuffsEnemy,  // 버프가 많은 순
                                Effect = "대상 선정: 버프가 많은 적군"
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
                                Cooldown = 96,
                                Effect = "모든 아군 링크[2턴]+디버프 해제 1개 (링크/아군 디버프 해제는 SkillEffect 모델 미지원)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 96,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Dmg_Rdc = 10 } }
                                },
                                Effect = "모든 아군 링크[2턴, 피해 감소율 10%]+디버프 해제 1개 (링크/아군 디버프 해제는 SkillEffect 모델 미지원)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 링크 지속 3턴 / 6초월: 행동 제어 면역[2턴] — 모델 미지원, Effect 텍스트로만
                            {2, new SkillTranscend{ Effect = "2초월: 링크 지속 3턴" }},
                            {6, new SkillTranscend{ Effect = "6초월: 행동 제어 면역[2턴]" }}
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
                                Effect = "강화: 생명력 가장 낮은 아군 2명(자신 제외)에게 최대생명력 8% 보호막[2턴] (대상 선정 모델 미지원)"
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
                                Cooldown = 104,
                                Ratio = 13,
                                HpRatio = 3,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Wek_Reduction = 22 } }
                                },
                                Effect = "각 공격마다 즉사 턴 감소 1턴[60%], 약점 공격 확률 감소 22%[100%][3턴] (즉사 턴 감소는 모델 미지원)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 13,
                                HpRatio = 3,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Wek_Reduction = 30 } }
                                },
                                Effect = "각 공격마다 즉사 턴 감소 1턴[70%], 약점 공격 확률 감소 30%[100%][3턴] (즉사 턴 감소는 모델 미지원)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: [자신] 감쇄 28% [5턴] = 받피감 28%
                            {2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Dmg_Rdc = 28 } }
                            }}}
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
                                Cooldown = 91,
                                Ratio = 30,
                                HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_HpRatio = 22 } }
                                },
                                Effect = "모든 아군 보호막 최대생명력 22%[2턴]+디버프 해제 1개 (아군 디버프 해제는 모델 미지원)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 91,
                                Ratio = 30,
                                HpRatio = 7,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_HpRatio = 25 } }
                                },
                                Effect = "모든 아군 보호막 최대생명력 25%[2턴]+디버프 해제 1개 (아군 디버프 해제는 모델 미지원)"
                                }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "어둠의 인도자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신 생명력 50% 이하 시 최대생명력 34% 회복(전투당 2회), 모든 아군 즉사 적용 확률 +25% (둘 다 계산기 미반영)
                            Effect = "[자신] 생명력 50% 이하 시 최대생명력 34% 회복(전투당2회), [모든 아군] 즉사 적용 확률 25% 증가"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "[자신] 생명력 50% 이하 시 최대생명력 40% 회복(전투당2회), [모든 아군] 즉사 적용 확률 25% 증가"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: [모든 아군] 링크[3턴] — 모델 미지원, Effect 텍스트로만
                        {6, new PassiveTranscend{ Effect = "6초월: [모든 아군] 링크[3턴]" }}
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
                                Cooldown = 96,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 15, Duration = 2 }
                                },
                                Effect = "자신 모든피해면역[2턴]+도발[2턴], 아군3명 지속회복[2턴] 시전자 최대생명력 15%/턴"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 96,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } },
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Regeneration, CustomHpRatio = 20, Duration = 2 }
                                },
                                Effect = "자신 모든피해면역[2턴]+도발[2턴], 아군3명 지속회복[2턴] 시전자 최대생명력 20%/턴"
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
                                Cooldown = 70,
                                Ratio = 80,
                                HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 35, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Eff_Red = 37 } }
                                },
                                Effect = ""
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 80,
                                HpRatio = 19,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.InstantDeath, Stacks = 1, Chance = 40, Duration = 3 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Debuff = new DebuffSet { Eff_Red = 37 } }
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
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 39 } }
                            },
                            Effect = "모든 아군 즉사 효과 적용 확률 +25% (상시), 자신 최대생명력 39% 보호막[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Shield_HpRatio = 45 } }
                            },
                            Effect = "모든 아군 즉사 효과 적용 확률 +25% (상시), 자신 최대생명력 45% 보호막[3턴]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Eff_Red = 34 } }
            }}},
                        {6, new PassiveTranscend{ Effect = "6초월: 자신 생명력 50% 이하 시 최대생명력 39% 보호막[3턴] (전투당 1회)" }}
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
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Buff = new BuffSet { Cooldown_Reduction = 9 } } },
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
                                Cooldown = 84,
                                Ratio = 110,
                                DefRatio = 130,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 130,
                                DefRatio = 150,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {6, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, TurnReduction = 2, Chance = 100 }
            }, Effect = "6초월: 적군 턴제 버프 감소 2턴[100%]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "노장의 위엄",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "피격 시 25% 확률로 시전자 방어력 40% 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 13 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, Chance = 25, TriggeredHealDefRatio = 40 }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "피격 시 25% 확률로 시전자 방어력 45% 회복",
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 13 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHit, Chance = 25, TriggeredHealDefRatio = 45 }
                            }
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        {2, new PassiveTranscend{ Effects = new List<PersistentEffect> {
                new PersistentEffect { Target = EffectTarget.Enemy, Type = PersistentEffectType.Debuff, Duration = 2, Condition = "자신 스킬 1회 발동 시[100%][2턴]", Debuff = new DebuffSet { Cri_Dmg_Reduction = 40 } }
            }}}
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
                                Cooldown = 108,
                                Ratio = 315,
                                HpRatio = 75,
                                TargetSelector = TargetSelector.BackRowEnemy,  // 후열 우선
                                Effect = "후열 우선 타게팅"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 108,
                                Ratio = 380,
                                HpRatio = 91,
                                TargetSelector = TargetSelector.BackRowEnemy,  // 후열 우선
                                Effect = "후열 우선 타게팅"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            {2, new SkillTranscend{ Bonus = new BuffSet{ Cri = 100 } }},
                            {6, new SkillTranscend{ Effects = new List<SkillEffect> {
                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, CustomHpConversionRatio = 11 }
            }, Effect = "6초월: 생명력 전환 11%" }}
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
                                Cooldown = 133,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_HpRatio = 31 } }
                                },
                                Effect = "모든 아군 시전자 최대생명력 31% 보호막[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 133,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Shield_HpRatio = 31 } }
                                },
                                Effect = "모든 아군 시전자 최대생명력 31% 보호막[3턴]"
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
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Blk = 19 } }
                            }
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 20 } },
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

            // 라드그리드
            new Character
            {
                Id = 406,
                Name = "라드그리드",
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
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 2, Chance = 100, Debuff = new DebuffSet { Atk_Reduction = 7 } }
                                },
                                Effect = "모든 공격력 감소[7%,2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 60,
                                DefRatio = 70,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 2, Chance = 100, Debuff = new DebuffSet { Atk_Reduction = 9 } }
                                },
                                Effect = "모든 공격력 감소[9%,2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "전창의 비",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 25,
                                DefRatio = 27,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Cri_Reduction = 19, Wek_Reduction = 22 } }
                                },
                                Effect = "관통, 각 공격마다 화상[40%,2턴], 치명타 확률 감소 19%[100%][3턴], 약점 공격 확률 감소 22%[100%][3턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 25,
                                DefRatio = 27,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Stacks = 1, Chance = 40, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Cri_Reduction = 25, Wek_Reduction = 30 } }
                                },
                                Effect = "관통, 각 공격마다 화상[40%,2턴], 치명타 확률 감소 25%[100%][3턴], 약점 공격 확률 감소 30%[100%][3턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 치확/약공 감소 지속 3턴 → 4턴 (base Effects의 Duration override 미지원 → Effect 텍스트로만)
                            {2, new SkillTranscend{ Effect = "2초월: 치명타 확률 감소/약점 공격 확률 감소 지속 4턴" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "작렬하는 창연",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 42,
                                DefRatio = 47,
                                LostHpBonusDmgMax = 100,
                                Effect = "자신이 잃은 생명력에 비례해 최대 100% 피해량 증가"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 4,
                                AtkCount = 2,
                                Cooldown = 104,
                                Ratio = 50,
                                DefRatio = 55,
                                LostHpBonusDmgMax = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "자신이 잃은 생명력에 비례해 최대 100% 피해량 증가, 강화: 버프 해제 2개[100%]"
                                }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "준비된 병략",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 자신: 방어력 증가 [상시] 31%
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 31 } }
                            },
                            Effect = "[상시][자신] 방어력 31%, 적 공격 3회 적중당할 시 방어력 40% 회복+방어력 45% 보호막[2턴]. [상시][아군 후열 만능형] 방호. [적군 3명] 아군 5회 적중당할 시 1485 관통 고정 피해+화상[35%,2턴] (방호/트리거 회복·고정피해는 계산기 미반영)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 39 } }
                            },
                            Effect = "[상시][자신] 방어력 39%, 적 공격 3회 적중당할 시 방어력 40% 회복+방어력 55% 보호막[2턴]. [상시][아군 후열 만능형] 방호. [적군 3명] 아군 5회 적중당할 시 1485 관통 고정 피해+화상[35%,2턴] (방호/트리거 회복·고정피해는 계산기 미반영)"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: [아군 후열 만능형] 자신이 적 공격 3회 적중당할 시 방어력 45% 보호막[2턴] — 계산기 미반영
                        {6, new PassiveTranscend{ Effect = "6초월: [아군 후열 만능형] 적 공격 3회 적중당할 시 시전자 방어력 45% 보호막[2턴]" }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            #endregion

            #region 전설 - 방어형 - 마법 426~

            #endregion
        
            #region 희귀 - 방어형 -물리 451~
    
            #endregion

            #region 희귀 - 방어형 - 마법 476~

            #endregion

            #region 희귀 - 공격형 - 물리 53~

            // 레오
            new Character
            {
                Id = 53,
                Name = "레오",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "단일 적군. 출혈[40%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "단일 적군. 출혈[40%][2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "무차별 살육",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 230,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "단일 적군. 방어력 감소[100%][3턴](29%), 출혈[100%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 270,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 39 } },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 방어력 감소 39%. 단일 적군. 출혈[100%][3턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "사자의 포효",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Dmg_Reduction = 11 } }
                                },
                                Effect = "적군 3명. 피해량 감소[100%][3턴](11%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Dmg_Reduction = 13 } }
                                },
                                Effect = "강화: 피해량 감소 13%. 적군 3명"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "2초월: 자신 흡혈[3턴] (피해량의 20% 생명력 회복)" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "야수의 근성",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.InstantDeath }, Duration = 2 } }
                            },
                            Effect = "[상시] 모든 아군 즉사 면역[2턴], 자신 기본 공격 1회 시 즉사 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.InstantDeath }, Duration = 2 } },
                                // 강화: 자신 모든 공격력 증가 24% [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 24, MagicAtk_Rate = 24 } },
                                // 6초월: 사망 시 생명력 80% 부활 (전투당 1회)
                                new PersistentEffect {
                                    Target = EffectTarget.Self,
                                    Type = PersistentEffectType.Revival,
                                    ApplyMode = ApplyMode.Triggered,
                                    TriggerCondition = TriggerCondition.SelfDeath,
                                    Revival = new Revival { ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "강화: 자신 모든 공격력 +24%[3턴]. 6초월: 사망 시 생명력 80%로 부활(전투당 1회)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 레이
            new Character
            {
                Id = 54,
                Name = "레이",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                Effect = "단일 적군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "단일 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "쾌속살법",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 47,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 40 }
                                },
                                Effect = "모든 적군. 중독[40%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 47,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 40 }
                                },
                                Effect = "강화: 관통 추가. 모든 적군. 중독[40%][2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "단도폭우",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 47,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "모든 적군. 출혈[40%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 2,
                                Cooldown = 80,
                                Ratio = 57,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "강화: 물리 공격력 57% 2회. 모든 적군. 출혈[40%][2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 55 }
                            }, Effect = "2초월: 각 공격마다 중독[55%][2턴]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "은신",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise, Stacks = 1, Duration = 2 },
                                // 자신 물리 공격력 증가 25% [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 25 } }
                            },
                            Effect = "[상시] 자신 위장[2턴], 물리 공격력 +25%[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.StatusAilment, StatusType = StatusEffectType.Disguise, Stacks = 1, Duration = 3 },
                                // 강화: 위장[3턴]. 6초월: 물리 공격력 증가 31%
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 31 } }
                            },
                            Effect = "강화: 위장[3턴]. 6초월: 물리 공격력 +31%[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 메이
            new Character
            {
                Id = 55,
                Name = "메이",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                Effect = "단일 적군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "단일 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "호신용 총",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 72,
                                Ratio = 95,
                                TargetSelector = TargetSelector.HighestAtkEnemy,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Duration = 3, Chance = 100 }
                                },
                                Effect = "공격력이 가장 높은 적군. 실명[100%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Cooldown = 72,
                                Ratio = 95,
                                TargetSelector = TargetSelector.HighestAtkEnemy,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Blind, Duration = 4, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 29 } }
                                },
                                Effect = "강화: 실명[4턴], 물리 취약[100%][5턴](받피 +29%). 공격력이 가장 높은 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "무차별 난사",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 98,
                                Ratio = 38,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Duration = 2, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 35 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 35 }
                                },
                                Effect = "적군 3명. 화상[35%][2턴], 중독[35%][2턴], 출혈[35%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 98,
                                Ratio = 38,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Duration = 2, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 40 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "강화: 화상·중독·출혈 확률 40%. 적군 3명"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effect = "6초월: 출혈·화상·중독 적용 횟수 2회" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "허세",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 아군 물리 피해량 증가 11% [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 11 } }
                            },
                            Effect = "[상시] 모든 아군 물리 피해량 +11%"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 강화: 물리 피해량 증가 15%
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_Type = 15 } },
                                // 2초월: 자신 물리 공격력 160% 보호막 [3턴] (라운드당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Shield_AtkRatio = 160 } }
                            },
                            Effect = "강화: 모든 아군 물리 피해량 +15%. 2초월: 자신 물리 공격력 160% 보호막[3턴](라운드당 1회)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 블랙로즈
            new Character
            {
                Id = 56,
                Name = "블랙로즈",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                Effect = "단일 적군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "단일 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "기습 침투",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effect = "단일 적군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 410,
                                Effect = "강화: 물리 공격력 410%. 단일 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "떨어지는 장미",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 125,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, Chance = 100, DispelBuffCount = 1 }
                                },
                                Effect = "모든 적군. 버프 해제 1개[100%]. 대상에게 해제 가능한 버프가 있어야 사용 가능"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 150,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, PreDamage = true, Chance = 100, DispelBuffCount = 1 }
                                },
                                Effect = "강화: 물리 공격력 150%. 모든 적군. 버프 해제 1개[100%]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Atk_Rate = 20, MagicAtk_Rate = 20 } }
                            }, Effect = "6초월: 모든 아군 모든 공격력 +20%[2턴]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "뛰어난 적응력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Petrify }, Duration = 2 } }
                            },
                            Effect = "[상시] 모든 아군 석화 면역[2턴], 자신 기본 공격 1회 시 석화 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Petrify }, Duration = 2 } },
                                // 2초월: 자신 물리 공격력 증가 31% [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Atk_Rate = 31 } }
                            },
                            Effect = "강화: 자신 스킬 1회 시 물리 공격력 90% 생명력 회복. 2초월: 자신 물리 공격력 +31%[상시]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 샤오
            new Character
            {
                Id = 57,
                Name = "샤오",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                Effect = "단일 적군"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "단일 적군"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정기흡공",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 78,
                                Ratio = 230,
                                ConditionalExtraDmg = 180,
                                ConditionalDesc = "대상이 [출혈] 상태일 경우 물리 공격력 180% 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 160 } }
                                },
                                Effect = "단일 적군. 모든 아군 시전자 물리 공격력 160% 보호막[2턴]. 대상 [출혈] 시 180% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 78,
                                Ratio = 270,
                                ConditionalExtraDmg = 180,
                                ConditionalDesc = "대상이 [출혈] 상태일 경우 물리 공격력 180% 추가 피해",
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 160 } }
                                },
                                Effect = "강화: 물리 공격력 270%. 모든 아군 보호막[2턴]. 대상 [출혈] 시 180% 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Debuff = new TimedDebuff { Def_Reduction = 39 }, Effect = "2초월: 단일 적군 방어력 감소[100%][5턴](39%)" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "혈강시 소환",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 170,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "단일 적군. 출혈[100%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 205,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 물리 공격력 205%. 단일 적군. 출혈[100%][3턴]"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "강시의 부적",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.InstantDeath }, Duration = 2 } },
                                // 자신 생명력 50% 이하 시 시전자 물리 공격력 40% 생명력 회복 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, TriggeredHealAtkRatio = 40 }
                            },
                            Effect = "[상시] 모든 아군 즉사 면역[2턴], 자신 기본 공격 1회 시 즉사 면역[2턴]. 자신 생명력 50% 이하 시 시전자 물리 공격력 40% 생명력 회복(전투당 1회)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.InstantDeath }, Duration = 2 } },
                                // 강화: 자신 생명력 50% 이하 시 시전자 물리 공격력 45% 생명력 회복 (전투당 1회)
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow, TriggerHpThreshold = 50, TriggeredHealAtkRatio = 45 },
                                // 6초월: 모든 아군 1인 공격기 피해량 증가 23% [상시]
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Dealt_1to3 = 23 } }
                            },
                            Effect = "강화: 생명력 회복량 물리 공격력 45%. 6초월: 모든 아군 1인 공격기 피해량 +23%"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 소이
            new Character
            {
                Id = 58,
                Name = "소이",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "단일 적군. 출혈[40%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 40 }
                                },
                                Effect = "단일 적군. 출혈[40%][2턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "심장 쏘기",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 315,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "단일 적군. 출혈[100%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 375,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 물리 공격력 375%. 단일 적군. 출혈[100%][3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 3 }
                            }, Effect = "6초월: 턴제 버프 감소 3턴[100%]" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "매 날리기",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "적군 3명(버프가 많은 순). 버프 해제 2개[100%]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "강화: 물리 공격력 145%. 적군 3명(버프가 많은 순). 버프 해제 2개[100%]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Effect = "2초월: 피해 대상이 1명 줄어들 때마다 대상 피해량 +15%" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "정찰",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                // 모든 아군 5인 공격기 감쇄 11% [상시] (5인기 받피감)
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc_Multi = 11 } },
                                // 자신 효과 적중 증가 31% [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 31 } }
                            },
                            Effect = "[상시] 모든 아군 5인 공격기 받피감 11%. 자신 효과 적중 +31%"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc_Multi = 15 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40 } }
                            },
                            Effect = "강화: 5인 공격기 받피감 15%, 효과 적중 +40%"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 스니퍼
            new Character
            {
                Id = 59,
                Name = "스니퍼",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "단일 적군" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "단일 적군" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "난사",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 240,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Duration = 3, Chance = 100 }
                                },
                                Effect = "단일 적군. 화상[100%][3턴](매턴 공격력 80%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 290,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Burn, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 290%. 화상[100%][3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend { Cooldown = 45, Effect = "2초월: 쿨타임 45초" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "사형 선고",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 340,
                                TargetMaxHpRatio = 20,
                                AtkCap = 1000,
                                Effect = "단일 적군. [사냥술] 4개 우선 소모하여 대상 최대 생명력 20% 추가 피해(공격력 1000% 제한)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 72,
                                Ratio = 410,
                                TargetMaxHpRatio = 20,
                                AtkCap = 1000,
                                Effect = "강화: 410%. 사냥술 4개 소모 → 대상 최대 생명력 20%(공격력 1000% 제한)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { TargetMaxHpRatio = 26, Effect = "6초월: 대상 최대 생명력 26%" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "사냥술",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.EnemyDeath, StacksPerTrigger = 1, MaxStacks = 4, IsPerStack = true, Buff = new BuffSet { Dmg_Dealt_Type = 11 } }
                            },
                            Effect = "자신: 적 1명 사망 시 사냥술 1중첩[최대 4중첩]. 사냥술(중첩) 주는 물리 피해량 +11%"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 물리 피해량 증가 13% (사냥술 중첩당)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.EnemyDeath, StacksPerTrigger = 1, MaxStacks = 4, IsPerStack = true, Buff = new BuffSet { Dmg_Dealt_Type = 13 } }
                            },
                            Effect = "강화: 사냥술 중첩당 주는 물리 피해량 +13%(최대 4중첩). 적 1명 사망 시 1중첩"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 제인
            new Character
            {
                Id = 60,
                Name = "제인",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 40 }
                                },
                                Effect = "단일 적군. 중독[40%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 40 }
                                },
                                Effect = "강화: 120%. 중독[40%][2턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 2, Chance = 50 }
                                },
                                Effect = "2초월: 중독 확률 50%" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "암습",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 60,
                                Ratio = 135,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 3, Chance = 70 }
                                },
                                Effect = "단일 적군 2회. 각 공격마다 중독[70%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Cooldown = 60,
                                Ratio = 162,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Duration = 3, Chance = 85 }
                                },
                                Effect = "강화: 162%, 중독 확률 85%. 각 공격마다 중독[3턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "급소 강타",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 285,
                                ConditionalExtraDmg = 190,
                                ConditionalDesc = "대상 [중독] 상태 시 물리 공격력 190% 추가 피해",
                                Effect = "단일 적군. 대상 중독 시 공격력 190% 추가 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 240,
                                ConditionalExtraDmg = 190,
                                ConditionalDesc = "대상 [중독] 상태 시 물리 공격력 190% 추가 피해",
                                Effect = "강화: 240%. 대상 중독 시 공격력 190% 추가 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend { ConditionalExtraDmg = 230, Effect = "6초월: 대상 중독 시 추가 피해 230%" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "은신",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "자신: 위장[2턴] (1인 공격 비대상, 피격 시 확정 빗나감)"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "강화: 위장 지속 3턴"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkEff
            },

            // 조운
            new Character
            {
                Id = 61,
                Name = "조운",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "단일 적군" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "단일 적군" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "천창투신각",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "단일 적군. 출혈[100%][3턴](매턴 공격력 60% 관통, 출혈 개수당 증가 최대 5)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 340%. 출혈[100%][3턴]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "회전 분열창",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 75 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Physical } }
                                },
                                Effect = "적군 3명. 출혈[75%][2턴]. 자신 물리 피해 면역[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Bleeding, Duration = 2, Chance = 85 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Physical } }
                                },
                                Effect = "강화: 출혈 확률 85%. 적군 3명. 자신 물리 피해 면역[2턴]"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "호위기세",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Blind }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 31 } }
                            },
                            Effect = "[상시] 모든 아군 실명 면역[2턴](기본공격 1회 시 재부여). 자신 효과 저항 +31%"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Blind }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Res = 40 } }
                            },
                            Effect = "강화: 효과 저항 +40%. 2초월: 스킬 1회 발동 시 물리 공격력 100% 보호막[3턴]. 6초월: 적 공격 적중당할 시 스킬 쿨타임 -8초"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 진
            new Character
            {
                Id = 62,
                Name = "진",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "단일 적군" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "단일 적군" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "진-파공권",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 120,
                                FixedDamage = 775,
                                Effect = "적군 3명. 고정 피해 775"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 84,
                                Ratio = 145,
                                FixedDamage = 1100,
                                Effect = "강화: 145%, 고정 피해 1100. 적군 3명"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "극한 연참권",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 98,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Duration = 2, Chance = 55 }
                                },
                                Effect = "적군 3명. 기절[55%][2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 98,
                                Ratio = 175,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Duration = 2, Chance = 65 }
                                },
                                Effect = "강화: 175%, 기절 확률 65%. 적군 3명"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "수련의 성과",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Silence }, Duration = 2 } }
                            },
                            Effect = "[상시] 모든 아군 침묵 면역[2턴](기본공격 1회 시 재부여)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Silence }, Duration = 2 } },
                                // 강화: 자신 치명타 확률 증가 27% [상시]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } },
                                // 6초월: 자신 물리 공격력 증가 31% [3턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 31 } }
                            },
                            Effect = "강화: 자신 치명타 확률 +27%[상시]. 2초월: 생명력 50% 이하 시 물리 공격력 160% 보호막[3턴](전투당 1회). 6초월: 물리 공격력 +31%[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 캐티
            new Character
            {
                Id = 63,
                Name = "캐티",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "적군 1명" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "적군 1명" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "마구 할퀴기",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 230,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 4, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = "적군 1명. 회복 불가[100%][4턴]. 물리 취약[100%][5턴](22%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 270,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Duration = 4, Chance = 100 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Phys_Dmg_Taken_Increase = 22 } }
                                },
                                Effect = "강화: 270%. 회복 불가[4턴], 물리 취약 22%[5턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Blk_Red = 33 } }
                                },
                                Effect = "2초월: 막기 확률 감소 33%[100%][5턴]" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "약점 찾기",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 0,
                                Cooldown = 64,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Cri = 21 } }
                                },
                                Effect = "모든 아군. 치명타 확률 증가[3턴](21%)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 0,
                                Cooldown = 64,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Cri = 27 } }
                                },
                                Effect = "강화: 치명타 확률 +27%[3턴]. 모든 아군"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Wek = 32 } }
                                },
                                Effect = "6초월: 약점 공격 확률 증가 32%[3턴]" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "날카로운 손톱",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 25 } }
                            },
                            Effect = "자신 물리 공격력 +25%[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 31 } }
                            },
                            Effect = "강화: 물리 공격력 +31%[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 헤브니아
            new Character
            {
                Id = 64,
                Name = "헤브니아",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
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
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Duration = 1, Chance = 45 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "단일 적군. 빙결[45%][1턴](해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 100,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Duration = 1, Chance = 50 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "강화: 빙결 확률 50%. 단일 적군(빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "검의 심판",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Duration = 2, Chance = 75 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "적군 3명. 빙결[75%][2턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Duration = 2, Chance = 75 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "강화: 145%. 적군 3명. 빙결[75%][2턴](빙결 해제 시 대상 최대 생명력 40% 방무 피해, 공격력 300% 제한)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Duration = 2, Chance = 85 }
                                },
                                Effect = "2초월: 빙결 확률 85%" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "맹렬한 공격",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = "단일 적군. 대상 턴제 버프 -2턴[100%]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = "강화: 340%. 대상 턴제 버프 -2턴[100%]"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "마법 포착",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 25 } }
                            },
                            Effect = "자신 물리 공격력 +25%[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Duration = 3, Buff = new BuffSet { Atk_Rate = 31 } },
                                // 6초월: 모든 피해 면역[2턴]
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.All } }
                            },
                            Effect = "강화: 물리 공격력 +31%[3턴]. 6초월: 모든 피해 면역[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 호킨
            new Character
            {
                Id = 65,
                Name = "호킨",
                Grade = "희귀",
                Type = "공격형",
                AttackType = AttackType.Physical,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "단일 적군" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "단일 적군" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "무기 투척",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 54,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Duration = 3, Chance = 85 }
                                },
                                Effect = "단일 적군. 기절[85%][3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 54,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Duration = 3, Chance = 85 }
                                },
                                Effect = "강화: 340%. 기절[85%][3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 39 } }
                                },
                                Effect = "2초월: 방어력 감소 39%[100%][5턴]" } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "민첩한 라이더",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 0,
                                Cooldown = 48,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Physical } }
                                },
                                Effect = "자신 물리 피해 면역[2턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 0,
                                Cooldown = 48,
                                Ratio = 0,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 3, DamageNullification = new DamageNullification { Duration = 3, Type = DamageNullType.Physical } }
                                },
                                Effect = "강화: 물리 피해 면역 지속 3턴"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Dmg_Dealt_Type = 33 } }
                                },
                                Effect = "6초월: 주는 물리 피해량 +33%[5턴]" } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "투쟁심",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Condition = "생명력 50% 이하", Buff = new BuffSet { Atk_Rate = 25 } }
                            },
                            Effect = "[상시] 생명력 50% 이하 시 물리 공격력 +25%"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, IsConditional = true, Condition = "생명력 50% 이하", Buff = new BuffSet { Atk_Rate = 31 } }
                            },
                            Effect = "강화: 생명력 50% 이하 시 물리 공격력 +31%"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            #endregion

            #region 희귀 - 마법형 - 마법 154~

            // 링링
            new Character
            {
                Id = 154,
                Name = "링링",
                Grade = "희귀",
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
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 자신 마법 공격력의 45%만큼 생명력 회복
                            { 2, new SkillTranscend {
                                HealAtkRatio = 45,
                                Effect = "2초월: 자신 마법 공격력 45% 생명력 회복"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "기공탄",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 7,
                                Cooldown = 60,
                                Ratio = 30,
                                // 피해량 감소: 주는 피해량 15% 감소 [100% 확률] [5턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Dmg_Reduction = 15 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 7,
                                Cooldown = 60,
                                Ratio = 30,
                                // 강화: 피해량 감소 20%
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Dmg_Reduction = 20 } }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "폭룡승천각",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 5,
                                Cooldown = 70,
                                Ratio = 20,
                                IgnoresTurnDamageImmunity = true,
                                // 대상의 턴제 버프 감소 2턴 [100% 확률]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 5,
                                Cooldown = 70,
                                Ratio = 25,
                                IgnoresTurnDamageImmunity = true,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = "관통(피해 면역 무시)"
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "순기흡공",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 아군 효과 적중 증가 19% [상시]
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 19 } }
                            },
                            Effect = ""
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 효과 적중 증가 25%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 25 } }
                            },
                            Effect = ""
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: 스킬 1회 발동 시 시전자 마법 공격력 45% 보호막 [2턴] (보호막은 데미지 비관여)
                        { 6, new PassiveTranscend {
                            Effect = "6초월: 스킬 1회 발동 시 시전자 마법 공격력 45% 보호막[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 베인
            new Character
            {
                Id = 155,
                Name = "베인",
                Grade = "희귀",
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
                        Name = "마력 분출",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 285,
                                // 침묵 [90% 확률] [3턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 90, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 90, Duration = 3 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 물리 공격력 감소 33% [100% 확률] [5턴] (데미지 계산 무관 → Effect 텍스트)
                            { 6, new SkillTranscend {
                                Effect = "6초월: 대상 물리 공격력 33% 감소[5턴]"
                            }}
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
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 120,
                                // 침묵 [65% 확률] [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 65, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Silence, Stacks = 1, Chance = 65, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "죽음 극복",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신 사망 시 불사 상태로 부활 [2턴] (전투당 1회)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 2, ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = ""
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 불사 지속 3턴
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 3, ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = ""
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 2초월: 효과 적중 증가 40% [상시]
                        { 2, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Eff_Hit = 40 } }
                            },
                            Effect = "2초월: 효과 적중 증가 40%"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 세라
            new Character
            {
                Id = 156,
                Name = "세라",
                Grade = "희귀",
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
                        Name = "망령의 웃음",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 60,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 2,
                                Cooldown = 70,
                                Ratio = 72,
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 치명타 발생 시 각 공격마다 마법 공격력의 32% 추가 피해 (타격당)
                            { 2, new SkillTranscend {
                                Bonus = new BuffSet { CriBonusDmg = 32, CriBonusDmgPerHit = true },
                                Effect = "2초월: 치명타 시 타격당 마법 공격력 32% 추가 피해"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "망령의 그림자",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                // 대상의 턴제 버프 감소 2턴 [100% 확률]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 175,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffTurnReduction, Chance = 100, TurnReduction = 2 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 발생 시 마법 공격력의 80% 추가 피해
                            { 6, new SkillTranscend {
                                Bonus = new BuffSet { CriBonusDmg = 80 },
                                Effect = "6초월: 치명타 시 마법 공격력 80% 추가 피해"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "천둥의 그림자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신 치명타 확률 증가 21% [상시]
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 21 } }
                            },
                            Effect = ""
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 치명타 확률 증가 27%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri = 27 } }
                            },
                            Effect = ""
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 실비아
            new Character
            {
                Id = 157,
                Name = "실비아",
                Grade = "희귀",
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
                        Name = "영혼의 숨결",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 150,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통(피해 면역 무시)"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 180,
                                IgnoresTurnDamageImmunity = true,
                                Effect = "관통(피해 면역 무시)"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 방어 무시 (대상 방어력 40% 무시)
                            { 2, new SkillTranscend {
                                Bonus = new BuffSet { Arm_Pen = 40 },
                                Effect = "2초월: 방어 무시 40%"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "쇠약의 저주",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 95,
                                // 방어력 감소 18% [100% 확률] [3턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 18 } }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 3, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 18 } }
                                },
                                Effect = ""
                                } }
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "빛나는 악령",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 자신 마법 공격력 증가 25% [3턴] + 사망 시 불사 부활 [2턴] (전투당 1회)
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 25 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 2, ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "마법 공격력 증가[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 마법 공격력 증가 31%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 31 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 2, ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "마법 공격력 증가[3턴]"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: 불사 지속 3턴
                        { 6, new PassiveTranscend {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 3, ReviveHp = 1, OncePerBattle = true } }
                            },
                            Effect = "6초월: 불사 지속 3턴"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 조커
            new Character
            {
                Id = 158,
                Name = "조커",
                Grade = "희귀",
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
                                AtkCount = 2,
                                Ratio = 50,
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 2,
                                Ratio = 60,
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "풀하우스",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 7,
                                Cooldown = 60,
                                Ratio = 28,
                                // 기절 [85% 확률] [3턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85, Duration = 3 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 7,
                                Cooldown = 60,
                                Ratio = 34,
                                // 강화: 기절 확률 100%
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 기절 지속 4턴
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100, Duration = 4 }
                                },
                                Effect = "2초월: 기절 지속 4턴"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "마술 모자",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 70,
                                Ratio = 40,
                                // 버프 해제 2개 [100% 확률] (버프 많은 순 대상)
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "버프 많은 순 대상"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 3,
                                Cooldown = 70,
                                Ratio = 48,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetSelector = TargetSelector.MostBuffsEnemy, Type = SkillEffectType.BuffDispel, Chance = 100, DispelBuffCount = 2 }
                                },
                                Effect = "버프 많은 순 대상"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 감소 23% [100% 확률] [3턴]
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Chance = 100, Duration = 3, Debuff = new DebuffSet { Cri_Reduction = 23 } }
                                },
                                Effect = "6초월: 대상 치명타 확률 23% 감소[100%][3턴]"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "행운의 동전",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 아군 치명타 피해 증가 19% [상시]
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 19 } }
                            },
                            Effect = ""
                        }},
                        { 1, new PassiveLevelData {
                            // 강화: 치명타 피해 증가 25%
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Cri_Dmg = 25 } }
                            },
                            Effect = ""
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            // 클레오
            new Character
            {
                Id = 159,
                Name = "클레오",
                Grade = "희귀",
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
                                // 중독 [40% 확률] [2턴] (매 턴 대상 최대HP 6%, 공격력 150% 상한)
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 40, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "맹독",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 120,
                                // 중독 [75% 확률] [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 75, Duration = 2 }
                                },
                                Effect = ""
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 75, Duration = 2 }
                                },
                                Effect = ""
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 물리 공격력 감소 25% [100% 확률] [3턴] (데미지 무관 → Effect 텍스트)
                            { 6, new SkillTranscend {
                                Effect = "6초월: 대상 물리 공격력 25% 감소[3턴]"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "시선",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 120,
                                // 석화 [50% 확률] [2턴]
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 50, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "석화 해제 시 시전자 공격력의 120% 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                Ratio = 145,
                                // 강화: 석화 확률 60%
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Petrify, AtkRatio = 120 },
                                Effect = "강화: 석화 확률 60%. 석화 해제 시 시전자 공격력의 120% 피해"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 중독 [85% 확률] [2턴] 추가
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Petrify, Stacks = 1, Chance = 60, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Poison, Stacks = 1, Chance = 85, Duration = 2 }
                                },
                                Effect = "2초월: 중독[85% 확률][2턴] 추가"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "아테나의 저주",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            // 모든 아군 침묵 면역 [2턴] + 자신 기본공격 1회 발동 시 침묵 면역 [2턴]
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Silence }, Duration = 2 } }
                            },
                            Effect = "자신 기본공격 1회 발동 시 침묵 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            // 강화[자신]: 마법 공격력 증가 31% [3턴]
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Silence }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { MagicAtk_Rate = 31 } }
                            },
                            Effect = "자신 기본공격 1회 발동 시 침묵 면역[2턴], 마법 공격력 증가[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkCri
            },

            #endregion

            #region 희귀 - 지원형 - 물리 277~

            // 루시
            new Character
            {
                Id = 277,
                Name = "루시",
                Grade = "희귀",
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
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 9 }, Duration = 2, Chance = 100 }
                                },
                                Effect = "단일 적군 마법 공격력 50% + 방어력 60% 1회 피해, 물리 공격력 감소 9%[2턴][100%]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 50,
                                DefRatio = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Atk_Reduction = 11 }, Duration = 2, Chance = 100 }
                                },
                                Effect = "강화: 물리 공격력 감소 11%[2턴][100%]"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "신비의 춤사위",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 95,
                                HealDefRatio = 100,
                                Effect = "[모든 아군] 시전자 방어력 100% 생명력 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 95,
                                HealDefRatio = 120,
                                Effect = "강화: [모든 아군] 시전자 방어력 120% 생명력 회복"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 아군 후열(마법형) 스킬 쿨타임 감소 15초
                            { 6, new SkillTranscend {
                                Effect = "6초월: [아군 후열(마법형)] 스킬 쿨타임 감소 15초"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "신내림",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 18 }, Duration = 3 }
                                },
                                Effect = "[모든 아군] 디버프 해제 2개, 방어력 증가 18%[3턴]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 22 }, Duration = 3 }
                                },
                                Effect = "강화: [모든 아군] 방어력 증가 22%[3턴], 디버프 해제 2개"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 디버프 해제 3개
                            { 2, new SkillTranscend {
                                Effect = "2초월: 디버프 해제 3개"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "영적능력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Poison }, Duration = 2 } }
                            },
                            Effect = "[모든 아군] 중독 면역[2턴] + 자신 기본 공격 1회 발동 시 중독 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Poison }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, TriggeredHealDefRatio = 92 }
                            },
                            Effect = "강화: [모든 아군] 중독 면역[2턴]. [자신] 스킬 1회 발동 시 시전자 방어력 92% 생명력 회복"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            #endregion

            #region 희귀 - 지원형 - 마법 252~

            // 사라
            new Character
            {
                Id = 252,
                Name = "사라",
                Grade = "희귀",
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
                                Effect = "단일 적군 마법 공격력 100% 1회 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "강화: 마법 공격력 120%"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "악마의 유혹",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Ratio = 95,
                                Cooldown = 72,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 20 }, Duration = 3, Chance = 100 }
                                },
                                Effect = "적군 3명 마법 공격력 95% 1회 피해, 방어력 감소 20%[3턴][100%]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Ratio = 95,
                                Cooldown = 72,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Def_Reduction = 24 }, Duration = 3, Chance = 100 }
                                },
                                Effect = "강화: 방어력 감소 24%[3턴][100%]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 피해량 감소 13%[3턴][100%]
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, TargetCount = 3, Type = SkillEffectType.Debuff, Debuff = new DebuffSet { Dmg_Reduction = 13 }, Duration = 3, Chance = 100 }
                                },
                                Effect = "2초월: [적군 3명] 피해량 감소 13%[3턴][100%]"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "악마의 키스",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 63,
                                Effect = "[아군 3명] 디버프 해제 2개"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 63,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, TargetCount = 3, Type = SkillEffectType.Buff, Buff = new BuffSet { Def_Rate = 34 }, Duration = 3 }
                                },
                                Effect = "강화: [아군 3명] 디버프 해제 2개, 방어력 증가 34%[3턴]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 대상 수 변경 아군 전체
                            { 6, new SkillTranscend {
                                TargetCountOverride = 5,
                                Effect = "6초월: 대상 수 변경 아군 전체"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "악마의 계약",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 12 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Sleep }, Duration = 2 } }
                            },
                            Effect = "[자신] 감쇄(받는 피해 감소 12%)[상시]. [모든 아군] 수면 면역[2턴] + 자신 기본 공격 1회 발동 시 수면 면역[2턴]. [자신] 생명력 30% 이하 시 디버프 해제 2개 (전투당 1회) + 최대 생명력 10% 회복 (전투당 1회)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Dmg_Rdc = 16 } },
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Sleep }, Duration = 2 } }
                            },
                            Effect = "강화: [자신] 감쇄(받는 피해 감소 16%)[상시]. [모든 아군] 수면 면역[2턴]. [자신] 생명력 30% 이하 시 디버프 해제 2개 + 최대 생명력 10% 회복 (각 전투당 1회)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 이주희
            new Character
            {
                Id = 253,
                Name = "이주희",
                Grade = "희귀",
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
                                Effect = "단일 적군 마법 공격력 100% 1회 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "강화: 마법 공격력 120%"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "자애의 마음",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                HealAtkRatio = 85,
                                Effect = "[아군 3명] 시전자 마법 공격력 85% 생명력 회복. [아군 후열] 디버프 해제 1개"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 70,
                                HealAtkRatio = 100,
                                Effect = "강화: [아군 3명] 시전자 마법 공격력 100% 생명력 회복. [아군 후열] 디버프 해제 1개"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 디버프 해제 2개
                            { 2, new SkillTranscend {
                                Effect = "2초월: [아군 후열] 디버프 해제 2개"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "치유의 손길",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 98,
                                HealAtkRatio = 85,
                                Effect = "[아군 3명] 시전자 마법 공격력 85% 생명력 회복"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 3,
                                AtkCount = 1,
                                Cooldown = 98,
                                HealAtkRatio = 100,
                                Effect = "강화: [아군 3명] 시전자 마법 공격력 100% 생명력 회복"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 쿨타임 75초
                            { 6, new SkillTranscend {
                                Cooldown = 75,
                                Effect = "6초월: 스킬 쿨타임 75초"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "겁쟁이의 용기",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Bleeding }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, Duration = 3, Buff = new BuffSet { Shield_AtkRatio = 115 } }
                            },
                            Effect = "[모든 아군] 출혈 면역[2턴] + 자신 기본 공격 1회 발동 시 출혈 면역[2턴]. [자신] 스킬 1회 발동 시 시전자 마법 공격력 115% 보호막[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Bleeding }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, Duration = 3, Buff = new BuffSet { Shield_AtkRatio = 135 } }
                            },
                            Effect = "강화: [자신] 스킬 1회 발동 시 시전자 마법 공격력 135% 보호막[3턴]. [모든 아군] 출혈 면역[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 카린
            new Character
            {
                Id = 254,
                Name = "카린",
                Grade = "희귀",
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
                                Effect = "단일 적군 마법 공격력 100% 1회 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "강화: 마법 공격력 120%"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "회복",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 83,
                                HealAtkRatio = 200,
                                Effect = "[단일 아군] 시전자 마법 공격력 200% 생명력 회복, 디버프 해제 2개"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 83,
                                HealAtkRatio = 230,
                                Effect = "강화: [단일 아군] 시전자 마법 공격력 230% 생명력 회복, 디버프 해제 3개"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 쿨타임 61초
                            { 2, new SkillTranscend {
                                Cooldown = 61,
                                Effect = "2초월: 스킬 쿨타임 61초"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "소생",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Revive, TargetCount = 1, ReviveHpPercent = 50 } },
                                Effect = "[단일 아군] 사망한 대상을 생명력 50%로 부활"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Revive, TargetCount = 1, ReviveHpPercent = 70 } },
                                Effect = "강화: [단일 아군] 사망한 대상을 생명력 70%로 부활"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 대상 수 변경 아군 2명
                            { 6, new SkillTranscend {
                                TargetCountOverride = 2,
                                Effect = "6초월: 대상 수 변경 아군 2명"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "축복의 소생",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 0, HitCount = 0, ReviveHpPercent = 65, OncePerBattle = true } }
                            },
                            Effect = "[자신] 사망 시 생명력 65%로 부활 (전투당 1회)"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Revival, Revival = new Revival { ImmortalTurns = 0, HitCount = 0, ReviveHpPercent = 80, OncePerBattle = true } }
                            },
                            Effect = "강화: [자신] 사망 시 생명력 80%로 부활 (전투당 1회)"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            // 클로에
            new Character
            {
                Id = 255,
                Name = "클로에",
                Grade = "희귀",
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
                                Effect = "단일 적군 마법 공격력 100% 1회 피해"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Ratio = 120,
                                Effect = "강화: 마법 공격력 120%"
                                } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "고양이의 은혜",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Ratio = 90,
                                Cooldown = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Chance = 100, Duration = 4 }
                                },
                                Effect = "단일 적군 마법 공격력 90% 3회 피해, 회복 불가[4턴][100%]"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 3,
                                Ratio = 108,
                                Cooldown = 60,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HealBlock, Chance = 100, Duration = 4 }
                                },
                                Effect = "강화: 마법 공격력 108% 3회 피해, 회복 불가[4턴][100%]"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 버프 해제 2개[100%]
                            { 2, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.BuffDispel, DispelBuffCount = 2, Chance = 100 }
                                },
                                Effect = "2초월: 버프 해제 2개[100%]"
                            }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "청소 시간",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Atk_Rate = 15, MagicAtk_Rate = 15 }, Duration = 3 }
                                },
                                Effect = "[모든 아군] 모든 공격력 증가 15%[3턴], 디버프 해제 2개"
                                } },
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Atk_Rate = 20, MagicAtk_Rate = 20 }, Duration = 3 }
                                },
                                Effect = "강화: [모든 아군] 모든 공격력 증가 20%[3턴], 디버프 해제 2개"
                                } }
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 치명타 확률 증가 27%[3턴]
                            { 6, new SkillTranscend {
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Buff = new BuffSet { Cri = 27 }, Duration = 3 }
                                },
                                Effect = "6초월: [모든 아군] 치명타 확률 증가 27%[3턴]"
                            }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "고양이 춤",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Shock }, Duration = 2 } }
                            },
                            Effect = "[모든 아군] 감전 면역[2턴] + 자신 기본 공격 1회 발동 시 감전 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Immunity, StatusImmunity = new StatusImmunity { Types = new[] { StatusEffectType.Shock }, Duration = 2 } },
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal, ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.SkillOnly, TriggerCount = 1, TriggeredHealHpRatio = 22 }
                            },
                            Effect = "강화: [모든 아군] 감전 면역[2턴]. [자신] 스킬 1회 발동 시 최대 생명력 22% 회복"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            #endregion

            #region 희귀 - 만능형 - 마법 377~

            // 라니아
            new Character
            {
                Id = 377,
                Name = "라니아",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "얼음 파편",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "빙결[100%][3턴], 빙결 해제 시 대상 최대생명력 40% 방어무시(방무40%) 피해, 상한 시전자 공격력 300%"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 340,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 100, Duration = 3 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "빙결[100%][3턴], 빙결 해제 시 대상 최대생명력 40% 방어무시(방무40%) 피해, 상한 시전자 공격력 300%"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: [자신] 마법 공격력 증가 35% [5턴]
                            {6, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { MagicAtk_Rate = 35 } }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "블리자드",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 95,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "모든 적군 빙결[60%][2턴], 빙결 해제 시 대상 최대생명력 40% 방어무시(방무40%) 피해, 상한 시전자 공격력 300%"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 115,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 60, Duration = 2 }
                                },
                                CleanseExplosion = new CleanseExplosion { TargetStatus = StatusEffectType.Freeze, TargetMaxHpRatio = 40, ArmorPen = 40, AtkCap = 300 },
                                Effect = "모든 적군 빙결[60%][2턴], 빙결 해제 시 대상 최대생명력 40% 방어무시(방무40%) 피해, 상한 시전자 공격력 300%"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 빙결 확률 60% → 70%
                            {2, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Freeze, Stacks = 1, Chance = 70, Duration = 2 }
                            }}}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "설원의 지배자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "[모든 아군] 빙결 면역[2턴], 자신 기본공격 1회 발동 시 빙결 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "[모든 아군] 빙결 면역[2턴], 자신 기본공격 1회 발동 시 빙결 면역[2턴]. 강화: [자신] 효과 적중 증가 46%[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkWek
            },

            // 아수라
            new Character
            {
                Id = 378,
                Name = "아수라",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 100, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 120, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "정령의 대검",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 4, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } }
                                },
                                Effect = "집중 공격[100%][4턴] (Taunt 류, 모델 미지원), 방어력 감소 29%[100%][4턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 88,
                                Ratio = 285,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 4, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } }
                                },
                                Effect = "강화: 집중 공격[100%][5턴], 방어력 감소 29%[100%][4턴] (집중 공격 = 도발류, 모델 미지원)"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 집중 공격 지속 5턴 (모델 미지원) / 6초월: 방어력 감소 지속 5턴
                            {2, new SkillTranscend{ Effect = "2초월: 집중 공격 지속 5턴 (모델 미지원)" }},
                            {6, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5, Chance = 100, Debuff = new DebuffSet { Def_Reduction = 29 } }
                            }, Effect = "6초월: 방어력 감소 지속 5턴" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "정령의 가호",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 85 } }
                                },
                                Effect = "모든 아군 보호막 시전자 마법공 85%[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_AtkRatio = 100 } }
                                },
                                Effect = "모든 아군 보호막 시전자 마법공 100%[2턴]"
                                }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "지혜의 눈동자",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 70 } }
                            },
                            Effect = "[자신] 약점 공격 확률 증가 70%[상시], 스킬 1회 발동 시 시전자 마법공 115% 보호막[3턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Wek = 80 } }
                            },
                            Effect = "[자신] 약점 공격 확률 증가 80%[상시], 스킬 1회 발동 시 시전자 마법공 135% 보호막[3턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.AtkDmgRdc
            },

            #endregion

            #region 희귀 - 방어형 - 물리 451~

            // 라쿤
            new Character
            {
                Id = 451,
                Name = "라쿤",
                Grade = "희귀",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 50, DefRatio = 60, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 60, DefRatio = 70, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "강철의 피부",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 45,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Def_Rate = 34 } }
                                },
                                Effect = "[자신] 방어력 증가 34%[5턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 45,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Def_Rate = 44 } }
                                },
                                Effect = "[자신] 방어력 증가 44%[5턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: [자신] 효과 저항 증가 55%[5턴]
                            {2, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Eff_Res = 55 } }
                            }}}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "오크킹의 함성",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 48,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Blk = 37 } }
                                },
                                Effect = "[자신] 도발[2턴] + 막기 확률 증가 37%[5턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 48,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 5, Buff = new BuffSet { Blk = 45 } }
                                },
                                Effect = "[자신] 도발[2턴] + 막기 확률 증가 45%[5턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: [자신] 방어력의 310%만큼 보호막[3턴]
                            {6, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.Buff, Duration = 3, Buff = new BuffSet { Shield_DefRatio = 310 } }
                            }, Effect = "6초월: [자신] 방어력의 310% 보호막[3턴]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "불굴의 오크",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Phys_Dmg_Rdc = 19 } }
                            },
                            Effect = "[자신] 물리 감쇄 19%[상시]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Phys_Dmg_Rdc = 24 } }
                            },
                            Effect = "[자신] 물리 감쇄 24%[상시]"
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 리
            new Character
            {
                Id = 452,
                Name = "리",
                Grade = "희귀",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 50, HpRatio = 12, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 60, HpRatio = 14, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "금강불괴",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Physical } },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 }
                                },
                                Effect = "[자신] 물리 피해 면역[2턴] + 도발[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 3, DamageNullification = new DamageNullification { Duration = 3, Type = DamageNullType.Physical } },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 }
                                },
                                Effect = "강화: [자신] 물리 피해 면역[3턴] + 도발[2턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: 쿨타임 90 → 68초
                            {6, new SkillTranscend{ Cooldown = 68 }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "패도멸악권",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Ratio = 120,
                                HpRatio = 29,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 28 }
                                },
                                Effect = "생명력 전환 28% (현재 생명력보다 높게는 전환 안 됨), 물리공 120% + 시전자 최대생명력 29% 피해"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 90,
                                Ratio = 120,
                                HpRatio = 29,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.HpConversion, Stacks = 1, Chance = 100, CustomHpConversionRatio = 39 }
                                },
                                Effect = "강화: 생명력 전환 39%, 물리공 120% + 시전자 최대생명력 29% 피해"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 쿨타임 90 → 68초
                            {2, new SkillTranscend{ Cooldown = 68 }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "응보의 진언",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effect = "[자신] 반격[29% 확률][상시] (반격 시 물리공 50% + 시전자 최대생명력 12% 피해 + 기절[35%][1턴]), 자신 생명력 50% 이하 시 시전자 최대생명력 29% 회복(전투당 1회)"
                        }},
                        { 1, new PassiveLevelData {
                            Effect = "강화: [자신] 반격[38% 확률][상시] (반격 시 물리공 50% + 시전자 최대생명력 12% 피해 + 기절[35%][1턴]), 자신 생명력 50% 이하 시 시전자 최대생명력 29% 회복(전투당 1회)"
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 에반
            new Character
            {
                Id = 453,
                Name = "에반",
                Grade = "희귀",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 50, DefRatio = 60, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 60, DefRatio = 70, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "연속 공격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 86, Duration = 3 }
                                },
                                Effect = "기절[86%][3턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 86, Duration = 4 }
                                },
                                Effect = "강화: 기절[86%][4턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 물리공 170% 방어력 200%, 기절 확률 100%
                            {2, new SkillTranscend{ Bonus = new BuffSet{ }, Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100, Duration = 4 }
                            }, Effect = "2초월: 물리공 170% 방어력 200% (배율 상승, Ratio/DefRatio override 미지원-원본 145/165 유지), 기절 확률 100%" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "균형의 방패",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_DefRatio = 110 } }
                                },
                                Effect = "모든 아군 보호막 시전자 방어력 110%[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_DefRatio = 130 } }
                                },
                                Effect = "강화: 모든 아군 보호막 시전자 방어력 130%[2턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: [모든 아군] 막기 확률 증가 21%[3턴]
                            {6, new SkillTranscend{ PartyBuff = new TimedBuff{ Blk = 21 }, Effect = "6초월: [모든 아군] 막기 확률 증가 21%[3턴]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "의지",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 13 } }
                            },
                            Effect = "[모든 아군] 방어력 증가 13%[상시] + 기절 면역[2턴], 자신 기본공격 1회 발동 시 기절 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 18 } }
                            },
                            Effect = "[모든 아군] 방어력 증가 18%[상시] + 기절 면역[2턴], 자신 기본공격 1회 발동 시 기절 면역[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 유진호
            new Character
            {
                Id = 454,
                Name = "유진호",
                Grade = "희귀",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 50, DefRatio = 60, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 60, DefRatio = 70, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "의지의 일격",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85, Duration = 3 }
                                },
                                Effect = "기절[85%][3턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 60,
                                Ratio = 145,
                                DefRatio = 165,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 85, Duration = 4 }
                                },
                                Effect = "강화: 기절[85%][4턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: 물리공 170% 방어력 200%, 기절 확률 100%
                            {2, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Stun, Stacks = 1, Chance = 100, Duration = 4 }
                            }, Effect = "2초월: 물리공 170% 방어력 200% (배율 상승, Ratio/DefRatio override 미지원-원본 145/165 유지), 기절 확률 100%" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "황금 방패",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_DefRatio = 110 } }
                                },
                                Effect = "모든 아군 보호막 시전자 방어력 110%[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 114,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.Buff, Duration = 2, Buff = new BuffSet { Shield_DefRatio = 130 } }
                                },
                                Effect = "강화: 모든 아군 보호막 시전자 방어력 130%[2턴]"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 6초월: [모든 아군] 감쇄 12%[3턴] = 받피감 12%
                            {6, new SkillTranscend{ PartyBuff = new TimedBuff{ Dmg_Rdc = 12 }, Effect = "6초월: [모든 아군] 감쇄(받피감) 12%[3턴]" }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "강철 근성",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 13 } }
                            },
                            Effect = "[모든 아군] 방어력 증가 13%[상시] + 침묵 면역[2턴], 자신 기본공격 1회 발동 시 침묵 면역[2턴]"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Party, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 18 } }
                            },
                            Effect = "[모든 아군] 방어력 증가 18%[상시] + 침묵 면역[2턴], 자신 기본공격 1회 발동 시 침묵 면역[2턴]"
                        }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

            // 헬레니아
            new Character
            {
                Id = 455,
                Name = "헬레니아",
                Grade = "희귀",
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
                            { 0, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 50, DefRatio = 60, Effect = "" } },
                            { 1, new SkillLevelData { TargetCount = 1, AtkCount = 1, Ratio = 60, DefRatio = 70, Effect = "" } }
                        }
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "하늘의 빛",
                        SkillType = SkillType.Skill1,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 50,
                                DefRatio = 55,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.DebuffCleanse, DispelDebuffCount = 1 } },
                                Effect = "모든 적군 물리공 50% + 방어력 55% 피해, [아군] 디버프 해제 1개"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 5,
                                AtkCount = 1,
                                Cooldown = 80,
                                Ratio = 50,
                                DefRatio = 55,
                                Effects = new List<SkillEffect> { new SkillEffect { Target = EffectTarget.Party, Type = SkillEffectType.DebuffCleanse, DispelDebuffCount = 2 } },
                                Effect = "강화: 모든 적군 물리공 50% + 방어력 55% 피해, [아군] 디버프 해제 2개"
                                }}
                        },
                        TranscendBonuses = new Dictionary<int, SkillTranscend>
                        {
                            // 2초월: [모든 적군] 빗나감 확률 증가 40%[100%][3턴] = Miss 상태이상
                            {2, new SkillTranscend{ Effects = new List<SkillEffect>
                            {
                                new SkillEffect { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Miss, Stacks = 1, Chance = 100, Duration = 3 }
                            }, Effect = "2초월: [모든 적군] 빗나감 확률 증가 40%[100%][3턴]" }}
                        }
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "하늘의 방패",
                        SkillType = SkillType.Skill2,
                        LevelData = new Dictionary<int, SkillLevelData>
                        {
                            { 0, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 2, DamageNullification = new DamageNullification { Duration = 2, Type = DamageNullType.Magic } },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 }
                                },
                                Effect = "[자신] 마법 피해 면역[2턴] + 도발[2턴]"
                                }},
                            { 1, new SkillLevelData {
                                TargetCount = 1,
                                AtkCount = 1,
                                Cooldown = 66,
                                Effects = new List<SkillEffect>
                                {
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.DamageNullification, Duration = 3, DamageNullification = new DamageNullification { Duration = 3, Type = DamageNullType.Magic } },
                                    new SkillEffect { Target = EffectTarget.Self, Type = SkillEffectType.StatusAilment, StatusType = StatusEffectType.Taunt, Duration = 2 }
                                },
                                Effect = "강화: [자신] 마법 피해 면역[3턴] + 도발[2턴]"
                                }}
                        }
                    }
                },
                Passive = new Passive
                {
                    Name = "발키리의 저력",
                    LevelData = new Dictionary<int, PassiveLevelData>
                    {
                        { 0, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 22 } }
                            },
                            Effect = "[자신] 방어력 증가 22%[상시], 자신이 피격 5회 시 현재 생명력 가장 낮은 아군에게 시전자 방어력 30% 회복"
                        }},
                        { 1, new PassiveLevelData {
                            Effects = new List<PersistentEffect>
                            {
                                new PersistentEffect { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = new BuffSet { Def_Rate = 22 } }
                            },
                            Effect = "[자신] 방어력 증가 22%[상시], 자신이 피격 5회 시 현재 생명력 가장 낮은 아군에게 시전자 방어력 35% 회복"
                        }}
                    },
                    TranscendBonuses = new Dictionary<int, PassiveTranscend>
                    {
                        // 6초월: 회복 대상 수 변경 — 현재 생명력 가장 낮은 아군 2명 (회복 미반영, Effect 텍스트로만)
                        {6, new PassiveTranscend{ Effect = "6초월: 회복 대상 현재 생명력 가장 낮은 아군 2명" }}
                    }
                },
                TranscendType = TranscendType.DefBlk
            },

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

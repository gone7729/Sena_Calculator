using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 적 데이터베이스
    /// </summary>
    public static class EnemyDb
    {
        /// <summary>
        /// 정적 초기화: 공성전 보스의 스킬을 SiegeBossSkillDb에서 연결한다.
        /// (스탯·감쇄는 EnemyDB, 스킬은 SiegeBossSkillDb — 캐릭터의 BasicStatDB/CharacterDB 분리와 동일)
        /// </summary>
        static EnemyDb()
        {
            // 공성전 보스 스킬 연결 (이름 기반)
            foreach (var boss in SiegeBosses)
                boss.Skills = SiegeBossSkillDb.Get(boss.Name);

            // 공성전 잡몹 스킬 연결 (Id 기반 — 같은 이름이 라운드별 다른 스탯/스킬)
            foreach (var mob in SiegeMobs)
            {
                mob.Skills = SiegeBossSkillDb.GetMobSkills(mob.Id);

                // 토요일 룩·챈슬러 공성전 감쇄 패시브 (스파이크와 동일: 마법 90% / 1인 70% / 5인 90%)
                if (mob.Id >= 501 && mob.Id <= 506)
                {
                    mob.MagicReduction = 90;
                    mob.SingleTargetReduction = 70;
                    mob.MultiTargetReduction = 90;
                }
            }
        }

        /// <summary>
        /// 공성전 보스 목록
        /// </summary>
        public static readonly List<Enemy> SiegeBosses = new List<Enemy>
        {
            new Enemy
            {
                Id = 1,
                Name = "루디",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "월요일",
                Stats = new BaseStatSet
                {
                    Atk = 1601,
                    Def = 930,
                    Hp = 70000,
                    Spd = 19,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄
                PhysicalReduction = 90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 2,
                Name = "아일린",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "화요일",
                Stats = new BaseStatSet
                {
                    Atk = 3181,
                    Def = 1714,
                    Hp = 60000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄
                PhysicalReduction = 90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 3,
                Name = "레이첼",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "수요일",
                Stats = new BaseStatSet
                {
                    Atk = 3181,
                    Def = 1714,
                    Hp = 60000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄
                PhysicalReduction = 90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 4,
                Name = "델론즈",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "목요일",
                Stats = new BaseStatSet
                {
                    Atk = 3407,
                    Def = 1044,
                    Hp = 50000,
                    Spd = 29,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄 (위키 "죽음의 경계": 받는 마법 피해 90% 감소)
                PhysicalReduction = 0, //물리피해감소
                MagicReduction = 90, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 5,
                Name = "제이브",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "금요일",
                Stats = new BaseStatSet
                {
                    Atk = 3181,
                    Def = 1023,
                    Hp = 30000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄
                PhysicalReduction = 0, //물리피해감소
                MagicReduction = 90, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 6,
                Name = "스파이크",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "토요일",
                Stats = new BaseStatSet
                {
                    Atk = 3181,
                    Def = 1323,
                    Hp = 60000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄
                PhysicalReduction = 0, //물리피해감소
                MagicReduction = 90, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Enemy
            {
                Id = 7,
                Name = "크리스",
                EnemyType = EnemyType.Siege,
                DayOfWeek = "일요일",
                Stats = new BaseStatSet
                {
                    Atk = 2983,
                    Def = 1625,
                    Hp = 60000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                // 피해 감쇄 (위키 "영혼 흡수": 5인 공격기에만 90% 감소 — 물/마 구분·1인기 감쇄 없음)
                PhysicalReduction = 0, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },
            
        };

        /// <summary>
        /// 레이드 보스 목록
        /// </summary>
        public static readonly List<Enemy> RaidBosses = new List<Enemy>
        {
            new Enemy
            {
                Id = 101,
                Name = "강철의 포식자",
                EnemyType = EnemyType.Raid,
                Difficulty = 15,
                Stats = new BaseStatSet
                {
                    Atk = 4745,
                    Def = 3906,
                    Hp = 380024,
                    Spd = 30,
                    Cri = 0,
                    Cri_Dmg = 0,
                    Eff_Hit = 100
                },
                DefenseIncrease = 40,
                DefenseIncreaseCondition = "체력 30% 이상"
            },

            new Enemy
            {
                Id = 102,
                Name = "파멸의 눈동자",
                EnemyType = EnemyType.Raid,
                Difficulty = 15,
                Stats = new BaseStatSet
                {
                    Atk = 5034,
                    Def = 2612,
                    Hp = 835466,
                    Spd = 60,
                    Cri = 0,
                    Cri_Dmg = 0,
                    Eff_Hit = 100
                }
            },

            new Enemy
            {
                Id = 103,
                Name = "우마왕",
                EnemyType = EnemyType.Raid,
                Difficulty = 15,
                Stats = new BaseStatSet
                {
                    Atk = 4423,
                    Def = 0,
                    Hp = 6012500,
                    Spd = 200,
                    Cri = 0,
                    Cri_Dmg = 0,
                    Eff_Hit = 100
                }
            },
        };

        /// <summary>
        /// 강림 보스 목록
        /// </summary>
        public static readonly List<Enemy> ForestBosses = new List<Enemy>
        {
            new Enemy
            {
                Id = 201,
                Name = "태오1페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 4282,
                    Def = 942,
                    Hp = 1000000,
                    Spd = 19,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 90, 
                MagicReduction = 0, 
                SingleTargetReduction = 0,
                TripleTargetReduction = 90,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0 
            },
            new Enemy
            {
                Id = 202,
                Name = "태오2페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 4810,
                    Def = 1061,
                    Hp = 5000000,
                    Spd = 29,
                    Cri = 25,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 90, 
                MagicReduction = 0, 
                SingleTargetReduction = 0,
                TripleTargetReduction = 90,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0
            },
            new Enemy
            {
                Id = 203,
                Name = "카일1페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 3576,
                    Def = 1047,
                    Hp = 1000000,
                    Spd = 29,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 0, 
                MagicReduction = 90, 
                SingleTargetReduction = 0,
                TripleTargetReduction = 90,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0
            },
            new Enemy
            {
                Id = 204,
                Name = "카일2페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 4203,
                    Def = 1202,
                    Hp = 5000000,
                    Spd = 29,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 0, 
                MagicReduction = 90, 
                SingleTargetReduction = 0,
                TripleTargetReduction = 90,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0
            },
            new Enemy
            {
                Id = 205,
                Name = "연희1페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 3382,
                    Def = 1045,
                    Hp = 400000,
                    Spd = 29,
                    Cri = 0,
                    Cri_Dmg = 250,
                    Eff_Hit = 100
                },
                PhysicalReduction = 90, 
                MagicReduction = 0, 
                SingleTargetReduction = 90,
                TripleTargetReduction = 0,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0
            },
            new Enemy
            {
                Id = 206,
                Name = "연희2페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 3921,
                    Def = 1202,
                    Hp = 2000000,
                    Spd = 29,
                    Cri = 0,
                    Cri_Dmg = 250,
                    Eff_Hit = 100
                },
                PhysicalReduction = 90, 
                MagicReduction = 0, 
                SingleTargetReduction = 90,
                TripleTargetReduction = 0,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0
            },
            new Enemy
            {
                Id = 207,
                Name = "카르마1페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 3085,
                    Def = 545,
                    Hp = 400000,
                    Spd = 19,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 0, 
                MagicReduction = 90, 
                SingleTargetReduction = 90,
                TripleTargetReduction = 0,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0,
                IsStackableDefenseIncrease = true,
                DefenseIncrease = 13,
                MaxDefenseStack = 8,
                DefenseIncreaseCondition = "스킬 1회 당 1중첩"
            },
            new Enemy
            {
                Id = 208,
                Name = "카르마2페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 3691,
                    Def = 767,
                    Hp = 2000000,
                    Spd = 19,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
                PhysicalReduction = 0, 
                MagicReduction = 90, 
                SingleTargetReduction = 90,
                TripleTargetReduction = 0,
                MultiTargetReduction = 90,
                DamageReduction = 0,      
                DamageTakenIncrease = 0, 
                Vulnerability = 0,
                IsStackableDefenseIncrease = true,
                DefenseIncrease = 13,
                MaxDefenseStack = 8,
                DefenseIncreaseCondition = "스킬 1회 당 1중첩"
            },
            new Enemy
            {
                Id = 209,
                Name = "강림1페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 4066,
                    Def = 1510,
                    Hp = 1000000,
                    Spd = 100,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
            },
            new Enemy
            {
                Id = 210,
                Name = "강림2페",
                EnemyType = EnemyType.Raid,
                Stats = new BaseStatSet
                {
                    Atk = 4231,
                    Def = 1733,
                    Hp = 5000000,
                    Spd = 100,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                },
            },

        };

        /// <summary>
        /// 성장던전 보스 목록
        /// </summary>
        public static readonly List<Enemy> GrowthDungeonBosses = new List<Enemy>
        {
            new Enemy
            {
                Id = 401,
                Name = "골드던전",
                EnemyType = EnemyType.GrowthDungeon,
                Difficulty = 15,
                Stats = new BaseStatSet
                {
                    Atk = 11596,
                    Def = 3932,
                    Hp = 747703,
                    Spd = 41,
                    Cri = 5,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                }
            },
        };

        // 잡몹
        public static readonly List<Enemy> Mobs = new List<Enemy>
        {
            new Enemy
            {
                Id = 301,
                Name = "우마왕 잡몹",
                EnemyType = EnemyType.Mob,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Def = 100,
                    Hp = 18000
                }
            },
            new Enemy
            {
                Id = 302,
                Name = "악몽 12",
                EnemyType = EnemyType.Mob,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Def = 491,
                    Hp = 36373
                }
            },
            new Enemy
            {
                Id = 303,
                Name = "악몽 16",
                EnemyType = EnemyType.Mob,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Def = 910,
                    Hp = 33553
                }
            },
        };

        /// <summary>
        /// 공성전 잡몹 (요일별 라운드에 등장). 라운드마다 스탯이 다를 수 있어 라운드별 별도 Id로 선언
        /// (한 라운드 내 같은 이름 몹은 동일 스탯). 라운드별 보스 취급 여부는 SiegeStages의 StageEnemy.IsBoss로 표기.
        /// 룩·챈슬러는 게임상 영웅이지만 공성전 몹 버전으로 별도 정의(스킬은 추후).
        /// 토요일 룩: R1=501 / R2=503 / R3보스=505,  챈슬러: R1=502 / R2=504 / R3보스=506.
        /// </summary>
        public static readonly List<Enemy> SiegeMobs = new List<Enemy>
        {
            // 토요일 — 경비대 룩 (round1)
            new Enemy
            {
                Id = 501,
                Name = "룩",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 542,
                    Def = 689,
                    Hp = 8650,
                    Spd = 15,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 0
                }
            },
            // 토요일 — 경비대장 챈슬러 (round1)
            new Enemy
            {
                Id = 502,
                Name = "챈슬러",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 849,
                    Def = 466,
                    Hp = 7870,
                    Spd = 21,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 0
                }
            },
            // 토요일 — 경비대 룩 (round2)
            new Enemy
            {
                Id = 503,
                Name = "룩",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 873,
                    Def = 1123,
                    Hp = 10790,
                    Spd = 17,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 0
                }
            },
            // 토요일 — 경비대장 챈슬러 (round2)
            new Enemy
            {
                Id = 504,
                Name = "챈슬러",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 1315,
                    Def = 784,
                    Hp = 9870,
                    Spd = 23,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 0
                }
            },
            // 토요일 — 친위대 룩 (round3, 보스 취급, Lv.100 6성)
            new Enemy
            {
                Id = 505,
                Name = "룩",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 1502,
                    Def = 1423,
                    Hp = 40000,
                    Spd = 19,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                }
            },
            // 토요일 — 친위대장 챈슬러 (round3, 보스 취급, Lv.100 6성)
            new Enemy
            {
                Id = 506,
                Name = "챈슬러",
                EnemyType = EnemyType.Siege,
                IsBoss = false,
                Stats = new BaseStatSet
                {
                    Atk = 1754,
                    Def = 1423,
                    Hp = 40000,
                    Spd = 25,
                    Cri = 0,
                    Cri_Dmg = 150,
                    Eff_Hit = 100
                }
            },
        };

        /// <summary>
        /// 공성전 요일별 라운드 구성 (Stage.Waves = 라운드). EnemyId로 SiegeMobs/SiegeBosses 참조.
        /// 1~2라운드는 일반 적군, 3라운드는 모든 적이 보스 취급(StageEnemy.IsBoss = true).
        /// </summary>
        public static readonly Dictionary<string, Stage> SiegeStages = new Dictionary<string, Stage>
        {
            ["토요일"] = new Stage
            {
                Id = 6,
                Name = "토요일 공성전 (혹한의 성)",
                StageType = EnemyType.Siege,
                Waves = new List<StageWave>
                {
                    // 라운드 1 (일반 적군): 룩, 챈슬러, 룩
                    new StageWave
                    {
                        WaveNumber = 1,
                        Enemies = new List<StageEnemy>
                        {
                            new StageEnemy { EnemyId = 501, Position = 1 }, // 룩
                            new StageEnemy { EnemyId = 502, Position = 2 }, // 챈슬러
                            new StageEnemy { EnemyId = 501, Position = 3 }, // 룩
                        },
                        // 스킬 우선순위: 챈슬러 1스킬(분쇄) 최우선 → 룩 1스킬(투창)
                        SkillPriority = new List<SiegeSkillOrder>
                        {
                            new() { EnemyId = 502, SkillType = SkillType.Skill1 }, // 챈슬러 분쇄
                            new() { EnemyId = 501, SkillType = SkillType.Skill1 }, // 룩 투창
                        }
                    },
                    // 라운드 2 (일반 적군): 룩, 챈슬러, 룩
                    new StageWave
                    {
                        WaveNumber = 2,
                        Enemies = new List<StageEnemy>
                        {
                            new StageEnemy { EnemyId = 503, Position = 1 }, // 룩
                            new StageEnemy { EnemyId = 504, Position = 2 }, // 챈슬러
                            new StageEnemy { EnemyId = 503, Position = 3 }, // 룩
                        },
                        // 스킬 우선순위: 챈슬러 1스킬(분쇄) 최우선 → 룩 1스킬(투창)
                        SkillPriority = new List<SiegeSkillOrder>
                        {
                            new() { EnemyId = 504, SkillType = SkillType.Skill1 }, // 챈슬러 분쇄
                            new() { EnemyId = 503, SkillType = SkillType.Skill1 }, // 룩 투창
                        }
                    },
                    // 라운드 3 (모두 보스 취급): 룩(보스), 스파이크(보스), 챈슬러(보스)
                    new StageWave
                    {
                        WaveNumber = 3,
                        Enemies = new List<StageEnemy>
                        {
                            new StageEnemy { EnemyId = 505, Position = 1, IsBoss = true }, // 룩 (친위대, R3)
                            new StageEnemy { EnemyId = 6,   Position = 2, IsBoss = true }, // 스파이크 (SiegeBosses)
                            new StageEnemy { EnemyId = 506, Position = 3, IsBoss = true }, // 챈슬러 (친위대장, R3)
                        },
                        // 스킬 우선순위: 챈슬러 1스킬 → 스파이크 2스킬(혹한의 지진) → 스파이크 1스킬(혹한의 일격) → 룩 1스킬
                        SkillPriority = new List<SiegeSkillOrder>
                        {
                            new() { EnemyId = 506, SkillType = SkillType.Skill1 }, // 챈슬러 분쇄
                            new() { EnemyId = 6,   SkillType = SkillType.Skill2 }, // 스파이크 혹한의 지진
                            new() { EnemyId = 6,   SkillType = SkillType.Skill1 }, // 스파이크 혹한의 일격
                            new() { EnemyId = 505, SkillType = SkillType.Skill1 }, // 룩 투창
                        }
                    },
                }
            },
        };

        /// <summary>
        /// 보스 목록 (IsBoss = true)
        /// </summary>
        public static List<Enemy> Bosses => SiegeBosses.Concat(RaidBosses).Concat(ForestBosses).Concat(GrowthDungeonBosses).ToList();

        /// <summary>
        /// 일반몹 목록 (IsBoss = false) — 일반 잡몹 + 공성전 잡몹
        /// </summary>
        public static List<Enemy> Commons => Mobs.Concat(SiegeMobs).ToList();

        /// <summary>
        /// 모든 적 목록
        /// </summary>
        public static List<Enemy> AllEnemies => Bosses.Concat(Commons).ToList();

        /// <summary>
        /// 이름으로 적 찾기
        /// </summary>
        public static Enemy GetByName(string name, int? difficulty = null)
        {
            if (difficulty.HasValue)
            {
                return AllEnemies.FirstOrDefault(b => b.Name == name && b.Difficulty == difficulty);
            }
            return AllEnemies.FirstOrDefault(b => b.Name == name);
        }

        /// <summary>
        /// 공성전 요일별 보스 찾기
        /// </summary>
        public static Enemy GetSiegeBoss(string dayOfWeek)
        {
            return SiegeBosses.FirstOrDefault(b => b.DayOfWeek == dayOfWeek);
        }

        /// <summary>
        /// 잡몹 이름으로 찾기
        /// </summary>
        public static Enemy GetMobByName(string name)
        {
            return Mobs.FirstOrDefault(m => m.Name == name);
        }
    }

    
}

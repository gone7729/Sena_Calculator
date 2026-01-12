using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 보스 데이터베이스
    /// </summary>
    public static class BossDb
    {
        /// <summary>
        /// 공성전 보스 목록
        /// </summary>
        public static readonly List<Boss> SiegeBosses = new List<Boss>
        {
            new Boss
            {
                Id = 1,
                Name = "루디",
                BossType = BossType.Siege,
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
                PhysicalReduction = 0.90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 2,
                Name = "아일린",
                BossType = BossType.Siege,
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
                PhysicalReduction = 0.90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 3,
                Name = "레이첼",
                BossType = BossType.Siege,
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
                PhysicalReduction = 0.90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 4,
                Name = "델론즈",
                BossType = BossType.Siege,
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
                // 피해 감쇄
                PhysicalReduction = 0.90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 5,
                Name = "제이브",
                BossType = BossType.Siege,
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
                MagicReduction = 0.90, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 6,
                Name = "스파이크",
                BossType = BossType.Siege,
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
                MagicReduction = 0.90, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },

            new Boss
            {
                Id = 7,
                Name = "크리스",
                BossType = BossType.Siege,
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
                // 피해 감쇄
                PhysicalReduction = 0.90, //물리피해감소
                MagicReduction = 0, //마법피해감소
                SingleTargetReduction = 0, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },
            
        };

        /// <summary>
        /// 레이드 보스 목록
        /// </summary>
        public static readonly List<Boss> RaidBosses = new List<Boss>
        {
            new Boss
            {
                Id = 101,
                Name = "강철의 포식자",
                BossType = BossType.Raid,
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
                DefenseIncrease = 0.30,
                DefenseIncreaseCondition = "체력 30% 이상"
            },

            new Boss
            {
                Id = 102,
                Name = "파멸의 눈동자",
                BossType = BossType.Raid,
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

            new Boss
            {
                Id = 103,
                Name = "우마왕",
                BossType = BossType.Raid,
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

        // 잡몹
        public static readonly List<Boss> Mobs = new List<Boss>
        {
            new Boss
            {
                Id = 201,
                Name = "우마왕 잡몹",
                BossType = BossType.Mob,
                Stats = new BaseStatSet
                {
                    Def = 100,
                    Hp = 18000
                }
            },
            new Boss
            {
                Id = 202,
                Name = "악몽 12",
                BossType = BossType.Mob,
                Stats = new BaseStatSet
                {
                    Def = 491,
                    Hp = 36373
                }
            },
            new Boss
            {
                Id = 203,
                Name = "악몽 16",
                BossType = BossType.Mob,
                Stats = new BaseStatSet
                {
                    Def = 491,
                    Hp = 36373
                }
            },
        };
        
        /// <summary>
        /// 모든 보스 목록
        /// </summary>
        public static List<Boss> AllBosses => SiegeBosses.Concat(RaidBosses).ToList();

        /// <summary>
        /// 이름으로 보스 찾기
        /// </summary>
        public static Boss GetByName(string name, int? difficulty = null)
        {
            if (difficulty.HasValue)
            {
                return AllBosses.FirstOrDefault(b => b.Name == name && b.Difficulty == difficulty);
            }
            return AllBosses.FirstOrDefault(b => b.Name == name);
        }

        /// <summary>
        /// 공성전 요일별 보스 찾기
        /// </summary>
        public static Boss GetSiegeBoss(string dayOfWeek)
        {
            return SiegeBosses.FirstOrDefault(b => b.DayOfWeek == dayOfWeek);
        }

        /// <summary>
        /// 잡몹 이름으로 찾기
        /// </summary>
        public static Boss GetMobByName(string name)
        {
            return Mobs.FirstOrDefault(m => m.Name == name);
        }
    }

    
}

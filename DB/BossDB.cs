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
                MagicReduction = 90, //마법피해감소
                SingleTargetReduction = 70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감
                DamageTakenIncrease = 0, //받는피해증가
                Vulnerability = 0 // 취약%
            },
            
            // ===== 다른 요일 보스 추가 =====
            // new Boss { Id = 2, Name = "???", DayOfWeek = "월요일", ... },
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
                DefenseIncrease = 0.4,
                DefenseIncreaseCondition = "체력 30% 이상"
            },
        
            
            // ===== 다른 레이드 보스 추가 =====
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
    }

    
}

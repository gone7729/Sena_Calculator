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
                MagicReduction = 0.90, //마법피해감소
                SingleTargetReduction = 0.70, //1인기감소
                TripleTargetReduction = 0, //3인기감소
                MultiTargetReduction = 0.90, //5인기감소
                DamageReduction = 0,      // 보스 고유 받피감 30%
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
                Difficulty = 7,
                Stats = new BaseStatSet
                {
                    Def = 1956
                },
                DefenseIncrease = 0.30,
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

    /// <summary>
    /// 보스 모델
    /// </summary>
    public class Boss
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BossType BossType { get; set; }

        // 공성전용
        public string DayOfWeek { get; set; }

        // 레이드용
        public int Difficulty { get; set; }

        // 기본 스탯
        public BaseStatSet Stats { get; set; } = new BaseStatSet();

        // 피해 감쇄
        public double PhysicalReduction { get; set; }
        public double MagicReduction { get; set; }
        public double SingleTargetReduction { get; set; }
        public double TripleTargetReduction { get; set; }   // 3인기 감쇄 추가
        public double MultiTargetReduction { get; set; }

        // 조건부 방어력 증가
        public double DefenseIncrease { get; set; }
        public string DefenseIncreaseCondition { get; set; }

        // 조건부 방어력 감소
        public double DefenseDecrease { get; set; }
        public string DefenseDecreaseCondition { get; set; }

        // 보스 고유 디버프 관련 추가
        public double DamageReduction { get; set; }         // 받피감%
        public double DamageTakenIncrease { get; set; }     // 받피증%
        public double Vulnerability { get; set; }           // 취약%

        /// <summary>
        /// 실제 방어력 계산 (조건 적용)
        /// </summary>
        public double GetEffectiveDefense(bool isConditionMet)
        {
            double baseDef = Stats.Def;

            if (isConditionMet && DefenseIncrease > 0)
            {
                baseDef *= (1 + DefenseIncrease);
            }

            return baseDef;
        }
    }

    /// <summary>
    /// 보스 타입
    /// </summary>
    public enum BossType
    {
        Siege,      // 공성전
        Raid,       // 레이드
        Forest,      // 강림
        Other
    }
}

namespace GameDamageCalculator.Models
{
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
        Other,
        Mob
    }
}
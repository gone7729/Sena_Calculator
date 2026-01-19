using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 진형 보너스 데이터
    /// </summary>
    public class FormationBonus
    {
        public double Atk_Rate_Back { get; set; }   // 후방 공격력%
        public double Def_Rate_Front { get; set; }  // 전방 방어력%
    }

    /// <summary>
    /// 진형 모델 (UI 상태 + 계산)
    /// </summary>
    public class Formation
    {
        public string Name { get; set; }
        public bool IsBackPosition { get; set; }

        /// <summary>
        /// 진형 공격력% 보너스 (후열)
        /// </summary>
        public double GetAtkRate()
        {
            if (string.IsNullOrEmpty(Name) || Name == "없음") return 0;
            if (!IsBackPosition) return 0;

            if (StatTable.FormationDb.Formations.TryGetValue(Name, out var bonus))
            {
                return bonus.Atk_Rate_Back;
            }
            return 0;
        }

        /// <summary>
        /// 진형 방어력% 보너스 (전열)
        /// </summary>
        public double GetDefRate()
        {
            if (string.IsNullOrEmpty(Name) || Name == "없음") return 0;
            if (IsBackPosition) return 0;

            if (StatTable.FormationDb.Formations.TryGetValue(Name, out var bonus))
            {
                return bonus.Def_Rate_Front;
            }
            return 0;
        }
    }
}
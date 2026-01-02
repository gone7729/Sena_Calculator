namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 진형 보너스
    /// </summary>
    public class FormationBonus
    {
        public double Atk_Rate_Back { get; set; }   // 후방 공격력%
        public double Def_Rate_Front { get; set; }  // 전방 방어력%
    }
}
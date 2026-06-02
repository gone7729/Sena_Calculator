using System.Collections.Generic;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 전용무기 등급
    /// </summary>
    public enum ExclusiveWeaponGrade
    {
        고급,    // 45% 확률 슬롯
        희귀,    // 35% 확률 슬롯
        전설     // 20% 확률 슬롯
    }

    /// <summary>
    /// 전용무기 조율 옵션 종류 (8종)
    /// </summary>
    public enum TuningOption
    {
        모든공격력,    // Atk_Rate
        방어력,        // Def_Rate
        생명력,        // Hp_Rate
        효과적중,      // Eff_Hit
        효과저항,      // Eff_Res
        피해증폭,      // Dmg_Dealt
        파쇄,          // Block_Reduction
        탄성           // CritDmg_Taken_Reduction
    }

    /// <summary>
    /// 조율 슬롯 1개 = (옵션, 등급)
    /// </summary>
    public class TuningSlot
    {
        public TuningOption Option { get; set; }
        public ExclusiveWeaponGrade Grade { get; set; }

        /// <summary>
        /// 옵션·등급에 따른 수치 반환
        /// </summary>
        public double GetValue()
        {
            return ExclusiveWeapon.GetTuningValue(Option, Grade);
        }

        /// <summary>
        /// 이 슬롯의 효과를 BaseStatSet에 적용
        /// </summary>
        public void ApplyTo(BaseStatSet stats)
        {
            double v = GetValue();
            switch (Option)
            {
                case TuningOption.모든공격력: stats.Atk_Rate += v; break;
                case TuningOption.방어력:     stats.Def_Rate += v; break;
                case TuningOption.생명력:     stats.Hp_Rate += v; break;
                case TuningOption.효과적중:   stats.Eff_Hit += v; break;
                case TuningOption.효과저항:   stats.Eff_Res += v; break;
                case TuningOption.피해증폭:   stats.Dmg_Dealt += v; break;
                case TuningOption.파쇄:       stats.Block_Reduction += v; break;
                case TuningOption.탄성:       stats.CritDmg_Taken_Reduction += v; break;
            }
        }
    }

    /// <summary>
    /// 전용무기.
    /// - 캐릭터별 전용무기: 공격력 flat + 조율 4슬롯
    /// - 공용 전용무기: 공격력 flat 만, 조율 없음
    /// 시뮬은 항상 풀강화(+15) 가정 → 공격력은 최고치(247)로 고정.
    /// </summary>
    public class ExclusiveWeapon
    {
        public string Name { get; set; } = "";
        /// <summary>대상 캐릭터 ID. 0이면 공용.</summary>
        public int OwnerCharacterId { get; set; }
        /// <summary>풀강 공격력 flat. 기본 247.</summary>
        public double Atk { get; set; } = 247;
        /// <summary>마법 공격력 (마법형 캐릭이면 MagicAtk로 적용)</summary>
        public bool IsMagic { get; set; }
        /// <summary>조율 4슬롯 (공용 무기는 비어있음)</summary>
        public List<TuningSlot> Tuning { get; set; } = new();

        public bool IsCharacterSpecific => OwnerCharacterId != 0 && Tuning.Count > 0;

        /// <summary>
        /// 이 무기의 전체 스탯 기여(공격력 + 조율) 를 BaseStatSet에 적용
        /// </summary>
        public void ApplyTo(BaseStatSet stats)
        {
            if (IsMagic) stats.MagicAtk += Atk;
            else stats.Atk += Atk;
            foreach (var slot in Tuning) slot.ApplyTo(stats);
        }

        /// <summary>
        /// 옵션·등급별 수치표.
        /// 고급 / 희귀 / 전설.
        /// </summary>
        public static double GetTuningValue(TuningOption opt, ExclusiveWeaponGrade grade)
        {
            return (opt, grade) switch
            {
                (TuningOption.모든공격력, ExclusiveWeaponGrade.고급) => 5,
                (TuningOption.모든공격력, ExclusiveWeaponGrade.희귀) => 7,
                (TuningOption.모든공격력, ExclusiveWeaponGrade.전설) => 12,

                (TuningOption.방어력, ExclusiveWeaponGrade.고급) => 5,
                (TuningOption.방어력, ExclusiveWeaponGrade.희귀) => 7,
                (TuningOption.방어력, ExclusiveWeaponGrade.전설) => 12,

                (TuningOption.생명력, ExclusiveWeaponGrade.고급) => 5,
                (TuningOption.생명력, ExclusiveWeaponGrade.희귀) => 7,
                (TuningOption.생명력, ExclusiveWeaponGrade.전설) => 12,

                (TuningOption.효과적중, ExclusiveWeaponGrade.고급) => 4,
                (TuningOption.효과적중, ExclusiveWeaponGrade.희귀) => 6,
                (TuningOption.효과적중, ExclusiveWeaponGrade.전설) => 10,

                (TuningOption.효과저항, ExclusiveWeaponGrade.고급) => 4,
                (TuningOption.효과저항, ExclusiveWeaponGrade.희귀) => 6,
                (TuningOption.효과저항, ExclusiveWeaponGrade.전설) => 10,

                (TuningOption.피해증폭, ExclusiveWeaponGrade.고급) => 1.6,
                (TuningOption.피해증폭, ExclusiveWeaponGrade.희귀) => 2.4,
                (TuningOption.피해증폭, ExclusiveWeaponGrade.전설) => 4,

                (TuningOption.파쇄, ExclusiveWeaponGrade.고급) => 4.8,
                (TuningOption.파쇄, ExclusiveWeaponGrade.희귀) => 7.2,
                (TuningOption.파쇄, ExclusiveWeaponGrade.전설) => 12,

                (TuningOption.탄성, ExclusiveWeaponGrade.고급) => 6,
                (TuningOption.탄성, ExclusiveWeaponGrade.희귀) => 9,
                (TuningOption.탄성, ExclusiveWeaponGrade.전설) => 15,

                _ => 0
            };
        }
    }
}

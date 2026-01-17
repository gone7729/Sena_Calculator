using System;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 디버프 (적에게 부여하는 약화 효과)
    /// </summary>
    public class DebuffSet
    {
        // ===== 방어 관련 =====
        public double Def_Reduction { get; set; }       // 방어력 감소%
        public double Blk_Red { get; set; }             // 막기 확률 감소%

        // ===== 피해 관련 =====
        public double Dmg_Taken_Increase { get; set; }  // 받는 피해 증가%
        public double Vulnerability { get; set; }       // 취약%
        public double Boss_Vulnerability { get; set; }  // 보스 취약%
        public double Dmg_Reduction { get; set; }       // 주는 피해량 감소%
        public double Cri_Dmg_Reduction { get; set; }   // 치명타 피해 감소%

        // ===== 스탯 감소 =====
        public double Atk_Reduction { get; set; }       // 공격력 감소%
        public double Spd_Reduction { get; set; }       // 속도 감소%

        // ===== 회복 관련 =====
        public double Heal_Reduction { get; set; }      // 회복량 감소%
        public double Unrecover { get; set; }           // 회복불가

        // ===== 효과 관련 =====
        public double Eff_Red { get; set; }             // 효과 저항 감소%

        /// <summary>
        /// 다른 DebuffSet을 현재 세트에 더함
        /// </summary>
        public void Add(DebuffSet other)
        {
            if (other == null) return;

            Def_Reduction += other.Def_Reduction;
            Dmg_Taken_Increase += other.Dmg_Taken_Increase;
            Vulnerability += other.Vulnerability;
            Boss_Vulnerability += other.Boss_Vulnerability;
            Atk_Reduction += other.Atk_Reduction;
            Spd_Reduction += other.Spd_Reduction;
            Dmg_Reduction += other.Dmg_Reduction;
            Cri_Dmg_Reduction += other.Cri_Dmg_Reduction;
            Heal_Reduction += other.Heal_Reduction;
            Unrecover += other.Unrecover;
            Eff_Red += other.Eff_Red;
            Blk_Red += other.Blk_Red;
        }

        /// <summary>
        /// 같은 속성의 최대값으로 병합 (중복 디버프 처리용)
        /// </summary>
        public void MaxMerge(DebuffSet other)
        {
            if (other == null) return;

            Def_Reduction = Math.Max(Def_Reduction, other.Def_Reduction);
            Dmg_Taken_Increase = Math.Max(Dmg_Taken_Increase, other.Dmg_Taken_Increase);
            Vulnerability = Math.Max(Vulnerability, other.Vulnerability);
            Boss_Vulnerability = Math.Max(Boss_Vulnerability, other.Boss_Vulnerability);
            Atk_Reduction = Math.Max(Atk_Reduction, other.Atk_Reduction);
            Spd_Reduction = Math.Max(Spd_Reduction, other.Spd_Reduction);
            Dmg_Reduction = Math.Max(Dmg_Reduction, other.Dmg_Reduction);
            Cri_Dmg_Reduction = Math.Max(Cri_Dmg_Reduction, other.Cri_Dmg_Reduction);
            Heal_Reduction = Math.Max(Heal_Reduction, other.Heal_Reduction);
            Unrecover = Math.Max(Unrecover, other.Unrecover);
            Eff_Red = Math.Max(Eff_Red, other.Eff_Red);
            Blk_Red = Math.Max(Blk_Red, other.Blk_Red);
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public virtual DebuffSet Clone()
        {
            return new DebuffSet
            {
                Def_Reduction = Def_Reduction,
                Dmg_Taken_Increase = Dmg_Taken_Increase,
                Vulnerability = Vulnerability,
                Boss_Vulnerability = Boss_Vulnerability,
                Atk_Reduction = Atk_Reduction,
                Spd_Reduction = Spd_Reduction,
                Dmg_Reduction = Dmg_Reduction,
                Cri_Dmg_Reduction = Cri_Dmg_Reduction,
                Heal_Reduction = Heal_Reduction,
                Unrecover = Unrecover,
                Eff_Red = Eff_Red,
                Blk_Red = Blk_Red,
            };
        }

        /// <summary>
        /// 모든 값 초기화
        /// </summary>
        public void Clear()
        {
            Def_Reduction = 0;
            Dmg_Taken_Increase = 0;
            Vulnerability = 0;
            Boss_Vulnerability = 0;
            Atk_Reduction = 0;
            Spd_Reduction = 0;
            Dmg_Reduction = 0;
            Cri_Dmg_Reduction = 0;
            Heal_Reduction = 0;
            Unrecover = 0;
            Eff_Red = 0;
            Blk_Red = 0;
        }
    }

    /// <summary>
    /// 상시 디버프 (패시브 등 - 같은 타입끼리 MaxMerge)
    /// </summary>
    public class PermanentDebuff : DebuffSet
    {
        public override DebuffSet Clone()
        {
            return new PermanentDebuff
            {
                Def_Reduction = Def_Reduction,
                Dmg_Taken_Increase = Dmg_Taken_Increase,
                Vulnerability = Vulnerability,
                Boss_Vulnerability = Boss_Vulnerability,
                Atk_Reduction = Atk_Reduction,
                Spd_Reduction = Spd_Reduction,
                Dmg_Reduction = Dmg_Reduction,
                Cri_Dmg_Reduction = Cri_Dmg_Reduction,
                Heal_Reduction = Heal_Reduction,
                Unrecover = Unrecover,
                Eff_Red = Eff_Red,
                Blk_Red = Blk_Red,
            };
        }
    }

    /// <summary>
    /// 턴제 디버프 (스킬 등 - 같은 타입끼리 MaxMerge)
    /// </summary>
    public class TimedDebuff : DebuffSet
    {
        public override DebuffSet Clone()
        {
            return new TimedDebuff
            {
                Def_Reduction = Def_Reduction,
                Dmg_Taken_Increase = Dmg_Taken_Increase,
                Vulnerability = Vulnerability,
                Boss_Vulnerability = Boss_Vulnerability,
                Atk_Reduction = Atk_Reduction,
                Spd_Reduction = Spd_Reduction,
                Dmg_Reduction = Dmg_Reduction,
                Cri_Dmg_Reduction = Cri_Dmg_Reduction,
                Heal_Reduction = Heal_Reduction,
                Unrecover = Unrecover,
                Eff_Red = Eff_Red,
                Blk_Red = Blk_Red,
            };
        }
    }
}
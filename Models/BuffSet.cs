using System;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 버프 (모든 스탯 보너스 통합)
    /// </summary>
    public class BuffSet
    {
        // ===== 기본 스탯 % =====
        public double Atk_Rate { get; set; }        // 공격력%
        public double Def_Rate { get; set; }        // 방어력%
        public double Hp_Rate { get; set; }         // 체력%

        // ===== 치명타 =====
        public double Cri { get; set; }             // 치명타 확률
        public double Cri_Dmg { get; set; }         // 치명타 피해
        public double CriBonusDmg { get; set; }     // 치명타 시 추가 피해 배율
        public bool CriBonusDmgPerHit { get; set; } // true면 타격당 적용

        // ===== 약점 =====
        public double Wek { get; set; }             // 약점 확률
        public double Wek_Dmg { get; set; }         // 약점 피해
        public double WekBonusDmg { get; set; }     // 약점 시 추가 피해 배율

        // ===== 피해량 =====
        public double Dmg_Dealt { get; set; }       // 피해량%
        public double Dmg_Dealt_Type { get; set; }  // 물리/마법 피해량% (유저가 타입에 맞게 적용)
        public double Dmg_Dealt_Bos { get; set; }   // 보스 피해량%
        public double Dmg_Dealt_1to3 { get; set; }  // 1-3인기 피해량%
        public double Dmg_Dealt_4to5 { get; set; }  // 4-5인기 피해량%

        // ===== 방어 관련 =====
        public double Arm_Pen { get; set; }         // 방어 관통%
        public double Dmg_Rdc { get; set; }         // 받는 피해 감소%
        public double Dmg_Rdc_Multi { get; set; }   // 5인기 받피감%
        public double Blk { get; set; }             // 막기 확률%

        // ===== 기타 =====
        public double Heal_Bonus { get; set; }      // 받는 회복량%
        public double Eff_Res { get; set; }         // 효과 저항%
        public double Eff_Hit { get; set; }         // 효과 적중%
        public double Shield_HpRatio { get; set; }  // 보호막%
        public double Blessing { get; set; }        // 축복 - 1회 피해 최대 HP% 제한

        /// <summary>
        /// 다른 BuffSet을 현재 세트에 더함
        /// </summary>
        public void Add(BuffSet other)
        {
            if (other == null) return;

            Atk_Rate += other.Atk_Rate;
            Def_Rate += other.Def_Rate;
            Hp_Rate += other.Hp_Rate;
            Cri += other.Cri;
            Cri_Dmg += other.Cri_Dmg;
            CriBonusDmg += other.CriBonusDmg;
            Wek += other.Wek;
            Wek_Dmg += other.Wek_Dmg;
            WekBonusDmg += other.WekBonusDmg;
            Dmg_Dealt += other.Dmg_Dealt;
            Dmg_Dealt_Type += other.Dmg_Dealt_Type;
            Dmg_Dealt_Bos += other.Dmg_Dealt_Bos;
            Dmg_Dealt_1to3 += other.Dmg_Dealt_1to3;
            Dmg_Dealt_4to5 += other.Dmg_Dealt_4to5;
            Arm_Pen += other.Arm_Pen;
            Dmg_Rdc += other.Dmg_Rdc;
            Dmg_Rdc_Multi += other.Dmg_Rdc_Multi;
            Blk += other.Blk;
            Heal_Bonus += other.Heal_Bonus;
            Eff_Res += other.Eff_Res;
            Eff_Hit += other.Eff_Hit;
            Shield_HpRatio += other.Shield_HpRatio;
            Blessing += other.Blessing;
        }

        /// <summary>
        /// 같은 속성의 최대값으로 병합 (중복 버프 처리용)
        /// </summary>
        public void MaxMerge(BuffSet other)
        {
            if (other == null) return;

            Atk_Rate = Math.Max(Atk_Rate, other.Atk_Rate);
            Def_Rate = Math.Max(Def_Rate, other.Def_Rate);
            Hp_Rate = Math.Max(Hp_Rate, other.Hp_Rate);
            Cri = Math.Max(Cri, other.Cri);
            Cri_Dmg = Math.Max(Cri_Dmg, other.Cri_Dmg);
            CriBonusDmg = Math.Max(CriBonusDmg, other.CriBonusDmg);
            Wek = Math.Max(Wek, other.Wek);
            Wek_Dmg = Math.Max(Wek_Dmg, other.Wek_Dmg);
            WekBonusDmg = Math.Max(WekBonusDmg, other.WekBonusDmg);
            Dmg_Dealt = Math.Max(Dmg_Dealt, other.Dmg_Dealt);
            Dmg_Dealt_Type = Math.Max(Dmg_Dealt_Type, other.Dmg_Dealt_Type);
            Dmg_Dealt_Bos = Math.Max(Dmg_Dealt_Bos, other.Dmg_Dealt_Bos);
            Dmg_Dealt_1to3 = Math.Max(Dmg_Dealt_1to3, other.Dmg_Dealt_1to3);
            Dmg_Dealt_4to5 = Math.Max(Dmg_Dealt_4to5, other.Dmg_Dealt_4to5);
            Arm_Pen = Math.Max(Arm_Pen, other.Arm_Pen);
            Dmg_Rdc = Math.Max(Dmg_Rdc, other.Dmg_Rdc);
            Dmg_Rdc_Multi = Math.Max(Dmg_Rdc_Multi, other.Dmg_Rdc_Multi);
            Blk = Math.Max(Blk, other.Blk);
            Heal_Bonus = Math.Max(Heal_Bonus, other.Heal_Bonus);
            Eff_Res = Math.Max(Eff_Res, other.Eff_Res);
            Eff_Hit = Math.Max(Eff_Hit, other.Eff_Hit);
            Shield_HpRatio = Math.Max(Shield_HpRatio, other.Shield_HpRatio);
            Blessing = Math.Max(Blessing, other.Blessing);
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public virtual BuffSet Clone()
        {
            return new BuffSet
            {
                Atk_Rate = Atk_Rate,
                Def_Rate = Def_Rate,
                Hp_Rate = Hp_Rate,
                Cri = Cri,
                Cri_Dmg = Cri_Dmg,
                CriBonusDmg = CriBonusDmg,
                CriBonusDmgPerHit = CriBonusDmgPerHit,
                Wek = Wek,
                Wek_Dmg = Wek_Dmg,
                WekBonusDmg = WekBonusDmg,
                Dmg_Dealt = Dmg_Dealt,
                Dmg_Dealt_Type = Dmg_Dealt_Type,
                Dmg_Dealt_Bos = Dmg_Dealt_Bos,
                Dmg_Dealt_1to3 = Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = Dmg_Dealt_4to5,
                Arm_Pen = Arm_Pen,
                Dmg_Rdc = Dmg_Rdc,
                Dmg_Rdc_Multi = Dmg_Rdc_Multi,
                Blk = Blk,
                Heal_Bonus = Heal_Bonus,
                Eff_Res = Eff_Res,
                Eff_Hit = Eff_Hit,
                Shield_HpRatio = Shield_HpRatio,
                Blessing = Blessing,
            };
        }

        /// <summary>
        /// 모든 값 초기화
        /// </summary>
        public void Clear()
        {
            Atk_Rate = 0;
            Def_Rate = 0;
            Hp_Rate = 0;
            Cri = 0;
            Cri_Dmg = 0;
            CriBonusDmg = 0;
            CriBonusDmgPerHit = false;
            Wek = 0;
            Wek_Dmg = 0;
            WekBonusDmg = 0;
            Dmg_Dealt = 0;
            Dmg_Dealt_Type = 0;
            Dmg_Dealt_Bos = 0;
            Dmg_Dealt_1to3 = 0;
            Dmg_Dealt_4to5 = 0;
            Arm_Pen = 0;
            Dmg_Rdc = 0;
            Dmg_Rdc_Multi = 0;
            Blk = 0;
            Heal_Bonus = 0;
            Eff_Res = 0;
            Eff_Hit = 0;
            Shield_HpRatio = 0;
            Blessing = 0;
        }
    }

    /// <summary>
    /// 상시 버프 (파티버프, 자버프 등 - 같은 타입끼리 MaxMerge)
    /// </summary>
    public class PermanentBuff : BuffSet
    {
        public override BuffSet Clone()
        {
            return new PermanentBuff
            {
                Atk_Rate = Atk_Rate,
                Def_Rate = Def_Rate,
                Hp_Rate = Hp_Rate,
                Cri = Cri,
                Cri_Dmg = Cri_Dmg,
                CriBonusDmg = CriBonusDmg,
                CriBonusDmgPerHit = CriBonusDmgPerHit,
                Wek = Wek,
                Wek_Dmg = Wek_Dmg,
                WekBonusDmg = WekBonusDmg,
                Dmg_Dealt = Dmg_Dealt,
                Dmg_Dealt_Type = Dmg_Dealt_Type,
                Dmg_Dealt_Bos = Dmg_Dealt_Bos,
                Dmg_Dealt_1to3 = Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = Dmg_Dealt_4to5,
                Arm_Pen = Arm_Pen,
                Dmg_Rdc = Dmg_Rdc,
                Dmg_Rdc_Multi = Dmg_Rdc_Multi,
                Blk = Blk,
                Heal_Bonus = Heal_Bonus,
                Eff_Res = Eff_Res,
                Eff_Hit = Eff_Hit,
                Shield_HpRatio = Shield_HpRatio,
                Blessing = Blessing,
            };
        }

        /// <summary>
        /// TimedBuff를 PermanentBuff로 암시적 변환
        /// </summary>
        public static implicit operator PermanentBuff(TimedBuff timedBuff)
        {
            if (timedBuff == null) return null;

            return new PermanentBuff
            {
                Atk_Rate = timedBuff.Atk_Rate,
                Def_Rate = timedBuff.Def_Rate,
                Hp_Rate = timedBuff.Hp_Rate,
                Cri = timedBuff.Cri,
                Cri_Dmg = timedBuff.Cri_Dmg,
                CriBonusDmg = timedBuff.CriBonusDmg,
                CriBonusDmgPerHit = timedBuff.CriBonusDmgPerHit,
                Wek = timedBuff.Wek,
                Wek_Dmg = timedBuff.Wek_Dmg,
                WekBonusDmg = timedBuff.WekBonusDmg,
                Dmg_Dealt = timedBuff.Dmg_Dealt,
                Dmg_Dealt_Type = timedBuff.Dmg_Dealt_Type,
                Dmg_Dealt_Bos = timedBuff.Dmg_Dealt_Bos,
                Dmg_Dealt_1to3 = timedBuff.Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = timedBuff.Dmg_Dealt_4to5,
                Arm_Pen = timedBuff.Arm_Pen,
                Dmg_Rdc = timedBuff.Dmg_Rdc,
                Dmg_Rdc_Multi = timedBuff.Dmg_Rdc_Multi,
                Blk = timedBuff.Blk,
                Heal_Bonus = timedBuff.Heal_Bonus,
                Eff_Res = timedBuff.Eff_Res,
                Eff_Hit = timedBuff.Eff_Hit,
                Shield_HpRatio = timedBuff.Shield_HpRatio,
                Blessing = timedBuff.Blessing,
            };
        }
    }

    /// <summary>
    /// 턴제 버프 (액티브 스킬, 조건부 패시브 등 - 같은 타입끼리 MaxMerge)
    /// </summary>
    public class TimedBuff : BuffSet
    {
        public override BuffSet Clone()
        {
            return new TimedBuff
            {
                Atk_Rate = Atk_Rate,
                Def_Rate = Def_Rate,
                Hp_Rate = Hp_Rate,
                Cri = Cri,
                Cri_Dmg = Cri_Dmg,
                CriBonusDmg = CriBonusDmg,
                CriBonusDmgPerHit = CriBonusDmgPerHit,
                Wek = Wek,
                Wek_Dmg = Wek_Dmg,
                WekBonusDmg = WekBonusDmg,
                Dmg_Dealt = Dmg_Dealt,
                Dmg_Dealt_Type = Dmg_Dealt_Type,
                Dmg_Dealt_Bos = Dmg_Dealt_Bos,
                Dmg_Dealt_1to3 = Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = Dmg_Dealt_4to5,
                Arm_Pen = Arm_Pen,
                Dmg_Rdc = Dmg_Rdc,
                Dmg_Rdc_Multi = Dmg_Rdc_Multi,
                Blk = Blk,
                Heal_Bonus = Heal_Bonus,
                Eff_Res = Eff_Res,
                Eff_Hit = Eff_Hit,
                Shield_HpRatio = Shield_HpRatio,
                Blessing = Blessing,
            };
        }
    }
}
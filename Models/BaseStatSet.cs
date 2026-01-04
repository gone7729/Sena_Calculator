namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 모든 스탯을 담는 기본 스탯 세트 클래스
    /// 캐릭터, 장비, 버프 등 모든 곳에서 공통으로 사용
    /// </summary>
    public class BaseStatSet
    {
        // ===== 기본 스탯 =====
        public double Atk { get; set; }         // 공격력
        public double Def { get; set; }         // 방어력
        public double Hp { get; set; }          // 생명력
        public double Spd { get; set; }         // 속공

        // ===== 치명타 관련 =====
        public double Cri { get; set; }         // 치명타 확률 (%)
        public double Cri_Dmg { get; set; }     // 치명타 피해 (%)

        // ===== 약점 공격 관련 =====
        public double Wek { get; set; }         // 약점 공격 확률 (%)
        public double Wek_Dmg { get; set; }     // 약점 공격 피해량 (%)

        // ===== 방어/생존 관련 =====
        public double Blk { get; set; }         // 막기 확률 (%)
        public double Blk_Red { get; set; }     // 막기 피해 감소 (%)
        public double Dmg_Rdc { get; set; }     // 받는 피해 감소 (%)
        public double Heal_Rec { get; set; }    // 받는 회복량 증가 (%)

        // ===== 효과 관련 =====
        public double Eff_Hit { get; set; }     // 효과 적중 (%)
        public double Eff_Res { get; set; }     // 효과 저항 (%)
        public double Eff_Acc { get; set; }     // 효과 적중 확률 (%)

        // ===== 피해량 관련 =====
        public double Arm_Pen { get; set; }     // 방어 무시 (%)
        public double Dmg_Dealt { get; set; }   // 주는 피해량 (%)
        public double Dmg_Dealt_Bos { get; set; } // 보스 대상 피해량 (%)

        // ===== 비율 증가 (장비/버프용) =====
        public double Atk_Rate { get; set; }    // 공격력 % 증가
        public double Def_Rate { get; set; }    // 방어력 % 증가
        public double Hp_Rate { get; set; }     // 생명력 % 증가

        /// <summary>
        /// 다른 스탯 세트를 현재 세트에 더함
        /// </summary>
        public void Add(BaseStatSet other)
        {
            if (other == null) return;

            Atk += other.Atk;
            Def += other.Def;
            Hp += other.Hp;
            Spd += other.Spd;
            Cri += other.Cri;
            Cri_Dmg += other.Cri_Dmg;
            Wek += other.Wek;
            Wek_Dmg += other.Wek_Dmg;
            Blk += other.Blk;
            Blk_Red += other.Blk_Red;
            Dmg_Rdc += other.Dmg_Rdc;
            Heal_Rec += other.Heal_Rec;
            Eff_Hit += other.Eff_Hit;
            Eff_Res += other.Eff_Res;
            Eff_Acc += other.Eff_Acc;
            Arm_Pen += other.Arm_Pen;
            Dmg_Dealt += other.Dmg_Dealt;
            Dmg_Dealt_Bos += other.Dmg_Dealt_Bos;
            Atk_Rate += other.Atk_Rate;
            Def_Rate += other.Def_Rate;
            Hp_Rate += other.Hp_Rate;
        }

        /// <summary>
        /// 특정 스탯명으로 값을 더함 (부옵션용)
        /// </summary>
        public void Add(string statName, double value)
        {
            switch (statName)
            {
                case "공격력": Atk += value; break;
                case "공격력%": Atk_Rate += value; break;
                case "방어력": Def += value; break;
                case "방어력%": Def_Rate += value; break;
                case "생명력": Hp += value; break;
                case "생명력%": Hp_Rate += value; break;
                case "속공": Spd += value; break;
                case "치명타확률%": Cri += value; break;
                case "치명타피해%": Cri_Dmg += value; break;
                case "약점공격확률%": Wek += value; break;
                case "약점공격피해%": Wek_Dmg += value; break;
                case "막기확률%": Blk += value; break;
                case "막기피해감소%": Blk_Red += value; break;
                case "받피감%": Dmg_Rdc += value; break;
                case "받는회복량%": Heal_Rec += value; break;
                case "효과적중%": Eff_Hit += value; break;
                case "효과저항%": Eff_Res += value; break;
                case "효과적중확률%": Eff_Acc += value; break;
                case "방무%": Arm_Pen += value; break;
                case "주는피해량%": Dmg_Dealt += value; break;
                case "보스피해량%": Dmg_Dealt_Bos += value; break;
            }
        }

        /// <summary>
        /// 새로운 복사본 생성
        /// </summary>
        public BaseStatSet Clone()
        {
            return new BaseStatSet
            {
                Atk = this.Atk,
                Def = this.Def,
                Hp = this.Hp,
                Spd = this.Spd,
                Cri = this.Cri,
                Cri_Dmg = this.Cri_Dmg,
                Wek = this.Wek,
                Wek_Dmg = this.Wek_Dmg,
                Blk = this.Blk,
                Blk_Red = this.Blk_Red,
                Dmg_Rdc = this.Dmg_Rdc,
                Heal_Rec = this.Heal_Rec,
                Eff_Hit = this.Eff_Hit,
                Eff_Res = this.Eff_Res,
                Eff_Acc = this.Eff_Acc,
                Arm_Pen = this.Arm_Pen,
                Dmg_Dealt = this.Dmg_Dealt,
                Dmg_Dealt_Bos = this.Dmg_Dealt_Bos,
                Atk_Rate = this.Atk_Rate,
                Def_Rate = this.Def_Rate,
                Hp_Rate = this.Hp_Rate
            };
        }

        /// <summary>
        /// 모든 스탯 초기화
        /// </summary>
        public void Clear()
        {
            Atk = Def = Hp = Spd = 0;
            Cri = Cri_Dmg = 0;
            Wek = Wek_Dmg = 0;
            Blk = Blk_Red = Dmg_Rdc = Heal_Rec = 0;
            Eff_Hit = Eff_Res = Eff_Acc = 0;
            Arm_Pen = Dmg_Dealt = Dmg_Dealt_Bos = 0;
            Atk_Rate = Def_Rate = Hp_Rate = 0;
        }

        public BaseStatSet Multiply(double multiplier)
        {
            return new BaseStatSet
            {
                Atk = this.Atk * multiplier,
                Atk_Rate = this.Atk_Rate * multiplier,
                Def = this.Def * multiplier,
                Def_Rate = this.Def_Rate * multiplier,
                Hp = this.Hp * multiplier,
                Hp_Rate = this.Hp_Rate * multiplier,
                Cri = this.Cri * multiplier,
                Cri_Dmg = this.Cri_Dmg * multiplier,
                Spd = this.Spd * multiplier,
                Wek = this.Wek * multiplier,
                Blk = this.Blk * multiplier,
                Eff_Hit = this.Eff_Hit * multiplier,
                Eff_Res = this.Eff_Res * multiplier
            };
        }
    }
}

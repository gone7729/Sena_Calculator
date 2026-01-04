using System.Collections.Generic;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 장비 모델
    /// </summary>
    public class Equipment
    {
        public string Name { get; set; }            // 장비 이름
        public string SetName { get; set; }         // 세트 이름 (선봉장, 암살자 등)
        public string Slot { get; set; }            // 부위 (무기, 방어구)

        // 메인 옵션
        public string MainStatName { get; set; }    // 메인 옵션 종류 ("공격력%", "치명타확률%" 등)
        
        // 부옵션 슬롯 (최대 4개)
        public List<SubStatSlot> SubSlots { get; set; } = new List<SubStatSlot>();

        // 장비 강화 단계 (0~5)
        public int EnhanceLevel { get; set; } = 5;

        /// <summary>
        /// 장비 기본 스탯 가져오기
        /// </summary>
        public BaseStatSet GetBaseStats()
        {
            if (Slot == "무기")
                return EquipmentDb.EquipStatTable.CommonWeaponStat.Clone();
            else if (Slot == "방어구")
                return EquipmentDb.EquipStatTable.CommonArmorStat.Clone();
            
            return new BaseStatSet();
        }

        /// <summary>
        /// 메인 옵션 스탯 가져오기
        /// </summary>
        public BaseStatSet GetMainStats()
        {
            if (EquipmentDb.MainStatDb.OptionsBySlot.TryGetValue(Slot, out var slotOptions))
            {
                if (slotOptions.TryGetValue(MainStatName, out var mainStat))
                {
                    return mainStat.Clone();
                }
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 부옵션 합산 스탯 가져오기
        /// </summary>
        public BaseStatSet GetSubStats()
        {
            BaseStatSet total = new BaseStatSet();
            foreach (var sub in SubSlots)
            {
                total.Add(sub.GetStats());
            }
            return total;
        }

        /// <summary>
        /// 장비의 전체 스탯 계산 (기본 + 메인 + 부옵션)
        /// 세트 효과는 별도로 계산
        /// </summary>
        public BaseStatSet GetTotalStats()
        {
            BaseStatSet total = GetBaseStats();
            total.Add(GetMainStats());
            total.Add(GetSubStats());
            return total;
        }
    }

    /// <summary>
    /// 부옵션 슬롯
    /// </summary>
    public class SubStatSlot
    {
        public string StatName { get; set; }    // 스탯 종류 ("공격력%", "치명타확률%" 등)
        public int Tier { get; set; }           // 단계 (1~5)

        /// <summary>
        /// 티어에 따른 실제 값 계산
        /// </summary>
        public BaseStatSet GetStats()
        {
            if (Tier <= 0) return new BaseStatSet();

            if (EquipmentDb.SubStatDb.SubStatBase.TryGetValue(StatName, out var baseStats))
            {
                return baseStats.Multiply(Tier);
            }
            return new BaseStatSet();
        }

        private double GetValueFromStatSet(BaseStatSet stat, string statName)
        {
            return statName switch
            {
                "공격력%" => stat.Atk_Rate,
                "공격력" => stat.Atk,
                "방어력%" => stat.Def_Rate,
                "방어력" => stat.Def,
                "생명력%" => stat.Hp_Rate,
                "생명력" => stat.Hp,
                "속공" => stat.Spd,
                "치명타확률%" => stat.Cri,
                "치명타피해%" => stat.Cri_Dmg,
                "약점공격확률%" => stat.Wek,
                "막기확률%" => stat.Blk,
                "효과적중%" => stat.Eff_Hit,
                "효과저항%" => stat.Eff_Res,
                _ => 0
            };
        }
    }

    /// <summary>
    /// 장비 세트 (2개 또는 4개 착용 시 효과)
    /// </summary>
    public class EquipmentSet
    {
        public string SetName { get; set; }
        public int PieceCount { get; set; }     // 착용 개수 (2 또는 4)

        /// <summary>
        /// 세트 효과 스탯 가져오기
        /// </summary>
        public BaseStatSet GetSetBonus()
        {
            if (EquipmentDb.SetEffects.TryGetValue(SetName, out var setData))
            {
                // 4세트면 2세트 + 4세트 효과 모두 적용
                BaseStatSet total = new BaseStatSet();
                
                if (PieceCount >= 2 && setData.TryGetValue(2, out var bonus2))
                {
                    total.Add(bonus2);
                }
                if (PieceCount >= 4 && setData.TryGetValue(4, out var bonus4))
                {
                    total.Add(bonus4);
                }
                
                return total;
            }
            return new BaseStatSet();
        }
    }
}

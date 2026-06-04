using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 캐릭터 1명의 장비 한벌 (무기2 + 방어구2 + 장신구1)
    /// </summary>
    public class EquipmentLoadout
    {
        public Equipment Weapon1 { get; set; }
        public Equipment Weapon2 { get; set; }
        public Equipment Armor1 { get; set; }
        public Equipment Armor2 { get; set; }
        public Accessory Accessory { get; set; }

        /// <summary>
        /// 전체 장비 목록 반환 (무기+방어구만, 장신구 제외)
        /// </summary>
        public IEnumerable<Equipment> GetEquipments()
        {
            var list = new List<Equipment>();
            if (Weapon1 != null) list.Add(Weapon1);
            if (Weapon2 != null) list.Add(Weapon2);
            if (Armor1 != null) list.Add(Armor1);
            if (Armor2 != null) list.Add(Armor2);
            return list;
        }

        /// <summary>
        /// 활성화된 세트 효과 목록 계산
        /// 4세트 효과는 2세트와 중첩되지 않음 (4세트만 적용)
        /// </summary>
        public List<EquipmentSet> GetActiveSets()
        {
            var setCounts = new Dictionary<string, int>();
            foreach (var equip in GetEquipments())
            {
                if (!string.IsNullOrEmpty(equip.SetName))
                {
                    if (!setCounts.ContainsKey(equip.SetName))
                        setCounts[equip.SetName] = 0;
                    setCounts[equip.SetName]++;
                }
            }

            var activeSets = new List<EquipmentSet>();
            foreach (var kvp in setCounts)
            {
                if (kvp.Value >= 4)
                {
                    // 4세트: 2세트 효과를 대체 (중첩 아님)
                    activeSets.Add(new EquipmentSet { SetName = kvp.Key, PieceCount = 4 });
                }
                else if (kvp.Value >= 2)
                {
                    activeSets.Add(new EquipmentSet { SetName = kvp.Key, PieceCount = 2 });
                }
            }
            return activeSets;
        }

        /// <summary>
        /// 모든 세트 보너스 합산 스탯
        /// </summary>
        public BaseStatSet GetTotalSetBonus()
        {
            var total = new BaseStatSet();
            foreach (var set in GetActiveSets())
            {
                var bonus = set.GetSetBonus();
                total.Add(bonus);
            }
            return total;
        }

        /// <summary>
        /// 모든 장비의 합산 스탯 (기본 + 메인옵 + 서브옵)
        /// </summary>
        public BaseStatSet GetTotalEquipmentStats()
        {
            var total = new BaseStatSet();
            foreach (var equip in GetEquipments())
            {
                total.Add(equip.GetTotalStats());
            }
            return total;
        }

        /// <summary>
        /// 장비 한벌 복사
        /// </summary>
        public EquipmentLoadout Clone()
        {
            return new EquipmentLoadout
            {
                Weapon1 = Weapon1,
                Weapon2 = Weapon2,
                Armor1 = Armor1,
                Armor2 = Armor2,
                Accessory = Accessory != null ? new Accessory
                {
                    Grade = Accessory.Grade,
                    MainOption = Accessory.MainOption,
                    SubOption = Accessory.SubOption,
                    RingName = Accessory.RingName
                } : null
            };
        }
    }
}

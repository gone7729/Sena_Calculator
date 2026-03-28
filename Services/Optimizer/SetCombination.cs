using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.Optimizer
{
    /// <summary>
    /// 장비 세트 조합 열거
    ///
    /// 장비 4개 (무기2 + 방어구2)의 세트 조합:
    /// - 4세트: AAAA (9가지)
    /// - 2+2세트: 무기세트 + 방어구세트 (9×9 - 9 = 72가지, 동일 제외)
    /// - 2세트만: 무기 or 방어구만 맞춤 (특수 케이스)
    ///
    /// 4세트 효과는 2세트와 중첩 안됨 (4세트만 적용)
    /// </summary>
    public static class SetCombination
    {
        /// <summary>
        /// 모든 유효한 세트 조합 생성
        /// </summary>
        public static List<EquipSetConfig> GenerateAllCombinations()
        {
            var setNames = EquipmentDb.SetEffects.Keys.ToList();
            var combinations = new List<EquipSetConfig>();

            // 1. 4세트 (무기2 + 방어구2 모두 같은 세트)
            foreach (var setName in setNames)
            {
                combinations.Add(new EquipSetConfig
                {
                    WeaponSetName = setName,
                    ArmorSetName = setName,
                    Is4Set = true,
                    Description = $"{setName} 4세트"
                });
            }

            // 2. 2+2세트 (무기세트 ≠ 방어구세트)
            for (int i = 0; i < setNames.Count; i++)
            {
                for (int j = 0; j < setNames.Count; j++)
                {
                    if (i == j) continue; // 같은 세트는 4세트로 이미 처리
                    combinations.Add(new EquipSetConfig
                    {
                        WeaponSetName = setNames[i],
                        ArmorSetName = setNames[j],
                        Is4Set = false,
                        Description = $"{setNames[i]} 2세트 + {setNames[j]} 2세트"
                    });
                }
            }

            return combinations;
        }

        /// <summary>
        /// DPS 관련 세트만 필터링 (공격형 캐릭터용)
        /// </summary>
        public static List<EquipSetConfig> GenerateDpsCombinations()
        {
            var dpsSetNames = new[] { "선봉장", "추적자", "암살자", "복수자" };
            var allSets = EquipmentDb.SetEffects.Keys.ToList();
            var combinations = new List<EquipSetConfig>();

            // 4세트 (DPS 세트만)
            foreach (var setName in dpsSetNames)
            {
                if (allSets.Contains(setName))
                {
                    combinations.Add(new EquipSetConfig
                    {
                        WeaponSetName = setName,
                        ArmorSetName = setName,
                        Is4Set = true,
                        Description = $"{setName} 4세트"
                    });
                }
            }

            // 2+2세트 (DPS 세트끼리 조합)
            for (int i = 0; i < dpsSetNames.Length; i++)
            {
                for (int j = 0; j < allSets.Count; j++)
                {
                    if (dpsSetNames[i] == allSets[j]) continue;
                    combinations.Add(new EquipSetConfig
                    {
                        WeaponSetName = dpsSetNames[i],
                        ArmorSetName = allSets[j],
                        Is4Set = false,
                        Description = $"{dpsSetNames[i]} 2세트 + {allSets[j]} 2세트"
                    });
                }
            }

            return combinations;
        }
    }

    /// <summary>
    /// 장비 세트 조합 설정
    /// </summary>
    public class EquipSetConfig
    {
        public string WeaponSetName { get; set; }   // 무기 세트
        public string ArmorSetName { get; set; }     // 방어구 세트
        public bool Is4Set { get; set; }             // 4세트 여부 (true면 무기+방어구 동일)
        public string Description { get; set; }

        /// <summary>
        /// 이 세트 조합의 보너스 스탯 계산
        /// </summary>
        public BaseStatSet GetTotalSetBonus()
        {
            var total = new BaseStatSet();

            if (Is4Set)
            {
                // 4세트: 4세트 효과만 (2세트 대체)
                if (EquipmentDb.SetEffects.TryGetValue(WeaponSetName, out var setData))
                {
                    if (setData.TryGetValue(4, out var bonus))
                        total.Add(bonus);
                }
            }
            else
            {
                // 2+2세트: 각각 2세트 효과
                if (EquipmentDb.SetEffects.TryGetValue(WeaponSetName, out var weaponSet))
                {
                    if (weaponSet.TryGetValue(2, out var bonus))
                        total.Add(bonus);
                }
                if (EquipmentDb.SetEffects.TryGetValue(ArmorSetName, out var armorSet))
                {
                    if (armorSet.TryGetValue(2, out var bonus))
                        total.Add(bonus);
                }
            }

            return total;
        }
    }
}

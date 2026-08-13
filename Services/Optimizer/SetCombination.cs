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

            // 2. 2+2세트 (무기세트 ≠ 방어구세트). j>i만 — (A무기,B방어구)와 (B무기,A방어구)는
            //    스탯이 완전히 같다(장비 기본스탯은 Slot에만 의존하고, 세트 보너스는 2세트(A)+2세트(B) 합으로 동일).
            //    옛 코드는 순서쌍 전체(72개)를 돌아 절반이 중복 탐색이었다.
            for (int i = 0; i < setNames.Count; i++)
            {
                for (int j = i + 1; j < setNames.Count; j++)
                {
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

            // 2+2세트 (DPS 세트 하나 이상 포함). 무기/방어구 순서는 스탯상 동치라 무순서 쌍으로 중복 제거.
            var seenPairs = new HashSet<string>();
            for (int i = 0; i < dpsSetNames.Length; i++)
            {
                for (int j = 0; j < allSets.Count; j++)
                {
                    if (dpsSetNames[i] == allSets[j]) continue;
                    var pair = string.CompareOrdinal(dpsSetNames[i], allSets[j]) <= 0
                        ? $"{dpsSetNames[i]}|{allSets[j]}" : $"{allSets[j]}|{dpsSetNames[i]}";
                    if (!seenPairs.Add(pair)) continue;
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
    /// 역할 기반 장비 탐색 제약 (허용 세트·메인옵·부옵). 치확%·약확%도 허용 옵션에 포함(SiegeOptimizer.GetGearConstraints):
    ///   시뮬 딜은 실제 확률 기댓값(ExpectedCritWeak)이라 치확/약확이 딜에 반영되며, FloorFirstGear가 파티버프 포함
    ///   100%까지 채우고 초과분만 치피/공%로 재배분한다.
    /// </summary>
    public class GearConstraints
    {
        public string[] AllowedSets { get; set; }   // 허용 세트 이름 (4세트 + 2+2 조합)
        public string[] WeaponMains { get; set; }    // 무기 메인옵 후보
        public string[] ArmorMains { get; set; }     // 방어구 메인옵 후보
        public string[] SubOptions { get; set; }     // 부옵 후보
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

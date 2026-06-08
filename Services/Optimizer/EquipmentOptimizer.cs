using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

namespace GameDamageCalculator.Services.Optimizer
{
    /// <summary>
    /// 장비 옵티마이저
    ///
    /// 세트 조합 → 메인옵션 → 서브옵션 순서로 탐색하여
    /// 가장 높은 데미지를 내는 장비 구성을 찾는다.
    /// </summary>
    public class EquipmentOptimizer
    {
        private readonly DamageCalculator _damageCalc = new();
        private readonly StatCalculator _statCalc = new();

        /// <summary>
        /// 서브옵션 총 티어 한도 (4장비 × 4슬롯 × 평균 3~4티어)
        /// </summary>
        public int MaxTotalSubTiers { get; set; } = 64; // 16슬롯 × 4티어 평균

        /// <summary>
        /// 결과 상위 N개 보관
        /// </summary>
        public int TopResultCount { get; set; } = 5;

        /// <summary>
        /// 단일 캐릭터의 최적 장비 탐색
        /// </summary>
        public CharacterOptimalEquipment OptimizeForCharacter(
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex)
        {
            var sw = Stopwatch.StartNew();
            var topLoadouts = new List<RankedLoadout>();
            long searchCount = 0;

            // 1. 세트 조합 열거
            var setCombinations = SetCombination.GenerateAllCombinations();

            // 2. 각 세트 조합에 대해 최적 메인옵 + 서브옵 탐색
            foreach (var setConfig in setCombinations)
            {
                var bestForSet = FindBestMainOptions(battleChar, config, charIndex, setConfig, ref searchCount);
                if (bestForSet != null)
                {
                    topLoadouts.Add(bestForSet);
                }
            }

            // 3. 상위 결과 정렬
            topLoadouts = topLoadouts
                .OrderByDescending(r => r.EstimatedDamage)
                .Take(TopResultCount)
                .ToList();

            for (int i = 0; i < topLoadouts.Count; i++)
                topLoadouts[i].Rank = i + 1;

            sw.Stop();

            return new CharacterOptimalEquipment
            {
                CharacterName = battleChar.Character.Name,
                PartyIndex = charIndex,
                BestSetConfig = topLoadouts.FirstOrDefault()?.SetConfig,
                BestLoadout = topLoadouts.FirstOrDefault()?.Loadout,
                EstimatedDamage = topLoadouts.FirstOrDefault()?.EstimatedDamage ?? 0,
                TopLoadouts = topLoadouts
            };
        }

        /// <summary>
        /// 빠른 단일 캐릭터 최적화 (공성전 다인 탐색용), 역할 기반 제약 적용.
        /// ① 허용 세트 × (무기메인 × 방어구메인, 슬롯별 균일)을 무서브옵으로 평가해 최고 (세트·메인) 선정
        /// → ② 그 구성에만 서브옵 그리디 + 장신구 최적화. 제약으로 탐색 공간이 작아 영웅당 ~3초.
        /// gc=null이면 전체 세트 + 공격력% 메인옵으로 폴백.
        /// </summary>
        public CharacterOptimalEquipment OptimizeForCharacterFast(
            BattleCharacter battleChar, BattleConfig config, int charIndex, GearConstraints gc = null)
        {
            var weaponAvail = EquipmentDb.MainStatDb.AvailableOptions["무기"];
            var armorAvail = EquipmentDb.MainStatDb.AvailableOptions["방어구"];

            // 시도할 메인옵 (역할 제약 ∩ 슬롯 가용). 없으면 공격력%로 폴백.
            string[] weaponMains = (gc?.WeaponMains ?? new[] { "공격력%" }).Where(weaponAvail.Contains).Distinct().ToArray();
            string[] armorMains = (gc?.ArmorMains ?? new[] { "공격력%" }).Where(armorAvail.Contains).Distinct().ToArray();
            if (weaponMains.Length == 0) weaponMains = new[] { "공격력%" };
            if (armorMains.Length == 0) armorMains = new[] { "공격력%" };

            var setCombos = gc?.AllowedSets != null && gc.AllowedSets.Length > 0
                ? BuildSetCombos(gc.AllowedSets)
                : SetCombination.GenerateAllCombinations();

            EquipmentLoadout best = null;
            EquipSetConfig bestSet = null;
            string bestW = "공격력%", bestA = "공격력%";
            double bestDamage = -1;

            foreach (var setConfig in setCombos)
                foreach (var wm in weaponMains)
                    foreach (var am in armorMains)
                    {
                        var lo = BuildLoadout(setConfig, wm, wm, am, am);   // 슬롯별 균일 (근사)
                        double d = EvaluateDamage(battleChar, config, charIndex, lo);
                        if (d > bestDamage) { bestDamage = d; best = lo; bestSet = setConfig; bestW = wm; bestA = am; }
                    }

            if (best != null)
            {
                OptimizeSubOptions(best, battleChar, config, charIndex, gc?.SubOptions);
                OptimizeAccessory(best, battleChar, config, charIndex);
                bestDamage = EvaluateDamage(battleChar, config, charIndex, best);
            }

            return new CharacterOptimalEquipment
            {
                CharacterName = battleChar.Character.Name,
                PartyIndex = charIndex,
                BestSetConfig = bestSet,
                BestLoadout = best,
                EstimatedDamage = bestDamage,
                TopLoadouts = new List<RankedLoadout>(),
            };
        }

        /// <summary>
        /// 허용 세트별로 최적화된 후보 장비를 1벌씩 생성 (세트=4세트, 메인옵·부옵은 프록시 데미지로 최적화).
        /// 공성전 풀시뮬 점수로 세트를 비교하기 위한 후보 목록. 세트 라벨과 함께 반환.
        /// </summary>
        public List<(string SetName, EquipmentLoadout Loadout)> BuildSetCandidates(
            BattleCharacter battleChar, BattleConfig config, int charIndex, GearConstraints gc,
            (double Cri, double Wek) partyCritWeakFloor = default)
        {
            var weaponAvail = EquipmentDb.MainStatDb.AvailableOptions["무기"];
            var armorAvail = EquipmentDb.MainStatDb.AvailableOptions["방어구"];
            string[] weaponMains = (gc?.WeaponMains ?? new[] { "공격력%" }).Where(weaponAvail.Contains).Distinct().ToArray();
            string[] armorMains = (gc?.ArmorMains ?? new[] { "공격력%" }).Where(armorAvail.Contains).Distinct().ToArray();
            if (weaponMains.Length == 0) weaponMains = new[] { "공격력%" };
            if (armorMains.Length == 0) armorMains = new[] { "공격력%" };

            var sets = (gc?.AllowedSets != null && gc.AllowedSets.Length > 0
                    ? gc.AllowedSets
                    : EquipmentDb.SetEffects.Keys.ToArray())
                .Where(EquipmentDb.SetEffects.ContainsKey).Distinct();

            var result = new List<(string, EquipmentLoadout)>();
            foreach (var setName in sets)
            {
                var setConfig = new EquipSetConfig { WeaponSetName = setName, ArmorSetName = setName, Is4Set = true, Description = $"{setName} 4세트" };
                EquipmentLoadout best = null; double bestD = -1;
                foreach (var wm in weaponMains)
                    foreach (var am in armorMains)
                    {
                        var lo = BuildLoadout(setConfig, wm, wm, am, am);
                        double d = EvaluateDamage(battleChar, config, charIndex, lo, partyCritWeakFloor);
                        if (d > bestD) { bestD = d; best = lo; }
                    }
                if (best == null) continue;
                OptimizeSubOptions(best, battleChar, config, charIndex, gc?.SubOptions, null, partyCritWeakFloor);
                OptimizeAccessory(best, battleChar, config, charIndex);
                result.Add((setName, best));
            }
            return result;
        }

        /// <summary>
        /// 고정 세트에 대해 메인옵·부옵·장신구를 외부 스코어러(예: 공성전 풀시뮬 팀 점수)로 최적화.
        /// 세트는 이미 정해졌고, 그 안에서 메인/부옵/장신구를 풀시뮬 기준으로 고른다.
        /// </summary>
        public EquipmentLoadout OptimizeForSetFull(BattleCharacter battleChar, BattleConfig config, int charIndex,
            string setName, GearConstraints gc, Func<EquipmentLoadout, double> scorer,
            (double Cri, double Wek) partyCritWeakFloor = default)
        {
            var weaponAvail = EquipmentDb.MainStatDb.AvailableOptions["무기"];
            var armorAvail = EquipmentDb.MainStatDb.AvailableOptions["방어구"];
            string[] weaponMains = (gc?.WeaponMains ?? new[] { "공격력%" }).Where(weaponAvail.Contains).Distinct().ToArray();
            string[] armorMains = (gc?.ArmorMains ?? new[] { "공격력%" }).Where(armorAvail.Contains).Distinct().ToArray();
            if (weaponMains.Length == 0) weaponMains = new[] { "공격력%" };
            if (armorMains.Length == 0) armorMains = new[] { "공격력%" };

            var setConfig = new EquipSetConfig { WeaponSetName = setName, ArmorSetName = setName, Is4Set = true, Description = $"{setName} 4세트" };

            // 1) 메인옵: (무기메인 × 방어구메인) 조합을 스코어러로 비교
            EquipmentLoadout best = null; double bestScore = -1;
            foreach (var wm in weaponMains)
                foreach (var am in armorMains)
                {
                    var lo = BuildLoadout(setConfig, wm, wm, am, am);
                    double s = scorer(lo);
                    if (s > bestScore) { bestScore = s; best = lo; }
                }
            if (best == null) return null;

            // 2) 부옵·장신구: 스코어러 기준 그리디 (치확/약확은 floor+장비가 100% 넘으면 캡)
            OptimizeSubOptions(best, battleChar, config, charIndex, gc?.SubOptions, scorer, partyCritWeakFloor);
            OptimizeAccessory(best, battleChar, config, charIndex, scorer);
            return best;
        }

        /// <summary>허용 세트 이름 목록으로 4세트 + 2+2세트(허용끼리) 조합 생성.</summary>
        private static List<EquipSetConfig> BuildSetCombos(string[] allowed)
        {
            var valid = allowed.Where(s => EquipmentDb.SetEffects.ContainsKey(s)).ToList();
            var combos = new List<EquipSetConfig>();
            foreach (var s in valid)
                combos.Add(new EquipSetConfig { WeaponSetName = s, ArmorSetName = s, Is4Set = true, Description = $"{s} 4세트" });
            for (int i = 0; i < valid.Count; i++)
                for (int j = 0; j < valid.Count; j++)
                {
                    if (i == j) continue;
                    combos.Add(new EquipSetConfig { WeaponSetName = valid[i], ArmorSetName = valid[j], Is4Set = false,
                        Description = $"{valid[i]} 2 + {valid[j]} 2" });
                }
            return combos;
        }

        /// <summary>
        /// 파티 전체 최적화 (각 캐릭터 독립 최적화)
        /// </summary>
        public OptimizerResult OptimizeParty(BattleConfig config)
        {
            var sw = Stopwatch.StartNew();
            var result = new OptimizerResult();
            long totalSearchCount = 0;

            for (int i = 0; i < config.AllyParty.Count; i++)
            {
                var charResult = OptimizeForCharacter(config.AllyParty[i], config, i);
                result.CharacterResults.Add(charResult);
                totalSearchCount += charResult.TopLoadouts.Sum(t => 1);
            }

            // 최적 장비 적용 후 배틀 시뮬레이션
            var optimizedConfig = ApplyOptimalEquipment(config, result);
            var battleSim = new BattleSimulator();
            var battleResult = battleSim.Simulate(optimizedConfig);
            result.EstimatedTotalDamage = battleResult.TotalDamage;

            sw.Stop();
            result.TotalCombinationsSearched = totalSearchCount;
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            return result;
        }

        #region 메인옵션 탐색

        /// <summary>
        /// 주어진 세트 조합에서 최적 메인옵션 찾기
        /// </summary>
        private RankedLoadout FindBestMainOptions(
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex,
            EquipSetConfig setConfig,
            ref long searchCount)
        {
            var weaponOptions = EquipmentDb.MainStatDb.AvailableOptions["무기"];
            var armorOptions = EquipmentDb.MainStatDb.AvailableOptions["방어구"];

            // DPS에 관련 없는 옵션 필터링
            var dpsWeaponOptions = FilterDpsOptions(weaponOptions, "무기");
            var dpsArmorOptions = FilterDpsOptions(armorOptions, "방어구");

            RankedLoadout best = null;

            // 무기1 메인옵 × 무기2 메인옵 × 방어구1 메인옵 × 방어구2 메인옵
            foreach (var w1 in dpsWeaponOptions)
            {
                foreach (var w2 in dpsWeaponOptions)
                {
                    foreach (var a1 in dpsArmorOptions)
                    {
                        foreach (var a2 in dpsArmorOptions)
                        {
                            searchCount++;

                            var loadout = BuildLoadout(setConfig, w1, w2, a1, a2);

                            // 서브옵션 그리디 배분
                            OptimizeSubOptions(loadout, battleChar, config, charIndex);

                            // 장신구 최적화
                            OptimizeAccessory(loadout, battleChar, config, charIndex);

                            // 데미지 평가
                            double damage = EvaluateDamage(battleChar, config, charIndex, loadout);

                            if (best == null || damage > best.EstimatedDamage)
                            {
                                best = new RankedLoadout
                                {
                                    SetConfig = setConfig,
                                    Loadout = loadout,
                                    EstimatedDamage = damage,
                                    Description = $"{setConfig.Description} | 무기: {w1},{w2} | 방어구: {a1},{a2}"
                                };
                            }
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// DPS 관련 메인옵션만 필터링
        /// </summary>
        private string[] FilterDpsOptions(string[] options, string slot)
        {
            // DPS에 기여하는 옵션만 선택
            var dpsOptions = new HashSet<string>
            {
                "공격력%", "공격력", "치명타확률%", "치명타피해%", "약점공격확률%"
            };

            return options.Where(o => dpsOptions.Contains(o)).ToArray();
        }

        #endregion

        #region 서브옵션 그리디 배분

        /// <summary>
        /// 서브옵션 최적 배분 — 게임 규칙 반영.
        /// 장비마다 부옵 4개(서로 다른 스탯, 메인옵 제외)가 전부 1티어로 시작하고,
        /// 15강까지 강화로 랜덤 티어업이 총 5번 발생 → 한 장비의 부옵 티어 합 = 4(기본) + 5(강화) = 9, 슬롯당 최대 6티어.
        /// 옵티마이저는 "어떤 부옵 4개 + 5번 상승을 어디에"를 데미지 기준 그리디로 결정 (장비별 독립).
        /// 값 = 기본 부옵 스탯 × 티어 (Equipment.SubStatSlot.GetStats).
        /// </summary>
        private void OptimizeSubOptions(
            EquipmentLoadout loadout,
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex,
            string[] subStatNames = null,
            Func<EquipmentLoadout, double> scorer = null,
            (double Cri, double Wek) partyCritWeakFloor = default)
        {
            const int SubSlotsPerEquip = 4;   // 부옵 슬롯 수
            const int TierUps = 5;             // 15강 동안 랜덤 티어업 횟수
            const int MaxTier = 6;             // 1(기본) + 5(전부 한 슬롯에)
            subStatNames ??= GetDpsSubStatNames();
            double Score(EquipmentLoadout lo) => scorer != null ? scorer(lo) : EvaluateDamage(battleChar, config, charIndex, lo, partyCritWeakFloor);

            // 저점-우선 캡: 치확/약확이 100% 도달하면 그 이상은 순수 낭비 → 후보 거절(자연스럽게 치피/공%로 우회).
            // 기준 = 프록시 DisplayStats(base·초월·펫·진형·장비) + 풀파티 버스트 버프 floor(비스킷 약확54·레이첼 약확27 등).
            bool ExceedsCritWeakCap(string statName)
            {
                if (statName != "치명타확률%" && statName != "약점공격확률%") return false;
                var (sr, _) = ComputeStatResult(loadout, battleChar, config, charIndex);
                if (sr.DisplayStats == null) return false;
                if (statName == "치명타확률%" && sr.DisplayStats.Cri + partyCritWeakFloor.Cri >= 100) return true;
                if (statName == "약점공격확률%" && sr.DisplayStats.Wek + partyCritWeakFloor.Wek >= 100) return true;
                return false;
            }

            foreach (var equip in loadout.GetEquipments())
            {
                int nSlots = System.Math.Min(SubSlotsPerEquip, equip.SubSlots.Count);
                for (int s = 0; s < equip.SubSlots.Count; s++) { equip.SubSlots[s].StatName = ""; equip.SubSlots[s].Tier = 0; }

                // 후보 부옵: 허용 목록 ∩ (메인옵 제외)
                var cands = subStatNames.Where(n => n != equip.MainStatName).Distinct().ToList();

                // 1) 서로 다른 부옵 4개를 1티어로 채움 (그리디: 점수 최대 / 치확·약확은 캡 초과 시 거절)
                var used = new HashSet<string>();
                for (int i = 0; i < nSlots; i++)
                {
                    var slot = equip.SubSlots[i];
                    string bestStat = null; double bestDmg = -1;
                    foreach (var stat in cands)
                    {
                        if (used.Contains(stat)) continue;
                        slot.StatName = stat; slot.Tier = 1;
                        if (ExceedsCritWeakCap(stat)) { slot.StatName = ""; slot.Tier = 0; continue; }   // 캡 초과 → 거절
                        double d = Score(loadout);
                        if (d > bestDmg) { bestDmg = d; bestStat = stat; }
                        slot.StatName = ""; slot.Tier = 0;
                    }
                    if (bestStat == null) break;
                    slot.StatName = bestStat; slot.Tier = 1; used.Add(bestStat);
                }

                // 2) 티어업 5번 분배 (각 슬롯 최대 6티어, 점수 증가 최대 슬롯에 / 캡 도달 슬롯 스킵)
                for (int up = 0; up < TierUps; up++)
                {
                    int bestSlot = -1; double bestDmg = -1;
                    for (int i = 0; i < nSlots; i++)
                    {
                        var slot = equip.SubSlots[i];
                        if (string.IsNullOrEmpty(slot.StatName) || slot.Tier >= MaxTier) continue;
                        slot.Tier++;
                        if (ExceedsCritWeakCap(slot.StatName)) { slot.Tier--; continue; }   // 캡 초과 → 거절
                        double d = Score(loadout);
                        if (d > bestDmg) { bestDmg = d; bestSlot = i; }
                        slot.Tier--;
                    }
                    if (bestSlot < 0) break;
                    equip.SubSlots[bestSlot].Tier++;
                }
            }
        }

        /// <summary>
        /// DPS 관련 서브옵션 이름 목록
        /// </summary>
        private string[] GetDpsSubStatNames()
        {
            return new[]
            {
                "공격력%", "공격력", "치명타확률%", "치명타피해%",
                "약점공격확률%", "속공"
            };
        }

        #endregion

        #region 장신구 최적화

        /// <summary>
        /// 최적 장신구 찾기
        /// </summary>
        private void OptimizeAccessory(
            EquipmentLoadout loadout,
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex,
            Func<EquipmentLoadout, double> scorer = null)
        {
            double Score(EquipmentLoadout lo) => scorer != null ? scorer(lo) : EvaluateDamage(battleChar, config, charIndex, lo);
            double bestDamage = -1;
            Accessory bestAccessory = null;

            var grades = new[] { 6, 5, 4 }; // 높은 등급부터

            foreach (var grade in grades)
            {
                if (!AccessoryDb.MainOptions.ContainsKey(grade)) continue;
                var mainOptions = AccessoryDb.MainOptions[grade].Keys.ToList();

                foreach (var mainOpt in mainOptions)
                {
                    if (grade == 6 && AccessoryDb.SubOptions.ContainsKey(6))
                    {
                        // 6성: 메인 + 서브
                        foreach (var subOpt in AccessoryDb.SubOptions[6].Keys)
                        {
                            if (subOpt == mainOpt) continue; // 메인과 서브 중복 불가 (가정)
                            var accessory = new Accessory { Grade = grade, MainOption = mainOpt, SubOption = subOpt };
                            loadout.Accessory = accessory;
                            double damage = Score(loadout);
                            if (damage > bestDamage)
                            {
                                bestDamage = damage;
                                bestAccessory = accessory;
                            }
                        }
                    }
                    else
                    {
                        // 4~5성: 메인만
                        var accessory = new Accessory { Grade = grade, MainOption = mainOpt };
                        loadout.Accessory = accessory;
                        double damage = Score(loadout);
                        if (damage > bestDamage)
                        {
                            bestDamage = damage;
                            bestAccessory = accessory;
                        }
                    }
                }
            }

            loadout.Accessory = bestAccessory ?? new Accessory { Grade = 6, MainOption = "피증%" };
        }

        #endregion

        #region 데미지 평가

        /// <summary>
        /// 영웅의 StatCalculationResult + 파티 디버프 합산을 계산 (EvaluateDamage 및 캡체크 공용).
        /// 파티 패시브 + 펫 + 진형 + 장비 전부 포함. (턴제 스킬버프는 미포함 — 프록시는 패시브 기반)
        /// </summary>
        private (StatCalculationResult Stat, DebuffSet Debuffs) ComputeStatResult(
            EquipmentLoadout loadout,
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex)
        {
            var character = battleChar.Character;

            var activeSets = loadout.GetActiveSets();
            string equipSetName = "";
            int equipSetCount = 0;
            foreach (var set in activeSets)
            {
                if (set.PieceCount > equipSetCount)
                {
                    equipSetName = set.SetName;
                    equipSetCount = set.PieceCount;
                }
            }

            var partyBuffConfigs = BuildPartyBuffConfigs(config, charIndex);
            var partyEffects = new EffectManager();
            partyEffects.AddEffects(EffectConverter.FromBuffConfigs(
                partyBuffConfigs, config.AllyPet, config.PetStar, config.PetEnhance, battleChar.Character.Type));
            var (partyPerm, partyTimed, partyPet) = partyEffects.GetSeparatedBuffs();
            var totalDebuffs = partyEffects.GetTotalDebuffs();

            var formation = new Formation
            {
                Name = config.FormationName,
                IsBackPosition = battleChar.IsBackPosition
            };

            var statInput = new StatCalculationInput
            {
                Character = character,
                TranscendLevel = battleChar.TranscendLevel,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                IsPassiveConditionMet = battleChar.IsPassiveConditionMet,
                Equipments = loadout.GetEquipments(),
                EquipSetName = equipSetName,
                EquipSetCount = equipSetCount,
                PotentialAtkLevel = battleChar.PotentialAtkLevel,
                PotentialDefLevel = battleChar.PotentialDefLevel,
                PotentialHpLevel = battleChar.PotentialHpLevel,
                Accessory = loadout.Accessory,
                Formation = formation,
                Pet = config.AllyPet,
                PetStar = config.PetStar,
                PetOptionAtkRate = config.PetOptionAtkRate,
                PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate,
                TotalBuffs = partyEffects.GetTotalBuffs(),
                TotalDebuffs = totalDebuffs,
                PartyPermanentBuffs = partyPerm,
                PartyTimedBuffs = partyTimed,
                PartyPetBuffs = partyPet
            };

            return (_statCalc.Calculate(statInput), totalDebuffs);
        }

        /// <summary>
        /// (가 가정) 딜러가 버스트 윈도우에 받는 파티 치확/약확 합 (패시브 + 스킬버프).
        /// 기어 최적화는 SoloConfig로 돌아 파티버프가 안 보이므로, 풀파티에서 따로 계산해 floor로 주입한다.
        /// 대상 선정(예: 비스킷 장비강화=공격력최고 아군)·버프 업타임은 무시하고 "받는다"고 가정.
        /// </summary>
        public (double Cri, double Wek) PartyBuffCritWeak(
            System.Collections.Generic.List<BattleCharacter> party, int charIndex, string dealerClass)
        {
            double cri = 0, wek = 0;
            void AccBuff(BuffSet b) { if (b != null) { cri += b.Cri; wek += b.Wek; } }
            void AccEffects(System.Collections.Generic.IEnumerable<Models.Effects.SkillEffect> effs)
            {
                if (effs == null) return;
                foreach (var e in effs)
                {
                    if (e.Type != Models.Effects.SkillEffectType.Buff || e.Target != Models.Effects.EffectTarget.Party || e.Buff == null) continue;
                    if (e.TargetClasses != null && dealerClass != null && !System.Linq.Enumerable.Contains(e.TargetClasses, dealerClass)) continue;
                    AccBuff(e.Buff);
                }
            }
            for (int i = 0; i < party.Count; i++)
            {
                if (i == charIndex || party[i]?.Character == null) continue;
                var bc = party[i];
                bool enh = bc.IsSkillEnhanced; int tx = bc.TranscendLevel;
                // 패시브 파티버프 (상시 + 조건부)
                AccBuff(bc.Character.Passive?.GetPartyBuff(enh, tx, dealerClass));
                AccBuff(bc.Character.Passive?.GetConditionalPartyBuff(enh, tx, dealerClass));
                // 스킬 파티버프 (Effects + 레거시 PartyBuff + 초월)
                foreach (var skill in bc.Character.Skills ?? System.Linq.Enumerable.Empty<Skill>())
                {
                    var ld = skill.GetLevelData(enh);
                    AccEffects(ld?.Effects);
                    AccBuff(ld?.PartyBuff);
                    var txb = skill.GetTranscendBonus(tx);
                    AccEffects(txb?.Effects);
                    AccBuff(txb?.PartyBuff);
                }
            }
            return (cri, wek);
        }

        private double EvaluateDamage(
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex,
            EquipmentLoadout loadout,
            (double Cri, double Wek) partyCritWeakFloor = default)
        {
            var character = battleChar.Character;
            var (statResult, totalDebuffs) = ComputeStatResult(loadout, battleChar, config, charIndex);

            // 대표 스킬: 가장 높은 배율의 스킬 선택
            var bestSkill = character.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .OrderByDescending(s => s.GetLevelData(battleChar.IsSkillEnhanced)?.Ratio ?? 0)
                .FirstOrDefault();

            if (bestSkill == null)
                bestSkill = character.Skills?.FirstOrDefault();

            if (bestSkill == null) return 0;

            var enemy = config.TargetEnemy;
            var damageInput = new DamageCalculator.DamageInput
            {
                Character = character,
                Skill = bestSkill,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                TranscendLevel = battleChar.TranscendLevel,
                FinalAtk = statResult.FinalAtk,
                FinalDef = statResult.FinalDef,
                FinalHp = statResult.FinalHp,
                CritDamage = statResult.DisplayStats.Cri_Dmg,
                DmgDealt = statResult.DisplayStats.Dmg_Dealt,
                DmgDealtType = statResult.DisplayStats.Dmg_Dealt_Type,
                DmgDealtBoss = statResult.DisplayStats.Dmg_Dealt_Bos,
                ArmorPen = statResult.DisplayStats.Arm_Pen,
                WeakpointDmg = statResult.DisplayStats.Wek_Dmg,
                Dmg1to3 = statResult.DisplayStats.Dmg_Dealt_1to3,
                Dmg4to5 = statResult.DisplayStats.Dmg_Dealt_4to5,
                // 치명·약점 확률 기반 기댓값 — 치확/약확 메인옵·부옵 탐색이 의미를 갖도록.
                // 파티 버스트 버프(floor)를 더해 realized 크리/약점 반영 → 캡(100%) 넘는 기어 치확/약확은 한계효용 0이 됨.
                ExpectedCritWeak = true,
                CritChance = statResult.DisplayStats.Cri + partyCritWeakFloor.Cri,
                WeakChance = statResult.DisplayStats.Wek + partyCritWeakFloor.Wek,
                DefReduction = totalDebuffs.Def_Reduction,
                DmgTakenIncrease = totalDebuffs.GetEffectiveDmgTakenIncrease(character.AttackType),
                Vulnerability = totalDebuffs.Vulnerability + (enemy?.Vulnerability ?? 0),
                BossVulnerability = totalDebuffs.Boss_Vulnerability,
                BossDef = enemy?.Stats?.Def ?? 0,
                BossDefIncrease = enemy?.DefenseIncrease ?? 0,
                BossDmgReduction = enemy?.DamageReduction ?? 0,
                BossTargetReduction = GetTargetReduction(enemy, bestSkill.GetTargetCount(battleChar.IsSkillEnhanced, battleChar.TranscendLevel)),
                BossHp = enemy?.Stats?.Hp ?? 0,
                TargetHp = enemy?.Stats?.Hp ?? 0,
                TargetCurrentHp = enemy?.Stats?.Hp ?? 0,
                IsCritical = true,
                IsWeakpoint = true,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = enemy?.IsBoss ?? true,
                SelfMaxHp = statResult.FinalHp
            };

            var result = _damageCalc.Calculate(damageInput);
            return result.FinalDamage;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 장비 한벌 생성
        /// </summary>
        private EquipmentLoadout BuildLoadout(EquipSetConfig setConfig,
            string w1Main, string w2Main, string a1Main, string a2Main)
        {
            return new EquipmentLoadout
            {
                Weapon1 = CreateEquipment("무기1", "무기", setConfig.WeaponSetName, w1Main),
                Weapon2 = CreateEquipment("무기2", "무기", setConfig.WeaponSetName, w2Main),
                Armor1 = CreateEquipment("방어구1", "방어구", setConfig.ArmorSetName, a1Main),
                Armor2 = CreateEquipment("방어구2", "방어구", setConfig.ArmorSetName, a2Main),
                Accessory = new Accessory { Grade = 6, MainOption = "피증%" }
            };
        }

        private Equipment CreateEquipment(string name, string slot, string setName, string mainStatName)
        {
            var equip = new Equipment
            {
                Name = name,
                Slot = slot,
                SetName = setName,
                MainStatName = mainStatName,
                SubSlots = new System.Collections.ObjectModel.ObservableCollection<SubStatSlot>
                {
                    new SubStatSlot(),
                    new SubStatSlot(),
                    new SubStatSlot(),
                    new SubStatSlot()
                }
            };
            return equip;
        }

        private List<BuffConfig> BuildPartyBuffConfigs(BattleConfig config, int excludeIndex)
        {
            var configs = new List<BuffConfig>();
            for (int i = 0; i < config.AllyParty.Count; i++)
            {
                if (i == excludeIndex) continue;
                var bc = config.AllyParty[i];
                configs.Add(new BuffConfig
                {
                    CharacterName = bc.Character.Name,
                    SkillName = null,
                    IsBuff = true,
                    IsChecked = true,
                    Level = GetBuffLevel(bc.IsSkillEnhanced, bc.TranscendLevel)
                });
                configs.Add(new BuffConfig
                {
                    CharacterName = bc.Character.Name,
                    SkillName = null,
                    IsBuff = false,
                    IsChecked = true,
                    Level = GetBuffLevel(bc.IsSkillEnhanced, bc.TranscendLevel)
                });
            }
            return configs;
        }

        private int GetBuffLevel(bool isEnhanced, int transcendLevel)
        {
            if (isEnhanced && transcendLevel >= 6) return 3;
            if (isEnhanced) return 1;
            if (transcendLevel >= 6) return 2;
            return 0;
        }

        private double GetTargetReduction(Enemy enemy, int targetCount)
        {
            if (enemy == null) return 0;
            return targetCount switch
            {
                1 => enemy.SingleTargetReduction,
                3 => enemy.TripleTargetReduction,
                >= 5 => enemy.MultiTargetReduction,
                _ => 0
            };
        }

        /// <summary>
        /// 최적 장비를 BattleConfig에 적용
        /// </summary>
        private BattleConfig ApplyOptimalEquipment(BattleConfig original, OptimizerResult optResult)
        {
            var config = new BattleConfig
            {
                AllyParty = new List<BattleCharacter>(),
                FormationName = original.FormationName,
                TargetEnemy = original.TargetEnemy,
                AllyPet = original.AllyPet,
                PetStar = original.PetStar,
                PetEnhance = original.PetEnhance,
                PetOptionAtkRate = original.PetOptionAtkRate,
                PetOptionDefRate = original.PetOptionDefRate,
                PetOptionHpRate = original.PetOptionHpRate,
                MaxTurns = original.MaxTurns,
                RotationMode = original.RotationMode,
                UserRotations = original.UserRotations,
                EnemyRotation = original.EnemyRotation
            };

            for (int i = 0; i < original.AllyParty.Count; i++)
            {
                var origChar = original.AllyParty[i];
                var optimalEquip = optResult.CharacterResults
                    .FirstOrDefault(r => r.PartyIndex == i)?.BestLoadout;

                config.AllyParty.Add(new BattleCharacter
                {
                    Character = origChar.Character,
                    TranscendLevel = origChar.TranscendLevel,
                    IsSkillEnhanced = origChar.IsSkillEnhanced,
                    IsPassiveConditionMet = origChar.IsPassiveConditionMet,
                    Equipment = optimalEquip ?? origChar.Equipment,
                    PotentialAtkLevel = origChar.PotentialAtkLevel,
                    PotentialDefLevel = origChar.PotentialDefLevel,
                    PotentialHpLevel = origChar.PotentialHpLevel,
                    IsBackPosition = origChar.IsBackPosition
                });
            }

            return config;
        }

        #endregion
    }
}

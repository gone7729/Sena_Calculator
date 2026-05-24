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
        /// 서브옵션을 그리디 방식으로 최적 배분
        /// 1티어당 데미지 증가량이 가장 큰 옵션에 우선 배분
        /// </summary>
        private void OptimizeSubOptions(
            EquipmentLoadout loadout,
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex)
        {
            var equipments = loadout.GetEquipments().ToList();
            var subStatNames = GetDpsSubStatNames();

            // 총 배분 가능한 티어 포인트
            int remainingTiers = MaxTotalSubTiers;

            // 각 장비의 4슬롯을 초기화
            foreach (var equip in equipments)
            {
                for (int s = 0; s < equip.SubSlots.Count; s++)
                {
                    equip.SubSlots[s].StatName = "";
                    equip.SubSlots[s].Tier = 0;
                }
            }

            // 그리디: 각 스텝에서 1티어 추가 시 가장 데미지가 많이 오르는 슬롯에 배분
            // 효율을 위해 미리 각 서브옵 종류별 1티어 효과를 한 번만 계산
            var baseDamage = EvaluateDamage(battleChar, config, charIndex, loadout);

            // 슬롯별 현재 할당된 서브옵
            var slotAssignments = new List<(Equipment equip, int slotIdx, string statName, int tier)>();

            // 각 장비 × 슬롯 × 가능한 서브옵 조합에서 가장 효율 좋은 것 선택
            while (remainingTiers > 0)
            {
                double bestGain = 0;
                Equipment bestEquip = null;
                int bestSlotIdx = -1;
                string bestStatName = null;

                foreach (var equip in equipments)
                {
                    for (int s = 0; s < equip.SubSlots.Count; s++)
                    {
                        var slot = equip.SubSlots[s];

                        if (string.IsNullOrEmpty(slot.StatName))
                        {
                            // 빈 슬롯: 각 서브옵 후보 시도
                            foreach (var statName in subStatNames)
                            {
                                // 같은 장비 내 중복 서브옵 불가
                                if (equip.SubSlots.Any(ss => ss.StatName == statName && ss != slot))
                                    continue;

                                // 메인옵과 동일한 서브옵 불가
                                if (equip.MainStatName == statName)
                                    continue;

                                slot.StatName = statName;
                                slot.Tier = 1;
                                double newDamage = EvaluateDamage(battleChar, config, charIndex, loadout);
                                double gain = newDamage - baseDamage;

                                if (gain > bestGain)
                                {
                                    bestGain = gain;
                                    bestEquip = equip;
                                    bestSlotIdx = s;
                                    bestStatName = statName;
                                }

                                slot.StatName = "";
                                slot.Tier = 0;
                            }
                        }
                        else if (slot.Tier < 6)
                        {
                            // 이미 할당된 슬롯: 티어 1 증가
                            slot.Tier++;
                            double newDamage = EvaluateDamage(battleChar, config, charIndex, loadout);
                            double gain = newDamage - baseDamage;

                            if (gain > bestGain)
                            {
                                bestGain = gain;
                                bestEquip = equip;
                                bestSlotIdx = s;
                                bestStatName = slot.StatName;
                            }

                            slot.Tier--;
                        }
                    }
                }

                // 더 이상 개선 불가
                if (bestEquip == null || bestGain <= 0)
                    break;

                // 최적 슬롯에 1티어 배분
                var bestSlot = bestEquip.SubSlots[bestSlotIdx];
                if (string.IsNullOrEmpty(bestSlot.StatName))
                {
                    bestSlot.StatName = bestStatName;
                    bestSlot.Tier = 1;
                }
                else
                {
                    bestSlot.Tier++;
                }

                baseDamage = EvaluateDamage(battleChar, config, charIndex, loadout);
                remainingTiers--;
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
            int charIndex)
        {
            double bestDamage = 0;
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
                            double damage = EvaluateDamage(battleChar, config, charIndex, loadout);
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
                        double damage = EvaluateDamage(battleChar, config, charIndex, loadout);
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
        /// 주어진 장비 구성으로 기대 데미지 평가
        /// 대표 스킬 1회 사용 기준
        /// </summary>
        private double EvaluateDamage(
            BattleCharacter battleChar,
            BattleConfig config,
            int charIndex,
            EquipmentLoadout loadout)
        {
            var character = battleChar.Character;

            // 세트 정보
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

            // 파티 버프 (간소화: 패시브만 고려)
            var partyBuffConfigs = BuildPartyBuffConfigs(config, charIndex);
            var partyEffects = new EffectManager();
            partyEffects.AddEffects(EffectConverter.FromBuffConfigs(
                partyBuffConfigs, config.AllyPet, config.PetStar, config.PetEnhance));
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

            var statResult = _statCalc.Calculate(statInput);

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

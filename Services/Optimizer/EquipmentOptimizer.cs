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
        /// 허용 세트 조합별로 최적화된 후보 장비를 1벌씩 생성 (메인옵·부옵은 프록시 데미지로 최적화).
        /// 공성전 풀시뮬 점수로 세트를 비교하기 위한 후보 목록. 세트 조합과 함께 반환.
        /// 4세트뿐 아니라 2+2세트도 후보에 포함한다 — 옛 코드는 4세트만 만들어 공성 기어가 2+2를
        /// 아예 탐색하지 못했다(예: 암살자2(치확15)+복수자2(피증15) 같은 혼합이 후보에조차 없었음).
        /// </summary>
        public List<(EquipSetConfig Set, EquipmentLoadout Loadout)> BuildSetCandidates(
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
                .Where(EquipmentDb.SetEffects.ContainsKey).Distinct().ToArray();

            var result = new List<(EquipSetConfig, EquipmentLoadout)>();
            foreach (var setConfig in BuildSetCombos(sets))
            {
                // 메인옵 × 부옵 조인트 (OptimizeForSetFull과 동일 원칙, 프록시 데미지 기준): 메인 후보마다 부옵까지
                //   최적화 후 비교 → 포화 치확/약확을 굵은 메인에 낭비하지 않고 부옵으로 캡 맞추는 빌드를 본다.
                EquipmentLoadout best = null; double bestD = -1;
                foreach (var wm in weaponMains)
                    foreach (var am in armorMains)
                    {
                        var lo = BuildLoadout(setConfig, wm, wm, am, am);
                        OptimizeSubOptions(lo, battleChar, config, charIndex, gc?.SubOptions, null, partyCritWeakFloor);
                        double d = EvaluateDamage(battleChar, config, charIndex, lo, partyCritWeakFloor);
                        if (d > bestD) { bestD = d; best = lo; }
                    }
                if (best == null) continue;
                OptimizeAccessory(best, battleChar, config, charIndex);
                result.Add((setConfig, best));
            }
            return result;
        }

        /// <summary>
        /// 고정 세트에 대해 메인옵·부옵·장신구를 외부 스코어러(예: 공성전 풀시뮬 팀 점수)로 최적화.
        /// 세트는 이미 정해졌고, 그 안에서 메인/부옵/장신구를 풀시뮬 기준으로 고른다.
        /// </summary>
        public EquipmentLoadout OptimizeForSetFull(BattleCharacter battleChar, BattleConfig config, int charIndex,
            EquipSetConfig setConfig, GearConstraints gc, Func<EquipmentLoadout, double> scorer,
            (double Cri, double Wek) partyCritWeakFloor = default)
        {
            if (setConfig == null) return null;
            var weaponAvail = EquipmentDb.MainStatDb.AvailableOptions["무기"];
            var armorAvail = EquipmentDb.MainStatDb.AvailableOptions["방어구"];
            string[] weaponMains = (gc?.WeaponMains ?? new[] { "공격력%" }).Where(weaponAvail.Contains).Distinct().ToArray();
            string[] armorMains = (gc?.ArmorMains ?? new[] { "공격력%" }).Where(armorAvail.Contains).Distinct().ToArray();
            if (weaponMains.Length == 0) weaponMains = new[] { "공격력%" };
            if (armorMains.Length == 0) armorMains = new[] { "공격력%" };

            // 1) 메인옵 × 부옵 조인트: 각 (무기메인×방어구메인) 후보마다 부옵까지 최적화한 뒤 전체 점수로 비교.
            //    메인 치확(24) = 부옵 치확 최대(24)로 동치라, 메인을 부옵과 분리해 고르면 포화 스탯(치확/약확)을
            //    굵은 메인 슬롯에 낭비하는 오선택이 난다(라이언: 치확 포화인데 치확 메인 채택→치피 메인 대비 −4%).
            //    부옵까지 포함해 평가해야 "치피를 굵은 메인에 + 치확을 잘게 부옵으로 캡 맞춤"을 옵티마이저가 스스로 본다.
            EquipmentLoadout best = null; double bestScore = -1;
            foreach (var wm in weaponMains)
                foreach (var am in armorMains)
                {
                    var lo = BuildLoadout(setConfig, wm, wm, am, am);
                    OptimizeSubOptions(lo, battleChar, config, charIndex, gc?.SubOptions, scorer, partyCritWeakFloor);
                    double s = scorer(lo);
                    if (s > bestScore) { bestScore = s; best = lo; }
                }
            if (best == null) return null;

            // 2) 장신구: 스코어러 기준 (부옵은 위 조인트 루프에서 메인별로 이미 최적화됨)
            //    JointAccessory ON이면 장신구↔부옵 조인트(치확/약확 장신구가 캡 공급 시 부옵 치피 전환) 비교.
            if (JointAccessory)
                OptimizeAccessoryJoint(best, battleChar, config, charIndex, gc?.SubOptions, scorer, partyCritWeakFloor);
            else
                OptimizeAccessory(best, battleChar, config, charIndex, scorer);
            return best;
        }

        /// <summary>허용 세트 이름 목록으로 4세트 + 2+2세트(허용끼리) 조합 생성.</summary>
        public static List<EquipSetConfig> BuildSetCombos(string[] allowed)
        {
            var valid = allowed.Where(s => EquipmentDb.SetEffects.ContainsKey(s)).ToList();
            var combos = new List<EquipSetConfig>();
            foreach (var s in valid)
                combos.Add(new EquipSetConfig { WeaponSetName = s, ArmorSetName = s, Is4Set = true, Description = $"{s} 4세트" });
            // j>i만 — (A무기,B방어구)와 (B무기,A방어구)는 스탯 동치(장비 기본스탯은 Slot에만 의존,
            //   세트 보너스는 2세트(A)+2세트(B) 합으로 같음)이므로 순서쌍 전수는 절반이 중복.
            for (int i = 0; i < valid.Count; i++)
                for (int j = i + 1; j < valid.Count; j++)
                {
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

            // 무기1 메인옵 × 무기2 메인옵 × 방어구1 메인옵 × 방어구2 메인옵.
            //   무기1/무기2(방어구1/2)는 슬롯·기본스탯이 완전히 같아 (A,B)와 (B,A)가 동치 → 비내림차순 조합만
            //   훑는다(옛 순서쌍 전수 대비 약 2.2배 절감). 내부에서 부옵 그리디 + 장신구 전수를 돌므로 체감 큼.
            for (int wi = 0; wi < dpsWeaponOptions.Length; wi++)
            {
                for (int wj = wi; wj < dpsWeaponOptions.Length; wj++)
                {
                    string w1 = dpsWeaponOptions[wi], w2 = dpsWeaponOptions[wj];
                    for (int ai = 0; ai < dpsArmorOptions.Length; ai++)
                    {
                        for (int aj = ai; aj < dpsArmorOptions.Length; aj++)
                        {
                            string a1 = dpsArmorOptions[ai], a2 = dpsArmorOptions[aj];
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
        /// 장비마다 부옵 4개(서로 다른 스탯, 단 메인옵과 같은 스탯은 부옵으로 허용 — 합산)가 전부 1티어로 시작하고,
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

            // 대표(최고 배율) 스킬의 내장 치확/약확(예: 파스칼 파괴의거인 확정치명 Cri=100). 캡 판정에 합산해
            //   이미 확정인 치확/약확 부옵을 낭비 배분하지 않게 한다. (데미지 평가는 DamageCalculator가 직접 반영)
            var capSkill = battleChar.Character?.Skills?
                .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                .OrderByDescending(s => s.GetLevelData(battleChar.IsSkillEnhanced)?.Ratio ?? 0)
                .FirstOrDefault();
            var capBonus = capSkill?.GetTotalBonus(battleChar.IsSkillEnhanced, battleChar.TranscendLevel) ?? new BuffSet();

            // 캡 판정 = "추가 前에 이미 100%인가". 이미 캡이면 그 부옵은 한계효용 0인 죽은 옵션이라 거절한다.
            //   옛 코드는 후보 티어를 반영한 뒤 ">= 100"이면 거절해서, 정확히 100을 맞추는 배분은 물론
            //   96→100.8처럼 캡을 살짝 넘기며 실효 치확을 100까지 끌어올리는 티어까지 전부 막았다(치확이
            //   96~99%에서 멈추는 언더슈트). 데미지 평가는 확률을 100으로 클램프한 기댓값이라 초과분 가치는
            //   이미 0으로 반영되므로, 캡 미만이면 거절하지 말고 Score 비교에 맡기는 게 옳다.
            //   기준 = 프록시 DisplayStats(base·초월·펫·진형·장비) + 파티 버스트 floor + 스킬 내장 확정치명(capBonus).
            (bool Cri, bool Wek) CappedNow()
            {
                var (sr, _) = ComputeStatResult(loadout, battleChar, config, charIndex);
                if (sr.DisplayStats == null) return (false, false);
                return (sr.DisplayStats.Cri + partyCritWeakFloor.Cri + capBonus.Cri >= 100,
                        sr.DisplayStats.Wek + partyCritWeakFloor.Wek + capBonus.Wek >= 100);
            }
            static bool IsDeadStat(string stat, (bool Cri, bool Wek) capped) =>
                (stat == "치명타확률%" && capped.Cri) || (stat == "약점공격확률%" && capped.Wek);

            foreach (var equip in loadout.GetEquipments())
            {
                int nSlots = System.Math.Min(SubSlotsPerEquip, equip.SubSlots.Count);
                for (int s = 0; s < equip.SubSlots.Count; s++) { equip.SubSlots[s].StatName = ""; equip.SubSlots[s].Tier = 0; }

                // 후보 부옵: 허용 목록 전체. 게임 규칙상 부옵은 메인옵과 같은 스탯도 붙을 수 있다(합산).
                //   (부옵끼리만 서로 다름 — 아래 used 해시셋이 보장. 메인=부옵은 허용.)
                var cands = subStatNames.Distinct().ToList();

                // 1) 서로 다른 부옵 4개를 1티어로 채움 (그리디: 점수 최대 / 이미 캡인 치확·약확은 거절)
                var used = new HashSet<string>();
                for (int i = 0; i < nSlots; i++)
                {
                    var slot = equip.SubSlots[i];
                    // 이 슬롯을 채우기 前 상태의 캡 여부. 슬롯이 비어 있으므로 후보마다 재계산할 필요 없다
                    //   (옛 코드는 후보 하나마다 StatCalculator 풀계산을 한 번씩 더 돌렸다).
                    var capped = CappedNow();
                    string bestStat = null; double bestDmg = -1;
                    string fallbackStat = null; double fallbackPrio = double.NegativeInfinity;   // 캡이라 이득 0이지만, 빈 슬롯 방지용 폴백(실제 장비는 부옵 4개)
                    foreach (var stat in cands)
                    {
                        if (used.Contains(stat)) continue;
                        if (IsDeadStat(stat, capped))
                        {
                            // 이미 캡 → 폴백 후보로만. 스킬 내장(확정치명 등 capBonus, 항상 가동)으로 캡된 스탯은
                            //   진짜 죽은 옵션이고, 파티 버프 floor로만 캡된 스탯(예: 약확)은 버프 비가동 구간·타라운드에서
                            //   잔존 가치 → capBonus 기여가 작은(=floor로만 캡된) 쪽을 우선해 더 의미있는 필러를 채운다.
                            double prio = -(stat == "치명타확률%" ? capBonus.Cri : capBonus.Wek);
                            if (prio > fallbackPrio) { fallbackPrio = prio; fallbackStat = stat; }
                            continue;
                        }
                        slot.StatName = stat; slot.Tier = 1;
                        double d = Score(loadout);
                        slot.StatName = ""; slot.Tier = 0;
                        if (d > bestDmg) { bestDmg = d; bestStat = stat; }
                    }
                    // 유효 후보가 없으면(남은 게 전부 캡인 치확/약확) 캡 후보로라도 채움 — 빈 슬롯 방지.
                    //   점수 불변(캡=한계효용 0), 표시·티어수(4기본) 정합.
                    bestStat ??= fallbackStat;
                    if (bestStat == null) break;
                    slot.StatName = bestStat; slot.Tier = 1; used.Add(bestStat);
                }

                // 2) 티어업 5번 분배 (각 슬롯 최대 6티어, 점수 증가 최대 슬롯에 / 이미 캡인 슬롯은 스킵)
                for (int up = 0; up < TierUps; up++)
                {
                    var capped = CappedNow();   // 티어업 前 상태 — 슬롯 스캔 중엔 불변이라 1회면 충분
                    int bestSlot = -1; double bestDmg = -1;
                    for (int i = 0; i < nSlots; i++)
                    {
                        var slot = equip.SubSlots[i];
                        if (string.IsNullOrEmpty(slot.StatName) || slot.Tier >= MaxTier) continue;
                        if (IsDeadStat(slot.StatName, capped)) continue;   // 이미 캡 → 더 올려도 이득 0
                        slot.Tier++;
                        double d = Score(loadout);
                        slot.Tier--;
                        if (d > bestDmg) { bestDmg = d; bestSlot = i; }
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

        // 좌표상승(SiegeOptimizer)에서 켜는 장신구↔부옵 조인트 플래그. 기본 OFF(기존 동작).
        public bool JointAccessory { get; set; }

        /// <summary>부옵 슬롯 상태(스탯명·티어) 스냅샷/복원 — 장신구 조인트에서 승자 부옵 보존용.</summary>
        private static List<List<(string Stat, int Tier)>> SnapshotSubs(EquipmentLoadout lo) =>
            lo.GetEquipments().Select(e => e.SubSlots.Select(s => (s.StatName, s.Tier)).ToList()).ToList();
        private static void RestoreSubs(EquipmentLoadout lo, List<List<(string Stat, int Tier)>> snap)
        {
            var eqs = lo.GetEquipments().ToList();
            for (int i = 0; i < eqs.Count && i < snap.Count; i++)
                for (int j = 0; j < eqs[i].SubSlots.Count && j < snap[i].Count; j++)
                { eqs[i].SubSlots[j].StatName = snap[i][j].Stat; eqs[i].SubSlots[j].Tier = snap[i][j].Tier; }
        }

        /// <summary>
        /// 장신구 ↔ 부옵 조인트 최적화. 기존 OptimizeAccessory(고정 부옵)에 더해, 치확/약확 장신구 메인이
        /// 캡을 공급하는 경우(→ 기어 부옵 치확/약확을 치피·공%로 돌릴 수 있음)를 부옵 재최적화와 함께 비교한다.
        /// 메인↔부옵 조인트(OptimizeForSetFull)와 같은 원리를 장신구로 확장 — "장신구 치확 = 부옵 치확" 동치 반영.
        /// </summary>
        private void OptimizeAccessoryJoint(
            EquipmentLoadout loadout, BattleCharacter battleChar, BattleConfig config, int charIndex,
            string[] subStatNames, Func<EquipmentLoadout, double> scorer, (double Cri, double Wek) partyCritWeakFloor)
        {
            double Score(EquipmentLoadout lo) => scorer != null ? scorer(lo) : EvaluateDamage(battleChar, config, charIndex, lo);

            // 1) 기준선: 고정 부옵 기준 최적 장신구(피증/보피증 등 비-치확/약확이 보통 여기서 선택).
            OptimizeAccessory(loadout, battleChar, config, charIndex, scorer);
            double best = Score(loadout);
            var bestAcc = loadout.Accessory;
            var bestSubs = SnapshotSubs(loadout);

            // 2) 조인트 후보: 치확/약확 장신구 메인(캡 공급) — 부옵 재최적화 후 비교. 6성·부옵=보피증 페어(보스 가정).
            if (AccessoryDb.MainOptions.TryGetValue(6, out var mains6))
                foreach (var accMain in new[] { "치명타확률%", "약점공격확률%" })
                {
                    if (!mains6.ContainsKey(accMain)) continue;
                    loadout.Accessory = new Accessory { Grade = 6, MainOption = accMain, SubOption = "보피증%" };
                    OptimizeSubOptions(loadout, battleChar, config, charIndex, subStatNames, scorer, partyCritWeakFloor);
                    double s = Score(loadout);
                    if (s > best) { best = s; bestAcc = loadout.Accessory; bestSubs = SnapshotSubs(loadout); }
                }

            loadout.Accessory = bestAcc;
            RestoreSubs(loadout, bestSubs);
        }

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
                            if (subOpt == mainOpt) continue; // 장신구는 메인=서브 중복 불가 (유저 확정). ※장비 부옵은 메인과 같은 스탯 허용 — OptimizeSubOptions 참조(규칙 다름)
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

            // 활성 세트 전부 (2+2세트면 두 세트 모두). 옛 코드는 최대 조각수 1개만 넘겨 2+2의 뒤쪽
            //   세트 보너스를 통째로 누락 → 2+2 탐색공간 전체가 체계적으로 저평가되던 버그.
            var activeSets = loadout.GetActiveSets();

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
                EquipSets = activeSets,
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
            // 버프 병합 규칙(EffectManager와 동일): 한 묶음(상시 / 턴제) 안에서 같은 종류는 MaxMerge,
            //   묶음 사이는 Add. 옛 코드는 전부 Add해서 비스킷 약확54 + 레이첼 약확27을 81로 봤다(실효는 54).
            //   floor가 과대하면 캡 거절이 일찍 걸려 부옵 약확이 실수요보다 부족하게 세팅된다.
            double permCri = 0, permWek = 0;     // 상시 패시브 파티버프 묶음
            double timedCri = 0, timedWek = 0;   // 턴제(조건부 패시브 + 스킬 파티버프) 묶음
            // HighestAtkAlly 선택자 버프(예: 비스킷 장비강화 약확54·6초월 수혜 2명)는 실효 수혜자(대표딜 상위 N)만
            //   받는다. floor가 이를 무시하고 전원에 가산하면 비수혜 멤버(샤오/미호 등) 약확이 과소 세팅됨(R3 약점공격
            //   미확보). 시뮬 ResolveAllyBuffTargets와 동일 취지로 대표딜 프록시(기본공격력×최대 공격스킬 배율) 상위 N 판정.
            double Proxy(BattleCharacter bc)
            {
                if (bc?.Character == null) return 0;
                var bs = bc.Character.GetBaseStats();
                double atk = (bs?.Atk ?? 0) + (bs?.MagicAtk ?? 0);
                double ratio = 0;
                foreach (var s in bc.Character.Skills ?? System.Linq.Enumerable.Empty<Skill>())
                {
                    if (s.SkillType == SkillType.Normal || s.SkillType == SkillType.Normal2) continue;
                    double r = s.GetLevelData(bc.IsSkillEnhanced)?.Ratio ?? 0;
                    if (r > ratio) ratio = r;
                }
                return atk * ratio;
            }
            var ranked = System.Linq.Enumerable.Range(0, party.Count)
                .OrderByDescending(i => Proxy(party[i])).ToList();
            bool IsTopN(int idx, int n) => ranked.Take(System.Math.Max(1, n)).Contains(idx);

            // 묶음 내 MaxMerge — 같은 종류(치확/약확)는 가장 높은 값 하나만 살아남는다.
            void MaxPerm(BuffSet b)
            {
                if (b == null) return;
                permCri = System.Math.Max(permCri, b.Cri);
                permWek = System.Math.Max(permWek, b.Wek);
            }
            void MaxTimed(BuffSet b)
            {
                if (b == null) return;
                timedCri = System.Math.Max(timedCri, b.Cri);
                timedWek = System.Math.Max(timedWek, b.Wek);
            }
            void MaxTimedEffects(System.Collections.Generic.IEnumerable<Models.Effects.SkillEffect> effs, int? selectorCount)
            {
                if (effs == null) return;
                foreach (var e in effs)
                {
                    if (e.Type != Models.Effects.SkillEffectType.Buff || e.Target != Models.Effects.EffectTarget.Party || e.Buff == null) continue;
                    if (e.TargetClasses != null && dealerClass != null && !System.Linq.Enumerable.Contains(e.TargetClasses, dealerClass)) continue;
                    // HighestAtkAlly 선택자면 대표딜 상위 N(초월 TargetCountOverride 반영)만 실효 수혜 → charIndex가 비수혜면 스킵.
                    if (e.TargetSelector == Models.Effects.TargetSelector.HighestAtkAlly)
                    {
                        int n = selectorCount ?? System.Math.Max(1, e.TargetCount);
                        if (!IsTopN(charIndex, n)) continue;
                    }
                    MaxTimed(e.Buff);
                }
            }
            for (int i = 0; i < party.Count; i++)
            {
                if (i == charIndex || party[i]?.Character == null) continue;
                var bc = party[i];
                bool enh = bc.IsSkillEnhanced; int tx = bc.TranscendLevel;
                // 패시브 파티버프: 상시는 상시 묶음, 조건부는 턴제 묶음.
                MaxPerm(bc.Character.Passive?.GetPartyBuff(enh, tx, dealerClass));
                MaxTimed(bc.Character.Passive?.GetConditionalPartyBuff(enh, tx, dealerClass));
                // 스킬 파티버프(턴제 묶음) — Effects + 레거시 PartyBuff + 초월. 초월 TargetCountOverride는 HighestAtkAlly 수혜 인원수.
                foreach (var skill in bc.Character.Skills ?? System.Linq.Enumerable.Empty<Skill>())
                {
                    var ld = skill.GetLevelData(enh);
                    var txb = skill.GetTranscendBonus(tx);
                    int? ov = txb?.TargetCountOverride;
                    MaxTimedEffects(ld?.Effects, ov);
                    MaxTimed(ld?.PartyBuff);
                    MaxTimedEffects(txb?.Effects, ov);
                    MaxTimed(txb?.PartyBuff);
                }
            }
            // 묶음 간 Add.
            return (permCri + timedCri, permWek + timedWek);
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

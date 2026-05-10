using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 통합 효과 관리자
    /// BattleEffect 인스턴스를 수집하고, StatCalculator/DamageCalculator가 사용하는
    /// 형태(BuffSet/DebuffSet)로 집계하여 반환.
    /// 게임 룰: 한 묶음(상시/턴제/펫) 안에서 같은 BuffSet 필드는 가장 높은 값만 살아남고(MaxMerge),
    /// 묶음 사이는 합산(Add).
    /// </summary>
    public class EffectManager
    {
        private readonly List<BattleEffect> _effects = new();

        /// <summary>활성 효과 수</summary>
        public int Count => _effects.Count;

        /// <summary>효과 추가</summary>
        public void AddEffect(BattleEffect effect)
        {
            if (effect != null)
                _effects.Add(effect);
        }

        /// <summary>효과 리스트 추가</summary>
        public void AddEffects(IEnumerable<BattleEffect> effects)
        {
            if (effects != null)
                _effects.AddRange(effects);
        }

        /// <summary>모든 효과 제거</summary>
        public void Clear() => _effects.Clear();

        /// <summary>특정 출처의 효과 제거</summary>
        public void RemoveBySource(string sourceName)
        {
            _effects.RemoveAll(e => e.SourceName == sourceName);
        }

        /// <summary>만료된 효과 제거</summary>
        public void RemoveExpired()
        {
            _effects.RemoveAll(e => e.IsExpired);
        }

        #region 버프 집계 (StatCalculator 호환)

        /// <summary>
        /// 분리된 버프 반환: (상시, 턴제, 펫)
        /// StatCalculator.Calculate()의 PartyPermanentBuffs/PartyTimedBuffs/PartyPetBuffs에 대응
        /// 게임 룰: 한 묶음(상시/턴제/펫) 안의 모든 효과를 통합해 같은 BuffSet 필드끼리 MaxMerge.
        /// 자버프/파티버프, 패시브/스킬을 더 잘게 쪼개지 않는다 — 그렇게 하면 카테고리 간 Add가 끼어 룰을 깬다.
        /// </summary>
        public (BuffSet Permanent, BuffSet Timed, BuffSet Pet) GetSeparatedBuffs()
        {
            // 상시 = (PassiveSelf, PassiveParty) 한 묶음으로 통합 MaxMerge
            var permanent = AggregateBuffsByCategories(
                EffectCategory.PassiveSelfBuff,
                EffectCategory.PassivePartyBuff);

            // 턴제 = (ConditionalSelf, ConditionalParty, ActiveSelf, ActiveParty) 한 묶음으로 통합 MaxMerge
            var timed = AggregateBuffsByCategories(
                EffectCategory.ConditionalSelfBuff,
                EffectCategory.ConditionalPartyBuff,
                EffectCategory.ActiveSelfBuff,
                EffectCategory.ActivePartyBuff);

            // 펫 = PetBuff (별개 카테고리, 같은 묶음 내에서는 동일 룰 적용)
            var pet = AggregateBuffsByCategories(EffectCategory.PetBuff);

            return (permanent, timed, pet);
        }

        /// <summary>
        /// 전체 버프 합산 (상시 + 턴제 + 펫)
        /// </summary>
        public BuffSet GetTotalBuffs()
        {
            var (permanent, timed, pet) = GetSeparatedBuffs();
            var total = new BuffSet();
            total.Add(permanent);
            total.Add(timed);
            total.Add(pet);
            return total;
        }

        /// <summary>
        /// 지정한 카테고리들의 버프를 한 묶음으로 보고 통합 MaxMerge.
        /// 같은 BuffSet 필드(같은 종류)면 가장 높은 값만 살아남는다.
        /// </summary>
        private BuffSet AggregateBuffsByCategories(params EffectCategory[] categories)
        {
            var total = new BuffSet();
            foreach (var category in categories)
            {
                foreach (var effect in _effects.Where(e =>
                    e.Category == category && e.IsStatBuff && !e.IsExpired))
                {
                    total.MaxMerge(effect.BuffValues);
                }
            }
            return total;
        }

        #endregion

        #region 디버프 집계 (DamageCalculator 호환)

        /// <summary>
        /// 전체 디버프 합산
        /// 게임 룰: (상시, 턴제, 펫) 3카테고리 — 카테고리 내 통합 MaxMerge, 카테고리 간 Add.
        /// </summary>
        public DebuffSet GetTotalDebuffs()
        {
            // 상시 = PassiveDebuff
            var permanent = AggregateDebuffsByCategories(EffectCategory.PassiveDebuff);

            // 턴제 = ConditionalDebuff + ActiveDebuff 한 묶음으로 통합 MaxMerge
            var timed = AggregateDebuffsByCategories(
                EffectCategory.ConditionalDebuff,
                EffectCategory.ActiveDebuff);

            // 펫 = PetDebuff (별개 카테고리)
            var pet = AggregateDebuffsByCategories(EffectCategory.PetDebuff);

            var total = new DebuffSet();
            total.Add(permanent);
            total.Add(timed);
            total.Add(pet);
            return total;
        }

        /// <summary>
        /// 지정한 카테고리들의 디버프를 한 묶음으로 보고 통합 MaxMerge.
        /// </summary>
        private DebuffSet AggregateDebuffsByCategories(params EffectCategory[] categories)
        {
            var total = new DebuffSet();
            foreach (var category in categories)
            {
                foreach (var effect in _effects.Where(e =>
                    e.Category == category && e.IsStatDebuff && !e.IsExpired))
                {
                    total.MaxMerge(effect.DebuffValues);
                }
            }
            return total;
        }

        #endregion

        #region 상태이상 조회

        /// <summary>
        /// 활성 상태이상 효과 목록
        /// </summary>
        public List<BattleEffect> GetActiveStatusEffects()
        {
            return _effects
                .Where(e => e.IsStatusEffect && !e.IsExpired)
                .ToList();
        }

        /// <summary>
        /// 특정 상태이상 타입의 활성 효과 조회
        /// </summary>
        public List<BattleEffect> GetStatusEffectsOfType(StatusEffectType type)
        {
            return _effects
                .Where(e => e.StatusType == type && !e.IsExpired)
                .ToList();
        }

        /// <summary>
        /// 특정 상태이상의 총 스택 수
        /// </summary>
        public int GetTotalStacks(StatusEffectType type)
        {
            return _effects
                .Where(e => e.StatusType == type && !e.IsExpired)
                .Sum(e => e.Stacks);
        }

        /// <summary>
        /// 게임 표기 기준 활성 디버프 개수
        /// = 합산 디버프셋의 활성 필드 수 + 활성 상태이상 종류 수
        /// (스택은 1개로 카운트, 같은 종류의 여러 인스턴스도 1개)
        /// </summary>
        public int GetActiveDebuffCount()
        {
            int count = 0;
            var debuffs = GetTotalDebuffs();
            if (debuffs.Def_Reduction > 0) count++;
            if (debuffs.Atk_Reduction > 0) count++;
            if (debuffs.Spd_Reduction > 0) count++;
            if (debuffs.Dmg_Reduction > 0) count++;
            if (debuffs.Cri_Dmg_Reduction > 0) count++;
            if (debuffs.Heal_Reduction > 0) count++;
            if (debuffs.Unrecover > 0) count++;
            if (debuffs.Eff_Red > 0) count++;
            if (debuffs.Eff_Hit_Red > 0) count++;
            if (debuffs.Blk_Red > 0) count++;
            if (debuffs.Dmg_Taken_Increase > 0) count++;
            if (debuffs.Vulnerability > 0) count++;
            if (debuffs.Boss_Vulnerability > 0) count++;

            count += _effects
                .Where(e => e.IsStatusEffect && !e.IsExpired && e.StatusType.HasValue)
                .Select(e => e.StatusType.Value)
                .Distinct()
                .Count();

            return count;
        }

        #endregion

        #region 턴 관리 (배틀 시뮬레이터용)

        /// <summary>
        /// 턴 경과: 모든 비영구 효과의 남은 턴 1 감소, 만료 효과 제거
        /// </summary>
        public void TickTurn()
        {
            foreach (var effect in _effects)
                effect.TickTurn();
            RemoveExpired();
        }

        #endregion

        #region 조회

        /// <summary>모든 활성 효과</summary>
        public IReadOnlyList<BattleEffect> GetAllEffects() => _effects.AsReadOnly();

        /// <summary>특정 카테고리의 활성 효과</summary>
        public List<BattleEffect> GetEffectsByCategory(EffectCategory category)
        {
            return _effects.Where(e => e.Category == category && !e.IsExpired).ToList();
        }

        /// <summary>CC 효과가 있는지 (행동 불가)</summary>
        public bool HasActionBlockingCC()
        {
            return _effects.Any(e =>
                e.IsStatusEffect && !e.IsExpired
                && e.StatusData != null && e.StatusData.BlocksAction);
        }

        #endregion
    }
}

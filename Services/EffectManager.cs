using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 통합 효과 관리자
    /// BattleEffect 인스턴스를 수집하고, 기존 StatCalculator/DamageCalculator가 사용하는
    /// 형태(BuffSet/DebuffSet)로 집계하여 반환
    /// BuffCalculator를 대체
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
        /// 병합 규칙: 같은 카테고리 내 MaxMerge, 카테고리 간 Add
        /// </summary>
        public (BuffSet Permanent, BuffSet Timed, BuffSet Pet) GetSeparatedBuffs()
        {
            // 상시 = PassiveSelfBuff + PassivePartyBuff (각각 내부 MaxMerge, 결과끼리 Add)
            var permanent = AggregateBuffsByCategories(
                EffectCategory.PassiveSelfBuff,
                EffectCategory.PassivePartyBuff);

            // 턴제 = ConditionalSelfBuff + ConditionalPartyBuff + ActiveSelfBuff + ActivePartyBuff
            var timed = AggregateBuffsByCategories(
                EffectCategory.ConditionalSelfBuff,
                EffectCategory.ConditionalPartyBuff,
                EffectCategory.ActiveSelfBuff,
                EffectCategory.ActivePartyBuff);

            // 펫 = PetBuff (항상 합산)
            var pet = new BuffSet();
            foreach (var effect in _effects.Where(e =>
                e.Category == EffectCategory.PetBuff && e.IsStatBuff && !e.IsExpired))
            {
                pet.Add(effect.BuffValues);
            }

            return (permanent, timed, pet);
        }

        /// <summary>
        /// 전체 버프 합산 (상시 + 턴제 + 펫)
        /// BuffCalculator.CalculateTotalBuffs() 대응
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
        /// 여러 카테고리의 버프를 집계
        /// 각 카테고리 내에서 MaxMerge, 카테고리 간 Add
        /// </summary>
        private BuffSet AggregateBuffsByCategories(params EffectCategory[] categories)
        {
            var total = new BuffSet();
            foreach (var category in categories)
            {
                var categoryResult = new BuffSet();
                foreach (var effect in _effects.Where(e =>
                    e.Category == category && e.IsStatBuff && !e.IsExpired))
                {
                    categoryResult.MaxMerge(effect.BuffValues);
                }
                total.Add(categoryResult);
            }
            return total;
        }

        #endregion

        #region 디버프 집계 (DamageCalculator 호환)

        /// <summary>
        /// 전체 디버프 합산
        /// 같은 카테고리 내 MaxMerge, 카테고리 간 Add
        /// </summary>
        public DebuffSet GetTotalDebuffs()
        {
            var total = new DebuffSet();
            var categories = new[]
            {
                EffectCategory.PassiveDebuff,
                EffectCategory.ConditionalDebuff,
                EffectCategory.ActiveDebuff,
                EffectCategory.PetDebuff
            };

            foreach (var category in categories)
            {
                var categoryResult = new DebuffSet();
                foreach (var effect in _effects.Where(e =>
                    e.Category == category && e.IsStatDebuff && !e.IsExpired))
                {
                    categoryResult.MaxMerge(effect.DebuffValues);
                }
                total.Add(categoryResult);
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

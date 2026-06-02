using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 전용무기 DB.
    /// - 공용 전용무기: 조율 슬롯 없이 공격력 flat 만 (모든 캐릭 착용 가능)
    /// - 캐릭터별 전용무기: 공격력 + 조율 4슬롯 (특정 캐릭만 착용)
    ///
    /// 사용자 실측 데이터 수집 중 — 캐릭별 구체적인 조율 옵션은
    /// 데이터 받는 대로 BuildCharacterSpecific에 등록한다.
    /// 등록되지 않은 캐릭은 "딜러 기본"(전설 4× 모든공격력) 가정으로 폴백.
    /// </summary>
    public static class ExclusiveWeaponDb
    {
        /// <summary>딜러 기본 — 가장 단순한 max-damage 조율 (전설 모든공격력 ×4 = +48% Atk_Rate)</summary>
        private static List<TuningSlot> DealerDefaultTuning() => new()
        {
            new TuningSlot { Option = TuningOption.모든공격력, Grade = ExclusiveWeaponGrade.전설 },
            new TuningSlot { Option = TuningOption.모든공격력, Grade = ExclusiveWeaponGrade.전설 },
            new TuningSlot { Option = TuningOption.모든공격력, Grade = ExclusiveWeaponGrade.전설 },
            new TuningSlot { Option = TuningOption.모든공격력, Grade = ExclusiveWeaponGrade.전설 },
        };

        /// <summary>공용 전용무기 (조율 없음, 어떤 캐릭이든 착용 가능)</summary>
        public static ExclusiveWeapon Universal(bool magic = false) => new()
        {
            Name = magic ? "공용 전용무기(마법)" : "공용 전용무기(물리)",
            OwnerCharacterId = 0,
            Atk = 247,
            IsMagic = magic,
            Tuning = new()
        };

        /// <summary>딜러 기본 캐릭별 전용무기 (조율 4× 전설 모든공격력)</summary>
        public static ExclusiveWeapon DealerDefault(int characterId, string name, bool magic = false) => new()
        {
            Name = name,
            OwnerCharacterId = characterId,
            Atk = 247,
            IsMagic = magic,
            Tuning = DealerDefaultTuning()
        };

        /// <summary>
        /// 캐릭터별 등록된 전용무기 (실측 데이터 기반).
        /// key = Character.Id
        /// </summary>
        private static readonly Dictionary<int, ExclusiveWeapon> _registry = BuildRegistry();

        private static Dictionary<int, ExclusiveWeapon> BuildRegistry()
        {
            // 캐릭터별 전용무기(조율 4슬롯 보유)만 등록한다.
            // 공용 전용무기·미장착 캐릭은 여기에 없고, 호출자가 명시적으로 set한 경우에만 적용.
            return new Dictionary<int, ExclusiveWeapon>();
        }

        /// <summary>
        /// 해당 캐릭터의 전용무기 반환.
        /// - 등록된 캐릭별 전용무기가 있으면 그것 반환.
        /// - 미등록이면 useUniversalFallback=true 시 공용 전용무기(조율 없음, Atk flat 247만) 반환.
        ///   캐릭별 전용무기를 장착하지 않은 캐릭은 게임에서 공용 전용무기를 끼는 게 일반적.
        /// - useUniversalFallback=false면 미등록은 null 반환.
        /// </summary>
        public static ExclusiveWeapon Get(int characterId, bool isMagic, bool useUniversalFallback = true)
        {
            if (_registry.TryGetValue(characterId, out var w)) return w;
            if (!useUniversalFallback) return null;
            return Universal(isMagic);
        }

        /// <summary>모든 등록된 캐릭별 전용무기.</summary>
        public static IReadOnlyDictionary<int, ExclusiveWeapon> All => _registry;
    }
}

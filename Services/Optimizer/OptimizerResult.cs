using System.Collections.Generic;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.Optimizer
{
    /// <summary>
    /// 옵티마이저 실행 결과
    /// </summary>
    public class OptimizerResult
    {
        /// <summary>
        /// 캐릭터별 최적 장비 결과
        /// </summary>
        public List<CharacterOptimalEquipment> CharacterResults { get; set; } = new();

        /// <summary>
        /// 최적 장비 적용 시 예상 총 배틀 데미지
        /// </summary>
        public double EstimatedTotalDamage { get; set; }

        /// <summary>
        /// 탐색한 총 조합 수
        /// </summary>
        public long TotalCombinationsSearched { get; set; }

        /// <summary>
        /// 소요 시간 (ms)
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 캐릭터 1명의 최적 장비 결과
    /// </summary>
    public class CharacterOptimalEquipment
    {
        public string CharacterName { get; set; }
        public int PartyIndex { get; set; }

        /// <summary>
        /// 최적 장비 세트 조합
        /// </summary>
        public EquipSetConfig BestSetConfig { get; set; }

        /// <summary>
        /// 최적 장비 한벌
        /// </summary>
        public EquipmentLoadout BestLoadout { get; set; }

        /// <summary>
        /// 이 장비로 달성 가능한 예상 데미지
        /// </summary>
        public double EstimatedDamage { get; set; }

        /// <summary>
        /// 상위 N개 장비 조합 (비교용)
        /// </summary>
        public List<RankedLoadout> TopLoadouts { get; set; } = new();
    }

    /// <summary>
    /// 순위가 매겨진 장비 조합
    /// </summary>
    public class RankedLoadout
    {
        public int Rank { get; set; }
        public EquipSetConfig SetConfig { get; set; }
        public EquipmentLoadout Loadout { get; set; }
        public double EstimatedDamage { get; set; }
        public string Description { get; set; }
    }
}

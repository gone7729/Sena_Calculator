using System.Collections.Generic;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 스테이지 정의
    /// </summary>
    public class Stage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EnemyType StageType { get; set; }
        public List<StageWave> Waves { get; set; } = new();
    }

    /// <summary>
    /// 웨이브 (스테이지 내 단계)
    /// </summary>
    public class StageWave
    {
        public int WaveNumber { get; set; }
        public List<StageEnemy> Enemies { get; set; } = new();

        /// <summary>
        /// 적군 스킬 사용 우선순위 (모든 스킬이 쿨다운 충족 시 이 순서로 사용).
        /// 리스트 앞쪽이 우선. 미포함 적/스킬은 기본 규칙으로 처리.
        /// </summary>
        public List<SiegeSkillOrder> SkillPriority { get; set; } = new();
    }

    /// <summary>
    /// 공성전 적 스킬 우선순위 항목 (적 Id + 스킬 종류).
    /// </summary>
    public class SiegeSkillOrder
    {
        public int EnemyId { get; set; }
        public SkillType SkillType { get; set; }
    }

    /// <summary>
    /// 웨이브 내 적 배치
    /// </summary>
    public class StageEnemy
    {
        public int EnemyId { get; set; }        // Enemy.Id 참조
        public int Count { get; set; } = 1;     // 해당 적 수
        public int Position { get; set; }       // 배치 위치 (진형 자리 1~5, 같은 속공 시 공격 순서 기준)
        public bool IsBoss { get; set; }        // 이 라운드에서 보스 취급 (공성전 3라운드 등) — HP 0이어도 사망 안 함, 약점공격 대상
    }
}

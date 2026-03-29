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
    }

    /// <summary>
    /// 웨이브 내 적 배치
    /// </summary>
    public class StageEnemy
    {
        public int EnemyId { get; set; }        // Enemy.Id 참조
        public int Count { get; set; } = 1;     // 해당 적 수
        public int Position { get; set; }       // 배치 위치
    }
}

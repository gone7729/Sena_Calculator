using System.Collections.Generic;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 시뮬 결과. 점수 = 70턴간 적에게 가한 누적 데미지.
    /// </summary>
    public class SiegeBattleResult
    {
        // 공성전 점수 (전 라운드 누적 데미지)
        public double TotalScore { get; set; }

        // 라운드별 점수
        public Dictionary<int, double> RoundScore { get; set; } = new();

        // 도달 턴 / 클리어한 라운드 수 (R1·R2 클리어 개수)
        public int TotalTurns { get; set; }
        public int RoundsCleared { get; set; }
        public double ElapsedSeconds { get; set; }   // 총 경과 게임시간(쿨다운 모델 진단용)
        public int AlliesAlive { get; set; }         // 전투 종료 시 살아있는 아군 수 (빔 생존 가치 평가용)

        // 턴별 로그
        public List<BattleTurnLog> TurnLogs { get; set; } = new();

        // 캐릭터별 기여
        public List<SiegeCharacterResult> CharacterResults { get; set; } = new();

        // 빔서치: 각 아군 스킬턴의 행동 후보 (config.RecordDecisionPoints=true일 때만 채워짐)
        public List<RotationDecisionPoint> DecisionPoints { get; set; } = new();
    }

    /// <summary>공성전 캐릭터별 데미지 기여.</summary>
    public class SiegeCharacterResult
    {
        public string CharacterName { get; set; }
        public int PartyIndex { get; set; }
        public double TotalDamage { get; set; }
        public double DamageShare { get; set; }
    }
}

using System.Collections.Generic;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 배틀 시뮬레이션 결과
    /// </summary>
    public class BattleResult
    {
        // 총 데미지
        public double TotalDamage { get; set; }

        // 진행된 총 턴 수
        public int TotalTurns { get; set; }

        // 캐릭터별 데미지 상세
        public List<CharacterDamageResult> CharacterResults { get; set; } = new();

        // 턴별 로그
        public List<BattleTurnLog> TurnLogs { get; set; } = new();

        // 보스 잔여 HP
        public double BossRemainingHp { get; set; }

        // DPS (데미지 / 턴)
        public double DamagePerTurn => TotalTurns > 0 ? TotalDamage / TotalTurns : 0;
    }

    /// <summary>
    /// 캐릭터별 데미지 결과
    /// </summary>
    public class CharacterDamageResult
    {
        public string CharacterName { get; set; }
        public int PartyIndex { get; set; }

        // 총 데미지
        public double TotalDamage { get; set; }

        // 기본공격 총 데미지
        public double NormalAttackDamage { get; set; }

        // 스킬 총 데미지
        public double SkillDamage { get; set; }

        // 패시브/협공 등 추가 데미지
        public double BonusDamage { get; set; }

        // 데미지 비중 (%)
        public double DamageShare { get; set; }

        // 스킬별 데미지 상세
        public Dictionary<string, double> SkillBreakdown { get; set; } = new();
    }
}

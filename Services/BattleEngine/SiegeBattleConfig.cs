using System.Collections.Generic;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 시뮬 입력 설정.
    /// 단일 보스전 BattleConfig와 달리 단일 적(TargetEnemy)이 아니라 라운드 구성(Stage)을 받는다.
    /// 진형 효과는 PVE에서 미적용이므로 FormationName은 스탯 계산에 빈 값으로 흘려보낸다(자리=Position은 별도).
    /// </summary>
    public class SiegeBattleConfig
    {
        // 아군 파티 (최대 5명)
        public List<BattleCharacter> AllyParty { get; set; } = new();

        // 공성전 라운드 구성 (EnemyDb.SiegeStages[요일])
        public Stage SiegeStage { get; set; }

        // 진형 — PVE는 진형 효과 미적용이라 스탯엔 빈 값. (자리 순서는 BattleCharacter 위치로)
        public string FormationName { get; set; } = "";

        // 펫
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }

        // 최대 턴 (공성전 70)
        public int MaxTurns { get; set; } = 70;

        // 아군 스킬 로테이션 (캐릭터 인덱스별 스킬 사용 순서). 비면 자동(궁→4→3→2→1).
        public Dictionary<int, List<SkillType>> AllyRotations { get; set; } = new();
    }
}

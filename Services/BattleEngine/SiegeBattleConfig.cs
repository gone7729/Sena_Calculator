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

        // 진형 — 아군은 진형효과 적용(기본/밸런스/보호). 적군만 미적용.
        // 옵티마이저가 3종을 순회하며 최고딜 진형을 찾는다. 자리(앞/뒤)는 BattleCharacter.IsBackPosition.
        public string FormationName { get; set; } = "기본 진형";

        // 펫
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public int PetEnhance { get; set; }   // 펫 스킬강화 (0=미강화, 1~3)
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }

        // 최대 턴 (공성전 70)
        public int MaxTurns { get; set; } = 70;

        // 아군 스킬 로테이션 (캐릭터 인덱스별 스킬 사용 순서). 비면 자동(궁→4→3→2→1).
        public Dictionary<int, List<SkillType>> AllyRotations { get; set; } = new();
    }
}

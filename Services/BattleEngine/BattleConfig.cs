using System.Collections.Generic;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 배틀 시뮬레이션 설정
    /// </summary>
    public class BattleConfig
    {
        // 아군 파티 (최대 5명)
        public List<BattleCharacter> AllyParty { get; set; } = new();

        // 진형
        public string FormationName { get; set; }

        // 적
        public Enemy TargetEnemy { get; set; }

        // 펫
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public int PetEnhance { get; set; }   // 펫 스킬강화 (0=미강화, 1~3). 성급값에 보너스 가산
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }

        // 배틀 설정
        public int MaxTurns { get; set; } = 40;

        // 스킬 로테이션 모드
        public RotationMode RotationMode { get; set; } = RotationMode.UserDefined;

        // 유저 지정 로테이션: 캐릭터 인덱스별 스킬 순서
        public Dictionary<int, List<SkillType>> UserRotations { get; set; } = new();

        // 적군 스킬 로테이션
        public List<EnemyAction> EnemyRotation { get; set; } = new();
    }

    /// <summary>
    /// 로테이션 모드
    /// </summary>
    public enum RotationMode
    {
        UserDefined,    // 유저 직접 지정 (아군만)
        AutoOptimize    // 자동 최적화
    }

    /// <summary>
    /// 적 행동 정의
    /// </summary>
    public class EnemyAction
    {
        public string SkillName { get; set; }
        public double CooldownSeconds { get; set; }
        public bool ClearsDebuffs { get; set; }         // 디버프 해제 여부
        public double DamageRatio { get; set; }          // 데미지 배율
        public BuffSet SelfBuff { get; set; }            // 자기 버프
        public DebuffSet AllyDebuff { get; set; }        // 아군에게 거는 디버프
    }
}

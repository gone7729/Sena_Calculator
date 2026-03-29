using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 배틀 진행 중 상태 관리
    /// </summary>
    public class BattleState
    {
        public int CurrentTurn { get; set; }
        public double ElapsedSeconds { get; set; }

        // 아군 상태
        public List<CharacterBattleState> AllyStates { get; set; } = new();

        // 적 상태
        public EnemyBattleState EnemyState { get; set; } = new();

        // 총 누적 데미지
        public double TotalDamageDealt { get; set; }

        // 턴별 데미지 로그
        public List<BattleTurnLog> TurnLogs { get; set; } = new();
    }

    /// <summary>
    /// 개별 캐릭터의 배틀 중 상태
    /// </summary>
    public class CharacterBattleState
    {
        public BattleCharacter Source { get; set; }
        public int PartyIndex { get; set; }

        // 현재 HP
        public double CurrentHp { get; set; }
        public double MaxHp { get; set; }

        // 최종 계산된 스탯 (장비/버프 적용 후)
        public double FinalAtk { get; set; }
        public double FinalDef { get; set; }
        public double FinalSpd { get; set; }

        // 패시브 스택
        public int PassiveStacks { get; set; }

        // 통합 효과 관리자 (버프/디버프/상태이상 통합)
        public EffectManager Effects { get; set; } = new();

        // 스킬 쿨다운 (초 기반)
        public Dictionary<SkillType, double> SkillCooldowns { get; set; } = new();

        // 스킬 로테이션 인덱스 (다음에 사용할 스킬)
        public int RotationIndex { get; set; }

        // 이 캐릭터가 누적 딜량
        public double TotalDamageDealt { get; set; }

        /// <summary>
        /// 사용 가능한 스킬인지 확인 (쿨다운 체크)
        /// </summary>
        public bool IsSkillReady(SkillType skillType)
        {
            if (!SkillCooldowns.ContainsKey(skillType))
                return true;
            return SkillCooldowns[skillType] <= 0;
        }

        /// <summary>
        /// 상대 스킬 사용 시 쿨다운 5초 감소
        /// </summary>
        public void ReduceCooldowns(double seconds)
        {
            var keys = SkillCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                if (SkillCooldowns[key] > 5) // 5초 이하 남은 스킬에는 미적용
                    SkillCooldowns[key] -= seconds;
                if (SkillCooldowns[key] < 0)
                    SkillCooldowns[key] = 0;
            }
        }
    }

    /// <summary>
    /// 적의 배틀 중 상태
    /// </summary>
    public class EnemyBattleState
    {
        public Enemy Enemy { get; set; }
        public double CurrentHp { get; set; }
        public double MaxHp { get; set; }

        // 방어력 스택 (스택형 방어력 증가 보스용)
        public int DefenseStacks { get; set; }

        // 통합 효과 관리자 (디버프/상태이상 통합)
        public EffectManager Effects { get; set; } = new();
    }

    /// <summary>
    /// 턴별 행동 로그
    /// </summary>
    public class BattleTurnLog
    {
        public int Turn { get; set; }
        public string ActorName { get; set; }
        public bool IsAlly { get; set; }
        public ActionType ActionType { get; set; }
        public string SkillName { get; set; }
        public double DamageDealt { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 행동 유형
    /// </summary>
    public enum ActionType
    {
        NormalAttack,   // 기본 공격
        SkillAttack,    // 스킬 사용
        BuffApplied,    // 버프 적용
        DebuffApplied,  // 디버프 적용
    }
}

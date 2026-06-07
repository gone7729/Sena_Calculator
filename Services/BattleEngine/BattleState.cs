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

        // 평타 로테이션: 아군 속공 내림차순 PartyIndex 순서 + 현재 커서 (살아있는 영웅만 순환)
        public List<int> AllySpeedOrder { get; set; } = new();
        public int AllyRotationCursor { get; set; }

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

        // ===== 공성전 아군 회복/보호막 (단일보스 sim 미사용) =====
        public double Shield { get; set; }                  // 흡수 보호막 풀 (Shield_HpRatio/AtkRatio 버프 → 피격 시 먼저 소진)
        public int ShieldTurns { get; set; }                // 보호막 잔여 턴
        public List<SiegeAllyRegen> Regens { get; set; } = new();  // 지속 회복(재생) — 매 턴 PerTurn만큼 회복
        public int LifestealTurns { get; set; }             // 흡혈 잔여 턴 (미호 등) — 활성 중 적에 준 피해의 LifestealRatio% 회복
        public double LifestealRatio { get; set; }          // 흡혈 회복 비율% (피해량 대비)
        public int SkillCastCount { get; set; }             // 자신 스킬 발동 누적 횟수 (N회마다 트리거용)
        public bool HpThresholdNullifyUsed { get; set; }    // 생명력 임계 피해무효(나타 50% 등) 전투당 1회 소비
        public bool HpThresholdHealUsed { get; set; }       // 생명력 임계 자힐(샤오 HP50%↓ 물공45% 등) 전투당 1회 소비

        // ===== 생존 메카닉 런타임 상태 =====
        public bool IsDead { get; set; }                    // 사망 시 이후 자기 턴 스킵
        public int NullifyHitsRemaining { get; set; }       // 피해 무효화 잔여 피격 횟수
        public int NullifyTurnsRemaining { get; set; }      // 피해 무효화 잔여 턴
        public DamageNullType NullifyType { get; set; } = DamageNullType.All; // 무효화 대상 피해 타입
        public bool AuthorityUsed { get; set; }             // 권능(현재HP 이상 피해 생존) 전투당 1회 소비
        public bool RevivalUsed { get; set; }               // 부활/불사/불굴 전투당 1회 소비
        public int ImmortalHitsRemaining { get; set; }      // 부활 후 사망무효 잔여 피격 (불굴)
        public int ImmortalTurnsRemaining { get; set; }     // 부활 후 사망무효 잔여 턴 (불사)
        public int TauntTurnsRemaining { get; set; }        // 도발 잔여 턴 — 활성 중 적 공격 타겟이 이 아군으로 유도됨 (라이언 강자사냥)

        // 최종 계산된 스탯 (장비/버프 적용 후)
        public double FinalAtk { get; set; }
        public double FinalDef { get; set; }
        public double FinalSpd { get; set; }

        // 버프 타게팅용 실효딜 가중치 (대표 스킬 per-hit 추정). 비딜러(지원/방어형)=0.
        // 비스킷 장비강화·라이언 평타쿨감 등 HighestAtkAlly 버프를 raw atk가 아닌 이 값으로 배분.
        public double DamageWeight { get; set; }

        // 전투 시작 시 최종 표시스탯 스냅샷 (치확·치피·약확·약피·피증·보스피증·타입피증·방관·n인기 등).
        // gear+세트+초월+버프+패시브가 모두 집계된 값. FinalAtk엔 공%만 반영됐고 이 배수들은 별도라
        // 데미지 계산 시 여기서 읽는다. 전투 중 스킬 버프는 Effects에 추가되어 델타로 합산됨.
        public BaseStatSet DisplayStats { get; set; } = new BaseStatSet();

        // 패시브 스택 트리거 카운터
        // Key: 효과 ID, Value: 현재 누적 공격 횟수
        public Dictionary<string, int> StackTriggerCounters { get; set; } = new();
        // Key: 효과 ID, Value: 현재 스택 수
        public Dictionary<string, int> CurrentStacks { get; set; } = new();

        // 통합 효과 관리자 (버프/디버프/상태이상 통합)
        public EffectManager Effects { get; set; } = new();

        // 턴제 버프 적용 순서 카운터 (버프 해제용 ApplyOrderId 부여). 1부터 증가.
        public int ApplyOrderCounter { get; set; }

        // 상태이상 면역 잔여 턴 (타입별). 면역 패시브 캐릭의 기본공격 시 파티에 부여되는 턴제 면역.
        // 공성전: 면역캐릭이 기본공격을 해야 갱신되므로, 죽거나 행동 못 하면 면역이 끊겨 CC를 맞는다.
        public Dictionary<StatusEffectType, int> StatusImmunityTurns { get; set; } = new();

        // 받피해 분산 큐 (트루드 PainEndurance 등) — 각 발동마다 독립 누적
        public List<PendingPainEnduranceDamage> PainEnduranceQueue { get; set; } = new();

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
    /// 받피해 분산 큐의 한 항목 (트루드 PainEndurance 등 받피해를 N턴에 걸쳐 분할 적용).
    /// 발동마다 한 항목이 등록되고, 매 턴 시작 시 PerTurnAmount만큼 차감 + RemainingTurns--.
    /// </summary>
    public class PendingPainEnduranceDamage
    {
        public double PerTurnAmount { get; set; }
        public int RemainingTurns { get; set; }
        public string SourceLabel { get; set; }   // 로그용 (예: "보스 광역 / 트루드")
    }

    /// <summary>공성전 아군 지속 회복(재생) 1건. 매 턴 PerTurn만큼 회복하고 RemainingTurns--.</summary>
    public class SiegeAllyRegen
    {
        public double PerTurn { get; set; }        // 턴당 회복량 (시전 시점 시전자 최대HP 비례 스냅샷)
        public int RemainingTurns { get; set; }
        public string SourceName { get; set; }     // 로그용 시전자명
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
        DoTDamage,      // 상태이상 틱 데미지 (화상/출혈 등)
    }
}

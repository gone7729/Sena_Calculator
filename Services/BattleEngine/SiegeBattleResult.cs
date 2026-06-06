using System.Collections.Generic;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 공성전 시뮬 결과. 점수 = 70턴간 적에게 가한 누적 데미지.
    /// </summary>
    public class SiegeBattleResult
    {
        // 공성전 점수 (전 라운드 누적 데미지) — 보고용 실제 딜. 페널티 미반영.
        public double TotalScore { get; set; }

        // 랭킹용 점수 = TotalScore − AllyDeathPenalty×사망수. 빔/옵티마이저가 이 값으로 빌드를 선택(생존 우선).
        //   AllyDeathPenalty=0이면 TotalScore와 동일. 보고(총점)는 항상 TotalScore 사용.
        public double RankScore { get; set; }

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

        // 빌드 실행가능성: RotationPlan 각 스텝의 계획대로-시전 여부·쿨 여유 (config.RecordFeasibility=true일 때만)
        public List<BuildStepFeasibility> Feasibility { get; set; } = new();

        // 아군 사망 이벤트 (턴/경과초/이름/사인). 사망 로그용.
        public List<SiegeDeathEvent> Deaths { get; set; } = new();
    }

    /// <summary>
    /// 빌드(생성 로테이션) 1스텝의 실행가능성 — 계획된 스킬이 그 시점에 실제로 시전됐는지와 쿨 여유.
    /// 시뮬은 쿨≤0일 때만 시전하므로(IsSkillReady 게이트), 실행 불가는 폴백(auto로 대체)으로 드러난다.
    /// </summary>
    public class BuildStepFeasibility
    {
        public int StepIndex { get; set; }          // 플랜 스텝(=아군 스킬턴 순서) 인덱스
        public int Turn { get; set; }               // 그 시점 누적 턴
        public double Elapsed { get; set; }         // 그 시점 경과 게임시간(초)
        public string HeroName { get; set; }        // 계획 영웅 (홀드면 "(홀드)")
        public string SkillName { get; set; }       // 계획 스킬 (홀드면 "")
        public bool Hold { get; set; }              // 의도된 홀드 스텝
        public bool ExecutedAsPlanned { get; set; } // 계획대로 시전됨? (홀드는 true 취급)
        public bool Reached { get; set; } = true;   // 전투 중 이 스텝에 도달했는지(false=전투 조기종료/스킬턴 부족)
        public string FallbackReason { get; set; }  // 폴백 사유 (시전자 사망/행동불가 CC/쿨 N초 남음/스킬 없음)
        public double CooldownRemaining { get; set; }// 폴백이 쿨 때문이면 잔여 쿨(초) = 위반 크기
        public double Slack { get; set; }           // 시전 시 쿨 여유(초) = 시전시각 − 준비완료시각 (재시전만 의미; 클수록 견고)
        public bool CooldownGated { get; set; }     // true=재시전(쿨 제약 받음, Slack 유효). false=첫 시전(쿨 무관 — 견고성 지표 제외)
        public List<string> BuffTargets { get; set; }   // 이 시전이 부여한 아군 버프 수령자(예: 비스킷 장비강화 → [타카,라이언]). 버프 없으면 null
        public List<string> DebuffTargets { get; set; }  // 이 시전이 적에 부여한 디버프 대상(예: 레이첼 불새 → [스파이크,룩,챈슬러]). 없으면 null
        public List<string> DispelTargets { get; set; }  // 이 시전이 버프해제한 적(예: 비스킷 리프어택 → [스파이크]). 없으면 null
    }

    /// <summary>아군 사망 이벤트 (사망 로그용).</summary>
    public class SiegeDeathEvent
    {
        public int Turn { get; set; }
        public double Elapsed { get; set; }
        public string AllyName { get; set; }
        public string Cause { get; set; }   // 가해 적 + 스킬 (예: "스파이크 혹한의 일격(추가타)")
    }

    /// <summary>공성전 캐릭터별 데미지 기여.</summary>
    public class SiegeCharacterResult
    {
        public string CharacterName { get; set; }
        public int PartyIndex { get; set; }
        public double TotalDamage { get; set; }
        public double DamageShare { get; set; }
        public bool Died { get; set; }   // 전투 종료 시 사망(부활 못 함) 여부 — 생존반지 탐색 게이팅용
    }
}

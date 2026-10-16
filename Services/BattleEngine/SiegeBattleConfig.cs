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

        // 빔서치 로테이션 플랜: 아군 스킬턴(발생 순서)별 행동 지정. null이면 자동(PickAllySkill).
        // 플랜 길이를 넘는 스킬턴은 자동으로 처리(빔서치의 디폴트 꼬리).
        public List<RotationDecision> RotationPlan { get; set; }

        // true면 매 아군 스킬턴의 (가능한 행동 후보)를 state.DecisionPoints에 기록 (빔서치 탐색용).
        public bool RecordDecisionPoints { get; set; }

        // [풀버프 정렬] true면 자동 로테가 파티버프 셋업 스킬(청소시간·장비강화·따뜻한울림 등)을 딜러 핵보다 먼저 시전.
        //   "버프 깔고 버스트" 전략. 옵티마이저가 빔(템포)과 이걸 둘 다 평가해 max 채택(무회귀).
        public bool BuffFirstAuto { get; set; }

        // true면 RotationPlan의 각 스텝이 실제 계획대로 시전됐는지(폴백 여부)·쿨 여유를 기록(빌드 실행가능성 검증).
        //   읽기 전용 계측 — 점수/전투 결과 불변. 보통 최종 빌드 1회 재생에만 켠다.
        public bool RecordFeasibility { get; set; }

        // [진단] true면 아군 효과 만료(Effects.TickTurn)를 건너뛴다 → 파티버프(청소·따뜻한울림 등) 강제 풀 uptime.
        //   "넉↔버프 정렬/uptime이 갭의 전부인가"를 못박는 실험용. 점수가 실측(13.15M)으로 가면 uptime이 전부.
        public bool ForceFullBuffUptime { get; set; }

        // 보스 반격(제이브 「복수의 갑옷」) 발동 확률 오버라이드(%). null이면 보스 정의값(25%) 그대로 사용.
        //   0 = 반격 OFF — 빔서치는 반격 RNG·시간경과에 의존하지 않는 로테를 산출(0회 발동에도 실행가능한 빌드).
        //   금요일(제이브) 외 보스는 Counterattack=null이라 이 값과 무관(월화수목토 무영향).
        public double? CounterattackChanceOverride { get; set; }

        // [버프 타게팅 강제] "공격력이 가장 높은 아군"(라이언·나타 평타쿨감, 비스킷 장비강화) 순위를 이 AllyParty 인덱스
        //   순서로 고정(앞이 1순위). 미포함 영웅은 그 뒤에 기존 순서(DamageWeight). null/빈 리스트면 기존 동작.
        //   인게임은 raw 공격력 기준이라, 유저가 지원형(리나)에 공격 세트를 끼워 일부러 쿨감 수령자로 만드는 빌드
        //   (예: 나타1위·리나2위 → 라이언쿨감→나타, 나타쿨감→리나)를 재현할 때 사용.
        public List<int> HighestAtkAllyOrder { get; set; }
        // true면 위 강제를 평타 쿨감(ApplyBasicAttackCdReduction)에만 적용하고 장비강화 등 스킬 버프는 기존 타게팅 유지.
        public bool HighestAtkAllyOverrideCdrOnly { get; set; }

        // [진단] true면 아군이 HP 피해를 받지 않는다(불사) — "생존을 전제로 한 순수 로테 품질" 비교 실험용. 점수 로직 불변.
        public bool DiagAlliesImmortal { get; set; }

        // [진단] true면 아군→적 치명·약점을 기댓값 대신 "항상 발동"으로 계산(실측 최고점 대조용 상한). 점수 비교 전용.
        public bool DiagForceCritWeak { get; set; }

        // 아군 스킬 쿨 안전 여유(초). 시전 시 쿨 = 원래 쿨 + 이 값 → 빔이 "딱 맞게 도는" 스텝을 피하고 항상 재현되는 로테만 산출.
        //   2026-10 월요일 연습전투: 시뮬 여유 2.0초 스텝(턴28 혼천·턴32 교만)이 DoT 지연 없으면 0초/−0.n초 → 시뮬 시간 ~2~2.5초 과대.
        //   0이면 기존 동작.
        public double AllyCooldownMargin { get; set; }
        // [진단] true면 시간 장부(SiegeBattleResult.TimeLedger) 기록 — 쿨 타이밍 검증용.
        public bool DiagTimeLedger { get; set; }

        // [시간 모델 보정] 평타 1회 소요시간(초). 기본 2.0 (정수 초 측정). 연습전투 쿨 타이밍 역산 후보 2.1.
        public double BasicAttackSeconds { get; set; } = 2.0;
        // [시간 모델 보정] true면 자기 스킬 쿨이 "시전 시작"부터 카운트(시전시간도 자기 쿨 감소). false면 "시전 완료 후"부터.
        //   2026-10 녹화 영상으로 확정: 완료 후(false). 미호 살육(1스킬) 시전 직후 70→1초 뒤 69, R2 미호 교만(2스킬) 컷신+사망연출
        //   직후 69, 울림 120→119. (이전 "시전 시작" 근거였던 유저 기록 R1 "나타 2스킬"은 실제로 1스킬(화첨)이었음 — 유저 확인.)
        public bool OwnCooldownFromCastStart { get; set; } = false;

        // 아군 사망 1명당 랭킹 점수 페널티. 0이면 없음(기존 동작). >0이면 빔/옵티마이저가 생존(버프해제)을 우선해
        //   전멸 빌드를 피한다. 보고 점수(TotalScore=실제 누적딜)는 불변 — RankScore에만 반영. 사망 없는 날은 무영향.
        public double AllyDeathPenalty { get; set; }
    }

    /// <summary>로테이션 플랜 1스텝: 특정 아군이 특정 스킬 시전, 또는 홀드(아무도 안 씀).</summary>
    public class RotationDecision
    {
        public int HeroIndex { get; set; }       // AllyParty 인덱스
        public SkillType Skill { get; set; }
        public bool Hold { get; set; }            // true면 이 스킬턴 스킵
        public override string ToString() => Hold ? "Hold" : $"H{HeroIndex}:{Skill}";
    }

    /// <summary>빔서치용 결정점: 한 아군 스킬턴에서 가능한 행동 후보 목록.</summary>
    public class RotationDecisionPoint
    {
        public int SkillTurnIndex { get; set; }
        public int Turn { get; set; }
        public List<RotationDecision> Choices { get; set; } = new();   // Hold 포함
    }
}

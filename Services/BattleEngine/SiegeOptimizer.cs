using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.Optimizer;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>공성전 탐색 입력.</summary>
    public class SiegeOptimizerConfig
    {
        public List<BattleCharacter> FixedMembers { get; set; } = new();   // 필수 포함 영웅(버퍼·면역·디버퍼 등)
        public List<BattleCharacter> Candidates { get; set; } = new();     // 나머지 후보 풀
        public Stage SiegeStage { get; set; }
        public Pet AllyPet { get; set; }
        public int PetStar { get; set; }
        public int PetEnhance { get; set; }   // 펫 스킬강화 (0=미강화, 1~3)
        public double PetOptionAtkRate { get; set; }
        public double PetOptionDefRate { get; set; }
        public double PetOptionHpRate { get; set; }
        public int MaxTurns { get; set; } = 70;
        public int PartySize { get; set; } = 5;

        // 장비 미지정 영웅에게 장비 옵티마이저로 최적 장비를 자동 장착할지 (R3 보스 기준 1회)
        public bool AutoEquip { get; set; } = true;

        // 전용무기 조율 4슬롯(전설 가정)을 영웅별로 풀시뮬 점수로 탐색할지. (메인옵/세공은 현 모델 미포함)
        //   기본 OFF(기존 동작 불변). ON이면 AutoEquip 기어 선택 후 영웅별 조율을 그리디 탐색.
        public bool SearchExclusiveWeapon { get; set; } = false;

        // 저점-우선 기어: 치확/약확을 (파티 버스트 버프 포함) 100%까지만 채우고 나머지는 치피/공%로.
        // 버프-정렬 로테이션 전제(가 가정)라 정렬 안 되면 평균 점수 소폭 하락 가능 → 기본 OFF, Phase 2와 함께 평가.
        public bool FloorFirstGear { get; set; } = false;

        // [실험] 딜러 기어 역할 제약 해제 — GetGearConstraints의 딜러 허용 세트/메인/부옵을 전 세트·광범위 옵션으로 개방.
        //   "옵티마이저가 제약 없이도 좋은 메인/부옵을 스스로 찾는가" 확인용. 기본 OFF(무회귀).
        public bool UnconstrainedGear { get; set; } = false;

        // 최종 best config(진형·자리·기어)에 스킬 로테이션 빔서치를 적용해 로테이션 최적 점수·플랜 산출
        public bool OptimizeRotation { get; set; } = true;
        public int RotationBeamWidth { get; set; } = 10;
        // 70턴 시즈의 아군 스킬턴(라운드 연쇄 포함 ~20개)을 전부 덮어야 막판 스킬턴까지 빔이 최적화한다.
        //   빔은 결정점 소진 시 조기종료(!extended)하므로 이 값은 상한일 뿐 — 넉넉히 둬도 낭비 없음.
        //   (이전 18은 막판 ~2 스킬턴을 자동 폴백=버프우선에 넘겨 끝물 공격스킬 채택을 놓쳤음)
        public int RotationMaxDepth { get; set; } = 28;

        // [반격 평균 탐색] >1이면 빔/채택 평가를 반격 ON(보스 정의 25%) N시드 평균으로 — 로테 선택이 반격 RNG에
        //   과적합되지 않게 평균점으로 고른다. 금요일(제이브)만 의미. 1이면 기존(빔=반격 OFF, 채택=ON 단일시드).
        //   플랜 구조(Dps)는 OFF로 결정론 유지하고 점수만 ON N시드 평균(RotationBeamSearch).
        public int CounterattackSearchSeeds { get; set; } = 1;

        // [채택 평가 평균 시드] >1이면 채택/비교 점수(ScoreOnPlan)를 N시드 평균으로 — 적의 아군 타겟 선정이
        //   랜덤(_rng)이라 단일 시드면 "누가 죽느냐"가 시드에 과적합됨(예: 일요일 미호가 시드777에서만 어둠의일격
        //   2연타로 사망). 채택/IsBetterPick의 RankScore가 기대 사망수를 반영하도록 평균. 빔(로테 발굴)은 단일
        //   시드 유지(로테 최적은 타겟 RNG에 둔감·속도). CounterattackSearchSeeds와 Max로 합쳐 적용.
        public int EvalSeeds { get; set; } = 1;

        // [허수아비 딜러 기어] ON이면 딜러(공격/마법/만능형)의 세트·메인부옵·전용조율을 "허수아비 단타 DPS"
        //   (스쿼드 풀버프 + 보스 HP0=잃은체력 최대)로 전수 탐색. 풀시뮬 좌표상승 프록시의 그리디·uptime 노이즈를
        //   제거하고 R3 넉 레짐을 직접 최대화. 탱·서포터는 기존 FullScore 유지. OFF면 전부 기존 동작(무회귀).
        public bool DummyGearForDealers { get; set; }

        // 진형 단일 강제 (실측 비교용). null이면 전 진형 탐색.
        public string ForcedFormation { get; set; }

        // [공성 진형 규칙] 기본진형(3후열) 포함 여부. 기본 OFF — 비스킷 장비강화 버프가 2명에게만 들어가 메인딜러는
        //   최대 2명이므로, 3후열은 약한 3번째 딜러에 후열공%를 분산해 비효율(유저 확정). 탐색은 밸런스(2)·보호(1)만.
        //   비교·디버그용으로만 true.
        public bool IncludeBasicFormation { get; set; }

        // [후열 = dummy DPS 상위 N딜러] 기본 ON. 후열마스크 전수탐색이 cadence 아티팩트(후열 누구냐가 속공/턴순서를
        //   바꿔 점수 ±흔들림)로 최고딜러를 전열에 버리는 오선택을 막는다. 도발은 열 무관(라이언 후열서도 탱킹)이라
        //   최고딜러는 후열에 둬야 공%버프까지 받아 무조건 이득. 후열 = 허수아비 단타 상위 requiredBack명으로 강제.
        //   false면 기존 전수 마스크 탐색(비교·디버그용).
        public bool DummyBackRow { get; set; } = true;

        // 후열 강제 (실측 비교용). null이면 전 마스크 탐색. 캐릭터 이름 리스트.
        public List<string> ForcedBackRow { get; set; }

        // 캐릭터별 강제 세트(실측 비교용). Key=Character.Id, Value=세트명("수문장","성기사" 등).
        // EquipCandidates에서 해당 캐릭터는 이 세트 후보만 사용 — 메인/부옵은 기존 로직대로 최적.
        public Dictionary<int, string> ForcedSetByCharId { get; set; } = new();

        // 캐릭터별 강제 메인옵션(실측 비교용). Key=Character.Id, Value=(무기메인, 방어구메인).
        // 지정 시 EquipCandidates는 해당 메인옵만 후보로 사용.
        public Dictionary<int, (string WeaponMain, string ArmorMain)> ForcedMainByCharId { get; set; } = new();

        // 기어 선정(세트·메인·부옵·전용) 평가에 쓸 정렬 로테이션. null이면 자동 로테(기존). 등록 시 기어가
        //   "정렬된 버스트" 기준으로 선정돼 버프-캡 스탯(약확/치확)이 치피/공%로 자동 재배분됨(메인옵 포함).
        //   HeroIndex = FixedMembers+Candidates 순서 기준.
        public List<RotationDecision> GearEvalRotation { get; set; }

        // 기어 선정 평가 진형 (null이면 "기본 진형"). 최종이 밸런스면 밸런스로 평가해야 메인옵 등 정합.
        public string GearEvalFormation { get; set; }
        // 기어 선정 평가 시 후열로 둘 영웅 이름(밸런스 등 진형 보너스 정합용). null이면 전부 전열.
        public List<string> GearEvalBackRow { get; set; }

        // 아군 사망 1명당 랭킹 페널티(딜). 보고 점수(TotalScore)는 불변, RankScore에만 반영. 0이면 페널티 없음.
        //   작게(20만): 막판 희생 같은 최적 공격빌드는 허용(>1등), 다수 조기사망 파괴적 전멸만 차단(안전망).
        //   큰 값(예 2M)은 0사망을 강요해 최고스펙 점수를 1등 미만으로 깎으므로 지양.
        public double AllyDeathPenalty { get; set; } = 200_000;

        // 생존(권능) 반지 후처리 사용 여부. 기본 ON(기존 동작). OFF면 ApplySurvivalRings 미실행 —
        //   6초월 실측 프로필처럼 권능반지를 끼지 않는 계정 가정 탐색에 사용.
        public bool EnableSurvivalRings { get; set; } = true;

        // [실험] 생존 장비 후처리(ApplySurvivalGear). 기본 OFF. 최종 config(진형·자리·로테 고정)에서 평가 시드 중 한 번이라도
        //   죽는 자동장착 영웅에게 생존 옵션(수문장·성기사 세트, 생명력%/방어력%/받피감% 메인, 생존 부옵)을 열어
        //   재탐색하고, 채택 기준(N시드 평균 RankScore)이 오를 때만 채택한다. 타입 무관(딜러 타입 미호도 대상).
        //   2026-11-17 일요일 실험: 미호(11/16시드 사망)·샤오·소교 모두 "생존 옵션으로 기대 점수 안 오름" → 채택 없음,
        //   탐색 +80분. 페널티 0(일요일 최고점 정책)에선 저기여 영웅 사망의 기대 손실이 딜 옵션 손실보다 작다는 뜻.
        public bool EnableSurvivalGear { get; set; } = false;
        // [스크리닝] 기어를 프록시(솔로 데미지) 1순위 세트 후보로만 — 풀시뮬 세트 비교·메인부옵 재최적화 생략.
        //   영웅 DB 탐색(tools/SiegeHeroExplore)의 대량 교체 평가용. 기본 OFF(정식 탐색 무영향).
        public bool QuickGear { get; set; } = false;
        // [실험] 생존반지 후보를 첫 시드 사망자가 아니라 "평가 시드 중 한 번이라도 죽는 영웅"으로 넓힘. 기본 OFF.
        public bool SurvivalAllSeedGating { get; set; } = false;

        // 속공 선행 탐색(후처리). 기본 OFF. 자동장착 영웅마다 "속공 부옵으로 아군 중 가장 먼저 행동"하게 만든 뒤
        //   로테를 재탐색해 랭크 점수가 오를 때만 채택한다. 행동 순서는 라운드 클리어 턴·스킬턴 정렬·쿨 여유를 바꾸는데
        //   부옵 그리디는 문턱(다른 아군 속공 초과)을 넘기 전까지 이득을 못 봐 스스로 찾지 못한다.
        //   2026-11-19 수요일 쥬리 편성: 비스킷 속공 19→31(부옵 3티어)이면 1턴에 R1 클리어 → 같은 장비 10.0M → 12.0M.
        public bool SearchSpeedLead { get; set; }

        // 행동 순서(진형 번호) 탐색(후처리). 기본 OFF. 같은 속공이면 진형 번호 1번부터 행동한다(유저 확인 2026-11-19):
        //   기본 전열 1·2 / 후열 3·4·5, 밸런스 전열 1·2·3 / 후열 4·5, 공격 전열 1 / 후열 2~5, 보호 전열 1~4 / 후열 5.
        //   전열끼리·후열끼리 번호를 바꿔 가며(전열이 항상 앞 번호) 순서마다 로테를 재탐색해 랭크 점수가 오르면 채택.
        //   순서에 따라 평타 쿨감(라이언 9초 등)이 스킬턴 직전에 들어오느냐가 갈려 딜러 스킬 횟수가 달라진다.
        public bool SearchActionOrder { get; set; }

        // 로테 다듬기(언덕 오르기). 기본 OFF. 빔이 찾은 플랜에서 스킬턴 한 칸의 선택을 다른 (영웅·스킬)로 바꾸거나
        //   가까운 두 칸을 맞바꿔 보며 랭크 점수가 오르는 동안 반복한다. 빔은 장비가 조금만 달라도 결과가 크게 흔들린다
        //   (2026-11-19 수요일 쥬리 편성: 1타 기댓값이 같은 두 장비에서 12.04M vs 10.43M — 같은 로테를 넣으면 12.41M / 12.48M).
        public bool PolishRotation { get; set; }

        // 공격 로테 + 생존 보강 트랙(후처리). 기본 OFF. 사망 페널티가 있으면 빔은 "아무도 안 죽는" 로테를 고르느라
        //   토템 영웅(라이언 등)의 스킬로 딜러 스킬턴을 메운다. 이 트랙은 (1) 페널티 없이 최대 딜 로테를 찾고
        //   (2) 그 로테에서 죽는 영웅에게 생존 반지·생존 장비를 입힌 뒤 (3) 페널티를 되살려 다시 다듬어,
        //   기존 결과보다 랭크 점수가 높을 때만 채택한다(무회귀).
        //   2026-11-19 수요일 쥬리 편성: 라이언 수문장 + 비스킷 권능 반지면 같은 딜러 장비로 11.0M → 12.6M.
        public bool SearchAggressiveSurvival { get; set; }

        // 공격력 순위 탐색(후처리). 기본 OFF. InGameBuffTargeting 전용.
        //   "공격력이 가장 높은 아군" 효과(비스킷 장비 강화·라이언 평타 쿨감 등)는 공격력 순위로 수령자가 갈린다.
        //   누가 1·2순위여야 하는지는 버프 가치·쿨감 누수·행동 순서가 얽혀 장비 그리디가 스스로 찾지 못한다
        //   (순위가 뒤집히는 문턱 전까지는 손해만 보인다). 이 트랙은
        //   (1) 순위를 강제(HighestAtkAllyOrder)한 채 로테를 재탐색해 "누가 1순위(·2순위)일 때 가장 높은가"를 훑고
        //   (2) 현재보다 높은 후보를 실제 장비로 실현한다 — 올릴 영웅은 공격력 쪽으로, 내릴 영웅은 공격력에서 빼는
        //       부옵 1티어 이동·부옵 교체·메인옵 교체 중 "순위 격차 1당 점수 손실"이 가장 작은 수를 반복
        //   (3) 강제를 풀고 정식 빔으로 재탐색해 랭크 점수가 오를 때만 채택한다(무회귀). 속공 부옵은 건드리지 않는다.
        public bool SearchAtkRank { get; set; }

        // ── 탐색 경로 (2026-11-20 유저 방침) ──
        //   정석(Standard): 기본 탐색 + 로테 다듬기 + 공격 로테·생존 보강. 속공 순서·공격력 순위처럼 "템 옵션을 따로 손봐야"
        //                   성립하는 수(SearchAtkRank / SearchActionOrder / SearchSpeedLead)는 쓰지 않는다 — 따라 하기 쉬운 빌드.
        //   고점(Peak):     정석 + 공격력 순위 → 행동 순서 → 속공 선행 → (순서가 바뀌면) 순위·생존 재탐색. 유저 최고점 추격용.
        //   고점 파이프라인의 앞부분이 곧 정석이므로, 고점 탐색 한 번이면 OnStage(StandardStage) 시점의 결과가 정석 빌드다.
        public void ApplyRoute(SiegeSearchRoute route)
        {
            bool peak = route == SiegeSearchRoute.Peak;
            PolishRotation = true;
            SearchAggressiveSurvival = true;
            SearchAtkRank = peak;
            SearchActionOrder = peak;
            SearchSpeedLead = peak;
        }

        // 단계 콜백 — 후처리 단계가 하나 끝날 때마다 (단계 이름, 그 시점 결과)를 알린다. 결과 객체는 이후 단계가 계속 고쳐 쓰므로
        //   받는 쪽이 그 자리에서 필요한 값을 복사해야 한다(SiegeBuildStore.Capture). StandardStage = 정석 경로가 끝난 시점.
        public System.Action<string, SiegeOptimizerResult> OnStage { get; set; }
        public const string StandardStage = "정석";

        // 시도 콜백 — 탐색이 "완성된 후보 하나"(자리·장비 + 그 후보의 최고 로테)를 평가할 때마다 알린다. 채택되지 않고 버려지는
        //   후보도 전부 온다(도태된 시도 기록용 — SiegeTrialStore). 넘어오는 파티·장비는 탐색이 계속 고쳐 쓰는 공유 객체이므로
        //   받는 쪽이 그 자리에서 복사해야 한다. 탐색 결과에는 영향을 주지 않는다(읽기 전용 알림).
        public System.Action<SiegeTrialInfo> OnTrial { get; set; }

        // 좌표상승(기어↔로테/진형) 2패스 탐색. 기본 OFF(기존 1패스 동작·무회귀).
        //   ON이면 1패스(기어=프록시 레짐)로 진형·자리·빔로테를 찾은 뒤, 그 최종 레짐을 GearEval*에 피드백해
        //   기어를 재최적화하고 다시 탐색 → 기어 선정 레짐 = 최종 평가 레짐 정합(2단계 근사 함정 교정).
        //   딜러 메인옵을 사람이 ForcedMain으로 박지 않아도 시뮬이 스스로 찾게 하는 게 목적.
        //   조합탐색(Candidates) 시엔 GearEvalRotation HeroIndex 정합이 깨질 수 있어 고정팀(Candidates 없음)에서만 적용.
        public bool CoordinateAscentGear { get; set; }

        // 외부 시드 로테이션 — 빔 교차수분(crossPlans)에 추가로 투입할 (HeroIndex,Skill) 플랜들. 예: 다른 초월
        //   프로필(6↔12초월)의 빔 결과. 빔이 이 config에서 못 찾은 더 높은 로테를 교차평가로 회수(무회귀).
        //   HeroIndex는 동일 팀 순서 기준 — 고정팀에서만 의미.
        public List<List<RotationDecision>> SeedRotations { get; set; }

        // [버프 타게팅 강제] SiegeBattleConfig.HighestAtkAllyOrder / ~CdrOnly 를 탐색 내 모든 시뮬에 그대로 전달.
        //   인덱스는 FixedMembers+Candidates 순서 기준 — 고정팀(Candidates 없음)에서만 의미. null이면 기존 동작.
        public List<int> HighestAtkAllyOrder { get; set; }
        public bool HighestAtkAllyOverrideCdrOnly { get; set; }
        // [인게임 버프 타게팅] SiegeBattleConfig.InGameBuffTargeting을 탐색 내 모든 시뮬에 전달.
        public bool InGameBuffTargeting { get; set; }
        // [실험] SiegeBattleConfig.HighestAtkBuffGate를 탐색 내 모든 시뮬에 전달.
        public System.Func<int, string, string, System.Collections.Generic.IReadOnlyList<string>, bool> HighestAtkBuffGate { get; set; }
        // [진단] 아군 불사(SiegeBattleConfig.DiagAlliesImmortal) — 생존 제약 없는 순수 로테 품질 탐색 실험용.
        public bool DiagAlliesImmortal { get; set; }
        // 아군 스킬 쿨 안전 여유(초) — SiegeBattleConfig.AllyCooldownMargin 전달. 0이면 기존 동작.
        public double AllyCooldownMargin { get; set; }
    }

    /// <summary>탐색 중 평가된 후보 1건(SiegeOptimizerConfig.OnTrial). Team = 진형 번호 순, IsBackPosition = 그 후보의 자리.</summary>
    public class SiegeTrialInfo
    {
        public string Phase { get; set; }                    // 어느 탐색 단계에서 나온 후보인지 (기본 탐색 / 공격력 순위 / 행동 순서 …)
        public List<BattleCharacter> Team { get; set; }
        public string Formation { get; set; }
        public List<RotationDecision> Plan { get; set; }
        public SiegeBattleResult Result { get; set; }
    }

    /// <summary>탐색 경로 — SiegeOptimizerConfig.ApplyRoute 설명 참조.</summary>
    public enum SiegeSearchRoute { Standard, Peak }

    /// <summary>공성전 탐색 결과 (최고딜 팀 + 진형).</summary>
    public class SiegeOptimizerResult
    {
        public List<BattleCharacter> BestParty { get; set; } = new();
        public string BestFormation { get; set; }
        public double BestScore { get; set; }              // 보고용 실제 딜(TotalScore)
        public double BestRankScore { get; set; }          // 선택용 랭킹 점수(생존 페널티 반영). 비교는 이 값으로.
        public SiegeBattleResult BestResult { get; set; }
        public List<string> BestBackRow { get; set; } = new();   // 최적 후열 배치 영웅 이름
        internal int BestMask { get; set; }                       // 최적 후열 배치 비트마스크 (재적용용)
        internal List<Accessory> RingedAcc { get; set; }          // 생존반지 적용 후 장신구 구성(후보별 격리 평가용, team 공유라 원복 후 재적용)
        public List<RotationDecision> BestRotationPlan { get; set; } = new();   // 빔서치 최적 스킬 로테이션
        public double AutoRotationScore { get; set; }             // 자동 로테이션 점수 (빔서치 전, 비교용)
        public int EvaluatedCount { get; set; }     // 평가한 (조합 × 진형 × 자리) 수
        public List<string> GearLog { get; set; } = new();   // 자동 장착된 영웅별 메인옵/부옵 값 로그
        public List<SiegeEvalEntry> EvalLog { get; set; } = new();   // 탐색 중 평가한 (팀×진형)별 점수
    }

    /// <summary>탐색 평가 1건 (팀 조합 × 진형 → 점수).</summary>
    public class SiegeEvalEntry
    {
        public string Formation { get; set; }
        public List<string> Party { get; set; } = new();
        public List<string> BackRow { get; set; } = new();   // 후열 배치 영웅
        public double Score { get; set; }
        public Dictionary<int, double> RoundScore { get; set; } = new();
    }

    /// <summary>
    /// 공성전 탐색: 영웅 조합 전수탐색 × 아군 진형 3종(기본/밸런스/보호) 순회 × 공성전 시뮬 → 최고딜 팀.
    /// 후보가 5~8명이라 C(N,5)×3 수준이라 전수탐색이 현실적([[simulator-optimizer-plan]]).
    /// 장비는 입력 BattleCharacter 그대로 사용 — 장비 옵티마이저(영웅×진형 캐싱) 연동은 후속.
    /// </summary>
    public class SiegeOptimizer
    {
        private readonly SiegeBattleSimulator _sim = new(GearCompareSeed);   // 고정 시드 — 배치/진형 탐색 재현성

        // 시도 기록(OnTrial)용 — 지금 어느 탐색 단계인지. Optimize가 단계마다 바꾼다.
        private string _trialPhase = "기본 탐색";
        private void Trial(SiegeOptimizerConfig config, List<BattleCharacter> team, string formation,
            List<RotationDecision> plan, SiegeBattleResult r)
        {
            if (config.OnTrial == null || r == null || team == null) return;
            config.OnTrial(new SiegeTrialInfo { Phase = _trialPhase, Team = team, Formation = formation, Plan = plan, Result = r });
        }

        /// <summary>시도 기록용 재생 — 탐색과 같은 조건(시드 777)으로 한 번 돌리되 스킬 순서 실행 기록(Feasibility)을 남긴다.
        ///   party는 진형 번호 순, IsBackPosition이 맞춰져 있어야 한다. 점수는 단일 시드 값(N시드 평균 요일은 EvaluateBuild와 다를 수 있음).</summary>
        public static SiegeBattleResult ReplayWithCasts(SiegeOptimizerConfig config, List<BattleCharacter> party,
            string formation, List<RotationDecision> plan, bool leak = false)
        {
            var sc = BuildSimConfig(config, party, formation);
            sc.RotationPlan = plan;
            sc.RecordFeasibility = true;
            sc.RecordLeak = leak;   // 누수 장부(result.Leak)도 함께
            return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
        }

        // 공성전 아군 순회 진형 (공격 진형 제외 — 기획)
        private static readonly string[] Formations = { "기본 진형", "밸런스 진형", "보호 진형" };

        /// <summary>
        /// 공성 탐색 진입점. 좌표상승 OFF면 1패스(OptimizeOnce) 그대로. ON이면 2패스:
        ///   1패스 결과(최종 진형·자리·빔로테)를 GearEval*에 피드백해 기어를 최종 레짐에서 재최적화하고 재탐색,
        ///   무회귀(IsBetterPick)로 더 나은 패스 채택. 공유 BattleCharacter 객체라 1패스 기어를 스냅샷해 복원한다.
        /// </summary>
        public SiegeOptimizerResult Optimize(SiegeOptimizerConfig config)
        {
            // 자동장착 대상(시작 시점 장비 미지정) — 생존 장비 후처리는 이 영웅들만 건드린다(유저 지정 장비 보존).
            var autoEquipped = config.AutoEquip
                ? config.FixedMembers.Concat(config.Candidates ?? new List<BattleCharacter>())
                    .Where(bc => bc != null && bc.Equipment == null).ToHashSet()
                : new HashSet<BattleCharacter>();
            _trialPhase = "기본 탐색";
            var res = OptimizeCore(config);
            if (config.AutoEquip && config.EnableSurvivalGear && res?.BestParty != null && res.BestParty.Count > 0)
                ApplySurvivalGear(config, res, autoEquipped);
            bool hasRes = res?.BestParty != null && res.BestParty.Count > 0;
            void Stage(string name) { if (hasRes) config.OnStage?.Invoke(name, res); }
            Stage("기본 탐색");
            if (config.PolishRotation && config.OptimizeRotation && hasRes && res.BestRotationPlan != null && res.BestRotationPlan.Count > 0)
            {
                for (int i = 0; i < res.BestParty.Count; i++)
                    res.BestParty[i].IsBackPosition = (res.BestMask & (1 << i)) != 0;
                var cur0 = ScoreOnPlan(config, res.BestParty, res.BestFormation, res.BestRotationPlan);
                var (pr, pp) = PolishPlan(config, res.BestParty, res.BestFormation, cur0, res.BestRotationPlan);
                if (IsBetterPick(pr.TotalScore, pr.RankScore, res.BestScore, res.BestRankScore))
                {
                    res.GearLog.Add($"[로테 다듬기] {res.BestScore:N0} → {pr.TotalScore:N0}");
                    res.BestScore = pr.TotalScore; res.BestRankScore = pr.RankScore; res.BestResult = pr; res.BestRotationPlan = pp;
                }
            }
            if (config.PolishRotation && config.OptimizeRotation) Stage("로테 다듬기");
            if (config.SearchAggressiveSurvival && config.AutoEquip && config.OptimizeRotation && hasRes)
            {
                _trialPhase = "공격 로테+생존 보강";
                ApplyAggressiveSurvival(config, res, autoEquipped);
                Stage("공격 로테+생존 보강");
            }
            Stage(SiegeOptimizerConfig.StandardStage);   // ── 여기까지가 정석 경로. 아래는 고점 경로 전용(템 옵션을 따로 손보는 수) ──
            bool changed = false;
            bool rankOn = config.SearchAtkRank && config.AutoEquip && config.OptimizeRotation && hasRes;
            if (rankOn) { _trialPhase = "공격력 순위"; changed |= ApplyAtkRank(config, res, autoEquipped); Stage("공격력 순위"); }   // 순서 탐색 전에 — 쿨감 수령자가 맞아야 순서가 의미 있다
            bool orderChanged = false;
            if (config.SearchActionOrder && config.OptimizeRotation && hasRes)
            {
                _trialPhase = "행동 순서";
                orderChanged |= ApplyActionOrder(config, res);
                Stage("행동 순서");
            }
            if (config.AutoEquip && config.SearchSpeedLead && config.OptimizeRotation && hasRes)
            {
                _trialPhase = "속공 선행";
                orderChanged |= ApplySpeedLead(config, res, autoEquipped);   // 선행 영웅 × 나머지 순서를 함께 탐색
                Stage("속공 선행");
            }
            if (rankOn && orderChanged) { _trialPhase = "공격력 순위(순서 변경 후)"; changed |= ApplyAtkRank(config, res, autoEquipped); Stage("공격력 순위(순서 변경 후)"); }   // 순서가 바뀌면 1순위여야 할 영웅도 달라질 수 있다
            changed |= orderChanged;
            if (changed && config.SearchAggressiveSurvival && config.AutoEquip && config.OptimizeRotation)
            {
                _trialPhase = "생존 보강(순서 변경 후)";
                ApplyAggressiveSurvival(config, res, autoEquipped);     // 순서가 바뀌면 죽는 영웅도 달라진다
                Stage("생존 보강(순서 변경 후)");
            }
            return res;
        }

        /// <summary>
        /// 공격 로테 + 생존 보강 트랙 — SiegeOptimizerConfig.SearchAggressiveSurvival 설명 참조.
        /// </summary>
        private bool ApplyAggressiveSurvival(SiegeOptimizerConfig config, SiegeOptimizerResult best, HashSet<BattleCharacter> autoEquipped)
        {
            if (config.AllyDeathPenalty <= 0) return false;
            var party = best.BestParty;
            for (int i = 0; i < party.Count; i++)
                party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

            // (1) 페널티 없이 최대 딜 로테
            double pen = config.AllyDeathPenalty;
            config.AllyDeathPenalty = 0;
            List<RotationDecision> rawPlan;
            try { (_, rawPlan) = SearchRotationFor(config, party, best.BestFormation, config.RotationBeamWidth, new[] { best.BestRotationPlan }); }
            finally { config.AllyDeathPenalty = pen; }
            if (rawPlan == null || rawPlan.Count == 0) return false;
            var withPen = ScoreOnPlan(config, party, best.BestFormation, rawPlan);

            // (2) 그 로테를 기준으로 생존 보강 (장비·반지는 공유 객체라 스냅샷 후 실패 시 복원)
            var eqSnap = party.Select(b => b.Equipment).ToList();
            var accSnap = party.Select(b => b.Equipment?.Accessory).ToList();
            var saved = (Score: best.BestScore, Rank: best.BestRankScore, Res: best.BestResult, Plan: best.BestRotationPlan, Ringed: best.RingedAcc);
            int logMark = best.GearLog.Count;
            best.BestRotationPlan = rawPlan; best.BestScore = withPen.TotalScore; best.BestRankScore = withPen.RankScore; best.BestResult = withPen;
            if (withPen.Deaths != null && withPen.Deaths.Count > 0)
            {
                if (config.EnableSurvivalRings) ApplySurvivalRings(config, best);
                ApplySurvivalGear(config, best, autoEquipped);
            }
            // (3) 페널티를 되살려 다시 탐색·다듬기
            var (fr, fp) = SearchRotationFor(config, party, best.BestFormation, config.RotationBeamWidth,
                new[] { best.BestRotationPlan, rawPlan, saved.Plan });
            if (fr != null && IsBetterPick(fr.TotalScore, fr.RankScore, best.BestScore, best.BestRankScore))
            { best.BestScore = fr.TotalScore; best.BestRankScore = fr.RankScore; best.BestResult = fr; best.BestRotationPlan = fp; }

            if (IsBetterPick(best.BestScore, best.BestRankScore, saved.Score, saved.Rank))
            {
                best.RingedAcc = party.Select(b => b.Equipment?.Accessory).ToList();
                best.GearLog.Add($"[공격 로테+생존 보강] 채택: {saved.Score:N0}(랭크 {saved.Rank:N0}) → {best.BestScore:N0}(랭크 {best.BestRankScore:N0})"
                    + $" — 페널티 없는 로테 {withPen.TotalScore:N0}·사망 {withPen.Deaths?.Count ?? 0}에서 출발");
                return true;
            }
            for (int i = 0; i < party.Count; i++)
            {
                party[i].Equipment = eqSnap[i];
                if (party[i].Equipment != null) party[i].Equipment.Accessory = accSnap[i];
            }
            if (best.GearLog.Count > logMark) best.GearLog.RemoveRange(logMark, best.GearLog.Count - logMark);
            best.GearLog.Add($"[공격 로테+생존 보강] 미채택: 시도 결과 {best.BestScore:N0}(랭크 {best.BestRankScore:N0}) ≤ 현재 {saved.Score:N0}(랭크 {saved.Rank:N0})");
            best.BestScore = saved.Score; best.BestRankScore = saved.Rank; best.BestResult = saved.Res; best.BestRotationPlan = saved.Plan; best.RingedAcc = saved.Ringed;
            return false;
        }

        /// <summary>파티에 "공격력 최고 아군" 대상 효과가 있으면 그 최대 대상 수(쿨감형 = 1), 없으면 0.</summary>
        private static int AtkRankTargetCount(List<BattleCharacter> party)
        {
            int n = 0;
            foreach (var bc in party)
                foreach (var sk in bc.Character?.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (sk.IsAwakenOnly && !bc.IsAwakened) continue;
                    var effs = sk.GetLevelData(bc.IsSkillEnhanced, bc.IsAwakened)?.Effects;
                    if (effs == null) continue;
                    foreach (var e in effs)
                    {
                        if (e.TargetSelector == Models.Effects.TargetSelector.HighestAtkAlly)
                            n = Math.Max(n, Math.Max(1, sk.GetTranscendBonus(bc.TranscendLevel)?.TargetCountOverride ?? e.TargetCount));
                        else if (e.Target == Models.Effects.EffectTarget.SelfAndHighestAtkAlly)
                            n = Math.Max(n, 1);
                    }
                }
            return n;
        }

        /// <summary>현재 장비 기준 아군 스탯 공격력(진형 보너스·펫 포함) — 인게임 "공격력 최고 아군" 순위의 바탕값.</summary>
        private static Dictionary<BattleCharacter, double> AllyAtks(SiegeOptimizerConfig config, SiegeOptimizerResult best,
            List<BattleCharacter> party)
        {
            var bcfg = new BattleConfig
            {
                AllyParty = party, AllyPet = config.AllyPet, PetStar = config.PetStar, PetEnhance = config.PetEnhance,
                PetOptionAtkRate = config.PetOptionAtkRate, FormationName = best.BestFormation,
            };
            var bsim = new BattleSimulator();
            return party.ToDictionary(b => b, b => bsim.InitializeCharacterState(bcfg, b, party.IndexOf(b)).FinalAtk);
        }

        /// <summary>공격력 순위 탐색 — SiegeOptimizerConfig.SearchAtkRank 설명 참조.</summary>
        private bool ApplyAtkRank(SiegeOptimizerConfig config, SiegeOptimizerResult best, HashSet<BattleCharacter> autoEquipped)
        {
            if (!config.InGameBuffTargeting || config.HighestAtkAllyOrder != null) return false;
            var party = best.BestParty;
            int buffTargets = AtkRankTargetCount(party);
            if (buffTargets <= 0) return false;
            for (int i = 0; i < party.Count; i++)
                party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
            string formation = best.BestFormation;
            int wScreen = config.PolishRotation ? Math.Max(4, config.RotationBeamWidth * 2 / 5) : Math.Max(6, config.RotationBeamWidth * 2 / 3);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            const string SPD = "속공";
            string[] atkStats = { "공격력%", "공격력" };

            string Nm(int i) => party[i].Character?.Name;
            string OrderStr(List<int> o) => string.Join(">", o.Select(Nm));
            string AtkStr(Dictionary<BattleCharacter, double> a) =>
                string.Join(" > ", party.OrderByDescending(b => a[b]).Select(b => $"{b.Character?.Name} {a[b]:N0}"));
            (SiegeBattleResult R, List<RotationDecision> Plan) ForcedSearch(List<int> order, int width, IEnumerable<List<RotationDecision>> extra)
            {
                config.HighestAtkAllyOrder = order;
                try { return SearchRotationFor(config, party, formation, width, extra); }
                finally { config.HighestAtkAllyOrder = null; }
            }
            SiegeBattleResult ForcedScore(List<int> order, List<RotationDecision> plan)
            {
                config.HighestAtkAllyOrder = order;
                try { return ScoreOnPlan(config, party, formation, plan); }
                finally { config.HighestAtkAllyOrder = null; }
            }

            // (1) 순위 강제 탐색: 1순위 후보 5명 → 상위 2명에 대해 2순위까지(대상 2명 이상인 버프가 있을 때만).
            var cands = new List<(List<int> Order, SiegeBattleResult R, List<RotationDecision> Plan)>();
            for (int a = 0; a < party.Count; a++)
            {
                var o = new List<int> { a };
                var (r, p) = ForcedSearch(o, wScreen, new[] { best.BestRotationPlan });
                if (r != null) cands.Add((o, r, p));
            }
            if (buffTargets >= 2)
                foreach (var top in cands.OrderByDescending(c => c.R.RankScore).ThenByDescending(c => c.R.TotalScore).Take(2).ToList())
                    for (int b = 0; b < party.Count; b++)
                    {
                        if (b == top.Order[0]) continue;
                        var o = new List<int> { top.Order[0], b };
                        var (r, p) = ForcedSearch(o, wScreen, new[] { best.BestRotationPlan, top.Plan });
                        if (r != null) cands.Add((o, r, p));
                    }
            var atk0 = AllyAtks(config, best, party);
            best.GearLog.Add($"[공격력 순위] 현재 {AtkStr(atk0)} (팀점수 {best.BestScore:N0}) — 순위 강제 시: "
                + string.Join(" / ", cands.OrderByDescending(c => c.R.RankScore).Select(c => $"{OrderStr(c.Order)} {c.R.TotalScore:N0}")));

            // (2) 현재보다 높은 후보를 장비로 실현.
            var movable = party.Where(b => autoEquipped.Contains(b) && b.Equipment != null).ToList();
            static List<(string Main, List<(string Stat, int Tier)> Subs)> SnapGear(BattleCharacter bc) =>
                bc.Equipment.GetEquipments().Select(e => (e.MainStatName, e.SubSlots.Select(x => (x.StatName, x.Tier)).ToList())).ToList();
            static void PutGear(BattleCharacter bc, List<(string Main, List<(string Stat, int Tier)> Subs)> snap)
            {
                var eq = bc.Equipment.GetEquipments().ToList();
                for (int e = 0; e < eq.Count; e++)
                {
                    eq[e].MainStatName = snap[e].Main;
                    for (int x = 0; x < eq[e].SubSlots.Count; x++)
                    { eq[e].SubSlots[x].StatName = snap[e].Subs[x].Stat; eq[e].SubSlots[x].Tier = snap[e].Subs[x].Tier; }
                }
            }
            var origGear = movable.ToDictionary(b => b, SnapGear);
            void RestoreAll() { foreach (var b in movable) PutGear(b, origGear[b]); }

            // 순위 위반량(부족분 합): 강제 순서의 마지막 영웅은 나머지 전원보다, 그 앞 영웅은 "뒤 영웅이 도달해야 할 값"보다
            //   m 이상 높아야 한다. 뒤 영웅의 목표치를 기준으로 삼아야 동률(예: 1순위 후보 = 순서 밖 영웅)에서 막히지 않는다 —
            //   인접 쌍의 현재 차이만 보면 "2순위를 올리면 1순위를 넘고, 1순위만 올리는 수는 위반이 안 줄어" 한 수씩으로는 못 푼다.
            double Viol(Dictionary<BattleCharacter, double> a, List<int> order, double m)
            {
                double v = 0, floor = double.NegativeInfinity;
                double lastAtk = a[party[order[order.Count - 1]]];
                for (int i = 0; i < party.Count; i++)
                    if (!order.Contains(i))
                    {
                        v += Math.Max(0, a[party[i]] + m - lastAtk);
                        floor = Math.Max(floor, a[party[i]]);
                    }
                double need = Math.Max(lastAtk, floor + m) + m;   // 마지막 영웅이 조건을 채웠을 때의 값 + m
                for (int i = order.Count - 2; i >= 0; i--)
                {
                    double cur = a[party[order[i]]];
                    v += Math.Max(0, need - cur);
                    need = Math.Max(cur, need) + m;
                }
                return v;
            }
            // 한 영웅의 공격력을 올리는(dir>0)/내리는(dir<0) 장비 수 — 효과가 같은 수는 Key로 한 번만.
            IEnumerable<(string Key, string Desc, Action Do, Action Undo)> Moves(BattleCharacter b, int dir)
            {
                var gc = GetGearConstraints(b, config.SiegeStage, config.UnconstrainedGear);
                string hero = b.Character.Name;
                foreach (var e in b.Equipment.GetEquipments())
                {
                    string m0 = e.MainStatName;
                    var avail = EquipmentDb.MainStatDb.AvailableOptions.TryGetValue(e.Slot ?? "", out var av) ? av : new string[0];
                    var mains = ((e.Slot == "무기" ? gc?.WeaponMains : gc?.ArmorMains) ?? new string[0]).Where(avail.Contains).ToArray();
                    if (dir < 0 && m0 == "공격력%")
                        foreach (var alt in mains.Where(x => !atkStats.Contains(x)))
                            yield return ($"{hero}|main|{e.Slot}|{alt}", $"{hero} {e.Name} 메인 공격력%→{alt}", () => e.MainStatName = alt, () => e.MainStatName = m0);
                    if (dir > 0 && m0 != "공격력%" && mains.Contains("공격력%"))
                        yield return ($"{hero}|main|{e.Slot}|{m0}→공", $"{hero} {e.Name} 메인 {m0}→공격력%", () => e.MainStatName = "공격력%", () => e.MainStatName = m0);

                    var slots = e.SubSlots.Where(x => !string.IsNullOrEmpty(x.StatName) && x.StatName != SPD).ToList();
                    var names = e.SubSlots.Select(x => x.StatName).ToHashSet();
                    foreach (var a in slots.Where(x => atkStats.Contains(x.StatName)))
                        foreach (var n in slots.Where(x => !atkStats.Contains(x.StatName)))
                        {
                            if (dir < 0 && a.Tier > 1 && n.Tier < 6)
                                yield return ($"{hero}|sub|{a.StatName}→{n.StatName}", $"{hero} {e.Name} 부옵 {a.StatName}→{n.StatName} 1티어",
                                    () => { a.Tier--; n.Tier++; }, () => { a.Tier++; n.Tier--; });
                            if (dir > 0 && n.Tier > 1 && a.Tier < 6)
                                yield return ($"{hero}|sub|{n.StatName}→{a.StatName}", $"{hero} {e.Name} 부옵 {n.StatName}→{a.StatName} 1티어",
                                    () => { n.Tier--; a.Tier++; }, () => { n.Tier++; a.Tier--; });
                        }
                    if (dir < 0)
                        foreach (var a in slots.Where(x => atkStats.Contains(x.StatName)))
                            foreach (var nn in (gc?.SubOptions ?? new string[0]).Concat(new[] { "생명력%", "방어력%" }).Distinct()
                                         .Where(x => !atkStats.Contains(x) && x != SPD && !names.Contains(x)))
                            {
                                string old = a.StatName;
                                yield return ($"{hero}|ren|{old}{a.Tier}→{nn}", $"{hero} {e.Name} 부옵 {old}({a.Tier}티어)→{nn}", () => a.StatName = nn, () => a.StatName = old);
                            }
                    if (dir > 0)
                        foreach (var n in slots.Where(x => !atkStats.Contains(x.StatName)))
                            foreach (var nn in atkStats.Where(x => !names.Contains(x)))
                            {
                                string old = n.StatName;
                                yield return ($"{hero}|ren|{old}{n.Tier}→{nn}", $"{hero} {e.Name} 부옵 {old}({n.Tier}티어)→{nn}", () => n.StatName = nn, () => n.StatName = old);
                            }
                }
            }

            var trials = new List<(List<int> Order, Dictionary<BattleCharacter, List<(string Main, List<(string Stat, int Tier)> Subs)>> Gear,
                List<string> Steps, SiegeBattleResult R, List<RotationDecision> Plan)>();
            int attempted = 0;
            foreach (var c in cands.Where(c => IsBetterPick(c.R.TotalScore, c.R.RankScore, best.BestScore, best.BestRankScore))
                                   .OrderByDescending(c => c.R.RankScore).ThenByDescending(c => c.R.TotalScore))
            {
                if (attempted >= 4 || trials.Count >= 2) break;
                attempted++;
                var steps = new List<string>();
                bool ok = true, realized = false;
                // 여유 단계별 (장비, 수 개수, 강제 없는 점수) — 강제 때 점수가 안 나와도 그중 가장 높은 단계를 쓴다.
                var stages = new List<(Dictionary<BattleCharacter, List<(string Main, List<(string Stat, int Tier)> Subs)>> Gear, int StepCount, double Free, double Margin)>();
                // 여유(공격력)를 넓혀 가며: 스탯 순위가 맞아도 전투 중 공격력 감소·버프로 순위가 뒤집힐 수 있다.
                foreach (double margin in new[] { 1.0, 60.0, 150.0 })
                {
                    for (int step = 0; step < 20; step++)
                    {
                        var a = AllyAtks(config, best, party);
                        double v0 = Viol(a, c.Order, margin);
                        if (v0 <= 0) break;
                        double s0 = ForcedScore(c.Order, c.Plan).RankScore;
                        (string Key, string Desc, Action Do, Action Undo) pick = default;
                        double pickEff = double.PositiveInfinity, pickV = v0;
                        var seen = new HashSet<string>();
                        var moves = new List<(string Key, string Desc, Action Do, Action Undo)>();
                        foreach (var b in movable)
                        {
                            int idx = party.IndexOf(b);
                            if (c.Order.Contains(idx)) moves.AddRange(Moves(b, +1));
                            if (idx != c.Order[0]) moves.AddRange(Moves(b, -1));
                        }
                        foreach (var mv in moves)
                        {
                            if (!seen.Add(mv.Key)) continue;
                            mv.Do();
                            double v = Viol(AllyAtks(config, best, party), c.Order, margin);
                            if (v < v0 - 0.5)
                            {
                                double s = ForcedScore(c.Order, c.Plan).RankScore;
                                double eff = (s0 - s) / (v0 - v);   // 순위 격차 1을 줄이는 데 드는 점수 손실 (음수 = 이득)
                                if (eff < pickEff) { pickEff = eff; pick = mv; pickV = v; }
                            }
                            mv.Undo();
                        }
                        if (pick.Do == null) { ok = false; break; }
                        pick.Do();
                        steps.Add($"{pick.Desc} (격차 {v0:N0}→{pickV:N0})");
                    }
                    if (!ok || Viol(AllyAtks(config, best, party), c.Order, margin) > 0) { ok = false; break; }
                    // 강제 없이 같은 로테를 돌려 강제 때와 같은 점수가 나오면 순위가 실제로 실현된 것.
                    double forced = ForcedScore(c.Order, c.Plan).RankScore;
                    double free = Math.Max(ScoreOnPlan(config, party, formation, c.Plan).RankScore,
                                           ScoreOnPlan(config, party, formation, best.BestRotationPlan).RankScore);
                    stages.Add((movable.ToDictionary(b => b, SnapGear), steps.Count, free, margin));
                    if (free >= forced * 0.985) { realized = true; break; }
                }
                if (stages.Count == 0)
                {
                    var af = AllyAtks(config, best, party);
                    best.GearLog.Add($"[공격력 순위] {OrderStr(c.Order)} — 장비 조정으로 실현 불가(자동장착 영웅의 부옵·메인옵으로 격차를 못 메움): "
                        + $"{steps.Count}수 후 남은 격차 {Viol(af, c.Order, 1):N0}, {AtkStr(af)}");
                    RestoreAll();
                    continue;
                }
                var stage = stages.OrderByDescending(x => x.Free).ThenBy(x => x.StepCount).First();
                foreach (var b in movable) PutGear(b, stage.Gear[b]);
                steps = steps.Take(stage.StepCount).ToList();
                var (fr, fp) = SearchRotationFor(config, party, formation, config.RotationBeamWidth, new[] { c.Plan, best.BestRotationPlan });
                if (fr != null)
                {
                    trials.Add((c.Order, movable.ToDictionary(b => b, SnapGear), steps, fr, fp));
                    best.GearLog.Add($"[공격력 순위] {OrderStr(c.Order)} 시도: 강제 {c.R.TotalScore:N0} → 장비 조정 {steps.Count}수(여유 {stage.Margin:0}) 후 {fr.TotalScore:N0}"
                        + $"{(realized ? "" : " (강제 때와 수령자가 다름)")} — {AtkStr(AllyAtks(config, best, party))}");
                }
                RestoreAll();
            }

            var win = trials.OrderByDescending(t => t.R.RankScore).ThenByDescending(t => t.R.TotalScore).FirstOrDefault();
            if (win.R == null || !IsBetterPick(win.R.TotalScore, win.R.RankScore, best.BestScore, best.BestRankScore))
            {
                best.GearLog.Add($"[공격력 순위] 유지 — 현재 {best.BestScore:N0}"
                    + (win.R != null ? $", 최고 시도 {OrderStr(win.Order)} {win.R.TotalScore:N0}" : ", 현재보다 높은 순위 후보 없음")
                    + $" ({clock.Elapsed.TotalMinutes:F1}분)");
                return false;
            }
            foreach (var b in movable) PutGear(b, win.Gear[b]);
            best.GearLog.Add($"[공격력 순위] 채택: {OrderStr(win.Order)} 우선 — 팀점수 {best.BestScore:N0} → {win.R.TotalScore:N0}, {AtkStr(AllyAtks(config, best, party))} ({clock.Elapsed.TotalMinutes:F1}분)");
            foreach (var s in win.Steps) best.GearLog.Add($"  [공격력 순위] {s}");
            best.BestScore = win.R.TotalScore; best.BestRankScore = win.R.RankScore; best.BestResult = win.R; best.BestRotationPlan = win.Plan;
            return true;
        }

        /// <summary>로테 빔 재탐색 + 후보 플랜 평가 → (최고 결과, 그 플랜). team 순서 = 진형 번호 순.</summary>
        private (SiegeBattleResult R, List<RotationDecision> Plan) SearchRotationFor(SiegeOptimizerConfig config,
            List<BattleCharacter> team, string formation, int beamWidth, IEnumerable<List<RotationDecision>> extraPlans)
        {
            var beam = new RotationBeamSearch(GearCompareSeed).Search(
                BuildSimConfig(config, team, formation), beamWidth, config.RotationMaxDepth, config.CounterattackSearchSeeds);
            var plans = new List<List<RotationDecision>>();
            if (beam.Plan != null && beam.Plan.Count > 0) plans.Add(beam.Plan);
            if (beam.RankPlan != null && beam.RankPlan.Count > 0) plans.Add(beam.RankPlan);
            if (extraPlans != null) plans.AddRange(extraPlans.Where(p => p != null && p.Count > 0));
            SiegeBattleResult pick = null; List<RotationDecision> pickPlan = null;
            foreach (var plan in plans)
            {
                var r = ScoreOnPlan(config, team, formation, plan);
                if (pick == null || IsBetterPick(r.TotalScore, r.RankScore, pick.TotalScore, pick.RankScore)) { pick = r; pickPlan = plan; }
            }
            if (config.PolishRotation && pick != null)
                (pick, pickPlan) = PolishPlan(config, team, formation, pick, pickPlan);
            Trial(config, team, formation, pickPlan, pick);
            return (pick, pickPlan);
        }

        /// <summary>
        /// 로테 다듬기 — 플랜의 각 스킬턴 칸에서 (가) 다른 영웅·스킬로 교체, (나) 3칸 이내의 칸과 맞바꾸기를 시도해
        /// 랭크 점수가 가장 많이 오르는 수를 적용하고, 한 바퀴 동안 개선이 없을 때까지 반복한다(최대 6바퀴, 무회귀).
        /// </summary>
        private static (SiegeBattleResult R, List<RotationDecision> Plan) PolishPlan(SiegeOptimizerConfig config,
            List<BattleCharacter> team, string formation, SiegeBattleResult start, List<RotationDecision> startPlan)
        {
            var plan = startPlan.Select(d => new RotationDecision { HeroIndex = d.HeroIndex, Skill = d.Skill, Hold = d.Hold }).ToList();
            var cur = start;
            var skills = new[] { SkillType.Skill1, SkillType.Skill2 };
            for (int pass = 0; pass < 6; pass++)
            {
                bool improved = false;
                for (int i = 0; i < plan.Count; i++)
                {
                    SiegeBattleResult bestR = null; Action bestApply = null;
                    void Consider(Action apply, Action undo)
                    {
                        apply();
                        var r = ScoreOnPlan(config, team, formation, plan);
                        undo();
                        if (IsBetterPick(r.TotalScore, r.RankScore, cur.TotalScore, cur.RankScore)
                            && (bestR == null || IsBetterPick(r.TotalScore, r.RankScore, bestR.TotalScore, bestR.RankScore)))
                        { bestR = r; bestApply = apply; }
                    }
                    var orig = plan[i];
                    for (int h = 0; h < team.Count; h++)
                        foreach (var sk in skills)
                        {
                            if (!orig.Hold && orig.HeroIndex == h && orig.Skill == sk) continue;
                            var cand = new RotationDecision { HeroIndex = h, Skill = sk };
                            int idx = i;
                            Consider(() => plan[idx] = cand, () => plan[idx] = orig);
                        }
                    for (int j = i + 1; j < plan.Count && j <= i + 3; j++)
                    {
                        int a = i, b = j;
                        Consider(() => (plan[a], plan[b]) = (plan[b], plan[a]), () => (plan[a], plan[b]) = (plan[b], plan[a]));
                    }
                    if (bestApply != null) { bestApply(); cur = bestR; improved = true; }
                }
                if (!improved) break;
            }
            return (cur, plan);
        }

        private static IEnumerable<List<T>> Permutations<T>(List<T> items)
        {
            if (items.Count <= 1) { yield return new List<T>(items); yield break; }
            for (int i = 0; i < items.Count; i++)
            {
                var rest = new List<T>(items); rest.RemoveAt(i);
                foreach (var p in Permutations(rest)) { p.Insert(0, items[i]); yield return p; }
            }
        }

        /// <summary>현재 장비 기준 아군 속공(진형 보너스·펫 포함).</summary>
        private static Dictionary<BattleCharacter, double> AllySpeeds(SiegeOptimizerConfig config, SiegeOptimizerResult best,
            List<BattleCharacter> party)
        {
            var bcfg = new BattleConfig
            {
                AllyParty = party, AllyPet = config.AllyPet, PetStar = config.PetStar, PetEnhance = config.PetEnhance,
                PetOptionAtkRate = config.PetOptionAtkRate, FormationName = best.BestFormation,
            };
            var bsim = new BattleSimulator();
            return party.ToDictionary(b => b, b => bsim.InitializeCharacterState(bcfg, b, party.IndexOf(b)).FinalSpd);
        }

        /// <summary>
        /// 진형 번호 순서 후보를 좁은 빔으로 훑는다 — 전열끼리·후열끼리 번호를 바꾼 모든 순서(전열이 앞 번호).
        /// 실제 행동 순서(속공 내림차순 → 번호)가 같은 것은 한 번만 본다. includeCurrent=false면 현재 순서는 제외.
        /// 반환 team 순서 = 진형 번호 순, Plan의 HeroIndex는 그 team 기준.
        /// </summary>
        private List<(List<BattleCharacter> Team, SiegeBattleResult R, List<RotationDecision> Plan)> ScreenOrders(
            SiegeOptimizerConfig config, SiegeOptimizerResult best, bool includeCurrent)
        {
            var party = best.BestParty;
            for (int i = 0; i < party.Count; i++)
                party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
            var front = party.Where(b => !b.IsBackPosition).ToList();
            var back = party.Where(b => b.IsBackPosition).ToList();
            var spd = AllySpeeds(config, best, party);
            string Key(List<BattleCharacter> team) => string.Join(">", team.Select((b, i) => (b, i))
                .OrderByDescending(x => spd[x.b]).ThenBy(x => x.i).Select(x => x.b.Character?.Name));

            var seen = new HashSet<string>();
            if (!includeCurrent) seen.Add(Key(party));
            int wScreen = config.PolishRotation ? Math.Max(4, config.RotationBeamWidth * 2 / 5) : Math.Max(6, config.RotationBeamWidth * 2 / 3);   // 폭 3은 좋은 순서를 놓친다(영상 장비: 폭3 10.77M / 폭6 12.04M)
            var cands = new List<(List<BattleCharacter> Team, SiegeBattleResult R, List<RotationDecision> Plan)>();
            var orders = new List<List<BattleCharacter>> { party };
            foreach (var fp in Permutations(front))
                foreach (var bp in Permutations(back))
                    orders.Add(fp.Concat(bp).ToList());
            foreach (var team in orders)
            {
                if (!seen.Add(Key(team))) continue;
                var (r, plan) = SearchRotationFor(config, team, best.BestFormation, wScreen,
                    new[] { RemapPlan(best.BestRotationPlan, party, team) });
                if (r != null) cands.Add((team, r, plan));
            }
            for (int i = 0; i < party.Count; i++) party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
            return cands;
        }

        private static List<RotationDecision> RemapPlan(List<RotationDecision> plan, List<BattleCharacter> from, List<BattleCharacter> to) =>
            plan?.Select(d => d.Hold ? new RotationDecision { Hold = true }
                                     : new RotationDecision { HeroIndex = to.IndexOf(from[d.HeroIndex]), Skill = d.Skill }).ToList();

        /// <summary>새 진형 번호 순서(team)와 그 결과를 best에 반영 — BestParty 순서·후열 마스크·반지 목록을 함께 옮긴다.</summary>
        private static void AdoptOrder(SiegeOptimizerResult best, List<BattleCharacter> team, SiegeBattleResult r, List<RotationDecision> plan)
        {
            var party = best.BestParty;
            var backSet = party.Where((b, i) => (best.BestMask & (1 << i)) != 0).ToHashSet();
            if (best.RingedAcc != null && best.RingedAcc.Count == party.Count)
                best.RingedAcc = team.Select(b => best.RingedAcc[party.IndexOf(b)]).ToList();
            int mask = 0;
            for (int i = 0; i < team.Count; i++) if (backSet.Contains(team[i])) mask |= 1 << i;
            best.BestParty = team; best.BestMask = mask;
            for (int i = 0; i < team.Count; i++) team[i].IsBackPosition = (mask & (1 << i)) != 0;
            best.BestScore = r.TotalScore; best.BestRankScore = r.RankScore;
            best.BestResult = r; best.BestRotationPlan = plan;
        }

        private static string OrderNames(List<BattleCharacter> t) => string.Join("·", t.Select(b => b.Character?.Name));

        /// <summary>
        /// 행동 순서(진형 번호) 후처리 — 순서 후보를 좁은 빔으로 훑고 상위 2개를 정식 빔으로 재탐색해
        /// 랭크 점수가 오르면 채택한다(무회귀). 채택 시 BestParty 순서 = 진형 번호 순.
        /// </summary>
        private bool ApplyActionOrder(SiegeOptimizerConfig config, SiegeOptimizerResult best)
        {
            var party = best.BestParty;
            var cands = ScreenOrders(config, best, includeCurrent: false);
            if (cands.Count == 0) return false;

            (List<BattleCharacter> Team, SiegeBattleResult R, List<RotationDecision> Plan) pick = default;
            foreach (var c in cands.OrderByDescending(c => c.R.RankScore).ThenByDescending(c => c.R.TotalScore).Take(2))
            {
                var (r, plan) = SearchRotationFor(config, c.Team, best.BestFormation, config.RotationBeamWidth,
                    new[] { c.Plan, RemapPlan(best.BestRotationPlan, party, c.Team) });
                if (r != null && (pick.R == null || IsBetterPick(r.TotalScore, r.RankScore, pick.R.TotalScore, pick.R.RankScore)))
                    pick = (c.Team, r, plan);
            }
            if (pick.R == null || !IsBetterPick(pick.R.TotalScore, pick.R.RankScore, best.BestScore, best.BestRankScore))
            {
                best.GearLog.Add($"[행동 순서] 유지: {OrderNames(party)} ({best.BestScore:N0}) — 다른 순서 {cands.Count}개 중 최고 {cands.Max(c => c.R.TotalScore):N0}");
                return false;
            }
            best.GearLog.Add($"[행동 순서] 채택: {OrderNames(party)} → {OrderNames(pick.Team)} (진형 번호 순), 팀점수 {best.BestScore:N0} → {pick.R.TotalScore:N0}");
            AdoptOrder(best, pick.Team, pick.R, pick.Plan);
            return true;
        }

        /// <summary>
        /// 속공 선행 후처리 — 자동장착 영웅 한 명씩 "아군 중 속공 1위"가 되도록 속공 부옵(1티어 = 4)을 넣어 보고,
        /// 그 상태에서 나머지 진형 번호 순서까지 함께 훑는다(선행 영웅 × 순서). 전체 후보 중 상위 2개를 정식 빔으로
        /// 재탐색해 랭크 점수가 오르면 채택한다(무회귀, 한 명만). 부옵은 장비 규칙을 지킨다: 부위당 4슬롯·합 9티어·슬롯 최대 6티어.
        /// 속공 1티어를 얻는 수 = (가) 속공 슬롯이 있으면 같은 부위의 다른 슬롯에서 1티어 이동, (나) 없으면 1티어짜리
        /// 슬롯 하나를 속공으로 교체. 수마다 현재 로테 점수 손실이 가장 작은 것을 고른다.
        /// </summary>
        private bool ApplySpeedLead(SiegeOptimizerConfig config, SiegeOptimizerResult best, HashSet<BattleCharacter> autoEquipped)
        {
            const string SPD = "속공";
            const double SpdPerTier = 4;
            const int MaxTier = 6, MaxNeed = 8;
            var party = best.BestParty;
            for (int i = 0; i < party.Count; i++)
                party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

            static List<List<(string Stat, int Tier)>> Snap(BattleCharacter bc) =>
                bc.Equipment.GetEquipments().Select(e => e.SubSlots.Select(x => (x.StatName, x.Tier)).ToList()).ToList();
            static void Put(BattleCharacter bc, List<List<(string Stat, int Tier)>> snap)
            {
                var eq = bc.Equipment.GetEquipments().ToList();
                for (int e = 0; e < eq.Count; e++)
                    for (int x = 0; x < eq[e].SubSlots.Count; x++)
                    { eq[e].SubSlots[x].StatName = snap[e][x].Stat; eq[e].SubSlots[x].Tier = snap[e][x].Tier; }
            }
            SiegeBattleResult SimPlan(List<RotationDecision> plan) => ScoreOnPlan(config, party, best.BestFormation, plan);

            var trials = new List<(BattleCharacter Hero, List<List<(string Stat, int Tier)>> Subs, double From, double To, int Need,
                List<BattleCharacter> Team, SiegeBattleResult R, List<RotationDecision> Plan)>();
            foreach (var bc in party.Where(autoEquipped.Contains).ToList())
            {
                if (bc.Equipment == null) continue;
                var spd0 = AllySpeeds(config, best, party);
                double mine = spd0[bc];
                double maxOther = party.Where(o => o != bc).Select(o => spd0[o]).DefaultIfEmpty(0).Max();
                if (mine > maxOther) continue;                                   // 이미 선행
                int need = (int)Math.Floor((maxOther - mine) / SpdPerTier) + 1;  // 초과에 필요한 속공 티어 수
                if (need > MaxNeed) continue;

                var orig = Snap(bc);
                var equips = bc.Equipment.GetEquipments().ToList();
                bool ok = true;
                for (int t = 0; t < need && ok; t++)
                {
                    Action bestDo = null; double bestRank = double.NegativeInfinity, bestTot = double.NegativeInfinity;
                    void Try(Action apply, Action undo)
                    {
                        apply();
                        var r = SimPlan(best.BestRotationPlan);
                        undo();
                        if (bestDo == null || IsBetterPick(r.TotalScore, r.RankScore, bestTot, bestRank))
                        { bestDo = apply; bestRank = r.RankScore; bestTot = r.TotalScore; }
                    }
                    foreach (var e in equips)
                    {
                        var spdSlot = e.SubSlots.FirstOrDefault(x => x.StatName == SPD);
                        if (spdSlot != null)
                        {
                            if (spdSlot.Tier >= MaxTier) continue;
                            foreach (var from in e.SubSlots.Where(x => x != spdSlot && !string.IsNullOrEmpty(x.StatName) && x.Tier > 1).ToList())
                                Try(() => { from.Tier--; spdSlot.Tier++; }, () => { from.Tier++; spdSlot.Tier--; });
                        }
                        else
                        {
                            foreach (var slot in e.SubSlots.Where(x => !string.IsNullOrEmpty(x.StatName) && x.Tier == 1).ToList())
                            {
                                string old = slot.StatName;
                                Try(() => slot.StatName = SPD, () => slot.StatName = old);
                            }
                        }
                    }
                    if (bestDo == null) ok = false; else bestDo();
                }
                double to = ok ? AllySpeeds(config, best, party)[bc] : mine;
                if (ok && to > maxOther)
                {
                    var subs = Snap(bc);
                    foreach (var c in ScreenOrders(config, best, includeCurrent: true))
                        trials.Add((bc, subs, mine, to, need, c.Team, c.R, c.Plan));
                }
                Put(bc, orig);
            }
            if (trials.Count == 0) return false;

            // 영웅별 최고 후보를 로그로 남기고, 전체 상위 2개만 정식 빔으로 재탐색.
            foreach (var g in trials.GroupBy(t => t.Hero))
            {
                var top = g.OrderByDescending(t => t.R.RankScore).ThenByDescending(t => t.R.TotalScore).First();
                best.GearLog.Add($"[{g.Key.Character.Name}] 속공 선행 후보: 속공 {top.From:0}→{top.To:0} (부옵 {top.Need}티어), 최고 순서 {OrderNames(top.Team)} = {top.R.TotalScore:N0} (현재 {best.BestScore:N0})");
            }
            (BattleCharacter Hero, List<List<(string Stat, int Tier)>> Subs, double From, double To, int Need,
                List<BattleCharacter> Team, SiegeBattleResult R, List<RotationDecision> Plan) pick = default;
            foreach (var t in trials.OrderByDescending(t => t.R.RankScore).ThenByDescending(t => t.R.TotalScore).Take(2))
            {
                var orig = Snap(t.Hero);
                Put(t.Hero, t.Subs);
                var (r, plan) = SearchRotationFor(config, t.Team, best.BestFormation, config.RotationBeamWidth, new[] { t.Plan });
                Put(t.Hero, orig);
                if (r != null && (pick.R == null || IsBetterPick(r.TotalScore, r.RankScore, pick.R.TotalScore, pick.R.RankScore)))
                    pick = (t.Hero, t.Subs, t.From, t.To, t.Need, t.Team, r, plan);
            }
            for (int i = 0; i < party.Count; i++) party[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
            if (pick.R == null || !IsBetterPick(pick.R.TotalScore, pick.R.RankScore, best.BestScore, best.BestRankScore))
            {
                best.GearLog.Add($"[속공 선행] 미채택 — 최고 후보 {(pick.R != null ? pick.R.TotalScore.ToString("N0") : "-")} (현재 {best.BestScore:N0})");
                return false;
            }
            Put(pick.Hero, pick.Subs);
            best.GearLog.Add($"[{pick.Hero.Character.Name}] 속공 선행 채택: 속공 {pick.From:0}→{pick.To:0} (부옵 {pick.Need}티어), 진형 번호 순 {OrderNames(pick.Team)}, 팀점수 {best.BestScore:N0} → {pick.R.TotalScore:N0}");
            best.GearLog.Add(FormatGear(pick.Hero));
            AdoptOrder(best, pick.Team, pick.R, pick.Plan);
            return true;
        }

        private SiegeOptimizerResult OptimizeCore(SiegeOptimizerConfig config)
        {
            // 좌표상승 적용 대상 = 시작 시점 장비 미지정(자동장착) 영웅. 조합탐색 시엔 로테 HeroIndex 정합이 깨져 제외.
            bool canAscend = config.CoordinateAscentGear && config.AutoEquip
                             && (config.Candidates == null || config.Candidates.Count == 0);
            var autoTargets = canAscend
                ? config.FixedMembers.Concat(config.Candidates ?? new List<BattleCharacter>())
                    .Where(bc => bc != null && bc.Equipment == null).ToList()
                : new List<BattleCharacter>();

            var r1 = OptimizeOnce(config);
            if (!canAscend || r1?.BestParty == null || autoTargets.Count == 0) return r1;

            // 1패스 기어 스냅샷(공유객체 보존) + 최종 레짐을 기어평가에 피드백.
            //   전용무기 조율은 BattleCharacter가 아니라 공유 Character 객체에 얹히므로(SearchExclusiveWeapon이
            //   bc.Character.ExclusiveWeapon을 직접 교체) Equipment만 스냅샷하면 2패스 조율이 롤백 후에도 남는다.
            //   → r1 채택 시 점수·로테는 1패스 조율 기준인데 캐릭은 2패스 조율을 낀 상태가 돼 결과가 재현 불가였다.
            var p1Gear = autoTargets.Select(t => t.Equipment).ToList();
            var p1Exclusive = autoTargets.Select(t => t.Character?.ExclusiveWeapon).ToList();
            config.GearEvalFormation = r1.BestFormation;
            config.GearEvalBackRow = r1.BestBackRow != null ? new List<string>(r1.BestBackRow) : null;
            config.GearEvalRotation = (r1.BestRotationPlan != null && r1.BestRotationPlan.Count > 0)
                ? new List<RotationDecision>(r1.BestRotationPlan) : null;

            // 재장착 유도 후 2패스.
            foreach (var t in autoTargets) t.Equipment = null;
            var r2 = OptimizeOnce(config);

            // 무회귀: 2패스가 더 나으면 채택, 아니면 1패스 기어·자리를 공유객체에 복원해 반환.
            bool adopt2 = r2?.BestParty != null && IsBetterPick(r2.BestScore, r2.BestRankScore, r1.BestScore, r1.BestRankScore);
            // 진단 로그: 두 패스 점수와 미채택 패스의 세트 선택 내역을 채택 결과의 GearLog에 남긴다.
            {
                var kept = adopt2 ? r2 : r1; var dropped = adopt2 ? r1 : r2;
                kept.GearLog.Add($"[좌표상승] 1패스 {r1.BestScore:N0}(랭크 {r1.BestRankScore:N0}) / 2패스 "
                    + (r2?.BestParty != null ? $"{r2.BestScore:N0}(랭크 {r2.BestRankScore:N0})" : "실패") + $" → {(adopt2 ? 2 : 1)}패스 채택");
                if (dropped?.GearLog != null)
                    foreach (var l in dropped.GearLog.Where(l => l.Contains("세트 선택")))
                        kept.GearLog.Add($"  (미채택 {(adopt2 ? 1 : 2)}패스) {l}");
            }
            if (adopt2)
                return r2;
            for (int i = 0; i < autoTargets.Count; i++)
            {
                autoTargets[i].Equipment = p1Gear[i];
                if (autoTargets[i].Character != null) autoTargets[i].Character.ExclusiveWeapon = p1Exclusive[i];
            }
            for (int i = 0; i < r1.BestParty.Count; i++)
                r1.BestParty[i].IsBackPosition = (r1.BestMask & (1 << i)) != 0;
            if (r1.RingedAcc != null)
                for (int i = 0; i < r1.RingedAcc.Count && i < r1.BestParty.Count; i++)
                    if (r1.BestParty[i].Equipment != null) r1.BestParty[i].Equipment.Accessory = r1.RingedAcc[i];
            return r1;
        }

        private SiegeOptimizerResult OptimizeOnce(SiegeOptimizerConfig config)
        {
            int remaining = config.PartySize - config.FixedMembers.Count;
            if (remaining < 0) remaining = 0;

            // 장비 자동 장착 (R3 보스 기준, 영웅별 1회) — 팀 탐색 전에 끝내 재사용
            var gearLog = config.AutoEquip ? EquipCandidates(config) : new List<string>();

            SiegeOptimizerResult best = null;
            // (B) 메인딜러 후열 휴리스틱 후보: 자동로테 점수가 가장 높은 "최고딜 캐릭이 후열인" config.
            //   2-stage 근사 보정용 — cleanse→버스트 시너지는 자동로테 점수에 안 잡혀 우승 마스크에서
            //   누락되기 쉬우므로, 빔서치 후보에 항상 함께 포함한다(공성전 한정).
            SiegeOptimizerResult bestDealerBack = null;
            // (B2) 위 (B)는 "1위 딜러"만 본다 — 밸런스(후열2)에서 1위가 이미 후열이면 (B)가 best와 같아져
            //   2위 딜러-후열 config가 빔평가에서 누락된다(목요일 라이언). 보완: 후열이 데미지 상위
            //   requiredBack명과 정확히 일치하는("상위 N딜러 전원 후열") config를 진형별로 추적해 빔 후보에
            //   추가 → 2위 딜러 후열도 공정 비교. Key=진형, Value=그 진형의 최고 자동로테 config.
            var bestDealerBackFull = new Dictionary<string, SiegeOptimizerResult>();
            int evaluated = 0;
            var evalLog = new List<SiegeEvalEntry>();

            foreach (var combo in Combinations(config.Candidates, remaining))
            {
                var team = new List<BattleCharacter>(config.FixedMembers);
                team.AddRange(combo);
                if (team.Count == 0) continue;

                int n = team.Count;

                // [후열 = dummy DPS 상위] 팀별 허수아비 단타 순위 1회 계산 → 진형별 top-requiredBack을 후열로 강제.
                int[] dummyRank = null;
                if (config.DummyBackRow && config.ForcedBackRow == null)
                {
                    var dsim = new SiegeBattleSimulator(GearCompareSeed);
                    var dcfg = new SiegeBattleConfig
                    {
                        AllyParty = team, FormationName = "밸런스 진형", SiegeStage = config.SiegeStage,
                        AllyPet = config.AllyPet, PetStar = config.PetStar, PetEnhance = config.PetEnhance,
                        PetOptionAtkRate = config.PetOptionAtkRate, PetOptionDefRate = config.PetOptionDefRate,
                        PetOptionHpRate = config.PetOptionHpRate, MaxTurns = config.MaxTurns,
                    };
                    // 위치 중립(전원 전열)으로 재서 순위가 "지금 누가 후열인가"에 오염되지 않게 한다.
                    //   team은 공유·변형 객체라 2패스에선 1패스 후열 플래그가 남아 있고, 밸런스 진형에선 후열이
                    //   공%를 받아 dummy DPS가 부풀려진다 → 1패스 후열 멤버가 계속 상위로 나오는 자기강화 편향
                    //   (좌표상승의 "2패스 재탐색" 취지와 충돌). 순위 산정 후 원래 플래그를 복원한다.
                    var posBak = team.Select(t => t.IsBackPosition).ToArray();
                    foreach (var t in team) t.IsBackPosition = false;
                    var dps = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        double bd = -1;
                        foreach (var sk in team[i].Character?.Skills ?? Enumerable.Empty<Skill>())
                        {
                            if (sk.SkillType == SkillType.Normal || sk.SkillType == SkillType.Normal2 || (sk.IsAwakenOnly && !team[i].IsAwakened)) continue;
                            double d = dsim.EvaluateDummyNuke(dcfg, i, sk);
                            if (d > bd) bd = d;
                        }
                        dps[i] = bd;
                    }
                    for (int i = 0; i < n; i++) team[i].IsBackPosition = posBak[i];
                    dummyRank = Enumerable.Range(0, n).OrderByDescending(i => dps[i]).ToArray();
                }
                // 진형 강제 옵션 (실측 비교): null이면 탐색. 기본진형은 비스킷 2명 버프 한계로 제외(밸런스·보호만),
                //   IncludeBasicFormation=true면 기본까지 포함(비교·디버그용).
                var searchFormations = config.IncludeBasicFormation
                    ? Formations
                    : Formations.Where(f => f != "기본 진형").ToArray();
                var formationsToTry = string.IsNullOrEmpty(config.ForcedFormation)
                    ? searchFormations
                    : new[] { config.ForcedFormation };
                foreach (var formation in formationsToTry)
                {
                    // 진형별 후열 인원 고정(기본3·밸런스2·공격4·보호1). 후열공% × 인원 = 42 일정.
                    int requiredBack = GameDamageCalculator.Database.StatTable.FormationDb.Formations.TryGetValue(formation, out var fb)
                        ? System.Math.Min(fb.BackRowCount, n) : n;

                    // 후열 강제 옵션 (실측 비교): 캐릭 이름으로 후열 마스크 고정.
                    int? forcedMask = null;
                    if (config.ForcedBackRow != null)
                    {
                        int fm = 0;
                        for (int i = 0; i < n; i++)
                            if (config.ForcedBackRow.Contains(team[i].Character?.Name)) fm |= (1 << i);
                        forcedMask = fm;
                    }
                    else if (dummyRank != null)
                    {
                        // 후열 = 허수아비 단타 상위 requiredBack명 강제 (cadence 노이즈 무시, 도발 열무관).
                        int fm = 0;
                        for (int k = 0; k < requiredBack && k < dummyRank.Length; k++) fm |= (1 << dummyRank[k]);
                        forcedMask = fm;
                    }

                    // 자리(전/후열) 배치 탐색: 후열 인원이 정확히 requiredBack인 마스크만.
                    //   누구를 후열에 둘지가 핵심 변수 (보호진형 후열 1 → 메인딜러 1명).
                    //   이 (조합×진형)의 전 마스크 결과를 모아 메인딜러-후열(B2) 판정에 재사용한다.
                    var group = new List<(int Mask, SiegeBattleResult Result, List<string> BackRow)>();
                    for (int mask = 0; mask < (1 << n); mask++)
                    {
                        if (forcedMask.HasValue && mask != forcedMask.Value) continue;
                        if (System.Numerics.BitOperations.PopCount((uint)mask) != requiredBack) continue;
                        for (int i = 0; i < n; i++) team[i].IsBackPosition = (mask & (1 << i)) != 0;

                        // BuildSimConfig 경유 — AllyDeathPenalty를 반드시 함께 전달한다. 옛 인라인 config는
                        //   페널티를 빼먹어 이 단계의 RankScore가 TotalScore와 같아졌고(사망 무시), 자리·진형
                        //   선택에서 전멸 빌드 회피가 전혀 작동하지 않았다. 게다가 이 무페널티 RankScore가
                        //   뒤의 ScoreOnPlan(페널티 반영)과 섞여 비교돼 사망 빌드가 부당하게 이길 수 있었다.
                        var simConfig = BuildSimConfig(config, team, formation);
                        // 매 마스크 새 시뮬 인스턴스 — 공유 인스턴스는 시뮬 간 상태(쿨/카운터 등) 누적 오염 위험
                        //   (같은 config가 자리탐색 경로에 따라 다른 자동로테 점수를 내던 문제 방지).
                        var result = new SiegeBattleSimulator(GearCompareSeed).Simulate(simConfig);
                        evaluated++;
                        var backRow = team.Where(c => c.IsBackPosition).Select(c => c.Character.Name).ToList();
                        evalLog.Add(new SiegeEvalEntry
                        {
                            Formation = formation,
                            Party = team.Select(c => c.Character.Name).ToList(),
                            BackRow = backRow,
                            Score = result.TotalScore,
                            RoundScore = new Dictionary<int, double>(result.RoundScore),
                        });
                        group.Add((mask, result, backRow));

                        if (best == null || IsBetterPick(result.TotalScore, result.RankScore, best.BestScore, best.BestRankScore))
                        {
                            best = new SiegeOptimizerResult
                            {
                                BestParty = team,
                                BestFormation = formation,
                                BestScore = result.TotalScore,
                                BestRankScore = result.RankScore,
                                BestResult = result,
                                BestBackRow = backRow,
                                BestMask = mask,
                            };
                        }

                        // (B) 데미지 1위 캐릭이 후열인 config (자동로테 최고점, 전역). 기존 동작 유지.
                        var topDealer = result.CharacterResults.Count > 0
                            ? result.CharacterResults.OrderByDescending(c => c.TotalDamage).First() : null;
                        if (topDealer != null && backRow.Contains(topDealer.CharacterName)
                            && (bestDealerBack == null || IsBetterPick(result.TotalScore, result.RankScore, bestDealerBack.BestScore, bestDealerBack.BestRankScore)))
                            bestDealerBack = new SiegeOptimizerResult
                            {
                                BestParty = team, BestFormation = formation, BestScore = result.TotalScore,
                                BestRankScore = result.RankScore,
                                BestResult = result, BestBackRow = backRow, BestMask = mask,
                            };
                    }

                    // (B2) 이 (조합×진형)의 "상위 N딜러 전원 후열" config를 빔 후보로 추가(진형별 최고점).
                    //   딜러 식별을 config별 자동로테 데미지로 하면 자기모순 — 자동로테는 딜러-후열 시너지를
                    //   과소평가하므로 정작 후열딜러 config에서 그 딜러가 저평가돼 상위에서 빠진다(목요일 라이언).
                    //   대신 이 그룹 내 캐릭별 '최대' 데미지(전열 등 최선 배치에서 찍은 값)로 딜러를 가린 뒤,
                    //   그 상위 N명이 전원 후열인 config를 찾는다. requiredBack==1이면 (B)와 동일 → 제외(중복 빔 방지).
                    if (requiredBack >= 2 && group.Count > 0)
                    {
                        var heroMax = new Dictionary<string, double>();
                        foreach (var g in group)
                            foreach (var cr in g.Result.CharacterResults)
                                heroMax[cr.CharacterName] = System.Math.Max(
                                    heroMax.TryGetValue(cr.CharacterName, out var v) ? v : 0, cr.TotalDamage);
                        // 딜러 식별 = 역할(공격형/마법형) 우선, 그 안에서 데미지. 데미지(heroMax)만 쓰면
                        //   전열 배치에서 후열한정 버프(지크 물공증) 못 받아 딜 낮은 딜러(금요일 라이언)가
                        //   상위에서 빠져 후열 후보에서 누락됨 → 라이언 후열(17.5M)을 못 찾던 버그.
                        bool IsDealerRole(string nm)
                        {
                            var ty = team.FirstOrDefault(x => x.Character?.Name == nm)?.Character?.Type;
                            return ty == "공격형" || ty == "마법형";
                        }
                        // 역할이 딜러여도 그 요일 딜이 미미하면(그룹 1위의 5% 미만) 유틸로 취급해 후열 강제 후보에서 제외.
                        //   (수요일 라이언: 공격형이지만 마법덱에선 쿨감·면역 유틸이라 딜 0.3% → 후열 자리 낭비.)
                        double maxDmgG = heroMax.Values.DefaultIfEmpty(0).Max();
                        bool IsActiveDealer(string nm) => IsDealerRole(nm) && heroMax.GetValueOrDefault(nm) >= maxDmgG * 0.05;
                        var carries = heroMax.OrderByDescending(kv => IsActiveDealer(kv.Key) ? 1 : 0)
                            .ThenByDescending(kv => kv.Value).Take(requiredBack)
                            .Select(kv => kv.Key).ToHashSet();
                        foreach (var (m, r, br) in group)
                            if (br.Count == carries.Count && br.All(carries.Contains)
                                && (!bestDealerBackFull.TryGetValue(formation, out var cf) || IsBetterPick(r.TotalScore, r.RankScore, cf.BestScore, cf.BestRankScore)))
                                bestDealerBackFull[formation] = new SiegeOptimizerResult
                                {
                                    BestParty = team, BestFormation = formation, BestScore = r.TotalScore,
                                    BestRankScore = r.RankScore,
                                    BestResult = r, BestBackRow = br, BestMask = m,
                                };
                    }
                }
            }

            if (best != null)
            {
                // 빔 후보들의 우승 로테 모음 — 생존반지 후처리(ApplySurvivalRings) 後 재평가에 재사용.
                var crossPlans = new List<List<RotationDecision>>();
                // 최종 best config(진형·자리·기어)에 스킬 로테이션 빔서치 → 로테이션 최적 점수·플랜.
                // 진형·자리·기어 탐색은 자동 로테이션 점수로 했으므로 2단계 근사. 단, 자동로테는
                // cleanse→버스트 시너지(메인딜러 후열 가치)를 과소평가 → 메인딜러-후열 후보(B)를
                // 빔서치 후보에 함께 넣고, 빔 점수가 더 높은 쪽을 채택해 근사 오류를 보정한다.
                if (config.OptimizeRotation)
                {
                    // 후보군: 자동로테 우승 + 메인딜러-후열(B) + 상위N딜러-전원후열(B2, 진형별).
                    //   (팀·진형·마스크)가 같은 config는 중복 빔서치 방지로 제외.
                    var beamCands = new List<SiegeOptimizerResult> { best };
                    void AddCand(SiegeOptimizerResult c)
                    {
                        if (c == null) return;
                        bool dup = beamCands.Any(b => ReferenceEquals(b.BestParty, c.BestParty)
                            && b.BestFormation == c.BestFormation && b.BestMask == c.BestMask);
                        if (!dup) beamCands.Add(c);
                    }
                    AddCand(bestDealerBack);
                    foreach (var full in bestDealerBackFull.Values) AddCand(full);

                    // 기준선 재평가: 마스크 탐색은 단일 시드(GearCompareSeed)로 점수를 냈는데, 아래 빔·교차수분
                    //   후보는 ScoreOnPlan(N시드 평균)으로 잰다. 그대로 비교하면 "단일시드 고점 vs 기대값"의
                    //   비대칭이라, 시드 777이 자동로테에 유리한 요일(일요일)에선 기대값이 더 높은 플랜이 기각되고
                    //   보고 점수도 단일시드 고점으로 남는다(평균 정책의 취지 위배). N>1이면 자동로테(plan=null)를
                    //   같은 평균 기준으로 다시 재서 기준선을 맞춘다.
                    int evalN = System.Math.Max(1, System.Math.Max(config.CounterattackSearchSeeds, config.EvalSeeds));
                    if (evalN > 1)
                        foreach (var cand in beamCands)
                        {
                            for (int i = 0; i < cand.BestParty.Count; i++)
                                cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                            var autoAvg = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, null);
                            cand.BestScore = autoAvg.TotalScore;
                            cand.BestRankScore = autoAvg.RankScore;
                            cand.BestResult = autoAvg;
                        }

                    foreach (var cand in beamCands)
                    {
                        // team 객체가 탐색·다른 후보 빔으로 변형되므로 이 후보 자리 배치를 재적용 후 빔.
                        for (int i = 0; i < cand.BestParty.Count; i++)
                            cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                        cand.AutoRotationScore = cand.BestScore;
                        var beam = new RotationBeamSearch(GearCompareSeed).Search(
                            BuildSimConfig(config, cand.BestParty, cand.BestFormation),
                            config.RotationBeamWidth, config.RotationMaxDepth, config.CounterattackSearchSeeds);
                        // 빔은 반격 OFF(0%)로 로테를 골랐다 → 채택·비교 점수는 반격 ON(실전·단일시드)으로 재평가.
                        //   금요일(제이브)만 OFF≠ON; 그 외 보스는 반격 없어 동일(회귀 없음). 비교는 RankScore(생존 페널티).
                        //   raw 트랙(Plan)·생존 트랙(RankPlan) 둘 다 평가해 IsBetterPick(생존 우선)으로 채택.
                        var beamPlans = new List<List<RotationDecision>> { beam.Plan };
                        if (beam.RankPlan != null && !ReferenceEquals(beam.RankPlan, beam.Plan))
                            beamPlans.Add(beam.RankPlan);
                        foreach (var bp in beamPlans)
                        {
                            var beamOn = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, bp);
                            if (IsBetterPick(beamOn.TotalScore, beamOn.RankScore, cand.BestScore, cand.BestRankScore))
                            {
                                cand.BestScore = beamOn.TotalScore;
                                cand.BestRankScore = beamOn.RankScore;
                                cand.BestResult = beamOn;
                                cand.BestRotationPlan = bp;
                            }
                        }
                    }

                    // 교차수분: 각 후보의 빔 플랜을 다른 후보 config에도 평가해, 빔이 그 config에서 못 찾은
                    //   더 높은 로테(다른 자리/진형의 우승 로테)를 채택한다. 로테는 (HeroIndex,Skill) 리스트라
                    //   자리·진형 무관 → 교차적용 가능. (목요일: 전열 우승로테를 라이언-후열 자리에 적용하면
                    //   후열 자체 빔보다 높음 — 빔이 후열을 지역최적으로 저평가한 것을 교정.) 무회귀(더 높을 때만).
                    crossPlans = beamCands
                        .Where(c => c.BestRotationPlan != null && c.BestRotationPlan.Count > 0)
                        .Select(c => c.BestRotationPlan).ToList();
                    // 외부 시드 로테 주입(프로필 간 교차수분 등) — 빔이 못 찾은 더 높은 로테를 교차평가로 회수.
                    if (config.SeedRotations != null)
                        crossPlans.AddRange(config.SeedRotations.Where(p => p != null && p.Count > 0));
                    foreach (var cand in beamCands)
                    {
                        for (int i = 0; i < cand.BestParty.Count; i++)
                            cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                        foreach (var plan in crossPlans)
                        {
                            if (ReferenceEquals(plan, cand.BestRotationPlan)) continue;   // 자기 플랜은 이미 반영됨
                            var r = ScoreOnPlan(config, cand.BestParty, cand.BestFormation, plan);
                            if (IsBetterPick(r.TotalScore, r.RankScore, cand.BestScore, cand.BestRankScore))
                            {
                                cand.BestScore = r.TotalScore;
                                cand.BestRankScore = r.RankScore;
                                cand.BestResult = r;
                                cand.BestRotationPlan = plan;
                            }
                        }
                        Trial(config, cand.BestParty, cand.BestFormation, cand.BestRotationPlan, cand.BestResult);   // 진형·자리 후보별 최고 로테
                    }
                    // 각 빔 후보에 생존반지+로테재최적화를 적용한 뒤 best 선정 — 딜러 후열 빌드가
                    //   "생존반지 전 점수"로 탈락하던 버그 수정. (라이언/타카 후열은 생존반지로 살리면 딜이
                    //   두 배인데, 1차빔은 사망 페널티로 RankScore가 낮아 기본진형에 밀려 후처리를 못 받았음.)
                    //   team 공유 객체라 cand마다 장신구를 백업→적용→평가→원복하고, best의 반지 구성은 RingedAcc에 저장.
                    if (config.AutoEquip && config.EnableSurvivalRings)
                    {
                        foreach (var cand in beamCands)
                        {
                            for (int i = 0; i < cand.BestParty.Count; i++)
                                cand.BestParty[i].IsBackPosition = (cand.BestMask & (1 << i)) != 0;
                            var accBak = cand.BestParty.Select(p => p.Equipment?.Accessory).ToList();
                            if (ApplySurvivalRings(config, cand) && config.OptimizeRotation)
                                ReoptimizeRotationAfterRings(config, cand, crossPlans);
                            cand.RingedAcc = cand.BestParty.Select(p => p.Equipment?.Accessory).ToList();
                            for (int i = 0; i < accBak.Count; i++)
                                if (cand.BestParty[i].Equipment != null) cand.BestParty[i].Equipment.Accessory = accBak[i];
                        }
                    }
                    // 생존(RankScore) 최고 후보 채택, 동률이면 raw(보고 점수) 높은 쪽 — IsBetterPick과 동일 기준.
                    best = beamCands.OrderByDescending(c => c.BestRankScore).ThenByDescending(c => c.BestScore).First();
                }

                best.EvaluatedCount = evaluated;
                best.GearLog = gearLog;
                best.EvalLog = evalLog;
                // team은 공유·변형 객체이므로 최종 채택 config의 자리 배치 + 생존반지 구성을 마지막에 확정 재적용.
                for (int i = 0; i < best.BestParty.Count; i++)
                    best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;
                if (best.RingedAcc != null)
                    for (int i = 0; i < best.RingedAcc.Count && i < best.BestParty.Count; i++)
                        if (best.BestParty[i].Equipment != null) best.BestParty[i].Equipment.Accessory = best.RingedAcc[i];

                // OptimizeRotation=false(빔 미사용)면 빔 후보 경로를 안 타므로 여기서 생존반지 후처리(기존 동작).
                if (config.AutoEquip && config.EnableSurvivalRings && !config.OptimizeRotation)
                    ApplySurvivalRings(config, best);
            }
            return best ?? new SiegeOptimizerResult { EvaluatedCount = 0, GearLog = gearLog, EvalLog = evalLog };
        }

        // 장비 후보(세트) 비교용 풀시뮬 고정 시드 — 후보 간 동일 RNG로 공정 비교.
        private const int GearCompareSeed = 777;

        /// <summary>선택 비교: 생존(RankScore=raw−사망페널티) 우선, 사실상 동률(±eps)일 때만 raw로 tie-break.
        ///   사망 빌드가 최종 채택되려면 사망당 20만 이상의 raw 우위가 필요 → 산발 사망 고점은 허용하되
        ///   "후반 줄사망" 빌드(예: 토요일 4사망 12.10M가 생존 빌드를 밀어내던 문제)는 걸러진다.
        ///   고점 라인 탐색 자체는 빔이 raw 트랙(Plan)과 생존 트랙(RankPlan)을 모두 유지하므로 가려지지 않는다.</summary>
        private static bool IsBetterPick(double total, double rank, double bestTotal, double bestRank)
        {
            const double eps = 1.0;   // 동률 간주 한계(딜)
            if (rank > bestRank + eps) return true;
            if (rank < bestRank - eps) return false;
            return total > bestTotal;   // rank 동률 → raw(보고 점수) 높은 쪽
        }

        /// <summary>저장된 빌드(SiegeBuildStore) 재평가용 — 탐색과 같은 조건(시드 수·사망 페널티·타게팅)으로 한 빌드를 점수 낸다.
        ///   party는 진형 번호 순, IsBackPosition이 맞춰져 있어야 한다.</summary>
        public static SiegeBattleResult EvaluateBuild(SiegeOptimizerConfig config, List<BattleCharacter> party,
            string formation, List<RotationDecision> plan) => ScoreOnPlan(config, party, formation, plan);

        /// <summary>두 빌드 비교(탐색의 채택 기준과 동일: 랭크 점수 우선, 동률이면 실제 점수).</summary>
        public static bool IsBetterBuild(double total, double rank, double bestTotal, double bestRank)
            => IsBetterPick(total, rank, bestTotal, bestRank);

        /// <summary>
        /// 장비 미지정 영웅에게 최적 장비 장착. 영웅별로 허용 세트마다 후보(메인·부옵 프록시 최적)를 만들고,
        /// 그 세트 선택을 공성전 풀시뮬 팀 점수(고정 시드·기본 진형, 좌표상승)로 비교해 정한다.
        /// </summary>
        private List<string> EquipCandidates(SiegeOptimizerConfig config)
        {
            var log = new List<string>();
            var boss = ResolveBoss(config.SiegeStage);
            if (boss == null) return log;

            var optimizer = new EquipmentOptimizer { JointAccessory = config.CoordinateAscentGear };
            var targets = config.FixedMembers.Concat(config.Candidates).Where(bc => bc != null && bc.Equipment == null).ToList();
            var team = config.FixedMembers.Concat(config.Candidates).ToList();   // 풀시뮬 평가 팀

            // 1) 영웅별 세트 후보 생성 (4세트 + 2+2세트, 메인·부옵은 프록시 데미지로 최적). 기준선 = 첫 후보.
            var cands = new Dictionary<BattleCharacter, List<(EquipSetConfig Set, EquipmentLoadout Lo)>>();
            // 딜러별 풀파티 버스트 크리/약점 floor (SoloConfig엔 파티버프가 없으므로 풀팀에서 따로 계산해 주입)
            // FloorFirstGear OFF면 default(0,0) → 기존 동작 그대로(회귀 없음).
            (double Cri, double Wek) Floor(BattleCharacter bc) =>
                config.FloorFirstGear ? optimizer.PartyBuffCritWeak(team, team.IndexOf(bc), bc.Character.Type) : default;

            // 강제 옵션을 GearConstraints에 반영하는 헬퍼 — BuildSetCandidates와 OptimizeForSetFull에서 공통 사용.
            GearConstraints GcForChar(BattleCharacter bc)
            {
                var gc = GetGearConstraints(bc, config.SiegeStage, config.UnconstrainedGear);
                if (config.ForcedSetByCharId != null && bc.Character != null
                    && config.ForcedSetByCharId.TryGetValue(bc.Character.Id, out var fs)
                    && !string.IsNullOrEmpty(fs))
                {
                    gc = new GearConstraints
                    {
                        AllowedSets = new[] { fs },
                        WeaponMains = gc?.WeaponMains, ArmorMains = gc?.ArmorMains, SubOptions = gc?.SubOptions,
                    };
                }
                if (config.ForcedMainByCharId != null && bc.Character != null
                    && config.ForcedMainByCharId.TryGetValue(bc.Character.Id, out var fm))
                {
                    gc = new GearConstraints
                    {
                        AllowedSets = gc?.AllowedSets,
                        WeaponMains = string.IsNullOrEmpty(fm.WeaponMain) ? gc?.WeaponMains : new[] { fm.WeaponMain },
                        ArmorMains = string.IsNullOrEmpty(fm.ArmorMain) ? gc?.ArmorMains : new[] { fm.ArmorMain },
                        SubOptions = gc?.SubOptions,
                    };
                }
                return gc;
            }

            foreach (var bc in targets)
            {
                cands[bc] = optimizer.BuildSetCandidates(bc, SoloConfig(config, boss, bc), 0, GcForChar(bc), Floor(bc));
                if (cands[bc].Count > 0) bc.Equipment = cands[bc][0].Lo;
            }

            // 2) 좌표상승: 영웅별로 각 세트 후보를 풀시뮬 팀 점수(고정 시드·기본 진형)로 비교해 최적 세트 선택
            //   GearEvalRotation 등록 시 그 정렬 로테로 평가 → 버프-캡 스탯이 치피/공%로 자동 재배분(메인옵 포함).
            double FullScore()
            {
                // 기어평가 진형·후열(최종 정합용). 기본은 "기본 진형"·전부 전열(기존 동작).
                if (config.GearEvalBackRow != null)
                    foreach (var t in team)
                        t.IsBackPosition = t.Character != null && config.GearEvalBackRow.Contains(t.Character.Name);
                var sc = BuildSimConfig(config, team, config.GearEvalFormation ?? "기본 진형");
                if (config.GearEvalRotation != null && config.GearEvalRotation.Count > 0)
                    sc.RotationPlan = config.GearEvalRotation;
                return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc).TotalScore;
            }

            // [허수아비 딜러 기어] 딜러 판별 + 대표 넉 + 허수아비 단타 DPS (스쿼드 풀버프 + 보스 HP0=잃은체력 최대).
            bool IsDealerChar(BattleCharacter b) => config.DummyGearForDealers && b.Character != null
                && (b.Character.Type == "공격형" || b.Character.Type == "마법형" || b.Character.Type == "만능형");
            // 넉 = 최대 배율이 아니라 ★최대 실제 허수아비 단타로 선정 — 죽음의무도(배율 낮아도 잃은체력 조건 260%·
            //   타수)처럼 S2가 메인딜인 경우를 올바로 잡는다. 스쿼드 풀버프+보스HP0 기준.
            Skill DealerNuke(BattleCharacter b)
            {
                Skill nuke = null; double best = -1;
                foreach (var sk in b.Character.Skills ?? Enumerable.Empty<Skill>())
                {
                    if (sk.SkillType == SkillType.Normal || sk.SkillType == SkillType.Normal2 || (sk.IsAwakenOnly && !b.IsAwakened)) continue;
                    double d = DummyScore(b, sk);
                    if (d > best) { best = d; nuke = sk; }
                }
                return nuke;
            }
            double DummyScore(BattleCharacter b, Skill nuke)
            {
                if (config.GearEvalBackRow != null)
                    foreach (var t in team)
                        t.IsBackPosition = t.Character != null && config.GearEvalBackRow.Contains(t.Character.Name);
                var dsc = BuildSimConfig(config, team, config.GearEvalFormation ?? "기본 진형");
                return new SiegeBattleSimulator(GearCompareSeed).EvaluateDummyNuke(dsc, team.IndexOf(b), nuke);
            }

            foreach (var bc in targets)
            {
                // [허수아비] 딜러는 세트·메인부옵·장신구·전용을 허수아비 단타 DPS로 전수탐색 (4세트 한정 v1).
                //   풀버프가 치확/약확 캡을 직접 반영 → Floor 프록시 불필요(default). 탱·서포터는 아래 기존 경로.
                if (IsDealerChar(bc))
                {
                    var nuke = DealerNuke(bc);
                    if (nuke != null)
                    {
                        var allowed = GcForChar(bc)?.AllowedSets ?? new[] { "복수자", "암살자", "추적자", "선봉장" };
                        EquipmentLoadout best = null; double bestS = -1; string bestSet = null; var per = new List<string>();
                        foreach (var setCombo in EquipmentOptimizer.BuildSetCombos(allowed))   // 4세트 + 2+2세트
                        {
                            var lo = optimizer.OptimizeForSetFull(bc, SoloConfig(config, boss, bc), 0, setCombo,
                                GcForChar(bc), l => { bc.Equipment = l; return DummyScore(bc, nuke); });
                            if (lo == null) continue;
                            bc.Equipment = lo; double s = DummyScore(bc, nuke);
                            per.Add($"{setCombo.Description}={s:N0}");
                            if (s > bestS) { bestS = s; best = lo; bestSet = setCombo.Description; }
                        }
                        if (best != null) bc.Equipment = best;
                        log.Add($"[{bc.Character.Name}] 세트 선택(허수아비 {nuke.Name} 단타 {bestS:N0}): {bestSet}  ← {string.Join(" / ", per)}");
                        log.Add(FormatGear(bc));
                        continue;
                    }
                }

                var list = cands[bc];
                var chosenSet = list.Count > 0 ? list[0].Set : null;
                if (config.QuickGear) { log.Add(FormatGear(bc)); continue; }   // 스크리닝: 프록시 1순위 그대로
                // [인게임 버프 타게팅 + 정렬 로테 평가(2패스)] 4세트 후보는 메인·부옵·장신구를 풀시뮬로 다시 맞춘 뒤 비교한다.
                //   세트 후보마다 프록시 부옵이 달라 공격력이 몇십 차이 나는데, "공격력 최고 아군" 버프(비스킷 장비 강화)는
                //   그 차이로 수령자가 바뀐다 → 세트 효과가 아니라 공격력 순서 사고로 점수가 갈렸다
                //   (2026-11-19 수요일 쥬리 편성: 나타 복수자 6.37M vs 추적자 8.96M — 실제론 복수자가 +1.7M).
                bool refineEachSet = config.InGameBuffTargeting
                    && config.GearEvalRotation != null && config.GearEvalRotation.Count > 0;
                bool chosenRefined = false;
                if (list.Count > 1)
                {
                    EquipmentLoadout bestLo = bc.Equipment; double bestScore = -1;
                    var perSet = new List<string>();
                    foreach (var (setCombo, lo) in list)
                    {
                        var cand = lo; bool refined = false;
                        if (refineEachSet && (setCombo.Description ?? "").Contains("4세트"))
                        {
                            var r = optimizer.OptimizeForSetFull(bc, SoloConfig(config, boss, bc), 0, setCombo,
                                GcForChar(bc), l => { bc.Equipment = l; return FullScore(); }, Floor(bc));
                            if (r != null) { cand = r; refined = true; }
                        }
                        bc.Equipment = cand;
                        double sc = FullScore();
                        perSet.Add($"{setCombo.Description}={sc:N0}{(refined ? "*" : "")}");
                        if (sc > bestScore) { bestScore = sc; bestLo = cand; chosenSet = setCombo; chosenRefined = refined; }
                    }
                    bc.Equipment = bestLo;
                    log.Add($"[{bc.Character.Name}] 세트 선택(풀시뮬{(refineEachSet ? ", *=부옵 재최적화 후" : "")}): {chosenSet?.Description}  ← {string.Join(" / ", perSet)}");
                }

                // 메인·부옵·장신구도 풀시뮬 점수로 재최적화 (선택된 세트 안에서, 강제 옵션 반영)
                if (chosenSet != null && !chosenRefined)
                {
                    var refined = optimizer.OptimizeForSetFull(bc, SoloConfig(config, boss, bc), 0, chosenSet,
                        GcForChar(bc), lo => { bc.Equipment = lo; return FullScore(); }, Floor(bc));
                    if (refined != null) bc.Equipment = refined;
                }
                log.Add(FormatGear(bc));
            }

            // 3) 전용무기 조율 4슬롯 탐색 (전설 가정) — 기어 확정 후 영웅별 그리디(풀시뮬 점수).
            if (config.SearchExclusiveWeapon)
            {
                foreach (var bc in targets)
                {
                    if (bc.Character == null) continue;
                    bool magic = bc.Character.AttackType == AttackType.Magic;
                    // 딜러(공격/마법/만능형) = 공격조율만. 지원/방어형만 생존조율 후보 포함.
                    bool isDealer = bc.Character.Type is "공격형" or "마법형" or "만능형";
                    // [허수아비] 딜러 전용조율도 허수아비 단타 DPS로 (탱·서포터는 FullScore).
                    var dummyNuke = IsDealerChar(bc) ? DealerNuke(bc) : null;
                    ExclusiveWeapon bestW = bc.Character.ExclusiveWeapon; double bestS = -1;
                    foreach (var w in ExclusiveTuningCandidates(magic, bc.Character.Id, isDealer))
                    {
                        bc.Character.ExclusiveWeapon = w;
                        double s = dummyNuke != null ? DummyScore(bc, dummyNuke) : FullScore();
                        if (s > bestS) { bestS = s; bestW = w; }
                    }
                    bc.Character.ExclusiveWeapon = bestW;
                    log.Add($"[{bc.Character.Name}] 전용조율: {DescribeTuning(bestW)}");
                }
            }
            return log;
        }

        /// <summary>
        /// 생존반지 후처리 — 최종 best config(진형·자리·로테이션 고정)에서 죽는(부활 못 한) 캐릭에
        /// 권능/부활/불사 반지를 같은 조건으로 재시뮬해 비교, 점수 개선 시 채택한다.
        /// 사망캐 게이팅 + 캐스케이드(한 명 살리면 새로 죽는 캐릭) 대응으로 반복(상한 5). 매 회 최선의
        /// (캐릭×반지) 1개만 그리디 채택. 스탯(등급/메인/부옵)은 기존 장신구 그대로, 생존효과만 부여.
        /// </summary>
        /// <returns>생존반지를 1개 이상 채택했으면 true (호출부가 로테 재최적화 트리거).</returns>
        private bool ApplySurvivalRings(SiegeOptimizerConfig config, SiegeOptimizerResult best)
        {
            // ScoreOnPlan 경유 — 페널티 + N시드 평균을 채택 판정과 동일 기준으로 맞춘다. 옛 인라인 config는
            //   AllyDeathPenalty를 빼먹어 RankScore=TotalScore가 됐고, 그 결과 ① "딜을 조금 낮추고 생존을
            //   얻는 반지"가 절대 채택되지 않았으며(생존 우선 철학이 무력화) ② 무페널티 RankScore로
            //   best.BestRankScore를 덮어써 최종 후보 선정이 페널티 유/무가 섞인 비대칭 비교가 됐다.
            SiegeBattleResult SimBest() =>
                ScoreOnPlan(config, best.BestParty, best.BestFormation, best.BestRotationPlan);

            var ringed = new HashSet<BattleCharacter>();
            for (int guard = 0; guard < 5; guard++)
            {
                var cur = SimBest();
                // 반지 후보 = 첫 시드(777) 사망자. (전 시드 사망자로 넓히는 DyingHeroes 게이팅은 2026-11-17 일요일 실험에서
                //   좌표상승 경로가 바뀌어 9.40M→9.26M·사망 0.56→2.25로 악화 — 기본 미적용. 실험 플래그로만.)
                var dyers = config.SurvivalAllSeedGating ? DyingHeroes(cur)
                    : cur.CharacterResults.Where(c => c.Died).Select(c => c.CharacterName).ToHashSet();
                if (dyers.Count == 0) break;

                // 채택 기준 생존(RankScore) 우선(동률 시 raw) — 사망을 막으면 rank +20만이라 raw를 약간
                //   낮추는 반지도 채택될 수 있다(생존 우선 철학). 표시는 실제 딜(TotalScore).
                BattleCharacter pick = null; Accessory pickAcc = null;
                double pickRank = cur.RankScore; double pickTotal = cur.TotalScore;
                foreach (var bc in best.BestParty)
                {
                    if (bc.Character == null || bc.Equipment?.Accessory == null) continue;
                    if (ringed.Contains(bc) || !dyers.Contains(bc.Character.Name)) continue;
                    var orig = bc.Equipment.Accessory;
                    foreach (var ring in SurvivalRingCandidates(orig))
                    {
                        bc.Equipment.Accessory = ring;
                        var rr = SimBest();
                        if (IsBetterPick(rr.TotalScore, rr.RankScore, pickTotal, pickRank)) { pickRank = rr.RankScore; pickTotal = rr.TotalScore; pick = bc; pickAcc = ring; }
                    }
                    bc.Equipment.Accessory = orig;   // 원복 (채택은 1회 1개만)
                }

                if (pick == null) break;             // 개선되는 생존반지 없음 → 종료
                double before = cur.TotalScore;
                pick.Equipment.Accessory = pickAcc;
                ringed.Add(pick);
                best.GearLog.Add($"[{pick.Character.Name}] 생존반지 채택: {pickAcc.RingName} (사망→생존, 팀점수 {before:N0}→{pickTotal:N0})");
            }

            if (ringed.Count > 0)
            {
                var final = SimBest();
                best.BestScore = final.TotalScore;
                best.BestRankScore = final.RankScore;
                best.BestResult = final;
            }
            return ringed.Count > 0;
        }

        /// <summary>
        /// 생존 장비 후처리 — 최종 config(진형·자리·로테 고정)에서 평가 시드 중 한 번이라도 죽는 자동장착 영웅에게
        /// 생존 옵션을 연 장비를 재탐색해, 채택 기준(ScoreOnPlan: N시드 평균·사망 페널티)이 오를 때만 채택한다.
        ///
        /// 왜 후처리인가: 기어 선정(EquipCandidates.FullScore)은 단일 시드·TotalScore라 "다른 시드에서 죽는다"를
        /// 못 보고, GetGearConstraints는 딜러 타입(공·마·만능)에 생존 옵션 후보 자체를 주지 않는다. 그래서 실질
        /// 서포터인 딜러 타입(일요일 미호: 기여 5%)이 유리몸 딜 세팅으로 죽어도 고칠 방법이 없었다.
        /// 여기선 타입을 보지 않고 "죽는가 + 살리면 기대 점수가 오르는가"만 본다(사망 없으면 no-op).
        ///
        /// 탐색: 세트 {현재, 수문장4, 현재무기세트2+수문장2, 현재무기세트2+성기사2} × 무기메인 {현재, 생명력%}
        ///       × 방어구메인 {현재, 생명력%, 방어력%, 받피감%} × 부옵(기존 허용 + 생명력%·방어력%·막기확률%) 그리디.
        ///       장신구는 고정(약확 메인·권능 반지 보존).
        /// </summary>
        private void ApplySurvivalGear(SiegeOptimizerConfig config, SiegeOptimizerResult best, HashSet<BattleCharacter> autoEquipped)
        {
            var boss = ResolveBoss(config.SiegeStage);
            if (boss == null || autoEquipped.Count == 0) return;
            for (int i = 0; i < best.BestParty.Count; i++)
                best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

            SiegeBattleResult SimBest() =>
                ScoreOnPlan(config, best.BestParty, best.BestFormation, best.BestRotationPlan);

            var optimizer = new EquipmentOptimizer();
            var tried = new HashSet<BattleCharacter>();
            bool adopted = false;
            for (int guard = 0; guard < 4; guard++)
            {
                var cur = SimBest();
                // 가장 자주 죽는 영웅부터 (캐스케이드: 한 명 살리면 다른 영웅이 대신 죽을 수 있어 반복).
                var bc = best.BestParty
                    .Where(p => p.Character != null && p.Equipment != null && autoEquipped.Contains(p) && !tried.Contains(p))
                    .Select(p => (P: p, N: DeathSeeds(cur, p.Character.Name)))
                    .Where(x => x.N > 0).OrderByDescending(x => x.N).Select(x => x.P).FirstOrDefault();
                if (bc == null) break;
                tried.Add(bc);

                var orig = bc.Equipment;
                var eqs = orig.GetEquipments().ToList();
                string wSet = eqs.FirstOrDefault(e => e.Slot == "무기")?.SetName;
                string aSet = eqs.FirstOrDefault(e => e.Slot == "방어구")?.SetName;
                string wMain = eqs.FirstOrDefault(e => e.Slot == "무기")?.MainStatName ?? "공격력%";
                string aMain = eqs.FirstOrDefault(e => e.Slot == "방어구")?.MainStatName ?? "공격력%";
                if (wSet == null || aSet == null) continue;

                var sets = new List<EquipSetConfig>
                {
                    new() { WeaponSetName = wSet, ArmorSetName = aSet, Is4Set = wSet == aSet, Description = wSet == aSet ? $"{wSet} 4세트" : $"{wSet} 2 + {aSet} 2" },
                };
                void AddSet(string w, string a)
                {
                    if (sets.Any(x => x.WeaponSetName == w && x.ArmorSetName == a)) return;
                    sets.Add(new EquipSetConfig { WeaponSetName = w, ArmorSetName = a, Is4Set = w == a, Description = w == a ? $"{w} 4세트" : $"{w} 2 + {a} 2" });
                }
                AddSet("수문장", "수문장");
                AddSet(wSet, "수문장");
                AddSet(wSet, "성기사");

                var baseGc = GetGearConstraints(bc, config.SiegeStage, config.UnconstrainedGear);
                var gc = new GearConstraints
                {
                    WeaponMains = new[] { wMain, "생명력%" }.Distinct().ToArray(),
                    ArmorMains = new[] { aMain, "생명력%", "방어력%", "받피감%" }.Distinct().ToArray(),
                    SubOptions = (baseGc?.SubOptions ?? new string[0]).Concat(new[] { "생명력%", "방어력%", "막기확률%" }).Distinct().ToArray(),
                };

                // 스코어러 = 채택 기준과 같은 N시드 평균 RankScore (사망의 딜 손실·페널티가 그대로 반영).
                int evals = 0;
                double Score(EquipmentLoadout lo)
                {
                    evals++;
                    bc.Equipment = lo;
                    return SimBest().RankScore;
                }

                EquipmentLoadout pick = null; string pickSet = null;
                double pickRank = cur.RankScore, pickTotal = cur.TotalScore;
                var solo = SoloConfig(config, boss, bc);
                foreach (var set in sets)
                {
                    var lo = optimizer.OptimizeMainsSubsFixedAccessory(bc, solo, 0, set, gc, Score, orig.Accessory);
                    if (lo == null) continue;
                    bc.Equipment = lo;
                    var r = SimBest();
                    if (IsBetterPick(r.TotalScore, r.RankScore, pickTotal, pickRank))
                    { pickRank = r.RankScore; pickTotal = r.TotalScore; pick = lo; pickSet = set.Description; }
                }
                bc.Equipment = pick ?? orig;

                int beforeN = DeathSeeds(cur, bc.Character.Name);
                if (pick == null)
                {
                    best.GearLog.Add($"[{bc.Character.Name}] 생존장비 후처리: 채택 없음 — {cur.EvalSeedCount}시드 중 {beforeN}시드 사망이지만 생존 옵션으로 기대 점수가 오르지 않음 (평가 {evals:N0}회)");
                    continue;
                }
                var after = SimBest();
                adopted = true;
                best.GearLog.Add($"[{bc.Character.Name}] 생존장비 채택: {pickSet} (사망 {beforeN}→{DeathSeeds(after, bc.Character.Name)}/{after.EvalSeedCount}시드, 팀점수 {cur.TotalScore:N0}→{after.TotalScore:N0}, 평가 {evals:N0}회) — 아래가 최종 장비");
                best.GearLog.Add(FormatGear(bc));
            }

            if (!adopted) return;
            var final = SimBest();
            best.BestScore = final.TotalScore;
            best.BestRankScore = final.RankScore;
            best.BestResult = final;
            // 생존으로 열린 로테 재탐색(무회귀) — 자기 플랜도 후보라 퇴보 없음.
            if (config.OptimizeRotation)
                ReoptimizeRotationAfterRings(config, best, new List<List<RotationDecision>>());
        }

        /// <summary>
        /// 생존반지 채택 後 로테이션 재최적화 — 반지가 살린 캐릭의 버프 유지로 더 높은 로테가 가능해진다.
        /// 반지-장착 best config에서 (1) 빔 재탐색 + (2) 기존 crossPlans 재평가 → 가장 높은 로테로 갱신(무회귀).
        /// 자기 플랜(현 BestRotationPlan)도 후보에 포함해 절대 퇴보하지 않게 한다.
        /// </summary>
        private void ReoptimizeRotationAfterRings(SiegeOptimizerConfig config, SiegeOptimizerResult best,
            List<List<RotationDecision>> crossPlans)
        {
            // 자리 재적용 (team은 공유·변형 객체).
            for (int i = 0; i < best.BestParty.Count; i++)
                best.BestParty[i].IsBackPosition = (best.BestMask & (1 << i)) != 0;

            // (1) 반지-장착 config에서 빔 재탐색 — 생존이 열어준 새 로테 공간 탐색.
            var beam = new RotationBeamSearch(GearCompareSeed).Search(
                BuildSimConfig(config, best.BestParty, best.BestFormation),
                config.RotationBeamWidth, config.RotationMaxDepth, config.CounterattackSearchSeeds);

            // (2) 후보 플랜 = 새 빔 + 기존 crossPlans + 현 채택 플랜. 반지-장착 config로 동일 시드 재평가.
            var candPlans = new List<List<RotationDecision>>();
            if (beam.Plan != null && beam.Plan.Count > 0) candPlans.Add(beam.Plan);
            if (beam.RankPlan != null && beam.RankPlan.Count > 0) candPlans.Add(beam.RankPlan);
            candPlans.AddRange(crossPlans);
            if (best.BestRotationPlan != null && best.BestRotationPlan.Count > 0) candPlans.Add(best.BestRotationPlan);

            double before = best.BestScore;
            foreach (var plan in candPlans)
            {
                var r = ScoreOnPlan(config, best.BestParty, best.BestFormation, plan);
                if (IsBetterPick(r.TotalScore, r.RankScore, best.BestScore, best.BestRankScore))
                {
                    best.BestScore = r.TotalScore;
                    best.BestRankScore = r.RankScore;
                    best.BestResult = r;
                    best.BestRotationPlan = plan;
                }
            }
            if (best.BestScore > before)
                best.GearLog.Add($"[로테 재최적화] 생존반지 後 천장 회수: {before:N0} → {best.BestScore:N0}");
        }

        /// <summary>생존반지 후보 — 반지는 메인옵 자리를 권능 효과가 대신하고 옵션은 부옵 1줄뿐(연습전투 실측).
        /// 기존 장신구의 메인/부옵 중 하나를 부옵으로 남기는 두 후보(같으면 1개). 등급 보너스는 유지.
        /// 공성전은 **권능의 반지만** 탐색한다: 권능은 사망하지 않고(생명력1로) 생존 → 버프 유지.
        /// 부활/불사는 사망 후 부활이라 보유 버프가 해제돼 저딜 서포터 생존 가치가 떨어짐(제외).</summary>
        private static IEnumerable<Accessory> SurvivalRingCandidates(Accessory baseAcc)
        {
            var subs = new[] { baseAcc.MainOption, baseAcc.SubOption }
                .Where(o => !string.IsNullOrEmpty(o) && o != "없음").Distinct().ToList();
            if (subs.Count == 0) subs.Add(null);
            foreach (var sub in subs)
                yield return new Accessory { Grade = baseAcc.Grade, SubOption = sub, RingName = "권능의 반지" };
        }

        /// <summary>전용무기 조율 후보(전설 4슬롯) — 딜러/탱커 공통 소수 조합만(8^4 전수 대신).
        /// 전용장비는 실게임에서 영웅당 1개뿐 → 요일별로 못 바꾼다. 따라서 딜러는 방어조율(탄성·생명력·방어력)을
        /// 후보에서 제외하고 공격조율(모든공격력·피해증폭)만 둔다. 탄성은 적 치확100 구간(토요일 챈슬러 버프)에서만
        /// 작동하는데 그건 비스킷 버프해제로 처리할 기믹이라, 딜러가 탄성을 끼면 상시 딜 손실이 된다(사용자 지적).
        /// 지원/방어형만 생존조율 후보를 갖는다.</summary>
        private static List<ExclusiveWeapon> ExclusiveTuningCandidates(bool magic, int charId, bool isDealer)
        {
            ExclusiveWeapon W(params TuningOption[] opts) => new()
            {
                Name = "조율탐색", OwnerCharacterId = charId, Atk = 247, IsMagic = magic,
                Tuning = opts.Select(o => new TuningSlot { Option = o, Grade = ExclusiveWeaponGrade.전설 }).ToList(),
            };
            var A = TuningOption.모든공격력; var D = TuningOption.피해증폭;
            var T = TuningOption.탄성; var H = TuningOption.생명력; var Df = TuningOption.방어력;
            var cands = new List<ExclusiveWeapon>
            {
                W(A, A, A, A),   // 모든공격력 몰빵
                W(A, A, A, D),
                W(A, A, D, D),
                W(A, D, D, D),
                W(D, D, D, D),   // 피해증폭 몰빵
            };
            if (isDealer) return cands;   // 딜러: 공격조율만 (방어조율은 전용장비 1개 제약상 상시 딜손실)
            // 지원/방어형: 생존조율도 후보
            cands.Add(W(A, A, T, T));   // 공격 + 생존(탄성)
            cands.Add(W(T, T, T, T));   // 생존
            cands.Add(W(H, H, Df, Df)); // 탱커
            return cands;
        }

        /// <summary>조율 구성 요약 문자열.</summary>
        private static string DescribeTuning(ExclusiveWeapon w)
        {
            if (w == null || w.Tuning == null || w.Tuning.Count == 0) return "(없음)";
            return string.Join("+", w.Tuning.GroupBy(t => t.Option)
                .Select(g => $"{g.Key}×{g.Count()}"));
        }

        /// <summary>장비 프록시 평가용 단일 영웅 BattleConfig (R3 보스 타깃).</summary>
        private static BattleConfig SoloConfig(SiegeOptimizerConfig config, Enemy boss, BattleCharacter bc) => new()
        {
            AllyParty = new List<BattleCharacter> { bc },
            TargetEnemy = boss,
            FormationName = "기본 진형",
            AllyPet = config.AllyPet,
            PetStar = config.PetStar,
            PetEnhance = config.PetEnhance,
            PetOptionAtkRate = config.PetOptionAtkRate,
            PetOptionDefRate = config.PetOptionDefRate,
            PetOptionHpRate = config.PetOptionHpRate,
        };

        /// <summary>빔이 고른 로테를 반격 ON(실전·기본 25%)으로 재평가 — 채택 점수 일관성.
        ///   Max(CounterattackSearchSeeds, EvalSeeds)>1이면 N시드 평균(TotalScore·RankScore)으로 — 반격(금요일)·
        ///   아군타겟(일요일 등) RNG 과적합 방지. 시드 GearCompareSeed+s가 _rng 전체(반격+타겟)를 함께 흔든다.
        ///   반환 result는 첫 시드 것(라운드/기여/로테 표시용)에 헤드라인 점수만 평균으로 덮어씀.</summary>
        private static SiegeBattleResult ScoreOnPlan(SiegeOptimizerConfig config, List<BattleCharacter> team,
            string formation, List<RotationDecision> plan)
        {
            var sc = BuildSimConfig(config, team, formation);   // override null = 반격 ON
            sc.RotationPlan = plan;
            int n = System.Math.Max(1, System.Math.Max(config.CounterattackSearchSeeds, config.EvalSeeds));
            if (n <= 1) return new SiegeBattleSimulator(GearCompareSeed).Simulate(sc);
            double sumT = 0, sumR = 0;
            SiegeBattleResult first = null;
            var deathSeeds = new Dictionary<string, int>();
            for (int s = 0; s < n; s++)
            {
                var r = new SiegeBattleSimulator(GearCompareSeed + s).Simulate(sc);
                sumT += r.TotalScore; sumR += r.RankScore;
                foreach (var c in r.CharacterResults)
                    if (c.Died) deathSeeds[c.CharacterName] = deathSeeds.GetValueOrDefault(c.CharacterName) + 1;
                first ??= r;
            }
            first.TotalScore = sumT / n;
            first.RankScore = sumR / n;
            first.DeathSeedCounts = deathSeeds;
            first.EvalSeedCount = n;
            return first;
        }

        /// <summary>평가 시드 중 한 번이라도 죽는 영웅 이름 (N시드 평가면 DeathSeedCounts, 단일 시드면 Died).</summary>
        private static HashSet<string> DyingHeroes(SiegeBattleResult r) =>
            r.DeathSeedCounts != null
                ? r.DeathSeedCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToHashSet()
                : r.CharacterResults.Where(c => c.Died).Select(c => c.CharacterName).ToHashSet();

        private static int DeathSeeds(SiegeBattleResult r, string name) =>
            r.DeathSeedCounts != null ? r.DeathSeedCounts.GetValueOrDefault(name)
                : (r.CharacterResults.Any(c => c.CharacterName == name && c.Died) ? 1 : 0);

        /// <summary>공성전 풀시뮬 SiegeBattleConfig 구성.</summary>
        private static SiegeBattleConfig BuildSimConfig(SiegeOptimizerConfig config, List<BattleCharacter> team, string formation) => new()
        {
            AllyParty = team,
            FormationName = formation,
            SiegeStage = config.SiegeStage,
            AllyPet = config.AllyPet,
            PetStar = config.PetStar,
            PetEnhance = config.PetEnhance,
            PetOptionAtkRate = config.PetOptionAtkRate,
            PetOptionDefRate = config.PetOptionDefRate,
            PetOptionHpRate = config.PetOptionHpRate,
            MaxTurns = config.MaxTurns,
            AllyDeathPenalty = config.AllyDeathPenalty,   // 생존 우선(전멸 빌드 회피) — 보고 점수 불변, RankScore에만 반영
            HighestAtkAllyOrder = config.HighestAtkAllyOrder,
            HighestAtkAllyOverrideCdrOnly = config.HighestAtkAllyOverrideCdrOnly,
            InGameBuffTargeting = config.InGameBuffTargeting,
            HighestAtkBuffGate = config.HighestAtkBuffGate,
            DiagAlliesImmortal = config.DiagAlliesImmortal,
            AllyCooldownMargin = config.AllyCooldownMargin,
        };

        /// <summary>장착된 장비의 세트·메인옵 값·부옵 값을 사람이 읽기 쉬운 문자열로.</summary>
        private static string FormatGear(BattleCharacter bc)
        {
            var lo = bc.Equipment;
            if (lo == null) return $"{bc.Character.Name}: 장비 없음";

            var sets = lo.GetActiveSets()?.Select(s => $"{s.SetName}{s.PieceCount}");
            var sb = new System.Text.StringBuilder();
            sb.Append($"[{bc.Character.Name}] 세트 {(sets != null ? string.Join("+", sets) : "")}");
            foreach (var e in lo.GetEquipments())
            {
                var subs = e.SubSlots
                    .Where(s => !string.IsNullOrEmpty(s.StatName))
                    .Select(s => $"{s.StatName} {s.DisplayValue}");
                sb.Append($"\n  {e.Name}: 메인 {e.MainStatName} {e.MainStatValue} | 부옵 {string.Join(", ", subs)}");
            }
            // 장신구
            if (lo.Accessory != null)
                sb.Append($"\n  장신구: {(string.IsNullOrEmpty(lo.Accessory.RingName) ? "" : $"《{lo.Accessory.RingName}》 ")}{lo.Accessory.Grade}성 메인 {lo.Accessory.MainOption}{(string.IsNullOrEmpty(lo.Accessory.SubOption) ? "" : $" / 부 {lo.Accessory.SubOption}")}");
            return sb.ToString();
        }

        // 딜러 = 공격형·마법형·만능형 / 딜러제외(서포터·탱커) = 지원형·방어형
        private static bool IsDealer(BattleCharacter bc)
        {
            var t = bc.Character.Type;
            return t == "공격형" || t == "마법형" || t == "만능형";
        }

        /// <summary>
        /// 역할 기반 장비 탐색 제약. 3-tier 분류:
        ///  · 효과적 딜러: DPS 기어
        ///  · 면역/유틸리티(라이언 등 — 면역 패시브 보유 + 효과적 비딜러): 방어 전용 기어 (생존 우선, 죽으면 utility 손실)
        ///  · 서포터/버퍼(비스킷·리나 등): 혼합 (데미지 + 방어)
        /// </summary>
        private static GearConstraints GetGearConstraints(BattleCharacter bc, Stage stage = null, bool unconstrained = false)
        {
            // [실험] 딜러 제약 해제: 전 세트 + 오펜/디펜 광범위 옵션 개방 (옵티마이저 자율 탐색 확인용).
            if (unconstrained && IsEffectiveDealer(bc, stage))
                return new GearConstraints
                {
                    AllowedSets = new[] { "복수자", "암살자", "추적자", "선봉장", "성기사", "수문장" },
                    WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%", "생명력%", "방어력%" },
                    ArmorMains = new[] { "공격력%", "생명력%", "방어력%", "받피감%" },
                    SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력", "생명력%", "방어력%", "막기확률%" },
                };
            if (IsEffectiveDealer(bc, stage))
                return new GearConstraints
                {
                    AllowedSets = new[] { "복수자", "암살자", "추적자", "선봉장" },
                    WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                    ArmorMains = new[] { "공격력%" },
                    SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력" },
                };
            // 면역 패시브 보유자(라이언 화상면역 등) — 죽으면 utility(쿨감·면역) 손실이 크므로 방어 전용.
            if (HasImmunityPassive(bc))
                return new GearConstraints
                {
                    AllowedSets = new[] { "수문장", "복수자" },
                    WeaponMains = new[] { "생명력%", "방어력%" },
                    ArmorMains = new[] { "생명력%", "방어력%", "받피감%" },
                    SubOptions = new[] { "생명력%", "방어력%", "생명력", "방어력", "막기확률%" },
                };
            return new GearConstraints
            {
                AllowedSets = new[] { "복수자", "수문장" },
                WeaponMains = new[] { "치명타확률%", "치명타피해%", "공격력%", "약점공격확률%" },
                ArmorMains = new[] { "공격력%", "받피감%" },
                SubOptions = new[] { "치명타확률%", "치명타피해%", "약점공격확률%", "공격력%", "공격력", "생명력%", "방어력%" },
            };
        }

        /// <summary>면역 패시브(StatusImmunity) 보유 여부 — 라이언(화상)·풍연(빙결) 등. 죽으면 효과 손실.</summary>
        private static bool HasImmunityPassive(BattleCharacter bc)
        {
            var passive = bc.Character.Passive;
            if (passive == null) return false;
            bool Has(System.Collections.Generic.IEnumerable<Models.Effects.PersistentEffect> effs) =>
                effs != null && effs.Any(e => e.Type == Models.Effects.PersistentEffectType.Immunity);
            return Has(passive.GetLevelData(bc.IsSkillEnhanced, bc.IsAwakened)?.Effects)
                || Has(passive.GetTranscendBonus(bc.TranscendLevel)?.Effects);
        }

        /// <summary>
        /// 효과적 딜러 판정: 타입이 딜러여도 요일 reduction으로 데미지가 90% 이상 차단되면 유틸 취급.
        /// 예) 라이언(공격형)이 수요일(Phys90) 채용 시 → false → 생존 세팅 탐색.
        /// </summary>
        private static bool IsEffectiveDealer(BattleCharacter bc, Stage stage)
        {
            if (!IsDealer(bc)) return false;
            if (stage == null) return true;
            // 무대의 아무 적이나 골라 reduction 프로필 확인 (요일 reduction은 동일)
            var anyEnemyId = stage.Waves?.FirstOrDefault()?.Enemies?.FirstOrDefault()?.EnemyId ?? 0;
            var enemy = EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == anyEnemyId);
            if (enemy == null) return true;
            double red = bc.Character.AttackType == AttackType.Physical ? enemy.PhysicalReduction : enemy.MagicReduction;
            return red < 90;   // 90% 이상이면 효과적 비딜러
        }

        /// <summary>
        /// 스테이지 마지막 라운드(R3)의 보스 적을 EnemyDb에서 조회 (장비 평가 타깃).
        /// Enemy.IsBoss=true인 적(예: 스파이크)을 우선 — 보스피증(복수자 등)이 평가에 반영되도록.
        /// (룩/챈슬러 친위대는 R3에서 보스 취급이나 Enemy.IsBoss=false라 그대로 쓰면 보스피증이 빠짐.)
        /// </summary>
        private static Enemy ResolveBoss(Stage stage)
        {
            var wave = stage?.Waves?.OrderByDescending(w => w.WaveNumber).FirstOrDefault();
            if (wave?.Enemies == null || wave.Enemies.Count == 0) return null;

            Enemy Lookup(StageEnemy se) => se == null ? null : EnemyDb.AllEnemies.FirstOrDefault(e => e.Id == se.EnemyId);

            // 1순위: StageEnemy.IsBoss이면서 Enemy.IsBoss=true (실제 보스)
            foreach (var se in wave.Enemies.Where(e => e.IsBoss))
            {
                var en = Lookup(se);
                if (en != null && en.IsBoss) return en;
            }
            // 2순위: 보스 취급 적, 3순위: 아무 적
            return Lookup(wave.Enemies.FirstOrDefault(e => e.IsBoss) ?? wave.Enemies.FirstOrDefault());
        }

        /// <summary>pool에서 k개 조합 (C(N,k)). k=0이면 빈 조합 하나.</summary>
        private static IEnumerable<List<BattleCharacter>> Combinations(List<BattleCharacter> pool, int k)
        {
            if (k <= 0) { yield return new List<BattleCharacter>(); yield break; }
            if (pool == null || pool.Count < k) yield break;

            int n = pool.Count;
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = i;

            while (true)
            {
                yield return idx.Select(i => pool[i]).ToList();

                int p = k - 1;
                while (p >= 0 && idx[p] == n - k + p) p--;
                if (p < 0) yield break;
                idx[p]++;
                for (int i = p + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
            }
        }
    }
}

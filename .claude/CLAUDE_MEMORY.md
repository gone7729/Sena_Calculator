# 프로젝트 컨텍스트 (최종 갱신: 2026-02-06)

> 대화 기록: `.claude/log/YYYY-MM-DD.md`

## 프로젝트 개요
- **이름**: 세나리 데미지 계산기 (세븐나이츠 리버스)
- **기술**: .NET 8.0, WPF, C# 12
- **목적**: 게임 내 데미지와 일치하는 정확한 계산기

## 현재 상태
- 핵심 데미지 계산 로직 구현 완료
- 빠른 비교 기능 (4가지 시나리오 자동 계산)
- 테스트 오차율: 에스파다 0.003%, 타카 1.6%, 루리 0.9%
- 배틀 시뮬레이터 & 장비 옵티마이저 구현 완료 (2026-03-29)

## 핵심 공식
```
BaseDamage = (공/방계수) × 스킬배율 × 치명계수 × 약점계수 × 피증계수
방어계수 = 1 + (보스방어 × (1+방증-방깎) × (1-방무)) / 467
피증계수 = 1 + (기본+타입+조건부+보스+인기-피감) / 100  [합연산]
최종피해 = (스킬피해 + HP비례 + 스택소모) × (1 + 취약/100)
버프 적용 = 1 + (상시 + 턴제 + 펫) / 100  [합연산]
```

## 스택소모 스킬 주의사항
- 스킬피해: SkillDmgMultiplier (자버프 타입피증 제외)
- 스택소모피해: DamageMultiplier (자버프 타입피증 포함)
- GetSelfBuffTypeDmg: Dmg_Dealt_Type + Mark_Energeia + Mark_Purify 모두 포함

## 주요 파일
| 파일 | 설명 |
|------|------|
| `Services/DamageCalculator.cs` | 데미지 계산 핵심 |
| `Services/StatCalculator.cs` | 스탯 계산 (합연산 방식) |
| `Services/BuffCalculator.cs` | 버프/디버프 합산 |
| `Services/BattleEngine/BattleSimulator.cs` | 배틀 시뮬 메인 루프 |
| `Services/BattleEngine/TurnManager.cs` | 턴 순서/행동 큐 |
| `Services/Optimizer/EquipmentOptimizer.cs` | 장비 최적화 |
| `DB/CharacterDB.cs` | 캐릭터/스킬 데이터 |
| `UI/MainWindow.xaml.cs` | UI 이벤트, 계산 호출 |
| `UI/SimulatorWindow.xaml.cs` | 시뮬레이터 UI |

## 최근 작업 (2026-03-29)
- 배틀 시뮬레이터 구현: 5인 파티 턴제 배틀 (TurnManager, BattleSimulator)
- 장비 옵티마이저 구현: 세트/메인옵/서브옵 그리디 탐색 (EquipmentOptimizer)
- SimulatorWindow UI: 파티 구성, 보스 선택, 시뮬/최적화 실행
- EquipmentLoadout 모델: 장비 한벌 관리 (무기2+방어구2+장신구1)
- Boss → Enemy 리네이밍 반영 (유저 수정)

### 이전 작업 (2026-02-06)
- 여포 패시브 시스템: FoolhardyBravery, MarkAttack, WekBonusDmgPerHit
- 버프 합연산 방식으로 수정 (곱연산→합연산)
- 에스파다 Mark_Purify 자버프 타입피증 인식 수정 (오차 0.003%)
- 라이언 광풍참 잃은HP 비례 피해 구현 (LostHpAssumedRemaining 모델)

## 잃은HP 비례 피해 주의사항
- SkillLevelData.LostHpAssumedRemaining: 특정조건 체크 시 가정할 대상 잔여HP%
- 값이 0이면 LostHpBonusDmgMax 그대로 적용 (최대 보너스)
- SkillDmgMultiplier와 DamageMultiplier 모두에 적용 필수
- 라이언 광풍참: 보스 HP 최저치 고정 기믹 → 잃은HP 100% → 50% 최대 보너스 (인게임 일치)

## 버프 중복 주의
- 캐릭터 선택 시 패시브 자버프가 자동 적용됨
- UI 버프 리스트에서 동일 캐릭터 버프를 체크하면 중복 적용 발생
- 딜러로 선택한 캐릭터의 버프는 리스트에서 체크하지 말 것

## 진행중/예정 작업
- [ ] 배틀 시뮬레이터 자동 최적화 모드 (스킬 순서 자동 탐색)
- [ ] 쿨다운 세부사항 반영 (유저 제공 대기)
- [ ] 옵티마이저 성능 최적화 (EvaluateDamage 캐싱)
- [ ] 추가 캐릭터 데이터 입력
- [ ] 다른 캐릭터 인게임 검증
- [x] 라이언 광풍참 인게임 검증 완료 (50% 보너스, 오차 ~0%)
- [x] 배틀 시뮬레이터 & 장비 옵티마이저 기본 구현 완료

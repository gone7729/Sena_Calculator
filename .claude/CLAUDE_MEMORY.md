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
| `DB/CharacterDB.cs` | 캐릭터/스킬 데이터 |
| `UI/MainWindow.xaml.cs` | UI 이벤트, 계산 호출 |

## 최근 작업 (2026-02-06)
- 여포 패시브 시스템: FoolhardyBravery, MarkAttack, WekBonusDmgPerHit
- 버프 합연산 방식으로 수정 (곱연산→합연산)
- 에스파다 Mark_Purify 자버프 타입피증 인식 수정 (오차 0.003%)
- ExtraDmgMultiplier에 n인기피증 적용
- UI 버프 리스트에 소교/빅토리아/여포 추가
- 셋팅 비교 UI 간소화 (차이값 제거, 결과만 표시)
- 프리셋 슬롯별 분리 (1번/2번 독립 프리셋, presets_1/2.json)
- 라이언 광풍참 잃은HP 비례 피해 구현 (LostHpAssumedRemaining 모델)
- SkillDmgMultiplier 버그 수정 (잃은HP 보너스 누락)
- 체력조건/특정조건 체크박스 분리 (체력조건=강포방증, 특정조건=스킬조건)

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
- [ ] 추가 캐릭터 데이터 입력
- [ ] 다른 캐릭터 인게임 검증 (타카 재검증 등)
- [x] 라이언 광풍참 인게임 검증 완료 (50% 보너스, 오차 ~0%)

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

## 진행중/예정 작업
- [ ] 추가 캐릭터 데이터 입력
- [ ] 다른 캐릭터 인게임 검증 (타카 재검증 등)

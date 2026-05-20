# Phase 5 — Magic 캐릭터 버프 마이그레이션 (`Atk_Rate` → `MagicAtk_Rate`)

## 목적

Phase 1~4까지 공격력 타입 분리(`Atk` / `MagicAtk`) 인프라를 마쳤다. `StatCalculator`는 캐릭터 `AttackType`에 따라 `BuffSet.Atk_Rate` 또는 `BuffSet.MagicAtk_Rate`를 선택해 사용한다.

이제 **Magic 캐릭터의 패시브·스킬 데이터에 있는 `Atk_Rate` 값을 `MagicAtk_Rate`로 옮긴다**. 게임 메카닉상 마법형 캐릭터의 "공격력 증가" 버프는 실제로 "마법공격력 증가"이기 때문이다.

## 영향 범위

DB/CharacterDB.cs 안의 Magic region 내 `Atk_Rate = N` 패턴, 총 **14건 / 6명 캐릭터**.

| 캐릭터 | Region | 라인 | 효과 종류 | 현재 값 |
|--------|--------|------|----------|---------|
| 파스칼 | 전설-마법형 | 1769, 1776 | Self 상시 자버프 (패시브 「천재의 권능」) | 27 / 33 |
| 연희 | 전설-마법형 | 2054, 2060 | Party 상시 파티버프 | 19 / 25 |
| 데이지 | 전설-마법형 | 2505, 2512 | Skill(불나비) Party 버프 | 27 / 33 |
| 데이지 | 전설-마법형 | 2527, 2534 | Passive Party 상시 버프 | 21 / 27 |
| 프레이야 | 전설-마법형 | 3077, 3083 | Skill PreCastBuff (Self) | 10 / 10 |
| 소교 | 전설-마법형 | 3620, 3629 | Skill(호접지몽) Party 버프 | 25 / 31 |
| 노호 | 영웅-마법형 | 3986, 3992 | Party 조건부 버프 | 19 / 21 |

> 라인 번호는 작업 시점에 변경될 수 있으므로, 실제 작업 시 region 기준으로 재확인.

## 유지 대상 (Physical region — 변경 금지)

물리 region 내 `Atk_Rate` 14건은 그대로 유지:
- 태오 / 클라한 / 여포 (전설 공격형)
- 아일린 / 지크 (전설 만능형 - 물리)
- 빅토리아 (영웅 만능형 - 물리)

## 작업 절차

1. 각 캐릭터별로 `Atk_Rate = N` 또는 `Atk_Rate = N, ...` 패턴을 `MagicAtk_Rate = N` 또는 `MagicAtk_Rate = N, ...`로 교체.
2. `BuffSet { Atk_Rate = 27, Cri_Dmg = 40 }` 같이 다른 필드와 함께 있는 경우 `MagicAtk_Rate`만 바꾸고 다른 필드는 그대로.
3. `PreCastBuff = new BuffSet{ Atk_Rate = 10 }` 같은 PreCastBuff 패턴도 동일 규칙 적용.
4. 14건 변경 후 `dotnet build` 검증.

## 검증

### 1. 빌드 검증
```
dotnet build Sena_Calculator.csproj
# 경고 0, 오류 0
```

### 2. 회귀 비교 (마법형 캐릭터)

마이그레이션 전후로 마법형 캐릭터의 최종 데미지가 **동일해야 함**. 이유:
- `StatCalculator.EffAtkRate(buff, atkType)` 헬퍼가 Magic 캐릭터에 대해 `MagicAtk_Rate`를 읽음
- 마이그레이션 전: `Atk_Rate = 27`인데 헬퍼는 `MagicAtk_Rate`(0)를 읽음 → 버프 0% 적용 (잘못된 동작이 노출되어 있던 상태)
- 마이그레이션 후: `MagicAtk_Rate = 27`을 헬퍼가 읽음 → 27% 적용 (올바른 동작)

→ **마이그레이션 후 마법형 캐릭터의 데미지가 증가**할 것이 예상됨. Phase 4 작업이 끝난 시점에 이미 버프가 무효화되어 있었기 때문.

### 3. 우선순위 검증 대상

다음 4명의 데미지를 게임 실측값과 비교 (기존 CLAUDE.md 표 기준):

| 캐릭터 | AttackType | 기존 오차 | 영향 받는 버프 |
|--------|-----------|---------|--------------|
| 에스파다 | Magic | 0.003% | 패시브 `Mark_Purify` (Atk_Rate 없음 → 영향 없음 추정) |
| 루리 | Magic | 0.9% | 패시브 `Dmg_Dealt_Type` (Atk_Rate 없음 → 영향 없음 추정) |
| 타카 | Physical | 1.6% | 영향 없음 |
| 라이언 | Physical | ~0% | 영향 없음 |

→ 4명 모두 `Atk_Rate` 직접 영향은 없을 가능성. 마이그레이션 후에도 기존 오차 유지 예상. 단 위 6명(쥬피·연희·데이지·프레이야·소교·아리엘)을 측정 가능하면 더 좋음.

### 4. 회귀 테스트 통합

[`memory/project_regression_test_plan.md`](../memory/project_regression_test_plan.md)에 따라 옵티마이저 확장 시 자동화 패턴으로 통합. Phase 5 종료 후 옵티마이저 작업 시점에 본격 검증.

## 후속 작업 (Phase 6)

- `EquipmentOptimizer`에 캐릭터 `AttackType` 필터 추가 (선택사항)
- 현재는 모든 캐릭터가 동일 무기 풀을 쓸 수 있는 구조이므로, 게임상 "공격형은 마법 무기 착용 불가" 제약을 코드에 반영할지 결정 필요

## 관련 코드

- `Models/Character.cs` — `AttackType` enum 정의
- `Models/BuffSet.cs` — `MagicAtk_Rate` 필드 정의
- `Services/StatCalculator.cs` — `EffAtkRate` 헬퍼
- `DB/CharacterDB.cs` — Magic region 내 변경 대상

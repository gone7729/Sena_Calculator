---
name: project-atk-type-split
description: 공격력 타입 분리(물리 Atk / 마법 MagicAtk) 작업이 Phase 1-5로 완료됨. Phase 6(옵티마이저 무기 타입 필터)은 미진행 결정.
metadata:
  type: project
---

세븐나이츠 리버스의 공격형/마법형 공격력 분리 작업. 2026-05-20 Phase 1~5 완료, crew 브랜치 커밋 ed9544c(Phase 1-4)·0e1b3a1(Phase 5).

**구조:**
- `BaseStatSet.Atk`(물리) / `MagicAtk`(마법), `BuffSet.Atk_Rate` / `MagicAtk_Rate` 필드 분리
- `Character.AttackType` enum(Physical/Magic) — 캐릭터마다 명시. CharacterDB는 `#region 등급-역할-물리/마법 ID범위~`로 분류
- `StatCalculator.EffAtkRate(buff, atkType)` 헬퍼가 캐릭터 AttackType에 따라 Atk_Rate ↔ MagicAtk_Rate 선택
- 무기/장비/메인옵션/서브옵션 데이터는 **분리하지 않음** — 단일 `Atk` 표기를 캐릭터 AttackType이 의미 결정(자동 매핑, B안). 마법형 캐릭터의 BaseStatSet.Atk는 의미상 마법공격력

**Phase 6 미진행:** 옵티마이저에 "공격형은 마법 무기 착용 불가" 타입 필터 추가는 보류. 무기 데이터를 물리/마법으로 분리하지 않은 현 구조에서는 실질 영향이 적어 사용자가 미진행 결정.

**Why:** 게임 메카닉 — 캐릭터 1명은 타입에 따라 공격력 OR 마법공격력 하나만 보유. 버프는 공격력증가/마법공격력증가로 분리되지만, 장비 옵션의 공격력%/공격력은 단일 표기로 착용 무기 타입에 따라 적용. 자세한 게임 규칙은 [[project-equipment-db-design]] 참조.

**How to apply:**
- 새 캐릭터 추가 시: 해당 region에 넣고 `AttackType = AttackType.Magic`(마법) 명시. 물리는 기본값이라 생략 가능
- 마법형 캐릭터 패시브/스킬에 "공격력 증가" 버프가 있으면 반드시 `MagicAtk_Rate`로 정의(`Atk_Rate`로 하면 EffAtkRate가 0을 읽어 무효화됨)
- 향후 옵티마이저 확장 시 Phase 6 재검토 가능 + [[project-regression-test-plan]] 회귀 테스트 통합
- 다음 작업 후보: docs/PHASE5_MAGIC_ATTACK_MIGRATION.md 참조

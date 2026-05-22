---
name: reference-buff-dispel-rules
description: 버프 해제(적 버프 제거) 게임 규칙 — 적용 순서·해제 불가 효과
metadata:
  type: reference
---

세나리 게임의 **버프 해제**(적군 버프 제거) 규칙:

- **턴제 버프만 해제 대상** — 상시 버프(`BattleEffect.IsPermanent`)는 해제되지 않는다. 추가로 적용되는 턴제 버프만 해제할 수 있다.
- **적용 순서대로 해제** — 먼저 적용된 턴제 버프부터 제거(FIFO).
- **해제 불가 효과**: 피해 면역, 피해 무효화, 권능(權能) 효과는 (턴제여도) 버프 해제로 제거되지 않는다. 해제 카운트 소비 시 이 효과들은 건너뛴다.

런타임 순서 추적 설계: 턴제 버프가 영웅에게 적용될 때 **각 영웅별로 1부터 증가하는 ID**(`BattleEffect.ApplyOrderId`)를 부여 → 해제 시 ID가 작은 것부터 제거. 카운터는 영웅별 상태(EffectManager/BattleState)에 보관. 0 = 미부여/상시.

모델: 버프 해제는 `SkillEffectType.BuffDispel` / `PersistentEffectType.BuffDispel` + `DispelBuffCount`(제거 개수)로 표현. (디버프 해제는 `DebuffCleanse` + `DispelDebuffCount`로 별개.) 런타임 동작(순서 추적·예외 효과 스킵)은 추후 구현.

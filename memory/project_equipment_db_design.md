---
name: project-equipment-db-design
description: 장비 DB 구조의 의도된 설계 — 메인/서브 변수명 공유, AccessoryDb 4/5성 미구현, SubStatBase+TierValues 중복은 강화 시스템 표현용
metadata:
  type: project
---

`DB/EquipmentDB.cs`에서 일견 중복·누락처럼 보이는 세 가지는 모두 의도된 설계.

**1. 메인옵션·서브옵션의 변수명 공유**
- `SubStatDb.AllStatNames`에 `"받피감%"`이 있지만 `SubStatBase`/`TierValues`에는 없음
- → 메인·서브가 변수명을 공유하기 때문에 발생하는 자연스러운 비대칭. `"받피감%"`는 방어구 메인옵션 전용. AllStatNames는 공통 풀.

**2. `AccessoryDb.SubOptions`가 6성만 정의됨**
- 4성·5성은 게임상 사용 빈도가 낮아 의도적 미구현. 미완성·버그 아님.

**3. `SubStatBase` ↔ `TierValues` 중복**
- 같은 정보를 `Dictionary<string, BaseStatSet>`와 `Dictionary<string, int>` 두 형태로 보유
- → 인게임 장비 강화 시스템(3강마다 빈 슬롯에 부옵션 추가 또는 기존 값 증가, 총 15강+) 표현 차원. 티어 정수값(`TierValues`)과 실제 스탯 누적값(`SubStatBase × Tier`)이 모두 필요.

**Why:** 2026-05-19에 사용자가 직접 확인. "메인과 서브옵션이 공통적인 변수명이 많아서 그래" / "4,5성은 사용필요성이 적어서 아직 미구현" / "강화가 3단계마다 빈 슬롯에 부옵션이 추가되거나 이미 존재하는 부옵션의 값이 증가하는 강화 시스템이라 저렇게 작업한거 같아"

**How to apply:**
- 위 세 가지는 검수에서 이슈로 올리지 말 것 — 의도된 설계
- 단, 만약 강화 시스템 자체를 다시 설계하는 작업이 들어오면 `SubStatBase` 단일 소스에서 파생 고려 가능
- 관련: [[project-web-migration-priority]] (UI 후순위)

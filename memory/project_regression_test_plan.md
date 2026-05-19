---
name: project-regression-test-plan
description: 테스트 캐릭터 4명(에스파다·루리·타카·라이언) 회귀 측정은 향후 옵티마이저 작업 시 자동화된 특정 패턴 테스트로 통합 진행 예정
metadata:
  type: project
---

CLAUDE.md에 명시된 4명 테스트 캐릭터(에스파다 0.003% / 루리 0.9% / 타카 1.6% / 라이언 ~0%)의 회귀 측정은 별도 수동 작업으로 진행하지 않음. 향후 `Services/Optimizer/` 작업 확장 시 특정 패턴 테스트로 자동화해 통합.

**Why:** 2026-05-19 결정. 검수 세션에서 통합 효과 시스템(EffectManager) 이관 이후 미측정 상태가 확인됐으나, 옵티마이저가 어차피 다양한 빌드 조합을 자동 평가하므로 그 흐름에 4명의 황금 케이스를 끼워 넣는 게 효율적이라는 판단.

**How to apply:**
- 옵티마이저 확장 작업 시 회귀 테스트 케이스(4명) 통합을 작업 항목에 포함
- 그 시점에 게임 내 실측값을 얻어와 계산기 출력과 대조
- 그 전까지는 코어 공식(DamageCalculator) 변경 시에만 별도 회귀 우려, 데이터 변환 경로(EffectConverter/EffectManager) 변경은 위 4명이 자동 검증 대상이 됨
- 측정 시 특히 주목할 영역:
  - **타카**: 매의 발톱 패시브가 `ApplyMode.Triggered`·`StacksPerTrigger`·`TriggerCondition.AllAttack` 새 시스템으로 전환됨 (commit 85380c0) — 광역기 사용 시 스택 누적이 정확한지
  - **에스파다**: `Mark_Purify` 특수 메카닉(타입피증과 별도 적용) + 정화탄 SkillEffect 디버프 + 신의 심판 ConsumeExtra가 복합 작동
  - **루리·라이언**: 단순 구조라 회귀 위험 낮음

관련: [[project-web-migration-priority]] (DB/Services가 웹 이관에서도 그대로 재사용되므로 이 회귀 검증이 웹 측 정확도도 보장)

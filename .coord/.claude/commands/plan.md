---
description: "[HEAD 전용] 사용자 요청 분해해 sub task 작성 (inbox 없이)"
argument-hint: "<작업 설명>"
requires-wt: head
min-coord-version: v0.42
---

**⚠ wt/head 에서만 실행.**

`/inbox` 은 원격 inbox 엔트리 기반 전체 파이프라인을 돌리지만, `/plan` 은 **사용자가 직접 준 요청을 분해만** 한다 (inbox 파일 없이). 실제 sub 실행은 별도 `/dispatch <subs>` 로.

## 경로 해결
```bash
source "$(git rev-parse --show-toplevel)/scripts/config.sh" 2>/dev/null || \
  source "$(git rev-parse --show-toplevel)/.coord/scripts/config.sh"
```

## 절차

1. **사전 분석**
   - 도메인 식별 (어떤 sub 영역인지 — config.yml 의 `subs[].scope_allowed` 참조)
   - `$COORD_ROOT/coordination/hotfixes.md` 최근 항목 확인
   - 관련 파일 최근 커밋 훑어보기

2. **분해 + 의존성**
   - Step 단위로 쪼개고 sub 간 의존성 (A 끝나야 B 가능 등) 명시
   - 동시 실행 가능한 Step 은 별도 그룹

3. **task 파일 작성** — `$COORD_ROOT/coordination/tasks/wt-<sub>.md`
   - 범위 / 금지 / 목표 / 검증 조건 명시
   - `task_start_commit` (현재 sub 브랜치 HEAD)

4. **STATUS.md 갱신** — `$COORD_ROOT/coordination/STATUS.md` 에 진행 상태

5. **사용자 보고** — 분해 결과 요약, 다음 단계 안내 (`/dispatch <subs>`)

## 금지
- 직접 코드 수정 (plan 만, 구현은 sub 몫)
- `coordination/roles/`, `coordination/inbox/`, `coordination/USAGE.md` 수정 (메인 정본)
- 승인 없이 `$WORK_BRANCH` 머지

---
description: "[HEAD 전용] needs-rework plan 재시도 (최대 2회)"
argument-hint: "<inbox-id>"
requires-wt: head
min-coord-version: v0.42
---

**⚠ wt/head 에서만 실행.**

## 경로 해결
```bash
source "$(git rev-parse --show-toplevel)/scripts/config.sh" 2>/dev/null || \
  source "$(git rev-parse --show-toplevel)/.coord/scripts/config.sh"
SIBLING_BASE="$(dirname "$PROJECT_ROOT")"
```

## 절차

1. **plan 확인** — `$COORD_ROOT/coordination/plans/<inbox-id>.md`
   - `status: needs-rework` 아니면 거절 (/retry 대상 아님)
   - `retry_count` 확인, **2회 초과면 거절** (무한 루프 방지)

2. **실패 sub 정리**
   ```bash
   for sub in <failed-subs>; do
     SUB_DIR="$SIBLING_BASE/${PROJECT_NAME}-wt-${sub}"
     cd "$SUB_DIR"
     TS=$(date +%Y%m%d-%H%M%S)
     git branch "failed/wt-${sub}-retry-${TS}"
     git reset --hard origin/"${WORK_BRANCH}"
   done
   ```

3. **task 갱신** — `$COORD_ROOT/coordination/tasks/wt-<sub>.md`
   - 이전 실패 원인 (review-inbox 의 `issues:`) 을 수정 요구사항으로 반영
   - `retry_count += 1`

4. **재dispatch** — `/dispatch <failed-subs>`

5. **재검증** — 성공 시 plan `status: done`, 실패 시 `status: blocked` (사람 개입 필요)

6. **Discord 알림**
   ```bash
   bash "$COORD_ROOT/scripts/notify-discord.sh" "🔄 재시도 (${retry_count}회차)" \
     "**id**: <inbox-id>\n**sub**: <failed-subs>\n**수정 요구사항**: <요약>"
   ```

## 금지
- retry_count > 2 상태에서 강제 재시도
- 이전 실패 원인 무시하고 동일 task 로 재실행

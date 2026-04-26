#!/usr/bin/env bash
# notify-step.sh — Step 별 Discord 알림 표준 포맷 (Phase 2 잔여)
#
# 사용법:
#   bash scripts/notify-step.sh <plan_id> <step_num>/<step_total> <sub_name> <status> [<summary>] [<link-path>]
#
# 인자:
#   plan_id     plan 식별자 (예: 2026-04-16-143000 또는 inbox id)
#   step        현재/전체 (예: 1/3)
#   sub_name    wt/<name> 의 name 부분 (db, backend, ...)
#   status      started | ok | done | fail | blocked | skip
#   summary     한줄 요약 (선택, "67 files, +124/-5" 같은 짧은 내용 권장)
#   link-path   첨부 링크 경로 (선택, notify-discord.sh 의 3번째 인자 참조)
#
# 예시:
#   bash scripts/notify-step.sh plan-abc 1/3 db started "SSOT 재정비 시작"
#   bash scripts/notify-step.sh plan-abc 1/3 db ok "7 files, +124/-5"
#   bash scripts/notify-step.sh plan-abc 2/3 backend fail "type-check 실패: 3 errors"
#
# 동작: notify-discord.sh 를 표준 title/message 포맷으로 호출. 실패해도 non-fatal (exit 0).

set -uo pipefail

PLAN_ID="${1:-}"
STEP="${2:-}"
SUB_NAME="${3:-}"
STATUS="${4:-}"
SUMMARY="${5:-}"
LINK_PATH="${6:-}"

if [[ -z "$PLAN_ID" ]] || [[ -z "$STEP" ]] || [[ -z "$SUB_NAME" ]] || [[ -z "$STATUS" ]]; then
  echo "사용법: $0 <plan_id> <step_num>/<step_total> <sub_name> <status> [<summary>] [<link-path>]"
  echo "  status: started | ok | done | fail | blocked | skip"
  exit 2
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# status 별 아이콘 + 톤
case "$STATUS" in
  started)  ICON="▶️";  TONE="진행" ;;
  ok|done)  ICON="✅";  TONE="완료" ;;
  fail)     ICON="❌";  TONE="실패" ;;
  blocked)  ICON="⏸️";  TONE="차단" ;;
  skip)     ICON="⏭️";  TONE="스킵" ;;
  *)
    echo "⚠ 알 수 없는 status: $STATUS (허용: started|ok|done|fail|blocked|skip)" >&2
    ICON="ℹ️"; TONE="$STATUS"
    ;;
esac

# 제목: "✅ Step 2/3 완료 (wt/backend)"
TITLE="$ICON Step $STEP $TONE (wt/$SUB_NAME)"

# 메시지: 1줄 요약 + plan_id
MSG_LINE1="plan: \`$PLAN_ID\`"
if [[ -n "$SUMMARY" ]]; then
  MESSAGE="$MSG_LINE1
$SUMMARY"
else
  MESSAGE="$MSG_LINE1"
fi

# notify-discord.sh 위임
if [[ -x "$SCRIPT_DIR/notify-discord.sh" ]]; then
  bash "$SCRIPT_DIR/notify-discord.sh" "$TITLE" "$MESSAGE" "$LINK_PATH" "📂 보기" 2>&1 || true
else
  echo "⚠ $SCRIPT_DIR/notify-discord.sh 없음 — 알림 건너뜀" >&2
fi

# 로컬 stdout 에도 1줄 — 호출자가 파이프/로그로 수집할 수 있게
echo "[notify-step] $TITLE | ${SUMMARY:-}"

# Discord 실패는 non-fatal (알림은 부가기능)
exit 0

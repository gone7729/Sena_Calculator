#!/usr/bin/env bash
# 토큰 사용량 % 체크 + 임계 알림 + 주간 리셋 자동 처리
# 사용법:
#   bash scripts/check-token-threshold.sh [<used_tokens>]
# (used 인자 없으면 token-budget.md 에서 읽음)

set -euo pipefail

BUDGET_MD="coordination/token-budget.md"
[[ ! -f "$BUDGET_MD" ]] && { echo "⚠ token-budget.md 없음"; exit 0; }

# config.sh 로드 (있으면)
SCRIPT_DIR_T="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR_T/config.sh" ]] && source "$SCRIPT_DIR_T/config.sh" 2>/dev/null || true

# === 설정 읽기 (config.yml 우선, 없으면 token-budget.md fallback) ===
LIMIT="${WEEKLY_LIMIT:-}"
if [[ -z "$LIMIT" ]]; then
  LIMIT=$(grep -E "weekly_limit:" "$BUDGET_MD" | head -1 | grep -oE '[0-9]+' | head -1 || echo "5000000")
fi
WEEK_START=$(grep -E "^- \*\*week_start_ts\*\*:" "$BUDGET_MD" | head -1 | sed 's/.*: //' | awk '{print $1}' || echo "")
LAST_ALERT=$(grep -E "^- \*\*last_alerted_threshold\*\*:" "$BUDGET_MD" | head -1 | grep -oE '[0-9]+' | head -1 || echo "0")
USED="${1:-$(grep -E "^- \*\*used_tokens\*\*:" "$BUDGET_MD" | head -1 | grep -oE '[0-9]+' | head -1 || echo "0")}"

# === 주간 리셋 체크 (월요일 00:00 KST 기준) ===
# 현재 주 월요일 00:00 KST 의 ISO timestamp 계산
THIS_MONDAY=$(python3 -c "
from datetime import datetime, timedelta, timezone
kst = timezone(timedelta(hours=9))
now = datetime.now(kst)
# 월요일=0, 일요일=6
days_since_monday = now.weekday()
monday = (now - timedelta(days=days_since_monday)).replace(hour=0, minute=0, second=0, microsecond=0)
print(monday.isoformat())
")

# week_start_ts 가 이번 월요일보다 이전 = 새 주차 진입
NEEDS_RESET=false
if [[ -z "$WEEK_START" ]] || [[ "$WEEK_START" < "$THIS_MONDAY" ]]; then
  NEEDS_RESET=true
fi

if $NEEDS_RESET; then
  PREV_USED=$USED
  PREV_LIMIT=$LIMIT
  PREV_PCT=$(python3 -c "print(round($PREV_USED * 100 / $PREV_LIMIT, 1))")

  # 리셋
  USED=0
  LAST_ALERT=0
  WEEK_START="$THIS_MONDAY"

  # token-budget.md 갱신
  python3 -c "
import re
with open('$BUDGET_MD') as f:
    c = f.read()
c = re.sub(r'- \*\*week_start_ts\*\*:.*', '- **week_start_ts**: $WEEK_START', c, count=1)
c = re.sub(r'- \*\*used_tokens\*\*:.*', '- **used_tokens**: 0', c, count=1)
c = re.sub(r'- \*\*last_alerted_threshold\*\*:.*', '- **last_alerted_threshold**: 0', c, count=1)
c = re.sub(r'- \*\*last_update\*\*:.*', '- **last_update**: $(date -Iseconds) (reset)', c, count=1)
with open('$BUDGET_MD', 'w') as f:
    f.write(c)
"

  # 회복 알림
  bash scripts/notify-discord.sh "🔄 토큰 카운터 리셋 — 새 주차 시작" \
    "지난 주 사용: $PREV_USED / $PREV_LIMIT (${PREV_PCT}%)
이번 주: 0 / $LIMIT (0%)
주차 시작: $THIS_MONDAY KST" \
    "coordination/token-budget.md" "📊 예산 보기"

  echo "🔄 주간 리셋: 사용량 0 으로 초기화"
  exit 0
fi

# === 사용량 % 계산 ===
PCT_INT=$(python3 -c "print(int($USED * 100 / $LIMIT))")
PCT_FLOAT=$(python3 -c "print(round($USED * 100 / $LIMIT, 2))")

# === 임계 매칭 (높은 값부터 — 한 번에 여러 개 넘을 수 있어 가장 높은 것만 알림) ===
THRESHOLDS=(99 98 97 96 95 90 80 70 60 50 40 30 20 10)

CROSSED=""
for t in "${THRESHOLDS[@]}"; do
  if [[ $PCT_INT -ge $t ]] && [[ $LAST_ALERT -lt $t ]]; then
    CROSSED="$t"
    break
  fi
done

if [[ -n "$CROSSED" ]]; then
  # 알림 톤 결정
  case "$CROSSED" in
    99|98|97|96|95) ICON="🚨"; TONE="critical" ;;
    90)             ICON="⚠️"; TONE="high" ;;
    80)             ICON="⚠"; TONE="warning" ;;
    *)              ICON="📊"; TONE="info" ;;
  esac

  REMAINING=$((LIMIT - USED))

  bash scripts/notify-discord.sh "$ICON 토큰 ${CROSSED}% 사용 (${TONE})" \
    "사용: $USED / $LIMIT (${PCT_FLOAT}%)
남은: $REMAINING 토큰
이번 주 시작: $WEEK_START KST" \
    "coordination/token-budget.md" "📊 예산 상세"

  # last_alerted_threshold 갱신
  python3 -c "
import re
with open('$BUDGET_MD') as f:
    c = f.read()
c = re.sub(r'- \*\*last_alerted_threshold\*\*:.*', '- **last_alerted_threshold**: $CROSSED', c, count=1)
with open('$BUDGET_MD', 'w') as f:
    f.write(c)
"

  echo "🔔 ${CROSSED}% 임계 알림 발송"
else
  echo "ℹ ${PCT_FLOAT}% (last_alert: ${LAST_ALERT}%, 다음 임계 미도달)"
fi

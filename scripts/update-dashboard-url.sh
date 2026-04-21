#!/usr/bin/env bash
# cloudflared 로그에서 현재 TryCloudflare URL 추출 → coordination/dashboard-url.txt 저장
# 사용법:
#   bash scripts/update-dashboard-url.sh                    # 자동 탐지
#   bash scripts/update-dashboard-url.sh <log-file>         # 특정 로그
#   bash scripts/update-dashboard-url.sh --url <full-url>   # 명시
#
# notify-discord.sh 가 이 파일 읽어 알림에 첨부함.

set -euo pipefail

OUT_FILE="coordination/dashboard-url.txt"
NOTIFY_ON_CHANGE=true

# 명시 URL 모드
if [[ "${1:-}" == "--url" ]]; then
  URL="${2:-}"
  [[ -z "$URL" ]] && { echo "❌ URL 인자 필요"; exit 1; }
else
  # 로그에서 추출
  LOG="${1:-/tmp/cloudflared.log}"
  [[ ! -f "$LOG" ]] && { echo "❌ 로그 없음: $LOG"; exit 1; }
  URL=$(grep -oE 'https://[a-z0-9-]+\.trycloudflare\.com' "$LOG" | tail -1)
  [[ -z "$URL" ]] && { echo "❌ 로그에서 URL 못 찾음 (cloudflared 시작 안 됐거나 named tunnel)"; exit 1; }
fi

# 변경 감지
CHANGED=false
if [[ -f "$OUT_FILE" ]]; then
  PREV=$(cat "$OUT_FILE")
  [[ "$PREV" != "$URL" ]] && CHANGED=true
else
  CHANGED=true
fi

# 저장
echo "$URL" > "$OUT_FILE"
echo "✅ URL 저장: $URL"

# 변경 시 Discord 알림
if $CHANGED && $NOTIFY_ON_CHANGE; then
  bash scripts/notify-discord.sh "🌐 Dashboard URL 갱신" \
    "현재 대시보드 URL:" \
    "$URL" "📊 열기" 2>/dev/null || true
fi

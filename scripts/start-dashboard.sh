#!/usr/bin/env bash
# Coordination Dashboard 서버 + (옵션) Cloudflare Tunnel
# 사용법:
#   bash scripts/start-dashboard.sh           # 서버만 (config.yml 의 dashboard.port)
#   bash scripts/start-dashboard.sh --tunnel  # 서버 + cloudflared

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR/.."

# config.sh 로 PORT 기본값 끌어오기 (DASHBOARD_PORT 가 이미 export 됨)
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true
PORT="${PORT:-${DASHBOARD_PORT:-8888}}"
WITH_TUNNEL=false
[[ "${1:-}" == "--tunnel" ]] && WITH_TUNNEL=true

# UTF-8 강제 (Windows cp949 회피)
export PYTHONUTF8=1
export PYTHONIOENCODING=utf-8

echo "▶ Dashboard server 시작 (port $PORT, UTF-8)..."
python3 scripts/dashboard.py &
SERVER_PID=$!
echo "  PID: $SERVER_PID"

trap 'echo "▶ 종료..."; kill $SERVER_PID 2>/dev/null; [[ -n "${TUNNEL_PID:-}" ]] && kill $TUNNEL_PID 2>/dev/null; exit 0' INT TERM

sleep 2

if $WITH_TUNNEL; then
  if ! command -v cloudflared >/dev/null 2>&1; then
    echo "⚠ cloudflared 미설치 — winget install cloudflare.cloudflared"
    echo "  서버는 계속 실행 (localhost:$PORT)"
  else
    echo "▶ Cloudflare Tunnel 시작..."
    # named tunnel 사용 시: cloudflared tunnel run ${PROJECT_NAME:-coord}-dashboard
    # 임시 (TryCloudflare): URL 자동 발급
    cloudflared tunnel --url "http://localhost:$PORT" 2>&1 | tee /tmp/cloudflared.log &
    TUNNEL_PID=$!
    sleep 5
    echo ""
    echo "🌐 Tunnel URL (위 로그 또는 /tmp/cloudflared.log 에서 확인):"
    grep -oE 'https://[a-z0-9-]+\.trycloudflare\.com' /tmp/cloudflared.log | head -1 || echo "  (대기 중...)"
  fi
fi

echo ""
echo "✅ Dashboard: http://localhost:$PORT"
echo "   API: http://localhost:$PORT/api/status"
echo "   Ctrl+C 로 종료"

wait $SERVER_PID

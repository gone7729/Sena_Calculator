#!/usr/bin/env bash
# Discord Bot Controller 실행 (Phase 6 MVP)
#
# 사용법:
#   bash scripts/start-bot.sh           # foreground
#   bash scripts/start-bot.sh --bg      # background (nohup)
#
# 사전 준비:
#   1. bot/requirements.txt 설치:  pip install -r bot/requirements.txt
#   2. bot/projects.yml.example → bot/projects.yml 복사 후 프로젝트 경로 기입
#   3. .env.local 에 DISCORD_BOT_TOKEN 세팅
#   4. Discord Developer Portal 에서 Message Content Intent 활성화

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR/.."

BG=false
[[ "${1:-}" == "--bg" ]] && BG=true

# .env.local 자동 로드 (DISCORD_BOT_TOKEN 세팅용)
if [[ -f ".env.local" ]]; then
  set -a
  # shellcheck disable=SC1091
  source ".env.local"
  set +a
fi

if [[ -z "${DISCORD_BOT_TOKEN:-}" ]]; then
  echo "❌ DISCORD_BOT_TOKEN 없음 — .env.local 에 세팅 후 재시도"
  exit 1
fi

if [[ ! -f "bot/projects.yml" ]]; then
  echo "❌ bot/projects.yml 없음 — bot/projects.yml.example 복사 후 편집"
  echo "   cp bot/projects.yml.example bot/projects.yml"
  exit 1
fi

# UTF-8 강제 (Windows cp949 회피) + 출력 비버퍼링 (nohup 로그 실시간 확인용)
export PYTHONUTF8=1
export PYTHONIOENCODING=utf-8
export PYTHONUNBUFFERED=1

if $BG; then
  LOG="/tmp/coord-bot-$(date +%Y%m%d-%H%M%S).log"
  echo "▶ bot 백그라운드 실행 → $LOG"
  nohup python3 bot/controller.py > "$LOG" 2>&1 &
  echo "  PID: $!"
  echo "  정지: kill $!"
else
  echo "▶ bot foreground 실행 (Ctrl+C 로 종료)"
  python3 bot/controller.py
fi

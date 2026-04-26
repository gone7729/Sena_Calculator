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

# v0.38+: config.sh 로 PROJECT_ROOT / ENV_FILE_RESOLVED 해결
# (subdir 모드에서 cwd=.coord/ 라 기존 "./.env.local" 은 miss. 구버전 동작 보존 위해 fallback 유지)
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true

ENV_FILE="${ENV_FILE_RESOLVED:-}"
if [[ -z "$ENV_FILE" ]]; then
  # config.sh 미사용 fallback — .coord/.env.local → PROJECT_ROOT/.env.local 순
  if [[ -f "$SCRIPT_DIR/../.env.local" ]]; then
    ENV_FILE="$SCRIPT_DIR/../.env.local"
  elif [[ -f "$SCRIPT_DIR/../../.env.local" ]]; then
    ENV_FILE="$SCRIPT_DIR/../../.env.local"
  fi
fi

# .env.local 자동 로드 (DISCORD_BOT_TOKEN 세팅용)
if [[ -n "$ENV_FILE" ]] && [[ -f "$ENV_FILE" ]]; then
  echo "📄 .env.local: $ENV_FILE"
  set -a
  # shellcheck disable=SC1091
  source "$ENV_FILE"
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

# v0.34: .venv 자동 감지 — install-deps 스크립트가 만든 venv 우선 사용
# (macOS/Linux: .venv/bin/python, Windows Git Bash: .venv/Scripts/python.exe)
PYTHON_BIN="python3"
if [[ -x ".venv/bin/python" ]]; then
  PYTHON_BIN=".venv/bin/python"
  echo "🐍 venv 감지: $PYTHON_BIN"
elif [[ -x ".venv/Scripts/python.exe" ]]; then
  PYTHON_BIN=".venv/Scripts/python.exe"
  echo "🐍 venv 감지: $PYTHON_BIN"
fi

# discord 모듈 사전 확인 (더 친절한 에러 메시지)
if ! "$PYTHON_BIN" -c "import discord" 2>/dev/null; then
  echo "❌ discord 모듈 없음 — 의존성 미설치 상태"
  echo ""
  echo "해결:"
  echo "  (A) 의존성 자동 설치:"
  echo "      bash scripts/install-deps-mac.sh          # macOS"
  echo "      scripts\\install-deps-windows.ps1          # Windows"
  echo ""
  echo "  (B) 수동 설치:"
  echo "      uv venv && source .venv/bin/activate && uv pip install -r bot/requirements.txt"
  echo "      또는"
  echo "      python3 -m venv .venv && source .venv/bin/activate && pip install -r bot/requirements.txt"
  exit 1
fi

if $BG; then
  LOG="/tmp/coord-bot-$(date +%Y%m%d-%H%M%S).log"
  echo "▶ bot 백그라운드 실행 → $LOG"
  nohup "$PYTHON_BIN" bot/controller.py > "$LOG" 2>&1 &
  echo "  PID: $!"
  echo "  정지: kill $!"
else
  echo "▶ bot foreground 실행 (Ctrl+C 로 종료)"
  "$PYTHON_BIN" bot/controller.py
fi

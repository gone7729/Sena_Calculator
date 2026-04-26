#!/usr/bin/env bash
# coord-template 의존성 자동 설치 (macOS)
# 사용: bash scripts/install-deps-mac.sh [--skip-cloudflared] [--yes]
#
# 설치 대상:
#   - Homebrew (없으면 안내만)
#   - python@3.12 / node / git / uv / (옵션) cloudflared
#   - claude CLI (npm 글로벌)
#   - bot/requirements.txt (.venv 생성 + uv pip)
#
# 이미 설치된 항목은 skip. `--yes` 면 확인 없이 진행.

set -euo pipefail

SKIP_CLOUDFLARED=0
AUTO_YES=0
for arg in "$@"; do
  case "$arg" in
    --skip-cloudflared) SKIP_CLOUDFLARED=1 ;;
    --yes|-y) AUTO_YES=1 ;;
    -h|--help)
      head -n 12 "$0" | grep -E "^#" | sed 's/^# \?//'
      exit 0
      ;;
    *) echo "⚠ 알 수 없는 인자: $arg" ; exit 1 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "📦 coord-template 의존성 설치 (macOS)"
echo "   repo: $REPO_ROOT"
echo

# ─── 1. Homebrew ──────────────────────────────────────────
if ! command -v brew >/dev/null 2>&1; then
  cat <<EOF
❌ Homebrew 가 설치돼있지 않습니다.
   먼저 아래 명령으로 설치 후 이 스크립트 재실행:

   /bin/bash -c "\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

   설치 후 shell 재시작 또는 eval "\$(/opt/homebrew/bin/brew shellenv)" 실행 필요.
EOF
  exit 1
fi
echo "✅ Homebrew: $(brew --version | head -1)"

# ─── 2. brew 패키지 (없는 것만 설치) ──────────────────────
BREW_PKGS=("python@3.12" "node" "git" "uv")
[[ $SKIP_CLOUDFLARED -eq 0 ]] && BREW_PKGS+=("cloudflared")

TO_INSTALL=()
for pkg in "${BREW_PKGS[@]}"; do
  # python@3.12 는 brew list 에서 python@3.12 로 조회됨
  if brew list --formula 2>/dev/null | grep -qx "${pkg}"; then
    echo "✅ 이미 설치됨: $pkg"
  else
    TO_INSTALL+=("$pkg")
  fi
done

if [[ ${#TO_INSTALL[@]} -gt 0 ]]; then
  echo "📥 설치 예정: ${TO_INSTALL[*]}"
  if [[ $AUTO_YES -eq 0 ]]; then
    read -r -p "   진행? [Y/n] " REPLY
    [[ "$REPLY" =~ ^[Nn] ]] && { echo "중단."; exit 1; }
  fi
  brew install "${TO_INSTALL[@]}"
fi

# ─── 3. claude CLI (npm 글로벌) ───────────────────────────
if command -v claude >/dev/null 2>&1; then
  echo "✅ 이미 설치됨: claude ($(claude --version 2>/dev/null | head -1 || echo '버전 조회 불가'))"
else
  echo "📥 claude CLI 설치: npm i -g @anthropic-ai/claude-code"
  npm install -g @anthropic-ai/claude-code
fi

# ─── 4. Python venv + bot 의존성 ──────────────────────────
REQ="$REPO_ROOT/bot/requirements.txt"
if [[ ! -f "$REQ" ]]; then
  echo "⚠ bot/requirements.txt 없음 — venv 생성 skip"
else
  VENV_DIR="$REPO_ROOT/.venv"
  if [[ -d "$VENV_DIR" ]]; then
    echo "✅ .venv 이미 존재 — 의존성만 재설치"
  else
    echo "📥 .venv 생성 (uv)"
    (cd "$REPO_ROOT" && uv venv)
  fi

  echo "📥 bot 의존성 설치 (uv pip)"
  (cd "$REPO_ROOT" && source .venv/bin/activate && uv pip install -r bot/requirements.txt)
fi

# ─── 5. 확인 + 다음 단계 안내 ─────────────────────────────
cat <<EOF

✅ 설치 완료 — 설치된 도구:
   python    : $(python3.12 --version 2>/dev/null || python3 --version)
   node      : $(node --version)
   npm       : $(npm --version)
   git       : $(git --version)
   uv        : $(uv --version)
   claude    : $(claude --version 2>/dev/null | head -1 || echo '(설치됐지만 버전 조회 실패)')
$([[ $SKIP_CLOUDFLARED -eq 0 ]] && echo "   cloudflared: \$(cloudflared --version 2>/dev/null | head -1)")

📋 다음 단계:
   1. source .venv/bin/activate                  # Python 환경 활성화
   2. cp .env.local.example .env.local           # 환경변수 채우기 (DISCORD_WEBHOOK_URL 등)
   3. cp bot/projects.yml.example bot/projects.yml   # 봇 레지스트리 설정
   4. bash scripts/init.sh                       # coord 구조 초기화 (worktree + config.yml)
   5. bash scripts/start-bot.sh --bg             # Discord 봇 실행

📚 자세한 설정: README.md / bot/README.md
EOF

#!/usr/bin/env bash
# coord 초기화 마법사
# 사용법: bash scripts/init.sh [--mode=flat|subdir|auto]
#
# 모드 (v0.4+):
#   flat    scripts/, coordination/ 가 프로젝트 루트에 직접 있음 (기본, 기존 방식)
#   subdir  .coord/ 서브디렉토리 안에 scripts/, coordination/ 가 있음 (subtree 친화)
#   auto    스크립트 위치로 자동 판단 (basename 이 .coord 면 subdir)
#
# 동작:
# 1. 프로젝트 상태 감지 (신규 / 기존)
# 2. 대화식 config.yml 생성
# 3. .env.local 템플릿 복사 (PROJECT_ROOT 기준)
# 4. coordination/ 디렉토리 골격 생성 (COORD_ROOT 기준)
# 5. .gitignore 보강 (PROJECT_ROOT)
# 6. git hooks 설치 (INSTALL_MODE 별 경로)
# 7. 다음 단계 안내 (첫 worktree, 대시보드, 첫 inbox)

set -euo pipefail

# === 플래그 파싱 ===
INIT_MODE="auto"
RESTORE_CONFIGS=0
for arg in "$@"; do
  case "$arg" in
    --mode=flat)   INIT_MODE="flat" ;;
    --mode=subdir) INIT_MODE="subdir" ;;
    --mode=auto)   INIT_MODE="auto" ;;
    --restore-configs) RESTORE_CONFIGS=1 ;;
    -h|--help)
      awk 'NR==1 && /^#!/ {next} /^#/ {sub(/^# ?/, ""); print; next} {exit}' "$0"
      exit 0 ;;
  esac
done

# === 색상 ===
B=$'\033[1m'; D=$'\033[0m'; G=$'\033[32m'; Y=$'\033[33m'; R=$'\033[31m'; C=$'\033[36m'

say() { echo -e "${C}▶${D} $*"; }
ok()  { echo -e "${G}✓${D} $*"; }
warn() { echo -e "${Y}⚠${D} $*"; }
err()  { echo -e "${R}✗${D} $*" >&2; }

ask() {
  local prompt="$1"
  local default="${2:-}"
  local var
  if [[ -n "$default" ]]; then
    read -rp "  $prompt [$default]: " var
    echo "${var:-$default}"
  else
    read -rp "  $prompt: " var
    echo "$var"
  fi
}

ask_yn() {
  local prompt="$1"
  local default="${2:-y}"
  local var
  read -rp "  $prompt (y/n) [$default]: " var
  var="${var:-$default}"
  [[ "$var" =~ ^[Yy]$ ]]
}

# === 사전 점검 + ROOT 검출 ===
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COORD_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# INSTALL_MODE 결정 (--mode=auto 시)
if [[ "$INIT_MODE" == "auto" ]]; then
  if [[ "$(basename "$COORD_ROOT")" == ".coord" ]]; then
    INSTALL_MODE="subdir"
  else
    INSTALL_MODE="flat"
  fi
else
  INSTALL_MODE="$INIT_MODE"
fi

if [[ "$INSTALL_MODE" == "subdir" ]]; then
  PROJECT_ROOT="$(cd "$COORD_ROOT/.." && pwd)"
else
  PROJECT_ROOT="$COORD_ROOT"
  # v0.37+: flat 모드 deprecated (coord-template repo 본체에서만 사용)
  if [[ "$PROJECT_ROOT" != *"coord-template"* ]]; then
    echo ""
    echo "⚠ flat 모드는 v0.37 부터 deprecated 입니다."
    echo "  소비 프로젝트는 subdir 모드 (.coord/ subtree) 를 권장합니다 —"
    echo "  coord 파일이 배포 브랜치에 노출되지 않고 깔끔하게 분리됩니다."
    echo "  README 의 'A. subtree 설치 (권장)' 섹션 참조."
    echo ""
  fi
fi

# 경계 변수는 이후 단계에서 explicit 하게 참조. REPO_ROOT 는 호환용으로 COORD_ROOT 지정.
REPO_ROOT="$COORD_ROOT"
cd "$COORD_ROOT"

# === --restore-configs 모드 (v0.39+) ===
# git checkout / subtree pull 후 사라진 설정 파일을 .example 에서 복구.
# 전체 init 흐름(worktree 생성, config.yml 대화식 생성 등) 은 건너뜀.
if [[ $RESTORE_CONFIGS -eq 1 ]]; then
  echo ""
  echo -e "${B}  coord --restore-configs — 설정 파일 복원 모드${D}"
  echo ""

  RESTORED=0
  SKIPPED=0

  # v0.40: ((var++)) 는 이전값 0 반환 → set -e 종료. $((var+1)) 할당 형태 사용.
  restore_from_example() {
    local target="$1" example="$2" label="$3"
    if [[ -f "$target" ]]; then
      ok "[$label] 이미 존재 — skip ($target)"
      SKIPPED=$((SKIPPED+1))
    elif [[ -f "$example" ]]; then
      cp "$example" "$target"
      ok "[$label] 복원 완료: $example → $target"
      RESTORED=$((RESTORED+1))
    else
      warn "[$label] example 없음 — skip ($example)"
    fi
  }

  restore_from_example \
    "$PROJECT_ROOT/.env.local" \
    "$COORD_ROOT/.env.local.example" \
    ".env.local"

  restore_from_example \
    "$COORD_ROOT/bot/projects.yml" \
    "$COORD_ROOT/bot/projects.yml.example" \
    "bot/projects.yml"

  restore_from_example \
    "$COORD_ROOT/coordination/config.yml" \
    "$COORD_ROOT/coordination/config.yml.example" \
    "coordination/config.yml"

  # v0.40: user-generated coordination 문서 — subtree pull 로 사라지면 빈 상태에서 시작 어려움
  restore_from_example \
    "$COORD_ROOT/coordination/hotfixes.md" \
    "$COORD_ROOT/coordination/hotfixes.md.example" \
    "coordination/hotfixes.md"

  restore_from_example \
    "$COORD_ROOT/coordination/token-budget.md" \
    "$COORD_ROOT/coordination/token-budget.md.example" \
    "coordination/token-budget.md"

  CLAUDE_DIR="$PROJECT_ROOT/.claude"
  [[ ! -d "$CLAUDE_DIR" ]] && CLAUDE_DIR="$COORD_ROOT/.claude"
  restore_from_example \
    "$CLAUDE_DIR/settings.local.json" \
    "$CLAUDE_DIR/settings.local.json.example" \
    ".claude/settings.local.json"

  echo ""
  ok "복원 완료 — $RESTORED 개 복원, $SKIPPED 개 skip"
  echo ""
  echo "복원된 파일은 .example 원본 — 실제 값으로 편집 필요:"
  echo "  - .env.local: DISCORD_WEBHOOK_URL, DISCORD_BOT_TOKEN, GITHUB_REPO_URL 등"
  echo "  - bot/projects.yml: 프로젝트 등록"
  echo "  - coordination/config.yml: subs/head 설정 (필요 시 대화식 재생성은 bash .coord/scripts/init.sh)"
  exit 0
fi

echo ""
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo -e "${B}  coord — 다중 Claude 병렬 오케스트레이션 초기화${D}"
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo ""

# git repo 확인
if ! git rev-parse --git-dir >/dev/null 2>&1; then
  err "git repo 가 아님 — 먼저 'git init' 후 다시 실행"
  exit 1
fi

# 필수 파일 점검
[[ -f "coordination/config.yml.example" ]] || { err "coordination/config.yml.example 없음 — 템플릿이 깨짐"; exit 1; }

# === 모드 감지 ===
say "프로젝트 상태 감지..."
TRACKED_FILES=$(git ls-files | wc -l)
HAS_CONFIG=false
[[ -f "coordination/config.yml" ]] && HAS_CONFIG=true

if $HAS_CONFIG; then
  warn "coordination/config.yml 이미 존재 — 덮어쓰기 모드"
  if ! ask_yn "기존 config 를 백업하고 새로 생성?" "n"; then
    say "중단. 기존 config 유지."
    exit 0
  fi
  cp coordination/config.yml "coordination/config.yml.bak.$(date +%s)"
  ok "백업 완료: coordination/config.yml.bak.*"
fi

if [[ "$TRACKED_FILES" -le 5 ]]; then
  MODE="new"
  ok "신규 프로젝트 (tracked files: $TRACKED_FILES)"
else
  MODE="existing"
  ok "기존 프로젝트 (tracked files: $TRACKED_FILES)"
fi

echo ""

# === 대화식 wizard ===
echo -e "${B}─── 1. 프로젝트 메타 ───${D}"
DEFAULT_NAME=$(basename "$PROJECT_ROOT" | tr -c 'a-zA-Z0-9-' '-' | sed 's/--*/-/g; s/^-//; s/-$//')
PROJECT_NAME=$(ask "프로젝트 코드명 (worktree 디렉토리에 사용: ../<name>-wt-<sub>/)" "$DEFAULT_NAME")

DEFAULT_BRANCH=$(git branch --show-current 2>/dev/null || echo "main")
WORK_BRANCH=$(ask "작업 통합 브랜치 (op 가 머지하는 정본)" "$DEFAULT_BRANCH")
STABLE_BRANCH=$(ask "안정/배포 브랜치 (PR 타겟)" "main")

REMOTE=$(ask "원격 이름" "origin")

echo ""
echo -e "${B}─── 2. Sub 구성 ───${D}"
echo "  기본 sub: db (3001), backend (3002), frontend (3003)"
echo "  나중에 coordination/config.yml 직접 편집해서 추가/제거 가능"
USE_DEFAULT_SUBS=true
if ! ask_yn "기본 3-sub 체제 사용?" "y"; then
  USE_DEFAULT_SUBS=false
  warn "Custom sub 구성은 init 후 coordination/config.yml 직접 편집"
fi

echo ""
echo -e "${B}─── 3. 알림 (선택) ───${D}"
DISCORD_WEBHOOK=$(ask "Discord webhook URL (없으면 엔터)" "")

echo ""
echo -e "${B}─── 4. 토큰 예산 ───${D}"
WEEKLY_LIMIT=$(ask "주간 토큰 예산" "5000000")

echo ""
echo -e "${B}─── 5. 대시보드 ───${D}"
DASHBOARD_PORT=$(ask "대시보드 포트" "8888")

# === config.yml 생성 ===
echo ""
say "coordination/config.yml 생성..."

cat > coordination/config.yml <<EOF
# coord config — $(date -Iseconds) 생성됨
# 수정 가능; subs[] 추가/제거 가능

project:
  name: $PROJECT_NAME

git:
  work_branch: $WORK_BRANCH
  stable_branch: $STABLE_BRANCH
  remote: $REMOTE

subs:
EOF

if $USE_DEFAULT_SUBS; then
  cat >> coordination/config.yml <<'EOF'
  - name: db
    port: 3001
    model: claude-sonnet-4-6
    scope_allowed:
      - "db/**"
      # 프로젝트별 추가 (예: supabase/**, lib/db/**, types/db.ts)
    validation: "echo 'add validation cmd'"

  - name: backend
    port: 3002
    model: claude-opus-4-7
    scope_allowed:
      - "src/**"
      # 프로젝트별 추가 (예: core/**, utils/**)
    validation: "echo 'add validation cmd'"

  - name: frontend
    port: 3003
    model: claude-sonnet-4-6
    scope_allowed:
      - "app/**"
      - "components/**"
      # 프로젝트별 추가 (예: hooks/**, stores/**)
    validation: "echo 'add validation cmd'"
EOF
else
  cat >> coordination/config.yml <<'EOF'
  # init 후 직접 편집해서 sub 추가
  # 예시:
  # - name: <sub-name>
  #   port: <port>
  #   model: claude-sonnet-4-6
  #   scope_allowed: ["path/**"]
  #   validation: "<cmd>"
EOF
fi

cat >> coordination/config.yml <<EOF

head:
  port: 3099
  model: claude-opus-4-7
  branch: wt/head

op:
  port: 3000

notifications:
  discord_webhook_env: DISCORD_WEBHOOK_URL
  token_thresholds: [10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 96, 97, 98, 99]
  weekly_reset_day: monday
  weekly_reset_hour_kst: 0

token_budget:
  weekly_limit: $WEEKLY_LIMIT

judiciary:
  enabled: true
  judge_model: claude-sonnet-4-6
  approval_ttl_minutes: 1440

dashboard:
  enabled: true
  port: $DASHBOARD_PORT
  external: trycloudflare

merge:
  to_work_branch: auto
  to_stable_branch: manual
EOF

ok "coordination/config.yml 생성"

# === .env.local (v0.39+: PROJECT_ROOT/.env.local 에 생성 — 관습 + 단일 source of truth) ===
# v0.38 은 subdir 모드에서 .coord/.env.local 에 생성했으나 Windows cp fallback 으로 stale
# 문제가 발생해 반전. .coord/.env.local 쓰려면 scripts/link-env-to-coord.sh 로 symlink.
say ".env.local 점검..."
ENV_LOCAL="$PROJECT_ROOT/.env.local"
ENV_COORD="$COORD_ROOT/.env.local"
ENV_EXAMPLE="$COORD_ROOT/.env.local.example"

if [[ -f "$ENV_LOCAL" ]]; then
  warn ".env.local 이미 있음 — 변경 안 함 ($ENV_LOCAL)"
elif [[ -f "$ENV_COORD" ]]; then
  # v0.38 잔재 — .coord/.env.local 에 파일이 있지만 PROJECT_ROOT 는 비어있음
  warn ".coord/.env.local 에만 파일 존재 (v0.38 잔재): $ENV_COORD"
  warn "  v0.39+ 권장: PROJECT_ROOT 에 두고 필요 시 symlink."
  warn "  원복: mv \"$ENV_COORD\" \"$ENV_LOCAL\""
  warn "  isolation 유지: bash $COORD_ROOT/scripts/link-env-to-coord.sh"
  warn "  (그대로 둬도 scripts 는 .coord/.env.local fallback 으로 계속 동작)"
elif [[ -f "$ENV_EXAMPLE" ]]; then
  cp "$ENV_EXAMPLE" "$ENV_LOCAL"
  if [[ -n "$DISCORD_WEBHOOK" ]]; then
    sed -i.bak "s|^DISCORD_WEBHOOK_URL=.*|DISCORD_WEBHOOK_URL=$DISCORD_WEBHOOK|" "$ENV_LOCAL"
    rm -f "$ENV_LOCAL.bak"
    ok ".env.local 생성 + Discord webhook 설정 ($ENV_LOCAL)"
  else
    ok ".env.local 생성 (Discord webhook 미설정): $ENV_LOCAL"
  fi
else
  warn ".env.local.example 없음 — .env.local 생성 skip"
fi

# === coordination/ 골격 ===
say "coordination/ 디렉토리 골격..."
for d in inbox plans tasks reports review-inbox escalations approvals inbox/attachments; do
  mkdir -p "coordination/$d"
  [[ ! -f "coordination/$d/.gitkeep" ]] && touch "coordination/$d/.gitkeep"
done

# token-budget.md, hotfixes.md 첫 생성
if [[ ! -f "coordination/token-budget.md" ]] && [[ -f "coordination/token-budget.md.example" ]]; then
  cp coordination/token-budget.md.example coordination/token-budget.md
  # week_start_ts 채우기 (이번 주 월요일 00:00 KST)
  THIS_MONDAY=$(python3 -c "
from datetime import datetime, timedelta, timezone
kst = timezone(timedelta(hours=9))
now = datetime.now(kst)
monday = (now - timedelta(days=now.weekday())).replace(hour=0, minute=0, second=0, microsecond=0)
print(monday.isoformat())
" 2>/dev/null || echo "$(date -Iseconds)")
  sed -i.bak "s|^- \*\*week_start_ts\*\*:.*|- **week_start_ts**: $THIS_MONDAY|" coordination/token-budget.md 2>/dev/null || true
  rm -f coordination/token-budget.md.bak
  ok "coordination/token-budget.md 초기화"
fi

if [[ ! -f "coordination/hotfixes.md" ]] && [[ -f "coordination/hotfixes.md.example" ]]; then
  cp coordination/hotfixes.md.example coordination/hotfixes.md
  ok "coordination/hotfixes.md 초기화"
fi

# .coord-version — upgrade-coord.sh 가 이후 이 값을 참조 (v0.3+)
if [[ ! -f "$COORD_ROOT/coordination/.coord-version" ]]; then
  # VERSION 파일 (템플릿 repo 가 각 릴리스마다 갱신) 우선. 없으면 unknown.
  if [[ -f "$COORD_ROOT/VERSION" ]]; then
    COORD_VERSION="$(head -1 "$COORD_ROOT/VERSION" | tr -d '[:space:]')"
  else
    COORD_VERSION="v0.0-unknown"
  fi
  COORD_URL="https://github.com/gone7729/coord-template.git"
  cat > "$COORD_ROOT/coordination/.coord-version" <<EOF
# coord-template 버전 메타데이터
# init.sh / upgrade-coord.sh 가 갱신 — 수동 편집 불필요
version: $COORD_VERSION
upgraded_at: $(date -Iseconds 2>/dev/null || date +%Y-%m-%dT%H:%M:%S%z)
upgraded_from: none
template_url: $COORD_URL
install_mode: $INSTALL_MODE
EOF
  ok "coordination/.coord-version 초기화 ($COORD_VERSION, mode=$INSTALL_MODE)"
fi

# === .gitignore 보강 (PROJECT_ROOT 의 .gitignore) ===
# v0.41+: .coord/ 통째 ignore 반전 — workflow 가 .coord/coordination/* 를
# git sync 로 통신하므로 통째 ignore 하면 inbox-send / review / 에스컬레이션 전부
# 동작 불능. 배포 청결은 .gitattributes 의 export-ignore 로 별도 달성.
# runtime 파일 + 사용자 secrets 만 개별 ignore.
say ".gitignore 보강 (PROJECT_ROOT)..."
GITIGNORE_FILE="$PROJECT_ROOT/.gitignore"
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  GITIGNORE_ENTRIES=(
    "# === coord-template runtime (v0.41+: 통째 ignore 는 workflow 파괴, 구체 파일만) ==="
    ".coord/coordination/HEAD_LOCK"
    ".coord/coordination/STOP"
    ".coord/coordination/dashboard-url.txt"
    ".coord/.env.local"
    ".coord/bot/projects.yml"
    ".coord/bot/usage-alerts-state.json"
    ".coord/.claude/settings.local.json"
    ".coord-backup/"
    ".env.local"
    ".env.local.wt"
    "run-dev.sh"
    "coord"
  )
else
  # flat 모드 (deprecated — coord-template repo 본체에서만 사용)
  GITIGNORE_ENTRIES=(
    "coordination/HEAD_LOCK"
    "coordination/dashboard-url.txt"
    "coordination/STOP"
    ".coord-backup/"
    ".env.local"
    ".env.local.wt"
    "run-dev.sh"
    ".claude/settings.local.json"
  )
fi
[[ ! -f "$GITIGNORE_FILE" ]] && touch "$GITIGNORE_FILE"
ADDED=0
for entry in "${GITIGNORE_ENTRIES[@]}"; do
  if ! grep -qxF "$entry" "$GITIGNORE_FILE" 2>/dev/null; then
    echo "$entry" >> "$GITIGNORE_FILE"
    ADDED=$((ADDED+1))
  fi
done
ok ".gitignore 보강 ($ADDED개 항목 추가 → $GITIGNORE_FILE)"

# === .gitattributes 설정 (v0.41+ export-ignore / v0.46+ merge=ours) ===
# - export-ignore: git archive 시 .coord/ 제외 (배포 tarball 청결)
# - merge=ours: work_branch ↔ main 머지 시 .coord/ 가 "현재 체크아웃 브랜치" 쪽으로 유지
#   → main 체크아웃에서 work 머지 시: main 쪽 (없음) 유지 → 자동 드롭
#   → work 체크아웃에서 main 머지 시 (back-merge): work 쪽 (있음) 유지 → 보존
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  GA_FILE="$PROJECT_ROOT/.gitattributes"
  [[ ! -f "$GA_FILE" ]] && touch "$GA_FILE"
  GA_ENTRIES=(
    ".coord/ export-ignore"
    ".coord/** merge=ours"
    ".claude/** merge=ours"
  )
  GA_ADDED=0
  for entry in "${GA_ENTRIES[@]}"; do
    if ! grep -qxF "$entry" "$GA_FILE" 2>/dev/null; then
      echo "$entry" >> "$GA_FILE"
      GA_ADDED=$((GA_ADDED+1))
    fi
  done
  [[ $GA_ADDED -gt 0 ]] && ok ".gitattributes $GA_ADDED 항목 추가 (export-ignore + merge=ours)"
fi

# === git hooks (INSTALL_MODE 별 경로) ===
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  HOOKS_REL=".coord/.githooks"
else
  HOOKS_REL=".githooks"
fi
say "git hooks 설치 (core.hooksPath = $HOOKS_REL)..."
if [[ -d "$PROJECT_ROOT/$HOOKS_REL" ]]; then
  (cd "$PROJECT_ROOT" && git config core.hooksPath "$HOOKS_REL")
  ok "hooks 활성 → $HOOKS_REL"
else
  warn "$HOOKS_REL 없음 — hooks 설치 생략"
fi

# === .claude/ 세팅 (PROJECT_ROOT 에 위치 — Claude Code 제약) ===
# subdir 모드: 템플릿은 .coord/.claude 에 있음, PROJECT_ROOT/.claude 로 symlink (v0.37+).
#             symlink 실패 시 (Windows 개발자 모드 미활성) cp fallback.
# flat 모드: PROJECT_ROOT = COORD_ROOT 이라 이미 .claude 가 제자리
say "Claude Code settings 점검..."
CLAUDE_SRC="$COORD_ROOT/.claude"
CLAUDE_DST="$PROJECT_ROOT/.claude"

if [[ "$INSTALL_MODE" == "subdir" ]] && [[ -d "$CLAUDE_SRC" ]] && [[ ! -e "$CLAUDE_DST" ]]; then
  if ln -s "$CLAUDE_SRC" "$CLAUDE_DST" 2>/dev/null; then
    ok "  .claude symlink → $CLAUDE_SRC"
  else
    say "  .coord/.claude → $CLAUDE_DST cp (symlink 실패 — Windows 개발자 모드 미활성)"
    mkdir -p "$CLAUDE_DST"
    cp -r "$CLAUDE_SRC/"* "$CLAUDE_DST/" 2>/dev/null || true
    ok "  .claude 초기 동기화 (cp fallback)"
  fi
fi

if [[ ! -f "$CLAUDE_DST/settings.local.json" ]] && [[ -f "$CLAUDE_DST/settings.local.json.example" ]]; then
  if ask_yn "Claude Code 권한 + 사법부 hook 자동 설치 (.claude/settings.local.json)?" "y"; then
    cp "$CLAUDE_DST/settings.local.json.example" "$CLAUDE_DST/settings.local.json"
    ok ".claude/settings.local.json 생성 (사법부 PreToolUse 활성)"
  else
    warn "수동 설정: $CLAUDE_DST/settings.local.json.example 참조"
  fi
elif [[ -f "$CLAUDE_DST/settings.local.json" ]]; then
  warn ".claude/settings.local.json 이미 있음 — 변경 안 함 (.example 참조해서 hook 추가)"
fi

# === 권한 부여 ===
chmod +x scripts/*.sh 2>/dev/null || true

# === root wrapper 생성 (v0.39+, subdir 모드만, 선택) ===
# PROJECT_ROOT 에서 `./coord start-bot.sh --bg` 같이 간단히 호출 가능하게.
# gitignored (gitignore 에 이미 있음). subtree pull 영향 없음.
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  WRAPPER="$PROJECT_ROOT/coord"
  if [[ ! -f "$WRAPPER" ]]; then
    if ask_yn "root wrapper 생성? (PROJECT_ROOT 에서 './coord <script>' 호출용)" "y"; then
      cat > "$WRAPPER" <<'WRAPPER_EOF'
#!/usr/bin/env bash
# coord wrapper (init.sh 가 생성, gitignored)
# 사용:
#   ./coord start-bot.sh --bg
#   ./coord status.sh
#   ./coord install-deps-mac.sh --yes
# 동작:
#   1. PROJECT_ROOT/.env.local 자동 source (있으면)
#   2. .coord/scripts/<arg1> 에 나머지 인자 전달
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
if [[ -f "$HERE/.env.local" ]]; then
  set -a; source "$HERE/.env.local"; set +a
fi
if [[ $# -eq 0 ]]; then
  echo "사용: ./coord <script-name> [args...]"
  echo "사용 가능한 스크립트:"
  ls "$HERE/.coord/scripts/"*.sh 2>/dev/null | xargs -n1 basename | sed 's/^/  /'
  exit 1
fi
SCRIPT="$HERE/.coord/scripts/$1"
shift
if [[ ! -f "$SCRIPT" ]]; then
  echo "❌ 스크립트 없음: $SCRIPT"
  exit 1
fi
exec bash "$SCRIPT" "$@"
WRAPPER_EOF
      chmod +x "$WRAPPER"
      ok "root wrapper 생성: $WRAPPER"
      echo "  사용 예: ./coord start-bot.sh --bg"
    else
      warn "root wrapper skip — 직접 호출: bash .coord/scripts/<script>.sh"
    fi
  else
    warn "root wrapper 이미 존재 — 변경 안 함 ($WRAPPER)"
  fi
fi

# === Sub worktree 일괄 생성 (선택) ===
echo ""
echo -e "${B}─── 6. Sub worktree 자동 생성 (선택) ───${D}"
SUB_LIST=$(python3 -c "
import yaml
with open('coordination/config.yml', encoding='utf-8') as f:
    d = yaml.safe_load(f)
for s in d.get('subs', []):
    print(f\"{s['name']}\\t{s.get('port', 3001)}\")
" 2>/dev/null || echo "")

if [[ -z "$SUB_LIST" ]]; then
  warn "config.yml 에 sub 정의 없음 — worktree 생성 건너뜀"
elif ! git show-ref --verify --quiet "refs/heads/$WORK_BRANCH"; then
  warn "work_branch '$WORK_BRANCH' 로컬에 없음"
  echo "  먼저 브랜치 생성 후 수동으로 각 sub 에 대해:"
  echo "    \$ bash scripts/setup-worktree.sh <name> <port> $WORK_BRANCH"
else
  # sub 목록 미리보기
  echo "  생성 예정:"
  while IFS=$'\t' read -r sub_name sub_port; do
    [[ -z "$sub_name" ]] && continue
    echo "    - $PROJECT_NAME-wt-$sub_name (포트 $sub_port, base: $WORK_BRANCH)"
  done <<< "$SUB_LIST"
  echo ""

  if ask_yn "모든 sub + head worktree 를 지금 생성?" "y"; then
    CREATED=0
    SKIPPED=0
    FAILED=0

    # head worktree (port 3099)
    HEAD_WT="$(dirname "$PROJECT_ROOT")/$PROJECT_NAME-wt-head"
    if [[ -d "$HEAD_WT" ]]; then
      warn "이미 있음: $HEAD_WT (skip)"
      SKIPPED=$((SKIPPED+1))
    else
      say "생성: head (포트 3099) ..."
      if bash scripts/setup-worktree.sh head 3099 "$WORK_BRANCH" >/dev/null 2>&1; then
        ok "head worktree 생성"
        CREATED=$((CREATED+1))
      else
        err "head 생성 실패"
        FAILED=$((FAILED+1))
      fi
    fi

    # sub worktree 각각
    while IFS=$'\t' read -r sub_name sub_port; do
      [[ -z "$sub_name" ]] && continue
      SUB_WT="$(dirname "$PROJECT_ROOT")/$PROJECT_NAME-wt-$sub_name"
      if [[ -d "$SUB_WT" ]]; then
        warn "이미 있음: $SUB_WT (skip)"
        SKIPPED=$((SKIPPED+1))
        continue
      fi
      say "생성: $sub_name (포트 $sub_port) ..."
      if bash scripts/setup-worktree.sh "$sub_name" "$sub_port" "$WORK_BRANCH" >/dev/null 2>&1; then
        ok "$sub_name worktree 생성"
        CREATED=$((CREATED+1))
      else
        err "$sub_name 생성 실패"
        FAILED=$((FAILED+1))
      fi
    done <<< "$SUB_LIST"

    echo ""
    ok "worktree 요약: 생성 $CREATED / 기존 유지 $SKIPPED / 실패 $FAILED"
    echo ""
    warn "각 worktree 에서 빌드 도구 의존성 설치 필요 (npm/pnpm/poetry 등)"
    echo "     예시: cd ../$PROJECT_NAME-wt-<sub> && npm install"
  else
    warn "수동 생성: bash scripts/setup-worktree.sh <name> <port> $WORK_BRANCH"
  fi
fi

# === 마무리 안내 ===
echo ""
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo -e "${G}  ✅ coord 초기화 완료${D}"
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo ""
echo -e "${B}프로젝트:${D} $PROJECT_NAME ($INSTALL_MODE 모드)"
echo -e "${B}브랜치:${D} $WORK_BRANCH (work) / $STABLE_BRANCH (stable)"
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  echo -e "${B}설정:${D} .coord/coordination/config.yml"
  echo -e "${B}COORD_ROOT:${D} $COORD_ROOT"
  echo -e "${B}PROJECT_ROOT:${D} $PROJECT_ROOT"
else
  echo -e "${B}설정:${D} coordination/config.yml"
fi
echo ""
echo -e "${B}다음 단계${D}"
echo ""
echo "  ${C}1.${D} config 검토 + 수정 (sub scope_allowed 등):"
echo "     \$ \$EDITOR coordination/config.yml"
echo ""
echo "  ${C}2.${D} 각 worktree 에서 빌드 의존성 설치 (프로젝트별 도구):"
echo "     \$ cd ../$PROJECT_NAME-wt-<sub> && npm install (또는 pnpm/poetry 등)"
echo ""
echo "  ${C}3.${D} 메인 repo 루트에서 Claude 세션 시작 → 첫 작업 지시:"
echo "     \$ claude"
echo "     /inbox-send 첫 요청 — ..."
echo ""
echo "  ${C}4.${D} (선택) 대시보드 기동:"
echo "     \$ bash scripts/start-dashboard.sh &"
echo "     \$ open http://localhost:$DASHBOARD_PORT"
echo ""
echo "  ${C}5.${D} 외부 접근 (선택, 모바일/팀 공유):"
echo "     \$ cloudflared tunnel --url http://localhost:$DASHBOARD_PORT &"
echo "     \$ bash scripts/update-dashboard-url.sh &"
echo ""

if $HAS_CONFIG; then
  echo -e "${Y}백업:${D} 기존 config 는 coordination/config.yml.bak.* 로 보관됨"
  echo ""
fi

if [[ "$MODE" == "existing" ]]; then
  echo -e "${Y}TIP:${D} 기존 프로젝트라면 CLAUDE.md 에 coord 운영 규약 추가 권장"
  echo "     ($PROJECT_ROOT/CLAUDE.md 에 '병렬 Worktree 작업 규칙' 섹션 등)"
  echo ""
fi

echo -e "${B}문서:${D} README.md, INSTALL.md, coordination/USAGE.md"
echo ""
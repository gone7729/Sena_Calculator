#!/usr/bin/env bash
# upgrade-coord.sh — coord-template 업그레이드 자동화 (v0.3, Phase 5-8)
#
# init.sh 로 설치된 프로젝트에서 **사용자 설정/런타임 상태를 깨지 않고**
# 최신 coord-template 을 받아 파일 분류 규칙에 따라 적용한다.
#
# 사용법:
#   bash scripts/upgrade-coord.sh [--version vX.Y] [--mode MODE] [--repo URL]
#                                  [--accept-commands] [--accept-roles] [--accept-all]
#                                  [--dry-run] [--skip-backup] [--force]
#
# --version     업스트림 버전 (예: v0.3). 생략 시 "latest" 로 gh release 조회.
# --mode        tarball | git | subtree | auto (기본: auto)
# --repo        템플릿 repo URL (기본: .coord-version 의 template_url)
# --accept-*    manual-merge 자동 승인 (commands / roles / all)
# --dry-run     실제 쓰기 없이 계획만 출력
# --skip-backup 백업 단계 건너뜀 (위험 — 복구 불가)
# --force       git clean / STOP 시그널 체크 건너뜀
#
# 동작 (10 단계):
#   1) 사전 점검 (git clean, work_branch, STOP, config.yml)
#   2) 모드 감지 (auto 시 .coord/ subtree / .coord-version / tarball 순)
#   3) 소스 획득 (tarball 다운로드 / git clone / subtree pull)
#   4) 백업 (.coord-backup/<ts>/)
#   5) 분류별 적용 (overwrite / preserve / manual-merge)
#   6) .claude/ 재동기화 (scripts/lib/sync-claude.sh)
#   7) config.yml 신규 필드 마이그레이션 제안
#   8) .coord-version 갱신
#   9) CHANGELOG 출력 (이전 버전 ~ 신버전)
#  10) 사후 점검 (bash -n, pre-commit hook)

set -euo pipefail

# === 색상 / 로그 ===
B=$'\033[1m'; D=$'\033[0m'; G=$'\033[32m'; Y=$'\033[33m'; R=$'\033[31m'; C=$'\033[36m'
say()  { echo -e "${C}▶${D} $*"; }
ok()   { echo -e "${G}✓${D} $*"; }
warn() { echo -e "${Y}⚠${D} $*"; }
err()  { echo -e "${R}✗${D} $*" >&2; }

# === 위치 결정 (v0.4+: COORD_ROOT / PROJECT_ROOT 분리) ===
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COORD_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
if [[ "$(basename "$COORD_ROOT")" == ".coord" ]]; then
  PROJECT_ROOT="$(cd "$COORD_ROOT/.." && pwd)"
  INSTALL_MODE="subdir"
else
  PROJECT_ROOT="$COORD_ROOT"
  INSTALL_MODE="flat"
fi
# REPO_ROOT 는 호환용 alias (이전 v0.3 코드에서 사용). 기본은 COORD_ROOT.
REPO_ROOT="$COORD_ROOT"
cd "$COORD_ROOT"

# === lib 로드 ===
LIB_DIR="$SCRIPT_DIR/lib"
[[ -f "$LIB_DIR/classify.sh" ]] || { err "$LIB_DIR/classify.sh 없음 — 템플릿 파일 누락"; exit 1; }
[[ -f "$LIB_DIR/sync-claude.sh" ]] || { err "$LIB_DIR/sync-claude.sh 없음"; exit 1; }
# shellcheck source=lib/classify.sh
source "$LIB_DIR/classify.sh"
# shellcheck source=lib/sync-claude.sh
source "$LIB_DIR/sync-claude.sh"

# === 기본값 / 플래그 파싱 ===
VERSION=""
MODE="auto"
REPO_URL=""
ACCEPT_COMMANDS=0
ACCEPT_ROLES=0
DRY_RUN=0
SKIP_BACKUP=0
FORCE=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) VERSION="$2"; shift 2 ;;
    --mode)    MODE="$2"; shift 2 ;;
    --repo)    REPO_URL="$2"; shift 2 ;;
    --accept-commands) ACCEPT_COMMANDS=1; shift ;;
    --accept-roles)    ACCEPT_ROLES=1; shift ;;
    --accept-all)      ACCEPT_COMMANDS=1; ACCEPT_ROLES=1; shift ;;
    --dry-run)    DRY_RUN=1; shift ;;
    --skip-backup) SKIP_BACKUP=1; shift ;;
    --force)      FORCE=1; shift ;;
    -h|--help)
      # 파일 상단 헤더 블록만 (shebang 다음 연속된 주석 라인)
      awk '
        NR==1 && /^#!/ { next }
        /^#/ { sub(/^# ?/, ""); print; next }
        { exit }
      ' "$0"
      exit 0 ;;
    *) err "알 수 없는 옵션: $1"; exit 2 ;;
  esac
done

export ACCEPT_COMMANDS DRY_RUN  # sync-claude.sh 가 참조

echo ""
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo -e "${B}  coord-template 업그레이드${D}"
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo ""

# === 1. 사전 점검 ===
say "1/10 사전 점검..."

if ! git rev-parse --git-dir >/dev/null 2>&1; then
  err "git repo 아님"; exit 1
fi

# config.yml 확인 (COORD_ROOT 기준)
if [[ ! -f "$COORD_ROOT/coordination/config.yml" ]]; then
  err "$COORD_ROOT/coordination/config.yml 없음 — 먼저 init.sh 실행"; exit 1
fi

# work_branch 확인 + git clean 체크 (force 면 스킵)
CURRENT_BRANCH="$(git branch --show-current 2>/dev/null || echo "")"
# Windows Git Bash 대응: Python 에 절대경로 넘길 때 cygpath 필요
_to_native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1" 2>/dev/null || echo "$1"
  else echo "$1"
  fi
}
WORK_BRANCH="$(python3 -c "
import yaml
with open(r'$(_to_native "$COORD_ROOT/coordination/config.yml")', encoding='utf-8') as f:
    d = yaml.safe_load(f)
print(d.get('git', {}).get('work_branch', ''))
" 2>/dev/null)"

if [[ "$FORCE" -eq 0 ]]; then
  if [[ -n "$WORK_BRANCH" ]] && [[ "$CURRENT_BRANCH" != "$WORK_BRANCH" ]]; then
    warn "현재 브랜치: $CURRENT_BRANCH (work_branch: $WORK_BRANCH)"
    warn "업그레이드는 work_branch 에서 수행 권장. --force 로 무시 가능."
    exit 1
  fi
  if ! git diff --quiet HEAD 2>/dev/null || ! git diff --cached --quiet 2>/dev/null; then
    err "uncommitted 변경 있음. 커밋/stash 후 재시도 (--force 로 무시 가능)"
    git status --short | head -20 >&2
    exit 1
  fi
  if [[ -f "$COORD_ROOT/coordination/STOP" ]]; then
    err "$COORD_ROOT/coordination/STOP 시그널 존재 — 먼저 해제 후 진행"
    exit 1
  fi
fi

# 현재 버전 읽기
CURRENT_VERSION="unknown"
TEMPLATE_URL_FROM_FILE=""
CURRENT_INSTALL_MODE="$INSTALL_MODE"
if [[ -f "$COORD_ROOT/coordination/.coord-version" ]]; then
  CURRENT_VERSION="$(grep -E '^version:' "$COORD_ROOT/coordination/.coord-version" | sed 's/^version:[[:space:]]*//' | head -1)"
  TEMPLATE_URL_FROM_FILE="$(grep -E '^template_url:' "$COORD_ROOT/coordination/.coord-version" | sed 's/^template_url:[[:space:]]*//' | head -1)"
  CURRENT_INSTALL_MODE="$(grep -E '^install_mode:' "$COORD_ROOT/coordination/.coord-version" | sed 's/^install_mode:[[:space:]]*//' | head -1)"
fi
[[ -z "$REPO_URL" ]] && REPO_URL="${TEMPLATE_URL_FROM_FILE:-https://github.com/gone7729/coord-template.git}"

ok "현재 버전: $CURRENT_VERSION ($INSTALL_MODE 모드)"
ok "업스트림: $REPO_URL"

# === 2. 모드 감지 ===
say "2/10 모드 감지..."

if [[ "$MODE" == "auto" ]]; then
  # subtree prefix 감지: git log --grep 로 subtree commit 찾기 (간이)
  if [[ -d ".coord" ]] && git log --max-count=1 --grep="git-subtree-dir: .coord" --oneline >/dev/null 2>&1 \
     && [[ -n "$(git log --grep="git-subtree-dir: .coord" --oneline 2>/dev/null | head -1)" ]]; then
    MODE="subtree"
  elif command -v gh >/dev/null 2>&1; then
    MODE="tarball"
  elif command -v git >/dev/null 2>&1; then
    MODE="git"
  else
    err "gh / git 둘 다 없음"; exit 1
  fi
fi
ok "모드: $MODE"

# 버전 해석 (latest 는 gh release view 로 확정)
if [[ -z "$VERSION" ]] || [[ "$VERSION" == "latest" ]]; then
  if [[ "$MODE" == "tarball" ]] && command -v gh >/dev/null 2>&1; then
    VERSION="$(gh release view --json tagName -q .tagName --repo "${REPO_URL#https://github.com/}" 2>/dev/null | sed 's|\.git$||' || echo "")"
  fi
  [[ -z "$VERSION" ]] && { err "버전 자동 감지 실패 — --version 으로 명시"; exit 1; }
fi
ok "업그레이드 대상: $CURRENT_VERSION → $VERSION"

if [[ "$CURRENT_VERSION" == "$VERSION" ]]; then
  warn "현재 버전과 동일. 계속? (y/n)"
  read -r ans </dev/tty
  [[ "$ans" =~ ^[Yy]$ ]] || exit 0
fi

# === 3. 소스 획득 ===
say "3/10 소스 획득..."

TMP_DIR=""
cleanup() {
  [[ -n "$TMP_DIR" ]] && [[ -d "$TMP_DIR" ]] && rm -rf "$TMP_DIR"
}
trap cleanup EXIT

case "$MODE" in
  tarball)
    TMP_DIR="$(mktemp -d)"
    REPO_SLUG="${REPO_URL#https://github.com/}"; REPO_SLUG="${REPO_SLUG%.git}"
    say "  gh release download $VERSION from $REPO_SLUG ..."
    if ! gh release download "$VERSION" --repo "$REPO_SLUG" --archive=tar.gz -O "$TMP_DIR/src.tar.gz" 2>/dev/null; then
      # archive=tar.gz 미지원 구버전 gh 용 fallback
      warn "archive 다운로드 실패 — git clone fallback"
      MODE="git"
    else
      tar -xzf "$TMP_DIR/src.tar.gz" -C "$TMP_DIR"
      SRC_DIR="$(find "$TMP_DIR" -maxdepth 2 -type d -name 'coord-template-*' | head -1)"
      [[ -z "$SRC_DIR" ]] && SRC_DIR="$(find "$TMP_DIR" -mindepth 1 -maxdepth 1 -type d | head -1)"
      [[ -z "$SRC_DIR" ]] && { err "tarball 펼침 실패"; exit 1; }
      ok "  tarball 추출: $SRC_DIR"
    fi
    ;;
esac

if [[ "$MODE" == "git" ]]; then
  [[ -z "$TMP_DIR" ]] && TMP_DIR="$(mktemp -d)"
  SRC_DIR="$TMP_DIR/coord-template"
  say "  git clone --depth 1 -b $VERSION $REPO_URL ..."
  if ! git clone --depth 1 --branch "$VERSION" "$REPO_URL" "$SRC_DIR" 2>/dev/null; then
    err "git clone 실패 — 버전 태그 $VERSION 존재 확인"; exit 1
  fi
  ok "  clone 완료"
elif [[ "$MODE" == "subtree" ]]; then
  say "  git subtree pull 수행..."
  if [[ "$DRY_RUN" -eq 1 ]]; then
    warn "[dry-run] git subtree pull --prefix=.coord $REPO_URL $VERSION --squash"
  else
    git subtree pull --prefix=.coord "$REPO_URL" "$VERSION" --squash \
      || { err "subtree pull 실패 — 수동 충돌 해결 필요"; exit 1; }
  fi
  SRC_DIR="$REPO_ROOT/.coord"
  [[ ! -d "$SRC_DIR" ]] && { err "subtree 후 .coord/ 없음"; exit 1; }
  ok "  subtree pull 완료"
fi

# === 4. 백업 ===
TS="$(date +%Y-%m-%dT%H-%M-%S)"
BACKUP_DIR="$REPO_ROOT/.coord-backup/$TS"
if [[ "$SKIP_BACKUP" -eq 1 ]]; then
  warn "4/10 백업 스킵 (--skip-backup)"
  BACKUP_DIR=""
else
  say "4/10 백업: .coord-backup/$TS/"
  if [[ "$DRY_RUN" -eq 1 ]]; then
    warn "[dry-run] 백업 생성"
  else
    mkdir -p "$BACKUP_DIR"
    {
      echo "# coord-template upgrade backup"
      echo ""
      echo "- timestamp: $TS"
      echo "- from: $CURRENT_VERSION"
      echo "- to: $VERSION"
      echo "- mode: $MODE"
      echo "- source: $REPO_URL"
      echo ""
      echo "## 복구"
      echo ""
      echo 'rsync -a .coord-backup/'"$TS"'/ ./'
    } > "$BACKUP_DIR/MANIFEST.md"
    ok "  백업 MANIFEST 작성"
  fi
fi
export BACKUP_DIR

# === 5. 분류별 적용 ===
say "5/10 분류별 적용..."

STATS_OW=0; STATS_PR=0; STATS_MM=0; STATS_SK=0

apply_file() {
  local src="$1" rel="$2"
  local policy; policy="$(classify_path "$rel")"
  local root_scope; root_scope="$(classify_root "$rel")"

  # subtree 모드: coord 스코프 파일은 subtree pull 이 이미 반영함 → 재적용 skip
  if [[ "$MODE" == "subtree" ]] && [[ "$root_scope" == "coord" ]]; then
    STATS_SK=$((STATS_SK+1))
    return
  fi

  local target_root
  if [[ "$root_scope" == "project" ]]; then
    target_root="$PROJECT_ROOT"
  else
    target_root="$COORD_ROOT"
  fi
  local dst="$target_root/$rel"
  local dst_dir; dst_dir="$(dirname "$dst")"

  # manual-merge 중 roles/head.md 는 --accept-roles 플래그 별도
  case "$rel" in
    coordination/roles/head.md)
      if [[ "$policy" == "manual-merge" ]] && [[ "$ACCEPT_ROLES" -eq 1 ]]; then
        policy="overwrite"
      fi ;;
    .claude/commands/*)
      if [[ "$policy" == "manual-merge" ]] && [[ "$ACCEPT_COMMANDS" -eq 1 ]]; then
        policy="overwrite"
      fi ;;
  esac

  case "$policy" in
    skip)
      STATS_SK=$((STATS_SK+1)) ;;
    preserve)
      STATS_PR=$((STATS_PR+1)) ;;
    overwrite)
      if [[ "$DRY_RUN" -eq 1 ]]; then
        echo "    [dry-run] overwrite: $rel"
      else
        [[ -n "$BACKUP_DIR" ]] && [[ -f "$dst" ]] && {
          mkdir -p "$BACKUP_DIR/$(dirname "$rel")"
          cp -p "$dst" "$BACKUP_DIR/$rel"
        }
        mkdir -p "$dst_dir"
        cp -p "$src" "$dst"
      fi
      STATS_OW=$((STATS_OW+1)) ;;
    manual-merge)
      if [[ ! -f "$dst" ]]; then
        # 로컬에 없으면 신규 추가
        if [[ "$DRY_RUN" -eq 1 ]]; then
          echo "    [dry-run] add new: $rel"
        else
          mkdir -p "$dst_dir"
          cp -p "$src" "$dst"
        fi
        STATS_OW=$((STATS_OW+1))
      elif cmp -s "$src" "$dst" 2>/dev/null; then
        : # 동일 → 무시
      elif [[ "$DRY_RUN" -eq 1 ]]; then
        echo "    [dry-run] manual-merge prompt: $rel"
        STATS_MM=$((STATS_MM+1))
      elif [[ ! -e /dev/tty ]]; then
        warn "  manual-merge: $rel — TTY 없음, 기존 유지 (skip)"
        STATS_MM=$((STATS_MM+1))
      else
        warn "  manual-merge: $rel"
        echo "  --- 현재 vs 새 템플릿 (최대 60줄) ---"
        diff -u "$dst" "$src" 2>/dev/null | head -60 || true
        echo "  ---"
        local ans
        read -rp "  적용? [y]es / [n]o : " ans </dev/tty
        if [[ "$ans" =~ ^[Yy]$ ]]; then
          [[ -n "$BACKUP_DIR" ]] && {
            mkdir -p "$BACKUP_DIR/$(dirname "$rel")"
            cp -p "$dst" "$BACKUP_DIR/$rel"
          }
          cp -p "$src" "$dst"
        fi
        STATS_MM=$((STATS_MM+1))
      fi ;;
  esac
}

# SRC_DIR 아래 모든 파일 순회
while IFS= read -r -d '' src_file; do
  rel="${src_file#$SRC_DIR/}"
  # .claude/ 는 별도 단계 (6번) 에서 처리 — 여기서 스킵
  [[ "$rel" == .claude/* ]] && continue
  # .git/ 제외
  [[ "$rel" == .git/* ]] && continue
  apply_file "$src_file" "$rel"
done < <(find "$SRC_DIR" -type f -print0 2>/dev/null)

ok "  적용 요약: overwrite $STATS_OW / preserve $STATS_PR / manual-merge $STATS_MM / skip $STATS_SK"

# === 6. .claude/ 재동기화 ===
# v0.3 flat: SRC_DIR/.claude → PROJECT_ROOT/.claude
# v0.4 subdir subtree: COORD_ROOT/.claude (= SRC_DIR/.claude) → PROJECT_ROOT/.claude
say "6/10 .claude/ 재동기화..."
if [[ -d "$SRC_DIR/.claude" ]]; then
  BACKUP_DIR="${BACKUP_DIR:-}" sync_claude "$SRC_DIR/.claude" "$PROJECT_ROOT/.claude"
else
  warn "  .claude/ 소스 없음 — skip"
fi

# === 7. config.yml 신규 필드 마이그레이션 제안 ===
say "7/10 config.yml 신규 필드 확인..."
NEW_EXAMPLE="$SRC_DIR/coordination/config.yml.example"
CUR_CONFIG="$COORD_ROOT/coordination/config.yml"

# Git Bash on Windows: /tmp/... 경로를 Python 이 해석 못하므로 Windows 경로로 변환
# (_to_native 는 상단 1단계에서 정의됨)

if [[ -f "$NEW_EXAMPLE" ]] && [[ -f "$CUR_CONFIG" ]]; then
  MIGRATION_HINTS="$(PYTHONIOENCODING=utf-8 python3 - "$(_to_native "$NEW_EXAMPLE")" "$(_to_native "$CUR_CONFIG")" <<'PY'
import yaml, sys
new_ex_path, cur_path = sys.argv[1], sys.argv[2]
try:
    with open(new_ex_path, encoding='utf-8') as f:
        new_ex = yaml.safe_load(f) or {}
    with open(cur_path, encoding='utf-8') as f:
        cur = yaml.safe_load(f) or {}
    def walk(prefix, a, b):
        hints = []
        if isinstance(a, dict):
            for k, v in a.items():
                key = f"{prefix}.{k}" if prefix else k
                if not isinstance(b, dict) or k not in b:
                    hints.append(f"  + {key}")
                else:
                    hints.extend(walk(key, v, b.get(k)))
        return hints
    hints = walk("", new_ex, cur)
    if hints:
        print("신규 필드 (config.yml 에 추가 검토):")
        for h in hints[:20]:
            print(h)
    else:
        print("신규 필드 없음")
except Exception as e:
    print(f"  (확인 실패: {e})")
PY
)"
  echo "$MIGRATION_HINTS" | sed 's/^/  /'
fi

# === 8. .coord-version 갱신 ===
say "8/10 .coord-version 갱신..."
if [[ "$DRY_RUN" -eq 1 ]]; then
  warn "[dry-run] .coord-version → version=$VERSION, install_mode=$INSTALL_MODE"
else
  cat > "$COORD_ROOT/coordination/.coord-version" <<EOF
# coord-template 버전 메타데이터
# upgrade-coord.sh 가 갱신 — 수동 편집 불필요
version: $VERSION
upgraded_at: $(date -Iseconds 2>/dev/null || date +%Y-%m-%dT%H:%M:%S%z)
upgraded_from: $CURRENT_VERSION
template_url: $REPO_URL
install_mode: $INSTALL_MODE
EOF
  ok "  $CURRENT_VERSION → $VERSION ($INSTALL_MODE 모드)"
fi

# === 9. CHANGELOG ===
say "9/10 CHANGELOG ($CURRENT_VERSION → $VERSION)..."
if [[ "$MODE" == "git" ]] || [[ "$MODE" == "subtree" ]]; then
  if [[ -d "$SRC_DIR/.git" ]]; then
    (cd "$SRC_DIR" && git log --oneline --no-merges "${CURRENT_VERSION}..${VERSION}" 2>/dev/null | head -30 | sed 's/^/  /') \
      || warn "  git log 불가 (태그 없을 수 있음)"
  fi
elif command -v gh >/dev/null 2>&1; then
  REPO_SLUG="${REPO_URL#https://github.com/}"; REPO_SLUG="${REPO_SLUG%.git}"
  gh release view "$VERSION" --repo "$REPO_SLUG" --json body -q .body 2>/dev/null | head -40 | sed 's/^/  /' \
    || warn "  gh release view 실패"
fi

# === 10. 사후 점검 ===
say "10/10 사후 점검..."
SYNTAX_OK=1
for f in scripts/*.sh scripts/lib/*.sh; do
  [[ -f "$f" ]] || continue
  if ! bash -n "$f" 2>/dev/null; then
    err "  구문 오류: $f"
    SYNTAX_OK=0
  fi
done
[[ "$SYNTAX_OK" -eq 1 ]] && ok "  bash 구문 검증 통과"

# pre-commit hook 경로 확인 (INSTALL_MODE 별 기대값)
HOOKS_PATH="$(git config --get core.hooksPath 2>/dev/null || echo "")"
if [[ "$INSTALL_MODE" == "subdir" ]]; then
  EXPECTED_HOOKS=".coord/.githooks"
else
  EXPECTED_HOOKS=".githooks"
fi
if [[ "$HOOKS_PATH" != "$EXPECTED_HOOKS" ]]; then
  warn "  core.hooksPath = '$HOOKS_PATH' ($EXPECTED_HOOKS 예상) — 'git config core.hooksPath $EXPECTED_HOOKS' 재설정 고려"
fi

# === 완료 ===
echo ""
echo -e "${B}══════════════════════════════════════════════════════${D}"
if [[ "$DRY_RUN" -eq 1 ]]; then
  echo -e "${Y}  dry-run 완료 — 실제 적용 안 됨${D}"
else
  echo -e "${G}  ✅ 업그레이드 완료: $CURRENT_VERSION → $VERSION${D}"
fi
echo -e "${B}══════════════════════════════════════════════════════${D}"
echo ""
if [[ -n "$BACKUP_DIR" ]] && [[ "$DRY_RUN" -eq 0 ]]; then
  echo "  백업: .coord-backup/$TS/"
  echo "  롤백: rsync -a .coord-backup/$TS/ ./"
  echo ""
fi
echo "  smoke test 권장:"
echo "    \$ bash scripts/list-worktrees.sh"
echo "    \$ /inbox-send 테스트 업그레이드 확인"
echo ""

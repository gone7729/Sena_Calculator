#!/usr/bin/env bash
# sync-claude.sh — .claude/ 재동기화 헬퍼
#
# source 후 사용:
#   sync_claude <source_dir> <target_dir> [flags...]
#
# 인자:
#   source_dir    — 템플릿 측 .claude 내용 (예: /tmp/coord-upgrade/.claude
#                   또는 v0.4 subdir 모드에서 .coord/claude-template)
#   target_dir    — 프로젝트 측 .claude (보통 ./.claude)
#
# flags (환경변수로도 지정 가능):
#   ACCEPT_COMMANDS=1  — .claude/commands/*.md 일괄 승인
#   DRY_RUN=1          — 실제 쓰기 없이 계획만 출력
#   BACKUP_DIR=<path>  — 덮어쓸 파일을 이 디렉토리에 보존
#
# 분류 규칙 (classify.sh 와 동일):
#   .claude/commands/*.md          → manual-merge
#   .claude/settings.json          → overwrite
#   .claude/settings.local.json    → preserve (절대 건드리지 않음)
#   .claude/settings.local.json.example → overwrite
#
# v0.3 (flat 모드) 와 v0.4 (subdir 모드) 가 모두 이 함수를 호출.
# - v0.3: source=/tmp/coord-upgrade/.claude (업스트림 tarball/git 의 .claude)
# - v0.4: source=.coord/claude-template (subtree 로 들어온 템플릿 측 .claude)

# classify.sh 가 이미 source 돼 있어야 함 (classify_path 사용)

_sync_claude_color_init() {
  if [[ -z "${B:-}" ]]; then
    B=$'\033[1m' D=$'\033[0m' G=$'\033[32m' Y=$'\033[33m' R=$'\033[31m' C=$'\033[36m'
  fi
  return 0
}

_sync_claude_log() {
  _sync_claude_color_init
  local level="$1"; shift
  case "$level" in
    say)  echo -e "${C}▶${D} $*" ;;
    ok)   echo -e "${G}✓${D} $*" ;;
    warn) echo -e "${Y}⚠${D} $*" ;;
    err)  echo -e "${R}✗${D} $*" >&2 ;;
    diff) echo -e "${Y}~${D} $*" ;;
  esac
}

# 3-way 없이 2-way diff 기반 prompt (y/n/s=skip-all)
_sync_claude_prompt_merge() {
  local src="$1" dst="$2" rel="$3"
  if [[ ! -f "$dst" ]]; then
    # 새 파일 — prompt 없이 추가 (manual-merge 도 새 파일은 자동 추가)
    echo "ADD"
    return
  fi

  if cmp -s "$src" "$dst"; then
    echo "SAME"
    return
  fi

  if [[ "${ACCEPT_COMMANDS:-0}" == "1" ]]; then
    echo "ACCEPT"
    return
  fi

  if [[ "${SYNC_CLAUDE_SKIP_ALL:-0}" == "1" ]]; then
    echo "SKIP"
    return
  fi

  if [[ "${DRY_RUN:-0}" == "1" ]]; then
    echo "DRY_PROMPT"
    return
  fi

  # non-interactive 환경 대비 — TTY 없으면 안전하게 SKIP
  if [[ ! -e /dev/tty ]]; then
    echo "SKIP"
    return
  fi

  _sync_claude_log diff "manual-merge: $rel"
  echo "--- 현재 (프로젝트) vs 새 템플릿 ---"
  diff -u "$dst" "$src" 2>/dev/null | head -80 || true
  echo "--- (최대 80줄) ---"
  local answer
  read -rp "  적용? [y]es / [n]o=skip / [s]=skip all .claude/commands : " answer </dev/tty
  case "$answer" in
    y|Y) echo "ACCEPT" ;;
    s|S) export SYNC_CLAUDE_SKIP_ALL=1; echo "SKIP" ;;
    *)   echo "SKIP" ;;
  esac
}

_sync_claude_apply_file() {
  local src="$1" dst="$2" rel="$3" policy="$4"
  local dst_dir
  dst_dir="$(dirname "$dst")"

  case "$policy" in
    preserve)
      _sync_claude_log say "preserve: $rel (skip)"
      ;;
    overwrite)
      if [[ "${DRY_RUN:-0}" == "1" ]]; then
        _sync_claude_log say "[dry-run] overwrite: $rel"
      else
        [[ -n "${BACKUP_DIR:-}" ]] && [[ -f "$dst" ]] && {
          mkdir -p "$BACKUP_DIR/$(dirname "$rel")"
          cp -p "$dst" "$BACKUP_DIR/$rel"
        }
        mkdir -p "$dst_dir"
        cp -p "$src" "$dst"
        _sync_claude_log ok "overwrite: $rel"
      fi
      ;;
    manual-merge)
      local decision
      decision="$(_sync_claude_prompt_merge "$src" "$dst" "$rel")"
      case "$decision" in
        ADD)
          if [[ "${DRY_RUN:-0}" == "1" ]]; then
            _sync_claude_log say "[dry-run] add new: $rel"
          else
            mkdir -p "$dst_dir"
            cp -p "$src" "$dst"
            _sync_claude_log ok "add new: $rel"
          fi
          ;;
        SAME)
          : # no output for no-op
          ;;
        ACCEPT)
          [[ -n "${BACKUP_DIR:-}" ]] && {
            mkdir -p "$BACKUP_DIR/$(dirname "$rel")"
            cp -p "$dst" "$BACKUP_DIR/$rel"
          }
          cp -p "$src" "$dst"
          _sync_claude_log ok "merge: $rel"
          ;;
        DRY_PROMPT)
          _sync_claude_log say "[dry-run] manual-merge prompt: $rel"
          ;;
        SKIP)
          _sync_claude_log warn "skip: $rel (기존 유지)"
          ;;
      esac
      ;;
    skip)
      : # silently ignored
      ;;
    *)
      _sync_claude_log warn "unknown policy '$policy' for $rel — skip"
      ;;
  esac
}

# 공개 진입점
sync_claude() {
  local src_dir="$1"
  local dst_dir="$2"

  if [[ ! -d "$src_dir" ]]; then
    _sync_claude_log err "sync_claude: source 없음: $src_dir"
    return 1
  fi

  mkdir -p "$dst_dir"

  _sync_claude_log say ".claude/ 재동기화: $src_dir → $dst_dir"

  # source 의 모든 파일 순회 (hidden 포함)
  local src_file rel policy
  while IFS= read -r -d '' src_file; do
    rel=".claude/${src_file#$src_dir/}"
    policy="$(classify_path "$rel")"
    _sync_claude_apply_file "$src_file" "$dst_dir/${src_file#$src_dir/}" "$rel" "$policy"
  done < <(find "$src_dir" -type f -print0 2>/dev/null)

  # settings.local.json 은 source 에 없어도 target 에 있으면 유지 (명시)
  if [[ -f "$dst_dir/settings.local.json" ]]; then
    _sync_claude_log say "preserve: .claude/settings.local.json (개인 설정)"
  elif [[ -f "$dst_dir/settings.local.json.example" ]] && [[ ! -f "$dst_dir/settings.local.json" ]]; then
    _sync_claude_log warn ".claude/settings.local.json 없음 — .example 복사 고려"
  fi

  _sync_claude_log ok ".claude/ 재동기화 완료"
}

export -f sync_claude _sync_claude_apply_file _sync_claude_prompt_merge _sync_claude_log _sync_claude_color_init 2>/dev/null || true

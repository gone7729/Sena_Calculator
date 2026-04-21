#!/usr/bin/env bash
# 모든 worktree를 각각 새 VSCode 창으로 열기
# config.yml 의 PROJECT_NAME + sub 목록 동적 로딩
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true

PROJECT="${PROJECT_NAME:-project}"
ROOT="$(cd "$(dirname "$0")/.." && git rev-parse --show-toplevel)"
PARENT="$(dirname "$ROOT")"

WORKTREES=("$PROJECT-wt-head")
if command -v sub_names >/dev/null 2>&1; then
  while IFS= read -r sub; do
    [[ -n "$sub" ]] && WORKTREES+=("$PROJECT-wt-$sub")
  done < <(sub_names 2>/dev/null)
fi

# 메인도 열지 여부 (인자로 --all 주면 포함)
if [[ "${1:-}" == "--all" ]]; then
  WORKTREES=("$PROJECT" "${WORKTREES[@]}")
fi

for wt in "${WORKTREES[@]}"; do
  path="$PARENT/$wt"
  if [[ -d "$path" ]]; then
    echo "▶ opening: $wt"
    code -n "$path"
  else
    echo "⚠ skip (없음): $path"
  fi
done

echo "✅ 완료 — $((${#WORKTREES[@]})) 개 창 요청됨"

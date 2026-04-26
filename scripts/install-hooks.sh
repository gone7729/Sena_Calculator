#!/usr/bin/env bash
# 각 worktree 에 공용 훅 연결 (core.hooksPath)
# 사용법: bash scripts/install-hooks.sh
# config.yml 의 PROJECT_NAME + sub 목록 동적 로딩
#
# v0.4+: INSTALL_MODE 에 따라 hooksPath 경로 다름
#   flat:   .githooks
#   subdir: .coord/.githooks

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh"

: "${PROJECT_ROOT:?config.sh 가 로드돼야 함}"
: "${PROJECT_NAME:?config.yml 에 project.name 필요}"

PARENT="$(dirname "$PROJECT_ROOT")"
HOOKS_PATH="$(coord_hooks_path)"

install_hook_in() {
  local dir="$1" label="$2"
  if [[ ! -d "$dir" ]]; then
    echo "⚠ 없음: $dir (skip)"
    return
  fi
  cd "$dir"
  git config core.hooksPath "$HOOKS_PATH"
  [[ -f "$HOOKS_PATH/pre-commit" ]] && chmod +x "$HOOKS_PATH/pre-commit" 2>/dev/null || true
  echo "✅ $label → core.hooksPath = $HOOKS_PATH"
}

# 메인 repo (PROJECT_ROOT 자체)
install_hook_in "$PROJECT_ROOT" "$PROJECT_NAME (main)"

# head + sub worktree 들 (PROJECT_ROOT 의 siblings)
SIBLINGS=("$PROJECT_NAME-wt-head")
if command -v sub_names >/dev/null 2>&1; then
  while IFS= read -r sub; do
    [[ -n "$sub" ]] && SIBLINGS+=("$PROJECT_NAME-wt-$sub")
  done < <(sub_names 2>/dev/null)
fi

for wt in "${SIBLINGS[@]}"; do
  install_hook_in "$PARENT/$wt" "$wt"
done

echo ""
echo "설치 완료. 테스트:"
echo "  cd <worktree>"
echo "  touch test-violation.txt"
echo "  git add test-violation.txt"
echo "  git commit -m 'test'  # 차단될 것"

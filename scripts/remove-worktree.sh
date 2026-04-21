#!/usr/bin/env bash
# worktree 안전 제거 (uncommitted 변경 있으면 중단)
set -euo pipefail

NAME="${1:-}"
if [[ -z "$NAME" ]]; then
  echo "사용법: bash scripts/remove-worktree.sh <name>"
  echo "현재 worktree:"
  git worktree list
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true
PROJECT="${PROJECT_NAME:-project}"

REPO_ROOT="$(git rev-parse --show-toplevel)"
PARENT_DIR="$(dirname "$REPO_ROOT")"
WT_DIR="$PARENT_DIR/$PROJECT-wt-$NAME"
BRANCH="wt/$NAME"

if [[ ! -d "$WT_DIR" ]]; then
  echo "❌ 없음: $WT_DIR"
  exit 1
fi

# uncommitted 변경 체크
changes=$(git -C "$WT_DIR" status --short | wc -l | tr -d ' ')
if [[ "$changes" -gt 0 ]]; then
  echo "⚠️  uncommitted 변경 $changes개"
  git -C "$WT_DIR" status --short
  read -p "정말 삭제? (yes 입력): " ans
  [[ "$ans" == "yes" ]] || { echo "취소"; exit 1; }
fi

# push 안 된 커밋 체크
ahead=$(git -C "$WT_DIR" rev-list --count "@{u}..HEAD" 2>/dev/null || echo "0")
if [[ "$ahead" -gt 0 ]]; then
  echo "⚠️  push 안 된 커밋 $ahead개 (브랜치: $BRANCH)"
  read -p "브랜치도 삭제? (yes 입력): " ans
  [[ "$ans" == "yes" ]] || { echo "취소"; exit 1; }
fi

git worktree remove "$WT_DIR" --force
git branch -D "$BRANCH" 2>/dev/null || true
echo "✅ 삭제 완료: $NAME"

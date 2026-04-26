#!/usr/bin/env bash
# 현재 worktree 현황 + 각 브랜치 상태 요약
set -euo pipefail

echo "=== Worktree 목록 ==="
git worktree list
echo ""

echo "=== 각 worktree 상태 ==="
git worktree list --porcelain | awk '/^worktree /{print $2}' | while read -r wt; do
  name="$(basename "$wt")"
  if [[ -d "$wt/.git" ]] || [[ -f "$wt/.git" ]]; then
    branch=$(git -C "$wt" branch --show-current 2>/dev/null || echo "?")
    changes=$(git -C "$wt" status --short 2>/dev/null | wc -l | tr -d ' ')
    ahead=$(git -C "$wt" rev-list --count "@{u}..HEAD" 2>/dev/null || echo "-")
    echo "  $name"
    echo "    브랜치: $branch"
    echo "    변경파일: $changes개"
    echo "    push 대기: $ahead commits"
  fi
done

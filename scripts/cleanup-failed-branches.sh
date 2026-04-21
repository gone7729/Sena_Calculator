#!/usr/bin/env bash
# 30일 이상 지난 failed/* 브랜치 삭제
# 사용자가 직접 실행 (자동 cron 없음)
# 사용법:
#   bash scripts/cleanup-failed-branches.sh          # dry-run (목록만)
#   bash scripts/cleanup-failed-branches.sh --apply  # 실제 삭제

set -euo pipefail

APPLY=false
[[ "${1:-}" == "--apply" ]] && APPLY=true

# 30일 전 timestamp (초 단위)
THRESHOLD=$(( $(date +%s) - 30*24*60*60 ))

echo "=== failed/* 브랜치 스캔 ==="
echo "기준: 30일 이상 미사용"
echo ""

FOUND=0
DELETED=0

# failed/ 접두어 브랜치 목록
while IFS= read -r branch; do
  [[ -z "$branch" ]] && continue
  FOUND=$((FOUND+1))

  # 마지막 커밋 시각
  LAST_COMMIT_TS=$(git log -1 --format=%ct "$branch" 2>/dev/null || echo 0)

  if [[ "$LAST_COMMIT_TS" -lt "$THRESHOLD" ]]; then
    AGE_DAYS=$(( ($(date +%s) - LAST_COMMIT_TS) / 86400 ))
    echo "🗑  $branch (${AGE_DAYS}일 경과)"

    if $APPLY; then
      git branch -D "$branch" 2>&1 | sed 's/^/    /'
      DELETED=$((DELETED+1))
    fi
  fi
done < <(git branch --list "failed/*" | sed 's/^[* ] //')

echo ""
echo "=== 요약 ==="
echo "검사: $FOUND 개"

if $APPLY; then
  echo "삭제: $DELETED 개"
else
  echo "dry-run (실제 삭제하려면 --apply 추가)"
fi

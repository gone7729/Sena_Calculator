#!/usr/bin/env bash
# wt/head 가 work_branch 와 심하게 divergent 할 때 안전하게 reset
# 사용법:
#   bash scripts/reset-wt-head.sh         # dry-run
#   bash scripts/reset-wt-head.sh --apply # 실행
#
# 동작:
#   1. wt/head worktree 의 .claude/commands 자동 백업
#   2. 사용자에게 보존 원하는 파일 (review-inbox 등) 확인
#   3. 확인 후 wt/head 를 origin/<work_branch> 로 reset --hard
#   4. commands 파일 복원

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh"

: "${PROJECT_ROOT:?config.sh 가 로드돼야 함}"
: "${PROJECT_NAME:?config.yml 에 project.name 필요}"
: "${WORK_BRANCH:?config.yml 에 git.work_branch 필요}"
: "${REMOTE:=origin}"

APPLY=false
[[ "${1:-}" == "--apply" ]] && APPLY=true

# worktree 들은 PROJECT_ROOT 의 sibling
HEAD_WT="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"

if [[ ! -d "$HEAD_WT" ]]; then
  echo "❌ head worktree 없음: $HEAD_WT"
  exit 1
fi

cd "$HEAD_WT"

CURRENT=$(git rev-parse HEAD)
ORIGIN_WB=$(git rev-parse "$REMOTE/$WORK_BRANCH" 2>/dev/null || git ls-remote "$REMOTE" "$WORK_BRANCH" | awk '{print $1}')

echo "=== wt/head reset 도구 ==="
echo "현재 HEAD          : $CURRENT"
echo "$REMOTE/$WORK_BRANCH: $ORIGIN_WB"
echo ""

# divergence 확인
AHEAD=$(git rev-list --count "$REMOTE/$WORK_BRANCH..HEAD" 2>/dev/null || echo "?")
BEHIND=$(git rev-list --count "HEAD..$REMOTE/$WORK_BRANCH" 2>/dev/null || echo "?")
echo "상태: $AHEAD 커밋 앞섬, $BEHIND 커밋 뒤처짐"
echo ""

if [[ "$AHEAD" == "0" && "$BEHIND" == "0" ]]; then
  echo "✅ 이미 동기화됨. reset 불필요."
  exit 0
fi

# 보존할 파일 경고
echo "⚠ reset 후 잃게 될 것:"
echo "- wt/head 에만 있는 $AHEAD 개 커밋"
echo ""
echo "보존 권장:"
echo "- coordination/review-inbox/*.md (검토 기록)"
echo "- coordination/reports/archive/** (과거 보고서)"
echo ""
echo "이 파일들은 먼저 $WORK_BRANCH 로 복사 후 reset 실행을 권장합니다."
echo ""

if ! $APPLY; then
  echo "dry-run 종료. 실제 reset: bash $0 --apply"
  exit 0
fi

# 백업
echo "▶ .claude/commands 백업..."
bash "$SCRIPT_DIR/backup-commands.sh" "$HEAD_WT"

# 확인 받기
read -p "계속? (yes 입력): " ans
[[ "$ans" == "yes" ]] || { echo "취소"; exit 1; }

git fetch "$REMOTE" "$WORK_BRANCH" --quiet
git reset --hard "$REMOTE/$WORK_BRANCH"
git push -f "$REMOTE" wt/head

echo ""
echo "✅ reset 완료"
echo "wt/head tip: $(git rev-parse HEAD)"
echo ""
echo "복원 필요 시:"
echo "  백업 위치: ~/.claude-coord-backups/"
echo "  cp -r <백업>/commands/* $HEAD_WT/.claude/commands/"

#!/usr/bin/env bash
# v0.47+: sub worktree 백그라운드 spawn (head 의 Bash 도구 10분 cap 우회)
#
# 배경: head 가 직접 `claude -p "/go" ...` 동기 호출하면 head 의 Bash 도구
# 자체가 max 10분 (Anthropic 플랫폼 default) 에서 끊김. sub 가 30분 작업해야 하면
# 기다릴 수 없음. background + poll 패턴으로 우회.
#
# 사용:
#   bash scripts/spawn-sub-bg.sh <sub-name> <model> [<timeout-sec>]
# 출력 (한 줄, head 가 파싱):
#   PID=<pid> LOG=<log-path> START_TS=<unix-ts>
# 즉시 return (1초 내). head 는 scripts/poll-sub.sh 로 상태 확인.

set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "ERROR 사용법: bash $0 <sub-name> <model> [timeout-sec]"
  echo "       예: bash $0 backend claude-opus-4-7 1800"
  exit 1
fi

SUB="$1"
MODEL="$2"
MAX_SEC="${3:-1800}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true

: "${PROJECT_ROOT:?PROJECT_ROOT 미해결 — config.sh 가 source 되는 곳에서 실행하세요}"
: "${PROJECT_NAME:?PROJECT_NAME 미해결 — config.yml 의 project.name 확인}"

SIBLING_BASE="$(dirname "$PROJECT_ROOT")"
SUB_DIR="$SIBLING_BASE/${PROJECT_NAME}-wt-${SUB}"

if [[ ! -d "$SUB_DIR" ]]; then
  echo "ERROR sub worktree 없음: $SUB_DIR — bash scripts/setup-worktree.sh $SUB 먼저"
  exit 1
fi

# claude CLI 위치
CLAUDE_BIN="${COORD_CLAUDE_BIN:-$(command -v claude || true)}"
if [[ -z "$CLAUDE_BIN" ]]; then
  echo "ERROR claude CLI 미발견 — npm i -g @anthropic-ai/claude-code 후 재시도"
  exit 1
fi

TS=$(date +%s)
LOG="${TMPDIR:-/tmp}/wt-${SUB}-${TS}.log"
PID_FILE="${TMPDIR:-/tmp}/wt-${SUB}-${TS}.pid"

# 백그라운드 spawn — head 종료에 독립 (nohup + & + disown)
# shell 의 timeout 명령으로 hard 한계도 같이 걸기 (poll-sub 의 soft kill 외 안전망)
(
  cd "$SUB_DIR"
  nohup timeout --kill-after=10 "$MAX_SEC" "$CLAUDE_BIN" -p "/go" \
    --permission-mode bypassPermissions \
    --model "$MODEL" \
    --output-format json \
    > "$LOG" 2>&1
) &
SUB_PID=$!
disown "$SUB_PID" 2>/dev/null || true

echo "$SUB_PID" > "$PID_FILE"
echo "PID=$SUB_PID LOG=$LOG START_TS=$TS"

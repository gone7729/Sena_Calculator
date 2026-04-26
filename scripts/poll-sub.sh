#!/usr/bin/env bash
# v0.47+: 백그라운드 sub 프로세스 1회 status check (즉시 return)
#
# 배경: head 가 sub 종료를 동기 대기하면 Bash 도구 10분 cap 에 걸림. 이 스크립트는
# **1회 polling** — alive/dead/timeout 만 보고하고 즉시 종료. head 가 매 30초마다
# 별도 Bash 호출로 다시 invoke. 각 호출은 1초 안이라 cap 무관.
#
# 사용:
#   bash scripts/poll-sub.sh <pid> <start-ts> [<max-sec>]
# 출력 (한 줄, head 가 파싱):
#   RUNNING <elapsed-sec>           (아직 실행 중)
#   DONE <elapsed-sec>              (정상 종료 — exit-code 는 log 마지막 줄 확인)
#   TIMEOUT <elapsed-sec>           (max 초과로 kill 시도)
#
# 종료 코드 / log tail 등 상세 정보는 head 가 별도 Read/Bash 로 확인.

set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "ERROR 사용법: bash $0 <pid> <start-ts> [max-sec]"
  exit 1
fi

PID="$1"
START_TS="$2"
MAX_SEC="${3:-1800}"

NOW=$(date +%s)
ELAPSED=$(( NOW - START_TS ))

# Timeout 초과 시 kill (poll-sub 가 자체 kill — spawn-sub-bg 의 timeout 명령보다 먼저 작동 가능)
if [[ $ELAPSED -gt $MAX_SEC ]]; then
  if kill -0 "$PID" 2>/dev/null; then
    kill -TERM "$PID" 2>/dev/null || true
    sleep 1
    kill -KILL "$PID" 2>/dev/null || true
  fi
  echo "TIMEOUT $ELAPSED"
  exit 0
fi

# Alive 체크
if kill -0 "$PID" 2>/dev/null; then
  echo "RUNNING $ELAPSED"
else
  echo "DONE $ELAPSED"
fi

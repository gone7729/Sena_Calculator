#!/usr/bin/env bash
# PreToolUse Hook — 사법부 (3심 구조)
# 입력: tool call JSON (stdin)
# 출력: exit 0 = 허가, exit 2 = 거부 (stderr 로 이유 전달)
#
# 심급:
#   1심: 화이트리스트 (빠른 허가 — 읽기/안전 명령)
#   2심: 블랙리스트 (즉시 거부 — 파괴적 명령)
#   3심: LLM 판사 (Sonnet 4.6, 맥락 기반)
# 3심 거부 시: coordination/escalations/ 자동 생성 + Discord 알림

set -e

# config 조기 로드 (2심 blacklist 에서 WORK_BRANCH 사용)
SCRIPT_DIR_EARLY="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR_EARLY/config.sh" ]] && source "$SCRIPT_DIR_EARLY/config.sh" 2>/dev/null || true

INPUT=$(cat)
LOG=/tmp/judge-log.txt

# Python 으로 JSON 파싱 (Windows Git Bash 호환)
TOOL=$(echo "$INPUT" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('tool_name',''))" 2>/dev/null || echo "")
TOOL_INPUT=$(echo "$INPUT" | python3 -c "import sys,json; d=json.load(sys.stdin); print(json.dumps(d.get('tool_input',{})))" 2>/dev/null || echo "{}")

# 로그 헬퍼
log() { echo "[$(date -Iseconds)] $1" >> "$LOG"; }

# ========================================
# 1심: 화이트리스트
# ========================================
case "$TOOL" in
  Read|Grep|Glob|LS|NotebookRead)
    log "ALLOW[1심/read] $TOOL"
    exit 0
    ;;
esac

if [[ "$TOOL" == "Bash" ]]; then
  CMD=$(echo "$TOOL_INPUT" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('command',''))" 2>/dev/null || echo "")

  # 안전 조회성 명령
  case "$CMD" in
    git\ status*|git\ log*|git\ diff*|git\ branch*|git\ fetch*|git\ show*|git\ ls-tree*|git\ rev-parse*|git\ worktree*|git\ config*)
      log "ALLOW[1심/git-read] $CMD"
      exit 0 ;;
    ls\ *|ls|cat\ *|pwd|echo\ *|which\ *|date*|whoami)
      log "ALLOW[1심/posix-read] $CMD"
      exit 0 ;;
    npm\ run\ type-check*|npm\ run\ lint*|npm\ run\ ssot:check*|npm\ run\ test*|npm\ test*)
      log "ALLOW[1심/npm-check] $CMD"
      exit 0 ;;
    bash\ scripts/notify-discord.sh*|bash\ scripts/backup-commands.sh*|bash\ scripts/list-worktrees.sh*)
      log "ALLOW[1심/internal-script] $CMD"
      exit 0 ;;
  esac

  # ========================================
  # 2심: 블랙리스트 (즉시 거부)
  # ========================================
  case "$CMD" in
    *rm\ -rf\ /*|*rm\ -rf\ \~*|*rm\ -rf\ \$HOME*)
      REASON="전역 rm -rf 감지 (치명적)"
      echo "🚫 [2심/deny] $REASON" >&2
      log "DENY[2심/destructive] $CMD"
      exit 2 ;;
    *sudo\ *)
      REASON="sudo 명령 금지"
      echo "🚫 [2심/deny] $REASON" >&2
      log "DENY[2심/privilege] $CMD"
      exit 2 ;;
    *curl*|*sh*|*wget*|*sh*)
      # curl|sh 파이프 패턴만 차단 (단순 curl 은 3심으로)
      if echo "$CMD" | grep -qE "curl.*\|.*(bash|sh|zsh)"; then
        REASON="curl | sh 파이프 금지 (원격 코드 실행 위험)"
        echo "🚫 [2심/deny] $REASON" >&2
        log "DENY[2심/pipe-exec] $CMD"
        exit 2
      fi
      ;;
    *git\ push*-f*main*|*git\ push*--force*main*)
      REASON="main 브랜치 강제 푸시 금지"
      echo "🚫 [2심/deny] $REASON" >&2
      log "DENY[2심/force-main] $CMD"
      exit 2 ;;
    *git\ push*-f*${WORK_BRANCH:-main}*|*git\ push*--force*${WORK_BRANCH:-main}*)
      REASON="${WORK_BRANCH:-main} (work_branch) 강제 푸시 금지 (협업자 작업 손실 위험)"
      echo "🚫 [2심/deny] $REASON" >&2
      log "DENY[2심/force-work-branch] $CMD"
      exit 2 ;;
    *rm\ -rf\ .git*|*rm\ -rf\ .claude*|*rm\ -rf\ coordination*)
      REASON="핵심 디렉토리 삭제 금지 (.git/.claude/coordination)"
      echo "🚫 [2심/deny] $REASON" >&2
      log "DENY[2심/core-delete] $CMD"
      exit 2 ;;
  esac
fi

# ========================================
# 2.5심: 사용자 승인 (approvals/) 확인 (Phase 2.6)
# ========================================
# 같은 action 이 직전 escalation + approval 흐름을 거쳤다면 통과
# 효력: config.yml 의 judiciary.approval_ttl_minutes (기본 1440분 = 24시간)
SCRIPT_DIR_J="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR_J/config.sh" ]] && source "$SCRIPT_DIR_J/config.sh" 2>/dev/null || true
APPROVAL_TTL_MIN="${APPROVAL_TTL_MIN:-1440}"
RECENT_APPROVAL=$(find coordination/approvals -name "*.md" -mmin -${APPROVAL_TTL_MIN} 2>/dev/null | head -1)
if [[ -n "$RECENT_APPROVAL" ]]; then
  if grep -q "approved.*true" "$RECENT_APPROVAL"; then
    log "ALLOW[2.5심/recent-approval] $TOOL — $RECENT_APPROVAL"
    exit 0
  fi
fi

# ========================================
# 3심: LLM 판사 (Sonnet 4.6)
# ========================================

# 맥락 수집
BRANCH=$(git branch --show-current 2>/dev/null || echo "?")
ROLE_NAME="${BRANCH#wt/}"
ROLE_FILE="coordination/roles/$ROLE_NAME.md"
ROLE_CONTENT=""
[[ -f "$ROLE_FILE" ]] && ROLE_CONTENT=$(head -60 "$ROLE_FILE")

TASK_FILE="coordination/tasks/wt-$ROLE_NAME.md"
TASK_CONTENT=""
[[ -f "$TASK_FILE" ]] && TASK_CONTENT=$(head -50 "$TASK_FILE")

# 최신 plan
PLAN_CONTENT=""
LATEST_PLAN=$(ls -t coordination/plans/*.md 2>/dev/null | grep -v _TEMPLATE | head -1 || true)
[[ -n "$LATEST_PLAN" ]] && PLAN_CONTENT=$(head -50 "$LATEST_PLAN")

# 판사 프롬프트
JUDGE_PROMPT="당신은 ${PROJECT_NAME:-coord} 자율 오케스트레이션의 **안전 판사** 다. 다음 action 이 허용 가능한지 판단하라.

## 판단 원칙
- **기본은 허가** (보수적 거부 금지 — 자율성 보장)
- **명확한 위험**만 거부
- 역할 범위 밖이어도 task 에 필수면 허가 가능 (단, escalations 필요한 건 표기)

## 현재 세션
- 브랜치: $BRANCH
- 역할: $ROLE_NAME

## 역할 규약 (roles/$ROLE_NAME.md)
$ROLE_CONTENT

## 현재 task
$TASK_CONTENT

## 현재 plan
$PLAN_CONTENT

## 심사 대상
Tool: $TOOL
Input: $TOOL_INPUT

## 판단 기준 (순서대로)
1. 파괴적/되돌릴 수 없는 작업? (데이터 손실, 브랜치 파괴 등)
2. 명백히 역할/task 와 무관한 action?
3. 보안/권한 위반? (외부 네트워크, 시스템 변경 등)
4. 위 3개 해당 없으면 → ALLOW

## 출력 (반드시 한 줄)
ALLOW: <한 문장 이유>
또는
DENY: <구체적 사유 (뭐가 왜 위험한지)>"

# Sonnet 4.6 판사 호출
JUDGMENT=$(echo "$JUDGE_PROMPT" | timeout 30 claude -p \
  --model "${JUDGE_MODEL:-claude-sonnet-4-6}" \
  --output-format text \
  --max-turns 1 \
  --permission-mode bypassPermissions \
  2>&1 || echo "DENY: 판사 타임아웃/오류 — 안전상 거부")

log "JUDGE[3심] $TOOL: $JUDGMENT"

if echo "$JUDGMENT" | grep -qiE "^ALLOW"; then
  exit 0
fi

# ========================================
# 거부 시 자동 에스컬레이션
# ========================================
TS=$(date +%Y%m%d-%H%M%S)
ESC_FILE="coordination/escalations/wt-$ROLE_NAME-judge-$TS.md"
mkdir -p coordination/escalations

cat > "$ESC_FILE" <<ESCEOF
# Escalation: head 판사 거부 (자동)

- **id**: $TS
- **worktree**: $BRANCH
- **created**: $(date -Iseconds)
- **status**: pending
- **source**: judge-action.sh (3심 LLM 판사)

## 거부된 action
- **Tool**: $TOOL
- **Input**: \`$TOOL_INPUT\`

## 판사 판결
$JUDGMENT

## 현재 task (참고)
$TASK_FILE (있으면 참조)

## 승인 방법
코드 검토 후 승인 원하면 \`coordination/approvals/$TS.md\` 생성:
\`\`\`markdown
approved: true
escalation_id: $TS
reason: <승인 이유>
\`\`\`

거부 유지는 파일 삭제 또는 \`approved: false\`
ESCEOF

# Discord 알림 (실패해도 진행)
# v0.8+: escalation-id 를 parseable 형태로 포함 → 봇 리액션 핸들러가 ✅/❌ 감지 시 자동 응답
if [[ -x "scripts/notify-discord.sh" ]]; then
  bash scripts/notify-discord.sh "🔔 head 판사 거부 — 승인 요청" \
    "**escalation-id**: \`$TS\`
Tool: $TOOL
사유: $JUDGMENT
파일: $ESC_FILE

✅ 리액션 = 승인 (resolve)   /   ❌ 리액션 = 거부 (reject)" 2>/dev/null || true
fi

echo "🚫 [3심/deny] 판사 거부" >&2
echo "$JUDGMENT" >&2
echo "에스컬레이션 생성: $ESC_FILE" >&2

exit 2

---
description: "[HEAD 전용] 원격 inbox 엔트리 → 다단계 plan → Step dispatch → review-inbox"
requires-wt: head
min-coord-version: v0.42
---

**⚠ 이 명령은 wt/head worktree 에서만 실행.** 다른 worktree 에선 의미 없음.

너는 head 오케스트레이터다. 원격 작업 지시를 다단계로 실행한다.

## 경로 해결 규칙

시작 전에 `scripts/config.sh` 를 source 해서 다음 환경변수를 얻는다:
- `COORD_ROOT` — coord 폴더 (subdir 모드 `<worktree>/.coord`, flat 모드 `<worktree>`)
- `PROJECT_ROOT` — 프로젝트 루트 (모드에 따라 다름, worktree 에선 worktree 자신)
- `PROJECT_NAME` / `WORK_BRANCH` — config.yml 에서

이후 모든 파일 경로는 `$COORD_ROOT/coordination/...` 형태.
head worktree 의 sibling sub: `$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-<sub>`.

```bash
source "$(git rev-parse --show-toplevel)/scripts/config.sh" 2>/dev/null || \
  source "$(git rev-parse --show-toplevel)/.coord/scripts/config.sh"
HEAD_WT="$PROJECT_ROOT"  # head 는 자기 worktree
SIBLING_BASE="$(dirname "$HEAD_WT")"
```

## 0. 워킹트리 자동 정리 (보호 디렉토리 제외)

```bash
cd "$HEAD_WT"
if [[ -n "$(git status --short)" ]]; then
  echo "⚠ 워킹트리 오염 감지 — 자동 정리 (escalations/는 보호)"
  git status --short

  # modified/deleted 만 원복 (안전)
  git checkout -- "${COORD_ROOT#$HEAD_WT/}/coordination/" 2>/dev/null || true

  # untracked 정리 — escalations/, approvals/, inbox/attachments/ 보존 (메인 정본)
  # HEAD_LOCK, dashboard-url.txt 는 gitignored 라 영향 X
  git clean -fd "${COORD_ROOT#$HEAD_WT/}/coordination/" \
    -e "escalations/" \
    -e "approvals/" \
    -e "inbox/attachments/" 2>/dev/null || true

  REMAINING=$(git status --short)
  if [[ -n "$REMAINING" ]]; then
    echo "ℹ 보호 디렉토리에 untracked (메인 흡수 대기) — 진행은 계속:"
    echo "$REMAINING"
  fi
fi
```

## 1. Upstream pull
- `git fetch origin "$WORK_BRANCH" --quiet`
- `git rebase "origin/$WORK_BRANCH"` (충돌 시: inbox/* 는 `--skip` 후보)
- 전체 실패 시 사용자 알림 + 중단

## 1.5. 미해결 escalation + approval 확인
```bash
for esc in "$COORD_ROOT"/coordination/escalations/*.md; do
  [[ ! -f "$esc" ]] && continue
  STATUS=$(grep "^- \*\*status\*\*:" "$esc" | head -1)
  if echo "$STATUS" | grep -q "pending"; then
    ID=$(grep "^- \*\*id\*\*:" "$esc" | head -1 | sed 's/.*: //')
    APPROVAL="$COORD_ROOT/coordination/approvals/${ID}.md"
    if [[ -f "$APPROVAL" ]]; then
      APPROVED=$(grep "approved" "$APPROVAL" | head -1 | grep -oE "(true|false)")
      if [[ "$APPROVED" == "true" ]]; then
        sed -i 's/status\*\*: pending/status**: resolved-approved/' "$esc"
      elif [[ "$APPROVED" == "false" ]]; then
        sed -i 's/status\*\*: pending/status**: resolved-rejected/' "$esc"
      fi
    fi
  fi
done
```

## 2. STOP 확인
- `$COORD_ROOT/coordination/STOP` 존재 → Discord 알림 + 즉시 종료

## 3. Pending 엔트리 찾기
- `$COORD_ROOT/coordination/inbox/*.md` 중 `status: pending` 만, 시간순
- 없으면 "처리할 inbox 없음" 보고 후 종료

## 4. 엔트리 순차 처리 (동시 1개)

### 4-1. 사전 분석 (inbox status 변경 금지 — 메인 정본)
- `$COORD_ROOT/coordination/hotfixes.md` 상단 10개
- 언급 파일 최신 커밋
- 이전 `$COORD_ROOT/coordination/reports/wt-*.md`

### 4-2. 토큰 예산 확인 (`$COORD_ROOT/coordination/token-budget.md`)

### 4-3. Plan 작성 (`$COORD_ROOT/coordination/plans/<id>.md`, _TEMPLATE 포맷)
- 요청 원문, 관련 파일, Step 분해 (의존성)
- 커밋 + push

**처리 시작 Discord 알림** (Plan 생성 직후):
```bash
INBOX_FILE="$COORD_ROOT/coordination/inbox/<id>.md"
PLAN_FILE="$COORD_ROOT/coordination/plans/<id>.md"
TITLE=$(grep "^# Inbox:" "$INBOX_FILE" | sed 's/^# Inbox: //')
SUB_LIST=$(grep -oE "wt-[a-z]+" "$PLAN_FILE" | sort -u | tr '\n' ' ')
bash "$COORD_ROOT/scripts/notify-discord.sh" "🚀 처리 시작" \
  "**id**: <id>\n**제목**: $TITLE\n**Sub**: $SUB_LIST\n**Steps**: $TOTAL_STEPS"
```

### 4-4. Step 실행 루프
각 Step:
- (a) STOP 재확인
- (b) 의존성 검증
- (c) Step `in_progress`, started
- (d) task 파일 작성 (`$COORD_ROOT/coordination/tasks/wt-<sub>.md` — task_start_commit, 범위/금지/목표)
- (e) sub worktree 로 task/STATUS/hotfixes 복사 (sub 의 COORD_ROOT 로)
- (f) Headless sub 실행 — **v0.47+ background + poll 패턴** (head 의 Bash 도구 10분 cap 우회):

  ```bash
  # 1. 모델 결정 (config.yml 의 subs[].model 참조)
  MODEL=$(sub_model "<sub>" 2>/dev/null || \
          python3 -c "import yaml; d=yaml.safe_load(open('$COORD_ROOT/coordination/config.yml')); \
          print(next((s['model'] for s in d.get('subs',[]) if s['name']=='<sub>'), 'claude-sonnet-4-6'))")

  # 2. task_start_commit 기록 (sub worktree 의 HEAD)
  SUB_DIR="$SIBLING_BASE/${PROJECT_NAME}-wt-<sub>"
  TASK_START=$(cd "$SUB_DIR" && git rev-parse HEAD)

  # 3. background spawn (즉시 return, PID + LOG 출력)
  SPAWN_OUT=$(bash "$COORD_ROOT/scripts/spawn-sub-bg.sh" "<sub>" "$MODEL" 1800)
  # SPAWN_OUT 형식: "PID=12345 LOG=/tmp/wt-<sub>-<ts>.log START_TS=<ts>"
  SUB_PID=$(echo "$SPAWN_OUT" | grep -oE 'PID=[0-9]+' | cut -d= -f2)
  SUB_LOG=$(echo "$SPAWN_OUT" | grep -oE 'LOG=[^ ]+' | cut -d= -f2)
  SUB_START=$(echo "$SPAWN_OUT" | grep -oE 'START_TS=[0-9]+' | cut -d= -f2)
  ```

  **다음 — head 가 따라야 할 polling 패턴**:
  ```
  매 30초마다 별도 Bash 호출 (각 호출은 1초 안에 끝나서 10분 cap 무관):
    bash $COORD_ROOT/scripts/poll-sub.sh $SUB_PID $SUB_START 1800
    출력 한 줄:
      RUNNING <elapsed>     → sleep 30 후 다시 poll
      DONE <elapsed>        → log 분석 단계로
      TIMEOUT <elapsed>     → 실패 처리

  사이사이 별도 Bash 로 부분 진행 확인 가능:
    tail -20 $SUB_LOG     # 최근 활동 확인
    cat $COORD_ROOT/coordination/reports/wt-<sub>.md   # sub 가 보고서 작성 중인지
  ```

  **DONE 받은 뒤 결과 판정**:
  ```bash
  # claude -p --output-format json 의 마지막 줄에 종합 JSON
  TAIL=$(tail -1 "$SUB_LOG")
  IS_ERROR=$(echo "$TAIL" | python3 -c "import sys,json; d=json.loads(sys.stdin.read()); print(d.get('is_error', False))")
  # "True" / "False" — True 면 sub 실패
  ```

  **참고 — 직접 동기 호출은 금지**: `timeout 1800 claude -p "/go" ...` 패턴은 head Bash 도구 10분 cap 에 걸림. 반드시 spawn-sub-bg.sh + poll-sub.sh 조합 사용.
- (g) 보고서 수집 (`$COORD_ROOT/coordination/reports/wt-<sub>.md`)
- (h) 성공:
  ```bash
  cd "$SIBLING_BASE/${PROJECT_NAME}-wt-<sub>"
  git add -A; git commit -m "<sub>: Step N <title> (inbox: <id>)"
  git push origin "wt/<sub>"
  ```
  Plan: `status: done`, commit, completed
  Discord: ✅ Step done
  토큰 추적: `bash "$COORD_ROOT/scripts/track-token-usage.sh" "$SUB_LOG" "wt-<sub>/step-N"`
- (i) 실패: `failed/wt-<sub>-<ts>` 브랜치 보관 + reset + Discord + 중단
- (j) 에스컬레이션 감지: blocked + Discord + 중단

### 4-5. Head 자체 검증 (모두 done 시)
- type-check / ssot:check (해당 sub 의 `validation` 필드) / lint
- 통과 → 4-6, 실패 → `needs-rework` + 종료

### 4-6. review-inbox 작성 (`$COORD_ROOT/coordination/review-inbox/<ts>-<id>.md`)
- `reviewed: false`, 변경 요약, 브랜치 커밋, 검토 포인트

### 4-7. coordination 커밋+push
```bash
cd "$HEAD_WT"
git add "${COORD_ROOT#$HEAD_WT/}/coordination/"
git commit -m "coordination: <id> 처리 결과"
git push origin wt/head
```
**금지**: inbox/*, hotfixes, roles, 문서 수정 (메인 정본)

Discord:
- 성공: ✅ 완료 | review-inbox/<file>
- 실패: ⚠️ 부분완료

## 5. 최종 보고
- 처리 카운트, review-inbox 경로, 다음: 메인 `/review-inbox`

## 금지
- inbox 파일 직접 수정 (메인만)
- hotfixes/roles/문서 수정 (메인만)
- sub 브랜치 범위 외 커밋
- 승인 없이 `$WORK_BRANCH` 머지
- 동시 2개 이상 엔트리

## 지금 할 것
위 절차대로 pending inbox 처리.
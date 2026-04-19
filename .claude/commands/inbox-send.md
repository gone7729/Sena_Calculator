---
description: 어디서든 head에 작업지시 — md 생성 + 커밋 + 푸시 (STOP 특수 처리 포함)
argument-hint: <작업지시 내용, 또는 "STOP">
---
> v0.5.1+: 모든 경로/브랜치는 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드 (모든 bash 블록 실행 전 1회)

```bash
# config.sh 가 PROJECT_NAME / WORK_BRANCH / REMOTE / PROJECT_ROOT 를 export
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
```

너는 메인 세션이다. 사용자 입력 `$ARGUMENTS` 를 inbox 엔트리로 만들어 커밋/푸시한다.

## 특수 키워드: STOP

입력이 정확히 `STOP` (대문자) 이면 **긴급 중단 모드**:
1. `$COORD_ROOT/coordination/STOP` 파일 생성 (내용: "STOPPED at YYYY-MM-DD HH:MM KST")
2. `git add "$COORD_ROOT/coordination/STOP"`
3. `git commit -m "coordination: STOP signal"`
4. `git push "$REMOTE" "$WORK_BRANCH"`
5. `bash scripts/notify-discord.sh "🚨 STOP" "사용자가 전체 중단 요청. 실행 중인 sub 들이 매 단계 시작 전 이 파일 확인 후 abort 한다."`
6. 사용자에게: "STOP 시그널 전파 완료. head/sub이 다음 체크포인트에서 중단합니다."

STOP 해제는 별도 명령: `coordination/STOP` 파일 수동 삭제 + 커밋

## 일반 작업지시 처리

1. **타임스탬프** 생성: `TS=$(date +%Y-%m-%d-%H%M%S)`

1.5. **클립보드 이미지 자동 첨부 (있을 때만)**
```bash
# 사용자가 Win+Shift+S 등으로 캡처해 클립보드에 이미지 보유 시 자동 저장
TAG=$(echo "$ARGUMENTS" | head -c 20 | tr -c 'a-zA-Z0-9가-힣' '-' | sed 's/--*/-/g; s/^-//; s/-$//' | head -c 30)
[[ -z "$TAG" ]] && TAG="inbox"

ATTACH_PATH=$(bash scripts/clipboard-to-attachment.sh "$TAG" 2>/dev/null || echo "")

if [[ -n "$ATTACH_PATH" ]]; then
  echo "📎 클립보드 이미지 자동 저장: $ATTACH_PATH"
  ATTACH_NOTE="

## 첨부
- [\`$ATTACH_PATH\`]($ATTACH_PATH)
  (사용자 클립보드에서 자동 첨부됨, $TS)
"
else
  ATTACH_NOTE=""
fi
```

2. **파일 생성**: `coordination/inbox/$TS.md`

   포맷:
   ```markdown
   # Inbox: <한줄 요약>

   - **id**: $TS
   - **created**: YYYY-MM-DD HH:MM KST
   - **author**: main-session (또는 실제 사용자 닉네임)
   - **status**: pending
   - **priority**: normal  (또는 high, low)

   ## 요청 내용

   $ARGUMENTS

   ## 참고 (있을 때만)
   - 관련 파일 / 이슈 / 보고서
   - 예상 범위 (db / backend / frontend)

   ## 제약
   - (사용자가 제시한 제약이 있으면 여기)
   ```

2.5. **inbox 본문에 첨부 섹션 자동 포함**
- 위 1.5에서 클립보드 이미지 저장됐으면 `$ATTACH_NOTE` 를 inbox md 끝에 자동 추가
- head 가 inbox 읽을 때 첨부 path 인지 → Read 툴로 이미지 분석 가능

3. **커밋 + 푸시** (이미지도 같이)
   - `git add "$COORD_ROOT/coordination/inbox/$TS.md"`
   - 첨부 있으면: `git add "$ATTACH_PATH"`
   - `git commit -m "inbox: <한줄 요약>${ATTACH_PATH:+ (+첨부 이미지)}"`
   - `git push "$REMOTE" "$WORK_BRANCH"`

4. **Discord 알림** (선택, 있으면 발송):
   - `bash scripts/notify-discord.sh "📥 신규 inbox" "<한줄 요약>\nid: $TS"`
   - 실패해도 무시 (알림은 부가기능)

5. **head 자동 spawn (Phase 2.6+)** — 사용자가 head 창 갈 필요 없음:

```bash
# head worktree 경로: PROJECT_ROOT 의 sibling
HEAD_WT="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"
LOCK_FILE="$HEAD_WT/coordination/HEAD_LOCK"

if [[ -f "$LOCK_FILE" ]]; then
  LOCK_TS=$(cat "$LOCK_FILE" 2>/dev/null)
  echo "⚠ head 이미 실행 중 (시작: $LOCK_TS) — 자동 spawn 생략"
  echo "수동 트리거: head 창에서 /inbox"
else
  # 락 생성 + head spawn (백그라운드)
  echo "$(date -Iseconds)" > "$LOCK_FILE"
  (
    cd "$HEAD_WT"
    claude -p "/inbox" \
      --permission-mode bypassPermissions \
      --model "${HEAD_MODEL:-claude-opus-4-6}" \
      --output-format json \
      > "/tmp/head-auto-$(date +%s).log" 2>&1
    rm -f "coordination/HEAD_LOCK"  # 종료 시 락 해제
  ) &

  HEAD_PID=$!
  echo "✅ head 자동 spawn 됨 (PID: $HEAD_PID, 백그라운드)"
fi
```

**효과**:
- 사용자는 `/inbox-send <내용>` 한 번만 → inbox 등록 + head 자동 처리 시작
- 새 세션 = 사법부 hooks 신선 로드 (항상 활성)
- LOCK_FILE 로 동시 head 실행 방지

6. **사용자에게 보고**:
   - 생성된 inbox 파일 경로, 커밋 해시
   - "head 자동 처리 시작 (백그라운드)" 또는 "이미 head 실행 중"
   - 모니터링: `tail -f /tmp/head-auto-*.log` 또는 `/status`
   - Discord 알림 대기 안내

## 품질 규칙

- 제목은 `$ARGUMENTS` 첫 문장 또는 핵심 단어로 짧게 (예: "매스 꼬임 삼각분할 교체")
- 요청이 모호하면 파일 생성 **전** 사용자에게 되물어라 (추측 금지)
- 이미 같은 내용의 pending 엔트리가 있으면 중복 방지 경고

## 지금 할 것

`$ARGUMENTS` 를 위 절차대로 처리하라.
---
description: 사법부 거부 에스컬레이션 승인 — approvals 파일 생성 + push + head 재진입 안내
argument-hint: <escalation-id> [선택: 승인 이유]
---
> v0.5.1+: 모든 경로/브랜치는 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
```

너는 op (메인 세션) 의 에스컬레이션 처리자다. 사용자가 사법부 거부를 검토 후 승인할 때 사용.

## 절차

### 1. 대상 escalation 확인
```bash
git fetch "$REMOTE" "$WORK_BRANCH" --quiet

# 인자에서 id 추출 (예: 20260415-161403)
ID="$ARGUMENTS_FIRST_TOKEN"
REASON="$ARGUMENTS_REST"

# escalation 파일 찾기 (work_branch 또는 origin/<head_branch>)
FILE=$(ls "$COORD_ROOT/coordination/escalations/"*${ID}*.md 2>/dev/null | head -1)
if [[ -z "$FILE" ]]; then
  # head_branch 에서도 검색
  FILE=$(git ls-tree -r "origin/$HEAD_BRANCH" coordination/escalations/ | grep "$ID" | awk '{print $4}' | head -1)
  [[ -n "$FILE" ]] && git show "origin/$HEAD_BRANCH:$FILE" > "$COORD_ROOT/coordination/escalations/$(basename $FILE)"
  FILE="$COORD_ROOT/coordination/escalations/$(basename $FILE)"
fi

if [[ -z "$FILE" ]] || [[ ! -f "$FILE" ]]; then
  echo "❌ escalation $ID 없음"
  exit 1
fi
```

### 2. 사용자에게 에스컬레이션 내용 표시
```
사법부 거부 내용:
- 거부된 action: <Tool> / <Input 요약>
- 판사 판결: <DENY 사유>
- worktree: <wt/...>
- 발생 시각: <ts>

승인 시 영향:
- head 가 해당 action 을 다시 시도할 수 있음
- 사법부 룰 우회 → 한 번만 효력
```

진행 확인 후 다음 단계.

### 3. approvals 파일 생성
```bash
TS=$(date +%Y-%m-%d-%H%M%S)
APPROVAL_FILE="$COORD_ROOT/coordination/approvals/${ID}.md"

cat > "$APPROVAL_FILE" <<EOF
# Approval: escalation $ID

- **escalation_id**: $ID
- **approved**: true
- **approved_at**: $(date -Iseconds)
- **approved_by**: user (via op /resolve-escalation)
- **reason**: ${REASON:-사용자 의도된 작업으로 확인}

## 적용 범위
- 본 escalation 의 단일 action 1회 한정 승인
- head 가 같은 action 을 재시도하면 통과 (사법부 1회 우회)
- 같은 패턴 다른 action 은 별도 escalation 발생

## escalation 원본 참조
[escalations/$(basename $FILE)](../escalations/$(basename $FILE))
EOF
```

### 4. escalation 파일 status 갱신
- `status: pending` → `status: resolved-approved`
- `resolved_at`, `resolved_by: user (via op)` 추가
- `approval_ref: approvals/${ID}.md` 링크

### 5. 커밋 + 푸시
```bash
git add "$COORD_ROOT/coordination/approvals/${ID}.md" "$COORD_ROOT/coordination/escalations/$(basename $FILE)"
git commit -m "approval: escalation ${ID} 사용자 승인

reason: ${REASON:-사용자 의도된 작업}
approved_via: op /resolve-escalation"
git push "$REMOTE" "$WORK_BRANCH"
```

### 6. Discord 알림
```bash
bash scripts/notify-discord.sh "✅ 에스컬레이션 승인" \
  "id: ${ID}\n사유: ${REASON}\nhead 재진입 가능"
```

### 7. head 자동 재spawn (Phase 2.6+)

```bash
HEAD_WT="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"
LOCK_FILE="$HEAD_WT/coordination/HEAD_LOCK"

if [[ -f "$LOCK_FILE" ]]; then
  echo "⚠ head 이미 실행 중 — 다음 /inbox 시 1.5단계가 approval 자동 발견"
else
  echo "$(date -Iseconds)" > "$LOCK_FILE"
  (
    cd "$HEAD_WT"
    claude -p "/inbox" \
      --permission-mode bypassPermissions \
      --model "${HEAD_MODEL:-claude-opus-4-6}" \
      --output-format json \
      > "/tmp/head-auto-$(date +%s).log" 2>&1
    rm -f "coordination/HEAD_LOCK"
  ) &
  echo "✅ head 자동 재진입 (PID: $!)"
fi
```

### 8. 사용자에게 보고
```
✅ 승인 완료
- approval: coordination/approvals/${ID}.md
- escalation 상태: resolved-approved
- head 자동 재진입 시작 (백그라운드)
  - 새 세션 = 사법부 활성, approval 발견 후 막혔던 action 재시도
- 모니터링: tail -f /tmp/head-auto-*.log 또는 /status
- 완료 알림: Discord 대기
```

## 금지
- escalation 없이 approval 생성 금지 (id 검증 필수)
- 의심스러운 action 자동 승인 금지 — 사용자가 명시적으로 reason 제공해야 함
- 같은 escalation 중복 승인 방지 (이미 resolved 면 경고)

## 지금 할 것
`$ARGUMENTS` (id + 선택 reason) 로 승인 처리.
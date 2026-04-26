---
description: 사법부 거부 에스컬레이션 거부 — head 가 다른 방식 찾도록 신호
argument-hint: <escalation-id> [거부 사유]
---
> v0.5.1+: 모든 경로/브랜치는 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
```

너는 op (메인 세션). 사법부 거부를 사용자가 검토 후 거부 유지 (= head 도 다른 방식 찾으라고 지시).

## 절차

### 1. 대상 escalation 확인
```bash
ID="$ARGUMENTS_FIRST_TOKEN"
REASON="$ARGUMENTS_REST"

FILE=$(ls "$COORD_ROOT/coordination/escalations/"*${ID}*.md 2>/dev/null | head -1)
if [[ -z "$FILE" ]]; then
  FILE=$(git ls-tree -r "origin/$HEAD_BRANCH" coordination/escalations/ | grep "$ID" | awk '{print $4}' | head -1)
  [[ -n "$FILE" ]] && git show "origin/$HEAD_BRANCH:$FILE" > "$COORD_ROOT/coordination/escalations/$(basename $FILE)"
  FILE="$COORD_ROOT/coordination/escalations/$(basename $FILE)"
fi

[[ ! -f "$FILE" ]] && { echo "❌ escalation $ID 없음"; exit 1; }
```

### 2. approvals 파일 생성 (거부 응답)
```bash
APPROVAL_FILE="$COORD_ROOT/coordination/approvals/${ID}.md"
cat > "$APPROVAL_FILE" <<EOF
# Approval: escalation $ID

- **escalation_id**: $ID
- **approved**: false
- **rejected_at**: $(date -Iseconds)
- **rejected_by**: user (via op /reject-escalation)
- **reason**: ${REASON:-사용자가 거부함, head 가 다른 방식 찾기 권장}

## head 에게 신호
- 본 action 은 사용자 의도 아님
- **다른 접근 방식 모색 필요**:
  - 가능하면 task 범위 안에서 해결
  - 정 안 되면 새 escalation 으로 다른 action 시도

## escalation 원본
[escalations/$(basename $FILE)](../escalations/$(basename $FILE))
EOF
```

### 3. escalation 파일 status 갱신
- `status: pending` → `status: resolved-rejected`
- `resolved_at`, `resolved_by: user (via op)` 추가

### 4. 커밋 + 푸시
```bash
git add "$COORD_ROOT/coordination/approvals/${ID}.md" "$COORD_ROOT/coordination/escalations/$(basename $FILE)"
git commit -m "approval: escalation ${ID} 사용자 거부

reason: ${REASON:-거부됨}
rejected_via: op /reject-escalation"
git push "$REMOTE" "$WORK_BRANCH"
```

### 5. Discord 알림
```bash
bash scripts/notify-discord.sh "❌ 에스컬레이션 거부" \
  "id: ${ID}\n사유: ${REASON}\nhead 가 다른 방식 모색"
```

### 6. 사용자에게 보고
```
❌ 거부 완료
- approval: coordination/approvals/${ID}.md (approved: false)
- escalation 상태: resolved-rejected
- 다음 단계: head 가 다른 방식 시도하거나, 작업 자체 abort 하고 보고
- 사용자가 새 inbox 로 다른 접근 지시 가능
```

## 지금 할 것
`$ARGUMENTS` 로 거부 처리.

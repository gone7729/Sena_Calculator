---
description: 전체 coordination 시스템 현황 한눈에 — inbox/escalations/sub 브랜치 상태
---
> v0.5.1+: 모든 경로/브랜치/sub 이름은 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
SUBS=()
while IFS= read -r s; do [[ -n "$s" ]] && SUBS+=("$s"); done < <(sub_names)
```

너는 op (메인 세션). 사용자가 "지금 뭐 돌아가?" 물었을 때 1초에 답할 수 있게 핵심 상태 요약.

## 절차

### 1. 최신화
```bash
# 모든 관련 브랜치 fetch (work_branch + head + subs)
git fetch "$REMOTE" "$WORK_BRANCH" "$HEAD_BRANCH" "${SUBS[@]/#/wt/}" --quiet
```

### 2. 데이터 수집

**Inbox 상태 (work_branch 기준)**
```bash
echo "=== 📥 Inbox 상태 ==="
for f in "$COORD_ROOT/coordination/inbox/"*.md; do
  [[ "$f" =~ _TEMPLATE|gitkeep ]] && continue
  ID=$(grep "^- \*\*id\*\*:" "$f" | head -1 | sed 's/.*: //')
  STATUS=$(grep "^- \*\*status\*\*:" "$f" | head -1 | sed 's/.*: //')
  TITLE=$(head -1 "$f" | sed 's/^# Inbox: //')
  echo "  [$STATUS] $ID — $TITLE"
done
```

**Escalation 대기 (work_branch + origin/head_branch)**
```bash
echo ""
echo "=== 🔔 에스컬레이션 (pending) ==="
for f in "$COORD_ROOT/coordination/escalations/"*.md; do
  [[ ! -f "$f" ]] && continue
  STATUS=$(grep "status:" "$f" | head -1)
  if echo "$STATUS" | grep -q "pending"; then
    ID=$(basename "$f" .md)
    REASON=$(grep -A1 "판사 판결" "$f" | tail -1 | head -c 80)
    echo "  ⚠ $ID"
    echo "    $REASON..."
  fi
done

# head 의 미반영 escalation 도 체크
git ls-tree -r "origin/$HEAD_BRANCH" coordination/escalations/ 2>/dev/null | grep -v "_TEMPLATE\|gitkeep" | while read -r line; do
  FPATH=$(echo "$line" | awk '{print $4}')
  LOCAL=$(basename "$FPATH")
  [[ ! -f "$COORD_ROOT/coordination/escalations/$LOCAL" ]] && echo "  ($HEAD_BRANCH 만 있음) $LOCAL"
done
```

**Sub 브랜치 상태**
```bash
echo ""
echo "=== 🌿 Sub 브랜치 vs $WORK_BRANCH ==="
for sub in "${SUBS[@]}"; do
  AHEAD=$(git rev-list --count "$WORK_BRANCH..origin/wt/$sub" 2>/dev/null || echo "?")
  BEHIND=$(git rev-list --count "origin/wt/$sub..$WORK_BRANCH" 2>/dev/null || echo "?")
  LATEST=$(git log "origin/wt/$sub" --oneline -1 2>/dev/null)
  echo "  wt/$sub: ahead=$AHEAD, behind=$BEHIND"
  echo "    최근: $LATEST"
done
```

**Head 브랜치 상태**
```bash
echo ""
echo "=== 🎯 $HEAD_BRANCH 상태 ==="
HEAD_AHEAD=$(git rev-list --count "$WORK_BRANCH..origin/$HEAD_BRANCH" 2>/dev/null || echo "?")
HEAD_BEHIND=$(git rev-list --count "origin/$HEAD_BRANCH..$WORK_BRANCH" 2>/dev/null || echo "?")
echo "  $HEAD_BRANCH: ahead=$HEAD_AHEAD, behind=$HEAD_BEHIND"
echo "  최근 review-inbox:"
git ls-tree -r "origin/$HEAD_BRANCH" coordination/review-inbox/ 2>/dev/null | grep -v "_TEMPLATE\|gitkeep" | tail -3 | awk '{print "    "$4}'
```

**메인 검토 대기 (review-inbox 미reviewed)**
```bash
echo ""
echo "=== 📝 검토 대기 review-inbox ==="
git ls-tree -r "origin/$HEAD_BRANCH" coordination/review-inbox/ 2>/dev/null | grep -v "_TEMPLATE\|gitkeep" | awk '{print $4}' | while read -r f; do
  CONTENT=$(git show "origin/$HEAD_BRANCH:$f" 2>/dev/null)
  if echo "$CONTENT" | grep -q "reviewed: false"; then
    BASENAME=$(basename "$f")
    echo "  📌 $BASENAME"
  fi
done
```

**STOP 시그널**
```bash
if [[ -f "$COORD_ROOT/coordination/STOP" ]]; then
  echo ""
  echo "🛑 STOP 시그널 활성 — 모든 sub 일시 중단됨"
fi
```

**최근 hotfixes**
```bash
echo ""
echo "=== 🔧 최근 hotfixes (3건) ==="
grep -A2 "^### " "$COORD_ROOT/coordination/hotfixes.md" 2>/dev/null | head -9
```

**Failed 보관 브랜치**
```bash
echo ""
echo "=== 💀 Failed 보관 브랜치 ==="
git branch -a --list "*failed/*" | head -5
```

### 3. 사용자 친화적 요약

데이터 수집 후 **한 화면 표 형태로 정리**:

```markdown
## 🎯 Coordination 현황 요약

### 진행 중
- 처리 대기 inbox: N건 (id 목록)
- pending escalation: M건 (id + 사유)
- 검토 대기 review-inbox: K건

### 완료 (최근)
- 머지된 inbox: <id> @ <시각>
- 마지막 hotfix: <제목> @ <커밋>

### 시스템 상태
- STOP: <ON/OFF>
- 사법부: <활성/비활성 — head 세션 시작 시점 기준 추정>
- failed/* 브랜치: N개 (정리 권장 여부)

### 다음 권장 액션 (제안)
- (있으면) "review-inbox 검토 필요: /review-inbox"
- (있으면) "에스컬레이션 응답 필요: /resolve-escalation X 또는 /reject-escalation X"
- (있으면) "STOP 해제 필요"
- 없으면 "조용함 — 다음 작업 지시 가능"
```

## 금지
- 단순 raw 출력 금지 — 사용자 친화적 요약 필수
- 추측성 정보 금지 (확실하지 않으면 "확인 필요" 표기)

## 지금 할 것
위 절차로 데이터 수집 + 요약 보고.

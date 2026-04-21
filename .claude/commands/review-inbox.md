---
description: head가 처리 완료한 review-inbox 엔트리를 검토 — pull + 요약 + 실제 diff 대조 + go/no-go 판정
argument-hint: (선택) 특정 엔트리 id 또는 "all"
---
> v0.5.1+: 모든 경로/브랜치/sub 이름은 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
# SUBS 배열 만들기
SUBS=()
while IFS= read -r s; do [[ -n "$s" ]] && SUBS+=("$s"); done < <(sub_names)
```

너는 메인 세션의 검토자다. head 가 처리 완료한 review-inbox 엔트리를 검증한다.

## 0-1. 모드 플래그 파싱 (v0.14+)

`$ARGUMENTS` 안에서 아래 플래그를 **가장 먼저** 추출해 동작 범위를 결정한다:

| 플래그 | 의미 | 수행 범위 |
|--------|------|----------|
| `--verdict-only` | 검증 + verdict 판정까지만 | 1~6 단계. **7단계 (머지) 절대 금지.** 종료. |
| `--merge-only` | 이미 verdict: go 판정 끝난 엔트리에 머지만 | 1~6 건너뜀. **해당 엔트리의 verdict 가 `go` 인지만 확인**, 아니면 즉시 중단. 그 후 7단계 수행. |
| `--auto-yes` | 7단계 사용자 확인 질문을 자동 yes | `--merge-only` 와 주로 함께 사용. 단독으로도 유효 (전체 흐름 중 최종 머지 확인만 자동). |
| (없음) | 기본 — 전체 흐름 (1~8) | 기존 동작 유지. 하위 호환. |

플래그를 제거하고 남은 토큰은 **inbox id** (또는 `all`) 로 간주. 플래그 2개 조합 가능 (e.g. `/review-inbox 2026-04-17-180512 --merge-only --auto-yes`).

**금지**:
- `--verdict-only` + `--merge-only` 동시 지정 → "플래그 충돌" 보고 후 중단.
- 플래그 해석 실패 시 → 사용자에게 확인 요청, 추측 금지.

## 절차

### 1. 최신화
- `git fetch "$REMOTE" "$WORK_BRANCH" "$HEAD_BRANCH" "${SUBS[@]/#/wt/}" --quiet`
- `git status --short` 로 로컬 uncommitted 확인
  - 있으면 사용자에게 먼저 처리 의향 확인 (stash/commit)
  - 없으면 다음
- `git pull --rebase "$REMOTE" "$WORK_BRANCH"` 로 원격 최신 반영
  - 충돌 시 중단 + 사용자 개입 요청

### 2. 대기 중인 검토 엔트리 찾기

**head 는 coordination 파일을 `$HEAD_BRANCH` 브랜치에 커밋함** — `$WORK_BRANCH` 에는 없음.
따라서 head 브랜치에서 직접 읽어야 함:

```bash
# head 브랜치의 review-inbox 파일 목록
git ls-tree -r "origin/$HEAD_BRANCH" coordination/review-inbox/ | grep -v "_TEMPLATE\|.gitkeep" | awk '{print $4}'

# 각 파일 내용 읽기
git show "origin/$HEAD_BRANCH:coordination/review-inbox/<file>"
```

- **reviewed: true** 헤더 없는 엔트리만 대상 (내용 확인으로 판별)
- `$ARGUMENTS` 가 특정 id 면 해당 엔트리만 대상
- 없으면:
  - "head 에 review-inbox 없음" 또는
  - "검토 대기 없음 (모두 reviewed)" 보고 후 종료

**대안** (더 간단): head worktree 를 직접 읽기
- `$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head/coordination/review-inbox/*.md` 직접 Read
- 단, head 가 아직 커밋 안 한 파일까지 포함 → 주의 필요
- 정식 운영 시엔 git show 방식 권장 (커밋된 것만)

### 3. 각 엔트리 검증 (시간순)

#### 3-1. review-inbox 파일 읽기
- inbox id, 처리 시각, 최종 상태 확인
- head 가 요약한 변경 내역 파악

#### 3-2. 원본 inbox 엔트리 대조
- `$COORD_ROOT/coordination/inbox/<id>.md` 읽고 **원래 요청 내용** 확인
- head 가 제대로 이해하고 분해했는지 판단

#### 3-3. Plan 파일 확인 (다단계 작업이면)
- `$COORD_ROOT/coordination/plans/<id>.md` 읽기
- 각 Step 의 status, commit hash, branch, failed_branch 확인
- Plan 에 명시된 Step 순서/의존성 대비 실제 수행 내역 대조

#### 3-4. 각 sub 보고서 심층 확인
- 각 `$COORD_ROOT/coordination/reports/wt-<sub>.md` 읽기 (sub 목록은 `${SUBS[@]}`)
- 보고서의 주장 (status, 변경 파일, 테스트 결과) 파악

#### 3-5. 실제 git diff 대조 ← 핵심
보고서/plan 주장과 실제 변경이 일치하는지 **반드시 검증**:
```bash
# 각 sub 브랜치 원격 최신화
git fetch "$REMOTE" "${SUBS[@]/#/wt/}" --quiet

# Plan 에 기록된 commit hash 가 실제 브랜치 tip 인지 확인
for sub in "${SUBS[@]}"; do
  echo "=== wt/$sub ==="
  git log "origin/wt/$sub" --oneline -5
done

# 각 sub 브랜치 대 work_branch 차이 (이번 inbox 처리로 쌓인 변경)
for sub in "${SUBS[@]}"; do
  echo "=== wt/$sub vs $WORK_BRANCH ==="
  git diff "$WORK_BRANCH...origin/wt/$sub" --stat
  # 파일명만 추출해 범위 이탈 체크
  git diff "$WORK_BRANCH...origin/wt/$sub" --name-only
done

# 특정 Plan 의 커밋만 보려면 (커밋 메시지에 inbox id 포함됨)
git log --all --grep="inbox: <id>" --oneline
```
검증 항목:
- 각 sub 이 자기 범위 밖 파일을 건드렸는지 (roles/*.md 참조)
- 보고서에 언급 안 된 추가 변경이 있는지
- Plan 의 Step commit hash 와 실제 브랜치 tip 일치 여부
- 실패 보관 브랜치 (`failed/wt-*-*`) 있으면 내용 요약
  ```bash
  git branch -a --list "failed/*" | head -20
  ```

#### 3-6. 품질 체크
- 각 sub 의 validation 명령 (config.yml 의 `subs[].validation`) 실행 가능
- 프로젝트 공통 타입/린트/테스트 실행 (예: `npm run type-check`, `npm run lint`)
- 관련 hotfix 확인: `$COORD_ROOT/coordination/hotfixes.md` 상단 10개와 관련성 재확인

#### 3-7. 통합 리스크 분석
여러 sub 변경이 서로:
- 충돌하는지 (같은 함수 시그니처 변경 등)
- 중복되는지 (동일 로직을 양쪽에서 구현)
- 누락됐는지 (DB 타입은 바뀌었는데 frontend 가 갱신 안 됨 등)

### 4. 검토 결과 작성
- 원본 review-inbox 엔트리 파일 상단에 추가:
  ```markdown
  ---
  reviewed: true
  reviewed_at: YYYY-MM-DD HH:MM KST
  reviewer: main
  verdict: go | needs-fix | block
  ---
  ```
- 바로 아래 "## 메인 검토 의견" 섹션 추가:
  ```markdown
  ## 메인 검토 의견

  ### 검증 결과
  - [x/✗] 보고서 주장과 실제 diff 일치
  - [x/✗] 범위 준수 (이탈 없음)
  - [x/✗] 각 sub validation 통과
  - [x/✗] 타입/테스트 통과
  - [x/✗] 통합 충돌 없음

  ### 발견 사항
  - (있으면 나열, 없으면 "없음")

  ### verdict 근거
  - <결론 이유>

  ### 다음 액션
  - <사용자에게 제안: 머지 / 수정 요청 / 추가 확인>
  ```

### 5. 사용자에게 보고

단일 엔트리면:
```
# <제목> 검토 완료
- id: <id>
- verdict: go | needs-fix | block
- 핵심 발견: <2~3줄>
- 권장: <액션>
```

여러 엔트리면 표 형식:
```
| id | verdict | 핵심 이슈 |
|----|---------|----------|
| ... | go | 없음 |
| ... | needs-fix | backend 에서 범위 이탈 |
```

### 6. 후속 처리

- **verdict: go** → 아래 7단계 (work_branch 머지) 진행 제안
- **verdict: needs-fix** → head 에 `/retry <inbox-id>` 실행 권장 (head 가 문제 Step 재dispatch)
  - 심각 문제면 사용자가 `/inbox-send 수정 요청 <상세>` 로 신규 엔트리 생성도 가능
- **verdict: block** → 심각 문제, 사용자 개입 필수, 머지 절대 금지

**`--verdict-only` 분기** (v0.14+): 여기서 **즉시 종료**. 5단계 보고까지만 수행하고 7단계로 진입하지 않는다. 사용자 (혹은 봇) 가 별도로 `--merge-only` 로 재호출해야 머지 진행.

### 7. work_branch 통합 머지 + 검증 (verdict: go 한정)

**`--merge-only` 진입 전 사전 체크** (v0.14+): 이 섹션을 독립 실행 (1~6 스킵) 하는 경우:

1. 해당 inbox id 의 review-inbox 파일을 `origin/$HEAD_BRANCH` 또는 head worktree 에서 읽기
2. `verdict:` 헤더 확인
   - `go` → 계속 진행
   - `needs-fix` / `block` / `merge-conflict` / `validation-failed` → **즉시 중단**, 사용자에게 "verdict 가 go 아님 (`$verdict`) — 머지 거부" 보고
   - verdict 헤더 없음 → **즉시 중단**, "아직 verdict 판정 안 됨 — `--verdict-only` 먼저 실행" 보고
3. `reviewed: true` 헤더도 확인. 없으면 중단.

사전 체크 통과 후에만 아래 Stage 1 진행.

**전략**: `$WORK_BRANCH` 에 임시 머지 → 검증 → OK 시 push / 실패 시 reset 롤백
(별도 head staging 브랜치 쓰지 않음 — `$WORK_BRANCH` 자체가 staging 역할)

사용자에게 명시적 확인 후 진행:
```
"검토 결과 go. wt/<sub>들을 $WORK_BRANCH 에 순차 머지 후 검증합니다.
 실패 시 자동 롤백 됩니다. 진행? (yes/no)"
```

**`--auto-yes` 분기** (v0.14+): 플래그가 있으면 위 확인 질문을 **건너뛰고 즉시 yes 로 간주**. 대신 채널에 "자동 yes 로 머지 진행 중" 한 줄 남기고 Stage 1 로 직행.

**yes 응답 시 — Stage 1: 로컬 머지**

```bash
cd "$PROJECT_ROOT"  # 루트 worktree

# 로컬 work_branch 를 원격 최신으로 (push 안된 것 있으면 먼저 처리)
git checkout "$WORK_BRANCH"
git fetch "$REMOTE" "$WORK_BRANCH" --quiet
git pull --rebase "$REMOTE" "$WORK_BRANCH"

# 안전망: 머지 전 시점 기록 (롤백용)
ROLLBACK_POINT=$(git rev-parse HEAD)
echo "rollback point: $ROLLBACK_POINT"

# 의존성 순서대로 머지 (plan 파일에 명시된 순서, 없으면 config.yml 의 subs 순서)
# plan 에서 실제 사용된 sub 만 포함하도록 MERGE_ORDER 조정
MERGE_ORDER=("${SUBS[@]}")
for sub in "${MERGE_ORDER[@]}"; do
  echo "=== merging wt/$sub ==="
  if ! git merge --no-ff "origin/wt/$sub" \
       -m "merge wt/$sub: <plan 제목> (inbox: <id>)"; then
    echo "❌ merge conflict on wt/$sub"
    # 즉시 롤백
    git merge --abort 2>/dev/null || true
    git reset --hard "$ROLLBACK_POINT"
    # 사용자 알림 + 중단
    exit 1
  fi
done
```

**머지 충돌 시:**
- 자동 rollback (git reset --hard $ROLLBACK_POINT)
- review-inbox 에 `verdict: merge-conflict` 기록
- Discord 알림: `⚠️ 머지 충돌 — 수동 해결 필요`
- 사용자에게 충돌 파일 목록 + 해결 가이드 제시
- 7단계 중단

**Stage 2: 통합 검증 (머지 성공 후)**

```bash
# 통합된 work_branch 상태에서 검증 (프로젝트별 명령은 config.yml 의 subs[].validation
# 또는 프로젝트 루트의 표준 명령 사용)
npm run type-check 2>/dev/null; TYPE_OK=$?
npm run lint 2>/dev/null; LINT_OK=$?

# 빌드 테스트 (선택적, 시간 걸림)
# npm run build

# 핵심 sub 들의 validation 명령 실행 (config.yml 기반)
VALIDATION_ERRORS=0
for sub in "${SUBS[@]}"; do
  cmd=$(sub_validation "$sub")
  if [[ -n "$cmd" ]] && [[ "$cmd" != "echo 'add validation cmd'" ]]; then
    echo "=== $sub: $cmd ==="
    eval "$cmd" || VALIDATION_ERRORS=$((VALIDATION_ERRORS+1))
  fi
done

if [[ $TYPE_OK -eq 0 && $VALIDATION_ERRORS -eq 0 ]]; then
  VALIDATION_OK=true
else
  VALIDATION_OK=false
fi
```

**Stage 3: push 또는 rollback**

```bash
if $VALIDATION_OK; then
  git push "$REMOTE" "$WORK_BRANCH"

  # 각 wt/<sub> 브랜치도 work_branch 최신으로 rebase (다음 작업 준비)
  for sub in "${SUBS[@]}"; do
    WT_DIR="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-$sub"
    [[ ! -d "$WT_DIR" ]] && continue
    cd "$WT_DIR"
    git fetch "$REMOTE" "$WORK_BRANCH" --quiet
    if ! git rebase "origin/$WORK_BRANCH"; then
      echo "⚠️ wt/$sub rebase 충돌 — 수동 해결 필요 ($WORK_BRANCH 머지는 성공)"
      git rebase --abort
    else
      git push -f "$REMOTE" "wt/$sub"  # rebase 후 force push
    fi
    cd - >/dev/null
  done
else
  # 검증 실패 → 롤백
  git reset --hard "$ROLLBACK_POINT"
  # push 는 안 함 (아직 안 올라감)

  # review-inbox 에 기록
  # verdict: validation-failed
  # 실패 요약 포함

  # Discord 알림
  bash scripts/notify-discord.sh "⚠️ 통합 검증 실패" \
    "로컬 $WORK_BRANCH 롤백됨. type: $TYPE_OK, validation_errors: $VALIDATION_ERRORS"
fi
```

**머지 성공 시:**

1. **review-inbox 엔트리 상단에 추가** (`$WORK_BRANCH` 의 복사본):
  ```markdown
  merged: true
  merged_at: YYYY-MM-DD HH:MM KST
  merge_commits:
    - wt/<sub1>: <hash>
    - wt/<sub2>: <hash>
    - ...
  base_commit_work_branch: <hash>
  ```

2. **inbox 엔트리 마킹** (`$WORK_BRANCH` 의 `coordination/inbox/<id>.md` — **메인 세션이 직접 수정, head 는 만지지 않음**):
  ```markdown
  - status: merged
  - started_at: <head 처리 시작, review-inbox 에서 추출>
  - completed_at: <head 처리 완료, review-inbox 에서 추출>
  - merged_at: <YYYY-MM-DD HH:MM KST>
  - review: [review-inbox/<file>](../review-inbox/<file>)
  - merge_commits: <해시들>
  ```
  이게 Phase 2.5+ 도입된 규칙 — inbox 파일은 **메인만** 수정. head 가 자기 브랜치에서 status 변경하던 방식 폐기 (이중 정본 방지).

3. **commit + push** — 한 커밋에 모두 포함:
  - `$COORD_ROOT/coordination/inbox/<id>.md` (status: merged + 메타)
  - `$COORD_ROOT/coordination/review-inbox/<file>.md` (merged 기록)

  ```bash
  git add "$COORD_ROOT/coordination/inbox/<id>.md" "$COORD_ROOT/coordination/review-inbox/<file>.md"
  git commit -m "coordination: <제목> merged 기록 (inbox: <id>)"
  git push "$REMOTE" "$WORK_BRANCH"
  ```
- main 세션에서 Discord 알림:
  ```bash
  bash scripts/notify-discord.sh "🎉 $WORK_BRANCH 머지 완료" "<plan 제목> | inbox: <id>"
  ```

### 8. 머지 후 권고 (main 세션용)

- `$WORK_BRANCH` → `$STABLE_BRANCH` 머지는 **절대 자동 수행 금지**
- 협업자 협의가 필요하므로 사용자에게 다음만 안내:
  > "$WORK_BRANCH 업데이트 완료. $STABLE_BRANCH 머지는 협업자와 협의 후 수동 진행하세요."

## 금지
- 검토 결과 없이 "OK" 판정 금지 (실제 diff 확인 필수)
- 자동 머지 금지 — **예외**: `--auto-yes` 플래그가 명시된 경우에 한해 사용자 확인 skip 허용 (v0.14+)
- `reviewed: true` 기록 누락 시 중복 검토 발생 — 반드시 마킹
- `--verdict-only` 로 호출됐는데 7단계까지 진입 금지 (플래그 무시 = bug)
- `--merge-only` 로 호출됐는데 사전 체크 (verdict: go 확인) 없이 머지 진입 금지

## 지금 할 것
1. `$ARGUMENTS` 에서 플래그 (`--verdict-only` / `--merge-only` / `--auto-yes`) 를 먼저 추출
2. 남은 토큰은 inbox id (또는 `all`, 비어있으면 전체)
3. 플래그에 맞춰 1~8 단계 중 해당 범위만 수행

# Coordination 트러블슈팅

> **템플릿 사용자**: 이 문서는 원본 DEALOS (브랜치 `3dview`, 디렉토리 `dealos/`) 기준 작성됨.
> 자기 프로젝트의 work_branch / project_name 으로 읽어 해석하면 됨. 정본은 [config.yml.example](config.yml.example).

Level 1 운영 중 발견된 이슈와 해결 방법 모음.

---

## 1. 슬래시 명령이 "없다" 나옴

### 증상
head 또는 sub 창에서 `/inbox`, `/go` 등 입력했는데 명령이 인식 안 됨.

### 원인
1. 현재 worktree 의 `.claude/commands/` 에 해당 파일이 없음
2. 파일은 있는데 Claude 세션 캐시가 오래됨

### 해결
```bash
# 1. 파일 확인
ls .claude/commands/

# 2. 없으면 원격에서 복구
git fetch origin <이 worktree 브랜치>
git reset --hard origin/<브랜치>

# 3. 그래도 안 보이면 VSCode 재시작
# Ctrl+Shift+P → Developer: Reload Window
```

### 예방
- `.claude/commands/` 는 tracked (git 공유) 상태 유지
- gitignore 에 포함시키지 않음

---

## 2. `claude -p` 가 Read 권한도 거부됨

### 증상
`--permission-mode acceptEdits` 로 sub 돌리면 `permission_denials: Read coordination/tasks/wt-*.md` 발생.

### 원인
`acceptEdits` 가 Edit 만 자동 승인하고 Read 는 여전히 막는 환경 설정 가능성.

### 해결
`--permission-mode bypassPermissions` 로 전환. sub worktree 격리 덕분에 안전:
- 각 sub 은 자기 디렉토리만 접근
- 역할 정의 (`roles/*.md`) 에 따른 범위 제한은 프롬프트 레벨에서 준수
- 범위 이탈 시 `failed/*` 브랜치 보관 후 reset 으로 자동 복구

### 현재 상태
`/inbox`, `/dispatch` 명령에 `bypassPermissions` 기본값 반영 필요 (Phase 2).

---

## 3. wt/head 가 3dview 와 심하게 어긋남 (rebase 충돌)

### 증상
head 창에서 `/inbox` 실행 시 `git rebase origin/3dview` 가 충돌.
또는 head 가 3dview 의 새 inbox 파일을 못 봄.

### 원인
- wt/head 가 자체 커밋을 많이 쌓는 동안 3dview 도 독립 진행
- coordination/ 파일 양쪽 수정 → rebase 충돌

### 해결
```bash
# 선행: source scripts/config.sh  (PROJECT_NAME / WORK_BRANCH / REMOTE / PROJECT_ROOT 로드)

# 1. wt/head 에서 보존할 산출물 확인 (review-inbox 등)
cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"
ls coordination/review-inbox/

# 2. 보존 필요한 파일을 work_branch 로 복사 (메인 세션에서)
cd "$PROJECT_ROOT"
git fetch "$REMOTE" wt/head
git show origin/wt/head:coordination/review-inbox/<file>.md > coordination/review-inbox/<file>.md
git add coordination/review-inbox/
git commit -m "coordination: <file> wt/head → $WORK_BRANCH 이관"
git push "$REMOTE" "$WORK_BRANCH"

# 3. wt/head reset
bash scripts/reset-wt-head.sh --apply
```

### 예방
- `/inbox` 명령 첫 단계 `fetch + rebase` 를 항상 수행
- 긴 기간 wt/head 가 divergent 하지 않도록 주기적 rebase

---

## 4. Sub 브랜치에서 `coordination/` 전체 삭제 커밋 발견

### 증상
`git diff 3dview...wt/<sub> --name-only` 에 `coordination/README.md` 등 대량 `D` (deleted) 표시.

### 원인 (과거 이슈)
초기 설계에서 "coordination 은 wt/head 에만 두고 sub 에서는 junction 으로 공유" 시도 → sub 가 coordination/ 삭제 + `.gitignore` 에 `coordination/` 추가.

### 해결 (이미 적용됨, 2026-04-15)
모든 sub 브랜치를 `origin/3dview` 로 reset → coordination/ 복원.
앞으로 sub 는 자기 작업에만 집중, coordination/ 는 전 worktree 에서 tracked.

### 재발 시
```bash
# 선행: source scripts/config.sh
for sub in $(sub_names); do
  cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-$sub"
  git reset --hard "origin/$WORK_BRANCH"
  git push -f "$REMOTE" "wt/$sub"
  cd -
done
```

---

## 5. 3dview 머지 시 `.claude/commands/go.md` add/add 충돌

### 증상
`git merge wt/<sub>` 실행 시 `.claude/commands/go.md` 에 add/add CONFLICT.

### 원인
각 sub 브랜치에 자기 버전의 `go.md` 가 tracked. 3dview 에는 없어야 하는 파일인데 merge 시 추가됨.

### 해결
3dview 에서 해당 파일 제거 (sub-only 파일임):
```bash
git rm .claude/commands/go.md
git commit --no-edit  # 머지 커밋 완료
```

두 번째 sub 머지에서 또 추가되면 같은 방식으로 제거.
모든 머지 완료 후 마지막에 한 번 정리 커밋:
```bash
git rm .claude/commands/go.md
git commit -m "coordination: 3dview 에서 sub /go 제거"
```

### 근본 해결 (Phase 2)
`.claude/commands/go.md` 는 3dview 에 두지 않음 (sub-only). merge 시 자동 제외되도록 `.gitattributes` 또는 custom merge driver 설정 검토.

---

## 6. Sub worktree 에서 `rebase` 가 uncommitted 변경으로 막힘

### 증상
```
error: cannot rebase: You have unstaged changes.
error: Please commit or stash them.
```

### 원인
head 가 task 파일 복사 등으로 sub worktree 에 untracked/modified 파일 남김. 이후 rebase 시 방해.

### 해결
```bash
# 선행: source scripts/config.sh
cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-<sub>"
# 복사된 stale 파일 제거 (head 가 넘긴 task/reports 복사본)
git checkout -- .
git clean -fd coordination/

# rebase 재시도
git rebase "origin/$WORK_BRANCH"
```

주의: `.claude/settings.local.json` 은 개인 설정이라 유지. 나머지 coordination/ 복사본만 제거.

---

## 7. Discord 알림이 안 옴

### 증상
head 작업 완료/실패 시 Discord 채널에 메시지 안 옴.

### 원인
1. `.env.local` 의 `DISCORD_WEBHOOK_URL` 미설정 또는 오타
2. 웹훅 URL 만료/삭제됨
3. `scripts/notify-discord.sh` 실행 권한 없음
4. Python 3 경로 문제 (한글 JSON 인코딩)

### 해결
```bash
# 1. 환경변수 확인
grep DISCORD .env.local

# 2. 수동 발송 테스트
bash scripts/notify-discord.sh "테스트" "메시지"

# 3. HTTP 400/404 나오면 웹훅 URL 재생성
# Discord 채널 설정 → 연동 → 웹훅 → URL 복사

# 4. 모든 worktree 에 새 URL 복사 (선행: source scripts/config.sh)
for sub in head $(sub_names); do
  WT="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-$sub"
  [[ -d "$WT" ]] && cp "$PROJECT_ROOT/.env.local" "$WT/.env.local"
done
```

---

## 8. Sub 가 실패했지만 failed/* 브랜치 안 만들어짐

### 증상
sub 작업 실패 후 `git branch --list "failed/*"` 에 새 브랜치 없음.

### 원인
- sub Claude 세션이 권한 문제로 `git branch` 실행 못함
- timeout 으로 강제 종료되어 실패 프로토콜 도달 못함

### 해결 (수동 복구)
```bash
# 선행: source scripts/config.sh
cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-<sub>"
TS=$(date +%Y%m%d-%H%M%S)
git branch "failed/wt-<sub>-$TS" HEAD
git reset --hard <task_start_commit>  # task 파일 헤더에 기록된 해시
git push "$REMOTE" "failed/wt-<sub>-$TS"
```

---

## 9. 메인 세션에서 `/review-inbox` 가 파일 못 찾음

### 증상
"처리할 review-inbox 없음" 이지만 head 가 완료 보고했다고 했음.

### 원인
head 가 `coordination/review-inbox/*.md` 를 wt/head 브랜치에만 커밋하고 3dview 에 없음. 메인 세션은 3dview 를 주로 봄.

### 해결
```bash
# wt/head 원격에서 직접 읽기
git fetch origin wt/head --quiet
git ls-tree -r origin/wt/head coordination/review-inbox/
git show origin/wt/head:coordination/review-inbox/<file>.md

# 3dview 로 복사 보존하려면
git show origin/wt/head:coordination/review-inbox/<file>.md > coordination/review-inbox/<file>.md
git add coordination/review-inbox/<file>.md
git commit -m "coordination: review-inbox 보존"
```

---

## 10. STOP 시그널 해제 후에도 sub 이 안 움직임

### 증상
`coordination/STOP` 파일 삭제 + 커밋했는데 다음 `/inbox` 에서 여전히 "STOP 상태" 라고 함.

### 원인
- 원격 push 누락
- head 가 캐시된 상태로 판단

### 해결
```bash
# 1. STOP 파일 확실히 제거
rm -f coordination/STOP
git rm coordination/STOP 2>/dev/null || true
git commit --allow-empty -m "coordination: STOP 해제"
git push "$REMOTE" "$WORK_BRANCH"

# 2. head worktree 에서 fetch + rebase (선행: source scripts/config.sh)
cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"
git fetch "$REMOTE" "$WORK_BRANCH" --quiet
git rebase "origin/$WORK_BRANCH"

# 3. /inbox 재시도
```

---

## 11. 사법부 거부로 head 멈춤 (Phase 2.5+)

### 증상
- Discord 에 "🔔 head 판사 거부 — 승인 요청" 알림
- head 작업 중단, escalation 파일 생성됨

### 진단
```bash
ls coordination/escalations/wt-head-judge-*.md
cat coordination/escalations/wt-head-judge-<id>.md
# "거부된 action" + "판사 판결" 확인
```

### 해결 (Phase 2.6 op 명령)

**판단: 정당한 거부였나?**

**A. 정당한 거부** → 승인 후 진행
```
/resolve-escalation <id> 의도된 작업이며 위험 없음
```
→ approval 자동 생성 + head 자동 재spawn + 막힌 작업 재시도

**B. 부적절한 거부 (head 가 실수했음)** → 거부 유지
```
/reject-escalation <id> 잘못된 접근, 다른 방식 찾아라
```
→ head 가 다른 방식 모색

**C. head 가 작업 자체를 잘못 이해** → 새 inbox 로 재지시
```
/inbox-send <상세 지시>
```

### 예방
- 판사 거부 사유 로그 정기 검토 (`/tmp/judge-log.txt`)
- 자주 거부되는 패턴은 1심 화이트리스트에 추가 (`scripts/judge-action.sh`)
- 의도된 위험한 작업 (예: 큰 cleanup) 은 **사전 approval 미리 작성**으로 통과

---

## 12. /inbox 가 ghost state 정리 못함 (Phase 2.5 도입 후)

### 증상
- /inbox 0단계 워킹트리 정리 단계에서 사법부에 차단
- escalation 발생: `git checkout -- coordination/` 거부

### 원인
이전 head 작업의 미커밋 파일들이 워킹트리에 남아 있음. 사법부가 "in-progress 데이터 손실 위험" 으로 판단.

### 해결
대부분의 경우 잔여 파일은 stale (이미 head 다른 커밋이나 3dview 에 흡수됨). 안전하게 승인:
```
/resolve-escalation <id> stale 파일 정리 — 실제 작업은 다른 곳에 보존됨
```

만약 정말 in-progress 작업 (안 커밋한 중요 변경) 있으면:
1. head worktree 가서 변경 확인: `git status --short` + `git diff`
2. 보존 가치 있으면 별도 커밋 후 reject
3. 없으면 approve

---

## 13. 토큰 임계 알림 안 옴

### 증상
사용량 80% 넘었는데 Discord 알림 못 받음

### 원인
- 토큰 추적은 **headless `claude -p` 호출** 만 추적
- Interactive (메인/head/sub VSCode chat) 사용은 미추적
- 즉 추정치 ≤ 실제 사용량
- 또는 token-budget.md 의 weekly_limit 너무 높음

### 해결
```bash
# 현재 추정 사용량 확인
bash scripts/check-token-threshold.sh

# 한도 조정 (token-budget.md 의 weekly_limit)
# 보수적으로 잡기: Anthropic 콘솔의 실제 사용량 / 추정 비율 곱하기
```

### 예방
- 주기적으로 Anthropic 콘솔 확인 + token-budget.md 한도 보정
- 큰 헤드리스 작업 후 `/token-status` 호출 (자동 임계 체크)

---

## 14. 토큰 리셋 알림이 월요일에 안 옴

### 증상
월요일 00:00 KST 지났는데 🔄 리셋 알림 안 받음

### 원인
리셋은 **사용 추적 시점에 트리거** — 월요일 첫 헤드리스 작업이 일어나야 감지됨.
주말 내내 작업 안 했다면 월요일에 첫 작업 시작 시점에 알림.

### 해결
**즉시 리셋 확인하고 싶으면:**
```
/token-status
# 또는
bash scripts/check-token-threshold.sh
```
→ 자동으로 리셋 감지 + 알림 발송

### 자동화 (선택)
Claude Code 의 `schedule` 기능으로 매주 월요일 09:00 KST 자동 호출 등록 가능.

---

## 15. 클립보드 이미지 자동 첨부 안 됨

### 증상
`/inbox-send` 시 첨부 path 가 inbox 본문에 안 보임

### 원인
- 클립보드에 이미지가 실제로 없음 (Win+Shift+S 후 파일로 저장만 했고 클립보드 아님)
- PowerShell 호출 실패 (권한/경로 문제)

### 해결
```bash
# 클립보드 상태 확인
powershell.exe -Command "if ([System.Windows.Forms.Clipboard]::ContainsImage()) { 'HAS_IMAGE' } else { 'NO_IMAGE' }"

# 수동 테스트
bash scripts/clipboard-to-attachment.sh test
```

### 예방
- Win+Shift+S 캡처 후 **저장 안 하고 바로** /inbox-send 호출
- 저장+클립보드 동시 원하면 캡처 도구 설정 확인

---

## 16. upgrade-coord.sh 실패 복구 (v0.3+)

### 증상
`bash scripts/upgrade-coord.sh` 실행 중 중단 / 에러 / 잘못된 결과.

### 원인 유형
1. **소스 획득 실패** — gh release 태그 미존재 / 네트워크 / 권한
2. **백업 없이 중단** — `--skip-backup` 사용 후 쓰기 도중 실패
3. **manual-merge 잘못 승인** — .claude/commands/*.md 가 원치 않는 버전으로 교체됨
4. **subtree pull 충돌** — 사용자가 `.coord/` 내부 파일 수정 → merge 충돌

### 해결

**A. 백업에서 롤백 (가장 안전)**
```bash
# 가장 최근 백업 확인
ls -lt .coord-backup/ | head -5

# 롤백 (덮어쓰기)
rsync -a .coord-backup/<timestamp>/ ./
# MANIFEST.md 도 같이 복원되는데, 무시해도 됨 (다음 업그레이드 시 덮임)

# 또는 git 으로 되돌리기 (커밋 전이어야 함)
git checkout -- scripts/ .githooks/ templates/ docs/
git checkout -- coordination/*.md .claude/commands/
```

**B. 특정 파일만 되돌리기**
```bash
# 백업에서 한 파일만 복원
cp .coord-backup/<timestamp>/.claude/commands/inbox-send.md .claude/commands/inbox-send.md

# 또는 git 에서
git checkout HEAD -- .claude/commands/inbox-send.md
```

**C. subtree pull 충돌**
```bash
# 충돌 파일 확인
git status | grep "both modified"

# 충돌 해결 후
git add <resolved-files>
git commit   # subtree merge commit 완성

# 그 후 .claude/ 재동기화만 따로 실행
bash scripts/upgrade-coord.sh --mode=subtree --force --skip-backup
```

**D. .coord-version 수동 복구**
자동 갱신 실패 시:
```bash
# 현재 버전 기록되지 않았으면 수동 작성
cat > coordination/.coord-version <<EOF
version: v0.3
upgraded_at: $(date -Iseconds)
upgraded_from: v0.2
template_url: https://github.com/gone7729/coord-template.git
install_mode: flat
EOF
```

### 예방
- 항상 `--dry-run` 먼저 실행
- `--skip-backup` 은 정말 긴급할 때만
- 업그레이드 전 `git status` 가 clean 한지 확인 (자동 점검되지만 `--force` 쓸 땐 본인 확인)
- manual-merge prompt 는 신중히 — diff 읽고 `n` 으로 건너뛰는 것이 default 선택

---

## 빠른 진단 체크리스트

작업 안 될 때 순서대로 확인:

1. [ ] `git status` — 로컬 uncommitted 있나?
2. [ ] `git fetch --all` 후 각 브랜치 최신 상태 확인
3. [ ] `.env.local` 에 필요 변수 다 있나? (`DISCORD_WEBHOOK_URL`, `NEXT_PUBLIC_*`)
4. [ ] `.claude/commands/` 파일 존재?
5. [ ] `coordination/STOP` 있나?
6. [ ] `git branch --list "failed/*"` 에 방해되는 잔여 브랜치?
7. [ ] 원격 sub 브랜치 `origin/wt/*` 가 최신인가?

---

## 긴급 복구 시나리오

### "전부 꼬였다. 처음부터 다시 시작하고 싶다"

극단적이지만 안전한 방법:
```bash
# 선행: source scripts/config.sh

# 1. 모든 sub + head worktree reset
for sub in head $(sub_names); do
  cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-$sub"
  git fetch "$REMOTE" "$WORK_BRANCH" --quiet
  git reset --hard "origin/$WORK_BRANCH"
  git push -f "$REMOTE" "wt/$sub"
  cd -
done

# 2. 진행 중 inbox 정리 (필요 시)
cd "$PROJECT_ROOT"
# 개별 inbox 파일 확인 후 status 수정 또는 삭제

# 3. STOP 해제
rm -f coordination/STOP

# 4. Discord 로 상태 보고
bash scripts/notify-discord.sh "🔄 시스템 리셋" "전체 worktree 를 origin/3dview 로 정렬"
```

⚠ sub 의 **push 안 한 로컬 작업이 있다면 사라짐**. 먼저 각 sub worktree 의 `git log HEAD ^origin/wt/<sub>` 로 확인.

# Coordination 시스템 사용 가이드

> **템플릿 사용자**: 이 문서는 원본 DEALOS (브랜치 `3dview`, 디렉토리 `dealos/`) 기준 작성됨.
> 자기 프로젝트의 work_branch / project_name 으로 읽어 해석하면 됨. 정본은 [config.yml.example](config.yml.example).

DEALOS 병렬 Claude 세션 오케스트레이션 시스템 사용법.

---

## 1. 개념 요약

### 왜 필요한가
- 여러 Claude 세션을 동시에 운영해 작업 병렬화
- 세션 간 직접 통신 불가 → **파일 기반 협업** (git + 로컬 파일)
- 머신 밖에서도 (모바일 등) 작업 지시 가능

### 구성 요소

```
┌─────────────────────────────────────────────────────────┐
│                  사용자 (어디서든)                       │
│   - GitHub 모바일 / 웹 / 로컬 VSCode                    │
│   - /inbox-send <지시>  → git push                      │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│            dealos-wt-head  (오케스트레이터)              │
│   - /inbox  → pull → 분해 → dispatch → review-inbox     │
│   - 코드 직접 수정 안 함                                 │
└─────────────────────────────────────────────────────────┘
                          ↓
┌──────────┬──────────────┬──────────────────────────────┐
│ wt-db    │ wt-backend   │ wt-frontend                  │
│ (DB/SQL) │ (로직/계산)  │ (UI + 디자인)                │
│  /go     │  /go         │  /go                         │
└──────────┴──────────────┴──────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│            dealos/  (메인, 검토자)                       │
│   - 이 세션에서 최종 검토 + /fix 로 핫픽스              │
│   - 사용자에게 최종 보고                                 │
└─────────────────────────────────────────────────────────┘
```

### 브랜치 구조
| Worktree | 브랜치 | 포트 | 역할 |
|----------|--------|------|------|
| `dealos/` | `3dview` | 3000 | 메인 + 검토 |
| `dealos-wt-head/` | `wt/head` | 3099 | 오케스트레이터 |
| `dealos-wt-db/` | `wt/db` | 3001 | DB/SQL |
| `dealos-wt-backend/` | `wt/backend` | 3002 | 로직/계산 |
| `dealos-wt-frontend/` | `wt/frontend` | 3003 | UI/디자인 |

---

## 2. 일상 작업 시나리오

### 시나리오 A: 로컬에서 간단한 버그 수정

가장 빈도 높은 흐름. 메인 세션(이 Claude)에서 직접 수정.

```
1. 메인 창(회색)에서:
   /fix 매스 꼬임 earcut → libtess 교체

2. Claude가 원인 분석 + 수정 + 사용자 승인 후 커밋
3. coordination/hotfixes.md 에 자동 엔트리 추가
4. sub 들은 다음 /go 실행 시 이 변경 자동 인지
```

### 시나리오 B: 복잡한 교차영역 작업 (로컬 head 경유) — 다단계 Plan 흐름

DB 스키마 + 로직 + UI 를 모두 건드리는 작업.

```
1. 메인 창에서:
   /inbox-send setback 캐시 TTL을 7일로 확장, 관련 DB/로직/UI 모두 갱신

2. 자동으로 coordination/inbox/YYYY-MM-DD-HHMMSS.md 생성 + 커밋 + 푸시
3. Discord 알림 발송

4. head 창(초록)으로 이동:
   /inbox

5. head Claude가 자동으로:
   (a) git pull 최신화
   (b) 관련 파일 git log 조사 + hotfixes 확인
   (c) coordination/plans/<id>.md 에 다단계 Plan 작성:
       - Step 1: db — 스키마 컬럼 추가 [wt/db]
       - Step 2: backend — 캐시 정책 갱신 (depends: Step 1) [wt/backend]
       - Step 3: frontend — UI 설정값 연결 (depends: Step 2) [wt/frontend]
   (d) Plan 순차 실행:
       - Step 1 → task 작성 → headless sub 실행 → 보고서 수집
       - 성공 시: wt/db 브랜치에 커밋 + origin 푸시 + Plan 갱신
       - Step 2 진행 (Step 1 결과 참조)
       - Step 3 진행
   (e) 모두 완료 → review-inbox 작성 + Discord 알림

6. 메인 창에서:
   /review-inbox

7. 내가 (이 Claude가):
   - 각 sub 브랜치(wt/db, wt/backend, wt/frontend) 실제 커밋 대조
   - 범위 이탈/SSOT 위반 체크
   - 통합 리스크 분석
   - go/no-go 판정

8. go 판정 후 사용자가 머지 (wt/db → wt/backend → wt/frontend → 3dview)
```

### 시나리오 B-대체: 중단/실패 시

Step 2 에서 실패 발생:
- sub worktree 변경을 `failed/wt-backend-<ts>` 브랜치에 보관 + origin 푸시
- 해당 Step status: failed
- 의존성 있는 Step 3: blocked
- Plan status: blocked
- inbox 엔트리 status: partial
- Discord 알림 발송
- 사용자 개입: `/inbox-send 수정 요청 — 실패 원인은 X, 다른 접근 Y` 로 재지시

### 시나리오 C: 외출 중 원격 지시 (모바일)

머신 앞이 아닐 때.

```
1. 폰에서 GitHub 앱 열기 → dealos 저장소 → coordination/inbox/
2. "+" 버튼 → 새 파일 생성: 2026-04-15-1430.md
3. 내용:
   # Inbox: P0-4 자동 구역 매칭 버그 재현 시도
   - id: 2026-04-15-1430
   - created: 2026-04-15 14:30 KST
   - status: pending
   - priority: high

   ## 요청 내용
   자동 구역 매칭에서 pnu가 null 인 경우 오류. 재현 후 근본 원인 파악.

4. 커밋 (모바일 GitHub 앱에서 가능)
5. 로컬 돌아와서 head 창에서:
   /inbox
6. 결과는 review-inbox/ 에 — GitHub 웹으로 모바일에서도 확인 가능
```

### 시나리오 D: 긴급 중단

작업 중 "이건 아니다" 싶을 때.

```
1. 아무 창에서:
   /inbox-send STOP
2. coordination/STOP 파일 생성 + 커밋 + 푸시 + Discord 알림
3. 실행 중인 모든 sub 은 매 단계 시작 전 STOP 확인 → abort
4. sub 은 변경을 failed/wt-<name>-<ts> 브랜치로 보관 후 reset
5. 재개: coordination/STOP 파일 삭제 + 커밋 → 다시 /inbox 가능
```

### 시나리오 E: sub 에스컬레이션 (범위 초과 요청)

sub 이 작업 중 범위 밖 수정이 필요함을 발견.

```
1. sub 이 자동으로:
   - coordination/escalations/wt-<name>-<ts>.md 작성
     (reason / requested_scope / impact / status: pending)
   - 현재 작업 중단
2. head 가 감지 → Discord 알림: "🔔 wt-<name> 승인 요청"
3. 사용자가 GitHub 모바일/웹에서:
   - coordination/approvals/<ts>.md 생성:
     ```
     approved: true
     additional_scope: <허용할 추가 경로>
     note: <조건/지침>
     ```
   - 커밋
4. head 다시 /inbox 호출 → sub 재개 (확장된 범위로)
```

---

## 3. 슬래시 명령 레퍼런스

### 메인 세션 (op, `dealos/`)

| 명령 | 용도 |
|------|------|
| `/inbox-send <내용>` | 작업 지시 등록 + Discord + **head 자동 spawn** (Phase 2.6+) + 클립보드 이미지 자동 첨부 |
| `/inbox-send STOP` | 긴급 중단 시그널 |
| `/fix <내용>` | 메인이 직접 버그 수정 + 승인 + hotfixes.md 자동 로그 |
| `/review-inbox [id]` | head 처리 결과 검증 + verdict + (go 시) 3dview 머지 |
| `/resolve-escalation <id> [이유]` | 사법부 거부 승인 → approvals 생성 + head 자동 재진입 |
| `/reject-escalation <id> [이유]` | 사법부 거부 유지 → head 가 다른 방식 모색 |
| `/status` | 시스템 현황 한눈 (inbox/escalation/sub/STOP/lock) |
| `/token-status` | 토큰 사용량 + 임계 + 리셋 상태 + 자동 리셋 트리거 |

### Head 세션 (`dealos-wt-head/`)

| 명령 | 용도 |
|------|------|
| `/inbox` | inbox 처리: rebase → STOP/escalation 확인 → plan → dispatch → review-inbox + 토큰 추적 |
| `/plan <내용>` | inbox 없이 즉석 task 분해 (수동 운영) |
| `/dispatch <sub...>` | 지정된 sub 순차 헤드리스 실행 |
| `/retry <inbox-id>` | 검증 실패 plan 재시도 (최대 2회) |

### Sub 세션 (각 worktree)

| 명령 | 용도 |
|------|------|
| `/go` | task 파일 읽고 자동 실행 + STOP 체크 + 실패 시 failed/* 브랜치 보관 + 자동 commit/push |

---

## 3.5. 파일 쓰기 권한 (이중 정본 방지)

`coordination/` 의 모든 파일은 **단일 작성자** 에게 귀속. 두 브랜치에서 같은 파일 경쟁 수정 금지.

### 메인 세션만 쓰기 (3dview 정본)
- `inbox/*.md` — `/inbox-send` 로 생성, `/review-inbox` 머지 성공 시 `status: merged` 마킹
- `hotfixes.md` — `/fix` 로만 추가
- `roles/*.md` — 구조 규약 (거의 불변)
- `README.md`, `USAGE.md`, `ROADMAP.md`, `TROUBLESHOOTING.md` — 문서
- `token-budget.md` — 사용자/메인 갱신
- `approvals/*.md` — 에스컬레이션 승인 응답

### Head 세션만 쓰기 (wt/head 브랜치)
- `plans/*.md` — 다단계 실행 계획
- `tasks/*.md` — sub 지시서
- `reports/*.md` — sub 보고서 수집본
- `review-inbox/*.md` — 처리 완료 결과 (reviewed 마킹은 메인)
- `STATUS.md` — 실시간 현황
- `token-log.md` — 사용량 기록 (추가 only)

### Sub 세션 쓰기 (wt/<name> 브랜치)
- 자기 `reports/wt-<name>.md` (head 가 복사해감)
- `escalations/wt-<name>-*.md` (범위 초과 요청)

### 이전 이슈 (해결됨)
Round 2 처리 시 head 가 `inbox/*.md` 의 status 를 wt/head 에서 수정 → 메인이 관리하는 3dview 쪽과 divergence 발생. 이후 규칙 명확화 + pre-commit hook 으로 구조적 차단.

### pre-commit hook 강제
`.githooks/pre-commit` 이 브랜치별 허용 경로 regex 로 커밋 시점에 차단.
우회 필요 시 `git commit --no-verify` (권장 안 함).

## 3.6. 사법부 시스템 (Phase 2.5)

Head 의 모든 Write/Edit/Bash 동작은 PreToolUse hook 으로 자동 심사:

**3심 구조:**
1. **1심 화이트리스트**: git status/log/diff, ls/cat, npm run check 등 → 즉시 허가 (<100ms)
2. **2심 블랙리스트**: rm -rf /, sudo, curl|sh, main/3dview 강제 푸시 → 즉시 거부
3. **2.5심 사용자 승인**: 최근 1시간 내 approval 있으면 통과 (1회용)
4. **3심 LLM 판사**: Sonnet 4.6 가 맥락 (역할/task/plan) 기반 판단 (~2초, ~$0.01)

**거부 시 자동 흐름:**
- `coordination/escalations/wt-head-judge-<ts>.md` 생성
- Discord "🔔 head 판사 거부 — 승인 요청" 알림
- 사용자가 op 에서 `/resolve-escalation <id>` 또는 `/reject-escalation <id>` 응답
- 승인 시 head 자동 재spawn (1.5단계가 approval 발견 → 막혔던 작업 재시도)

**적용 범위:** wt/head 만. sub 는 추후 확장 후보.

## 3.7. op→head 자동 spawn (Phase 2.6)

`/inbox-send`, `/resolve-escalation` 끝에 **head 백그라운드 자동 spawn**:
```bash
# config.sh 로드 후 (source scripts/config.sh)
cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"
claude -p "/inbox" --permission-mode bypassPermissions --model "${HEAD_MODEL}" ... &
```

**효과:**
- 사용자가 head 창 가서 `/inbox` 칠 필요 없음
- `coordination/HEAD_LOCK` 파일로 동시 실행 방지
- 새 세션 = 사법부 hooks 신선 로드 = 항상 활성

## 3.8. 클립보드 이미지 자동 첨부

`/inbox-send` 시 클립보드에 이미지 있으면 자동 저장 + inbox 본문에 path 명시:
```
1. 사용자: Win+Shift+S 캡처 (클립보드 보관)
2. /inbox-send 매스 꼬임 ...
3. op 자동:
   - scripts/clipboard-to-attachment.sh 호출
   - coordination/inbox/attachments/<ts>-<tag>.png 저장
   - inbox 본문에 [path] 삽입
   - git add (이미지 + inbox) + commit + push
4. head 가 inbox 처리 시 path 발견 → Read 툴로 이미지 분석
```

## 3.9. 토큰 사용량 추적 + 임계/리셋 알림

**자동 추적:**
- head 의 `/inbox` 가 sub Step 완료마다 `scripts/track-token-usage.sh` 호출
- 헤드리스 `claude -p --output-format json` 의 토큰 사용량 파싱
- `coordination/token-log.md` 에 누적 + `token-budget.md` 의 used_tokens 갱신

**임계 알림 (Discord):**
- 10/20/30/40/50/60/70/80/90/95/96/97/98/99% 도달 시 1회 알림
- 80% 이상 ⚠️ warning, 90%+ critical, 95%+ 🚨 1% 단위

**자동 리셋:**
- 매주 월요일 00:00 KST 후 첫 추적 호출 시 자동 감지
- `🔄 토큰 카운터 리셋 — 새 주차 시작` Discord 알림
- used_tokens, last_alerted_threshold 초기화

**제약:**
- headless `claude -p` 호출만 추적 (interactive 세션 미추적)
- 실제 사용량 ≥ 추적 사용량 (보수적 한도 설정 권장)
- Claude Max 플랜의 정확한 quota API 없음 (사용자 설정 한도 기반)

## 4. 파일 레이아웃

```
coordination/
├── USAGE.md              ← 이 문서
├── README.md             ← 전체 구조 설명
├── STATUS.md             ← 현재 진행 현황판 (head가 갱신)
├── hotfixes.md           ← 메인 세션 핫픽스 로그 (sub가 참고)
├── token-budget.md       ← 토큰 예산 관리
│
├── inbox/                ← 들어온 지시서 (원격/로컬)
│   ├── 2026-04-14-1430.md
│   └── attachments/      ← 첨부 이미지 (클립보드 자동 저장)
│       └── 20260415-173513-가각.png
│
├── plans/                ← inbox별 다단계 실행 계획
│   ├── _TEMPLATE.md
│   └── 2026-04-14-1430.md
│
├── tasks/                ← head가 분해한 sub별 지시서 (plan의 각 Step)
│   ├── _TEMPLATE.md
│   ├── wt-db.md
│   ├── wt-backend.md
│   └── wt-frontend.md
│
├── reports/              ← sub 완료 보고서
│   ├── _TEMPLATE.md
│   ├── wt-db.md
│   └── wt-backend.md
│
├── review-inbox/         ← head가 모은 최종 결과 (사용자 검토용)
│   └── 2026-04-14-1430-done.md
│
├── escalations/          ← sub 범위 초과 요청
│   └── wt-backend-20260414-1530.md
│
├── approvals/            ← 사용자 승인 응답
│   └── 20260414-1530.md
│
├── roles/                ← 각 sub 역할 정의 (불변)
│   ├── db.md
│   ├── backend.md
│   └── frontend.md
│
└── STOP                  ← (있을 때만) 긴급 중단 시그널
```

---

## 5. 알림 (Discord)

### 발송 조건
- 신규 inbox 엔트리 등록
- head 처리 완료 (성공/실패)
- **Step 별 — 각 sub 완료 시점마다** (`notify-step.sh` 표준 포맷)
- 에스컬레이션 (승인 요청)
- STOP 시그널
- 실패/오류

### 수동 발송
```bash
# 범용
bash scripts/notify-discord.sh "<title>" "<message>"

# Step 진행률 (head 가 plan 실행 중 sub 완료마다 호출)
bash scripts/notify-step.sh <plan_id> <step>/<total> <sub_name> <status> [<summary>]
# 예: bash scripts/notify-step.sh plan-abc 2/3 backend ok "type-check pass"
```

### 알림 채널 변경
`.env.local` 의 `DISCORD_WEBHOOK_URL` 수정 후 모든 worktree 에 복사:
```bash
# config.sh 로드 후
source scripts/config.sh

# head + 모든 sub worktree 에 .env.local 복사
for sub in head $(sub_names); do
  WT="$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-$sub"
  [[ -d "$WT" ]] && cp "$PROJECT_ROOT/.env.local" "$WT/.env.local"
done
```

---

## 6. 안전 장치

### STOP 시그널
- `coordination/STOP` 파일 존재 시 sub 이 **매 단계 시작 전** 확인 후 abort
- 재개는 파일 삭제 + 커밋

### 실패 보관 브랜치
- sub 실패 시 `failed/wt-<name>-YYYYMMDD-HHMMSS` 브랜치에 변경 내용 보존
- 원래 브랜치는 `task_start_commit` 으로 reset → 재시도 가능
- 30일 이상 된 보관 브랜치는 `bash scripts/cleanup-failed-branches.sh --apply` 로 정리

### 에스컬레이션
- sub 은 범위 밖 작업 시 반드시 `escalations/` 에 기록 후 중단
- 무단 범위 확장 금지 (CLAUDE.md 규칙)

### 토큰 예산
- `coordination/token-budget.md` 에서 주간 한도/사용량 추적
- head 가 dispatch 전 예상치 vs 남은 토큰 비교 → 초과 시 사용자 확인

### 핫픽스 동기화
- 메인의 `/fix` 는 `hotfixes.md` 에 자동 로그
- sub 의 `/go` 는 시작 시 hotfixes 읽어 중복/영향 체크

---

## 7. 워크트리 관리

### 생성
```bash
bash scripts/setup-worktree.sh <name> <port> <base-branch>
# 예: bash scripts/setup-worktree.sh new-feature 3004 "$WORK_BRANCH"
# (base-branch 생략 시 config.yml 의 git.work_branch 가 기본값)
```

### 현황
```bash
bash scripts/list-worktrees.sh
```

### 삭제
```bash
bash scripts/remove-worktree.sh <name>
```

### 일괄 열기
```bash
bash scripts/open-worktrees.sh       # 4개 sub
bash scripts/open-worktrees.sh --all # 메인 포함 5개
```

또는 Windows 탐색기에서 `open-worktrees.cmd` 더블클릭.

### VSCode 창 색상
- 메인: ⚫ 회색
- Head: 🟢 초록
- DB: 🔵 파랑
- Backend: 🟣 보라
- Frontend: 🟠 주황

`.vscode/settings.json` 에 정의 (gitignored, worktree-local).

---

## 8. 트러블슈팅

### "task 파일 없음" 에러 (sub /go)
head 에서 아직 task 분해 안 됨. 먼저 head 에서 `/inbox` 또는 `/plan` 실행.

### Sub 가 동일 커밋을 중복 처리
inbox 엔트리 `status: pending → processing` 전환 실패. 수동으로 엔트리 헤더 편집.

### Discord 알림 안 옴
1. `.env.local` 의 `DISCORD_WEBHOOK_URL` 확인
2. `bash scripts/notify-discord.sh "test" "msg"` 수동 실행
3. HTTP 400 → 웹훅 URL 재생성 (Discord 채널 설정)

### Upstream rebase 충돌
sub 에서 `git fetch "$REMOTE" "$WORK_BRANCH" && git rebase "origin/$WORK_BRANCH"` 시 충돌:
1. 메인의 최근 변경이 sub 범위와 겹침 → 보고서에 `blocked` 기록 후 사용자 개입
2. 간단 충돌이면 sub 이 해결 후 계속 (헤드리스에선 자동 abort)

### STOP 해제 후에도 sub 가 멈춘 상태
1. `coordination/STOP` 파일 삭제됐는지 확인
2. `git status` 로 파일 실제 제거 확인
3. 필요시 head 재시작 (`/inbox` 재실행)

### Failed 브랜치 너무 많음
```bash
bash scripts/cleanup-failed-branches.sh          # 삭제 후보 목록
bash scripts/cleanup-failed-branches.sh --apply  # 실제 삭제 (30일+)
```

### 세션 간 파일 동기화 안 됨
- coordination/ 는 git 공유 → 반드시 커밋 + 푸시 + 다른 worktree 에서 pull/rebase
- tasks/reports 는 dispatch 시점에 file-copy 방식으로도 전파 (headless 운용 시)

---

## 9. 권장 운영 규칙

### 커밋 품질
- `/inbox-send` 가 자동 커밋 — 메시지 포맷 `inbox: <요약>` 유지
- `/fix` 도 자동 — `fix: <요약>` 또는 한글 스타일 일치
- 수동 커밋은 `docs:`, `refactor:`, `test:` 등 접두어 유지

### 머지 순서 (PR 시)
- 의존성: db → backend → frontend
- 같은 sub 에서 여러 작업 쌓이면 작은 단위로 PR 분할

### 브랜치 플로우 (중요)

```
wt/db, wt/backend, wt/frontend
       ↓ (자동 가능 — head/sub/메인 세션 판단)
     3dview  (작업 통합 브랜치)
       ↓ (수동 필수 — 사용자+협업자 협의)
     main    (안정/배포 브랜치)
```

- **sub → 3dview**: 자동화 범위. head/sub가 보고서 검증 + 메인 세션 검토 후 머지 가능
- **3dview → main**: **절대 자동 금지.** 협업자와 코드 충돌 의견 교환, 릴리즈 타이밍 결정, 배포 영향도 판단 등이 필요. 사용자가 수동으로 PR 생성 + 협업자 리뷰 후 머지
- **head/sub 의 보고서**: "머지 가능" 이라는 표현은 항상 **3dview 기준**. main 머지 판단은 보고서에 포함하지 않음

### 정기 점검
- 주 1회: `list-worktrees.sh` 로 현황 확인
- 월 1회: `cleanup-failed-branches.sh --apply`
- 주 1회: `token-budget.md` 수동 갱신 (실제 사용량 vs 추정)

### 협업자 (1명) 관련
- 협업자는 `/inbox-send` 등 coordination 시스템 미사용
- 협업자 작업은 별도 브랜치 → main 머지 시 일반 충돌 해결
- 양측 모두 건드리는 파일은 hotfixes.md 로 의도 공유

---

## 10. 확장 로드맵

### Level 2 (크론/스케줄): 배제됨
- 와일드한 자동 실행 리스크 때문에 의도적으로 제외

### Level 3 (실시간 훅): 향후 검토
- Cloudflare Tunnel + 로컬 웹훅 수신기
- GitHub push → 즉시 head 트리거
- Discord bot 로 승인 인터랙션 (현재는 GitHub 커밋 방식)
- 사전 조건: Level 1 실사용 안정화 후

### 기타 아이디어
- 토큰 사용량 자동 수집 (`/cost` 파싱)
- 작업별 소요시간 분석 대시보드
- 에스컬레이션 자동 분류 (범위 벗어남 vs 외부 의존성)

---

## 11. 빠른 레퍼런스 카드

### 메인 (op) 창

```
# 작업 지시 (head 자동 spawn)
/inbox-send <내용>
  - 클립보드 이미지 있으면 자동 첨부
  - head 가 백그라운드에서 처리 시작

# 단순 버그 수정 (head 거치지 않음)
/fix <내용>

# head 처리 결과 검증 + 머지
/review-inbox

# 사법부 거부 응답 (Phase 2.6)
/resolve-escalation <id> [이유]   # 승인 → head 자동 재진입
/reject-escalation <id> [이유]    # 거부 → head 가 다른 방식

# 현황 한눈
/status               # inbox/escalation/sub 상태
/token-status         # 토큰 + 임계 + 리셋 (자동 트리거)

# 긴급 중단
/inbox-send STOP
```

### Head 창 (자동 spawn 됨, 수동 필요 거의 없음)

```
/inbox          # 일반: pending inbox 처리 (op 가 자동 호출)
/plan <내용>    # inbox 없이 즉석 분해
/retry <id>     # 검증 실패 plan 재시도
/dispatch <sub> # 특정 sub 만 헤드리스 실행
```

### Sub 창 (자동 spawn 됨)

```
/go             # task 파일 읽고 자동 실행
```

### 일반 도구

```bash
# Worktree
bash scripts/list-worktrees.sh
bash scripts/setup-worktree.sh <name> <port> <base>
bash scripts/remove-worktree.sh <name>

# 일괄 VSCode 열기
bash scripts/open-worktrees.sh

# Discord 수동 알림
bash scripts/notify-discord.sh "<title>" "<message>" [<link-path>] [<label>]

# Step 별 Discord 알림 (head 가 각 sub 완료 시점마다 호출 — 표준 포맷)
bash scripts/notify-step.sh <plan_id> <step_num>/<step_total> <sub_name> <status> [<summary>] [<link-path>]
#   status: started | ok | done | fail | blocked | skip
#   예: bash scripts/notify-step.sh plan-abc 1/3 db ok "7 files, +124/-5"

# 토큰 상태 (자동 임계 알림 트리거)
bash scripts/check-token-threshold.sh

# 백업 (reset 전)
bash scripts/backup-commands.sh

# Failed 브랜치 정리 (30일+)
bash scripts/cleanup-failed-branches.sh --apply
```

### 일상 흐름 (Phase 2.6+ / 터미널 직접 운영)

```
1. /inbox-send 작업 X (클립보드에 스크린샷 있으면 자동 첨부)
   ↓ (head 자동 spawn, 사법부 활성)
2. 알림 대기 (Discord — Step 별 + 토큰 임계 + 완료)
3. /review-inbox → 검증 + verdict + (go) 머지
4. 머지 성공 → 완료
```

### 흔한 시나리오

```
# 사법부 거부 알림 받음
/resolve-escalation <id> 의도된 작업
  → head 자동 재진입 + 막힌 작업 재시도

# 토큰 80% 도달 알림
→ 작업 페이스 조절 또는 다음 주차 대기

# 매주 월요일 첫 사용
→ 자동으로 🔄 리셋 알림 받음
```

---

## 7. Discord 봇 기반 운영 (Phase 6, v0.6+)

터미널 슬래시 명령을 Discord 앱에서 원격 조작. **핸드폰/웹/PC 어디서든** 같은 흐름.

**설치/셋업**: [bot/README.md](../bot/README.md) 전주기 가이드 (Developer Portal / intents / OAuth / projects.yml / 실행).

### 권장 채널 레이아웃 (v0.11+)

같은 Discord 서버 안에 2 텍스트 채널:

```
#대화방   ← 봇 @mention 받는 채널 (@bot send, review, fix, status, ...)
#작업장   ← webhook 완료 알림 + inbox thread 가 모이는 채널
```

`bot/projects.yml`:
```yaml
projects:
  - id: myapp
    path: /path/to/myapp
    discord_channel_id: "<대화방 id>"          # 명령 받는 채널
    discord_alert_channel_id: "<작업장 id>"    # thread + webhook 채널
```

`.env.local` 의 `DISCORD_WEBHOOK_URL` 은 #작업장 의 webhook URL 로 설정.

### 전체 자동 Orchestration (v0.12+)

`@bot send <text>` **한 번** 이면 전체 흐름 자동:

```
User (대화방):   @bot send 가격 계산 리팩터링
Bot  (대화방):   ⏳ dealos 큐 추가 — 즉시 처리
Bot  (대화방):   ➡ dealos <#작업장> 에서 진행

[작업장]
Bot:   📥 dealos 작업 시작 + thread (inbox-dealos-XXXXXX)
       ✅ inbox-send spawn → head 자동 돌고 있음

(head 작업 수 분 ~ 수십 분)

Bot:   🔎 head 완료 감지 — YYYY-MM-DD-HHMMSS review 자동 진행 중…
Bot:   ✅ Review 완료 — 머지 진행할까요?
       inbox: YYYY-MM-DD-HHMMSS
       verdict: go (통과)
       [✅]   [❌]   ← 리액션

[User 가 ✅ 클릭]
Bot:   🚀 머지 spawn → /review-inbox Step 7 자동 yes
Webhook: 🎉 <project> 머지 완료
```

verdict 별 분기:
- **go + ✅** → 머지 단계 spawn (자동 yes + push)
- **needs-fix + ✅** → `/fix` 자동 spawn (review-inbox 의 needs-fix 항목 자동 탐색)
- **❌** → 보류 (수동)
- **block** → 경고만, 자동 action 없음 (사용자 개입 필수)

### 주요 명령

`@bot help` 로 Embed 형태 전체 가이드. 요약:

| 명령 | 동작 |
|------|------|
| `@bot send <text>` | `/inbox-send` spawn + 자동 orchestration 시작 |
| `@bot review [id]` | `/review-inbox` 수동 (id 생략 시 최근 미review 자동) |
| `@bot fix [desc]` | `/fix` (desc 생략 시 needs-fix review 자동 탐색) |
| `@bot resolve <id>` / `@bot reject <id>` | escalation 응답 (리액션 대체) |
| `@bot status` / `@bot projects` / `@bot queue` | 상태 조회 |
| `@bot stop <project>` / `@bot retry <inbox-id>` | 제어 |
| `@bot budget` / `@bot usage [days]` | 토큰 예산 / 실 비용 (ccusage) |
| `@bot register <id> <path>` / `bind` / `unbind` / `reload` | 레지스트리 관리 |
| ✅/❌ 리액션 | escalation 응답 / verdict prompt 응답 |

### 사법부 거부 자동 처리

head 가 사법부에 막히면 Discord 에 **🔔 head 판사 거부 — 승인 요청** 메시지 (webhook). 사용자는 그 메시지에 ✅/❌ 리액션만:

- **✅** → `/resolve-escalation <id>` 자동 spawn (승인)
- **❌** → `/reject-escalation <id>` 자동 spawn (거부)

메시지 내용에서 escalation id 자동 파싱, 현재 채널 바인딩 프로젝트로 라우팅.

### 동시 요청 큐 직렬화 (v0.10+)

같은 프로젝트에 `@bot send A` / `@bot send B` 연속 와도 `busy` 거부 없이 자동 순서 처리. Worker 가 HEAD_LOCK 해제를 기다리고 다음 job spawn. `@bot queue` 로 현재 대기 상태 확인.

---

*마지막 갱신: 2026-04-17, v0.12 — Phase 6 Discord Bot 자동 orchestration 완결 시점*

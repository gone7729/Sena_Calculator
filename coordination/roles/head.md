# Role: head (오케스트레이터)

당신은 head 세션입니다. 직접 코드 수정은 거의 하지 않고, 작업을 분해 → sub 에 dispatch → 결과 수집하는 역할입니다.

## 워크트리
- 경로: `dealos-wt-head/`
- 브랜치: `wt/head`
- 포트: 3099

## 쓰기 권한 (중요 — 이중 정본 방지)

### ✅ head 가 쓸 수 있는 파일
| 파일/디렉토리 | 용도 |
|-------------|------|
| `coordination/plans/*.md` | 다단계 실행 계획 |
| `coordination/tasks/*.md` | sub 지시서 |
| `coordination/reports/*.md` | sub 보고서 수집본 (sub 브랜치에서 복사) |
| `coordination/review-inbox/*.md` | 처리 완료 결과 |
| `coordination/STATUS.md` | 실시간 현황 |
| `.claude/commands/*.md` (head 전용) | inbox, dispatch, plan, retry |

### ❌ head 가 절대 수정하지 않는 파일 (메인 세션 정본)

| 파일/디렉토리 | 주인 |
|-------------|------|
| `coordination/inbox/*.md` | 메인만 (/inbox-send 로 생성, /review-inbox 머지 시 `status: merged` 마킹) |
| `coordination/hotfixes.md` | 메인만 (/fix) |
| `coordination/roles/*.md` | 메인만 (구조 규약) |
| `coordination/USAGE.md`, `README.md`, `ROADMAP.md`, `TROUBLESHOOTING.md` | 메인 (문서) |
| `coordination/token-budget.md` | 메인 갱신 (head 는 읽기만, 로그는 token-log.md 에 쓰기 가능) |
| `coordination/approvals/*.md` | 메인/사용자 (승인 응답) |

**왜 중요**: 이전에 head 가 inbox status 를 wt/head 브랜치에서 수정했는데, 메인도 같은 파일을 3dview 에서 관리해 **같은 파일 두 브랜치에서 경쟁적 수정 → 이중 정본 divergence 발생**. 해결 규칙은 위 표.

## head 가 "처리 중 / 완료" 를 표현하는 방법

inbox 파일 수정 금지 → 대신:

1. **처리 중**: `coordination/plans/<inbox-id>.md` 생성 + 각 Step 의 `status: in_progress`
2. **완료**: `coordination/review-inbox/<ts>-<id>.md` 생성 (`verdict: pending` 으로 초기화)
3. **verdict 사전 기록 (v0.13+)**: 위 2번 직후 같은 세션에서 `/review-inbox <inbox-id> --verdict-only` 를 **즉시** 실행해 verdict (`go` / `needs-fix` / `block`) 를 review-inbox 파일에 기록. 메인/봇이 나중에 다시 review 를 spawn 할 필요가 없어짐.
4. **머지됨**: **메인 세션이** 3dview 에서 inbox 파일에 `status: merged` + merge_commits 1회 마킹

### 왜 verdict 를 head 가 사전 기록하는가 (v0.13)

v0.12 까진 봇이 HEAD_LOCK 해제 후 op 세션을 다시 spawn 해 `/review-inbox --verdict-only` 를 돌렸다. head 가 자기 review-inbox 를 작성한 직후라 리뷰에 필요한 모든 파일 (plan, reports, diff) 이 이미 head 컨텍스트에 있는데 — 새 세션을 spawn 하면 전부 다시 읽어야 해서 **수만 토큰 낭비**. v0.13 에선 head 가 완료 직전에 self-review 를 돌려 verdict 를 사전 기록. 봇은 verdict 가 이미 있으면 spawn 생략.

fallback: 어떤 이유로 head 가 verdict 기록 없이 종료하면 봇이 기존 Phase D 로 spawn (하위 호환).

## 책임
1. `/inbox-send` 로 온 지시 분해 → Plan 작성 → sub dispatch
2. 각 sub 보고서 수집 → review-inbox 작성
3. STATUS.md 실시간 갱신
4. 실패/에스컬레이션 감지 + Discord 알림
5. 직접 코드 수정은 하지 않음 (sub 에게 위임)

## 협업 인터페이스
- 메인의 `/review-inbox` 가 3dview 로 머지하면 head 는 rebase 로 최신 3dview 받음
- sub 작업이 실패하면 `failed/wt-<sub>-<ts>` 브랜치로 보관 (sub 가 직접)

## 시작 시 체크리스트
- [ ] `git branch --show-current` 가 `wt/head` 확인
- [ ] `git fetch + rebase origin/3dview` (자동 동기화)
- [ ] `coordination/STOP` 없는지 확인
- [ ] `coordination/inbox/*.md` 에서 `status: pending` 엔트리 찾기

## 금지 (추가)
- inbox 엔트리 삭제 금지 (status 필드도 건드리지 않음)
- sub 브랜치 범위 외 커밋 금지 (각 sub 이 자기 브랜치에만 쓰기)
- 3dview 로 직접 머지 금지 (메인 세션이 `/review-inbox` 에서 담당)
- `coordination/STOP` 자체 해제 금지 (사용자만)

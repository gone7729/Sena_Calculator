# Plan: <제목>

- **inbox id**: <id>
- **created**: YYYY-MM-DD HH:MM KST
- **updated**: YYYY-MM-DD HH:MM KST
- **status**: in_progress | done | blocked | cancelled
- **head**: wt/head

## 요청 원문
> <inbox 엔트리 요청 내용 요약>

## 사전 분석

### 관련 파일 (head 가 /inbox 진입 시 조사)
- `path/to/file.ts` (최신 커밋: `<hash>` - 메시지)
- `path/to/other.ts` (최신 커밋: `<hash>`)

### 최근 관련 hotfixes (hotfixes.md 에서 추출)
- `<hash>` <제목> — 관련성: ...

### 관련 reports 이력 (reports/ 과거 유사 작업)
- wt-backend.md (YYYY-MM-DD) — ...

## 단계 (Step)

### Step 1: <제목> [wt/<sub>]
- **status**: pending | in_progress | done | failed | blocked
- **task**: [tasks/wt-<sub>.md](../tasks/wt-<sub>.md)
- **report**: [reports/wt-<sub>.md](../reports/wt-<sub>.md) (완료 후)
- **depends**: - (또는 Step N)
- **commit**: `<hash>` (완료 후 채움)
- **branch**: wt/<sub>
- **started**: -
- **completed**: -
- **notes**: (있으면)

### Step 2: <제목> [wt/<sub>]
- **status**: pending
- **depends**: Step 1
- ...

### Step 3: <제목> [wt/<sub>]
- **status**: pending
- **depends**: Step 2
- ...

## 전체 진행률

- 완료: 0 / N 단계
- 진행 중: Step X
- 차단/이슈: (있으면)

## 완료 후

- review-inbox 생성: `review-inbox/<ts>-<id>.md`
- 각 sub 브랜치 최종 커밋 해시
- 통합 리스크 요약

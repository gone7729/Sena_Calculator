# Task: <작업 제목>

- **담당 worktree**: wt/<name>
- **브랜치**: wt/<name>
- **base**: main
- **작성일**: YYYY-MM-DD
- **작성자**: head

## 배경
<왜 이 작업이 필요한지, 관련 맥락/이슈 링크>

## 범위 (수정 허용)
- `path/to/dir/**`
- `path/to/file.ts`

## 금지 (절대 수정 불가)
- `coordination/` (보고서 제외)
- `CLAUDE.md`, `MEMORY.md`
- 다른 worktree의 도메인
- `supabase/migrations/` (기존)

## 목표 (완료 조건)
- [ ] 구체 목표 1
- [ ] 구체 목표 2
- [ ] 테스트 통과: `npm run test:smoke`

## 제약
- SSOT 규칙 (CLAUDE.md 참조)
- TypeScript strict 유지
- 외부 패키지 추가 금지 (필요 시 보고서에 요청)

## 참고 파일
- [core/xxx/yyy.ts](../../core/xxx/yyy.ts)

## 완료 후 작성할 보고서
`coordination/reports/wt-<name>.md` 에 아래 항목 포함:
- 변경 파일 목록 (git diff --name-only)
- 테스트 결과
- 새로 생긴 의존성/사이드이펙트
- head가 검증해야 할 포인트
- blocked 항목 (있다면)

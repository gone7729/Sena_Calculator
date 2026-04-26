# Report: <작업 제목>

- **worktree**: wt/<name>
- **브랜치**: wt/<name>
- **작성일**: YYYY-MM-DD
- **task 원본**: [../tasks/wt-<name>.md](../tasks/wt-<name>.md)
- **상태**: completed | partial | blocked

## 변경 요약
<2~3줄로 무엇을 왜 바꿨는지>

## 변경 파일
```
<git diff --name-only 결과>
```

## 검증
- [ ] type-check: `npm run type-check` 통과
- [ ] lint: `npm run lint` 통과
- [ ] smoke test: `npm run test:smoke` 통과
- [ ] SSOT check: `npm run ssot:check` 통과

## Head 검증 포인트
- <리뷰어가 반드시 확인해야 할 항목>

## 사이드이펙트 / 주의사항
- <다른 모듈에 미치는 영향>

## Blocked (있을 때만)
- **막힌 지점**:
- **이유**:
- **필요한 결정/지원**:

## 다음 단계 제안
- <후속 작업 아이디어>

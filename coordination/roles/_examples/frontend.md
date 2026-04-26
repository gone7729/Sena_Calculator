# Role: sub-3 (frontend)

당신은 **프론트엔드 + 디자인 통합 작업자**입니다. UI 컴포넌트, 상태 관리, 데이터 바인딩, 비주얼 디자인을 모두 담당합니다.

## 워크트리
- 경로: `dealos-wt-frontend/`
- 브랜치: `wt/frontend`
- 포트: 3003

## 담당 범위 (수정 허용)
- `components/**` — 모든 React 컴포넌트
- `hooks/**` — React 훅 (오케스트레이션)
- `stores/**` — Zustand 스토어 (입력 상태만)
- `styles/**` — 글로벌 CSS
- `tailwind.config.ts`, `postcss.config.js`
- `app/**` (Next.js App Router)
- `pages/**` (있다면)

## 절대 금지
- 순수 계산 로직 직접 구현 (SSOT 위반)
  - `Math.pow`, PMT 공식, IRR 직접 계산 금지
  - 반드시 `core/finance/engine.ts` 함수 호출
- DB 마이그레이션, 스키마
- `core/**` 파일 수정 (import는 OK)

## 책임
1. UI 렌더링만 (계산은 훅 → engine.ts 경유)
2. 상태 관리: 입력값은 store, 파생값은 훅에서 계산
3. 반응형/접근성 (Tailwind + semantic HTML)
4. 사용자 인터랙션 (이벤트, 폼)
5. 디자인 시스템 일관성 (색상, 타이포, 간격)
6. 소비: backend(sub-2)가 제공한 계산 함수/타입 사용

## 협업 인터페이스
- 새 데이터 필드 필요 → backend/db task로 요청 (보고서에 "blocked: waiting for X")
- 계산 결과 타입 변경 시 → backend 보고서의 시그니처 확인 후 적용

## 시작 시 체크리스트
- [ ] `git worktree list`로 현재 위치가 `dealos-wt-frontend` 확인
- [ ] `git branch --show-current` 가 `wt/frontend` 확인
- [ ] `coordination/tasks/wt-frontend.md` 읽기
- [ ] CLAUDE.md의 SSOT 섹션 재확인 (components 내 계산 금지)

## 완료 시
- `coordination/reports/wt-frontend.md` 작성
- 영향받은 화면/경로, 신규 컴포넌트, 접근성 체크 결과 명시
- `npm run lint`, `npm run type-check` 결과 포함
- UI 변경 시 스크린샷 경로 또는 확인 방법 기술

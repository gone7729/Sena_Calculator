# Role: sub-2 (backend)

당신은 **백엔드 로직 전담 작업자**입니다. 이격거리 판정, 건물 매스 생성, 재무 계산 등 모든 순수 계산 로직을 담당합니다.

## 워크트리
- 경로: `dealos-wt-backend/`
- 브랜치: `wt/backend`
- 포트: 3002

## 담당 범위 (수정 허용)
- `core/legal/**` (setbackDbService.ts 제외)
  - `setbackRules.ts`, `sunlightSetbackRules.ts`, `constants.ts`
- `core/spatial/**` — geometry 유틸
- `core/massing/**` — 매스 생성기, shape generator
- `core/design-automation/**` — 법규 엔진
- `core/finance/**` — 재무 엔진 (SSOT)
- `utils/massingEngine.ts`, `utils/complianceCalculations.ts`
- 새 순수 로직 파일: `core/*/` 하위

## 절대 금지
- DB 접근 (import DB 서비스는 OK, 수정 금지)
- UI/컴포넌트 (`components/**`)
- 훅 중 UI 지향 (`hooks/backbone/**` 등)
- 마이그레이션 SQL

## 책임
1. SSOT 준수 (CLAUDE.md 재무 섹션, 매싱 섹션 참조)
2. 순수 함수 유지 (입력→출력, 사이드이펙트 없음)
3. 타입 안전성 (strict mode, Result<T> 패턴)
4. 테스트 가능성: 로직 변경 시 `__tests__/core/` 업데이트
5. 공급: DB(sub-1)에서 받은 데이터 → 계산 → frontend(sub-3)에 소비될 결과 반환

## 협업 인터페이스
- DB 스키마 변경 필요 → task 작성 시 head에 명시 (db sub 선행)
- 새 계산 API → frontend에 타입/시그니처 문서화 (보고서에 기록)

## 시작 시 체크리스트
- [ ] `git worktree list`로 현재 위치가 `dealos-wt-backend` 확인
- [ ] `git branch --show-current` 가 `wt/backend` 확인
- [ ] `coordination/tasks/wt-backend.md` 읽기
- [ ] CLAUDE.md의 SSOT/Massing 섹션 재확인

## 완료 시
- `coordination/reports/wt-backend.md` 작성
- 변경된 공용 함수 시그니처, 신규 상수, 테스트 통과 여부 명시
- `npm run ssot:check`, `npm run type-check` 결과 포함

# Role: sub-1 (db)

당신은 **DB/SQL 전담 작업자**입니다.

## 워크트리
- 경로: `dealos-wt-db/`
- 브랜치: `wt/db`
- 포트: 3001

## 담당 범위 (수정 허용)
- `supabase/migrations/**` — 마이그레이션 SQL
- `supabase/config.toml`
- `core/legal/setbackDbService.ts` — 이격거리 DB 액세스 레이어
- `lib/supabase.ts`, `lib/supabase-*.ts` — 클라이언트/서비스 설정
- `types/supabase.ts` — DB 타입 정의 (수동 유지)
- 새 DB 서비스 파일: `core/*/dbService.ts` 패턴

## 절대 금지
- 계산 로직 (이격거리 판정, 매스 생성, 재무 계산)
- UI/컴포넌트 (`components/**`)
- 훅/스토어 (`hooks/**`, `stores/**`)
- `core/legal/setbackRules.ts` 같은 순수 로직 파일

## 책임
1. 스키마 설계 (RLS, 인덱스, FK)
2. 마이그레이션 작성 + 시드 데이터
3. DB 타입 동기화 (`types/supabase.ts`)
4. DB 서비스 레이어 (CRUD, 쿼리 최적화)
5. 공급: backend(sub-2)가 쓸 DB 인터페이스 제공

## 협업 인터페이스
- backend(sub-2)가 필요한 데이터 모양 → task에 명시됨
- 스키마 변경은 backend와 frontend 양쪽에 영향 → 보고서에 **breaking change** 플래그 필수

## 시작 시 체크리스트
- [ ] `git worktree list`로 현재 위치가 `dealos-wt-db` 확인
- [ ] `git branch --show-current` 가 `wt/db` 확인
- [ ] `coordination/tasks/wt-db.md` 존재 확인 후 읽기
- [ ] 범위 밖 파일 수정 금지 재확인

## 완료 시
- `coordination/reports/wt-db.md` 작성
- 마이그레이션 파일명, 영향받은 테이블, breaking change 여부 명시

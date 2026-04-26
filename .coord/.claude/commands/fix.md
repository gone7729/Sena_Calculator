---
description: 메인 세션 긴급 수정 — 수정 + 커밋 + hotfixes.md 로그 작성
argument-hint: <수정 내용 설명>
---
> v0.5.1+: 경로는 `coordination/config.yml` 참조. 수동 편집 불필요.

## 0. 환경 로드

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음 — init.sh 먼저 실행"; exit 1
fi
```

너는 메인 세션 (`$PROJECT_ROOT`, `$WORK_BRANCH` 브랜치) 의 검토/수정 역할이다.
사용자 요청 `$ARGUMENTS` 을 아래 절차로 처리한다.

## 절차

1. **현재 상태 확인**
   - `git status --short` 로 uncommitted 변경 확인
   - 관련 파일 읽고 원인 파악

2. **수정 수행**
   - 최소 변경 원칙 (CLAUDE.md의 "Don't add features, refactor, or introduce abstractions" 준수)
   - SSOT 규칙 준수 (프로젝트 고유 SSOT — 예: 계산 로직이 특정 모듈에 있다면 거기만 수정)
   - 타입 안전성 유지
   - 불필요한 주석/에러핸들링 추가 금지

3. **검증**
   - 프로젝트 표준 타입체크/빌드 명령 실행 (예: `npm run type-check`)
   - 버그가 재현되던 상황에서 수정 확인 (가능하면)

4. **커밋**
   - 사용자에게 커밋 메시지 제안 후 승인 받기 (자동 커밋 금지)
   - 승인 시 커밋 실행

5. **hotfixes.md 엔트리 추가**
   - `$COORD_ROOT/coordination/hotfixes.md` 상단 "## 로그 (최신순)" 바로 아래에 새 엔트리:
     ```markdown
     ### YYYY-MM-DD <한줄 제목>
     - **커밋**: `<short-hash>`
     - **영향 파일**: 실제 변경된 경로들
     - **요약**: 무엇을 왜 고쳤는지 1~2줄
     - **영향 범위**: config.yml 의 subs[].name 중 관련된 것
     - **후속 조치**: sub들이 주의할 점 (없으면 생략)
     ```
   - hotfixes.md 변경도 같은 커밋에 포함 (amend) 또는 별도 후속 커밋

6. **사용자에게 보고**
   - 수정 요약 + 커밋 해시 + hotfixes 엔트리 위치
   - sub에 영향 있으면 "head에게 /plan 으로 rebase 지시 필요" 안내

## 금지
- 범위를 벗어난 "김에 같이" 수정 금지
- hotfixes 로그 누락 금지 (sub들이 못 봄)
- 자동 커밋 금지 (사용자 승인 후에만)
- 테스트 없이 "고쳤습니다" 선언 금지

## 지금 할 것
위 절차대로 `$ARGUMENTS` 수정하라.
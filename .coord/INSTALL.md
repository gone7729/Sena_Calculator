# 설치 가이드

이 문서는 `coord` 템플릿을 기존 또는 신규 프로젝트에 적용하는 상세 절차를 다룬다.
Quick Start 는 [README.md](README.md) 참조.

## 사전 준비

- **git** 2.20+ (worktree 지원)
- **Claude Code** CLI 설치 (`claude` 명령 실행 가능)
- **Python 3.11+** (PyYAML, FastAPI 등 dashboard 용)
- **bash** (Windows 는 Git Bash 권장)
- **Discord webhook URL** (선택, 알림 원할 시)
- **Cloudflare 계정** (선택, 라이브 대시보드 외부 공유 시 — 무료 TryCloudflare 는 계정 불필요)

## 1. 템플릿 적재

### 옵션 A — 새 프로젝트 (clone 후 재초기화)
```bash
git clone https://github.com/gone7729/coord-template.git my-project
cd my-project
rm -rf .git
git init
git remote add origin <your-new-repo-url>
```

### 옵션 B — 기존 프로젝트 (파일만 복사)
```bash
# 임시 clone
git clone https://github.com/gone7729/coord-template.git /tmp/coord

# 프로젝트 루트에서:
cp -r /tmp/coord/coordination ./
cp -r /tmp/coord/scripts ./
cp -r /tmp/coord/templates ./
cp -r /tmp/coord/.claude ./
cp -r /tmp/coord/.githooks ./
cp /tmp/coord/docs/setup-cloudflare-tunnel.md ./docs/ 2>/dev/null || true
```

### 옵션 C — 서브트리
```bash
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git main --squash
```

## 2. config.yml 설정

```bash
cp coordination/config.yml.example coordination/config.yml
```

필수 수정:
- `project.name` — 프로젝트 코드명 (worktree 디렉토리명에 사용됨: `../<name>-wt-<sub>/`)
- `git.work_branch` — 통합 브랜치 (예: `main`, `develop`, `3dview`)
- `git.stable_branch` — 안정 브랜치 (PR 타겟)
- `git.remote` — 원격명 (보통 `origin`)
- `subs[]` — 각 sub 의 이름/포트/모델/범위/검증 명령
- `notifications.discord_webhook_env` — Discord webhook 환경변수명 (파일에 URL 직접 쓰지 말 것)

선택 수정:
- `judiciary.approval_ttl_minutes` — 승인 유효 시간 (기본 1440 = 24시간)
- `token_budget.weekly_limit` — 주간 토큰 예산
- `dashboard.port` — 대시보드 포트 (기본 8888)

## 3. 환경변수 설정

```bash
cp .env.local.example .env.local
```

`.env.local` 에서:
- `DISCORD_WEBHOOK_URL` — Discord webhook URL (Server Settings → Integrations → Webhooks 에서 생성)
- `GITHUB_REPO_URL` — (선택) 알림 링크용

`source .env.local` 또는 쉘 rc 에 등록.

## 4. Git hook 설치

**각 worktree 에서 1회씩** 실행:
```bash
git config core.hooksPath .githooks
```

또는 헬퍼 사용:
```bash
bash scripts/install-hooks.sh
```

## 5. Claude Code 권한 설정

`init.sh` 가 자동으로 `.claude/settings.local.json.example` → `.claude/settings.local.json` 복사를 묻는다.
**자동 설치 완료**되면 5단계 스킵.

수동 설치가 필요하면:
```bash
cp .claude/settings.local.json.example .claude/settings.local.json
```

기본 포함 사항:
- 안전한 git/npm/bash/Read/Write 권한
- 위험 명령 deny (rm -rf /, sudo, force push to main)
- **PreToolUse hook**: Bash/Write/Edit 시도마다 `scripts/judge-action.sh` 3심 판사 검증
- **PostToolUse hook**: 토큰 % 임계 자동 체크

사법부를 끄려면 `.claude/settings.local.json` 의 `hooks` 섹션 제거.

`.claude/settings.local.json` 자체는 `.gitignore` 됨 (개인 권한이라 커밋 금지). 팀과 공유할 권한이 있으면 `.claude/settings.json` (프로젝트 공유본) 로 분리.

## 6. 첫 worktree 생성

```bash
# db sub 예시 (이름/포트/base 브랜치)
bash scripts/setup-worktree.sh db 3001 main
```

결과:
- `../<project>-wt-db/` 디렉토리 생성
- 브랜치 `wt/db` (base 에서 분기)
- `.env.local` 복사 + `run-dev.sh` 생성 (포트 3001)

생성된 worktree 에서:
```bash
cd ../<project>-wt-db
npm install  # 또는 프로젝트 빌드 명령
claude       # 새 Claude 세션 시작
```

## 7. 대시보드 기동 (선택)

```bash
# 로컬
bash scripts/start-dashboard.sh &
# 기본 http://localhost:8888

# 외부 접근 (TryCloudflare — 계정 불필요)
cloudflared tunnel --url http://localhost:8888 > /tmp/cloudflared.log 2>&1 &
bash scripts/update-dashboard-url.sh &   # URL 감지 + Discord 알림
```

상세: [docs/setup-cloudflare-tunnel.md](docs/setup-cloudflare-tunnel.md)

## 8. 첫 inbox 테스트

메인 세션(op)에서:
```
/inbox-send 테스트 요청 — hello world 파일 생성
```

확인 흐름:
1. `coordination/inbox/<ts>.md` 생성됨
2. head worktree 에 Claude 세션 시작 → `/go` 또는 수동 `/inbox` 실행
3. head 가 plan 작성 → sub 디스패치 → sub 실행 → 보고 → review-inbox 제출
4. op 에서 `/review-inbox` 로 검증 → verdict: go 시 3dview 머지

## 자주 겪는 문제

- **사법부가 일반 명령을 거부** → `coordination/escalations/*.md` 확인, `/resolve-escalation <id>` 로 승인
- **pre-commit 이 커밋 차단** → `coordination/config.yml` 의 해당 sub `scope_allowed` 에 경로 추가
- **head 가 wt/head 브랜치 reset 시 슬래시 명령이 사라짐** → 3dview 에도 동일 명령 심기(옵션) 또는 wt/head 에서만 실행
- **Windows 에서 PyYAML 인코딩 오류** → `scripts/config.sh` 가 `encoding='utf-8'` 명시하므로 최신 버전 사용 확인

더 많은 문제: [coordination/TROUBLESHOOTING.md](coordination/TROUBLESHOOTING.md)

## 다음 단계

- `coordination/README.md` 정독 — 조율 규약
- `coordination/roles/head.md` 와 `roles/_examples/` 참조하여 내 프로젝트의 `roles/<sub>.md` 작성
- `.claude/commands/` 의 슬래시 명령 목록 확인 — op 사용법 파악
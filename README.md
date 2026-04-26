# coord — 다중 Claude 병렬 오케스트레이션 템플릿

여러 Claude Code 세션을 **git worktree + 파일 기반 조율 + Discord 봇** 으로 병렬 구동하는 시스템.
어떤 프로젝트에도 `.coord/` subtree 로 붙이거나 파일 복사만으로 즉시 사용 가능.

> 원본: DEALOS 프로젝트 (부동산 개발 재무 모델링) 에서 사용 중인 구조를 일반화.

**현재 버전**: v0.46 — [ROADMAP](coordination/ROADMAP.md) | [릴리스 노트](https://github.com/gone7729/coord-template/releases)

## 특징

- **3-tier 구조** — op(메인) → head(오케스트레이터) → sub(구현자) 역할 분리, 독립 Claude 프로세스
- **파일 기반 조율** — `coordination/` 에 inbox/plans/tasks/reports/review-inbox/escalations 흐름
- **runtime 사법부** — PreToolUse hook 이 3심(whitelist → blacklist → LLM judge) 으로 위험 action 차단
- **자동 에스컬레이션** — 거부된 action 은 `coordination/escalations/` 에 쌓여 op 가 승인/반려 (Discord 리액션 ✅/❌ 지원)
- **토큰 예산 추적** — 주간 리셋 + 임계(%) Discord 알림 + `ccusage` 실 비용 조회
- **라이브 대시보드** — FastAPI + HTMX, Cloudflare TryCloudflare 로 모바일에서도 접근
- **Discord 봇 (Phase 6)** — `@bot send <text>` → **자동 inbox-send → head → review → verdict 리액션 prompt → 머지/fix** 전 흐름. 멀티 프로젝트 라우팅, 채널 자동 바인딩, 큐 직렬화.
- **설치/업그레이드 자동화** — `init.sh` 대화식 초기 셋업 + `upgrade-coord.sh` 업그레이드 (tarball / git / subtree 3-mode)
- **`.coord/` subdir 모드** — `git subtree add --prefix=.coord ...` 로 템플릿 격리 설치 (신규 프로젝트 권장)
- **YAML config** — `coordination/config.yml` 한 곳에서 브랜치/포트/모델/범위/봇 레지스트리

## 사전 의존성 설치 (v0.34+)

처음 도입하는 환경이면 아래 스크립트로 Python / Node / git / uv / claude CLI / (옵션) cloudflared 를 한 번에 설치할 수 있습니다. 이미 있는 건 skip.

### macOS
```bash
# Homebrew 가 없으면 먼저 설치: https://brew.sh
bash scripts/install-deps-mac.sh          # 대화식
bash scripts/install-deps-mac.sh --yes    # 전부 yes 자동
```
- 필요 시 `--skip-cloudflared` (대시보드 외부 노출 안 쓸 때)
- 스크립트: [scripts/install-deps-mac.sh](scripts/install-deps-mac.sh)

### Windows (PowerShell)
```powershell
# winget (App Installer) 이 기본 탑재된 Windows 10 1809+ / 11 대상
powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1
powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1 -Yes
```
- 필요 시 `-SkipCloudflared`
- 스크립트: [scripts/install-deps-windows.ps1](scripts/install-deps-windows.ps1)
- Git Bash 환경에서 bash 스크립트 (init.sh 등) 를 실행하므로 Git for Windows 는 필수 (winget 으로 함께 설치됨)

### 수동 설치 (스크립트 없이)

필요한 것만 정리:
- **Python 3.10+** (type annotation 호환) — bot 실행
- **Node.js LTS** — claude CLI / ccusage 실행
- **claude CLI** — `npm i -g @anthropic-ai/claude-code`
- **git 2.5+** — worktree 기능
- **uv** (권장) 또는 pip — Python 의존성 관리
- **cloudflared** (선택) — 대시보드 외부 노출

설치 후 `uv pip install -r bot/requirements.txt` (또는 `pip install -r bot/requirements.txt`).

---

## Quick Start

> **v0.46+**: subtree (subdir) 설치가 **기본/권장** 입니다. 이유는 아래 "왜 subdir 모드" 섹션 참조.

### A. 신규/기존 프로젝트 subtree 설치 (권장)
```bash
cd my-project                                    # 기존 프로젝트든 신규든 OK

# 1. git 초기화 + initial commit (신규만 — 빈 repo 에서 subtree add 는 HEAD 필요)
if [[ ! -d .git ]]; then
  git init
  git commit --allow-empty -m "chore: initial commit"
fi

# 2. working tree clean 확인 (필요 시 먼저 커밋/stash)
git status                                       # clean 이어야 함

# 3. subtree add + 대화식 init
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git v0.46 --squash
bash .coord/scripts/init.sh --mode=subdir        # config.yml / worktree / hooks / .gitignore / .gitattributes
```

**자주 나는 에러**:
- `fatal: ambiguous argument 'HEAD'` — git init 후 커밋이 없음 → 위 1단계의 `--allow-empty` 커밋 필요
- `fatal: working tree has modifications` — stage/untracked 파일 있음 → `git status` 확인 후 커밋하거나 `git stash`

init 이 끝나면 프로젝트 구조:
```
my-project/
├── .coord/              ← subtree 로 tracked (워크플로우 데이터 sync 용)
│   ├── coordination/    ← inbox/plans/tasks/reports 등 (git sync 필요)
│   ├── scripts/
│   ├── bot/
│   ├── .claude/
│   ├── .githooks/
│   └── (runtime 파일들은 개별 gitignored: HEAD_LOCK, STOP 등)
├── .claude/             ← .coord/.claude 로 symlink (Claude Code 제약)
├── .env.local           ← gitignored, 프로젝트 자체 용도
├── coord                ← (옵션) gitignored wrapper — `./coord <script>` 호출용
├── .gitignore           ← runtime + secrets 개별 ignore
└── .gitattributes       ← `.coord/ export-ignore` → git archive / release tarball 에서 제외
```

**git 에 기록되는 것 / 제외되는 것**:
- `git log main` — **main 에는 `.coord/` 없음** (v0.46: `.gitattributes merge=ours` 로 work_branch → main 머지 시 자동 드롭)
- `git log <work_branch>` — `.coord/coordination/inbox/...` 등 workflow commit 포함 (의도적 — 이게 통신 채널)
- `git archive HEAD` / release tarball — `.gitattributes export-ignore` 로 `.coord/` 제외
- `git clone` — tracked branch 에 있으면 포함, 아니면 제외

**v0.46 머지 플로우**:
- `@bot main-merge` — work → main (coord 자동 드롭) + **자동 back-merge** (main → work, coord 자동 보존)
- `@bot main-merge --drop-coord` — 과거 main 에 남은 `.coord/` 이력 일회성 청소 (이후 재유입 방지)
- `@bot back-merge` — 독립 back-merge (main 에 hotfix 직접 커밋 후 work 최신화)

### 왜 subdir 모드
- **폴더 통합** — coord 관련 파일이 `.coord/` 한 곳에 모임 (루트 오염 ↓)
- **배포 tarball 청결** — `.gitattributes export-ignore` 로 릴리스 산출물에 coord 미포함
- **머지 방향 자동화 (v0.46+)** — `.gitattributes merge=ours` 로 work → main 시 `.coord/` 자동 드롭, main → work 역머지 시 자동 보존
- **subtree 업그레이드 용이** — `git subtree pull` 한 방에 템플릿 동기화
- **팀 공유 유연** — coord 는 tracked 이지만 개인 secrets (`.env.local`, `projects.yml`) 는 별도 ignore

> **v0.37~v0.46 회고**: `.coord/` 통째 gitignore (v0.37~v0.40) → workflow 파괴 → tracking 복원 (v0.41) → 머지 시 coord 비대칭으로 충돌 → `.gitattributes merge=ours` + 자동 back-merge 체인 (v0.46) 으로 정착. "coord utility 숨김" 의도는 최종적으로 `.gitattributes` (export-ignore + merge=ours) 조합으로 달성.

### B. Legacy flat 설치 (deprecated, coord-template repo 본체 전용)
이 repo 자체를 개발할 때만 사용. 일반 프로젝트는 A 를 쓰세요.

```bash
cd coord-template-dev
git clone https://github.com/gone7729/coord-template ._tmp
cp -r ._tmp/{coordination,scripts,.claude,.githooks,bot,templates,docs,.gitattributes} .
cp ._tmp/{.env.local.example,.gitignore,VERSION} . 2>/dev/null
rm -rf ._tmp
bash scripts/init.sh             # flat 모드 경고 출력됨
```

### 이후 공통
```bash
# Discord 봇 사용 (선택) — subdir 모드면 .coord/bot 경로로
cp .coord/bot/projects.yml.example .coord/bot/projects.yml
bash .coord/scripts/install-deps-mac.sh       # 또는 windows.ps1
bash .coord/scripts/start-bot.sh --bg

# 대시보드 (선택)
bash .coord/scripts/start-dashboard.sh &
```

## Upgrade (기존 사용자)

새 버전이 릴리스되면 `scripts/upgrade-coord.sh` 하나로 끝. 설치 방식(flat vs subtree)은 자동 감지.

### 3단계 표준 흐름

```bash
# 1. 현재 상태 점검
git status                                      # clean 이어야 함
cat coordination/.coord-version 2>/dev/null     # 현재 버전 확인

# 2. dry-run 으로 영향 미리 보기
bash scripts/upgrade-coord.sh --version v0.12 --dry-run

# 3. 실제 적용 + 커밋
bash scripts/upgrade-coord.sh --version v0.12
git add -A && git commit -m "chore: upgrade coord-template to v0.12"
```

### 설치 방식별 차이

**subtree 설치 (`.coord/`, 권장)** — 두 방법 중 택1:
```bash
# A. upgrade-coord.sh 가 subtree 자동 감지 + .gitignore 재적용 (v0.37+)
bash .coord/scripts/upgrade-coord.sh --version v0.37

# B. 수동 subtree pull (더 git-native)
git subtree pull --prefix=.coord https://github.com/gone7729/coord-template.git v0.37 --squash
```

v0.37+ 의 `upgrade-coord.sh` 는 subdir 모드 감지 시 `.gitignore` 에 `.coord/`, `.claude/` 등을 자동 재적용. 이미 tracked 된 상태면 `git rm -r --cached` 명령을 안내.

**flat 설치 (deprecated)** — coord-template repo 본체 개발용만. 일반 프로젝트는 subtree 로 이전 권장.

### 분류 규칙 (내 커스터마이즈 안전 장치)

| 정책 | 동작 | 예시 |
|------|------|------|
| **overwrite** | 무조건 교체 | `scripts/`, `bot/controller.py`, 문서류, `.githooks/` |
| **preserve** | 절대 안 건드림 | `coordination/config.yml`, `coordination/bot.yml` (v0.22+), `inbox/`, `.env.local`, `CLAUDE.md`, `bot/projects.yml` |
| **manual-merge** | diff 보여주고 y/n | `.claude/commands/*.md`, `coordination/roles/head.md`, `README.md` |
| **skip** | 무시 | `.git/`, `__pycache__/` |

→ 토큰/config/inbox 기록은 안전. 커스터마이즈 없으면 `--accept-all` 로 일괄 승인:

```bash
bash scripts/upgrade-coord.sh --version v0.12 --accept-all
```

### 봇 재시작 (실행 중일 때)

업그레이드로 `bot/controller.py` 가 교체되므로 재시작 필요:

```bash
taskkill //F //IM python3.11.exe          # Windows
# Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

### worktree 전파

업그레이드는 **메인 repo 에서만** 실행. 각 worktree 는 브랜치 rebase 로 자동 수신:

```bash
for wt in head db backend frontend; do
  cd ../myproject-wt-$wt
  git fetch origin main && git rebase origin/main
  cd -
done
```

### 롤백

`.coord-backup/<timestamp>/` 에 백업 자동 생성. 아직 커밋 안 했으면 `git checkout --` 가 최우선.

```bash
ls -lt .coord-backup/ | head -5                   # 백업 확인
rsync -a .coord-backup/20260420-XXXXXX/ ./        # 전체 복원
```

상세 플래그/트러블슈팅: [docs/upgrade.md](docs/upgrade.md) / [coordination/TROUBLESHOOTING.md](coordination/TROUBLESHOOTING.md)

## 디렉토리 맵

```
coord-template/
├── coordination/                # 조율 허브 (프로젝트마다 적재)
│   ├── config.yml.example       # 중앙 설정 템플릿
│   ├── README.md / USAGE.md / ROADMAP.md / TROUBLESHOOTING.md
│   ├── roles/head.md + _examples/   # sub 역할 예시
│   └── inbox/ plans/ tasks/ reports/ review-inbox/ escalations/ approvals/
├── scripts/                     # 실행 스크립트
│   ├── config.sh                # config.yml 헬퍼 (COORD_ROOT/PROJECT_ROOT 자동 감지)
│   ├── init.sh / upgrade-coord.sh / start-bot.sh / start-dashboard.sh
│   ├── setup-worktree.sh / reset-wt-head.sh / install-hooks.sh
│   ├── judge-action.sh          # PreToolUse 3심 사법부
│   ├── notify-discord.sh / notify-step.sh   # webhook 알림
│   ├── dashboard.py             # FastAPI 대시보드
│   └── lib/classify.sh / sync-claude.sh      # 업그레이드 엔진
├── bot/                         # Discord 봇 (Phase 6, v0.6+)
│   ├── controller.py            # discord.py 라우터 + 자동 orchestration
│   ├── projects.yml.example
│   ├── requirements.txt
│   └── README.md                # 봇 셋업 전주기 가이드
├── templates/                   # 대시보드 HTML
├── docs/
│   ├── upgrade.md               # upgrade-coord.sh 사용법
│   ├── install-subtree.md       # `.coord/` subtree 설치
│   └── setup-cloudflare-tunnel.md
├── .claude/commands/            # op 슬래시 명령 (fix / inbox-send / review-inbox 등)
├── .githooks/pre-commit         # branch-scope 검증
├── VERSION                      # 릴리스 시 bump (단일 진실원)
└── .gitattributes               # LF 강제 (Windows + subtree 대응)
```

## 3-tier 운영 모델

| 계층 | 역할 | 실행 위치 |
|------|------|----------|
| **op** | 사용자 대면, inbox 수신, 검토, 머지, 에스컬레이션 승인 | 메인 repo (work_branch) |
| **head** | 요청 분해 → plan 작성 → sub 디스패치 → 결과 취합 → review-inbox 제출 | `../<project>-wt-head/` (wt/head) |
| **sub** | 할당된 task 만 수행, 범위 밖 금지 | `../<project>-wt-<name>/` (wt/\<name\>) |

각 계층은 **독립 Claude 프로세스** (subprocess 로 spawn). 세션 간 직접 통신 없고 파일이 유일한 정본.

**Discord 봇 (Phase 6)** 은 op 의 원격 트리거 인터페이스 — `@bot send` 한 번이면 op→head→sub 전체 스폰 + review verdict 리액션 prompt 까지 자동.

## 안전장치

1. **pre-commit scope 검증** — `wt/<sub>` 브랜치에서 자기 범위 밖 파일 커밋 시도 차단
2. **3심 LLM 판사** — PreToolUse 에서 파괴적/모호한 명령 심사 후 에스컬레이션
3. **dual-source 방지** — head 만 쓰는 영역 / op 만 쓰는 영역 명확 분리
4. **STOP 시그널** — `coordination/STOP` 파일 존재 시 모든 sub 대기
5. **failed 브랜치 보관** — sub 실패 시 커밋은 `failed/wt-<name>-<ts>` 로 보존
6. **HEAD_LOCK 직렬화** — 같은 head worktree 에 동시 요청 → 봇이 큐로 순차 처리

## 문서

### 사용자
- [INSTALL.md](INSTALL.md) — flat 설치 상세
- [docs/install-subtree.md](docs/install-subtree.md) — `.coord/` subtree 설치
- [docs/upgrade.md](docs/upgrade.md) — 업그레이드 가이드 (tarball/git/subtree)
- [bot/README.md](bot/README.md) — Discord 봇 셋업 + 명령 + 자동 orchestration
- [docs/setup-cloudflare-tunnel.md](docs/setup-cloudflare-tunnel.md) — 대시보드 외부 접근

### 운영
- [coordination/USAGE.md](coordination/USAGE.md) — 시나리오별 사용 흐름
- [coordination/README.md](coordination/README.md) — 조율 규약
- [coordination/TROUBLESHOOTING.md](coordination/TROUBLESHOOTING.md) — 장애 대응
- [coordination/ROADMAP.md](coordination/ROADMAP.md) — 로드맵 / 현재 상태

### 유지보수
- [CLAUDE.md](CLAUDE.md) — 이 repo 개발 규약 (PR 원칙, 테스트, 역전파)

## 상태 (2026-04-24 기준, v0.46)

| Phase | 설명 | 상태 |
|-------|------|------|
| 0~2.6 | 파일 기반 조율, 3심 사법부, 토큰 추적, 클립보드 첨부 | ✅ 완성 |
| 2 | 안정화 (notify-step, reset-wt-head config-driven) | ✅ 완성 |
| 3 | 라이브 대시보드 + Cloudflare TryCloudflare | ✅ 완성 |
| 5-2 | `init.sh` 초기화 마법사 | ✅ 완성 |
| 5-7 | sub worktree 일괄 자동 생성 | ✅ 완성 |
| 5-8 | `upgrade-coord.sh` 3-mode 업그레이드 | ✅ 완성 (v0.3) |
| 5-9 | `.coord/` subdir + COORD_ROOT/PROJECT_ROOT | ✅ 완성 (v0.4) |
| 6 | Discord Bot MVP + 자동 orchestration | ✅ 완성 (v0.6 → v0.12) |
| 머지 | work ↔ main 자동화 (merge=ours + auto back-merge) | ✅ 완성 (v0.46) |
| 다음 | `/inbox` pending loop / `/fix` auto-commit | 🔄 v0.47 후보 |
| 다음 | verdict: go 자동 work-branch 머지 + inbox status 갱신 | 🔄 v0.48 후보 |
| 4 | GitHub webhook 수신기 | ↓ 우선순위 (Phase 6 로 대체) |
| 7 | CLI | ❌ 계획 제외 |

**v1.0 gate**: DEALOS 외 제3 프로젝트 실사용 검증.

## 라이센스

MIT (추후 확정)

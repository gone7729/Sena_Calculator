# coord-template 업그레이드 가이드

`scripts/upgrade-coord.sh` 는 이미 설치된 coord-template 을 **사용자 설정/런타임 상태를 깨지 않고** 신 버전으로 갱신한다.

> v0.3 에서 도입. v0.2 이하 설치는 최초 1회 v0.3 로 업그레이드 (또는 v0.3 이상으로) 이후부터 이 스크립트 사용.

---

## TL;DR

```bash
# 가장 흔한 사용 (tarball + dry-run 먼저)
bash .coord/scripts/upgrade-coord.sh --version v0.46 --dry-run
bash .coord/scripts/upgrade-coord.sh --version v0.46

# subtree 로 설치한 프로젝트 (v0.4+ 에서 정식 지원, v0.46+ 기본)
bash .coord/scripts/upgrade-coord.sh --mode subtree --version v0.46

# 모든 manual-merge 일괄 승인 (내 커스터마이즈 없을 때)
bash .coord/scripts/upgrade-coord.sh --version v0.46 --accept-all
```

**v0.46+ 업그레이드 체크리스트** (DEALOS 같은 v0.44 이전 설치본):
1. upgrade-coord.sh 실행 — `.gitattributes` 에 `.coord/** merge=ours` 자동 추가
2. `@bot main-merge --drop-coord` 1회 실행 — main 에 남은 과거 `.coord/` 이력 청소
3. 이후 `@bot main-merge` 는 자동 drop + forward merge + auto back-merge

---

## 동작 요약 (10단계)

1. **사전 점검** — git clean / work_branch 일치 / STOP 시그널 없음 / config.yml 존재
2. **모드 감지** — `.coord/` subtree prefix 유무, gh/git 가용성 → `tarball | git | subtree` 중 하나
3. **소스 획득** — 모드에 따라 tarball 다운로드 / git clone / subtree pull
4. **백업** — `.coord-backup/<timestamp>/` 에 덮어쓸/병합할 파일 복사 (MANIFEST.md 포함)
5. **분류별 적용** — 파일별로 overwrite / preserve / manual-merge 판정 후 처리
6. **.claude/ 재동기화** — `sync-claude.sh` 로 별도 경로 (manual-merge 규칙 그대로 적용)
7. **config.yml 신규 필드 확인** — 업스트림 `.example` vs 로컬 config 구조 diff
8. **.coord-version 갱신** — 새 버전 기록
9. **CHANGELOG 출력** — 이전 버전 ~ 새 버전 간 커밋 요약
10. **사후 점검** — `bash -n` + `core.hooksPath` 확인

---

## 분류 규칙

소스 모드와 무관하게 **동일** 적용:

| 정책 | 동작 | 대상 |
|------|------|------|
| **overwrite** | 자동 덮어쓰기 (기존 백업) | `scripts/`, `.githooks/`, `templates/`, `docs/`, `coordination/README.md / USAGE.md / ROADMAP.md / TROUBLESHOOTING.md`, `coordination/*.example`, `coordination/roles/_examples/`, `INSTALL.md`, `.env.local.example`, `.gitattributes`, `.claude/settings.local.json.example`, `.claude/settings.json` |
| **preserve** | 건드리지 않음 | `coordination/config.yml`, `coordination/bot.yml` (v0.22+), `coordination/.coord-version`, `coordination/{inbox,plans,tasks,reports,review-inbox,escalations,approvals}/`, `coordination/hotfixes.md`, `coordination/token-budget.md`, `coordination/STOP`/`HEAD_LOCK`/`dashboard-url.txt`, `coordination/roles/*.md` (head.md 제외), `.env.local`, `.env.local.wt`, `.claude/settings.local.json`, `CLAUDE.md` |
| **manual-merge** | diff 보고 y/n | `.claude/commands/*.md`, `coordination/roles/head.md`, `README.md`, `.gitignore` |
| **skip** | 무시 | `.git/`, `.coord-backup/`, `node_modules/`, `*.bak`, `*.swp`, `__pycache__/` |

분류 로직 수정은 [scripts/lib/classify.sh](../scripts/lib/classify.sh).

---

## 플래그

| 플래그 | 설명 |
|--------|------|
| `--version vX.Y` | 업스트림 버전 태그 (생략 시 `gh release view` 로 latest 조회) |
| `--mode MODE` | `tarball` / `git` / `subtree` / `auto` (기본: auto) |
| `--repo URL` | 템플릿 repo URL (기본: `.coord-version` 의 `template_url`) |
| `--accept-commands` | `.claude/commands/*.md` 일괄 승인 |
| `--accept-roles` | `coordination/roles/head.md` 일괄 승인 |
| `--accept-all` | 위 둘 합친 것 |
| `--dry-run` | 실제 쓰기 없이 계획만 출력 |
| `--skip-backup` | 백업 건너뜀 (위험 — 롤백 불가) |
| `--force` | git clean / STOP / work_branch 체크 건너뜀 |

---

## 3가지 소스 모드

### tarball (기본, 가장 가볍고 빠름)
- `gh release download vX.Y --archive=tar.gz` 로 공식 릴리스 tar 파일 받음
- `gh` CLI 필요 (authenticated)
- private repo 도 gh 인증만 되면 가능

### git (gh 없을 때 fallback)
- `git clone --depth 1 -b vX.Y <url>` 로 소스 트리 받음
- 태그가 해당 버전으로 푸시돼 있어야 함
- `--repo` 로 커스텀 URL 지정 가능

### subtree (v0.4+ 전용 권장)
- `git subtree pull --prefix=.coord <url> vX.Y --squash` 직접 수행
- 이미 subtree 로 설치한 프로젝트만 자동 감지
- subtree merge 충돌 발생 시 수동 해결 후 `--mode=subtree --force --skip-backup` 재실행

---

## 워크플로우 추천

### 첫 업그레이드 (v0.2 → v0.3 예시)

```bash
# 1. 업그레이드 전 상태 점검
git status                          # clean 이어야 함
cat coordination/.coord-version 2>/dev/null || echo "(v0.2 이하)"

# 2. dry-run
bash scripts/upgrade-coord.sh --version v0.3 --dry-run

# 3. 실제 적용 (manual-merge 시 diff 읽고 판단)
bash scripts/upgrade-coord.sh --version v0.3

# 4. 변경사항 커밋
git add -A
git status                          # 예상 변경인지 확인
git commit -m "chore: upgrade coord-template to v0.3"

# 5. smoke test
bash scripts/list-worktrees.sh
# Claude 세션에서 /inbox-send "업그레이드 후 테스트"
```

### 커스터마이즈 없을 때 (일괄 자동)

```bash
bash scripts/upgrade-coord.sh --version v0.3 --accept-all
```

### 여러 worktree 에 동기화

head / sub worktree 는 업그레이드 대상이 아님 (메인 repo 에서만 실행). 업그레이드 후 각 worktree 는 `wt/*` 브랜치가 work_branch 로 rebase 되면 자동으로 새 `scripts/`, `.claude/commands/` 를 받음.

```bash
# 메인 repo 업그레이드 + 커밋 + push 후
for wt in head db backend frontend; do
  cd ../myproject-wt-$wt
  git fetch origin main
  git rebase origin/main
  cd -
done
```

---

## 롤백

**자동 롤백 명령은 없다** (의도적 — 위험한 작업).

```bash
# 최근 백업 확인
ls -lt .coord-backup/ | head -5

# 전체 복원
rsync -a .coord-backup/<timestamp>/ ./

# 특정 파일만
cp .coord-backup/<timestamp>/.claude/commands/inbox-send.md .claude/commands/
```

git 이 커밋 전이면 `git checkout --` 가 더 빠름.

자세한 복구 시나리오: [coordination/TROUBLESHOOTING.md](../coordination/TROUBLESHOOTING.md#16) 16번 섹션.

---

## 알려진 제약

- **멀티 worktree**: 업그레이드는 메인 repo 에서만. 각 worktree 는 rebase 로 전파.
- **기존 flat → subdir (v0.4) 마이그레이션 없음**: v0.4 의 subdir 레이아웃은 신규 프로젝트 전용.
- **template_url 오탐**: `.coord-version` 없이 처음 업그레이드하면 기본 URL (`gone7729/coord-template`) 사용. 포크 사용자는 `--repo` 로 명시.
- **Windows line endings**: Git Bash 에서 `autocrlf=true` 면 쉘 스크립트 LF/CRLF 오염 가능. `.gitattributes` 로 `*.sh text eol=lf` 강제 (v0.4 에서 정식 추가).

---

## 관련 파일

- [scripts/upgrade-coord.sh](../scripts/upgrade-coord.sh) — 메인 진입점
- [scripts/lib/classify.sh](../scripts/lib/classify.sh) — 분류 규칙
- [scripts/lib/sync-claude.sh](../scripts/lib/sync-claude.sh) — `.claude/` 재동기화 헬퍼
- [coordination/.coord-version.example](../coordination/.coord-version.example) — 버전 메타데이터 포맷
- [coordination/ROADMAP.md](../coordination/ROADMAP.md#phase-5-8-v03-후보) — Phase 5-8 설계
- [coordination/TROUBLESHOOTING.md](../coordination/TROUBLESHOOTING.md#16) — 실패 복구

# `.coord/` 서브디렉토리 모드 (subtree 설치)

v0.4 부터 coord-template 은 **git subtree** 로 프로젝트에 `.coord/` 디렉토리로 마운트할 수 있다.
기존 **flat 모드**(파일을 프로젝트 루트에 직접 복사) 와 **signal-equivalent** — 기능은 동일하고 레이아웃만 다름.

> **마이그레이션 없음**: 이미 flat 모드로 설치한 프로젝트(DEALOS 포함)는 그대로 유지. subtree 는 **신규 프로젝트 한정** 옵션.

---

## 왜 subtree 모드?

- **업그레이드 단순화** — `git subtree pull` 한 줄로 템플릿 갱신. [upgrade-coord.sh](upgrade.md) 가 `.claude/` 재동기화/CHANGELOG/버전 갱신을 이어받는다.
- **템플릿 경계 가시화** — `.coord/` 안이면 템플릿, 밖이면 내 프로젝트. 파일 섞임 없음.
- **부분 체크아웃 친화** — CI 에서 `.coord/` 만 git sparse-checkout 으로 받아 검증 가능.

**단점:**
- `.coord/` 경로 접두사 익숙해질 시간 필요 (`bash .coord/scripts/setup-worktree.sh ...`)
- `.claude/` 는 여전히 루트에 있어야 함 (Claude Code 제약) — init.sh 가 `.coord/.claude/` → 루트 `.claude/` 로 동기화
- flat ↔ subdir 상호 마이그레이션 미지원

---

## 레이아웃 비교

### flat (v0.3 까지, 기본)
```
myproject/
├── scripts/                        # 템플릿 스크립트
├── coordination/                   # 설정 + 런타임 상태
├── templates/ docs/ .githooks/     # 템플릿
├── .claude/                        # Claude Code 설정
├── .env.local                      # 사용자 비밀
└── (프로젝트 고유 파일)
```

### subdir (v0.4+, subtree)
```
myproject/
├── .coord/                         # subtree prefix — 템플릿 소유
│   ├── scripts/
│   ├── coordination/               # config.yml, inbox/, plans/ 등
│   ├── templates/ docs/ .githooks/
│   ├── .claude/                    # ← 템플릿 측 원본 (init.sh 가 아래로 복사)
│   └── VERSION
├── .claude/                        # 프로젝트 루트 (Claude Code 가 여기를 읽음)
├── .env.local                      # 사용자 비밀 (PROJECT_ROOT)
├── .gitignore / .gitattributes     # 프로젝트 루트
└── (프로젝트 고유 파일)
```

**중요**:
- 대부분의 파일 → `.coord/` 안 (COORD_ROOT)
- `.claude/`, `.env.local`, `.gitignore`, `.gitattributes` → 프로젝트 루트 (PROJECT_ROOT)
- `coordination/config.yml` 및 런타임 상태(inbox/plans/...) → `.coord/coordination/`

---

## 설치

### 최초 설치

```bash
cd my-new-project
git init

# 1. coord-template 을 .coord/ 서브트리로 마운트
git subtree add --prefix=.coord \
  https://github.com/gone7729/coord-template.git v0.4 --squash

# 2. init.sh 실행 — subdir 모드 자동 감지
bash .coord/scripts/init.sh

# 생성되는 것:
#   .coord/coordination/config.yml
#   PROJECT_ROOT/.env.local
#   PROJECT_ROOT/.claude/ ← .coord/.claude/ 에서 복사됨
#   PROJECT_ROOT/.gitignore 갱신
#   core.hooksPath = .coord/.githooks 설정

# 3. (선택) 커밋
git add .
git commit -m "chore: coord-template v0.4 subtree 설치"
```

### 업그레이드

```bash
# 1. subtree pull 로 업스트림 반영
git subtree pull --prefix=.coord \
  https://github.com/gone7729/coord-template.git v0.5 --squash

# 2. .claude/ 재동기화 + CHANGELOG + 버전 기록
bash .coord/scripts/upgrade-coord.sh --mode=subtree

# 3. 예상대로면 커밋
git status
git commit -am "chore: upgrade coord-template to v0.5"
```

`upgrade-coord.sh --mode=subtree` 는 subtree pull 이 `.coord/` 에 써둔 내용 위에:
- `.claude/commands/*.md` — manual-merge 룰 적용 (사용자 커스터마이즈 보존)
- `.env.local.example`, `.gitignore`, `.gitattributes` — PROJECT_ROOT 에 동기화
- `coordination/.coord-version` 갱신
- CHANGELOG 출력

---

## 작동 원리 (경로 라우팅)

scripts/config.sh 가 **두 가지 ROOT** 를 자동 감지:

```bash
# 예시: bash .coord/scripts/setup-worktree.sh db
# scripts/config.sh 가 로드되면:
COORD_ROOT=/home/user/myproject/.coord
PROJECT_ROOT=/home/user/myproject
INSTALL_MODE=subdir
```

스크립트는 리소스 종류별로 어느 ROOT 를 쓸지 결정:

| 리소스 | 기준 | 예시 경로 |
|--------|------|----------|
| `coordination/*`, `scripts/*`, `templates/*`, `docs/*` | COORD_ROOT | `.coord/coordination/config.yml` |
| `.env.local`, `.claude/`, `.gitignore`, `.gitattributes` | PROJECT_ROOT | `.env.local` |
| worktree 디렉토리 `../<project>-wt-<sub>/` | PROJECT_ROOT sibling | `../myproject-wt-db/` |
| `core.hooksPath` | PROJECT_ROOT 상대 | `.coord/.githooks` |

flat 모드에선 COORD_ROOT = PROJECT_ROOT 라 구분 무의미 (자연스럽게 동일 동작).

---

## 커스텀 COORD_ROOT 경로

기본 prefix 는 `.coord` 지만, 다른 이름을 쓰고 싶으면:

```bash
git subtree add --prefix=tools/coord ...
```

하지만 **검증 안 된 경로**. `config.sh` 의 `basename == ".coord"` 감지가 작동하지 않아 INSTALL_MODE 가 flat 으로 오인됨. 이후 v1.0 에서 정식 지원 예정. 지금은 `.coord` prefix 권장.

---

## subtree pull 충돌

`git subtree pull` 은 사용자가 `.coord/` 내부 파일을 수정한 경우 표준 git merge 충돌 발생:

```
CONFLICT (content): Merge conflict in .coord/scripts/foo.sh
```

해결:
```bash
# 충돌 파일 확인
git status

# 수동 해결 후
git add .coord/scripts/foo.sh
git commit    # subtree merge 커밋 완성

# 이어서 upgrade-coord.sh 의 나머지 단계
bash .coord/scripts/upgrade-coord.sh --mode=subtree --force --skip-backup
```

**팁**: 사용자 커스터마이즈는 `.coord/` 바깥 (예: 자체 스크립트 `./scripts/my-custom.sh`) 또는 `coordination/config.yml` (보존 대상) 에 두면 subtree pull 충돌 없음.

---

## 관련 문서

- [upgrade.md](upgrade.md) — 업그레이드 흐름 전반 (tarball / git / subtree 3-mode 공통)
- [README.md](../README.md) — 템플릿 전체 소개
- [INSTALL.md](../INSTALL.md) — flat 모드 설치 가이드 (기존)
- [coordination/ROADMAP.md](../coordination/ROADMAP.md) — Phase 5-9 설계

#!/usr/bin/env bash
# classify.sh — 파일 경로를 업그레이드 정책으로 분류
#
# source 후 사용:
#   classify_path "scripts/foo.sh"           # → overwrite
#   classify_path "coordination/config.yml"  # → preserve
#   classify_path ".claude/commands/go.md"   # → manual-merge
#
# 정책:
#   overwrite    — 새 버전으로 완전 교체 (템플릿 지분)
#   preserve     — 건드리지 않음 (사용자/런타임 지분)
#   manual-merge — diff 출력 후 사용자 승인 (경계 지분)
#   skip         — 무시 (백업/임시/git 내부)
#
# 정책 우선순위: skip > preserve > manual-merge > overwrite > (unknown → overwrite)
#
# v0.3 (Phase 5-8) 과 v0.4 (Phase 5-9) 모두 이 파일을 source 로 공유.

classify_path() {
  local path="$1"

  # v0.42+: subdir 모드 경로 정규화 — 호출자가 .coord/... 를 넘겨도 flat 패턴으로 매칭
  # classify 는 "소유권" 판단이라 prefix 와 무관해야 함.
  local normalized="${path#.coord/}"

  # === SKIP (절대 건드리지 않음, 백업 대상도 아님) ===
  case "$path" in
    .git/*|.git) echo "skip"; return ;;
    .coord-backup/*) echo "skip"; return ;;
    node_modules/*) echo "skip"; return ;;
    __pycache__/*|*.pyc|*.pyo) echo "skip"; return ;;
    *.swp|.DS_Store|Thumbs.db) echo "skip"; return ;;
    coordination/config.yml.bak.*|.coord/coordination/config.yml.bak.*) echo "skip"; return ;;
    .env.local.bak|*.bak) echo "skip"; return ;;
  esac

  # 정규화된 경로로 preserve/manual-merge/overwrite 매칭 — .coord/ prefix 제거 후 기존 규칙 재사용
  [[ "$normalized" != "$path" ]] && path="$normalized"

  # === PRESERVE (사용자 데이터 / 런타임 상태 / 비밀) ===
  case "$path" in
    # config 본체
    coordination/config.yml) echo "preserve"; return ;;
    coordination/.coord-version) echo "preserve"; return ;;
    coordination/bot.yml) echo "preserve"; return ;;                 # v0.22: 봇 바인딩
    # 런타임 상태
    coordination/inbox/*) echo "preserve"; return ;;
    coordination/plans/*) echo "preserve"; return ;;
    coordination/tasks/*) echo "preserve"; return ;;
    coordination/reports/*) echo "preserve"; return ;;
    coordination/review-inbox/*) echo "preserve"; return ;;
    coordination/escalations/*) echo "preserve"; return ;;
    coordination/approvals/*) echo "preserve"; return ;;
    coordination/STOP) echo "preserve"; return ;;
    coordination/HEAD_LOCK) echo "preserve"; return ;;
    coordination/dashboard-url.txt) echo "preserve"; return ;;
    # 사용자가 채운 장기 기록
    coordination/hotfixes.md) echo "preserve"; return ;;
    coordination/token-budget.md) echo "preserve"; return ;;
    # 비밀 / 개인 설정
    .env.local) echo "preserve"; return ;;
    .env.local.wt) echo "preserve"; return ;;
    .claude/settings.local.json) echo "preserve"; return ;;
    bot/projects.yml) echo "preserve"; return ;;
    # 사용자가 작성한 최상위 규약 문서
    CLAUDE.md) echo "preserve"; return ;;
    # 사용자-작성 역할 (head.md / _examples 는 아래에서 처리)
    coordination/roles/_examples/*) ;;  # fall through to overwrite
    coordination/roles/head.md) ;;      # fall through to manual-merge
    coordination/roles/*) echo "preserve"; return ;;
  esac

  # === MANUAL-MERGE (사용자가 커스터마이즈했을 수 있음) ===
  case "$path" in
    .claude/commands/*.md) echo "manual-merge"; return ;;
    coordination/roles/head.md) echo "manual-merge"; return ;;
    # 최상위 가변 문서 — 템플릿 + 프로젝트 고유 섞일 수 있음
    README.md) echo "manual-merge"; return ;;
    .gitignore) echo "manual-merge"; return ;;
  esac

  # === OVERWRITE (순수 템플릿 지분) ===
  case "$path" in
    scripts/*) echo "overwrite"; return ;;
    .githooks/*) echo "overwrite"; return ;;
    templates/*) echo "overwrite"; return ;;
    docs/*) echo "overwrite"; return ;;
    bot/controller.py|bot/requirements.txt|bot/README.md) echo "overwrite"; return ;;
    bot/*.example) echo "overwrite"; return ;;
    .claude/settings.local.json.example) echo "overwrite"; return ;;
    .claude/settings.json) echo "overwrite"; return ;;
    coordination/*.example) echo "overwrite"; return ;;
    coordination/README.md) echo "overwrite"; return ;;
    coordination/USAGE.md) echo "overwrite"; return ;;
    coordination/ROADMAP.md) echo "overwrite"; return ;;
    coordination/TROUBLESHOOTING.md) echo "overwrite"; return ;;
    coordination/roles/_examples/*) echo "overwrite"; return ;;
    INSTALL.md) echo "overwrite"; return ;;
    .env.local.example) echo "overwrite"; return ;;
    .gitattributes) echo "overwrite"; return ;;
  esac

  # === UNKNOWN (분류 규칙에 없음 → 신규 파일로 간주해 overwrite) ===
  # upgrade-coord.sh 가 로컬에만 있고 upstream 엔 없는 케이스는 자체 처리
  # (이 함수는 upstream 에 있는 파일 기준으로만 호출됨)
  echo "overwrite"
}

# === 디렉토리가 완전 overwrite 단위인지 ===
# (rsync --delete 로 한꺼번에 처리 가능 = 디렉토리 전체가 템플릿 소유)
# 반환: 0 = 단위 overwrite 가능, 1 = 파일별 분류 필요
classify_dir_is_pure_overwrite() {
  local dir="$1"
  case "$dir" in
    scripts|scripts/*) return 0 ;;
    .githooks|.githooks/*) return 0 ;;
    templates|templates/*) return 0 ;;
    docs|docs/*) return 0 ;;
    coordination/roles/_examples|coordination/roles/_examples/*) return 0 ;;
    *) return 1 ;;
  esac
}

# === 사람이 읽는 정책 설명 ===
classify_policy_label() {
  case "$1" in
    overwrite)    echo "자동 덮어쓰기 (템플릿 지분)" ;;
    preserve)     echo "보존 (사용자/런타임 지분)" ;;
    manual-merge) echo "수동 병합 (경계 지분 — 커스터마이즈 가능)" ;;
    skip)         echo "무시 (백업/임시)" ;;
    *)            echo "unknown" ;;
  esac
}

# === 파일이 어느 ROOT 에 귀속되는지 (v0.4 subdir 모드 대응) ===
# 반환: "project" (PROJECT_ROOT 기준) | "coord" (COORD_ROOT 기준)
#   project — Claude Code 규약 / git repo 규약상 반드시 프로젝트 루트에 있어야 함
#   coord   — 템플릿 내부 (flat 이면 project-root, subdir 이면 .coord/)
# flat 모드에선 두 ROOT 가 동일해서 구분 무의미. subdir 모드 라우팅용.
classify_root() {
  local path="$1"
  case "$path" in
    .claude/*)             echo "project"; return ;;
    .env.local.example)    echo "project"; return ;;
    .gitignore)            echo "project"; return ;;
    .gitattributes)        echo "project"; return ;;
  esac
  # 루트 markdown 파일도 flat 에선 PROJECT_ROOT 에, subdir 에선 .coord/ 에 머물러야 함
  # → "coord" 로 두면 COORD_ROOT 기준으로 라우팅돼 flat/subdir 둘 다 자연스럽게 작동.
  echo "coord"
}

export -f classify_path classify_dir_is_pure_overwrite classify_policy_label classify_root 2>/dev/null || true

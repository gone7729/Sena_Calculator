#!/usr/bin/env bash
# 병렬 worktree 세팅 스크립트 (config.yml 기반)
# 사용법:
#   bash scripts/setup-worktree.sh <name> [port] [base-branch]
# 예:
#   bash scripts/setup-worktree.sh a 3001 main
#   bash scripts/setup-worktree.sh feat-massing 3002 develop
#
# 결과:
#   ../<project>-wt-<name>/  디렉토리에 새 worktree 생성
#   - 브랜치: wt/<name> (base에서 분기)
#   - .env.local 복사 + npm install 안내 + PORT 설정

set -euo pipefail

# config.sh 로드
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true
PROJECT="${PROJECT_NAME:-project}"
DEFAULT_BASE="${WORK_BRANCH:-main}"

NAME="${1:-}"
PORT="${2:-3001}"
BASE="${3:-$DEFAULT_BASE}"

if [[ -z "$NAME" ]]; then
  echo "사용법: bash scripts/setup-worktree.sh <name> [port] [base-branch]"
  echo "예:    bash scripts/setup-worktree.sh a 3001 $DEFAULT_BASE"
  echo "프로젝트: $PROJECT"
  exit 1
fi

# 현재 repo 루트 (v0.4+: config.sh 의 PROJECT_ROOT 사용)
# config.sh 가 로드되면 PROJECT_ROOT 가 export 됨. 그 외 fallback 으로 git rev-parse.
REPO_ROOT="${PROJECT_ROOT:-$(git rev-parse --show-toplevel)}"
PARENT_DIR="$(dirname "$REPO_ROOT")"
WT_DIR="$PARENT_DIR/${PROJECT}-wt-$NAME"
BRANCH="wt/$NAME"

echo "▶ Worktree 세팅"
echo "  이름   : $NAME"
echo "  경로   : $WT_DIR"
echo "  브랜치 : $BRANCH (base: $BASE)"
echo "  포트   : $PORT"
echo ""

# 1. 이미 존재하는지 확인
if [[ -d "$WT_DIR" ]]; then
  echo "❌ 이미 존재: $WT_DIR"
  echo "   삭제하려면: git worktree remove $WT_DIR"
  exit 1
fi

if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  echo "❌ 브랜치 '$BRANCH' 이미 존재"
  echo "   다른 이름을 쓰거나 삭제: git branch -D $BRANCH"
  exit 1
fi

# 2. base 브랜치 최신화 (fetch 만, merge 안 함)
echo "▶ origin fetch..."
git fetch origin "$BASE" --quiet || echo "  (원격 접근 실패 - 로컬 base 사용)"

# 3. worktree 생성
echo "▶ worktree 생성..."
git worktree add -b "$BRANCH" "$WT_DIR" "$BASE"

# 4. .env.local 연결 (v0.35: symlink 우선 — main 수정만으로 전 worktree 반영)
# v0.38+: ENV_FILE_RESOLVED (config.sh 해결값) 사용 — .coord/.env.local 우선.
ENV_SRC="${ENV_FILE_RESOLVED:-$REPO_ROOT/.env.local}"
if [[ -f "$ENV_SRC" ]]; then
  if ln -s "$ENV_SRC" "$WT_DIR/.env.local" 2>/dev/null; then
    echo "▶ .env.local symlink → $ENV_SRC"
  else
    # symlink 실패 (Windows 권한 없음 등) → cp fallback
    cp "$ENV_SRC" "$WT_DIR/.env.local"
    echo "▶ .env.local 복사 (symlink 실패 — Windows 는 개발자 모드 활성화 시 symlink 가능)"
  fi
fi

# 5. 포트 오버라이드 파일 (Next.js는 PORT 환경변수 사용)
cat > "$WT_DIR/.env.local.wt" <<EOF
# 이 worktree 전용 - git ignored
PORT=$PORT
WT_NAME=$NAME
EOF

# 6. 실행 스크립트 생성 (포트 포함)
cat > "$WT_DIR/run-dev.sh" <<EOF
#!/usr/bin/env bash
# worktree '$NAME' 전용 dev 서버 (포트 $PORT)
cd "\$(dirname "\$0")"
PORT=$PORT npm run dev
EOF
chmod +x "$WT_DIR/run-dev.sh"

# 7. npm install (시간 오래 걸림 - 백그라운드 권장)
echo "▶ npm install (시간 소요 - 별도 터미널 권장)..."
echo "   실행: cd $WT_DIR && npm install"
echo ""

# 8. .gitignore 점검 (worktree용 파일)
if ! grep -q ".env.local.wt" "$REPO_ROOT/.gitignore" 2>/dev/null; then
  echo ".env.local.wt" >> "$REPO_ROOT/.gitignore"
  echo "run-dev.sh" >> "$REPO_ROOT/.gitignore"
  echo "▶ .gitignore 갱신 (.env.local.wt, run-dev.sh)"
fi

echo ""
echo "✅ 완료: $WT_DIR"
echo ""
echo "다음 단계:"
echo "  1. cd $WT_DIR"
echo "  2. npm install          # 의존성 설치"
echo "  3. ./run-dev.sh          # dev 서버 (포트 $PORT)"
echo "  4. claude                # 이 디렉토리에서 새 Claude 세션 시작"
echo ""
echo "Worktree 목록 확인: git worktree list"
echo "삭제:              git worktree remove $WT_DIR && git branch -D $BRANCH"

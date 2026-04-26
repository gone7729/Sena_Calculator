#!/usr/bin/env bash
# 기존 worktree 의 .env.local 을 main repo 의 .env.local symlink 로 변환 (v0.35+)
#
# 배경: v0.34 이전에는 setup-worktree.sh 가 .env.local 을 cp 로 복사했다.
# 결과로 worktree 마다 독립 사본이 생겨, webhook URL 같은 값을 바꾸면
# main 외에도 모든 worktree 를 개별 수정해야 했음 (옛 채널로 알림 가는 사고 다발).
# v0.35 부터 setup-worktree.sh 는 symlink 로 바꿨고, 이 스크립트는
# 기존 설치본을 같은 구조로 마이그레이션한다.
#
# 사용법:
#   bash scripts/relink-worktree-env.sh [--dry-run] [--force]
#
# 동작:
#   1. PROJECT_ROOT/.env.local 존재 확인
#   2. ../<project>-wt-* 의 모든 worktree 순회
#   3. 각 worktree 의 .env.local 을:
#      - 이미 symlink  → skip (이미 변환됨)
#      - 파일 없음     → symlink 생성
#      - 실제 파일이면 main 과 내용 비교:
#        · 같으면       → 백업 후 삭제 + symlink 생성
#        · 다르면       → 기본은 skip (경고). --force 면 강제 교체
#
# 옵션:
#   --dry-run   실제 변경 없이 계획만 출력
#   --force     내용 다른 worktree 도 main 기준으로 덮어쓰기 (backup 남김)
#
# 멱등성: 여러 번 돌려도 안전. 이미 symlink 면 skip.

set -euo pipefail

DRY_RUN=0
FORCE=0
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    --force)   FORCE=1 ;;
    -h|--help)
      sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
      exit 0
      ;;
    *) echo "⚠ 알 수 없는 인자: $arg"; exit 1 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true

REPO_ROOT="${PROJECT_ROOT:-$(git rev-parse --show-toplevel)}"
PROJECT="${PROJECT_NAME:-$(basename "$REPO_ROOT")}"
PARENT_DIR="$(dirname "$REPO_ROOT")"

# v0.38+: ENV_FILE_RESOLVED (config.sh 해결) 우선 — .coord/.env.local
MAIN_ENV="${ENV_FILE_RESOLVED:-$REPO_ROOT/.env.local}"

echo "🔗 .env.local worktree symlink 마이그레이션"
echo "   PROJECT_ROOT : $REPO_ROOT"
echo "   main .env    : $MAIN_ENV"
[[ $DRY_RUN -eq 1 ]] && echo "   mode         : DRY-RUN (변경 안 함)"
[[ $FORCE -eq 1 ]]   && echo "   mode         : FORCE (내용 다르면 덮어쓰기)"
echo

if [[ ! -f "$MAIN_ENV" ]]; then
  echo "❌ main .env.local 없음 — 먼저 $MAIN_ENV 을 만드세요"
  exit 1
fi

# worktree 디렉토리 수집
shopt -s nullglob
WT_DIRS=("$PARENT_DIR/${PROJECT}-wt-"*)
shopt -u nullglob

if [[ ${#WT_DIRS[@]} -eq 0 ]]; then
  echo "⚠ worktree 없음 (패턴: $PARENT_DIR/${PROJECT}-wt-*)"
  exit 0
fi

echo "📦 발견된 worktree: ${#WT_DIRS[@]} 개"
for wt in "${WT_DIRS[@]}"; do echo "   · $wt"; done
echo

# 각 worktree 처리
CONVERTED=0
SKIPPED_ALREADY=0
SKIPPED_DIFF=0
CREATED=0
FAILED=0

for wt in "${WT_DIRS[@]}"; do
  [[ -d "$wt" ]] || continue
  target="$wt/.env.local"
  name="$(basename "$wt")"

  # case 1: 이미 symlink
  # v0.40: ((var++)) 는 이전값 0 반환 → set -e 종료. $((var+1)) 할당 형태 사용.
  if [[ -L "$target" ]]; then
    link_dest="$(readlink "$target")"
    if [[ "$link_dest" == "$MAIN_ENV" ]] || [[ "$(cd "$wt" && realpath "$target" 2>/dev/null)" == "$(realpath "$MAIN_ENV" 2>/dev/null)" ]]; then
      echo "✅ [$name] 이미 symlink → $link_dest"
      SKIPPED_ALREADY=$((SKIPPED_ALREADY+1))
      continue
    else
      echo "⚠ [$name] symlink 이지만 다른 경로 가리킴: $link_dest"
      if [[ $FORCE -eq 1 ]]; then
        if [[ $DRY_RUN -eq 0 ]]; then
          rm "$target"
          if ln -s "$MAIN_ENV" "$target" 2>/dev/null; then
            echo "   → 새 symlink 생성: $MAIN_ENV"
            CONVERTED=$((CONVERTED+1))
          else
            FAILED=$((FAILED+1))
          fi
        else
          echo "   (dry-run) 삭제 + symlink 재생성"
        fi
      else
        echo "   (--force 없이는 skip)"
      fi
      continue
    fi
  fi

  # case 2: 파일 없음
  if [[ ! -e "$target" ]]; then
    if [[ $DRY_RUN -eq 1 ]]; then
      echo "➕ [$name] .env.local 없음 — symlink 생성 예정 (dry-run)"
    else
      if ln -s "$MAIN_ENV" "$target" 2>/dev/null; then
        echo "➕ [$name] symlink 신규 생성"
        CREATED=$((CREATED+1))
      else
        echo "❌ [$name] symlink 생성 실패 (Windows 권한?)"
        FAILED=$((FAILED+1))
      fi
    fi
    continue
  fi

  # case 3: 실제 파일
  if cmp -s "$target" "$MAIN_ENV"; then
    # 내용 동일 — 안전하게 symlink 로 변환
    if [[ $DRY_RUN -eq 1 ]]; then
      echo "🔄 [$name] 내용 동일 — symlink 로 변환 예정 (dry-run)"
    else
      mv "$target" "$target.bak"
      if ln -s "$MAIN_ENV" "$target" 2>/dev/null; then
        echo "🔄 [$name] symlink 로 변환 (백업: .env.local.bak)"
        CONVERTED=$((CONVERTED+1))
      else
        # symlink 실패 — 원래대로 되돌림
        mv "$target.bak" "$target"
        echo "❌ [$name] symlink 생성 실패 — 원본 복구"
        FAILED=$((FAILED+1))
      fi
    fi
  else
    # 내용 다름
    if [[ $FORCE -eq 1 ]]; then
      if [[ $DRY_RUN -eq 1 ]]; then
        echo "⚠ [$name] 내용 다름 — FORCE 모드로 덮어쓰기 예정 (dry-run)"
      else
        mv "$target" "$target.bak"
        if ln -s "$MAIN_ENV" "$target" 2>/dev/null; then
          echo "⚠ [$name] 내용 달랐음 — 덮어씀 (백업: .env.local.bak)"
          CONVERTED=$((CONVERTED+1))
        else
          mv "$target.bak" "$target"
          echo "❌ [$name] symlink 생성 실패 — 원본 복구"
          FAILED=$((FAILED+1))
        fi
      fi
    else
      echo "⚠ [$name] main 과 내용 다름 — skip (덮어쓰려면 --force)"
      echo "   diff (처음 10줄):"
      diff "$MAIN_ENV" "$target" 2>/dev/null | head -10 | sed 's/^/      /'
      SKIPPED_DIFF=$((SKIPPED_DIFF+1))
    fi
  fi
done

echo
echo "── 결과 ────────────────────────"
echo "  ✅ 이미 symlink (skip)    : $SKIPPED_ALREADY"
echo "  ➕ 신규 symlink 생성      : $CREATED"
echo "  🔄 파일→symlink 변환      : $CONVERTED"
echo "  ⚠ 내용 다름 (skip)       : $SKIPPED_DIFF"
echo "  ❌ 실패                   : $FAILED"

if [[ $SKIPPED_DIFF -gt 0 ]]; then
  echo
  echo "💡 내용 다른 worktree 는 개별 편집본이 남아있을 수 있습니다."
  echo "   확인 후 덮어써도 되면: bash scripts/relink-worktree-env.sh --force"
fi

if [[ $FAILED -gt 0 ]]; then
  echo
  echo "❌ symlink 생성 실패 — Windows 에서 개발자 모드 활성화 필요:"
  echo "   설정 → 업데이트 및 보안 → 개발자용 → 개발자 모드 ON"
  echo "   또는 관리자 권한 shell 에서 재실행."
fi

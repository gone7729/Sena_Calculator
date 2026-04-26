#!/usr/bin/env bash
# .coord/.env.local 을 PROJECT_ROOT/.env.local 로 symlink 설정 (v0.39+)
#
# 배경 (v0.38 회고):
#   v0.38 에서 subdir 모드 시 .coord/.env.local 을 자동 생성했으나,
#   Windows Git Bash 에서 symlink 실패 → cp fallback → PROJECT_ROOT/.env.local
#   수정 시 .coord/.env.local 옛 값 고착 → Discord 채널 이전 사고와 동일 패턴.
#   v0.39 부터 PROJECT_ROOT/.env.local 이 primary. isolation 원하면 이 스크립트로
#   명시적 symlink — 실패 시 cp 안 함 (stale 원천 차단).
#
# 사용 시점:
#   프로젝트가 자체 .env.local 을 쓰는 경우 (Next.js 등) — coord secrets 를
#   .coord/.env.local 로 격리하고 싶을 때.
#
# 사용법:
#   bash .coord/scripts/link-env-to-coord.sh [--force]
#
# 동작:
#   1. PROJECT_ROOT/.env.local 존재 확인 (source)
#   2. COORD_ROOT/.env.local 검사:
#      - 이미 symlink 면 → 올바른 경로 가리키는지 확인 후 skip 또는 --force 교체
#      - 실제 파일이면 → 기본 skip (내용 보존). --force 면 백업 후 symlink 교체
#      - 없으면 → symlink 생성
#   3. symlink 실패 시 (Windows 개발자 모드 미활성 등) → 명확히 실패 메시지,
#      cp fallback 안 함 (stale 위험 때문)

set -euo pipefail

FORCE=0
for arg in "$@"; do
  case "$arg" in
    --force|-f) FORCE=1 ;;
    -h|--help)
      sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
      exit 0
      ;;
    *) echo "⚠ 알 수 없는 인자: $arg"; exit 1 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true

: "${PROJECT_ROOT:?PROJECT_ROOT 미해결 — config.sh 를 source 하는 위치에서 실행하세요}"
: "${COORD_ROOT:?COORD_ROOT 미해결 — config.sh 를 source 하는 위치에서 실행하세요}"

if [[ "$PROJECT_ROOT" == "$COORD_ROOT" ]]; then
  echo "❌ flat 모드 — symlink 불필요 (PROJECT_ROOT == COORD_ROOT)"
  echo "   subdir 모드 (.coord/ subtree) 에서만 의미 있는 작업입니다."
  exit 1
fi

SRC="$PROJECT_ROOT/.env.local"
DST="$COORD_ROOT/.env.local"

echo "🔗 .env.local → .coord/.env.local symlink 설정"
echo "   source: $SRC"
echo "   dest  : $DST"
[[ $FORCE -eq 1 ]] && echo "   mode  : FORCE (기존 파일 덮어쓰기, 백업 남김)"
echo

if [[ ! -f "$SRC" ]]; then
  echo "❌ source 없음: $SRC"
  echo "   먼저 PROJECT_ROOT 에 .env.local 을 만드세요:"
  echo "   cp $COORD_ROOT/.env.local.example $SRC"
  exit 1
fi

# 기존 DST 처리
if [[ -L "$DST" ]]; then
  link_dest="$(readlink "$DST")"
  real_src="$(cd "$(dirname "$SRC")" && pwd)/$(basename "$SRC")"
  real_link="$(cd "$(dirname "$DST")" && readlink -f "$DST" 2>/dev/null || echo "$link_dest")"
  if [[ "$real_link" == "$real_src" ]]; then
    echo "✅ 이미 올바른 symlink → $link_dest"
    exit 0
  fi
  echo "⚠ 다른 경로 가리키는 symlink: $link_dest"
  if [[ $FORCE -eq 0 ]]; then
    echo "   교체하려면 --force 지정"
    exit 1
  fi
  rm "$DST"
elif [[ -f "$DST" ]]; then
  if [[ $FORCE -eq 0 ]]; then
    echo "⚠ $DST 에 실제 파일 존재 (내용 보존용 skip)"
    echo "   내용 비교:"
    if cmp -s "$SRC" "$DST"; then
      echo "   → 내용 동일. --force 로 symlink 변환 가능"
    else
      echo "   → 내용 다름. 먼저 수동 확인 후 --force:"
      diff "$SRC" "$DST" 2>/dev/null | head -20 | sed 's/^/      /'
    fi
    exit 1
  fi
  mv "$DST" "$DST.bak"
  echo "📦 기존 파일 백업: $DST.bak"
fi

# symlink 생성 시도
if ln -s "$SRC" "$DST" 2>/dev/null; then
  echo "✅ symlink 생성: $DST → $SRC"
  echo
  echo "이제 $SRC 만 편집하면 .coord/.env.local 도 자동 반영됩니다."
else
  echo "❌ symlink 생성 실패"
  echo
  echo "원인 (Windows 에서 자주 발생):"
  echo "  - 개발자 모드 비활성"
  echo "  - 관리자 권한 아닌 shell"
  echo
  echo "해결:"
  echo "  1. 설정 → 업데이트 및 보안 → 개발자용 → 개발자 모드 ON"
  echo "  2. Git Bash 재시작 후 이 스크립트 재실행"
  echo
  echo "cp fallback 을 의도적으로 제공하지 않습니다 — 수정 시 두 파일 분기로"
  echo "stale 문제가 재발하기 때문 (Discord 채널 이전 사고와 동일 패턴)."
  echo "symlink 못 만들면 PROJECT_ROOT/.env.local 만 사용하세요 — 기본 동작."
  exit 1
fi

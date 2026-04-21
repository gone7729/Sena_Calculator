#!/usr/bin/env bash
# .claude/commands/ 를 타임스탬프 디렉토리에 백업
# reset --hard 같은 위험 작업 전 실행 권장
# 사용법:
#   bash scripts/backup-commands.sh [worktree-path]
#   (인자 없으면 현재 worktree 백업)

set -euo pipefail

TARGET="${1:-.}"
TARGET_ABS="$(cd "$TARGET" && pwd)"
TARGET_NAME="$(basename "$TARGET_ABS")"

if [[ ! -d "$TARGET_ABS/.claude/commands" ]]; then
  echo "⚠ .claude/commands/ 없음: $TARGET_ABS"
  exit 0
fi

TS=$(date +%Y%m%d-%H%M%S)
BACKUP_ROOT="$HOME/.claude-coord-backups"
BACKUP_DIR="$BACKUP_ROOT/$TARGET_NAME-$TS"

mkdir -p "$BACKUP_DIR"
cp -r "$TARGET_ABS/.claude/commands" "$BACKUP_DIR/"

# 메타데이터
cat > "$BACKUP_DIR/meta.txt" <<EOF
source: $TARGET_ABS
branch: $(git -C "$TARGET_ABS" branch --show-current 2>/dev/null || echo "?")
commit: $(git -C "$TARGET_ABS" rev-parse HEAD 2>/dev/null || echo "?")
backed_up_at: $(date -Iseconds)
EOF

echo "✅ 백업 완료: $BACKUP_DIR"
echo "복원: cp -r $BACKUP_DIR/commands/* <target>/.claude/commands/"

# 오래된 백업 정리 (30일+)
find "$BACKUP_ROOT" -maxdepth 1 -type d -name "*-*" -mtime +30 -exec rm -rf {} + 2>/dev/null || true

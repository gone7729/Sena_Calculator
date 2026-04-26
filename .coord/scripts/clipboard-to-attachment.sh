#!/usr/bin/env bash
# 클립보드 이미지를 coordination/inbox/attachments/ 에 PNG 로 저장
# 사용법: bash scripts/clipboard-to-attachment.sh <inbox-id-or-tag>
# 출력 (stdout): 저장된 파일 경로 (성공) 또는 빈 문자열 (이미지 없음)
# Windows 전용 (PowerShell 사용)

set -euo pipefail

TAG="${1:-clipboard}"
TS=$(date +%Y%m%d-%H%M%S)
OUT_DIR="coordination/inbox/attachments"
OUT_FILE="${OUT_DIR}/${TS}-${TAG}.png"

mkdir -p "$OUT_DIR"

# Windows native path 변환
WIN_PATH=$(cygpath -w "$(realpath "$OUT_FILE" 2>/dev/null || echo "$(pwd)/$OUT_FILE")" 2>/dev/null || echo "$OUT_FILE")
# 백슬래시 이스케이프 (PowerShell 단일따옴표 안에서)
WIN_PATH_ESC="${WIN_PATH//\\/\\\\}"

# PowerShell command — 경로 직접 인터폴레이션
PS_CMD="Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing; if ([System.Windows.Forms.Clipboard]::ContainsImage()) { \$img = [System.Windows.Forms.Clipboard]::GetImage(); \$img.Save('$WIN_PATH_ESC', [System.Drawing.Imaging.ImageFormat]::Png); Write-Output 'OK' } else { Write-Output 'NO_IMAGE' }"

RESULT=$(powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$PS_CMD" 2>&1 | tr -d '\r' | tail -1 || echo "ERROR")

case "$RESULT" in
  OK)
    if [[ -f "$OUT_FILE" ]]; then
      echo "$OUT_FILE"
      exit 0
    else
      echo "" >&2
      echo "⚠ PowerShell OK 응답이지만 파일 없음: $OUT_FILE" >&2
      exit 1
    fi
    ;;
  NO_IMAGE)
    rm -f "$OUT_FILE" 2>/dev/null
    echo ""
    exit 0
    ;;
  *)
    rm -f "$OUT_FILE" 2>/dev/null
    echo "" >&2
    echo "⚠ 클립보드 이미지 추출 실패: $RESULT" >&2
    exit 1
    ;;
esac

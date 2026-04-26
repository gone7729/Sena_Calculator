#!/usr/bin/env bash
# headless claude 호출 후 사용량 파싱 + 임계 체크
# 사용법:
#   bash scripts/track-token-usage.sh <log-file> [<context-tag>]
# log-file: claude -p --output-format json 의 출력 파일
# context-tag: 식별용 태그 (예: inbox-id, sub 이름)

set -euo pipefail

LOG_FILE="${1:-}"
TAG="${2:-unknown}"

[[ -z "$LOG_FILE" ]] && { echo "❌ log file 인자 필요"; exit 1; }
[[ ! -f "$LOG_FILE" ]] && { echo "⚠ log file 없음: $LOG_FILE"; exit 0; }

# JSON 출력에서 토큰 사용량 추출
TOKENS=$(python3 -c "
import json, sys
total = 0
try:
    with open('$LOG_FILE') as f:
        for line in f:
            line = line.strip()
            if not line: continue
            try:
                d = json.loads(line)
                # 'result' 타입 메시지의 usage
                if d.get('type') == 'result' and 'usage' in d:
                    u = d['usage']
                    total = (u.get('input_tokens', 0)
                           + u.get('output_tokens', 0)
                           + u.get('cache_creation_input_tokens', 0)
                           + u.get('cache_read_input_tokens', 0))
                    break
                # 'message' 타입에 usage 가 있는 경우 (다른 형식)
                elif 'usage' in d:
                    u = d['usage']
                    t = (u.get('input_tokens', 0)
                       + u.get('output_tokens', 0)
                       + u.get('cache_creation_input_tokens', 0)
                       + u.get('cache_read_input_tokens', 0))
                    if t > total: total = t
            except json.JSONDecodeError:
                pass
except Exception as e:
    print(0)
    sys.exit(0)
print(total)
" 2>/dev/null || echo "0")

if [[ "$TOKENS" == "0" ]] || [[ -z "$TOKENS" ]]; then
  echo "⚠ 토큰 사용량 파싱 실패 또는 0 (log: $LOG_FILE)"
  exit 0
fi

# token-log.md 에 추가
LOG_MD="coordination/token-log.md"
[[ ! -f "$LOG_MD" ]] && echo "# Token Usage Log" > "$LOG_MD"
echo "$(date -Iseconds) | $TAG | $TOKENS tokens | $LOG_FILE" >> "$LOG_MD"

# token-budget.md 의 used_tokens 갱신
BUDGET_MD="coordination/token-budget.md"
if [[ -f "$BUDGET_MD" ]]; then
  CURRENT_USED=$(grep -E "^- \*\*used_tokens\*\*:" "$BUDGET_MD" | head -1 | grep -oE '[0-9]+' | head -1 || echo "0")
  NEW_USED=$((CURRENT_USED + TOKENS))
  NOW=$(date -Iseconds)

  # in-place 갱신
  python3 -c "
import re
with open('$BUDGET_MD') as f:
    content = f.read()
content = re.sub(r'- \*\*used_tokens\*\*:.*', '- **used_tokens**: $NEW_USED', content, count=1)
content = re.sub(r'- \*\*last_update\*\*:.*', '- **last_update**: $NOW', content, count=1)
with open('$BUDGET_MD', 'w') as f:
    f.write(content)
" 2>/dev/null

  echo "✅ tokens=+$TOKENS, total=$NEW_USED"

  # 임계 체크
  bash scripts/check-token-threshold.sh "$NEW_USED"
fi

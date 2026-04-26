#!/usr/bin/env bash
# Discord 웹훅 알림 발송
# 사용법:
#   bash scripts/notify-discord.sh "<title>" "<message>" [<link-url-or-path>] [<link-label>]
#
# 예시:
#   bash scripts/notify-discord.sh "📥 신규 inbox" "Round X" \
#     "coordination/inbox/2026-04-15-X.md" "📂 inbox 보기"
#
# 자동 링크 첨부 (3가지 형태):
#   (a) 환경변수 DASHBOARD_LINKS=auto 면 매 알림 끝에 표준 링크 자동 첨부
#   (b) 3번째 인자로 path 전달 시 GitHub 웹 URL 변환
#   (c) 3번째 인자가 http(s):// 면 그대로 사용

set -euo pipefail

TITLE="${1:-알림}"
MESSAGE="${2:-내용 없음}"
LINK_INPUT="${3:-}"
LINK_LABEL="${4:-📎 보기}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# config.yml 로드 (PROJECT_ROOT / WORK_BRANCH / COORD_ROOT)
[[ -f "$SCRIPT_DIR/config.sh" ]] && source "$SCRIPT_DIR/config.sh" 2>/dev/null || true
WB="${WORK_BRANCH:-main}"

# v0.38+: config.sh 의 ENV_FILE_RESOLVED 사용 — .coord/.env.local 우선, PROJECT_ROOT/.env.local fallback.
# config.sh 미로드 상태(bot/worktree 외부 직접 호출 등) 대비 fallback 로직 포함.
if [[ -n "${ENV_FILE_RESOLVED:-}" ]]; then
  ENV_FILE="$ENV_FILE_RESOLVED"
else
  _FALLBACK_ROOT="${PROJECT_ROOT:-$SCRIPT_DIR/..}"
  if [[ -f "$_FALLBACK_ROOT/.coord/.env.local" ]]; then
    ENV_FILE="$_FALLBACK_ROOT/.coord/.env.local"
  else
    ENV_FILE="$_FALLBACK_ROOT/.env.local"
  fi
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ .env.local 없음: $ENV_FILE"
  exit 1
fi

WEBHOOK_URL=$(grep -E "^DISCORD_WEBHOOK_URL=" "$ENV_FILE" | head -1 | cut -d= -f2-)
GITHUB_REPO=$(grep -E "^GITHUB_REPO_URL=" "$ENV_FILE" | head -1 | cut -d= -f2- || echo "")
DASHBOARD_LINKS=$(grep -E "^DASHBOARD_LINKS=" "$ENV_FILE" | head -1 | cut -d= -f2- || echo "")

if [[ -z "$WEBHOOK_URL" ]]; then
  echo "❌ DISCORD_WEBHOOK_URL 설정 안됨"
  exit 1
fi

# === 링크 URL 변환 ===
# (a) http:// 또는 https:// 면 그대로
# (b) 그 외 path 면 GITHUB_REPO + branch + path 로 조합
LINK_URL=""
if [[ -n "$LINK_INPUT" ]]; then
  if [[ "$LINK_INPUT" =~ ^https?:// ]]; then
    LINK_URL="$LINK_INPUT"
  elif [[ -n "$GITHUB_REPO" ]]; then
    # path → GitHub web URL (work_branch 기본 — config.yml git.work_branch)
    BRANCH="${GITHUB_BRANCH:-$WB}"
    # /tree/ for dirs, /blob/ for files (heuristic: . in last segment = file)
    if [[ "$LINK_INPUT" == */ ]] || [[ ! "$LINK_INPUT" =~ \. ]]; then
      LINK_URL="${GITHUB_REPO%.git}/tree/${BRANCH}/${LINK_INPUT}"
    else
      LINK_URL="${GITHUB_REPO%.git}/blob/${BRANCH}/${LINK_INPUT}"
    fi
  fi
fi

# === 표준 대시보드 링크 ===
# 우선순위: live dashboard URL > GitHub URL
STANDARD_FIELDS_JSON="[]"

# 라이브 대시보드 URL (cloudflared / Tailscale / localhost) — coordination/ 안이므로 COORD_ROOT 기준
LIVE_URL=""
DASHBOARD_URL_FILE="${COORD_ROOT:-$SCRIPT_DIR/..}/coordination/dashboard-url.txt"
[[ -f "$DASHBOARD_URL_FILE" ]] && LIVE_URL=$(cat "$DASHBOARD_URL_FILE" | tr -d '\n\r ')

if [[ "$DASHBOARD_LINKS" == "auto" ]]; then
  REPO="${GITHUB_REPO%.git}"
  if [[ -n "$LIVE_URL" ]]; then
    # 라이브 대시보드 우선 — display text 와 URL 분리해야 Discord 가 hyperlink 렌더링
    STANDARD_FIELDS_JSON=$(cat <<EOF
[
  {"name": "📊 라이브 대시보드", "value": "**[👉 클릭해서 대시보드 열기]($LIVE_URL)**", "inline": false},
  {"name": "📥 inbox", "value": "[GitHub 보기](${REPO}/tree/${WB}/coordination/inbox)", "inline": true},
  {"name": "📝 review", "value": "[GitHub 보기](${REPO}/tree/wt/head/coordination/review-inbox)", "inline": true}
]
EOF
)
  elif [[ -n "$GITHUB_REPO" ]]; then
    # 라이브 URL 없으면 GitHub fallback
    STANDARD_FIELDS_JSON=$(cat <<EOF
[
  {"name": "📂 coordination", "value": "[GitHub](${REPO}/tree/${WB}/coordination)", "inline": true},
  {"name": "📥 inbox", "value": "[목록](${REPO}/tree/${WB}/coordination/inbox)", "inline": true},
  {"name": "📝 review (wt/head)", "value": "[목록](${REPO}/tree/wt/head/coordination/review-inbox)", "inline": true}
]
EOF
)
  fi
fi

# === Payload 생성 ===
TMP_JSON=$(mktemp)
trap 'rm -f "$TMP_JSON"' EXIT

PYTHON_CMD=""
if command -v python3 >/dev/null 2>&1; then PYTHON_CMD="python3"
elif command -v py >/dev/null 2>&1; then PYTHON_CMD="py -3"
fi

if [[ -n "$PYTHON_CMD" ]]; then
  $PYTHON_CMD -c "
import json, sys, re
title, message, link_url, link_label, std_fields_str = sys.argv[1:6]

embed = {
  'title': title,
  'description': message,
  'color': 3447003,
  'footer': {'text': '${PROJECT_NAME:-coord} coordination'}
}

# 단일 링크 (3번째 인자) 가 있으면 description 끝에 첨부
if link_url:
    embed['description'] = f'{message}\n\n**[{link_label}]({link_url})**'

# 표준 대시보드 fields (DASHBOARD_LINKS=auto 시)
fields = []
try:
    std_fields = json.loads(std_fields_str)
    if std_fields:
        fields.extend(std_fields)
except Exception:
    pass

# v0.30: 완료 관련 알림 (✅ / 완료 / done) 에 inbox id 가 있으면 @bot review 힌트 자동 추가
completion_keywords = ['완료', '✅', 'done', 'finish', 'complete']
is_completion = any(kw in title.lower() for kw in completion_keywords)
if is_completion:
    id_match = re.search(r'(?:^|[\s:])(\d{4}-\d{2}-\d{2}-\d{6})', message)
    if id_match:
        inbox_id = id_match.group(1)
        fields.append({
            'name': '💡 다음 단계',
            'value': f'\`@bot review {inbox_id}\` — verdict 확인 + 승인 리액션 (✅/❌)',
            'inline': False,
        })

if fields:
    embed['fields'] = fields

print(json.dumps({'embeds': [embed]}))
" "$TITLE" "$MESSAGE" "$LINK_URL" "$LINK_LABEL" "$STANDARD_FIELDS_JSON" > "$TMP_JSON"
else
  # fallback (Python 없을 때 — 단순)
  ET=$(printf '%s' "$TITLE" | sed 's/\\/\\\\/g; s/"/\\"/g')
  EM=$(printf '%s' "$MESSAGE" | sed 's/\\/\\\\/g; s/"/\\"/g' | awk '{printf "%s\\n", $0}' | sed 's/\\n$//')
  if [[ -n "$LINK_URL" ]]; then
    EM="${EM}\\n\\n**[${LINK_LABEL}](${LINK_URL})**"
  fi
  printf '{"embeds":[{"title":"%s","description":"%s","color":3447003,"footer":{"text":"%s coordination"}}]}' "$ET" "$EM" "${PROJECT_NAME:-coord}" > "$TMP_JSON"
fi

RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  --data "@$TMP_JSON" \
  "$WEBHOOK_URL")

if [[ "$RESPONSE" =~ ^2 ]]; then
  echo "✅ Discord 알림 발송 (HTTP $RESPONSE)"
else
  echo "❌ Discord 실패 (HTTP $RESPONSE)"
  echo "Payload:"
  cat "$TMP_JSON"
  exit 1
fi

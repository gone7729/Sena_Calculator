# coord Discord Bot — Phase 6 MVP

로컬 머신에서 24/7 실행하는 Discord 봇. 등록된 프로젝트로 `/inbox-send` 를 라우팅해 **어디서든 핸드폰/웹 Discord 에서 작업 지시**가 가능.

> v0.6 MVP 범위 — 단일/멀티 프로젝트 라우팅 + status/stop/projects 명령. 리액션 승인/retry/budget 은 후속 릴리스.

---

## 아키텍처 (MVP)

```
Discord 사용자 (@bot <text>)
        ↓ WebSocket
Discord Gateway
        ↓ 이벤트 push
controller.py (사용자 로컬 머신, 상시 구동)
        ↓ subprocess.Popen
claude -p "/inbox-send <text>"  (프로젝트 루트 cwd)
        ↓ 기존 coord 경로
head → sub → 완료
        ↓
head 가 기존 notify-discord.sh 로 Discord 에 결과 알림
```

**핵심 원칙**: 봇은 **라우터 + 상태 조회만**. inbox 처리/plan 생성/완료 알림은 **기존 coord 경로가 담당**. 봇이 중복 발송하지 않음.

---

## 사전 준비 (1회 세팅)

### 1. Discord Developer Portal
1. https://discord.com/developers/applications → "New Application"
2. 왼쪽 **Bot** 탭 → "Reset Token" → 토큰 복사 (한 번만 표시됨)
3. **Privileged Gateway Intents** 섹션:
   - ✅ **Message Content Intent** (필수 — 봇이 메시지 읽으려면 필요)
   - ⬜ Server Members Intent (개인 서버는 불필요)
4. 왼쪽 **OAuth2 → URL Generator**:
   - Scopes: `bot`
   - Bot Permissions: `Send Messages`, `Read Message History`, `Embed Links`, `Add Reactions`
     *(v0.8 리액션 승인/거부 기능엔 `Read Message History` + `Add Reactions` 필수. v0.8.1 Thread 에선 추가로 `Manage Threads`, `Create Public Threads` 권장)*
   - 생성된 URL 을 브라우저에서 열어 본인 Discord 서버에 초대

### 2. 본 repo 설정
```bash
# .env.local 에 토큰 기입 (gitignored)
echo "DISCORD_BOT_TOKEN=여기에_실제토큰" >> .env.local

# 레지스트리 준비
cp bot/projects.yml.example bot/projects.yml
$EDITOR bot/projects.yml
# - bot.admin_user_ids: 본인 Discord user id 기입
#   (Discord Settings → Advanced → Developer Mode 활성 → 유저 우클릭 → Copy User ID)
# - projects[].path: 실제 프로젝트 절대 경로
# - projects[].enabled: true

# Python 의존성
pip install -r bot/requirements.txt    # 또는 uv pip install
```

### 3. 시험 실행
```bash
bash scripts/start-bot.sh              # foreground
# 또는
bash scripts/start-bot.sh --bg         # nohup background, 로그 /tmp/coord-bot-*.log
```

처음 연결되면 터미널에 다음 출력:
```
✅ Logged in as YourBot#1234 (id: ...)
   Projects: ['my-project']
   Admins:   ['123456789012345678']
   Channels: (any mention)
```

---

## 명령

전부 **봇을 @mention** 하면서 사용. `@bot help` 로 목록 확인 가능.

### 작업 지시 (v0.11+)
| 명령 | 동작 |
|------|------|
| `@bot send <text>` | 현재 채널 바인딩 프로젝트로 `/inbox-send` |
| `@bot send <project-id> <text>` | 명시적 프로젝트 지정 |
| `@bot send <text>` + 📎 이미지 첨부 | v0.17: 이미지 자동 다운로드 → `coordination/inbox/attachments/<ts>-<name>`, inbox 본문에 `## 첨부` 섹션 자동 추가 |
| `@bot review [inbox-id]` | v0.21: `/review-inbox --verdict-only` 완료 대기 + **verdict prompt embed 자동 표시** (✅/❌ 리액션으로 머지/보류) — id 생략 시 최근 미review 자동 선택 |
| `@bot fix [description]` | `/fix` — description 생략 시 **review-inbox 의 needs-fix 항목 자동 탐색** |
| `@bot main-merge [project-id]` | v0.27: `work_branch` → `stable_branch` (보통 3dview → main). **dry-run 으로 충돌 사전 감지**, 안전하면 실제 머지 + push + embed. 충돌 시 파일 목록 + 수동 해결 가이드 + stable 안 건드림 |
| `@bot resolve <id> [reason]` | `/resolve-escalation` — 리액션 ✅ 대체 텍스트 버전 |
| `@bot reject <id> [reason]` | `/reject-escalation` — 리액션 ❌ 대체 텍스트 버전 |

**`@bot <text>` (send 없이)** — v0.12+ 자연어 대화 예약. 현재는 안내 메시지만 응답.

### 조회
| 명령 | 동작 |
|------|------|
| `@bot projects` | 등록 목록 + path 존재 확인 |
| `@bot active` | v0.23: **전체 프로젝트 head 작업 상태 + 경과 시간** (채널 필터 무시) — "지금 어디서 뭐 돌고 있지?" 한눈에 |
| `@bot status [project-id]` | v0.23: HEAD_LOCK (head worktree 기준) + 경과 시간 + pending inbox. id 생략 시 현재 채널 필터 적용 |
| `@bot queue [project-id]` | in-memory 큐 상태 (v0.10+) |

### 제어
| 명령 | 동작 |
|------|------|
| `@bot stop <project-id>` | `coordination/STOP` 파일 작성 (커밋/푸시는 사용자 수동) |
| `@bot retry <inbox-id> [project-id]` | head worktree 에 `/retry <inbox-id>` spawn (실패 plan 재시도) |
| `@bot budget [project-id]` | 프로젝트 토큰 예산 Embed (progress bar + used/limit/주간 리셋) — 수동 집계 |
| `@bot usage [days=7]` | **Claude CLI 일별 비용** — `ccusage daily` 로컬 JSONL 파싱 (사용자 전체 집계, 프로젝트 무관) |
| `@bot blocks` | **현재 5시간 과금 윈도우** — `ccusage blocks --active` 기반. 사용량 · burn rate · 이 속도 유지 시 투영 · 남은 시간 (v0.33+) |

### 채널 분리 (v0.11+): 대화방 / 작업장
한 Discord 서버 안에 2 텍스트 채널로 역할 분리 가능:

- **#대화방** (`discord_channel_id`) — 봇 @mention 받는 채널. `@bot send`, `@bot review`, `@bot status` 등
- **#작업장** (`discord_alert_channel_id`) — webhook 알림 + inbox thread 가 모이는 채널

설정 예:
```yaml
projects:
  - id: dealos
    path: c:/Users/you/projects/dealos
    discord_channel_id: "11111..."         # #대화방
    discord_alert_channel_id: "22222..."   # #작업장
```

+ `.env.local` 의 `DISCORD_WEBHOOK_URL` 을 #작업장 채널 webhook URL 로 설정:
- head 완료 알림이 #작업장 에 들어옴
- 봇 spawn thread 도 #작업장 에 생성됨
- 리액션 ✅/❌ 도 #작업장 메시지에서 정상 작동 (봇이 alert_channel 까지 _resolve_project 함)

둘 중 하나만 바인딩하는 것도 OK (기존 v0.10 동작 그대로).

### inbox 큐 (v0.10+)
`@bot <text>` 는 전부 **프로젝트별 asyncio.Queue** 경유. HEAD_LOCK 직렬화를 봇이 투명하게 관리:
- 큐 비어있고 HEAD_LOCK 없음 → 즉시 spawn
- HEAD_LOCK 잡혀있거나 대기 중 → "⏳ N번째" 응답, worker 가 자동 대기 후 spawn
- Worker 는 프로젝트 당 1개 (필요 시 asyncio.create_task), 10분 idle → 자동 종료
- HEAD_LOCK 30분 대기 후에도 안 풀리면 그 job 만 skip (다음 job 으로 진행)

**장점**: 같은 프로젝트에 `@bot <text1>`, `@bot <text2>` 연속 입력해도 "busy" 응답 없이 자동 순서 처리. 새 thread 는 실제 spawn 시점에 생성.

**상태 확인**: `@bot queue` — 전체, `@bot queue <project-id>` — 특정 프로젝트.

**제약**:
- **in-memory** — 봇 재시작 시 큐 손실 (pending job 재전송 필요). 영속화는 후속 후보.
- 큐 크기 상한 없음 — 실수로 100개 연속 보내도 다 쌓임 (spam 보호는 admin_user_ids 에 의존).

### 자동 Orchestration (v0.12+, v0.13 self-review + v0.14 플래그 + v0.15/0.16.7 마일스톤)

`@bot send <text>` 한 번으로 전체 흐름 자동:

```
1. @bot send X                  → 봇이 /inbox-send spawn + thread 생성
2. ⏱ head 세션 스폰 대기 중…     (v0.15)
3. 📍 head 시작 감지             (HEAD_LOCK 생성 감지, v0.15)
4. 🕐 head 작업 중 (N분 경과)    (v0.16.7: 10/30/60분 마일스톤, 최대 3회)
   → head spawn prompt 에 "완료 후 /review-inbox --verdict-only 자동 실행" 지시 포함 (v0.13)
5. ✅ head 완료 감지             (HEAD_LOCK 해제, v0.15)
6. 봇이 review-inbox 파일에서 verdict 조회:
   - v0.13 self-review 성공 시 → verdict 이미 기록됨 → Phase D spawn 생략 (✨ "head self-review 감지")
   - self-review 없으면 → fallback 으로 /review-inbox <id> --verdict-only 자동 spawn
7. verdict 읽어서 Discord 에 리액션 prompt:
   - go       → "머지 진행? ✅ / 보류 ❌"
   - needs-fix → "fix 진행? ✅ / 무시 ❌"
   - block    → 경고만 (자동 action 없음)
8. 사용자 리액션:
   - go + ✅    → /review-inbox <id> --merge-only --auto-yes spawn (v0.14)
   - needs-fix + ✅ → /fix 자동 spawn (review-inbox 의 needs-fix 항목 자동 탐색)
   - ❌         → "보류" 메시지 + 수동 처리 안내
```

**v0.15 실시간 heartbeat + v0.16.7 마일스톤**: 이전엔 `@bot send` 직후 spawn 응답 → (30분 침묵) → 완료 verdict prompt 만 떴다. 사용자가 "봇이 죽었나?" 헷갈림. v0.15 에서 각 phase 전환 (⏱/📍/🕐/✅) 채널 노출 추가.

v0.16.7 부터는 🕐 heartbeat 가 **주기 반복이 아닌 마일스톤 방식** — 10분/30분/60분 경과 시 딱 1회씩. 짧은 작업엔 0개, 긴 작업 (1시간) 도 총 3개. 과빈도 피드백 방지.

튜닝: `_V016_7_HEARTBEAT_MILESTONES_MIN = [10, 30, 60]` 상수 수정. 더 자주 원하면 `[5, 10, 20, 30, 45, 60]` 식으로 확장, 더 조용히 원하면 `[30, 60]`.

**v0.13 변경 (head self-review)**: head 가 inbox 처리 직후 같은 세션에서 `/review-inbox <id> --verdict-only` 를 로컬 수행. head 컨텍스트엔 plan/reports/diff 가 이미 로드돼 있어 재진입보다 **훨씬 저렴**. 봇은 verdict 가 이미 있으면 Phase D spawn 을 완전 생략. fallback 경로는 유지 (head 가 self-review 실패/스킵했을 때).

head spawn prompt 주입은 `.claude/commands/inbox-send.md` 및 `resolve-escalation.md` 에서 자동 처리 (템플릿이 소비 프로젝트의 `/inbox` 슬래시 명령 본문을 모르더라도 작동).

**v0.14 변경**: 이전(v0.12~v0.13)까진 "Step 1~6 만 수행 / Step 7 만 수행" 같은 **prompt 본문 지시** 로 phase 를 분리했다. v0.14 에서 `review-inbox.md` 에 공식 플래그 3종 (`--verdict-only`, `--merge-only`, `--auto-yes`) 을 도입해 **구조화된 호출**로 대체. Claude 가 지시 본문 해석에 실패해도 플래그가 명령 스펙에 박혀 있으므로 견고.

**타임아웃**:
- head 시작 대기 3분 (HEAD_LOCK 생김 안 하면 skip)
- head 완료 대기 60분 (너무 오래면 skip)
- review spawn 10분

**HEAD_LOCK 경로** (v0.12 에서 수정): `<parent>/<project>-wt-head/coordination/HEAD_LOCK`
이전 버전은 project root 의 HEAD_LOCK 을 봐서 실제 detect 못 하고 있었음 — v0.12 에서 올바른 경로로.

### Option C — @bot <text> 자연어 대화 (v0.16+, 라우팅은 v0.16.1)

어느 채널에서든 `@bot <자연어 질문>` 을 보내면 Anthropic SDK 직결로 응답. 응답은 항상 **대화방** (`discord_channel_id`) 으로 라우팅돼서 작업장/thread 가 어지러워지지 않음. coord 상태를 context 에 번들해 **프로젝트 맥락을 아는** 어시스턴트로 동작.

**예시**:
```
user: @bot 지금 pending inbox 몇 개야?
bot:  대화방 프로젝트 dealos:
      - HEAD_LOCK: ⚪ idle
      - pending inbox: 0 건
      - 가장 최근 처리: 2026-04-17-180512 (verdict: go, merged)
      추가 작업 지시는 `@bot send <내용>` 으로 하면 됩니다.

user: @bot 이 v0.15 릴리스 요약해줘
bot:  (Claude 가 STATUS.md / 최근 review-inbox 를 기반으로 요약)
```

**동작 조건**:
- `.env.local` 에 `ANTHROPIC_API_KEY` 설정 (https://console.anthropic.com/ 에서 발급)
- `projects.yml` 의 `bot.chat.enabled: true` (기본 true)
- 프로젝트 바인딩된 채널 또는 default_project 로 프로젝트 식별 가능해야 함

**채널 라우팅 (v0.16.1)**:
- 대화방에서 질의 → 대화방에 답 (자연스러운 대화)
- 작업장/다른 채널에서 질의 → 원래 채널엔 `💬 → <#대화방> 에서 응답합니다…` 한 줄, 실제 답은 대화방으로
- 대화방 미설정 시 → 원래 채널에 답 (fallback, 채널 분리 안 쓰는 단순 구성)
- thread 내부 질의도 parent 채널 기준으로 판정 후 대화방 라우팅

**context 번들** (system prompt 에 자동 포함):
- HEAD_LOCK 상태 (head 작업 중 / idle)
- 최근 inbox N건 제목 + status (기본 3건, `include_recent_inbox` 로 조정)
- `coordination/STATUS.md` 내용 (4KB truncated, `include_status_md` 로 on/off)
- 최근 review-inbox 2건의 verdict + merged 여부
- 시스템 규칙 (3-tier, coord 규약)

**비용 관리**:
- 기본 모델 `claude-sonnet-4-6` (저렴)
- `max_tokens: 1024` 제한 (Discord 2000자 제한 대응)
- system prompt 의 static 부분은 **prompt caching** 사용 (cache_control ephemeral) — 5분 내 재질의 시 80~90% 할인
- 응답 footer 에 사용량 표시: `sonnet-4-6 · in:1200 out:340 cache_r:800 cache_w:400`

**설정 예시 (`projects.yml`)**:
```yaml
bot:
  chat:
    enabled: true
    model: "claude-sonnet-4-6"       # or claude-opus-4-7 / claude-haiku-4-5
    max_tokens: 1024
    include_status_md: true
    include_recent_inbox: 3
```

**비활성화 시나리오**:
- `ANTHROPIC_API_KEY` 없음 → 자동 off, fallback 안내
- `chat.enabled: false` → off
- 작업장 채널 → off (의도적 분리)
- 프로젝트 바인딩 없음 → off

### review-inbox 플래그 직접 사용 (v0.14+)

자동 orchestration 외에도 수동 호출 시 플래그 사용 가능:

```bash
/review-inbox <id>                          # 기존 전체 흐름 (하위 호환)
/review-inbox <id> --verdict-only           # 검증 + verdict 판정까지만 (머지 금지)
/review-inbox <id> --merge-only             # verdict: go 확인 후 머지만 (사용자 확인은 물음)
/review-inbox <id> --merge-only --auto-yes  # 머지 + 최종 확인 자동 yes
/review-inbox <id> --auto-yes               # 전체 흐름 + 최종 머지 확인만 자동 yes
```

사용 사례:
- **CI 처럼 운영** — `@bot send` → 자동 `--verdict-only` → 사람이 Discord 에서 verdict 확인 → ✅ 로 `--merge-only --auto-yes`
- **로컬 테스트** — `/review-inbox <id> --verdict-only` 로 먼저 판정만 보고, 필요하면 별도로 `/review-inbox <id> --merge-only` 실행
- **긴급 머지 (아직 안 추천)** — 이미 verdict: go 판정된 엔트리를 CI/자동화 파이프라인에서 `--merge-only --auto-yes` 로 즉시 머지

### 프로젝트-측 `bot.yml` (v0.22+)

`@bot register` / `@bot bind` / `@bot unbind` 실행 시 프로젝트의 `coordination/bot.yml` 에도 채널 바인딩 기록을 **자동 동기화**:

```yaml
# <project>/coordination/bot.yml  (자동 생성/갱신)
discord_channel_id: "1234567890123456789"
discord_alert_channel_id: "9876543210987654321"   # (현재는 수동 편집만)
updated_at: "2026-04-21 14:30 KST"
```

**용도**:
- **이식성**: 프로젝트 git clone 시 바인딩 정보 따라옴 → 다른 사람도 채널 id 확인 가능
- **명시성**: 프로젝트 파일만 봐도 "이 프로젝트 어느 채널에 바인딩됐는지" 확인
- **복구**: 중앙 `bot/projects.yml` 손상 시 프로젝트별 bot.yml 을 참고해 재구성

**source of truth**:
- **런타임**: 중앙 `bot/projects.yml` (봇이 시작 시 로드)
- **프로젝트 사본**: `coordination/bot.yml` (쓰기 자동 동기화, v0.22 시점엔 읽기 연동 없음 — v0.23 후보)

**upgrade 보호**: classify.sh 가 `coordination/bot.yml` 을 `preserve` 분류 → `upgrade-coord.sh` 실행 시 덮어써지지 않음.

**.gitignore 에 포함 여부**: 기본 **git 에 커밋** (이식성 목적). 민감 정보 아님 (채널 id 는 서버 멤버면 볼 수 있는 정보).

### Thread 격리 (v0.9+)
`@bot <text>` spawn 성공 시 봇 응답 메시지에서 **inbox-\<project\>-\<ts\>** 이름의 Discord Thread 자동 생성. 해당 inbox 의 진행/대화를 thread 안에 격리.
- Thread 내부에서 `@bot <text>` 보내도 parent 채널의 프로젝트 바인딩 상속 → 정상 라우팅
- `@bot bind/unbind/status/retry/budget/usage` 모두 thread 에서 실행 가능 (parent 채널 기준으로 인식)
- Auto-archive: 1440분 (24h)
- 봇 권한 중 **Create Public Threads** 필요 (권한 없으면 thread 생성 조용히 skip — spawn 은 성공)

### 사용량 자동 경고 (v0.9+)
`bot/projects.yml` 에 `bot.usage_alerts` 추가 시 봇이 주기적으로 `ccusage` 실행 → 임계 초과하면 지정 채널에 자동 알림:
```yaml
bot:
  usage_alerts:
    enabled: true
    check_interval_minutes: 60
    alert_channel_id: "123456789012345678"
    daily_usd_threshold: 10.0
    monthly_usd_threshold: 100.0
```
- 체크 시작은 `on_ready` 이후 (봇 시작 약 5초 뒤 첫 체크)
- **하루/한달 각 1회만** 알림 (state: `bot/usage-alerts-state.json`, gitignored)
- 임계 0 이면 해당 체크 비활성 (daily 만 쓸 수도, monthly 만 쓸 수도)

### 에스컬레이션 리액션 (v0.8+)
head 가 사법부 거부 시 발송하는 `🔔 head 판사 거부 — 승인 요청` 메시지에 **리액션**:
- **✅** → `/resolve-escalation <id>` 자동 spawn (승인)
- **❌** → `/reject-escalation <id>` 자동 spawn (거부)

*`admin_user_ids` 에 등록된 사용자의 리액션만 처리. 메시지 내 `escalation-id: <id>` 또는 `escalations/...-<id>.md` 패턴을 자동 파싱. 이 채널이 어떤 프로젝트에 바인딩됐는지에 따라 라우팅.*

### budget vs usage 구분
| | `@bot budget` | `@bot usage` |
|---|---|---|
| 데이터 소스 | 프로젝트 `coordination/token-budget.md` (수동 집계) | `~/.claude/projects/*.jsonl` (Claude CLI 실 데이터) |
| 단위 | 프로젝트별 | Claude CLI 사용자 전체 |
| 정확도 | 휴리스틱 (`claude -p` spawn 만 추적) | 정확 (모든 대화 포함) |
| 비용 표시 | 토큰만 | 토큰 + **실 USD 비용** + 모델별 breakdown |
| 의존성 | coord 내장 | `ccusage` npm 패키지 (npx 로 auto-fetch) |

둘을 **함께 쓰는 걸 권장** — `budget` 은 프로젝트 한계 관리, `usage` 는 전체 실 지출 추적.

### 레지스트리 관리 (v0.7+)
| 명령 | 동작 |
|------|------|
| `@bot register <path>` | v0.20: **config.yml 의 `project.name` 자동 추출** + 현재 채널 바인딩 + projects.yml 저장 + **프로젝트 `coordination/bot.yml` 도 생성 (v0.22)** |
| `@bot register <id> <path>` | 수동 id 로 등록 (`config.yml` 미사용 / 여러 별칭 필요 시) |
| `@bot unregister <id>` | 레지스트리 제거 (코드/데이터는 삭제 안 됨) |
| `@bot bind <id>` | 프로젝트를 현재 채널에 (재)바인딩 + **bot.yml 갱신 (v0.22)** |
| `@bot unbind <id>` | 채널 바인딩 해제 + **bot.yml 채널 필드 제거 (v0.22)** |
| `@bot bind <id>` | 이미 등록된 프로젝트를 현재 채널에 (재)바인딩 |
| `@bot unbind <id>` | 채널 바인딩 해제 (프로젝트는 레지스트리에 유지) |
| `@bot reload` | projects.yml 수동 편집 후 재읽기 (봇 재시작 없이) |

**라우팅 우선순위**:
1. 명시적 `@bot <project-id> <text>` → 그 프로젝트
2. 현재 채널에 바인딩된 프로젝트 있으면 → 자동 그 프로젝트
3. 둘 다 아니면 → `default_project`

---

## 신규 프로젝트 등록 흐름 (v0.7+)

새 프로젝트를 봇에 연결하려면 **두 단계**만:

### Phase 1: 프로젝트에 coord 설치 (터미널, 1회)
```bash
cd /path/to/new-project
# 옵션 A: flat 설치
git clone https://github.com/gone7729/coord-template ._tmp && \
  cp -r ._tmp/{coordination,scripts,.claude,.githooks,bot,templates,docs} . && \
  rm -rf ._tmp && \
  bash scripts/init.sh

# 옵션 B: subtree 설치 (권장)
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git v0.7 --squash
bash .coord/scripts/init.sh
```

### Phase 2: Discord 에서 봇에 등록
- 새 채널 생성 (또는 기존 채널)
- 그 채널에서:
  ```
  @coord-bot register /path/to/new-project      # v0.20: id 자동 감지
  @coord-bot register myapp /path/to/new-project  # 또는 수동 id
  ```
- 봇이:
  1. 경로 존재 + coord 설치 여부 확인 (flat 또는 subdir 모두 자동 감지)
  2. `bot/projects.yml` 에 엔트리 추가 + 현재 채널 ID 자동 바인딩
  3. in-memory 레지스트리 갱신 (재시작 불필요)
- 이제 그 채널에서 `@coord-bot <text>` 만 쳐도 myapp 으로 자동 라우팅

**주의**: 봇 명령으로 `projects.yml` 을 저장하면 **YAML 주석이 사라짐** (PyYAML 제약). 주석 유지하려면:
- 수동으로 `bot/projects.yml` 편집 + `@bot reload` 명령
- 또는 봇 명령 사용 후 주석 재추가

---

## 보안 / 권한 모델

- **admin_user_ids 비우면 전원 허용** — 반드시 비공개 서버에서만 비우기. 공개 서버는 필수 채움.
- **allowed_channel_ids** — 지정 시 해당 채널에서만 반응. 비우면 @mention 한 모든 채널.
- **discord_channel_id** (projects 별) — 프로젝트를 특정 채널에 묶음. 다른 채널에서 해당 프로젝트 조작 불가.
- **Bot 토큰 유출 시**: Developer Portal → Bot → Reset Token. 기존 토큰은 즉시 무효화됨.
- **spawn 은 shell=False** — 사용자 입력이 셸 인자로 직접 해석되지 않음.

---

## HEAD_LOCK / 동시성

- 봇이 spawn 전에 `<project>/coordination/HEAD_LOCK` 존재 확인.
- 이미 실행 중이면 "⚠ 이미 실행 중" 응답 후 spawn 하지 않음 → **중복 spawn 방지**.
- 현재 head 세션이 끝나면(또는 크래시되어 락 잔존 시 사용자가 수동 해제하면) 다음 요청 spawn 가능.

---

## 완료 알림

봇 자체는 완료 알림을 **발송하지 않음**. 기존 경로 유지:
- head 가 처리 완료 시점에 `scripts/notify-discord.sh` (또는 `scripts/notify-step.sh`) 호출
- **→ Discord webhook** 으로 결과 embed 전송
- 봇이 **subprocess 종료를 기다리지 않기** 때문에 장기 실행(수십 분~1시간)도 문제없음

즉 **Discord 채널에 두 메시지** 가 흐름상 도착:
1. 봇: "✅ 시작 — log: bot-<project>-xxx.log"
2. Webhook (head): "🎉 완료 — inbox: ...,  3개 sub pass"

---

## 로그 / 디버깅

- **봇 로그**: `/tmp/coord-bot-YYYYMMDD-HHMMSS.log` (— `--bg` 실행 시)
- **subprocess 로그**: `/tmp/bot-<project>-<ts>.log`
- **환경변수 `COORD_BOT_LOG_DIR`** 로 로그 디렉토리 변경 가능

일반 오류:
| 증상 | 원인 | 해결 |
|------|------|------|
| `❌ DISCORD_BOT_TOKEN 환경변수 없음` | .env.local 미로드 | `source .env.local` 후 재시작 또는 `start-bot.sh` 사용 |
| `❌ Discord 로그인 실패` | 토큰 잘못됨/만료 | Developer Portal 에서 Reset Token |
| `privileged intents required` | Message Content Intent 미활성 | Developer Portal 토글 ON |
| 봇이 메시지 읽지 못함 | 채널 권한 부족 | Bot 역할에 Read Message History / Send Messages 부여 |
| `claude CLI 찾을 수 없음` | PATH 문제 | `which claude` 확인, 봇 실행 환경 PATH 점검 |

---

## 24/7 호스팅

로컬 PC 가 MVP 로 충분. 장기 운영 시:
- **Raspberry Pi**: 초기 ~$50, 월 전기료 ~$2
- **Oracle Cloud Free Tier**: ARM 인스턴스 영구 무료 (약관 변동 주의)
- **Hetzner / DigitalOcean**: 월 $4~6

systemd unit 예시 (Linux 상시 실행):
```ini
# /etc/systemd/system/coord-bot.service
[Unit]
Description=coord Discord Bot
After=network.target

[Service]
Type=simple
User=<your-user>
WorkingDirectory=/home/<your-user>/projects/coord-template
EnvironmentFile=/home/<your-user>/projects/coord-template/.env.local
ExecStart=/usr/bin/python3 bot/controller.py
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable --now coord-bot
journalctl -u coord-bot -f
```

---

## 후속 릴리스 (미포함)

- **리액션 승인/거부** — `/resolve-escalation` / `/reject-escalation` 를 ✅ / ❌ 리액션으로
- **`@bot retry <inbox-id>`** — 실패한 plan 재시도
- **`@bot budget`** — 토큰 사용량 요약 (전체/프로젝트별)
- **Thread 기반 컨텍스트** — 각 inbox 를 Discord thread 로 격리
- **완료 알림 통합** — head 의 webhook 과 봇 메시지를 같은 embed 로

---

## 관련 파일

- [bot/controller.py](controller.py) — 메인 엔트리
- [bot/projects.yml.example](projects.yml.example) — 레지스트리 포맷
- [scripts/start-bot.sh](../scripts/start-bot.sh) — 실행 스크립트
- [coordination/ROADMAP.md Phase 6](../coordination/ROADMAP.md) — 전체 설계

# coord-template — 개발 규약

이 repo 는 **coord-template 유지보수 전용 세션**용 지침이다.
원본 소비 프로젝트(DEALOS 등)에서 검증된 패턴을 일반화·추출해 여기에 반영한다.

- **현재 버전**: v0.47 (2026-04-26) — sub spawn 패턴을 background + poll 로 재설계. head 의 Bash 도구 자체가 max 10분 (Anthropic 플랫폼 default) cap 이라 sub 가 30분 작업해야 하면 head 가 dispatch 자체를 거부하던 문제 해결. `scripts/spawn-sub-bg.sh` (즉시 return) + `scripts/poll-sub.sh` (1회 status check) 신설. inbox.md 4-4(f) 와 dispatch.md sub 호출 부분이 이 패턴 사용. 각 head Bash call 이 1초 안에 끝나서 cap 무관 — sub 는 shell `timeout 1800` 까지 자유롭게 실행
- **v0.46** — 머지 플로우 전반 정비. `.gitattributes merge=ours` 자동 등록. `@bot main-merge --drop-coord` + 자동 back-merge 체인. `cmd_back_merge` 신설
- **v0.45** — P0 버그 수정 + 알림 중복 제거. Phase A timeout `180s → 900s`. HEAD_LOCK atomic acquire. 머지 완료 중복 알림 제거
- **v0.44** — v0.43 의 범위 확장. `cmd_stop` 롤백 로직의 추가 2곳 (`coord_sub = base / "coordination"`, inbox status 수정) 도 subdir 모드에서 미매칭되던 것 수정
- **v0.43** — `@bot review` verdict 읽기 실패 수정. `_head_coord_dir()` 헬퍼 신설해 head worktree 경로 6군데 subdir 모드 대응
- **v0.42** — head-side slash-commands 4종 (`inbox` / `dispatch` / `plan` / `retry`) 정식 편입. pre-commit regex + classify.sh 에 subdir 경로 지원. CLAUDE.md §3.1.1 "추출 완전성 체크리스트"
- **v0.41** — v0.37 `.coord/` 통째 gitignore 반전. workflow 가 `.coord/coordination/*` git sync 에 의존하므로 runtime + secrets 만 개별 ignore, 배포 청결은 `.gitattributes export-ignore` 로 분리
- **v0.40** — v0.39 hotfix. `((var++))` + `set -euo pipefail` 충돌 수정. `--restore-configs` 에 `coordination/hotfixes.md` + `token-budget.md` 복원 추가
- **v0.39** — v0.38 `.coord/.env.local` 우선 전략 반전. `PROJECT_ROOT/.env.local` primary, `.coord/.env.local` 은 opt-in isolation. `link-env-to-coord.sh` (cp fallback 금지). `init.sh --restore-configs` 추가. root wrapper (`./coord <script>`) 선택 생성
- **v0.38** — `.env.local` 을 `.coord/.env.local` 로 이동 가능하게 했으나 Windows cp 문제로 v0.39 에서 반전
- **v0.37** — subdir 모드를 **기본/권장**으로 승격. coord 는 사용자 작업 utility 이므로 배포 브랜치 비노출 원칙 확립 (방안 A). `init.sh` + `upgrade-coord.sh` 가 subdir 모드에서 `.coord/` / `.claude/` 전체를 gitignore 자동 등록. `.claude/` cp → symlink
- **v0.36** — Opus 4.6 → 4.7 승급. 활성 config 6개 파일 일괄 교체. CLAUDE.md §5 "모델 승급 절차" 추가
- **v0.35** — worktree `.env.local` symlink 화. `setup-worktree.sh` 가 cp 대신 `ln -s`(실패 시 cp fallback). 기존 설치본 마이그레이션용 `relink-worktree-env.sh` 신설
- **v0.34** — 의존성 자동 설치 스크립트 (`install-deps-mac.sh` + `install-deps-windows.ps1`). Homebrew/winget 기반, idempotent. Mac/Windows 신규 환경 onboarding 원샷
- **v0.33** — `@bot blocks` 추가. Claude 5시간 과금 윈도우 실시간 조회 (`ccusage blocks --active`). 현재 윈도우의 사용량 · burn rate · 투영 · 남은 시간. Max 구독 rate limit 페이스 판단용 — 일별 통계(`@bot usage`)는 사후, blocks 는 실시간
- **v0.32** — `📍 head 시작 감지` 메시지 제거 + `@bot stop` 전체 롤백 재설계 (HEAD_LOCK clear 대기 + plan/tasks/reports/pending-review 삭제 + inbox status:cancelled + 각 worktree 브랜치 failed/<name>-<ts> 로 이동 + work_branch 로 재생성). "작업 전 버전으로 복귀" 철학
- **완료 트랙 (2026-04-17 ~ 2026-04-20)**:
  - v0.5 — upgrade-coord.sh + `.coord/` subdir + notify-step
  - v0.5.1 — DEALOS runtime 하드코딩 제거
  - v0.6 ~ v0.10.3 — Discord Bot MVP (채널 라우팅 / 레지스트리 / 리액션 / retry / budget / usage / Thread / 큐)
  - v0.11 — 명시적 명령 체계 + 채널 분리 (대화방/작업장)
  - v0.12 — 자동 orchestration (head 완료 감지 → review → verdict 리액션 prompt → 머지/fix)
  - v0.14 — `/review-inbox` 에 `--verdict-only` / `--merge-only` / `--auto-yes` 플래그 도입, 봇의 prompt hack 제거
  - v0.13 — head self-review (inbox 처리 직후 자동 `/review-inbox --verdict-only`) → 봇 Phase D 생략
  - v0.15 — orchestration 각 phase 전환 (⏱/📍/🕐/✅) thread 실시간 노출 + 5분 간격 heartbeat
  - v0.16 — Option C: 대화방에서 `@bot <text>` → Anthropic SDK (Sonnet 4.6) + prompt caching + 프로젝트 context 번들
  - v0.16.1 — Option C 응답 라우팅: 어느 채널에서 질의해도 답은 대화방(discord_channel_id)으로, 원래 채널엔 간단 꼬리표만. 작업장 깨끗하게 유지
- **버전 순서 주의**: v0.12 → v0.14 → v0.13 → v0.15 → v0.16 릴리스됨 (로드맵 label 순서 유지)
- **다음 후보**:
  - **v0.17 (후보)** — 대화 memory (thread/채널 단위로 직전 N 턴 유지 → multi-turn 질의)
  - **v0.18 (후보)** — tool-use (봇이 파일 read/grep 등 직접 수행, 현재는 system prompt 에 바닐라 dump)
  - **v1.0** — DEALOS 외 제3 프로젝트 실사용 검증 후 태깅
  - 상세: [ROADMAP.md](coordination/ROADMAP.md)

---

## 1. PR 원칙

### 1.1 브랜치 / 커밋
- `main` 직접 커밋 허용 범위: 문서 오탈자, 1~2줄 수정, 명백한 버그.
- 그 외 중·대형 변경은 `feat/<topic>` 또는 `chore/<topic>` 에서 작업 후 PR.
- 커밋 메시지 제목은 한국어, Phase 번호 prefix 부착: `feat(phase 5-7): sub worktree 일괄 자동 생성`.
- Claude 협업 커밋은 `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` 필수. (v0.36 부터 4.7 — 구버전 commit 은 그대로 둠)

### 1.2 변경 범위 규칙
- `scripts/*.sh` — 외부 의존(config.sh / .env.local) 을 **직접 접근 대신 헬퍼 경유**. 새 스크립트도 `source scripts/config.sh` 패턴 준수.
- `bot/*.py` — discord.py 기반 봇 (Phase 6). 수정 시 `bot/README.md` 명령 표 + help embed 동기화. projects.yml 스키마 확장 시 `projects.yml.example` 도 갱신.
- `coordination/*.md` — 문서 상단에 "템플릿 사용자 주의" 배너 유지 (DEALOS 기준이면 명시).
- DEALOS 전용 예시는 `_examples/` 폴더에만. 일반 경로에는 추상 설명만.
- `.claude/settings.local.json` 은 gitignore 대상 — 팀 공유 권한은 `settings.json` 쪽으로.
- `.env.local.example` 에 **실제 토큰/비밀 절대 금지** — placeholder 만. 실 값은 gitignored `.env.local` 에.

### 1.3 버전 / 릴리스
- SemVer 유사: `v0.X` 는 pre-1.0 자유 이동, `v1.0` 은 Phase 5 템플릿화 안정 후.
- **릴리스 노트**: 단일 `CHANGELOG.md` (tracked) 에 최신이 위로 append.
  - 개별 `RELEASE_NOTES_v*.md` 파일 **생성 금지** (v0.31+ 규칙)
  - 새 버전 노트는 CHANGELOG.md 맨 위에 추가
- 릴리스 절차:
  ```bash
  # 1. CHANGELOG.md 에 새 버전 섹션 최상단 append
  # 2. 커밋 + tag + push
  git commit -am "feat(vX.Y): ..."
  git tag vX.Y && git push origin main vX.Y
  # 3. gh release — CHANGELOG 에서 해당 섹션 추출해서 --notes 인자로
  SECTION=$(awk '/<!-- ===== vX.Y ===== -->/,/<!-- ===== v[0-9]/' CHANGELOG.md | sed '$d')
  gh release create vX.Y --title "vX.Y — <one-liner>" --notes "$SECTION"
  # 또는 CHANGELOG 링크만 짧게:
  gh release create vX.Y --title "vX.Y — <one-liner>" \
    --notes "상세: [CHANGELOG.md](../blob/main/CHANGELOG.md#-vxy)"
  ```
- 릴리스 직후 반드시 §3.4 체크리스트로 DEALOS 동기화 시도.

---

## 2. 테스트 정책

이 repo 는 bash / yaml / markdown / python(dashboard) 혼합이라 전통적 단위테스트가 없다. 3단 검증으로 대체한다.

### 2.1 정적 검증 (PR 전 로컬, 권장)
- `bash -n scripts/*.sh` — 구문 오류 체크.
- `shellcheck scripts/*.sh` — 설치돼 있을 때.
- `python -c "import yaml; yaml.safe_load(open('coordination/config.yml.example'))"` — YAML 파싱.
- `python -m py_compile scripts/dashboard.py` — dashboard 변경 시.

**bash 관용 주의 (v0.40 사고 후)**:
- `set -euo pipefail` 하에서 `((var++))` 금지 — post-increment 이전값 0 반환 → `set -e` 종료.
  항상 `var=$((var+1))` 할당 형태 사용. 이 함정은 `bash -n` 으로 감지 안 됨 (런타임 behavior).
- 배열 선언 `var=()` + `set -u` 조합도 주의 — `${arr[@]}` 같은 빈 확장은 unbound 에러. `"${arr[@]+"${arr[@]}"}"` 패턴 또는 `|| true`.
- Grep 후 `((COUNT++))` 류가 의심되면 `grep -n '((.*++))' scripts/*.sh` 로 사전 감지.

### 2.2 행동 검증 (기능 변경 시 필수)
- **init.sh 변경** — 임시 디렉토리에서 끝까지 실행, 생성 산출물(worktree, config.yml, .claude) 확인.
- **setup-worktree.sh / reset-wt-head.sh 변경** — 버릴 수 있는 테스트 프로젝트에서 실제 wt 생성·파기.
- **judge-action.sh / hook 변경** — DEALOS 에서 실 trigger. mock 으로는 의미 없음.

### 2.3 통합 검증 (릴리스 게이트)
- **DEALOS 를 상시 CI 환경으로 취급**. 변경 반영 후 최소 1~2일 실사용 → 이상 없으면 `vX.Y` 태깅.
- 테스트 파일을 따로 쓰지 않는 이유: 템플릿의 가치는 "다른 프로젝트에 붙여서 돈다" 이고, mock 은 그 의미를 축소한다.

---

## 3. 역전파 창구 (DEALOS ↔ coord-template)

### 3.1 방향 A: DEALOS → coord-template (일반화 추출)
DEALOS 에서 검증된 패턴을 템플릿화할 때:
1. DEALOS 에서 최소 1주 실사용 후 안정성 확인.
2. 하드코딩 치환:
   - `3dview` → `{{work_branch}}` (config 변수)
   - `dealos/`, `dealos-wt-*` → `{{project.name}}`
   - sub 이름 `db`/`backend`/`frontend` → `{{subs[].name}}`
   - 모델 ID (`claude-opus-4-6` 등) → config.yml 참조 (`sub_model` 헬퍼 또는 yaml 파싱)
   - 절대 경로 `coordination/` → config.sh 의 `$COORD_ROOT/coordination/` (subdir 모드 대응)
3. DEALOS 원본은 `_examples/` 에 학습 자료로 보존.
4. README / USAGE 상단 "템플릿 사용자 주의" 배너 추가.

### 3.1.1 추출 완전성 체크리스트 (v0.42+ 신설, v0.37~v0.41 사고 후)

**왜 필요한가**: `f2ddc04 initial extract from DEALOS` (초기 추출) 시 **head-side slash-commands 4개** (`inbox/dispatch/plan/retry`) 가 빠진 상태로 release 됐고 v0.42 까지 발견 안 됨. "op 가 쓰는 명령" 만 추출 가치로 인지한 실수. 다음 추출/승격 시 같은 누락 방지 체크리스트:

- [ ] **`.claude/commands/`** — op-side (`inbox-send`, `review-inbox`, `fix`, `status`, `token-status`, `resolve-escalation`, `reject-escalation`) + **head-side** (`inbox`, `dispatch`, `plan`, `retry`) 모두 확인. head 가 `/inbox` 호출할 때 `.coord/.claude/commands/inbox.md` 가 **각 worktree 에서 접근 가능** 해야 함.
- [ ] **`scripts/config.sh`** 의 env var — 추가된 게 있으면 사용처 (`notify-discord.sh`, `start-bot.sh`, 각 command md) 모두 갱신
- [ ] **`config.yml.example`** — `subs[].model`, `subs[].validation`, `subs[].scope_allowed`, `head.model`, `judiciary.judge_model`, `token_budget.*` 등 참조가 필요한 필드 모두 `.example` 에 있는지
- [ ] **`.githooks/pre-commit`** — 새 경로 패턴이 COMMON/ALLOWED regex 에 반영됐는지
- [ ] **`scripts/lib/classify.sh`** — 새 파일 타입이 preserve/overwrite/manual-merge 분류에 있는지
- [ ] **`scripts/notify-discord.sh`** — 새 알림 유형 있으면 표준화
- [ ] **`coordination/roles/`** — head/sub 역할 문서 갱신 여부
- [ ] **smoke test (필수)** — subdir 모드 새 프로젝트에서 `@bot send "test"` → `/inbox-send` → head spawn → `/inbox` → plan → `/dispatch` → sub → `/review-inbox` 까지 **end-to-end 1회 성공** 확인. 이게 빠지면 추출 누락은 항상 늦게 발견됨.

### 3.2 방향 B: coord-template → DEALOS (개선 역주입)
여기서 개선한 것을 DEALOS 로 돌릴 때:
1. 이 repo 에서 변경 → `vX.Y` 릴리스.
2. DEALOS 에서 `scripts/`, `.claude/commands/`, `.githooks/` diff 확인 후 수동 복사.
3. config 변수는 DEALOS 의 실제 값으로 치환 (자동화 없음 — 현재는).
4. 역주입 후 문제 생기면 A 로 되돌아가 재수정.

### 3.3 피드백 수집 경로
- Issue tracker 없음 (private repo 전제).
- DEALOS 운영 중 이슈는 DEALOS `coordination/hotfixes.md` 에 1차 기록 → 일반화 가치 있으면 이 repo 의 ROADMAP 에 Phase 항목으로 승격.
- 제3자 사용자는 직접 메시지 / Discord DM 으로 수신.

### 3.4 릴리스 직후 DEALOS 동기화 체크리스트
- [ ] `scripts/` diff → 변경분만 복사 (프로젝트 고유 수정 덮어쓰지 않기).
- [ ] `.claude/commands/` diff 확인.
- [ ] `bot/` (v0.6+) — `controller.py` / `requirements.txt` / `README.md` 갱신. `projects.yml` 은 사용자 고유라 skip.
- [ ] `coordination/config.yml.example` 신규 필드 → DEALOS `config.yml` 반영.
- [ ] `.githooks/` 변경 있으면 `git config core.hooksPath .githooks` 재확인.
- [ ] 실제 inbox 1회 돌려 smoke test (`@bot send` 로 자동 orchestration 확인 포함).

---

## 4. 이 repo 특유의 주의

- 여기는 **실행 코드가 적고 템플릿 자체가 산출물**이다. "동작"은 소비 프로젝트에서 증명된다 — 과한 내부 추상화는 이식성 저하.
- 사법부(`judge-action.sh`) / 토큰 예산 / 대시보드 관련 변경은 DEALOS 에서 활발히 돌고 있으므로 **보수적으로** 접근. 기본값 변경은 별도 커밋으로 격리.
- 이 repo 자체는 coordination 시스템을 **쓰지 않는다** (템플릿을 만드는 repo 가 스스로를 오케스트레이션하지는 않는다). inbox/plans/tasks 디렉토리는 **템플릿 디렉토리 구조**일 뿐 실제 작업 흐름 아님.

---

## 5. 모델 승급 절차 (새 Opus/Sonnet/Haiku 출시 시)

config 에 모델이 **고정 버전**(`claude-opus-4-7` 등)으로 박혀있어 자동 승급되지 않는다. `latest` alias 는 예측 가능성 저하로 사용하지 않는다. 새 모델 나오면 다음 순서로 반영:

### 5.1 승급 대상 파일 (grep 기준)
```bash
# Opus 승급 예시 — 모든 occurrence 한 번에 확인
grep -rn "claude-opus-4-6" . \
  --exclude-dir=.git --exclude-dir=.venv --exclude-dir=node_modules \
  --exclude="CHANGELOG.md"
```

전형적으로 업데이트할 위치:
- `coordination/config.yml.example` — head / subs / judge_model
- `scripts/init.sh` — 새 프로젝트 초기화 시 생성되는 config.yml 템플릿
- `.env.local.example` — CLAUDE_HEAD_MODEL / CLAUDE_JUDGE_MODEL 주석 예시
- `.claude/commands/inbox-send.md` — `${HEAD_MODEL:-...}` fallback
- `.claude/commands/resolve-escalation.md` — 동일 fallback
- `coordination/ROADMAP.md` — 샘플 config 블록
- `bot/controller.py` — Option C SDK 기본 모델 (chat_model) **만 Sonnet 승급 시**
- 이 CLAUDE.md 의 Co-Authored-By 컨벤션 줄

### 5.2 CHANGELOG.md 에 승급 섹션 작성
```markdown
<!-- ===== vX.Y ===== -->

# coord-template vX.Y — Opus 4.X 승급

## 변경
- coordination/config.yml.example, scripts/init.sh 등 N개 파일에서 옛 모델 ID 교체
- (승급 후 1~2일 관측하며 체감 변화 기록)

## 관측 (릴리스 직후 빈 칸 → DEALOS 실사용 후 채우기)
- 토큰 소비 변화:
- 응답 품질 체감:
- 비용 (ccusage 비교):
```

### 5.3 CHANGELOG 역사 보존 규칙
**이미 릴리스된 버전의 CHANGELOG 항목은 변경하지 말 것.** 과거 노트에 "claude-opus-4-6" 언급이 있어도 그대로 둔다 — 그 시점의 정확한 기록.

현재 활성 문서(config.yml.example 등)만 승급. 이 규칙은 예시 로그에도 적용 — `scripts/notify-discord.sh` 의 "claude-opus-4-6 → opus" 주석 같은 illustrative 주석은 교체 필수 아님 (regex 는 4-6/4-7 모두 매칭됨).

### 5.4 DEALOS 동기화
§3.4 체크리스트에 추가로:
- [ ] DEALOS 의 `coordination/config.yml` (example 아님) 에서 모델 ID 교체
- [ ] 1~2일 실사용 후 이상 있으면 로컬에서 다시 구버전으로 되돌리고 ROADMAP 에 기록

### 5.5 Sonnet/Haiku 승급
같은 절차. Sonnet 은 judge_model + sub (db/frontend) + bot chat_model 영향. Haiku 는 현재 템플릿에서 고정 사용 없음 (ccusage 요약에서만 축약 대상).

---

## 6. 설치 모드 정책 (v0.41+)

### 6.1 원칙
- **소비 프로젝트는 subdir 모드 (`.coord/` subtree) 기본**. flat 은 coord-template repo **본체** 개발 전용.
- **`.coord/` 는 tracked**. workflow 가 `.coord/coordination/*` 파일의 git sync 로 통신하므로 git 에 있어야 함.
- **배포 산출물 청결**은 `.gitattributes export-ignore` 로. `git archive` / release tarball 에서만 `.coord/` 제외.
- **secrets / runtime 파일**만 개별 gitignore.

### 6.2 subdir 모드 구조 (v0.41+)
```
project/
├── .coord/              ← tracked (subtree + workflow data)
│   ├── coordination/    ← inbox / plans / tasks / reports / review-inbox / escalations — git sync 채널
│   │   ├── HEAD_LOCK    ← gitignored (runtime)
│   │   ├── STOP         ← gitignored (runtime)
│   │   └── dashboard-url.txt ← gitignored (runtime)
│   ├── scripts/         ← template code (subtree 로 tracked)
│   ├── bot/
│   │   ├── projects.yml ← gitignored (사용자 secrets)
│   │   └── usage-alerts-state.json ← gitignored (runtime)
│   ├── .claude/         ← template — source of truth
│   │   └── settings.local.json ← gitignored (사용자 local override)
│   ├── .githooks/
│   ├── templates/
│   ├── docs/
│   ├── _examples/
│   └── .env.local       ← gitignored (opt-in isolation location)
├── .claude/             ← .coord/.claude 로 symlink (Claude Code 제약)
├── .env.local           ← gitignored (primary 위치)
├── coord                ← gitignored wrapper (선택)
├── .gitignore           ← 위 runtime/secret 개별 ignore
└── .gitattributes       ← `.coord/ export-ignore` (배포 tarball 에서 제외)
```

### 6.3 gitignore 정책 — "runtime + secrets only"
통째 `.coord/` ignore 금지. 다음만 개별 ignore:
```
.coord/coordination/HEAD_LOCK
.coord/coordination/STOP
.coord/coordination/dashboard-url.txt
.coord/.env.local
.coord/bot/projects.yml
.coord/bot/usage-alerts-state.json
.coord/.claude/settings.local.json
.coord-backup/
.env.local
.env.local.wt
run-dev.sh
coord
```

### 6.4 `.env.local` 경로 해결 (`scripts/config.sh`)
1. `$PROJECT_ROOT/.env.local` 이 있으면 그거 사용 (**primary**)
2. 없고 `$COORD_ROOT/.env.local` 있으면 그거 사용 (opt-in isolation, symlink 권장)
3. 둘 다 없으면 `$PROJECT_ROOT/.env.local` 을 기대 경로로 반환

`ENV_FILE_RESOLVED` 로 export 되어 모든 스크립트가 일관되게 참조.

**`.coord/.env.local` isolation** (프로젝트가 자체 `.env.local` 충돌 시):
```bash
bash .coord/scripts/link-env-to-coord.sh  # symlink 만 허용, cp fallback 없음
```

### 6.5 v0.37~v0.40 실패 회고 (반복 금지용)

**v0.37 가정** — "coord 는 사용자 utility 라 main 에 흔적 없어야" → `.coord/` 통째 gitignore.

**실제 파괴된 것**:
- `/inbox-send` skill 이 `.coord/coordination/inbox/*.md` git commit/push → head 가 origin polling 으로 감지 → spawn. 통째 ignore 로 **inbox 가 git 에 안 올라감** → spawn trigger 자체 부재.
- 전 workflow (plans/tasks/reports/review-inbox/escalations) 가 같은 패턴으로 동작 불능.

**근본 오류**: `.coord/` 를 **single concept** 으로 본 것. 실제로는 두 종류 섞여있었음:
- **Template code** (scripts/bot/.claude/templates/docs) — subtree 로 tracked, workflow 와 무관
- **Workflow data** (coordination/inbox/plans/...) — workflow 통신 매체, git sync 필수

"utility 숨김" 욕구가 data 동기화 채널까지 끊어버림.

**v0.41 정답**: 폴더 차원 정책 포기. 구체 파일 단위로 판단. 배포 청결은 `.gitattributes export-ignore` (git archive 단계에서만 제외) 로 분리.

**교훈**:
- 폴더 차원 gitignore 는 내부 파일 역할이 섞여있을 때 위험. "이 폴더 안에 뭐가 있지?" 를 항상 확인.
- workflow / mechanism 영향을 design 단계에서 체크. "inbox-send 는 아직 안 돌려봤는데 build 는 통과했으니 OK" 는 심각한 gap.
- "배포 청결" 같은 최종 목표는 처음부터 `.gitattributes` / `export-ignore` / CI 단계에 국한. 개발 단계 (git log) 까지 끌어오지 말 것.

### 6.3 자동 gitignore 등록
- `init.sh` (신규 설치) 와 `upgrade-coord.sh` (업그레이드) 양쪽에서 subdir 모드면 아래 항목 자동 등록:
  ```
  .coord/
  .claude/
  .coord-backup/
  .env.local
  .env.local.wt
  run-dev.sh
  ```
- 이미 tracked 된 상태면 `upgrade-coord.sh` 가 `git rm -r --cached` 명령을 안내 (자동 실행은 위험해서 skip).

### 6.4 flat 모드 deprecated 경고
- `init.sh` 는 flat 모드 실행 시 PROJECT_ROOT 가 `coord-template` 문자열 포함하지 않으면 deprecated 경고 출력.
- coord-template repo 본체에서 init 돌릴 때는 경고 없이 동작 (개발자는 flat 이 의도).

### 6.5 마이그레이션 (기존 flat → subdir)
자동화 스크립트 없음 (사용자가 직접 수행). 절차:
1. 기존 `coordination/`, `scripts/`, `bot/`, `.claude/`, `.githooks/`, `templates/`, `docs/` 를 백업
2. 해당 폴더들 제거 (`git rm -rf ...` + commit)
3. subtree 추가: `git subtree add --prefix=.coord ...`
4. `bash .coord/scripts/init.sh --mode=subdir`
5. 백업한 사용자 설정 (`coordination/config.yml`, `bot/projects.yml` 등) 을 `.coord/` 하위로 복사
6. `git config core.hooksPath .coord/.githooks` 재설정 확인

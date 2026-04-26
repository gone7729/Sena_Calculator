# Coordination System Roadmap

> **템플릿 사용자**: 이 문서는 원본 DEALOS (브랜치 `3dview`, 디렉토리 `dealos/`) 기준 작성됨.
> 자기 프로젝트의 work_branch / project_name 으로 읽어 해석하면 됨. 정본은 [config.yml.example](config.yml.example).

DEALOS 에서 시작된 Claude 세션 오케스트레이션 시스템의 장기 비전과 단계별 계획.

---

## 비전

> 사용자가 어디에서든 (로컬/모바일/Discord) 자연어로 작업 지시를 내리면,
> 여러 Claude 세션이 병렬로 작동해 코드 작업을 수행하고,
> 사용자가 최종 검토만 하는 자동화 오케스트레이션 시스템.

**최종 목표 (원거리)**: 멀티 프로젝트를 Discord 메시지 하나로 통제.
**단기 목표**: DEALOS 프로젝트 내부 안정화.

---

## 원칙

1. **파일 기반 협업**: 세션 간 직접 통신 대신 git + coordination/ 파일
2. **3dview 단일 정본**: coordination 의 권위 브랜치
3. **수동 통제 지점 유지**: 자동화하되 사용자 최종 승인 (특히 main 머지)
4. **오픈소스 아님 / NPM 아님**: 프라이빗 재사용, 소규모 사용자 대상
5. **로컬 컨트롤러**: 중앙 서버 없음. 사용자 머신이 컨트롤러 호스트
6. **Discord = UI 레이어**: 원격 조작용, 호스팅 기능 아님

---

## 현재 상태 (Level 1 완료)

**구축됨:**
- 파일 기반 coordination (`coordination/` 디렉토리)
- 슬래시 명령 시스템 (tracked in git)
  - 3dview: `fix`, `inbox-send`, `review-inbox`
  - wt/head: `inbox`, `dispatch`, `plan`, `retry`
  - wt/<sub>: `go`
- Plan 기반 다단계 실행 (Step 순차 + 의존성)
- Head 가 headless sub 프로세스 spawn (`claude -p`)
- 실패 보관 브랜치 (`failed/*`)
- STOP 시그널
- 에스컬레이션 플로우
- Discord 알림 (단방향, 주요 이벤트)
- 토큰 예산 (휴리스틱)
- 3dview 임시 머지 + 검증 + push/rollback

**브랜치 규약:**
- `wt/<sub>` → `3dview` 자동 (검증 통과 시)
- `3dview` → `main` 수동 전용 (협업자 협의 필수)

---

## Phase 2.6: op-head-sub 3-tier 정식화 + 에스컬레이션 중계 (완료, 2026-04-15)

**핵심 인사이트**: 사용자가 자연어로 op 와 대화 → op 가 구조화된 파일로 head 에게 전달.
사법부 거부도 이 경로로 처리 (head 가 사용자 직접 호출 X, op 경유 O).

### 3-tier 구조
```
사용자 ↔ op (메인 세션) ↔ head ↔ sub
        자연어             파일+git
```

### 추가된 op 명령
- `/resolve-escalation <id> [reason]`: 사법부 거부 승인 → approvals 파일 + Discord
- `/reject-escalation <id> [reason]`: 거부 유지 → head 가 다른 방식 모색
- `/status`: 전체 시스템 현황 한눈 요약

### Head 흐름 추가
- `/inbox` 1.5단계: pending escalation + approval 자동 확인 + 갱신
- 승인된 escalation 은 status: resolved-approved 로 자동 전환

### Judge 에 2.5심 추가
- 최근 1시간 내 approval 있으면 통과 (1회용)
- 사용자 친화적 — 판사 거부 후 승인 → 즉시 재시도 가능

### Phase 6 와의 연결
- Discord bot 도 op 의 frontend
- 사용자 Discord 메시지 → bot → op 명령 호출 → 동일 동작
- VSCode chat 과 Discord 가 두 진입점

## Phase 2: 안정화 (완료 — 2026-04-16)

**목표**: 운영 중 드러난 이슈 해결. 신규 기능 없음.

### 작업 목록
- [x] 권한 모드 표준화: 슬래시 명령에서 sub 호출 시 `bypassPermissions` 기본값 (inbox-send.md / resolve-escalation.md / judge-action.sh 모두 적용)
- [x] wt/head 복구 스크립트: `scripts/reset-wt-head.sh` (v0.4 에서 config-driven 으로 재작성)
- [x] pre-commit hook: sub 이 역할 밖 파일 커밋 차단 (`.githooks/pre-commit`)
- [x] `coordination/TROUBLESHOOTING.md` 작성 (16개 항목)
- [x] `scripts/backup-commands.sh` (안전망)
- [x] **Step 별 Discord 알림**: 각 sub 완료 시점마다 알림 (`scripts/notify-step.sh` — 표준 포맷 helper)
  - 포맷: `✅ Step 1/3 완료 (wt/db)` + plan_id + summary
  - status: started | ok | done | fail | blocked | skip
- [~] `/go` 수동 흐름 commit+push 보강 — **DEALOS 측 작업** (템플릿엔 `/go` 명령 파일 없음; sub-role 의존). 템플릿은 관련 문서화만 제공.

### 발견된 이슈 (이 Phase 에서 해결)
- 1. `acceptEdits` 가 Read 까지 차단 — `bypassPermissions` 로 해결 ✓
- 2. `reset --hard` 로 `.claude/commands/` 손실 — tracked 전환으로 해결 ✓
- 3. wt/head 브랜치 divergence 시 rebase 충돌 — `reset-wt-head.sh` 로 완화 ✓
- 4. 수동 /go 흐름의 commit+push 미검증 — DEALOS 측 작업 (템플릿 범위 밖)

---

## Phase 3: 모니터링 / 가시성 (구체화 — 대시보드 서버 중심)

**목표**: 항상 켠 PC 에 대시보드 서버 + Tunnel → 모바일에서 실시간 확인.

### 결정사항 (2026-04-16)
- **방식**: HTML 정적 X, **대시보드 서버** (FastAPI + Cloudflare Tunnel)
- **이유**: 항상 켠 PC 활용 + 모바일 실시간 + 그래프/상호작용
- **Tunnel**: Cloudflare Tunnel (무료, 카드 등록 필요) 또는 대안 (Tailscale, ngrok)

### 기술 스택
- **Python FastAPI** (가벼움, ~100줄)
- **HTML + Tailwind + HTMX** (5초마다 fetch, 의존성 최소)
- **데이터 소스**: `coordination/` 파일들 (이미 모든 정보 보유)
- **외부 노출**: Cloudflare Tunnel (Free Zero Trust)
- **인증**: Cloudflare Access (이메일 화이트리스트, Free 50 user)

### 페이지 구성 (단일 페이지 + 상세 라우트)
```
/  ← 대시보드 메인
   - 진행 중 plan (Step 진행률 ████░░ 50%)
   - 토큰 사용량 (% bar, 다음 임계까지 남은 양, 리셋까지 남은 시간)
   - inbox 통계 (pending / processing / merged 카운트)
   - 활성 escalation (있으면 빨간 배너)
   - 검토 대기 review-inbox (있으면 알림)
   - sub 브랜치 ahead/behind 상태
   - 최근 hotfixes 3건

/inbox/<id>          ← 특정 inbox 상세
/review/<id>         ← review-inbox 상세
/escalations/<id>    ← escalation 상세 + 승인/거부 버튼
/tokens              ← 토큰 사용 내역 그래프 (히스토리)
/branches            ← 브랜치 상태 상세
```

### 작업 목록
- [ ] `scripts/dashboard.py` — FastAPI 앱
- [ ] `templates/dashboard.html` (+ Tailwind CDN, HTMX)
- [ ] `scripts/start-dashboard.sh` — 서버 + Tunnel 동시 기동
- [ ] `docs/setup-tunnel.md` — Cloudflare Tunnel 셋업 가이드
- [ ] `scripts/notify-discord.sh` 에 대시보드 URL 자동 첨부 옵션
- [ ] (옵션) `scripts/coord-stats.sh` — 성공률, 평균 Step 시간 분석

### 외부 노출 옵션 비교

| 옵션 | 가격 | 사용자 환경 부합도 | 채택 |
|------|-----|----------------|------|
| **Cloudflare TryCloudflare** | $0 (계정 X) | 임시 URL, 지금 사용 중 | ✅ **현재** |
| Cloudflare Named Tunnel + Zero Trust | $0~10/년 (도메인) | 영구 도메인 + 인증 | 향후 영구화 시 |
| Tailscale | $0 (카드 X) | 사적 VPN, 본인 디바이스만 | (백업, 미사용) |
| ngrok Free | $0 (카드 X) | 임시 URL | 미채택 |

**현재 운영**: Cloudflare TryCloudflare URL + Discord 알림에 라이브 링크 자동 첨부.
URL 변경 시 `scripts/update-dashboard-url.sh` 가 갱신 + Discord 알림.

**보안**: TryCloudflare URL 은 4-단어 random → 추측 불가. 대시보드는 메타데이터만 (코드/비밀 X) 노출되므로 URL 비공개로 충분.

**Tailscale**: 초기 PoC 로 테스트했으나 Cloudflare 가 같은 역할 (모바일 외부 접근) 더 잘 수행. 클라이언트 설치된 상태로 백업 (제거 불필요).

### 보안 (외부 노출 시 필수)
- Cloudflare Access 정책: 이메일 화이트리스트
- 또는 Tailscale: 사적 VPN 망 (외부인 접근 불가)
- ngrok 유료: Basic auth

---

## Phase 4: Level 3 원격 트리거 (Phase 3 후, 1주)

**목표**: 로컬 머신 앞에 없어도 자동 처리.

### 작업 목록
- [ ] GitHub webhook 수신기 (로컬)
- [ ] Cloudflare Tunnel 설정 (무료, 안정)
- [ ] push → `/inbox` 자동 트리거
- [ ] 안전장치
  - 동시 실행 방지 (lock file)
  - author 화이트리스트
  - rate limiting

**참고**: Phase 6 Discord Bot 이 구축되면 Level 3 의 GitHub webhook 방식 대체 가능
(Discord Bot 은 outbound 연결이라 인프라 훨씬 단순). 이 경우 Phase 4 skip 또는
최소 구현만 하고 Phase 6 로 바로 진입 가능.

---

## Phase 5: 프라이빗 템플릿화 (Phase 4 후 또는 병행, 1주)

**목표**: DEALOS 외 프로젝트에서 재사용 가능한 구조로 추출.

### 핵심 아이디어
- DEALOS 하드코딩 부분을 config 로 파라미터화
- GitHub 프라이빗 템플릿 저장소로 추출
- 새 프로젝트 시작 시 `git clone + init.sh` 로 10분 내 세팅

### 일반화 대상
| 항목 | 현재 (DEALOS) | 템플릿 |
|------|-------------|--------|
| 기본 브랜치 | `3dview` | config |
| 안정 브랜치 | `main` | config |
| Sub 개수 | 3 (db/backend/frontend) | **자유 설정 (2~N)** |
| Sub 이름 | db/backend/frontend | config |
| 역할 정의 | roles/*.md 정적 | config + 동적 prompt |
| 검증 명령 | `npm run ssot:check` | config |
| Discord 채널 | 고정 | config |
| 모델 배분 | opus=backend | config |

### 설정 파일 예시 (`coordination/config.yml`)
```yaml
project:
  name: my-project
  root_branch: develop
  stable_branch: main

subs:
  - name: api
    port: 3001
    model: claude-sonnet-4-6
    scope_allowed: ["src/api/**", "db/**"]
    validation: "npm test"
  - name: ui
    port: 3002
    model: claude-sonnet-4-6
    scope_allowed: ["src/ui/**"]
    validation: "npm run lint"

head:
  port: 3099
  model: claude-opus-4-7

notifications:
  discord_webhook_env: DISCORD_WEBHOOK_URL

merge:
  to_root: auto    # sub → root_branch
  to_stable: manual # root_branch → stable_branch
```

### 동적 역할 시스템
- `roles/*.md` 는 **선택적 기본값**
- Head 가 task 마다 역할 설명을 task 파일에 **inline 주입**
- 프로젝트 타입 무관 (React / Python ML / Go / Rust 등 모두 지원)
- sub 개수도 매 task 마다 다를 수 있음

### 배포 방식
- GitHub 프라이빗 저장소: `claude-coord-template`
- 새 프로젝트에서:
  ```bash
  git clone --depth 1 git@github.com:<you>/claude-coord-template ._tmp
  cp -r ._tmp/{coordination,scripts,.claude} .
  rm -rf ._tmp
  bash scripts/init.sh
  ```

---

## Phase 5-8 (v0.3 후보): `scripts/upgrade-coord.sh` — 템플릿 업그레이드 자동화

**목표**: 이미 init.sh 로 설치된 소비 프로젝트(DEALOS 등)에서 **사용자 설정/런타임 상태를 깨지 않고** 최신 coord-template 을 받아오는 공식 경로 확보. **tarball / git clone / subtree 3가지 소스 모드** 모두 지원.

**배경**: init.sh 는 최초 셋업 전용. 지금은 역주입(CLAUDE.md §3.2)이 수동 diff → 복사라서 릴리스마다 휴먼 에러 위험이 있다. Phase 5-9 (v0.4) 의 subtree 모드와도 맞물리도록 동일 분류 엔진을 사용한다.

### 분류 규칙

파일/디렉토리 단위로 3가지 정책 중 하나에 매핑:

| 정책 | 대상 | 동작 |
|------|------|------|
| **overwrite** (자동 덮어쓰기) | `scripts/`, `.githooks/`, `templates/`, `coordination/*.md` (docs), `coordination/roles/_examples/` | 새 버전으로 완전 교체. 로컬 수정은 backup 에만 보존. |
| **preserve** (보존) | `coordination/config.yml`, `coordination/roles/` (사용자 역할 파일), `coordination/inbox/`, `coordination/plans/`, `coordination/tasks/`, `coordination/reports/`, `coordination/review-inbox/`, `coordination/escalations/`, `coordination/approvals/`, `coordination/hotfixes.md`, `coordination/token-budget.md`, `.env.local`, `.claude/settings.local.json` | 건드리지 않음. 신규 파일만 `*.example` 로 제공. |
| **manual-merge** (수동 병합) | `.claude/commands/*.md` (op 슬래시 명령), `coordination/roles/head.md` | 3-way diff 출력 → 사용자가 승인해야 적용. 기본 skip, `--accept-commands` / `--accept-roles` 플래그로 일괄 승인 가능. |

**기본 원칙**: 소스(템플릿 지분) = overwrite, 런타임/사용자 지분 = preserve, 경계 지분(사용자가 커스터마이징했을 수 있는 슬래시 명령/역할) = manual-merge.

**소스 모드와 분류 규칙의 독립성**: 분류 규칙은 소스 모드(tarball/git/subtree)와 무관하게 동일하게 적용된다. subtree 모드는 "파일을 어떻게 가져오느냐"만 다를 뿐, 분류·백업·`.claude/` 재동기화·CHANGELOG 는 그대로.

### 동작 흐름

```
upgrade-coord.sh [--version vX.Y] [--mode tarball|git|subtree|auto]
                 [--accept-commands] [--accept-roles] [--dry-run]
  ↓
1. 사전 점검: git clean 상태 / work_branch 확인 / STOP 시그널 없음
2. 모드 감지 (--mode=auto 기본):
   - `.coord/` 가 subtree prefix 로 등록돼 있으면 → subtree
   - `coordination/.coord-version` 이 있고 git 원격 접근 가능 → git
   - 그 외 → tarball
3. 소스 획득:
   - tarball: gh release download vX.Y -p 'coord-vX.Y.tar.gz' → /tmp 에 펼침
   - git:     git clone --depth 1 -b vX.Y <template-url> /tmp/coord-upgrade
   - subtree: git subtree pull --prefix=.coord <template-url> vX.Y --squash
              (파일은 이미 `.coord/` 내부로 들어와 있음 → 분류 엔진이 그 경로를 참조)
4. 백업: `.coord-backup/<ts>/` 에 overwrite + manual-merge 대상 현재 파일 복사
5. 분류별 적용:
   - overwrite: rsync --delete <src>/ <dst>/
   - preserve: 건너뜀 (신규면 `.example` suffix 로만 생성)
   - manual-merge: diff 출력 → prompt (y/n/s=skip-all)
6. `.claude/` 재동기화 (subdir 모드 전용, Phase 5-9 연계):
   - `.coord/claude-template/commands/*.md` → 루트 `.claude/commands/*.md` 로
     **manual-merge 분류 그대로 적용** (사용자 커스터마이즈 보존)
   - `.coord/claude-template/settings.json` → 루트 `.claude/settings.json` overwrite
   - `.claude/settings.local.json` 은 절대 건드리지 않음 (preserve)
7. 설정 마이그레이션: `config.yml.example` 신규 필드 감지 → 사용자 `config.yml` 에 주석으로 추가 제안
8. 버전 기록: `coordination/.coord-version` 갱신 (before → after)
9. CHANGELOG 출력: 이전 버전 ~ 신버전 간 `git log` 요약 + 깨짐 위험 항목 highlight
10. 사후 점검: `bash -n scripts/*.sh` / pre-commit 훅 경로 / smoke 안내
```

### 산출물
- `scripts/upgrade-coord.sh` — 메인 진입점
- `scripts/lib/classify.sh` — 분류 규칙 테이블 (Phase 5-9 에서도 동일 엔진 재사용)
- `scripts/lib/sync-claude.sh` — `.claude/` 재동기화 전용 헬퍼 (subdir 모드 / init.sh 도 호출)
- `docs/upgrade.md` — 사용자 가이드 (dry-run 권장, 롤백 절차, 3가지 소스 모드 선택 가이드)
- `coordination/TROUBLESHOOTING.md` — 업그레이드 실패 시 복구 섹션 추가
- `coordination/.coord-version` — 현재 적용 버전 기록 파일 (신규)

### 검증
- DEALOS (flat 설치) 를 v0.2 → v0.3 으로 tarball 모드 업그레이드 dogfooding (CLAUDE.md §2.3 통합 검증 경로).
- 테스트 프로젝트에서 subtree 모드 (v0.4 와 함께) — subtree pull 후 `.claude/` 재동기화까지 확인.
- 최소 4회: dry-run / tarball 정상 / subtree 정상 / manual-merge 거부 시나리오.

### Phase 5 와의 관계
- Phase 5-2 (init.sh) = **greenfield**
- Phase 5-8 (upgrade-coord.sh) = **brownfield** (flat + subdir 공통)
- Phase 5-9 (subdir 모드) = **레이아웃 변경**, 5-8 의 `.claude/` 재동기화 로직을 사용
- 5-2 + 5-8 + 5-9 완료 시 Phase 5 실무 완결 → v1.0 논의 가능.

### 후속 고려사항 (Phase 5-8 이후, 필수 아님)
- 역방향 diff 수집: 소비 프로젝트에서 템플릿 대비 수정분을 자동으로 추출 → 역전파 창구(§3.1) 입력 후보화.

---

## Phase 5-9 (v0.4 후보): `.coord/` 서브디렉토리 모드 (full subdir + subtree 지원)

**목표**: 새 프로젝트가 **`git subtree`** 로 템플릿을 `.coord/` 에 마운트하고, `git subtree pull` 로 업데이트 받을 수 있는 레이아웃 제공. 기존 flat 설치 (DEALOS 포함) 는 그대로 유지 — v0.4 는 신규 프로젝트 선택지.

**배경**: flat 설치는 `scripts/`, `coordination/`, `templates/` 가 루트에 흩어져 프로젝트 파일과 혼재. subtree 로 `.coord/` 에 묶으면 (a) 템플릿 vs 프로젝트 경계가 시각적으로 분명, (b) `git subtree pull` 한 줄로 업그레이드, (c) Phase 5-8 의 upgrade-coord.sh 와 조합 시 `.claude/` 재동기화만 추가로 수행하면 됨.

### 결정사항 (요약)
- **레이아웃**: full subdir — `coordination/`, `scripts/`, `templates/`, `.githooks/` 모두 `.coord/` 내부. 예외는 `.claude/` (Claude Code 제약).
- **.claude/ 동기화 정책**: 템플릿은 `.coord/claude-template/` 로 배포. init.sh / upgrade-coord.sh 가 루트 `.claude/` 로 재동기화. `commands/*.md` 는 Phase 5-8 의 manual-merge 규칙 적용 → 사용자 커스터마이즈 보존. `settings.local.json` 은 절대 건드리지 않음.
- **COORD_ROOT 환경변수**: 모든 스크립트 진입점에서 자동 검출 (`.coord/scripts/` 아래면 `COORD_ROOT=.coord`, 그 외면 `COORD_ROOT=.`). 명시 override 가능.
- **런타임 상태 위치**: subdir 모드에서도 `.coord/coordination/inbox/` 등 `.coord/` 내부. subtree pull 과의 비충돌 계약은 "upstream 이 런타임 디렉토리와 `config.yml` 을 tracked 로 두지 않는다" (이미 gitignored) — 문서화만 필요.

### 레이아웃 비교

**flat (v0.3 까지, DEALOS)**
```
project/
├── scripts/  coordination/  templates/  .githooks/  docs/
├── .claude/
└── (프로젝트 고유 파일)
```

**subdir (v0.4 신규 프로젝트 옵션)**
```
project/
├── .coord/                    # subtree prefix
│   ├── scripts/
│   ├── coordination/          # config.yml, inbox/, plans/ ... 모두 여기
│   ├── templates/
│   ├── .githooks/
│   ├── claude-template/       # → 루트 .claude/ 로 재동기화
│   └── docs/
├── .claude/                   # 루트에 존재 (Claude Code 제약) — sync 대상
└── (프로젝트 고유 파일)
```

### 작업 목록
- [ ] `COORD_ROOT` 자동 검출 로직 — `scripts/config.sh` 상단에 `COORD_ROOT=$(detect_coord_root)` 추가
- [ ] 전 스크립트 경로 재작성 — `coordination/` → `$COORD_ROOT/coordination/`, `scripts/lib/` 참조 동일하게
- [ ] `.githooks/` 경로 — `git config core.hooksPath .coord/.githooks` 로 설정 (subdir 모드)
- [ ] `.coord/claude-template/` 구조 신설 — 기존 `.claude/` 내용을 템플릿 측에서는 여기에 배치
- [ ] `scripts/lib/sync-claude.sh` — Phase 5-8 산출물과 공유
- [ ] `scripts/init.sh` 에 `--mode=flat|subdir` 플래그 추가 (기본: subdir, 기존 사용자는 `--mode=flat`)
- [ ] `.gitattributes` — `.coord/` 내부 `*.sh` 는 LF 강제 (Windows subtree 대응)
- [ ] `docs/install-subtree.md` — subtree 전용 설치 가이드
- [ ] `coordination/TROUBLESHOOTING.md` — subtree pull 충돌 복구 섹션

### 마이그레이션 (기존 flat → subdir)
v0.4 릴리스 시점에 flat → subdir 이전은 **지원하지 않음** (git history 재작성 또는 주의 깊은 수동 이동 필요). 기존 프로젝트는 flat 유지 + Phase 5-8 upgrade-coord.sh 로 계속 업그레이드. 신규 프로젝트만 subdir 채택.

### subtree 흐름 (사용자 관점)
```bash
# 최초 설치 (신규 프로젝트)
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git v0.4 --squash
bash .coord/scripts/init.sh   # config.yml 세팅 + .claude/ 재동기화 + worktree 생성

# 업데이트
git subtree pull --prefix=.coord https://github.com/gone7729/coord-template.git v0.5 --squash
bash .coord/scripts/upgrade-coord.sh --mode=subtree   # .claude/ 재동기화 + CHANGELOG + .coord-version 갱신
```

### 검증
- 신규 테스트 프로젝트에 subtree add → init.sh → 첫 inbox 1회 완주.
- v0.4 → v0.5 (가상) subtree pull + upgrade-coord.sh --mode=subtree 사이클.
- 기존 DEALOS (flat) 는 영향 없는지 upgrade-coord.sh --mode=tarball 동작 확인.

### Phase 5 와의 관계
Phase 5-2 (init.sh greenfield) + 5-8 (upgrade brownfield) + 5-9 (subdir/subtree) = 프라이빗 템플릿화 완결. 이후 v1.0.

---

## Phase 6: Controller + Discord Bot + 멀티 프로젝트 (MVP 완료 — 2026-04-17)

**목표**: 여러 프로젝트를 Discord 로 통합 제어.

### 완료된 범위 (v0.6 → v0.9)
- [x] discord.py 봇 기본 구조 (v0.6 — `bot/controller.py`, `bot/projects.yml.example`, `bot/requirements.txt`, `bot/README.md`, `scripts/start-bot.sh`)
- [x] Windows Git Bash 호환 (v0.6.1 — UTF-8 stdout, `shutil.which("claude")`, `tempfile.gettempdir()`, `PYTHONUNBUFFERED=1`)
- [x] 채널별 프로젝트 자동 라우팅 (v0.7 — `_resolve_project(channel_id)`)
- [x] 레지스트리 관리 명령 (v0.7 — `register` / `unregister` / `bind` / `unbind` / `reload`)
- [x] help 명령 Discord Embed (v0.7.1)
- [x] 서버 초대 시 자동 환영 메시지 (v0.7.1 — `on_guild_join`)
- [x] DEALOS 잔재 정리 (v0.7.2 — docs / 복붙 블록)
- [x] 리액션 기반 escalation 승인/거부 (v0.8 — `on_raw_reaction_add`, `judge-action.sh` 에 parseable id)
- [x] `@bot retry <inbox-id>` — head 에 `/retry` spawn (v0.8)
- [x] `@bot budget [project]` — `coordination/token-budget.md` 기반 progress bar (v0.8)
- [x] `@bot usage [days]` — **ccusage 통합** Claude CLI 실 비용 (v0.8.1)
- [x] Thread 격리 — `@bot <text>` spawn 시 Discord thread 자동 생성 + thread 내 자동 라우팅 (v0.9)
- [x] Usage 자동 경고 — ccusage 임계 초과 시 Discord 자동 알림 (v0.9)

### 미구현 (후속 후보)
- [ ] 멀티 프로젝트 동시 head 스케줄링 (현재는 HEAD_LOCK 으로 단일 직렬)
- [ ] `@bot subscribe/unsubscribe` — 특정 프로젝트 완료 알림 채널 구독
- [ ] `@bot dispatch <sub>` — 특정 sub 만 수동 dispatch
- [ ] 봇 명령으로 `projects.yml` 저장 시 YAML 주석 보존 (ruamel.yaml 도입)

### Spawn 모델 (Phase 2.6 op→head 자동 spawn 의 확장)

```
사용자 (Discord 메시지)
  ↓
Discord Bot (Python, 항상 켜짐)
  ↓ subprocess
op 세션 spawn (claude -p "/inbox-send <내용>" --cwd <project>/)
  ↓ subprocess (Phase 2.6 에서 이미 구현됨)
head 세션 spawn (claude -p "/inbox" --cwd <project>-wt-head/)
  ↓ subprocess (이미 구현됨)
sub 세션들 spawn (claude -p "/go" --cwd <project>-wt-<sub>/)
  ↓
완료 → head 가 직접 Discord 알림
```

### 핵심 인사이트
- **각 계층이 다음 계층을 headless spawn** (op→head→sub)
- 모든 계층 동일 패턴 — 일관성
- bot 은 op 의 진입점만 추가 (op 자체 동작은 동일)
- 새 세션 = hooks 신선 로드 = 사법부 항상 활성

### 동시성 제어
- `coordination/HEAD_LOCK` 파일로 head 동시 실행 방지
- 멀티 프로젝트는 worktree 격리로 자연스레 병렬 가능 (각 프로젝트 자체 lock)

### Long-running 처리
- inbox 처리 30분~1시간 가능
- Bot 응답: spawn 즉시 "처리 시작" 응답 → 완료는 head 가 직접 Discord
- 사용자 입장: 채팅에 "✅ 시작" → "🎉 완료" 두 메시지 받음


### 아키텍처

```
User (Discord 앱, 어디서든)
  ↓ 메시지: "@bot [project-a] 가격 계산 리팩터링"
  ↓ WebSocket
Discord API
  ↑ push 이벤트
  ↓
Controller (User 로컬 머신, 항상 켜짐)
  ├─ discord.py 봇
  ├─ FastAPI (내부 API)
  ├─ 프로젝트 레지스트리 (config)
  ├─ 메시지 파싱 + 라우팅
  └─ 각 head 프로세스 관리
         ↓
    [Head A] [Head B] [Head C]  (백그라운드 실행)
         ↓         ↓         ↓
    subs     subs     subs
```

### 주요 기능
- **자연어 지시**: `@bot 로그인 버그 고쳐줘 [api-project]`
- **멀티 프로젝트**: 동시에 여러 프로젝트 head 실행
- **양방향 대화**: Bot 이 승인 요청 → User 가 이모지 리액션으로 승인
- **히스토리 자동**: Discord 채널 = 작업 로그
- **첨부 지원**: 스크린샷으로 버그 전달 → Bot 이 task 파일에 임베드
- **상태 조회**: `/status` → 현재 실행 중인 모든 프로젝트 진행률

### 기술 스택
- **언어**: Python (discord.py, FastAPI)
- **선정 이유**: Discord bot 생태계 성숙 + 다양한 언어/시스템 처리 능력
- **실행 위치**: User 로컬 (프라이빗)
  - 데스크탑 상시 / Raspberry Pi / 저렴 VPS 중 선택
  - 제품화해도 중앙 서버 없음 — User 가 자기 머신에서 실행

### Controller 설정 예시 (`~/.claude-coord/projects.yml`)
```yaml
projects:
  - id: dealos
    path: /home/user/projects/dealos
    discord_channel_id: "1234567890"
    enabled: true
    
  - id: fintech
    path: /home/user/projects/fintech
    discord_channel_id: "1234567891"
    enabled: true

controller:
  discord_bot_token_env: DISCORD_BOT_TOKEN
  max_concurrent_heads: 3  # 토큰 예산에 따라
  log_dir: ~/.claude-coord/logs
```

### 주요 명령
| Discord 메시지 | 효과 |
|--------------|------|
| `@bot [project] <작업 내용>` | inbox-send |
| `@bot [project] status` | 현재 진행 상황 |
| `@bot [project] stop` | STOP 시그널 |
| `@bot [project] retry <inbox-id>` | 실패한 plan 재시도 |
| `@bot projects` | 등록된 프로젝트 목록 |
| `@bot budget` | 토큰 예산 전체 요약 |
| (Bot 메시지에 ✅ 리액션) | 승인 |
| (Bot 메시지에 ❌ 리액션) | 거부 |

### 토큰 소비 무관심 사용자 (Max/Enterprise 플랜) 지원
- 병렬 프로젝트 (예: 5개 동시 진행)
- 병렬 sub (순차 대신 병렬 dispatch 옵션)
- 자동 재시도 (실패 시 즉시 변형 재시도)
- 백그라운드 상시 실행

---

## ~~Phase 7: CLI~~ (계획 제외 — 2026-04-17)

**제외 사유**: Phase 6 Discord Bot 이 원격 조작 요구를 전부 커버. 별도 CLI 유지 부담 > 효용. 필요 시 다음 중 하나로 대체 가능:
- 기존 `scripts/*.sh` 직접 호출 (로컬 터미널)
- 슬래시 명령 직접 실행 (op 세션)
- Discord 봇 API 를 curl 로 직접 호출 (편법)

**관련 후보가 필요해질 만한 상황**: CI/CD pipeline 에서 coord 트리거, headless 서버 전용 운영 등. 그때 재평가.

---

## Phase 8+: 고도화 (필요 시)

### 후속 후보 리스트
- **Web 대시보드 멀티 프로젝트 뷰** (현재는 단일 프로젝트)
- **Agent Teams 통합** — 연구/병렬 탐색 시 자동 활용
- **Past report learning** — 유사 task 시 과거 보고서 자동 참조
- **코드 품질 자동 judge** — 프로젝트별 룰 embed
- **Thread 격리 확장** — inbox 와 thread 연결 상태를 projects.yml 이나 런타임 메타에 영구 기록 (현재는 thread 는 "정리 용도" 만)
- **모바일 앱** — Discord 외 대안 iOS/Android native (우선순위 낮음, Discord 가 충분)
- **팀 라이선스 기능** — 복수 User 한 Controller 공유 (프라이빗 유지 방침상 우선순위 낮음)

### 의사결정 대기 중
- **v1.0 기준**: Phase 5 (템플릿화) + Phase 6 (봇) 안정화 후 DEALOS 외 2~3개 프로젝트 실사용 검증 완료 시 태깅.

---

## 결정사항 요약

| 주제 | 결정 |
|------|------|
| 배포 모델 | 프라이빗 (오픈소스 X, NPM X) |
| Controller 호스팅 | User 로컬 (중앙 서버 없음) |
| 제품화 시 서버 부담 | 없음 (User 가 자기 머신 담당) |
| 언어 | Python (Controller/Bot), Bash (스크립트) |
| Sub 개수 | 자유 설정 (2~N) |
| 역할 | 동적 주입 (head 가 task 마다 정의) |
| 브랜치 전략 | wt/* → root (자동), root → stable (수동) |
| Discord 역할 | UI 레이어 (호스팅 아님) |
| Level 3 vs Phase 6 | Phase 6 (Discord Bot) 이 더 단순 → Level 3 우선순위 낮아짐 |

---

## 우선순위 요약 (실행 계획)

### 현재까지 진행 (2026-04-17 기준)

```
✅ Phase 0~2.6 (파일 조율 + 사법부 + 토큰 + 첨부)
✅ Phase 2 안정화 (notify-step 등)
✅ Phase 3 모니터링 (대시보드 + Cloudflare Tunnel)
✅ Phase 5 프라이빗 템플릿화 (init.sh / upgrade-coord.sh / .coord subdir)
✅ Phase 6 Discord Bot MVP (v0.6 → v0.9, 9개 릴리스)
```

### 미진행 / 차기

```
Phase 4 (Level 3 GitHub webhook) — 우선순위 ↓
  이유: Phase 6 Discord Bot 이 같은 문제 (원격 트리거) 를 더 단순하게 해결.
        GitHub webhook 수신기는 별도 인프라(공개 endpoint) 필요. Discord 는
        outbound 연결이라 방화벽 설정 불필요.

Phase 7 (CLI) — 계획 제외
  이유: Discord Bot 이 원격 조작 요구 커버. CLI 유지 부담 > 효용.
        대체: scripts/*.sh 직접 호출, 슬래시 명령, curl 봇 API.

Phase 8+ 고도화 — 수요 기반
```

### v1.0 기준
Phase 6 안정화 + DEALOS 외 2~3개 프로젝트 실사용 검증 완료 시 v1.0 태깅.
현재 DEALOS 만 검증됨 — 제3 프로젝트 실사용 축적이 gate.

---

## 재검토 주기

- **매 Phase 완료 후**: 이 문서 업데이트
- **분기별**: 원칙/비전이 여전히 유효한지 재확인
- **신규 기술 도입 시**: (예: Claude Code 의 Agent Teams 정식화) 로드맵 재조정

---

*작성: Round 2 dispatch 중, Phase 1 완료 시점.*
*협업: DEALOS 프로젝트 사용자 + Claude Opus 4.6 (메인 세션).*

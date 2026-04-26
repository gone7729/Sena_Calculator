# CHANGELOG — coord-template

릴리스별 상세 노트. **최신이 위**.

> v0.31+ 부턴 파일별 분리 없이 이 파일에 append.
> GitHub Releases: https://github.com/gone7729/coord-template/releases

---

<!-- ===== v0.47 ===== -->

# coord-template v0.47 — sub spawn 을 background + poll 패턴으로 재설계

## 배경 — DEALOS 진단 (head 자체 보고)

사용자 보고: `@bot retry 2026-04-26-031436` 후 head 가 dispatch 자체를 거부하고 종료. head 자체 분석 결과:

> head 가 plan 작성 후 sub (예: wt/backend) 에 dispatch 하려면 `claude -p "..."` headless 호출로 sub 세션 spawn. 이 호출은 **head 의 Bash 도구로 실행** → Bash 도구 timeout 한계에 걸림.
>
> | 한계 | 값 | 의미 |
> |------|-----|------|
> | Bash timeout | 10분 (max 600000ms) | head 의 Bash 한 번 호출 = sub 가 10분 안에 끝나야 |
> | shell timeout | 30분 | 우리가 inbox.md 에 prescribe |
>
> head 가 두 제약 인지 → "Bash 도구로 30분 기다릴 수 없다" 판단 → **dispatch 자체를 안 함** → verdict: pending 으로 종료.

즉 우리가 inbox.md / dispatch.md 에 `timeout 1800 claude -p "/go" ...` 를 동기 호출로 명시한 게 근본 원인. head 의 Bash 도구는 Anthropic 플랫폼 default 로 max 10분 — sub 가 그 안에 못 끝나면 head 가 보수적으로 dispatch 거부.

## 검토했지만 채택 안 한 대안

**옵션 1**: `BASH_MAX_TIMEOUT_MS` 환경변수 raise — Anthropic hard cap 미지수로 효과 불확실. 시도해도 600000ms 강제될 가능성. 검증 비용 vs 결과 불확실성.

→ 채택 안 함. 대신 정공법인 background + poll 로.

## 수정

### 1. `scripts/spawn-sub-bg.sh` (신규) — background spawn

```bash
bash scripts/spawn-sub-bg.sh <sub-name> <model> [<timeout-sec>]
```

동작:
- `claude -p "/go"` 를 **`nohup` + `&` + `disown`** 으로 백그라운드 실행
- shell `timeout` 으로 hard 한계 같이 걸기 (poll-sub 의 soft kill 외 안전망)
- **즉시 return** (1초 안). stdout 한 줄: `PID=<pid> LOG=<path> START_TS=<unix>`
- head 가 이 출력 파싱해 변수 저장

### 2. `scripts/poll-sub.sh` (신규) — 1회 status check

```bash
bash scripts/poll-sub.sh <pid> <start-ts> [<max-sec>]
```

동작:
- `kill -0 <pid>` 로 프로세스 alive 체크
- timeout 초과 시 SIGTERM → SIGKILL → "TIMEOUT" 반환
- alive: "RUNNING <elapsed>"
- dead: "DONE <elapsed>"
- **즉시 return** (1초 안). head 가 30초마다 별도 Bash 호출로 다시 invoke

### 3. `.claude/commands/inbox.md` 4-4(f) 재작성

**기존 (10분 cap 에 걸림)**:
```bash
timeout 1800 claude -p "/go" \
  --permission-mode bypassPermissions \
  --model "$MODEL" \
  --output-format json \
  > /tmp/wt-<sub>.log 2>&1
```

**v0.47**:
```bash
SPAWN_OUT=$(bash "$COORD_ROOT/scripts/spawn-sub-bg.sh" "<sub>" "$MODEL" 1800)
# 즉시 return — head 의 Bash 도구는 1초 안에 끝남
SUB_PID=$(echo "$SPAWN_OUT" | grep -oE 'PID=[0-9]+' | cut -d= -f2)
# ... LOG, START_TS 추출

# 매 30초마다 head 가 별도 Bash 호출로 polling
bash "$COORD_ROOT/scripts/poll-sub.sh" $SUB_PID $SUB_START 1800
# 한 줄 결과: RUNNING <e> | DONE <e> | TIMEOUT <e>
```

각 Bash 호출이 1초 안이라 10분 cap 무관. 사이사이 `tail -20 $SUB_LOG` 로 진행 확인 가능.

### 4. `.claude/commands/dispatch.md` 동일 패턴 적용

직접 동기 호출 패턴 제거, spawn-sub-bg + poll-sub 으로 통일.

### 5. "직접 동기 호출 금지" 명시

inbox.md 와 dispatch.md 모두 끝부분에 경고 추가:
> `timeout 1800 claude -p "/go" ...` 직접 동기 호출 금지: head Bash 도구 자체가 max 10분 cap 이라 sub 가 30분 걸리면 head 가 못 기다림. 반드시 spawn-sub-bg + poll 사용.

## DONE 후 결과 판정

`claude -p --output-format json` 의 stdout 마지막에 종합 JSON. head 가:
```bash
TAIL=$(tail -1 "$SUB_LOG")
IS_ERROR=$(echo "$TAIL" | python3 -c "import sys,json; d=json.loads(sys.stdin.read()); print(d.get('is_error', False))")
```
True / False 로 성공/실패 판단. 추가로 `$COORD_ROOT/coordination/reports/wt-<sub>.md` 의 `status:` 필드 cross-check.

## Backward compatibility

- 기존 inbox.md / dispatch.md 패턴 (직접 동기 호출) 도 동작은 함 — 단지 10분 cap 에 걸려 dispatch 거부 발생. v0.47 에서 그 한계 제거.
- 신규 헬퍼 스크립트는 `Bash(bash scripts/*)` 권한이 이미 settings.local.json.example 에 있어서 권한 추가 불필요.
- `_PHASE_B_HEAD_COMPLETE_TIMEOUT_SEC` (기본 3600 = 60분) 은 그대로. 단일 sub 30분 + 다른 sub 까지 합산하면 60분 부족할 수 있음 — 환경변수 `COORD_HEAD_COMPLETE_TIMEOUT_SEC` 로 override 가능 (v0.45+).

## DEALOS 적용 절차

```bash
# 1. subtree pull
bash .coord/scripts/upgrade-coord.sh --version v0.47
# 2. 신규 헬퍼 스크립트 권한 확인
ls -l .coord/scripts/spawn-sub-bg.sh .coord/scripts/poll-sub.sh
# 3. 기존 needs-rework plan 으로 retry 재시도
@bot retry <inbox-id>
# → head 가 background spawn → polling → sub 가 실제로 작업
```

## 검증 권장

1. 단일 sub Step 부터 — backend 같이 작업량 큰 sub 가 실제로 30분까지 작업 가능한지
2. polling 사이에 `tail -20 $SUB_LOG` 로 sub 진행 상황 head 가 인지하는지
3. TIMEOUT 분기 — 의도적으로 1800 초과하는 task 줘서 정상 kill 동작 확인
4. 다중 sub Step — 순차 spawn + poll → 모두 정상 처리되는지

## 알려진 한계

- **단일 머신 동시 spawn 제한** — Phase B (`_QUEUE_HEAD_LOCK_WAIT_TIMEOUT_SEC=1800`) 큐 직렬화는 그대로. 같은 head 안에서 sub 들은 순차.
- **PID file 정리** — `${TMPDIR}/wt-<sub>-<ts>.pid` 가 누적될 수 있음. /tmp 는 OS 가 주기적 청소 — Windows 는 명시적 청소 필요할 수 있음 (사용자 환경에서 1주 후 정리 권장).
- **Windows nohup** — Git Bash 의 nohup 지원 OK 확인됨 (MSYS 제공). PowerShell 단독 환경에선 미동작.

---

<!-- ===== v0.46 ===== -->

# coord-template v0.46 — 머지 플로우 전반 정비 (main-merge 자동 back-merge + coord 드롭 옵션)

## 배경 — DEALOS 실사용 진단

사용자가 `cmd_main_merge` 소스 분석 후 보고:

> 현재 로직 (v0.44 까지): git status → fetch → stable checkout → dry-run temp → merge or 중단. coord 파일 구분/제외 로직 일체 없음. **단순 git merge 래퍼**.
> 즉 현재 구조에서 `@bot main-merge` 는 항상 rename/delete 충돌로 중단 — 3dview 의 `.coord/coordination/reports/wt-backend.md` 등 coord 자산이 main 에 없음 → git 이 rename/delete 감지 → 봇은 embed 출력 + 중단.
> 사실상 DEALOS 에서 동작 안 함 (coord 자산 비대칭). **역머지 (main → 3dview) 명령은 미구현**.

또한 중요 포인트: "역머지 단계에서 coord 파일을 처리하는 부분이 미흡. coord 를 쓸 때는 사용자가 흐름을 모두 보고있지 않으니 자동 처리해야 함".

## 수정

### 1. `.gitattributes` merge driver — 방향 무관 자동 보존/드롭

```
.coord/ export-ignore     # v0.41 (git archive 청결)
.coord/** merge=ours      # v0.46 신규
.claude/** merge=ours     # v0.46 신규
```

git 의 내장 `ours` 머지 드라이버 동작: **현재 체크아웃 브랜치 쪽 버전 유지**.

| 체크아웃 | 머지 방향 | `.coord/` 결과 |
|---------|----------|---------------|
| main | main ← work (forward) | main 쪽 유지 (= 없음) → 자동 드롭 ✅ |
| work | work ← main (back-merge) | work 쪽 유지 (= 있음) → 자동 보존 ✅ |

즉 **방향에 상관없이 "work_branch 에만 coord" 상태가 유지됨**. `.gitattributes` 1줄이 근본 해결.

`init.sh` 와 `upgrade-coord.sh` 가 subdir 모드에서 이 설정을 `.gitattributes` 에 자동 등록.

### 2. `@bot main-merge --drop-coord` — 과거 이력 일회성 청소

DEALOS 처럼 이미 main 에 `.coord/` 삭제 이력이 있는 경우, `.gitattributes` 만으론 과거 rename/delete 충돌 복원 불가. `--drop-coord` 플래그로:

```bash
@bot main-merge --drop-coord
```

동작:
```
1. stable_branch 체크아웃
2. git ls-files -- .coord/    # 현재 tracked 목록
3. git rm -r --cached .coord/
4. git commit -m "chore: drop .coord/ from <stable> (coord v0.46 merge hygiene)"
5. git push
(이후 .gitattributes merge=ours 로 재유입 방지)
```

**일회성**. 한번 청소 후엔 `.gitattributes` 가 알아서 유지.

### 3. `cmd_main_merge` 성공 시 자동 back-merge

v0.45 까지: forward merge (work → main) 성공 후 종료. `work_branch` 는 main 최신 상태 아님 → 다음 작업 시 main 과 divergent.

v0.46: forward merge 성공 직후 **자동 back-merge** 체인:
```
Step 1-4. 기존 forward merge 로직
Step 5. (신규) work_branch 체크아웃 + pull
Step 6. git merge origin/<stable_branch> --no-ff --no-commit 안 하는 이유:
        merge=ours 가 .coord/ 자동 처리하므로 conflict 없이 깔끔
Step 7. push work_branch
Step 8. 🔄 역머지 완료 embed (commit 해시)
```

사용자가 수동 개입 없이 `@bot main-merge` 한 번으로 양쪽 브랜치 최신화. `.coord/` 는 `.gitattributes merge=ours` 로 자동 보존.

**실패 시**: forward merge 는 성공이므로 stable 은 이미 완료. back-merge 만 실패 알림 + 수동 실행 안내 (`@bot back-merge <project>`).

### 4. `@bot back-merge` 독립 명령 신설

main 에 직접 hotfix 를 커밋했거나 back-merge 만 필요한 경우:

```bash
@bot back-merge                # 현재 채널 프로젝트
@bot back-merge <project-id>
```

내부적으로 `_back_merge_main_to_work` 헬퍼 재사용 (cmd_main_merge 의 Step 5-8 부분과 동일).

## 신규 헬퍼 함수

- `_drop_coord_from_stable(project_path, remote, stable_branch)` — `--drop-coord` 실행 로직
- `_back_merge_main_to_work(project_path, remote, work_branch, stable_branch)` — 역머지 로직 (main-merge 자동 체인 + cmd_back_merge 공유)

## 전체 흐름 (v0.46+)

```
사용자: @bot send "..."
  → @bot 이 /inbox-send spawn
  → head 가 /inbox (plan + sub dispatch + review-inbox)
  → verdict: go 감지 → Discord ✅/❌ 리액션 prompt
사용자: ✅ 리액션
  → 봇이 /review-inbox --merge-only --auto-yes spawn (work_branch 머지)
  → "🎉 머지 완료" rich embed (v0.45: webhook 중복 제거됨)
사용자: @bot main-merge (또는 리액션 트리거 — v0.48 후보)
  → v0.46: forward merge (work → main, .coord/ 자동 드롭)
  → v0.46: 자동 back-merge (main → work, .coord/ 자동 보존)
  → "🔄 역머지 완료" embed
```

## 마이그레이션 (DEALOS 같은 기존 설치본)

```bash
# 1. 템플릿 업그레이드
bash .coord/scripts/upgrade-coord.sh --version v0.46
#    → .gitattributes 에 merge=ours 자동 추가

# 2. 과거 .coord/ 이력 청소 (일회성)
@bot main-merge --drop-coord
#    → main 의 tracked .coord/* 제거 + push
#    → 이후 forward merge + auto back-merge 까지 자동 진행

# 3. 이후부턴 그냥
@bot main-merge
#    → 자동 drop + forward + auto back-merge
```

## Backward compatibility

- `.gitattributes merge=ours` 는 **신규 추가**. 기존 설치본은 upgrade 가 추가. 수동 머지할 때도 정상 동작.
- `cmd_main_merge` 의 기본 동작 변화: 성공 시 자동 back-merge 가 추가됨. 이전엔 forward 만 했음.
  - back-merge 실패해도 forward 는 이미 성공이라 stable 엔 영향 없음.
  - 단, `work_branch` 상태가 미묘하게 다름 — 이전엔 그대로, v0.46 부턴 main 병합된 상태.
- `cmd_back_merge` 는 신규 명령이라 기존 사용자 영향 0.

## 검증 권장 (DEALOS)

1. `bash .coord/scripts/upgrade-coord.sh --version v0.46` — `.gitattributes` 3줄 추가 확인
2. `@bot main-merge --drop-coord` (일회성) — main 에서 `.coord/*` untrack 확인
3. 이후 `@bot main-merge` — forward + auto back-merge 양쪽 성공 확인
4. `.coord/coordination/inbox/*.md` 가 work_branch 에 유지되는지 확인 (back-merge 후)
5. `@bot back-merge` 독립 호출도 동작 확인

---

<!-- ===== v0.45 ===== -->

# coord-template v0.45 — Phase A timeout 확장 + HEAD_LOCK atomic + 머지 알림 중복 제거

## 배경

DEALOS 실사용 중 사용자 보고 7건:
1. head 타임아웃 감지 3분 → 15분으로 (Phase A)
2. status / 완료 알림 race (타이밍 불일치)
3. 동시 `@bot send` 시 HEAD_LOCK race → 2개 head spawn
4. head 한번에 1건만 처리 (pending loop 없음)
5. verdict: go 여도 자동 머지 안 됨
6. `/fix` 검증 PASS 후 commit 안 함
7. 머지 후 inbox status 자동 갱신 안 됨

v0.45 는 P0 버그 (1, 3) + 알림 중복 제거 Phase 1 에 집중. 나머지는 v0.46~v0.48 로 순차 진행.

## 수정

### 1. Phase A timeout: 180s → 900s (환경변수 override)

`bot/controller.py`:
```python
# Before
_wait_for_file_state(head_lock, want_exists=True, timeout=180, poll=5)

# After
_PHASE_A_HEAD_SPAWN_TIMEOUT_SEC = int(os.environ.get("COORD_HEAD_SPAWN_TIMEOUT_SEC", "900"))
_PHASE_B_HEAD_COMPLETE_TIMEOUT_SEC = int(os.environ.get("COORD_HEAD_COMPLETE_TIMEOUT_SEC", "3600"))
_wait_for_file_state(head_lock, want_exists=True, timeout=_PHASE_A_HEAD_SPAWN_TIMEOUT_SEC, poll=5)
```

실제 DEALOS 관측: plan 작성 + context loading 에 3~7분 걸림 → 180s 는 false positive 대량. 15분이 현실적. 환경변수로 오버라이드 가능해 개별 환경 튜닝 여지 확보.

### 2. HEAD_LOCK atomic acquire (race 제거)

`.claude/commands/inbox-send.md`, `.claude/commands/resolve-escalation.md`:
```bash
# Before (race)
if [[ -f "$LOCK_FILE" ]]; then
  echo "⚠ head 이미 실행 중"
else
  echo "$(date -Iseconds)" > "$LOCK_FILE"   # 두 요청이 동시에 이 시점 도달 가능
fi

# After (atomic)
if ! (set -o noclobber; echo "$(date -Iseconds)" > "$LOCK_FILE") 2>/dev/null; then
  echo "⚠ head 이미 실행 중"
else
  # lock 획득 확정
fi
```

`set -o noclobber` 는 파일 존재 시 redirection 을 atomic 하게 실패시킴. check-then-create 패턴의 race window 가 사라짐.

부수 효과: LOCK_FILE 경로가 subdir 모드 (`$HEAD_WT/.coord/coordination/HEAD_LOCK`) 와 flat (`$HEAD_WT/coordination/HEAD_LOCK`) 둘 다 지원하도록 head-side 커맨드에도 dual-path 로직 추가.

### 3. 머지 완료 알림 중복 제거

사용자 보고: "머지완료 (리액션 버튼 없음)" + "머지완료 (결과요약 + main-merge 유도)" 2개가 동시에 뜸.

**원인**: head-side 의 `notify-discord.sh "🎉 $WORK_BRANCH 머지 완료"` (webhook) + bot-side 의 `_post_merge_summary` (rich embed) 가 같은 채널에 같은 타이밍으로 발송.

**수정**: head-side webhook 제거 (`.claude/commands/review-inbox.md` 섹션 7). bot embed 가 훨씬 정보 풍부 (결과요약 / 다음 단계 / 리뷰 파일 링크 포함) 하므로 통합.

### 4. "✅ head 완료 감지 — verdict 확인 중" 중간 메시지 제거

`bot/controller.py:1001` 의 "head 완료 감지" 메시지는 **head webhook "🎉 완료" + bot "📋 Plan 요약" + bot "✨ self-review 감지"** 3개와 의미가 겹쳐 체감상 중복 소음. 제거.

## 알림 flow — 변경 전/후

### Before (v0.44)
```
📥 신규 inbox
⏱ head 세션 스폰 대기 중
🎉 완료 (head webhook)
✅ head 완료 감지 — verdict 확인 중    ← 제거
📋 Plan 요약
✨ self-review 감지 — verdict: go
(리액션 prompt)
[✅ 리액션 → merge]
🎉 WORK_BRANCH 머지 완료 (head webhook)    ← 제거
🎉 머지 완료 (bot rich embed + 다음 단계)
```

### After (v0.45)
```
📥 신규 inbox
⏱ head 세션 스폰 대기 중
🎉 완료 (head webhook)
📋 Plan 요약
✨ self-review 감지 — verdict: go
(리액션 prompt)
[✅ 리액션 → merge]
🎉 머지 완료 (bot rich embed + 다음 단계)
```

2개 중복 제거. 단계별 흐름 명확해짐.

## 범위 밖 (v0.46+ 로)

| # | 내용 | 예정 버전 |
|---|------|-----------|
| 2 | status/완료 알림 race | 재현 경로 확보 후 별도 (defer) |
| 4 | `/inbox` pending loop 명시화 | v0.46 |
| 5 | verdict: go 자동 work_branch 머지 | v0.47 |
| 6 | `/fix` auto-commit | v0.46 |
| 7 | 머지 후 inbox status 자동 갱신 | v0.47 |
| - | main-merge 리액션 (embed 에 ✅/🕐) | v0.48 |
| - | 단계별 실패 알림 보강 | v0.48 |

## Backward compatibility

- Phase A timeout 은 환경변수 `COORD_HEAD_SPAWN_TIMEOUT_SEC` 없으면 기본 900s. 구버전 봇은 180s 유지 → **신버전 봇 설치 후 15분이 기본**
- HEAD_LOCK atomic 은 head-side 커맨드 변경 (subtree pull 필요). 구버전 커맨드 그대로면 race 취약 상태 유지 — DEALOS 에서 subtree pull v0.45 필수
- 머지 webhook 제거는 head-side 커맨드 변경 → subtree pull 후부터 적용
- 환경변수로 override 가능해 개별 프로젝트 타임아웃 튜닝 여지

## 테스트 (권장)

DEALOS 에서 v0.45 pull 후:
1. `@bot send "test"` → 15분 내 plan 못 끝내도 false positive 안 뜨는지
2. 동시 `@bot send` 2개 → 한 쪽만 spawn, 나머지는 "HEAD_LOCK 대기" 메시지인지
3. 정상 플로우에서 머지 완료 알림이 1개만 오는지 (bot rich embed 만)
4. "✅ head 완료 감지 — verdict 확인 중" 메시지 사라졌는지

---

<!-- ===== v0.44 ===== -->

# coord-template v0.44 — v0.43 범위 확장: `cmd_stop` 도 subdir 경로 대응

## 배경

v0.43 에서 head worktree 의 6군데를 `_head_coord_dir()` 로 수정했으나, **`cmd_stop` 롤백 로직 안에 남아있던 2곳** 은 놓침. 사용자가 v0.43 검토 중 발견.

## 수정 (추가 2곳)

### 1. `cmd_stop` 5-a 롤백 파일 삭제 (line 2254)

```python
# Before (v0.43 까지)
for base in search_dirs:  # [project_path, head_wt]
    coord_sub = base / "coordination"  # ← subdir 모드에선 .coord/coordination 못 찾음

# After (v0.44)
for base in search_dirs:
    coord_sub = _coord_root(base) / "coordination"
```

영향: `@bot stop` 실행 시 subdir 모드에서 plans/tasks/reports/review-inbox 파일이 **삭제되지 않음** → 다음 동명 inbox 시도 시 "중복" 오판 가능.

### 2. `cmd_stop` 5-b inbox status cancelled 마킹 (line 2309)

```python
# Before (v0.43 까지)
inbox_file = project_path / "coordination" / "inbox" / f"{target_inbox_id}.md"

# After (v0.44)
inbox_file = _coord_root(project_path) / "coordination" / "inbox" / f"{target_inbox_id}.md"
```

영향: `@bot stop <id>` 후 inbox status 가 `cancelled` 로 안 바뀜 → 나중에 pending 으로 남아 중복 체크 잘못 처리.

## 전수 검증

```bash
$ grep -n '/ "coordination"' bot/controller.py | grep -v '_coord_root\|coord[ /]\|coord_or_lock'
301: return (path / "coordination" / "config.yml").exists() ...   # _is_coord_installed — 의도적 dual check
452: 기존 코드가 head_wt / "coordination" 만 쓴 탓에 ...           # docstring
3786: coord_dir = coord_root / "coordination"                       # coord_root = _coord_root(project_path)
```

직접 하드코딩은 0건. `_coord_root()` 또는 `_head_coord_dir()` 경유로 통일.

## 사용자 보고 내용 일부 확인

사용자 v0.43 패치 요청 요약에서:
- `_latest_plan_for_retry()` L578-579 → 실제 함수는 `_plan_path()` (동일 효과, 이미 v0.43 에서 수정됨)
- `_post_spawn_orchestration()` L704-705 → 실제 위치는 `_read_verdict()` 였으나 orchestration 이 이 함수 경유하므로 동일 효과 (v0.43 수정)
- `_find_inbox` / L2241-2242 → inbox 탐색은 `_coord_root(project_path)` 이미 사용 중 (L491 / L1492 / L1531 확인). `/retry` 가 plan 못 찾던 건 `_plan_path()` 쪽 문제로 v0.43 에서 해결
- `_find_review_file` L1277-1278 → v0.43 수정

추가로 발견한 2군데 (`cmd_stop` 의 line 2254 / 2309) 가 v0.44 범위.

## Backward compatibility

- flat 모드: `_coord_root(base) == base` → 기존 동작과 동일
- subdir 모드: `_coord_root(base) == base/.coord` → 정상 동작

## 패턴 고정

앞으로 `bot/controller.py` 또는 관련 스크립트에 **새 `/ "coordination"` 참조** 추가 시:
- 항상 `_coord_root(...)` 또는 `_head_coord_dir(...)` 경유
- 직접 `path / "coordination"` 패턴 금지
- 리뷰 시 `grep -n '/ "coordination"' bot/controller.py | grep -v ...` 체크리스트로 사전 확인

---

<!-- ===== v0.43 ===== -->

# coord-template v0.43 — v0.42 hotfix: `@bot review` verdict 못 읽던 버그 수정

## 버그 (사용자 보고)

DEALOS 에서 `@bot review 2026-04-24-122117` 수동 호출 시 **"verdict 를 읽지 못함"** 응답. v0.42 subdir 모드 환경.

## 원인

`bot/controller.py` 에 head worktree 의 coordination 경로를 참조하는 곳 6군데가 모두 **`head_wt / "coordination"` 로 하드코딩**:

```python
# 문제 패턴 (v0.42 까지)
head_wt = project_path.parent / f"{project_id}-wt-head"
head_review_dir = head_wt / "coordination" / "review-inbox"   # ← subdir 모드 무시
```

v0.42 에서 subdir 모드 기본화 이후, head worktree 에도 `.coord/` subtree 가 복사되므로 실제 경로는 `head_wt/.coord/coordination/review-inbox/`. 기존 코드는 `.coord/` 없이 찾다가 파일 없음 → `(None, None)` 반환.

영향 받은 기능:
| 함수 | 영향 |
|------|------|
| `_head_lock_path` | HEAD_LOCK 감지 실패 → `@bot status` / orchestration Phase A 누락 |
| `_plan_path` | plan 파일 못 읽음 → `/retry` 거절 |
| `_read_verdict` | verdict 못 읽음 → `@bot review` / 자동 orchestration Phase F 실패 |
| `_find_review_file` | review 파일 경로 못 찾음 → 리액션 prompt 의 링크 깨짐 |
| `_latest_needs_fix_review` | needs-fix 자동 감지 실패 |
| head_review_dir (summarize_context) | `@bot status` 의 최근 verdict 섹션 누락 |

## 수정

### `_head_coord_dir` 헬퍼 신설

```python
def _head_coord_dir(project_id: str, project_path: Path) -> Path:
    """head worktree 의 coordination 디렉토리 (flat/subdir 자동 감지)."""
    head_wt = project_path.parent / f"{project_id}-wt-head"
    return _coord_root(head_wt) / "coordination"
```

`_coord_root()` 가 이미 flat / subdir 감지 로직을 가지고 있었으므로 head_wt 에도 동일 적용하면 끝.

### 6군데 일괄 교체

```python
# Before (v0.42 까지)
head_wt = project_path.parent / f"{project_id}-wt-head"
head_review_dir = head_wt / "coordination" / "review-inbox"

# After (v0.43)
head_review_dir = _head_coord_dir(project_id, project_path) / "review-inbox"
```

`_find_review_file` 은 head_wt 와 project_path 두 base 를 순회하는데, 각 base 에 `_coord_root()` 적용해 subdir 모드 자동 대응.

## 근본 원인 — v0.35 버그와 동일 패턴 재현

v0.35 의 worktree `.env.local` symlink cp fallback 사고도 같은 뿌리: **worktree 가 독립 PROJECT_ROOT 라는 걸 잊고 flat 경로 하드코딩**. v0.43 에서도 controller.py 가 head worktree 에 `_coord_root()` 적용 안 함.

**교훈**: worktree 관련 경로 계산은 항상 `_coord_root(worktree_path) / "coordination"` 패턴. 하드코딩 `worktree_path / "coordination"` 발견 시 즉시 helper 로 교체.

## Backward compatibility

- flat 모드: `_coord_root(head_wt) == head_wt` 이므로 기존 `head_wt / "coordination"` 와 동일 결과 — 무변경 동작
- subdir 모드: `_coord_root(head_wt) == head_wt/.coord` → 올바른 `.coord/coordination/` 참조

## 검증

```bash
$ py -3.12 -m py_compile bot/controller.py
(syntax OK)

$ grep -n 'head_wt.*"coordination"\|wt-head.*"coordination"' bot/controller.py
452:    기존 코드가 head_wt / "coordination" 만 쓴 탓에 ...
456:    return _coord_root(head_wt) / "coordination"
```

`head_wt / "coordination"` 직접 참조는 `_head_coord_dir` 구현부 1곳만 남음 (의도된 지점). 나머지 호출부는 모두 helper 경유.

## DEALOS 에서 영향

subtree pull v0.43 후:
- `@bot review <inbox-id>` 정상 동작
- `@bot status` 에 HEAD_LOCK / 최근 verdict 정상 표시
- 자동 orchestration Phase F (verdict 감지 → 리액션 prompt) 정상 작동

---

<!-- ===== v0.42 ===== -->

# coord-template v0.42 — head-side commands 4종 정식 편입 + subdir 경로 정합

## 배경 — `f2ddc04` 초기 추출 때 누락된 것

DEALOS 에서 v0.37~v0.41 동안 `@bot send` 가 "Unknown command: /inbox" 에러로 실패한 원인 추적 중 발견: `.claude/commands/` 에 **head-side 슬래시 커맨드 4종이 coord-template 에 존재한 적 없음**.

```bash
$ git log --all --oneline --diff-filter=AD -- '.claude/commands/{inbox,dispatch,plan,retry}.md'
(no output — 추가/삭제 이력 전무)
```

`f2ddc04 feat: initial extract from DEALOS coordination system` 시점에 op-side 명령만 (`inbox-send`, `review-inbox`, `fix`, `status`, `token-status`, `resolve-escalation`, `reject-escalation`) 추출됐고 head-side 4종은 DEALOS 로컬에만 존재. 님(사용자)이 DEALOS 1명만 쓰던 상황이라 **coord-template 자체로 설치한 제3 프로젝트가 없어서 v0.42 까지 생존**.

## 추가 — 4개 head commands 정식 편입

`.claude/commands/` 에 추가 (frontmatter + 일반화 완료):

### `.claude/commands/inbox.md` (HEAD)
원격 inbox 엔트리 → 다단계 plan → Step dispatch → review-inbox.  본격 파이프라인. Upstream pull, STOP 확인, escalation + approval 동기화, Plan 작성, Step 실행 루프, Head 자체 검증, review-inbox 작성까지.

### `.claude/commands/dispatch.md` (HEAD)
지정된 sub worktree 들 headless 순차 실행. 모델 / 타임아웃은 config.yml 의 `subs[]` 참조.

### `.claude/commands/plan.md` (HEAD)
사용자 요청 분해해 sub task 작성 (inbox 파일 없이). `/inbox` 은 원격 엔트리 기반 전체 파이프라인, `/plan` 은 task 분해만.

### `.claude/commands/retry.md` (HEAD)
`needs-rework` plan 재시도 (최대 2회). 실패 sub 는 `failed/wt-<sub>-retry-<ts>` 브랜치 보관 후 reset.

### 일반화 내용
DEALOS 고유 값 제거:

| 원본 (DEALOS) | 일반화 |
|---|---|
| `dealos-wt-head` | `$HEAD_WT` (config.sh 에서 유도) |
| `../dealos-wt-<sub>` | `$SIBLING_BASE/${PROJECT_NAME}-wt-<sub>` |
| `3dview` (work_branch) | `$WORK_BRANCH` (config.sh) |
| `coordination/` | `$COORD_ROOT/coordination/` (flat / subdir 자동 대응) |
| `claude-opus-4-6` / `claude-sonnet-4-6` | `config.yml` 의 `subs[].model` 참조 (yaml 파싱 또는 `sub_model` 헬퍼) |
| sub 이름 `db\|backend\|frontend` 하드코딩 | `wt-[a-z]+` 또는 `config.yml` 동적 조회 |

모든 파일에 frontmatter 추가:
```yaml
---
description: "..."
requires-wt: head       # 잘못된 worktree 에서 실행 시 Claude Code 가 안내
min-coord-version: v0.42
---
```

## 수정 — subdir 경로 정합

### 1. `.githooks/pre-commit` regex 업데이트

v0.41 subdir 모드에서 `.coord/coordination/` 가 COMMON regex 에 매칭 안 돼 **"out of scope" 오판**. 수정:

```bash
# v0.42+: .coord/ prefix 허용
COORD_PREFIX_PAT='(\.coord/)?'
COMMON="^${COORD_PREFIX_PAT}(coordination/|\.claude/|\.githooks/|scripts/|templates/|docs/|\.env\.local\.example)|^(\.gitignore|\.gitattributes|CLAUDE\.md|README\.md|VERSION)\$"
```

CONFIG_YML 경로도 flat / subdir 둘 다 시도하도록 fallback 추가.

### 2. `scripts/lib/classify.sh` 정규화

`upgrade-coord.sh` 가 classify_path 를 호출할 때 `.coord/coordination/...` 가 들어오면 flat 패턴 (`coordination/...`) 에 매칭 안 돼 분류 실패. 수정:

```bash
# 호출자가 .coord/... 를 넘겨도 flat 패턴으로 매칭되도록 정규화
local normalized="${path#.coord/}"
[[ "$normalized" != "$path" ]] && path="$normalized"
```

## 추가 — CLAUDE.md §3.1.1 "추출 완전성 체크리스트"

v0.37~v0.42 의 시리즈 사고는 근본적으로 **"초기 추출이 불완전한 채로 시작됐고 1인 사용자라 발견 지연"** 이 원인. 재발 방지 체크리스트 신설:

- `.claude/commands/` op-side + head-side 양쪽 확인
- `scripts/config.sh`, `config.yml.example`, `.githooks/pre-commit`, `scripts/lib/classify.sh`, `scripts/notify-discord.sh`, `coordination/roles/` 각각 점검
- **smoke test 필수** — subdir 모드 새 프로젝트에서 end-to-end 1회 실행 (`@bot send → inbox-send → /inbox → plan → /dispatch → sub → /review-inbox`)

## DEALOS 에서 영향

- subtree pull 로 4개 새 파일이 `.coord/.claude/commands/` 에 복사됨
- head worktree 에서 `.claude/commands/` 에 접근 가능하려면 각 worktree 가 `.claude -> .coord/.claude` symlink 필요 (setup-worktree.sh 가 이미 main worktree 에만 생성 — **worktree 별 symlink 는 별도 설정**. 알려진 한계, v0.43 후보)
- pre-commit hook 이 subdir 경로를 인식하므로 `.coord/coordination/inbox/` 등 커밋이 범위 내로 정상 판정 → head 가 `--no-verify` 우회 불필요

## 알려진 한계 (v0.43+ 후보)

**worktree 별 `.claude/` 부재** — `init.sh` 는 main worktree 에만 `.claude -> .coord/.claude` symlink 를 만든다. head worktree 생성 시 `setup-worktree.sh` 도 같은 symlink 를 만들어야 Claude Code 가 head 에서 `/inbox` 등을 로드 가능. 현재 DEALOS 는 수동으로 해결 중.

v0.43 에서 setup-worktree.sh 에 `.claude` symlink 생성 추가 예정 (cp fallback 없이 symlink 만, v0.35 memory feedback 원칙 준수).

## 교훈

1. **초기 추출은 구조 정규 점검 + smoke test 가 필수**. "op 쪽만 외부 노출" 류 편견이 큰 누락을 만들 수 있음.
2. **1인 사용자 환경은 버그 생존 위험**. 제3 프로젝트 adoption 이 지연되면 같은 가정이 오래 들키지 않음.
3. **CLAUDE.md 체크리스트로 패턴 고정**. v0.37 (gitignore 폴더 차원), v0.38 (symlink cp fallback), v0.40 (bash set-e), v0.42 (추출 완전성) 모두 CLAUDE.md 에 전이되어 재발 방지.

---

<!-- ===== v0.41 ===== -->

# coord-template v0.41 — `.coord/` 통째 gitignore 반전 (workflow 복원)

## 버그 (사용자 보고, critical)

DEALOS 에서 `@bot send` → `/inbox-send` skill 실행 → inbox 파일 생성됐으나 **head 가 spawn 안 됨**. 원인 체인:

1. `/inbox-send` 가 `.coord/coordination/inbox/2026-04-23-183850.md` 생성 (1778 bytes, working tree only)
2. v0.37+ 의 `.gitignore` 에 `.coord/` 통째 entry → 신규 inbox 파일이 **git 에 안 올라감**
3. `/inbox-send` 스킬의 commit/push 단계 skip (gitignored 감지)
4. head 가 origin polling 으로 새 inbox 감지 → 변화 없음 → **spawn trigger 부재**
5. HEAD_LOCK 없음, 봇 살아있으나 할 일이 없는 상태

**근본 모순**: v0.37 "`.coord/` 통째 비노출" 정책 vs workflow 가 `.coord/coordination/*` git sync 로 통신하는 메커니즘. 두 정책이 양립 불가.

## 설계 오류 인정 — v0.37 의 잘못된 전제

v0.37 은 `.coord/` 를 single concept ("coord utility") 로 보고 통째 gitignore 했으나, 실제로는 두 종류가 섞여있었음:

- **Template code** (scripts/bot/.claude/templates/docs) — subtree 로 tracked, workflow 무관
- **Workflow data** (coordination/inbox/plans/tasks/reports/review-inbox/escalations) — **workflow 통신 매체**, git sync 필수

"utility 숨김" 욕구가 **data 동기화 채널까지 끊어버림**. 영향 받는 전 workflow:

| 단계 | 파일 | 통신 경로 |
|------|------|----------|
| inbox-send | `.coord/coordination/inbox/*.md` | op → origin → head pull |
| plan 작성 | `.coord/coordination/plans/*.md` | head → origin → op review |
| sub 위임 | `.coord/coordination/tasks/*.md` | head → origin → sub (wt/head branch) |
| sub 완료 | `.coord/coordination/reports/*.md` | sub → origin → head |
| review-inbox | `.coord/coordination/review-inbox/*.md` | head → origin → op |
| 에스컬레이션 | `.coord/coordination/escalations/*.md` | judge → origin → op |

전부 git 기반. v0.37~v0.40 동안 workflow 자체가 죽어있었음.

## 수정 — runtime + secrets only, 배포 청결은 `.gitattributes` 로

### 1. `scripts/init.sh` GITIGNORE_ENTRIES 재작성 (subdir 모드)

```bash
# v0.37~v0.40 (잘못됨 — workflow 파괴)
".coord/"
".claude/"

# v0.41+ (구체 파일만)
".coord/coordination/HEAD_LOCK"
".coord/coordination/STOP"
".coord/coordination/dashboard-url.txt"
".coord/.env.local"
".coord/bot/projects.yml"
".coord/bot/usage-alerts-state.json"
".coord/.claude/settings.local.json"
# + .coord-backup/, .env.local, .env.local.wt, run-dev.sh, coord (wrapper)
```

### 2. `scripts/upgrade-coord.sh` — stale entry 자동 제거

기존 설치본 업그레이드 시 `.gitignore` 에 남아있는 `.coord/` / `.claude/` 통째 entry 를 자동 감지·삭제:

```bash
v0.37~v0.40 stale entry N 개 제거 (workflow 복원)
```

### 3. `.gitattributes` — 배포 tarball 청결 분리

```
.coord/ export-ignore
```

→ `git clone` / `git log` / `git diff` 에는 coord 보임 (workflow 동작), `git archive` / release tarball 은 `.coord/` 제외. v0.37 의 "배포 청결" 의도를 **정확한 레이어** 에서 실현.

init.sh 와 upgrade-coord.sh 양쪽에서 `.gitattributes` 에 자동 등록.

### 4. CLAUDE.md §6 재작성

- 새 원칙: "runtime + secrets only, 배포 청결은 export-ignore"
- v0.37~v0.40 실패 회고 (CHANGELOG 에 상세, CLAUDE.md 에 요약)
- "폴더 차원 gitignore 는 내부 파일 역할 섞여있을 때 위험" 명시

### 5. README.md Quick Start 업데이트

- "왜 subdir 모드" 섹션 — "배포 비노출" 문구 제거, `export-ignore` 메커니즘 설명
- 폴더 구조 그림에서 `.coord/` 를 tracked 로 표시

## 마이그레이션 (v0.37~v0.40 설치본 → v0.41)

```bash
# A. upgrade-coord.sh 로 자동 (권장)
bash .coord/scripts/upgrade-coord.sh --version v0.41
#   → stale .coord/ / .claude/ entry 제거
#   → 개별 runtime/secret entry 추가
#   → .gitattributes 에 export-ignore 등록

# B. coord-template 이 아직 v0.41 전이거나 수동 복구 시:
cd my-project
# .gitignore 에서 .coord/ 와 .claude/ 줄 제거
sed -i.bak '/^\.coord\/$/d; /^\.claude\/$/d' .gitignore
# 개별 entry 추가 (init.sh 가 idempotent 하게 넣어줄 것)
bash .coord/scripts/init.sh  # 기존 설치 감지 → gitignore 만 갱신
# 이미 working tree 에 있던 inbox 등을 git 에 올리기:
git add .coord/coordination/
git commit -m "chore: restore coord tracking (v0.41 workflow fix)"
git push origin <your-work-branch>
#   → head 가 origin 변화 감지 → inbox-send workflow 재개
```

## Backward compatibility

- **v0.36 이하 설치본**: 이미 `.coord/` 통째 ignore 없이 개별 ignore 방식이라 **무변경 동작**.
- **v0.37~v0.40 설치본**: upgrade-coord.sh 가 stale entry 제거 → workflow 복원. 단, 이미 gitignored 된 `.coord/coordination/*` 파일들은 **수동 `git add`** 후 commit/push 필요 (script 가 자동 add 까지는 안 함 — 사용자 의도 불명확 파일 commit 은 위험).

## 교훈

1. **폴더 차원 정책은 내부 파일 역할 확인 필수**. `.coord/` 를 single concept 으로 본 것이 오류의 뿌리.
2. **workflow / mechanism 영향 테스트를 design 단계 필수 체크리스트**로. v0.37 릴리스 전 `/inbox-send` 한 번만 실제 돌렸어도 즉시 잡혔을 버그.
3. **"배포 청결" 같은 최종 목표는 해당 레이어에만 국한**. `.gitattributes` / CI / release script 단계에서. 개발 단계(git log / git clone) 까지 끌어오려 하면 workflow 에 영향.
4. **회고를 CHANGELOG 에 명시**해 다음 충동 때 상기. v0.37~v0.41 4 개 버전에 걸친 잘못된 방향 기록.

---

<!-- ===== v0.40 ===== -->

# coord-template v0.40 — v0.39 hotfix: `((var++))` + `set -euo pipefail` 충돌 수정

## 버그 (사용자 보고)

`init.sh --restore-configs` 가 첫 skip 후 즉시 종료. 원인:

```bash
set -euo pipefail
...
SKIPPED=0
((SKIPPED++))     # ← post-increment 이전값 0 반환 → exit code 1 → set -e 종료
```

bash 의 `((expr))` 는 **산술 결과가 0 이면 exit status 1**. `SKIPPED=0` 일 때 `SKIPPED++` 가 먼저 0 을 반환하고 (그 다음 1 증가) → `set -e` 가 스크립트 종료시킴.

같은 패턴이 `scripts/relink-worktree-env.sh` (v0.35) 에도 10여 군데 존재 — 동시 수정.

## 수정

`$((var+1))` 할당 형태로 전환 — 항상 exit 0 반환 (할당 성공).

```bash
# Before (버그)
((SKIPPED++))

# After (v0.40)
SKIPPED=$((SKIPPED+1))
```

### 수정 파일
- `scripts/init.sh`: 2곳 (`--restore-configs` 의 `RESTORED`, `SKIPPED`)
- `scripts/relink-worktree-env.sh`: 10곳 (`SKIPPED_ALREADY`, `CREATED`, `CONVERTED`, `FAILED`, `SKIPPED_DIFF`)

`grep -rn '((.*++))' scripts/*.sh` 로 전수 검사 → 수정 후 match 없음 확인.

## 추가 — `--restore-configs` 복원 대상 확장

사용자 지적: `coordination/hotfixes.md.example` / `token-budget.md.example` 이 존재하는데 v0.39 복원 목록에서 누락.

v0.40 추가:
- `coordination/hotfixes.md`
- `coordination/token-budget.md`

전체 복원 대상 (6개):
1. `$PROJECT_ROOT/.env.local`
2. `$COORD_ROOT/bot/projects.yml`
3. `$COORD_ROOT/coordination/config.yml`
4. `$COORD_ROOT/coordination/hotfixes.md` ← 추가
5. `$COORD_ROOT/coordination/token-budget.md` ← 추가
6. `$CLAUDE_DIR/settings.local.json`

## CLAUDE.md §2.1 에 bash 관용 주의 추가

같은 함정 재발 방지용:

```
bash 관용 주의 (v0.40 사고 후):
- set -euo pipefail 하에서 ((var++)) 금지 — post-increment 이전값 0 반환 → set -e 종료.
  항상 var=$((var+1)) 할당 형태 사용. 이 함정은 bash -n 으로 감지 안 됨 (런타임 behavior).
- 배열 선언 + set -u 조합 주의.
- 의심되면 grep -n '((.*++))' scripts/*.sh 로 사전 감지.
```

`bash -n` / `shellcheck` 도 이 패턴을 잡아주지 못한다. 런타임에만 드러나는 함정 — 체크리스트로 방어.

## 교훈

v0.35 에 이미 있던 버그(relink-worktree-env.sh)가 v0.40 까지 발현 안 된 이유: 사용자가 `relink-worktree-env.sh` 를 **실제로 돌린 적이 없어서**. DEALOS 에서 v0.35 릴리스 직후 symlink 전환은 수동 sed 명령으로 진행, 스크립트는 사용 안 함.

→ **문서화만 되고 실사용 안 된 스크립트는 런타임 버그가 오래 생존한다**. 향후 신규 스크립트는 최소 1회 end-to-end 실행 smoke test 를 릴리스 gate 로 추가 고려.

---

<!-- ===== v0.39 ===== -->

# coord-template v0.39 — v0.38 `.coord/.env.local` 반전 + restore-configs + root wrapper

## 배경 — v0.38 회고

v0.38 에서 subdir 모드의 `.env.local` 기본 위치를 `.coord/.env.local` 로 이동시켰다. 의도: Node.js / Next.js 프로젝트가 자체 `.env.local` 을 쓰는 경우 coord secrets 와 격리.

**실제 결과** (DEALOS 에서 재현):
1. Windows Git Bash 에서 `ln -s` 실패 → cp fallback 동작
2. 사용자가 관습대로 `PROJECT_ROOT/.env.local` 을 편집 → `.coord/.env.local` (cp) 은 옛 값 유지
3. `notify-discord.sh` 등 스크립트가 `.coord/.env.local` 을 먼저 읽음 → 옛 Discord 채널로 알림 발송
4. **v0.35 worktree `.env.local` 사고와 동일 패턴 재발**

즉 "cp fallback 을 허용한 symlink 패턴" 자체가 stale 을 필연적으로 만들어낸다. Windows 환경에서 symlink 가 보장되지 않으므로.

## 변경

### 1. `config.sh` — `ENV_FILE_RESOLVED` 순서 반전

```bash
# v0.38 (문제)                   # v0.39 (수정)
if [[ -f "$COORD_ROOT/.env.local" ]]; then       if [[ -f "$PROJECT_ROOT/.env.local" ]]; then
  ENV_FILE_RESOLVED="$COORD_ROOT/..."              ENV_FILE_RESOLVED="$PROJECT_ROOT/..."
else                                             elif [[ -f "$COORD_ROOT/.env.local" ]]; then
  ENV_FILE_RESOLVED="$PROJECT_ROOT/..."            ENV_FILE_RESOLVED="$COORD_ROOT/..."
fi                                               else
                                                   ENV_FILE_RESOLVED="$PROJECT_ROOT/..."
                                                 fi
```

이제 `PROJECT_ROOT/.env.local` 이 primary — 관습 + 단일 source of truth. `.coord/.env.local` 은 **opt-in isolation** 전용.

### 2. `init.sh` — `.env.local` 을 PROJECT_ROOT 에 생성 (반전)

v0.38 에서 subdir 모드면 `.coord/.env.local` 에 생성했으나 v0.39 부터는 다시 `PROJECT_ROOT/.env.local` 로 통일.

v0.38 잔재 감지 로직 추가 — `.coord/.env.local` 에만 파일 있고 `PROJECT_ROOT` 비어있으면 마이그레이션 안내 출력 (자동 이동은 안 함).

### 3. `scripts/link-env-to-coord.sh` (신규) — opt-in isolation

프로젝트가 자체 `.env.local` 을 쓰는 경우 (Next.js 등) coord secrets 를 `.coord/.env.local` 로 격리하고 싶을 때 **명시적으로** 호출:

```bash
bash .coord/scripts/link-env-to-coord.sh           # 기본
bash .coord/scripts/link-env-to-coord.sh --force   # 기존 파일 덮어쓰기
```

**핵심: cp fallback 제공 안 함**. symlink 실패 시 명확히 실패 메시지 + 개발자 모드 안내. stale 위험을 원천 차단.

### 4. `init.sh --restore-configs` 신규 플래그

`git checkout` / `git subtree pull` 이후 user-generated 설정 파일이 사라졌을 때 `.example` 에서 복구. 대화식 init 흐름 (worktree 생성 등) 건너뜀.

```bash
bash .coord/scripts/init.sh --restore-configs
```

복원 대상:
- `$PROJECT_ROOT/.env.local` ← `.env.local.example`
- `$COORD_ROOT/bot/projects.yml` ← `bot/projects.yml.example`
- `$COORD_ROOT/coordination/config.yml` ← `coordination/config.yml.example`
- `$PROJECT_ROOT/.claude/settings.local.json` ← `.example`

이미 존재하면 skip (덮어쓰지 않음).

### 5. root wrapper `./coord <script>` (subdir 모드, 선택 생성)

v0.37 이후 `bash .coord/scripts/<script>.sh` 매번 입력이 번거로웠음. init.sh 마지막에 선택 프롬프트:

```bash
root wrapper 생성? (PROJECT_ROOT 에서 './coord <script>' 호출용) [Y/n]
```

생성 시 `$PROJECT_ROOT/coord` (gitignored, 실행가능) 이 만들어지고:
- `PROJECT_ROOT/.env.local` 자동 source
- `.coord/scripts/$1` 에 나머지 인자 전달

```bash
./coord start-bot.sh --bg       # vs. bash .coord/scripts/start-bot.sh --bg
./coord status.sh
./coord install-deps-mac.sh --yes
./coord                         # 사용 가능한 스크립트 목록
```

gitignore 에 `coord` 항목 자동 추가.

## 마이그레이션 (v0.38 → v0.39)

### 시나리오 A — v0.38 `.coord/.env.local` 쓰던 사용자
```bash
# 옵션 1: PROJECT_ROOT 로 원복 (권장)
mv .coord/.env.local .env.local

# 옵션 2: isolation 유지 (symlink)
mv .coord/.env.local .env.local
bash .coord/scripts/link-env-to-coord.sh
```

### 시나리오 B — `.coord/bot/projects.yml` 등 사라진 상태
```bash
bash .coord/scripts/init.sh --restore-configs
# → .example 에서 복원 후 실제 값 편집
```

### 시나리오 C — wrapper 쓰고 싶음
```bash
bash .coord/scripts/init.sh   # 프롬프트에서 y 선택
# 이후 ./coord <script> 로 호출
```

## Backward compatibility

- v0.38 `.coord/.env.local` 은 여전히 fallback 으로 읽힘 — 무변경 동작
- v0.37 이하 (PROJECT_ROOT/.env.local 만 있음) — v0.39 primary 경로라 무변경 동작
- root wrapper 미생성 시 기존 `bash .coord/scripts/...` 호출 방식 그대로

## 교훈 (CHANGELOG 에 명시)

- **symlink + cp fallback 은 환경 차이로 stale 을 만들어낸다.** Windows Git Bash 처럼 symlink 가 조건부로만 되는 환경에선 cp 로 떨어지는 순간 "동기화 안 됨" 에 무감각해진다.
- **해결책은 "symlink 실패 시 명시적 에러" + "primary 경로 단일화"**. `link-env-to-coord.sh` 가 cp 를 제공하지 않는 건 의도적.
- 다음번 "두 파일을 동기화" 욕구가 들면 이 v0.38 사고 기록을 먼저 떠올릴 것.

---

<!-- ===== v0.38 ===== -->

# coord-template v0.38 — `.env.local` 을 `.coord/.env.local` 로 이동 가능

## 배경

v0.37 로 `.coord/` 안에 coord 파일을 통합했지만 `.env.local` 은 관습적으로 프로젝트 루트에 남겨뒀다. 하지만 프로젝트 자체가 Node.js / Next.js 로 자체 `.env.local` 을 쓰는 경우:
- 두 `.env.local` 이 같은 위치에서 공존 불가 → coord 쪽을 프로젝트 env 에 섞어야 함
- coord secrets (DISCORD_WEBHOOK_URL 등) 이 프로젝트 env 와 뒤섞여 관리 어려움

해결: **coord secrets 전용 위치 `.coord/.env.local` 추가**. 프로젝트 자체 env 와 완전 분리.

## 추가

### 1. `scripts/config.sh` — `ENV_FILE_RESOLVED` 헬퍼

```bash
# v0.38+: .coord/.env.local 우선, PROJECT_ROOT/.env.local fallback
if [[ -f "$COORD_ROOT/.env.local" ]]; then
  ENV_FILE_RESOLVED="$COORD_ROOT/.env.local"
else
  ENV_FILE_RESOLVED="$PROJECT_ROOT/.env.local"
fi
export ENV_FILE_RESOLVED
```

### 2. 스크립트들이 `$ENV_FILE_RESOLVED` 사용

- `scripts/notify-discord.sh` — webhook URL 로드
- `scripts/start-bot.sh` — DISCORD_BOT_TOKEN 로드 (부수적으로 subdir 모드 버그 수정: v0.37 에선 cwd=.coord/ 라 `./.env.local` 을 못 찾던 상태)
- `scripts/init.sh` — subdir 모드면 `.coord/.env.local` 에 생성
- `scripts/setup-worktree.sh` — worktree symlink source 로 사용
- `scripts/relink-worktree-env.sh` — main env 로 사용

config.sh 미로드 상태 (bot 직접 호출 등) 대비 각 스크립트에 fallback 로직 포함.

### 3. `init.sh` 마이그레이션 힌트

구버전 경로(`$PROJECT_ROOT/.env.local`)에 파일이 있으면 경고만 출력, **자동 이동 안 함**:

```
⚠ 구버전 경로에 .env.local 존재: /path/to/project/.env.local
  v0.38+ 권장 경로: /path/to/project/.coord/.env.local
  이동하려면: mv "/path/to/project/.env.local" "/path/to/project/.coord/.env.local"
  (이동 안 해도 scripts 는 구버전 경로 fallback 으로 계속 동작)
```

사용자가 원할 때만 이동. 구버전 설치본 regression 방지.

## 부가 효과 — `start-bot.sh` subdir 모드 버그 수정

v0.37 의 `start-bot.sh` 는 `cd "$SCRIPT_DIR/.."` 로 cwd 를 `.coord/` 로 옮긴 뒤 `./.env.local` 을 source 했음. subdir 모드에선 `.env.local` 이 `.coord/` 밖 (PROJECT_ROOT) 에 있어서 **로드 실패**. v0.38 부터는 `ENV_FILE_RESOLVED` 절대경로 사용으로 확실히 해결.

## 사용 시나리오

### 시나리오 A — 프로젝트가 자체 `.env.local` 을 씀 (Next.js 등)
```
project/
├── .env.local              ← 프로젝트 자체 (NEXT_PUBLIC_* 등)
├── .coord/
│   └── .env.local          ← coord 전용 (DISCORD_*, GITHUB_REPO_URL 등)
└── ...
```
스크립트는 `.coord/.env.local` 을 사용 — 프로젝트 env 오염 없음.

### 시나리오 B — 프로젝트가 자체 `.env.local` 을 안 씀
```
project/
├── .coord/
│   └── .env.local          ← coord 만
└── ...
```
프로젝트 루트에 `.env.local` 없음. 깔끔.

### 시나리오 C — 구버전 설치본 (마이그레이션 전)
```
project/
├── .env.local              ← 기존 coord secrets
├── .coord/
│   └── (.env.local 없음)
└── ...
```
`ENV_FILE_RESOLVED` 가 fallback 으로 `$PROJECT_ROOT/.env.local` 사용 → 동작 유지. 사용자가 원할 때 이동.

## Backward compatibility

- **v0.37 이하 설치본**: `.coord/.env.local` 이 없으므로 `ENV_FILE_RESOLVED` fallback 으로 기존 경로 사용 → **무변경 동작**
- **새 install (v0.38+)**: subdir 모드면 `.coord/.env.local` 에 자동 생성
- **마이그레이션**: 단일 `mv` 명령 (경고에 표시됨). upgrade-coord.sh 가 자동으로 옮기진 않음 — 사용자 판단

---

<!-- ===== v0.37 ===== -->

# coord-template v0.37 — subdir 모드 기본 승격 + coord 배포 비노출

## 배경

원칙: **coord 는 사용자 작업 utility 이지 제품 코드가 아니다.** 따라서 소비 프로젝트의 배포 브랜치에 coord 흔적이 없어야 한다.

v0.36 이전 상태:
- flat 설치본: `coordination/`, `bot/`, `scripts/`, `.claude/` 등이 프로젝트 루트에 **tracked** 로 커밋됨
- subdir 설치본: `.coord/` 안으로 묶이긴 했지만 **여전히 tracked** (`.gitignore` 에 최소 항목만)
- 결과: 팀 협업자가 clone 해도 coord 파일이 따라옴, `git log main` 에 coord 커밋 섞임, release tarball 에 포함됨

사용자 의도: "팀원과 coord 공유 불필요 — 각자 로컬 utility". → **방안 A (완전 gitignore)** 로 확정.

## 변경

### 1. `init.sh` — subdir 모드에서 `.coord/`, `.claude/` 전체 gitignore

`GITIGNORE_ENTRIES` 확장:
```
# v0.36 이전 (subdir 모드)
".coord/coordination/HEAD_LOCK"
".coord/coordination/dashboard-url.txt"
".coord/coordination/STOP"
".env.local", ".env.local.wt", ".coord-backup/", "run-dev.sh"
".claude/settings.local.json"

# v0.37+ (subdir 모드)
".coord/"              ← 전체 ignore
".claude/"             ← 전체 ignore (symlink 도 포함)
".coord-backup/"
".env.local"
".env.local.wt"
"run-dev.sh"
```

### 2. `init.sh` — `.claude/` cp → symlink (v0.35 패턴 재사용)

subdir 모드에서 `PROJECT_ROOT/.claude` 를 `COORD_ROOT/.claude` 로 `ln -s`. Windows 에서 symlink 실패 시 cp fallback. 장점: `.coord/.claude/` (source of truth) 수정이 즉시 반영.

### 3. `init.sh` — flat 모드 deprecated 경고

PROJECT_ROOT 이름이 `coord-template` 이 아니면 (= 일반 소비 프로젝트) 경고 출력:
```
⚠ flat 모드는 v0.37 부터 deprecated 입니다.
  소비 프로젝트는 subdir 모드 (.coord/ subtree) 를 권장합니다 —
  coord 파일이 배포 브랜치에 노출되지 않고 깔끔하게 분리됩니다.
```
기능은 유지 — coord-template repo 본체 개발용.

### 4. `upgrade-coord.sh` — 업그레이드 시 gitignore 재적용

신규 step 9.5 추가:
- subdir 모드 감지 시 `.gitignore` 를 v0.37 기준으로 재적용
- 이미 tracked 된 `.coord/` / `.claude/` 가 있으면 경고 + untrack 명령 안내 (자동 실행은 안 함 — 사용자 판단)
- dry-run 지원

### 5. `README.md` Quick Start 재배치

- **A. subtree 설치 (권장)** — 먼저 제시, 결과 폴더 구조 그림 포함
- **B. Legacy flat 설치 (deprecated)** — coord-template repo 본체 전용이라 명시
- "왜 subdir 모드" 섹션 신설 — 배포 비노출 / 팀 공유 불필요 / 폴더 통합 3가지 이유

### 6. `CLAUDE.md` §6 "설치 모드 정책" 신설

- 원칙 / 폴더 구조 / 자동 gitignore / deprecated 경고 / 마이그레이션 절차를 단일 섹션에 명시.

## 기존 설치본 업그레이드 절차

flat 모드 → subdir 전환 (권장):
1. 기존 coord 폴더 백업
2. `git rm -rf coordination/ bot/ scripts/ .claude/ .githooks/ templates/ docs/` + commit
3. `git subtree add --prefix=.coord ... v0.37 --squash`
4. `bash .coord/scripts/init.sh --mode=subdir` (.gitignore + symlink 자동)
5. 사용자 설정 (`config.yml`, `projects.yml`) 을 `.coord/` 하위로 복사
6. `git config core.hooksPath .coord/.githooks` 확인

subdir 모드 유지 (단순 업그레이드):
```
bash .coord/scripts/upgrade-coord.sh --version v0.37
# → step 9.5 에서 .gitignore 자동 갱신
# → tracked 상태면 경고 + untrack 명령 안내
```

## 유지 (의도적)

- **`.env.local`** 은 최상위 유지 — Node.js / Next.js 등이 프로젝트 루트에서 읽는 관습. `.coord/` 로 이동 시 프로젝트 고유 환경변수와 충돌 위험.
- **flat 모드 코드** — 한 버전 유예. coord-template repo 본체는 flat 로 계속 개발.
- **자동 flat→subdir 마이그레이션 스크립트** — 수동 안내로 충분 (DEALOS 외 알려진 소비자 없음).

---

<!-- ===== v0.36 ===== -->

# coord-template v0.36 — Opus 4.6 → 4.7 승급 + 모델 승급 절차 문서화

## 승급 대상

config 에 고정 박힌 `claude-opus-4-6` 을 `claude-opus-4-7` 로 일괄 교체. Sonnet 4.6 / Haiku 4.5 는 현재 최신이라 유지.

**변경 파일** (총 6개 활성 문서):
- `coordination/config.yml.example` — head / backend sub (2곳)
- `scripts/init.sh` — 새 프로젝트 init 시 생성되는 config.yml 템플릿 (2곳)
- `.env.local.example` — `CLAUDE_HEAD_MODEL` 주석 예시
- `.claude/commands/inbox-send.md` — `${HEAD_MODEL:-...}` fallback
- `.claude/commands/resolve-escalation.md` — 동일 fallback
- `coordination/ROADMAP.md` — 샘플 config 블록

**의도적으로 유지**:
- `CHANGELOG.md` — 과거 릴리스 노트 (역사 보존)
- `bot/controller.py:3134` — 축약 regex 설명 주석 (`claude-opus-4-6 → opus` 는 illustrative, regex 는 4-7 도 매칭)
- `bot/controller.py` 의 Option C SDK chat_model 기본값 — Sonnet 이라 Opus 승급과 무관

## CLAUDE.md §5 추가 — 모델 승급 절차

자동화 없이 매번 수동 교체하는 방식이라, 재발 시 기준이 될 체크리스트 문서화:
1. 승급 대상 파일 grep 기준
2. CHANGELOG 섹션 작성 템플릿
3. **과거 CHANGELOG 변경 금지** 규칙
4. DEALOS 동기화 확장 (§3.4 추가 항목)
5. Sonnet/Haiku 승급 시 영향 범위 (judge_model / bot chat_model)

## Co-Authored-By 컨벤션 업데이트

`Claude Opus 4.6 (1M context)` → `Claude Opus 4.7 (1M context)`.
구버전 커밋은 그대로 두고 v0.36 부터 적용.

## `latest` alias 를 쓰지 않는 이유

Anthropic API 는 `claude-opus-latest` 같은 family alias 를 지원하지만 의도적으로 사용하지 않음:
- 새 모델이 호환성/행동 변경 가져와도 **예고 없이** 반영됨
- 토큰 소비 패턴 / 응답 품질 변화 디버깅 어려움
- 릴리스 타이밍을 소비 프로젝트 배포 주기에 맞춰 통제할 수 없음

대신 고정 버전 + 의식적 승급 이벤트로 관리 — git log 로 "언제부터 4.7 썼는지" 추적 가능.

## 관측 (릴리스 직후 공란 — 실사용 후 채울 예정)

- 토큰 소비 변화 (ccusage `@bot usage` / `@bot blocks` 비교):
- 응답 품질 체감:
- 비용 (일일 평균 $ 비교):
- plan 단계 소요 시간:

---

<!-- ===== v0.35 ===== -->

# coord-template v0.35 — worktree `.env.local` symlink 화

## 배경 (실제 사고)

DEALOS 에서 Discord 채널 이전 중 다음 상황 발생:
1. 새 채널로 이전 — 메인 repo 의 `.env.local` 의 `DISCORD_WEBHOOK_URL` 을 새 URL 로 교체
2. `bot/projects.yml` 의 `discord_channel_id` / `discord_alert_channel_id` 도 갱신
3. `@bot reload` 후 일반 명령어는 새 채널로 잘 감
4. **그런데 "처리 시작 / 완료 / Sub 재실행" 알림만 옛 채널로** 계속 감

원인: `scripts/notify-discord.sh` 는 `$PROJECT_ROOT/.env.local` 을 참조하는데, worktree 안에서 호출되면 `PROJECT_ROOT` 가 **worktree 루트** 가 된다 (main repo 아님). v0.34 이전 `setup-worktree.sh` 가 `cp` 로 복사해서 각 worktree 가 **독립 사본** 을 가지고 있었고, 그중 어느 것도 갱신 안 된 것.

```
main repo:              .env.local  ← 새 URL (수정됨)
dealos-wt-head:         .env.local  ← 옛 URL (사본, 미수정)
dealos-wt-db:           .env.local  ← 옛 URL
dealos-wt-backend:      .env.local  ← 옛 URL
dealos-wt-frontend:     .env.local  ← 옛 URL
```

## 수정

### 1. `setup-worktree.sh` — symlink 우선

```bash
# v0.35 (변경 후)
if ln -s "$REPO_ROOT/.env.local" "$WT_DIR/.env.local" 2>/dev/null; then
  echo "▶ .env.local symlink → $REPO_ROOT/.env.local"
else
  # Windows 권한 없음 등 symlink 실패 시 cp fallback
  cp "$REPO_ROOT/.env.local" "$WT_DIR/.env.local"
  echo "▶ .env.local 복사 (symlink 실패 — Windows 개발자 모드 필요)"
fi
```

**효과**: 신규 worktree 는 자동 symlink → main 만 수정하면 전 worktree 반영.

### 2. `scripts/relink-worktree-env.sh` — 기존 설치본 마이그레이션

v0.34 이전에 생성된 worktree 들을 일괄 symlink 로 변환.

```bash
# dry-run 으로 영향 미리 보기
bash scripts/relink-worktree-env.sh --dry-run

# 실제 변환 (내용 다른 worktree 는 skip — 안전)
bash scripts/relink-worktree-env.sh

# 강제 변환 (내용 다른 것도 main 기준으로 덮어쓰기, 백업 남김)
bash scripts/relink-worktree-env.sh --force
```

**동작**:
- `$PARENT_DIR/<project>-wt-*` 패턴으로 모든 worktree 순회
- 각 worktree 의 `.env.local` 을:
  - 이미 symlink → skip (멱등)
  - 파일 없음 → symlink 생성
  - 실제 파일 + 내용 같음 → 백업 후 symlink 로 변환
  - 실제 파일 + 내용 다름 → 기본 skip (개별 편집 존중). `--force` 시 덮어쓰기

**안전장치**:
- 파일 → symlink 변환 시 `.env.local.bak` 백업 자동 생성
- symlink 생성 실패 시 원본 즉시 복구 (상태 유실 없음)
- Windows 에서 symlink 실패 시 개발자 모드 안내

## Windows 대응

Windows 에서 `ln -s` 는 기본으론 실패할 수 있음 (관리자 권한 필요). 해결:
- Windows 10 1703+: 설정 → 업데이트 및 보안 → 개발자용 → **개발자 모드 ON**
- 이후 Git Bash 에서 `ln -s` 가 일반 사용자 권한으로 symlink 생성 가능
- 그래도 안 되면 cp fallback 으로 동작 (기존 방식) — 단, 그 환경에선 main 수정 시 worktree 수동 갱신 필요

## 마이그레이션 흐름 (DEALOS 같은 기존 설치본)

```bash
cd /path/to/project

# 1. webhook URL 이 옛 값 이면 먼저 일괄 교체 (기존 가이드)
NEW_URL="https://discord.com/api/webhooks/..."
for f in ../project-wt-*/.env.local; do
  sed -i.bak "s|^DISCORD_WEBHOOK_URL=.*|DISCORD_WEBHOOK_URL=$NEW_URL|" "$f"
done

# 2. 각 worktree 의 .env.local 이 main 과 내용 같아짐 → symlink 화 가능
bash scripts/relink-worktree-env.sh --dry-run
bash scripts/relink-worktree-env.sh

# 3. 이후 webhook URL 변경은 main 한 군데만
```

---

<!-- ===== v0.34 ===== -->

# coord-template v0.34 — 의존성 자동 설치 스크립트 (Mac + Windows) + start-bot.sh venv 자동 감지

## 배경

신규 환경 (새 Mac / 새 Windows) 에서 coord-template 셋업하려면 Python / Node / git / claude CLI / uv / cloudflared 등을 수동으로 하나씩 설치해야 했다. README 에 "brew install ... && npm i -g ... && pip install ..." 을 복붙하는 식.

## 추가

### `scripts/install-deps-mac.sh`

Homebrew 기반, idempotent.

```bash
bash scripts/install-deps-mac.sh              # 대화식 (각 설치 전 y/n)
bash scripts/install-deps-mac.sh --yes        # 전부 자동
bash scripts/install-deps-mac.sh --skip-cloudflared
```

**동작**:
1. Homebrew 존재 확인 (없으면 안내 후 종료)
2. `brew list` 로 이미 설치된 패키지 제외 → 필요한 것만 `brew install`
   - python@3.12, node, git, uv, (옵션) cloudflared
3. `command -v claude` 체크 → 없으면 `npm i -g @anthropic-ai/claude-code`
4. `.venv` 없으면 `uv venv` → `uv pip install -r bot/requirements.txt`
5. 최종 버전 출력 + 다음 단계 안내

### `scripts/install-deps-windows.ps1`

winget 기반 (Windows 10 1809+ / 11 기본 탑재), idempotent.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1
powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1 -Yes
powershell -ExecutionPolicy Bypass -File scripts\install-deps-windows.ps1 -SkipCloudflared
```

**동작**:
1. winget 존재 확인 (없으면 안내 후 종료 — Microsoft Store "App Installer" 필요)
2. 각 패키지에 대해 `Get-Command <check>` 로 이미 설치 여부 확인 → 필요한 것만 `winget install --silent`
   - Python.Python.3.12, OpenJS.NodeJS.LTS, Git.Git, astral-sh.uv, (옵션) Cloudflare.cloudflared
3. PATH 환경변수 재로드 (방금 설치한 것 즉시 인식)
4. claude CLI → uv venv + uv pip install (bash 스크립트와 동일 로직)

### README 갱신

Quick Start 위에 "사전 의존성 설치" 섹션 신설. 스크립트 링크 + 수동 설치 가이드 (각 도구 용도 명시).

## 설계 원칙

- **Idempotent**: 이미 설치된 건 skip. 여러 번 돌려도 문제없음
- **Opt-in 확인**: 기본은 대화식 (`read -p` / `Read-Host`). `--yes` / `-Yes` 로 자동
- **필수 vs 옵션**: cloudflared 는 대시보드 외부 노출 안 쓰면 불필요 → `--skip-cloudflared` 로 건너뛰기
- **사후 안내**: 설치 끝나면 **다음 단계 5줄** (venv 활성화 / .env.local / projects.yml / init.sh / start-bot.sh) 출력

## 제3 프로젝트 onboarding 단축

기존: README 에서 각 도구 설치 명령 복붙 (Mac 5~6줄, Windows 는 choco/scoop/winget 어떤 걸 쓸지도 결정)
v0.34 이후: clone → `bash scripts/install-deps-mac.sh --yes` 한 줄

## `start-bot.sh` 수정 — `.venv` 자동 감지

**문제**: 의존성을 `.venv` 에 설치해도 `start-bot.sh` 는 plain `python3` 로 시스템 Python 을 써서 `ModuleNotFoundError: No module named 'discord'` 발생 (macOS 실사용 중 확인).

**수정**:
1. `.venv/bin/python` (Unix) 또는 `.venv/Scripts/python.exe` (Windows Git Bash) 존재 시 자동 사용
2. 없으면 `python3` fallback
3. discord 모듈 import 체크 — 실패 시 친절한 메시지 (install-deps 스크립트 또는 수동 설치 가이드 출력)

이제 `source .venv/bin/activate` 없이도 `bash scripts/start-bot.sh` 바로 가능.

---

<!-- ===== v0.33 ===== -->

# coord-template v0.33 — `@bot blocks` 5시간 과금 윈도우 조회

## 배경

`@bot usage` 는 **일별 누적** (ccusage daily). 하지만 Claude Max 구독은 **5시간 윈도우** 단위로 rate limit 이 돌아가서, "지금 이 윈도우에서 얼마 썼고 얼마 남았나" 가 일별 통계보다 실제 의사결정에 가깝다. 일별 누적은 사후 통계라 이미 소진됐으면 늦다.

## 추가 — `@bot blocks`

`ccusage blocks --active` 를 감싸서 현재 활성 5시간 윈도우 1개를 embed 로 표시:

```
⚡ 현재 5시간 윈도우
💵 $98.31 · 167,674,908 tokens · 918 msgs
🤖 models: opus, haiku, sonnet

⏰ 윈도우                🔥 burn rate
15:00 ~ 20:00          $63.83/h
경과 1시간 33분         1,814k tok/min
남은 3시간 27분

📈 이 속도 유지 시 (윈도우 끝)
$318.11 · 542,568,600 tokens
```

**필드 의미**:
- **💵 현재 사용**: 이 윈도우 시작부터 지금까지 누적 ($, tokens, 메시지 수)
- **🤖 models**: 이 윈도우에서 호출된 모델들 (중복 제거)
- **⏰ 윈도우**: 5시간 구간 (첫 메시지 기준 시작 → +5h) · 경과/남은 시간
- **🔥 burn rate**: 시간당 $, 분당 토큰 (현재 페이스)
- **📈 투영**: burn rate 그대로 유지 시 윈도우 끝에 어디까지 갈지

## 활성 블록 없을 때 (idle > 5h)

```
💤 현재 활성 5시간 윈도우 없음
마지막 윈도우: $X.XX · N tokens · M msgs
종료: YYYY-MM-DD HH:MM
```

## 주의 — 무엇이 "남은 한도" 인가

ccusage 는 **Anthropic 공식 rate limit 수치를 모른다** (비공개). 따라서 `blocks` 는 "한도의 몇 %" 가 아니라:

- **실제 사용량** (정확)
- **burn rate** (정확 — 현재 페이스)
- **투영** (burn rate × 남은시간 — 추정)

만 보여준다. "한도 소진" 판단은 burn rate 와 과거 블록들과의 비교로 사용자가 직접 한다.

## 기존 `@bot usage` 와의 차이

| 명령어 | 범위 | 용도 |
|--------|------|------|
| `@bot usage [days=7]` | 일별 누적 (최근 N일) | 주간/월간 소비 추이, 사후 통계 |
| `@bot blocks` | **현재 5시간 윈도우만** | 실시간 페이스 판단 — 작업 배분 결정 |

## 권한

기존 `usage` / `budget` 과 동일하게 `admin_user_ids` 에 등록된 사용자만 호출 가능.

---

<!-- ===== v0.32 ===== -->

# coord-template v0.32 — `📍 head 시작 감지` 제거 + `@bot stop` 전체 롤백 재설계

## 변경

### 1. `📍 head 시작 감지 — 작업 진행 중 (10/30/60분 경과 시 알림)` 제거

사용자 피드백: "이 알림 이제 필요없고". heartbeat 마일스톤 (🕐) + 완료 감지 (✅) 로 충분. 불필요한 중간 노이즈 정리.

### 2. `@bot stop` 재설계 — 완전 롤백

**이전 (v0.30)**: `coordination/STOP` 파일만 작성. 프로세스 kill / 롤백 없음.

**v0.32**: STOP 시그널 + **HEAD_LOCK 대기** + **전체 롤백**:

```
@bot stop                    # 현재 채널 프로젝트
@bot stop <project-id>       # 명시적

흐름:
1. 프로젝트 식별
2. HEAD_LOCK 상태 + 활성 inbox 감지 → 사용자에게 미리 고지
3. coordination/STOP 작성 (기존)
4. HEAD_LOCK clear 대기 (최대 5분, poll 10초)
   타임아웃 시 → "head 수동 종료 후 재시도" 안내, 롤백 skip
5. 롤백 수행:
   a) 파일 삭제 (head worktree + work_branch 양쪽 스캔):
      - coordination/plans/<inbox-id>.md
      - coordination/tasks/wt-*.md (inbox id 포함 본문만)
      - coordination/reports/wt-*.md (inbox id 포함 본문만)
      - coordination/review-inbox/*<inbox-id>*.md (verdict 미확정만 — go/needs-fix/block 이면 보존)
   b) inbox 상태: status: pending → status: cancelled 마킹
   c) 각 worktree 브랜치 failed/ 로 이동 + work_branch 최신 재생성:
      - wt/<name> 이 origin/<work_branch> 보다 ahead 면
      - branch -m wt/<name> failed/wt/<name>-<ts>
      - checkout -B wt/<name> origin/<work_branch>
      - push origin failed/ (보존) + push -f origin wt/<name> (reset)
6. Embed 응답:
   - inbox 상태 변경
   - 삭제된 파일 목록 (최대 15개 + 카운트)
   - 이동된 브랜치 목록 (ahead 커밋 수 + tip hash + failed/ 이름)
   - skip 된 worktree (ahead 0 또는 실패 사유)
   - 보존된 파일 (verdict 확정 review-inbox)
   - STOP 해제 가이드
```

## 새 helper

- `_list_sub_names(project_path)` — config.yml 의 `subs[].name`
- `_list_project_worktrees(project_path, subs)` — project 의 head + sub worktree 경로 목록
- `_branch_has_commits_ahead(wt_path, remote, work_branch)` — ahead 커밋 수 + tip hash
- `_rollback_worktree_to_work_branch(wt_path, branch, remote, work_branch, ts)` — branch → failed/<...>-<ts> 이동 + work_branch 로 재생성

## 안전장치

- HEAD_LOCK clear 대기 (head 가 파일 쓰는 중일 때 race 방지)
- 5분 타임아웃 → 롤백 skip + 수동 안내
- verdict 확정된 review-inbox 는 **삭제 안 함**
- inbox 파일은 **삭제 안 함** (status: cancelled 마킹만)
- failed/ 브랜치 원격 push → 복구 가능
- force push 는 wt/<name> 재생성에만 (로컬/원격 대칭)

## 철학

사용자 말: **"작업 전 버전으로 돌아가기만 하면 되니까"**. v0.32 는 이걸 실현:
- work_branch 기준으로 모든 worktree 재설정
- coord 파일 중 **이번 inbox 전용** 것만 삭제 (타 inbox 건드림 X)
- merged 된 것은 손 대지 않음 (이미 반영됐으므로)

## 주의

- **복구**: failed/ 브랜치로 보존됨. 필요 시 `git checkout failed/wt/<name>-<ts>` 로 되돌릴 수 있음.
- **공유 브랜치 영향**: `wt/<name>` force push 로 다른 협업자가 해당 브랜치 기반 작업 중이면 충격 가능. coord 는 "wt/* 는 단일 사용자 작업 영역" 전제라 문제 없음.
- **merged inbox**: status: merged 면 자동 감지 안 됨 (이미 완료). 혹시 중간에 섞여있으면 review-inbox verdict 체크로 파일 보존.

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.32
# bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
# head 작업 중간에 stop
@bot send <작업>   # head 실행 중
@bot stop
  → 🛑 STOP 처리 시작 (HEAD_LOCK active / 대상 inbox: 2026-04-21-XXXXXX)
  → (head checkpoint 도달하고 abort 대기, 최대 5분)
  → 🛑 STOP + 롤백 완료 embed
     - inbox: cancelled 마킹
     - 삭제: plans/..., tasks/wt-backend.md, reports/wt-backend.md
     - 브랜치 이동: wt/backend (+3 commits, tip abc1234) → failed/wt/backend-20260421-153000
     - STOP 해제: rm 경로 + commit
```

---

<!-- ===== v0.31 ===== -->

# coord-template v0.31 — 릴리스 노트 통합 (CHANGELOG.md)

## 배경

이전엔 릴리스마다 `RELEASE_NOTES_v<ver>.md` 파일 생성 (gitignored) — 누적 42개. 로컬 파일 폭증.

## 변경

- 전체 42개 파일 내용을 `CHANGELOG.md` 하나로 병합 (최신이 위)
- 개별 `RELEASE_NOTES_v*.md` 삭제
- `.gitignore` 에서 `RELEASE_NOTES_v*.md` 패턴 제거
- `CHANGELOG.md` 는 **git 추적** (단일 진실원)

## 향후 워크플로우

```bash
# 1. CHANGELOG.md 최상단에 새 버전 섹션 append (v0.31 바로 아래)
# 2. 커밋 + 태그
git commit -am "feat(vX.Y): ..."
git tag vX.Y && git push origin main vX.Y

# 3. GitHub Release — CHANGELOG 에서 해당 섹션 추출
SECTION=$(awk '/<!-- ===== vX.Y ===== -->/,/<!-- ===== v[0-9]/' CHANGELOG.md | sed '$d')
gh release create vX.Y --title "vX.Y — <one-liner>" --notes "$SECTION"
```

CLAUDE.md §1.3 에 워크플로우 명시.

## 하위 호환

- 기존 GitHub Releases 는 그대로 (각 버전 태그에 당시 notes 로 등록된 상태)
- CHANGELOG.md 는 추가 browse 용 — 로컬에서 버전별 내용 빠르게 확인 가능
- 42개 이전 파일의 내용 전부 CHANGELOG.md 에 보존

---

<!-- ===== v0.30 ===== -->

# coord-template v0.30 — webhook 완료 알림에 `@bot review` 힌트 자동 추가

## 사용자 제안

```
✅ 완료
id: 2026-04-21-142259
제목: 건폐율 초과 방지 — 내접 사각형 잔여면적 인자 규칙
Sub: wt-backend (a3e9d4bc)
검증: tsc PASS, ssot:check PASS
review-inbox: 20260421-1435-2026-04-21-142259.md
```
→ "이 메세지에 review 명령어 연계를 알려주는것도 있으면 좋을듯"

## 배경

이 "✅ 완료" 메시지는 **head 세션이 webhook 으로 전송** 하는 것 (봇이 만드는 embed 아님). v0.29 에서 봇의 📥 작업 시작 anchor + 🎉 머지 완료 embed 에 명령어 힌트는 추가했지만, webhook 메시지는 별도.

v0.30 에서 `scripts/notify-discord.sh` 자체에 로직 추가 — **완료 관련 webhook 알림에 inbox id 가 감지되면 `@bot review` 힌트 자동 append**.

## 동작

### 감지 조건 (둘 다 만족)

1. **title 에 완료 keyword** — "완료", "✅", "done", "finish", "complete" 중 하나 포함
2. **message 에 inbox id 패턴** — `YYYY-MM-DD-HHMMSS` 정규식 매칭

### 추가되는 embed field

```
💡 다음 단계
@bot review <inbox-id> — verdict 확인 + 승인 리액션 (✅/❌)
```

### 예시

**이전 (v0.29 까지)**:
```
embed:
  title: ✅ 완료
  description: id: 2026-04-21-142259
               제목: 건폐율 초과 방지 ...
               Sub: wt-backend (a3e9d4bc)
               ...
```

**v0.30**:
```
embed:
  title: ✅ 완료
  description: id: 2026-04-21-142259
               제목: 건폐율 초과 방지 ...
               Sub: wt-backend (a3e9d4bc)
               ...
  fields:
    - name: 💡 다음 단계
      value: `@bot review 2026-04-21-142259` — verdict 확인 + 승인 리액션 (✅/❌)
```

## 구현

`scripts/notify-discord.sh` 의 Python embed 빌더에 로직 추가:

```python
completion_keywords = ['완료', '✅', 'done', 'finish', 'complete']
is_completion = any(kw in title.lower() for kw in completion_keywords)
if is_completion:
    id_match = re.search(r'(?:^|[\s:])(\d{4}-\d{2}-\d{2}-\d{6})', message)
    if id_match:
        inbox_id = id_match.group(1)
        fields.append({
            'name': '💡 다음 단계',
            'value': f'`@bot review {inbox_id}` — verdict 확인 + 승인 리액션 (✅/❌)',
            'inline': False,
        })
```

**위치**: `DASHBOARD_LINKS=auto` 필드 append 이후 — 대시보드 링크와 함께 나란히.

## 대상 알림 (자동 적용)

이 조건을 만족하는 **기존 webhook 호출들** 이 모두 영향:

- `head` 가 inbox 처리 완료 시 "✅ 완료" 웹훅 → 자동 review 힌트
- `/review-inbox` 의 "🎉 머지 완료" → inbox id 있으면 힌트 (하지만 이미 봇이 rich embed 올린 상태라 약간 중복, 무해)
- 사용자 커스텀 notify-discord.sh 호출 중 위 조건 만족 → 자동 적용

## 잘못 트리거 안 함

- title 에 "완료" 없는 알림 → hint 안 붙음
  - "📥 신규 inbox" — id 있어도 skip (review 아직 불필요)
  - "🚨 STOP" — review 무관
  - "⚠ 머지 충돌" — review 단계 지남
- message 에 inbox id 패턴 없는 알림 → 당연히 skip

## 하위 호환

- 기존 notify-discord.sh 인터페이스 변화 없음 (3-4 인자 동일)
- 추가 embed field 만 늘어남 — 기존 description/표준링크 유지
- Python 없는 환경 fallback 은 hint 적용 안 됨 (기능 축소 — 하지만 Python 거의 항상 있음)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.30
## scripts/notify-discord.sh 변경

## 봇 재시작 불필요 (shell script 변경이라 다음 호출부터 자동 반영)
```

## 테스트

head 가 inbox 처리 후 webhook 호출 (DEALOS 내부 /inbox 명령 에서):
```
✅ 완료
id: 2026-04-21-142259
제목: ...

💡 다음 단계    ← v0.30 자동 추가
@bot review 2026-04-21-142259 — verdict 확인 + 승인 리액션 (✅/❌)
```

사용자가 webhook 채널에서 barka 이 힌트 보고 대화방으로 가서 `@bot review 2026-04-21-142259` 실행 → verdict prompt → ✅ 리액션 → 자동 orchestration 완주.

## 오늘 세션 총 13회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | `@bot review` 자동 선택 단순화 |
| v0.26 | verdict prompt 이중 포스트 방지 |
| v0.27 | `@bot main-merge` (dry-run) |
| v0.28 | 최신 claude-code native binary 호환 |
| v0.29 | 봇 알림에 명령어 힌트 |
| v0.30 | **webhook 완료 알림에 review 힌트 자동** |

봇/webhook 양쪽 모두 **"다음 뭐 할지 알려주는"** 알림 체계 완성.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.29 ===== -->

# coord-template v0.29 — 시작/완료 알림에 다음 명령어 힌트 추가

## 사용자 제안

> "시작과 완료 알림 메세지에 연계해서 사용할 수 있는 명령어를 표현하면 좋을거같아
> 시작에는 마지막 줄에 active로 작업 상황 확인
> 완료에는 review로 ~~ 진행 이렇게 (~~에는 알맞은 내용을 써줘)"

봇 명령 발견성 향상 — 알림 메시지 안에서 **자연스럽게 다음 action 유도**.

## 변경

### 1. 📥 작업 시작 anchor

**이전**:
```
📥 dealos 작업 시작
본문: 건폐율 초과 방지 — 내접 사각형 ...
✅ dealos inbox-send spawn
log: C:\Users\...\bot-dealos-inbox-send-....log
```

**v0.29**:
```
📥 dealos 작업 시작
본문: 건폐율 초과 방지 — 내접 사각형 ...
✅ dealos inbox-send spawn
log: C:\Users\...\bot-dealos-inbox-send-....log
💡 @bot active 로 전체 작업 현황 확인
```

### 2. 🎉 머지 완료 embed — "다음 단계" 필드 신설

**이전 (v0.24)**: 완료 embed 에 변경 요약 / 발견 사항 / verdict 근거 만.

**v0.29**: 새 필드 `💡 다음 단계`:
```
💡 다음 단계
  • @bot main-merge — 3dview → main 반영 (dry-run 사전 감지)
  • @bot active — 전체 프로젝트 작업 현황 확인
  • @bot status dealos — dealos 상세 상태
```

조건부: `work_branch == stable_branch` 면 main-merge 안내 생략 (불필요).

### 3. `@bot main-merge` 성공 embed 도 통일

v0.27 에 이미 "다음 단계" 있었는데 `@bot active` 명령 추가해 일관성 유지:
```
💡 다음 단계
  • 필요 시 wt/* 브랜치 rebase (자동 orchestration 에서 자동 처리됨)
  • 협업자에게 main 업데이트 공지
  • @bot active — 전체 프로젝트 작업 현황 확인
```

## 효과

사용자가 알림만 봐도 다음 할 수 있는 것 파악:
- 시작: "지금 이 작업 말고 다른 건?" → `@bot active`
- 완료: "main 에 반영?" → `@bot main-merge` / "다른 프로젝트?" → `@bot active`

명령어 외우지 않아도 흐름 따라가면 됨.

## 하위 호환

- 정보 추가만 — 기존 메시지 포맷 변화 미미
- 조건부 main-merge 힌트 → 단일 브랜치 운영 프로젝트엔 깔끔
- `_git_branches` helper 재사용 (v0.27 에서 도입)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.29
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
@bot send <작업>
  → 📥 작업 시작 anchor:
     "💡 @bot active 로 전체 작업 현황 확인" 줄 확인
  → [후속 orchestration]
  → ✅ 리액션
  → 🎉 머지 완료 embed:
     "💡 다음 단계" 필드에 3개 명령어 표시
```

## 오늘 세션 총 12회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | `@bot review` 자동 선택 단순화 |
| v0.26 | verdict prompt 이중 포스트 방지 |
| v0.27 | `@bot main-merge` (dry-run) |
| v0.28 | 최신 claude-code native binary 호환 |
| v0.29 | **시작/완료 알림에 명령어 힌트** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.28 ===== -->

# coord-template v0.28 — 최신 claude-code 패키지 구조 변경 호환성 (native binary 지원)

## 증상

사용자 제보:
> "원인 확정: 최신 claude-code 패키지 구조 변경 — 예전 cli.js 없고 bin/claude.exe native binary 로 바뀜. v0.16.5 의 Windows cmd 우회 로직이 hardcoded cli.js 경로를 찾음 → 파일 없어서 node 실행 실패."

## 원인

`claude-code` npm 패키지가 최근 업데이트되면서 bin 구조 변경:
- **구 버전 (~2025)**: `node_modules/@anthropic-ai/claude-code/cli.js` — Node.js 스크립트
- **최신 버전**: `node_modules/@anthropic-ai/claude-code/bin/claude.exe` — native binary

`claude.cmd` wrapper 도 따라 변경:
```cmd
## 구
"%~dp0\node_modules\@anthropic-ai\claude-code\cli.js" %*
→ 실제론 node "%~dp0\..." %* 형태

## 최신
"%dp0%\node_modules\@anthropic-ai\claude-code\bin\claude.exe"   %*
```

v0.16.5 의 정규식은 `".js"` 패턴만 매칭:
```python
m = re.search(r'"([^"]+\.js)"\s+%\*', content)
```

최신 wrapper 엔 `.js` 없음 → **매칭 실패** → fallback 으로 CLAUDE_BIN (claude.cmd) 직접 사용 → subprocess 가 cmd.exe 경유 → **multi-line prompt 개행 버그 재발** (v0.16.5 가 해결했던 문제).

## 수정

두 패턴 모두 지원하도록 확장:

```python
def _resolve_claude_invocation():
    ...
    content = claude_path.read_text(...)
    
    def _resolve_path(raw):
        return Path(raw.replace("%dp0%", ...).replace("%~dp0", ...))
    
    # v0.28: 새 패턴 (native .exe) 먼저 시도
    m_exe = re.search(r'"([^"]+\.exe)"\s+%\*', content)
    if m_exe:
        exe_path = _resolve_path(m_exe.group(1))
        if exe_path.exists():
            return [str(exe_path)]   # claude.exe 직접 호출 — cmd.exe X
    
    # v0.16.5: 구 패턴 (node + cli.js)
    m_js = re.search(r'"([^"]+\.js)"\s+%\*', content)
    if m_js:
        script_path = _resolve_path(m_js.group(1))
        if script_path.exists() and shutil.which("node"):
            return [node_bin, str(script_path)]
    
    # 둘 다 실패 → fallback (cmd.exe 개행 버그 가능)
    return [CLAUDE_BIN]
```

우선순위:
1. **`.exe` native binary** (최신 claude-code)
2. **`node + .js` script** (구 claude-code)
3. **`CLAUDE_BIN` fallback** (마지막 수단, 경고 로그)

## 기동 로그

```
## 최신 claude-code 환경
[bot] ✅ Windows cmd.exe 우회 — claude.exe native binary 직접 호출 (v0.28)

## 구 claude-code 환경
[bot] ✅ Windows cmd.exe 우회 — node + cli.js 호출 (v0.16.5 구조)

## 둘 다 실패 (이례적)
[bot] ⚠ claude.cmd 우회 패턴 매칭 실패 — fallback (cmd.exe 개행 버그 가능)
```

## 사용자 환경 검증

실제 사용자 Windows 환경:
```
claude.CMD: C:\nvm4w\nodejs\claude.CMD
내용: "%dp0%\node_modules\@anthropic-ai\claude-code\bin\claude.exe"   %*

v0.28 파싱 결과:
- 새 패턴 (.exe) 매칭: bin\claude.exe
- resolved: C:\nvm4w\nodejs\node_modules\@anthropic-ai\claude-code\bin\claude.exe
- exists: True ✓
→ [str(exe_path)] 반환, cmd.exe 우회 성공
```

## 하위 호환

- 구 claude-code 환경 (cli.js) → v0.16.5 로직 그대로 작동
- 완전 다른 패턴 wrapper → fallback + 경고 로그
- macOS / Linux → 영향 없음 (os.name != "nt" early return)

## 교훈

Python subprocess 의 Windows `.cmd` 처리 이슈는 "workaround 하면 끝" 이 아니라 **상위 도구의 구조 변경에 따라 꾸준히 갱신 필요**. claude-code 가 native binary 전환한 건 v0.16.5 당시엔 예측 못 한 변화.

다음번 claude-code 구조 변경 (또는 다른 방식 wrapper) 이 있으면 또 추가 패턴 지원해야 할 수 있음. 기본 원칙 유지: **cmd.exe 를 피할 수 있으면 피한다**.

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.28
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

기동 로그에 `✅ claude.exe native binary 직접 호출` 뜨면 성공. `⚠ 매칭 실패` 뜨면 추가 조사 필요 — 해당 claude.cmd 내용 공유해주세요.

## 테스트

```
@bot send
[multi-line prompt with error stack]

→ v0.28 이전: cmd.exe 개행 버그로 첫 줄만 전달 (flags 도 잘림)
→ v0.28: claude.exe 직접 실행 → 개행 보존, 전체 메시지 전달
```

## 오늘 세션 총 11회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | `@bot review` 자동 선택 단순화 |
| v0.26 | verdict prompt 이중 포스트 방지 |
| v0.27 | `@bot main-merge` (dry-run 감지) |
| v0.28 | **최신 claude-code native binary 호환** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.27 ===== -->

# coord-template v0.27 — `@bot main-merge` 명령 (dry-run 충돌 감지 + 안전 머지)

## 배경

사용자 제안:
> "옵션 A와 B[=C] 추가하자 감지하고 문제없으면 머지하면 되니까"
> "문제 생길 시 수동 머지하면 되고"

기존 설계:
- 자동 orchestration 은 `wt/<sub>` → `work_branch` (3dview) 까지만 처리
- `work_branch` → `stable_branch` (main) 은 **수동 영역**

사용자가 Discord 에서도 `work_branch → stable_branch` 머지를 trigger 하되:
- **충돌 사전 감지** (temp 브랜치 dry-run)
- 안전하면 → 실제 머지 + push
- 충돌하면 → stable 안 건드리고 파일 목록 + 수동 해결 가이드

**철학**: 봇은 자동 conflict 해결 시도 안 함. "문제 생기면 수동" 원칙 유지.

## 사용법

```
@bot main-merge                    # 현재 채널 프로젝트
@bot main-merge dealos             # 명시적 프로젝트
```

## 동작 흐름

```
@bot main-merge
  ↓
🔍 사전 점검 중…
  ↓
Step 1: git status — uncommitted 있으면 중단
Step 2: fetch origin <work> <stable>
Step 3: checkout stable + pull --ff-only
Step 4: temp 브랜치 생성 (tmp-main-merge-check-<HHMMSS>)
Step 5: merge --no-commit --no-ff origin/<work> (dry-run)
  ↓
┌─ 충돌 없음 ─┐              ┌─ 충돌 있음 ─┐
│             │              │             │
│ temp abort  │              │ 파일 목록   │
│ + delete    │              │ 수집        │
│             │              │             │
│ 실제 merge  │              │ abort +     │
│ --no-ff     │              │ temp 삭제   │
│             │              │             │
│ push        │              │ embed 포스트│
│             │              │ (충돌 파일+ │
│ 🎉 embed    │              │ 수동 가이드)│
│             │              │             │
└─────────────┘              └─────────────┘
```

## 결과 Embed — 성공

```
🎉 3dview → main 머지 완료
  project: dealos
  trigger: reaction by @user

📍 머지 커밋
  main: c6d6c958

💡 다음 단계
  • 필요 시 wt/* 브랜치 rebase (자동 orchestration 에서 처리됨)
  • 협업자에게 main 업데이트 공지
```

## 결과 Embed — 충돌

```
⚠ 3dview → main 머지 충돌 감지
  project: dealos
  stable 안 건드림 — 자동 머지 중단, 사용자 수동 해결 필요

🔥 충돌 파일 (3개)
  • src/core/payment.ts
  • src/lib/fees/calc.ts
  • docs/payment.md

🔧 수동 해결 가이드
  cd dealos
  git checkout main
  git merge 3dview
  # 충돌 파일 수정 후:
  git add <files>
  git commit
  git push origin main
```

## 안전장치

1. **uncommitted 있으면 중단** — 기존 상태 보호
2. **`pull --ff-only`** — 로컬 이 원격보다 뒤면 정상, 앞서면 중단 (의도치 않은 rewind 방지)
3. **temp 브랜치로 dry-run** — 실패해도 stable/work 전혀 안 건드림
4. **dry-run 성공 후에야 본 merge** — 두 단계 모두 OK 여야 push
5. **충돌 시 stable 그대로** — 어떤 변경도 안 남김

## 새 helper

### `_run_git(cwd, *args, timeout=120)`

```python
async def _run_git(cwd, *args, timeout=120) -> tuple[int, str, str]:
    """git 명령 → (returncode, stdout, stderr)"""
    proc = await asyncio.create_subprocess_exec("git", *args, ...)
    stdout, stderr = await asyncio.wait_for(proc.communicate(), timeout=timeout)
    return proc.returncode, stdout.decode(), stderr.decode()
```

### `_git_branches(project_path) -> (remote, work, stable)`

```python
def _git_branches(project_path):
    cfg = _read_project_config(project_path)
    git_cfg = cfg.get("git") or {}
    return (
        git_cfg.get("remote", "origin"),
        git_cfg.get("work_branch", "main"),
        git_cfg.get("stable_branch", "main"),
    )
```

config.yml 미설정 시 기본 `(origin, main, main)` — 이 경우 `@bot main-merge` 가 "이미 같음" 응답 후 종료.

### `_read_project_config(project_path) -> dict`

`coordination/config.yml` 전체 파싱.

## config 요구사항

`coordination/config.yml`:
```yaml
git:
  remote: origin
  work_branch: 3dview
  stable_branch: main
```

work == stable 이면 `@bot main-merge` 는 "이미 같음" 으로 early exit.

## 하위 호환

- 기존 자동 orchestration 흐름 영향 없음 (`wt/*` → `work_branch` 만 처리)
- `@bot main-merge` 는 **명시 trigger 전용** — 자동 불가, 사용자 의도 필요

## 보안 / 위험 최소화

- admin 권한 체크 (`_authorized`)
- 채널 필터 (`_channel_ok`)
- 사전 감지로 **stable 은 충돌 시 전혀 안 건드림** — main 망가뜨릴 리스크 0

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.27
## bot/controller.py + bot/README.md

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 정상 케이스: 3dview 에 새 커밋 있고 main 과 충돌 없음
@bot main-merge
  → 🔍 사전 점검 중…
  → ✅ 충돌 없음 — main 에 3dview 머지 진행
  → 🎉 3dview → main 머지 완료 embed

## 충돌 케이스: main 에만 있는 변경 + 3dview 에 겹치는 변경
@bot main-merge
  → ⚠ 머지 충돌 감지
  → 🔥 충돌 파일 목록
  → 🔧 수동 해결 가이드

## 이미 동일 케이스
@bot main-merge
  (work == stable 이면)
  → ℹ work_branch 와 stable_branch 같음 — 불필요

  (3dview == main 이면)
  → git merge 가 "Already up to date" 반환 — dry-run 성공 + 실제 merge 는 no-op
  → embed 는 뜨지만 새 커밋 없음
```

## 오늘 세션 총 10회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | `@bot review` 자동 선택 단순화 |
| v0.26 | verdict prompt 이중 포스트 방지 |
| v0.27 | **`@bot main-merge` (dry-run + 안전 머지)** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.26 ===== -->

# coord-template v0.26 — verdict prompt 이중 포스트 방지 (race condition 포함)

## 증상

사용자 제보 (스크린샷):
```
@bot review 2026-04-21-142259
⏳ review 실행 중…

✅ head 완료 감지 — verdict 확인 중           ← 자동 orchestration
📋 Plan — 건폐율 초과 방지 ...                 ← v0.19 plan 요약
✨ head self-review 감지 — verdict: go        ← 자동 orch Phase C-bis

[verdict prompt embed ✅/❌]    ← 1번
[verdict prompt embed ✅/❌]    ← 2번 (DUP!)
```

두 verdict prompt 가 **같은 inbox** 에 대해 연속 포스트. 어떤 리액션을 눌러야 할지 혼란.

## 원인: race condition

두 독립 경로가 거의 동시에 `_post_verdict_prompt` 호출:

1. **자동 orchestration** (v0.12~) — `@bot send` 로 시작, Phase C-bis (v0.13 self-review) 또는 Phase F 가 verdict prompt 포스트
2. **수동 `@bot review`** (v0.21) — 사용자가 명시 실행, `_spawn_and_wait` 완료 후 verdict prompt 포스트

두 경로가 **겹치는 시나리오**:
- 사용자가 `@bot send` 후 auto orch 결과를 기다리다, 중간에 `@bot review <id>` 로 수동 확인
- 혹은 auto orch 가 완료 직전인데 사용자가 prompt 못 받은 줄 알고 `@bot review`
- 두 spawn 모두 같은 inbox 의 review-inbox 파일을 읽고 verdict prompt 포스트

결과: Discord 채널에 같은 verdict prompt 2개.

## 수정 — 다층 방어

### 1. `_post_verdict_prompt` 내부 dedup (핵심)

모든 호출 경로 공통 방어. 포스트 직전 채널 스캔:

```python
async def _post_verdict_prompt(channel, ...):
    # v0.26: 채널 내 같은 inbox 의 기존 prompt 스캔
    existing = await _find_existing_verdict_prompt(channel, inbox_id)
    if existing is not None:
        print(f"[verdict-prompt] {inbox_id} 기존 prompt 있음 — skip")
        return
    # 기존 embed 빌드 + post
    ...
```

호출 순서:
- 경로 A가 먼저 post → 경로 B가 호출됐을 때 A의 prompt 발견 → silent skip
- race 로 동시 호출되면 둘 다 스캔 결과 empty 라 drain 할 수 있음 (극히 드문 케이스)

### 2. `cmd_review` early exit (UX)

수동 review 시점에 이미 prompt 있으면 사용자에게 jump URL 응답:
```
⚠ dealos 2026-04-21-142259 verdict prompt 이미 존재
https://discord.com/channels/.../message_id
→ 기존 prompt 에 리액션 (✅/❌) 사용. 새 review 실행 안 함.
```

Review spawn 도 생략 (토큰 절약).

### 3. `cmd_review` verdict 기록 체크

Prompt 아직 없어도 verdict 파일에 기록됐으면 spawn 생략:
```
✨ dealos 2026-04-21-142259 verdict 이미 기록됨: go — review spawn 생략, prompt 표시 시도
```

## Helper 신규

```python
async def _find_existing_verdict_prompt(
    channel: discord.abc.Messageable, inbox_id: str, limit: int = 100
) -> discord.Message | None:
    """채널 최근 `limit` 메시지 중 해당 inbox_id 의 verdict-prompt embed 탐색."""
    marker_prefix = f"{_VERDICT_MARKER_FOOTER_PREFIX}{inbox_id}|"
    async for msg in channel.history(limit=limit):
        if not msg.author.bot:
            continue
        for embed in msg.embeds:
            footer = embed.footer.text if embed.footer else None
            if footer and footer.startswith(marker_prefix):
                return msg
    return None
```

embed 의 footer marker (`verdict-prompt|<inbox-id>|<verdict>`) 로 동일 inbox 여부 판별.

## 효과

| 시나리오 | 이전 (v0.25) | v0.26 |
|---------|--------------|-------|
| auto orch 만 | prompt 1개 | prompt 1개 (동일) |
| auto orch → 수동 `@bot review` | prompt 2개 DUP | prompt 1개, 수동 은 jump URL 응답 |
| 수동 `@bot review` 만 | prompt 1개 | prompt 1개 (동일) |
| 수동 → auto orch race (동시) | prompt 2개 DUP | prompt 1개 (먼저 post 한 쪽만) |

## 엣지 케이스

- **극단 race**: 두 경로가 ms 단위로 동시에 `_post_verdict_prompt` 진입 → 둘 다 scan empty → 둘 다 post. 이론적으론 가능하지만 실사용에선 거의 안 일어남. 완벽한 방지는 dist lock / DB 필요.
- **100개 메시지 밖**: scan limit 넘어가면 못 찾음 → 새 prompt 포스트. 실사용에서 문제 없음 (최소 1~2 시간치 메시지).
- **다른 채널**: auto orch 는 작업장 thread, 수동은 대화방 → 두 채널 scan 결과 각자 empty → 2개 post 정상 (의도).

## 하위 호환

- `_post_verdict_prompt` 에 dedup 추가 — 기존 호출부 (cmd_review, _post_spawn_orchestration) 수정 불필요
- `channel.history` 호출 실패 시 graceful (로그만, 진행 continue)
- `cmd_review` 의 early exit 는 UX 개선 (에러 아니므로 명시 응답)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.26
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 1. @bot send X → auto orch 진행 중
## 2. 도중에 @bot review <id> 실행
##    → ⚠ verdict prompt 이미 존재 (jump URL 응답)
##    → spawn 안 함, 새 prompt 안 생성
## 3. Discord 채널엔 prompt 하나만

## 또는: @bot send 만 돌면 prompt 1개 (이전과 동일)
```

## 오늘 세션 총 9회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로/경과시간 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | `@bot review` 자동 선택 단순화 |
| v0.26 | **verdict prompt 이중 포스트 방지 (race 포함)** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.25 ===== -->

# coord-template v0.25 — `@bot review` 자동 선택 단순화 (status: pending 기준)

## 증상

사용자 제보:
```
@bot review
→ ✅ dealos 검토 대기 inbox 없음 (모두 review 됨)

@bot review 2026-04-21-142259     ← 수동 id 지정해야만 진행됨
→ ⏳ review 실행 중…
```

`2026-04-21-142259` 은 **아직 머지 안 된** inbox (`status: pending`) 인데 봇이 "이미 review 됨" 으로 판정. 수동 id 지정 없이는 자동 선택 불가.

## 원인

v0.18 까지의 `_latest_unreviewed_inbox` 는 **두 단계** 필터:
1. inbox 의 `status:` 헤더가 pending 아니면 skip (v0.18 추가)
2. inbox_id 가 review-inbox 파일 **본문 어디에든** 언급되면 skip (기존 로직)

문제: 2번 체크가 너무 공격적. review-inbox 파일에 id 언급만 있어도 → `@bot review` 자동 선택 제외. 하지만 **verdict 는 확정됐으나 사용자 ✅ 리액션이 유실됐거나, 봇이 prompt 포스트 실패한 상태** 에선 여전히 사용자 개입 필요 → 자동 선택 대상이어야 함.

핵심: **inbox status: pending = 아직 사용자 action 대기 중 = 자동 선택 후보**. review-inbox 파일 존재 여부는 부차적.

## 수정

`_latest_unreviewed_inbox` 를 **status 기준만** 으로 단순화:

```python
for inbox_file in candidates:   # mtime 역순
    status = extract_status(inbox_file)
    if status is None:          # 헤더 없음 → 보수적으로 pending 간주
        return inbox_file.stem
    if status == "pending":
        return inbox_file.stem
    # merged / cancelled / done / failed / superseded 등 → skip
```

제거된 것:
- head_wt + work_branch 의 review-inbox 전체 텍스트 스캔 (`reviewed_text`)
- inbox_id 가 본문에 있는지 매칭 체크

유지된 것 (v0.18):
- inbox status 가 pending 외 다른 값이면 skip

## 판정 테이블 (v0.25)

| inbox status | 자동 선택 대상? |
|--------------|----------------|
| `pending` | ✅ 후보 |
| `merged` | ❌ skip |
| `cancelled` | ❌ skip |
| `done` | ❌ skip |
| `failed` | ❌ skip |
| `superseded` | ❌ skip |
| (헤더 없음) | ✅ 후보 (보수적) |

review-inbox 파일 존재 여부는 **무관**. "사용자 action 이 필요한가" (= status pending) 만 기준.

## 하위 호환

- `@bot review <id>` 수동 지정은 그대로
- 기존 선택 기준이 더 엄격했는데 완화 방향이라 **이전 OK 케이스는 그대로 OK**, 추가로 기존 누락 케이스가 자동 선택됨
- `_find_needs_fix_candidate` 등 다른 helper 는 영향 없음

## 효과

| 케이스 | 이전 (v0.18/v0.19) | v0.25 |
|--------|--------|-------|
| inbox status: pending + review-inbox 없음 | 자동 선택 | 자동 선택 (동일) |
| inbox status: pending + review-inbox 있고 verdict: pending | 자동 선택 (id 언급 없으면) | 자동 선택 |
| **inbox status: pending + review-inbox 있고 verdict: go** | **skip (버그)** | **자동 선택** ✅ |
| inbox status: merged | skip | skip (동일) |

굵은 부분이 이번 수정 포인트. "머지 대기 상태" 가 자동 선택에 포함됨.

## 실제 테스트

```python
## DEALOS 현재 상태로 테스트
inbox_id, status = latest_unreviewed(dealos)
## → 2026-04-21-142259 (status=pending)
```

이전엔 "모두 review 됨" 반환. v0.25 에선 pending inbox 정상 선택.

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.25
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트 흐름

```
@bot review
  → 🔎 dealos 최근 미review inbox 자동 선택: 2026-04-21-142259
  → ⏳ review 실행 중 (최대 10분)…
  [완료]
  → [verdict prompt embed + ✅/❌ 리액션]
  → ✅
  → 🚀 머지 진행 중 (최대 30분 대기)
  [완료]
  → 🎉 머지 완료 rich embed
```

v0.21 (수동 review prompt) + v0.24 (머지 rich summary) + v0.25 (자동 선택 정상화) 조합으로 **수동 개입 최소화** 완성.

## 오늘 세션 총 8회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로/경과시간 |
| v0.24 | 머지 완료 rich summary embed |
| v0.25 | **`@bot review` 자동 선택 단순화** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.24 ===== -->

# coord-template v0.24 — 머지 완료 후 rich summary embed

## 배경

사용자 제보:
> "완료 시 완료 알림밖에 없음. 사용자가 직접 log 파일을 보는건 자세하게 알아야할 때고 그전에는 어떤 작업이 되었고 어떤 문제를 어떻게 해결했다는 요약 디스코드 알림이 필요해"

현재 머지 완료 시 보이는 메시지:
```
완료
id: 2026-04-21-103145
제목: 건폐율 기준 최대 사각형 선택 로직
commit: f94b4ac8 (wt/backend)
review: review-inbox/...
```

→ "뭘 했는지 / 어떤 문제가 있었는지 / 어떻게 해결했는지" 없음. 로그 파일 직접 확인해야 함.

## 원인

봇의 verdict ✅ 리액션 핸들러가 **fire-and-forget** 이었음:

```python
## v0.23 이전
ok, msg = await _spawn_claude_command(...)    # Popen, 즉시 return
await channel.send("🚀 머지 spawn")             # 이게 전부
```

머지 자체는 `/review-inbox --merge-only --auto-yes` 안에서 webhook 으로 완료 알림 보냄 (최소 텍스트). 봇은 완료를 안 기다려서 rich embed 안 만들었음.

## 수정

봇이 머지 완료까지 대기 + review-inbox 재파싱 + rich embed 포스트:

```python
## v0.24
if verdict == "go":
    await channel.send("🚀 머지 진행 중 (최대 30분 대기)")
    
    ok, out = await _spawn_and_wait(
        f"/review-inbox {inbox_id} --merge-only --auto-yes",
        cwd=project_path, timeout=1800,
    )
    if not ok:
        await channel.send(f"❌ 머지 실패: {out[:500]}")
        return
    
    # 머지 후 review-inbox 재조회 (merge_commits 포함됨)
    _, post_merge_fname = _read_verdict(...)
    await _post_merge_summary(channel, project_id, project_path, inbox_id, post_merge_fname)
```

## 결과 embed 포맷

```
🎉 머지 완료 — 건폐율 기준 최대 사각형 선택 로직

inbox: 2026-04-21-103145
project: dealos

🔀 머지 커밋
  • wt/backend → f94b4ac8

⏰ 머지 시각: 2026-04-21 15:30 KST
📍 base commit: abc1234

📝 변경 요약
### wt/backend — commit f94b4ac8
core/massing/simpleMassGenerator.ts 2곳 수정:
1. _findRectsInAlignedZone iter=0 BCR 제약
2. generateFootprintV2 2-pass 각도 선택
...

🔍 발견 사항
- logger.log에 .toFixed() 3건 — 디버그 로그용, SSOT 대상 아님
- Math.floor 축소로 약간 undershoot 가능, 후속 shrink가 보정

💬 verdict 근거
- 요청 내용 (BCR 이내 최대 사각형 선택) 정확히 구현
- 2중 안전망: iter=0 비례 축소 + angle 2-pass 선택
- 변경 46줄, 단일 파일 — 최소 침습
...

footer: review: 20260421-1130-2026-04-21-103145.md
```

## 새 helper

### `_parse_merge_info(review_path) -> dict`

review-inbox 파일에서 머지 메타 추출:
- `merged: bool`
- `merged_at: str`
- `commits: list[(sub_name, hash)]`
- `base_commit: str`
- `title: str`

**2가지 포맷 동시 지원**:
- YAML 리스트: `merge_commits:\n  - wt/backend: abc1234`
- Markdown 테이블: `| backend | wt/backend | f94b4ac8 | ... |`

DEALOS 와 coord-template 의 review-inbox.md 둘 다 어느 포맷이든 파싱 가능.

### `_post_merge_summary(channel, project_id, project_path, inbox_id, review_filename)`

- review-inbox 파일 로드
- 머지 메타 + 섹션 (v0.19 `_parse_review_sections`) 파싱
- Discord Embed 빌드 (최대 6개 Field)
- 채널에 전송 (실패 시 간단 텍스트 fallback)

## 타임아웃

30분 (1800초). 이유:
- Merge 자체: <1분 per sub
- Validation (type-check, lint, test): 2~10분
- Push + coordination 파일 업데이트 + push: <1분

합계 5~15분 일반적, 큰 프로젝트 느린 CI 라도 30분 충분. 드물게 timeout 되면 사용자에게 에러 메시지 + 수동 확인 안내.

## 하위 호환

- 기존 webhook 경로 (`scripts/notify-discord.sh "🎉 머지 완료"`) 는 그대로 — webhook 채널에 간단 알림 유지
- 봇의 rich embed 는 **verdict prompt 가 올라왔던 채널 (= 작업장 thread 또는 source)** 에 추가 포스트
- 두 메시지 병존 — webhook 은 짧은 알림, embed 는 상세
- 기존 코드 경로 fire-and-forget → synchronous 로 변경됐지만 worker 큐 영향 없음 (reaction handler 는 큐 밖에서 실행)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.24
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 1. @bot send <작업> → head 실행 → verdict prompt
## 2. verdict prompt 에 ✅ 리액션
##    → 🚀 머지 진행 중 (최대 30분 대기)
##    [5~15분 대기]
##    → 🎉 머지 완료 embed (rich summary)
## 3. webhook 도 기존대로 "🎉 머지 완료" 간단 메시지
```

## 오늘 세션 총 7회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | `@bot active` + HEAD_LOCK 경로/경과시간 |
| v0.24 | **머지 완료 rich summary embed** |

자동 orchestration 의 "시작 → 진행 → verdict → 머지" 전체 흐름에 **정보 노출 완성**.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.23 ===== -->

# coord-template v0.23 — `@bot active` + HEAD_LOCK 경로/경과 시간 보정

## 배경

사용자 요청:
> "현재 누가 작업중인지를 확인하는 명령어는 있어야할거같아"

여러 프로젝트 등록 상태에서 **전체 한 눈 파악** 필요. 기존 `@bot status` 는 현재 채널 필터가 적용돼서 다른 프로젝트는 안 보임. 또 `_summarize_status` 가 **v0.12 HEAD_LOCK 경로 버그 잔재** 가 남아서 work_branch 의 HEAD_LOCK 만 체크 (실제론 `<project>-wt-head/coordination/HEAD_LOCK`) → head 돌고 있어도 "⚪ 대기" 오판.

v0.23 에서 두 가지 동시 해결.

## 변경

### 1. `_summarize_status` 경로 수정 + 경과 시간

```python
## 이전 (버그)
lock = coord / "coordination" / "HEAD_LOCK"    # work_branch — 실제 위치 아님
running = "🟢 실행 중" if lock.exists() else "⚪ 대기"

## v0.23
active, started, elapsed_min = _head_lock_info(project_id, project_path)
if active:
    running = f"🔒 작업 중 ({elapsed_min}분 경과) 시작: {started}"
else:
    running = "⚪ 대기"
```

`_head_lock_info()` 헬퍼 신규:
- `_head_lock_path(project_id, project_path)` 로 올바른 head worktree 경로
- HEAD_LOCK 파일 내용이 ISO-8601 timestamp (`date -Iseconds`) → 파싱해서 경과 분 계산
- 반환: `(active, started_str, elapsed_minutes)`

### 2. `@bot active` 신규 명령

```
@bot active
  → 작업 현황 전체

  🔒 **작업 중**
    • `dealos` — 12분 경과 (시작 14:32 KST)

  ⚪ **대기 중**: `sena`

  📬 **pending inbox**
    • `dealos`: 2 건
```

**특징**:
- 채널 필터 무시 — 어느 채널에서 실행해도 전체 프로젝트 조회
- 경과 시간 기준 정렬 (오래된 것부터)
- pending 이 있는 프로젝트만 하단 표시
- 전원 idle 이면 "⚪ 전원 대기" 한 줄

### 3. `@bot status` 와의 차이

| 명령 | 범위 | 용도 |
|------|------|------|
| `@bot status` | 현재 채널 필터 적용 | 이 채널 프로젝트 상세 |
| `@bot status <id>` | 지정 프로젝트만 | 특정 프로젝트 상세 (채널 필터 우회) |
| `@bot active` | **전체 프로젝트** | 글로벌 작업 현황 |

## 구현

### `_head_lock_info()`

```python
def _head_lock_info(project_id: str, project_path: Path) -> tuple[bool, str | None, int]:
    head_lock = _head_lock_path(project_id, project_path)    # <project>-wt-head/.../HEAD_LOCK
    if not head_lock.exists():
        return False, None, 0
    raw = head_lock.read_text(...).strip()
    # ISO-8601 파싱
    ts = datetime.fromisoformat(raw)
    elapsed_min = int((datetime.now(tz) - ts).total_seconds() / 60)
    return True, ts.astimezone().strftime("%H:%M KST"), elapsed_min
```

- ISO 파싱 실패 시 raw string 그대로 반환 (30자 제한)
- 타임스탬프 없어도 "작업 중" 판정은 유지

### `cmd_active()`

모든 `PROJECTS` 순회 (admin 권한 체크만, 채널 필터 X):
```python
for pid, p in PROJECTS.items():
    active, started, elapsed = _head_lock_info(pid, Path(p["path"]))
    ...
```

Busy/idle/pending 세 구획으로 응답 구성.

## 하위 호환

- 기존 `@bot status` 동작 변화 없음 (채널 필터 그대로) — 단 HEAD_LOCK 경로만 수정돼 **정확성 향상**
- `_head_lock_path` 가 이미 존재하던 헬퍼 (v0.12 에서 도입) — 재사용
- ISO-8601 파싱 안 되면 fallback 동작

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.23
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 1. 아무것도 안 돌고 있을 때
@bot active
  → ⚪ 전원 대기 (head 실행 중인 프로젝트 없음)

## 2. dealos 에서 @bot send 후 (head 돌고 있을 때)
@bot active
  → 🔒 dealos — 3분 경과 (시작 14:35 KST)
  → ⚪ 대기 중: sena

## 3. status 도 정확해짐
@bot status dealos
  → **dealos**: 🔒 작업 중 (3분 경과) 시작: 14:35 KST — pending inbox: 1
```

## 오늘 세션 총 6회 릴리스

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt |
| v0.22 | 프로젝트 bot.yml write-through |
| v0.23 | **`@bot active` + HEAD_LOCK 경로/경과시간** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.22 ===== -->

# coord-template v0.22 — `coordination/bot.yml` write-through (프로젝트-측 바인딩 기록)

## 배경

사용자 제안:
> "프로젝트에 coord 를 설치하고 적용한뒤 bot 에 한 번더 레지스트를 해야하는데 bot 에 레지스트하면 해당 경로 프로젝트 설정에 변경되게는 안되나?"

기존 구조의 비대칭:
- 프로젝트 설정 (분산): `coordination/config.yml` 에 저장
- 봇 설정 (중앙): `bot/projects.yml` 에 저장
- 둘이 따로 관리 — 중복 작업 + 이식성 낮음

v0.22 에서 해소 (write-through):
- `@bot register`/`@bot bind`/`@bot unbind` 시 **프로젝트 측 `coordination/bot.yml`** 에도 자동 기록
- 런타임 source of truth 는 여전히 `bot/projects.yml` — 하지만 사본이 프로젝트에 남음
- 프로젝트 git 에 커밋하면 clone 시 채널 정보 따라옴

## 동작

### `@bot register <path>`

```
[Discord]
@bot register c:/path/to/myproject

✅ myproject 등록 완료 (config.yml 자동 감지)
- path: c:/path/to/myproject
- channel: 1234... (이 채널)
- 프로젝트 coordination/bot.yml 에도 기록됨 (git commit 권장)     ← v0.22
이제 이 채널에서 @bot <text> 만 쳐도 자동 라우팅됨
```

생성되는 파일:
```yaml
## c:/path/to/myproject/coordination/bot.yml  (자동 생성)
## coord bot 바인딩 (v0.22+ 자동 생성)
## 이 파일은 봇이 @bot register / @bot bind 시 자동 갱신.
## 중앙 source of truth 는 bot/projects.yml — 이 파일은 프로젝트-side 사본.
## git 에 커밋하면 clone 시 바인딩 정보 이식됨.

discord_channel_id: "1234567890123456789"
updated_at: "2026-04-21 14:30 KST"
```

### `@bot bind <id>` / `@bot unbind <id>`

둘 다 bot.yml 동시 업데이트:
- `bind` → channel 필드 현재 채널로 갱신
- `unbind` → channel 필드 제거 (파일은 남음, 다른 필드 있으면)

두 필드 (`discord_channel_id`, `discord_alert_channel_id`) 모두 없어지면 bot.yml 파일 자체 삭제.

## 구현

### 새 helper

```python
def _project_coord_dir(project_path: Path) -> Path | None:
    """flat (<path>/coordination) 또는 subdir (<path>/.coord/coordination) 감지"""
    ...

def _write_project_bot_yml(
    project_path: Path,
    discord_channel_id: str | None = None,
    discord_alert_channel_id: str | None = None,
    clear_channel: bool = False,
    clear_alert: bool = False,
) -> tuple[bool, str]:
    """bot.yml 읽고 필드 업데이트 후 저장 (PyYAML). 헤더 주석 포함."""
    ...
```

### 통합

- `cmd_register` 성공 시 호출 → 새 bot.yml 생성 (신규 프로젝트)
- `cmd_bind` 성공 시 호출 → 기존 bot.yml 갱신 (또는 생성)
- `cmd_unbind` 성공 시 호출 → bot.yml 에서 channel 필드 제거

### upgrade 보호

`scripts/lib/classify.sh` 의 preserve 목록에 `coordination/bot.yml` 추가:

```bash
## === PRESERVE (사용자 데이터 / 런타임 상태 / 비밀) ===
coordination/config.yml) echo "preserve"; return ;;
coordination/.coord-version) echo "preserve"; return ;;
coordination/bot.yml) echo "preserve"; return ;;     # v0.22
```

→ `upgrade-coord.sh` 실행 시 덮어써지지 않음. 기존 바인딩 유지.

## 이점

| 시나리오 | 이전 | v0.22 |
|---------|------|-------|
| 프로젝트 git clone 후 채널 확인 | 중앙 yml 만 봐야 함 | 프로젝트 bot.yml 파일 바로 확인 |
| 중앙 projects.yml 손상 | 채널 id 모두 재등록 | 각 프로젝트 bot.yml 보며 재구성 |
| 팀원과 바인딩 공유 | 수동 공유 | git 커밋 — 자동 |
| 명시성 | "이 프로젝트는 어디 바인딩?" 확인 번거로움 | 프로젝트 파일 한 줄 |

## 하위 호환

- 기존 `bot/projects.yml` 중심 운영 그대로 작동 — bot.yml 없어도 무관
- 새 등록부터 bot.yml 생김 — 기존 프로젝트는 **재 `@bot register` 또는 `@bot bind`** 시 생성
- bot.yml 수동 삭제해도 다음 bind/register 에 재생성

## 알려진 제약 (v0.23 후보)

- **읽기 연동 없음** — `@bot register <path>` 시 bot.yml 이 이미 있어도 참조 안 함. 항상 현재 채널 기준 갱신. git clone 후 기존 바인딩 자동 복원하려면 v0.23 추가 작업 필요.
- **작업장 bind 명령 없음** — `discord_alert_channel_id` 는 여전히 bot/projects.yml 수동 편집. `@bot bind-alert` 같은 명령 추가 여지 있음.
- **PyYAML 주석 보존 미지원** — bot.yml 은 봇이 전적으로 관리하므로 문제 없지만, 사용자가 수동 편집한 주석은 다음 bind 시 소실. 실사용 영향 미미.

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.22
## bot/controller.py + scripts/lib/classify.sh 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

이미 등록된 프로젝트는 `@bot bind <id>` 한 번 실행해서 bot.yml 생성:
```
@bot bind dealos
@bot bind sena
```

## 테스트

```
## 1. 새 프로젝트 등록
@bot register c:/path/to/new-project
  → ✅ 등록 완료
  → 프로젝트 coordination/bot.yml 에도 기록됨 (git commit 권장)

## 2. 파일 확인
cat c:/path/to/new-project/coordination/bot.yml
  → discord_channel_id: "1234..."
  → updated_at: "2026-04-21 ..."

## 3. 채널 변경 후 rebind
@bot bind new-project  # 다른 채널에서
  → 🔗 ↔ 이 채널 바인딩
  → 프로젝트 coordination/bot.yml 갱신됨

## 4. unbind
@bot unbind new-project
  → 🔓 바인딩 해제
  → 프로젝트 coordination/bot.yml bot.yml 삭제 (바인딩 없음)
```

## 오늘까지 누적 (2026-04-21 세션)

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown 형식 |
| v0.19 | 정보 가시성 (plan 요약 + verdict embed) |
| v0.20 | `@bot register <path>` 자동 id |
| v0.21 | 수동 review 에 verdict prompt 연결 |
| v0.22 | **프로젝트 bot.yml write-through** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.21 ===== -->

# coord-template v0.21 — `@bot review` 수동 실행에 verdict prompt 자동 연결

## 증상

사용자 제보:
> "dealos쪽 세션에서 직접 review 파일 확인해보니 merge를 기다리고있는 상태라는데 디스코드에는 merge 승인 요청이 안넘어왔단말이지"

시나리오:
1. 자동 orchestration 이 Phase E 에서 `⚠ verdict 를 읽지 못함` (v0.18.1 이전 버그들)
2. 사용자가 `@bot review <id>` 수동 실행
3. Claude 가 review 완료하고 파일에 `verdict: go` 기록 → "merge 대기" 상태
4. **봇은 spawn 만 하고 끝 — Discord 에 승인 prompt 안 뜸**
5. 사용자가 머지 승인할 방법 없음 → 수동 Git merge 해야 함

## 원인

**`cmd_review` 가 fire-and-forget** 이었음:

```python
## v0.20 이전
ok, msg = await _spawn_claude_command(...)   # Popen (즉시 return)
await ctx.reply(msg)                          # "✅ spawn" 만 응답, 끝
```

자동 orchestration 의 Phase D fallback 만 `_spawn_and_wait` (synchronous) + `_post_verdict_prompt` 로 prompt 표시. **수동 review 는 이 경로 안 탐**.

v0.18.1 regex 수정으로 자동 orchestration 은 고쳐졌지만, 수동 review 는 prompt 표시 자체가 빠져 있었음.

## 수정

`cmd_review` 를 자동 orchestration 과 동일한 완료 대기 + prompt 경로로 연결:

```python
## v0.21
await ctx.reply(f"⏳ `{project_id}` `{inbox_id}` review 실행 중 (최대 10분)…")

review_prompt = f"/review-inbox {inbox_id} --verdict-only"
ok, out = await _spawn_and_wait(review_prompt, cwd=project_path, timeout=600)
if not ok:
    await ctx.reply(f"❌ review 실패: {out[:500]}")
    return

verdict, review_fname = _read_verdict(project_id, project_path, inbox_id)
if verdict is None:
    await ctx.reply("⚠ verdict 파일에서 읽지 못함 — review-inbox 파일 직접 확인 필요")
    return

await _post_verdict_prompt(
    target_channel, project_id, inbox_id, verdict, review_fname,
    project_path=project_path,
)
```

### `--verdict-only` 플래그 사용 이유

Head 의 Step 7 (머지) 은 **리액션 ✅ 이후에만** 실행. 수동 review 시 Claude 가 바로 머지 confirmation 질문 ("진행할까요? yes/no") 에서 block 되는 것 방지. verdict 판정까지만 수행.

### 응답 메시지 흐름

```
사용자: @bot review 2026-04-21-103145

봇: 🔎 dealos 최근 미review inbox 자동 선택: 2026-04-21-103145    (id 생략 시만)
봇: ⏳ dealos 2026-04-21-103145 review 실행 중 (최대 10분)…

[Claude 실행 — 2~3분]

봇: [verdict prompt embed: ✅ Review 완료 — 머지 진행할까요? + 변경 요약/발견 사항/verdict 근거]
    [✅] [❌]

사용자: ✅ 리액션
봇: 🚀 dealos 2026-04-21-103145 머지 spawn (reaction by @user)
```

리액션 처리는 기존 `_handle_verdict_reaction` 그대로 — `/review-inbox <id> --merge-only --auto-yes` 로 머지 진행.

## 하위 호환

- 자동 orchestration flow 는 변화 없음 (이미 같은 경로 사용)
- `@bot review` 타임아웃 10분 — 대부분 review 는 2~5분이라 충분
- 타임아웃 시 명확한 에러 메시지 (기존엔 fire-forget 이라 타임아웃도 감지 안 됐음)

## 효과

| 케이스 | 이전 | v0.21 |
|--------|------|-------|
| `@bot review <id>` 수동 | spawn 만 → 결과 확인 위해 파일 직접 봐야 함 | 완료 대기 → verdict embed + 리액션 ✅/❌ |
| `@bot review` (id 생략) | 최근 미review 자동 선택만 | + 완료 후 verdict prompt 자동 |
| 자동 orchestration | 이미 verdict prompt 표시 | 동일 |

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.21
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
@bot review 2026-04-21-103145
  → ⏳ review 실행 중 (최대 10분)…
  → [2~3분 대기]
  → [verdict prompt embed + ✅/❌]
  → ✅ 리액션 → 🚀 머지 spawn (v0.14 --merge-only --auto-yes)
```

## 오늘까지 누적 (2026-04-21 세션)

| 버전 | 변경 |
|------|------|
| v0.18.1 | verdict regex markdown 형식 |
| v0.19 | plan 요약 + verdict embed 섹션 |
| v0.20 | `@bot register <path>` 자동 id 추출 |
| v0.21 | **`@bot review` 수동 실행에 verdict prompt 연결** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.20 ===== -->

# coord-template v0.20 — `@bot register <path>` 자동 id 추출

## 배경

어제 세션에서 사용자 제기:
> "하나의 로컬에서 여러 프로젝트를 한다 했을 때 각 프로젝트에서 설정을 완료해도 봇 앱은 하나인데 `@bot register` 로 봇에게도 한번 더 다른 프로젝트에 대한 값을 넣어줘야하나?"

기존: `@bot register <id> <path>` — id 와 path 둘 다 타이핑 필요. 근데 id 는 이미 `<path>/coordination/config.yml` 의 `project.name` 에 명시돼 있어서 **중복 정보**.

v0.20 에서 `@bot register <path>` 1-인자 형식 추가. 봇이 config.yml 을 읽어 id 자동 추출.

## 동작

### 이전 (v0.19 까지)

```
[Discord]
@coord-bot register myapp c:/Users/me/Desktop/projects/myapp
  ✅ myapp 등록 완료 ...
```

### v0.20

```
[Discord]
@coord-bot register c:/Users/me/Desktop/projects/myapp
  ✅ myapp 등록 완료 (config.yml 자동 감지)
  - path: c:/Users/me/Desktop/projects/myapp
  - channel: 1234... (이 채널)
```

봇이 `<path>/coordination/config.yml` 에서:
```yaml
project:
  name: myapp          ← 이 값을 id 로 사용
```

### 수동 id 여전히 가능

config.yml 이 없거나 별칭 쓰고 싶을 때:
```
@coord-bot register myalias c:/Users/me/Desktop/projects/myapp
```

2-인자 형식도 유지.

## 구현

### 새 helper

```python
def _extract_project_name_from_config(project_path: Path) -> str | None:
    """<path>/coordination/config.yml 또는 <path>/.coord/coordination/config.yml 에서
    project.name 추출. 찾지 못하면 None."""
    for rel in ("coordination/config.yml", ".coord/coordination/config.yml"):
        cfg_path = project_path / rel
        if not cfg_path.exists():
            continue
        try:
            with cfg_path.open(encoding="utf-8") as f:
                data = yaml.safe_load(f) or {}
            name = ((data.get("project") or {}).get("name") or "").strip()
            if name:
                return name
        except Exception as e:
            print(f"[register] config.yml 파싱 실패 ({cfg_path}): {e}", flush=True)
    return None
```

flat 설치 (`<path>/coordination/config.yml`) 와 subdir 설치 (`<path>/.coord/coordination/config.yml`) 둘 다 지원.

### `cmd_register` 시그니처 변경

```python
async def cmd_register(
    ctx: commands.Context, first: str, second: str = ""
) -> None:
```

- `second` 가 비어있으면 → 1-arg 모드: `first` = path, id 자동 추출
- `second` 가 있으면 → 2-arg 모드: `first` = id, `second` = path (기존 동작)

### 분기 로직

```python
if not second:
    # 1-arg: path 검증 → config.yml 에서 id 추출
    path = Path(first).expanduser().resolve()
    if not path.is_dir():
        await ctx.reply("⚠ 경로 없음/디렉토리 아님...")
        return
    project_id = _extract_project_name_from_config(path)
    if not project_id:
        await ctx.reply(f"⚠ config.yml 에서 project.name 찾지 못함. 수동 지정: `@bot register <id> {path}`")
        return
else:
    # 2-arg: 명시적 id + path (기존)
    project_id = first
    path = Path(second).expanduser().resolve()
    ...
```

에러 메시지가 "1-arg 실패 → 2-arg 로 재시도" 방법을 알려줘 사용자 복구 쉬움.

## 응답 메시지 변화

1-arg 성공 시 "(config.yml 자동 감지)" 꼬리표로 경로 명시:
```
✅ `myapp` 등록 완료 (config.yml 자동 감지)
- path: `c:/...`
- channel: `1234...`
이제 이 채널에서 @bot <text> 만 쳐도 자동 라우팅됨
```

2-arg 는 기존대로 (꼬리표 없음).

## 사용 시나리오

### 신규 프로젝트 추가 흐름 (v0.20 이후 최소 단계)

```bash
## 1. 프로젝트에 coord 설치 (로컬)
cd /path/to/newproject
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git v0.20 --squash
bash .coord/scripts/init.sh --mode=subdir
## init.sh 가 config.yml 에 project.name 기록

## 2. Discord 대화방에서 (이게 전부)
@bot register /path/to/newproject
  ✅ newproject 등록 완료 (config.yml 자동 감지)
```

이전엔 id 를 수동 타이핑해야 했고, 오타 시 중복 방지 매칭 실패 가능. v0.20 은 config.yml 이 정본이라 한 곳에서만 관리.

## 하위 호환

- 2-arg 형식 `@bot register <id> <path>` 완전 유지
- config.yml 없는 프로젝트도 2-arg 로 등록 가능
- `@bot bind`, `@bot unregister` 등 다른 명령 영향 없음

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.20
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 1-arg 자동 감지
@bot register c:/Users/me/proj
  ✅ proj 등록 완료 (config.yml 자동 감지)

## config.yml 없는 경로 → 에러 안내
@bot register c:/Users/me/nocoord
  ⚠ c:/.../nocoord/coordination/config.yml 에서 project.name 찾지 못함.
  id 명시 필요: @bot register <id> c:/.../nocoord

## 2-arg 명시적
@bot register myalias c:/Users/me/proj
  ✅ myalias 등록 완료 (꼬리표 없음)
```

## 후속 후보

- **B. Sub 진행 알림** (v0.21?) — `coordination/reports/wt-*.md` 생성 감지 시 thread 알림
- **init.sh 출력에 `@bot register` 안내 추가** (하위 작업, 문서 개선)
- **Option C (프로젝트 분산 레지스트리)** — 각 프로젝트가 자기 Discord 메타를 소유. 큰 수술.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.19 ===== -->

# coord-template v0.19 — 정보 가시성 (Plan 요약 + 완료 embed 섹션)

## 배경

사용자 제보: "인포메이션이 적어 그래서 더 오래걸린다고 느끼는거 같아"

head 가 작성하는 풍부한 메타데이터 (plan, tasks, reports, review-inbox 섹션) 가 파일에는 남아있지만 **Discord 로 전달 안 됨** → "봇이 조용히 일하는 것처럼 보여" 체감 지연.

v0.19 에서 두 지점 가시화:

## A. Plan 요약 (head 시작 직후)

`coordination/plans/<inbox-id>.md` 가 head 에 의해 작성되면 자동 파싱 → 한 줄 요약 메시지:

```
📋 dealos Plan — 건폐율 기준 최대 사각형 선택 로직
  단계: 3개, sub: `wt/backend`, `wt/frontend`
  ```
  1. BCR 기준 최대 사각형 선택 로직 구현 `wt/backend`
  2. 프론트엔드 표시 갱신 `wt/frontend`
  3. 통합 검증 `wt/backend`
  ```
```

파싱 대상:
- `# Plan: <title>` → 제목
- `### Step N: <title> [wt/<sub>]` → 단계 수, sub 할당
- 상위 5개 step preview

**Phase 위치**: Phase C (inbox id 탐색) 직후, Phase C-bis (self-review 체크) 직전. head 가 plan 을 먼저 작성하고 → sub dispatch → work 진행 하므로, HEAD_LOCK 해제 시점엔 plan 이 이미 존재.

## C. 완료 요약 (verdict prompt embed)

기존 verdict embed 에 review-inbox 섹션 3개 자동 파싱해 Field 로 추가:

### 이전 (v0.18.1)

```
✅ Review 완료 — 머지 진행할까요?
  inbox: 2026-04-21-103145
  verdict: go (통과)
  리액션: ✅ → 머지 / ❌ → 보류
```

### v0.19

```
✅ Review 완료 — 머지 진행할까요?
  inbox: 2026-04-21-103145
  verdict: go (통과)
  리액션: ✅ → 머지 / ❌ → 보류

📝 변경 요약
### wt/backend — commit `f94b4ac8`
`core/massing/simpleMassGenerator.ts` 2곳 수정:
1. `_findRectsInAlignedZone` iter=0 BCR 제약: 비례 축소
2. `generateFootprintV2` 2-pass 각도 선택: BCR 준수 후보 우선 채택
### 검증 결과 - type-check: PASS - ssot:check: 위반 0건 …

🔍 발견 사항
- logger.log에 .toFixed() 3건 — 디버그 로그용이므로 SSOT 규칙 대상 아님, OK
- Math.floor 축소로 약간 undershoot 가능하나 후속 shrink가 정밀 보정하므로 실용적 영향 없음

💬 verdict 근거
- 요청 내용(BCR 이내 최대 사각형 선택) 정확히 구현
- 2중 안전망: iter=0 비례 축소 + angle 2-pass 선택
- 변경 46줄, 단일 파일 — 최소 침습
```

이제 리액션 ✅ 누르기 전에 **뭐가 바뀌었고 왜 go 인지** 한눈에 파악 가능.

## 구현

### 새 helper 4개

```python
def _plan_path(project_id, project_path, inbox_id) -> Path:
    """head worktree 의 plans/<id>.md 경로"""

def _parse_plan_summary(plan_path) -> dict | None:
    """title / step_count / subs / steps_preview"""

async def _wait_for_plan_file(...) -> Path | None:
    """2분 timeout, 5초 poll"""

def _parse_review_sections(review_path) -> dict:
    """변경 요약 / 발견 사항 / verdict 근거
    level-aware — 섹션 body 는 같거나 상위 level 헤더까지 (하위 subsection 포함)"""

def _find_review_path(project_id, project_path, review_filename) -> Path | None:
    """head_wt → work_branch 순 탐색"""
```

### Level-aware 섹션 추출

```python
def _extract(header_re, level, max_chars=500):
    # level 이하 (더 얕은) 헤더에서 멈춤
    # 예: level=2 (## ) → # 또는 ## 에서 멈춤 (### subsection 은 본문에 포함)
    stop_alts = "|".join("#" * i + r"\s" for i in range(1, level + 1))
    pattern = header_re + r"[^\n]*\n(.*?)(?=\n(?:" + stop_alts + r")|\Z)"
    ...
```

`##` 레벨 섹션 body 에 `###` 서브섹션이 포함되는 게 정상 markdown 인데, 이전 시도 (lookahead `#{1,3}\s`) 가 서브섹션에서 멈춰서 body 가 비어버림. level-aware 로 해결.

### Phase flow 변경

```
Phase A → 📍 head 시작 감지
Phase B (대기 + heartbeat)
Phase B 완료 → ✅ head 완료 감지
Phase C → inbox id 탐색
Phase C-prep (v0.19 A) → 📋 Plan 요약 포스트   ← 신규
Phase C-bis (v0.13) → self-review verdict 체크
Phase D (fallback) → /review-inbox --verdict-only spawn
Phase E → verdict 읽기
Phase F → verdict prompt embed (v0.19 C — 섹션 포함)   ← 확장
```

## 실제 파일 regex 검증 (v0.18.1 반성 반영)

이번엔 DEALOS 의 실제 plan + review-inbox 로 **구현 전 regex 테스트**:

```python
title_m = re.search(r"^#\s+Plan:\s*(.+)$", text, re.MULTILINE)
## → "건폐율 기준 최대 사각형 선택 로직"

step_matches = re.findall(r"^###\s+Step\s+(\d+):\s*(.+?)(?:\s*\[wt/([\w-]+)\])?\s*$", text, re.MULTILINE)
## → [('1', 'BCR 기준 최대 사각형 선택 로직 구현', 'backend')]

## 변경 요약 level 2 추출 → 실제 body 정상 출력 확인
## 발견 사항 level 3 추출 → 정상
## verdict 근거 level 3 추출 → 정상
```

사전 검증 완료 후 릴리스.

## 하위 호환

- plan 파일 없으면 요약 skip (head 가 아직 안 만들었거나 포맷 다른 프로젝트)
- review-inbox 섹션 매칭 실패해도 기존 embed 그대로 표시
- 새 파라미터 `project_path` 는 default `None` 으로 optional — 기존 호출부도 작동

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.19
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
@bot send <작업 지시>
  ⏱ head 세션 스폰 대기 중…
  📍 head 시작 감지 — 작업 진행 중 (10/30/60분 경과 시 알림)
  🕐 head 작업 중 (10분 경과)
  ✅ head 완료 감지 — verdict 확인 중
  📋 Plan — <제목>         ← v0.19 A
    단계: 3개, sub: wt/backend, wt/frontend
    ```
    1. 첫번째 단계 wt/backend
    2. 두번째 단계 wt/frontend
    3. 통합 검증 wt/backend
    ```
  ✨ head self-review 감지 — verdict: go
  [📊 verdict prompt embed — 변경 요약/발견 사항/verdict 근거 포함]  ← v0.19 C
  [✅] [❌]
```

## 후속 후보

- **B. Sub 진행 알림** (v0.20?) — `coordination/reports/wt-*.md` 파일 생성 감지 → "✅ wt/backend 완료 (commit: abc)"
- `@bot register <path>` 자동 추출 (v0.20 B 옵션 잔존)
- inbox `created` 필드 체크 보강

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.18.1 ===== -->

# coord-template v0.18.1 — verdict 정규식 markdown 형식 대응

## 증상

v0.16.8 (review-inbox 경로 수정) 이후에도 사용자 제보:
```
✅ dealos head 완료 감지 — verdict 확인 중
🔎 dealos head 완료 감지 (self-review 없음) — ... review 자동 spawn…
⚠ dealos review 후 verdict 를 읽지 못함 — 수동 @bot review
```

review-inbox 파일은 정상 생성 + `verdict: go` 기록 돼 있는데 봇이 "verdict 를 읽지 못함" 판정.

## 원인

봇의 verdict 추출 정규식이 **markdown 리스트 + 볼드 형식을 못 맞춤**.

**실제 head 가 쓰는 파일 내용**:
```markdown
## Review-Inbox: 건폐율 기준 최대 사각형 선택 로직

- **inbox id**: 2026-04-21-103145
- **reviewed**: true
- **verdict**: go
```

**기존 정규식 (v0.16.8 이전부터)**:
```python
re.search(r"^verdict\s*:\s*(\w[\w-]*)", text, re.MULTILINE | re.IGNORECASE)
```

- `^verdict` → 줄 시작이 `verdict` 여야 함
- 실제 줄은 `- **verdict**:` 로 시작 → **매칭 실패**

결과: 정규식이 항상 실패 → 봇이 verdict 를 영영 못 찾음 → 수동 개입만 가능.

v0.16.8 이 파일 경로는 고쳤지만, 정규식 자체 버그는 남아있었음. 두 번째 잠복 버그.

## 수정

v0.18.1 에서 정규식을 markdown 형식 허용하도록 확장:

```python
_VERDICT_RE = re.compile(
    r"^\s*(?:-\s+)?\*{0,2}verdict\*{0,2}\s*:\s*(\w[\w-]*)",
    re.IGNORECASE | re.MULTILINE,
)
```

구성 요소:
- `^\s*` — 선행 공백
- `(?:-\s+)?` — optional markdown 리스트 불릿
- `\*{0,2}` — optional 볼드 마커
- `verdict` — 리터럴
- `\*{0,2}\s*:\s*` — 닫는 볼드 + 콜론 + 공백
- `(\w[\w-]*)` — verdict 값 (go / needs-fix / block / 등)

### 커버하는 형식

| 형식 | 매칭 |
|------|------|
| `verdict: go` | ✅ |
| `- verdict: go` | ✅ |
| `- **verdict**: go` | ✅ (head 가 실제 쓰는 것) |
| `**verdict**: needs-fix` | ✅ |
| `-   **verdict** : block` | ✅ |
| `Verdict: GO` (case 변주) | ✅ |
| `The verdict says it's fine` | ❌ (정상 — false match 방지) |

### needs-fix 탐색 regex 도 같이 수정

`_find_needs_fix_candidate` 의 패턴:
```python
## 이전
r"verdict\s*:\s*needs[-_]fix"

## v0.18.1
r"\*{0,2}verdict\*{0,2}\s*:\s*needs[-_]fix"
```

볼드 형식도 매칭 가능하도록.

## 실제 파일 테스트

```python
import re
VERDICT_RE = re.compile(r"^\s*(?:-\s+)?\*{0,2}verdict\*{0,2}\s*:\s*(\w[\w-]*)", re.I | re.M)
with open("20260421-1130-2026-04-21-103145.md") as f:
    text = f.read()
m = VERDICT_RE.search(text)
print(m.group(1).lower())  # → 'go'
```

## 효과

| 케이스 | 이전 | v0.18.1 |
|--------|------|---------|
| 자동 orchestration Phase D 후 verdict 읽기 | ❌ 항상 실패 | ✅ 매칭 |
| 수동 `@bot review <id>` 완료 후 verdict 읽기 | ❌ 항상 실패 | ✅ 매칭 |
| head self-review (v0.13) 감지 | ❌ 항상 실패 (같은 regex) | ✅ 매칭 |
| needs-fix 탐색 (`@bot fix` 자동) | ❌ 볼드 형식 놓침 | ✅ 매칭 |

**핵심: v0.13 self-review, v0.16.8 경로 수정, v0.18 status 체크 — 이 모든 것이 이 regex 버그로 **무력화** 돼 있었음.** v0.18.1 로 드디어 전체 체인 정상 작동.

## 하위 호환

- plain `verdict: go` 도 여전히 매칭 (`^\s*(?:-\s+)?\*{0,2}` 이 0회 매칭)
- 기존 파일 재처리 불필요 — regex 만 바뀜

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.18.1
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

이미 verdict 가 기록된 review-inbox 가 있는 상태에서:
```
@bot review
  → (예상) 🔎 dealos ... verdict: `go` (또는 needs-fix/block) 자동 감지
  → Discord verdict prompt embed + ✅/❌ 리액션 자동 표시
```

**수동 `@bot review <id>` 불필요** — 자동 orchestration 이 드디어 완주.

## 반성 (다시)

v0.16.8 에서 파일 경로 고쳤을 때 **실제 파일 내용을 정규식에 넣어보는 검증** 을 안 했음. 경로만 맞으면 된다고 가정. 결과: 파일은 찾지만 내용 파싱 실패 — 두 번째 잠복 버그가 v0.18.1 까지 살아남음.

다음 파일 파싱 관련 수정부턴 **실제 파일 content 로 regex match 확인** 단계 추가.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.18 ===== -->

# coord-template v0.18 — `_latest_unreviewed_inbox` inbox status 헤더 체크 추가

## 배경

사용자 제보: `@bot review` (id 생략) 실행 시 **이미 머지 완료된 old inbox** 를 다시 review 하는 상황 발생. 원인: review-inbox 파일이 rebase / force-push 로 유실되면 `_latest_unreviewed_inbox` 가 해당 inbox 를 "아직 review 안 됨" 으로 판정 → 다시 review 돌림 → Claude 가 "이미 처리 완료된 건" 결론만 내고 종료 (토큰 낭비).

## 수정

`_latest_unreviewed_inbox()` 의 판정 규칙에 **inbox 본문의 `status:` 헤더 체크** 추가:

```python
## v0.18: inbox 자체 status: 헤더가 pending 이 아니면 이미 settled
## → review-inbox 파일 유실돼도 오래된 것 재review 방지
try:
    inbox_text = inbox_file.read_text(...)
    m = re.search(r"^-\s*\*?\*?status\*?\*?:\s*(\S+)", inbox_text, re.I | re.M)
    if m and m.group(1).strip().lower() != "pending":
        continue
except Exception:
    pass
## 기존 review-inbox 체크 로직
if inbox_id not in reviewed_text:
    return inbox_id
```

## 판정 흐름

| inbox status | review-inbox 매칭 | 판정 |
|-------------|------------------|------|
| `pending` | 있음 | reviewed (skip) |
| `pending` | 없음 | **미review — 후보** |
| `merged` | 있음/없음 | settled (skip) |
| `cancelled` | 있음/없음 | settled (skip) |
| `done` | 있음/없음 | settled (skip) |
| `failed` | 있음/없음 | settled (skip) — 재review 안 할 것 |
| 헤더 없음 | 있음 | reviewed (skip) |
| 헤더 없음 | 없음 | **미review — 후보 (기존 동작)** |

즉 status 헤더가 pending 이 아니면 무조건 skip, pending 이거나 없으면 review-inbox 체크.

## 안전성

- inbox 자체 status 는 **메인 세션이 머지 시점에 정본으로 기록** (head 는 안 건드림) → 신뢰 가능
- review-inbox 파일은 rebase/force-push 로 브랜치 간 유실 가능 → 취약
- v0.18 은 **두 소스 중 더 안정적인 inbox status 를 우선** 으로 두어 복원력 향상

## 효과

**이전**: DEALOS 에서 `2026-04-16-162428` (이미 main 까지 merged) 을 `@bot review` 가 자동 선택 → 2.5분 review spawn → `$0.87` 비용 → "이미 처리 완료" 결론만.

**v0.18**: inbox 의 `status: merged` 감지 → 즉시 skip → 다음 후보 (진짜 pending) 선택. 불필요 spawn 0회.

## 하위 호환

- `status: pending` + review-inbox 존재 → 기존대로 skip
- `status: pending` + review-inbox 없음 → 기존대로 후보
- status 헤더 누락된 inbox → 기존대로 review-inbox 만 체크
- 새로 추가된 조건은 **추가 필터** 라 기존 후보가 되지 않던 inbox 가 후보가 되는 일 없음 (false negative 불가)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.18
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

```
## 이미 merged 된 old inbox 의 review-inbox 파일 삭제 후
rm coordination/review-inbox/20260416-1822-2026-04-16-162428.md

@bot review
  → (v0.17 이전) 🔎 dealos 최근 미review inbox 자동 선택: 2026-04-16-162428 ← old 재review
  → (v0.18)     🔎 dealos 최근 미review inbox 자동 선택: 2026-04-20-154801 ← 진짜 pending
```

## 세션 총 9회 릴리스 (2026-04-20)

| 버전 | 핵심 |
|------|------|
| v0.13 | head self-review (Phase D 생략) |
| v0.14 | `/review-inbox` 플래그 (`--verdict-only` / `--merge-only` / `--auto-yes`) |
| v0.15 | orchestration phase 전환 메시지 |
| v0.16 | Option C 자연어 대화 |
| v0.16.1 | 자연어 응답 대화방 라우팅 |
| v0.16.2 | 모호 감지 (regression) |
| v0.16.3 | `/inbox-send` 판정 완화 |
| v0.16.4 | v0.16.2 타이밍 수정 |
| v0.16.5 | Windows cmd.exe 개행 우회 |
| v0.16.6 | CLI spawn 에서 API 키 제거 (Max 보호) |
| v0.16.7 | heartbeat 마일스톤 (10/30/60분) |
| v0.16.8 | review-inbox 경로 head worktree 로 |
| v0.17 | Discord 이미지 첨부 지원 |
| v0.18 | **`_latest_unreviewed_inbox` inbox status 헤더 체크** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.17 ===== -->

# coord-template v0.17 — `@bot send` Discord 이미지 첨부 지원

## 배경

이전엔 `@bot send <텍스트>` 에 이미지를 첨부해도 봇이 **무시**. 에러 스크린샷 / 디자인 mockup / 참고 이미지 공유에 `@bot send` 쓸 수 없었음 (사용자가 로컬 CLI 에서 `/inbox-send` 칠 때만 클립보드 이미지 첨부 가능).

v0.17 에서 Discord 첨부 이미지를 봇이 자동으로 다운로드 → 프로젝트 내 `coordination/inbox/attachments/` 에 저장 → inbox 본문에 `## 첨부` 섹션으로 경로 삽입. head 가 inbox 읽을 때 Read 툴로 이미지 분석 가능.

## 동작

```
[Discord]
@bot send 이 에러 스크린샷 분석해서 원인 수정해줘
[📎 error-2026-04-20.png 첨부]
         ↓
[Bot 내부]
1. ctx.message.attachments 스캔 — image/* MIME 만 허용
2. coordination/inbox/attachments/<ts>-<i>-<safe-name>.png 로 저장
3. 본문 뒤에 "## 첨부\n- [error-2026-04-20.png](coordination/inbox/attachments/20260420-152233-0-error-2026-04-20.png)" 추가
4. /inbox-send <최종 본문> 으로 spawn
         ↓
[Bot 응답]
⏳ dealos 큐 추가 (📎 1) — 즉시 처리

[inbox 파일 생성]
coordination/inbox/2026-04-20-XXXX.md:
  # Inbox: 이 에러 스크린샷 분석해서 원인 수정해줘
  ...
  ## 요청 내용
  이 에러 스크린샷 분석해서 원인 수정해줘

  ## 첨부
  - [error-2026-04-20.png](coordination/inbox/attachments/...png)

[Head 세션]
inbox 읽고 Read 툴로 이미지 분석 → plan 작성
```

## 구현

### `_download_attachments()` 신규 helper

```python
async def _download_attachments(
    message: discord.Message,
    project_id: str,
    project_path: Path,
) -> tuple[list[str], list[Path]]:
    """Discord 첨부 이미지를 coordination/inbox/attachments/ 에 저장.
    반환: (markdown 줄 목록, 저장된 파일 경로 목록)."""
```

- MIME 필터: `image/*` 만 (pdf / video / 일반파일 skip)
- 용량 한도: 25MB (Discord 기본 한도 동일)
- 파일명: `<ts>-<i>-<sanitized_filename>` (path traversal 차단)
- 저장 실패 / skip 시 조용히 다음 첨부로 진행

### `cmd_send` 통합

body 조립 단계에 `## 첨부` 섹션 추가:

```python
attach_lines, saved_attachments = await _download_attachments(
    ctx.message, project_id, project_path
)
final_body = body
if attach_lines:
    final_body = body + "\n\n## 첨부\n" + "\n".join(attach_lines)

job.prompt = f"/inbox-send {final_body}"
```

첨부 카운트는 큐 응답 메시지에 표시:
```
⏳ dealos 큐 추가 (📎 2) — 즉시 처리
```

### 파일명 sanitization

```python
def _sanitize_filename(name: str) -> str:
    safe = re.sub(r"[^A-Za-z0-9._\-가-힣]+", "_", name)
    return safe[:80] or "attachment"
```

- path traversal (`../../`) 차단
- 80자 제한
- 한글 파일명 보존

## 상수

```python
_V017_ALLOWED_ATTACHMENT_MIME_PREFIXES = ("image/",)   # 확장하려면 ("image/", "application/pdf") 등
_V017_MAX_ATTACHMENT_BYTES = 25 * 1024 * 1024          # 25MB
```

## 저장 경로

```
<project>/coordination/inbox/attachments/
  20260420-152233-0-error-screenshot.png
  20260420-152233-1-before-after.jpg
  ...
```

- `.gitignore` 엔 없음 → commit 대상 (의도적 — inbox 와 첨부가 같이 이력 남아야 함)
- DEALOS 등 소비 프로젝트에서 이미 `attachments/` 디렉토리 사용 중 (v0.5 이전부터)

## 에러 처리

- 이미지 외 확장자 → skip + 로그 `[attach] `project_id` skip non-image: <name> (<mime>)`
- 25MB 초과 → skip + 로그
- 다운로드 네트워크 실패 → skip + 로그 — 다른 첨부는 계속 진행
- 첨부 없으면 기존 흐름 그대로

## 하위 호환

- 텍스트만 보내는 기존 `@bot send <text>` 동작 동일
- 첨부 없으면 `## 첨부` 섹션도 없음
- DEALOS 의 `/inbox-send.md` 는 이미 `## 첨부` 섹션 처리 로직 있음 (v0.5 이전) — 봇이 보낸 것도 동일하게 처리됨

## 확장 여지 (v0.18+)

- 이미지 외 파일 지원 (PDF, 텍스트, 로그 등)
- 용량 한도 config 화
- 첨부 여러 개일 때 최대 개수 제한
- 첨부 변환 (PDF → 텍스트 추출 등)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.17
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

DEALOS 같은 소비 프로젝트 추가 변경 불필요 — `/inbox-send.md` 의 `## 첨부` 섹션 해석은 이미 가능.

## 테스트

```
[Discord]
## 에러 스크린샷 첨부한 요청
@bot send 이 에러 분석해줘
[📎 screenshot.png]
  → ⏳ dealos 큐 추가 (📎 1) — 즉시 처리

## 확인
ls coordination/inbox/attachments/ | tail -1
  → 20260420-152233-0-screenshot.png

cat coordination/inbox/<새로 생성된 파일>.md | grep -A 3 첨부
  → ## 첨부
  → - [screenshot.png](coordination/inbox/attachments/20260420-...)
```

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.8 ===== -->

# coord-template v0.16.8 — review-inbox 읽기 경로 head worktree 로 수정 (v0.12 HEAD_LOCK 버그와 동일 패턴)

## 증상

사용자 제보:
```
🔎 dealos head 완료 감지 (self-review 없음) — 2026-04-20-143827 review 자동 spawn…
⚠ dealos review 후 verdict 를 읽지 못함 — 수동 @bot review 2026-04-20-143827
```

review 는 정상 완료 (Claude 로그에 `verdict: go` 출력), 실제 파일도 `<head-wt>/coordination/review-inbox/20260420-1500-2026-04-20-143827.md` 에 생성되어 있는데 봇이 찾지 못하고 사용자에게 수동 재시도 요청.

## 원인 (v0.12 HEAD_LOCK 버그와 동일 패턴)

head 는 coordination 파일을 **`wt/head` 브랜치 (= `<project>-wt-head/` 워크트리)** 에 커밋한다. work_branch 로는 `/review-inbox` 머지 처리 후에만 반영된다.

즉 신규 verdict 는 항상 `<project>-wt-head/coordination/review-inbox/` 에 먼저 쓰이는데, 봇의 여러 helper 가 **project_path (work_branch)** 를 보고 있었다:

| 함수 | 역할 | 기존 (v0.16.7) | 실제 파일 위치 |
|------|------|---------------|---------------|
| `_read_verdict` | verdict 판독 | `project_path/coordination/review-inbox` | `<project>-wt-head/coordination/review-inbox` ✗ |
| `_find_needs_fix_candidate` | needs-fix 자동 탐색 | `project_path/...` | `<head-wt>/...` ✗ |
| `_latest_unreviewed_inbox` | 미review inbox 탐색 | `project_path/...` | `<head-wt>/...` ✗ |
| `_gather_project_state` (chat context) | 최근 verdict 요약 | `project_path/...` | `<head-wt>/...` ✗ |

→ Phase E (verdict 판독) 이 항상 실패 → `⚠ verdict 를 읽지 못함` 안내 → 사용자가 수동 `@bot review` 해도 또 같은 위치를 봐서 또 실패.

이는 v0.12 에서 HEAD_LOCK 경로를 `<project>/coordination/HEAD_LOCK` 에서 `<project>-wt-head/coordination/HEAD_LOCK` 로 수정한 것과 **완전히 같은 패턴** — 당시 HEAD_LOCK 만 수정하고 review-inbox 경로는 놓쳤음.

## 수정

모든 review-inbox 읽기 경로를 **head worktree 우선 + work_branch fallback** 으로 변경.

### `_read_verdict` — head 우선 + fallback

```python
def _read_verdict(
    project_id: str, project_path: Path, inbox_id: str
) -> tuple[str | None, str | None]:
    # 1. head worktree 우선 (신규 verdict 여기에 기록됨)
    head_wt = project_path.parent / f"{project_id}-wt-head"
    head_review_dir = head_wt / "coordination" / "review-inbox"
    verdict, fname = _scan_review_dir_for_verdict(head_review_dir, inbox_id)
    if verdict is not None:
        return verdict, fname

    # 2. work_branch fallback (merge 된 과거 inbox)
    coord = _coord_root(project_path)
    work_review_dir = coord / "coordination" / "review-inbox"
    return _scan_review_dir_for_verdict(work_review_dir, inbox_id)
```

호출부에 `project_id` 추가.

### `_find_needs_fix_candidate` — 양쪽 모두 스캔 후 최신순

needs-fix 판정은 merge 안 된 새 것일 수도, 이미 merge 후 남은 것일 수도 있어 **양쪽 다 스캔**. mtime 기준 최신 먼저.

### `_latest_unreviewed_inbox` — 양쪽 review-inbox 모두 체크

"review 됨" 판정 시 양쪽 디렉토리의 파일 내용을 합쳐서 스캔. head 에 있어도 work_branch 에 있어도 "review 됨" 으로 간주.

### `_gather_project_state` (Option C chat context)

head worktree 에 review-inbox 있으면 거기를 우선. 없으면 work_branch.

### 내부 helper

```python
def _scan_review_dir_for_verdict(
    review_dir: Path, inbox_id: str
) -> tuple[str | None, str | None]:
    """review-inbox 디렉토리 1곳에서 verdict 스캔. 내부 helper."""
```

공통 스캔 로직 추출. 경로만 바뀌는 패턴이라 재사용.

## 동작 변화

```
[이전 v0.15 ~ v0.16.7]
head self-review 없음 → Phase D fallback spawn → ✅ 완료
→ _read_verdict(project_path) → work_branch 체크 → (None, None)
→ "⚠ verdict 를 읽지 못함 — 수동 @bot review"
사용자 @bot review → 또 같은 위치 → 또 실패 (무한 루프)

[v0.16.8]
head self-review 없음 → Phase D fallback spawn → ✅ 완료
→ _read_verdict(project_id, project_path) → head-wt 먼저 체크 → verdict: go 발견
→ ✨ Discord verdict prompt embed 정상 표시
```

## 하위 호환

- `project_id` 인자가 추가됐지만 봇 내부 helper 라 외부 호환성 영향 0
- work_branch 만 설정된 프로젝트 (head worktree 없음) → fallback 으로 동일 동작
- merge 완료된 inbox 의 verdict 조회도 정상 (work_branch 에 복사돼 있어서)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.8
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

사용자가 확인했던 시나리오:
```
@bot send <작업 지시>
...head 작업 (15분)...
✅ head 완료 감지 — verdict 확인 중
```

기대:
- head self-review 했으면 → ✨ self-review 감지 메시지
- 안 했으면 → 🔎 Phase D fallback → review 완료 → verdict prompt embed 자동 표시

**수동 `@bot review` 필요 없음** — verdict 판독 자동화 완성.

## v0.16.x 최종 정리

| 버전 | 변경 |
|------|------|
| v0.16 | Option C 자연어 대화 |
| v0.16.1 | 자연어 응답 대화방 라우팅 |
| v0.16.2 | inbox 감지 (regression) |
| v0.16.3 | `/inbox-send` 판정 완화 |
| v0.16.4 | 감지 타이밍 수정 (poll) |
| v0.16.5 | Windows cmd.exe 개행 우회 |
| v0.16.6 | CLI spawn 에서 API 키 제거 (Max 구독) |
| v0.16.7 | heartbeat 마일스톤 (10/30/60분) |
| v0.16.8 | **review-inbox 경로 head worktree 로 수정** |

## 반성

v0.12 에서 HEAD_LOCK 경로를 head worktree 로 수정했을 때 같은 위치 (review-inbox) 의 다른 읽기 코드는 점검 안 함. "비슷한 구조의 경로는 한꺼번에 훑어본다" 가 부족했음. 다음 경로 관련 수정부턴 **grep "project_path.*coordination"** 전역 점검 루틴 가미.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.7 ===== -->

# coord-template v0.16.7 — heartbeat 마일스톤 방식 (5분 주기 → 10/30/60분 1회씩)

## 사용자 피드백

> "5분 간격으로 작업중 메시지는 과하다"

v0.15 도입한 5분 주기 heartbeat 이 1시간 작업에 12회 메시지 → 채널 시끄러움. **마일스톤 방식** 으로 전환해 최대 3회로 축소.

## 변경

**v0.15 ~ v0.16.6 (주기 반복)**:
```
🕐 head 작업 중 (5분 경과)
🕐 head 작업 중 (10분 경과)
🕐 head 작업 중 (15분 경과)
🕐 head 작업 중 (20분 경과)
...  ← 1시간이면 12회
```

**v0.16.7 (마일스톤 1회씩)**:
```python
_V016_7_HEARTBEAT_MILESTONES_MIN: list[int] = [10, 30, 60]
```
```
🕐 head 작업 중 (10분 경과)    ← 1회
🕐 head 작업 중 (30분 경과)    ← 1회
🕐 head 작업 중 (60분 경과)    ← 1회
```

- 10분 미만 작업 → 메시지 0개
- 10~30분 → 1개
- 30~60분 → 2개
- 60분 이상 → 3개 (HEAD_LOCK 타임아웃 60분 근접 → Phase B timeout 메시지로 이어짐)

## 구현 변경

### 상수

```python
## 이전 (v0.15)
_V015_HEARTBEAT_INTERVAL_SEC = 300

## v0.16.7
_V016_7_HEARTBEAT_MILESTONES_MIN: list[int] = [10, 30, 60]
```

### `_head_heartbeat()` 재작성

주기 `asyncio.sleep(interval)` 반복 → 마일스톤별 `asyncio.sleep(target_elapsed - now)`:

```python
for milestone in milestones_min:
    target_elapsed = milestone * 60
    sleep_duration = target_elapsed - (time.time() - start_ts)
    if sleep_duration > 0:
        await asyncio.sleep(sleep_duration)
    if not head_lock.exists():
        return  # head 완료 — 남은 마일스톤 skip
    await target_channel.send(f"🕐 head 작업 중 ({milestone}분 경과)")
```

### 안내 메시지

```
이전: 📍 head 시작 감지 — 작업 진행 중 (5분 간격 heartbeat)
v0.16.7: 📍 head 시작 감지 — 작업 진행 중 (10/30/60분 경과 시 알림)
```

사용자가 예상 가능한 알림 시점을 명시.

## 튜닝

더 자주 피드백 원하면 상수 확장:
```python
_V016_7_HEARTBEAT_MILESTONES_MIN = [5, 15, 30, 45, 60]
```

더 조용히:
```python
_V016_7_HEARTBEAT_MILESTONES_MIN = [30, 60]  # 2회만
```

완전 off:
```python
_V016_7_HEARTBEAT_MILESTONES_MIN = []
```

## 하위 호환

- v0.15 의 phase 전환 메시지 (⏱/📍/✅) 은 그대로 유지
- `_V015_HEARTBEAT_INTERVAL_SEC` 상수는 **제거됨** — 외부에서 참조하던 코드 없음 확인
- DEALOS 등 소비 프로젝트엔 영향 없음 (봇 내부 로직만)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.7
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

## 테스트

30분짜리 head 작업 예시:

```
[작업장]
📥 dealos 작업 시작 ...
⏱ dealos head 세션 스폰 대기 중…
📍 dealos head 시작 감지 — 작업 진행 중 (10/30/60분 경과 시 알림)
🕐 dealos head 작업 중 (10분 경과)    ← 1번째
(20분 조용)
🕐 dealos head 작업 중 (30분 경과)    ← 이때 마침 head 도 완료
✅ dealos head 완료 감지 — verdict 확인 중
✨ dealos head self-review 감지 — verdict: go
[Verdict prompt embed + ✅/❌]
```

v0.15 대비 약 4분의 1 수준 메시지량.

## v0.16.x 최종 정리

| 버전 | 변경 |
|------|------|
| v0.16 | Option C 자연어 대화 |
| v0.16.1 | 자연어 응답 대화방 라우팅 |
| v0.16.2 | inbox 감지 (regression) |
| v0.16.3 | `/inbox-send` 판정 완화 |
| v0.16.4 | 감지 타이밍 수정 (poll) |
| v0.16.5 | Windows cmd.exe 개행 우회 |
| v0.16.6 | CLI spawn 에서 API 키 제거 (Max 구독 보호) |
| v0.16.7 | **heartbeat 마일스톤 방식 (5분 → 10/30/60분)** |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.6 ===== -->

# coord-template v0.16.6 — Claude CLI spawn 환경에서 ANTHROPIC_API_KEY 제거 (Max 구독 보호)

## 증상

v0.16.5 이후 사용자 제보:
```
⚠ dealos inbox 생성 안 됨 (60초 대기 후에도 파일 없음)
log: {"result":"Credit balance is too low", "duration_ms":477}
```

Claude CLI 가 477ms 만에 **크레딧 부족** 이유로 즉시 거부. 하지만 사용자는 Claude Max 5x 구독 중 — 이론적으로 구독 한도 내 무료여야 함.

## 원인 — v0.16 의 숨은 회귀

Claude CLI 의 인증 우선순위:
1. `ANTHROPIC_API_KEY` 환경변수 존재 → **API 과금 모드** (pay-per-token, API 크레딧 필요)
2. 없으면 → 저장된 OAuth 자격증명 → **Max / Pro 구독 모드** (구독 한도 내 무료)

v0.16 Option C 릴리스에서 `.env.local` 에 `ANTHROPIC_API_KEY` 를 추가하라고 안내했다. 봇 기동 시 `source .env.local` 하면 **subprocess.Popen 이 기본적으로 부모 env 를 상속** → spawn 되는 Claude CLI 도 이 키를 받음 → CLI 가 API 모드로 전환 → 구독 무시하고 API 크레딧 요구.

즉 v0.16 이전: 키 없음 → CLI → Max OAuth → 무료
v0.16 이후: 키 있음 (Python SDK 용) → CLI 에도 상속 → API 모드 → 크레딧 없으면 거부

사용자는 "Option C 를 위해 API 키 설정했을 뿐" 인데 기존 agent spawn 전체가 API 과금으로 전환된 것. **실질적으로 Max 구독이 무력화됨**.

## 수정

봇의 **spawn 경로** (Claude CLI 호출) 에서만 `ANTHROPIC_API_KEY` 제거. 봇 자체의 Python SDK 호출 (Option C 자연어 대화) 은 이 키가 여전히 필요하므로 영향 없음.

```python
def _claude_cli_env() -> dict[str, str]:
    """Claude CLI spawn 용 환경변수 — ANTHROPIC_API_KEY 제거.
    CLI 가 저장된 OAuth 자격증명 (Max/Pro 구독) 을 쓰도록 유도."""
    env = os.environ.copy()
    env.pop("ANTHROPIC_API_KEY", None)
    env.pop("ANTHROPIC_AUTH_TOKEN", None)
    return env
```

`_spawn_claude_command` (Popen) / `_spawn_and_wait` (asyncio) 둘 다 `env=_claude_cli_env()` 전달.

### 두 경로 분리 요약

| 경로 | 용도 | API 키 | 과금 |
|------|------|--------|------|
| **Python SDK** (`_ANTHROPIC_CLIENT`) | 봇 자체 자연어 대화 (Option C) | `ANTHROPIC_API_KEY` 필요 | API pay-per-token (계정 크레딧) |
| **Claude CLI** (spawn) | head/sub/inbox-send 등 agent | API 키 **제거** → OAuth 폴백 | Max/Pro 구독 한도 내 |

## 기동 로그

```
[bot] ✅ Anthropic SDK 활성화 (Option C 자연어 대화 가능)
[bot] ℹ Claude CLI spawn 은 ANTHROPIC_API_KEY 제거 후 실행 → Max/Pro 구독 OAuth 사용 (v0.16.6)
```

두 번째 줄이 나오는지 확인하면 분리 활성 확인.

## 영향

| 사용자 유형 | v0.16.5 | v0.16.6 |
|-----------|---------|---------|
| Max/Pro 구독 + Option C 활성 | ❌ CLI 도 API 모드 → 크레딧 요구 | ✅ 봇 chat 만 API, CLI 는 구독 |
| API only (구독 없음) + Option C | CLI 가 API 쓰는 게 의도 | **주의**: v0.16.6 이후엔 CLI 가 OAuth 찾는데 없으면 로그인 필요 |
| Max 구독 + Option C 비활성 (키 없음) | 정상 (CLI OAuth) | 정상 (동일) |
| Option C 비활성 | 영향 없음 | 영향 없음 |

### API only 사용자 대응

`ANTHROPIC_API_KEY` 로 CLI 를 돌리고 싶은 사용자 (구독 없이 API 만 쓰는 경우) 는 지금까지 자연스레 동작했지만 v0.16.6 이후엔 OAuth 가 필요. 대안:
- `claude setup-token` 으로 CLI 에 별도 인증 (권장)
- 또는 CLI 호출 전 env override — 추후 config 플래그 검토 (v0.16.7+ 후보)

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.6
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
## 로그에 "Claude CLI spawn 은 ANTHROPIC_API_KEY 제거" 라인 확인
```

## 테스트

v0.16.5 와 동일한 메시지 재전송:
```
@bot send 오류 해결 요청
## Error Type
Console TypeError
...
```

기대:
- Claude CLI 가 OAuth (Max 구독) 로 실행 → 크레딧 요구 없음
- 스택트레이스 포함한 inbox 파일 실제 생성
- `📥 dealos 작업 시작` + thread + head spawn

## 반성

v0.16 Option C 릴리스 때 "ANTHROPIC_API_KEY 가 subprocess 로 상속되는 것" 을 고려하지 못했음. 환경변수의 **프로세스 트리 상속** 은 POSIX / Windows 공통 기본 동작이라 의도치 않은 side effect 가 일반적. SDK 와 CLI 가 인증 방식 다를 때 경로 분리가 필요. 이번 교훈 기록 (`coordination/hotfixes.md` 에 추가될 수 있게 사용자 repo 에서 반영 권장).

## v0.16.x 계열 (업데이트)

| 버전 | 변경 | 상태 |
|------|------|------|
| v0.16 | Option C 자연어 대화 도입 | 정상 but 2개 숨은 regression |
| v0.16.1 | 자연어 응답 대화방 라우팅 | 정상 |
| v0.16.2 | inbox 감지 (타이밍 버그) | 버그 |
| v0.16.3 | `/inbox-send` 판정 규칙 완화 | 정상 |
| v0.16.4 | v0.16.2 타이밍 버그 수정 | 정상 but Windows 버그 드러남 |
| v0.16.5 | Windows cmd.exe 개행 mangling 우회 | 정상 but Max 구독 버그 드러남 |
| v0.16.6 | **CLI spawn 에서 API 키 제거** — Max 구독 OAuth 복구 | 실사용 정상 기대 |

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.5 ===== -->

# coord-template v0.16.5 — Windows cmd.exe 개행 mangling 우회

## 증상

v0.16.4 로 inbox 감지 타이밍 버그는 고쳤는데, 에러 스택 + "해결" 요청이 **여전히** 거절됨. 로그 확인:

```
파일 쓰기 권한이 거부되었습니다.

또한 입력 내용(`## Error Type`)이 실제 에러 내용 없이 헤더만 있는 상태입니다...
```

두 가지 증상이 동시에:
1. Claude 가 사용자 메시지의 **첫 줄만** 봄 (스택트레이스 전부 소실)
2. "파일 쓰기 권한 거부" — `--permission-mode bypassPermissions` 가 적용 안 됨

## 원인 (Windows 고유)

봇의 `_spawn_claude_command` 는 `shutil.which("claude")` 가 반환한 `claude.CMD` wrapper 를 subprocess.Popen 으로 호출한다. `.cmd` 파일은 **cmd.exe 를 통해 실행되는데, cmd.exe 는 명령줄 인자에 포함된 개행을 명령 구분자로 오해해서 자른다.**

즉 사용자가 이렇게 보내면:
```
@bot send 오류 해결 요청
## Error Type
Console TypeError
...stack trace...
```

subprocess 로 전달되는 실제 argv:
```
["claude.CMD", "-p", "/inbox-send 오류 해결 요청\n## Error Type\n...", "--permission-mode", "bypassPermissions", ...]
```

cmd.exe 가 첫 `\n` 에서 잘라서 Claude 가 받는 건:
```
claude -p "/inbox-send 오류 해결 요청"
(나머지 인자 모두 소실 — --permission-mode, --output-format, --model 포함)
```

그래서:
1. Claude 는 `## Error Type` 헤더 한 줄만 본 것처럼 말함 (실제론 "오류 해결 요청" 한 줄만 받았고, 이게 잘못 해석됨)
2. `--permission-mode bypassPermissions` 가 잘려 기본 "ask" 모드로 실행 → 파일 쓰기 거부

## 수정

**Windows 에서 `claude.cmd` wrapper 를 파싱해 내부의 `node + cli.js` 구문을 추출하고, 이걸 직접 호출.** Python subprocess → CreateProcess → node.exe 경로는 cmd.exe 를 거치지 않으므로 개행 보존됨.

```python
def _resolve_claude_invocation() -> list[str]:
    """Windows .cmd wrapper 우회. npm-style wrapper 의 내부 구문 파싱."""
    if os.name != "nt":
        return [CLAUDE_BIN]
    claude_path = Path(CLAUDE_BIN)
    if claude_path.suffix.lower() not in (".cmd", ".bat"):
        return [CLAUDE_BIN]
    content = claude_path.read_text(encoding="utf-8", errors="ignore")
    # "<node>" "<...>\cli.js" %*  패턴
    m = re.search(r'"([^"]+\.js)"\s+%\*', content)
    if not m:
        return [CLAUDE_BIN]
    wrapper_dir = str(claude_path.parent)
    resolved = m.group(1).replace("%dp0%", wrapper_dir + "\\").replace("%~dp0", wrapper_dir + "\\")
    script_path = Path(resolved.replace("\\\\", "\\"))
    if not script_path.exists():
        return [CLAUDE_BIN]
    node_bin = shutil.which("node")
    if not node_bin:
        return [CLAUDE_BIN]
    return [node_bin, str(script_path)]


CLAUDE_INVOCATION = _resolve_claude_invocation()  # 봇 기동 시 1회 결정
```

`_spawn_claude_command` / `_spawn_and_wait` 에서:
```python
cmd = [*CLAUDE_INVOCATION, "-p", claude_prompt, "--permission-mode", "bypassPermissions", ...]
```

## 기동 시 확인

봇 시작 로그에 다음 한 줄 추가:
```
[bot] ✅ Windows cmd.exe 우회 — node + cli.js 직접 호출 (v0.16.5)
```

이 메시지가 안 뜨고 `CLAUDE_BIN` 그대로 사용되면 wrapper 파싱 실패 (.cmd 구조가 예상과 다름) → 로그에 fallback 사유 출력.

## Fallback

파싱 실패 / cli.js 없음 / node 없음 중 어느 상황이든 v0.16.4 이전 동작 (`CLAUDE_BIN` 직접 호출) 유지. 즉 v0.16.5 가 파싱에 실패해도 **봇은 계속 돌아감** (그 대신 이전 버그 상태). 파싱 성공 시에만 우회 활성.

## 플랫폼별 영향

| OS | 변화 |
|----|------|
| Windows | `.cmd` 파싱 성공 시 node 직접 호출 — 개행 보존 ✅ |
| macOS / Linux | `os.name != "nt"` → 기존 CLAUDE_BIN 그대로 (영향 없음) |
| WSL | Linux 환경 감지 → 기존 경로 |

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.5
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
## 로그에 "Windows cmd.exe 우회" 메시지 뜨는지 확인
```

## 테스트

```
@bot send 오류 해결 요청
## Error Type
Console TypeError

## Error Message
TypeError: Cannot read property...
    at foo (file.ts:10:5)
```

→ 기대:
- Claude 가 전체 스택트레이스 인지
- `--permission-mode bypassPermissions` 정상 적용 → 파일 쓰기 성공
- inbox 생성 + head spawn

## v0.16.x 계열 최종 정리

| 버전 | 변경 | 상태 |
|------|------|------|
| v0.16 | Option C 자연어 대화 도입 | 정상 |
| v0.16.1 | 자연어 응답 대화방 라우팅 | 정상 |
| v0.16.2 | inbox 감지 (❌ eager check 타이밍 버그 — regression) | 버그 |
| v0.16.3 | `/inbox-send` 판정 규칙 완화 | 정상 (but v0.16.2 버그로 가려짐) |
| v0.16.4 | v0.16.2 타이밍 버그 수정 (eager → poll) | 정상 (but 윈도우 cmd.exe 버그 드러남) |
| v0.16.5 | **Windows cmd.exe 개행 mangling 우회** | ← 드디어 실제 작동 |

## 교훈

- subprocess 플랫폼 차이 (Windows vs Unix) 는 always-overlooked
- 실사용 테스트 없이 구조 변경하지 말 것 (v0.16.2 → v0.16.4 → v0.16.5 세 번 거침)
- multi-line shell 인자는 플랫폼에 관계없이 fragile — stdin 또는 temp file 이 더 안전

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.4 ===== -->

# coord-template v0.16.4 — inbox 생성 감지 타이밍 버그 수정 (v0.16.2 regression)

## 실사용 제보

사용자가 Next.js 에러 스택 + "오류 해결 요청" 을 `@bot send` 로 보냈는데 v0.16.3 의 `/inbox-send` 규칙 완화에도 불구하고 여전히 "inbox 생성 안 됨" 메시지. 로그 확인:

- `/tmp/bot-dealos-inbox-send-*.log` = **0 bytes** (Claude 백그라운드 실행 중)
- `coord-bot-*.log`: `[queue] dealos /inbox-send 성공했으나 inbox 파일 생성 없음 — orchestration skip`

## 원인

v0.16.2 에서 추가한 inbox 감지 로직이 **타이밍 버그**:

1. `_spawn_claude_command` 는 `subprocess.Popen(...)` **fire-and-forget** 방식
2. spawn 즉시 `ok=True` 반환, Claude 는 백그라운드에서 실행
3. v0.16.2 코드가 **spawn 직후 즉시** inbox/ 디렉토리 체크
4. 이 시점에 Claude 는 아직 파일을 쓸 시간 없음 → `inbox_post - inbox_pre == empty`
5. 봇이 "inbox 미생성" 으로 오판 → orchestration skip
6. **사용자 요청이 명확하든 모호하든 상관 없이 항상 거절** (!!)

이 버그는 v0.16.2 릴리스 후 모든 `@bot send` 를 무력화했음. 초기 DEALOS 테스트에서만 작동한 건 "아직 v0.16.2 미반영" 이었기 때문.

## 수정

eager 체크 → **poll 방식** 으로 교체:

```python
_V016_INBOX_CREATION_WAIT_SEC = 60    # 최대 대기
_V016_INBOX_CREATION_POLL_SEC = 3     # 3초 간격 체크

ok, msg = await _spawn_claude_command(...)

inbox_created = False
if ok:
    poll_deadline = time.time() + _V016_INBOX_CREATION_WAIT_SEC
    while time.time() < poll_deadline:
        await asyncio.sleep(_V016_INBOX_CREATION_POLL_SEC)
        if inbox_dir.exists():
            inbox_post = {...}
            if inbox_post - inbox_pre:
                inbox_created = True
                break
```

- Claude 가 실제로 inbox 파일을 생성할 때까지 최대 60초 대기
- 생기면 → 정상 orchestration 진행
- 60초 내 안 생기면 → 모호 판정 / 실패로 간주 → skip + 명확한 안내

## 왜 60초?

- Claude CLI 로딩 + `/inbox-send.md` 파싱 + context build: 5~15초
- 파일 쓰기 + 클립보드 이미지 첨부: 5~10초
- 네트워크 지연 / git commit: 5초
- 모호 판정 → 거절 응답: 5~10초

간단 요청은 15~30초면 끝, 복잡한 것도 40~50초. 60초 여유.

## 영향 / 테스트

| 케이스 | v0.16.2-3 (버그) | v0.16.4 |
|--------|-----------------|---------|
| 명확한 `@bot send` | ❌ 항상 skip (버그) | ✅ 정상 orchestration |
| 에러 덤프 + "해결" | ❌ skip | ✅ inbox 생성 감지 (v0.16.3 규칙과 결합) |
| 모호한 `@bot send` | ❌ skip (이유 틀림) | ✅ 60초 후 skip (정상) |
| `@bot send` 부하 (10초 대기) | ❌ skip | ✅ 감지 |

## 하위 호환

- 명확한 flow → 이제야 제대로 작동 (v0.16.2 이후 첫 정상)
- 모호한 flow → 60초 대기 후 알림 (v0.16.2 의도한 동작, 이번엔 실제로 작동)
- Phase A (HEAD_LOCK 감지) 는 기존대로 poll 성공 후 시작

## 업그레이드 (봇 측만)

```bash
bash scripts/upgrade-coord.sh --version v0.16.4
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe      # Windows
bash scripts/start-bot.sh --bg
```

DEALOS 슬래시 명령은 v0.16.3 이면 OK (변경 없음).

## 반성 / 교훈

v0.16.2 는 행동 테스트 없이 머지됐음. fire-and-forget spawn 의 의미 (Popen vs await) 를 코드에서 놓침. 타이밍 관련 변경은 반드시 실제 spawn + inbox 생성 흐름으로 검증 필요.

## v0.16.x 계열 총정리

| 버전 | 변경 |
|------|------|
| v0.16 | Option C: Anthropic 자연어 대화 도입 |
| v0.16.1 | 자연어 대화 응답을 대화방으로 강제 라우팅 |
| v0.16.2 | 모호 감지 (❌ 타이밍 버그 있음 — 모든 spawn 거절) |
| v0.16.3 | `/inbox-send` 명확/모호 판정 규칙 완화 (슬래시 명령 쪽) |
| v0.16.4 | v0.16.2 타이밍 버그 수정 (eager → poll) ← **정상 작동은 이때부터** |

v0.16.2 는 사실상 회귀. v0.16.4 가 Option C 의 첫 정상 릴리스.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.3 ===== -->

# coord-template v0.16.3 — /inbox-send 명확/모호 판정 규칙 완화

v0.16.2 에서 모호한 `@bot send` 시 타임아웃 대신 즉시 에러 안내로 바꿨다. 그런데 **근본 원인** — `/inbox-send.md` 슬래시 명령이 "명확 vs 모호" 판정을 너무 엄격하게 해서 **정상 요청도 거절하는 것** — 은 남아있었다.

실사용 사례: 사용자가 Next.js 에러 스택 붙여넣고 "오류 해결" 지시 → `/inbox-send` 가 `## Error Type` 헤더 보고 "뭘 묻는 건지 모르겠다" 로 오독 → 거절.

v0.16.3 에서 판정 기준을 세분화해 이런 정상 케이스가 통과하도록 규칙 완화.

---

## 변경

`.claude/commands/inbox-send.md` 의 "품질 규칙" 섹션 재작성.

**이전 (너무 엄격)**:
```
- 요청이 모호하면 파일 생성 전 사용자에게 되물어라 (추측 금지)
```

**v0.16.3**: 아래 중 **하나라도** 해당하면 **명확** 으로 간주하고 즉시 inbox 생성:

| 기준 | 예시 |
|------|------|
| 동작 지시어 | 추가 / 수정 / 삭제 / 고치다 / 해결 / 디버그 / 리팩터 / 분석 / 설명 / 테스트 / 문서화 |
| 대상 지칭 | 파일명, 함수명, 에러 메시지, 스택 트레이스, 로그, 코드 스니펫, 기능명 |
| 에러/로그/코드 덤프 본문 | 짧은 지시어 + 긴 덤프 구조 ("fix this:<stack>") |
| 첨부 이미지 | `clipboard-to-attachment.sh` 자동 저장 포함 |

**모호** 는 위 네 항목 **전부 부재**:
- "뭐 해봐" / "알아서"
- 단독 헤더 한 줄 (`## Error Type`) — 본문 없음
- 의미 없는 단어 조합

**원칙**: 의심될 땐 되묻지 말고 **일단 inbox 기록**. head 가 처리 단계에서 추가 질의 가능. inbox 진입 장벽 낮춰야 "에러 덤프 + 해결" 같은 실무 케이스가 막히지 않음.

---

## 영향

| 케이스 | 이전 | v0.16.3 |
|--------|------|---------|
| `@bot send Next.js 에러 스택 붙여넣기 + 해결해줘` | 거절 (false-reject) | 통과 → inbox 생성 |
| `@bot send README 에 한 줄 추가` | 통과 | 통과 (동일) |
| `@bot send 뭐 해봐` | 거절 | 거절 (동일) |
| `@bot send ## Error Type` (단독 헤더) | 거절 | 거절 (동일) |

---

## 하위 호환

- 기존 명확한 요청은 모두 동일하게 처리
- 기존 모호한 요청 (진짜 지시 없음) 도 동일하게 거절
- 변경된 것: 이전엔 "애매해서 거절" 하던 **에러 덤프 + 해결 요청** 류가 이제 통과

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.3
## .claude/commands/inbox-send.md 는 manual-merge — diff 확인 후 승인

## 봇 재시작 불필요 (슬래시 명령은 Claude CLI 가 매 spawn 마다 읽음)
```

---

## 테스트

```
## 에러 덤프 + 해결 (이전엔 막혔음)
@bot send
## Error Type
Build Error

## Error Message
Failed to compile

오류 해결해줘
  → (예상) 📥 작업 시작 + inbox 생성 + head spawn

## 진짜 모호 (이전/지금 동일 거절)
@bot send 뭐
  → (예상) "요청이 모호합니다 — 되묻기"
```

---

## 세트 정리 (v0.16.x 계열)

| 버전 | 변경 |
|------|------|
| v0.16 | Option C: `@bot <text>` Anthropic 자연어 대화 도입 |
| v0.16.1 | 자연어 대화 응답을 대화방으로 강제 라우팅 |
| v0.16.2 | 모호한 `@bot send` 시 inbox 미생성 감지 + orchestration skip (봇측) |
| v0.16.3 | `/inbox-send` 명확/모호 판정 규칙 완화 (슬래시 명령측, 근본 원인) |

실사용 제보로 발견된 패턴을 양쪽 방향에서 모두 대응.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.2 ===== -->

# coord-template v0.16.2 — 모호한 @bot send 처리 수정 + task_done() double-call 버그 정리

## 증상 (실사용 버그)

사용자가 `@bot send ## Error Type` 처럼 모호한 텍스트를 보냈을 때:

1. 봇이 `/inbox-send` spawn → Claude CLI 가 "요청이 모호하다" 고 판단 → inbox 파일 **안 만듦**
2. 그런데 봇은 spawn 자체는 성공 (`ok=True`) 이라 다음 단계 진행
3. Phase A (HEAD_LOCK 생김 대기) 에서 **3분 타임아웃** — head 가 안 뜰 거라는 걸 봇이 모르므로
4. 사용자는 3분 내내 spinning, 결국 `⚠ head 세션이 시작 안 됨` 메시지

원인: 봇이 **"spawn 성공 == inbox 생성됨"** 으로 가정. 실제로는 Claude 가 내부 판단으로 파일 생성 거부 가능.

---

## 수정

### 1. inbox 스냅샷 비교로 실제 생성 여부 판정

`_project_worker()` 에서 spawn 전후 `coordination/inbox/*.md` 파일 목록 비교:

```python
inbox_dir = _coord_root(job.project_path) / "coordination" / "inbox"
inbox_pre = {p.name for p in inbox_dir.glob("*.md") if p.name != ".gitkeep"}

ok, msg = await _spawn_claude_command(...)

inbox_post = {p.name for p in inbox_dir.glob("*.md") if p.name != ".gitkeep"}
new_inboxes = inbox_post - inbox_pre
inbox_created = bool(new_inboxes)
```

### 2. inbox 미생성 시 분기

- **작업장 anchor 메시지** 변경: `📥 작업 시작` → `⚠ inbox 생성 안 됨 — 요청이 모호해서 /inbox-send 가 파일 생성 거부`
- **Thread 생성 skip** (작업 단위가 없으니까)
- **`_post_spawn_orchestration` 자동 호출 skip** — head 가 어차피 안 뜸 → Phase A 타임아웃 없음
- 사용자에게 구체적 지시 예시 안내

### 3. double `q.task_done()` 버그 함께 정리

기존 코드에서 `if not ok: q.task_done(); continue` 패턴은 **`finally:` 블록의 `q.task_done()` 과 중복 호출** 됨 (Python `continue` 는 finally 를 실행). `asyncio.Queue.task_done()` 은 과호출 시 `ValueError` 발생 — 워커 크래시 잠재.

이번에 추가한 경로 포함 3곳의 `q.task_done()` 명시 호출 제거. `finally` 에만 맡김.

---

## 영향 범위

- **증상 개선**: 모호한 `@bot send` 시 3분 타임아웃 대신 **즉시** 명확한 에러 메시지
- **사용자 가이드**: anchor 메시지에 "좀 더 구체적으로 (파일명/경로/동작) 지시" 직접 포함
- **안정성**: 워커 크래시 잠재 버그 제거 (워커가 `HEAD_LOCK` 대기 실패 / spawn 실패 경로 탈 때 `ValueError` 발생 가능성)

---

## 하위 호환

- 정상 flow (모호하지 않은 `@bot send`) 는 동작 그대로
- `inbox_pre` 는 `.gitkeep` 제외 필터링 (빈 inbox 폴더도 정상 처리)
- `inbox_dir` 이 없는 초기 상태도 안전 (빈 set 비교)

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.2
## bot/controller.py 만 변경

taskkill //F //IM python3.11.exe      # Windows
bash scripts/start-bot.sh --bg
```

---

## 테스트

```
## 모호한 입력
@bot send ## Error Type
  → (예상) ⚠ inbox 생성 안 됨 — 요청이 모호해서 /inbox-send 가 파일 생성 거부.
           좀 더 구체적으로 (파일명/경로/동작) 지시해서 다시 @bot send <text> 보내주세요.

## 구체적 입력
@bot send README 하단에 "coord bot v0.16.2 테스트" 한 줄 추가
  → (예상) 📥 작업 시작 + thread 생성 + 기존 orch 흐름
```

---

## 제보 / 커밋

사용자 제보로 발견 + 진단. `@bot send ## Error Type` 실사용 시나리오에서 Phase A 타임아웃 관찰.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16.1 ===== -->

# coord-template v0.16.1 — 자연어 대화 응답을 대화방으로 강제 라우팅

v0.16 에선 작업장(`discord_alert_channel_id`) 에서 `@bot <chat>` 하면 "채팅 비활성" 이라고 거절했다. 근데 실사용에선 사용자가 작업 thread 안에서 quick status 를 묻는 경우가 많다 — 거절보다 **대화방으로 답을 라우팅** 하는 게 자연스러움.

v0.16.1 에서 채널 분리 대칭 완성:
- `@bot send <text>` → 작업 알림은 **작업장** (기존 v0.11+)
- `@bot <chat>` → 자연어 응답은 **대화방** (v0.16.1 신규)

어느 채널에서 물어도 응답은 대화방 한 곳에 모임 → 작업장은 작업만, 대화방은 대화만.

---

## 동작 변경

### v0.16 (이전)

```
[작업장]
user: @bot 상태 어때?
bot:  💬 **자연어 대화 비활성**: 작업장 채널 (대화방 에서만 채팅 가능)
      대안: @bot status ...
```

### v0.16.1 (현재)

```
[작업장]
user: @bot 상태 어때?
bot:  💬 자연어 대화는 <#대화방> 대화방에서 응답합니다…

[대화방]
bot:  @user 질문 (from <#작업장>):
      > 상태 어때?

      dealos 상태:
      - HEAD_LOCK: ⚪ idle
      - pending inbox: 0건
      ...
      _claude-sonnet-4-6 · in:1200 out:320_
```

### 대화방에서 질의 시 (변경 없음)

```
[대화방]
user: @bot 상태 어때?
bot:  dealos 상태: ...  ← 같은 채널에 답
```

---

## 구현

### `_resolve_chat_channel()` 신규 helper

```python
async def _resolve_chat_channel(
    project: dict[str, Any], source_message: discord.Message
) -> tuple[discord.abc.Messageable, bool]:
    """응답 채널 결정. (channel, redirected) 반환.
    - discord_channel_id (대화방) 설정돼있으면 그곳으로 (redirected=True if 출처≠대화방)
    - 미설정 시 source 채널 그대로 (redirected=False)"""
```

### `_chat_with_claude()` 수정

- 응답 대상 채널을 `_resolve_chat_channel()` 로 결정
- redirected 이면 원래 채널에 `💬 → <#대화방> 에서 응답합니다…` 한 줄 발송
- 실제 답은 대화방에 `<@user> 질문 (from <#출처>):\n> 원문 snippet\n\n<answer>` 포맷으로
- 같은 채널 (대화방 직접 질의) 이면 prefix 없이 깔끔하게

### `_handle_freeform()` 수정

- 작업장 차단 로직 제거
- 프로젝트 바인딩만 조건 (대화방 미설정이어도 동작)
- chat_available = True 이면 `_chat_with_claude()` 위임

### help embed / README

라우팅 설명 추가, "작업장 에선 동작 안 함" 문구 → "어디서든 질문, 응답은 대화방" 으로 교체.

---

## 하위 호환

- 대화방 미설정 (단일 채널 구성) → 기존대로 현재 채널에 답
- `default_project` 만 있고 채널 바인딩 없음 → 현재 채널에 답 (fallback)
- prompt caching / state 번들 / 모델 설정 모두 v0.16 과 동일

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16.1
## bot/controller.py, bot/README.md overwrite

taskkill //F //IM python3.11.exe      # Windows
bash scripts/start-bot.sh --bg
```

추가 설정 없음 — `discord_channel_id` 가 이미 `projects.yml` 에 있으면 자동 적용.

---

## 알려진 제약

- **thread 내부 질의**: 작업 thread 에서 `@bot <chat>` 하면 응답이 대화방 parent 로 감. thread 컨텍스트는 끊어짐. 원한다면 thread 에서는 `@bot status` 같은 정형 명령 사용 권장.
- **대화방 thread 에서 질의**: 대화방 parent 에 답 (thread 로 되돌아가지 않음). 이건 v0.17 memory 와 함께 재검토.

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.16 ===== -->

# coord-template v0.16 — Option C: 자연어 대화 (Anthropic SDK 직결)

v0.11 채널 분리 (대화방/작업장) 후 **대화방에서 무엇을 하느냐** 는 미답 질문이었다. 일반 명령 (`@bot status`, `@bot queue`) 은 정형이고, 작업 지시는 `@bot send`. 그런데 "지금 뭐 돌아가?", "이 inbox 왜 needs-fix 됐어?" 같은 **자유 질문** 은 처리 경로가 없었음.

v0.16 에서 Anthropic SDK 를 직결해 `@bot <text>` (send/기타 명령 없이) 를 **대화방 전용 자연어 Q&A** 로 활성화.

---

## 동작

### 예시

```
[대화방]
user: @bot 지금 pending inbox 몇 개야?
bot:  📊 dealos 상태
      - HEAD_LOCK: ⚪ idle
      - pending inbox: 0 건
      최근 처리: 2026-04-17-180512 (verdict: go, merged ✅)
      추가 작업 지시는 `@bot send <내용>` 으로 하면 됩니다.
      _claude-sonnet-4-6 · in:1200 out:320 cache_r:800 cache_w:400_

user: @bot v0.15 가 뭐 바뀌었는지 설명해줘
bot:  v0.15 는 자동 orchestration 중간 단계를 Discord thread 에 실시간 노출합니다.
      - ⏱ head 스폰 대기 메시지
      - 📍 HEAD_LOCK 감지 시 "시작" 메시지
      - 🕐 5분 간격 heartbeat ("N분 경과")
      - ✅ HEAD_LOCK 해제 감지 시 "완료" 메시지
      이전엔 head 작업 30분 동안 침묵 → 완료 prompt 만 떴는데, 사용자가 진행 상태를 확인할 수 있게 됐어요.
      ...
```

### 동작 조건 (모두 true 여야 활성)

1. `ANTHROPIC_API_KEY` 환경변수 존재 (`.env.local` 에 기록)
2. `anthropic` Python 패키지 설치 (`pip install -r bot/requirements.txt`)
3. `projects.yml` 의 `bot.chat.enabled: true` (기본 `true`)
4. 메시지가 **대화방** (`discord_channel_id`) 에서 옴 — **작업장** (`discord_alert_channel_id`) 에선 동작 안 함
5. 채널이 프로젝트와 바인딩됐거나 `default_project` 설정됨

하나라도 안 맞으면 기존 fallback 안내 메시지.

---

## 구현

### 1. 새 의존성

`bot/requirements.txt` 에 `anthropic>=0.40.0` 추가.

### 2. 환경변수

`.env.local.example` 에 `ANTHROPIC_API_KEY=` 플레이스홀더.

### 3. 설정 스키마

`bot/projects.yml.example` 에 `bot.chat` 블록:

```yaml
bot:
  chat:
    enabled: true
    model: "claude-sonnet-4-6"       # Sonnet 4.6 기본
    max_tokens: 1024
    include_status_md: true
    include_recent_inbox: 3
```

### 4. 봇 로직

**Anthropic 클라이언트 초기화** (controller.py 기동 시):
- `ANTHROPIC_API_KEY` + `anthropic` 패키지 있으면 `_ANTHROPIC_CLIENT` 에 세팅
- 없으면 `None` 유지 (자연어 대화 비활성)

**`_gather_project_state()`**: 프로젝트 상태를 텍스트로 요약
- HEAD_LOCK (active/idle + 시작 ts)
- 최근 inbox N건 (제목 + status)
- STATUS.md (4KB truncated)
- 최근 review-inbox 2건 (verdict + merged 여부)

**`_chat_with_claude()`**: Anthropic API 호출
- system prompt = [base 규칙 (cache_control=ephemeral), 실시간 state] 2블록
- user = 사용자 메시지 (자유 텍스트)
- `asyncio.to_thread` 로 blocking 호출 격리
- 응답 + usage footer (입력/출력/cache 토큰 표시)

**`_handle_freeform()` 개선**:
- 채널 바인딩 조회 → 작업장이면 fallback
- chat_available 조건 통과 시 `_chat_with_claude()` 호출
- 아니면 비활성 사유 나열 후 fallback 안내

### 5. help embed

`@bot help` 에 💬 자연어 대화 섹션 추가. 활성/비활성 상태 실시간 표시.

---

## 비용 / 성능 / 안전장치

### prompt caching

system prompt 의 **base 규칙** (~500 tokens) 은 모든 대화에 공통 → `cache_control: ephemeral` 로 캐시. 5분 내 재질의 시 캐시 히트 → 입력 토큰 80~90% 할인.

**state 블록** 은 동적이므로 캐시 안 함. 매번 fresh read.

### 비용 가드

- `max_tokens: 1024` 기본 (응답 제한)
- Discord 메시지 2000자 제한 → 1900자 넘으면 잘림 표시
- 사용자가 폭주하면 admin_user_ids 로 차단 가능

### 작업장 분리

작업장 (discord_alert_channel_id) 에선 **일부러 비활성**. 이유: 작업장은 head/bot 자동 notify 가 쏟아지는 공간 → 채팅이 섞이면 다음 명령어 못 찾음. 대화방을 별도로 두는 이유.

### 키 없을 때

`ANTHROPIC_API_KEY` 없이 봇 기동 시 로그에 `ℹ ANTHROPIC_API_KEY 없음 — Option C 자연어 대화 비활성` 출력. 다른 명령은 정상 동작.

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.16
## bot/controller.py, bot/README.md, bot/projects.yml.example, bot/requirements.txt overwrite
## .env.local.example manual-merge (ANTHROPIC_API_KEY 라인 확인)

## 의존성 설치
pip install -r bot/requirements.txt

## API 키 발급 후 .env.local 에 추가
## https://console.anthropic.com/ → API Keys → Create Key
echo 'ANTHROPIC_API_KEY=sk-ant-...' >> .env.local

## 봇 재시작
taskkill //F //IM python3.11.exe      # Windows
bash scripts/start-bot.sh --bg

## 대화방에서 테스트
## @bot 지금 상태 어때?
```

---

## 알려진 제약 / 후속

- **대화 memory 없음**: 매 `@bot` 호출이 독립. "방금 답한 그 inbox" 같은 지시어 불가. → v0.17 후속.
- **tool-use 없음**: 봇이 직접 `coordination/plans/*.md` 같은 파일을 찾아 읽을 수 없음 (system prompt 에 bundle 된 범위만). → v0.18 후속.
- **한 프로젝트만 대응**: 메시지가 온 채널의 프로젝트 기준. cross-project 질의 ("dealos vs other 비교") 불가.
- **Thread 내 대화**: thread 도 parent channel 의 프로젝트를 상속 → 정상 작동. 단 thread 가 작업장 산하면 비활성.
- **rate limiting 없음**: 사용자가 1초에 100번 `@bot` 보내도 다 Anthropic API 호출. admin_user_ids 로 제한하거나 후속에 per-user rate limit 추가 고려.

---

## 후속 후보

- **v0.17**: 대화 memory — thread/채널 단위로 직전 N 턴 저장 → multi-turn 대화 가능
- **v0.18**: tool-use — 봇이 Read/Grep/Bash 제한된 tool 로 coordination/ 탐색 → 더 정교한 답변
- **v1.0**: DEALOS 외 제3 프로젝트 실사용 검증 후 태깅

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.15 ===== -->

# coord-template v0.15 — orchestration 실시간 heartbeat + phase 전환 thread 메시지

v0.12 이후 자동 orchestration 이 `@bot send` 직후 thread 를 만들고 → head 가 끝날 때까지 (수 분~수십 분) **침묵** → verdict prompt 만 찍혔다. 사용자가 "봇이 죽었나? 진행 중인가? 에러난 건가?" 구분 못 했다.

v0.15 에선 각 phase 전환이 thread 에 실시간 노출되고, 장시간 작업 중엔 5분 간격 heartbeat 가 경과 시간을 보여준다.

---

## Thread 에 뜨는 메시지 (v0.15)

```
[#작업장]
@bot send 가격 계산 리팩터링
Bot: 📥 dealos 작업 시작
     본문: 가격 계산 리팩터링
     ✅ dealos inbox-send spawn
       └ inbox-dealos-180512 (thread)

[inbox-dealos-180512 thread 내부]
Bot: ⏱ `dealos` head 세션 스폰 대기 중…
Bot: 📍 `dealos` head 시작 감지 — 작업 진행 중 (5분 간격 heartbeat)
Bot: 🕐 `dealos` head 작업 중 (5분 경과)      ← v0.15 heartbeat
Bot: 🕐 `dealos` head 작업 중 (10분 경과)
Bot: 🕐 `dealos` head 작업 중 (15분 경과)
Bot: ✅ `dealos` head 완료 감지 — verdict 확인 중
Bot: ✨ `dealos` head self-review 감지 — verdict: `go` (v0.13)
Bot: ✅ Review 완료 — 머지 진행할까요?   [verdict prompt embed]
     [✅] [❌]
```

### 메시지 해석

| 이모지 | 의미 | 언제 |
|--------|------|------|
| ⏱ | head 스폰 대기 시작 | 자동 orch 최초 |
| 📍 | head 시작 감지 (HEAD_LOCK 생성) | Phase A 성공 |
| 🕐 | head 작업 중 heartbeat | Phase B 중 5분마다 |
| ✅ | head 완료 감지 (HEAD_LOCK 해제) | Phase B 성공 |
| ✨ | head self-review verdict 감지 (Phase D 생략) | Phase C-bis 성공 (v0.13) |
| 🔎 | Phase D fallback spawn 진입 | self-review 없을 때 |
| ⚠ / ❌ | 에러 | 타임아웃, spawn 실패 |

짧은 작업 (5분 미만) 엔 🕐 heartbeat 가 안 뜸 — 불필요한 noise 방지.

---

## 구현

### `_head_heartbeat()` 신규 helper

```python
async def _head_heartbeat(
    target_channel: discord.abc.Messageable,
    project_id: str,
    head_lock: Path,
    start_ts: float,
    interval_sec: int = _V015_HEARTBEAT_INTERVAL_SEC,
) -> None:
    """head 작업 중 thread/채널에 주기적 진행 메시지.
    HEAD_LOCK 이 사라지거나 task cancel 되면 종료."""
    try:
        while True:
            await asyncio.sleep(interval_sec)
            if not head_lock.exists():
                return
            elapsed_min = max(1, int((time.time() - start_ts) / 60))
            try:
                await target_channel.send(
                    f"🕐 `{project_id}` head 작업 중 ({elapsed_min}분 경과)"
                )
            except Exception:
                pass
    except asyncio.CancelledError:
        return
```

### `_post_spawn_orchestration()` 확장

- Phase A 앞에 ⏱ 메시지
- Phase A 성공 뒤에 📍 메시지
- Phase B 실행 중 병렬 heartbeat task (`asyncio.create_task`) 로 🕐 노출
- Phase B 성공 뒤에 ✅ 메시지
- 기존 ✨ (self-review 감지) / 🔎 (fallback spawn) 메시지 유지

heartbeat task 는 `finally` 블록에서 cancel + await 로 안전 정리.

### 상수

```python
_V015_HEARTBEAT_INTERVAL_SEC = 300   # 기본 5분
```

튜닝 시 이 값만 수정. 3분으로 줄이면 피드백 ↑ / noise ↑, 10분으로 늘리면 반대.

---

## 하위 호환

- thread 없이 채널 직접 사용하는 경우에도 동일하게 동작 (target_channel 이 thread 든 TextChannel 이든 `send()` 가능하면 OK)
- heartbeat task 실패 (send 예외) 는 조용히 무시 — orch 는 계속 진행
- v0.14/v0.13 동작은 그대로 — v0.15 는 **추가 메시지만** 붙임

---

## 알려진 제약

- **Thread auto-archive**: Discord thread 는 기본 24h archive 후 새 메시지 차단. 60분 이상 head 작업 시엔 archive 걱정 없음. 그 이상 유지하려면 thread 의 `auto_archive_duration` 증가 (현재 1440분 = 24h)
- **5분 미만 세밀 피드백 없음**: `_V015_HEARTBEAT_INTERVAL_SEC = 300` 기준. 더 짧은 간격 원하면 상수 조정
- **STATUS.md 변경 감지 미포함**: head 가 쓰는 `coordination/STATUS.md` 의 마지막 줄을 heartbeat 에 포함하면 더 유용하겠지만 v0.15 범위 외. 후속 후보.
- **Discord rate limit**: 계정당 채널별 5개/5초. 5분 간격이면 문제없음

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.15
## bot/controller.py 는 overwrite (변경만)
## review-inbox.md / inbox-send.md / resolve-escalation.md 변경 없음 (v0.14/v0.13 에서 처리 끝)

taskkill //F //IM python3.11.exe      # Windows
## Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

---

## 후속 후보 (v0.16 이상)

- **STATUS.md 변경 감지**: heartbeat 메시지에 STATUS.md 마지막 1~2줄 포함 (head 가 쓰는 현황판)
- **Step 단위 notify**: sub task 완료 시 `coordination/reports/wt-<name>.md` 생성 감지 → thread 에 "🔧 sub/<name> 완료" 알림
- **Discord thread auto-extend**: long-running 작업 24h 근접 시 archive 방지 갱신
- **v1.0 gate**: DEALOS 외 제3 프로젝트 실사용 검증 완료 후 태깅

---

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.14 ===== -->

# coord-template v0.14 — review-inbox 플래그 기반 phase 분리

v0.12 에서 도입한 자동 orchestration 은 "Step 1~6 만 수행" / "Step 7 만 수행" 같은 **prompt 본문 지시** 로 review-inbox 의 phase 를 쪼갰다. Claude 가 지시를 정확히 따른다고 가정한 prompt hack — 가장 약한 고리였다.

v0.14 에서 `.claude/commands/review-inbox.md` 에 **공식 플래그 3종** 을 추가해 prompt hack 을 제거한다.

---

## 플래그

| 플래그 | 의미 | 수행 범위 |
|--------|------|----------|
| `--verdict-only` | 검증 + verdict 판정까지만 | 1~6 단계. **7단계 (머지) 절대 금지.** |
| `--merge-only` | 이미 verdict: go 판정 끝난 엔트리에 머지만 | 1~6 스킵. 해당 엔트리 verdict 가 `go` 인지 사전 체크 후 7단계. |
| `--auto-yes` | 7단계 사용자 확인 질문을 자동 yes | `--merge-only` 와 주로 함께. 단독으로도 유효. |
| (없음) | 기본 — 전체 흐름 (1~8) | 기존 동작 유지. 하위 호환. |

### 조합 예

```bash
/review-inbox <id>                          # v0.13 이하와 동일 (하위 호환)
/review-inbox <id> --verdict-only           # 판정만
/review-inbox <id> --merge-only --auto-yes  # 자동 머지 (봇이 리액션 처리에 사용)
```

### 사전 체크 (`--merge-only` 의 견고성)

`--merge-only` 로 진입 시:
1. 해당 inbox id 의 review-inbox 파일에서 `verdict:` 헤더 확인
2. `go` → 계속, 그 외 → **즉시 중단** ("verdict 가 go 아님 — 머지 거부")
3. verdict 없음 → **즉시 중단** ("판정 안 됨 — `--verdict-only` 먼저")
4. `reviewed: true` 없으면 중단

→ 플래그 남용으로 미검증 머지가 발생할 수 없음.

---

## 봇 변경 (bot/controller.py)

prompt hack 제거:

**이전 (v0.12~v0.13)**
```python
## Phase D (자동 review)
review_prompt = (
    f"/review-inbox {inbox_id} "
    f"— Step 1~6 (verdict 판정) 만 수행. Step 7 (머지 진행) 은 건너뛰고 종료. "
    f"verdict 는 review-inbox 엔트리 파일에 반드시 기록."
)

## 리액션 (go + ✅)
prompt = (
    f"/review-inbox {inbox_id} "
    f"— 이미 verdict: go 판정 완료. Step 1~6 은 건너뛰고 Step 7 (머지 진행) 만 수행. "
    f"사용자 확인 자동 yes 로 간주하고 머지 + push 까지 완료."
)
```

**v0.14**
```python
## Phase D
review_prompt = f"/review-inbox {inbox_id} --verdict-only"

## 리액션 (go + ✅)
prompt = f"/review-inbox {inbox_id} --merge-only --auto-yes"
```

---

## 하위 호환

- 플래그 없이 호출하는 기존 명령 (`/review-inbox <id>` / `/review-inbox all`) 은 **완전히 동일** 하게 동작
- v0.13 이하 봇을 v0.14 review-inbox.md 와 혼용해도 안전 (봇이 긴 prompt 지시를 보내도 Claude 가 여전히 해석)
- v0.14 봇과 v0.13 이하 review-inbox.md 를 혼용하면 플래그가 무시되어 **전체 흐름이 실행됨** → 예상치 못한 자동 머지 위험. 이 조합은 **권장하지 않음**: 봇 업그레이드 시 review-inbox.md 도 같이 갱신 필요.

---

## 파일 변경

- `.claude/commands/review-inbox.md`:
  - 섹션 0-1 "모드 플래그 파싱" 신설
  - 6단계에 `--verdict-only` 조기 종료 분기
  - 7단계 시작부에 `--merge-only` 사전 체크 블록
  - 7단계 사용자 확인 블록에 `--auto-yes` 분기
  - "금지" 및 "지금 할 것" 섹션 플래그 인식으로 재작성
- `bot/controller.py`:
  - `_post_spawn_orchestration()` 의 review prompt → `--verdict-only`
  - `_handle_verdict_reaction()` 의 머지 prompt → `--merge-only --auto-yes`
  - help embed 자동 orchestration 설명 플래그 반영
- `bot/README.md`: 자동 Orchestration 섹션 + 신설 "review-inbox 플래그 직접 사용" 섹션
- `VERSION`: v0.14

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.14
## .claude/commands/review-inbox.md 는 manual-merge — diff 확인 후 승인
## bot/controller.py 는 overwrite

## 봇 재시작
taskkill //F //IM python3.11.exe      # Windows
## Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

**중요**: DEALOS 동기화 시 `.claude/commands/review-inbox.md` 는 꼭 같이 복사. 봇만 v0.14 로 올리고 review-inbox.md 가 v0.13 상태면 플래그 무시되어 자동 머지 가능.

---

## 후속 후보

- **v0.13** (순서상 다음): head 의 `/inbox` 끝에서 `--verdict-only` 자동 호출 → 봇의 Phase D 가 생략 가능. v0.14 플래그 기반이라 이제 구현이 깔끔.
- **v0.15**: orchestration 중간 단계를 Discord thread 에 실시간 업데이트
- **v1.0**: DEALOS 외 제3 프로젝트 실사용 검증

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.13 ===== -->

# coord-template v0.13 — head self-review (봇 Phase D 생략)

v0.12 까진 봇이 HEAD_LOCK 해제 후 op 세션을 다시 spawn 해 `/review-inbox --verdict-only` 를 돌려 verdict 를 판정했다. 그런데 head 가 review-inbox 를 작성한 직후엔 이미 **plan / reports / diff 가 head 컨텍스트에 로드돼 있음** — 새 세션 spawn 은 전부 다시 읽어야 해서 수만 토큰 낭비.

v0.13 에선 head 가 inbox 처리 완료 직후 **같은 세션에서** `/review-inbox <id> --verdict-only` (v0.14 플래그) 를 실행해 verdict 를 사전 기록. 봇은 verdict 가 이미 있으면 Phase D spawn 을 완전 생략.

> **버전 순서 주의**: v0.12 → v0.14 → v0.13 으로 릴리스됨. v0.13 은 v0.14 플래그 기반이라 구현 의존성상 v0.14 가 먼저였다 (로드맵 label 순서는 유지).

---

## 변경 요약

### 1. head 역할 문서 (`coordination/roles/head.md`)

새 섹션 추가: **"verdict 사전 기록 (v0.13+)"**
- review-inbox 엔트리 작성 직후 같은 세션에서 `/review-inbox <id> --verdict-only` 즉시 수행
- verdict (go/needs-fix/block) 를 review-inbox 파일 헤더에 기록 후 종료
- 왜 head 가 직접 하는가: 컨텍스트 재로딩 없이 바로 판정 가능 → 토큰 수만 토큰 절약

### 2. op 슬래시 명령 수정 (head spawn prompt)

**`.claude/commands/inbox-send.md`** 와 **`resolve-escalation.md`** 의 head spawn:

이전:
```bash
claude -p "/inbox" \
```

v0.13:
```bash
HEAD_SPAWN_PROMPT="/inbox

[v0.13 자동 review 지시]
inbox 처리 완료 + review-inbox 엔트리 작성 직후, 같은 세션에서 반드시 아래를 실행:
    /review-inbox <방금 생성한 inbox id> --verdict-only
결과의 verdict 가 review-inbox 파일 헤더에 기록되어야 이번 세션 종료 가능."

claude -p "$HEAD_SPAWN_PROMPT" \
```

왜 prompt 주입 방식: 템플릿은 소비 프로젝트의 head-side `/inbox` 슬래시 명령 본문을 모름 (프로젝트마다 다름). prompt 에 지시를 덧붙이면 `/inbox` 가 어떻게 구현됐든 head 가 완료 시점에 self-review 를 수행.

### 3. 봇 Phase D early-return (`bot/controller.py`)

`_post_spawn_orchestration()` 에 Phase C-bis 신설:

```python
## Phase C-bis (v0.13): head 가 self-review 로 verdict 를 이미 기록했는지 확인
pre_verdict, pre_fname = _read_verdict(project_path, inbox_id)
if pre_verdict is not None:
    await target_channel.send(
        f"✨ `{project_id}` head self-review 감지 — `{inbox_id}` verdict: `{pre_verdict}` (v0.13)"
    )
    await _post_verdict_prompt(target_channel, project_id, inbox_id, pre_verdict, pre_fname)
    return

## Phase D (fallback): head self-review 안 했을 때만 spawn
...
```

- self-review 성공 시 → Phase D 완전 생략, 즉시 verdict prompt 포스트
- self-review 실패/누락 시 → 기존 Phase D spawn 으로 fallback (하위 호환)

### 4. help embed / bot/README.md

자동 Orchestration 설명에 v0.13 단계 반영.

---

## 효과

| 지표 | v0.12 | v0.13 | 감소 |
|------|-------|-------|------|
| 평균 review 토큰 | ~30,000 (fresh load) | ~3,000 (in-context) | ~90% |
| 자동 orch 지연 | HEAD_LOCK 해제 + review spawn 시작 + 수 분 | HEAD_LOCK 해제 즉시 verdict prompt | ~수 분 단축 |
| op 세션 spawn 횟수 | 1회 (review) + 1회 (머지) | 1회 (머지만) | 절반 |

---

## 하위 호환

- head 가 self-review 를 수행하지 않으면 (prompt 해석 실패, 타임아웃 등) 봇이 기존 Phase D 로 **자동 fallback**
- 메시지 `✨ head self-review 감지` vs `🔎 head 완료 감지 (self-review 없음)` 로 경로 구분
- v0.13 봇 + v0.12 이하 `inbox-send.md` 조합도 동작 (self-review 지시 없으니 항상 Phase D fallback — 효과 없지만 안전)
- v0.12 봇 + v0.13 이상 `inbox-send.md` 조합: head 가 self-review 후 verdict 기록 + 봇이 Phase D 중복 spawn → verdict 파일 2번 수정되지만 내용 동일 → 중복 리소스만 소비, 결과는 정상

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.13
## .claude/commands/inbox-send.md / resolve-escalation.md / coordination/roles/head.md
## 는 manual-merge — diff 확인 후 승인
## bot/controller.py 는 overwrite

taskkill //F //IM python3.11.exe      # Windows
## Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

---

## 알려진 제약

- **head spawn prompt 길이 증가**: Claude headless 모드의 `-p` 인자 길이 제한은 수천 자 수준이라 여유 있음
- **head 의 self-review 실패**: prompt 해석 오류, Claude 의 /review-inbox 실행 거부 등 여러 이유로 실패 가능. 이 경우 봇이 fallback 으로 커버
- **review-inbox 파일명 불일치**: `_read_verdict` 는 inbox id 로 파일을 찾는데, head 가 다른 파일명 규칙을 쓰면 못 찾아 Phase D fallback 탄다. head.md 의 포맷 가이드 준수 필요

---

## 후속 후보

- **v0.15** (다음 예정): orchestration 중간 단계를 Discord thread 에 실시간 업데이트
- **v1.0**: DEALOS 외 제3 프로젝트 실사용 검증 후 태깅

🤖 Claude Opus 4.7 협업.


---

<!-- ===== v0.12 ===== -->

# coord-template v0.12 — 자동 Orchestration + HEAD_LOCK bug fix

`@bot send <text>` 한 번이면 **전체 inbox 흐름 자동**. head 완료 감지 → review 자동 → verdict 기반 Discord 리액션 → 머지/fix/보류.

`@bot review` / `@bot fix` 수동 명령도 유지 (수동 개입 필요 시).

---

## 전체 흐름

```
User:   @bot send 가격 계산 리팩터링
Bot:    ⏳ dealos 큐 추가 — 즉시 처리

[#대화방]
Bot:    ➡ dealos <#작업장> 에서 진행

[#작업장]
Bot:    📥 dealos 작업 시작
        본문: 가격 계산 리팩터링
        ✅ dealos inbox-send spawn
          └ inbox-dealos-XXXXXX (thread)

[2~30분 경과, head 작업 진행]

Bot:    🔎 dealos head 완료 감지 — 2026-04-17-180512 review 자동 진행 중…
Bot:    ✅ Review 완료 — 머지 진행할까요?
        inbox: 2026-04-17-180512
        verdict: go (통과)
        ✅ → 머지 단계 spawn
        ❌ → 보류 (수동)
        [✅] [❌]   ← 리액션

[User 가 ✅ 리액션]
Bot:    🚀 dealos 2026-04-17-180512 머지 spawn (reaction by @user)
        ✅ dealos merge-... spawn
        log: ...

[merge 완료 후 webhook 알림]
Webhook: 🎉 dealos 머지 완료 — ...
```

---

## 주요 변경

### 1. 자동 Orchestration (`_post_spawn_orchestration`)

Worker 가 inbox-send spawn 직후 백그라운드 task 를 돌려 전체 chain 감지:

| Phase | 동작 | 타임아웃 |
|-------|------|---------|
| A | HEAD_LOCK 생김 대기 (head 시작) | 3분 |
| B | HEAD_LOCK 해제 대기 (head 완료) | 60분 |
| C | 최근 inbox 파일 id 추출 | — |
| D | `/review-inbox <id> — Step 1~6 만 수행, Step 7 (머지) 스킵` spawn (synchronous, await 10분) | 10분 |
| E | review-inbox 파일에서 verdict 추출 (regex) | — |
| F | Discord 에 verdict prompt embed + ✅/❌ 리액션 부착 | — |

Worker 는 이 chain 을 기다리지 않고 `task_done()` → 다음 job 진행. chain 은 `asyncio.create_task()` 로 독립 실행.

### 2. HEAD_LOCK 경로 bug fix

**이전 (bug)**: `<project>/coordination/HEAD_LOCK` 체크 — 엉뚱한 위치
**지금**: `<parent>/<project>-wt-head/coordination/HEAD_LOCK` — 올바른 head worktree

`_head_lock_path(project_id, project_path)` 헬퍼 도입. v0.10 의 queue 직렬화가 실제론 작동 안 했던 bug 도 같이 해결.

### 3. Verdict 리액션 handler 확장

기존 `on_raw_reaction_add` 에 verdict-prompt 감지 분기 추가:
- embed footer 에 `verdict-prompt|<inbox-id>|<verdict>` marker
- ✅ / ❌ 리액션 감지 시 `_handle_verdict_reaction()` 호출

**`_handle_verdict_reaction`**:
- `go + ✅`: `/review-inbox <id> — Step 7 머지 단계만, 자동 yes` spawn
- `needs-fix + ✅`: `/fix <review-inbox 의 needs-fix 항목 자동 탐색>` spawn
- `❌`: "보류" 메시지
- `block`: 자동 action 없음 — 사용자 개입 필수

### 4. Synchronous spawn helper

`_spawn_and_wait(prompt, cwd, timeout)`:
- `asyncio.create_subprocess_exec` + `asyncio.wait_for`
- Popen (fire-and-forget) 와 다르게 **완료 대기**. review spawn 용.
- stdout 반환 + exit code 판정

### 5. 문서

- help embed: 🔁 자동 orchestration 섹션 신규
- bot/README.md: 자동 Orchestration 섹션 상세 + HEAD_LOCK 경로 수정 명시

---

## 수동 명령 (유지)

자동 흐름을 사용 안 하거나 세부 개입 시:
- `@bot review [id]` — 수동 review 트리거 (v0.11)
- `@bot fix [desc]` — 수동 fix 트리거 (v0.11)
- `@bot resolve/reject <id>` — 수동 escalation 응답 (v0.8)

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.12
## bot/controller.py + bot/README.md overwrite

taskkill //F //IM python3.11.exe      # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

**중요**: v0.12 는 head worktree 의 HEAD_LOCK 을 봄. 만약 사용자가 `dealos-wt-head/coordination/` 에 존재하지 않는 디렉토리 구조면 자동 orchestration 이 head 시작 감지에서 timeout 됨 — 이 경우 worktree 셋업 확인 필요.

---

## 알려진 제약

- **review-inbox.md 구조 가정**: Step 7 이 머지, Step 1~6 이 verdict. 이 구조 어긋난 프로젝트는 prompt 지시 효과 제한.
- **Claude 의 prompt 해석 여부**: "Step 7 건너뛰기" 지시를 Claude 가 따를지는 slash 명령 본문의 명확성에 의존. 만약 머지가 예상대로 안 스킵되면 v0.14 에서 슬래시 명령 분리 고려.
- **verdict 파일 감지**: 로컬 review-inbox/ 에만 기록되고 그 곳을 grep 함. git branch 간 divergence 시 못 찾을 수 있음 (head 가 wt/head 에 따로 커밋하면).
- **타임아웃 60분**: 아주 긴 작업은 시간 초과. 필요하면 상수 튜닝.

---

## 후속 후보

- **v0.13**: review-inbox.md 를 head 의 `/inbox` 끝에 자동 호출 (head-side 수정) → bot 의 orchestration Phase D 를 생략 가능
- **v0.14**: review-inbox.md 를 `--verdict-only` / `--merge-only` 플래그로 명확 분리 → prompt hack 제거
- **v0.15**: orchestration 중간 단계를 Discord thread 에 실시간 업데이트 (현재는 완료 후 verdict prompt 만)

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.11 ===== -->

# coord-template v0.11 — 명시적 명령 체계 + 채널 분리 (대화방/작업장)

**봇 명령 체계 재정비**. op 의 모든 슬래시 명령을 봇 명령으로 매핑 + 채널 2개로 역할 분리 가능.

---

## 주요 변경

### 1. 명령 체계 재정비

**v0.10 까지**: `@bot <text>` 가 바로 `/inbox-send` 로 spawn (freeform).

**v0.11**: 명시적 명령 체계.
- `@bot send <text>` — `/inbox-send` (기존 freeform 로직 이관)
- `@bot review [inbox-id]` — `/review-inbox` (id 생략 시 자동 탐색)
- `@bot fix [description]` — `/fix` (생략 시 needs-fix review 자동 탐색)
- `@bot resolve <id> [reason]` — `/resolve-escalation` (리액션 대체 텍스트)
- `@bot reject <id> [reason]` — `/reject-escalation` (리액션 대체 텍스트)
- `@bot <text>` (send 없이) — **Option C 자연어 대화 예약** (v0.12+). 현재는 안내.
- `/` 로 시작하는 명령 (예: `@bot /review-inbox`) → 매핑 안내 메시지.

**op 슬래시 명령 ↔ 봇 명령 매핑 완성도**:

| op 슬래시 | 봇 명령 (v0.11) |
|---|---|
| `/inbox-send` | `@bot send` |
| `/review-inbox` | `@bot review` |
| `/fix` | `@bot fix` |
| `/resolve-escalation` | `@bot resolve` + ✅ 리액션 |
| `/reject-escalation` | `@bot reject` + ❌ 리액션 |
| `/status` | `@bot status` (부분 — 봇은 프로젝트 overview 관점) |
| `/token-status` | `@bot budget` + `@bot usage` (두 관점으로 대체) |

### 2. 자동 탐색 (생략 시)

**`@bot review`** (id 생략):
- `coordination/inbox/*.md` mtime 역순 스캔
- `coordination/review-inbox/` 로컬 파일에 해당 id 포함 안 돼있으면 = 미review
- 최근 미review 첫 inbox 자동 선택 → `/review-inbox <id>` spawn
- 모두 review 완료면 "검토 대기 없음" 응답

**`@bot fix`** (description 생략):
- `coordination/review-inbox/` mtime 역순 스캔
- `verdict: needs-fix` 정규식 매칭 → 가장 최근 항목
- 제목 + 파일명을 description 으로 `/fix` spawn
- 없으면 "needs-fix 대상 없음" 응답

### 3. 채널 분리 (대화방 / 작업장)

같은 Discord 서버 안에 2 텍스트 채널로 역할 격리:

```yaml
projects:
  - id: dealos
    discord_channel_id: "11111..."        # #대화방 (봇 @mention 수신)
    discord_alert_channel_id: "22222..."  # #작업장 (alert + thread 생성)
```

**동작**:
- `@bot send <text>` 를 **대화방** 에서 받으면:
  - 봇이 대화방에 ACK ("⏳ 큐 추가")
  - Worker spawn 후 **작업장에 "anchor" 메시지** 포스트 + **거기서 thread 생성**
  - 대화방에는 "➡ 작업장 에서 진행" 간단 링크
- head webhook 은 `.env.local` 의 `DISCORD_WEBHOOK_URL` 이 작업장 webhook 이면 작업장에 도착
- 리액션 ✅/❌ 는 작업장 메시지에서 눌러도 **봇이 `discord_alert_channel_id` 까지 `_resolve_project` 로 인식**

**구현**:
- `_project_channel_match(p, channel_id)` — `discord_channel_id` 또는 `discord_alert_channel_id` 둘 중 하나라도 매칭 시 True
- `_resolve_project(channel_id)` / `_channel_ok(...)` 가 이 helper 사용
- `_resolve_anchor_channel(project, source_message)` — alert_channel 있으면 그 채널 객체, 없으면 source 채널

설정 생략하면 v0.10 동작 그대로 (단일 채널에서 전부).

### 4. `_handle_freeform` 재작성
- 빈 mention 만 있으면 help embed 노출
- `/` 로 시작하면 슬래시 명령 매핑 안내
- 그 외 텍스트 → "자연어 대화 v0.12+ 예약" 안내
- 기존 inbox-send 라우팅은 `@bot send` 로 이관

---

## 사용 예시

### 채널 분리 설정 후 대화방에서
```
User (대화방):  @bot send 가격 계산 리팩터링
Bot (대화방):   ⏳ `dealos` 큐 추가 — 즉시 처리
Bot (대화방):   ➡ `dealos` <#작업장> 에서 진행
Bot (작업장):   📥 `dealos` 작업 시작
                본문: `가격 계산 리팩터링`
                ✅ `dealos` inbox-send spawn
                ├ inbox-dealos-082119 (thread)
Webhook (작업장): 🎉 작업 완료 …
```

### 자동 탐색 명령
```
User:  @bot review
Bot:   🔎 `dealos` 최근 미review inbox 자동 선택: `2026-04-17-130000`
Bot:   ✅ `dealos` review-2026-04-17-130000 spawn

User:  @bot fix
Bot:   🔎 `dealos` 자동 탐색: `wt-backend-20260417.md` → /fix 스폰
Bot:   ✅ `dealos` fix spawn
```

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.11
## bot/controller.py + bot/README.md + bot/projects.yml.example overwrite
## bot/projects.yml (사용자) 은 preserve — 원하면 수동으로 discord_alert_channel_id 추가

taskkill //F //IM python3.11.exe
bash scripts/start-bot.sh --bg
```

**기존 `@bot <text>` 사용자 주의**: v0.11 부터는 `@bot send <text>` 사용. 그냥 `@bot <text>` 는 안내 메시지만 옴.

---

## 미구현 (후속)

- **Option C 자연어 대화** (v0.12+) — `@bot <text>` 가 Anthropic SDK/CLI 로 Claude 와 대화
- **`@bot review` origin/wt/head 브랜치 검색** — 현재 로컬 review-inbox 파일만 체크. 더 정확하려면 git ls-tree 필요.
- **`@bot fix` 후보 다중 표시** — 현재는 가장 최근 하나만. 사용자가 여럿 중 선택하는 UX

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.10.2 ===== -->

# coord-template v0.10.2 — `@bot usage` 깔끔 텍스트 테이블 (matplotlib 제거)

v0.10.1 의 matplotlib PNG 차트는 의존성 부담 + Discord 첨부라 가시성 이슈. v0.10.2 는 **순수 텍스트 테이블** 로 회귀 — 사용자 요청 포맷.

---

## 변경

### 1. matplotlib 완전 제거
- `import matplotlib` + `_MATPLOTLIB_OK` lazy import 삭제
- `_render_usage_chart()` 함수 제거
- `bot/requirements.txt` 에서 matplotlib 항목 제거

### 2. `@bot usage` 새 포맷 (code block 테이블)

```
📊 Claude CLI 사용량 — 최근 7일
총 $15.40 · 1,500,000 tokens · 7 활성일
평균: $2.20/day
기준 한계 (일): 5,000,000 tokens

날짜          사용량           잔량 /        총량   막대그래프              %
───────────────────────────────────────────────────────────────────────────
2026-04-17   1,024,000    3,976,000 /  5,000,000   ████░░░░░░░░░░░░░░░░  20%
2026-04-16     876,543    4,123,457 /  5,000,000   ███░░░░░░░░░░░░░░░░░  17%
2026-04-15     200,000    4,800,000 /  5,000,000   █░░░░░░░░░░░░░░░░░░░   4%
...
```

- **날짜** / **사용량** / **잔량** / **총량** / **막대그래프** / **%**
- 막대 20자 `█`/`░`, 백분율 표시
- desc 정렬 (오늘 ↓ 과거)
- 최대 10일 표시, 초과 시 footer 에 안내

### 3. `usage_display.daily_token_limit` 설정 (선택)

```yaml
bot:
  usage_display:
    daily_token_limit: 5000000    # 기본 5M
```

이 값이 테이블의 "총량" 기준 + 막대그래프 scale. 없으면 5M 기본.

---

## 영향

- **matplotlib 의존성 제거** → `pip install -r bot/requirements.txt` 용량 크게 감소 (~30MB)
- **PNG 첨부 없음** — 모든 응답이 텍스트 embed 로 깔끔
- **모바일 Discord 가독성 개선** — 이미지 로딩 대기 없음
- v0.10.1 의 chart 기능은 롤백 상태

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.10.2
## bot/controller.py + bot/requirements.txt + bot/projects.yml.example overwrite

## matplotlib 제거 원하면 (선택):
pip uninstall matplotlib

taskkill //F //IM python3.11.exe      # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

matplotlib 를 다른 용도로 쓰고 있으면 제거 안 해도 됨 — 봇은 더 이상 사용 안 함.

---

## 후속 후보 (필요 시)

- `@bot budget` 에도 동일 테이블 포맷 (현재는 progress bar + field 나열)
- 모델별 사용량 별도 행 (stacked bar 대안)
- 주간/월간 합계 행 추가

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.10.1 ===== -->

# coord-template v0.10.1 — `@bot usage` 그래프 (matplotlib bar chart)

v0.8.1 의 `@bot usage` 가 텍스트 Embed 뿐이라 가시성 부족. v0.10.1 에서 **일별 cost bar chart PNG** 자동 첨부 → 비용 추세가 눈으로 바로 보임.

---

## 변경

### 1. `_render_usage_chart()` 신규
- **matplotlib** (Agg headless) 로 bar chart PNG 생성
- 날짜 오름차순 x-axis (ccusage 는 desc 로 오는데 차트는 asc 가 자연스러움)
- **색상 gradient**: cost 크기별
  - 저렴 (<33% of max) → 초록 `#4ade80`
  - 중간 (33~66%) → 앰버 `#fbbf24`
  - 비쌈 (>66%) → 빨강 `#f87171`
- 막대 위에 값 label (`$X.XX`)
- 상단 우측에 **Total** annotation (파란 배지)
- Discord dark mode 친화 (`#2b2d31` 배경 / `#e5e7eb` 텍스트)
- PNG 바이트를 `io.BytesIO()` 로 반환 → `discord.File` 에 바로 첨부

### 2. `cmd_usage` 변경
- ccusage 결과 있으면:
  - 기존 Embed 는 그대로 (top 5 일 세부)
  - **chart PNG 생성 후 `embed.set_image(url="attachment://usage.png")` + 첨부**
- matplotlib 미설치면 → 차트 없이 embed 만 (graceful fallback)
  - Footer 에 "matplotlib 미설치 — 차트 생략" 표시

### 3. matplotlib 을 optional dep 로
- `bot/requirements.txt` — `matplotlib>=3.7.0` 추가 + "선택" 주석
- `controller.py` 상단 lazy import (try/except → `_MATPLOTLIB_OK` 플래그)
- 봇 기본 동작은 matplotlib 없이도 문제없음

---

## 렌더 예시 (설명)

```
┌──────────────────────────────────────────────────────────┐
│  Claude CLI Cost — last 7 days       [ Total: $15.80 ]   │
│                                                          │
│  USD                                                     │
│   5 ┤                                      ██ $4.20      │
│   4 ┤                                                    │
│   3 ┤        ██ $2.60                                    │
│   2 ┤  ██                ██                              │
│   1 ┤  ██ $1.10          ██ $2.10   ██ $1.50             │
│   0 └──────────────────────────────────────────────────  │
│       04-11 04-12 04-13 04-14 04-15 04-16 04-17          │
└──────────────────────────────────────────────────────────┘
```

(실제 Discord 에선 그라데이션 컬러 bar PNG)

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.10.1
pip install -r bot/requirements.txt   # matplotlib 추가 설치

taskkill //F //IM python3.11.exe      # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

matplotlib 설치 거부 시 기존 텍스트 응답으로 fallback — 안전.

---

## 후속 아이디어

- `@bot usage --breakdown` — 모델별 stacked bar (opus/sonnet/haiku)
- `@bot budget` 에도 gauge 차트 (% 대비 한계)
- 주간/월간 trend line chart
- cumulative cost 곡선

필요 시 v0.10.2+ 또는 v0.11 에서 추가.

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.10 ===== -->

# coord-template v0.10 — inbox 큐 (동시 요청 자동 직렬화)

`@bot <text>` 가 **HEAD_LOCK busy** 거부를 보는 일 없음. 프로젝트별 asyncio.Queue 경유로 요청을 쌓고 봇이 HEAD_LOCK 해제를 대기 후 자동 순차 spawn. 사용자는 "⏳ N번째" 응답 받고 그냥 기다리면 끝.

---

## 변경

### 1. 프로젝트별 큐 시스템
- **`_QueueJob` dataclass** — project_id, path, prompt, source_message, preview, enqueued_at
- **`_project_queues: dict[str, asyncio.Queue]`** — 프로젝트별 FIFO
- **`_project_workers: dict[str, asyncio.Task]`** — 프로젝트별 single-consumer task
- **Worker**:
  - `q.get()` 기다림 (10분 idle 시 자동 종료 → 메모리 정리)
  - job 들어오면: HEAD_LOCK clear 까지 10초 간격 polling (30분 타임아웃)
  - clear 되면 `_spawn_claude_command(check_head_lock=False, ...)` 실행
  - Thread 생성 (TextChannel 에서만)
  - spawn 응답 + thread 생성 모두 worker 가 처리 (UI 일관성)
- **`_enqueue_job(project_id, job)`**: 큐 추가 + worker 없거나 완료면 재시작. 반환: 자기 포함 큐 크기.

### 2. `_handle_freeform` 단순화
모든 inbox-send 요청을 무조건 큐 경유:
```
pos = await _enqueue_job(project_id, job)
if pos == 1: "⏳ <project> 큐 추가 — 즉시 처리"        (또는 HEAD_LOCK 해제 대기)
else:        "⏳ <project> 큐 N번째 대기"
```
Worker 가 실제 spawn 시점에 `source_message.reply()` 로 결과 + thread.

### 3. `@bot queue` 신규 명령
- `@bot queue` — 현재 채널 바인딩 프로젝트 (없으면 전체)
- `@bot queue <project-id>` — 특정 프로젝트
- Embed 로: 실행 중 여부 / worker 상태 / 큐 대기 수 / footer: 총 대기

### 4. help/README 업데이트
- 🔍 조회 섹션에 `@bot queue` 추가
- 🗂 큐 섹션 신규 (help embed)
- README 에 동작/제약 상세

---

## UX 변화

**Before (v0.9)**:
```
User: @bot 작업 A
Bot: ✅ dealos /inbox-send spawn
(5초 뒤)
User: @bot 작업 B
Bot: ⚠ dealos head 이미 실행 중 (시작: xxx) — 완료 후 재시도
```

**After (v0.10)**:
```
User: @bot 작업 A
Bot: ⏳ dealos 큐 추가 — 즉시 처리
  (worker 가 spawn → 결과 + thread)
User: @bot 작업 B (바로 이어서)
Bot: ⏳ dealos 큐 2번째 대기 — 앞 job 완료 후 자동 spawn
  (앞 job 끝나면 worker 가 자동 처리 → 결과 + thread)
```

---

## 튜닝 상수 (controller.py 상단)
- `_QUEUE_WORKER_IDLE_TIMEOUT_SEC = 600` — 10분 idle 시 worker 종료
- `_QUEUE_HEAD_LOCK_WAIT_TIMEOUT_SEC = 1800` — 30분 HEAD_LOCK 대기 한도
- `_QUEUE_HEAD_LOCK_POLL_INTERVAL_SEC = 10` — polling 주기

필요 시 env 변수화 가능 (후속).

---

## 제약

- **in-memory 큐** — 봇 재시작 시 대기 중 job 전부 손실. 다시 보내야 함. 영속화는 후속 후보 (SQLite or JSON file).
- **큐 크기 상한 없음** — admin_user_ids 로 spam 방지 의존. 필요 시 `max_queue_size` config 추가.
- **HEAD_LOCK 타임아웃 30분** 넘어가면 그 job 만 skip (다음 job 은 진행). 복구 필요하면 수동 재전송.
- **retry / escalation 반응은 큐 통과 안 함** — 직접 spawn (긴급 경로 유지).

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.10
## bot/controller.py + bot/README.md overwrite

taskkill //F //IM python3.11.exe      # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

첫 spawn 후 `@bot queue` 로 상태 확인해보면 worker 가 붙어있는 게 보임 (10분 idle 시 자동 종료).

---

## 후속 후보

- **큐 영속화** — SQLite or `bot/queue-state.json` 으로 재시작 survival
- **큐 크기 상한** — spam 방지 (`max_queue_size: 10`)
- **`@bot cancel <N>`** — N번째 대기 job 취소
- **`@bot priority <N>`** — 특정 job 우선순위 올림 (긴급 우회)

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.9 ===== -->

# coord-template v0.9 — Thread 격리 + usage 자동 경고 + ROADMAP 현행화

Phase 6 Discord Bot 의 마지막 굵직한 기능 두 개 + ROADMAP 업데이트. v0.9 릴리스로 Phase 6 MVP 를 **완결** 선언.

---

## 1. Thread 격리

### 동작
`@bot <text>` spawn 성공 시 봇 응답 메시지에서 **`inbox-<project>-<ts>`** 이름의 Discord Thread 자동 생성. 해당 inbox 진행/대화가 thread 안에 격리돼 본 채널 히스토리가 깔끔해짐.

### 구현
- `_handle_freeform` 에서 `_spawn_inbox_send` 성공 + `isinstance(message.channel, discord.TextChannel)` 일 때 `reply_msg.create_thread(name=..., auto_archive_duration=1440)` 호출.
- Thread 내부에서 `@bot` 명령도 정상 작동:
  - `_binding_channel_id()` helper 도입 — `discord.Thread` 객체면 `parent_id`, TextChannel 이면 `id` 반환.
  - 모든 channel-scope 로직 (`_channel_ok`, `_resolve_project`, `register`/`bind`, reaction handler) 이 이 helper 를 통과 → thread 도 parent 바인딩 상속.

### 권한
Bot Permissions 에 **Create Public Threads** 필요. 없으면 thread 생성 조용히 skip (spawn 자체는 성공).

### 제약
- Thread 내부에서 `@bot register` 하면 **parent 채널** 바인딩 (thread 자체가 아님) — 의도된 동작.
- 이미 thread 내부에서 `@bot <text>` 보내면 nested thread 생성 안 함 (Discord 미지원).

## 2. Usage 자동 경고 (ccusage 연동 확장)

### 동작
`bot/projects.yml` 의 `bot.usage_alerts` 에 임계값 설정 시 봇이 주기적으로 `ccusage daily` 체크 → 오늘/이번달 사용량이 임계 초과하면 **지정 채널에 Discord 알림** 자동 발송. 하루/한달 각 1회만 (중복 방지).

### 설정 예시
```yaml
bot:
  usage_alerts:
    enabled: true                     # 활성화
    check_interval_minutes: 60        # 체크 주기 (최소 1분)
    alert_channel_id: "123456..."     # 알림 받을 채널
    daily_usd_threshold: 10.0         # 오늘 $10 초과 시 알림 (0=비활성)
    monthly_usd_threshold: 100.0      # 이번 달 $100 초과 시 알림 (0=비활성)
```

### 구현
- `_check_usage_alerts_once()`: daily / monthly 각각 `ccusage daily --since <date>` 실행 → 합계 비교 → 임계 초과 + 같은 기간 알림 없으면 Embed 전송.
- `_usage_alert_loop()`: `on_ready` 에서 asyncio.create_task 로 시작. `enabled:true` 일 때만.
- Alert state: `bot/usage-alerts-state.json` (gitignored) — `last_daily_alert_date`, `last_monthly_alert_month` 등으로 중복 방지.
- 임계 0 또는 미설정이면 해당 체크 skip (daily 만, monthly 만 쓸 수도).

### Alert Embed 예시
```
⚠ 오늘 Claude CLI 사용량 임계 초과
오늘 누적: $12.34  (임계: $10.00)
상세: @bot usage 1
— ccusage daily · 2026-04-17
```

## 3. ROADMAP 현행화

`coordination/ROADMAP.md` 대폭 업데이트:

- **Phase 6 → "MVP 완료 — 2026-04-17"** 로 상태 변경. v0.6 → v0.9 릴리스 체크리스트 명시.
- **미구현 후속 후보** 섹션 추가 (멀티 head 스케줄링, subscribe/unsubscribe, dispatch, YAML 주석 보존).
- **Phase 7 (CLI)** 명령 예시에 `coord usage` 추가, 봇과 공유 내부 API 언급.
- **Phase 8+** 후보 리스트 정비 + **v1.0 기준** 명시 (DEALOS 외 2~3 프로젝트 실사용 검증 gate).
- 우선순위 요약 전면 개편 — 지금까지의 9개 릴리스 (v0.5~v0.9) 를 진행 완료로 명시.

---

## 추가/변경 파일
- `bot/controller.py` — Thread 격리 + `_binding_channel_id` helper + usage alerts loop + `_load/save_alert_state`
- `bot/projects.yml.example` — `usage_alerts` 스키마 추가 (기본 disabled)
- `bot/README.md` — Thread + 자동 경고 섹션 추가
- `coordination/ROADMAP.md` — Phase 6 완료 마킹 + Phase 7/8 정비
- `.gitignore` — `bot/usage-alerts-state.json` 추가

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.9
## bot/controller.py, projects.yml.example, README.md overwrite
## bot/projects.yml (기존) 는 preserve — 원하면 수동으로 usage_alerts 섹션 추가

taskkill //F //IM python3.11.exe    # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

Thread 생성을 활성화하려면 Discord 서버에서 봇 역할에 **Create Public Threads** 권한 추가 (초대 URL 생성 시 이미 포함됐으면 OK).

---

## Phase 6 완결 선언

**v0.6 → v0.9 9개 릴리스** 걸쳐 Discord Bot MVP 목표 달성:
- ✅ 라우팅 / 레지스트리 / 채널 자동 바인딩
- ✅ escalation 리액션 승인/거부
- ✅ retry / budget / usage 조회
- ✅ Thread 격리 + 자동 경고
- ✅ Windows 환경 호환 + 보안 (admin_user_ids, 토큰 placeholder)
- ✅ help/welcome Embed + 온보딩

### 미구현 (후속 — 필요 시)
- 멀티 head 동시 스케줄링
- `@bot subscribe` — 완료 알림 채널 구독
- ruamel.yaml 도입 (YAML 주석 보존)

🤖 Claude Opus 4.6 협업 · 총 9 릴리스 / 1일.


---

<!-- ===== v0.8.1 ===== -->

# coord-template v0.8.1 — `@bot usage` (ccusage 통합)

v0.8 의 `@bot budget` 은 프로젝트별 수동 집계 (휴리스틱 기반). v0.8.1 에서 **Claude CLI 의 실제 JSONL 로그** 를 파싱하는 [ccusage](https://github.com/ryoppippi/ccusage) 연동 — 정확한 USD 비용 + 모델별 breakdown 을 Discord 에서 바로 조회.

> v0.8.1 Thread 격리는 **deferred** (v0.9 이후 후보). 완료/중단 알림은 기존 webhook 으로 충분하다는 판단.

---

## 신규: `@bot usage [days=7]`

```
@bot usage        # 최근 7일
@bot usage 30     # 최근 30일
```

**동작**:
1. `npx ccusage@latest daily --json --since <YYYYMMDD> --order desc` 실행
2. 결과 JSON 파싱 → Discord Embed:
   - 총 비용(USD) · 총 토큰 · 활성일 수 · 평균 daily 비용
   - 상위 5일 세부 (날짜 / 비용 / 토큰 / 사용 모델)
   - powered by ccusage footer

### 표시 예시
```
📊 Claude CLI 사용량 — 최근 7일
총 $12.34 · 1,234,567 tokens
평균: $1.76/day (7 활성일)

📅 2026-04-17 · $3.21
1,024,000 tokens · models: opus, haiku

📅 2026-04-16 · $2.10
876,543 tokens · models: opus, sonnet, haiku
...
```

---

## budget vs usage 차이

| | `@bot budget` | `@bot usage` |
|---|---|---|
| 데이터 소스 | `coordination/token-budget.md` (수동) | `~/.claude/projects/*.jsonl` (실 데이터) |
| 단위 | 프로젝트별 | Claude CLI 사용자 전체 |
| 정확도 | 휴리스틱 (`claude -p` spawn 만) | 정확 (모든 대화 포함) |
| 비용 | 토큰만 | 토큰 + **실 USD** + 모델별 breakdown |
| 의존성 | coord 내장 | `ccusage` (npx auto-fetch) |

**함께 쓰기 권장**:
- `budget` → 프로젝트 한계 / 주간 리셋 관리
- `usage` → 전체 실 지출 모니터링 (월말 청구 전 감각)

---

## 구현 디테일

### `_run_ccusage()` 헬퍼
- `shutil.which("npx")` 로 풀 경로 해결 (Windows `.cmd` 대응)
- `asyncio.to_thread` 로 subprocess 블로킹 비동기화 (봇 다른 이벤트 블록 방지)
- timeout 90s (첫 npx 호출은 패키지 다운로드라 길 수 있음)
- JSON 파싱 실패 / exit code non-zero 시 명확한 에러 메시지

### `cmd_usage` 명령
- `days` 파라미터 1~90 검증
- `ctx.typing()` 으로 "입력 중…" 인디케이터 (첫 호출 지연 가려줌)
- 모델명 축약 (`claude-opus-4-6` → `opus`, `claude-haiku-4-5-...` → `haiku`)
- 5일 초과 시 "추가 N일은 ccusage 직접 실행" 힌트

### 종속성
- **사용자 시스템에 Node.js/npx 필요** (봇이 찾아 쓸 PATH)
- ccusage 자체는 npx 가 자동 설치 — 별도 설치 불필요
- npm 글로벌 설치 원하면: `npm install -g ccusage` (선택, 더 빠름)

---

## help/welcome Embed 업데이트

"🛑 제어" 섹션에 `@bot usage` 추가. `budget` 설명도 "프로젝트 토큰 예산 (progress bar, 수동 집계)" 로 명확화 — `usage` 와 혼동 방지.

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.8.1
## bot/controller.py + bot/README.md overwrite

taskkill //F //IM python3.11.exe       # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

Node.js 확인:
```bash
npx --version   # 있으면 OK, 없으면 https://nodejs.org 설치
```

---

## 미포함

- **Thread 기반 inbox 컨텍스트 격리** — 완료/중단 알림이 webhook 으로 이미 잘 들어와 deferred
- **`@bot usage` session / weekly / monthly subcommand** — 현재는 daily 만. 필요하면 v0.8.2+
- **비용 경고 임계** — coord 내장 token-budget 의 알림 임계와 별개로 ccusage 기반 자동 경고는 미구현

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.8 ===== -->

# coord-template v0.8 — 리액션 승인/거부 + retry/budget

Discord 봇이 이제 **사법부 거부 대응**까지 원격으로 처리. 이모지 리액션 한 번으로 escalation 승인/거부. `@bot retry`, `@bot budget` 으로 플랜 재시도 + 토큰 모니터링 강화.

---

## 주요 변경

### 1. 리액션 기반 escalation 처리 (핵심)
head 가 사법부에 차단되면 Discord 채널에 **🔔 head 판사 거부 — 승인 요청** 알림이 발송됨. 이제 이 메시지에 **이모지 리액션** 달면 자동 응답:
- **✅** → `/resolve-escalation <id>` spawn (승인)
- **❌** → `/reject-escalation <id>` spawn (거부)

**동작 메커니즘**:
1. `judge-action.sh` 의 Discord 알림에 `**escalation-id**: <id>` 라인 추가 (parseable 포맷)
2. 봇 `on_raw_reaction_add` 이벤트 핸들러:
   - 이모지 체크 → `admin_user_ids` 체크 → 메시지 fetch
   - 메시지 content + embed 모든 필드 스캔 → 정규식으로 escalation id 추출
   - 2개 패턴 지원: `escalation-id: <id>` / `escalations/.*-<id>.md` (fallback)
   - 현재 채널에 바인딩된 프로젝트로 `/resolve-` 또는 `/reject-escalation` spawn
3. 봇이 결과 메시지를 채널에 포스팅

**장점**: 승인/거부 시 터미널 전환 불필요. 폰에서 Discord 만 써도 됨.

### 2. `@bot retry <inbox-id> [project-id]`
head worktree 에서 `/retry <inbox-id>` spawn. needs-fix verdict 후 실패한 plan 재dispatch.
- 프로젝트 생략 시 현재 채널 바인딩 사용
- HEAD_LOCK 체크 (이미 실행 중이면 거부)
- head worktree 부재 시 setup-worktree 실행 안내

### 3. `@bot budget [project-id]`
`coordination/token-budget.md` 파싱 → Discord Embed:
- Progress bar (▰▱ 20-char)
- used / limit / 주간 리셋 타임스탬프 / last_alert
- 프로젝트 생략 시 현재 채널 바인딩 or 전체 합계 footer
- 여러 프로젝트 동시 운영 시 사용량 한눈에

### 4. 리팩터: `_spawn_claude_command()`
기존 `_spawn_inbox_send` 를 일반화. 파라미터:
- `claude_prompt`: `/inbox-send ...`, `/retry ...`, `/resolve-escalation ...` 등
- `cwd`: head worktree 등 다른 디렉토리 지정 가능
- `check_head_lock`: escalation 응답은 false (락 무관하게 긴급 처리)
- `label`: 로그 파일명 라벨

위에 얇은 래퍼 3개: `_spawn_inbox_send`, `_spawn_head_retry`, `_spawn_escalation_response`.

### 5. Help/Welcome Embed 업데이트
`@bot help` 에 **🛑 제어** 섹션 확장 (retry/budget) + **🔔 에스컬레이션 리액션** 섹션 신규. Welcome embed 도 동일 구조.

---

## 사용 예시

### 리액션 승인 플로우
```
1. op 세션에서 /inbox-send "중요한 작업"
2. head 가 처리 중 사법부 3심 거부 → escalation 자동 발송
3. Discord 채널에 "🔔 head 판사 거부 — 승인 요청" embed 도착
   (**escalation-id**: `20260417-130000` 포함)
4. 메시지에 ✅ 리액션 클릭
5. 봇: "✅ dealos resolve-esc-20260417-130000 spawn\nlog: ..."
6. op 세션이 /resolve-escalation 실행 → head 자동 재진입 → 작업 계속
```

### 토큰 예산 확인
```
@bot budget
→ 💰 토큰 예산 요약 Embed
  `dealos` — 23.5%
  ▰▰▰▰▰▱▱▱▱▱▱▱▱▱▱▱▱▱▱▱
  used: 1,175,000 / limit: 5,000,000
  week_start: 2026-04-14T00:00:00+09:00 · last_alert: 20%
```

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.8
## scripts/judge-action.sh overwrite (escalation id 포맷 변경)
## bot/controller.py, bot/README.md overwrite

## 봇 재시작
taskkill //F //IM python3.11.exe     # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

**중요**: 업그레이드 후 봇이 v0.8 코드를 로드해야 리액션 핸들러가 작동. `@bot help` 로 "🔔 에스컬레이션 리액션" 섹션이 보이면 OK.

---

## 권한 / Intent

기존 Bot Permissions 로 충분 (`Read Message History` + `Add Reactions`). 추가 설정 불필요.

intents: `discord.Intents.default()` 는 `guild_reactions` 포함 — 코드 변경 없음.

---

## 제약

- **리액션 처리는 봇 메시지 / webhook 메시지 모두 감지** (메시지 발신자 무관). 단 메시지에 escalation id 패턴 없으면 무시.
- **동일 escalation 에 여러 번 리액션**: 각 리액션마다 spawn 가능. `/resolve-escalation` 이 중복 승인 방지는 op 측 로직 담당. 보수적으로는 한 번만 클릭 권장.
- **retry 는 `/retry` 슬래시 명령이 있는 프로젝트만** (head-side). 대부분 DEALOS-like 설정에 이미 있음 — 없으면 Claude 가 unknown command 리턴.

---

## 후속 (v0.8.1 예정)

- **Thread 기반 inbox 컨텍스트 격리** — `@bot <text>` 시 inbox-id 로 Discord thread 생성 → 해당 작업의 진행 메시지/리액션을 thread 안에 격리
- 선택적: `@bot subscribe <project>` — 특정 프로젝트 완료 알림을 현재 채널에 구독

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.7.2 ===== -->

# coord-template v0.7.2 — DEALOS 잔재 2차 정리 (docs + 카피페이스터블 블록)

v0.5.1 에서 runtime 코드 하드코딩을 제거했지만 **문서/예시/복붙 bash 블록**에는 DEALOS 경로가 남아있었음. 다른 프로젝트 사용자가 이 코드를 복사 → 그대로 실행하면 경로 실패. 이번 릴리스에서 모두 config-driven 으로 일반화.

---

## 변경

### 1. `docs/setup-cloudflare-tunnel.md`
- 제목: "DEALOS Dashboard 외부 노출" → "Coordination Dashboard 외부 노출"
- `cloudflared tunnel create dealos-dashboard` → `cloudflared tunnel create <project>-dashboard`
- 전 예시의 `dealos-dashboard` / "DEALOS Dashboard" → `<project>-dashboard` / `<project> Dashboard`
- Windows 경로 하드코딩 → 일반 placeholder + OS별 예시
- 포트 `8888` → "config.yml 의 dashboard.port 와 일치" 안내

### 2. `bot/projects.yml.example`
- `default_project: dealos` → `default_project: my-project`
- `path: c:/Users/rhkde/Desktop/dealos_main/dealos` → `/path/to/my-project` (Windows 예시 주석 포함)
- `- id: dealos` → `- id: my-project`
- 주석에 v0.7+ 봇 명령 관리 + YAML 주석 손실 주의 명시

### 3. `bot/README.md`
- 예시 출력 `Projects: ['dealos']` → `Projects: ['my-project']`
- `bot-dealos-xxx.log` → `bot-<project>-xxx.log`

### 4. `coordination/USAGE.md` (카피페이스터블 블록)
- head 자동 spawn 예시: `cd ../dealos-wt-head` → `cd "$(dirname "$PROJECT_ROOT")/${PROJECT_NAME}-wt-head"`
- Discord webhook 복사 loop: `for wt in dealos-wt-head dealos-wt-db ...` → `for sub in head $(sub_names); do ...`
- setup-worktree 예시: `3dview` → `"$WORK_BRANCH"` + base-branch 생략 시 기본값 안내
- rebase 충돌 예시: `origin 3dview` → `"$REMOTE" "$WORK_BRANCH"`

### 5. `coordination/TROUBLESHOOTING.md` (카피페이스터블 블록)
다음 섹션의 bash 블록 전체가 **`source scripts/config.sh` 선행 + config 변수 사용**으로 재작성:
- §3 wt/head rebase 충돌 해결
- §4 coordination/ 대량 삭제 재발 시 복구
- §6 Sub rebase uncommitted 해결
- §7 Discord 알림 전 worktree 복사
- §8 failed/* 브랜치 수동 복구
- §10 STOP 해제 후 head 재진입
- §긴급 복구 "전부 꼬였다" 시나리오

### 유지된 (§1.2 규약 — 배너 하에 허용)
- 문서 상단 "DEALOS 기준 작성됨" 배너
- 증상/원인 설명 본문의 `3dview` / `dealos-wt-*` 역사적 언급
- 아키텍처 다이어그램에 DEALOS 예시 (명확히 "DEALOS 예시" 라벨)
- `coordination/roles/_examples/` 학습 자료

---

## 영향

- 런타임 코드는 v0.5.1 에서 이미 클린. 이번은 **문서 복붙 사용성**만 개선.
- 기존 DEALOS 사용자 영향 없음 (배너와 예시는 그대로).
- **다른 프로젝트 신규 사용자**: TROUBLESHOOTING 의 복구 명령을 그대로 복붙해도 `source scripts/config.sh` 로드만 하면 작동.

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.7.2
## docs/, bot/*, coordination/USAGE.md, TROUBLESHOOTING.md 업데이트
## manual-merge 프롬프트: README.md / .gitignore 없음 — 이번 릴리스는 overwrite 만
```

---

## 누적 DEALOS 클린 트랙

| 버전 | 범위 |
|------|------|
| v0.5.1 | Runtime (scripts/* + .claude/commands/* + templates/*) |
| **v0.7.2** | Docs 복붙 블록 (USAGE / TROUBLESHOOTING / bot / cloudflare) |

**남은 것** (의도적 유지, §1.2 규약):
- 문서 배너 / prose 설명 / `_examples/` / `coordination/config.yml.example` 의 `[DEALOS 예시]`

---

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.7.1 ===== -->

# coord-template v0.7.1 — 봇 help Embed + 서버 초대 시 자동 환영 메시지

v0.7 에 추가된 레지스트리 명령들의 **발견성(discoverability) 개선**. 봇을 처음 쓰는 사용자가 Discord 안에서 바로 사용법을 확인할 수 있음.

---

## 변경

### 1. `@bot help` — Discord Embed 로 리팩터
기존 plain text 응답 → 섹션별 field 로 구조화된 Embed:
- 🎯 라우팅 (기본 사용)
- 🔍 조회
- 🛑 제어
- 📋 레지스트리 관리 (v0.7+)
- ❓ help

제목 / 설명 / footer 포함. 모바일 Discord 에서도 정돈된 형태로 렌더링.

### 2. `on_guild_join` — 봇 추가 시 자동 환영 메시지
봇이 **새 서버에 추가되는 순간** 시스템 채널(또는 봇이 쓸 수 있는 첫 텍스트 채널)에 환영 Embed 자동 전송:
- 👋 봇 소개
- ⚙️ 먼저 해야 할 것 (admin_user_ids 설정, 프로젝트 init, register 방법)
- 🚀 빠른 시작 (채널별 register / bind 예시)
- 📚 GitHub / README 링크
- "이 메시지는 1회만 표시됩니다" footer

**채널 선택 로직**:
1. `guild.system_channel` (서버 설정에서 지정된 시스템 채널)
2. fallback: 봇이 `Send Messages` 권한 있는 첫 텍스트 채널
3. 모두 실패 시 stderr 로그 (운영자 확인)

### 3. 공용 Embed 헬퍼
`_build_help_embed()` / `_build_welcome_embed()` 분리. `help` 명령과 `on_guild_join` 모두 같은 스타일/구조 공유. 명령 추가 시 한 곳만 수정.

---

## 영향

- **기존 서버엔 환영 메시지 재전송 안 됨** (on_guild_join 은 "새로 추가될 때만" 발동). 이미 봇이 들어있는 서버는 `@bot help` 로 동일 정보 조회 가능.
- 단일 프로젝트/단일 서버 운영에도 변화 없음.
- v0.7 projects.yml 포맷 완전 호환.

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.7.1
taskkill //F //IM python3.11.exe     # 또는 Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

---

## 테스트 팁

환영 메시지 실제 렌더링 확인하려면:
1. 테스트용 Discord 서버 새로 생성
2. OAuth URL 로 봇 초대
3. 초대 직후 시스템 채널에 환영 Embed 자동 표시됨

기존 서버에선 `@bot help` 로 Embed 스타일만 확인 가능.

🤖 Claude Opus 4.6 협업.


---

<!-- ===== v0.7 ===== -->

# coord-template v0.7 — 채널 자동 라우팅 + 봇 레지스트리 관리

봇 MVP (v0.6.1) 에 **멀티 프로젝트/멀티 채널 운영**에 필요한 5개 명령 + 채널 기반 자동 라우팅 추가. 사용자가 `projects.yml` 을 수동 편집하거나 봇을 재시작할 필요 없어짐.

---

## 주요 변경

### 1. 채널 → 프로젝트 자동 라우팅

**전 (v0.6.1)**: `@bot <text>` 는 항상 `default_project` 로만 감. 다른 프로젝트는 매번 `@bot <project-id> <text>` 명시 필요.

**후 (v0.7)**: 메시지 보낸 채널의 `discord_channel_id` 와 매칭되는 프로젝트가 있으면 **자동 선택**.

**우선순위**:
1. 명시적 `@bot <project-id> <text>` → 그 프로젝트
2. 현재 채널에 바인딩된 프로젝트 → 자동 선택
3. 둘 다 없으면 → `default_project` fallback

### 2. 레지스트리 관리 명령 (5개)

| 명령 | 설명 |
|------|------|
| `@bot register <id> <path>` | 프로젝트 추가 + **현재 채널 자동 바인딩** + 즉시 반영 |
| `@bot unregister <id>` | 레지스트리 제거 |
| `@bot bind <id>` | 기존 프로젝트를 현재 채널에 (재)바인딩 |
| `@bot unbind <id>` | 채널 바인딩만 해제 (프로젝트는 유지) |
| `@bot reload` | `projects.yml` 수동 편집 후 디스크에서 재읽기 |

**+ `@bot help`** 추가 — 전체 명령 목록.

### 3. register 명령의 검증 흐름
1. 경로 존재 확인
2. coord 설치 여부 자동 감지 (flat `coordination/config.yml` 또는 subdir `.coord/coordination/config.yml`)
3. 미설치면 "먼저 `bash scripts/init.sh` 실행" 안내
4. 중복 id 면 `bind` / `unregister` 안내
5. atomic write (tmp + rename) → in-memory 갱신

### 4. 아키텍처 리팩터

- `_load_registry()` / `_save_registry()` 헬퍼 분리
- 모듈 전역 `_RAW_CFG` 로 YAML 원본 보존 → 봇이 몰라도 되는 필드는 그대로 유지됨
- 재진입 안전 (reload 여러번 OK)

---

## 신규 프로젝트 등록 흐름 (v0.7+ 간단 버전)

### Phase 1: 터미널 (프로젝트에 coord 1회 설치)
```bash
## flat
cd /path/to/myapp
bash scripts/init.sh

## 또는 subtree
git subtree add --prefix=.coord https://github.com/gone7729/coord-template.git v0.7 --squash
bash .coord/scripts/init.sh
```

### Phase 2: Discord (봇 레지스트리 추가)
```
## 프로젝트 전용 채널에서
@coord-bot register myapp /path/to/myapp
```
- 경로 검증 + 채널 바인딩 자동 + 즉시 라우팅 활성
- 이제 그 채널에서 `@coord-bot <text>` 만 쳐도 myapp 으로 감

---

## 제약 / 주의

- **YAML 주석 손실**: 봇 명령으로 저장 시 `projects.yml` 의 주석이 사라짐 (PyYAML 제약). 주석 유지하려면:
  - 수동 편집 + `@bot reload` (권장)
  - 또는 봇 명령 실행 후 필요 주석 재추가
- **admin_user_ids 빈 상태 시 전원 허용** → 공개 서버는 반드시 채울 것
- **register 경로는 절대경로 권장** — 상대경로도 동작하지만 봇 작업 디렉토리 기준이라 혼동 가능

---

## 업그레이드 (v0.6.1 → v0.7)

```bash
bash scripts/upgrade-coord.sh --version v0.7
## bot/controller.py + bot/README.md 덮어쓰기 (projects.yml 은 preserve)

## 봇 재시작
taskkill //F //IM python3.11.exe     # Linux: pkill -f bot/controller.py
bash scripts/start-bot.sh --bg
```

기존 `projects.yml` 포맷은 v0.7 과 완전 호환 — 마이그레이션 불필요.

---

## 후속 (v0.8 후보)

- 리액션 기반 승인/거부 (✅/❌ → `/resolve-escalation` / `/reject-escalation`)
- `@bot retry <inbox-id>` — 실패 plan 재시도
- `@bot budget` — 토큰 사용량 요약
- Thread 기반 inbox 컨텍스트 격리
- 봇 명령 저장 시 YAML 주석 보존 (ruamel.yaml 옵션)

🤖 Claude Opus 4.6 (1M context) 협업.


---

<!-- ===== v0.6.1 ===== -->

# coord-template v0.6.1

**v0.6 smoke test (Windows Git Bash + DEALOS) 에서 발견된 3종 이슈 fix**.

v0.6 릴리스 당일 실제 Discord 서버에 봇을 올려 `/inbox-send` E2E 를 시도하면서 드러난 Windows 플랫폼 고유 문제들을 해결. repo 자체 검증만으로는 잡을 수 없었던 통합 이슈.

---

## 수정 내역

### 1. `bot/controller.py` — stdout UTF-8 강제
**증상**: `python bot/controller.py` 직접 실행 시 한글 에러 메시지가 cp949 로 mojibake (`❌ ... 없음` → `\u274c ... ����`).
**원인**: Windows 기본 stdout 인코딩이 cp949, 비 TTY 환경에서 UTF-8 강제 안 됨.
**수정**: 파일 상단에 `sys.stdout.reconfigure(encoding="utf-8")` / `sys.stderr.reconfigure(...)`.
**영향**: start-bot.sh 경유하지 않는 직접 실행에서도 한글 출력 정상.

### 2. `bot/controller.py` — `claude` CLI 풀 경로 사전 해결
**증상**: `@bot <text>` spawn 시 Discord 로 "❌ `claude` CLI 찾을 수 없음 (PATH 확인)" 응답.
**원인**: Windows 에서 `claude` 는 `.cmd` 래퍼. `subprocess.Popen(["claude", ...], shell=False)` 는 `.cmd` 확장자를 자동 해결 못함.
**수정**:
- 시작 시 `shutil.which("claude")` 로 풀 경로(`C:\nvm4w\nodejs\claude.CMD`) 해결해 전역 `CLAUDE_BIN` 에 저장
- `COORD_CLAUDE_BIN` 환경변수로 override 가능
- `which` 로도 못 찾으면 즉시 fail (안내 메시지 포함)
- `subprocess.Popen` 은 `[CLAUDE_BIN, ...]` 으로 호출

### 3. `bot/controller.py` — 로그 디렉토리 `tempfile.gettempdir()` 사용
**증상**: 봇 응답 메시지의 로그 경로(`/tmp/bot-dealos-*.log`)를 Git Bash 에서 열면 `No such file`.
**원인**: Windows 에서 Git Bash 의 `/tmp` 는 `C:\Users\<user>\AppData\Local\Temp` 로 매핑되지만, Windows Python 은 `/tmp` 를 `C:\tmp` 로 해석 → 두 프로세스가 서로 다른 디렉토리에 접근.
**수정**:
- `LOG_DIR` 기본값을 `tempfile.gettempdir()` (Windows 에서 `AppData\Local\Temp`) 로
- 봇 응답 메시지도 로그 파일 **절대 경로**를 포함해 복사 → 터미널에 바로 붙여넣기 가능
- `COORD_BOT_LOG_DIR` 환경변수로 override 가능

### 4. `scripts/start-bot.sh` — `PYTHONUNBUFFERED=1` 추가
**증상**: `start-bot.sh --bg` nohup 실행 시 `on_ready` 배너가 로그에 한참 안 나타남.
**원인**: Python stdout 이 non-TTY 환경에서 블록 버퍼링 → 봇이 online 인지 즉시 확인 안 됨.
**수정**: `PYTHONUNBUFFERED=1` export 추가 + `controller.py` 의 `print()` 호출에 `flush=True`.

---

## Smoke test 결과 (v0.6.1 검증)

DEALOS 프로젝트 대상 실 Discord E2E 완료:
- ✅ Discord 메시지 → 봇 라우팅
- ✅ subprocess claude CLI spawn (Windows `.cmd` 해결)
- ✅ `/inbox-send` 슬래시 명령 실제 실행 → `coordination/inbox/<ts>.md` 생성
- ✅ git commit + push to `origin/<work_branch>` 성공
- ✅ 기존 webhook 경로로 Discord 완료 알림 (HTTP 204)
- 💰 비용 예시: $0.36 (Opus 40s, 7 turns) per inbox

`/inbox-send <text>` 실행은 head 자동 spawn 로직과도 연동되므로 Claude 가 요청 범위에 따라 head spawn 여부를 판단.

---

## 업그레이드 (v0.6 → v0.6.1)

```bash
bash scripts/upgrade-coord.sh --version v0.6.1
## bot/controller.py 만 덮어쓰기 (projects.yml 은 preserve).
## 봇 재시작:
taskkill //F //IM python3.11.exe   # 또는 Linux: pkill -f "bot/controller.py"
bash scripts/start-bot.sh --bg
```

---

## 후속 (v0.7 후보)

v0.6 MVP 범위 유지. 다음 릴리스에서 고려:
- 리액션 기반 승인/거부 (✅ → resolve / ❌ → reject)
- `@bot retry <inbox-id>`
- `@bot budget` — 토큰 요약
- Thread 기반 inbox 컨텍스트 격리
- 봇/webhook 완료 알림 단일화

🤖 Claude Opus 4.6 협업 + 사용자 실 Smoke test.


---

<!-- ===== v0.6 ===== -->

# coord-template v0.6 — Phase 6 MVP: Discord Bot Controller

**Discord 봇을 통한 원격 작업 지시** — 핸드폰/웹 Discord 에서 `@bot <text>` 한 줄로 등록 프로젝트의 `/inbox-send` spawn.

> Phase 6 전체 설계는 ROADMAP 참조. 이 릴리스는 **라우팅 + 상태 조회 MVP**. 리액션 승인/retry/budget 은 후속.

---

## 핵심 변경

### 새 구성요소
- **`bot/controller.py`** — discord.py 기반 봇. 메시지 → subprocess `claude -p "/inbox-send <text>"` 라우팅
- **`bot/projects.yml.example`** — 멀티 프로젝트 레지스트리 포맷 (default_project, allowed_channel_ids, admin_user_ids)
- **`bot/requirements.txt`** — discord.py>=2.3 / PyYAML / python-dotenv
- **`bot/README.md`** — Developer Portal 세팅 / Intent / Invite URL / systemd 예시까지 엔드투엔드 가이드
- **`scripts/start-bot.sh`** — foreground/`--bg` 실행, `.env.local` 자동 로드

### 보조
- **`.env.local.example`** — `DISCORD_BOT_TOKEN=` placeholder + ⚠ 실제 토큰 금지 경고 추가
- **`.gitignore`** — `bot/projects.yml` 추가 (사용자 고유 프로젝트 경로 포함하므로)
- **`scripts/lib/classify.sh`** — bot/ 하위 분류 규칙 추가 (controller.py / README / .example → overwrite, projects.yml → preserve)

---

## 아키텍처

```
Discord 사용자 (@bot <text>)
        ↓ WebSocket
Discord Gateway
        ↓ push
controller.py (로컬 상시 구동)
        ↓ subprocess.Popen (shell=False)
claude -p "/inbox-send <text>"  (프로젝트 루트 cwd)
        ↓ 기존 coord 경로
head → sub → 완료
        ↓
head 가 기존 notify-discord.sh 로 Discord 에 결과 webhook
```

**원칙**: 봇은 라우터 + 상태 조회만. inbox 처리/plan/완료 알림은 **기존 coord 경로가 담당**. 봇이 중복 발송하지 않음.

---

## 지원 명령 (MVP)

| 명령 | 동작 |
|------|------|
| `@bot <text>` | default_project 로 `/inbox-send` spawn |
| `@bot <project_id> <text>` | 특정 프로젝트로 라우팅 |
| `@bot status [project_id]` | HEAD_LOCK + pending inbox 요약 |
| `@bot projects` | 등록 프로젝트 목록 + path 존재 확인 |
| `@bot stop <project_id>` | `coordination/STOP` 작성 (커밋/푸시는 사용자) |

---

## 보안 / 동시성

- **HEAD_LOCK 사전 체크** — 이미 실행 중이면 spawn 거부 (중복 방지)
- **admin_user_ids** — 비우면 전원 허용 (비공개 서버 한정), 공개 서버는 필수 채움
- **allowed_channel_ids / discord_channel_id** — 채널 단위 권한 격리
- **subprocess.Popen shell=False** — 사용자 입력이 셸 인자로 해석 안 됨
- **Token 취급**: `.env.local.example` 은 placeholder 만, 실제 토큰은 `.env.local` (gitignored)

---

## 사전 준비 (신규 기능)

1. Discord Developer Portal → App 생성 → Bot 탭 → Token Reset
2. Privileged Gateway Intents: **Message Content Intent** 활성
3. OAuth2 URL Generator → bot scope + Send/Read/Embed 권한 → 본인 서버 초대
4. `.env.local` 에 `DISCORD_BOT_TOKEN=` 기입
5. `bot/projects.yml.example` → `bot/projects.yml` 복사, 프로젝트 경로 / admin id 기입
6. `pip install -r bot/requirements.txt`
7. `bash scripts/start-bot.sh` (foreground) 또는 `--bg` (nohup)

자세한 가이드: `bot/README.md`

---

## 호스팅 비용

- Discord 자체: **$0** (Developer Portal / 본인 서버 무료)
- Bot 인프라: 로컬 PC / Raspberry Pi / Oracle Cloud Free / 저렴 VPS
- 실질 비용은 **Claude API 호출료** (Opus head + Sonnet sub 병렬) — 별개 예산

---

## 업그레이드

```bash
bash scripts/upgrade-coord.sh --version v0.6
## bot/ 은 새 디렉토리라 자동 추가
## bot/projects.yml 작성은 사용자 몫
```

---

## 미포함 (후속 후보)

- 리액션 기반 승인/거부 (`✅` → `/resolve-escalation`, `❌` → `/reject-escalation`)
- `@bot retry <inbox-id>` — 실패한 plan 재시도
- `@bot budget` — 토큰 사용량 요약 (전체/프로젝트별)
- Thread 기반 컨텍스트 — 각 inbox 를 Discord thread 로 격리
- 봇 완료 알림 통합 — head webhook + 봇 메시지 단일 embed

---

## 검증

- `python3 -m py_compile bot/controller.py` 통과
- `bash -n scripts/start-bot.sh` 통과
- classify_path 단위 테스트 (bot/* 경로 5종) — 기대값 일치
- **실 Discord E2E 는 이 repo 에서 수행 불가** (토큰/서버 필요) — 사용자가 `bot/README.md` 따라 세팅 후 smoke test

🤖 Claude Opus 4.6 (1M context) 협업.


---

<!-- ===== v0.5.1 ===== -->

# coord-template v0.5.1

**Fix release**: DEALOS(`3dview`, `dealos-wt-*`, `dealos/`) 하드코딩을 runtime 코드에서 완전 제거.

> 사용자 제보: "다른 프로젝트에서 사용할 때 문제될 수 있음"

---

## 변경 범위

Runtime 에 남아있던 하드코딩만 제거. `_examples/`, 문서의 "DEALOS 예시" 배너는 CLAUDE.md §1.2 규약상 유지.

### Scripts (Python + Bash)
- **`scripts/dashboard.py`** — 파일 상단에 `coordination/config.yml` 로드 추가. `PROJECT_NAME` / `WORK_BRANCH` / `HEAD_BRANCH` / sub 목록 / `DASHBOARD_PORT` 모두 config 에서. 하드코딩된 `"dealos-wt-head"` / `"3dview..origin/wt/<sub>"` / `["db", "backend", "frontend"]` 제거.
- **`scripts/notify-discord.sh`** — Discord embed footer `"DEALOS coordination"` → `"${PROJECT_NAME} coordination"` (Python + bash fallback 둘 다)
- **`scripts/judge-action.sh`** — 판사 프롬프트 `"DEALOS 자율 오케스트레이션"` → `"${PROJECT_NAME} 자율 오케스트레이션"`
- **`scripts/setup-worktree.sh`** — PROJECT_NAME fallback `"dealos"` → `"project"` (config 없을 때만 발동)
- **`scripts/start-dashboard.sh`** — config.sh 자동 source → `$DASHBOARD_PORT` 기본값 활용

### HTML 템플릿
- **`templates/dashboard.html`** — `<title>` 및 `<h1>` 을 `{{ data.project_name }}` 으로 바인딩
- **`templates/partials/main.html`** — escalation source 비교 `'3dview'` → `data.work_branch`

### Op-side 슬래시 명령 (.claude/commands/)
모든 명령 상단에 **"## 0. 환경 로드"** 블록 추가 — config.sh 자동 source (flat/subdir 양쪽 지원):

```bash
if [[ -f scripts/config.sh ]]; then source scripts/config.sh
elif [[ -f .coord/scripts/config.sh ]]; then source .coord/scripts/config.sh
else echo "❌ config.sh 없음"; exit 1
fi
```

- **`inbox-send.md`** — `git push origin 3dview` → `git push "$REMOTE" "$WORK_BRANCH"`, head worktree 경로 동적 계산
- **`resolve-escalation.md`** / **`reject-escalation.md`** — 동일 패턴
- **`review-inbox.md`** (큰 파일 — 전면 재작성) — 하드코딩된 `db backend frontend` 루프 → `"${SUBS[@]}"` 배열, **`c:/Users/rhkde/Desktop/dealos_main/...`** 절대 Windows 경로 완전 제거, validation 명령을 `sub_validation` 헬퍼로 동적 실행
- **`status.md`** — SUBS 배열 루프 + `$HEAD_BRANCH` / `$WORK_BRANCH` 치환
- **`fix.md`** / **`token-status.md`** — DEALOS 배너 정리 + 환경 로드 블록 (fix.md)

---

## 유지된 "DEALOS 예시" (의도)

CLAUDE.md §1.2 규약 — "문서 상단에 '템플릿 사용자 주의' 배너 유지":
- `coordination/README.md`, `USAGE.md`, `ROADMAP.md`, `TROUBLESHOOTING.md`
- `coordination/roles/_examples/*.md` (학습 자료)
- `coordination/config.yml.example` 의 `[DEALOS 예시]` 주석
- `INSTALL.md` / `CLAUDE.md` 본문 내 역사적 맥락

이들은 문서 독자에게 **원본이 DEALOS 라는 사실을 알리는 가치**가 있어 유지. 사용자는 이 배너를 보고 자기 프로젝트 값으로 해석.

---

## 업그레이드 (v0.5 → v0.5.1)

```bash
bash scripts/upgrade-coord.sh --version v0.5.1
```

`.claude/commands/*.md` 는 **manual-merge** 분류라 diff 확인 후 승인해야 반영됨 (`--accept-commands` 로 일괄 승인 가능).

---

## 검증

- `bash -n scripts/*.sh` 전체 통과
- `python3 -m py_compile scripts/dashboard.py` 통과
- `grep "3dview|dealos-wt|dealos/" scripts/ .claude/ templates/` → **빈 결과** (runtime 클린)

---

## 포함 commit

- `fix(hardcoding): DEALOS/3dview/dealos-wt-* 하드코딩 runtime 완전 제거`

---

## 후속 (이 릴리스 미포함)

- `coordination/USAGE.md` / `TROUBLESHOOTING.md` 의 **카피페이스터블 bash 블록** 일반화 (예: `for wt in dealos-wt-head dealos-wt-db ...`) — 다음 마이너 릴리스에서 고려.

🤖 Claude Opus 4.6 (1M context) 협업.


---

<!-- ===== v0.5 ===== -->

# coord-template v0.5

v0.2 이후 누적된 3개 후보(Phase 5-8 업그레이드 자동화, Phase 5-9 subdir 모드, Phase 2 안정화)를 **한 릴리스로 통합**.

> DEALOS 검증을 릴리스 게이트로 운영(CLAUDE.md §2.3). v0.2 에 오래 머무는 대신 자체 E2E + bash 구문 검증 + dry-run 시나리오 통과분을 한 번에 승격.

---

## Highlights

### 🔧 Phase 5-8 — 업그레이드 자동화 (v0.3 후보 통합)
- **`scripts/upgrade-coord.sh`** — 10단계 플로우, `tarball` / `git` / `subtree` 3-mode 지원.
  파일 분류 규칙(overwrite / preserve / manual-merge / skip)으로 **사용자 설정·런타임 상태를 깨지 않고** 업그레이드.
- **`scripts/lib/classify.sh`** — 분류 엔진 (파일별 정책 + root 스코프)
- **`scripts/lib/sync-claude.sh`** — `.claude/` 재동기화 헬퍼 (v0.4 와 공유)
- **`coordination/.coord-version.example`** — 버전 메타 포맷 (init/upgrade 가 갱신)
- **`VERSION`** — 릴리스 시 bump 하는 SSOT
- **`docs/upgrade.md`** — 사용자 가이드

### 📦 Phase 5-9 — `.coord/` subdir 모드 (v0.4 후보 통합)
- 신규 프로젝트가 `git subtree add --prefix=.coord ...` 로 템플릿을 서브디렉토리에 마운트 가능
- **`COORD_ROOT` / `PROJECT_ROOT` / `INSTALL_MODE`** 자동 감지 (config.sh)
  - coordination/, scripts/, templates/ → COORD_ROOT
  - .claude/, .env.local, .gitignore, .gitattributes → PROJECT_ROOT
- **`scripts/init.sh --mode=flat|subdir|auto`** — 모드별 설치 로직
- **`.gitattributes`** — `*.sh`/`*.py`/`*.md` LF 강제 (Windows + subtree CRLF 오염 방지)
- **`docs/install-subtree.md`** — 신규 프로젝트 subtree 가이드
- flat 모드는 완전 하위 호환 (COORD_ROOT = PROJECT_ROOT)

### 🛡 Phase 2 — 안정화 (v0.5 후보)
- **`scripts/notify-step.sh`** — Step 별 Discord 알림 표준 포맷.
  `bash scripts/notify-step.sh <plan_id> <step>/<total> <sub> <status> [<summary>]`
  status: `started | ok | done | fail | blocked | skip`
- **`scripts/reset-wt-head.sh`** — 전면 config-driven 재작성 (하드코딩된 `dealos-wt-head`/`origin/3dview` 제거)
- **`coordination/TROUBLESHOOTING.md` §16** — 업그레이드 실패 복구 시나리오

### 📝 문서 / 거버넌스
- **`CLAUDE.md`** — 템플릿 유지보수 개발 규약 (PR 원칙 / 테스트 정책 / 역전파 창구)
- **`coordination/ROADMAP.md`** — Phase 2 완료 + Phase 5-8 / 5-9 설계 섹션 추가

---

## 업그레이드 (v0.2 → v0.5)

### 이미 flat 모드로 설치된 프로젝트 (예: DEALOS)

```bash
## 1. 업그레이드 전 dry-run 필수
bash scripts/upgrade-coord.sh --version v0.5 --dry-run

## 2. 실행 (manual-merge prompt 는 diff 읽고 판단)
bash scripts/upgrade-coord.sh --version v0.5

## 3. 커밋
git add -A && git commit -m "chore: upgrade coord-template to v0.5"
```

> **`upgrade-coord.sh` 는 v0.3 부터 도입됨** — v0.2 이하 프로젝트는 최초 1회 수동 복사 (또는 git subtree pull) 후 이 스크립트 사용 가능.

### 신규 프로젝트 (subdir / subtree 추천)

```bash
git subtree add --prefix=.coord \
  https://github.com/gone7729/coord-template.git v0.5 --squash

bash .coord/scripts/init.sh
```

---

## Breaking changes
**없음.** flat 모드는 기존과 동일 동작. subdir 모드는 신규 설치 한정 신규 옵션.

## Migration notes
- flat → subdir 마이그레이션 도구는 미제공 (v0.5 에선 의도적 제외)
- 기존 프로젝트는 flat 모드 유지 + `upgrade-coord.sh` 로 지속 업그레이드
- subtree prefix 는 `.coord` 만 지원 (basename 감지 로직; custom prefix 는 후속 릴리스)

---

## 검증
- **flat 모드 regression** — 기존 스크립트 bash 구문 전수 통과, COORD_ROOT = PROJECT_ROOT 보존
- **subdir 모드 E2E** — init.sh `--mode=subdir` 실행 → `.claude/` 루트 동기화, `.gitignore` 의 `.coord/coordination/*` prefix, `core.hooksPath = .coord/.githooks`, `.coord-version install_mode=subdir` 기록 확인
- **upgrade-coord.sh dry-run** — v0.1→v0.2 시뮬레이션(flat) 및 subdir+git mode 시뮬 10단계 전 구간 통과
- **Windows Git Bash** — `cygpath -w` 를 통한 Python 경로 변환으로 `/tmp/*` 및 `/c/*` 환경 대응

---

## 후속 후보 (이 릴리스 미포함)

- **Phase 6** — Discord Bot + Controller (2~3주 범위)
- flat → subdir 마이그레이션 도구
- custom subtree prefix (`.coord` 외 이름)
- dashboard.py / clipboard-to-attachment.sh 의 subdir 모드 완전 인식 (현재 flat 기본)

---

## 포함 commits

- `a4dec61` — docs: CLAUDE.md 개발 규약 추가
- `55e100c` — feat(phase 5-8): upgrade-coord.sh + 분류 엔진 + .claude 재동기화
- `273013e` — feat(phase 5-9): `.coord/` 서브디렉토리 모드 + subtree 지원
- `d73baad` — feat(phase 2): notify-step.sh — Step 별 Discord 알림 표준 helper
- (VERSION bump)

---

🤖 릴리스 준비: Claude Opus 4.6 (1M context) 협업. 실 검증은 DEALOS 프로젝트에서.


---


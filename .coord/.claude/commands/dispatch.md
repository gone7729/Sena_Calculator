---
description: "[HEAD 전용] 지정된 sub worktree 들 headless 순차 실행"
argument-hint: "<sub-list — 예: db backend frontend>"
requires-wt: head
min-coord-version: v0.42
---

**⚠ wt/head 에서만 실행.**

## 경로 해결
시작 시 config.sh 를 source 해서 `COORD_ROOT`, `PROJECT_ROOT`, `PROJECT_NAME` 얻기:

```bash
source "$(git rev-parse --show-toplevel)/scripts/config.sh" 2>/dev/null || \
  source "$(git rev-parse --show-toplevel)/.coord/scripts/config.sh"
SIBLING_BASE="$(dirname "$PROJECT_ROOT")"
```

## 절차

1. task 파일 (`$COORD_ROOT/coordination/tasks/wt-<sub>.md`) → sub worktree 로 복사:
   ```bash
   SUB_DIR="$SIBLING_BASE/${PROJECT_NAME}-wt-<sub>"
   # sub 의 COORD_ROOT 결정 (subdir 면 .coord/, flat 면 루트)
   SUB_COORD="$SUB_DIR/.coord"
   [[ ! -d "$SUB_COORD" ]] && SUB_COORD="$SUB_DIR"
   cp "$COORD_ROOT/coordination/tasks/wt-<sub>.md" "$SUB_COORD/coordination/tasks/"
   cp "$COORD_ROOT/coordination/STATUS.md" "$SUB_COORD/coordination/" 2>/dev/null || true
   cp "$COORD_ROOT/coordination/hotfixes.md" "$SUB_COORD/coordination/" 2>/dev/null || true
   ```

2. **v0.47+ background + poll 패턴** — 각 sub 마다:

   ```bash
   # 모델 결정 (config.yml 참조)
   MODEL=$(python3 -c "
   import yaml
   d = yaml.safe_load(open('$COORD_ROOT/coordination/config.yml'))
   print(next(s['model'] for s in d.get('subs', []) if s['name'] == '<sub>'))
   ")

   # background spawn (즉시 return)
   SPAWN_OUT=$(bash "$COORD_ROOT/scripts/spawn-sub-bg.sh" "<sub>" "$MODEL" 1800)
   SUB_PID=$(echo "$SPAWN_OUT" | grep -oE 'PID=[0-9]+' | cut -d= -f2)
   SUB_LOG=$(echo "$SPAWN_OUT" | grep -oE 'LOG=[^ ]+' | cut -d= -f2)
   SUB_START=$(echo "$SPAWN_OUT" | grep -oE 'START_TS=[0-9]+' | cut -d= -f2)
   ```

   **이후 polling — 매 30초마다 별도 Bash 호출** (head Bash 도구 10분 cap 우회):
   ```
   bash $COORD_ROOT/scripts/poll-sub.sh $SUB_PID $SUB_START 1800
   결과 한 줄: RUNNING <elapsed> | DONE <elapsed> | TIMEOUT <elapsed>
   ```

   - RUNNING: sleep 30 후 재 poll. 필요 시 `tail -20 $SUB_LOG` 로 진행 확인
   - DONE: log 마지막 줄의 JSON 에서 `is_error` 확인 → 성공/실패 판정
   - TIMEOUT: 실패 처리

3. 종료 코드 + `$SUB_COORD/coordination/reports/wt-<sub>.md` 의 `status` 필드 확인
4. 성공 → 다음 sub, 실패 → `failed/wt-<sub>-<ts>` 브랜치 보관 + 중단
5. 결과 요약 + `bash "$COORD_ROOT/scripts/notify-discord.sh"` 로 Discord

**참고 — `timeout 1800 claude -p "/go"` 직접 동기 호출 금지**: head Bash 도구 자체가 max 10분 (Anthropic 플랫폼 default) cap 이라 sub 가 30분 걸리면 head 가 못 기다림. 반드시 background + poll 사용.

## 기본 타임아웃
- db / frontend: 15분 (900s) — 가벼운 작업 위주
- backend: 30분 (1800s) — 복잡도 높음
- config.yml 에 `subs[].timeout` 있으면 그 값 우선

## 금지
- task 없이 sub spawn
- scope_allowed 밖 파일 수정 지시 (config.yml 참조)
# Coordination — Head/Sub 세션 오케스트레이션

> **템플릿 사용자 주의**: 이 문서는 원본 DEALOS 프로젝트(브랜치 `3dview`, 디렉토리 `dealos/`) 기준으로 작성됨.
> 내 프로젝트의 브랜치/디렉토리명으로 읽어 해석하면 됨. 정본 설정은 [config.yml.example](config.yml.example) → `coordination/config.yml`.

병렬 worktree 작업 시 세션 간 조율용 파일 저장소.

## 구조
```
coordination/
├── tasks/      head가 sub에게 내리는 작업지시서 (wt-<name>.md)
├── reports/    sub가 완료 후 head에게 보고하는 결과 (wt-<name>.md)
└── STATUS.md   전체 진행 현황 보드 (head가 갱신)
```

## 역할 분리

### Head 세션 (메인 repo: `dealos/`, 브랜치 `3dview` 등)
- 사용자 요청 수신 → 작업 분해
- 각 worktree별 `tasks/wt-<name>.md` 작성
- `STATUS.md` 갱신
- sub의 `reports/wt-<name>.md` 읽고 검증/통합

### Sub 세션 (worktree: `dealos-wt-a/` 등)
- 시작 시 `coordination/tasks/wt-<name>.md` 읽고 실행
- 완료 후 `coordination/reports/wt-<name>.md` 작성
- 자기 도메인 밖 파일 수정 금지

## 브랜치 플로우 (중요)

- `wt/*` (sub) → `3dview` : 자동화 범위 (head/sub/메인이 검증 + 머지)
- `3dview` → `main` : **수동 전용.** 사용자+협업자 협의 후에만 진행
- head/sub 가 "머지 가능" 이라 할 때는 **3dview 기준**

## 지시 흐름

```
사용자 → Head
         ↓ (분해)
         tasks/wt-a.md  tasks/wt-b.md  tasks/wt-c.md
         ↓ (사용자가 각 worktree Claude에 "task 읽고 실행" 전달)
         Sub-a          Sub-b          Sub-c
         ↓ (완료)
         reports/wt-a.md reports/wt-b.md reports/wt-c.md
         ↓
         Head (검증/통합) → 사용자 보고
```

## 원격 진입 경로 (Phase 6, v0.6+)

**Discord 봇** (`bot/controller.py`) 을 통해 op 계층에 원격 트리거 가능. 사용자 관점에선 op 슬래시 명령과 1:1 매핑:

```
사용자 (Discord)
  ↓ @bot send <작업>
Bot (로컬 상시구동)
  ↓ subprocess spawn
op 세션 (`claude -p "/inbox-send <작업>"`)
  ↓ (기존 흐름)
head → sub → review → Discord 리액션 prompt → 머지/fix
```

자세한 흐름: [USAGE.md §7](USAGE.md#7-discord-봇-기반-운영-phase-6-v06) / [bot/README.md](../bot/README.md)

터미널 vs 봇은 **진입점만 다를 뿐 동일 조율 메커니즘** — 파일 기반 coordination / 사법부 / 토큰 예산 그대로.

## Task 파일 템플릿 (`tasks/wt-<name>.md`)

```markdown
# Task: <작업 제목>
- 담당: wt/<name>
- 브랜치: wt/<name>
- 범위: <수정 허용 경로 예: core/massing/*>
- 금지: <건드리면 안 되는 경로>
- 배경: <왜 하는지, 관련 맥락>

## 목표
- [ ] 구체 목표 1
- [ ] 구체 목표 2

## 제약
- SSOT 규칙 준수 (CLAUDE.md 참조)
- 테스트: <실행해야 할 명령>

## 완료 기준
- <verifiable check>

## 보고 방식
완료 시 `coordination/reports/wt-<name>.md` 작성:
- 변경 파일 목록
- 테스트 결과
- 주의할 사이드이펙트
- 다음 작업자에게 남기는 노트
```

## Sub 세션 시작 프롬프트 (복붙용)

```
너는 wt/<name> 워커 세션이다.
1. coordination/tasks/wt-<name>.md 를 읽어라
2. 거기 명시된 범위와 제약만 준수해서 구현해라
3. 완료 시 coordination/reports/wt-<name>.md 에 결과 보고서 작성
4. 범위 밖 파일은 절대 수정하지 마라
5. 막히면 수정하지 말고 보고서에 "blocked: <이유>" 남기고 멈춰라
```

## Head 세션 시작 프롬프트 (복붙용)

```
너는 head 오케스트레이터 세션이다.
- 내가 주는 요청을 worktree 단위로 분해한다
- coordination/tasks/wt-<name>.md 를 작성해서 각 sub에게 지시한다
- 직접 구현은 하지 않는다 (경량 수정은 예외)
- 각 sub의 reports/ 를 읽고 충돌/누락을 검증한다
- STATUS.md 를 유지한다
```

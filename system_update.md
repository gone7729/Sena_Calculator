# 웹 배포 작업 계획 (Next.js + Vercel)

> 작성일: 2026-04-21
> 대상: 세븐나이츠 리버스 캐릭터/장비/추천/옵티마이저 웹 앱
> 관련 inbox: [`.coord/coordination/inbox/2026-04-20-205220.md`](.coord/coordination/inbox/2026-04-20-205220.md)

## 확정 사양

| 항목 | 값 |
|---|---|
| 프레임워크 | Next.js (App Router) |
| 언어 | TypeScript |
| UI | React |
| 배포 | Vercel |
| 데이터 | JSON-only (Supabase 미사용) |
| DB 이전 | C# DB 클래스 → JSON export 스크립트 |
| 기존 WPF 계산기 | 유지 (병행 운영) |

---

## Phase 0 — 준비 & 디렉토리 구조 결정

**목표**: 웹 앱 코드의 위치와 repo 전략 확정

- [ ] 디렉토리 위치 결정
  - (a) `web/` 서브디렉토리 — 현재 repo 내 공존 (추천)
  - (b) 별도 repo 분리 — 독립 관리, 데이터 동기화 필요
- [ ] 루트 `.gitignore` 에 `web/node_modules`, `web/.next`, `web/.vercel` 추가
- [ ] README 에 웹 앱 섹션 추가 (기존 WPF 계산기와 관계 명시)

**산출물**: 디렉토리 결정, gitignore 업데이트

---

## Phase 1 — 데이터 레이어 (C# → JSON export)

**목표**: WPF 계산기가 쓰는 DB 데이터를 웹에서 소비 가능한 JSON 으로 변환

- [ ] export 스크립트 설계
  - (a) .NET 콘솔 앱: 기존 `DB/*.cs` 를 직접 참조해 JSON 직렬화 (추천)
  - (b) Roslyn 파싱: C# 소스를 AST 로 읽어 변환
- [ ] 대상 데이터 정의
  - `CharacterDB` → `characters.json` (스킬/패시브/스탯)
  - `EquipmentDB` → `equipment.json`, `equipment-sets.json`
  - `EnemyDB` → `enemies.json` (보스/일반몹)
  - `BasicStatDB` → `basic-stats.json` (등급/타입 기본 스탯, 초월 보너스)
  - `PetDB` → `pets.json`
  - `StatusEffect.cs` → `status-effects.json`
- [ ] TypeScript 타입 정의 (`types/*.ts`) — JSON schema 와 1:1 매핑
- [ ] export 실행 스크립트 (`scripts/export-db.sh` 또는 `tools/DataExporter/`)
- [ ] CI/pre-commit 훅에서 WPF DB 변경 시 JSON 재생성 유도 검토

**산출물**: `web/data/*.json`, `web/types/*.ts`, export 도구

**리스크**: C# 표현식(예: `new BuffSet { ... }`)이 JSON 으로 손실 없이 직렬화되는지 검증 필요

---

## Phase 2 — Next.js 프로젝트 scaffolding

**목표**: 배포 가능한 빈 껍데기 확보

- [ ] `create-next-app@latest web --typescript --app --tailwind --eslint` 실행
- [ ] 기본 레이아웃 (`app/layout.tsx`) — 네비게이션 (캐릭터/장비/던전/옵티마이저)
- [ ] 스타일 기반: Tailwind + shadcn/ui 도입 검토
- [ ] 다크 모드 (기본 다크, 게임 톤에 맞춤)
- [ ] 데이터 로딩 유틸 (`lib/data.ts`) — JSON import / fetch 추상화
- [ ] Vercel 프로젝트 생성 + GitHub 연동 + 첫 배포 성공
- [ ] 도메인 결정 (Vercel 기본 / 커스텀)

**산출물**: 배포된 빈 Next.js 앱 URL

---

## Phase 3 — 기본 정보 페이지 (MVP 진입)

**목표**: 데이터 열람 페이지 — 계산/추천 없이 "보기"만 먼저

- [ ] 캐릭터 목록 페이지 (`/characters`)
  - 등급/타입/속성 필터, 정렬
  - 썸네일 (있으면) + 핵심 스탯
- [ ] 캐릭터 상세 페이지 (`/characters/[id]`)
  - 스킬 1~4 + 궁극기, 패시브, 초월 보너스
  - 효과(Effects / 레거시) 파싱 및 표시
- [ ] 장비 목록 (`/equipment`) — 세트별 효과, 메인/서브 옵션
- [ ] 장신구 목록 — 등급별 보너스
- [ ] 펫 목록 — 기본 스탯, 펫 옵션
- [ ] 상태이상 사전 (`/status-effects`) — DoT/CC/특수 분류
- [ ] 적/보스 목록 (`/enemies`) — 고유 버프/디버프, 취약성

**산출물**: 읽기 전용 데이터 브라우저 완성

---

## Phase 4 — 추천 시스템 (규칙 기반)

**목표**: 계산 엔진 없이 휴리스틱으로 추천 제공

- [ ] 영웅별 템 추천 규칙 정의
  - 물리 딜러 → 물공% 세트, 치명/약점 서브옵
  - 마법 딜러 → 마공% 세트, 약점 서브옵
  - 힐러 → 지속효과 보너스
  - 탱커 → HP/방어 계열
- [ ] 던전(보스)별 영웅 추천 규칙
  - 보스 취약 속성 매칭
  - 보스 내성 스킬 회피
  - 기믹 대응 (잃은HP 보정 / 카운터 / 해제 등)
- [ ] 추천 결과 UI — 이유 설명 + 대안 표시

**산출물**: `/characters/[id]/build` (템 추천), `/enemies/[id]/party` (영웅 추천)

---

## Phase 5 — 계산 엔진 TS 포팅 (Full 단계 진입)

**목표**: `DamageCalculator` / `StatCalculator` / `EffectManager` 를 TS 로 재구현

- [ ] `StatCalculator.ts` — 3단계 버프 적용 (기본/별도/승수)
- [ ] `EffectManager.ts` — 효과 집계 (상시/턴제/펫 분리)
- [ ] `DamageCalculator.ts` — 19단계 파이프라인
  - 방어계수, 치명, 약점, 피증, 취약
  - 스택소모, 잃은HP 비례, HP비례, 블록/축복
- [ ] 단위 테스트 (기존 WPF 테스트 결과와 오차 비교)
  - 에스파다 0.003%, 루리 0.9%, 타카 1.6%, 라이언 광풍참 ~0%

**산출물**: `web/lib/calc/` 모듈, 테스트 스위트

**리스크**: C# `decimal` vs JS `number` 정밀도 차이 — 고정소수점 라이브러리 검토

---

## Phase 6 — 시뮬레이터 / 옵티마이저 포팅

**목표**: 배틀 시뮬 + 장비 최적화 기능 제공

- [ ] `BattleSimulator.ts` — 턴 루프, 행동 큐, 쿨다운
- [ ] `TurnManager.ts` — 선공 결정, 기본공격 순서
- [ ] `EquipmentOptimizer.ts` — 3단계 탐색 (세트 → 메인옵 → 서브옵)
- [ ] UI — 파티 빌더, 보스 선택, 시뮬/최적화 실행
- [ ] Web Worker 사용 검토 (옵티마이저 계산 중 UI 블로킹 방지)

**산출물**: `/simulator`, `/optimizer`

**리스크**: 옵티마이저 탐색 공간 크기 — 브라우저 성능 한계 도달 시 서버 함수 고려

---

## Phase 7 — 배포 안정화 & 공개

**목표**: 실사용 가능한 수준

- [ ] Vercel 프로덕션 배포 + 커스텀 도메인
- [ ] SEO 메타 태그, OG 이미지
- [ ] analytics (Vercel Analytics 또는 GA)
- [ ] 에러 모니터링 (Sentry 옵션)
- [ ] README, 사용 가이드
- [ ] 데이터 갱신 정책 — 패치 시 WPF DB 수정 → JSON 재export → 배포

**산출물**: 공개 URL + 운영 문서

---

## 진행 방식

- **계층**: MVP (Phase 0~4) → Full (Phase 5~7)
- **우선순위**: Phase 0 → 1 → 2 → 3 병렬 가능
- **역할 분담 (coord sub 활용 시)**:
  - `db` sub → Phase 1 (export 스크립트)
  - `frontend` sub → Phase 2, 3, 4 (Next.js UI)
  - `backend` sub → Phase 5, 6 (계산 엔진 TS 포팅)

## 미결정 사항 (진행 중 확정)

- [ ] 웹 앱 디렉토리 위치 (`web/` vs 별도 repo)
- [ ] UI 라이브러리 (shadcn/ui vs Mantine vs 직접 구성)
- [ ] 계산 엔진 포팅 시점 (MVP 완성 후 vs 병행)
- [ ] 사용자 기능 도입 시점 (프리셋 저장 — Supabase 재검토 타이밍)

---
name: project-web-nextjs
description: 웹 공략 사이트(BeHumble)는 web/ 폴더의 Next.js 16 프로젝트. Turbopack 한글경로 버그로 webpack 빌드 고정. 영웅 데이터는 C# export 도구로 생성.
metadata:
  type: project
---

세븐나이츠 리버스 공략 사이트(브랜드명 BeHumble)를 `web/` 폴더에 Next.js 16 + React 19 + Tailwind v4(App Router, src-dir, TS)로 구현. 2026-05-20 시작.

**구조:**
- `web/src/app/layout.tsx` — 루트 레이아웃 (사이드바 + 헤더 공통). `@/*` → `./src/*` alias 사용
- `web/src/components/Sidebar.tsx` ('use client', usePathname로 active), `Header.tsx`
- `web/src/app/page.tsx` — 메인 환영 카드, `web/src/app/heroes/page.tsx` — 영웅 페이지(필터+리스트+상세, 'use client')
- `web/src/data/characters.json` — 영웅 66명 (C# 추출 도구 산출물, 직접 편집 금지)
- `web/src/app/globals.css` — BeHumble 브랜드 토큰(navy #1f2a44 + bronze #b08968) + 와이어프레임 이식 CSS

**데이터 파이프라인:** `tools/SenaDataExport/`(net8.0 콘솔)가 DB/Models 소스를 직접 Compile Include(WPF 의존성 없음)하여 `CharacterDb.Characters`를 characters.json으로 직렬화. 영웅별 baseStats·transcend6/12·스킬 tiers(기본/강화/초월의 쿨타임·대상수·공격횟수·배율)·효과 태그를 산출. 데이터 갱신 시 `cd tools/SenaDataExport && dotnet run` 재실행. 루트 `dev.cmd` 더블클릭 시 web 의존성 설치 후 dev 서버 구동.

**중요 — Turbopack 한글경로 버그:** 프로젝트 경로에 한글(`김광동/개인`)이 있어 Turbopack이 char-boundary 패닉으로 빌드 실패. **webpack으로 고정**해야 함 — package.json scripts가 `next dev --webpack` / `next build --webpack`. dev 서버는 localhost:3000(점유 시 3001).

**WPF 클린 빌드 (해결됨):** pull로 들어온 `tools/`·`web/`의 .cs가 WPF 컴파일에 섞여 wpftmp AssemblyInfo 중복으로 클린 빌드가 깨졌으나, `Sena_Calculator.csproj`에 `<Compile Remove="tools/**/*.cs" />`·`<Compile Remove="web/**/*.cs" />`를 추가해 해결. 이제 `dotnet build Sena_Calculator.csproj` 클린 빌드 정상.

**스킬 티어별 선언 (2026-05-21):** `SkillLevelData`에 TargetCount/AtkCount/Cooldown, `SkillTranscend`에 TargetCountOverride/AtkCountOverride/Cooldown. `Skill.GetTargetCount/GetAtkCount/GetCooldown(isEnhanced, transcendLevel)` 헬퍼(초월 override > 레벨별 > Skill 기본값). 웹 스킬 툴팁의 기본/강화/초월 3단 표와 정합.

**Why:** 사용자가 와이어프레임(메인+영웅 페이지) 제공, Next.js + 실제 CharacterDB 연동 + repo 내 web/ 폴더로 결정.

**How to apply:**
- web/에 작업 시 반드시 webpack 사용 (Turbopack 금지 — 한글경로 패닉)
- web/CLAUDE.md(→AGENTS.md)가 "Next.js 16은 breaking changes 있으니 node_modules/next/dist/docs/ 먼저 읽어라" 지시 — 준수
- 영웅 페이지 필터는 현재 등급/역할군/공격타입만. 와이어프레임의 버프/효과 칩(물공증/마공증 등)과 추천 세팅(무기/방어구 세트)은 공략 데이터 미입력 상태(골격만) — 추후 데이터 소스 필요
- 관련: [[project-web-migration-priority]], [[project-atk-type-split]] (영웅 attackType이 페이지에 표시됨)

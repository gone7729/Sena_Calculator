---
name: project-web-migration-priority
description: 현재 웹 서비스 마이그레이션 준비 중이므로 검수·리팩토링·버그 분류 시 DB와 Services 계층이 최우선이고 XAML/UI 계층은 우선순위 낮음
metadata:
  type: project
---

WPF UI(XAML/MainWindow/SimulatorWindow)는 곧 웹페이지로 이관 예정 — 검수/리팩토링 우선순위에서 후순위. DB 계층(`DB/*.cs`)과 계산 로직(`Services/*.cs`, 특히 `DamageCalculator.cs`·`StatCalculator.cs`·`EffectManager.cs`)이 웹 이관에서도 그대로 재사용되므로 데이터·계산 정합성 이슈가 모든 다른 이슈보다 우선.

**Why:** 사용자가 2026-05-19에 "현재 웹페이지에서 서비스를 준비 중이라 XAML 쪽 코드는 중복이나 사소한 에러 있어도 괜찮고 DB쪽 데이터와 계산쪽 데이터만 무사하면 된다"고 명시.

**How to apply:**
- 검수 보고 시 이슈를 (DB/계산 / UI) 카테고리로 분리, UI 단독 이슈는 Low 또는 생략
- UI 코드(MainWindow.xaml(.cs), SimulatorWindow.xaml(.cs), UI/Converters.cs)에서만 발견된 중복·미사용·스타일 이슈는 무시
- DB·Services 변경 시 UI 컴파일이 깨지면 최소한의 수정만 (기능 보장 수준)
- 향후 [[project-web-frontend-status]] 같은 메모리가 생기면 거기서 진척도 추적

# 프로젝트 컨텍스트 (최종 갱신: 2026-05-10)

> 대화 기록: `.claude/log/YYYY-MM-DD.md`

## 프로젝트 개요
- **이름**: 세나리 데미지 계산기 (세븐나이츠 리버스)
- **현재 상태**: WPF 데스크톱 → 웹(Next.js + TS, Vercel) 서비스로 이전 결정 (2026-05-10)
- **현 단계**: WPF exe 빌드 중단. 핵심 로직(Services/, Models/, DB/)을 정합성 확보 후 TS로 포팅 예정

## 게임 룰 — 데미지 계산 정확도의 근간

### 버프/디버프 합산 (β 해석 확정 2026-05-10, 4카테고리 확정 2026-06-08)
- **4카테고리 — 상시 / 턴제 / 펫 / 진형** (펫·진형은 별개 채널. 진형%는 StatCalculator 2단계 별도보너스로 Add).
- 카테고리 내: 같은 BuffSet/DebuffSet 필드끼리 **MaxMerge** (가장 높은 값만). **디버프도 버프와 동일.**
- 카테고리 간: **Add** (합산). 상시·턴제는 동시 적용.
- "같은 종류"는 BuffSet 필드 단위. 게임 내부 분류명이 다르면 별도 필드(예: Mark_Purify, Mark_Energeia)로 분리되어 있어 자동으로 Add 처리됨.

### 턴제 효과 차감 시점 (2026-05-10 추가, **다음 라운드 적용 예정**)
- 모든 턴제 디/버프 + 상태이상의 턴 차감은 **이펙트 보유자가 평타할 때 발생**.
- 상태이상: 평타 **전** 효과 적용 + 턴 차감.
- 디/버프: 평타 **후** 턴 차감.

### 트루드 PainEndurance 룰
- 직접 피해 ≥ 최대 HP 10% 시 발동.
- 25% 즉시 + 75% 5턴 분산 (15%/턴).
- 발동 누적 가능 (각자 독립 큐).
- 분산은 트리거된 평타 직후가 아니라 **다음 본인 평타 차례 시작 시** 1회 처리 후 평타.

## 핵심 공식
```
BaseDamage = (공/방계수) × 스킬배율 × 치명계수 × 약점계수 × 피증계수
방어계수 = 1 + (보스방어 × (1+방증-방깎) × (1-방무)) / 467
피증계수 = 1 + (기본+타입+조건부+보스+인기-피감) / 100  [합연산]
최종피해 = (스킬피해 + HP비례 + 스택소모) × (1 + 취약/100)
버프 적용 = 1 + (상시 + 턴제 + 펫 + 진형) / 100  [4카테고리 합연산]
```

## 주요 파일
| 파일 | 설명 |
|------|------|
| `Services/DamageCalculator.cs` | 데미지 계산 핵심 (보스→아군에도 재사용) |
| `Services/StatCalculator.cs` | 스탯 계산 (FlatBonus 적용 추가됨) |
| `Services/EffectManager.cs` | 통합 효과 집계 (BuffCalculator 대체, 룰 일치) |
| `Services/EffectConverter.cs` | Skill/Passive → BattleEffect 변환 |
| `Services/BattleEngine/BattleSimulator.cs` | 시뮬 메인 + 받피해 흐름 |
| `Services/BattleEngine/BattleState.cs` | PainEnduranceQueue 보유 |
| `Services/Optimizer/EquipmentOptimizer.cs` | 장비 최적화 |
| `DB/CharacterDB.cs` | 캐릭터/스킬 데이터 (사용자가 보스 스킬 DB 추가 예정) |

## 코드 구조 변화 (2026-05-10 세션)
- **BuffCalculator 완전 제거** (-233 LOC). EffectManager가 단일 집계기.
- **EffectManager 집계 의미** 게임 룰과 일치하도록 수정 (카테고리 묶음 통합 MaxMerge).
- **StatusEffect 분리** — enum은 Models/StatusEffectType.cs, class+DB는 DB/StatusEffect.cs (namespace = Database).
- **SubOptionDb 분리** — Models/Equipment.cs에서 DB/SubOptionDb.cs로.
- **FlatBonus 소비 코드 추가** — 밀리아 「광채의 수정비늘」 정상 적용.
- **PainEndurance 인프라** — BattleSimulator에 `ApplyIncomingDamage` / `TickPainEnduranceQueue` / `CalculateIncomingDamage` / `ResolveEnemySkill` 추가.

## 다음 세션 진행 후보 (우선순위)
- [ ] **A. 턴제 효과 차감 시점 룰** — EffectManager에 시전자 필터 + 카테고리별 TickTurn. 광범위 변경. 다른 룰의 기반.
- [ ] **B. 아군 사망 + 부활** — HP 0 = 행동 불능. 부활 스킬 데이터 모델링 (SkillEffectType.Revive 등).
- [ ] **C. 상태이상 부여 흐름 + 보스 면역 가드** — 보스 = CC 면역 + 즉사 면역. 한 줄 가드.
- [ ] **D. Stage → 다중 페이즈 + PartyConfig 분리** — Stage.Phases / BossPhase / PartyConfig 신설.
- [ ] **E. 보스 기본공격 신설 + 스킬 로테이션 인덱스 관리** — 사용자가 보스 스킬 DB 추가 후 진행.
- [ ] **F. 옵티마이저 풀 시뮬 호출 연결** — D + E 완료 후. 페이즈별 파티 최적화.

## 미확인 사항 (인게임 검증 필요)
- 다중 캐릭터에서 같은 속성을 조건부 패시브 + 액티브 스킬로 동시에 부여하는 빌드의 실제 데미지 (단일 카테고리 MaxMerge 가정 검증)
- PainEndurance 분산 시작 타이밍 — 사용자 진술 그대로 "다음 본인 평타 차례"로 구현됨, 인게임 검증 필요

## 테스트 결과 (이전)
| 캐릭터 | 오차 | 상태 |
|--------|------|------|
| 에스파다 | 0.003% | ✅ |
| 루리 (약점) | 0.9% | ✅ |
| 타카 | 1.6% | ✅ |
| 라이언 광풍참 | ~0% | ✅ |

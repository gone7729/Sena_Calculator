# 세나리 데미지 계산기 (Sena Calculator)

세븐나이츠 리버스 게임의 데미지 계산기 + 배틀 시뮬레이터 WPF 애플리케이션

> **시스템 단일 기준 문서: [SYSTEM.md](SYSTEM.md)** — 데미지 계산식·스탯/버프·공성전 규칙·옵티마이저 등
> 게임 시스템 서술은 SYSTEM.md가 최신 기준이다. 아래 요약과 충돌하면 SYSTEM.md(와 코드)를 따른다.

## 개발 환경

- .NET 8.0 (Windows), WPF, C# 12
- 빌드: `dotnet build` / `dotnet run`

---

## 프로젝트 구조

```


---

## 데미지 계산 공식 (DamageCalculator.cs)

### 전체 파이프라인

```
1. PreCast 버프 적용 (스킬 발동 전 공격력 보정)
2. 스킬 배율 = Ratio / 100
3. 방어관통 = min(입력방무 + 스킬방무, 100%)
4. 방어계수 = 1 + (적방어 × 방어수정 × 관통수정) / 467
5. 치명계수 = 치명 시 치피%/100, 아니면 1.0
6. 약점계수 = 약점 시 약피%/100, 아니면 1.0
7. 피증계수 = 1 + (기본피증 + 타입피증 + 조건부 + 보스피증 + n인기 - 피감) / 100
8. 잃은HP 보너스 적용 (조건 충족 시)
9. 기본피해 = (공/방계수 × 배율) × 치명 × 약점 × 피증 + 고정피해
10. 조건부 추가피해
11. 약점 추가피해 (WekBonusDmg)
12. 치명 추가피해 (CriBonusDmg)
13. HP비례 피해 = 대상최대HP × 비율% (공격력 상한 적용)
14. 스택소모 추가피해 (ConsumeExtra)
15. 블록 적용 (50% 감소)
16. 축복 적용 (1회 피해 상한)
17. 1타 피해 = 기본 + 조건부 + 약점추가 + 치명추가
18. 상태이상 피해 (화상/출혈/폭탄 등)
19. 최종피해 = (스킬피해 + HP비례 + 스택소모) × 취약계수
```

### 핵심 공식

```
방어계수 = 1 + (적방어력 × (1 + 방증% - 방깎%) × (1 - 방무%)) / 467

피증계수 = 1 + (기본피증 + 타입피증 + 조건부 + 보스피증 + n인기피증 - 피감) / 100

취약계수 = 1 + (취약% + 보스취약% + 받피증%) / 100

최종피해 = (1타피해 × 타수 + HP비례 + 스택소모) × 취약계수
```

### 특수 케이스

| 케이스 | 설명 |
|--------|------|
| 스택소모 스킬 | 스킬피해는 SkillDmgMultiplier(타입피증 제외), 스택소모는 DamageMultiplier(타입피증 포함, **잃은HP 보너스 제외**) |
| 잃은HP 비례 | `LostHpBonusDmgMax × (100 - 잔여HP%) / 100`으로 피증계수에 곱연산 (스택소모 피해에는 미적용) |
| 보스피증 | `IsTargetBoss = true`일 때만 DmgDealtBoss 적용 |
| PreCastBuff | 스킬 발동 전 임시 공격력 보정 (스킬 계산에만 적용) |

---

## 스탯 계산 (StatCalculator.cs)

### 계산 순서

```
1. 캐릭터 기본 스탯 (등급/타입별)
2. 초월 보너스 (레벨 1~12)
3. 장비 기본 + 메인옵 + 서브옵
4. 장신구 (등급 보너스 + 메인/서브)
5. 세트 효과 (4세트 또는 2+2세트)
6. 펫 기본 스탯
7. 잠재능력 (3단계)
8. 진형 보너스 (전/후열)
```

### 버프 적용 (3단계 곱연산)

```
1단계: 기본스탯 × (1 + 스탯창%합) + 고정스탯
2단계: + 별도보너스 (진형%, 펫옵션%)
3단계: × (1 + (상시버프% + 턴제버프% + 펫버프%) / 100)
```

### 버프/디버프 병합 규칙 (버프·디버프 동일)

| 카테고리 | 카테고리 내 (같은 종류) | 카테고리 간 |
|----------|-----------|-----------|
| 상시 패시브 | MaxMerge (최대값) | Add (합산) |
| 턴제/조건부 | MaxMerge (최대값) | Add (합산) |
| 펫 버프 | - | 항상 Add |
| 진형 보너스 | - | 항상 Add (별도 채널) |

- **4카테고리 — 상시 / 턴제 / 펫 / 진형** (펫·진형은 별개). 상시·턴제는 동시에 적용되며 묶음 간 Add. 같은 묶음 안에서 같은 종류(같은 BuffSet/DebuffSet 필드)는 가장 높은 값만 남긴다(MaxMerge).
- **디버프도 버프와 동일 규칙** (예: 상시 방깎20 + 턴제 방깎36 = 56[간 Add], 단 턴제 방깎29 + 턴제 방깎36 = 36[내 MaxMerge]).

---

## 효과 시스템 (이중 구조)

### 레거시 (기존 캐릭터)
```csharp
// Skill에 별도 필드
Bonus = new BuffSet { ... },
SelfBuff = new TimedBuff { ... },
PartyBuff = new TimedBuff { ... },
DebuffEffect = new TimedDebuff { ... },
StatusEffects = new List<SkillStatusEffect> { ... }

// Passive에 별도 필드
SelfBuff = new PermanentBuff { ... },
PartyBuff = new PermanentBuff { ... },
Debuff = new PermanentDebuff { ... },
ConditionalSelfBuff = new TimedBuff { ... },
```

### 신규 (통합 리스트)
```csharp
// Skill - 턴제 효과 통합
Effects = new List<SkillEffect>
{
    new() { Target = Enemy, Type = StatusAilment, StatusType = Bleeding, Chance = 55 },
    new() { Target = Party, Type = Buff, Buff = new BuffSet { Arm_Pen = 10 } },
    new() { Target = Enemy, Type = Debuff, Debuff = new DebuffSet { Def_Reduction = 29 } },
}

// Passive - 지속 효과 통합
Effects = new List<PersistentEffect>
{
    new() { Target = Party, Type = Buff, Buff = new BuffSet { Atk_Rate = 31 } },
    new() { Target = Self, Type = Buff, IsConditional = true, Buff = new BuffSet { Wek_Dmg = 28 } },
    new() { Target = Enemy, Type = MarkAttack, MarkAttack = new MarkAttack { ... } },
}
```

### 호환성
- 새 Effects가 있으면 우선 사용, 없으면 레거시 폴백
- Passive.GetPartyBuff() 등 레거시 메서드가 Effects도 자동 확인
- Skill.StatusEffects 프로퍼티가 Effects에서 자동 변환
- Bonus (스킬 종속 스탯)는 별도 유지 (효과 리스트에 포함하지 않음)

### EffectManager (EffectManager.cs)
- BuffCalculator를 대체하는 통합 효과 집계
- `GetSeparatedBuffs()` → (상시, 턴제, 펫) 분리 반환
- `GetTotalDebuffs()` → 디버프 합산
- `GetActiveStatusEffects()` → 활성 상태이상
- `TickTurn()` → 턴 경과 (만료 효과 자동 제거)

### EffectConverter (EffectConverter.cs)
- `FromPassive()` / `FromSkill()` / `FromPet()` → BattleEffect 변환
- `FromBuffConfigs()` → UI BuffConfig → BattleEffect 변환
- 새/레거시 양쪽 모두 지원

---

## 배틀 시뮬레이터

### 턴 구조
```
배틀 시작(0턴) → 선공 스킬 (턴 소모 없음)
→ 후공 기본공격 2회 (1~2턴) + 후공 스킬
→ 선공 기본공격 2회 (3~4턴) + 선공 스킬
→ 반복...
```

- 선공 결정: 아군 총 속공 합 vs 적 속공
- 기본공격 순서: 파티 내 속공 높은 순 순환
- 스킬 로테이션: 유저 지정 / 자동 (궁극기→스킬4→3→2→1)
- 쿨다운: 초 기반, 상대 스킬 사용 시 5초 감소 (5초 이하 잔여 시 미적용)

### 장비 옵티마이저

**3단계 탐색:**
1. 세트 조합 (4세트 9개 + 2+2세트 72개 = 81개)
2. 메인옵션 (DPS 필터 적용, 무기/방어구 각 5종)
3. 서브옵션 (그리디: 1티어당 데미지 증가량 최대 옵션에 배분)
4. 장신구 (등급×메인×서브 순회)

---

## Enemy (적) 시스템


---

## 상태이상 시스템 (StatusEffect.cs)



---

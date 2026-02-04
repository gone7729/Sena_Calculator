# 세나리 데미지 계산기 (Sena Calculator)

> **세션 컨텍스트**: [.claude/CLAUDE_MEMORY.md](.claude/CLAUDE_MEMORY.md) 참조
> **대화 기록**: [.claude/log/](.claude/log/) (날짜별 저장)

## ⚠️ Claude 작업 지시

### 세션 시작 시
1. `git pull` 실행하여 최신 작업 상황 동기화 (이미 clone된 프로젝트면 pull만)
2. `.claude/CLAUDE_MEMORY.md` 읽어서 이전 컨텍스트 파악

### 대화 중
3. 모든 Q&A를 `.claude/log/YYYY-MM-DD.md`에 기록 (전체 내용, 요약 금지)
   - `.claude/log/` 폴더 내 파일 생성/수정은 **확인 없이 자동 진행**
   - 로그 파일 Edit 현황은 사용자에게 **표시하지 않음**

### 세션 종료 시 (사용자가 "작업 정리" 요청 시)
3. 당일 로그 파일 끝에 `## 📋 오늘 작업 요약` 추가
4. `.claude/CLAUDE_MEMORY.md` 갱신 (최근 작업, 진행중 작업 업데이트)
5. Git commit & push 실행:
   ```bash
   git add .claude/CLAUDE_MEMORY.md .claude/log/
   git commit -m "docs: 작업 기록 업데이트 (YYYY-MM-DD)"
   git push
   ```

---

세븐나이츠 리버스 게임의 데미지 계산기 WPF 애플리케이션

## 프로젝트 구조

```
Sena_Calculator/
├── DB/                      # 게임 데이터베이스
│   ├── CharacterDB.cs       # 캐릭터/스킬 정보
│   ├── BossDB.cs           # 보스/몹 정보
│   ├── PetDB.cs            # 펫 정보
│   ├── EquipmentDb.cs      # 장비 세트 효과
│   ├── StatusEffectDb.cs   # 상태이상 정보
│   └── SubOptionDb.cs      # 장비 부옵션 수치
├── Models/                  # 데이터 모델
│   ├── Character.cs        # 캐릭터, 스킬, 패시브
│   ├── Boss.cs             # 보스/몹 모델
│   ├── BuffSet.cs          # 버프/디버프 세트
│   ├── Equipment.cs        # 장비 모델
│   └── Preset.cs           # 프리셋 저장 모델
├── Services/                # 계산 로직
│   ├── DamageCalculator.cs # 데미지 계산 핵심
│   ├── StatCalculator.cs   # 스탯 계산
│   └── BuffCalculator.cs   # 버프/디버프 합산
└── UI/                      # WPF UI
    ├── MainWindow.xaml      # 메인 UI
    └── MainWindow.xaml.cs   # UI 코드비하인드
```

## 핵심 데미지 공식

### 1. 기본 데미지
```
BaseDamage = (공격력 / 방어계수) × 스킬배율 × 치명계수 × 약점계수 × 피증계수 + 고정피해
```

### 2. 방어 계수
```
DEF_CONSTANT = 467
실효방어력 = 보스방어력 × (1 + 방증% - 방깎%) × (1 - 방무%)
방어계수 = 1 + 실효방어력 / 467
```

### 3. 치명타 계수
```
치명계수 = 치명타 발동 시: 치피% / 100
           미발동 시: 1.0
```

### 4. 약점 계수
```
약점계수 = 약점 발동 시: 약피% / 100  (예: 130% → 1.30x)
           미발동 시: 1.0
```

### 5. 피해 증가 계수 (합연산)
```
피증계수 = 1 + (기본피증 + 타입피증 + 조건부피증 + 보스피증 + n인기피증 - 피감) / 100
```

### 6. 취약/받피증 (별도 곱연산)
```
취약계수 = 1 + (취약% + 보스취약% + 받피증%) / 100
```

### 7. 최종 데미지
```
1타 피해 = 기본피해 + 조건부추가 + 약점추가 + 치명추가
스킬피해 = 1타 피해 × 타수
최종피해 = (스킬피해 + HP비례피해 + 스택소모피해) × 취약계수
```

## 특수 계산 케이스

### 스택소모 스킬 (타카 등)
- 스킬피해: 자버프 타입피증 **제외**
- 스택소모피해: 자버프 타입피증 **포함**
- `SkillDmgMultiplier` vs `DamageMultiplier` 분리 사용

### 조건부 추가피해
- 방어계수 적용됨 (방어무시 아님)
- n인기피증 적용 여부: **테스트 필요** (현재 적용됨)

### 약점/치명 추가피해
- 일부 캐릭터 스킬에 있는 별도 보너스
- 예: 루리 - 약점공격시 155% 추가피해

## 버프/디버프 시스템

### BuffSet (버프)
- `Atk_Rate`: 공격력%
- `Def_Rate`: 방어력%
- `Dmg_Dealt`: 피해증가%
- `Dmg_Dealt_Type`: 타입피해증가%
- `Dmg_Dealt_Bos`: 보스피해증가%
- `Cri_Dmg`: 치명타피해%
- `Wek_Dmg`: 약점피해%
- `Arm_Pen`: 방어관통%

### DebuffSet (디버프)
- `Def_Reduction`: 방어력감소%
- `Dmg_Taken_Increase`: 받는피해증가%
- `Vulnerability`: 취약%
- `Boss_Vulnerability`: 보스취약%

## UI 기능

### 빠른 비교 (Quick Compare)
- 치명/약점 체크박스 제거
- 4가지 시나리오 자동 계산:
  - 치명+약점
  - 치명만
  - 약점만
  - 일반
- 최대 데미지 기준 상세 표시

### 프리셋 시스템
- 캐릭터/스킬/장비/버프 설정 저장
- JSON 파일로 저장 (`presets.json`)

## 테스트 결과

### 검증된 케이스
- 타카: 1.6% 오차 ✅
- 루리 (약점): 0.9% 오차 ✅

### 알려진 이슈
- 파스칼/루리 등 타입피증 있는 캐릭터에서 오차 발생
- 피증 계산 방식 추가 검증 필요

## 개발 환경

- .NET 8.0 (Windows)
- WPF (Windows Presentation Foundation)
- C# 12

## 빌드

```bash
cd Sena_Calculator
dotnet build
dotnet run
```

## 주요 파일 설명

| 파일 | 설명 |
|------|------|
| `DamageCalculator.cs` | 데미지 계산 핵심 로직 |
| `CharacterDB.cs` | 캐릭터/스킬 데이터 정의 |
| `MainWindow.xaml.cs` | UI 이벤트 핸들러, 계산 호출 |
| `BuffCalculator.cs` | 파티 버프/디버프 합산 |
| `StatCalculator.cs` | 최종 스탯 계산 |

## 미확인 사항

1. **조건부 추가피해 + n인기피증**
   - 현재: SkillDmgMultiplier 사용 (n인기 포함)
   - ExtraDmgMultiplier 계산됨 (n인기 제외) 하지만 미사용
   - 게임 내 테스트로 확인 필요

2. **피증 합연산 vs 곱연산**
   - 현재: 합연산 적용
   - 이전 테스트에서 곱연산 시도했으나 오차 발생

# 후속 작업 정리 (크롤 111명 DB 등재 이후)

크롤링 데이터로 전 캐릭터(전설/희귀 111명)를 DB에 등재하면서, **모델/데이터까지만 구조화하고 런타임·표현을 미룬 항목**들을 모았다. 우선순위 순 정리.

---

## A. 런타임 동작 미구현 (모델·데이터는 존재, 동작 추후)

배틀 시뮬레이터에서 아직 소비하지 않는 효과들. 모델 필드/데이터는 채워져 있음.

- **부활/불사/불굴** `Revival` — 사망 감지 → 부활(HP/HP%), 피격 N회 또는 N턴 무적 소비, 적군 사망 시 잔여 피격 +1, 전투당 1회
- **권능** `Authority` — 현재 HP 이상 피해 시 HP 1로 1회 생존 + 발동 시 보호막
- **피해 무효화** `DamageNullification` — 피격 N회 / N턴 / 물·마 한정 소비
- **버프 해제** `BuffDispel`(+`DispelBuffCount`) — 적용순 해제, 상시·면역·무효화·권능 제외 (`BattleEffect.ApplyOrderId`로 순서 추적)
- **디버프 해제** `DebuffCleanse`(+`DispelDebuffCount`) — 아군 디버프 N개 제거
- **트리거류** — `TriggeredFixedDamage`, `TriggeredHeal`(`TriggeredHealAtkRatio`), `TriggeredSkillCast`, `OnKillRecast`(처치 시 재시전), `CooldownReset`(부활/적사망 시 쿨초기화)
- **트리거 조건** — `OnHpBelow`(+`TriggerHpThreshold`), `EnemyDeath`, `AllyDeath`, `SelfDeath`, `OnRevival`
- **강자주시** `FocusTarget`(`FocusTargetSelector`)
- **위장** `StatusEffectType.Disguise`(1인 공격 비대상), **관통** `IgnoresTurnDamageImmunity`(피해면역[N턴] 무시)
- **직업군 타겟 필터** `PersistentEffect.TargetClasses` — 런타임에서 공격형/만능형 등 직업군만 버프 적용
- **PainEndurance**(트루드) 받피해 분산 — 연결 상태 점검 필요
- **턴제 버프 감소** `BuffTurnReduction`(`TurnReduction`)

## B. 모델 부재 → Effect 텍스트로만 남긴 메카닉 (모델 확장 후보)

해당하는 모델 필드가 없어 `Effect = "..."` 설명으로만 보존. 필요 시 모델 추가.

- **흡혈/피해량 비례 자힐** — 다수 (HealDmgRatio는 일부 스킬만; 패시브/트리거 자힐 채널 부재)
- **방어력(DEF) 비례 회복/보호막** — 챈슬러·겔리두스·아라곤·루시·라쿤·에반·유진호·라드그리드 (`Heal*`·`Shield_*`는 HP/ATK 비례만)
- **열(전열/후열) 한정 타겟** — 후열 우선 1명/후열 한정 버프 (지크·관우·룩·빅토리아·여포 6초월 등)
- **특수 아군 타겟 선정** — "공격력 가장 높은 아군 N명", "버프 많은 순", "생명력 낮은 아군" (비스킷·노호·오를리·오목·루디 등; `EffectTarget`엔 `SelfAndHighestAtkAlly`만)
- **스택 누적 동적 피증** — 사냥술(스니퍼)·레벨업(성진우)·신성(프레이야)·7첩 반상(돼오) 등 처치/시간 비례
- **쿨타임 증가 디버프(쿨증)** — 바네사·니아
- **적 약점확률/치명확률 감소 디버프** — 다수 (`DebuffSet`에 약확/치확 감소 없음)
- **상태이상 해제 시 폭발 피해** — 빙결/석화 해제 시 추가피해 (헤브니아·바네사·라니아)
- **지속 회복(per-turn regen)** — 리나·녹스
- **피해 대상 수 감소 시 피증** — 여포·손오공·제이브 평타 ("1명 줄 때마다 +N%")
- **현재 HP 비례 피증** — 오목 (잃은 HP `LostHpBonusDmgMax`와 별개)
- **약점확률 StatScaling** — 미스트 (`StatType`에 Wek 타겟 없음)
- **집중 공격**(도발과 별개) — 아수라
- **마법 공격력 감소 디버프** — 현재 `Atk_Reduction`로 근사 (플라튼·유이)

## C. 데이터 정합성 후속

- **상태이상 초월 확률 상승 중복** — 초월 `SkillEffect` 상태이상은 base에 append되어, "기절 55%→65%" 류가 base+초월 두 번 적용될 수 있음. 런타임에서 동일 상태이상 dedup(최댓값) 처리 또는 데이터 표현 정리 필요 (니아·크리스·스파이크·아일린 등 다수, 현재 초월분은 Effect 텍스트로만 표기한 케이스 다수)
- **희귀 등급 초월 보너스 수치** — `TranscendDb.Rare`가 실제 희귀 % 인지 확인 (현재 구 영웅 값 사용)
- **칼 헤론** 등 표기에 공백/특수문자 있는 이름 처리 점검

## D. 웹

- **패시브 buffTiers 표시 UI** — export에 `passive.buffTiers`(기본/강화/초월 비0 버프) 추가됨, 화면 연동 미완
- **희귀 등급 필터** — 연결됨(GRADES). 동작 확인
- 캐릭터 이미지, 스킬 effect 툴팁 다듬기

---

## 진행 원칙 (이번 세션 합의)

- 효과는 **모델·데이터로 먼저 구조화**, 런타임은 추후 (팀 패턴)
- 초월 버프/Bonus는 **필드별 override**(겹치는 필드만 초월값으로, 안 겹치면 유지)
- 스탯은 BasicStatDB가 등급(전설/희귀)×타입으로 조회 — 캐릭터 선언엔 스탯 미포함
- 검증: WPF 풀빌드 대신 `dotnet run`(tools/SenaDataExport)로 컴파일+JSON 재생성 겸용

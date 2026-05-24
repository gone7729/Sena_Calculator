using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 턴제 배틀 시뮬레이터
    /// DamageCalculator, StatCalculator, EffectManager를 활용하여
    /// 5인 파티 vs 보스 배틀을 시뮬레이션
    /// </summary>
    public class BattleSimulator
    {
        private readonly DamageCalculator _damageCalc = new();
        private readonly StatCalculator _statCalc = new();

        /// <summary>
        /// 배틀 시뮬레이션 실행
        /// </summary>
        public BattleResult Simulate(BattleConfig config)
        {
            // 1. 초기 상태 설정
            var state = InitializeBattleState(config);

            // 2. 턴 매니저 생성
            var turnManager = new TurnManager(config, state.AllyStates);
            state.AllySpeedOrder = turnManager.AllySpeedOrder.ToList(); // 평타 로테이션 기준

            // 3. 행동 큐 생성
            var actionQueue = turnManager.GenerateActionQueue(config.MaxTurns);

            // 4. 스킬 로테이션 준비
            var rotations = PrepareRotations(config, state);

            // 5. 배틀 루프
            foreach (var action in actionQueue)
            {
                state.CurrentTurn = action.Turn;

                if (action.IsAlly)
                {
                    if (action.IsSkill)
                    {
                        ExecuteAllySkill(config, state, rotations);
                    }
                    else
                    {
                        // 평타 캐릭터를 살아있는 영웅 중 다음 속공 순번으로 결정 (사망 영웅은 제외되고 다음 영웅이 그 턴을 가져감)
                        int idx = ResolveNextAliveAlly(state);
                        if (idx >= 0)
                            ExecuteAllyNormalAttack(config, state, idx);
                    }
                }
                else
                {
                    if (action.IsSkill)
                    {
                        ExecuteEnemySkill(config, state);
                    }
                    else
                    {
                        // 보스 기본공격 - 아군에 데미지 (현재는 스킵, 데미지 계산 집중)
                    }
                }

                // 턴 종료 처리: 버프/디버프 지속시간 감소
                ProcessTurnEnd(state, action);

                // 보스 HP 체크
                if (state.EnemyState.CurrentHp <= 0)
                    break;
            }

            // 6. 결과 생성
            return BuildResult(state, config);
        }

        #region 초기화

        /// <summary>
        /// 배틀 초기 상태 설정
        /// </summary>
        private BattleState InitializeBattleState(BattleConfig config)
        {
            var state = new BattleState();

            // 아군 캐릭터별 스탯 계산
            for (int i = 0; i < config.AllyParty.Count; i++)
            {
                var battleChar = config.AllyParty[i];
                var charState = InitializeCharacterState(config, battleChar, i);
                state.AllyStates.Add(charState);
            }

            // 보스 상태
            var enemy = config.TargetEnemy;
            state.EnemyState = new EnemyBattleState
            {
                Enemy = enemy,
                CurrentHp = enemy.Stats.Hp,
                MaxHp = enemy.Stats.Hp,
                DefenseStacks = 0
            };

            // 아군 패시브의 "적 대상" 지속 디버프/상태이상을 적에 등록 (상시 디버프가 데미지 계산에 반영되도록)
            RegisterAllyPassiveEnemyEffects(state);

            return state;
        }

        /// <summary>
        /// 개별 캐릭터 초기 스탯 계산.
        /// 공성전 등 다른 시뮬레이터에서도 아군 스탯 초기화에 재사용 (public).
        /// </summary>
        public CharacterBattleState InitializeCharacterState(BattleConfig config, BattleCharacter battleChar, int index)
        {
            // 장비 세트 정보 추출
            var loadout = battleChar.Equipment;
            var activeSets = loadout?.GetActiveSets() ?? new List<EquipmentSet>();

            // 가장 큰 세트 효과를 EquipSetName/Count로 전달
            string equipSetName = "";
            int equipSetCount = 0;
            foreach (var set in activeSets)
            {
                if (set.PieceCount > equipSetCount)
                {
                    equipSetName = set.SetName;
                    equipSetCount = set.PieceCount;
                }
            }

            // 파티 버프 계산 (다른 파티원의 패시브/스킬 버프)
            var partyBuffConfigs = BuildPartyBuffConfigs(config, index);
            var partyEffects = new EffectManager();
            partyEffects.AddEffects(EffectConverter.FromBuffConfigs(
                partyBuffConfigs, config.AllyPet, config.PetStar, config.PetEnhance));
            var (partyPerm, partyTimed, partyPet) = partyEffects.GetSeparatedBuffs();
            var totalDebuffs = partyEffects.GetTotalDebuffs();

            // 진형
            var formation = new Formation
            {
                Name = config.FormationName,
                IsBackPosition = battleChar.IsBackPosition
            };

            // StatCalculator로 최종 스탯 계산
            var statInput = new StatCalculationInput
            {
                Character = battleChar.Character,
                TranscendLevel = battleChar.TranscendLevel,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                IsPassiveConditionMet = battleChar.IsPassiveConditionMet,
                Equipments = loadout?.GetEquipments(),
                EquipSetName = equipSetName,
                EquipSetCount = equipSetCount,
                PotentialAtkLevel = battleChar.PotentialAtkLevel,
                PotentialDefLevel = battleChar.PotentialDefLevel,
                PotentialHpLevel = battleChar.PotentialHpLevel,
                Accessory = loadout?.Accessory,
                Formation = formation,
                Pet = config.AllyPet,
                PetStar = config.PetStar,
                PetOptionAtkRate = config.PetOptionAtkRate,
                PetOptionDefRate = config.PetOptionDefRate,
                PetOptionHpRate = config.PetOptionHpRate,
                TotalBuffs = partyEffects.GetTotalBuffs(),
                TotalDebuffs = totalDebuffs,
                PartyPermanentBuffs = partyPerm,
                PartyTimedBuffs = partyTimed,
                PartyPetBuffs = partyPet
            };

            var statResult = _statCalc.Calculate(statInput);

            var charState = new CharacterBattleState
            {
                Source = battleChar,
                PartyIndex = index,
                CurrentHp = statResult.FinalHp,
                MaxHp = statResult.FinalHp,
                FinalAtk = statResult.FinalAtk,
                FinalDef = statResult.FinalDef,
                FinalSpd = statResult.FinalSpd,
                RotationIndex = 0,
                TotalDamageDealt = 0
            };

            // 상시(Immediate) 패시브 피해 무효화 충전 (예: 자신 모든 피해 무효화[피격 N회])
            var passive = battleChar.Character.Passive;
            if (passive != null)
            {
                foreach (var e in (passive.GetLevelData(battleChar.IsSkillEnhanced)?.Effects)
                                  ?? new List<PersistentEffect>())
                {
                    if (e.Type == PersistentEffectType.DamageNullification && e.ApplyMode == ApplyMode.Immediate
                        && e.DamageNullification != null)
                    {
                        charState.NullifyHitsRemaining = e.DamageNullification.HitCount;
                        charState.NullifyTurnsRemaining = e.DamageNullification.Duration;
                        charState.NullifyType = e.DamageNullification.Type;
                    }
                }
            }

            return charState;
        }

        /// <summary>
        /// 파티 버프 설정 생성 (지정된 캐릭터 제외한 나머지 파티원의 버프)
        /// </summary>
        private List<BuffConfig> BuildPartyBuffConfigs(BattleConfig config, int excludeIndex)
        {
            var configs = new List<BuffConfig>();
            for (int i = 0; i < config.AllyParty.Count; i++)
            {
                if (i == excludeIndex) continue;
                var bc = config.AllyParty[i];

                // 패시브 버프
                configs.Add(new BuffConfig
                {
                    CharacterName = bc.Character.Name,
                    SkillName = null, // 패시브
                    IsBuff = true,
                    IsChecked = true,
                    Level = GetBuffLevel(bc.IsSkillEnhanced, bc.TranscendLevel)
                });

                // 패시브 디버프
                configs.Add(new BuffConfig
                {
                    CharacterName = bc.Character.Name,
                    SkillName = null,
                    IsBuff = false,
                    IsChecked = true,
                    Level = GetBuffLevel(bc.IsSkillEnhanced, bc.TranscendLevel)
                });
            }
            return configs;
        }

        /// <summary>
        /// BuffConfig Level 값 변환
        /// 0: 기본, 1: 스강, 2: 초월6, 3: 스강+초월6
        /// </summary>
        private int GetBuffLevel(bool isEnhanced, int transcendLevel)
        {
            if (isEnhanced && transcendLevel >= 6) return 3;
            if (isEnhanced) return 1;
            if (transcendLevel >= 6) return 2;
            return 0;
        }

        #endregion

        #region 전투 실행

        /// <summary>
        /// 아군 기본공격 실행
        /// 흐름: DoT 처리(+1초) → 스택 트리거 → 기본공격(+2초) → 턴제 효과 턴-1 → 쿨다운 감소
        /// </summary>
        /// <summary>
        /// 평타 로테이션에서 "살아있는 다음 속공 순번" 아군을 반환하고 커서를 진행한다.
        /// 사망 영웅은 건너뛰며 그 다음 영웅이 해당 평타 턴을 가져간다. 전원 사망이면 -1.
        /// (부활하면 다시 로테이션에 포함됨)
        /// </summary>
        private int ResolveNextAliveAlly(BattleState state)
        {
            var order = state.AllySpeedOrder;
            if (order == null || order.Count == 0) return -1;

            for (int n = 0; n < order.Count; n++)
            {
                int idx = order[state.AllyRotationCursor % order.Count];
                state.AllyRotationCursor = (state.AllyRotationCursor + 1) % order.Count;
                if (idx >= 0 && idx < state.AllyStates.Count && !state.AllyStates[idx].IsDead)
                    return idx;
            }
            return -1; // 전원 사망
        }

        private void ExecuteAllyNormalAttack(BattleConfig config, BattleState state, int charIndex)
        {
            if (charIndex < 0 || charIndex >= state.AllyStates.Count) return;

            var charState = state.AllyStates[charIndex];
            if (charState.IsDead) return;   // 사망한 아군은 행동 스킵
            var battleChar = charState.Source;
            var character = battleChar.Character;

            var normalSkill = character.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
            if (normalSkill == null) return;

            double totalDuration = 0;

            // 1. DoT 데미지 먼저 처리 (적에게 걸린 상태이상, +1초)
            double dotDuration = ProcessDoTDamage(config, state, charState);
            totalDuration += dotDuration;

            // 2. PainEndurance 분산 큐 1회 처리 (본인 보유분만, 평타 직전)
            //    룰: 트리거 시 즉시 25% 받고, 그 다음 본인 평타 차례에 분산 1회 적용 후 평타.
            TickPainEnduranceQueue(state, charState);

            // 3. 스택 트리거 처리 (공격 전)
            ProcessStackTriggers(charState, state, isSkill: false);
            ProcessTriggeredFixedDamage(config, state, charState, isSkill: false);

            // 3. 기본공격 실행 (+2초)
            var damage = CalculateSkillDamage(config, state, charState, normalSkill);
            ApplyDamage(state, charState, damage, normalSkill.Name, ActionType.NormalAttack);
            totalDuration += normalSkill.GetActionDuration();

            // 평타의 효과(디버프·상태이상·버프)를 대상에 등록
            RegisterCastEffects(state, charState, normalSkill);

            // 4. 경과 시간 업데이트
            state.ElapsedSeconds += totalDuration;

            // 5. 소요시간만큼 모든 스킬 쿨다운 감소
            ReduceAllCooldowns(state, totalDuration);

            // 6. 턴제 효과 턴 -1
            state.EnemyState.Effects.TickTurn();
            charState.Effects.TickTurn();
        }

        /// <summary>
        /// 아군 스킬 실행 (로테이션에 따라)
        /// </summary>
        private void ExecuteAllySkill(BattleConfig config, BattleState state, Dictionary<int, List<SkillType>> rotations)
        {
            // 스킬을 사용할 캐릭터 결정
            // 로테이션에서 다음 스킬이 있는 캐릭터를 순서대로 탐색
            foreach (var kvp in rotations)
            {
                int charIdx = kvp.Key;
                var rotation = kvp.Value;
                var charState = state.AllyStates[charIdx];

                if (charState.IsDead) continue;   // 사망한 아군은 행동 스킵

                if (charState.RotationIndex >= rotation.Count)
                    continue; // 이 캐릭터의 로테이션이 끝남

                var nextSkillType = rotation[charState.RotationIndex];
                var skill = charState.Source.Character.Skills?
                    .FirstOrDefault(s => s.SkillType == nextSkillType);

                if (skill == null) continue;

                // 쿨다운 확인
                if (!charState.IsSkillReady(nextSkillType))
                    continue;

                // 스택 트리거 처리 (공격 전)
                ProcessStackTriggers(charState, state, isSkill: true);
                ProcessTriggeredFixedDamage(config, state, charState, isSkill: true);

                // 스킬 실행
                var damage = CalculateSkillDamage(config, state, charState, skill);
                ApplyDamage(state, charState, damage, skill.Name, ActionType.SkillAttack);

                // 스킬의 효과(디버프·상태이상·버프)를 대상에 등록
                RegisterCastEffects(state, charState, skill);

                // 스킬 쿨다운 세팅 (티어별)
                double cooldown = skill.GetCooldown(charState.Source.IsSkillEnhanced, charState.Source.TranscendLevel);
                if (cooldown > 0)
                    charState.SkillCooldowns[nextSkillType] = cooldown;

                // 스킬 소요시간만큼 경과 및 쿨다운 감소
                double skillDuration = skill.GetActionDuration();
                state.ElapsedSeconds += skillDuration;
                ReduceAllCooldowns(state, skillDuration);

                // 로테이션 인덱스 증가
                charState.RotationIndex++;

                return; // 스킬은 1회만
            }
        }

        /// <summary>
        /// 보스 스킬 실행 — 모든 아군에게 받피해 적용 (광역 가정).
        /// 받피해는 DamageCalculator를 attacker=보스/target=아군 의미로 호출하고,
        /// 적용은 ApplyIncomingDamage로 — PainEndurance 트리거 시 분산 큐에 등록된다.
        /// </summary>
        private void ExecuteEnemySkill(BattleConfig config, BattleState state)
        {
            // 보스 스킬 사용 → 아군 스킬 쿨다운 5초 감소
            foreach (var charState in state.AllyStates)
            {
                charState.ReduceCooldowns(5);
            }

            var enemySkill = ResolveEnemySkill(config, state);
            string enemyName = config.TargetEnemy?.Name ?? "보스";
            string skillName = enemySkill?.Name ?? "보스 스킬";

            if (enemySkill != null)
            {
                foreach (var charState in state.AllyStates)
                {
                    if (charState.CurrentHp <= 0) continue;

                    double damage = CalculateIncomingDamage(config, state, charState, enemySkill);
                    if (damage <= 0) continue;

                    ApplyIncomingDamage(state, charState, damage, $"{enemyName} {skillName}");
                }
            }

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = enemyName,
                IsAlly = false,
                ActionType = ActionType.SkillAttack,
                SkillName = skillName,
                DamageDealt = 0,
                Description = $"보스 스킬 사용 ({skillName}, 아군 쿨다운 5초 감소)"
            });
        }

        /// <summary>
        /// 보스의 다음 행동(스킬)을 결정. enemy.Skills가 있으면 첫 스킬 사용.
        /// 없으면 임시 fallback (보스 ATK × 100% 광역 공격) 생성.
        /// 추후 EnemyRotation 인덱스 관리/스킬 데이터 연동으로 확장.
        /// </summary>
        private Skill ResolveEnemySkill(BattleConfig config, BattleState state)
        {
            var enemy = config.TargetEnemy;
            if (enemy?.Skills != null && enemy.Skills.Count > 0)
            {
                // 단순화: 첫 번째 스킬 사용. 추후 로테이션 인덱스 관리.
                return enemy.Skills[0];
            }

            // Fallback: 기본 광역 공격 (보스 ATK × 100%)
            return new Skill
            {
                Name = "보스 광역 공격",
                SkillType = SkillType.Skill1,
                TargetCount = 5,
                Atk_Count = 1,
                LevelData = new Dictionary<int, SkillLevelData>
                {
                    { 0, new SkillLevelData { Ratio = 100 } }
                }
            };
        }

        /// <summary>
        /// 스킬 데미지 계산 (기존 DamageCalculator 활용)
        /// </summary>
        private double CalculateSkillDamage(BattleConfig config, BattleState state,
            CharacterBattleState charState, Skill skill)
        {
            var battleChar = charState.Source;
            var enemyState = state.EnemyState;

            // 적 디버프 합산 (EffectManager 통합)
            var enemyDebuffs = state.EnemyState.Effects.GetTotalDebuffs();
            int enemyDebuffCount = state.EnemyState.Effects.GetActiveDebuffCount();

            // 디버프당 동적 피증 (PerEnemyDebuffDmgBonus) — 패시브+스킬에서 합산
            var perDebuffBonus = PerDebuffBonusExtractor.From(
                battleChar.Character, skill, battleChar.IsSkillEnhanced, battleChar.TranscendLevel);

            // 타겟 수에 따른 보스 피해감소
            double targetReduction = GetTargetReduction(config.TargetEnemy,
                skill.GetTargetCount(battleChar.IsSkillEnhanced, battleChar.TranscendLevel));

            // 적 방어력 (스택 포함)
            double enemyDef = config.TargetEnemy.Stats.Def;
            double enemyDefIncrease = 0;
            if (config.TargetEnemy.IsStackableDefenseIncrease)
            {
                enemyDefIncrease = config.TargetEnemy.GetStackableDefenseIncrease(enemyState.DefenseStacks);
            }
            else if (config.TargetEnemy.DefenseIncrease > 0)
            {
                enemyDefIncrease = config.TargetEnemy.DefenseIncrease;
            }

            // DisplayStats에서 추가 스탯 가져오기
            // 간소화: 초기 계산된 스탯 사용
            var damageInput = new DamageCalculator.DamageInput
            {
                Character = battleChar.Character,
                Skill = skill,
                IsSkillEnhanced = battleChar.IsSkillEnhanced,
                TranscendLevel = battleChar.TranscendLevel,
                FinalAtk = charState.FinalAtk,
                FinalDef = charState.FinalDef,
                FinalHp = charState.MaxHp,
                CritDamage = battleChar.Character.GetBaseStats().Cri_Dmg,
                DmgDealt = 0,  // StatCalculator 결과에서 가져와야 하지만, 초기화 시 저장 필요
                DmgDealtType = 0,
                DmgDealtBoss = 0,
                ArmorPen = 0,
                WeakpointDmg = battleChar.Character.GetBaseStats().Wek_Dmg,
                BossDef = enemyDef,
                BossDefIncrease = enemyDefIncrease,
                BossDmgReduction = config.TargetEnemy.DamageReduction,
                BossTargetReduction = targetReduction,
                BossHp = enemyState.MaxHp,
                TargetHp = enemyState.MaxHp,
                TargetCurrentHp = enemyState.CurrentHp,
                DefReduction = enemyDebuffs.Def_Reduction,
                DmgTakenIncrease = enemyDebuffs.GetEffectiveDmgTakenIncrease(battleChar.Character.AttackType),
                Vulnerability = enemyDebuffs.Vulnerability + config.TargetEnemy.Vulnerability,
                BossVulnerability = enemyDebuffs.Boss_Vulnerability,
                // 시뮬에서는 치명/약점을 확률적으로 처리하거나 항상 발동으로 설정
                IsCritical = true,
                IsWeakpoint = true,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = config.TargetEnemy?.IsBoss ?? true,
                SelfMaxHp = charState.MaxHp,

                // 디버프당 동적 피증
                TargetDebuffCount = enemyDebuffCount,
                PerDebuffBonusPercent = perDebuffBonus.PercentPerDebuff,
                PerDebuffBonusMaxStacks = perDebuffBonus.MaxStacks
            };

            var result = _damageCalc.Calculate(damageInput);
            return result.FinalDamage;
        }

        /// <summary>
        /// 데미지 적용 및 로그 기록
        /// </summary>
        private void ApplyDamage(BattleState state, CharacterBattleState charState,
            double damage, string skillName, ActionType actionType)
        {
            state.EnemyState.CurrentHp -= damage;
            charState.TotalDamageDealt += damage;
            state.TotalDamageDealt += damage;

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = charState.Source.Character.Name,
                IsAlly = true,
                ActionType = actionType,
                SkillName = skillName,
                DamageDealt = damage,
                Description = $"{charState.Source.Character.Name} → {skillName}: {damage:N0}"
            });
        }

        /// <summary>
        /// 아군이 받은 직접 피해를 적용한다.
        /// PainEndurance(트루드 「전투의 희열」) 트리거 조건(rawDamage >= 최대 HP × Threshold%) 충족 시
        /// 즉시 (100 - ReductionRate)% 만 받고, ReductionRate% 는 Duration 턴에 걸쳐 매 턴 균등 분산.
        /// 발동마다 큐 항목이 독립 등록되어 자연스럽게 누적된다.
        /// </summary>
        private void ApplyIncomingDamage(BattleState state, CharacterBattleState target,
            double rawDamage, string sourceLabel, DamageNullType incomingType = DamageNullType.All)
        {
            if (rawDamage <= 0 || target.IsDead) return;

            // 피해 무효화 (피격 N회 / N턴, 물·마 한정 가능) — 피해 자체를 0으로
            if ((target.NullifyHitsRemaining > 0 || target.NullifyTurnsRemaining > 0)
                && (target.NullifyType == DamageNullType.All || target.NullifyType == incomingType))
            {
                if (target.NullifyHitsRemaining > 0) target.NullifyHitsRemaining--;
                state.TurnLogs.Add(new BattleTurnLog
                {
                    Turn = state.CurrentTurn, ActorName = target.Source.Character.Name, IsAlly = true,
                    ActionType = ActionType.BuffApplied, SkillName = "피해 무효화",
                    DamageDealt = 0, Description = $"{sourceLabel} 피격 {rawDamage:N0} 무효화 (잔여 {target.NullifyHitsRemaining}회)"
                });
                return;
            }

            var painEndurance = GetActivePainEndurance(target);
            bool triggers = painEndurance != null
                            && painEndurance.Duration > 0
                            && rawDamage >= target.MaxHp * (painEndurance.Threshold / 100.0);

            double immediateDamage;
            if (triggers)
            {
                double immediateRatio = (100.0 - painEndurance.ReductionRate) / 100.0;
                double deferredRatio = painEndurance.ReductionRate / 100.0;
                immediateDamage = rawDamage * immediateRatio;
                double perTurn = (rawDamage * deferredRatio) / painEndurance.Duration;

                target.PainEnduranceQueue.Add(new PendingPainEnduranceDamage
                {
                    PerTurnAmount = perTurn,
                    RemainingTurns = painEndurance.Duration,
                    SourceLabel = sourceLabel
                });

                state.TurnLogs.Add(new BattleTurnLog
                {
                    Turn = state.CurrentTurn,
                    ActorName = target.Source.Character.Name,
                    IsAlly = true,
                    ActionType = ActionType.BuffApplied,
                    SkillName = "고통 인내 발동",
                    DamageDealt = 0,
                    Description = $"{sourceLabel} 피격 {rawDamage:N0} → 즉시 {immediateDamage:N0}, 매 턴 {perTurn:N0}씩 {painEndurance.Duration}턴 분산"
                });
            }
            else
            {
                immediateDamage = rawDamage;
            }

            double newHp = target.CurrentHp - immediateDamage;
            bool lethal = newHp <= 0;
            target.CurrentHp = Math.Max(0, newHp);

            if (lethal)
                ResolveLethalDamage(state, target, immediateDamage);
        }

        /// <summary>
        /// 치사 피해 발생 시 생존 메카닉 적용 순서: 부활 후 무적(불굴/불사) → 권능(생존) → 부활 → 사망.
        /// </summary>
        private void ResolveLethalDamage(BattleState state, CharacterBattleState target, double lethalDamage)
        {
            // 생존 판정은 공유 SurvivalResolver로 위임 (단일보스전/공성전 공통). 로그만 여기서 기록.
            var r = SurvivalResolver.ResolveLethal(target);
            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = target.Source.Character.Name,
                IsAlly = true,
                ActionType = ActionType.BuffApplied,
                SkillName = r.Label,
                DamageDealt = 0,
                Description = r.Description,
            });
        }

        /// <summary>
        /// 매 턴 시작 시 호출 — PainEnduranceQueue의 각 항목에서 PerTurnAmount만큼 차감,
        /// RemainingTurns 1 감소, 만료 항목 제거.
        /// 누적된 여러 발동의 분산 피해를 한 번에 합산해 받는다.
        /// </summary>
        private void TickPainEnduranceQueue(BattleState state, CharacterBattleState charState)
        {
            if (charState.PainEnduranceQueue.Count == 0) return;

            double totalThisTurn = 0;
            foreach (var pending in charState.PainEnduranceQueue)
            {
                totalThisTurn += pending.PerTurnAmount;
                pending.RemainingTurns--;
            }
            charState.PainEnduranceQueue.RemoveAll(p => p.RemainingTurns <= 0);

            if (totalThisTurn <= 0) return;

            double newHp = charState.CurrentHp - totalThisTurn;
            bool lethal = newHp <= 0;
            charState.CurrentHp = Math.Max(0, newHp);

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = charState.Source.Character.Name,
                IsAlly = true,
                ActionType = ActionType.DoTDamage,
                SkillName = "고통 인내 분산",
                DamageDealt = totalThisTurn,
                Description = $"분산 합계 {totalThisTurn:N0}"
            });

            if (lethal)
                ResolveLethalDamage(state, charState, totalThisTurn);
        }

        /// <summary>
        /// 캐릭터 패시브에서 활성 PainEndurance 데이터를 조회 (없으면 null).
        /// </summary>
        private PainEndurance GetActivePainEndurance(CharacterBattleState charState)
        {
            var passive = charState.Source.Character.Passive;
            if (passive == null) return null;

            var levelData = passive.GetLevelData(charState.Source.IsSkillEnhanced);
            return levelData?.PainEndurance;
        }

        // (생존 메카닉 GetPassiveSurvival/ResolveLethal/ApplyRevivalCooldownReset은 SurvivalResolver로 추출 — 단일보스전/공성전 공유)

        /// <summary>
        /// 보스가 한 명의 아군을 공격할 때 발생하는 1회 받피해를 계산한다.
        /// DamageCalculator를 attacker=보스, target=아군 의미로 호출.
        /// "Boss" prefix가 붙은 입력 필드는 의미상 "target" — 그대로 아군 데이터로 채워 사용.
        /// </summary>
        private double CalculateIncomingDamage(BattleConfig config, BattleState state,
            CharacterBattleState target, Skill enemySkill, bool isCritical = false, bool isWeakpoint = false)
        {
            var enemy = config.TargetEnemy;
            if (enemy == null || enemySkill == null) return 0;

            // 아군이 보유한 받피감 (자버프 자체 받피감 합산)
            var (targetPerm, targetTimed, targetPet) = target.Effects.GetSeparatedBuffs();
            double targetDmgRdc = targetPerm.Dmg_Rdc + targetTimed.Dmg_Rdc + targetPet.Dmg_Rdc;

            // 아군에게 걸린 디버프 (받피증/취약)
            var targetDebuffs = target.Effects.GetTotalDebuffs();

            var damageInput = new DamageCalculator.DamageInput
            {
                // === attacker 측 = 보스 ===
                Character = null,                 // 보스는 Character 모델이 없음 (DamageCalculator는 ?. 처리)
                Skill = enemySkill,
                IsSkillEnhanced = false,
                TranscendLevel = 0,
                FinalAtk = enemy.Stats.Atk,
                FinalDef = 0,
                FinalHp = enemy.Stats.Hp,
                CritDamage = enemy.Stats.Cri_Dmg,
                DmgDealt = enemy.Stats.Dmg_Dealt,
                DmgDealtType = enemy.Stats.Dmg_Dealt_Type,
                DmgDealtBoss = 0,                 // 아군 대상 → 보스피증 미적용
                ArmorPen = enemy.Stats.Arm_Pen,
                WeakpointDmg = enemy.Stats.Wek_Dmg,

                // === target 측 = 아군 ("Boss*" 필드는 의미상 target) ===
                BossDef = target.FinalDef,
                BossDefIncrease = 0,
                BossDmgReduction = targetDmgRdc,  // 아군의 받피감
                BossTargetReduction = 0,          // n인기 감쇄는 보스 전용 개념, 아군엔 미적용
                BossHp = target.MaxHp,
                TargetHp = target.MaxHp,
                TargetCurrentHp = target.CurrentHp,

                // 아군에게 걸린 디버프 → 받는 피해 증가 (취약/받피증)
                DefReduction = targetDebuffs.Def_Reduction,
                DmgTakenIncrease = targetDebuffs.Dmg_Taken_Increase,
                Vulnerability = targetDebuffs.Vulnerability,
                BossVulnerability = 0,            // 아군 대상이라 미적용

                // 전투 옵션 (보스 측 치명/약점은 호출자가 지정)
                IsCritical = isCritical,
                IsWeakpoint = isWeakpoint,
                IsSkillConditionMet = false,
                IsLostHpConditionMet = false,
                IsTargetBoss = false,             // 핵심 — Dmg_Dealt_Bos 미적용
                SelfMaxHp = enemy.Stats.Hp,
                Mode = BattleMode.Boss
            };

            return _damageCalc.Calculate(damageInput).FinalDamage;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 스킬 로테이션 준비
        /// </summary>
        private Dictionary<int, List<SkillType>> PrepareRotations(BattleConfig config, BattleState state)
        {
            if (config.RotationMode == RotationMode.UserDefined && config.UserRotations != null)
            {
                return config.UserRotations;
            }

            // 기본 로테이션: 궁극기 → 스킬2 → 스킬1 순서 반복
            var rotations = new Dictionary<int, List<SkillType>>();
            for (int i = 0; i < state.AllyStates.Count; i++)
            {
                var character = state.AllyStates[i].Source.Character;
                var availableSkills = character.Skills?
                    .Where(s => s.SkillType != SkillType.Normal && s.SkillType != SkillType.Normal2)
                    .OrderByDescending(s => s.SkillType) // Ultimate > Skill4 > Skill3 > Skill2 > Skill1
                    .Select(s => s.SkillType)
                    .ToList() ?? new List<SkillType>();

                // 로테이션을 충분히 반복 생성
                var rotation = new List<SkillType>();
                for (int r = 0; r < 20; r++) // 최대 20사이클
                {
                    rotation.AddRange(availableSkills);
                }
                rotations[i] = rotation;
            }
            return rotations;
        }

        /// <summary>
        /// 타겟 수에 따른 보스 피해감소율
        /// </summary>
        /// <summary>
        /// 스택 트리거 처리: 패시브의 Triggered 효과에 대해 공격 카운터 증가 및 스택 적용
        /// </summary>
        private void ProcessStackTriggers(CharacterBattleState charState, BattleState state, bool isSkill)
        {
            var passive = charState.Source.Character.Passive;
            if (passive == null) return;

            var levelData = passive.GetLevelData(charState.Source.IsSkillEnhanced);
            if (levelData.Effects == null) return;

            // 초월 Effects가 있으면 같은 StatusType을 오버라이드
            var transcend = passive.GetTranscendBonus(charState.Source.TranscendLevel);
            var transcendEffects = transcend?.Effects;

            foreach (var baseEffect in levelData.Effects)
            {
                // 초월이 같은 StatusType을 정의하고 있으면 오버라이드
                var effect = baseEffect;
                if (transcendEffects != null)
                {
                    var override_ = transcendEffects.FirstOrDefault(
                        e => e.StatusType == baseEffect.StatusType && e.StatusType != StatusEffectType.None);
                    if (override_ != null)
                        effect = override_;
                }
                if (effect.ApplyMode != ApplyMode.Triggered) continue;
                if (effect.MaxStacks <= 0) continue;

                // 트리거 조건 확인
                bool matches = effect.TriggerCondition switch
                {
                    TriggerCondition.AllAttack => true,
                    TriggerCondition.SkillOnly => isSkill,
                    TriggerCondition.NormalOnly => !isSkill,
                    _ => false
                };
                if (!matches) continue;

                string effectId = $"stack:{charState.Source.Character.Name}:{effect.Type}:{effect.Target}";

                // 카운터 증가
                if (!charState.StackTriggerCounters.ContainsKey(effectId))
                    charState.StackTriggerCounters[effectId] = 0;
                charState.StackTriggerCounters[effectId]++;

                // 트리거 횟수 도달 시 스택 부여
                if (charState.StackTriggerCounters[effectId] >= effect.TriggerCount)
                {
                    charState.StackTriggerCounters[effectId] = 0;

                    if (!charState.CurrentStacks.ContainsKey(effectId))
                        charState.CurrentStacks[effectId] = 0;

                    int newStacks = Math.Min(
                        charState.CurrentStacks[effectId] + effect.StacksPerTrigger,
                        effect.MaxStacks);
                    charState.CurrentStacks[effectId] = newStacks;

                    // 스택에 따른 효과 적용 (버프/디버프를 EffectManager에 반영)
                    ApplyStackEffect(charState, state, effect, effectId, newStacks);
                }
            }
        }

        /// <summary>
        /// 트리거형 추가 피해(TriggeredFixedDamage) 처리 — N회 공격마다 적군에게 고정/공격력비례 추가 피해.
        /// 합성 스킬로 만들어 CalculateSkillDamage를 재사용(방어계수·버프·n인기 감쇄 일관 적용)한 뒤 보스에 적용.
        /// 여포(기본 2회마다 공45%)·발리스타·챈슬러·겔리두스 등.
        /// </summary>
        private void ProcessTriggeredFixedDamage(BattleConfig config, BattleState state,
            CharacterBattleState charState, bool isSkill)
        {
            var passive = charState.Source.Character.Passive;
            if (passive == null) return;

            var effects = new List<PersistentEffect>();
            var lvl = passive.GetLevelData(charState.Source.IsSkillEnhanced);
            if (lvl?.Effects != null) effects.AddRange(lvl.Effects);
            var tr = passive.GetTranscendBonus(charState.Source.TranscendLevel);
            if (tr?.Effects != null) effects.AddRange(tr.Effects);

            foreach (var e in effects)
            {
                if (e.Type != PersistentEffectType.TriggeredFixedDamage || e.TriggeredFixedDamage == null) continue;
                var tfd = e.TriggeredFixedDamage;

                bool matches = tfd.TriggerOn switch
                {
                    TriggerCondition.AllAttack => true,
                    TriggerCondition.SkillOnly => isSkill,
                    TriggerCondition.NormalOnly => !isSkill,
                    _ => false
                };
                if (!matches) continue;

                string key = $"tfd:{charState.Source.Character.Name}:{tfd.TriggerOn}:{tfd.AtkRatio}:{tfd.FixedDamage}";
                if (!charState.StackTriggerCounters.ContainsKey(key)) charState.StackTriggerCounters[key] = 0;
                charState.StackTriggerCounters[key]++;
                if (charState.StackTriggerCounters[key] < Math.Max(1, tfd.TriggerCount)) continue;
                charState.StackTriggerCounters[key] = 0;

                var synth = new Skill
                {
                    Name = "추가공격",
                    SkillType = SkillType.Skill1,
                    LevelData = new Dictionary<int, SkillLevelData>
                    {
                        { 0, new SkillLevelData { Ratio = tfd.AtkRatio, FixedDamage = tfd.FixedDamage, TargetCount = Math.Max(1, tfd.TargetCount), AtkCount = Math.Max(1, tfd.HitCount) } },
                        { 1, new SkillLevelData { Ratio = tfd.AtkRatio, FixedDamage = tfd.FixedDamage, TargetCount = Math.Max(1, tfd.TargetCount), AtkCount = Math.Max(1, tfd.HitCount) } },
                    }
                };
                double dmg = CalculateSkillDamage(config, state, charState, synth);
                if (dmg > 0)
                    ApplyDamage(state, charState, dmg, "추가공격", ActionType.SkillAttack);
            }
        }

        #region 캐스트 타임 효과 등록 (디버프·상태이상·버프를 대상 EffectManager에 적용)

        /// <summary>
        /// 스킬/평타 시전 시 그 스킬의 효과(디버프·상태이상·버프)를 대상 EffectManager에 등록한다.
        /// - 적 대상 디버프/상태이상 → 적 EffectManager (이후 CalculateSkillDamage·ProcessDoTDamage가 소비)
        /// - 자버프/파티버프 → 아군 EffectManager (현재 스탯은 전투 시작 시 고정 계산되어 공격 스탯엔
        ///   소급되지 않지만, 받피감/버프 해제·후속 메카닉이 참조)
        /// 시뮬은 결정론적(치명·약점 항상 발동)이라 ApplyChance와 무관하게 적용한다.
        /// </summary>
        private void RegisterCastEffects(BattleState state, CharacterBattleState caster, Skill skill)
        {
            if (skill == null) return;

            var effects = EffectConverter.FromSkill(
                skill, caster.Source.Character.Name,
                caster.Source.IsSkillEnhanced, caster.Source.TranscendLevel);

            foreach (var effect in effects)
            {
                // 스킬 보너스(해당 스킬 데미지 계산 전용)는 적용 대상이 아님
                if (effect.Category == EffectCategory.SkillBonus) continue;
                // 즉시 소멸용(계산 전용) 비상태 효과 스킵
                if (!effect.IsStatusEffect && !effect.IsPermanent && effect.RemainingTurns <= 0) continue;

                RouteEffect(state, caster, effect);
            }
        }

        /// <summary>
        /// 전투 시작 시 각 아군 패시브의 "적 대상" 지속 디버프/상태이상을 적 EffectManager에 등록.
        /// (아군 대상 패시브 버프는 StatCalculator에서 이미 스탯에 반영되므로 제외.)
        /// </summary>
        private void RegisterAllyPassiveEnemyEffects(BattleState state)
        {
            foreach (var ally in state.AllyStates)
            {
                var passive = ally.Source.Character.Passive;
                if (passive == null) continue;

                var effects = EffectConverter.FromPassive(
                    passive, ally.Source.Character.Name,
                    ally.Source.IsSkillEnhanced, ally.Source.TranscendLevel,
                    ally.Source.IsPassiveConditionMet);

                foreach (var effect in effects)
                {
                    if (effect.Target != EffectTarget.Enemy && effect.Target != EffectTarget.AllEnemies)
                        continue;
                    RegisterToManager(state.EnemyState.Effects, effect, ownerForOrder: null);
                }
            }
        }

        /// <summary>변환된 BattleEffect를 Target에 따라 알맞은 EffectManager로 분배한다.</summary>
        private void RouteEffect(BattleState state, CharacterBattleState caster, BattleEffect effect)
        {
            switch (effect.Target)
            {
                case EffectTarget.Enemy:
                case EffectTarget.AllEnemies:
                    RegisterToManager(state.EnemyState.Effects, effect, ownerForOrder: null);
                    break;

                case EffectTarget.Self:
                case EffectTarget.SingleAlly: // 근사: 단일 아군 선정 런타임은 후속 — 시전자 본인
                    RegisterToManager(caster.Effects, effect, ownerForOrder: caster);
                    break;

                case EffectTarget.Party:
                case EffectTarget.SelfAndHighestAtkAlly: // 근사: 파티 전체 — 타겟 셀렉터 런타임은 후속
                    foreach (var ally in state.AllyStates)
                    {
                        if (ally.IsDead) continue;
                        RegisterToManager(ally.Effects, effect.Clone(), ownerForOrder: ally);
                    }
                    break;
            }
        }

        /// <summary>
        /// 효과를 매니저에 등록한다. 재시전 시 동일 효과(Id)는 갱신(제거 후 재등록)하여 무한 중첩을 막는다.
        /// 턴제 버프엔 버프 해제(FIFO)용 ApplyOrderId를 부여한다(상시·디버프·상태이상·해제불가 제외).
        /// </summary>
        private void RegisterToManager(EffectManager manager, BattleEffect effect, CharacterBattleState ownerForOrder)
        {
            if (!string.IsNullOrEmpty(effect.Id))
            {
                effect.SourceName = effect.Id;     // RemoveBySource(Id)로 갱신 가능하게
                manager.RemoveBySource(effect.Id);
            }

            if (ownerForOrder != null && effect.IsStatBuff && !effect.IsPermanent)
                effect.ApplyOrderId = ++ownerForOrder.ApplyOrderCounter;

            manager.AddEffect(effect);
        }

        #endregion

        /// <summary>
        /// 스택 효과를 EffectManager에 적용
        /// </summary>
        private void ApplyStackEffect(CharacterBattleState charState, BattleState state,
            PersistentEffect effect, string effectId, int currentStacks)
        {
            switch (effect.Type)
            {
                case PersistentEffectType.Buff:
                    if (effect.Buff == null) break;
                    // 기존 효과 제거 후 스택 반영된 새 효과 추가
                    charState.Effects.RemoveBySource(effectId);
                    var scaledBuff = ScaleBuffByStacks(effect.Buff, currentStacks);
                    charState.Effects.AddEffect(new BattleEffect
                    {
                        Id = effectId,
                        SourceName = effectId,
                        Category = effect.Target == EffectTarget.Self
                            ? EffectCategory.PassiveSelfBuff
                            : EffectCategory.PassivePartyBuff,
                        Target = effect.Target,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = true,
                        BuffValues = scaledBuff
                    });
                    break;

                case PersistentEffectType.Debuff:
                    if (effect.Debuff == null) break;
                    state.EnemyState.Effects.RemoveBySource(effectId);
                    var scaledDebuff = ScaleDebuffByStacks(effect.Debuff, currentStacks);
                    state.EnemyState.Effects.AddEffect(new BattleEffect
                    {
                        Id = effectId,
                        SourceName = effectId,
                        Category = EffectCategory.PassiveDebuff,
                        Target = EffectTarget.Enemy,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = true,
                        DebuffValues = scaledDebuff
                    });
                    break;
            }
        }

        /// <summary>
        /// BuffSet을 스택 수에 비례하여 스케일링
        /// </summary>
        private BuffSet ScaleBuffByStacks(BuffSet baseBuff, int stacks)
        {
            if (stacks <= 1) return baseBuff;
            var result = new BuffSet();
            for (int i = 0; i < stacks; i++)
                result.Add(baseBuff);
            return result;
        }

        /// <summary>
        /// DebuffSet을 스택 수에 비례하여 스케일링
        /// </summary>
        private DebuffSet ScaleDebuffByStacks(DebuffSet baseDebuff, int stacks)
        {
            if (stacks <= 1) return baseDebuff;
            var result = new DebuffSet();
            for (int i = 0; i < stacks; i++)
                result.Add(baseDebuff);
            return result;
        }

        /// <summary>
        /// 적에게 걸린 DoT 상태이상 틱 데미지 처리
        /// 기본공격 전에 먼저 실행됨
        /// </summary>
        /// <returns>DoT 처리 소요시간 (초). DoT 없으면 0</returns>
        private double ProcessDoTDamage(BattleConfig config, BattleState state, CharacterBattleState charState)
        {
            var dotEffects = state.EnemyState.Effects.GetActiveStatusEffects()
                .Where(e => e.Category == EffectCategory.DamageOverTime && e.StatusData != null)
                .ToList();

            if (dotEffects.Count == 0) return 0;

            foreach (var dot in dotEffects)
            {
                double dotDamage = 0;
                var data = dot.StatusData;

                // 공격력 비례 DoT (화상, 출혈 등)
                if (data.AtkRatio > 0)
                {
                    dotDamage = charState.FinalAtk * (data.AtkRatio / 100.0);
                }
                // 최대 HP 비례 DoT (중독, 마력역류 등)
                else if (data.TargetMaxHpRatio > 0)
                {
                    dotDamage = state.EnemyState.MaxHp * (data.TargetMaxHpRatio / 100.0);
                    if (data.AtkCap > 0)
                        dotDamage = Math.Min(dotDamage, charState.FinalAtk * (data.AtkCap / 100.0));
                }
                // 현재 HP 비례 DoT (즉사 등)
                else if (data.TargetCurrentHpRatio > 0)
                {
                    dotDamage = state.EnemyState.CurrentHp * (data.TargetCurrentHpRatio / 100.0);
                }
                // 고정 피해 (수정 결정 등)
                else if (data.FixedDamage > 0)
                {
                    dotDamage = data.FixedDamage;
                }

                // 스택 수 반영
                dotDamage *= dot.Stacks;

                if (dotDamage > 0)
                {
                    state.EnemyState.CurrentHp -= dotDamage;
                    state.TotalDamageDealt += dotDamage;
                    charState.TotalDamageDealt += dotDamage;

                    string dotName = dot.StatusType.HasValue
                        ? StatusEffectDb.Get(dot.StatusType.Value)?.Name ?? dot.StatusType.ToString()
                        : "DoT";

                    state.TurnLogs.Add(new BattleTurnLog
                    {
                        Turn = state.CurrentTurn,
                        ActorName = charState.Source.Character.Name,
                        IsAlly = true,
                        ActionType = ActionType.DoTDamage,
                        SkillName = dotName,
                        DamageDealt = dotDamage,
                        Description = $"[DoT] {dotName} {dot.Stacks}스택: {dotDamage:N0}"
                    });
                }
            }

            // DoT 처리 소요시간: 1초 (고정)
            return 1.0;
        }

        /// <summary>
        /// 모든 아군 캐릭터의 스킬 쿨다운을 시간만큼 감소
        /// </summary>
        private void ReduceAllCooldowns(BattleState state, double seconds)
        {
            foreach (var charState in state.AllyStates)
            {
                var keys = charState.SkillCooldowns.Keys.ToList();
                foreach (var key in keys)
                {
                    charState.SkillCooldowns[key] = Math.Max(0, charState.SkillCooldowns[key] - seconds);
                }
            }
        }

        private double GetTargetReduction(Enemy enemy, int targetCount)
        {
            if (enemy == null) return 0;
            return targetCount switch
            {
                1 => enemy.SingleTargetReduction,
                3 => enemy.TripleTargetReduction,
                >= 5 => enemy.MultiTargetReduction,
                _ => 0
            };
        }

        /// <summary>
        /// 턴 종료 처리
        /// </summary>
        private void ProcessTurnEnd(BattleState state, BattleAction action)
        {
            // 기본공격의 턴제 효과/DoT/쿨다운은 ExecuteAllyNormalAttack 내에서 직접 처리.
            // 턴 기반 생존 메카닉(피해무효화[N턴]·불사[N턴]) 잔여 턴 감소.
            foreach (var ally in state.AllyStates)
            {
                if (ally.IsDead) continue;
                if (ally.NullifyTurnsRemaining > 0) ally.NullifyTurnsRemaining--;
                if (ally.ImmortalTurnsRemaining > 0) ally.ImmortalTurnsRemaining--;
            }
        }

        #endregion

        #region 결과 생성

        /// <summary>
        /// 시뮬레이션 결과 생성
        /// </summary>
        private BattleResult BuildResult(BattleState state, BattleConfig config)
        {
            var result = new BattleResult
            {
                TotalDamage = state.TotalDamageDealt,
                TotalTurns = state.CurrentTurn,
                TurnLogs = state.TurnLogs,
                EnemyRemainingHp = Math.Max(0, state.EnemyState.CurrentHp)
            };

            foreach (var charState in state.AllyStates)
            {
                var charResult = new CharacterDamageResult
                {
                    CharacterName = charState.Source.Character.Name,
                    PartyIndex = charState.PartyIndex,
                    TotalDamage = charState.TotalDamageDealt,
                    DamageShare = state.TotalDamageDealt > 0
                        ? charState.TotalDamageDealt / state.TotalDamageDealt * 100
                        : 0
                };

                // 기본공격/스킬 데미지 분류
                var charLogs = state.TurnLogs
                    .Where(l => l.IsAlly && l.ActorName == charState.Source.Character.Name);
                charResult.NormalAttackDamage = charLogs
                    .Where(l => l.ActionType == ActionType.NormalAttack)
                    .Sum(l => l.DamageDealt);
                charResult.SkillDamage = charLogs
                    .Where(l => l.ActionType == ActionType.SkillAttack)
                    .Sum(l => l.DamageDealt);

                result.CharacterResults.Add(charResult);
            }

            return result;
        }

        #endregion
    }
}

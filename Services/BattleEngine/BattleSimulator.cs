using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 턴제 배틀 시뮬레이터
    /// 기존 DamageCalculator, StatCalculator, BuffCalculator를 활용하여
    /// 5인 파티 vs 보스 배틀을 시뮬레이션
    /// </summary>
    public class BattleSimulator
    {
        private readonly DamageCalculator _damageCalc = new();
        private readonly StatCalculator _statCalc = new();
        private readonly BuffCalculator _buffCalc = new();

        /// <summary>
        /// 배틀 시뮬레이션 실행
        /// </summary>
        public BattleResult Simulate(BattleConfig config)
        {
            // 1. 초기 상태 설정
            var state = InitializeBattleState(config);

            // 2. 턴 매니저 생성
            var turnManager = new TurnManager(config, state.AllyStates);

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
                        ExecuteAllyNormalAttack(config, state, action.CharacterIndex);
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

            return state;
        }

        /// <summary>
        /// 개별 캐릭터 초기 스탯 계산
        /// </summary>
        private CharacterBattleState InitializeCharacterState(BattleConfig config, BattleCharacter battleChar, int index)
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
            var (partyPerm, partyTimed, partyPet) = _buffCalc.CalculateSeparatedPartyBuffs(
                partyBuffConfigs, config.AllyPet, config.PetStar);
            var totalDebuffs = _buffCalc.CalculateTotalDebuffs(partyBuffConfigs, config.AllyPet, config.PetStar);

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
                TotalBuffs = _buffCalc.CalculateTotalBuffs(partyBuffConfigs, config.AllyPet, config.PetStar),
                TotalDebuffs = totalDebuffs,
                PartyPermanentBuffs = partyPerm,
                PartyTimedBuffs = partyTimed,
                PartyPetBuffs = partyPet
            };

            var statResult = _statCalc.Calculate(statInput);

            return new CharacterBattleState
            {
                Source = battleChar,
                PartyIndex = index,
                CurrentHp = statResult.FinalHp,
                MaxHp = statResult.FinalHp,
                FinalAtk = statResult.FinalAtk,
                FinalDef = statResult.FinalDef,
                FinalSpd = statResult.FinalSpd,
                PassiveStacks = 0,
                RotationIndex = 0,
                TotalDamageDealt = 0
            };
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
        /// </summary>
        private void ExecuteAllyNormalAttack(BattleConfig config, BattleState state, int charIndex)
        {
            if (charIndex < 0 || charIndex >= state.AllyStates.Count) return;

            var charState = state.AllyStates[charIndex];
            var battleChar = charState.Source;
            var character = battleChar.Character;

            // 평타 스킬 찾기
            var normalSkill = character.Skills?.FirstOrDefault(s => s.SkillType == SkillType.Normal);
            if (normalSkill == null) return;

            var damage = CalculateSkillDamage(config, state, charState, normalSkill);

            // 데미지 적용
            ApplyDamage(state, charState, damage, normalSkill.Name, ActionType.NormalAttack);
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

                if (charState.RotationIndex >= rotation.Count)
                    continue; // 이 캐릭터의 로테이션이 끝남

                var nextSkillType = rotation[charState.RotationIndex];
                var skill = charState.Source.Character.Skills?
                    .FirstOrDefault(s => s.SkillType == nextSkillType);

                if (skill == null) continue;

                // 쿨다운 확인
                if (!charState.IsSkillReady(nextSkillType))
                    continue;

                // 스킬 실행
                var damage = CalculateSkillDamage(config, state, charState, skill);
                ApplyDamage(state, charState, damage, skill.Name, ActionType.SkillAttack);

                // 로테이션 인덱스 증가
                charState.RotationIndex++;

                // 상대 측 쿨다운 감소 (아군 스킬 사용 → 보스의 쿨다운에는 영향 없음)
                // 보스 스킬 사용 시 아군 쿨다운 5초 감소는 ExecuteBossSkill에서 처리

                return; // 스킬은 1회만
            }
        }

        /// <summary>
        /// 보스 스킬 실행
        /// </summary>
        private void ExecuteEnemySkill(BattleConfig config, BattleState state)
        {
            // 보스 스킬 사용 → 아군 스킬 쿨다운 5초 감소
            foreach (var charState in state.AllyStates)
            {
                charState.ReduceCooldowns(5);
            }

            // 보스 로테이션에서 현재 스킬 가져오기
            if (config.EnemyRotation != null && config.EnemyRotation.Count > 0)
            {
                // TODO: 보스 스킬 로테이션 인덱스 관리
                // 현재는 쿨다운 감소만 처리
            }

            state.TurnLogs.Add(new BattleTurnLog
            {
                Turn = state.CurrentTurn,
                ActorName = config.TargetEnemy?.Name ?? "보스",
                IsAlly = false,
                ActionType = ActionType.SkillAttack,
                SkillName = "보스 스킬",
                DamageDealt = 0,
                Description = "보스 스킬 사용 (아군 쿨다운 5초 감소)"
            });
        }

        /// <summary>
        /// 스킬 데미지 계산 (기존 DamageCalculator 활용)
        /// </summary>
        private double CalculateSkillDamage(BattleConfig config, BattleState state,
            CharacterBattleState charState, Skill skill)
        {
            var battleChar = charState.Source;
            var enemyState = state.EnemyState;

            // 적 디버프 합산
            var enemyDebuffs = AggregateEnemyDebuffs(state);

            // 타겟 수에 따른 보스 피해감소
            double targetReduction = GetTargetReduction(config.TargetEnemy, skill.TargetCount);

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
                DmgTakenIncrease = enemyDebuffs.Dmg_Taken_Increase,
                Vulnerability = enemyDebuffs.Vulnerability + config.TargetEnemy.Vulnerability,
                BossVulnerability = enemyDebuffs.Boss_Vulnerability,
                // 시뮬에서는 치명/약점을 확률적으로 처리하거나 항상 발동으로 설정
                IsCritical = true,
                IsWeakpoint = true,
                IsSkillConditionMet = true,
                Mode = BattleMode.Boss,
                IsTargetBoss = config.TargetEnemy?.IsBoss ?? true,
                SelfMaxHp = charState.MaxHp
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
        /// 보스에 걸린 디버프 합산
        /// </summary>
        private DebuffSet AggregateEnemyDebuffs(BattleState state)
        {
            var total = new DebuffSet();
            foreach (var debuff in state.EnemyState.ActiveDebuffs)
            {
                if (debuff.IsPermanent || debuff.RemainingTurns > 0)
                {
                    total.Def_Reduction = Math.Max(total.Def_Reduction, debuff.Debuff.Def_Reduction);
                    total.Dmg_Taken_Increase = Math.Max(total.Dmg_Taken_Increase, debuff.Debuff.Dmg_Taken_Increase);
                    total.Vulnerability = Math.Max(total.Vulnerability, debuff.Debuff.Vulnerability);
                    total.Boss_Vulnerability = Math.Max(total.Boss_Vulnerability, debuff.Debuff.Boss_Vulnerability);
                }
            }
            return total;
        }

        /// <summary>
        /// 타겟 수에 따른 보스 피해감소율
        /// </summary>
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
            // 버프/디버프 지속시간 감소 (실제 턴 변경 시에만)
            // 스킬은 턴 소모하지 않으므로, 기본공격 시에만 처리
            if (!action.IsSkill)
            {
                // 아군 버프 지속시간 감소
                foreach (var charState in state.AllyStates)
                {
                    charState.ActiveBuffs.RemoveAll(b => !b.IsPermanent && --b.RemainingTurns <= 0);
                }

                // 보스 디버프 지속시간 감소
                state.EnemyState.ActiveDebuffs.RemoveAll(d => !d.IsPermanent && --d.RemainingTurns <= 0);
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

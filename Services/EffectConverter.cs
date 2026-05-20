using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 기존 Skill/Passive/Pet 정의를 BattleEffect로 변환
    /// CharacterDB 형식을 유지하면서 통합 Effect 시스템 사용 가능
    /// </summary>
    public static class EffectConverter
    {
        #region Passive → BattleEffect

        /// <summary>
        /// 패시브의 모든 효과를 BattleEffect 리스트로 변환
        /// 새 Effects 리스트가 있으면 우선 사용, 없으면 레거시 필드 사용
        /// </summary>
        public static List<BattleEffect> FromPassive(
            Passive passive, string characterName,
            bool isEnhanced, int transcendLevel,
            bool isConditionMet)
        {
            var effects = new List<BattleEffect>();
            if (passive == null) return effects;

            var levelData = passive.GetLevelData(isEnhanced);

            // 새 Effects 리스트가 있으면 우선 사용
            if (levelData.Effects != null && levelData.Effects.Count > 0)
            {
                effects.AddRange(FromPersistentEffects(levelData.Effects, characterName, isConditionMet));

                // 초월 보너스에 별도 Effects 리스트가 있으면 함께 변환
                var transcendNew = passive.GetTranscendBonus(transcendLevel);
                if (transcendNew?.Effects != null && transcendNew.Effects.Count > 0)
                {
                    effects.AddRange(FromPersistentEffects(transcendNew.Effects, characterName, isConditionMet));
                }
                return effects;
            }

            // === 레거시 필드 변환 ===

            // 상시 자버프
            var selfBuff = passive.GetTotalSelfBuff(isEnhanced, transcendLevel);
            if (selfBuff != null && !IsEmpty(selfBuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"passive_self:{characterName}",
                    SourceName = characterName,
                    Category = EffectCategory.PassiveSelfBuff,
                    Target = EffectTarget.Self,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = true,
                    BuffValues = selfBuff
                });
            }

            // 상시 파티버프
            var partyBuff = passive.GetPartyBuff(isEnhanced, transcendLevel);
            if (partyBuff != null && !IsEmpty(partyBuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"passive_party:{characterName}",
                    SourceName = characterName,
                    Category = EffectCategory.PassivePartyBuff,
                    Target = EffectTarget.Party,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = true,
                    BuffValues = partyBuff
                });
            }

            // 상시 디버프
            var debuff = passive.GetDebuff(isEnhanced, transcendLevel);
            if (debuff != null && !IsEmptyDebuff(debuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"passive_debuff:{characterName}",
                    SourceName = characterName,
                    Category = EffectCategory.PassiveDebuff,
                    Target = EffectTarget.Enemy,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = true,
                    DebuffValues = debuff
                });
            }

            // 조건부 효과
            if (isConditionMet)
            {
                var condSelf = passive.GetConditionalSelfBuff(isEnhanced, transcendLevel);
                if (condSelf != null && !IsEmpty(condSelf))
                {
                    effects.Add(new BattleEffect
                    {
                        Id = $"passive_cond_self:{characterName}",
                        SourceName = characterName,
                        Category = EffectCategory.ConditionalSelfBuff,
                        Target = EffectTarget.Self,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = 99,
                        BuffValues = condSelf
                    });
                }

                var condParty = passive.GetConditionalPartyBuff(isEnhanced, transcendLevel);
                if (condParty != null && !IsEmpty(condParty))
                {
                    effects.Add(new BattleEffect
                    {
                        Id = $"passive_cond_party:{characterName}",
                        SourceName = characterName,
                        Category = EffectCategory.ConditionalPartyBuff,
                        Target = EffectTarget.Party,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = 99,
                        BuffValues = condParty
                    });
                }

                var condDebuff = passive.GetConditionalDebuff(isEnhanced, transcendLevel);
                if (condDebuff != null && !IsEmptyDebuff(condDebuff))
                {
                    effects.Add(new BattleEffect
                    {
                        Id = $"passive_cond_debuff:{characterName}",
                        SourceName = characterName,
                        Category = EffectCategory.ConditionalDebuff,
                        Target = EffectTarget.Enemy,
                        MergeStrategy = MergeStrategy.MaxMerge,
                        IsPermanent = false,
                        RemainingTurns = 99,
                        DebuffValues = condDebuff
                    });
                }
            }

            // 패시브 상태이상
            if (levelData.StatusEffects != null)
            {
                foreach (var se in levelData.StatusEffects)
                {
                    effects.Add(FromSkillStatusEffect(se, characterName, passive.Name));
                }
            }

            return effects;
        }

        #endregion

        #region Skill → BattleEffect

        /// <summary>
        /// 스킬의 모든 효과를 BattleEffect 리스트로 변환
        /// 새 Effects 리스트가 있으면 우선 사용, 없으면 레거시 필드 사용
        /// </summary>
        public static List<BattleEffect> FromSkill(
            Skill skill, string characterName,
            bool isEnhanced, int transcendLevel)
        {
            var effects = new List<BattleEffect>();
            if (skill == null) return effects;

            var levelData = skill.GetLevelData(isEnhanced);
            var totalBonus = skill.GetTotalBonus(isEnhanced, transcendLevel);

            // 새 Effects가 있으면 우선 사용 (Bonus는 별도 유지)
            if (levelData?.Effects != null && levelData.Effects.Count > 0)
            {
                // Bonus는 항상 레거시 방식 유지
                if (totalBonus != null && !IsEmpty(totalBonus))
                {
                    effects.Add(new BattleEffect
                    {
                        Id = $"skill_bonus:{characterName}:{skill.Name}",
                        SourceName = characterName,
                        Category = EffectCategory.SkillBonus,
                        Target = EffectTarget.Self,
                        MergeStrategy = MergeStrategy.Additive,
                        IsPermanent = false,
                        RemainingTurns = 0,
                        BuffValues = totalBonus
                    });
                }

                effects.AddRange(FromSkillEffects(levelData.Effects, characterName, skill.Name));

                // 초월 Effects도 추가
                var txBonus = skill.GetTranscendBonus(transcendLevel);
                if (txBonus?.Effects != null && txBonus.Effects.Count > 0)
                {
                    effects.AddRange(FromSkillEffects(txBonus.Effects, characterName, $"{skill.Name}(초월)"));
                }

                return effects;
            }

            // === 레거시 필드 변환 ===

            // 스킬 보너스 (해당 스킬 데미지 계산에만 적용)
            if (totalBonus != null && !IsEmpty(totalBonus))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_bonus:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.SkillBonus,
                    Target = EffectTarget.Self,
                    MergeStrategy = MergeStrategy.Additive,
                    IsPermanent = false,
                    RemainingTurns = 0, // 즉시 소멸 (계산에만 사용)
                    BuffValues = totalBonus
                });
            }

            // 스킬 자버프
            if (levelData?.SelfBuff != null && !IsEmpty(levelData.SelfBuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_self:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.ActiveSelfBuff,
                    Target = EffectTarget.Self,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = false,
                    RemainingTurns = 3, // 기본 3턴 (스킬별 다를 수 있음)
                    BuffValues = levelData.SelfBuff
                });
            }

            // 스킬 파티버프
            if (levelData?.PartyBuff != null && !IsEmpty(levelData.PartyBuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_party:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.ActivePartyBuff,
                    Target = EffectTarget.Party,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = false,
                    RemainingTurns = 3,
                    BuffValues = levelData.PartyBuff
                });
            }

            // 스킬 디버프
            if (levelData?.DebuffEffect != null && !IsEmptyDebuff(levelData.DebuffEffect))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_debuff:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.ActiveDebuff,
                    Target = EffectTarget.Enemy,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = false,
                    RemainingTurns = 3,
                    DebuffValues = levelData.DebuffEffect
                });
            }

            // 초월 보너스 파티버프/디버프
            var transcendBonus = skill.GetTranscendBonus(transcendLevel);
            if (transcendBonus?.PartyBuff != null && !IsEmpty(transcendBonus.PartyBuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_transcend_party:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.ActivePartyBuff,
                    Target = EffectTarget.Party,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = false,
                    RemainingTurns = 3,
                    BuffValues = transcendBonus.PartyBuff
                });
            }

            if (transcendBonus?.Debuff != null && !IsEmptyDebuff(transcendBonus.Debuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"skill_transcend_debuff:{characterName}:{skill.Name}",
                    SourceName = characterName,
                    Category = EffectCategory.ActiveDebuff,
                    Target = EffectTarget.Enemy,
                    MergeStrategy = MergeStrategy.MaxMerge,
                    IsPermanent = false,
                    RemainingTurns = 3,
                    DebuffValues = transcendBonus.Debuff
                });
            }

            // 상태이상
            if (levelData?.StatusEffects != null)
            {
                foreach (var se in levelData.StatusEffects)
                {
                    effects.Add(FromSkillStatusEffect(se, characterName, skill.Name));
                }
            }

            // 초월 상태이상
            if (transcendBonus?.StatusEffects != null)
            {
                foreach (var se in transcendBonus.StatusEffects)
                {
                    effects.Add(FromSkillStatusEffect(se, characterName, $"{skill.Name}(초월)"));
                }
            }

            return effects;
        }

        #endregion

        #region Pet → BattleEffect

        /// <summary>
        /// 펫 효과를 BattleEffect 리스트로 변환
        /// </summary>
        public static List<BattleEffect> FromPet(Pet pet, int petStar)
        {
            var effects = new List<BattleEffect>();
            if (pet == null) return effects;

            var buff = pet.GetSkillBuff(petStar);
            if (buff != null && !IsEmpty(buff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"pet_buff:{pet.Name}",
                    SourceName = pet.Name,
                    Category = EffectCategory.PetBuff,
                    Target = EffectTarget.Party,
                    MergeStrategy = MergeStrategy.Additive,
                    IsPermanent = true,
                    BuffValues = buff
                });
            }

            var debuff = pet.GetSkillDebuff(petStar);
            if (debuff != null && !IsEmptyDebuff(debuff))
            {
                effects.Add(new BattleEffect
                {
                    Id = $"pet_debuff:{pet.Name}",
                    SourceName = pet.Name,
                    Category = EffectCategory.PetDebuff,
                    Target = EffectTarget.Enemy,
                    MergeStrategy = MergeStrategy.Additive,
                    IsPermanent = true,
                    DebuffValues = debuff
                });
            }

            return effects;
        }

        #endregion

        #region 새 모델 변환

        /// <summary>
        /// SkillEffect 리스트 → BattleEffect 리스트 (스킬 턴제 효과)
        /// </summary>
        private static List<BattleEffect> FromSkillEffects(
            List<SkillEffect> skillEffects, string characterName, string skillName)
        {
            var results = new List<BattleEffect>();
            if (skillEffects == null) return results;

            foreach (var se in skillEffects)
            {
                var effect = new BattleEffect
                {
                    SourceName = characterName,
                    IsPermanent = false,
                    RemainingTurns = se.Duration > 0 ? se.Duration : 99,
                    ApplyChance = se.Chance,
                };

                // 대상 매핑
                effect.Target = se.Target;

                switch (se.Type)
                {
                    case SkillEffectType.Buff:
                        effect.Id = $"skill_effect_buff:{characterName}:{skillName}:{se.Target}";
                        effect.Category = se.Target == EffectTarget.Self
                            ? EffectCategory.ActiveSelfBuff
                            : EffectCategory.ActivePartyBuff;
                        effect.MergeStrategy = MergeStrategy.MaxMerge;
                        effect.BuffValues = se.Buff;
                        break;

                    case SkillEffectType.Debuff:
                        effect.Id = $"skill_effect_debuff:{characterName}:{skillName}";
                        effect.Category = EffectCategory.ActiveDebuff;
                        effect.MergeStrategy = MergeStrategy.MaxMerge;
                        effect.DebuffValues = se.Debuff;
                        break;

                    case SkillEffectType.StatusAilment:
                        effect.Id = $"skill_effect_status:{characterName}:{skillName}:{se.StatusType}";
                        effect.Category = ResolveStatusCategory(se.StatusType);
                        effect.MergeStrategy = MergeStrategy.Stack;
                        effect.StatusType = se.StatusType;
                        effect.Stacks = se.Stacks;
                        effect.MaxStacks = StatusEffectDb.Get(se.StatusType)?.MaxStacks ?? 1;
                        // 커스텀 오버라이드 → StatusEffectData
                        var baseEffect = StatusEffectDb.Get(se.StatusType);
                        var data = StatusEffectData.FromDbEffect(baseEffect);
                        if (se.CustomAtkRatio.HasValue) data.AtkRatio = se.CustomAtkRatio.Value;
                        if (se.CustomHpRatio.HasValue) data.TargetMaxHpRatio = se.CustomHpRatio.Value;
                        if (se.CustomTargetMaxHpRatio.HasValue) data.TargetMaxHpRatio = se.CustomTargetMaxHpRatio.Value;
                        if (se.CustomAtkCap.HasValue) data.AtkCap = se.CustomAtkCap.Value;
                        if (se.CustomArmorPen.HasValue) data.ArmorPen = se.CustomArmorPen.Value;
                        if (se.CustomFixedDamage.HasValue) data.FixedDamage = se.CustomFixedDamage.Value;
                        if (se.MaxConsume > 0) data.MaxConsume = se.MaxConsume;
                        effect.StatusData = data;
                        effect.RemainingTurns = se.Duration > 0 ? se.Duration : baseEffect?.Duration ?? 2;
                        break;

                    case SkillEffectType.PerEnemyDebuffDmgBonus:
                        // BattleEffect로 변환 안 함 — PerDebuffBonusExtractor가 동적으로 소비
                        continue;
                }

                results.Add(effect);
            }

            return results;
        }

        /// <summary>
        /// PersistentEffect 리스트 → BattleEffect 리스트 (패시브 지속 효과)
        /// </summary>
        private static List<BattleEffect> FromPersistentEffects(
            List<PersistentEffect> persistentEffects, string characterName, bool isConditionMet)
        {
            var results = new List<BattleEffect>();
            if (persistentEffects == null) return results;

            foreach (var pe in persistentEffects)
            {
                // 조건부 효과인데 조건 미충족이면 스킵
                if (pe.IsConditional && !isConditionMet)
                    continue;

                var effect = new BattleEffect
                {
                    SourceName = characterName,
                    Target = pe.Target,
                    IsPermanent = !pe.IsConditional,
                    RemainingTurns = pe.IsConditional ? 99 : -1,
                };

                switch (pe.Type)
                {
                    case PersistentEffectType.Buff:
                        bool isSelf = pe.Target == EffectTarget.Self;
                        effect.Id = $"persistent_buff:{characterName}:{pe.Target}:{(pe.IsConditional ? "cond" : "perm")}";
                        effect.Category = pe.IsConditional
                            ? (isSelf ? EffectCategory.ConditionalSelfBuff : EffectCategory.ConditionalPartyBuff)
                            : (isSelf ? EffectCategory.PassiveSelfBuff : EffectCategory.PassivePartyBuff);
                        effect.MergeStrategy = MergeStrategy.MaxMerge;
                        effect.BuffValues = pe.Buff;
                        break;

                    case PersistentEffectType.Debuff:
                        effect.Id = $"persistent_debuff:{characterName}:{(pe.IsConditional ? "cond" : "perm")}";
                        effect.Category = pe.IsConditional
                            ? EffectCategory.ConditionalDebuff
                            : EffectCategory.PassiveDebuff;
                        effect.MergeStrategy = MergeStrategy.MaxMerge;
                        effect.DebuffValues = pe.Debuff;
                        break;

                    case PersistentEffectType.StatusAilment:
                        effect.Id = $"persistent_status:{characterName}:{pe.StatusType}";
                        effect.Category = ResolveStatusCategory(pe.StatusType);
                        effect.MergeStrategy = MergeStrategy.Stack;
                        effect.StatusType = pe.StatusType;
                        effect.Stacks = pe.Stacks;
                        effect.StatusData = ResolvePersistentStatusData(StatusEffectDb.Get(pe.StatusType), pe);
                        effect.ApplyChance = pe.Chance;
                        break;

                    case PersistentEffectType.CoopAttack:
                    case PersistentEffectType.MarkAttack:
                    case PersistentEffectType.StatScaling:
                    case PersistentEffectType.PainEndurance:
                    case PersistentEffectType.FlatBonus:
                    case PersistentEffectType.PerEnemyDebuffDmgBonus:
                        // 의도된 스킵 — 이 메카닉들은 BuffSet/DebuffSet/StatusType 추상화에
                        // 안 맞으므로 BattleEffect로 변환하지 않는다.
                        // 소비 위치:
                        //   CoopAttack/MarkAttack       → DamageCalculator
                        //   StatScaling/FlatBonus       → StatCalculator
                        //   PainEndurance               → 받피해 처리 경로 (현재 미연결)
                        //   PerEnemyDebuffDmgBonus      → PerDebuffBonusExtractor가 동적으로 소비
                        // 데이터는 Passive.LevelData의 fallback getter로 노출된다.
                        continue;
                }

                results.Add(effect);
            }

            return results;
        }

        #endregion

        #region SkillStatusEffect → BattleEffect (레거시)

        private static BattleEffect FromSkillStatusEffect(
            SkillStatusEffect sse, string characterName, string skillName)
        {
            var baseEffect = StatusEffectDb.Get(sse.Type);
            var category = ResolveStatusCategory(sse.Type);

            return new BattleEffect
            {
                Id = $"status:{characterName}:{skillName}:{sse.Type}",
                SourceName = characterName,
                Category = category,
                Target = EffectTarget.Enemy,
                MergeStrategy = MergeStrategy.Stack,
                IsPermanent = false,
                RemainingTurns = sse.Duration > 0 ? sse.Duration : baseEffect?.Duration ?? 2,
                StatusType = sse.Type,
                StatusData = ResolveStatusData(baseEffect, sse),
                Stacks = sse.Stacks,
                MaxStacks = baseEffect?.MaxStacks ?? 1,
                ApplyChance = sse.Chance
            };
        }

        private static StatusEffectData ResolveStatusData(StatusEffect baseEffect, SkillStatusEffect sse)
        {
            var data = StatusEffectData.FromDbEffect(baseEffect);
            // 스킬별 커스텀 오버라이드 적용
            if (sse.CustomAtkRatio.HasValue) data.AtkRatio = sse.CustomAtkRatio.Value;
            if (sse.CustomHpRatio.HasValue) data.TargetMaxHpRatio = sse.CustomHpRatio.Value;
            if (sse.CustomTargetMaxHpRatio.HasValue) data.TargetMaxHpRatio = sse.CustomTargetMaxHpRatio.Value;
            if (sse.CustomAtkCap.HasValue) data.AtkCap = sse.CustomAtkCap.Value;
            if (sse.CustomArmorPen.HasValue) data.ArmorPen = sse.CustomArmorPen.Value;
            if (sse.CustomFixedDamage.HasValue) data.FixedDamage = sse.CustomFixedDamage.Value;
            if (sse.MaxConsume > 0) data.MaxConsume = sse.MaxConsume;
            return data;
        }

        private static StatusEffectData ResolvePersistentStatusData(StatusEffect baseEffect, PersistentEffect pe)
        {
            var data = StatusEffectData.FromDbEffect(baseEffect);
            if (pe.CustomAtkRatio.HasValue) data.AtkRatio = pe.CustomAtkRatio.Value;
            if (pe.CustomHpRatio.HasValue) data.TargetMaxHpRatio = pe.CustomHpRatio.Value;
            if (pe.CustomTargetMaxHpRatio.HasValue) data.TargetMaxHpRatio = pe.CustomTargetMaxHpRatio.Value;
            if (pe.CustomAtkCap.HasValue) data.AtkCap = pe.CustomAtkCap.Value;
            if (pe.CustomArmorPen.HasValue) data.ArmorPen = pe.CustomArmorPen.Value;
            if (pe.CustomFixedDamage.HasValue) data.FixedDamage = pe.CustomFixedDamage.Value;
            if (pe.MaxConsume > 0) data.MaxConsume = pe.MaxConsume;
            return data;
        }

        private static EffectCategory ResolveStatusCategory(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun or StatusEffectType.Silence or StatusEffectType.Freeze
                    or StatusEffectType.Petrify or StatusEffectType.IceExtreme or StatusEffectType.Paralysis
                    or StatusEffectType.Shock or StatusEffectType.Sleep or StatusEffectType.Confusion
                    or StatusEffectType.Concussion => EffectCategory.CrowdControl,

                StatusEffectType.Burn or StatusEffectType.Bleeding or StatusEffectType.Poison
                    or StatusEffectType.InstantDeath or StatusEffectType.ManaBackflow
                    or StatusEffectType.ChainDamage => EffectCategory.DamageOverTime,

                _ => EffectCategory.SpecialStatus
            };
        }

        #endregion

        #region BuffConfig → BattleEffect (UI 호환)

        /// <summary>
        /// UI의 BuffConfig 리스트 + 펫을 BattleEffect 리스트로 변환.
        /// EffectManager에 흘려보내면 GetSeparatedBuffs/GetTotalBuffs/GetTotalDebuffs로 집계 가능.
        /// </summary>
        public static List<BattleEffect> FromBuffConfigs(
            IEnumerable<BuffConfig> buffConfigs, Pet pet, int petStar)
        {
            var effects = new List<BattleEffect>();

            foreach (var config in buffConfigs)
            {
                if (!config.IsChecked) continue;

                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = Database.CharacterDb.GetByName(config.CharacterName);
                if (character == null) continue;

                if (config.SkillName == null)
                {
                    // 패시브 효과
                    if (config.IsBuff)
                    {
                        // 패시브 파티버프 (상시)
                        var partyBuff = character.Passive?.GetPartyBuff(isEnhanced, transcendLevel);
                        if (partyBuff != null && !IsEmpty(partyBuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"passive_party:{config.CharacterName}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.PassivePartyBuff,
                                Target = EffectTarget.Party,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = true,
                                BuffValues = partyBuff
                            });
                        }

                        // 패시브 파티버프 (조건부)
                        var condPartyBuff = character.Passive?.GetConditionalPartyBuff(isEnhanced, transcendLevel);
                        if (condPartyBuff != null && !IsEmpty(condPartyBuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"passive_cond_party:{config.CharacterName}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ConditionalPartyBuff,
                                Target = EffectTarget.Party,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                BuffValues = condPartyBuff
                            });
                        }
                    }
                    else
                    {
                        // 패시브 디버프 (상시)
                        var debuff = character.Passive?.GetDebuff(isEnhanced, transcendLevel);
                        if (debuff != null && !IsEmptyDebuff(debuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"passive_debuff:{config.CharacterName}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.PassiveDebuff,
                                Target = EffectTarget.Enemy,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = true,
                                DebuffValues = debuff
                            });
                        }

                        // 패시브 디버프 (조건부)
                        var condDebuff = character.Passive?.GetConditionalDebuff(isEnhanced, transcendLevel);
                        if (condDebuff != null && !IsEmptyDebuff(condDebuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"passive_cond_debuff:{config.CharacterName}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ConditionalDebuff,
                                Target = EffectTarget.Enemy,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                DebuffValues = condDebuff
                            });
                        }
                    }
                }
                else
                {
                    // 스킬 효과
                    var skill = character.Skills?.FirstOrDefault(s => s.Name == config.SkillName);
                    if (skill == null) continue;

                    if (config.IsBuff)
                    {
                        // 스킬 파티버프
                        var levelData = skill.GetLevelData(isEnhanced);
                        if (levelData?.PartyBuff != null && !IsEmpty(levelData.PartyBuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"skill_party:{config.CharacterName}:{skill.Name}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ActivePartyBuff,
                                Target = EffectTarget.Party,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                BuffValues = levelData.PartyBuff
                            });
                        }

                        // 초월 파티버프
                        var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                        if (transcendBonus?.PartyBuff != null && !IsEmpty(transcendBonus.PartyBuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"skill_transcend_party:{config.CharacterName}:{skill.Name}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ActivePartyBuff,
                                Target = EffectTarget.Party,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                BuffValues = transcendBonus.PartyBuff
                            });
                        }
                    }
                    else
                    {
                        // 스킬 디버프
                        var levelData = skill.GetLevelData(isEnhanced);
                        if (levelData?.DebuffEffect != null && !IsEmptyDebuff(levelData.DebuffEffect))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"skill_debuff:{config.CharacterName}:{skill.Name}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ActiveDebuff,
                                Target = EffectTarget.Enemy,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                DebuffValues = levelData.DebuffEffect
                            });
                        }

                        // 초월 디버프
                        var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                        if (transcendBonus?.Debuff != null && !IsEmptyDebuff(transcendBonus.Debuff))
                        {
                            effects.Add(new BattleEffect
                            {
                                Id = $"skill_transcend_debuff:{config.CharacterName}:{skill.Name}",
                                SourceName = config.CharacterName,
                                Category = EffectCategory.ActiveDebuff,
                                Target = EffectTarget.Enemy,
                                MergeStrategy = MergeStrategy.MaxMerge,
                                IsPermanent = false,
                                RemainingTurns = 99,
                                DebuffValues = transcendBonus.Debuff
                            });
                        }
                    }
                }
            }

            // 펫 효과
            effects.AddRange(FromPet(pet, petStar));

            return effects;
        }

        #endregion

        #region 유틸리티

        private static bool IsEmpty(BuffSet buff)
        {
            if (buff == null) return true;
            return buff.Atk_Rate == 0 && buff.MagicAtk_Rate == 0 && buff.Def_Rate == 0 && buff.Hp_Rate == 0
                && buff.Cri == 0 && buff.Cri_Dmg == 0 && buff.CriBonusDmg == 0
                && buff.Wek == 0 && buff.Wek_Dmg == 0 && buff.WekBonusDmg == 0
                && buff.Dmg_Dealt == 0 && buff.Dmg_Dealt_Type == 0 && buff.Dmg_Dealt_Bos == 0
                && buff.Dmg_Dealt_1to3 == 0 && buff.Dmg_Dealt_4to5 == 0
                && buff.Mark_Energeia == 0 && buff.Mark_Purify == 0
                && buff.Arm_Pen == 0 && buff.Dmg_Rdc == 0 && buff.Blk == 0
                && buff.Heal_Bonus == 0 && buff.Eff_Res == 0 && buff.Eff_Hit == 0
                && buff.Shield_HpRatio == 0 && buff.Blessing == 0
                && buff.Phys_Dmg_Rdc == 0 && buff.Mag_Dmg_Rdc == 0
                && buff.Dmg_Rdc_Multi == 0 && buff.Coop_Chance == 0;
        }

        private static bool IsEmptyDebuff(DebuffSet debuff)
        {
            if (debuff == null) return true;
            return debuff.Def_Reduction == 0 && debuff.Dmg_Taken_Increase == 0
                && debuff.Phys_Dmg_Taken_Increase == 0 && debuff.Mag_Dmg_Taken_Increase == 0
                && debuff.Vulnerability == 0 && debuff.Boss_Vulnerability == 0
                && debuff.Atk_Reduction == 0 && debuff.Spd_Reduction == 0
                && debuff.Dmg_Reduction == 0 && debuff.Cri_Dmg_Reduction == 0
                && debuff.Heal_Reduction == 0 && debuff.Unrecover == 0
                && debuff.Eff_Red == 0 && debuff.Eff_Hit_Red == 0 && debuff.Blk_Red == 0;
        }

        #endregion
    }
}

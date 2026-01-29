using System;
using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 버프/디버프 계산 서비스
    /// </summary>
    public class BuffCalculator
    {
        /// <summary>
        /// 전체 버프 합산 (상시 + 턴제 + 펫)
        /// </summary>
        public BuffSet CalculateTotalBuffs(
            IEnumerable<BuffConfig> buffConfigs,
            Pet pet,
            int petStar)
        {
            // 상시 버프 (패시브 상시끼리 MaxMerge)
            PermanentBuff permanentBuffs = GetPermanentPassiveBuffs(buffConfigs);

            // 턴제 버프 (패시브 조건부 + 액티브 스킬 끼리 MaxMerge)
            TimedBuff timedBuffs = new TimedBuff();
            timedBuffs.MaxMerge(GetTimedPassiveBuffs(buffConfigs));
            timedBuffs.MaxMerge(GetActiveBuffs(buffConfigs));

            // 펫 버프 (별도 합산)
            BuffSet petBuffs = pet?.GetSkillBuff(petStar) ?? new BuffSet();

            // 최종 합산
            BuffSet total = new BuffSet();
            total.Add(permanentBuffs);
            total.Add(timedBuffs);
            total.Add(petBuffs);

            return total;
        }

        /// <summary>
        /// 지속/턴제 분리된 파티 버프 반환
        /// </summary>
        public (BuffSet Permanent, BuffSet Timed) CalculateSeparatedPartyBuffs(
            IEnumerable<BuffConfig> buffConfigs,
            Pet pet,
            int petStar)
        {
            // 상시 버프 (패시브 상시끼리 MaxMerge)
            PermanentBuff permanentBuffs = GetPermanentPassiveBuffs(buffConfigs);

            // 턴제 버프 (패시브 조건부 + 액티브 스킬 끼리 MaxMerge)
            TimedBuff timedBuffs = new TimedBuff();
            timedBuffs.MaxMerge(GetTimedPassiveBuffs(buffConfigs));
            timedBuffs.MaxMerge(GetActiveBuffs(buffConfigs));

            // 펫 버프는 턴제로 취급
            BuffSet petBuffs = pet?.GetSkillBuff(petStar) ?? new BuffSet();

            BuffSet permanent = new BuffSet();
            permanent.Add(permanentBuffs);

            BuffSet timed = new BuffSet();
            timed.Add(timedBuffs);
            timed.Add(petBuffs);

            return (permanent, timed);
        }

        /// <summary>
        /// 전체 디버프 합산 (상시 + 턴제 + 펫)
        /// </summary>
        public DebuffSet CalculateTotalDebuffs(
            IEnumerable<BuffConfig> buffConfigs, 
            Pet pet, 
            int petStar)
        {
            // 상시 디버프 (패시브 상시끼리 MaxMerge)
            PermanentDebuff permanentDebuffs = GetPermanentPassiveDebuffs(buffConfigs);

            // 턴제 디버프 (패시브 조건부 + 액티브 스킬 끼리 MaxMerge)
            TimedDebuff timedDebuffs = new TimedDebuff();
            timedDebuffs.MaxMerge(GetTimedPassiveDebuffs(buffConfigs));
            timedDebuffs.MaxMerge(GetActiveDebuffs(buffConfigs));

            // 펫 디버프 (별도 합산)
            DebuffSet petDebuffs = pet?.GetSkillDebuff(petStar) ?? new DebuffSet();

            // 최종 합산
            DebuffSet total = new DebuffSet();
            total.Add(permanentDebuffs);
            total.Add(timedDebuffs);
            total.Add(petDebuffs);

            return total;
        }

        #region 버프 수집

        private PermanentBuff GetPermanentPassiveBuffs(IEnumerable<BuffConfig> configs)
        {
            PermanentBuff total = new PermanentBuff();
            
            foreach (var config in configs.Where(c => c.IsBuff && c.SkillName == null))
            {
                if (!config.IsChecked) continue;
                
                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var buff = character?.Passive?.GetPartyBuff(isEnhanced, transcendLevel);
                
                if (buff != null) total.MaxMerge(buff);
            }
            
            return total;
        }

        private TimedBuff GetTimedPassiveBuffs(IEnumerable<BuffConfig> configs)
        {
            TimedBuff total = new TimedBuff();
            
            foreach (var config in configs.Where(c => c.IsBuff && c.SkillName == null))
            {
                if (!config.IsChecked) continue;
                
                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var buff = character?.Passive?.GetConditionalPartyBuff(isEnhanced, transcendLevel);
                
                if (buff != null) total.MaxMerge(buff);
            }
            
            return total;
        }

        private TimedBuff GetActiveBuffs(IEnumerable<BuffConfig> configs)
        {
            TimedBuff total = new TimedBuff();
            
            foreach (var config in configs.Where(c => c.IsBuff && c.SkillName != null))
            {
                if (!config.IsChecked) continue;
                
                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var skill = character?.Skills?.FirstOrDefault(s => s.Name == config.SkillName);

                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.PartyBuff != null)
                        total.MaxMerge(levelData.PartyBuff);

                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.PartyBuff != null)
                        total.MaxMerge(transcendBonus.PartyBuff);
                }
            }
            
            return total;
        }

        #endregion

        #region 디버프 수집

        private PermanentDebuff GetPermanentPassiveDebuffs(IEnumerable<BuffConfig> configs)
        {
            PermanentDebuff total = new PermanentDebuff();
            
            foreach (var config in configs.Where(c => !c.IsBuff && c.SkillName == null))
            {
                if (!config.IsChecked) continue;
                
                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var debuff = character?.Passive?.GetDebuff(isEnhanced, transcendLevel);
                
                if (debuff != null) total.MaxMerge(debuff);
            }
            
            return total;
        }

        private TimedDebuff GetTimedPassiveDebuffs(IEnumerable<BuffConfig> configs)
        {
            TimedDebuff total = new TimedDebuff();
            
            foreach (var config in configs.Where(c => !c.IsBuff && c.SkillName == null))
            {
                if (!config.IsChecked) continue;
                
                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var debuff = character?.Passive?.GetConditionalDebuff(isEnhanced, transcendLevel);
                
                if (debuff != null) total.MaxMerge(debuff);
            }
            
            return total;
        }

        private TimedDebuff GetActiveDebuffs(IEnumerable<BuffConfig> configs)
        {
            TimedDebuff total = new TimedDebuff();
            
            foreach (var config in configs.Where(c => !c.IsBuff && c.SkillName != null))
            {
                if (!config.IsChecked) continue;

                var (isEnhanced, transcendLevel) = config.GetBuffOption();
                var character = CharacterDb.GetByName(config.CharacterName);
                var skill = character?.Skills?.FirstOrDefault(s => s.Name == config.SkillName);

                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.DebuffEffect != null)
                        total.MaxMerge(levelData.DebuffEffect);

                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.Debuff != null)
                        total.MaxMerge(transcendBonus.Debuff);
                }
            }
            
            return total;
        }

        #endregion
    }
}
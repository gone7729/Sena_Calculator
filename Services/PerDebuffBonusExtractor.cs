using System;
using System.Collections.Generic;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// 캐릭터의 패시브/스킬에서 "적 디버프 1개당 피해량 증가"(PerEnemyDebuffDmgBonus) 효과를 추출.
    ///
    /// 게임 룰:
    ///  - 패시브(상시 카테고리) 내부: MaxMerge — 한 효과의 PercentPerDebuff 가장 큰 것
    ///  - 스킬(턴제 카테고리) 내부: MaxMerge
    ///  - 카테고리 간 (패시브 + 스킬): Add — 합산
    /// MaxDebuffStacks는 모든 효과 중 최대값을 채택.
    /// </summary>
    public static class PerDebuffBonusExtractor
    {
        public readonly struct Result
        {
            public double PercentPerDebuff { get; }
            public int MaxStacks { get; }
            public Result(double percent, int max) { PercentPerDebuff = percent; MaxStacks = max; }
            public static Result Empty => new Result(0, 0);
            public bool IsActive => PercentPerDebuff > 0 && MaxStacks > 0;
        }

        /// <summary>
        /// 캐릭터(공격자)의 활성 패시브 + 사용 중인 스킬에서 동적 피증을 합산.
        /// </summary>
        public static Result From(Character character, Skill currentSkill,
            bool isEnhanced, int transcendLevel)
        {
            var passive = ExtractFromPassive(character?.Passive, isEnhanced, transcendLevel);
            var skill = ExtractFromSkill(currentSkill, isEnhanced, transcendLevel);

            return new Result(
                passive.PercentPerDebuff + skill.PercentPerDebuff,
                Math.Max(passive.MaxStacks, skill.MaxStacks));
        }

        private static Result ExtractFromPassive(Passive passive, bool isEnhanced, int transcendLevel)
        {
            if (passive == null) return Result.Empty;

            double percent = 0;
            int max = 0;

            var levelData = passive.GetLevelData(isEnhanced);
            ScanPersistent(levelData?.Effects, ref percent, ref max);

            var transcend = passive.GetTranscendBonus(transcendLevel);
            ScanPersistent(transcend?.Effects, ref percent, ref max);

            return new Result(percent, max);
        }

        private static Result ExtractFromSkill(Skill skill, bool isEnhanced, int transcendLevel)
        {
            if (skill == null) return Result.Empty;

            double percent = 0;
            int max = 0;

            var levelData = skill.GetLevelData(isEnhanced);
            ScanSkill(levelData?.Effects, ref percent, ref max);

            var transcend = skill.GetTranscendBonus(transcendLevel);
            ScanSkill(transcend?.Effects, ref percent, ref max);

            return new Result(percent, max);
        }

        private static void ScanPersistent(List<PersistentEffect> effects, ref double percent, ref int max)
        {
            if (effects == null) return;
            foreach (var e in effects)
            {
                if (e.Type != PersistentEffectType.PerEnemyDebuffDmgBonus) continue;
                // 시전자 본인/파티 한정 (적 대상 효과는 의미 없음)
                if (e.Target != EffectTarget.Self && e.Target != EffectTarget.Party && e.Target != EffectTarget.SingleAlly)
                    continue;
                if (e.PercentPerDebuff > percent) percent = e.PercentPerDebuff;
                if (e.MaxDebuffStacks > max) max = e.MaxDebuffStacks;
            }
        }

        private static void ScanSkill(List<SkillEffect> effects, ref double percent, ref int max)
        {
            if (effects == null) return;
            foreach (var e in effects)
            {
                if (e.Type != SkillEffectType.PerEnemyDebuffDmgBonus) continue;
                if (e.Target != EffectTarget.Self && e.Target != EffectTarget.Party && e.Target != EffectTarget.SingleAlly)
                    continue;
                if (e.PercentPerDebuff > percent) percent = e.PercentPerDebuff;
                if (e.MaxDebuffStacks > max) max = e.MaxDebuffStacks;
            }
        }

        /// <summary>
        /// 단순 합산 헬퍼: 디버프 개수와 효과를 받아 최종 피증% 반환.
        /// </summary>
        public static double Resolve(Result result, int debuffCount)
        {
            if (!result.IsActive || debuffCount <= 0) return 0;
            int effectiveCount = Math.Min(debuffCount, result.MaxStacks);
            return result.PercentPerDebuff * effectiveCount;
        }
    }

    /// <summary>
    /// DebuffSet에서 게임 표기 기준 활성 디버프 필드 수 카운트.
    /// MainWindow 단독 계산 시 사용 (BattleSimulator는 EffectManager.GetActiveDebuffCount 사용).
    /// </summary>
    public static class DebuffCounter
    {
        public static int CountFields(DebuffSet d)
        {
            if (d == null) return 0;
            int n = 0;
            if (d.Def_Reduction > 0) n++;
            if (d.Atk_Reduction > 0) n++;
            if (d.Spd_Reduction > 0) n++;
            if (d.Dmg_Reduction > 0) n++;
            if (d.Cri_Dmg_Reduction > 0) n++;
            if (d.Heal_Reduction > 0) n++;
            if (d.Unrecover > 0) n++;
            if (d.Eff_Red > 0) n++;
            if (d.Eff_Hit_Red > 0) n++;
            if (d.Blk_Red > 0) n++;
            if (d.Dmg_Taken_Increase > 0) n++;
            if (d.Vulnerability > 0) n++;
            if (d.Boss_Vulnerability > 0) n++;
            return n;
        }
    }
}

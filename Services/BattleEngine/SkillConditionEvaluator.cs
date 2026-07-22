using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 조건부 효과(추가피해·배율보너스·피증)의 발동 조건 판정.
    ///
    /// 전에는 시뮬이 IsSkillConditionMet=true로 하드코딩해 조건부 피해가 항상 터졌다.
    /// (예: 타카 죽음의 무도 +260%가 보스 풀피에도 발동 → 딜 과대평가.
    ///  실측 기록상 게임은 조건을 게이팅한다 — SIEGE_FORMULA_CALIBRATION.md의 "룩(조건 미발동)".)
    ///
    /// Condition이 null이면 true를 반환한다 — 조건 미입력 스킬은 종전대로 항상 발동하므로,
    /// DB를 한 번에 다 채우지 않아도 회귀가 생기지 않는다.
    /// </summary>
    public static class SkillConditionEvaluator
    {
        /// <summary>아군이 적을 때릴 때의 조건 판정. state는 "단일 적군" 판정에만 쓰인다(없으면 그 조건은 false).</summary>
        public static bool IsMet(SkillCondition cond, CharacterBattleState attacker, SiegeEnemyState target,
            SiegeBattleState state = null)
        {
            if (cond == null || cond.Type == SkillConditionType.None) return true;

            switch (cond.Type)
            {
                case SkillConditionType.TargetHpAtOrBelow:
                    return HpPct(target) <= cond.Threshold;
                case SkillConditionType.TargetHpBelow:
                    return HpPct(target) < cond.Threshold;
                case SkillConditionType.TargetHpAtOrAbove:
                    return HpPct(target) >= cond.Threshold;

                case SkillConditionType.SelfHpAtOrBelow:
                    return attacker != null && attacker.MaxHp > 0
                        && attacker.CurrentHp / attacker.MaxHp * 100.0 <= cond.Threshold;

                case SkillConditionType.TargetHasStatus:
                    return target?.Effects != null && cond.Status != StatusEffectType.None
                        && target.Effects.GetTotalStacks(cond.Status) > 0;

                case SkillConditionType.SelfHasStatus:
                    return attacker?.Effects != null && cond.Status != StatusEffectType.None
                        && attacker.Effects.GetTotalStacks(cond.Status) > 0;

                case SkillConditionType.TargetStatLowerThanSelf:
                    return CompareStat(cond.Stat, attacker, target);

                case SkillConditionType.TargetHasDefenseBuff:
                    // 적의 방어력 증가 버프 — 턴제 적버프(EnemyBuff.Def_Rate)와 스택형 방어 증가(보스 고유) 둘 다 인정.
                    if (target == null) return false;
                    if (target.HasEnemyBuff && target.EnemyBuff?.Def_Rate > 0) return true;
                    return target.Source != null && target.Source.DefenseIncrease > 0;

                case SkillConditionType.SingleEnemy:
                    // 살아있는(HP>0) 적이 1명뿐. 상태를 모르면 판정 불가 → 미발동으로 안전하게.
                    if (state?.Enemies == null) return false;
                    int alive = 0;
                    foreach (var e in state.Enemies)
                        if (e != null && e.CurrentHp > 0) alive++;
                    return alive <= 1;

                default:
                    return true;
            }
        }

        /// <summary>대상 잔여 생명력%. 공성 R3 보스는 HP가 음수까지 내려가므로 0으로 클램프.</summary>
        private static double HpPct(SiegeEnemyState target)
        {
            if (target == null || target.MaxHp <= 0) return 0;
            double pct = target.CurrentHp / target.MaxHp * 100.0;
            return pct < 0 ? 0 : pct;
        }

        /// <summary>대상 스탯 &lt; 자신 스탯 인지. 비교 불가한 스탯이면 false(미발동).</summary>
        private static bool CompareStat(StatType stat, CharacterBattleState attacker, SiegeEnemyState target)
        {
            if (attacker == null || target == null) return false;
            switch (stat)
            {
                case StatType.Atk: return target.FinalAtk < attacker.FinalAtk;
                case StatType.Def: return target.FinalDef < attacker.FinalDef;
                case StatType.Spd: return target.FinalSpd < attacker.FinalSpd;
                default: return false;
            }
        }
    }
}

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
                    if (target == null || cond.Status == StatusEffectType.None) return false;
                    // 공성 시뮬의 적 DoT(화상·출혈·중독)는 Effects가 아니라 ActiveDots에 등록된다 → 둘 다 확인.
                    //   (2026-11-17 수정: 예전엔 Effects만 봐서 샤오 출혈 조건·동영 중독 조건이 공성에서 항상 미충족)
                    if (target.ActiveDots != null)
                        foreach (var d in target.ActiveDots)
                            if (d.Type == cond.Status && d.RemainingTurns > 0) return true;
                    return target.Effects != null && target.Effects.GetTotalStacks(cond.Status) > 0;

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

                case SkillConditionType.TargetTopAtkEnemy:
                {
                    // 대상이 생존 적 중 공격력 상위 N명 안 (쥬다스 칠흑의 낙인 = 상위 3명). 적이 N명 이하면 항상 충족.
                    if (target == null) return false;
                    if (state?.Enemies == null) return true;
                    int n = cond.Threshold > 0 ? (int)cond.Threshold : 3;
                    int higher = 0;
                    foreach (var e in state.Enemies)
                        if (e != null && e != target && e.FinalAtk > target.FinalAtk) higher++;
                    return higher < n;
                }

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
        /// <summary>조건 충족 확률 — TargetHasStatus는 기댓값 DoT 인스턴스들의 "하나라도 걸려 있을" 확률 1−Π(1−p).
        ///   그 외 조건은 IsMet 결과(0/1).</summary>
        public static double MetProbability(SkillCondition cond, CharacterBattleState attacker, SiegeEnemyState target,
            SiegeBattleState state = null)
        {
            if (cond == null || cond.Type != SkillConditionType.TargetHasStatus)
                return IsMet(cond, attacker, target, state) ? 1.0 : 0.0;
            if (target == null || cond.Status == StatusEffectType.None) return 0;
            if (target.Effects != null && target.Effects.GetTotalStacks(cond.Status) > 0) return 1.0;
            double none = 1.0;
            if (target.ActiveDots != null)
                foreach (var d in target.ActiveDots)
                    if (d.Type == cond.Status && d.RemainingTurns > 0) none *= 1 - System.Math.Clamp(d.Probability, 0, 1);
            return 1 - none;
        }

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

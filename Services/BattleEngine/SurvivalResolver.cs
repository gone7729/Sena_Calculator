using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 사망 시 생존 메카닉(부활·권능·피해무효화) 판정 — 단일보스전/공성전 공유.
    /// CharacterBattleState + 캐릭터 패시브 기반이라 BattleState 의존이 없다(로그는 SurvivalResult로 반환,
    /// 호출 측이 자기 로그 형식에 맞게 기록). DamageCalculator를 공유하듯 생존 로직도 공유한다.
    /// </summary>
    public static class SurvivalResolver
    {
        public sealed class SurvivalEffects
        {
            public Revival Revival;
            public Authority Authority;
            public DamageNullification Nullification;
        }

        public sealed class SurvivalResult
        {
            public bool Survived;
            public string Label;        // 로그용 (불굴/불사·권능·부활·사망)
            public string Description;
        }

        /// <summary>캐릭터 패시브(레벨+초월) + 장착 장신구에서 부활/권능/피해무효화를 조회 (없으면 빈 결과).
        /// 패시브가 우선이고, 장신구(권능/부활/불사 반지)는 패시브에 없는 슬롯만 보강한다.</summary>
        public static SurvivalEffects GetPassiveSurvival(CharacterBattleState charState)
        {
            var result = new SurvivalEffects();
            // fillOnly=false면 덮어쓰기(패시브 lvl/초월 last-wins), true면 빈 슬롯만 채움(장신구는 패시브 보강).
            void Scan(IEnumerable<PersistentEffect> effects, bool fillOnly)
            {
                if (effects == null) return;
                foreach (var e in effects)
                {
                    if (e.Type == PersistentEffectType.Revival && e.Revival != null && (!fillOnly || result.Revival == null)) result.Revival = e.Revival;
                    else if (e.Type == PersistentEffectType.Authority && e.Authority != null && (!fillOnly || result.Authority == null)) result.Authority = e.Authority;
                    else if (e.Type == PersistentEffectType.DamageNullification && e.DamageNullification != null && (!fillOnly || result.Nullification == null)) result.Nullification = e.DamageNullification;
                }
            }

            var passive = charState.Source.Character.Passive;
            if (passive != null)
            {
                Scan(passive.GetLevelData(charState.Source.IsSkillEnhanced, charState.Source.IsAwakened)?.Effects, false);
                Scan(passive.GetTranscendBonus(charState.Source.TranscendLevel)?.Effects, false);
            }

            // 장착 장신구(권능/부활/불사 반지)의 생존효과 — 패시브에 없을 때만 보강(패시브 우선).
            var acc = charState.Source.Equipment?.Accessory;
            if (acc != null) Scan(acc.GetEffects(), true);

            return result;
        }

        /// <summary>
        /// 치사 피해 발생 시 생존 판정. 적용 순서: 부활 후 무적(불굴/불사) → 권능 → 부활 → 사망.
        /// CharacterBattleState 상태를 직접 갱신하고, 로그용 결과를 반환한다.
        /// </summary>
        public static SurvivalResult ResolveLethal(CharacterBattleState target)
        {
            // 0. 영멸: 보유 시 모든 생존기(불굴/불사/권능/부활) 미발동 — 즉시 사망.
            if (target.Effects != null && target.Effects.GetStatusEffectsOfType(StatusEffectType.Annihilation).Count > 0)
                return new SurvivalResult { Survived = false, Label = "사망", Description = "영멸 (생존기 미발동)" };

            // 1. 부활 후 무적 (불굴 피격횟수 / 불사 턴) — 사망 무효
            if (target.ImmortalHitsRemaining > 0 || target.ImmortalTurnsRemaining > 0)
            {
                if (target.ImmortalHitsRemaining > 0) target.ImmortalHitsRemaining--;
                target.CurrentHp = 1;
                return new SurvivalResult { Survived = true, Label = "불굴/불사", Description = $"치사 피해 무효 (잔여 피격 {target.ImmortalHitsRemaining})" };
            }

            // 2. 권능 — 현재 생명력 이상 피해 시 ReviveHpPercent(MaxHp%) 또는 ReviveHp로 1회 생존 (전투당 1회).
            //    사망이 아니므로 보유 버프 유지(공성전 저딜 서포터 생존에 적합). 발동 연계 보호막(시전자 공격력%)도 부여
            //    → HP1 맨몸으로 잡타에 즉사하지 않고 보호막 지속턴만큼 버틴다(공성전 ally.Shield 소비, 단일보스 sim 미사용).
            var authority = GetPassiveSurvival(target)?.Authority;
            if (authority != null && !target.AuthorityUsed)
            {
                target.AuthorityUsed = true;
                double hp = authority.ReviveHpPercent > 0
                    ? target.MaxHp * (authority.ReviveHpPercent / 100.0)
                    : System.Math.Max(1, authority.ReviveHp);
                target.CurrentHp = hp;
                string shieldDesc = "";
                if (authority.ShieldAtkRatio > 0)
                {
                    double shield = target.FinalAtk * (authority.ShieldAtkRatio / 100.0);
                    if (shield > target.Shield) target.Shield = shield;
                    target.ShieldTurns = System.Math.Max(target.ShieldTurns, authority.ShieldDuration);
                    shieldDesc = $", 보호막 {shield:N0}[{authority.ShieldDuration}턴]";
                }
                return new SurvivalResult { Survived = true, Label = "권능", Description = $"치사 피해 생존 → 생명력 {hp:N0}{shieldDesc}" };
            }

            // 3. 부활 — 사망 시 부활 (전투당 1회) + 무적 윈도우 설정
            var revival = GetPassiveSurvival(target)?.Revival;
            if (revival != null && !target.RevivalUsed)
            {
                target.RevivalUsed = true;
                double hp = revival.ReviveHpPercent > 0
                    ? target.MaxHp * (revival.ReviveHpPercent / 100.0)
                    : System.Math.Max(1, revival.ReviveHp);
                target.CurrentHp = hp;
                target.ImmortalHitsRemaining = revival.HitCount;
                target.ImmortalTurnsRemaining = revival.ImmortalTurns;
                ApplyRevivalCooldownReset(target);
                return new SurvivalResult
                {
                    Survived = true,
                    Label = "부활",
                    Description = $"부활 → 생명력 {hp:N0}"
                        + (revival.HitCount > 0 ? $", 불굴 피격 {revival.HitCount}회" : "")
                        + (revival.ImmortalTurns > 0 ? $", 불사 {revival.ImmortalTurns}턴" : ""),
                };
            }

            // 4. 사망
            target.IsDead = true;
            return new SurvivalResult { Survived = false, Label = "사망", Description = $"{target.Source.Character.Name} 사망" };
        }

        /// <summary>부활 발동 시 CooldownReset(TriggerCondition=OnRevival) 효과로 스킬 쿨타임 초기화/감소.</summary>
        public static void ApplyRevivalCooldownReset(CharacterBattleState target)
        {
            var passive = target.Source.Character.Passive;
            if (passive == null) return;

            var effects = new List<PersistentEffect>();
            var lvl = passive.GetLevelData(target.Source.IsSkillEnhanced, target.Source.IsAwakened);
            if (lvl?.Effects != null) effects.AddRange(lvl.Effects);
            var tr = passive.GetTranscendBonus(target.Source.TranscendLevel);
            if (tr?.Effects != null) effects.AddRange(tr.Effects);

            foreach (var e in effects)
            {
                if (e.Type != PersistentEffectType.CooldownReset || e.CooldownReset == null) continue;
                if (e.TriggerCondition != TriggerCondition.OnRevival) continue;

                var cr = e.CooldownReset;
                foreach (var key in target.SkillCooldowns.Keys.ToList())
                {
                    if (cr.OnlySkillType.HasValue && key != cr.OnlySkillType.Value) continue;
                    if (cr.ReduceSeconds > 0)
                        target.SkillCooldowns[key] = System.Math.Max(0, target.SkillCooldowns[key] - cr.ReduceSeconds);
                    else
                        target.SkillCooldowns[key] = 0;
                }
            }
        }
    }
}

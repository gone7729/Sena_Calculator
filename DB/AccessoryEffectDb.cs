using System.Collections.Generic;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 장신구 고유 효과 (32종 × 4/5/6성). 스탯(GradeBonus/MainOptions)과 별개로 종류별 고유 효과를 선언.
    /// PersistentEffect 리스트로 표현 — 착용 영웅(Self) 버프/생존, 또는 기본공격 시 적(Enemy)에 상태이상.
    /// (런타임 소비: 시뮬에서 Accessory→효과 연결은 후속.)
    /// </summary>
    public static partial class AccessoryDb
    {
        public static List<PersistentEffect> GetEffects(string name, int grade)
            => Effects.TryGetValue(name, out var byGrade) && byGrade.TryGetValue(grade, out var list)
                ? list : new List<PersistentEffect>();

        public static readonly Dictionary<string, Dictionary<int, List<PersistentEffect>>> Effects = new()
        {
            // ===== 전설 =====
            ["부활의 반지"] = ByGrade(Revive(25), Revive(50), Revive(100)),
            ["불사의 반지"] = ByGrade(Immortal(1), Immortal(2), Immortal(3)),
            ["권능의 반지"] = ByGrade(AuthorityRing(30, 2), AuthorityRing(75, 3), AuthorityRing(150, 4)),

            // ===== 희귀 ===== (전투 시작 시 보호막 / 피증 / 회복)
            ["기합의 반지"] = ByGrade(SelfBuff(new BuffSet { Shield_AtkRatio = 30 }), SelfBuff(new BuffSet { Shield_AtkRatio = 50 }), SelfBuff(new BuffSet { Shield_AtkRatio = 90 })),
            ["철벽의 반지"] = ByGrade(SelfBuff(new BuffSet { Shield_DefRatio = 35 }), SelfBuff(new BuffSet { Shield_DefRatio = 55 }), SelfBuff(new BuffSet { Shield_DefRatio = 100 })),
            ["건강의 반지"] = ByGrade(SelfBuff(new BuffSet { Shield_HpRatio = 6 }), SelfBuff(new BuffSet { Shield_HpRatio = 11 }), SelfBuff(new BuffSet { Shield_HpRatio = 18 })),
            ["토벌의 반지"] = ByGrade(SelfBuff(new BuffSet { Dmg_Dealt_Bos = 4 }), SelfBuff(new BuffSet { Dmg_Dealt_Bos = 8 }), SelfBuff(new BuffSet { Dmg_Dealt_Bos = 12 })),
            ["공성의 반지"] = ByGrade(SelfBuff(new BuffSet { Dmg_Dealt_1to3 = 3 }), SelfBuff(new BuffSet { Dmg_Dealt_1to3 = 6 }), SelfBuff(new BuffSet { Dmg_Dealt_1to3 = 10 })),
            ["섬멸의 반지"] = ByGrade(SelfBuff(new BuffSet { Dmg_Dealt_4to5 = 3 }), SelfBuff(new BuffSet { Dmg_Dealt_4to5 = 6 }), SelfBuff(new BuffSet { Dmg_Dealt_4to5 = 10 })),
            ["근성의 반지"] = ByGrade(SelfHeal(6), SelfHeal(11), SelfHeal(18)),

            // ===== 고급 ===== (기본공격 시 확률로 적에 상태이상 / 버프 조작)
            ["마법의 반지"] = ByGrade(DispelOnNormal(4), DispelOnNormal(8), DispelOnNormal(12)),
            ["시간의 반지"] = ByGrade(TurnReduceOnNormal(4), TurnReduceOnNormal(8), TurnReduceOnNormal(12)),
            ["기회의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Stun, 3, 1), CcOnNormal(StatusEffectType.Stun, 6, 1), CcOnNormal(StatusEffectType.Stun, 10, 1)),
            ["저주의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Silence, 4, 1), CcOnNormal(StatusEffectType.Silence, 8, 1), CcOnNormal(StatusEffectType.Silence, 12, 1)),
            // 죽음: 4성 침묵 3턴, 5·6성 즉사 3턴
            ["죽음의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Silence, 2, 3), CcOnNormal(StatusEffectType.InstantDeath, 4, 3), CcOnNormal(StatusEffectType.InstantDeath, 6, 3)),
            ["재앙의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Paralysis, 3, 1), CcOnNormal(StatusEffectType.Paralysis, 6, 1), CcOnNormal(StatusEffectType.Paralysis, 10, 1)),
            ["번뜩이는 반지"] = ByGrade(CcOnNormal(StatusEffectType.Shock, 3, 1), CcOnNormal(StatusEffectType.Shock, 6, 1), CcOnNormal(StatusEffectType.Shock, 10, 1)),
            ["공포의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Blind, 4, 1), CcOnNormal(StatusEffectType.Blind, 8, 1), CcOnNormal(StatusEffectType.Blind, 12, 1)),
            ["설원의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Freeze, 4, 1), CcOnNormal(StatusEffectType.Freeze, 8, 1), CcOnNormal(StatusEffectType.Freeze, 12, 1)),
            ["매두사의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Petrify, 3, 1), CcOnNormal(StatusEffectType.Petrify, 6, 1), CcOnNormal(StatusEffectType.Petrify, 10, 1)),
            ["꿈의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Sleep, 4, 1), CcOnNormal(StatusEffectType.Sleep, 8, 1), CcOnNormal(StatusEffectType.Sleep, 12, 1)),
            ["가시 반지"] = ByGrade(CcOnNormal(StatusEffectType.Bleeding, 5, 2), CcOnNormal(StatusEffectType.Bleeding, 10, 2), CcOnNormal(StatusEffectType.Bleeding, 15, 2)),
            ["샐러맨더의 반지"] = ByGrade(CcOnNormal(StatusEffectType.Burn, 5, 2), CcOnNormal(StatusEffectType.Burn, 10, 2), CcOnNormal(StatusEffectType.Burn, 15, 2)),
            ["독사 반지"] = ByGrade(CcOnNormal(StatusEffectType.Poison, 5, 2), CcOnNormal(StatusEffectType.Poison, 10, 2), CcOnNormal(StatusEffectType.Poison, 15, 2)),

            // ===== 일반 ===== (전투 시작 시 자신 스탯 상승 = 상시 버프로 근사)
            ["복수의 반지"] = ByGrade(SelfBuff(new BuffSet { Dmg_Dealt = 2 }), SelfBuff(new BuffSet { Dmg_Dealt = 4 }), SelfBuff(new BuffSet { Dmg_Dealt = 6 })),
            ["보호의 반지"] = ByGrade(SelfBuff(new BuffSet { Def_Rate = 2 }), SelfBuff(new BuffSet { Def_Rate = 6 }), SelfBuff(new BuffSet { Def_Rate = 10 })),
            ["행운의 반지"] = ByGrade(SelfBuff(new BuffSet { Cri = 3 }), SelfBuff(new BuffSet { Cri = 6 }), SelfBuff(new BuffSet { Cri = 10 })),
            ["수호의 반지"] = ByGrade(SelfBuff(new BuffSet { Blk = 3 }), SelfBuff(new BuffSet { Blk = 6 }), SelfBuff(new BuffSet { Blk = 10 })),
            ["집중의 반지"] = ByGrade(SelfBuff(new BuffSet { Wek = 4 }), SelfBuff(new BuffSet { Wek = 8 }), SelfBuff(new BuffSet { Wek = 12 })),
            ["자연의 반지"] = ByGrade(SelfBuff(new BuffSet { Hp_Rate = 3 }), SelfBuff(new BuffSet { Hp_Rate = 6 }), SelfBuff(new BuffSet { Hp_Rate = 10 })),
            ["적중의 반지"] = ByGrade(SelfBuff(new BuffSet { Eff_Hit = 5 }), SelfBuff(new BuffSet { Eff_Hit = 10 }), SelfBuff(new BuffSet { Eff_Hit = 15 })),
            ["저항의 반지"] = ByGrade(SelfBuff(new BuffSet { Eff_Res = 5 }), SelfBuff(new BuffSet { Eff_Res = 10 }), SelfBuff(new BuffSet { Eff_Res = 15 })),
        };

        // ===== 헬퍼 =====
        private static Dictionary<int, List<PersistentEffect>> ByGrade(
            List<PersistentEffect> g4, List<PersistentEffect> g5, List<PersistentEffect> g6)
            => new() { [4] = g4, [5] = g5, [6] = g6 };

        private static List<PersistentEffect> SelfBuff(BuffSet buff)
            => new() { new() { Target = EffectTarget.Self, Type = PersistentEffectType.Buff, Buff = buff } };

        /// <summary>근성: 생명력 50% 이하가 되면 최대HP% 회복 (전투당 1회).</summary>
        private static List<PersistentEffect> SelfHeal(double hpRatio)
            => new() { new() {
                Target = EffectTarget.Self, Type = PersistentEffectType.TriggeredHeal,
                ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.OnHpBelow,
                TriggerHpThreshold = 50, OncePerBattle = true, TriggeredHealHpRatio = hpRatio } };

        private static List<PersistentEffect> Revive(double hpPercent)
            => new() { new() {
                Target = EffectTarget.Self, Type = PersistentEffectType.Revival,
                Revival = new Revival { ReviveHpPercent = hpPercent, OncePerBattle = true } } };

        private static List<PersistentEffect> Immortal(int turns)
            => new() { new() {
                Target = EffectTarget.Self, Type = PersistentEffectType.Revival,
                Revival = new Revival { ImmortalTurns = turns, OncePerBattle = true } } };

        private static List<PersistentEffect> AuthorityRing(double shieldAtk, int shieldDur)
            => new() { new() {
                Target = EffectTarget.Self, Type = PersistentEffectType.Authority,
                Authority = new Authority { ReviveHp = 1, OncePerBattle = true, ShieldAtkRatio = shieldAtk, ShieldDuration = shieldDur } } };

        /// <summary>고급: 기본공격 시 Chance% 확률로 적에 상태이상 부여.</summary>
        private static List<PersistentEffect> CcOnNormal(StatusEffectType type, double chance, int duration)
            => new() { new() {
                Target = EffectTarget.Enemy, Type = PersistentEffectType.StatusAilment, StatusType = type,
                ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.NormalOnly, TriggerCount = 1,
                Chance = chance, Duration = duration } };

        /// <summary>마법: 기본공격 시 Chance% 확률로 적 버프 1개 해제.</summary>
        private static List<PersistentEffect> DispelOnNormal(double chance)
            => new() { new() {
                Target = EffectTarget.Enemy, Type = PersistentEffectType.BuffDispel,
                ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.NormalOnly,
                Chance = chance, DispelBuffCount = 1 } };

        /// <summary>시간: 기본공격 시 Chance% 확률로 적 턴제버프 1턴 감소.</summary>
        private static List<PersistentEffect> TurnReduceOnNormal(double chance)
            => new() { new() {
                Target = EffectTarget.Enemy, Type = PersistentEffectType.BuffTurnReduction,
                ApplyMode = ApplyMode.Triggered, TriggerCondition = TriggerCondition.NormalOnly,
                Chance = chance, TurnReduction = 1 } };
    }
}

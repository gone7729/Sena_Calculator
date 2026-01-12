using System;
using System.Collections.Generic;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 상태이상 타입
    /// </summary>
    public enum StatusEffectType
    {
        None,
        
        // === CC (행동 불가) ===
        Stun,           // 기절
        Silence,        // 침묵
        Freeze,         // 빙결
        Petrify,        // 석화
        IceExtreme,     // 빙극
        Paralysis,      // 마비
        Shock,          // 감전
        Sleep,          // 수면
        Confusion,      // 혼란
        Concussion,     // 진탕
        
        // === DoT (지속 피해) ===
        Burn,           // 화상
        InstantDeath,   // 즉사
        ManaBackflow,   // 마력 역류
        Bleeding, // 출혈
        ChainDamage, // 카일꺼
        
        // === 특수 ===
        Bomb,           // 폭탄
        BombDetonation, // 폭탄 폭파
        BleedExplosion, // 출혈 폭발
        Crystal,        // 수정 결정
        CrystalResonance,  // 수정 공명
        Miss,           // 빗나감
        HealBlock,      // 회복 불가
        HpConversion,   // 생명력 전환
    }

    /// <summary>
    /// 상태이상 데이터
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // 지속 시간/횟수
        public int Duration { get; set; }           // 지속 턴
        public int MaxStacks { get; set; } = 1;     // 최대 중첩
        public int TriggerCount { get; set; }       // 발동 횟수 (수정 결정 등)
        
        
        // 피해 관련
        public double AtkRatio { get; set; }        // 공격력 비례% (화상 80%, 석화 120%)
        public double TargetMaxHpRatio { get; set; } // 대상 최대 HP 비례% (빙결 40%, 빙극 60%, 마력역류 12%)
        public double TargetCurrentHpRatio { get; set; } // 대상 현재 HP 비례% (즉사 20%)
        public double AtkCap { get; set; }          // 공격력 제한% (빙결 300%, 마력역류 90%)
        public double ArmorPen { get; set; }        // 방어 무시% (빙결 40%, 폭탄 40%)
        public double FixedDamage { get; set; }     // 고정 피해 (수정 결정 2435)
        public double  ChainDamage { get; set; }     // 카일꺼
        
        // 추가 효과
        public double HealRatio { get; set; }       // 회복량% (빙극 100%)
        public bool IsGuaranteedCrit { get; set; }  // 확정 치명타 (수면)
        public double MissChanceIncrease { get; set; } // 빗나감 확률 증가% (혼란 100%)
        public double DamageReduction { get; set; } // 피해 감소% (빗나감 20%)
        public bool BlocksBlock { get; set; }       // 막기 불가 (마비)
        public bool BlocksHeal { get; set; }        // 회복 불가
        public bool BlocksAction { get; set; }      // 행동 불가
        public bool BlocksActiveSkill { get; set; } // 액티브 스킬 불가 (침묵)
        public double WakeUpThreshold { get; set; } // 해제 조건 HP% (수면 7%)
        
        // 생명력 전환
        public double HpConversionAmount { get; set; } // 전환량

        // 소모형 효과 (폭탄 폭파, 출혈 폭발 등)
        public StatusEffectType? ConsumeType { get; set; }  // 소모할 상태이상 타입
        public int MaxConsume { get; set; } = 0;            // 최대 소모 개수
        public int DefaultRemainingTurns { get; set; } = 2;  // 남은 턴 기본값
        public bool IsHpConversion { get; set; }  // HP 전환 여부
    }

    /// <summary>
    /// 상태이상 데이터베이스
    /// </summary>
    public static class StatusEffectDb
    {
        public static Dictionary<StatusEffectType, StatusEffect> Effects = new Dictionary<StatusEffectType, StatusEffect>
        {
            // === CC (행동 불가) ===
            
            { StatusEffectType.Stun, new StatusEffect
            {
                Type = StatusEffectType.Stun,
                Name = "기절",
                Description = "지속시간 동안 행동할 수 없다",
                BlocksAction = true
            }},
            
            { StatusEffectType.Silence, new StatusEffect
            {
                Type = StatusEffectType.Silence,
                Name = "침묵",
                Description = "지속시간 동안 액티브 스킬 사용 불가",
                BlocksActiveSkill = true
            }},
            
            { StatusEffectType.Freeze, new StatusEffect
            {
                Type = StatusEffectType.Freeze,
                Name = "빙결",
                Description = "지속시간 동안 행동 불가, 피격 시 해제되며 대상 최대 HP 40% 방어무시(40%) 피해 (공격력 300% 제한)",
                BlocksAction = true,
                TargetMaxHpRatio = 40,
                ArmorPen = 40,
                AtkCap = 300
            }},
            
            { StatusEffectType.Petrify, new StatusEffect
            {
                Type = StatusEffectType.Petrify,
                Name = "석화",
                Description = "지속시간 동안 행동 불가, 해제 시 시전자 공격력의 120% 피해",
                BlocksAction = true,
                AtkRatio = 120
            }},
            
            { StatusEffectType.IceExtreme, new StatusEffect
            {
                Type = StatusEffectType.IceExtreme,
                Name = "빙극",
                Description = "지속시간 동안 행동 불가, 피격 시 해제되며 공격자 피해량 100% 회복, 대상 최대 HP 60% 방어무시(40%) 피해. 빙결로 취급",
                BlocksAction = true,
                TargetMaxHpRatio = 60,
                ArmorPen = 40,
                HealRatio = 100
            }},
            
            { StatusEffectType.Paralysis, new StatusEffect
            {
                Type = StatusEffectType.Paralysis,
                Name = "마비",
                Description = "지속시간 동안 행동 불가, 막기 확률 0%",
                BlocksAction = true,
                BlocksBlock = true
            }},
            
            { StatusEffectType.Shock, new StatusEffect
            {
                Type = StatusEffectType.Shock,
                Name = "감전",
                Description = "지속시간 동안 행동 불가, 피격 시 시전자 공격력의 40% 추가 피해",
                BlocksAction = true,
                AtkRatio = 40
            }},
            
            { StatusEffectType.Sleep, new StatusEffect
            {
                Type = StatusEffectType.Sleep,
                Name = "수면",
                Description = "지속시간 동안 행동 불가, 피격 시 확정 치명타, 최대 HP 7% 이상 피해 시 해제",
                BlocksAction = true,
                IsGuaranteedCrit = true,
                WakeUpThreshold = 7
            }},
            
            { StatusEffectType.Confusion, new StatusEffect
            {
                Type = StatusEffectType.Confusion,
                Name = "혼란",
                Description = "지정 횟수 동안 빗나감 확률 100% 증가 (피해 20% 감소, 치명타/효과적중 0%)",
                MissChanceIncrease = 100,
                DamageReduction = 20
            }},
            
            { StatusEffectType.Concussion, new StatusEffect
            {
                Type = StatusEffectType.Concussion,
                Name = "진탕",
                Description = "지속시간 동안 행동할 수 없다",
                BlocksAction = true
            }},
            
            // === DoT (지속 피해) ===
            
            { StatusEffectType.Burn, new StatusEffect
            {
                Type = StatusEffectType.Burn,
                Name = "화상",
                Description = "매 턴마다 시전자 공격력의 80% 피해",
                AtkRatio = 80
            }},
            
            { StatusEffectType.InstantDeath, new StatusEffect
            {
                Type = StatusEffectType.InstantDeath,
                Name = "즉사",
                Description = "3턴 간 매 턴마다 대상 현재 생명력의 20% 피해",
                Duration = 3,
                TargetCurrentHpRatio = 20
            }},
            
            { StatusEffectType.ManaBackflow, new StatusEffect
            {
                Type = StatusEffectType.ManaBackflow,
                Name = "마력 역류",
                Description = "피격 시 대상 최대 HP 12% 피해 (공격력 90% 제한), 최대 7중첩",
                TargetMaxHpRatio = 12,
                AtkCap = 90,
                MaxStacks = 7
            }},

            { StatusEffectType.Bleeding, new StatusEffect
            {
                Type = StatusEffectType.Bleeding,
                Name = "출혈",
                Description = "매턴마다 시전자 공격력의 60%, 최대 5중첩",
                AtkRatio = 60
            }},
            
            // === 특수 ===
            
            { StatusEffectType.Bomb, new StatusEffect
            {
                Type = StatusEffectType.Bomb,
                Name = "폭탄",
                Description = "지속시간 만료 시 시전자 공격력의 120% 방어무시(40%) 피해, 최대 3중첩",
                AtkRatio = 120,
                ArmorPen = 40,
                MaxStacks = 3
            }},
            
            { StatusEffectType.BombDetonation, new StatusEffect
            {
                Type = StatusEffectType.BombDetonation,
                Name = "폭탄 폭파",
                Description = "폭탄 폭파 시 공격력 150% 피해, 폭탄 개수만큼 피해 (최대 3개)",
                AtkRatio = 150,
                ConsumeType = StatusEffectType.Bomb,  // 폭탄을 소모
                MaxConsume = 3,
                ArmorPen = 40
            }},

            { StatusEffectType.BleedExplosion, new StatusEffect
            {
                Type = StatusEffectType.BleedExplosion,
                Name = "출혈 폭발",
                Description = "대상 출혈 효과를 폭발시켜 남은 턴과 개수에 비례해 대미지 증가(예: 3턴 남은 출혈이 3개 있다면 (AtkRatio*3)*3)",
                AtkRatio = 120,
                ConsumeType = StatusEffectType.Bleeding,  //출혈 소모
                MaxConsume = 3,
                DefaultRemainingTurns = 2
            }},

            { StatusEffectType.ChainDamage, new StatusEffect
            {
                Type = StatusEffectType.ChainDamage,
                Name = "카일꺼",
                Description = "공격 2회 시 대상 최대 생명력의 23% 1회 방어무시(40%) 공격력 100%제한",
                TargetMaxHpRatio = 23,
                AtkCap = 100,
                MaxStacks = 1
            }},
            
            { StatusEffectType.Crystal, new StatusEffect
            {
                Type = StatusEffectType.Crystal,
                Name = "수정 결정",
                Description = "지정 횟수만큼 피격 시 2435 고정 피해",
                FixedDamage = 2435,
                TriggerCount = 6
            }},

            { StatusEffectType.CrystalResonance, new StatusEffect
            {
                Type = StatusEffectType.CrystalResonance,
                Name = "수정 공명",
                Description = "지정 횟수만큼 수정 결정 감소",
                MaxConsume = 4
            }},
            
            { StatusEffectType.Miss, new StatusEffect
            {
                Type = StatusEffectType.Miss,
                Name = "빗나감",
                Description = "지속시간 동안 공격 시 빗나감 확률 발생 (피해 20% 감소, 치명타/효과적중 0%)",
                DamageReduction = 20
            }},
            
            { StatusEffectType.HealBlock, new StatusEffect
            {
                Type = StatusEffectType.HealBlock,
                Name = "회복 불가",
                Description = "지속시간 동안 생명력을 회복할 수 없다",
                BlocksHeal = true
            }},
            
            { StatusEffectType.HpConversion, new StatusEffect
            {
                Type = StatusEffectType.HpConversion,
                Name = "생명력 전환",
                Description = "대상의 생명력을 표시된 수치만큼 전환 (현재 생명력 초과 불가)",
                IsHpConversion = true
            }},
        };

        public static StatusEffect Get(StatusEffectType type)
        {
            return Effects.TryGetValue(type, out var effect) ? effect : null;
        }
    }
}

using System.Collections.Generic;
using GameDamageCalculator.Models;
using GameDamageCalculator.Models.Effects;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 공성전 보스 스킬 정의 DB (캐릭터의 CharacterDB에 대응).
    /// 보스 스탯·감쇄·메타는 EnemyDB(SiegeBosses)에 있고, 여기서는 스킬(기본공격+스킬2)만 정의한다.
    /// EnemyDB가 초기화 시 각 보스의 Enemy.Skills를 Get(이름)으로 연결한다.
    ///
    /// 보스 스킬은 단일 티어이므로 LevelData[0]만 사용한다.
    /// 보스가 "적군(=플레이어 파티)"에게 거는 상태이상/디버프는 SkillEffect.Target = Enemy로 표기한다
    /// (보스 시점의 상대편). 런타임 소비(보스 행동)는 후속 작업.
    ///
    /// === 모델 미반영 고유 지속효과 (보스 행동 구현 시 Enemy 확장 예정) ===
    /// - 공성전 감쇄: EnemyDB의 PhysicalReduction/MagicReduction + Dmg_Rdc_Single/Multi에 이미 반영됨.
    /// - 광폭화(전 보스 공통): 30턴 후 주는 피해 +50%, 이후 40턴 +100% / 50턴 +150% / 55턴 +200% / 60턴 +500%.
    /// - 반격(제이브 25%), 적군 사망 시 모든 피해 무효화[피격 4회](델론즈), 자기 보호막(루디).
    /// 위 항목들은 모델 필드가 없어 이번 범위에서 제외. 각 보스 주석에 원문 보존.
    /// </summary>
    public static class SiegeBossSkillDb
    {
        public static List<Skill> Get(string bossName)
            => Skills.TryGetValue(bossName, out var list) ? list : new List<Skill>();

        public static readonly Dictionary<string, List<Skill>> Skills = new()
        {
            // ===== 월 : 수호자의 성 - 루디 (방어형) =====
            // 고유 「견고한 방패」: 공성전 감쇄(물리 90% / 1인 70% / 5인 90%) + 30턴 후 광폭화.
            ["루디"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new() { [0] = new SkillLevelData { Ratio = 120, TargetCount = 1, AtkCount = 1 } },
                },
                new Skill
                {
                    Name = "돌격",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 220, TargetCount = 3, AtkCount = 1, Cooldown = 30,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Stun, Chance = 100, Duration = 3 },
                            },
                        },
                    },
                },
                new Skill
                {
                    // 모든 아군(보스 자기편) 링크 5턴 + 시전자 방어력 10,000% 보호막 5턴 — 자기 보호막이라 단일 보스 시뮬에선 영향 없음.
                    Name = "방어 준비",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Cooldown = 80,
                            Effect = "모든 아군 링크[5턴] + 시전자 방어력의 10,000% 보호막[5턴]",
                        },
                    },
                },
            },

            // ===== 화 : 포디나의 성 - 아일린 (만능형) =====
            // 고유 「포디나의 분노」: 공성전 감쇄(물리 90% / 1인 70% / 5인 90%) + 30턴 후 광폭화.
            ["아일린"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 130, TargetCount = 1, AtkCount = 1,
                            ConditionalRatioBonus = 75, ConditionalDesc = "대상이 [감전] 상태일 경우 물리 공격력의 75% 추가 피해",
                        },
                    },
                },
                new Skill
                {
                    Name = "뇌제의 분노",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 80, TargetCount = 3, AtkCount = 3, Cooldown = 40,
                            IgnoresTurnDamageImmunity = true, // 관통: 피해 면역 무시
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Shock, Chance = 100, Duration = 3 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "청천벽력",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 80, TargetCount = 5, AtkCount = 2, Cooldown = 80,
                            IgnoresTurnDamageImmunity = true,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Shock, Chance = 100, Duration = 2 },
                            },
                        },
                    },
                },
            },

            // ===== 수 : 불멸의 성 - 레이첼 (만능형) =====
            // 고유 「화염의 힘」: 공성전 감쇄(물리 90% / 1인 70% / 5인 90%) + 광폭화 + 피격 1회당 적군 3명 화상[100%/2턴].
            ["레이첼"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 120, TargetCount = 1, AtkCount = 1,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Burn, Chance = 100, Duration = 2 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "염화",
                    SkillType = SkillType.Skill1,
                    LevelData = new() { [0] = new SkillLevelData { Ratio = 120, TargetCount = 3, AtkCount = 2, Cooldown = 30 } },
                },
                new Skill
                {
                    Name = "불새",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 160, TargetCount = 5, AtkCount = 1, Cooldown = 80,
                            Effects = new()
                            {
                                // 방어력 감소 36% [5턴]
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5,
                                        Debuff = new DebuffSet { Def_Reduction = 36 } },
                                // 모든 공격력 감소 24% [5턴] (물리+마법)
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.Debuff, Duration = 5,
                                        Debuff = new DebuffSet { Atk_Reduction = 24, MagicAtk_Reduction = 24 } },
                            },
                        },
                    },
                },
            },

            // ===== 목 : 죽음의 성 - 델론즈 (공격형) =====
            // 고유 「죽음의 경계」: 공성전 감쇄(마법 90% / 1인 70% / 5인 90%) + 광폭화 + 적군 사망 시 모든 피해 무효화[피격 4회].
            ["델론즈"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 120, TargetCount = 1, AtkCount = 1,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Silence, Chance = 100, Duration = 1 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "사신강림",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 170, TargetCount = 5, AtkCount = 1, Cooldown = 70,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Silence, Chance = 100, Duration = 4 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "죽음의 일격",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 300, TargetCount = 1, AtkCount = 5, Cooldown = 60,
                            // 직접 피해로 적군 처치 시 100% 위력으로 연속 발동
                            OnKillRecast = new OnKillRecast { RatioPercent = 100, Chance = 100 },
                        },
                    },
                },
            },

            // ===== 금 : 고대용의 성 - 제이브 (만능형) =====
            // 고유 「복수의 갑옷」: 공성전 감쇄(마법 90% / 1인 70% / 5인 90%) + 반격[25%] + 광폭화.
            //   반격 발동 시 적군 3명 물리 60% 1회[치확 100%/치피 +500%] + 용염[100%/2턴].
            //   용염: 매 턴 시전자 공격력의 120% 피해(화상으로 간주).
            ["제이브"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 120, TargetCount = 1, AtkCount = 1,
                            Effects = new()
                            {
                                // 용염 = 화상(매 턴 공격력 120%)
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Burn, Chance = 100, Duration = 2, CustomAtkRatio = 120 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "분노의 일격",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 120, TargetCount = 5, AtkCount = 1, Cooldown = 60,
                            DmgBonusPerMissingTarget = 15, // 피해 대상 1명 줄어들 때마다 +15%
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Stun, Chance = 100, Duration = 2 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "용의 분노",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 55, TargetCount = 5, AtkCount = 2, Cooldown = 60,
                            IgnoresTurnDamageImmunity = true, // 관통
                            DmgBonusPerMissingTarget = 8, // 피해 대상 1명 줄어들 때마다 +8%
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Burn, Chance = 100, Duration = 2, CustomAtkRatio = 120 },
                            },
                        },
                    },
                },
            },

            // ===== 토 : 혹한의 성 - 스파이크 (만능형) =====
            // 고유 「혹한의 심장」: 공성전 감쇄(마법 90% / 1인 70% / 5인 90%) + 광폭화.
            //   빙결 해제 시 대상 최대 생명력 40% 방어무시(방어 40% 고정 무시, 공격력 300% 상한) 피해.
            ["스파이크"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new() { [0] = new SkillLevelData { Ratio = 120, TargetCount = 1, AtkCount = 1 } },
                },
                new Skill
                {
                    // 주 대상 195% + 동일 열 적군 75%(별도 타격) — 동일 열 추가타격은 Effect 텍스트로 보존.
                    Name = "혹한의 일격",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 195, TargetCount = 1, AtkCount = 1, Cooldown = 40,
                            Effect = "주 대상과 동일 열 적군에게 물리 75% 1회 + 빙결[100%/3턴] 추가",
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Freeze, Chance = 100, Duration = 3 },
                            },
                            CleanseExplosion = new CleanseExplosion
                            {
                                TargetStatus = StatusEffectType.Freeze,
                                TargetMaxHpRatio = 40, AtkCap = 300, ArmorPen = 40,
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "혹한의 지진",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 80, TargetCount = 5, AtkCount = 2, Cooldown = 80,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.Freeze, Chance = 100, Duration = 2 },
                            },
                            CleanseExplosion = new CleanseExplosion
                            {
                                TargetStatus = StatusEffectType.Freeze,
                                TargetMaxHpRatio = 40, AtkCap = 300, ArmorPen = 40,
                            },
                        },
                    },
                },
            },

            // ===== 일 : 지옥의 성 - 크리스 (만능형) =====
            // 고유 「영혼 흡수」: 공성전 감쇄(5인 공격기 90%만) + 광폭화.
            //   즉사: 매 턴 대상 현재 생명력 20% 피해, 마지막 턴 즉시 사망. 2중첩 시 즉시 사망.
            ["크리스"] = new List<Skill>
            {
                new Skill
                {
                    Name = "기본 공격",
                    SkillType = SkillType.Normal,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 120, TargetCount = 1, AtkCount = 1,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.InstantDeath, Chance = 100, Duration = 3 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "어둠의 일격",
                    SkillType = SkillType.Skill1,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 300, TargetCount = 1, AtkCount = 1, Cooldown = 30,
                            Effects = new()
                            {
                                // 즉사 2중첩 부여 (즉시 사망 트리거)
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.InstantDeath, Chance = 100, Duration = 3, Stacks = 2 },
                            },
                        },
                    },
                },
                new Skill
                {
                    Name = "어둠의 속삭임",
                    SkillType = SkillType.Skill2,
                    LevelData = new()
                    {
                        [0] = new SkillLevelData
                        {
                            Ratio = 160, TargetCount = 5, AtkCount = 1, Cooldown = 70,
                            Effects = new()
                            {
                                new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                        StatusType = StatusEffectType.InstantDeath, Chance = 100, Duration = 3 },
                            },
                        },
                    },
                },
            },
        };

        // ===== 공성전 잡몹 스킬 헬퍼 (요일별 배율·상태이상으로 SiegeDayDb가 호출) =====

        /// <summary>
        /// 공성전 잡몹(룩/챈슬러) 스킬: 기본공격(단일 normalRatio) + 1스킬(단일 skill1Ratio + 상태이상[statusDur/100%]).
        /// 요일별 상태이상이 다름. cd 미제공 데이터라 토요일 기준(룩 70 / 챈슬러 85) 가정.
        /// hpConvPct>0이면 생명력 전환(월), statusAtkRatio로 용염(금 챈슬러, Burn 120%), extra는 모델 미반영 효과 텍스트.
        /// </summary>
        public static List<Skill> MobSkills(double normalRatio, double skill1Ratio, double cooldown,
            StatusEffectType status, string skill1Name = "1스킬", int statusDur = 3,
            double? statusAtkRatio = null, double hpConvPct = 0, string extra = null, int grantEnemyImmunity = 0)
        {
            var effects = new List<SkillEffect>();
            if (hpConvPct > 0)
                effects.Add(new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                    StatusType = StatusEffectType.HpConversion, Chance = 100, Duration = statusDur, CustomHpConversionRatio = hpConvPct });
            effects.Add(new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                StatusType = status, Chance = 100, Duration = statusDur, CustomAtkRatio = statusAtkRatio });
            return new()
            {
                new Skill { Name = "기본 공격", SkillType = SkillType.Normal,
                    LevelData = new() { [0] = new SkillLevelData { Ratio = normalRatio, TargetCount = 1, AtkCount = 1 } } },
                new Skill { Name = skill1Name, SkillType = SkillType.Skill1,
                    LevelData = new() { [0] = new SkillLevelData
                        { Ratio = skill1Ratio, TargetCount = 1, AtkCount = 1, Cooldown = cooldown, Effect = extra, Effects = effects,
                          GrantEnemyImmunityTurns = grantEnemyImmunity } } },
            };
        }

        /// <summary>룩: 기본공격 + 투창(빙결 100%[3턴], 쿨 70초)</summary>
        public static List<Skill> LookSkills(double normalRatio, double skill1Ratio) => new()
        {
            new Skill
            {
                Name = "기본 공격",
                SkillType = SkillType.Normal,
                LevelData = new() { [0] = new SkillLevelData { Ratio = normalRatio, TargetCount = 1, AtkCount = 1 } },
            },
            new Skill
            {
                Name = "투창",
                SkillType = SkillType.Skill1,
                LevelData = new()
                {
                    [0] = new SkillLevelData
                    {
                        Ratio = skill1Ratio, TargetCount = 1, AtkCount = 1, Cooldown = 70,
                        Effects = new()
                        {
                            new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                    StatusType = StatusEffectType.Freeze, Chance = 100, Duration = 3 },
                        },
                    },
                },
            },
        };

        /// <summary>
        /// 챈슬러: 기본공격 + 분쇄(빙결 100%[3턴], 쿨 85초).
        /// R3는 분쇄 시 "공격력 가장 높은 아군"에게 혹한의 기운/숨결 부여(스파이크 액티브 치확100%·치피+500%) —
        /// 보스팀 시너지라 모델 부재, Effect 텍스트로 보존.
        /// </summary>
        public static List<Skill> ChancellorSkills(double normalRatio, double skill1Ratio, bool r3Frost) => new()
        {
            new Skill
            {
                Name = "기본 공격",
                SkillType = SkillType.Normal,
                LevelData = new() { [0] = new SkillLevelData { Ratio = normalRatio, TargetCount = 1, AtkCount = 1 } },
            },
            new Skill
            {
                Name = "분쇄",
                SkillType = SkillType.Skill1,
                LevelData = new()
                {
                    [0] = new SkillLevelData
                    {
                        Ratio = skill1Ratio, TargetCount = 1, AtkCount = 1, Cooldown = 85,
                        Effects = new()
                        {
                            new() { Target = EffectTarget.Enemy, Type = SkillEffectType.StatusAilment,
                                    StatusType = StatusEffectType.Freeze, Chance = 100, Duration = 3 },
                        },
                        Effect = r3Frost
                            ? "공격력 가장 높은 아군에게 혹한의 기운[5턴](스파이크 액티브 치명타 확률 100%) + 혹한의 숨결[5턴](스파이크 액티브 치명타 피해 +500%)"
                            : null,
                    },
                },
            },
        };
    }
}

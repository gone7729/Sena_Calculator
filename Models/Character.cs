using System.Collections.Generic;
using System.Linq;
using System;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    #region 캐릭터

    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }       // 전설, 영웅
        public string Type { get; set; }        // 공격형, 마법형, 만능형, 지원형, 방어형

        public List<Skill> Skills { get; set; } = new List<Skill>();
        public Passive Passive { get; set; }
        public List<TranscendBonus> TranscendBonuses { get; set; } = new List<TranscendBonus>();

        // 런타임 상태
        public int TranscendLevel { get; set; } = 0;
        public bool IsSkillEnhanced { get; set; } = false;

        public BaseStatSet GetBaseStats()
        {
            if (StatTable.AllBaseStats.TryGetValue(Grade, out var gradeStats))
            {
                if (gradeStats.TryGetValue(Type, out var typeStats))
                {
                    return typeStats.Clone();
                }
            }
            return new BaseStatSet();
        }

        public BaseStatSet GetTranscendStats(int level)
        {
            var result = new BaseStatSet();
            if (level <= 0) return result;

            // 캐릭터 고유 초월 (1~6)
            foreach (var bonus in TranscendBonuses.Where(b => b.Level <= level))
            {
                result.Add(bonus.BonusStats);
            }

            // 공통 초월 (7~12)
            foreach (var bonus in StatTable.TranscendDb.CommonBonuses.Where(b => b.Level <= level))
            {
                result.Add(bonus.BonusStats);
            }

            return result;
        }
    }

    /// <summary>
    /// 캐릭터 초월 스탯 보너스 (1~12레벨)
    /// </summary>
    public class TranscendBonus
    {
        public int Level { get; set; }
        public BaseStatSet BonusStats { get; set; } = new BaseStatSet();
        public string SpecialEffect { get; set; }
    }

    #endregion

    #region 스킬

    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SkillType SkillType { get; set; }
        public int TargetCount { get; set; } = 1;
        public int Atk_Count { get; set; } = 1;
        public int Ticks { get; set; } = 1;

        // 레벨별 데이터 (0=기본, 1=강화)
        public Dictionary<int, SkillLevelData> LevelData { get; set; } = new();

        // 초월별 추가 효과
        public Dictionary<int, SkillTranscend> TranscendBonuses { get; set; } = new();

        // === 헬퍼 메서드 ===

        public SkillLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new SkillLevelData();
        }

        public SkillTranscend GetTranscendBonus(int level)
        {
            var result = new SkillTranscend();
            if (TranscendBonuses == null) return result;

            foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
            {
                result.Bonus.Add(kvp.Value.Bonus);
                result.Debuff.Add(kvp.Value.Debuff);
                if (kvp.Value.TargetCountOverride.HasValue)
                    result.TargetCountOverride = kvp.Value.TargetCountOverride;
                result.Effect = kvp.Value.Effect;
            }
            return result;
        }

        public int GetTargetCount(int transcendLevel)
        {
            var bonus = GetTranscendBonus(transcendLevel);
            return bonus.TargetCountOverride ?? TargetCount;
        }

        /// <summary>
        /// 스킬 레벨 + 초월 보너스 합산된 BuffSet 반환
        /// </summary>
        public BuffSet GetTotalBonus(bool isEnhanced, int transcendLevel)
        {
            var result = new BuffSet();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.Bonus != null) result.Add(levelData.Bonus);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Bonus != null) result.Add(transcend.Bonus);

            return result;
        }
    }

    /// <summary>
    /// 스킬 레벨별 데이터 (0=기본, 1=강화)
    /// </summary>
    public class SkillLevelData
    {
        // 배율
        public double Ratio { get; set; }               // 공격력 비례
        public double DefRatio { get; set; }            // 방어력 비례
        public double HpRatio { get; set; }             // 생명력 비례
        public double SpdRatio { get; set; }            // 속공 비례

        // 조건부 효과 (체력 30% 미만 등)
        public double ConditionalRatioBonus { get; set; }
        public double ConditionalExtraDmg { get; set; }
        public string ConditionalDesc { get; set; }

        // 스탯 보너스 (방관, 치피, 약점 등 모두 여기로)
        public BuffSet Bonus { get; set; } = new BuffSet();

        // 아군에게 부여하는 버프 (비스킷 장비강화 등)
        public BuffSet BuffEffect { get; set; }

        // 적에게 부여하는 디버프
        public DebuffSet DebuffEffect { get; set; }
        public int EffectDuration { get; set; }
        public double EffectChance { get; set; } = 100;

        // 회복
        public double HealAtkRatio { get; set; }
        public double HealDefRatio { get; set; }
        public double HealHpRatio { get; set; }

        public string Effect { get; set; }
    }

    /// <summary>
    /// 스킬 초월 보너스
    /// </summary>
    public class SkillTranscend
    {
        public BuffSet Bonus { get; set; } = new BuffSet();
        public DebuffSet Debuff { get; set; } = new DebuffSet();
        public int? TargetCountOverride { get; set; }
        public string Effect { get; set; }
    }

    public enum SkillType
    {
        Normal,
        Skill1,
        Skill2,
        Ultimate
    }

    #endregion

    #region 패시브

    public class Passive
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxStacks { get; set; } = 1;

        public Dictionary<int, PassiveLevelData> LevelData { get; set; } = new();
        public Dictionary<int, PassiveTranscend> TranscendBonuses { get; set; } = new();

        public PassiveLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new PassiveLevelData();
        }

        public PassiveTranscend GetTranscendBonus(int level)
        {
            var result = new PassiveTranscend();
            if (TranscendBonuses == null) return result;

            foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
            {
                result.Buff.Add(kvp.Value.Buff);
                result.Debuff.Add(kvp.Value.Debuff);
                result.Effect = kvp.Value.Effect;
            }
            return result;
        }

        /// <summary>
        /// 패시브 레벨 + 초월 합산 버프
        /// </summary>
        public BuffSet GetTotalBuff(bool isEnhanced, int transcendLevel)
        {
            var result = new BuffSet();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.Buff != null) result.Add(levelData.Buff);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Buff != null) result.Add(transcend.Buff);

            return result;
        }

        /// <summary>
        /// 패시브 레벨 + 초월 합산 디버프
        /// </summary>
        public DebuffSet GetTotalDebuff(bool isEnhanced, int transcendLevel)
        {
            var result = new DebuffSet();

            var levelData = GetLevelData(isEnhanced);
            if (levelData.Debuff != null) result.Add(levelData.Debuff);

            var transcend = GetTranscendBonus(transcendLevel);
            if (transcend.Debuff != null) result.Add(transcend.Debuff);

            return result;
        }
    }

    /// <summary>
    /// 패시브 레벨별 데이터
    /// </summary>
    public class PassiveLevelData
    {
        public BuffSet Buff { get; set; } = new BuffSet();
        public DebuffSet Debuff { get; set; } = new DebuffSet();
        public string Effect { get; set; }
    }

    /// <summary>
    /// 패시브 초월 보너스
    /// </summary>
    public class PassiveTranscend
    {
        public BuffSet Buff { get; set; } = new BuffSet();
        public DebuffSet Debuff { get; set; } = new DebuffSet();
        public string Effect { get; set; }
    }

    #endregion

    #region 버프/디버프

    /// <summary>
    /// 버프 (모든 스탯 보너스 통합)
    /// </summary>
    public class BuffSet
    {
        // 기본 스탯
        public double Atk_Rate { get; set; }        // 공격력%
        public double Def_Rate { get; set; }        // 방어력%
        public double Hp_Rate { get; set; }         // 체력%

        // 치명타
        public double Cri { get; set; }             // 치명타 확률
        public double Cri_Dmg { get; set; }         // 치명타 피해

        // 약점
        public double Wek { get; set; }             // 약점 확률
        public double Wek_Dmg { get; set; }         // 약점 피해
        public double WekBonusDmg { get; set; }     // 약점 시 추가 피해 배율

        // 피해량
        public double Dmg_Dealt { get; set; }       // 피해량%
        public double Dmg_Dealt_Bos { get; set; }   // 보스 피해량%
        public double Dmg_Dealt_1to3 { get; set; }  // 1-3인기 피해량%
        public double Dmg_Dealt_4to5 { get; set; }  // 4-5인기 피해량%

        // 방어 관련
        public double Arm_Pen { get; set; }         // 방어 관통%
        public double Dmg_Rdc { get; set; }         // 받는 피해 감소%
        public double Dmg_Rdc_Multi { get; set; }   // 5인기 받피감%

        // 기타
        public double Heal_Bonus { get; set; }      // 받는 회복량%
        public double Eff_Res { get; set; }         // 효과 저항%
        public double Shield_HpRatio { get; set; }  // 보호막%

        public void Add(BuffSet other)
        {
            if (other == null) return;

            Atk_Rate += other.Atk_Rate;
            Def_Rate += other.Def_Rate;
            Hp_Rate += other.Hp_Rate;
            Cri += other.Cri;
            Cri_Dmg += other.Cri_Dmg;
            Wek += other.Wek;
            Wek_Dmg += other.Wek_Dmg;
            WekBonusDmg += other.WekBonusDmg;
            Dmg_Dealt += other.Dmg_Dealt;
            Dmg_Dealt_Bos += other.Dmg_Dealt_Bos;
            Dmg_Dealt_1to3 += other.Dmg_Dealt_1to3;
            Dmg_Dealt_4to5 += other.Dmg_Dealt_4to5;
            Arm_Pen += other.Arm_Pen;
            Dmg_Rdc += other.Dmg_Rdc;
            Dmg_Rdc_Multi += other.Dmg_Rdc_Multi;
            Heal_Bonus += other.Heal_Bonus;
            Eff_Res += other.Eff_Res;
            Shield_HpRatio += other.Shield_HpRatio;
        }

        public void MaxMerge(BuffSet other)
        {
            if (other == null) return;

            Atk_Rate = Math.Max(Atk_Rate, other.Atk_Rate);
            Def_Rate = Math.Max(Def_Rate, other.Def_Rate);
            Hp_Rate = Math.Max(Hp_Rate, other.Hp_Rate);
            Cri = Math.Max(Cri, other.Cri);
            Cri_Dmg = Math.Max(Cri_Dmg, other.Cri_Dmg);
            Wek = Math.Max(Wek, other.Wek);
            Wek_Dmg = Math.Max(Wek_Dmg, other.Wek_Dmg);
            WekBonusDmg = Math.Max(WekBonusDmg, other.WekBonusDmg);
            Dmg_Dealt = Math.Max(Dmg_Dealt, other.Dmg_Dealt);
            Dmg_Dealt_Bos = Math.Max(Dmg_Dealt_Bos, other.Dmg_Dealt_Bos);
            Dmg_Dealt_1to3 = Math.Max(Dmg_Dealt_1to3, other.Dmg_Dealt_1to3);
            Dmg_Dealt_4to5 = Math.Max(Dmg_Dealt_4to5, other.Dmg_Dealt_4to5);
            Arm_Pen = Math.Max(Arm_Pen, other.Arm_Pen);
            Dmg_Rdc = Math.Max(Dmg_Rdc, other.Dmg_Rdc);
            Dmg_Rdc_Multi = Math.Max(Dmg_Rdc_Multi, other.Dmg_Rdc_Multi);
            Heal_Bonus = Math.Max(Heal_Bonus, other.Heal_Bonus);
            Eff_Res = Math.Max(Eff_Res, other.Eff_Res);
            Shield_HpRatio = Math.Max(Shield_HpRatio, other.Shield_HpRatio);
        }

        public BuffSet Clone()
        {
            return new BuffSet
            {
                Atk_Rate = Atk_Rate,
                Def_Rate = Def_Rate,
                Hp_Rate = Hp_Rate,
                Cri = Cri,
                Cri_Dmg = Cri_Dmg,
                Wek = Wek,
                Wek_Dmg = Wek_Dmg,
                WekBonusDmg = WekBonusDmg,
                Dmg_Dealt = Dmg_Dealt,
                Dmg_Dealt_Bos = Dmg_Dealt_Bos,
                Dmg_Dealt_1to3 = Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = Dmg_Dealt_4to5,
                Arm_Pen = Arm_Pen,
                Dmg_Rdc = Dmg_Rdc,
                Dmg_Rdc_Multi = Dmg_Rdc_Multi,
                Heal_Bonus = Heal_Bonus,
                Eff_Res = Eff_Res,
                Shield_HpRatio = Shield_HpRatio
            };
        }
    }

    /// <summary>
    /// 디버프
    /// </summary>
    public class DebuffSet
    {
        public double Def_Reduction { get; set; }       // 방어력 감소%
        public double Dmg_Taken_Increase { get; set; }  // 받는 피해 증가%
        public double Vulnerability { get; set; }       // 취약%
        public double Atk_Reduction { get; set; }       // 공격력 감소%
        public double Spd_Reduction { get; set; }       // 속도 감소%
        public double Dmg_Reduction { get; set; }       // 주는 피해량 감소%
        public double Heal_Reduction { get; set; }      // 회복량 감소%

        public void Add(DebuffSet other)
        {
            if (other == null) return;

            Def_Reduction += other.Def_Reduction;
            Dmg_Taken_Increase += other.Dmg_Taken_Increase;
            Vulnerability += other.Vulnerability;
            Atk_Reduction += other.Atk_Reduction;
            Spd_Reduction += other.Spd_Reduction;
            Dmg_Reduction += other.Dmg_Reduction;
            Heal_Reduction += other.Heal_Reduction;
        }

        public void MaxMerge(DebuffSet other)
        {
            if (other == null) return;

            Def_Reduction = Math.Max(Def_Reduction, other.Def_Reduction);
            Dmg_Taken_Increase = Math.Max(Dmg_Taken_Increase, other.Dmg_Taken_Increase);
            Vulnerability = Math.Max(Vulnerability, other.Vulnerability);
            Atk_Reduction = Math.Max(Atk_Reduction, other.Atk_Reduction);
            Spd_Reduction = Math.Max(Spd_Reduction, other.Spd_Reduction);
            Dmg_Reduction = Math.Max(Dmg_Reduction, other.Dmg_Reduction);
            Heal_Reduction = Math.Max(Heal_Reduction, other.Heal_Reduction);
        }

        public DebuffSet Clone()
        {
            return new DebuffSet
            {
                Def_Reduction = Def_Reduction,
                Dmg_Taken_Increase = Dmg_Taken_Increase,
                Vulnerability = Vulnerability,
                Atk_Reduction = Atk_Reduction,
                Spd_Reduction = Spd_Reduction,
                Dmg_Reduction = Dmg_Reduction,
                Heal_Reduction = Heal_Reduction
            };
        }
    }

    #endregion
}

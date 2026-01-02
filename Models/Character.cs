using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 캐릭터 모델
    /// </summary>
    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }       // 전설, 영웅
        public string Type { get; set; }        // 공격형, 만능형, 지원형, 방어형

        // 스킬 목록
        public List<Skill> Skills { get; set; } = new List<Skill>();

        // 패시브
        public Passive Passive { get; set; }

        // 초월 단계별 스탯 증가
        public List<TranscendBonus> TranscendBonuses { get; set; } = new List<TranscendBonus>();

        // 현재 초월 단계 (0~12)
        public int TranscendLevel { get; set; } = 0;

        // 스킬 강화 여부
        public bool IsSkillEnhanced { get; set; } = false;

        /// <summary>
        /// 기본 스탯 가져오기 (등급 + 타입 기반)
        /// </summary>
        public BaseStatSet GetBaseStats()
        {
            if (Database.StatTable.AllBaseStats.TryGetValue(Grade, out var gradeStats))
            {
                if (gradeStats.TryGetValue(Type, out var typeStats))
                {
                    return typeStats.Clone();
                }
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 초월 보너스 스탯 계산
        /// </summary>
        public BaseStatSet GetTranscendStats()
        {
            BaseStatSet bonus = new BaseStatSet();
            
            for (int i = 1; i <= TranscendLevel; i++)
            {
                var transcend = TranscendBonuses.FirstOrDefault(t => t.Level == i);
                if (transcend != null)
                {
                    bonus.Add(transcend.BonusStats);
                }
            }
            
            return bonus;
        }

        /// <summary>
        /// 초월 보너스 스탯 계산 (레벨 직접 지정) ← 이거만 추가!
        /// </summary>
        public BaseStatSet GetTranscendStats(int level)
        {
            BaseStatSet bonus = new BaseStatSet();
            
            for (int i = 1; i <= level; i++)
            {
                var transcend = TranscendBonuses.FirstOrDefault(t => t.Level == i);
                if (transcend != null)
                {
                    bonus.Add(transcend.BonusStats);
                }
            }
            
            return bonus;
        }

        /// <summary>
        /// 최종 기본 스탯 (기본 + 초월)
        /// </summary>
        public BaseStatSet GetFinalBaseStats()
        {
            var stats = GetBaseStats();
            stats.Add(GetTranscendStats());
            return stats;
        }
    }

    /// <summary>
    /// 스킬 모델
    /// </summary>
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // 스킬 배율 (1.0 = 100%)
        public double Ratio { get; set; }
        public double RatioEnhanced { get; set; }   // 강화 시 배율

        // 방어 무시 (%)
        public double ArmorPen { get; set; }
        public double ArmorPenEnhanced { get; set; }

        // 추가 치명타 피해 (%)
        public double BonusCriDmg { get; set; }
        public double BonusCriDmgEnhanced { get; set; }

        // 조건부 추가 피해 (체력 30% 미만 등)
        public double ConditionalDmg { get; set; }
        public double ConditionalDmgEnhanced { get; set; }
        public string ConditionalDesc { get; set; }     // "체력 30% 미만"

        // 스킬 타입
        public SkillType SkillType { get; set; }

        // 타겟 수 (1=단일, 5=전체 등)
        public int TargetCount { get; set; } = 1;

        /// <summary>
        /// 강화 여부에 따른 실제 배율 반환
        /// </summary>
        public double GetRatio(bool isEnhanced)
        {
            return isEnhanced ? RatioEnhanced : Ratio;
        }

        public double GetArmorPen(bool isEnhanced)
        {
            return isEnhanced ? ArmorPenEnhanced : ArmorPen;
        }

        public double GetBonusCriDmg(bool isEnhanced)
        {
            return isEnhanced ? BonusCriDmgEnhanced : BonusCriDmg;
        }

        public double GetConditionalDmg(bool isEnhanced)
        {
            return isEnhanced ? ConditionalDmgEnhanced : ConditionalDmg;
        }
    }

    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Normal,     // 평타
        Skill1,     // 1스킬
        Skill2,     // 2스킬
        Ultimate    // 궁극기
    }

    /// <summary>
    /// 패시브 모델
    /// </summary>
    public class Passive
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // 패시브로 주는 스탯 보너스
        public BaseStatSet BonusStats { get; set; } = new BaseStatSet();

        // 적에게 주는 디버프 (받는 피해 증가 등)
        public BaseStatSet DebuffToEnemy { get; set; } = new BaseStatSet();

        // 중첩 수
        public int MaxStacks { get; set; } = 1;

        // 강화 시 추가 효과
        public BaseStatSet EnhancedBonusStats { get; set; } = new BaseStatSet();
    }

    /// <summary>
    /// 초월 단계별 보너스
    /// </summary>
    public class TranscendBonus
    {
        public int Level { get; set; }          // 1~12
        public BaseStatSet BonusStats { get; set; } = new BaseStatSet();
        public string SpecialEffect { get; set; }   // 특수 효과 설명 (6초월 방무 등)
    }
}

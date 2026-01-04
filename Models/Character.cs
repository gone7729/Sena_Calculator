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
        public BaseStatSet GetTranscendStats(int level)
        {
            BaseStatSet result = new BaseStatSet();
        
            if (level <= 0) return result;
        
            // 1~6 캐릭터 고유 초월 (누적 합산)
            foreach (var bonus in TranscendBonuses)
            {
                if (bonus.Level <= level)
                {
                    result.Add(bonus.BonusStats);
                }
            }
        
            // 7~12 공통 초월 (누적 합산)
            foreach (var bonus in StatTable.TranscendDb.CommonBonuses)  // ← 여기 수정!
            {
                if (bonus.Level <= level)
                {
                    result.Add(bonus.BonusStats);
                }
            }
        
            return result;
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
        public SkillType SkillType { get; set; }
        public int TargetCount { get; set; } = 1;
        public int Atk_Count { get; set; } = 1;
        public string ConditionalDesc { get; set; }
    
        // 레벨별 스탯 (0=기본, 1=강화)
        public Dictionary<int, SkillLevelData> LevelData { get; set; } = new Dictionary<int, SkillLevelData>();
        
        // 초월별 추가 효과
        public Dictionary<int, SkillTranscendBonus> TranscendBonuses { get; set; } = new Dictionary<int, SkillTranscendBonus>();
    
        // ===== 헬퍼 메서드 =====
        public SkillLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new SkillLevelData();
        }
    
        public SkillTranscendBonus GetTranscendBonus(int level)
        {
            SkillTranscendBonus result = new SkillTranscendBonus();
            if (TranscendBonuses == null) return result;
    
            // 해당 레벨 이하의 모든 보너스 누적
            foreach (var kvp in TranscendBonuses.Where(t => t.Key <= level).OrderBy(t => t.Key))
            {
                result.ArmorPen += kvp.Value.ArmorPen;
                result.BonusDmg += kvp.Value.BonusDmg;
                result.BonusCriDmg += kvp.Value.BonusCriDmg;
                result.Effect = kvp.Value.Effect;  // 마지막 효과 설명 사용
            }
            return result;
        }
    
        // 편의 메서드
        public double GetRatio(bool isEnhanced) 
            => GetLevelData(isEnhanced).Ratio;
    
        public double GetArmorPen(bool isEnhanced, int transcendLevel)
            => GetLevelData(isEnhanced).ArmorPen + GetTranscendBonus(transcendLevel).ArmorPen;
    
        public double GetBonusCriDmg(bool isEnhanced, int transcendLevel)
            => GetLevelData(isEnhanced).BonusCriDmg + GetTranscendBonus(transcendLevel).BonusCriDmg;
    
        public double GetConditionalRatioBonus(bool isEnhanced)
            => GetLevelData(isEnhanced).ConditionalRatioBonus;

        public double GetConditionalExtraDmg(bool isEnhanced)
            => GetLevelData(isEnhanced).ConditionalExtraDmg;
    }
    
    /// <summary>
    /// 스킬 레벨별 데이터
    /// </summary>
    public class SkillLevelData
    {
        public double Ratio { get; set; }               // 기본 스킬 배율

        // 추가 스케일링 (다른 스탯 기준)
        public double DefRatio { get; set; }        // 방어력 비례 배율
        public double HpRatio { get; set; }         // 생명력 비례 배율
        public double SpdRatio { get; set; }        // 속공 비례 배율
        public double ArmorPen { get; set; }            // 방어 관통
        public double BonusCriDmg { get; set; }         // 추가 치피

        // 조건부 효과 (체력 30% 미만 등)
        public double ConditionalRatioBonus { get; set; }   // 조건 시 배율 증가 (예: +50% → 0.50)
        public double ConditionalExtraDmg { get; set; }     // 조건 시 추가 피해 (공격력의 X%)

        // 스킬이 부여하는 버프/디버프
        public BaseStatSet BuffEffect { get; set; }      // 아군에게 버프
        public DebuffSet DebuffEffect { get; set; }      // 적에게 디버프
        public int EffectDuration { get; set; }          // 지속 턴
        public double EffectChance { get; set; } = 100;  // 적용 확률%
    }
    
    /// <summary>
    /// 스킬 초월 보너스
    /// </summary>
    public class SkillTranscendBonus
    {
        public double ArmorPen { get; set; }
        public double BonusDmg { get; set; }
        public double BonusCriDmg { get; set; }
        public string Effect { get; set; }
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
        public int MaxStacks { get; set; } = 1;
        
        // Target 제거 (데이터 유무로 판단)
        // public PassiveTarget Target { get; set; }  ← 삭제
        
        public Dictionary<int, PassiveLevelData> LevelData { get; set; } = new Dictionary<int, PassiveLevelData>();
        public Dictionary<int, PassiveTranscendBonus> TranscendBonuses { get; set; } = new Dictionary<int, PassiveTranscendBonus>();

        // ===== 헬퍼 메서드 =====
        public PassiveLevelData GetLevelData(bool isEnhanced)
        {
            int key = isEnhanced ? 1 : 0;
            return LevelData.TryGetValue(key, out var data) ? data : new PassiveLevelData();  // 없으면 빈 객체!
        }

        public PassiveTranscendBonus GetTranscendBonus(int transcendLevel)
        {
            PassiveTranscendBonus result = new PassiveTranscendBonus();
            if (TranscendBonuses == null) return result;

            // 해당 레벨 이하의 모든 보너스 누적
            foreach (var kvp in TranscendBonuses.Where(t => t.Key <= transcendLevel).OrderBy(t => t.Key))
            {
                result.BuffModifier.Add(kvp.Value.BuffModifier);
                result.DebuffModifier.Add(kvp.Value.DebuffModifier);
                result.Effect = kvp.Value.Effect;
            }
            return result;
        }

        // 편의 메서드 (스킬 레벨 + 초월 합산)
        public BaseStatSet GetBuffModifier(bool isEnhanced, int transcendLevel)
        {
            var result = new BaseStatSet();
            var levelData = GetLevelData(isEnhanced);
            var transcendBonus = GetTranscendBonus(transcendLevel);

            if (levelData.BuffModifier != null) result.Add(levelData.BuffModifier);
            if (transcendBonus.BuffModifier != null) result.Add(transcendBonus.BuffModifier);

            return result;
        }

        public DebuffSet GetDebuffModifier(bool isEnhanced, int transcendLevel)
        {
            var result = new DebuffSet();
            var levelData = GetLevelData(isEnhanced);
            var transcendBonus = GetTranscendBonus(transcendLevel);

            if (levelData.DebuffModifier != null) result.Add(levelData.DebuffModifier);
            if (transcendBonus.DebuffModifier != null) result.Add(transcendBonus.DebuffModifier);

            return result;
        }
    }

    /// <summary>
    /// 패시브 레벨별 데이터 (0=기본, 1=강화)
    /// </summary>
    public class PassiveLevelData
    {
        public BaseStatSet BuffModifier { get; set; } = new BaseStatSet();
        public DebuffSet DebuffModifier { get; set; } = new DebuffSet();
        public string Effect { get; set; }
    }

    /// <summary>
    /// 패시브 초월 보너스
    /// </summary>
    public class PassiveTranscendBonus
    {
        public BaseStatSet BuffModifier { get; set; } = new BaseStatSet();
        public DebuffSet DebuffModifier { get; set; } = new DebuffSet();
        public string Effect { get; set; }
    }
    
    public enum PassiveTarget { Self, Ally, Enemy }

    /// <summary>
    /// 초월 단계별 보너스
    /// </summary>
    public class TranscendBonus
    {
        public int Level { get; set; }          // 1~12
        public BaseStatSet BonusStats { get; set; } = new BaseStatSet();
        public string SpecialEffect { get; set; }   // 특수 효과 설명 (6초월 방무 등)
    }

    /// <summary>
    /// 적에게 거는 디버프 (패시브/스킬)
    /// </summary>
    public class DebuffSet
    {
        public double Def_Reduction { get; set; }       // 방어력 감소%
        public double Dmg_Taken_Increase { get; set; }  // 받는 피해 증가%
        public double Vulnerability { get; set; }       // 취약%
        public double Atk_Reduction { get; set; }       // 공격력 감소%
        public double Spd_Reduction { get; set; }       // 속도 감소%
        public double Dmg_Reduction { get; set; }       // 주는 피해량 감소% ← 추가
    
        public void Add(DebuffSet other)
        {
            if (other == null) return;
            
            Def_Reduction += other.Def_Reduction;
            Dmg_Taken_Increase += other.Dmg_Taken_Increase;
            Vulnerability += other.Vulnerability;
            Atk_Reduction += other.Atk_Reduction;
            Spd_Reduction += other.Spd_Reduction;
        }
    
        public DebuffSet Clone()
        {
            return new DebuffSet
            {
                Def_Reduction = this.Def_Reduction,
                Dmg_Taken_Increase = this.Dmg_Taken_Increase,
                Vulnerability = this.Vulnerability,
                Atk_Reduction = this.Atk_Reduction,
                Spd_Reduction = this.Spd_Reduction
            };
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 초월 타입
    /// </summary>
    public enum TranscendType
    {
        AtkCri,      // 공격 + 치확
        AtkCriDmg,   // 공격 + 치피
        AtkWek,      // 공격 + 약점
        AtkEff,      // 공격 + 효적
        AtkDmgRdc,   // 공격 + 피감
        DefBlk,      // 방어 + 막기
        DefDmgRdc    // 방어 + 피감
    }

    /// <summary>
    /// 공격 타입 — 데미지 계산에 사용할 공격력 종류
    /// </summary>
    public enum AttackType
    {
        Physical,   // 물리 공격력 (Atk)
        Magic       // 마법 공격력 (MagicAtk)
    }

    /// <summary>
    /// 캐릭터
    /// </summary>
    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }       // 전설, 영웅
        public string Type { get; set; }        // 공격형, 마법형, 만능형, 지원형, 방어형
        public AttackType AttackType { get; set; } = AttackType.Physical;  // 물리 또는 마법 (CharacterDB에서 명시)

        public List<Skill> Skills { get; set; } = new List<Skill>();
        public Passive Passive { get; set; }

        /// <summary>
        /// 주 사용처 (공성전 / 모험·스토리 / 강림-태오 / 결투장 …).
        /// 시뮬 계산에는 쓰이지 않는 표시용 메타데이터.
        /// 값의 기준 목록·저장소는 웹 쪽: web/src/data/usageCategories.ts, heroUsage.json
        /// (웹 영웅 페이지에서 편집 → heroUsage.json에 저장 → export 시 characters.json에 병합)
        /// </summary>
        public List<string> MainUsages { get; set; } = new List<string>();
        
        // 초월 타입 (TranscendDb에서 보너스 가져옴)
        public TranscendType TranscendType { get; set; } = TranscendType.AtkCri;

        // 런타임 상태
        public int TranscendLevel { get; set; } = 0;
        public bool IsSkillEnhanced { get; set; } = false;

        /// <summary>
        /// 전용무기 (캐릭터별 또는 공용). null이면 미장착.
        /// 시뮬은 항상 풀강(+15) 가정 — Atk flat 247 + 조율 4슬롯.
        /// </summary>
        public ExclusiveWeapon ExclusiveWeapon { get; set; }

        /// <summary>
        /// 등급/타입에 따른 기본 스탯 가져오기
        /// </summary>
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

        /// <summary>
        /// 초월 타입에 따른 고유 보너스 리스트 (1~6) - 등급별 분기
        /// </summary>
        private List<TranscendBonus> GetUniqueBonuses()
        {
            // 전설 등급
            if (Grade == "전설")
            {
                return TranscendType switch
                {
                    TranscendType.AtkCri => StatTable.TranscendDb.Legendary.AtkCriBonuses,
                    TranscendType.AtkCriDmg => StatTable.TranscendDb.Legendary.AtkCriDmgBonuses,
                    TranscendType.AtkWek => StatTable.TranscendDb.Legendary.AtkWekBonuses,
                    TranscendType.AtkEff => StatTable.TranscendDb.Legendary.AtkEffBonuses,
                    TranscendType.AtkDmgRdc => StatTable.TranscendDb.Legendary.AtkDmgRdcBonuses,
                    TranscendType.DefBlk => StatTable.TranscendDb.Legendary.DefBlkBonuses,
                    TranscendType.DefDmgRdc => StatTable.TranscendDb.Legendary.DefDmgRdcBonuses,
                    _ => StatTable.TranscendDb.Legendary.AtkCriBonuses
                };
            }
            // 영웅 등급
            else if (Grade == "희귀")
            {
                return TranscendType switch
                {
                    TranscendType.AtkCri => StatTable.TranscendDb.Rare.AtkCriBonuses,
                    TranscendType.AtkCriDmg => StatTable.TranscendDb.Rare.AtkCriDmgBonuses,
                    TranscendType.AtkWek => StatTable.TranscendDb.Rare.AtkWekBonuses,
                    TranscendType.AtkEff => StatTable.TranscendDb.Rare.AtkEffBonuses,
                    TranscendType.AtkDmgRdc => StatTable.TranscendDb.Rare.AtkDmgRdcBonuses,
                    TranscendType.DefBlk => StatTable.TranscendDb.Rare.DefBlkBonuses,
                    TranscendType.DefDmgRdc => StatTable.TranscendDb.Rare.DefDmgRdcBonuses,
                    _ => StatTable.TranscendDb.Rare.AtkCriBonuses
                };
            }
            // 기본값 (전설)
            return StatTable.TranscendDb.Legendary.AtkCriBonuses;
        }

        /// <summary>
        /// 공통 보너스 리스트 (7~12) - 등급별 분기
        /// </summary>
        private List<TranscendBonus> GetCommonBonuses()
        {
            if (Grade == "희귀")
                return StatTable.TranscendDb.Rare.CommonBonuses;
            return StatTable.TranscendDb.Legendary.CommonBonuses;
        }

        /// <summary>
        /// 초월 타입에 따른 전체 보너스 리스트 (1~12)
        /// </summary>
        public List<TranscendBonus> GetTranscendBonuses()
        {
            var uniqueBonuses = GetUniqueBonuses();
            var commonBonuses = GetCommonBonuses();
            
            var full = new List<TranscendBonus>(uniqueBonuses);
            full.AddRange(commonBonuses);
            return full;
        }

        /// <summary>
        /// 초월 레벨에 따른 스탯 보너스 가져오기
        /// </summary>
        public BaseStatSet GetTranscendStats(int level)
        {
            var result = new BaseStatSet();
            if (level <= 0) return result;

            var allBonuses = GetTranscendBonuses();
            foreach (var bonus in allBonuses.Where(b => b.Level <= level))
            {
                result.Add(bonus.BonusStats);
            }

            return result;
        }

        /// <summary>
        /// 이름으로 스킬 찾기
        /// </summary>
        public Skill GetSkillByName(string skillName)
        {
            return Skills.FirstOrDefault(s => s.Name == skillName);
        }

        /// <summary>
        /// 타입으로 스킬 찾기
        /// </summary>
        public Skill GetSkillByType(SkillType skillType)
        {
            return Skills.FirstOrDefault(s => s.SkillType == skillType);
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
}
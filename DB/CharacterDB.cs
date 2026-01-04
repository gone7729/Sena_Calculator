using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 캐릭터 데이터베이스
    /// 세븐나이츠 리버스 영웅 목록
    /// 등급: 전설, 희귀
    /// 유형: 공격형, 마법형, 방어형, 지원형, 만능형
    /// </summary>
    public static class CharacterDb
    {
        public static readonly List<Character> Characters = new List<Character>
        {
            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ★ 전설 등급 - 공격형 ★                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 타카 =====
            new Character
            {
                Id = 1,
                Name = "타카",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = 1,
                        Name = "평타",
                        SkillType = SkillType.Normal,
                        Ratio = 1.0,
                        RatioEnhanced = 1.2,
                        TargetCount = 1
                    },
                    new Skill
                    {
                        Id = 2,
                        Name = "바람의 칼날",
                        SkillType = SkillType.Skill1,
                        Ratio = 1.45,
                        RatioEnhanced = 1.70,
                        BonusCriDmg = 0.37,
                        BonusCriDmgEnhanced = 0.46,
                        ConditionalDmg = 0.45,
                        ConditionalDmgEnhanced = 0.55,
                        ConditionalDesc = "체력 30% 미만",
                        TargetCount = 3
                    },
                    new Skill
                    {
                        Id = 3,
                        Name = "죽음의 무도",
                        SkillType = SkillType.Skill2,
                        Ratio = 1.45,
                        RatioEnhanced = 1.70,
                        ArmorPen = 0,
                        ArmorPenEnhanced = 0.40,
                        ConditionalDmg = 2.60,
                        ConditionalDmgEnhanced = 2.60,
                        ConditionalDesc = "체력 30% 미만",
                        TargetCount = 3
                    }
                },
                Passive = new Passive
                {
                    Name = "매의 발톱",
                    Target = PassiveTarget.Enemy,
                    StatModifier = new BaseStatSet { Dmg_Taken = 4 },
                    MaxStacks = 8
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "매의 발톱 받는 물리 피해 3% → 4%" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "죽음의 무도 방어력 40% 무시" }
                }
            },

            // ===== 제이브 (세븐나이츠) =====
            new Character
            {
                Id = 2,
                Name = "제이브",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "화염 일격", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 5, Description = "화상 부여" },
                    new Skill { Id = 3, Name = "용의 숨결", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "기절 부여" }
                },
                Passive = new Passive { Name = "고대용의 힘" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "행동 제어 면역" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "화상 강화" }
                }
            },

            // ===== 델론즈 (세븐나이츠) =====
            new Character
            {
                Id = 3,
                Name = "델론즈",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "침묵의 낫", SkillType = SkillType.Skill1, Ratio = 1.45, TargetCount = 5, Description = "침묵 부여" },
                    new Skill { Id = 3, Name = "죽음의 일격", SkillType = SkillType.Skill2, Ratio = 2.50, TargetCount = 1, Description = "처치 시 연쇄 발동" }
                },
                Passive = new Passive 
                { 
                    Name = "죽음의 권능", 
                    Target = PassiveTarget.Ally,
                    StatModifier = new BaseStatSet { Dmg_Dealt = 20 }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "죽음의 일격 연쇄 발동" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "아군 사망 시 피해 무효화" }
                }
            },

            // ===== 태오 (사황) =====
            new Character
            {
                Id = 4,
                Name = "태오",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "단죄의 검", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 5 },
                    new Skill { Id = 3, Name = "심판의 일격", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5 }
                },
                Passive = new Passive { Name = "불멸의 의지", Description = "모든 피해 무력화, 불사 2턴 지속" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 세인 =====
            new Character
            {
                Id = 5,
                Name = "세인",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "성스러운 일격", SkillType = SkillType.Skill1, Ratio = 2.0, TargetCount = 1 },
                    new Skill { Id = 3, Name = "심판의 빛", SkillType = SkillType.Skill2, Ratio = 2.5, TargetCount = 1 }
                },
                Passive = new Passive { Name = "성십자단의 힘", Description = "단일 대상 피해 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 비담 =====
            new Character
            {
                Id = 6,
                Name = "비담",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "혈풍", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "출혈 부여" },
                    new Skill { Id = 3, Name = "폭혈참", SkillType = SkillType.Skill2, Ratio = 1.70, TargetCount = 3, Description = "출혈 스택 폭발" }
                },
                Passive = new Passive { Name = "검귀의 칼날", Description = "출혈 스택에 비례한 추가 피해" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "디버프 해제" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 발리스타 =====
            new Character
            {
                Id = 7,
                Name = "발리스타",
                Grade = "전설",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "관통 사격", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "폭발 화살", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3 }
                },
                Passive = new Passive { Name = "명사수", Description = "치명타 확률 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ★ 전설 등급 - 마법형 ★                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 바네사 (사황) =====
            new Character
            {
                Id = 10,
                Name = "바네사",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "불꽃 폭발", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "마력 폭주", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 4, Description = "쿨타임 증가" }
                },
                Passive = new Passive 
                { 
                    Name = "화염의 여왕",
                    Target = PassiveTarget.Enemy,
                    StatModifier = new BaseStatSet { Def_Red = 24 }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "적군 버프 해제" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "전투 시작 시 디버프" }
                }
            },

            // ===== 실베스타 =====
            new Character
            {
                Id = 11,
                Name = "실베스타",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "어둠의 화살", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 1, Description = "연쇄 공격" },
                    new Skill { Id = 3, Name = "그림자 폭풍", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "턴제 버프 감소" }
                },
                Passive = new Passive { Name = "어둠의 군주", Description = "단일/연쇄 딜러" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "방어 무시 피해 추가" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "생명력 회복" }
                }
            },

            // ===== 연희 =====
            new Character
            {
                Id = 12,
                Name = "연희",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "종말의 영면", SkillType = SkillType.Skill1, Ratio = 1.80, TargetCount = 5 },
                    new Skill { Id = 3, Name = "파멸의 꿈", SkillType = SkillType.Skill2, Ratio = 2.00, TargetCount = 5 }
                },
                Passive = new Passive 
                { 
                    Name = "영원한 꿈", 
                    Target = PassiveTarget.Ally,
                    StatModifier = new BaseStatSet { Atk_Rate = 25 }
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 파스칼 =====
            new Character
            {
                Id = 13,
                Name = "파스칼",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "마력 집중", SkillType = SkillType.Skill1, Ratio = 2.0, TargetCount = 1, Description = "방어력 감소" },
                    new Skill { Id = 3, Name = "파멸의 마법", SkillType = SkillType.Skill2, Ratio = 2.5, TargetCount = 1 }
                },
                Passive = new Passive 
                { 
                    Name = "대마법사", 
                    Target = PassiveTarget.Self,
                    StatModifier = new BaseStatSet { Atk_Rate = 33, Cri_Dmg = 40 } 
                },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 벨리카 =====
            new Character
            {
                Id = 14,
                Name = "벨리카",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "번개 폭풍", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 5 },
                    new Skill { Id = 3, Name = "천둥의 심판", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5 }
                },
                Passive = new Passive { Name = "번개의 마녀", Description = "치명타 확률 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 루리 =====
            new Character
            {
                Id = 15,
                Name = "루리",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "마력의 화살", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "신비의 폭발", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3 }
                },
                Passive = new Passive { Name = "정령의 축복", Description = "약점공격 확률 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 밀리아 (구세븐나이츠) =====
            new Character
            {
                Id = 16,
                Name = "밀리아",
                Grade = "전설",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "수정 화살", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "수정 결정" },
                    new Skill { Id = 3, Name = "수정 폭발", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "쿨타임 증가" }
                },
                Passive = new Passive { Name = "수정룡", Description = "마법형 아군 3명 배치 시 수정 결정 발동" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ★ 전설 등급 - 방어형 ★                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 루디 (세븐나이츠) =====
            new Character
            {
                Id = 20,
                Name = "루디",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "방어 준비", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 0, Description = "보호막 생성" },
                    new Skill { Id = 3, Name = "수호의 일격", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 1, Description = "기절 부여" }
                },
                Passive = new Passive { Name = "수호자의 맹세", Description = "아군 방어력 증가, 링크 효과" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "링크 턴 증가" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "행동 제어 면역 부여" }
                }
            },

            // ===== 크리스 (세븐나이츠) =====
            new Character
            {
                Id = 21,
                Name = "크리스",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1, Description = "즉사 확률" },
                    new Skill { Id = 2, Name = "죽음의 낫", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 1, Description = "즉사 부여" },
                    new Skill { Id = 3, Name = "지옥의 심판", SkillType = SkillType.Skill2, Ratio = 1.40, TargetCount = 5, Description = "즉사 부여" }
                },
                Passive = new Passive { Name = "지옥의 군주", Description = "불사 효과, 아군 회복" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "쿨타임 초기화" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 }, SpecialEffect = "즉사 부여 횟수 증가" }
                }
            },

            // ===== 손오공 =====
            new Character
            {
                Id = 22,
                Name = "손오공",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "여의봉 강타", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "분신술", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "분신 버프" }
                },
                Passive = new Passive { Name = "투전승불", Description = "공격력에 비례한 방어력 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 } }
                }
            },

            // ===== 겔리두스 (구세븐나이츠) =====
            new Character
            {
                Id = 23,
                Name = "겔리두스",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "빙극의 숨결", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "빙극 부여" },
                    new Skill { Id = 3, Name = "혹한의 분노", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "빙극 부여" }
                },
                Passive = new Passive { Name = "혹한의 강자", Description = "빙극 - 행동 불가, 해제 시 방어 무시 피해" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 } }
                }
            },

            // ===== 룩 =====
            new Character
            {
                Id = 24,
                Name = "룩",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "철벽 방어", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 0, Description = "방어력 증가" },
                    new Skill { Id = 3, Name = "돌진", SkillType = SkillType.Skill2, Ratio = 1.40, TargetCount = 3 }
                },
                Passive = new Passive { Name = "불굴의 의지", Description = "높은 방어력" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 } }
                }
            },

            // ===== 챈슬러 =====
            new Character
            {
                Id = 25,
                Name = "챈슬러",
                Grade = "전설",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "수호의 검", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "결의의 일격", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 1 }
                },
                Passive = new Passive { Name = "수호 기사", Description = "아군 보호" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 6 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ★ 전설 등급 - 지원형 ★                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 아일린 (세븐나이츠) =====
            new Character
            {
                Id = 30,
                Name = "아일린",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "번개 일격", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "감전 부여" },
                    new Skill { Id = 3, Name = "천둥의 분노", SkillType = SkillType.Skill2, Ratio = 1.40, TargetCount = 5, Description = "감전 부여" }
                },
                Passive = new Passive { Name = "포디나의 여제", Description = "아군 물리 공격력 증가, 1회 부활" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "관통 효과" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "감전 확률 증가" }
                }
            },

            // ===== 리나 =====
            new Character
            {
                Id = 31,
                Name = "리나",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "치유의 빛", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "성스러운 가호", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "피해량/효과저항 증가" }
                },
                Passive = new Passive { Name = "성녀의 축복", Description = "적 전열 방어력 감소" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 6 } }
                }
            },

            // ===== 엘리스 =====
            new Character
            {
                Id = 32,
                Name = "엘리스",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "회복의 빛", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "보호의 빛", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "보호막" }
                },
                Passive = new Passive { Name = "빛의 사제", Description = "순수 힐러, 생존력 특화" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 6 } }
                }
            },

            // ===== 로지 (구세븐나이츠) =====
            new Character
            {
                Id = 33,
                Name = "로지",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "달빛의 치유", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "달빛의 심판", SkillType = SkillType.Skill2, Ratio = 1.40, TargetCount = 5, Description = "즉사 부여" }
                },
                Passive = new Passive { Name = "달빛의 여신", Description = "즉사 파티 핵심" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 6 } }
                }
            },

            // ===== 플라튼 (다크나이츠) =====
            new Character
            {
                Id = 34,
                Name = "플라튼",
                Grade = "전설",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "암흑의 치유", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "어둠의 가호", SkillType = SkillType.Skill2, Ratio = 1.40, TargetCount = 5 }
                },
                Passive = new Passive { Name = "대사제", Description = "방어덱 지원 특화, 쫄작 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 6 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ★ 전설 등급 - 만능형 ★                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 레이첼 (세븐나이츠) =====
            new Character
            {
                Id = 40,
                Name = "레이첼",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "불꽃의 화살", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "화상 부여" },
                    new Skill { Id = 3, Name = "불멸의 화염", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3, Description = "공격력/반격 감소" }
                },
                Passive = new Passive { Name = "불멸의 여제", Description = "아군 약점공격 확률 증가" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "디버프 턴 증가" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "피해 감소 디버프" }
                }
            },

            // ===== 스파이크 (세븐나이츠) =====
            new Character
            {
                Id = 41,
                Name = "스파이크",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "혹한의 일격", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "빙결 부여 (방무)" },
                    new Skill { Id = 3, Name = "얼음의 심판", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5, Description = "빙결 부여" }
                },
                Passive = new Passive { Name = "혹한의 왕", Description = "아군 효과 저항 증가, 디버프 해제" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "권능 효과" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "생명력 회복" }
                }
            },

            // ===== 에이스 (사황) =====
            new Character
            {
                Id = 42,
                Name = "에이스",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "달빛 베기", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "방무, 회복량 감소, 막기 감소" },
                    new Skill { Id = 3, Name = "일도천화엽", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3, Description = "방무, 방어력 감소" }
                },
                Passive = new Passive { Name = "불굴의 지휘", Description = "물리 취약 디버프" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "아군 디버프 해제" },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 }, SpecialEffect = "물리 공격력 증가" }
                }
            },

            // ===== 에스파다 =====
            new Character
            {
                Id = 43,
                Name = "에스파다",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "검무", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "비검술", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3 }
                },
                Passive = new Passive { Name = "검의 달인", Description = "균형잡힌 스탯" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 유신 =====
            new Character
            {
                Id = 44,
                Name = "유신",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "섬광", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3 },
                    new Skill { Id = 3, Name = "천뢰", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 5 }
                },
                Passive = new Passive { Name = "번개의 검", Description = "빠른 속공" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ===== 녹스 =====
            new Character
            {
                Id = 45,
                Name = "녹스",
                Grade = "전설",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "죽음의 손길", SkillType = SkillType.Skill1, Ratio = 1.50, TargetCount = 3, Description = "즉사 부여" },
                    new Skill { Id = 3, Name = "암흑의 심판", SkillType = SkillType.Skill2, Ratio = 1.60, TargetCount = 3 }
                },
                Passive = new Passive { Name = "죽음의 사자", Description = "즉사 파티 핵심" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 6 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 18 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 18 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 12 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 6 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ☆ 희귀 등급 - 공격형 ☆                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 풍연 =====
            new Character
            {
                Id = 50,
                Name = "풍연",
                Grade = "희귀",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "질풍검", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "폭풍 참격", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "빙결 면역", Description = "빙결 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 쥬피 =====
            new Character
            {
                Id = 51,
                Name = "쥬피",
                Grade = "희귀",
                Type = "공격형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "저격", SkillType = SkillType.Skill1, Ratio = 1.80, TargetCount = 1 },
                    new Skill { Id = 3, Name = "관통 사격", SkillType = SkillType.Skill2, Ratio = 2.00, TargetCount = 1 }
                },
                Passive = new Passive { Name = "명사수", Description = "단일 대상 특화" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ☆ 희귀 등급 - 마법형 ☆                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 니아 =====
            new Character
            {
                Id = 55,
                Name = "니아",
                Grade = "희귀",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "마력 화살", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 5 },
                    new Skill { Id = 3, Name = "폭발 마법", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 5 }
                },
                Passive = new Passive { Name = "마력 보호막", Description = "전열 배치 시 보호막 충전" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 아리엘 =====
            new Character
            {
                Id = 56,
                Name = "아리엘",
                Grade = "희귀",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "정령의 화살", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "정령 폭발", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "화상 면역", Description = "화상 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 세라 =====
            new Character
            {
                Id = 57,
                Name = "세라",
                Grade = "희귀",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "빙결 마법", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "눈보라", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3, Description = "턴제 버프 감소" }
                },
                Passive = new Passive { Name = "얼음 마법사", Description = "3인기 마법 딜러" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 유리 =====
            new Character
            {
                Id = 58,
                Name = "유리",
                Grade = "희귀",
                Type = "마법형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "불꽃 마법", SkillType = SkillType.Skill1, Ratio = 1.80, TargetCount = 1 },
                    new Skill { Id = 3, Name = "화염 폭발", SkillType = SkillType.Skill2, Ratio = 2.00, TargetCount = 1 }
                },
                Passive = new Passive { Name = "화염 마법사", Description = "단일 대상 마법 딜러" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ☆ 희귀 등급 - 방어형 ☆                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 에반 =====
            new Character
            {
                Id = 60,
                Name = "에반",
                Grade = "희귀",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "방어 태세", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 보호" },
                    new Skill { Id = 3, Name = "돌진", SkillType = SkillType.Skill2, Ratio = 1.30, TargetCount = 3 }
                },
                Passive = new Passive { Name = "기절 면역", Description = "기절 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 5 } }
                }
            },

            // ===== 지크 =====
            new Character
            {
                Id = 61,
                Name = "지크",
                Grade = "희귀",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "철벽", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "방어력 증가" },
                    new Skill { Id = 3, Name = "강타", SkillType = SkillType.Skill2, Ratio = 1.30, TargetCount = 1 }
                },
                Passive = new Passive { Name = "기절 면역", Description = "기절 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 5 } }
                }
            },

            // ===== 리 =====
            new Character
            {
                Id = 62,
                Name = "리",
                Grade = "희귀",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "금강불괴", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 0, Description = "도발, 물리 피해 면역" },
                    new Skill { Id = 3, Name = "패도멸악권", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "응보의 진언", Description = "도발 및 물리 피해 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 5 } }
                }
            },

            // ===== 레오 =====
            new Character
            {
                Id = 63,
                Name = "레오",
                Grade = "희귀",
                Type = "방어형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "방패 방어", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 보호" },
                    new Skill { Id = 3, Name = "방패 강타", SkillType = SkillType.Skill2, Ratio = 1.30, TargetCount = 1 }
                },
                Passive = new Passive { Name = "즉사 면역", Description = "즉사 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Def_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Blk = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Def_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Def_Rate = 5 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ☆ 희귀 등급 - 지원형 ☆                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 유이 =====
            new Character
            {
                Id = 70,
                Name = "유이",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "치유", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "축복", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "버프" }
                },
                Passive = new Passive { Name = "화상 면역", Description = "화상 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ===== 카린 =====
            new Character
            {
                Id = 71,
                Name = "카린",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "성스러운 치유", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "성스러운 빛", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "보호막" }
                },
                Passive = new Passive { Name = "성녀", Description = "기본 힐러" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ===== 메이 =====
            new Character
            {
                Id = 72,
                Name = "메이",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "눈부신 빛", SkillType = SkillType.Skill1, Ratio = 1.20, TargetCount = 3, Description = "실명 부여" },
                    new Skill { Id = 3, Name = "치유의 빛", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "아군 회복" }
                },
                Passive = new Passive { Name = "빛의 사제", Description = "실명 부여 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ===== 클로에 =====
            new Character
            {
                Id = 73,
                Name = "클로에",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "회복", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 회복" },
                    new Skill { Id = 3, Name = "보호", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "보호막" }
                },
                Passive = new Passive { Name = "감전 면역", Description = "감전 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ===== 라니아 =====
            new Character
            {
                Id = 74,
                Name = "라니아",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "왕의 축복", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "아군 버프" },
                    new Skill { Id = 3, Name = "왕의 치유", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "아군 회복" }
                },
                Passive = new Passive { Name = "빙결 면역", Description = "빙결 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ===== 루시 =====
            new Character
            {
                Id = 75,
                Name = "루시",
                Grade = "희귀",
                Type = "지원형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "정화", SkillType = SkillType.Skill1, Ratio = 0, TargetCount = 5, Description = "버프 해제" },
                    new Skill { Id = 3, Name = "치유", SkillType = SkillType.Skill2, Ratio = 0, TargetCount = 5, Description = "아군 회복" }
                },
                Passive = new Passive { Name = "정화의 빛", Description = "적 버프 해제 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Hp_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Eff_Res = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Hp_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Hp_Rate = 5 } }
                }
            },

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║                    ☆ 희귀 등급 - 만능형 ☆                      ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ===== 진 =====
            new Character
            {
                Id = 80,
                Name = "진",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "검술", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "일섬", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 1 }
                },
                Passive = new Passive { Name = "침묵 면역", Description = "침묵 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 클레오 =====
            new Character
            {
                Id = 81,
                Name = "클레오",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "불꽃 마법", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 3 },
                    new Skill { Id = 3, Name = "화염 폭발", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "침묵 면역", Description = "침묵 상태 면역" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 아수라 =====
            new Character
            {
                Id = 82,
                Name = "아수라",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "분노", SkillType = SkillType.Skill1, Ratio = 1.40, TargetCount = 1, Description = "집중 공격" },
                    new Skill { Id = 3, Name = "파괴", SkillType = SkillType.Skill2, Ratio = 1.80, TargetCount = 1 }
                },
                Passive = new Passive { Name = "집중 공격", Description = "특정 대상 집중 공격" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 소이 =====
            new Character
            {
                Id = 83,
                Name = "소이",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "정화", SkillType = SkillType.Skill1, Ratio = 1.30, TargetCount = 3, Description = "버프 해제" },
                    new Skill { Id = 3, Name = "일격", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "정화", Description = "적 버프 해제 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 조커 =====
            new Character
            {
                Id = 84,
                Name = "조커",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "트릭", SkillType = SkillType.Skill1, Ratio = 1.30, TargetCount = 3, Description = "버프 해제" },
                    new Skill { Id = 3, Name = "조커 쇼", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 5 }
                },
                Passive = new Passive { Name = "트릭스터", Description = "적 버프 해제 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            },

            // ===== 노호 =====
            new Character
            {
                Id = 85,
                Name = "노호",
                Grade = "희귀",
                Type = "만능형",
                Skills = new List<Skill>
                {
                    new Skill { Id = 1, Name = "평타", SkillType = SkillType.Normal, Ratio = 1.0, TargetCount = 1 },
                    new Skill { Id = 2, Name = "정화의 일격", SkillType = SkillType.Skill1, Ratio = 1.30, TargetCount = 3, Description = "버프 해제" },
                    new Skill { Id = 3, Name = "강타", SkillType = SkillType.Skill2, Ratio = 1.50, TargetCount = 3 }
                },
                Passive = new Passive { Name = "정화", Description = "적 버프 해제 가능" },
                TranscendBonuses = new List<TranscendBonus>
                {
                    new TranscendBonus { Level = 1, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 2, BonusStats = new BaseStatSet { Atk_Rate = 5 } },
                    new TranscendBonus { Level = 3, BonusStats = new BaseStatSet { Hp_Rate = 15 } },
                    new TranscendBonus { Level = 4, BonusStats = new BaseStatSet { Cri = 15 } },
                    new TranscendBonus { Level = 5, BonusStats = new BaseStatSet { Atk_Rate = 10 } },
                    new TranscendBonus { Level = 6, BonusStats = new BaseStatSet { Atk_Rate = 5 } }
                }
            }
        };

        /// <summary>
        /// 이름으로 캐릭터 찾기
        /// </summary>
        public static Character GetByName(string name)
        {
            return Characters.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// ID로 캐릭터 찾기
        /// </summary>
        public static Character GetById(int id)
        {
            return Characters.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 등급별 캐릭터 목록
        /// </summary>
        public static List<Character> GetByGrade(string grade)
        {
            return Characters.Where(c => c.Grade == grade).ToList();
        }

        /// <summary>
        /// 타입별 캐릭터 목록
        /// </summary>
        public static List<Character> GetByType(string type)
        {
            return Characters.Where(c => c.Type == type).ToList();
        }

        /// <summary>
        /// 등급+타입으로 캐릭터 목록
        /// </summary>
        public static List<Character> GetByGradeAndType(string grade, string type)
        {
            return Characters.Where(c => c.Grade == grade && c.Type == type).ToList();
        }

        /// <summary>
        /// 캐릭터 이름 목록 (콤보박스용)
        /// </summary>
        public static List<string> GetAllNames()
        {
            return Characters.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// 등급별 캐릭터 이름 목록
        /// </summary>
        public static List<string> GetNamesByGrade(string grade)
        {
            return Characters.Where(c => c.Grade == grade).Select(c => c.Name).ToList();
        }
    }
}

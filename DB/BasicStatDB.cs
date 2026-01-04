using System.Collections.Generic;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 캐릭터 기본 스탯 테이블
    /// 등급(전설/영웅) × 타입(공격형/만능형/지원형/방어형)
    /// </summary>
    public static class StatTable
    {
        // 전설 등급의 타입별 스탯
        public static readonly Dictionary<string, BaseStatSet> LegendStats = new Dictionary<string, BaseStatSet>
        {
            { "공격형", new BaseStatSet { 
                Atk = 1500, Def = 571, Hp = 3326, Spd = 29, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "만능형", new BaseStatSet { 
                Atk = 1306, Def = 659, Hp = 3693, Spd = 25, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "지원형", new BaseStatSet { 
                Atk = 1095, Def = 675, Hp = 4458, Spd = 19, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "방어형", new BaseStatSet { 
                Atk = 727, Def = 892, Hp = 4825, Spd = 19, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }}
        };

        // 영웅 등급의 타입별 스탯
        public static readonly Dictionary<string, BaseStatSet> HeroStats = new Dictionary<string, BaseStatSet>
        {
            { "공격형", new BaseStatSet { 
                Atk = 1389, Def = 533, Hp = 3174, Spd = 25, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "만능형", new BaseStatSet { 
                Atk = 1238, Def = 616, Hp = 3529, Spd = 21, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "지원형", new BaseStatSet { 
                Atk = 1035, Def = 632, Hp = 4248, Spd = 16, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }},
            { "방어형", new BaseStatSet { 
                Atk = 704, Def = 818, Hp = 4572, Spd = 16, 
                Cri = 5, Cri_Dmg = 150, Wek = 0, Wek_Dmg = 130, 
                Blk = 0, Dmg_Rdc = 0, Eff_Hit = 0, Eff_Res = 0, 
                Heal_Rec = 0, Blk_Red = 0, Arm_Pen = 0, 
                Dmg_Dealt = 0, Dmg_Dealt_Bos = 0, Eff_Acc = 0, 
                Atk_Rate = 0, Def_Rate = 0, Hp_Rate = 0
            }}
        };

        /// <summary>
        /// 잠재능력 (1~3단계)
        /// </summary>
        public static class PotentialDb
        {
            public static readonly Dictionary<string, double[]> Stats = 
                new Dictionary<string, double[]>
            {
                { "공격력", new double[] { 100, 220, 370 } },    // 깡공
                { "방어력", new double[] { 0, 0, 0 } },          // 값 기입 필요
                { "생명력", new double[] { 0, 0, 0 } }           // 값 기입 필요
            };
        }

        public static class TranscendDb
        {
            public static readonly List<TranscendBonus> CommonBonuses = new List<TranscendBonus>
            {
                new TranscendBonus { Level = 7, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } },
                new TranscendBonus { Level = 8, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } },
                new TranscendBonus { Level = 9, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } },
                new TranscendBonus { Level = 10, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } },
                new TranscendBonus { Level = 11, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } },
                new TranscendBonus { Level = 12, BonusStats = new BaseStatSet { Atk_Rate = 2, Def_Rate = 2, Hp_Rate = 2 } }
            };

            /// <summary>
            /// 캐릭터 고유 초월(1~6) + 공통 초월(7~12) 합치기
            /// </summary>
            public static List<TranscendBonus> GetFullBonuses(List<TranscendBonus> uniqueBonuses)
            {
                var full = new List<TranscendBonus>(uniqueBonuses);
                full.AddRange(CommonBonuses);
                return full;
            }
        }

        // 전체 스탯 통합 맵
        public static readonly Dictionary<string, Dictionary<string, BaseStatSet>> AllBaseStats = 
            new Dictionary<string, Dictionary<string, BaseStatSet>>
        {
            { "전설", LegendStats },
            { "영웅", HeroStats }
        };

        /// <summary>
        /// 펫 희귀도 × 성급별 기본 스탯
        /// </summary>
        public static class PetStatTable
        {
            public static readonly Dictionary<string, Dictionary<int, BaseStatSet>> GradeStats = 
                new Dictionary<string, Dictionary<int, BaseStatSet>>
            {
                { "전설", new Dictionary<int, BaseStatSet>
                    {
                        { 4, new BaseStatSet { Atk = 301, Def = 180, Hp = 1004 } },
                        { 5, new BaseStatSet { Atk = 391, Def = 237, Hp = 1307 } },
                        { 6, new BaseStatSet { Atk = 564, Def = 344, Hp = 1895 } }
                    }
                },
                { "희귀", new Dictionary<int, BaseStatSet>
                    {
                        { 4, new BaseStatSet { Atk = 195, Def = 116, Hp = 653 } },
                        { 5, new BaseStatSet { Atk = 257, Def = 154, Hp = 853 } },
                        { 6, new BaseStatSet { Atk = 371, Def = 226, Hp = 1246 } }
                    }
                }
            };
        }

        /// <summary>
        /// 진형 데이터베이스
        /// </summary>
        public static class FormationDb
        {
            public static readonly Dictionary<string, FormationBonus> Formations = 
                new Dictionary<string, FormationBonus>
            {
                { "기본 진형", new FormationBonus { Atk_Rate_Back = 0.14, Def_Rate_Front = 0.21 } },
                { "밸런스 진형", new FormationBonus { Atk_Rate_Back = 0.21, Def_Rate_Front = 0.14 } },
                { "공격 진형", new FormationBonus { Atk_Rate_Back = 0.105, Def_Rate_Front = 0.42 } },
                { "보호 진형", new FormationBonus { Atk_Rate_Back = 0.42, Def_Rate_Front = 0.105 } }
            };
        }
    }
}

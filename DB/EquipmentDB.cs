using System.Collections.Generic;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 장비 관련 데이터베이스
    /// </summary>
    public static class EquipmentDb
    {   
        /// <summary>
        /// 장비 기본 스탯 (무기/방어구)
        /// </summary>
        public static class EquipStatTable
        {
            public static readonly BaseStatSet CommonWeaponStat = new BaseStatSet { Atk = 304 };
            public static readonly BaseStatSet CommonArmorStat = new BaseStatSet { Def = 189, Hp = 1079 };
        }

        /// <summary>
        /// 장비 메인 옵션
        /// </summary>
        public static class MainStatDb
        {
            public static readonly Dictionary<string, Dictionary<string, BaseStatSet>> OptionsBySlot = 
                new Dictionary<string, Dictionary<string, BaseStatSet>>
            {
                { "무기", new Dictionary<string, BaseStatSet> {
                    { "공격력%", new BaseStatSet { Atk_Rate = 0.28 } },
                    { "치명타확률%", new BaseStatSet { Cri = 0.24 } },
                    { "치명타피해%", new BaseStatSet { Cri_Dmg = 0.36 } },
                    { "약점공격확률%", new BaseStatSet { Wek = 0.28 } },
                    { "생명력%", new BaseStatSet { Hp_Rate = 0.28 } },
                    { "방어력%", new BaseStatSet { Def_Rate = 0.28 } },
                    { "효과적중%", new BaseStatSet { Eff_Hit = 0.3 } }
                }},

                { "방어구", new Dictionary<string, BaseStatSet> {
                    { "공격력%", new BaseStatSet { Atk_Rate = 0.28 } },
                    { "생명력%", new BaseStatSet { Hp_Rate = 0.28 } },
                    { "방어력%", new BaseStatSet { Def_Rate = 0.28 } },
                    { "효과저항%", new BaseStatSet { Eff_Res = 0.3 } },
                    { "받피감%", new BaseStatSet { Dmg_Rdc = 0.16 } },
                    { "막기확률%", new BaseStatSet { Blk = 0.24 } }
                }}
            };
        }

        /// <summary>
        /// 장비 부옵션 (티어별 수치)
        /// </summary>
        public static class SubStatDb
        {
            // Key: 스탯 종류, Value: 1~5단계 BaseStatSet 배열
            public static readonly Dictionary<string, BaseStatSet[]> SubStatTiers = 
                new Dictionary<string, BaseStatSet[]>
            {
                { "공격력%", new BaseStatSet[] {
                    new BaseStatSet { Atk_Rate = 0.05 },
                    new BaseStatSet { Atk_Rate = 0.10 },
                    new BaseStatSet { Atk_Rate = 0.15 },
                    new BaseStatSet { Atk_Rate = 0.20 },
                    new BaseStatSet { Atk_Rate = 0.25 }
                }},
                { "공격력", new BaseStatSet[] {
                    new BaseStatSet { Atk = 50 },
                    new BaseStatSet { Atk = 100 },
                    new BaseStatSet { Atk = 150 },
                    new BaseStatSet { Atk = 200 },
                    new BaseStatSet { Atk = 250 }
                }},
                { "치명타확률%", new BaseStatSet[] {
                    new BaseStatSet { Cri = 0.04 },
                    new BaseStatSet { Cri = 0.08 },
                    new BaseStatSet { Cri = 0.12 },
                    new BaseStatSet { Cri = 0.16 },
                    new BaseStatSet { Cri = 0.20 }
                }},
                { "치명타피해%", new BaseStatSet[] {
                    new BaseStatSet { Cri_Dmg = 0.06 },
                    new BaseStatSet { Cri_Dmg = 0.12 },
                    new BaseStatSet { Cri_Dmg = 0.18 },
                    new BaseStatSet { Cri_Dmg = 0.24 },
                    new BaseStatSet { Cri_Dmg = 0.30 }
                }},
                { "속공", new BaseStatSet[] {
                    new BaseStatSet { Spd = 4 },
                    new BaseStatSet { Spd = 8 },
                    new BaseStatSet { Spd = 12 },
                    new BaseStatSet { Spd = 16 },
                    new BaseStatSet { Spd = 20 }
                }},
                { "약점공격확률%", new BaseStatSet[] {
                    new BaseStatSet { Wek = 0.05 },
                    new BaseStatSet { Wek = 0.10 },
                    new BaseStatSet { Wek = 0.15 },
                    new BaseStatSet { Wek = 0.20 },
                    new BaseStatSet { Wek = 0.25 }
                }},
                { "생명력%", new BaseStatSet[] {
                    new BaseStatSet { Hp_Rate = 0.05 },
                    new BaseStatSet { Hp_Rate = 0.10 },
                    new BaseStatSet { Hp_Rate = 0.15 },
                    new BaseStatSet { Hp_Rate = 0.20 },
                    new BaseStatSet { Hp_Rate = 0.25 }
                }},
                { "생명력", new BaseStatSet[] {
                    new BaseStatSet { Hp = 180 },
                    new BaseStatSet { Hp = 360 },
                    new BaseStatSet { Hp = 540 },
                    new BaseStatSet { Hp = 720 },
                    new BaseStatSet { Hp = 900 }
                }},
                { "방어력%", new BaseStatSet[] {
                    new BaseStatSet { Def_Rate = 0.05 },
                    new BaseStatSet { Def_Rate = 0.10 },
                    new BaseStatSet { Def_Rate = 0.15 },
                    new BaseStatSet { Def_Rate = 0.20 },
                    new BaseStatSet { Def_Rate = 0.25 }
                }},
                { "방어력", new BaseStatSet[] {
                    new BaseStatSet { Def = 30 },
                    new BaseStatSet { Def = 60 },
                    new BaseStatSet { Def = 90 },
                    new BaseStatSet { Def = 120 },
                    new BaseStatSet { Def = 150 }
                }},
                { "막기확률%", new BaseStatSet[] {
                    new BaseStatSet { Blk = 0.04 },
                    new BaseStatSet { Blk = 0.08 },
                    new BaseStatSet { Blk = 0.12 },
                    new BaseStatSet { Blk = 0.16 },
                    new BaseStatSet { Blk = 0.20 }
                }},
                { "효과적중%", new BaseStatSet[] {
                    new BaseStatSet { Eff_Hit = 0.05 },
                    new BaseStatSet { Eff_Hit = 0.10 },
                    new BaseStatSet { Eff_Hit = 0.15 },
                    new BaseStatSet { Eff_Hit = 0.20 },
                    new BaseStatSet { Eff_Hit = 0.25 }
                }},
                { "효과저항%", new BaseStatSet[] {
                    new BaseStatSet { Eff_Res = 0.05 },
                    new BaseStatSet { Eff_Res = 0.10 },
                    new BaseStatSet { Eff_Res = 0.15 },
                    new BaseStatSet { Eff_Res = 0.20 },
                    new BaseStatSet { Eff_Res = 0.25 }
                }}
            };
        }

        /// <summary>
        /// 세트 효과 (2세트/4세트)
        /// </summary>
        public static readonly Dictionary<string, Dictionary<int, BaseStatSet>> SetEffects = 
            new Dictionary<string, Dictionary<int, BaseStatSet>>
        {
            { "선봉장", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Atk_Rate = 0.20 } },
                { 4, new BaseStatSet { Atk_Rate = 0.25, Eff_Hit = 0.20 } }
            }},
            { "추적자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Wek = 0.15 } },
                { 4, new BaseStatSet { Wek = 0.20, Wek_Dmg = 0.35 } }
            }},
            { "성기사", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Hp_Rate = 0.17 } },
                { 4, new BaseStatSet { Hp_Rate = 0.23, Heal_Rec = 0.20 } }
            }},
            { "수문장", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Blk = 0.15 } },
                { 4, new BaseStatSet { Blk = 0.15, Blk_Red = 0.10 } }
            }},
            { "수호자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Def_Rate = 0.20 } },
                { 4, new BaseStatSet { Def_Rate = 0.25, Eff_Res = 0.20 } }
            }},
            { "암살자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Cri = 0.15 } },
                { 4, new BaseStatSet { Cri = 0.15, Arm_Pen = 0.15 } }
            }},
            { "복수자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Dmg_Dealt = 0.15 } },
                { 4, new BaseStatSet { Dmg_Dealt = 0.15, Dmg_Dealt_Bos = 0.40 } }
            }},
            { "주술사", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Eff_Hit = 0.17 } },
                { 4, new BaseStatSet { Eff_Hit = 0.18, Eff_Acc = 0.10 } }
            }},
            { "조율자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Eff_Res = 0.17 } },
                { 4, new BaseStatSet { Eff_Res = 0.18 } }
            }}
        };
    }

    public static class AccessoryDb
    {
        /// <summary>
        /// 장신구 등급별 스탯
        /// Key: 등급 (4, 5, 6)
        /// </summary>
        public static readonly Dictionary<int, BaseStatSet> Stats = 
            new Dictionary<int, BaseStatSet>
        {
            { 4, new BaseStatSet { Atk_Rate = 0.05, Def_Rate = 0.05, Hp_Rate = 0.05 } },
            { 5, new BaseStatSet { Atk_Rate = 0.07, Def_Rate = 0.07, Hp_Rate = 0.07 } },
            { 6, new BaseStatSet { Atk_Rate = 0.10, Def_Rate = 0.10, Hp_Rate = 0.10 } }
        };
    }
}

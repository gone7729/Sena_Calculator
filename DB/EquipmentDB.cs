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
            // 공통 메인옵션 값
            public static readonly Dictionary<string, BaseStatSet> MainOptions = 
                new Dictionary<string, BaseStatSet>
            {
                { "공격력%", new BaseStatSet { Atk_Rate = 28 } },
                { "공격력", new BaseStatSet { Atk = 240 } },
                { "치명타확률%", new BaseStatSet { Cri = 24 } },
                { "치명타피해%", new BaseStatSet { Cri_Dmg = 36 } },
                { "약점공격확률%", new BaseStatSet { Wek = 28 } },
                { "생명력%", new BaseStatSet { Hp_Rate = 28 } },
                { "생명력", new BaseStatSet { Hp = 850 } },
                { "방어력%", new BaseStatSet { Def_Rate = 28 } },
                { "방어력", new BaseStatSet { Def = 160 } },
                { "효과적중%", new BaseStatSet { Eff_Hit = 30 } },
                { "효과저항%", new BaseStatSet { Eff_Res = 30 } },
                { "받피감%", new BaseStatSet { Dmg_Rdc = 16 } },
                { "막기확률%", new BaseStatSet { Blk = 24 } }
            };

            // 슬롯별 사용 가능한 옵션 목록
            public static readonly Dictionary<string, string[]> AvailableOptions = 
                new Dictionary<string, string[]>
            {
                { "무기", new[] { "공격력%", "공격력", "치명타확률%", "치명타피해%", "약점공격확률%", 
                                 "생명력%", "생명력", "방어력%", "방어력", "효과적중%" } },
                { "방어구", new[] { "공격력%", "공격력", "생명력%", "생명력", "방어력%", "방어력",
                                   "효과저항%", "받피감%", "막기확률%" } }
            };
        }

        /// <summary>
        /// 장비 부옵션 (티어별 수치)
        /// </summary>
        public static class SubStatDb
        {
            // Key: 스탯 종류, Value: 1단계 기본값 (단계 × 기본값 = 최종값)
            public static readonly Dictionary<string, BaseStatSet> SubStatBase = 
                new Dictionary<string, BaseStatSet>
            {
                { "공%", new BaseStatSet { Atk_Rate = 5 } },
                { "공", new BaseStatSet { Atk = 50 } },
                { "치확%", new BaseStatSet { Cri = 4 } },
                { "치피%", new BaseStatSet { Cri_Dmg = 6 } },
                { "속공", new BaseStatSet { Spd = 4 } },
                { "약공%", new BaseStatSet { Wek = 5 } },
                { "피통%", new BaseStatSet { Hp_Rate = 5 } },
                { "피통", new BaseStatSet { Hp = 180 } },
                { "방어%", new BaseStatSet { Def_Rate = 5 } },
                { "방어", new BaseStatSet { Def = 30 } },
                { "막기%", new BaseStatSet { Blk = 4 } },
                { "효적%", new BaseStatSet { Eff_Hit = 5 } },
                { "효저%", new BaseStatSet { Eff_Res = 5 } }
            };
        }

        /// <summary>
        /// 세트 효과 (2세트/4세트)
        /// </summary>
        public static readonly Dictionary<string, Dictionary<int, BaseStatSet>> SetEffects = 
            new Dictionary<string, Dictionary<int, BaseStatSet>>
        {
            { "선봉장", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Atk_Rate = 20 } },
                { 4, new BaseStatSet { Atk_Rate = 45, Eff_Hit = 20 } }
            }},
            { "추적자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Wek = 15 } },
                { 4, new BaseStatSet { Wek = 35, Wek_Dmg = 35 } }
            }},
            { "성기사", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Hp_Rate = 17 } },
                { 4, new BaseStatSet { Hp_Rate = 40, Heal_Rec = 20 } }
            }},
            { "수문장", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Blk = 15 } },
                { 4, new BaseStatSet { Blk = 30, Blk_Red = 10 } }
            }},
            { "수호자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Def_Rate = 20 } },
                { 4, new BaseStatSet { Def_Rate = 45, Eff_Res = 20 } }
            }},
            { "암살자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Cri = 15 } },
                { 4, new BaseStatSet { Cri = 30, Arm_Pen = 15 } }
            }},
            { "복수자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Dmg_Dealt = 15 } },
                { 4, new BaseStatSet { Dmg_Dealt = 30, Dmg_Dealt_Bos = 40 } }
            }},
            { "주술사", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Eff_Hit = 17 } },
                { 4, new BaseStatSet { Eff_Hit = 35, Eff_Acc = 10 } }
            }},
            { "조율자", new Dictionary<int, BaseStatSet> {
                { 2, new BaseStatSet { Eff_Res = 17 } },
                { 4, new BaseStatSet { Eff_Res = 35 } }
            }}
        };
    }

    public static class AccessoryDb
    {
        // 성급별 기본 보너스
        public static readonly Dictionary<int, BaseStatSet> GradeBonus = new Dictionary<int, BaseStatSet>
        {
            { 4, new BaseStatSet { Atk_Rate = 3, Def_Rate = 3, Hp_Rate = 3 } },
            { 5, new BaseStatSet { Atk_Rate = 5, Def_Rate = 5, Hp_Rate = 5 } },
            { 6, new BaseStatSet { Atk_Rate = 7, Def_Rate = 7, Hp_Rate = 7 } }
        };
    
        // 메인옵션 값 (성급별)
        public static readonly Dictionary<int, Dictionary<string, BaseStatSet>> MainOptions = 
            new Dictionary<int, Dictionary<string, BaseStatSet>>
        {
            { 4, new Dictionary<string, BaseStatSet> {
                { "피증%", new BaseStatSet { Dmg_Dealt = 2 } },
                { "방어력%", new BaseStatSet { Def_Rate = 3 } },
                { "생명력%", new BaseStatSet { Hp_Rate = 3 } },
                { "치명타확률%", new BaseStatSet { Cri = 3 } },
                { "막기%", new BaseStatSet { Blk = 3 } },
                { "약점공격확률%", new BaseStatSet { Wek = 4 } },
                { "효과적중%", new BaseStatSet { Eff_Hit = 5 } },
                { "효과저항%", new BaseStatSet { Eff_Res = 5 } },
                { "보피증%", new BaseStatSet { Dmg_Dealt_Bos = 4 } },
                { "1-3인기%", new BaseStatSet { Dmg_Dealt_1to3 = 3 } },
                { "4-5인기%", new BaseStatSet { Dmg_Dealt_4to5 = 3 } }
            }},
            { 5, new Dictionary<string, BaseStatSet> {
                { "피증%", new BaseStatSet { Dmg_Dealt = 4 } },
                { "방어력%", new BaseStatSet { Def_Rate = 6 } },
                { "생명력%", new BaseStatSet { Hp_Rate = 6 } },
                { "치명타확률%", new BaseStatSet { Cri = 6 } },
                { "막기%", new BaseStatSet { Blk = 6 } },
                { "약점공격확률%", new BaseStatSet { Wek = 8 } },
                { "효과적중%", new BaseStatSet { Eff_Hit = 10 } },
                { "효과저항%", new BaseStatSet { Eff_Res = 10 } },
                { "보피증%", new BaseStatSet { Dmg_Dealt_Bos = 8 } },
                { "1-3인기%", new BaseStatSet { Dmg_Dealt_1to3 = 6 } },
                { "4-5인기%", new BaseStatSet { Dmg_Dealt_4to5 = 6 } }
            }},
            { 6, new Dictionary<string, BaseStatSet> {
                { "피증%", new BaseStatSet { Dmg_Dealt = 6 } },
                { "방어력%", new BaseStatSet { Def_Rate = 10 } },
                { "생명력%", new BaseStatSet { Hp_Rate = 10 } },
                { "치명타확률%", new BaseStatSet { Cri = 10 } },
                { "막기%", new BaseStatSet { Blk = 10 } },
                { "약점공격확률%", new BaseStatSet { Wek = 12 } },
                { "효과적중%", new BaseStatSet { Eff_Hit = 15 } },
                { "효과저항%", new BaseStatSet { Eff_Res = 15 } },
                { "보피증%", new BaseStatSet { Dmg_Dealt_Bos = 12 } },
                { "1-3인기%", new BaseStatSet { Dmg_Dealt_1to3 = 10 } },
                { "4-5인기%", new BaseStatSet { Dmg_Dealt_4to5 = 10 } }
            }}
        };
    
        // 부옵션 값 (성급별)
        public static readonly Dictionary<int, Dictionary<string, BaseStatSet>> SubOptions = 
            new Dictionary<int, Dictionary<string, BaseStatSet>>
        {
            { 6, new Dictionary<string, BaseStatSet> {
                { "피증%", new BaseStatSet { Dmg_Dealt = 6 } },
                { "방어력%", new BaseStatSet { Def_Rate = 10 } },
                { "생명력%", new BaseStatSet { Hp_Rate = 10 } },
                { "치명타확률%", new BaseStatSet { Cri = 10 } },
                { "막기%", new BaseStatSet { Blk = 10 } },
                { "약점공격확률%", new BaseStatSet { Wek = 12 } },
                { "효과적중%", new BaseStatSet { Eff_Hit = 15 } },
                { "효과저항%", new BaseStatSet { Eff_Res = 15 } },
                { "보피증%", new BaseStatSet { Dmg_Dealt_Bos = 12 } },
                { "1-3인기%", new BaseStatSet { Dmg_Dealt_1to3 = 10 } },
                { "4-5인기%", new BaseStatSet { Dmg_Dealt_4to5 = 10 } }
            }}
        };
    }
}

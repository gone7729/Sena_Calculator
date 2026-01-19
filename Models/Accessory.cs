using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 장신구 모델
    /// </summary>
    public class Accessory
    {
        public int Grade { get; set; }  // 0=없음, 4=4성, 5=5성, 6=6성
        public string MainOption { get; set; }
        public string SubOption { get; set; }  // 6성만

        /// <summary>
        /// 장신구 전체 스탯 계산
        /// </summary>
        public BaseStatSet GetTotalStats()
        {
            BaseStatSet stats = new BaseStatSet();

            if (Grade <= 0) return stats;

            // 등급 기본 보너스
            if (AccessoryDb.GradeBonus.TryGetValue(Grade, out var gradeBonus))
                stats.Add(gradeBonus);

            // 메인옵션
            if (!string.IsNullOrEmpty(MainOption) && MainOption != "없음")
            {
                if (AccessoryDb.MainOptions.TryGetValue(Grade, out var mainOptions))
                {
                    if (mainOptions.TryGetValue(MainOption, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

            // 부옵션 (6성만)
            if (Grade == 6 && !string.IsNullOrEmpty(SubOption) && SubOption != "없음")
            {
                if (AccessoryDb.SubOptions.TryGetValue(Grade, out var subOptions))
                {
                    if (subOptions.TryGetValue(SubOption, out var subBonus))
                        stats.Add(subBonus);
                }
            }

            return stats;
        }

        /// <summary>
        /// 옵션 값 문자열 반환 (UI 표시용)
        /// </summary>
        public static string GetOptionDisplayValue(int grade, string optionName, bool isMainOption)
        {
            if (grade <= 0 || string.IsNullOrEmpty(optionName) || optionName == "없음")
                return "";

            var optionDb = isMainOption ? AccessoryDb.MainOptions : AccessoryDb.SubOptions;

            if (optionDb.TryGetValue(grade, out var options))
            {
                if (options.TryGetValue(optionName, out var stats))
                {
                    if (stats.Atk_Rate > 0) return $"{stats.Atk_Rate}%";
                    if (stats.Def_Rate > 0) return $"{stats.Def_Rate}%";
                    if (stats.Hp_Rate > 0) return $"{stats.Hp_Rate}%";
                    if (stats.Cri > 0) return $"{stats.Cri}%";
                    if (stats.Cri_Dmg > 0) return $"{stats.Cri_Dmg}%";
                    if (stats.Wek > 0) return $"{stats.Wek}%";
                    if (stats.Eff_Hit > 0) return $"{stats.Eff_Hit}%";
                    if (stats.Eff_Res > 0) return $"{stats.Eff_Res}%";
                    if (stats.Blk > 0) return $"{stats.Blk}%";
                    if (stats.Dmg_Dealt > 0) return $"{stats.Dmg_Dealt}%";
                    if (stats.Dmg_Dealt_Bos > 0) return $"{stats.Dmg_Dealt_Bos}%";
                    if (stats.Dmg_Dealt_1to3 > 0) return $"{stats.Dmg_Dealt_1to3}%";
                    if (stats.Dmg_Dealt_4to5 > 0) return $"{stats.Dmg_Dealt_4to5}%";
                }
            }
            return "";
        }
    }
}
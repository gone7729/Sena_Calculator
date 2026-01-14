using System;
using System.Windows;
using System.Windows.Controls;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Services
{
    /// <summary>
    /// UI 스탯 계산을 위한 헬퍼 클래스
    /// My/Enemy 접두사를 사용하여 동일한 로직으로 양쪽 캐릭터 스탯 계산
    /// </summary>
    public class StatCalculatorHelper
    {
        private readonly Window _window;

        public StatCalculatorHelper(Window window)
        {
            _window = window;
        }

        #region 컨트롤 접근 헬퍼

        private T FindControl<T>(string name) where T : class
        {
            return _window.FindName(name) as T;
        }

        private ComboBox GetComboBox(string prefix, string name) => FindControl<ComboBox>($"cbo{prefix}{name}");
        private TextBox GetTextBox(string prefix, string name) => FindControl<TextBox>($"txt{prefix}{name}");
        private CheckBox GetCheckBox(string prefix, string name) => FindControl<CheckBox>($"chk{prefix}{name}");
        private Button GetButton(string prefix, string name) => FindControl<Button>($"btn{prefix}{name}");

        #endregion

        #region 스탯 소스 가져오기 (파라미터화)

        /// <summary>
        /// 잠재능력 스탯 가져오기
        /// </summary>
        public BaseStatSet GetPotentialStats(string prefix)
        {
            BaseStatSet stats = new BaseStatSet();

            var cboPotentialAtk = GetComboBox(prefix, "PotentialAtk");
            var cboPotentialDef = GetComboBox(prefix, "PotentialDef");
            var cboPotentialHp = GetComboBox(prefix, "PotentialHp");

            if (cboPotentialAtk == null || cboPotentialDef == null || cboPotentialHp == null)
                return stats;

            int atkLevel = cboPotentialAtk.SelectedIndex;
            int defLevel = cboPotentialDef.SelectedIndex;
            int hpLevel = cboPotentialHp.SelectedIndex;

            if (atkLevel > 0)
                stats.Atk = StatTable.PotentialDb.Stats["공격력"][atkLevel - 1];
            if (defLevel > 0)
                stats.Def = StatTable.PotentialDb.Stats["방어력"][defLevel - 1];
            if (hpLevel > 0)
                stats.Hp = StatTable.PotentialDb.Stats["생명력"][hpLevel - 1];

            return stats;
        }

        /// <summary>
        /// 서브옵션 스탯 가져오기
        /// </summary>
        public BaseStatSet GetSubOptionStats(string prefix)
        {
            BaseStatSet result = new BaseStatSet();

            int atkRateTier = ParseTextBoxInt(prefix, "SubAtkRate");
            int atkTier = ParseTextBoxInt(prefix, "SubAtk");
            int criTier = ParseTextBoxInt(prefix, "SubCri");
            int criDmgTier = ParseTextBoxInt(prefix, "SubCriDmg");
            int wekTier = ParseTextBoxInt(prefix, "SubWek");
            int blkTier = ParseTextBoxInt(prefix, "SubBlk");
            int dmgRdcTier = ParseTextBoxInt(prefix, "SubDmgRdc");
            int defRateTier = ParseTextBoxInt(prefix, "SubDefRate");
            int defTier = ParseTextBoxInt(prefix, "SubDef");
            int hpRateTier = ParseTextBoxInt(prefix, "SubHpRate");
            int hpTier = ParseTextBoxInt(prefix, "SubHp");
            int effHitTier = ParseTextBoxInt(prefix, "SubEffHit");
            int effResTier = ParseTextBoxInt(prefix, "SubEffRes");
            int spdTier = ParseTextBoxInt(prefix, "SubSpd");

            if (atkRateTier > 0) result.Atk_Rate = 5 * atkRateTier;
            if (atkTier > 0) result.Atk = 30 * atkTier;
            if (criTier > 0) result.Cri = 4 * criTier;
            if (criDmgTier > 0) result.Cri_Dmg = 5 * criDmgTier;
            if (wekTier > 0) result.Wek = 4 * wekTier;
            if (blkTier > 0) result.Blk = 5 * blkTier;
            if (dmgRdcTier > 0) result.Dmg_Rdc = 3 * dmgRdcTier;
            if (defRateTier > 0) result.Def_Rate = 5 * defRateTier;
            if (defTier > 0) result.Def = 30 * defTier;
            if (hpRateTier > 0) result.Hp_Rate = 5 * hpRateTier;
            if (hpTier > 0) result.Hp = 200 * hpTier;
            if (effHitTier > 0) result.Eff_Hit = 5 * effHitTier;
            if (effResTier > 0) result.Eff_Res = 5 * effResTier;
            if (spdTier > 0) result.Spd = 4 * spdTier;

            return result;
        }

        /// <summary>
        /// 메인옵션 스탯 가져오기
        /// </summary>
        public BaseStatSet GetMainOptionStats(string prefix)
        {
            BaseStatSet stats = new BaseStatSet();

            var cboWeapon1 = GetComboBox(prefix, "Weapon1Main");
            var cboWeapon2 = GetComboBox(prefix, "Weapon2Main");
            var cboArmor1 = GetComboBox(prefix, "Armor1Main");
            var cboArmor2 = GetComboBox(prefix, "Armor2Main");

            // 무기 메인옵션
            AddMainOptionStat(stats, cboWeapon1, true);
            AddMainOptionStat(stats, cboWeapon2, true);
            // 방어구 메인옵션
            AddMainOptionStat(stats, cboArmor1, false);
            AddMainOptionStat(stats, cboArmor2, false);

            return stats;
        }

        private void AddMainOptionStat(BaseStatSet stats, ComboBox cbo, bool isWeapon)
        {
            if (cbo == null || cbo.SelectedIndex <= 0) return;

            string option = cbo.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(option)) return;

            // MainStatDb.MainOptions 사용 (무기/방어구 공통)
            if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(option, out var bonus))
                stats.Add(bonus);
        }

        /// <summary>
        /// 장신구 스탯 가져오기
        /// </summary>
        public BaseStatSet GetAccessoryStats(string prefix)
        {
            BaseStatSet stats = new BaseStatSet();

            var cboGrade = GetComboBox(prefix, "AccessoryGrade");
            var cboMain = GetComboBox(prefix, "AccessoryMain");
            var cboSub = GetComboBox(prefix, "AccessorySub");

            if (cboGrade == null || cboGrade.SelectedIndex <= 0) return stats;

            int grade = cboGrade.SelectedIndex + 3;

            if (AccessoryDb.GradeBonus.TryGetValue(grade, out var gradeBonus))
                stats.Add(gradeBonus);

            if (cboMain != null && cboMain.SelectedIndex > 0)
            {
                string mainOpt = cboMain.SelectedItem.ToString();
                if (AccessoryDb.MainOptions.TryGetValue(grade, out var mainOptions))
                {
                    if (mainOptions.TryGetValue(mainOpt, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

            if (grade == 6 && cboSub != null && cboSub.SelectedIndex > 0)
            {
                string subOpt = cboSub.SelectedItem.ToString();
                if (AccessoryDb.SubOptions.TryGetValue(grade, out var subOptions))
                {
                    if (subOptions.TryGetValue(subOpt, out var subBonus))
                        stats.Add(subBonus);
                }
            }

            return stats;
        }

        /// <summary>
        /// 세트 보너스 가져오기
        /// </summary>
        public BaseStatSet GetSetBonus(string prefix)
        {
            BaseStatSet total = new BaseStatSet();

            var cboSet1 = GetComboBox(prefix, "EquipSet1");
            var cboSet2 = GetComboBox(prefix, "EquipSet2");
            var cboCount1 = GetComboBox(prefix, "SetCount1");

            if (cboSet1 != null && cboSet1.SelectedIndex > 0)
            {
                string setName = cboSet1.SelectedItem.ToString();
                int setCount = (cboCount1?.SelectedIndex == 0) ? 2 : 4;
                AddSetBonus(total, setName, setCount);
            }

            if (cboCount1?.SelectedIndex == 0 && cboSet2 != null && cboSet2.SelectedIndex > 0)
            {
                string setName = cboSet2.SelectedItem.ToString();
                AddSetBonus(total, setName, 2);
            }

            return total;
        }

        private void AddSetBonus(BaseStatSet total, string setName, int setCount)
        {
            if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
            {
                if (setData.TryGetValue(setCount, out var bonus))
                {
                    total.Add(bonus);
                }
            }
        }

        /// <summary>
        /// 펫 깡스탯 가져오기
        /// </summary>
        public (double Atk, double Def, double Hp) GetPetFlatStats(string prefix)
        {
            var cboPet = GetComboBox(prefix, "Pet");
            var cboPetStar = GetComboBox(prefix, "PetStar");

            if (cboPet == null || cboPet.SelectedIndex <= 0)
                return (0, 0, 0);

            string petName = cboPet.SelectedItem.ToString();
            var pet = PetDb.GetByName(petName);
            if (pet == null) return (0, 0, 0);

            int star = GetPetStarValue(cboPetStar);
            var flatStats = pet.GetBaseStats(star);

            return (flatStats.Atk, flatStats.Def, flatStats.Hp);
        }

        /// <summary>
        /// 펫 옵션 % 가져오기
        /// </summary>
        public (double AtkRate, double DefRate, double HpRate) GetPetOptionRates(string prefix)
        {
            var txtAtk = GetTextBox(prefix, "PetAtk");
            var txtDef = GetTextBox(prefix, "PetDef");
            var txtHp = GetTextBox(prefix, "PetHp");

            double atkRate = ParseDouble(txtAtk?.Text);
            double defRate = ParseDouble(txtDef?.Text);
            double hpRate = ParseDouble(txtHp?.Text);

            return (atkRate, defRate, hpRate);
        }

        /// <summary>
        /// 펫 스킬 공격력% 가져오기
        /// </summary>
        public double GetPetSkillAtkRate(string prefix)
        {
            var cboPet = GetComboBox(prefix, "Pet");
            var cboPetStar = GetComboBox(prefix, "PetStar");

            if (cboPet == null || cboPet.SelectedIndex <= 0)
                return 0;

            string petName = cboPet.SelectedItem.ToString();
            var pet = PetDb.GetByName(petName);
            if (pet == null) return 0;

            int star = GetPetStarValue(cboPetStar);
            return pet.GetSkillBonus(star).Atk_Rate;
        }

        private int GetPetStarValue(ComboBox cboPetStar)
        {
            if (cboPetStar == null) return 6;

            string starStr = (cboPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6성";
            if (starStr == "4성") return 4;
            if (starStr == "5성") return 5;
            return 6;
        }

        /// <summary>
        /// 진형 보너스 가져오기
        /// </summary>
        public (double AtkRate, double DefRate) GetFormationRates(string prefix)
        {
            var cboFormation = GetComboBox(prefix, "Formation");
            var rbBack = FindControl<RadioButton>($"rb{prefix}Back");
            var rbFront = FindControl<RadioButton>($"rb{prefix}Front");

            // 버튼 방식 확인
            var btnPosition = GetButton(prefix, "FormationPosition");

            if (cboFormation == null || cboFormation.SelectedIndex <= 0)
                return (0, 0);

            string formationName = cboFormation.SelectedItem.ToString();
            if (!StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
                return (0, 0);

            bool isBack = true;

            // 버튼 Tag로 확인
            if (btnPosition != null)
            {
                isBack = btnPosition.Tag?.ToString() == "Back";
            }
            // 라디오버튼으로 확인 (fallback)
            else if (rbBack != null && rbFront != null)
            {
                isBack = rbBack.IsChecked == true;
            }

            double atkRate = isBack ? bonus.Atk_Rate_Back : 0;
            double defRate = !isBack ? bonus.Def_Rate_Front : 0;

            return (atkRate, defRate);
        }

        #endregion

        #region 스탯 계산 결과

        /// <summary>
        /// 캐릭터 정보 가져오기
        /// </summary>
        public CharacterInfo GetCharacterInfo(string prefix)
        {
            var cboCharacter = GetComboBox(prefix, "Character");
            var cboTranscend = GetComboBox(prefix, "Transcend");
            var chkSkillEnhanced = GetCheckBox(prefix, "SkillEnhanced");
            var chkPassiveCondition = GetCheckBox(prefix, "PassiveCondition");

            if (cboCharacter == null || cboCharacter.SelectedIndex <= 0)
                return null;

            string characterName = cboCharacter.SelectedItem.ToString();
            var character = CharacterDb.GetByName(characterName);
            if (character == null) return null;

            return new CharacterInfo
            {
                Character = character,
                TranscendLevel = cboTranscend?.SelectedIndex ?? 0,
                IsSkillEnhanced = chkSkillEnhanced?.IsChecked == true,
                IsPassiveConditionMet = chkPassiveCondition?.IsChecked == true
            };
        }

        /// <summary>
        /// 전체 스탯 계산 (UI 표시용 + 전투 계산용)
        /// </summary>
        public CalculatedStats CalculateAllStats(string prefix)
        {
            var result = new CalculatedStats();
            var charInfo = GetCharacterInfo(prefix);

            // 캐릭터 기본 스탯
            BaseStatSet characterStats = new BaseStatSet();
            double baseAtk = 0, baseDef = 0, baseHp = 0;

            if (charInfo != null)
            {
                characterStats = charInfo.Character.GetBaseStats();
                baseAtk = characterStats.Atk;
                baseDef = characterStats.Def;
                baseHp = characterStats.Hp;
            }

            // 각종 스탯 소스
            var potentialStats = GetPotentialStats(prefix);
            var subStats = GetSubOptionStats(prefix);
            var mainOptionStats = GetMainOptionStats(prefix);
            var accessoryStats = GetAccessoryStats(prefix);
            var setBonus = GetSetBonus(prefix);

            // 초월 스탯
            BaseStatSet transcendStats = new BaseStatSet();
            if (charInfo != null)
            {
                transcendStats = charInfo.Character.GetTranscendStats(charInfo.TranscendLevel);
            }

            // 장비 기본 깡스탯
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def * 2;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            // 펫 스탯
            var petFlat = GetPetFlatStats(prefix);
            var petRates = GetPetOptionRates(prefix);
            double petSkillAtkRate = GetPetSkillAtkRate(prefix);

            // 진형 보너스
            var formationRates = GetFormationRates(prefix);

            // ========== UI 표시용 (순수 기본 - 펫/진형/본인패시브 제외) ==========
            double pureFlatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + mainOptionStats.Atk;
            double pureFlatDef = equipFlatDef + potentialStats.Def + subStats.Def + mainOptionStats.Def;
            double pureFlatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + mainOptionStats.Hp;

            double pureRateAtk = mainOptionStats.Atk_Rate + subStats.Atk_Rate + transcendStats.Atk_Rate + accessoryStats.Atk_Rate + setBonus.Atk_Rate;
            double pureRateDef = mainOptionStats.Def_Rate + subStats.Def_Rate + transcendStats.Def_Rate + accessoryStats.Def_Rate + setBonus.Def_Rate;
            double pureRateHp = mainOptionStats.Hp_Rate + subStats.Hp_Rate + transcendStats.Hp_Rate + accessoryStats.Hp_Rate + setBonus.Hp_Rate;

            result.PureBaseAtk = baseAtk * (1 + pureRateAtk / 100) + pureFlatAtk;
            result.PureBaseDef = baseDef * (1 + pureRateDef / 100) + pureFlatDef;
            result.PureBaseHp = baseHp * (1 + pureRateHp / 100) + pureFlatHp;

            // ========== 전투 계산용 (전체 포함) ==========
            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlat.Atk + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlat.Def + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlat.Hp + mainOptionStats.Hp;

            double totalAtkRate = transcendStats.Atk_Rate + formationRates.AtkRate
                    + setBonus.Atk_Rate + subStats.Atk_Rate
                    + accessoryStats.Atk_Rate + petRates.AtkRate
                    + mainOptionStats.Atk_Rate + petSkillAtkRate;

            double totalDefRate = transcendStats.Def_Rate + formationRates.DefRate
                    + setBonus.Def_Rate + subStats.Def_Rate
                    + accessoryStats.Def_Rate + petRates.DefRate
                    + mainOptionStats.Def_Rate;

            double totalHpRate = transcendStats.Hp_Rate
                   + setBonus.Hp_Rate + subStats.Hp_Rate
                   + accessoryStats.Hp_Rate + petRates.HpRate
                   + mainOptionStats.Hp_Rate;

            // 기본 스탯 (버프 적용 전)
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // 속공
            result.TotalSpd = characterStats.Spd + subStats.Spd;

            // 스탯 비례 증가 계산
            if (charInfo?.Character?.Passive != null)
            {
                var passiveData = charInfo.Character.Passive.GetLevelData(charInfo.IsSkillEnhanced);
                if (passiveData?.StatScalings != null)
                {
                    foreach (var scaling in passiveData.StatScalings)
                    {
                        double sourceValue = scaling.SourceStat switch
                        {
                            StatType.Spd => result.TotalSpd,
                            StatType.Hp => baseStatHp,
                            StatType.Def => baseStatDef,
                            StatType.Atk => baseStatAtk,
                            _ => 0
                        };

                        double bonus = CalcStatScaling(sourceValue, scaling);

                        switch (scaling.TargetStat)
                        {
                            case StatType.Atk: baseStatAtk += bonus; break;
                            case StatType.Def: baseStatDef += bonus; break;
                            case StatType.Hp: baseStatHp += bonus; break;
                            case StatType.Cri: result.ScalingCri += bonus; break;
                        }
                    }
                }
            }

            // 결과 저장 (버프 적용 전 기본 스탯)
            result.BaseStatAtk = baseStatAtk;
            result.BaseStatDef = baseStatDef;
            result.BaseStatHp = baseStatHp;

            // 기타 스탯
            result.CharacterStats = characterStats;
            result.TranscendStats = transcendStats;
            result.SetBonus = setBonus;
            result.SubStats = subStats;
            result.MainOptionStats = mainOptionStats;
            result.AccessoryStats = accessoryStats;
            result.TotalAtkRate = totalAtkRate;

            return result;
        }

        private double CalcStatScaling(double sourceValue, StatScaling scaling)
        {
            if (scaling == null || scaling.SourceUnit <= 0) return 0;
            double multiplier = Math.Floor(sourceValue / scaling.SourceUnit);
            double bonus = multiplier * scaling.PerUnit;
            return Math.Min(bonus, scaling.MaxValue);
        }

        #endregion

        #region 유틸리티

        private int ParseTextBoxInt(string prefix, string name)
        {
            var textBox = GetTextBox(prefix, name);
            if (textBox == null) return 0;
            return int.TryParse(textBox.Text, out int result) ? result : 0;
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return double.TryParse(text, out double result) ? result : 0;
        }

        #endregion
    }

    #region 데이터 클래스

    public class CharacterInfo
    {
        public Character Character { get; set; }
        public int TranscendLevel { get; set; }
        public bool IsSkillEnhanced { get; set; }
        public bool IsPassiveConditionMet { get; set; }
    }

    public class CalculatedStats
    {
        // UI 표시용 (순수 기본)
        public double PureBaseAtk { get; set; }
        public double PureBaseDef { get; set; }
        public double PureBaseHp { get; set; }

        // 전투 계산용 (버프 적용 전)
        public double BaseStatAtk { get; set; }
        public double BaseStatDef { get; set; }
        public double BaseStatHp { get; set; }

        // 속공
        public double TotalSpd { get; set; }

        // 스탯 비례 보너스
        public double ScalingCri { get; set; }

        // 원본 스탯 세트 (기타 스탯 계산용)
        public BaseStatSet CharacterStats { get; set; } = new BaseStatSet();
        public BaseStatSet TranscendStats { get; set; } = new BaseStatSet();
        public BaseStatSet SetBonus { get; set; } = new BaseStatSet();
        public BaseStatSet SubStats { get; set; } = new BaseStatSet();
        public BaseStatSet MainOptionStats { get; set; } = new BaseStatSet();
        public BaseStatSet AccessoryStats { get; set; } = new BaseStatSet();

        // 합계 %
        public double TotalAtkRate { get; set; }
    }

    #endregion
}
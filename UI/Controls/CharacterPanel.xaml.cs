using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.UI.Controls
{
    public partial class CharacterPanel : UserControl
    {
        #region 필드

        private bool _isInitialized = false;

        #endregion

        #region 공개 프로퍼티 (스탯 결과)

        public double FinalAtk { get; private set; }
        public double FinalDef { get; private set; }
        public double FinalHp { get; private set; }
        public double CritRate { get; private set; }
        public double CritDamage { get; private set; }
        public double WeakRate { get; private set; }
        public double WeakDamage { get; private set; }
        public double DmgDealt { get; private set; }
        public double DmgDealtBoss { get; private set; }
        public double ArmorPen { get; private set; }
        public double DmgReduction { get; private set; }
        public double Dmg1to3 { get; private set; }
        public double Dmg4to5 { get; private set; }

        public Character SelectedCharacter => cboCharacter.SelectedIndex > 0
            ? CharacterDb.GetByName(cboCharacter.SelectedItem.ToString())
            : null;

        public Skill SelectedSkill
        {
            get
            {
                if (SelectedCharacter == null || cboSkill.SelectedIndex < 0) return null;
                string skillName = cboSkill.SelectedItem?.ToString();
                return SelectedCharacter.Skills.FirstOrDefault(s => s.Name == skillName);
            }
        }

        public int TranscendLevel => cboTranscend.SelectedIndex;
        public bool IsSkillEnhanced => chkSkillEnhanced.IsChecked == true;

        #endregion

        #region 이벤트

        public event EventHandler StatsChanged;

        protected virtual void OnStatsChanged()
        {
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 생성자

        public CharacterPanel()
        {
            InitializeComponent();
            InitializeComboBoxes();
            _isInitialized = true;
            RecalculateStats();
        }

        #endregion

        #region 초기화

        private void InitializeComboBoxes()
        {
            // 캐릭터 목록
            cboCharacter.Items.Add("선택하세요");
            foreach (var character in CharacterDb.Characters)
            {
                cboCharacter.Items.Add(character.Name);
            }
            cboCharacter.SelectedIndex = 0;

            // 초월 단계
            for (int i = 0; i <= 12; i++)
            {
                cboTranscend.Items.Add($"{i}초월");
            }
            cboTranscend.SelectedIndex = 0;

            // 장비 세트
            cboEquipSet1.Items.Add("없음");
            cboEquipSet2.Items.Add("없음");
            foreach (var setName in EquipmentDb.SetEffects.Keys)
            {
                cboEquipSet1.Items.Add(setName);
                cboEquipSet2.Items.Add(setName);
            }
            cboEquipSet1.SelectedIndex = 0;
            cboEquipSet2.SelectedIndex = 0;

            // 메인옵션
            InitializeMainOptionComboBoxes();

            // 서브옵션
            InitializeSubOptionComboBoxes();

            // 장신구
            InitializeAccessoryComboBoxes();

            // 진형
            cboFormation.Items.Add("없음");
            foreach (var formationName in StatTable.FormationDb.Formations.Keys)
            {
                cboFormation.Items.Add(formationName);
            }
            cboFormation.SelectedIndex = 0;

            // 펫
            cboPet.Items.Add("없음");
            foreach (var pet in PetDb.Pets)
            {
                cboPet.Items.Add(pet.Name);
            }
            cboPet.SelectedIndex = 0;
        }

        private void InitializeMainOptionComboBoxes()
        {
            cboWeapon1Main.Items.Add("없음");
            cboWeapon2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["무기"])
            {
                cboWeapon1Main.Items.Add(opt);
                cboWeapon2Main.Items.Add(opt);
            }
            cboWeapon1Main.SelectedIndex = 0;
            cboWeapon2Main.SelectedIndex = 0;

            cboArmor1Main.Items.Add("없음");
            cboArmor2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["방어구"])
            {
                cboArmor1Main.Items.Add(opt);
                cboArmor2Main.Items.Add(opt);
            }
            cboArmor1Main.SelectedIndex = 0;
            cboArmor2Main.SelectedIndex = 0;
        }

        private void InitializeSubOptionComboBoxes()
        {
            var typeOptions = new[] { "없음", "공%", "공", "치확%", "치피%", "속공", "약공%",
                                       "피통%", "피통", "방어%", "방어", "막기%", "효적%", "효저%" };

            var typeComboBoxes = new[] {
                cboSub1Type, cboSub2Type, cboSub3Type,
                cboSub4Type, cboSub5Type, cboSub6Type,
                cboSub7Type, cboSub8Type, cboSub9Type
            };

            foreach (var combo in typeComboBoxes)
            {
                foreach (var opt in typeOptions)
                    combo.Items.Add(opt);
                combo.SelectedIndex = 0;
            }
        }

        private void InitializeAccessoryComboBoxes()
        {
            cboAccessoryGrade.Items.Add("없음");
            cboAccessoryGrade.Items.Add("4성");
            cboAccessoryGrade.Items.Add("5성");
            cboAccessoryGrade.Items.Add("6성");
            cboAccessoryGrade.SelectedIndex = 0;

            var mainOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                      "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                      "1-3인기%", "4-5인기%" };
            foreach (var opt in mainOptions)
            {
                cboAccessoryMain.Items.Add(opt);
                cboAccessorySub.Items.Add(opt);
            }
            cboAccessoryMain.SelectedIndex = 0;
            cboAccessorySub.SelectedIndex = 0;
        }

        #endregion

        #region 이벤트 핸들러

        private void CboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            cboSkill.Items.Clear();
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    foreach (var skill in character.Skills)
                    {
                        cboSkill.Items.Add(skill.Name);
                    }
                    if (cboSkill.Items.Count > 0)
                        cboSkill.SelectedIndex = 0;
                }
            }
            RecalculateStats();
        }

        private void CboSkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            OnStatsChanged();
        }

        private void CboTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void Potential_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void CboEquipSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void CboSetCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (cboSetCount1.SelectedIndex == 0)
                gridEquipSet2.Visibility = Visibility.Visible;
            else
            {
                gridEquipSet2.Visibility = Visibility.Collapsed;
                cboEquipSet2.SelectedIndex = 0;
            }
            RecalculateStats();
        }

        private void MainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateMainOptionDisplay();
            RecalculateStats();
        }

        private void SubOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void SubOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void Tier_MouseLeft(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val))
            {
                txtBox.Text = (val + 1).ToString();
            }
        }

        private void Tier_MouseRight(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val) && val > 0)
            {
                txtBox.Text = (val - 1).ToString();
            }
            e.Handled = true;
        }

        private void CboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (cboAccessoryGrade.SelectedIndex == 3)
                panelAccessorySub.Visibility = Visibility.Visible;
            else
            {
                panelAccessorySub.Visibility = Visibility.Collapsed;
                cboAccessorySub.SelectedIndex = 0;
            }

            UpdateAccessoryDisplay();
            RecalculateStats();
        }

        private void CboAccessoryOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateAccessoryDisplay();
            RecalculateStats();
        }

        private void CboFormation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void Formation_PositionChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void CboPet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PetOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PetOption_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

        #region 스탯 계산

        public void RecalculateStats()
        {
            if (!_isInitialized) return;

            // 기본 스탯
            double baseAtk = 0, baseDef = 0, baseHp = 0;
            BaseStatSet characterStats = new BaseStatSet();

            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    characterStats = character.GetBaseStats();
                    baseAtk = characterStats.Atk;
                    baseDef = characterStats.Def;
                    baseHp = characterStats.Hp;
                }
            }

            // 캐릭터 패시브 버프
            BuffSet characterPassiveBuff = new BuffSet();
            if (SelectedCharacter?.Passive != null)
            {
                var buff = SelectedCharacter.Passive.GetTotalSelfBuff(IsSkillEnhanced, TranscendLevel);
                if (buff != null) characterPassiveBuff.Add(buff);
            }

            // 각종 스탯 소스
            var potentialStats = GetPotentialStats();
            var subStats = GetSubOptionStats();
            var mainOptionStats = GetMainOptionStats();
            var accessoryStats = GetAccessoryStats();

            // 장비 기본 스탯
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            // 펫 깡스탯
            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();

            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlatAtk + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlatDef + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlatHp + mainOptionStats.Hp;

            // 초월 스탯
            BaseStatSet transcendStats = new BaseStatSet();
            if (SelectedCharacter != null)
            {
                transcendStats = SelectedCharacter.GetTranscendStats(TranscendLevel);
            }

            // 진형%
            double formationAtkRate = GetFormationAtkRate();
            double formationDefRate = GetFormationDefRate();

            // 세트 보너스
            BaseStatSet setBonus = GetTotalSetBonus();

            // 펫옵션%
            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();

            // 합연산% 합계
            double totalAtkRate = transcendStats.Atk_Rate + formationAtkRate + setBonus.Atk_Rate
                                + subStats.Atk_Rate + accessoryStats.Atk_Rate + petOptionAtkRate + mainOptionStats.Atk_Rate;
            double totalDefRate = transcendStats.Def_Rate + formationDefRate + setBonus.Def_Rate
                                + subStats.Def_Rate + accessoryStats.Def_Rate + petOptionDefRate + mainOptionStats.Def_Rate;
            double totalHpRate = transcendStats.Hp_Rate + setBonus.Hp_Rate + subStats.Hp_Rate
                               + accessoryStats.Hp_Rate + petOptionHpRate + mainOptionStats.Hp_Rate;

            // 버프%
            double buffAtkRate = GetPetSkillAtkRate() + characterPassiveBuff.Atk_Rate;
            double buffDefRate = characterPassiveBuff.Def_Rate;
            double buffHpRate = characterPassiveBuff.Hp_Rate;

            // 기본 스탯 (버프 적용 전)
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // 최종 스탯 (버프 적용 후)
            FinalAtk = baseStatAtk * (1 + buffAtkRate / 100.0);
            FinalDef = baseStatDef * (1 + buffDefRate / 100.0);
            FinalHp = baseStatHp * (1 + buffHpRate / 100.0);

            // 기타 스탯
            CritRate = characterStats.Cri + transcendStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri + characterPassiveBuff.Cri;
            CritDamage = characterStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg + characterPassiveBuff.Cri_Dmg;
            WeakRate = characterStats.Wek + transcendStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek + characterPassiveBuff.Wek;
            WeakDamage = characterStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg + characterPassiveBuff.Wek_Dmg;
            DmgDealt = characterStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt + accessoryStats.Dmg_Dealt + characterPassiveBuff.Dmg_Dealt;
            DmgDealtBoss = characterStats.Dmg_Dealt_Bos + transcendStats.Dmg_Dealt_Bos + setBonus.Dmg_Dealt_Bos + accessoryStats.Dmg_Dealt_Bos + characterPassiveBuff.Dmg_Dealt_Bos;
            ArmorPen = characterStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen + characterPassiveBuff.Arm_Pen;
            DmgReduction = characterStats.Dmg_Rdc + transcendStats.Dmg_Rdc + setBonus.Dmg_Rdc + mainOptionStats.Dmg_Rdc + characterPassiveBuff.Dmg_Rdc;
            Dmg1to3 = characterStats.Dmg_Dealt_1to3 + transcendStats.Dmg_Dealt_1to3 + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3 + characterPassiveBuff.Dmg_Dealt_1to3;
            Dmg4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5;

            // UI 업데이트
            UpdateStatDisplay(baseStatAtk, baseStatDef, baseStatHp);
            OnStatsChanged();
        }

        private void UpdateStatDisplay(double baseAtk, double baseDef, double baseHp)
        {
            txtStatAtkBase.Text = baseAtk.ToString("N0");
            txtStatDefBase.Text = baseDef.ToString("N0");
            txtStatHpBase.Text = baseHp.ToString("N0");

            txtStatAtk.Text = FinalAtk.ToString("N0");
            txtStatDef.Text = FinalDef.ToString("N0");
            txtStatHp.Text = FinalHp.ToString("N0");

            txtStatCri.Text = $"{CritRate}%";
            txtStatCriDmg.Text = $"{CritDamage}%";
            txtStatWek.Text = $"{WeakRate}%";
            txtStatWekDmg.Text = $"{WeakDamage}%";
            txtStatDmgDealt.Text = $"{DmgDealt}%";
            txtStatBossDmg.Text = $"{DmgDealtBoss}%";
            txtStatArmPen.Text = $"{ArmorPen}%";
            txtStatBlk.Text = "0%";
            txtStatEffHit.Text = "0%";
            txtStatEffRes.Text = "0%";
            txtStatEffAcc.Text = "0%";
            txtStatDmgRdc.Text = $"{DmgReduction}%";
            txtStatDmg1to3.Text = $"{Dmg1to3}%";
            txtStatDmg4to5.Text = $"{Dmg4to5}%";
        }

        #endregion

        #region 스탯 헬퍼

        private BaseStatSet GetPotentialStats()
        {
            BaseStatSet stats = new BaseStatSet();
            int atkLevel = cboPotentialAtk.SelectedIndex;
            int defLevel = cboPotentialDef.SelectedIndex;
            int hpLevel = cboPotentialHp.SelectedIndex;

            if (atkLevel > 0) stats.Atk = StatTable.PotentialDb.Stats["공격력"][atkLevel - 1];
            if (defLevel > 0) stats.Def = StatTable.PotentialDb.Stats["방어력"][defLevel - 1];
            if (hpLevel > 0) stats.Hp = StatTable.PotentialDb.Stats["생명력"][hpLevel - 1];

            return stats;
        }

        private BaseStatSet GetSubOptionStats()
        {
            BaseStatSet result = new BaseStatSet();
            var subOptions = new[]
            {
                (cboSub1Type, txtSub1Tier), (cboSub2Type, txtSub2Tier), (cboSub3Type, txtSub3Tier),
                (cboSub4Type, txtSub4Tier), (cboSub5Type, txtSub5Tier), (cboSub6Type, txtSub6Tier),
                (cboSub7Type, txtSub7Tier), (cboSub8Type, txtSub8Tier), (cboSub9Type, txtSub9Tier)
            };

            foreach (var (typeCombo, tierTextBox) in subOptions)
            {
                if (typeCombo.SelectedIndex <= 0) continue;
                string statType = typeCombo.SelectedItem.ToString();
                if (!int.TryParse(tierTextBox.Text, out int tier) || tier <= 0) continue;
                if (EquipmentDb.SubStatDb.SubStatBase.TryGetValue(statType, out var baseStats))
                {
                    result.Add(baseStats.Multiply(tier));
                }
            }
            return result;
        }

        private BaseStatSet GetMainOptionStats()
        {
            BaseStatSet stats = new BaseStatSet();
            var combos = new[] { cboWeapon1Main, cboWeapon2Main, cboArmor1Main, cboArmor2Main };
            foreach (var cbo in combos)
            {
                if (cbo.SelectedIndex > 0)
                {
                    string option = cbo.SelectedItem.ToString();
                    if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(option, out var bonus))
                        stats.Add(bonus);
                }
            }
            return stats;
        }

        private BaseStatSet GetAccessoryStats()
        {
            BaseStatSet stats = new BaseStatSet();
            if (cboAccessoryGrade.SelectedIndex <= 0) return stats;

            int grade = cboAccessoryGrade.SelectedIndex + 3;
            if (AccessoryDb.GradeBonus.TryGetValue(grade, out var gradeBonus))
                stats.Add(gradeBonus);

            if (cboAccessoryMain.SelectedIndex > 0)
            {
                string mainOpt = cboAccessoryMain.SelectedItem.ToString();
                if (AccessoryDb.MainOptions.TryGetValue(grade, out var mainOptions))
                    if (mainOptions.TryGetValue(mainOpt, out var mainBonus))
                        stats.Add(mainBonus);
            }

            if (grade == 6 && cboAccessorySub.SelectedIndex > 0)
            {
                string subOpt = cboAccessorySub.SelectedItem.ToString();
                if (AccessoryDb.SubOptions.TryGetValue(grade, out var subOptions))
                    if (subOptions.TryGetValue(subOpt, out var subBonus))
                        stats.Add(subBonus);
            }
            return stats;
        }

        private BaseStatSet GetTotalSetBonus()
        {
            BaseStatSet setBonus = new BaseStatSet();
            if (cboEquipSet1.SelectedIndex > 0)
            {
                string setName = cboEquipSet1.SelectedItem.ToString();
                int setCount = cboSetCount1.SelectedIndex == 0 ? 2 : 4;
                if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
                    if (setData.TryGetValue(setCount, out var bonus))
                        setBonus.Add(bonus);
            }
            if (cboSetCount1.SelectedIndex == 0 && cboEquipSet2.SelectedIndex > 0)
            {
                string setName = cboEquipSet2.SelectedItem.ToString();
                if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
                    if (setData.TryGetValue(2, out var bonus))
                        setBonus.Add(bonus);
            }
            return setBonus;
        }

        private double GetFormationAtkRate()
        {
            if (cboFormation.SelectedIndex <= 0 || rbFront.IsChecked == true) return 0;
            string formationName = cboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
                return bonus.Atk_Rate_Back;
            return 0;
        }

        private double GetFormationDefRate()
        {
            if (cboFormation.SelectedIndex <= 0 || rbBack.IsChecked == true) return 0;
            string formationName = cboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
                return bonus.Def_Rate_Front;
            return 0;
        }

        private double GetPetFlatAtk()
        {
            if (cboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(cboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Atk;
            }
            return 0;
        }

        private double GetPetFlatDef()
        {
            if (cboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(cboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Def;
            }
            return 0;
        }

        private double GetPetFlatHp()
        {
            if (cboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(cboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Hp;
            }
            return 0;
        }

        private double GetPetSkillAtkRate()
        {
            if (cboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(cboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetSkillBonus(star).Atk_Rate;
            }
            return 0;
        }

        private double GetSinglePetOptionRate(ComboBox cbo, TextBox txt, string targetOption)
        {
            if (cbo.SelectedIndex <= 0) return 0;
            string option = (cbo.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (option == targetOption)
                return ParseDouble(txt.Text);
            return 0;
        }

        private double GetPetOptionAtkRate()
        {
            return GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "공격력%")
                 + GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "공격력%")
                 + GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "공격력%");
        }

        private double GetPetOptionDefRate()
        {
            return GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "방어력%")
                 + GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "방어력%")
                 + GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "방어력%");
        }

        private double GetPetOptionHpRate()
        {
            return GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "생명력%")
                 + GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "생명력%")
                 + GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "생명력%");
        }

        #endregion

        #region UI 헬퍼

        private void UpdateMainOptionDisplay()
        {
            txtWeapon1MainValue.Text = GetMainOptionDisplayValue(cboWeapon1Main);
            txtWeapon2MainValue.Text = GetMainOptionDisplayValue(cboWeapon2Main);
            txtArmor1MainValue.Text = GetMainOptionDisplayValue(cboArmor1Main);
            txtArmor2MainValue.Text = GetMainOptionDisplayValue(cboArmor2Main);
        }

        private string GetMainOptionDisplayValue(ComboBox cbo)
        {
            if (cbo.SelectedIndex <= 0) return "";
            string option = cbo.SelectedItem.ToString();
            if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(option, out var stats))
            {
                if (stats.Atk_Rate > 0) return $"{stats.Atk_Rate}%";
                if (stats.Atk > 0) return $"{stats.Atk}";
                if (stats.Def_Rate > 0) return $"{stats.Def_Rate}%";
                if (stats.Def > 0) return $"{stats.Def}";
                if (stats.Hp_Rate > 0) return $"{stats.Hp_Rate}%";
                if (stats.Hp > 0) return $"{stats.Hp}";
                if (stats.Cri > 0) return $"{stats.Cri}%";
                if (stats.Cri_Dmg > 0) return $"{stats.Cri_Dmg}%";
                if (stats.Wek > 0) return $"{stats.Wek}%";
            }
            return "";
        }

        private void UpdateAccessoryDisplay()
        {
            if (cboAccessoryGrade.SelectedIndex <= 0)
            {
                txtAccessoryMainValue.Text = "";
                txtAccessorySubValue.Text = "";
                return;
            }
            int grade = cboAccessoryGrade.SelectedIndex + 3;
            txtAccessoryMainValue.Text = GetAccessoryOptionValue(grade, cboAccessoryMain, AccessoryDb.MainOptions);
            txtAccessorySubValue.Text = grade == 6 ? GetAccessoryOptionValue(grade, cboAccessorySub, AccessoryDb.SubOptions) : "";
        }

        private string GetAccessoryOptionValue(int grade, ComboBox cbo, Dictionary<int, Dictionary<string, BaseStatSet>> optionDb)
        {
            if (cbo.SelectedIndex <= 0) return "";
            string option = cbo.SelectedItem.ToString();
            if (optionDb.TryGetValue(grade, out var options))
                if (options.TryGetValue(option, out var stats))
                {
                    if (stats.Dmg_Dealt > 0) return $"{stats.Dmg_Dealt}%";
                    if (stats.Def_Rate > 0) return $"{stats.Def_Rate}%";
                    if (stats.Hp_Rate > 0) return $"{stats.Hp_Rate}%";
                    if (stats.Cri > 0) return $"{stats.Cri}%";
                    if (stats.Wek > 0) return $"{stats.Wek}%";
                    if (stats.Dmg_Dealt_Bos > 0) return $"{stats.Dmg_Dealt_Bos}%";
                    if (stats.Dmg_Dealt_1to3 > 0) return $"{stats.Dmg_Dealt_1to3}%";
                    if (stats.Dmg_Dealt_4to5 > 0) return $"{stats.Dmg_Dealt_4to5}%";
                }
            return "";
        }

        private double ParseDouble(string text)
        {
            if (double.TryParse(text, out double result)) return result;
            return 0;
        }

        #endregion

        #region 공개 메서드

        public void Reset()
        {
            _isInitialized = false;

            cboCharacter.SelectedIndex = 0;
            cboSkill.Items.Clear();
            cboTranscend.SelectedIndex = 0;
            chkSkillEnhanced.IsChecked = false;

            cboPotentialAtk.SelectedIndex = 0;
            cboPotentialDef.SelectedIndex = 0;
            cboPotentialHp.SelectedIndex = 0;

            cboEquipSet1.SelectedIndex = 0;
            cboEquipSet2.SelectedIndex = 0;
            cboSetCount1.SelectedIndex = 1;

            cboWeapon1Main.SelectedIndex = 0;
            cboWeapon2Main.SelectedIndex = 0;
            cboArmor1Main.SelectedIndex = 0;
            cboArmor2Main.SelectedIndex = 0;

            var subCombos = new[] { cboSub1Type, cboSub2Type, cboSub3Type, cboSub4Type, cboSub5Type, cboSub6Type, cboSub7Type, cboSub8Type, cboSub9Type };
            var subTiers = new[] { txtSub1Tier, txtSub2Tier, txtSub3Tier, txtSub4Tier, txtSub5Tier, txtSub6Tier, txtSub7Tier, txtSub8Tier, txtSub9Tier };
            for (int i = 0; i < 9; i++)
            {
                subCombos[i].SelectedIndex = 0;
                subTiers[i].Text = "0";
            }

            cboAccessoryGrade.SelectedIndex = 0;
            cboAccessoryMain.SelectedIndex = 0;
            cboAccessorySub.SelectedIndex = 0;

            cboFormation.SelectedIndex = 0;
            rbBack.IsChecked = true;

            cboPet.SelectedIndex = 0;
            cboPetStar.SelectedIndex = 2;
            cboPetOpt1.SelectedIndex = 0; txtPetOpt1.Text = "0";
            cboPetOpt2.SelectedIndex = 0; txtPetOpt2.Text = "0";
            cboPetOpt3.SelectedIndex = 0; txtPetOpt3.Text = "0";

            _isInitialized = true;
            RecalculateStats();
        }

        #endregion
    }
}

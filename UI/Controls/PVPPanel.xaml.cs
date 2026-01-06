using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;
using GameDamageCalculator.Services;

namespace GameDamageCalculator.UI.Controls
{
    public partial class PVPPanel : UserControl
    {
        private bool _isInitialized = false;
        private DebuffSet _currentDebuffs = new DebuffSet();
        private List<BuffConfig> _buffConfigs;
        private double _weakDmgBuff = 0;
        private class BuffConfig
        {
            public string Key { get; set; }
            public CheckBox CheckBox { get; set; }
            public Button Button { get; set; }
            public string BaseName { get; set; }
            public string CharacterName { get; set; }
            public string SkillName { get; set; }
            public bool IsBuff { get; set; }
        }

        public PVPPanel()
        {
            InitializeComponent();
        }

        #region 초기화

        public void Initialize()
        {
            InitializeBuffConfigs();
            InitializeComboBoxes();
            _isInitialized = true;
            RecalculateStats();
        }

        private void InitializeBuffConfigs()
        {
            _buffConfigs = new List<BuffConfig>
            {
                // 버프 패시브
                new BuffConfig { Key = "BuffPassiveLion", CheckBox = PvpChkBuffPassiveLion, Button = PvpBtnBuffPassiveLion, BaseName = "라이언", CharacterName = "라이언", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLina", CheckBox = PvpChkBuffPassiveLina, Button = PvpBtnBuffPassiveLina, BaseName = "리나", CharacterName = "리나", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRachel", CheckBox = PvpChkBuffPassiveRachel, Button = PvpBtnBuffPassiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDelonz", CheckBox = PvpChkBuffPassiveDelonz, Button = PvpBtnBuffPassiveDelonz, BaseName = "델론즈", CharacterName = "델론즈", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveMiho", CheckBox = PvpChkBuffPassiveMiho, Button = PvpBtnBuffPassiveMiho, BaseName = "미호", CharacterName = "미호", SkillName = null, IsBuff = true },

                // 버프 액티브
                new BuffConfig { Key = "BuffActiveBiscuit", CheckBox = PvpChkBuffActiveBiscuit, Button = PvpBtnBuffActiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = "장비 강화", IsBuff = true },
                new BuffConfig { Key = "BuffActiveLina", CheckBox = PvpChkBuffActiveLina, Button = PvpBtnBuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = true },

                // 디버프 패시브
                new BuffConfig { Key = "DebuffPassiveTaka", CheckBox = PvpChkDebuffPassiveTaka, Button = PvpBtnDebuffPassiveTaka, BaseName = "타카", CharacterName = "타카", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBiscuit", CheckBox = PvpChkDebuffPassiveBiscuit, Button = PvpBtnDebuffPassiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = null, IsBuff = false },

                // 디버프 액티브
                new BuffConfig { Key = "DebuffActiveLina", CheckBox = PvpChkDebuffActiveLina, Button = PvpBtnDebuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelFlame", CheckBox = PvpChkDebuffActiveRachelFlame, Button = PvpBtnDebuffActiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "염화", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelPhoenix", CheckBox = PvpChkDebuffActiveRachelPhoenix, Button = null, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "불새", IsBuff = false }
            };
        }

        private void InitializeComboBoxes()
        {
            UpdateCharacterList();

            // 캐릭터 목록
            PvpCboCharacter.Items.Add("직접 입력하거나 골라주세요");
            foreach (var character in CharacterDb.Characters)
            {
                PvpCboCharacter.Items.Add(character.Name);
            }
            PvpCboCharacter.SelectedIndex = 0;

            // 초월 단계
            for (int i = 0; i <= 12; i++)
            {
                PvpCboTranscend.Items.Add($"{i}초월");
            }
            PvpCboTranscend.SelectedIndex = 0;

            // 장비 세트
            PvpCboEquipSet1.Items.Add("없음");
            PvpCboEquipSet2.Items.Add("없음");
            foreach (var setName in EquipmentDb.SetEffects.Keys)
            {
                PvpCboEquipSet1.Items.Add(setName);
                PvpCboEquipSet2.Items.Add(setName);
            }
            PvpCboEquipSet1.SelectedIndex = 0;
            PvpCboEquipSet2.SelectedIndex = 0;

            // 서브옵션 초기화
            InitializeSubOptionComboBoxes();

            // 진형 목록
            PvpCboFormation.Items.Add("없음");
            foreach (var formationName in StatTable.FormationDb.Formations.Keys)
            {
                PvpCboFormation.Items.Add(formationName);
            }
            PvpCboFormation.SelectedIndex = 0;

            InitializeMainOptionComboBoxes();
            InitializeAccessoryComboBoxes();

            // 펫 목록
            PvpCboPet.Items.Add("없음");
            foreach (var pet in PetDb.Pets)
            {
                PvpCboPet.Items.Add(pet.Name);
            }
            PvpCboPet.SelectedIndex = 0;
        }

        private void InitializeMainOptionComboBoxes()
        {
            // 무기 메인옵션
            PvpCboWeapon1Main.Items.Add("없음");
            PvpCboWeapon2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["무기"])
            {
                PvpCboWeapon1Main.Items.Add(opt);
                PvpCboWeapon2Main.Items.Add(opt);
            }
            PvpCboWeapon1Main.SelectedIndex = 0;
            PvpCboWeapon2Main.SelectedIndex = 0;

            // 방어구 메인옵션
            PvpCboArmor1Main.Items.Add("없음");
            PvpCboArmor2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["방어구"])
            {
                PvpCboArmor1Main.Items.Add(opt);
                PvpCboArmor2Main.Items.Add(opt);
            }
            PvpCboArmor1Main.SelectedIndex = 0;
            PvpCboArmor2Main.SelectedIndex = 0;
        }

        private void InitializeSubOptionComboBoxes()
        {
            var typeOptions = new[] { "없음", "공%", "공", "치확%", "치피%", "속공", "약공%",
                                      "피통%", "피통", "방어%", "방어", "막기%", "효적%", "효저%" };

            var typeComboBoxes = new[] {
                PvpCboSub1Type, PvpCboSub2Type, PvpCboSub3Type,
                PvpCboSub4Type, PvpCboSub5Type, PvpCboSub6Type,
                PvpCboSub7Type, PvpCboSub8Type, PvpCboSub9Type
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
            // 성급
            PvpCboAccessoryGrade.Items.Add("없음");
            PvpCboAccessoryGrade.Items.Add("4성");
            PvpCboAccessoryGrade.Items.Add("5성");
            PvpCboAccessoryGrade.Items.Add("6성");
            PvpCboAccessoryGrade.SelectedIndex = 0;

            // 메인옵션
            var mainOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                      "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                      "1-3인기%", "4-5인기%" };
            foreach (var opt in mainOptions)
                PvpCboAccessoryMain.Items.Add(opt);
            PvpCboAccessoryMain.SelectedIndex = 0;

            // 부옵션 (6성만)
            var subOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                     "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                     "1-3인기%", "4-5인기%" };
            foreach (var opt in subOptions)
                PvpCboAccessorySub.Items.Add(opt);
            PvpCboAccessorySub.SelectedIndex = 0;
        }

        #endregion

        #region 이벤트 핸들러

        private void PvpCboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateCharacterList();
        }

        private void UpdateCharacterList()
        {
            string previousSelection = PvpCboCharacter.SelectedItem?.ToString();

            PvpCboCharacter.Items.Clear();
            PvpCboCharacter.Items.Add("선택하세요");

            string gradeFilter = (PvpCboGrade.SelectedItem as ComboBoxItem)?.Content.ToString();
            string typeFilter = (PvpCboType.SelectedItem as ComboBoxItem)?.Content.ToString();

            foreach (var character in CharacterDb.Characters)
            {
                if (gradeFilter != "전체" && character.Grade != gradeFilter)
                    continue;
                if (typeFilter != "전체" && character.Type != typeFilter)
                    continue;

                PvpCboCharacter.Items.Add(character.Name);
            }

            if (previousSelection != null && PvpCboCharacter.Items.Contains(previousSelection))
                PvpCboCharacter.SelectedItem = previousSelection;
            else
                PvpCboCharacter.SelectedIndex = 0;
        }

        private void PvpCboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (PvpCboCharacter.SelectedIndex <= 0)
            {
                PvpCboSkill.Items.Clear();
                PvpCboSkill.Items.Add("직접 입력하거나 골라주세요");
                PvpCboSkill.SelectedIndex = 0;
                RecalculateStats();
                return;
            }

            string charName = PvpCboCharacter.SelectedItem.ToString();
            var character = CharacterDb.GetByName(charName);

            if (character != null)
            {
                PvpCboSkill.Items.Clear();
                foreach (var skill in character.Skills)
                {
                    PvpCboSkill.Items.Add(skill.Name);
                }
                if (PvpCboSkill.Items.Count > 0)
                {
                    PvpCboSkill.SelectedIndex = 0;
                }

                RecalculateStats();
            }
        }

        private void PvpCboSkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
        }

        private void PvpCboTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpCboEquipSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpCboSetCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (PvpCboSetCount1.SelectedIndex == 0)
            {
                PvpGridEquipSet2.Visibility = Visibility.Visible;
            }
            else
            {
                PvpGridEquipSet2.Visibility = Visibility.Collapsed;
                PvpCboEquipSet2.SelectedIndex = 0;
            }

            RecalculateStats();
        }

        private void PvpCboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (PvpCboAccessoryGrade.SelectedIndex == 3) // 6성
            {
                PvpPanelAccessorySub.Visibility = Visibility.Visible;
            }
            else
            {
                PvpPanelAccessorySub.Visibility = Visibility.Collapsed;
                PvpCboAccessorySub.SelectedIndex = 0;
            }

            UpdateAccessoryDisplay();
            RecalculateStats();
        }

        private void PvpCboAccessoryOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateAccessoryDisplay();
            RecalculateStats();
        }

        private void UpdateAccessoryDisplay()
        {
            if (PvpCboAccessoryGrade.SelectedIndex <= 0)
            {
                PvpTxtAccessoryMainValue.Text = "";
                PvpTxtAccessorySubValue.Text = "";
                return;
            }

            int grade = PvpCboAccessoryGrade.SelectedIndex + 3;

            PvpTxtAccessoryMainValue.Text = GetAccessoryOptionValue(grade, PvpCboAccessoryMain, AccessoryDb.MainOptions);

            if (grade == 6)
            {
                PvpTxtAccessorySubValue.Text = GetAccessoryOptionValue(grade, PvpCboAccessorySub, AccessoryDb.SubOptions);
            }
            else
            {
                PvpTxtAccessorySubValue.Text = "";
            }
        }

        private string GetAccessoryOptionValue(int grade, ComboBox cbo, Dictionary<int, Dictionary<string, BaseStatSet>> optionDb)
        {
            if (cbo.SelectedIndex <= 0) return "";

            string option = cbo.SelectedItem.ToString();
            if (optionDb.TryGetValue(grade, out var options))
            {
                if (options.TryGetValue(option, out var stats))
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

        private void PvpMainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateMainOptionDisplay();
            RecalculateStats();
        }

        private void UpdateMainOptionDisplay()
        {
            PvpTxtWeapon1MainValue.Text = GetMainOptionDisplayValue(PvpCboWeapon1Main);
            PvpTxtWeapon2MainValue.Text = GetMainOptionDisplayValue(PvpCboWeapon2Main);
            PvpTxtArmor1MainValue.Text = GetMainOptionDisplayValue(PvpCboArmor1Main);
            PvpTxtArmor2MainValue.Text = GetMainOptionDisplayValue(PvpCboArmor2Main);
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
                if (stats.Eff_Hit > 0) return $"{stats.Eff_Hit}%";
                if (stats.Eff_Res > 0) return $"{stats.Eff_Res}%";
                if (stats.Dmg_Rdc > 0) return $"{stats.Dmg_Rdc}%";
                if (stats.Blk > 0) return $"{stats.Blk}%";
            }
            return "";
        }

        private void PvpSubOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpSubOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpTier_MouseLeft(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val))
            {
                txtBox.Text = (val + 1).ToString();
                RecalculateStats();
            }
        }

        private void PvpTier_MouseRight(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val) && val > 0)
            {
                txtBox.Text = (val - 1).ToString();
                RecalculateStats();
            }
            e.Handled = true;
        }

        private void PvpPotential_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpCboPet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpPetOption_Changed(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpCboFormation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PvpFormation_PositionChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

        #region 스탯 계산

        public void RecalculateStats()
        {
            if (!_isInitialized) return;

            // 기본공격력
            double baseAtk = 0, baseDef = 0, baseHp = 0;
            BaseStatSet characterStats = new BaseStatSet();

            if (PvpCboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(PvpCboCharacter.SelectedItem.ToString());
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
            if (PvpCboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(PvpCboCharacter.SelectedItem.ToString());
                if (character?.Passive != null)
                {
                    bool isConditionMet = PvpChkPassiveCondition.IsChecked == true;
                    bool isEnhanced = PvpChkSkillEnhanced.IsChecked == true;
                    int transcendLevel = PvpCboTranscend.SelectedIndex;
                    var buff = character.Passive.GetTotalSelfBuff(isEnhanced, transcendLevel, isConditionMet);
                    if (buff != null) characterPassiveBuff.Add(buff);
                }
            }

            // 스탯 소스
            var potentialStats = GetPotentialStats();
            var subStats = GetSubOptionStats();
            var mainOptionStats = GetMainOptionStats();
            var accessoryStats = GetAccessoryStats();

            // 버프/디버프
            BuffSet passiveBuffs = GetAllPassiveBuffs();
            BuffSet activeBuffs = GetAllActiveBuffs();
            DebuffSet passiveDebuffs = GetAllPassiveDebuffs();
            DebuffSet activeDebuffs = GetAllActiveDebuffs();
            _weakDmgBuff = passiveBuffs.Wek_Dmg + activeBuffs.Wek_Dmg;

            BuffSet totalBuffs = new BuffSet();
            totalBuffs.Add(passiveBuffs);
            totalBuffs.Add(activeBuffs);

            _currentDebuffs = new DebuffSet();
            _currentDebuffs.Add(passiveDebuffs);
            _currentDebuffs.Add(activeDebuffs);

            // 깡스탯 합계
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();

            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlatAtk + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlatDef + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlatHp + mainOptionStats.Hp;

            // 초월 스탯
            BaseStatSet transcendStats = new BaseStatSet();
            if (PvpCboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(PvpCboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    int transcendLevel = PvpCboTranscend.SelectedIndex;
                    transcendStats = character.GetTranscendStats(transcendLevel);
                }
            }

            // 진형%
            double formationAtkRate = GetFormationAtkRate();
            double formationDefRate = GetFormationDefRate();

            // 세트%
            BaseStatSet setBonus = new BaseStatSet();
            if (PvpCboEquipSet1.SelectedIndex > 0)
            {
                string setName = PvpCboEquipSet1.SelectedItem.ToString();
                int setCount = PvpCboSetCount1.SelectedIndex == 0 ? 2 : 4;
                setBonus.Add(GetSetBonus(setName, setCount));
            }
            if (PvpCboSetCount1.SelectedIndex == 0 && PvpCboEquipSet2.SelectedIndex > 0)
            {
                string setName = PvpCboEquipSet2.SelectedItem.ToString();
                setBonus.Add(GetSetBonus(setName, 2));
            }

            // 펫옵션%
            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();

            // 합연산% 합계
            double totalAtkRate = transcendStats.Atk_Rate + formationAtkRate
                    + setBonus.Atk_Rate + subStats.Atk_Rate
                    + accessoryStats.Atk_Rate + petOptionAtkRate
                    + mainOptionStats.Atk_Rate;

            double totalDefRate = transcendStats.Def_Rate + formationDefRate
                    + setBonus.Def_Rate + subStats.Def_Rate
                    + accessoryStats.Def_Rate + petOptionDefRate
                    + mainOptionStats.Def_Rate;

            double totalHpRate = transcendStats.Hp_Rate
                   + setBonus.Hp_Rate + subStats.Hp_Rate
                   + accessoryStats.Hp_Rate + petOptionHpRate
                   + mainOptionStats.Hp_Rate;

            // 버프%
            double buffAtkRate = GetPetSkillAtkRate() + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate = totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate = totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            // 기본 스탯
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // 최종 스탯
            double totalAtk = baseStatAtk * (1 + buffAtkRate / 100.0);
            double totalDef = baseStatDef * (1 + buffDefRate / 100.0);
            double totalHp = baseStatHp * (1 + buffHpRate / 100.0);

            // UI 표시
            PvpTxtStatAtkBase.Text = baseStatAtk.ToString("N0");
            PvpTxtStatDefBase.Text = baseStatDef.ToString("N0");
            PvpTxtStatHpBase.Text = baseStatHp.ToString("N0");

            PvpTxtStatAtk.Text = totalAtk.ToString("N0");
            PvpTxtStatDef.Text = totalDef.ToString("N0");
            PvpTxtStatHp.Text = totalHp.ToString("N0");

            // 기타 스탯
            BaseStatSet displayStats = new BaseStatSet
            {
                Cri = characterStats.Cri + transcendStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri + characterPassiveBuff.Cri,
                Cri_Dmg = characterStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + characterPassiveBuff.Cri_Dmg,
                Wek = characterStats.Wek + transcendStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek + totalBuffs.Wek + characterPassiveBuff.Wek,
                Wek_Dmg = characterStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg + characterPassiveBuff.Wek_Dmg,
                Dmg_Dealt = characterStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt + characterPassiveBuff.Dmg_Dealt,
                Dmg_Dealt_Bos = characterStats.Dmg_Dealt_Bos + transcendStats.Dmg_Dealt_Bos + setBonus.Dmg_Dealt_Bos + accessoryStats.Dmg_Dealt_Bos + totalBuffs.Dmg_Dealt_Bos + characterPassiveBuff.Dmg_Dealt_Bos,
                Arm_Pen = characterStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen + totalBuffs.Arm_Pen + characterPassiveBuff.Arm_Pen,
                Blk = characterStats.Blk + transcendStats.Blk + setBonus.Blk + subStats.Blk + mainOptionStats.Blk + accessoryStats.Blk,
                Eff_Hit = characterStats.Eff_Hit + transcendStats.Eff_Hit + setBonus.Eff_Hit + subStats.Eff_Hit + mainOptionStats.Eff_Hit + accessoryStats.Eff_Hit,
                Eff_Res = characterStats.Eff_Res + transcendStats.Eff_Res + setBonus.Eff_Res + subStats.Eff_Res + mainOptionStats.Eff_Res + accessoryStats.Eff_Res + characterPassiveBuff.Eff_Res,
                Eff_Acc = characterStats.Eff_Acc + transcendStats.Eff_Acc + setBonus.Eff_Acc,
                Dmg_Rdc = characterStats.Dmg_Rdc + transcendStats.Dmg_Rdc + setBonus.Dmg_Rdc + mainOptionStats.Dmg_Rdc + totalBuffs.Dmg_Rdc + characterPassiveBuff.Dmg_Rdc,
                Dmg_Dealt_1to3 = characterStats.Dmg_Dealt_1to3 + transcendStats.Dmg_Dealt_1to3 + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3 + totalBuffs.Dmg_Dealt_1to3 + characterPassiveBuff.Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5
            };

            UpdateStatDisplay(displayStats);
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
            PvpTxtStatCri.Text = $"{stats.Cri}%";
            PvpTxtStatCriDmg.Text = $"{stats.Cri_Dmg}%";
            PvpTxtStatWek.Text = $"{stats.Wek}%";
            PvpTxtStatWekDmg.Text = $"{stats.Wek_Dmg}%";
            PvpTxtStatDmgDealt.Text = $"{stats.Dmg_Dealt}%";
            PvpTxtStatBossDmg.Text = $"{stats.Dmg_Dealt_Bos}%";
            PvpTxtStatDmg1to3.Text = $"{stats.Dmg_Dealt_1to3}%";
            PvpTxtStatDmg4to5.Text = $"{stats.Dmg_Dealt_4to5}%";
            PvpTxtStatArmPen.Text = $"{stats.Arm_Pen}%";
            PvpTxtStatBlk.Text = $"{stats.Blk}%";
            PvpTxtStatEffHit.Text = $"{stats.Eff_Hit}%";
            PvpTxtStatEffRes.Text = $"{stats.Eff_Res}%";
            PvpTxtStatEffAcc.Text = $"{stats.Eff_Acc}%";
            PvpTxtStatDmgRdc.Text = $"{stats.Dmg_Rdc}%";
        }

        #endregion

        #region DB 헬퍼

        private BaseStatSet GetMainOptionStats()
        {
            BaseStatSet stats = new BaseStatSet();
            var combos = new[] { PvpCboWeapon1Main, PvpCboWeapon2Main, PvpCboArmor1Main, PvpCboArmor2Main };

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

        private BaseStatSet GetSubOptionStats()
        {
            BaseStatSet result = new BaseStatSet();

            var subOptions = new[]
            {
                (PvpCboSub1Type, PvpTxtSub1Tier),
                (PvpCboSub2Type, PvpTxtSub2Tier),
                (PvpCboSub3Type, PvpTxtSub3Tier),
                (PvpCboSub4Type, PvpTxtSub4Tier),
                (PvpCboSub5Type, PvpTxtSub5Tier),
                (PvpCboSub6Type, PvpTxtSub6Tier),
                (PvpCboSub7Type, PvpTxtSub7Tier),
                (PvpCboSub8Type, PvpTxtSub8Tier),
                (PvpCboSub9Type, PvpTxtSub9Tier)
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

        private BaseStatSet GetAccessoryStats()
        {
            BaseStatSet stats = new BaseStatSet();

            if (PvpCboAccessoryGrade.SelectedIndex <= 0) return stats;

            int grade = PvpCboAccessoryGrade.SelectedIndex + 3;

            if (AccessoryDb.GradeBonus.TryGetValue(grade, out var gradeBonus))
                stats.Add(gradeBonus);

            if (PvpCboAccessoryMain.SelectedIndex > 0)
            {
                string mainOpt = PvpCboAccessoryMain.SelectedItem.ToString();
                if (AccessoryDb.MainOptions.TryGetValue(grade, out var mainOptions))
                {
                    if (mainOptions.TryGetValue(mainOpt, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

            if (grade == 6 && PvpCboAccessorySub.SelectedIndex > 0)
            {
                string subOpt = PvpCboAccessorySub.SelectedItem.ToString();
                if (AccessoryDb.SubOptions.TryGetValue(grade, out var subOptions))
                {
                    if (subOptions.TryGetValue(subOpt, out var subBonus))
                        stats.Add(subBonus);
                }
            }

            return stats;
        }

        private BaseStatSet GetPotentialStats()
        {
            BaseStatSet stats = new BaseStatSet();

            int atkLevel = PvpCboPotentialAtk.SelectedIndex;
            int defLevel = PvpCboPotentialDef.SelectedIndex;
            int hpLevel = PvpCboPotentialHp.SelectedIndex;

            if (atkLevel > 0)
                stats.Atk = StatTable.PotentialDb.Stats["공격력"][atkLevel - 1];
            if (defLevel > 0)
                stats.Def = StatTable.PotentialDb.Stats["방어력"][defLevel - 1];
            if (hpLevel > 0)
                stats.Hp = StatTable.PotentialDb.Stats["생명력"][hpLevel - 1];

            return stats;
        }

        private BaseStatSet GetSetBonus(string setName, int setCount)
        {
            BaseStatSet total = new BaseStatSet();

            if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
            {
                if (setData.TryGetValue(setCount, out var bonus))
                {
                    total.Add(bonus);
                }
            }
            return total;
        }

        #endregion

        #region 펫 관련

        private double GetPetFlatAtk()
        {
            if (PvpCboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(PvpCboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = PvpCboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Atk;
            }
            return 0;
        }

        private double GetPetFlatDef()
        {
            if (PvpCboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(PvpCboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = PvpCboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Def;
            }
            return 0;
        }

        private double GetPetFlatHp()
        {
            if (PvpCboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(PvpCboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = PvpCboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Hp;
            }
            return 0;
        }

        private double GetPetSkillAtkRate()
        {
            if (PvpCboPet.SelectedIndex <= 0) return 0;
            var pet = PetDb.GetByName(PvpCboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = PvpCboPetStar.SelectedIndex + 4;
                return pet.GetSkillBonus(star).Atk_Rate;
            }
            return 0;
        }

        private double GetSinglePetOptionRate(ComboBox cbo, TextBox txt, string targetOption)
        {
            if (cbo.SelectedIndex <= 0) return 0;

            string option = (cbo.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (option == targetOption)
            {
                return ParseDouble(txt.Text) / 100;
            }
            return 0;
        }

        private double GetPetOptionAtkRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(PvpCboPetOpt1, PvpTxtPetOpt1, "공격력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt2, PvpTxtPetOpt2, "공격력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt3, PvpTxtPetOpt3, "공격력%");
            return rate;
        }

        private double GetPetOptionDefRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(PvpCboPetOpt1, PvpTxtPetOpt1, "방어력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt2, PvpTxtPetOpt2, "방어력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt3, PvpTxtPetOpt3, "방어력%");
            return rate;
        }

        private double GetPetOptionHpRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(PvpCboPetOpt1, PvpTxtPetOpt1, "생명력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt2, PvpTxtPetOpt2, "생명력%");
            rate += GetSinglePetOptionRate(PvpCboPetOpt3, PvpTxtPetOpt3, "생명력%");
            return rate;
        }

        #endregion

        #region 진형 관련

        private double GetFormationAtkRate()
        {
            if (PvpCboFormation.SelectedIndex <= 0) return 0;
            if (PvpRbFront.IsChecked == true) return 0;

            string formationName = PvpCboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
            {
                return bonus.Atk_Rate_Back;
            }
            return 0;
        }

        private double GetFormationDefRate()
        {
            if (PvpCboFormation.SelectedIndex <= 0) return 0;
            if (PvpRbBack.IsChecked == true) return 0;

            string formationName = PvpCboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
            {
                return bonus.Def_Rate_Front;
            }
            return 0;
        }

        #endregion

        #region 버프/디버프 관련

        private static readonly Brush[] BuffBgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(58, 58, 58)),
            new SolidColorBrush(Color.FromRgb(180, 150, 50)),
            new SolidColorBrush(Color.FromRgb(70, 130, 180)),
            new SolidColorBrush(Color.FromRgb(138, 43, 226))
        };

        private static readonly Brush[] BuffFgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            Brushes.Black,
            Brushes.White,
            Brushes.White
        };

        private static readonly string[] BuffOptionSuffix = { "", " 스강", " 초월", " 풀" };

        private void PvpBuffOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int current = int.Parse(btn.Tag?.ToString() ?? "0");
                int next = (current + 1) % 4;
                btn.Tag = next;

                string baseName = btn.Content.ToString()
                    .Replace(" 풀", "").Replace(" 스강", "").Replace(" 초월", "");

                btn.Background = BuffBgColors[next];
                btn.Foreground = BuffFgColors[next];
                btn.Content = baseName + BuffOptionSuffix[next];

                RecalculateStats();
            }
        }

        private (bool isEnhanced, int transcendLevel) GetBuffOption(Button btn)
        {
            if (btn == null) return (false, 0);
            int state = int.Parse(btn.Tag?.ToString() ?? "0");
            return state switch
            {
                0 => (false, 0),
                1 => (true, 0),
                2 => (false, 6),
                3 => (true, 6),
                _ => (false, 0)
            };
        }

        private void PvpPassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private BuffSet GetAllPassiveBuffs()
        {
            BuffSet total = new BuffSet();
            foreach (var config in _buffConfigs.Where(c => c.IsBuff && c.SkillName == null))
            {
                if (config.CheckBox.IsChecked != true) continue;
                var (isEnhanced, transcendLevel) = GetBuffOption(config.Button);
                var character = CharacterDb.GetByName(config.CharacterName);
                var buff = character?.Passive?.GetPartyBuff(isEnhanced, transcendLevel);
                if (buff != null) total.MaxMerge(buff);
            }
            return total;
        }

        private BuffSet GetAllActiveBuffs()
        {
            BuffSet total = new BuffSet();
            foreach (var config in _buffConfigs.Where(c => c.IsBuff && c.SkillName != null))
            {
                if (config.CheckBox.IsChecked != true) continue;
                var (isEnhanced, transcendLevel) = GetBuffOption(config.Button);
                var character = CharacterDb.GetByName(config.CharacterName);
                var skill = character?.Skills?.FirstOrDefault(s => s.Name == config.SkillName);
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.BuffEffect != null) total.MaxMerge(levelData.BuffEffect);
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.Bonus != null) total.MaxMerge(transcendBonus.Bonus);
                }
            }
            return total;
        }

        private DebuffSet GetAllPassiveDebuffs()
        {
            DebuffSet total = new DebuffSet();
            foreach (var config in _buffConfigs.Where(c => !c.IsBuff && c.SkillName == null))
            {
                if (config.CheckBox.IsChecked != true) continue;
                var (isEnhanced, transcendLevel) = GetBuffOption(config.Button);
                var character = CharacterDb.GetByName(config.CharacterName);
                var debuff = character?.Passive?.GetTotalDebuff(isEnhanced, transcendLevel);
                if (debuff != null) total.MaxMerge(debuff);
            }
            return total;
        }

        private DebuffSet GetAllActiveDebuffs()
        {
            DebuffSet total = new DebuffSet();
            foreach (var config in _buffConfigs.Where(c => !c.IsBuff && c.SkillName != null))
            {
                if (config.CheckBox.IsChecked != true) continue;
                Button btn = config.Button ?? _buffConfigs.FirstOrDefault(c => c.CharacterName == config.CharacterName && c.Button != null)?.Button;
                var (isEnhanced, transcendLevel) = GetBuffOption(btn);
                var character = CharacterDb.GetByName(config.CharacterName);
                var skill = character?.Skills?.FirstOrDefault(s => s.Name == config.SkillName);
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.DebuffEffect != null) total.MaxMerge(levelData.DebuffEffect);
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.Debuff != null) total.MaxMerge(transcendBonus.Debuff);
                }
            }
            return total;
        }

        #endregion

        #region 유틸리티

        private double ParseDouble(string text)
        {
            if (double.TryParse(text, out double result))
                return result;
            return 0;
        }

        #endregion

        #region 외부 접근용 메서드

        public double GetFinalAtk()
        {
            string cleaned = PvpTxtStatAtk.Text.Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetFinalDef()
        {
            string cleaned = PvpTxtStatDef.Text.Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetFinalHp()
        {
            string cleaned = PvpTxtStatHp.Text.Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetCritDamage()
        {
            string cleaned = PvpTxtStatCriDmg.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetWeakpointDmg()
        {
            string cleaned = PvpTxtStatWekDmg.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetWeakDmgBuff()
        {
            return _weakDmgBuff;
        }

        public double GetDmgDealt()
        {
            string cleaned = PvpTxtStatDmgDealt.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetArmorPen()
        {
            string cleaned = PvpTxtStatArmPen.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetDmg1to3()
        {
            string cleaned = PvpTxtStatDmg1to3.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetDmg4to5()
        {
            string cleaned = PvpTxtStatDmg4to5.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetDmgRdc()
        {
            string cleaned = PvpTxtStatDmgRdc.Text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        public double GetDefIncrease()
        {
            // 진형에서 방증% 가져오기
            if (PvpCboFormation.SelectedIndex <= 0) return 0;
            if (PvpRbBack.IsChecked == true) return 0;  // 후방은 방증 없음

            string formationName = PvpCboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
            {
                return bonus.Def_Rate_Front;
            }
            return 0;
        }

        public double GetDmgRdc1to3()
        {
            BuffSet passiveBuffs = GetAllPassiveBuffs();
            BuffSet activeBuffs = GetAllActiveBuffs();
            return 0;
        }

        public double GetDmgRdc4to5()
        {
            BuffSet passiveBuffs = GetAllPassiveBuffs();
            BuffSet activeBuffs = GetAllActiveBuffs();
            return 0;
        }

        public bool IsSkillEnhanced()
        {
            return PvpChkSkillEnhanced.IsChecked == true;
        }

        public int GetTranscendLevel()
        {
            return PvpCboTranscend.SelectedIndex;
        }

        public Character GetSelectedCharacter()
        {
            if (PvpCboCharacter.SelectedIndex <= 0) return null;
            return CharacterDb.GetByName(PvpCboCharacter.SelectedItem.ToString());
        }

        public Skill GetSelectedSkill()
        {
            var character = GetSelectedCharacter();
            if (character == null || PvpCboSkill.SelectedIndex < 0) return null;
            string skillName = PvpCboSkill.SelectedItem?.ToString();
            return character.Skills.FirstOrDefault(s => s.Name == skillName);
        }

        public DebuffSet GetCurrentDebuffs()
        {
            return _currentDebuffs;
        }
        #endregion
    }
}

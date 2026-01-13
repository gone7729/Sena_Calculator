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

namespace GameDamageCalculator.UI
{
    public partial class MainWindow : Window
    {
        #region 필드 및 생성자

        private bool _isInitialized = false;
        private readonly DamageCalculator _calculator;
        private PresetManager _presetManager;
        private DebuffSet _currentDebuffs = new DebuffSet();
        private List<BuffConfig> _buffConfigs;

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

        public MainWindow()
        {
            InitializeComponent();
            _calculator = new DamageCalculator();
            InitializeComboBoxes();
            InitializeBuffConfigs();
            _presetManager = new PresetManager();
            RefreshPresetList();
            _isInitialized = true;
            RecalculateStats();
        }

        #endregion

        #region 초기화

        private void InitializeComboBoxes()
        {
            UpdateCharacterList();
            for (int i = 0; i <= 12; i++) cboTranscend.Items.Add($"{i}초월");
            cboTranscend.SelectedIndex = 0;

            cboEquipSet1.Items.Add("없음");
            cboEquipSet2.Items.Add("없음");
            foreach (var setName in EquipmentDb.SetEffects.Keys) { cboEquipSet1.Items.Add(setName); cboEquipSet2.Items.Add(setName); }
            cboEquipSet1.SelectedIndex = 0;
            cboEquipSet2.SelectedIndex = 0;

            cboFormation.Items.Add("없음");
            foreach (var fn in StatTable.FormationDb.Formations.Keys) cboFormation.Items.Add(fn);
            cboFormation.SelectedIndex = 0;

            InitializeMainOptionComboBoxes();
            InitializeAccessoryComboBoxes();
            UpdateBossList();

            cboPet.Items.Add("없음");
            foreach (var pet in PetDb.Pets) cboPet.Items.Add(pet.Name);
            cboPet.SelectedIndex = 0;

            cboMob.Items.Clear();
            cboMob.Items.Add("선택");
            foreach (var mob in BossDb.Mobs) cboMob.Items.Add(mob.Name);
            cboMob.SelectedIndex = 0;
        }

        private void InitializeMainOptionComboBoxes()
        {
            cboWeapon1Main.Items.Clear(); cboWeapon2Main.Items.Clear();
            cboWeapon1Main.Items.Add("없음"); cboWeapon2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["무기"]) { cboWeapon1Main.Items.Add(opt); cboWeapon2Main.Items.Add(opt); }
            cboWeapon1Main.SelectedIndex = 0; cboWeapon2Main.SelectedIndex = 0;

            cboArmor1Main.Items.Clear(); cboArmor2Main.Items.Clear();
            cboArmor1Main.Items.Add("없음"); cboArmor2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["방어구"]) { cboArmor1Main.Items.Add(opt); cboArmor2Main.Items.Add(opt); }
            cboArmor1Main.SelectedIndex = 0; cboArmor2Main.SelectedIndex = 0;
        }

        private void InitializeAccessoryComboBoxes()
        {
            cboAccessoryGrade.Items.Add("없음"); cboAccessoryGrade.Items.Add("4성"); cboAccessoryGrade.Items.Add("5성"); cboAccessoryGrade.Items.Add("6성");
            cboAccessoryGrade.SelectedIndex = 0;

            var opts = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%", "약점공격확률%", "효과적중%", "효과저항%", "보피증%", "1-3인기%", "4-5인기%" };
            foreach (var opt in opts) { cboAccessoryMain.Items.Add(opt); cboAccessorySub.Items.Add(opt); }
            cboAccessoryMain.SelectedIndex = 0; cboAccessorySub.SelectedIndex = 0;
        }

        private void InitializeBuffConfigs()
        {
            _buffConfigs = new List<BuffConfig>
            {
                new BuffConfig { Key = "BuffPassiveLion", CheckBox = chkBuffPassiveLion, Button = btnBuffPassiveLion, BaseName = "라이언", CharacterName = "라이언", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLina", CheckBox = chkBuffPassiveLina, Button = btnBuffPassiveLina, BaseName = "리나", CharacterName = "리나", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRachel", CheckBox = chkBuffPassiveRachel, Button = btnBuffPassiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDelonz", CheckBox = chkBuffPassiveDelonz, Button = btnBuffPassiveDelonz, BaseName = "델론즈", CharacterName = "델론즈", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveMiho", CheckBox = chkBuffPassiveMiho, Button = btnBuffPassiveMiho, BaseName = "미호", CharacterName = "미호", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffActiveBiscuit", CheckBox = chkBuffActiveBiscuit, Button = btnBuffActiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = "장비 강화", IsBuff = true },
                new BuffConfig { Key = "BuffActiveLina", CheckBox = chkBuffActiveLina, Button = btnBuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = true },
                new BuffConfig { Key = "DebuffPassiveTaka", CheckBox = chkDebuffPassiveTaka, Button = btnDebuffPassiveTaka, BaseName = "타카", CharacterName = "타카", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBiscuit", CheckBox = chkDebuffPassiveBiscuit, Button = btnDebuffPassiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffActiveLina", CheckBox = chkDebuffActiveLina, Button = btnDebuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelFlame", CheckBox = chkDebuffActiveRachelFlame, Button = btnDebuffActiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "염화", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelPhoenix", CheckBox = chkDebuffActiveRachelPhoenix, Button = null, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "불새", IsBuff = false }
            };
        }

        #endregion

        #region 캐릭터/스킬 이벤트

        private void CboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) UpdateCharacterList(); }

        private void UpdateCharacterList()
        {
            string prev = cboCharacter.SelectedItem?.ToString();
            cboCharacter.Items.Clear();
            cboCharacter.Items.Add("선택하세요");
            string gf = (cboGrade.SelectedItem as ComboBoxItem)?.Content.ToString();
            string tf = (cboType.SelectedItem as ComboBoxItem)?.Content.ToString();
            foreach (var c in CharacterDb.Characters)
            {
                if (gf != "전체" && c.Grade != gf) continue;
                if (tf != "전체" && c.Type != tf) continue;
                cboCharacter.Items.Add(c.Name);
            }
            cboCharacter.SelectedIndex = (prev != null && cboCharacter.Items.Contains(prev)) ? cboCharacter.Items.IndexOf(prev) : 0;
        }

        private void CboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (cboCharacter.SelectedIndex <= 0) { cboSkill.Items.Clear(); cboSkill.Items.Add("직접 입력하거나 골라주세요"); cboSkill.SelectedIndex = 0; RecalculateStats(); return; }
            var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
            if (character != null) { cboSkill.Items.Clear(); foreach (var s in character.Skills) cboSkill.Items.Add(s.Name); if (cboSkill.Items.Count > 0) cboSkill.SelectedIndex = 0; RecalculateStats(); }
        }

        private void CboSkill_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void CboTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        #endregion

        #region 장비/세트 이벤트

        private void CboEquipSet_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        private void CboSetCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            gridEquipSet2.Visibility = cboSetCount1.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (cboSetCount1.SelectedIndex != 0) cboEquipSet2.SelectedIndex = 0;
            RecalculateStats();
        }

        private void MainOption_Changed(object sender, SelectionChangedEventArgs e) { if (_isInitialized) { UpdateMainOptionDisplay(); RecalculateStats(); } }

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
            if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(cbo.SelectedItem.ToString(), out var s))
            {
                if (s.Atk_Rate > 0) return $"{s.Atk_Rate}%"; if (s.Atk > 0) return $"{s.Atk}";
                if (s.Def_Rate > 0) return $"{s.Def_Rate}%"; if (s.Def > 0) return $"{s.Def}";
                if (s.Hp_Rate > 0) return $"{s.Hp_Rate}%"; if (s.Hp > 0) return $"{s.Hp}";
                if (s.Cri > 0) return $"{s.Cri}%"; if (s.Cri_Dmg > 0) return $"{s.Cri_Dmg}%";
                if (s.Wek > 0) return $"{s.Wek}%"; if (s.Eff_Hit > 0) return $"{s.Eff_Hit}%";
                if (s.Eff_Res > 0) return $"{s.Eff_Res}%"; if (s.Dmg_Rdc > 0) return $"{s.Dmg_Rdc}%"; if (s.Blk > 0) return $"{s.Blk}%";
            }
            return "";
        }

        private void SubOption_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (_isInitialized) RecalculateStats(); }
        private void Potential_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        private void Tier_MouseLeft(object sender, MouseButtonEventArgs e) { var t = (sender as Border)?.Child as TextBox; if (t != null && int.TryParse(t.Text, out int v)) { t.Text = (v + 1).ToString(); RecalculateStats(); } }
        private void Tier_MouseRight(object sender, MouseButtonEventArgs e) { var t = (sender as Border)?.Child as TextBox; if (t != null && int.TryParse(t.Text, out int v) && v > 0) { t.Text = (v - 1).ToString(); RecalculateStats(); } e.Handled = true; }

        #endregion

        #region 장신구 이벤트

        private void CboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            panelAccessorySub.Visibility = cboAccessoryGrade.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
            if (cboAccessoryGrade.SelectedIndex != 3) cboAccessorySub.SelectedIndex = 0;
            UpdateAccessoryDisplay(); RecalculateStats();
        }

        private void CboAccessoryOption_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) { UpdateAccessoryDisplay(); RecalculateStats(); } }

        private void AccessoryGrade_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int next = (int.Parse(btn.Tag.ToString()) + 1) % 4;
            btn.Tag = next.ToString();
            btn.Content = new[] { "없음", "4성", "5성", "6성" }[next];
            cboAccessoryGrade.SelectedIndex = next;
            cboAccessorySub.Visibility = next == 3 ? Visibility.Visible : Visibility.Collapsed;
            txtAccessorySubValue.Visibility = next == 3 ? Visibility.Visible : Visibility.Collapsed;
            RecalculateStats();
        }

        private void UpdateAccessoryDisplay()
        {
            if (cboAccessoryGrade.SelectedIndex <= 0) { txtAccessoryMainValue.Text = ""; txtAccessorySubValue.Text = ""; return; }
            int g = cboAccessoryGrade.SelectedIndex + 3;
            txtAccessoryMainValue.Text = GetAccessoryOptionValue(g, cboAccessoryMain, AccessoryDb.MainOptions);
            txtAccessorySubValue.Text = g == 6 ? GetAccessoryOptionValue(g, cboAccessorySub, AccessoryDb.SubOptions) : "";
        }

        private string GetAccessoryOptionValue(int grade, ComboBox cbo, Dictionary<int, Dictionary<string, BaseStatSet>> db)
        {
            if (cbo.SelectedIndex <= 0) return "";
            if (db.TryGetValue(grade, out var opts) && opts.TryGetValue(cbo.SelectedItem.ToString(), out var s))
            {
                if (s.Atk_Rate > 0) return $"{s.Atk_Rate}%"; if (s.Def_Rate > 0) return $"{s.Def_Rate}%"; if (s.Hp_Rate > 0) return $"{s.Hp_Rate}%";
                if (s.Cri > 0) return $"{s.Cri}%"; if (s.Cri_Dmg > 0) return $"{s.Cri_Dmg}%"; if (s.Wek > 0) return $"{s.Wek}%";
                if (s.Eff_Hit > 0) return $"{s.Eff_Hit}%"; if (s.Eff_Res > 0) return $"{s.Eff_Res}%"; if (s.Blk > 0) return $"{s.Blk}%";
                if (s.Dmg_Dealt > 0) return $"{s.Dmg_Dealt}%"; if (s.Dmg_Dealt_Bos > 0) return $"{s.Dmg_Dealt_Bos}%";
                if (s.Dmg_Dealt_1to3 > 0) return $"{s.Dmg_Dealt_1to3}%"; if (s.Dmg_Dealt_4to5 > 0) return $"{s.Dmg_Dealt_4to5}%";
            }
            return "";
        }

        #endregion

        #region 진형 이벤트

        private void CboFormation_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }
        private void Formation_PositionChanged(object sender, RoutedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        private void FormationPosition_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Tag.ToString() == "Back") { btn.Tag = "Front"; btn.Content = "전열"; rbBack.IsChecked = false; rbFront.IsChecked = true; }
            else { btn.Tag = "Back"; btn.Content = "후열"; rbBack.IsChecked = true; rbFront.IsChecked = false; }
            RecalculateStats();
        }

        private double GetFormationAtkRate() { if (cboFormation.SelectedIndex <= 0 || rbFront.IsChecked == true) return 0; return StatTable.FormationDb.Formations.TryGetValue(cboFormation.SelectedItem.ToString(), out var b) ? b.Atk_Rate_Back : 0; }
        private double GetFormationDefRate() { if (cboFormation.SelectedIndex <= 0 || rbBack.IsChecked == true) return 0; return StatTable.FormationDb.Formations.TryGetValue(cboFormation.SelectedItem.ToString(), out var b) ? b.Def_Rate_Front : 0; }

        #endregion

        #region 펫 이벤트

        private void CboPet_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }
        private void CboPetFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }
        private void PetOption_Changed(object sender, EventArgs e) { if (_isInitialized) RecalculateStats(); }
        private void PetStar_Click(object sender, RoutedEventArgs e) { var btn = sender as Button; int n = int.Parse(btn.Tag.ToString()) == 6 ? 4 : int.Parse(btn.Tag.ToString()) + 1; btn.Tag = n.ToString(); btn.Content = $"{n}성"; RecalculateStats(); }

        private int GetPetStar() { var s = (cboPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6성"; return s == "4성" ? 4 : s == "5성" ? 5 : s == "6성" ? 6 : 0; }
        private double GetPetFlatAtk() { if (cboPet.SelectedIndex <= 0) return 0; var p = PetDb.GetByName(cboPet.SelectedItem.ToString()); return p?.GetBaseStats(GetPetStar()).Atk ?? 0; }
        private double GetPetFlatDef() { if (cboPet.SelectedIndex <= 0) return 0; var p = PetDb.GetByName(cboPet.SelectedItem.ToString()); return p?.GetBaseStats(GetPetStar()).Def ?? 0; }
        private double GetPetFlatHp() { if (cboPet.SelectedIndex <= 0) return 0; var p = PetDb.GetByName(cboPet.SelectedItem.ToString()); return p?.GetBaseStats(GetPetStar()).Hp ?? 0; }
        private double GetPetSkillAtkRate() { if (cboPet.SelectedIndex <= 0) return 0; var p = PetDb.GetByName(cboPet.SelectedItem.ToString()); int s = GetPetStar(); return s == 0 ? 0 : p?.GetSkillBonus(s).Atk_Rate ?? 0; }
        private double GetPetOptionAtkRate() => double.TryParse(txtPetAtk.Text, out double v) ? v : 0;
        private double GetPetOptionDefRate() => double.TryParse(txtPetDef.Text, out double v) ? v : 0;
        private double GetPetOptionHpRate() => double.TryParse(txtPetHp.Text, out double v) ? v : 0;

        #endregion

        #region 보스/대상 이벤트

        private void BossType_Changed(object sender, RoutedEventArgs e) { if (_isInitialized) UpdateBossList(); }
        private void BossOption_Changed(object sender, TextChangedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        private void BattleMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (rbBoss.IsChecked == true) { spBossType.Visibility = Visibility.Visible; cboBoss.Visibility = Visibility.Visible; cboMob.Visibility = Visibility.Collapsed; }
            else { spBossType.Visibility = Visibility.Collapsed; cboBoss.Visibility = Visibility.Collapsed; cboMob.Visibility = Visibility.Visible; }
            RecalculateStats();
        }

        private void CboBoss_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (cboBoss.SelectedIndex <= 0) { txtBossDef.Text = "0"; txtBossDefInc.Text = "0"; txtBossDmgRdc.Text = "0"; txtBoss1TargetRdc.Text = "0"; txtBoss3TargetRdc.Text = "0"; txtBoss5TargetRdc.Text = "0"; txtBossHp.Text = "0"; return; }
            string sel = cboBoss.SelectedItem?.ToString(); if (string.IsNullOrEmpty(sel)) return;
            Boss boss = rbSiege.IsChecked == true ? BossDb.SiegeBosses.FirstOrDefault(b => sel.Contains(b.Name)) : rbRaid.IsChecked == true ? BossDb.RaidBosses.FirstOrDefault(b => sel.Contains(b.Name) && sel.Contains($"{b.Difficulty}단계")) : null;
            if (boss != null)
            {
                txtBossDef.Text = boss.Stats.Def.ToString("N0"); txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0"); txtBossDmgRdc.Text = "0"; txtBossHp.Text = boss.Stats.Hp.ToString("N0");
                txtBoss1TargetRdc.Text = boss.SingleTargetReduction.ToString("F0"); txtBoss3TargetRdc.Text = boss.TripleTargetReduction.ToString("F0"); txtBoss5TargetRdc.Text = boss.MultiTargetReduction.ToString("F0");
                if (!string.IsNullOrEmpty(boss.DefenseIncreaseCondition)) { panelBossCondition.Visibility = Visibility.Visible; txtBossCondition.Text = boss.DefenseIncreaseCondition; chkBossCondition.IsChecked = false; }
                else { panelBossCondition.Visibility = Visibility.Collapsed; }
            }
        }

        private void CboMob_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_isInitialized) return; if (cboMob.SelectedIndex > 0) { var m = BossDb.GetMobByName(cboMob.SelectedItem.ToString()); if (m != null) { txtBossDef.Text = m.Stats.Def.ToString(); txtBossHp.Text = m.Stats.Hp.ToString("N0"); } } RecalculateStats(); }

        private void BossCondition_Changed(object sender, RoutedEventArgs e) { if (!_isInitialized) return; if (chkBossCondition.IsChecked == true) { txtBossDefInc.Text = "0"; txtBossHp.Text = (ParseDouble(txtBossHp.Text) * 0.29).ToString("N0"); } else { var b = GetSelectedBoss(); if (b != null) { txtBossDefInc.Text = b.DefenseIncrease.ToString("F0"); txtBossHp.Text = b.Stats.Hp.ToString("N0"); } } }

        private void UpdateBossList()
        {
            cboBoss.Items.Clear(); cboBoss.Items.Add("직접 입력");
            if (rbSiege.IsChecked == true) foreach (var b in BossDb.SiegeBosses) cboBoss.Items.Add($"{b.Name} ({b.DayOfWeek})");
            else if (rbRaid.IsChecked == true) foreach (var b in BossDb.RaidBosses) cboBoss.Items.Add($"{b.Name} {b.Difficulty}단계");
            else cboBoss.Items.Add("장원 보스");
            cboBoss.SelectedIndex = 0;
        }

        private void UpdateBossDebuffDisplay() { txtBossDefRed.Text = _currentDebuffs.Def_Reduction.ToString("F0"); txtBossDmgTaken.Text = _currentDebuffs.Dmg_Taken_Increase.ToString("F0"); txtBossVulnerable.Text = _currentDebuffs.Vulnerability.ToString("F0"); }

        private Boss GetSelectedBoss()
        {
            if (cboBoss.SelectedIndex <= 0) return new Boss { Name = "직접 입력", BossType = BossType.Siege, Stats = new BaseStatSet { Def = ParseDouble(txtBossDef.Text) }, DefenseIncrease = ParseDouble(txtBossDefInc.Text) };
            string sel = cboBoss.SelectedItem?.ToString();
            if (rbSiege.IsChecked == true) return BossDb.SiegeBosses.FirstOrDefault(b => sel.Contains(b.Name));
            if (rbRaid.IsChecked == true) return BossDb.RaidBosses.FirstOrDefault(b => sel.Contains(b.Name) && sel.Contains($"{b.Difficulty}단계"));
            return new Boss { Name = "Unknown", Stats = new BaseStatSet { Def = 0 } };
        }

        private double GetSelectedTargetReduction() => 0;

        #endregion

        #region 계산 버튼

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboCharacter.SelectedIndex <= 0) { txtResult.Text = "캐릭터를 선택해주세요."; return; }
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character == null) { txtResult.Text = "캐릭터 정보를 찾을 수 없습니다."; return; }

                Skill selectedSkill = null;
                if (cboSkill.SelectedIndex >= 0 && cboSkill.Items.Count > 0) selectedSkill = character.Skills.FirstOrDefault(s => s.Name == cboSkill.SelectedItem?.ToString());
                if (selectedSkill == null) { txtResult.Text = "스킬을 선택해주세요."; return; }

                BuffSet passiveBuffs = GetAllPassiveBuffs(), activeBuffs = GetAllActiveBuffs();
                double weakDmgBuff = passiveBuffs.Wek_Dmg + activeBuffs.Wek_Dmg;
                BattleMode mode = rbMob.IsChecked == true ? BattleMode.Mob : BattleMode.Boss;

                double buffAtkRate = GetPetOptionAtkRate() + GetAllPassiveBuffs().Atk_Rate + GetAllActiveBuffs().Atk_Rate;
                if (cboCharacter.SelectedIndex > 0)
                {
                    var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                    if (c?.Passive != null) { var buff = c.Passive.GetTotalSelfBuff(chkSkillEnhanced.IsChecked == true, cboTranscend.SelectedIndex, chkPassiveCondition.IsChecked == true); if (buff != null) buffAtkRate += buff.Atk_Rate; }
                }

                var input = new DamageCalculator.DamageInput
                {
                    Character = character, Skill = selectedSkill, IsSkillEnhanced = chkSkillEnhanced.IsChecked == true, TranscendLevel = cboTranscend.SelectedIndex,
                    FinalAtk = ParseStatValue(txtStatAtk.Text), FinalDef = ParseStatValue(txtStatDef.Text), FinalHp = ParseStatValue(txtStatHp.Text),
                    CritDamage = ParseStatValue(txtStatCriDmg.Text), DmgDealt = ParseStatValue(txtStatDmgDealt.Text), DmgDealtBoss = ParseStatValue(txtStatBossDmg.Text),
                    ArmorPen = ParseStatValue(txtStatArmPen.Text), WeakpointDmg = ParseStatValue(txtStatWekDmg.Text), WeakpointDmgBuff = weakDmgBuff,
                    Dmg1to3 = ParseStatValue(txtStatDmg1to3.Text), Dmg4to5 = ParseStatValue(txtStatDmg4to5.Text),
                    DefReduction = _currentDebuffs.Def_Reduction, DmgTakenIncrease = _currentDebuffs.Dmg_Taken_Increase, Vulnerability = _currentDebuffs.Vulnerability, HealReduction = _currentDebuffs.Heal_Reduction, EffResReduction = _currentDebuffs.Eff_Red,
                    BossDef = ParseDouble(txtBossDef.Text), BossDefIncrease = ParseDouble(txtBossDefInc.Text), BossDmgReduction = ParseDouble(txtBossDmgRdc.Text), BossTargetReduction = GetSelectedTargetReduction(),
                    IsCritical = chkCritical.IsChecked == true, IsWeakpoint = chkWeakpoint.IsChecked == true, IsBlocked = chkBlock.IsChecked == true,
                    TargetStackCount = int.TryParse(txtTargetStackCount.Text, out int stacks) ? stacks : 0, ForceStatusEffect = chkStatusEffect.IsChecked == true, IsLostHpConditionMet = chkLostHpCondition.IsChecked == true,
                    IsSkillConditionMet = chkSkillCondition.IsChecked == true, AtkBuff = buffAtkRate, Mode = mode
                };
                txtResult.Text = _calculator.Calculate(input).Details;
            }
            catch (Exception ex) { txtResult.Text = $"오류: {ex.Message}"; }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            cboCharacter.SelectedIndex = 0; cboTranscend.SelectedIndex = 0; chkSkillEnhanced.IsChecked = false;
            cboPotentialAtk.SelectedIndex = 0; cboPotentialDef.SelectedIndex = 0; cboPotentialHp.SelectedIndex = 0;
            cboEquipSet1.SelectedIndex = 0; cboEquipSet2.SelectedIndex = 0; cboSetCount1.SelectedIndex = 1;
            cboAccessoryGrade.SelectedIndex = 0;
            txtSubAtkRate.Text = "0"; txtSubAtk.Text = "0"; txtSubCri.Text = "0"; txtSubCriDmg.Text = "0"; txtSubWek.Text = "0"; txtSubBlk.Text = "0"; txtSubDmgRdc.Text = "0";
            txtSubDefRate.Text = "0"; txtSubDef.Text = "0"; txtSubHpRate.Text = "0"; txtSubHp.Text = "0"; txtSubEffHit.Text = "0"; txtSubEffRes.Text = "0"; txtSubSpd.Text = "0";
            cboFormation.SelectedIndex = 0; rbBack.IsChecked = true;
            cboPet.SelectedIndex = 0; cboPetStar.SelectedIndex = 2; txtPetOpt1.Text = "0"; txtPetOpt2.Text = "0"; txtPetOpt3.Text = "0";
            txtBossDef.Text = "0"; txtBossDefInc.Text = "0"; txtBossDmgRdc.Text = "0"; txtBossDefRed.Text = "0"; txtBossDmgTaken.Text = "0"; txtBossVulnerable.Text = "0"; txtBossHp.Text = "0";
            txtBoss1TargetRdc.Text = "0"; txtBoss3TargetRdc.Text = "0"; txtBoss5TargetRdc.Text = "0";
            panelBossCondition.Visibility = Visibility.Collapsed; chkBossCondition.IsChecked = false;
            foreach (var c in _buffConfigs) { c.CheckBox.IsChecked = false; if (c.Button != null) ResetBuffOptionButton(c.Button, c.BaseName); }
            txtResult.Text = "계산 버튼을 눌러\n결과를 확인하세요.";
        }

        #endregion

        #region 스탯 계산

        private void RecalculateStats()
        {
            if (!_isInitialized) return;

            double baseAtk = 0, baseDef = 0, baseHp = 0;
            BaseStatSet characterStats = new BaseStatSet();
            if (cboCharacter.SelectedIndex > 0) { var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString()); if (c != null) { characterStats = c.GetBaseStats(); baseAtk = characterStats.Atk; baseDef = characterStats.Def; baseHp = characterStats.Hp; } }

            BuffSet characterPassiveBuff = new BuffSet();
            if (cboCharacter.SelectedIndex > 0) { var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString()); if (c?.Passive != null) { var buff = c.Passive.GetTotalSelfBuff(chkSkillEnhanced.IsChecked == true, cboTranscend.SelectedIndex, chkPassiveCondition.IsChecked == true); if (buff != null) characterPassiveBuff.Add(buff); } }

            var potentialStats = GetPotentialStats(); var subStats = GetSubOptionStats(); var mainOptionStats = GetMainOptionStats(); var accessoryStats = GetAccessoryStats();
            BuffSet passiveBuffs = GetAllPassiveBuffs(), activeBuffs = GetAllActiveBuffs();
            BuffSet totalBuffs = new BuffSet(); totalBuffs.Add(passiveBuffs); totalBuffs.Add(activeBuffs);
            _currentDebuffs = new DebuffSet(); _currentDebuffs.Add(GetAllPassiveDebuffs()); _currentDebuffs.Add(GetAllActiveDebuffs());

            BaseStatSet transcendStats = new BaseStatSet();
            if (cboCharacter.SelectedIndex > 0) { var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString()); if (c != null) transcendStats = c.GetTranscendStats(cboTranscend.SelectedIndex); }

            BaseStatSet setBonus = new BaseStatSet();
            if (cboEquipSet1.SelectedIndex > 0) setBonus.Add(GetSetBonus(cboEquipSet1.SelectedItem.ToString(), cboSetCount1.SelectedIndex == 0 ? 2 : 4));
            if (cboSetCount1.SelectedIndex == 0 && cboEquipSet2.SelectedIndex > 0) setBonus.Add(GetSetBonus(cboEquipSet2.SelectedItem.ToString(), 2));

            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2, equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def * 2, equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;
            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + GetPetFlatAtk() + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + GetPetFlatDef() + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + GetPetFlatHp() + mainOptionStats.Hp;

            double totalSpd = characterStats.Spd + subStats.Spd;
            double totalAtkRate = transcendStats.Atk_Rate + GetFormationAtkRate() + setBonus.Atk_Rate + subStats.Atk_Rate + accessoryStats.Atk_Rate + GetPetOptionAtkRate() + mainOptionStats.Atk_Rate + GetPetSkillAtkRate();
            double totalDefRate = transcendStats.Def_Rate + GetFormationDefRate() + setBonus.Def_Rate + subStats.Def_Rate + accessoryStats.Def_Rate + GetPetOptionDefRate() + mainOptionStats.Def_Rate;
            double totalHpRate = transcendStats.Hp_Rate + setBonus.Hp_Rate + subStats.Hp_Rate + accessoryStats.Hp_Rate + GetPetOptionHpRate() + mainOptionStats.Hp_Rate;

            double buffAtkRate = GetPetOptionAtkRate() + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate = GetPetOptionDefRate() + totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate = GetPetOptionHpRate() + totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            double scalingFlatAtk = 0, scalingFlatDef = 0, scalingFlatHp = 0, scalingCri = 0;
            if (cboCharacter.SelectedIndex > 0)
            {
                var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (c?.Passive != null)
                {
                    var passiveData = c.Passive.GetLevelData(chkSkillEnhanced.IsChecked == true);
                    if (passiveData?.StatScalings != null)
                    {
                        foreach (var sc in passiveData.StatScalings)
                        {
                            double src = sc.SourceStat switch { StatType.Spd => totalSpd, StatType.Hp => baseStatHp, StatType.Def => baseStatDef, StatType.Atk => baseStatAtk, _ => 0 };
                            double bonus = CalcStatScaling(src, sc);
                            switch (sc.TargetStat) { case StatType.Atk: scalingFlatAtk += bonus; break; case StatType.Def: scalingFlatDef += bonus; break; case StatType.Hp: scalingFlatHp += bonus; break; case StatType.Cri: scalingCri += bonus; break; }
                        }
                    }
                }
            }
            baseStatAtk += scalingFlatAtk; baseStatDef += scalingFlatDef; baseStatHp += scalingFlatHp;

            double totalAtk = baseStatAtk * (1 + buffAtkRate / 100.0), totalDef = baseStatDef * (1 + buffDefRate / 100.0), totalHp = baseStatHp * (1 + buffHpRate / 100.0);
            double pureBaseAtk = baseAtk * (1 + (mainOptionStats.Atk_Rate + subStats.Atk_Rate + transcendStats.Atk_Rate + accessoryStats.Atk_Rate + setBonus.Atk_Rate) / 100) + flatAtk;
            double pureBaseDef = baseDef * (1 + (mainOptionStats.Def_Rate + subStats.Def_Rate + transcendStats.Def_Rate + accessoryStats.Def_Rate + setBonus.Def_Rate) / 100) + flatDef;
            double pureBaseHp = baseHp * (1 + (mainOptionStats.Hp_Rate + subStats.Hp_Rate + transcendStats.Hp_Rate + accessoryStats.Hp_Rate + setBonus.Hp_Rate) / 100) + flatHp;

            txtStatAtkBase.Text = pureBaseAtk.ToString("N0"); txtStatDefBase.Text = pureBaseDef.ToString("N0"); txtStatHpBase.Text = pureBaseHp.ToString("N0");
            txtStatAtk.Text = totalAtk.ToString("N0"); txtStatDef.Text = totalDef.ToString("N0"); txtStatHp.Text = totalHp.ToString("N0"); txtStatSpd.Text = totalSpd.ToString("N0");

            BaseStatSet ds = new BaseStatSet
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
                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5,
                Atk_Rate = totalAtkRate
            };
            UpdateStatDisplay(ds); UpdateBossDebuffDisplay();
        }

        private void UpdateStatDisplay(BaseStatSet s)
        {
            txtStatCri.Text = $"{s.Cri}%"; txtStatCriDmg.Text = $"{s.Cri_Dmg}%"; txtStatWek.Text = $"{s.Wek}%"; txtStatWekDmg.Text = $"{s.Wek_Dmg}%";
            txtStatDmgDealt.Text = $"{s.Dmg_Dealt}%"; txtStatBossDmg.Text = $"{s.Dmg_Dealt_Bos}%"; txtStatDmg1to3.Text = $"{s.Dmg_Dealt_1to3}%"; txtStatDmg4to5.Text = $"{s.Dmg_Dealt_4to5}%";
            txtStatArmPen.Text = $"{s.Arm_Pen}%"; txtStatBlk.Text = $"{s.Blk}%"; txtStatEffHit.Text = $"{s.Eff_Hit}%"; txtStatEffRes.Text = $"{s.Eff_Res}%";
            txtStatEffAcc.Text = $"{s.Eff_Acc}%"; txtStatDmgRdc.Text = $"{s.Dmg_Rdc}%"; txtStatAtkRate.Text = $"{s.Atk_Rate}%";
        }

        private double CalcStatScaling(double src, StatScaling sc) => sc == null || sc.SourceUnit <= 0 ? 0 : Math.Min(Math.Floor(src / sc.SourceUnit) * sc.PerUnit, sc.MaxValue);

        #endregion

        #region DB 헬퍼

        private BaseStatSet GetSetBonus(string name, int count) { BaseStatSet t = new BaseStatSet(); if (EquipmentDb.SetEffects.TryGetValue(name, out var d) && d.TryGetValue(count, out var b)) t.Add(b); return t; }

        private BaseStatSet GetMainOptionStats() { BaseStatSet s = new BaseStatSet(); foreach (var c in new[] { cboWeapon1Main, cboWeapon2Main, cboArmor1Main, cboArmor2Main }) if (c.SelectedIndex > 0 && EquipmentDb.MainStatDb.MainOptions.TryGetValue(c.SelectedItem.ToString(), out var b)) s.Add(b); return s; }

        private BaseStatSet GetSubOptionStats()
        {
            BaseStatSet r = new BaseStatSet();
            if (int.TryParse(txtSubAtkRate.Text, out int t) && t > 0) r.Atk_Rate = 5 * t;
            if (int.TryParse(txtSubAtk.Text, out t) && t > 0) r.Atk = 30 * t;
            if (int.TryParse(txtSubCri.Text, out t) && t > 0) r.Cri = 4 * t;
            if (int.TryParse(txtSubCriDmg.Text, out t) && t > 0) r.Cri_Dmg = 5 * t;
            if (int.TryParse(txtSubWek.Text, out t) && t > 0) r.Wek = 4 * t;
            if (int.TryParse(txtSubBlk.Text, out t) && t > 0) r.Blk = 5 * t;
            if (int.TryParse(txtSubDmgRdc.Text, out t) && t > 0) r.Dmg_Rdc = 3 * t;
            if (int.TryParse(txtSubDefRate.Text, out t) && t > 0) r.Def_Rate = 5 * t;
            if (int.TryParse(txtSubDef.Text, out t) && t > 0) r.Def = 30 * t;
            if (int.TryParse(txtSubHpRate.Text, out t) && t > 0) r.Hp_Rate = 5 * t;
            if (int.TryParse(txtSubHp.Text, out t) && t > 0) r.Hp = 200 * t;
            if (int.TryParse(txtSubEffHit.Text, out t) && t > 0) r.Eff_Hit = 5 * t;
            if (int.TryParse(txtSubEffRes.Text, out t) && t > 0) r.Eff_Res = 5 * t;
            if (int.TryParse(txtSubSpd.Text, out t) && t > 0) r.Spd = 4 * t;
            return r;
        }

        private BaseStatSet GetAccessoryStats()
        {
            BaseStatSet s = new BaseStatSet(); if (cboAccessoryGrade.SelectedIndex <= 0) return s;
            int g = cboAccessoryGrade.SelectedIndex + 3;
            if (AccessoryDb.GradeBonus.TryGetValue(g, out var gb)) s.Add(gb);
            if (cboAccessoryMain.SelectedIndex > 0 && AccessoryDb.MainOptions.TryGetValue(g, out var mo) && mo.TryGetValue(cboAccessoryMain.SelectedItem.ToString(), out var mb)) s.Add(mb);
            if (g == 6 && cboAccessorySub.SelectedIndex > 0 && AccessoryDb.SubOptions.TryGetValue(g, out var so) && so.TryGetValue(cboAccessorySub.SelectedItem.ToString(), out var sb)) s.Add(sb);
            return s;
        }

        private BaseStatSet GetPotentialStats()
        {
            BaseStatSet s = new BaseStatSet();
            if (cboPotentialAtk.SelectedIndex > 0) s.Atk = StatTable.PotentialDb.Stats["공격력"][cboPotentialAtk.SelectedIndex - 1];
            if (cboPotentialDef.SelectedIndex > 0) s.Def = StatTable.PotentialDb.Stats["방어력"][cboPotentialDef.SelectedIndex - 1];
            if (cboPotentialHp.SelectedIndex > 0) s.Hp = StatTable.PotentialDb.Stats["생명력"][cboPotentialHp.SelectedIndex - 1];
            return s;
        }

        #endregion

        #region 유틸리티

        private double ParseStatValue(string t) => double.TryParse(t.Replace("%", "").Replace(",", "").Trim(), out double r) ? r : 0;
        private double ParseDouble(string t) => double.TryParse(t, out double r) ? r : 0;

        #endregion

        #region 버프/디버프 옵션 버튼

        private static readonly Brush[] BuffBgColors = { new SolidColorBrush(Color.FromRgb(58, 58, 58)), new SolidColorBrush(Color.FromRgb(180, 150, 50)), new SolidColorBrush(Color.FromRgb(70, 130, 180)), new SolidColorBrush(Color.FromRgb(138, 43, 226)) };
        private static readonly Brush[] BuffFgColors = { new SolidColorBrush(Color.FromRgb(204, 204, 204)), Brushes.Black, Brushes.White, Brushes.White };
        private static readonly string[] BuffOptionSuffix = { "", " 스강", " 초월", " 풀" };

        private void BuffOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int next = (int.Parse(btn.Tag?.ToString() ?? "0") + 1) % 4;
                btn.Tag = next;
                string baseName = btn.Content.ToString().Replace(" 풀", "").Replace(" 스강", "").Replace(" 초월", "");
                btn.Background = BuffBgColors[next]; btn.Foreground = BuffFgColors[next]; btn.Content = baseName + BuffOptionSuffix[next];
                RecalculateStats();
            }
        }

        private (bool isEnhanced, int transcendLevel) GetBuffOption(Button btn) => btn == null ? (false, 0) : int.Parse(btn.Tag?.ToString() ?? "0") switch { 1 => (true, 0), 2 => (false, 6), 3 => (true, 6), _ => (false, 0) };
        private void ResetBuffOptionButton(Button btn, string name) { btn.Tag = 0; btn.Background = BuffBgColors[0]; btn.Foreground = BuffFgColors[0]; btn.Content = name; }
        private void ApplyBuffButtonState(Button btn, string name, int state) { btn.Tag = state; btn.Background = BuffBgColors[state]; btn.Foreground = BuffFgColors[state]; btn.Content = name + BuffOptionSuffix[state]; }

        #endregion

        #region 버프/디버프 합산

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e) { if (_isInitialized) RecalculateStats(); }

        private BuffSet GetAllPassiveBuffs() { BuffSet t = new BuffSet(); foreach (var c in _buffConfigs.Where(x => x.IsBuff && x.SkillName == null)) { if (c.CheckBox.IsChecked != true) continue; var (en, tr) = GetBuffOption(c.Button); var ch = CharacterDb.GetByName(c.CharacterName); var buff = ch?.Passive?.GetPartyBuff(en, tr); if (buff != null) t.MaxMerge(buff); } return t; }

        private BuffSet GetAllActiveBuffs()
        {
            BuffSet t = new BuffSet();
            foreach (var c in _buffConfigs.Where(x => x.IsBuff && x.SkillName != null))
            {
                if (c.CheckBox.IsChecked != true) continue;
                var (en, tr) = GetBuffOption(c.Button);
                var ch = CharacterDb.GetByName(c.CharacterName);
                var skill = ch?.Skills?.FirstOrDefault(s => s.Name == c.SkillName);
                if (skill != null)
                {
                    var ld = skill.GetLevelData(en); if (ld?.PartyBuff != null) t.MaxMerge(ld.PartyBuff);
                    var tb = skill.GetTranscendBonus(tr); if (tb?.Bonus != null) t.MaxMerge(tb.Bonus);
                }
            }
            return t;
        }

        private DebuffSet GetAllPassiveDebuffs() { DebuffSet t = new DebuffSet(); foreach (var c in _buffConfigs.Where(x => !x.IsBuff && x.SkillName == null)) { if (c.CheckBox.IsChecked != true) continue; var (en, tr) = GetBuffOption(c.Button); var ch = CharacterDb.GetByName(c.CharacterName); var d = ch?.Passive?.GetTotalDebuff(en, tr); if (d != null) t.MaxMerge(d); } return t; }

        private DebuffSet GetAllActiveDebuffs()
        {
            DebuffSet t = new DebuffSet();
            foreach (var c in _buffConfigs.Where(x => !x.IsBuff && x.SkillName != null))
            {
                if (c.CheckBox.IsChecked != true) continue;
                Button btn = c.Button ?? _buffConfigs.FirstOrDefault(x => x.CharacterName == c.CharacterName && x.Button != null)?.Button;
                var (en, tr) = GetBuffOption(btn);
                var ch = CharacterDb.GetByName(c.CharacterName);
                var skill = ch?.Skills?.FirstOrDefault(s => s.Name == c.SkillName);
                if (skill != null) { var ld = skill.GetLevelData(en); if (ld?.DebuffEffect != null) t.MaxMerge(ld.DebuffEffect); var tb = skill.GetTranscendBonus(tr); if (tb?.Debuff != null) t.MaxMerge(tb.Debuff); }
            }
            return t;
        }

        #endregion

        #region 프리셋

        private void RefreshPresetList() { cboPreset.Items.Clear(); cboPreset.Items.Add("-- 프리셋 선택 --"); foreach (var n in _presetManager.GetPresetNames()) cboPreset.Items.Add(n); cboPreset.SelectedIndex = 0; }

        private Preset CreatePresetFromUI()
        {
            var p = new Preset
            {
                CharacterName = cboCharacter.SelectedIndex > 0 ? cboCharacter.SelectedItem.ToString() : "", SkillName = cboSkill.SelectedIndex >= 0 ? cboSkill.SelectedItem?.ToString() : "",
                TranscendLevel = cboTranscend.SelectedIndex, IsSkillEnhanced = chkSkillEnhanced.IsChecked == true,
                PotentialAtk = cboPotentialAtk.SelectedIndex, PotentialDef = cboPotentialDef.SelectedIndex, PotentialHp = cboPotentialHp.SelectedIndex,
                EquipSet1 = cboEquipSet1.SelectedIndex > 0 ? cboEquipSet1.SelectedItem.ToString() : "", EquipSet2 = cboEquipSet2.SelectedIndex > 0 ? cboEquipSet2.SelectedItem.ToString() : "", SetCount1 = cboSetCount1.SelectedIndex,
                MainWeapon1 = cboWeapon1Main.SelectedIndex > 0 ? cboWeapon1Main.SelectedItem.ToString() : "", MainWeapon2 = cboWeapon2Main.SelectedIndex > 0 ? cboWeapon2Main.SelectedItem.ToString() : "",
                MainArmor1 = cboArmor1Main.SelectedIndex > 0 ? cboArmor1Main.SelectedItem.ToString() : "", MainArmor2 = cboArmor2Main.SelectedIndex > 0 ? cboArmor2Main.SelectedItem.ToString() : "",
                AccessoryGrade = cboAccessoryGrade.SelectedIndex, AccessoryOption = cboAccessoryMain.SelectedIndex > 0 ? cboAccessoryMain.SelectedItem.ToString() : "", AccessorySubOption = cboAccessorySub.SelectedIndex > 0 ? cboAccessorySub.SelectedItem.ToString() : "",
                Formation = cboFormation.SelectedIndex > 0 ? cboFormation.SelectedItem.ToString() : "", IsBackPosition = rbBack.IsChecked == true,
                PetName = cboPet.SelectedIndex > 0 ? cboPet.SelectedItem.ToString() : "", PetStar = cboPetStar.SelectedIndex,
                BossType = rbSiege.IsChecked == true ? "Siege" : rbRaid.IsChecked == true ? "Raid" : "Descend", BossName = cboBoss.SelectedIndex > 0 ? cboBoss.SelectedItem.ToString() : ""
            };
            p.SubOptions = new Dictionary<string, int> { {"AtkRate",int.TryParse(txtSubAtkRate.Text,out int t)?t:0},{"Atk",int.TryParse(txtSubAtk.Text,out t)?t:0},{"Cri",int.TryParse(txtSubCri.Text,out t)?t:0},{"CriDmg",int.TryParse(txtSubCriDmg.Text,out t)?t:0},{"Wek",int.TryParse(txtSubWek.Text,out t)?t:0},{"Blk",int.TryParse(txtSubBlk.Text,out t)?t:0},{"DmgRdc",int.TryParse(txtSubDmgRdc.Text,out t)?t:0},{"DefRate",int.TryParse(txtSubDefRate.Text,out t)?t:0},{"Def",int.TryParse(txtSubDef.Text,out t)?t:0},{"HpRate",int.TryParse(txtSubHpRate.Text,out t)?t:0},{"Hp",int.TryParse(txtSubHp.Text,out t)?t:0},{"EffHit",int.TryParse(txtSubEffHit.Text,out t)?t:0},{"EffRes",int.TryParse(txtSubEffRes.Text,out t)?t:0} };
            p.PetAtkRate = ParseDouble(txtPetAtk.Text); p.PetDefRate = ParseDouble(txtPetDef.Text); p.PetHpRate = ParseDouble(txtPetHp.Text);
            p.BuffChecks = new Dictionary<string, bool>(); p.BuffStates = new Dictionary<string, int>();
            foreach (var c in _buffConfigs) { p.BuffChecks[c.Key] = c.CheckBox.IsChecked == true; if (c.Button != null) p.BuffStates[c.Key] = int.Parse(c.Button.Tag?.ToString() ?? "0"); }
            return p;
        }

        private void ApplyPresetToUI(Preset p)
        {
            if (p == null) return; _isInitialized = false;
            SelectComboBoxItem(cboCharacter, p.CharacterName); cboTranscend.SelectedIndex = p.TranscendLevel; chkSkillEnhanced.IsChecked = p.IsSkillEnhanced;
            if (cboCharacter.SelectedIndex > 0) { var c = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString()); if (c != null) { cboSkill.Items.Clear(); foreach (var s in c.Skills) cboSkill.Items.Add(s.Name); } }
            SelectComboBoxItem(cboSkill, p.SkillName);
            cboPotentialAtk.SelectedIndex = p.PotentialAtk; cboPotentialDef.SelectedIndex = p.PotentialDef; cboPotentialHp.SelectedIndex = p.PotentialHp;
            SelectComboBoxItem(cboEquipSet1, p.EquipSet1); SelectComboBoxItem(cboEquipSet2, p.EquipSet2); cboSetCount1.SelectedIndex = p.SetCount1;
            SelectComboBoxItem(cboWeapon1Main, p.MainWeapon1); SelectComboBoxItem(cboWeapon2Main, p.MainWeapon2); SelectComboBoxItem(cboArmor1Main, p.MainArmor1); SelectComboBoxItem(cboArmor2Main, p.MainArmor2);
            cboAccessoryGrade.SelectedIndex = p.AccessoryGrade; SelectComboBoxItem(cboAccessoryMain, p.AccessoryOption); SelectComboBoxItem(cboAccessorySub, p.AccessorySubOption);
            SelectComboBoxItem(cboFormation, p.Formation); if (p.IsBackPosition) rbBack.IsChecked = true; else rbFront.IsChecked = true;
            SelectComboBoxItem(cboPet, p.PetName); cboPetStar.SelectedIndex = p.PetStar;
            if (p.SubOptions != null) { txtSubAtkRate.Text = p.SubOptions.GetValueOrDefault("AtkRate",0).ToString(); txtSubAtk.Text = p.SubOptions.GetValueOrDefault("Atk",0).ToString(); txtSubCri.Text = p.SubOptions.GetValueOrDefault("Cri",0).ToString(); txtSubCriDmg.Text = p.SubOptions.GetValueOrDefault("CriDmg",0).ToString(); txtSubWek.Text = p.SubOptions.GetValueOrDefault("Wek",0).ToString(); txtSubBlk.Text = p.SubOptions.GetValueOrDefault("Blk",0).ToString(); txtSubDmgRdc.Text = p.SubOptions.GetValueOrDefault("DmgRdc",0).ToString(); txtSubDefRate.Text = p.SubOptions.GetValueOrDefault("DefRate",0).ToString(); txtSubDef.Text = p.SubOptions.GetValueOrDefault("Def",0).ToString(); txtSubHpRate.Text = p.SubOptions.GetValueOrDefault("HpRate",0).ToString(); txtSubHp.Text = p.SubOptions.GetValueOrDefault("Hp",0).ToString(); txtSubEffHit.Text = p.SubOptions.GetValueOrDefault("EffHit",0).ToString(); txtSubEffRes.Text = p.SubOptions.GetValueOrDefault("EffRes",0).ToString(); }
            txtPetAtk.Text = p.PetAtkRate.ToString(); txtPetDef.Text = p.PetDefRate.ToString(); txtPetHp.Text = p.PetHpRate.ToString();
            if (p.BossType == "Siege") rbSiege.IsChecked = true; else if (p.BossType == "Raid") rbRaid.IsChecked = true; else rbDescend.IsChecked = true;
            UpdateBossList(); SelectComboBoxItem(cboBoss, p.BossName);
            if (p.BuffChecks != null) foreach (var c in _buffConfigs) { c.CheckBox.IsChecked = p.BuffChecks.GetValueOrDefault(c.Key, false); if (c.Button != null && p.BuffStates != null) ApplyBuffButtonState(c.Button, c.BaseName, p.BuffStates.GetValueOrDefault(c.Key, 0)); }
            _isInitialized = true; UpdateMainOptionDisplay(); UpdateAccessoryDisplay(); RecalculateStats();
        }

        private void SelectComboBoxItem(ComboBox cbo, string val) { if (string.IsNullOrEmpty(val)) { cbo.SelectedIndex = 0; return; } for (int i = 0; i < cbo.Items.Count; i++) if (cbo.Items[i].ToString() == val) { cbo.SelectedIndex = i; return; } cbo.SelectedIndex = 0; }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e) { if (cboPreset.SelectedIndex > 0) { var p = CreatePresetFromUI(); p.Name = cboPreset.SelectedItem.ToString(); _presetManager.UpdatePreset(cboPreset.SelectedIndex - 1, p); MessageBox.Show($"'{p.Name}' 프리셋이 저장되었습니다.", "저장 완료"); } else BtnSaveAsPreset_Click(sender, e); }

        private void BtnSaveAsPreset_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "프리셋 이름", Width = 300, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = new SolidColorBrush(Color.FromRgb(39, 39, 39)) };
            var sp = new StackPanel { Margin = new Thickness(15) }; var lbl = new TextBlock { Text = "프리셋 이름을 입력하세요:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) }; var tb = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
            var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; var ok = new Button { Content = "확인", Width = 60, Margin = new Thickness(0, 0, 5, 0) }; var cc = new Button { Content = "취소", Width = 60 };
            ok.Click += (s, ev) => w.DialogResult = true; cc.Click += (s, ev) => w.DialogResult = false;
            bp.Children.Add(ok); bp.Children.Add(cc); sp.Children.Add(lbl); sp.Children.Add(tb); sp.Children.Add(bp); w.Content = sp;
            if (w.ShowDialog() == true && !string.IsNullOrWhiteSpace(tb.Text)) { var p = CreatePresetFromUI(); p.Name = tb.Text; _presetManager.AddPreset(p); RefreshPresetList(); cboPreset.SelectedIndex = cboPreset.Items.Count - 1; MessageBox.Show($"'{p.Name}' 프리셋이 저장되었습니다.", "저장 완료"); }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e) { if (cboPreset.SelectedIndex <= 0) return; string n = cboPreset.SelectedItem.ToString(); if (MessageBox.Show($"'{n}' 프리셋을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { _presetManager.DeletePreset(cboPreset.SelectedIndex - 1); RefreshPresetList(); } }
        private void CboPreset_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_isInitialized || cboPreset.SelectedIndex <= 0) return; ApplyPresetToUI(_presetManager.GetPreset(cboPreset.SelectedIndex - 1)); }

        #endregion
    }
}

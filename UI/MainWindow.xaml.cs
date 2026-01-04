using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;
using GameDamageCalculator.Services;
using System.Windows.Input;

namespace GameDamageCalculator.UI
{
    public partial class MainWindow : Window
    {
        private readonly DamageCalculator _calculator;
        private bool _isInitialized = false;
        
        public MainWindow()
        {
            InitializeComponent();
            _calculator = new DamageCalculator();
            InitializeComboBoxes();
            _isInitialized = true;
            RecalculateStats();
        }

        #region 초기화

        private void InitializeComboBoxes()
        {
            // 캐릭터 목록
            cboCharacter.Items.Add("직접 입력하거나 골라주세요");
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
            cboTranscend.SelectedIndex = 6;

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

            // 서브옵션 초기화
            InitializeSubOptionComboBoxes();

            // 진형 목록
            cboFormation.Items.Add("없음");
            foreach (var formationName in StatTable.FormationDb.Formations.Keys)
            {
                cboFormation.Items.Add(formationName);
            }
            cboFormation.SelectedIndex = 0;

            InitializeMainOptionComboBoxes(); 

            // 보스 목록 초기화
            UpdateBossList();

            // 펫 목록
            cboPet.Items.Add("없음");
            foreach (var pet in PetDb.Pets)
            {
                cboPet.Items.Add(pet.Name);
            }
            cboPet.SelectedIndex = 0;
        }

        private void InitializeMainOptionComboBoxes()
        {
            // 무기 메인옵션
            var weaponOptions = new[] {"없음", "공격력%", "치명타확률%", "치명타피해%", "약점공격확률%", "생명력%", "방어력%", "효과적중%" };
            foreach (var opt in weaponOptions)
            {
                cboWeapon1Main.Items.Add(opt);
                cboWeapon2Main.Items.Add(opt);
            }
            cboWeapon1Main.SelectedIndex = 0;
            cboWeapon2Main.SelectedIndex = 0;

            // 방어구 메인옵션
            var armorOptions = new[] { "공격력%", "생명력%", "방어력%", "효과저항%", "받는피해감소%", "막기확률%" };
            foreach (var opt in armorOptions)
            {
                cboArmor1Main.Items.Add(opt);
                cboArmor2Main.Items.Add(opt);
            }
            cboArmor1Main.SelectedIndex = 0;
            cboArmor2Main.SelectedIndex = 0;
        }

        private BaseStatSet GetMainOptionStats()
        {
            BaseStatSet stats = new BaseStatSet();

            // 무기1
            if (cboWeapon1Main.SelectedItem != null)
            {
                double value = ParseDouble(txtWeapon1MainValue.Text) / 100.0;
                ApplyMainOption(stats, cboWeapon1Main.SelectedItem.ToString(), value);
            }
            // 무기2
            if (cboWeapon2Main.SelectedItem != null)
            {
                double value = ParseDouble(txtWeapon2MainValue.Text) / 100.0;
                ApplyMainOption(stats, cboWeapon2Main.SelectedItem.ToString(), value);
            }
            // 방어구1
            if (cboArmor1Main.SelectedItem != null)
            {
                double value = ParseDouble(txtArmor1MainValue.Text) / 100.0;
                ApplyMainOption(stats, cboArmor1Main.SelectedItem.ToString(), value);
            }
            // 방어구2
            if (cboArmor2Main.SelectedItem != null)
            {
                double value = ParseDouble(txtArmor2MainValue.Text) / 100.0;
                ApplyMainOption(stats, cboArmor2Main.SelectedItem.ToString(), value);
            }

            return stats;
        }

        private void ApplyMainOption(BaseStatSet stats, string option, double value)
        {
            switch (option)
            {
                case "공격력%": stats.Atk_Rate += value; break;
                case "방어력%": stats.Def_Rate += value; break;
                case "생명력%": stats.Hp_Rate += value; break;
                case "치명타확률%": stats.Cri += value * 100; break;
                case "치명타피해%": stats.Cri_Dmg += value * 100; break;
                case "약점공격확률%": stats.Wek += value * 100; break;
                case "효과적중%": stats.Eff_Hit += value * 100; break;
                case "효과저항%": stats.Eff_Res += value * 100; break;
                case "받는피해감소%": stats.Dmg_Rdc += value * 100; break;
                case "막기확률%": stats.Blk += value * 100; break;
            }
        }

        private void MainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        private void MainOption_ValueChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateStats();
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

        #endregion

        #region 이벤트 핸들러

        private void BossType_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateBossList();
        }

        private void CboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (cboCharacter.SelectedIndex <= 0) 
            {
                cboSkill.Items.Clear();
                cboSkill.Items.Add("직접 입력하거나 골라주세요");
                cboSkill.SelectedIndex = 0;
                RecalculateStats();
                return;
            }

            string charName = cboCharacter.SelectedItem.ToString();
            var character = CharacterDb.GetByName(charName);
            
            if (character != null)
            {
                cboSkill.Items.Clear();
                foreach (var skill in character.Skills)
                {
                    cboSkill.Items.Add(skill.Name);
                }
                if (cboSkill.Items.Count > 0)
                {
                    cboSkill.SelectedIndex = 0;
                }
                
                RecalculateStats();
            }
        }

        private void CboSkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
        }

        private void CboTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            {
                gridEquipSet2.Visibility = Visibility.Visible;
            }
            else
            {
                gridEquipSet2.Visibility = Visibility.Collapsed;
                cboEquipSet2.SelectedIndex = 0;
            }
            
            RecalculateStats();
        }

        private void CboAccessory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void SubOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void SubOption_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void Potential_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void CboPet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void PetOption_Changed(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
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

        private void CboBoss_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (cboBoss.SelectedIndex <= 0) return;

            string selected = cboBoss.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            Boss boss = null;

            if (rbSiege.IsChecked == true)
            {
                boss = BossDb.SiegeBosses.FirstOrDefault(b => selected.Contains(b.Name));
            }
            else if (rbRaid.IsChecked == true)
            {
                boss = BossDb.RaidBosses.FirstOrDefault(b => selected.Contains(b.Name) && selected.Contains($"{b.Difficulty}단계"));
            }

            if (boss != null)
            {
                txtBossDef.Text = boss.Stats.Def.ToString("N0");
                txtBossDefInc.Text = $"{boss.DefenseIncrease * 100:F0}";
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double totalAtk = CalculateTotalAtk();
                txtResult.Text = $"총 공격력: {totalAtk:N0}";
            }
            catch (Exception ex)
            {
                txtResult.Text = $"오류: {ex.Message}";
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // 캐릭터
            cboCharacter.SelectedIndex = 0;
            cboTranscend.SelectedIndex = 6;
            chkSkillEnhanced.IsChecked = false;

            // 잠재능력
            cboPotentialAtk.SelectedIndex = 0;
            cboPotentialDef.SelectedIndex = 0;
            cboPotentialHp.SelectedIndex = 0;

            // 세트
            cboEquipSet1.SelectedIndex = 0;
            cboEquipSet2.SelectedIndex = 0;
            cboSetCount1.SelectedIndex = 1;

            // 장신구
            cboAccessory.SelectedIndex = 0;

            // 서브옵션
            cboSub1Type.SelectedIndex = 0; cboSub2Type.SelectedIndex = 0; cboSub3Type.SelectedIndex = 0;
            cboSub4Type.SelectedIndex = 0; cboSub5Type.SelectedIndex = 0; cboSub6Type.SelectedIndex = 0;
            cboSub7Type.SelectedIndex = 0; cboSub8Type.SelectedIndex = 0; cboSub9Type.SelectedIndex = 0;

            txtSub1Tier.Text = "0"; txtSub2Tier.Text = "0"; txtSub3Tier.Text = "0";
            txtSub4Tier.Text = "0"; txtSub5Tier.Text = "0"; txtSub6Tier.Text = "0";
            txtSub7Tier.Text = "0"; txtSub8Tier.Text = "0"; txtSub9Tier.Text = "0";

            // 진형
            cboFormation.SelectedIndex = 0;
            rbBack.IsChecked = true;

            // 펫
            cboPet.SelectedIndex = 0;
            cboPetStar.SelectedIndex = 2;
            cboPetOpt1.SelectedIndex = 0;
            cboPetOpt2.SelectedIndex = 0;
            cboPetOpt3.SelectedIndex = 0;
            txtPetOpt1.Text = "0";
            txtPetOpt2.Text = "0";
            txtPetOpt3.Text = "0";

            // 보스
            cboBoss.SelectedIndex = 0;
            chkCritical.IsChecked = true;
            chkWeakpoint.IsChecked = false;
            chkBlock.IsChecked = false;

            txtResult.Text = "계산 버튼을 눌러\n결과를 확인하세요.";
        }
        
        private void Tier_MouseLeft(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val))
            {
                txtBox.Text = (val + 1).ToString();
                RecalculateStats();
            }
        }

        private void Tier_MouseRight(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = FindName(border.Tag.ToString()) as TextBox;
            if (txtBox != null && int.TryParse(txtBox.Text, out int val) && val > 0)
            {
                txtBox.Text = (val - 1).ToString();
                RecalculateStats();
            }
            e.Handled = true;  // 우클릭 메뉴 방지
        }

        private void SubOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

        #region 스탯 계산 (UI 표시용)

        /// <summary>
        /// 최종 공격력 = 기본공격력 × (1 + 총 공격력%) + 깡공 합계 × (1 + 버프%)
        /// </summary>
        private void RecalculateStats()
        {
            if (!_isInitialized) return;
        
            // ========== 기본공격력 (캐릭터 DB 기초 스탯) ==========
            double baseAtk = 0;
            double baseDef = 0;
            double baseHp = 0;
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
        
            // ========== 깡스탯 합계 (장비 + 잠재능력 + 서브옵션 + 펫) ==========
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;
        
            var potentialStats = GetPotentialStats();
            BaseStatSet subStats = GetSubOptionStats();
        
            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();
        
            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlatAtk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlatDef;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlatHp;
        
            // ========== 합연산% ==========
            // 초월%
            double transcendAtkRate = 0;
            double transcendDefRate = 0;
            double transcendHpRate = 0;
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    int transcendLevel = cboTranscend.SelectedIndex;
                    var transcendStats = character.GetTranscendStats(transcendLevel);
                    transcendAtkRate = transcendStats.Atk_Rate;
                    transcendDefRate = transcendStats.Def_Rate;
                    transcendHpRate = transcendStats.Hp_Rate;
                }
            }
        
            // 진형%
            double formationAtkRate = GetFormationAtkRate();
            double formationDefRate = GetFormationDefRate();
        
            // 세트%
            BaseStatSet setBonus = new BaseStatSet();
            if (cboEquipSet1.SelectedIndex > 0)
            {
                string setName = cboEquipSet1.SelectedItem.ToString();
                int setCount = cboSetCount1.SelectedIndex == 0 ? 2 : 4;
                setBonus.Add(GetSetBonus(setName, setCount));
            }
            if (cboSetCount1.SelectedIndex == 0 && cboEquipSet2.SelectedIndex > 0)
            {
                string setName = cboEquipSet2.SelectedItem.ToString();
                setBonus.Add(GetSetBonus(setName, 2));
            }
        
            // 장신구%
            BaseStatSet accessoryStats = GetAccessoryStats();

            // 메인옵션% ← 여기 추가
            BaseStatSet mainOptionStats = GetMainOptionStats();
        
            // 펫옵션%
            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();
        
            // 합연산% 합계
            double totalAtkRate = transcendAtkRate + formationAtkRate 
                    + setBonus.Atk_Rate + subStats.Atk_Rate 
                    + accessoryStats.Atk_Rate + petOptionAtkRate
                    + mainOptionStats.Atk_Rate;  // ← 추가

            double totalDefRate = transcendDefRate + formationDefRate 
                    + setBonus.Def_Rate + subStats.Def_Rate 
                    + accessoryStats.Def_Rate + petOptionDefRate
                    + mainOptionStats.Def_Rate;  // ← 추가

            double totalHpRate = transcendHpRate 
                   + setBonus.Hp_Rate + subStats.Hp_Rate 
                   + accessoryStats.Hp_Rate + petOptionHpRate
                   + mainOptionStats.Hp_Rate;  // ← 추가
        
            // ========== 버프% (펫스킬% + 패시브% + 액티브%) ==========
            double buffAtkRate = GetBuffAtkRate();
            double buffDefRate = GetBuffDefRate();
            double buffHpRate = GetBuffHpRate();
        
            // ========== 최종 스탯 계산 ==========
            // 공식: (기본공격력 × (1 + 총공격력%) + 깡공합계) × (1 + 버프%)
            double totalAtk = (baseAtk * (1 + totalAtkRate) + flatAtk) * (1 + buffAtkRate);
            double totalDef = (baseDef * (1 + totalDefRate) + flatDef) * (1 + buffDefRate);
            double totalHp = (baseHp * (1 + totalHpRate) + flatHp) * (1 + buffHpRate);
        
            // ========== UI에 표시 ==========
            BaseStatSet displayStats = new BaseStatSet
            {
                Atk = totalAtk,
                Def = totalDef,
                Hp = totalHp,
                Cri = characterStats.Cri + setBonus.Cri + subStats.Cri * 100,
                Cri_Dmg = characterStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg * 100,
                Wek = characterStats.Wek + setBonus.Wek + subStats.Wek,
                Wek_Dmg = characterStats.Wek_Dmg + setBonus.Wek_Dmg,
                Dmg_Dealt = characterStats.Dmg_Dealt + setBonus.Dmg_Dealt,
                Dmg_Dealt_Bos = characterStats.Dmg_Dealt_Bos + setBonus.Dmg_Dealt_Bos,
                Arm_Pen = characterStats.Arm_Pen + setBonus.Arm_Pen,
                Blk = characterStats.Blk + setBonus.Blk + subStats.Blk,
                Eff_Hit = characterStats.Eff_Hit + setBonus.Eff_Hit + subStats.Eff_Hit,
                Eff_Res = characterStats.Eff_Res + setBonus.Eff_Res + subStats.Eff_Res,
                Eff_Acc = characterStats.Eff_Acc + setBonus.Eff_Acc,
                Dmg_Rdc = characterStats.Dmg_Rdc + setBonus.Dmg_Rdc
            };
        
            UpdateStatDisplay(displayStats);
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
            txtStatAtk.Text = stats.Atk.ToString("N0");
            txtStatDef.Text = stats.Def.ToString("N0");
            txtStatHp.Text = stats.Hp.ToString("N0");
            txtStatCri.Text = $"{stats.Cri}%";
            txtStatCriDmg.Text = $"{stats.Cri_Dmg}%";
            txtStatWek.Text = $"{stats.Wek * 100:F0}%";
            txtStatWekDmg.Text = $"{stats.Wek_Dmg}%";
            txtStatDmgDealt.Text = $"{stats.Dmg_Dealt * 100:F0}%";
            txtStatBossDmg.Text = $"{stats.Dmg_Dealt_Bos * 100:F0}%";
            txtStatArmPen.Text = $"{stats.Arm_Pen * 100:F0}%";
            txtStatBlk.Text = $"{stats.Blk * 100:F0}%";
            txtStatEffHit.Text = $"{stats.Eff_Hit * 100:F0}%";
            txtStatEffRes.Text = $"{stats.Eff_Res * 100:F0}%";
            txtStatEffAcc.Text = $"{stats.Eff_Acc * 100:F0}%";
            txtStatDmgRdc.Text = $"{stats.Dmg_Rdc * 100:F0}%";
        }

        #endregion

        #region 총 공격력 계산

        private double CalculateTotalAtk()
        {
            RecalculateStats();
            return ParseStatValue(txtStatAtk.Text);
        }

        private double GetBaseAtk()
        {
            if (cboCharacter.SelectedIndex <= 0) return 0;
            
            var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
            if (character != null)
            {
                return character.GetBaseStats().Atk;
            }
            return 0;
        }

        #endregion

        #region 펫 관련

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
            {
                return ParseDouble(txt.Text) / 100;
            }
            return 0;
        }

        private double GetPetOptionAtkRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "공격력%");
            rate += GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "공격력%");
            rate += GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "공격력%");
            return rate;
        }

        private double GetPetOptionDefRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "방어력%");
            rate += GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "방어력%");
            rate += GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "방어력%");
            return rate;
        }

        private double GetPetOptionHpRate()
        {
            double rate = 0;
            rate += GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "생명력%");
            rate += GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "생명력%");
            rate += GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "생명력%");
            return rate;
        }

        #endregion

        #region 진형 관련

        private double GetFormationAtkRate()
        {
            if (cboFormation.SelectedIndex <= 0) return 0;
            
            if (rbFront.IsChecked == true) return 0;
            
            string formationName = cboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
            {
                return bonus.Atk_Rate_Back;
            }
            return 0;
        }

        private double GetFormationDefRate()
        {
            if (cboFormation.SelectedIndex <= 0) return 0;
            
            if (rbBack.IsChecked == true) return 0;
            
            string formationName = cboFormation.SelectedItem.ToString();
            if (StatTable.FormationDb.Formations.TryGetValue(formationName, out var bonus))
            {
                return bonus.Def_Rate_Front;
            }
            return 0;
        }

        #endregion

        #region 버프 관련

        private double GetBuffAtkRate()
        {
            double buffRate = 0;
            
            // 펫 스킬
            buffRate += GetPetSkillAtkRate();
            
            // TODO: 패시브 버프 (델론즈, 에이린 등)
            // TODO: 액티브 버프 (바스킷, 리나 등)
            
            return buffRate;
        }

        private double GetBuffDefRate()
        {
            double rate = 0;
            // TODO: 패시브/액티브 방어 버프
            return rate;
        }

        private double GetBuffHpRate()
        {
            double rate = 0;
            // TODO: 패시브/액티브 체력 버프
            return rate;
        }

        #endregion

        #region 보스 관련

        private void UpdateBossList()
        {
            cboBoss.Items.Clear();
            cboBoss.Items.Add("직접 입력");

            if (rbSiege.IsChecked == true)
            {
                foreach (var boss in BossDb.SiegeBosses)
                {
                    cboBoss.Items.Add($"{boss.Name} ({boss.DayOfWeek})");
                }
            }
            else if (rbRaid.IsChecked == true)
            {
                foreach (var boss in BossDb.RaidBosses)
                {
                    cboBoss.Items.Add($"{boss.Name} {boss.Difficulty}단계");
                }
            }
            else
            {
                cboBoss.Items.Add("장원 보스");
            }

            cboBoss.SelectedIndex = 0;
        }

        #endregion

        #region DB 헬퍼

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

        private BaseStatSet GetSubOptionStats()
        {
            BaseStatSet result = new BaseStatSet();

            var subOptions = new[]
            {
                (cboSub1Type, txtSub1Tier),
                (cboSub2Type, txtSub2Tier),
                (cboSub3Type, txtSub3Tier),
                (cboSub4Type, txtSub4Tier),
                (cboSub5Type, txtSub5Tier),
                (cboSub6Type, txtSub6Tier),
                (cboSub7Type, txtSub7Tier),
                (cboSub8Type, txtSub8Tier),
                (cboSub9Type, txtSub9Tier)
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
            if (cboAccessory.SelectedIndex <= 0) return new BaseStatSet();
            
            int grade = cboAccessory.SelectedIndex + 3;  // 1→4성, 2→5성, 3→6성
            
            if (AccessoryDb.Stats.TryGetValue(grade, out var stats))
            {
                return stats;
            }
            return new BaseStatSet();
        }

        private BaseStatSet GetPotentialStats()
        {
            BaseStatSet stats = new BaseStatSet();
            
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

        #endregion

        #region 유틸리티

        private double ParseStatValue(string text)
        {
            string cleaned = text.Replace("%", "").Replace(",", "").Trim();
            if (double.TryParse(cleaned, out double result))
                return result;
            return 0;
        }

        private double ParseDouble(string text)
        {
            if (double.TryParse(text, out double result))
                return result;
            return 0;
        }

        #endregion
    }
}
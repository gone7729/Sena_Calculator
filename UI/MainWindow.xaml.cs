using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;
using GameDamageCalculator.Services;

namespace GameDamageCalculator.UI
{
    public partial class MainWindow : Window
    {
        private readonly DamageCalculator _calculator;
        private bool _isInitialized = false;
        private ComboBox[] _subOptionCombos;
        private ComboBox[] _subLevelCombos;
        
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

        private void InitializeSubOptionComboBoxes()
        {
            _subOptionCombos = new ComboBox[] 
            { 
                cboSub1, cboSub2, cboSub3, cboSub4,
                cboSub5, cboSub6, cboSub7, cboSub8,
                cboSub9, cboSub10, cboSub11, cboSub12,
                cboSub13, cboSub14, cboSub15, cboSub16
            };
            
            _subLevelCombos = new ComboBox[] 
            { 
                cboSubLv1, cboSubLv2, cboSubLv3, cboSubLv4,
                cboSubLv5, cboSubLv6, cboSubLv7, cboSubLv8,
                cboSubLv9, cboSubLv10, cboSubLv11, cboSubLv12,
                cboSubLv13, cboSubLv14, cboSubLv15, cboSubLv16
            };
            
            // 옵션 종류
            foreach (var cbo in _subOptionCombos)
            {
                cbo.Items.Add("없음");
                foreach (var optionName in EquipmentDb.SubStatDb.SubStatTiers.Keys)
                {
                    cbo.Items.Add(optionName);
                }
                cbo.SelectedIndex = 0;
            }
            
            // 레벨 (1~5)
            foreach (var cbo in _subLevelCombos)
            {
                for (int i = 1; i <= 5; i++)
                {
                    cbo.Items.Add(i.ToString());
                }
                cbo.SelectedIndex = 0;
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
            foreach (var cbo in _subOptionCombos) cbo.SelectedIndex = 0;
            foreach (var cbo in _subLevelCombos) cbo.SelectedIndex = 0;

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

        #endregion

        #region 스탯 계산 (UI 표시용)

        /// <summary>
        /// 최종공격력 = (스탯공격력 + 펫깡공 + 기초공 × (1 + 합연산%)) × (1 + 버프%)
        /// 스탯공격력 = 캐릭터 DB Atk
        /// 기초공 = 장비깡공 + 잠재능력깡공 + 서브옵션깡공
        /// 합연산% = 초월% + 진형% + 세트% + 서브옵션% + 장신구% + 펫옵션%
        /// 버프% = 펫스킬% + 패시브% + 액티브%
        /// </summary>
        private void RecalculateStats()
        {
            if (!_isInitialized) return;

            // ========== 스탯공격력 (캐릭터 DB 기초 스탯) ==========
            double statAtk = 0;
            double statDef = 0;
            double statHp = 0;
            BaseStatSet characterStats = new BaseStatSet();

            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    characterStats = character.GetBaseStats();
                    statAtk = characterStats.Atk;
                    statDef = characterStats.Def;
                    statHp = characterStats.Hp;
                }
            }

            // ========== 펫깡공 ==========
            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();

            // ========== 기초공 (장비깡공 + 잠재능력깡공 + 서브옵션깡공) ==========
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            var potentialStats = GetPotentialStats();
            BaseStatSet subStats = GetSubOptionStats();

            double baseAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk;
            double baseDef = equipFlatDef + potentialStats.Def + subStats.Def;
            double baseHp = equipFlatHp + potentialStats.Hp + subStats.Hp;

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

            // 세트% + 서브옵션%
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

            double equipAtkRate = setBonus.Atk_Rate + subStats.Atk_Rate;
            double equipDefRate = setBonus.Def_Rate + subStats.Def_Rate;
            double equipHpRate = setBonus.Hp_Rate + subStats.Hp_Rate;

            // 장신구%
            BaseStatSet accessoryStats = GetAccessoryStats();
            double accessoryAtkRate = accessoryStats.Atk_Rate;
            double accessoryDefRate = accessoryStats.Def_Rate;
            double accessoryHpRate = accessoryStats.Hp_Rate;

            // 펫옵션%
            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();

            // 합연산% 합계
            double totalAtkRate = transcendAtkRate + formationAtkRate + equipAtkRate + accessoryAtkRate + petOptionAtkRate;
            double totalDefRate = transcendDefRate + formationDefRate + equipDefRate + accessoryDefRate + petOptionDefRate;
            double totalHpRate = transcendHpRate + equipHpRate + accessoryHpRate + petOptionHpRate;

            // ========== 버프% (펫스킬% + 패시브% + 액티브%) ==========
            double buffAtkRate = GetBuffAtkRate();
            double buffDefRate = GetBuffDefRate();
            double buffHpRate = GetBuffHpRate();

            // ========== 최종 스탯 계산 ==========
            double totalAtk = (statAtk + petFlatAtk + baseAtk * (1 + totalAtkRate)) * (1 + buffAtkRate);
            double totalDef = (statDef + petFlatDef + baseDef * (1 + totalDefRate)) * (1 + buffDefRate);
            double totalHp = (statHp + petFlatHp + baseHp * (1 + totalHpRate)) * (1 + buffHpRate);

            // UI에 표시
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
            BaseStatSet total = new BaseStatSet();
            
            for (int i = 0; i < 16; i++)
            {
                if (_subOptionCombos[i].SelectedIndex <= 0) continue;
                
                string optionName = _subOptionCombos[i].SelectedItem.ToString();
                int level = _subLevelCombos[i].SelectedIndex;
                
                if (EquipmentDb.SubStatDb.SubStatTiers.TryGetValue(optionName, out var statArray))
                {
                    total.Add(statArray[level]);
                }
            }
            
            return total;
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
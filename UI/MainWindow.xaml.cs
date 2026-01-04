using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;
using GameDamageCalculator.Services;
using System.Windows.Input;
using System.Windows.Media;

namespace GameDamageCalculator.UI
{
    public partial class MainWindow : Window
    {
        private readonly DamageCalculator _calculator;
        private bool _isInitialized = false;

        private DebuffSet _currentDebuffs = new DebuffSet();
        
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

            InitializeAccessoryComboBoxes();

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
            cboWeapon1Main.Items.Add("없음");
            cboWeapon2Main.Items.Add("없음");
            foreach (var opt in EquipmentDb.MainStatDb.AvailableOptions["무기"])
            {
                cboWeapon1Main.Items.Add(opt);
                cboWeapon2Main.Items.Add(opt);
            }
            cboWeapon1Main.SelectedIndex = 0;
            cboWeapon2Main.SelectedIndex = 0;

            // 방어구 메인옵션
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

        private void MainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateMainOptionDisplay();
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

        private void InitializeAccessoryComboBoxes()
        {
            // 성급
            cboAccessoryGrade.Items.Add("없음");
            cboAccessoryGrade.Items.Add("4성");
            cboAccessoryGrade.Items.Add("5성");
            cboAccessoryGrade.Items.Add("6성");
            cboAccessoryGrade.SelectedIndex = 0;

            // 메인옵션
            var mainOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                      "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                      "1-3인기%", "4-5인기%" };
            foreach (var opt in mainOptions)
                cboAccessoryMain.Items.Add(opt);
            cboAccessoryMain.SelectedIndex = 0;

            // 부옵션 (6성만)
            var subOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                     "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                     "1-3인기%", "4-5인기%" };
            foreach (var opt in subOptions)
                cboAccessorySub.Items.Add(opt);
            cboAccessorySub.SelectedIndex = 0;
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

        private void CboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
        
            // 6성이면 부옵션 표시, 아니면 숨김
            if (cboAccessoryGrade.SelectedIndex == 3)  // 6성
            {
                panelAccessorySub.Visibility = Visibility.Visible;
            }
            else
            {
                panelAccessorySub.Visibility = Visibility.Collapsed;
                cboAccessorySub.SelectedIndex = 0;  // 부옵션 초기화
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
        
        private void UpdateAccessoryDisplay()
        {
            if (cboAccessoryGrade.SelectedIndex <= 0)
            {
                txtAccessoryMainValue.Text = "";
                txtAccessorySubValue.Text = "";
                return;
            }
        
            int grade = cboAccessoryGrade.SelectedIndex + 3;  // 1→4성, 2→5성, 3→6성
        
            // 메인옵션 값 표시
            txtAccessoryMainValue.Text = GetAccessoryOptionValue(grade, cboAccessoryMain, AccessoryDb.MainOptions);
        
            // 부옵션 값 표시 (6성만)
            if (grade == 6)
            {
                txtAccessorySubValue.Text = GetAccessoryOptionValue(grade, cboAccessorySub, AccessoryDb.SubOptions);
            }
            else
            {
                txtAccessorySubValue.Text = "";
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
            if (cboBoss.SelectedIndex <= 0)
            {
                txtBossDef.Text = "0";
                txtBossDefInc.Text = "0";
                txtBossDmgRdc.Text = "0";
                txtBoss1TargetRdc.Text = "0";
                txtBoss3TargetRdc.Text = "0";
                txtBoss5TargetRdc.Text = "0";
                return;
            }

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
                txtBossDefInc.Text = (boss.DefenseIncrease * 100).ToString("F0");
                txtBossDmgRdc.Text = "0";

                // 인기별 피해감소 (소수 → 백분율)
                txtBoss1TargetRdc.Text = (boss.SingleTargetReduction * 100).ToString("F0");
                txtBoss3TargetRdc.Text = (boss.TripleTargetReduction * 100).ToString("F0");
                txtBoss5TargetRdc.Text = (boss.MultiTargetReduction * 100).ToString("F0");
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 캐릭터 확인
                if (cboCharacter.SelectedIndex <= 0)
                {
                    txtResult.Text = "캐릭터를 선택해주세요.";
                    return;
                }

                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character == null)
                {
                    txtResult.Text = "캐릭터 정보를 찾을 수 없습니다.";
                    return;
                }

                // 스킬 확인
                Skill selectedSkill = null;
                if (cboSkill.SelectedIndex >= 0 && cboSkill.Items.Count > 0)
                {
                    string skillName = cboSkill.SelectedItem?.ToString();
                    selectedSkill = character.Skills.FirstOrDefault(s => s.Name == skillName);
                }

                if (selectedSkill == null)
                {
                    txtResult.Text = "스킬을 선택해주세요.";
                    return;
                }

                // DamageInput 생성
                var input = new DamageCalculator.DamageInput
                {
                    // 캐릭터/스킬
                    Character = character,
                    Skill = selectedSkill,
                    IsSkillEnhanced = chkSkillEnhanced.IsChecked == true,
                    TranscendLevel = cboTranscend.SelectedIndex,

                    // UI에서 계산된 최종 스탯
                    FinalAtk = ParseStatValue(txtStatAtk.Text),
                    CritDamage = ParseStatValue(txtStatCriDmg.Text),
                    DmgDealt = ParseStatValue(txtStatDmgDealt.Text),
                    DmgDealtBoss = ParseStatValue(txtStatBossDmg.Text),
                    ArmorPen = ParseStatValue(txtStatArmPen.Text),
                    WeakpointDmg = ParseStatValue(txtStatWekDmg.Text),

                    // 디버프
                    DefReduction = _currentDebuffs.Def_Reduction,
                    DmgTakenIncrease = _currentDebuffs.Dmg_Taken_Increase,
                    Vulnerability = _currentDebuffs.Vulnerability,

                    // 보스 정보
                    BossDef = ParseDouble(txtBossDef.Text),
                    BossDefIncrease = ParseDouble(txtBossDefInc.Text),
                    BossDmgReduction = ParseDouble(txtBossDmgRdc.Text),
                    BossTargetReduction = GetSelectedTargetReduction(),

                    // 전투 옵션
                    IsCritical = chkCritical.IsChecked == true,
                    IsWeakpoint = chkWeakpoint.IsChecked == true,
                    IsBlocked = chkBlock.IsChecked == true,

                    // 조건 (TODO: 체크박스 추가 시)
                    IsSkillConditionMet = false
                };

                // 계산 및 출력
                var result = _calculator.Calculate(input);
                txtResult.Text = result.Details;
            }
            catch (Exception ex)
            {
                txtResult.Text = $"오류: {ex.Message}";
            }
        }

        private double GetSelectedTargetReduction()
        {
            // 스킬 타겟 수에 따라 인기감소 선택
            // TODO: 스킬별 타겟 수 확인 후 적용
            // 현재는 UI에서 직접 선택하거나 0 반환
            return 0;
        }

        private Boss GetSelectedBoss()
        {
            // 직접 입력이면 임시 보스 생성
            if (cboBoss.SelectedIndex <= 0)
            {
                return new Boss
                {
                    Name = "직접 입력",
                    BossType = BossType.Siege,
                    Stats = new BaseStatSet { Def = ParseDouble(txtBossDef.Text) },
                    DefenseIncrease = ParseDouble(txtBossDefInc.Text) / 100
                };
            }

            string selected = cboBoss.SelectedItem?.ToString();

            if (rbSiege.IsChecked == true)
            {
                return BossDb.SiegeBosses.FirstOrDefault(b => selected.Contains(b.Name));
            }
            else if (rbRaid.IsChecked == true)
            {
                return BossDb.RaidBosses.FirstOrDefault(b => selected.Contains(b.Name) && selected.Contains($"{b.Difficulty}단계"));
            }

            return new Boss { Name = "Unknown", Stats = new BaseStatSet { Def = 0 } };
        }

        private BaseStatSet GetTotalEquipmentStats()
        {
            BaseStatSet total = new BaseStatSet();

            // 장비 기본 스탯
            total.Atk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            total.Def = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            total.Hp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            // 잠재능력
            total.Add(GetPotentialStats());

            // 서브옵션
            total.Add(GetSubOptionStats());

            // 메인옵션
            total.Add(GetMainOptionStats());

            // 장신구
            total.Add(GetAccessoryStats());

            // 펫 옵션 (% 스탯)
            total.Atk_Rate += GetPetOptionAtkRate();
            total.Def_Rate += GetPetOptionDefRate();
            total.Hp_Rate += GetPetOptionHpRate();

            return total;
        }

        private BaseStatSet GetTotalSetBonus()
        {
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

            return setBonus;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // 캐릭터
            cboCharacter.SelectedIndex = 0;
            cboTranscend.SelectedIndex = 0;
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
            cboAccessoryGrade.SelectedIndex = 0;

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

            // 보스 스탯 초기화
            txtBossDef.Text = "0";
            txtBossDefInc.Text = "0";
            txtBossDmgRdc.Text = "0";
            txtBossDefRed.Text = "0";
            txtBossDmgTaken.Text = "0";
            txtBossVulnerable.Text = "0";
            // 보스 인기감소 초기화
            txtBoss1TargetRdc.Text = "0";
            txtBoss3TargetRdc.Text = "0";
            txtBoss5TargetRdc.Text = "0";

            // ===== 버프/디버프 초기화 =====
            // 디버프 패시브
            chkDebuffPassiveTaka.IsChecked = false;
            ResetBuffOptionButton(btnDebuffPassiveTaka, "타카");
        
            // TODO: 다른 캐릭터 추가 시
            // chkBuffPassiveXXX.IsChecked = false;
            // ResetBuffOptionButton(btnBuffPassiveXXX, "캐릭터명");
            

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
            System.Diagnostics.Debug.WriteLine($"선택된 옵션: '{option}'");

            if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(option, out var stats))
            {
                System.Diagnostics.Debug.WriteLine($"  DB에서 찾음! Atk_Rate={stats.Atk_Rate}, Cri={stats.Cri}, Cri_Dmg={stats.Cri_Dmg}, Atk={stats.Atk}");
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
            }else
            {
                System.Diagnostics.Debug.WriteLine($"  DB에서 찾지 못함!");
            }
            return "";
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

            // ========== 각종 스탯 소스 가져오기 ==========
            var potentialStats = GetPotentialStats();
            BaseStatSet subStats = GetSubOptionStats();
            BaseStatSet mainOptionStats = GetMainOptionStats();
            BaseStatSet accessoryStats = GetAccessoryStats();

            // 버프/디버프
            BaseStatSet passiveBuffs = GetAllPassiveBuffs();
            BaseStatSet activeBuffs = GetAllActiveBuffs();
            DebuffSet passiveDebuffs = GetAllPassiveDebuffs();
            DebuffSet activeDebuffs = GetAllActiveDebuffs();

            BaseStatSet totalBuffs = new BaseStatSet();
            totalBuffs.Add(passiveBuffs);
            totalBuffs.Add(activeBuffs);

            _currentDebuffs = new DebuffSet();
            _currentDebuffs.Add(passiveDebuffs);
            _currentDebuffs.Add(activeDebuffs);

            // ========== 깡스탯 합계 (장비 + 잠재능력 + 서브옵션 + 펫 + 메인옵션) ==========
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();

            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlatAtk + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlatDef + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlatHp + mainOptionStats.Hp;

            // ========== 합연산% (정수로 합산) ==========
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

            // 펫옵션%
            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();

            // 합연산% 합계 (정수)
            double totalAtkRate = transcendAtkRate + formationAtkRate 
                    + setBonus.Atk_Rate + subStats.Atk_Rate 
                    + accessoryStats.Atk_Rate + petOptionAtkRate
                    + mainOptionStats.Atk_Rate;

            double totalDefRate = transcendDefRate + formationDefRate 
                    + setBonus.Def_Rate + subStats.Def_Rate 
                    + accessoryStats.Def_Rate + petOptionDefRate
                    + mainOptionStats.Def_Rate;

            double totalHpRate = transcendHpRate 
                   + setBonus.Hp_Rate + subStats.Hp_Rate 
                   + accessoryStats.Hp_Rate + petOptionHpRate
                   + mainOptionStats.Hp_Rate;

            // ========== 버프% (펫스킬% + 패시브% + 액티브%) ==========
            double buffAtkRate = GetPetSkillAtkRate() + totalBuffs.Atk_Rate;
            double buffDefRate = totalBuffs.Def_Rate;
            double buffHpRate = totalBuffs.Hp_Rate;

            // ========== 최종 스탯 계산 (정수 → /100.0) ==========
            double totalAtk = (baseAtk * (1 + totalAtkRate / 100.0) + flatAtk) * (1 + buffAtkRate / 100.0);
            double totalDef = (baseDef * (1 + totalDefRate / 100.0) + flatDef) * (1 + buffDefRate / 100.0);
            double totalHp = (baseHp * (1 + totalHpRate / 100.0) + flatHp) * (1 + buffHpRate / 100.0);

            // ========== UI에 표시 ==========
            BaseStatSet displayStats = new BaseStatSet
            {
                Atk = totalAtk,
                Def = totalDef,
                Hp = totalHp,
                Cri = characterStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri,
                Cri_Dmg = characterStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg,
                Wek = characterStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek,
                Wek_Dmg = characterStats.Wek_Dmg + setBonus.Wek_Dmg,
                Dmg_Dealt = characterStats.Dmg_Dealt + setBonus.Dmg_Dealt + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt,
                Dmg_Dealt_Bos = characterStats.Dmg_Dealt_Bos + setBonus.Dmg_Dealt_Bos + accessoryStats.Dmg_Dealt_Bos,
                Arm_Pen = characterStats.Arm_Pen + setBonus.Arm_Pen,
                Blk = characterStats.Blk + setBonus.Blk + subStats.Blk + mainOptionStats.Blk + accessoryStats.Blk,
                Eff_Hit = characterStats.Eff_Hit + setBonus.Eff_Hit + subStats.Eff_Hit + mainOptionStats.Eff_Hit + accessoryStats.Eff_Hit,
                Eff_Res = characterStats.Eff_Res + setBonus.Eff_Res + subStats.Eff_Res + mainOptionStats.Eff_Res + accessoryStats.Eff_Res,
                Eff_Acc = characterStats.Eff_Acc + setBonus.Eff_Acc,
                Dmg_Rdc = characterStats.Dmg_Rdc + setBonus.Dmg_Rdc + mainOptionStats.Dmg_Rdc,
                Dmg_Dealt_1to3 = characterStats.Dmg_Dealt_1to3 + setBonus.Dmg_Dealt_1to3 + accessoryStats.Dmg_Dealt_1to3,
                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5
            };

            UpdateStatDisplay(displayStats);
            UpdateBossDebuffDisplay();

            // 디버그 출력
            System.Diagnostics.Debug.WriteLine("========== 디버그 ==========");
            System.Diagnostics.Debug.WriteLine($"기본공격력: {baseAtk}");
            System.Diagnostics.Debug.WriteLine($"깡공합계: {flatAtk}");
            System.Diagnostics.Debug.WriteLine($"총 공격력%: {totalAtkRate}");
            System.Diagnostics.Debug.WriteLine($"버프 공격력%: {buffAtkRate}");
            System.Diagnostics.Debug.WriteLine($"패시브 버프 피증%: {passiveBuffs.Dmg_Dealt}");
            System.Diagnostics.Debug.WriteLine($"액티브 버프 피증%: {activeBuffs.Dmg_Dealt}");
            System.Diagnostics.Debug.WriteLine($"총 디버프 받피증%: {_currentDebuffs.Dmg_Taken_Increase}");
            System.Diagnostics.Debug.WriteLine($"최종 공격력: {totalAtk}");
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
            txtStatAtk.Text = stats.Atk.ToString("N0");
            txtStatDef.Text = stats.Def.ToString("N0");
            txtStatHp.Text = stats.Hp.ToString("N0");
            // % 스탯 - 정수 그대로 표시
            txtStatCri.Text = $"{stats.Cri}%";
            txtStatCriDmg.Text = $"{stats.Cri_Dmg}%";
            txtStatWek.Text = $"{stats.Wek}%";
            txtStatWekDmg.Text = $"{stats.Wek_Dmg}%";
            txtStatDmgDealt.Text = $"{stats.Dmg_Dealt}%";
            txtStatBossDmg.Text = $"{stats.Dmg_Dealt_Bos}%";
            txtStatDmg1to3.Text = $"{stats.Dmg_Dealt_1to3}%";
            txtStatDmg4to5.Text = $"{stats.Dmg_Dealt_4to5}%";
            txtStatArmPen.Text = $"{stats.Arm_Pen}%";
            txtStatBlk.Text = $"{stats.Blk}%";
            txtStatEffHit.Text = $"{stats.Eff_Hit}%";
            txtStatEffRes.Text = $"{stats.Eff_Res}%";
            txtStatEffAcc.Text = $"{stats.Eff_Acc}%";
            txtStatDmgRdc.Text = $"{stats.Dmg_Rdc}%";
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

        private void UpdateBossDebuffDisplay()
        {
            txtBossDefRed.Text = _currentDebuffs.Def_Reduction.ToString("F0");
            txtBossDmgTaken.Text = _currentDebuffs.Dmg_Taken_Increase.ToString("F0");
            txtBossVulnerable.Text = _currentDebuffs.Vulnerability.ToString("F0");
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
            BaseStatSet stats = new BaseStatSet();

            if (cboAccessoryGrade.SelectedIndex <= 0) return stats;

            int grade = cboAccessoryGrade.SelectedIndex + 3;  // 1→4성, 2→5성, 3→6성

            // 성급 기본 보너스
            if (AccessoryDb.GradeBonus.TryGetValue(grade, out var gradeBonus))
                stats.Add(gradeBonus);

            // 메인옵션
            if (cboAccessoryMain.SelectedIndex > 0)
            {
                string mainOpt = cboAccessoryMain.SelectedItem.ToString();
                if (AccessoryDb.MainOptions.TryGetValue(grade, out var mainOptions))
                {
                    if (mainOptions.TryGetValue(mainOpt, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

            // 부옵션 (6성만)
            if (grade == 6 && cboAccessorySub.SelectedIndex > 0)
            {
                string subOpt = cboAccessorySub.SelectedItem.ToString();
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

        #region 버프/디버프 옵션 버튼 로직

        // 버튼 상태별 색상: 기본, 스강★, 초월◆, 풀★◆
        private static readonly Brush[] BuffBgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(58, 58, 58)),   // 0: 기본
            new SolidColorBrush(Color.FromRgb(180, 150, 50)), // 1: 스강
            new SolidColorBrush(Color.FromRgb(70, 130, 180)), // 2: 초월
            new SolidColorBrush(Color.FromRgb(138, 43, 226))  // 3: 풀
        };

        private static readonly Brush[] BuffFgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(204, 204, 204)), // 0: 기본
            Brushes.Black,   // 1: 스강
            Brushes.White,   // 2: 초월
            Brushes.White    // 3: 풀
        };

        private static readonly string[] BuffOptionSuffix = { "", " 스강", " 초월", " 풀" };

        private void BuffOption_Click(object sender, RoutedEventArgs e)
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

        private void ResetBuffOptionButton(Button btn, string baseName)
        {
            btn.Tag = 0;
            btn.Background = BuffBgColors[0];
            btn.Foreground = BuffFgColors[0];
            btn.Content = baseName;
        }

        #endregion

        #region 버프/디버프 합산 메서드

        private BaseStatSet GetAllPassiveBuffs()
        {
            BaseStatSet total = new BaseStatSet();
            // TODO: 버프 패시브 캐릭터 추가 시
            return total;
        }

        private BaseStatSet GetAllActiveBuffs()
        {
            BaseStatSet total = new BaseStatSet();
            // TODO: 버프 액티브 캐릭터 추가 시
            return total;
        }

        private DebuffSet GetAllPassiveDebuffs()
        {
            DebuffSet total = new DebuffSet();

            if (chkDebuffPassiveTaka.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffPassiveTaka);
                var taka = CharacterDb.GetByName("타카");
                var debuff = taka?.Passive?.GetDebuffModifier(isEnhanced, transcendLevel);
                if (debuff != null)
                {
                    total.Add(debuff);
                }
            }

            return total;
        }

        private DebuffSet GetAllActiveDebuffs()
        {
            DebuffSet total = new DebuffSet();
            // TODO: 디버프 액티브 캐릭터 추가 시
            return total;
        }

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion
    }
}
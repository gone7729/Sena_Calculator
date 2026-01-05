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
        // 필드 추가 (클래스 상단)
        private PresetManager _presetManager;
        private bool _isInitialized = false;

        private DebuffSet _currentDebuffs = new DebuffSet();
        
        public MainWindow()
        {
            InitializeComponent();
            _calculator = new DamageCalculator();
            InitializeComboBoxes();
             // 생성자에서 초기화 (InitializeComboBoxes() 다음에)
            _presetManager = new PresetManager();
            RefreshPresetList();
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

                // 조건부 방증 처리
                if (!string.IsNullOrEmpty(boss.DefenseIncreaseCondition))
                {
                    panelBossCondition.Visibility = Visibility.Visible;
                    txtBossCondition.Text = boss.DefenseIncreaseCondition;
                    chkBossCondition.IsChecked = false;  // 기본: 조건 미충족 (방증 적용)
                    txtBossDefInc.Text = (boss.DefenseIncrease * 100).ToString("F0");
                }
                else
                {
                    panelBossCondition.Visibility = Visibility.Collapsed;
                    txtBossDefInc.Text = (boss.DefenseIncrease * 100).ToString("F0");
                }
            }
        }

        private void BossCondition_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            // 조건 충족 체크 시 → 방증 해제 (체력 30% 미만이면 방증 없음)
            if (chkBossCondition.IsChecked == true)
            {
                txtBossDefInc.Text = "0";
            }
            else
            {
                // 조건 미충족 → 방증 적용
                var boss = GetSelectedBoss();
                if (boss != null)
                {
                    txtBossDefInc.Text = (boss.DefenseIncrease * 100).ToString("F0");
                }
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
                    FinalDef = ParseStatValue(txtStatDef.Text),
                    FinalHp = ParseStatValue(txtStatHp.Text),
                    CritDamage = ParseStatValue(txtStatCriDmg.Text),
                    DmgDealt = ParseStatValue(txtStatDmgDealt.Text),
                    DmgDealtBoss = ParseStatValue(txtStatBossDmg.Text),
                    ArmorPen = ParseStatValue(txtStatArmPen.Text),
                    WeakpointDmg = ParseStatValue(txtStatWekDmg.Text),

                    // 디버프
                    DefReduction = _currentDebuffs.Def_Reduction,
                    DmgTakenIncrease = _currentDebuffs.Dmg_Taken_Increase,
                    Vulnerability = _currentDebuffs.Vulnerability,
                    HealReduction = _currentDebuffs.Heal_Reduction,

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
            // 보스 조건 초기화
            panelBossCondition.Visibility = Visibility.Collapsed;
            chkBossCondition.IsChecked = false;

            /// ===== 버프/디버프 초기화 =====
            // 버프 패시브
            chkBuffPassiveLion.IsChecked = false;
            ResetBuffOptionButton(btnBuffPassiveLion, "라이언");
            chkBuffPassiveLina.IsChecked = false;
            ResetBuffOptionButton(btnBuffPassiveLina, "리나");
            chkBuffPassiveRachel.IsChecked = false;
            ResetBuffOptionButton(btnBuffPassiveRachel, "레이첼");
            
            // 버프 액티브
            chkBuffActiveBiscuit.IsChecked = false;
            ResetBuffOptionButton(btnBuffActiveBiscuit, "비스킷");
            chkBuffActiveLina.IsChecked = false;
            ResetBuffOptionButton(btnBuffActiveLina, "리나");
            
            // 디버프 패시브
            chkDebuffPassiveTaka.IsChecked = false;
            ResetBuffOptionButton(btnDebuffPassiveTaka, "타카");
            chkDebuffPassiveBiscuit.IsChecked = false;
            ResetBuffOptionButton(btnDebuffPassiveBiscuit, "비스킷");
            
            // 디버프 액티브
            chkDebuffActiveLina.IsChecked = false;
            ResetBuffOptionButton(btnDebuffActiveLina, "리나");
            chkDebuffActiveRachelFlame.IsChecked = false;
            chkDebuffActiveRachelPhoenix.IsChecked = false;
            ResetBuffOptionButton(btnDebuffActiveRachel, "레이첼");
            

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

            // ========== 선택한 캐릭터의 패시브 버프 (자신에게 적용) ==========
            BuffSet characterPassiveBuff = new BuffSet();
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character?.Passive != null)
                {
                    bool isEnhanced = chkSkillEnhanced.IsChecked == true;
                    int transcendLevel = cboTranscend.SelectedIndex;
                    var buff = character.Passive.GetBuffModifier(isEnhanced, transcendLevel);
                    if (buff != null)
                    {
                        characterPassiveBuff.Add(buff);
                    }
                }
            }

            // ========== 각종 스탯 소스 가져오기 ==========
            var potentialStats = GetPotentialStats();
            BaseStatSet subStats = GetSubOptionStats();
            BaseStatSet mainOptionStats = GetMainOptionStats();
            BaseStatSet accessoryStats = GetAccessoryStats();

            // 버프/디버프 (UI 체크박스)
            BuffSet passiveBuffs = GetAllPassiveBuffs();
            BuffSet activeBuffs = GetAllActiveBuffs();
            DebuffSet passiveDebuffs = GetAllPassiveDebuffs();
            DebuffSet activeDebuffs = GetAllActiveDebuffs();

            BuffSet totalBuffs = new BuffSet();
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
            // 초월 스탯 (전체)
            BaseStatSet transcendStats = new BaseStatSet();
        
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    int transcendLevel = cboTranscend.SelectedIndex;
                    transcendStats = character.GetTranscendStats(transcendLevel);
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

            // 합연산% 합계 (정수) - 진형 포함
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

            // ========== 버프% (펫스킬% + UI버프% + 캐릭터패시브%) ==========
            double buffAtkRate = GetPetSkillAtkRate() + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate = totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate = totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            // ========== 기본 스탯 (버프 적용 전) ==========
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // ========== 최종 스탯 (버프 적용 후) ==========
            double totalAtk = baseStatAtk * (1 + buffAtkRate / 100.0);
            double totalDef = baseStatDef * (1 + buffDefRate / 100.0);
            double totalHp = baseStatHp * (1 + buffHpRate / 100.0);

            // ========== 기본/최종 스탯 UI 표시 ==========
            // 표시용 (진형 미포함) - 게임 스탯창과 동일
            double displayBaseAtk = baseAtk * (1 + (totalAtkRate - formationAtkRate) / 100.0) + flatAtk;
            double displayBaseDef = baseDef * (1 + (totalDefRate - formationDefRate) / 100.0) + flatDef;
            txtStatAtkBase.Text = baseStatAtk.ToString("N0");
            txtStatDefBase.Text = baseStatDef.ToString("N0");
            txtStatHpBase.Text = baseStatHp.ToString("N0");

            txtStatAtk.Text = totalAtk.ToString("N0");
            txtStatDef.Text = totalDef.ToString("N0");
            txtStatHp.Text = totalHp.ToString("N0");

            // ========== 기타 스탯 UI 표시 ==========
            BaseStatSet displayStats = new BaseStatSet
            {
                Cri = characterStats.Cri + transcendStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri + characterPassiveBuff.Cri,
                Cri_Dmg = characterStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + characterPassiveBuff.Cri_Dmg,
                Wek = characterStats.Wek + transcendStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek + totalBuffs.Wek + characterPassiveBuff.Wek,
                Wek_Dmg = characterStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg + totalBuffs.Wek_Dmg + characterPassiveBuff.Wek_Dmg,
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
            UpdateBossDebuffDisplay();

            // 디버그 출력
            System.Diagnostics.Debug.WriteLine("========== 디버그 ==========");
            System.Diagnostics.Debug.WriteLine($"기본공격력(버프전): {baseStatAtk}");
            System.Diagnostics.Debug.WriteLine($"최종공격력(버프후): {totalAtk}");
            System.Diagnostics.Debug.WriteLine($"버프 공격력%: {buffAtkRate}");
            System.Diagnostics.Debug.WriteLine($"캐릭터 패시브 공격력%: {characterPassiveBuff.Atk_Rate}");
            System.Diagnostics.Debug.WriteLine($"캐릭터 패시브 치피%: {characterPassiveBuff.Cri_Dmg}");
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
            // 공/방/생은 RecalculateStats에서 직접 처리
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

        private BuffSet GetAllPassiveBuffs()
        {
            BuffSet total = new BuffSet();

            // 라이언 - 쾌속의 마검사 (1-3인기 피증)
            if (chkBuffPassiveLion.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnBuffPassiveLion);
                var lion = CharacterDb.GetByName("라이언");
                var buff = lion?.Passive?.GetBuffModifier(isEnhanced, transcendLevel);
                if (buff != null)
                {
                    total.MaxMerge(buff);
                }
            }

            // 리나 - 불협화음 (치피 @2초월)
            if (chkBuffPassiveLina.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnBuffPassiveLina);
                var lina = CharacterDb.GetByName("리나");
                var buff = lina?.Passive?.GetBuffModifier(isEnhanced, transcendLevel);
                if (buff != null)
                {
                    total.MaxMerge(buff);
                }
            }

            // 레이첼 - 화염의 힘 (약공확)
            if (chkBuffPassiveRachel.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnBuffPassiveRachel);
                var rachel = CharacterDb.GetByName("레이첼");
                var buff = rachel?.Passive?.GetBuffModifier(isEnhanced, transcendLevel);
                if (buff != null)
                {
                    total.MaxMerge(buff);
                }
            }

            return total;
        }

        private BuffSet GetAllActiveBuffs()
        {
            BuffSet total = new BuffSet();

            // 비스킷 - 장비 강화 (보피증, 약공확)
            if (chkBuffActiveBiscuit.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnBuffActiveBiscuit);
                var biscuit = CharacterDb.GetByName("비스킷");
                var skill = biscuit?.Skills?.FirstOrDefault(s => s.Name == "장비 강화");
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.BuffEffect != null)
                    {
                        total.MaxMerge(levelData.BuffEffect);
                    }
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.BuffModifier != null)
                    {
                        total.MaxMerge(transcendBonus.BuffModifier);
                    }
                }
            }

            // 리나 - 따뜻한 울림 (피증)
            if (chkBuffActiveLina.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnBuffActiveLina);
                var lina = CharacterDb.GetByName("리나");
                var skill = lina?.Skills?.FirstOrDefault(s => s.Name == "따뜻한 울림");
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.BuffEffect != null)
                    {
                        total.MaxMerge(levelData.BuffEffect);
                    }
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.BuffModifier != null)
                    {
                        total.MaxMerge(transcendBonus.BuffModifier);
                    }
                }
            }

            return total;
        }

        private DebuffSet GetAllPassiveDebuffs()
        {
            DebuffSet total = new DebuffSet();

            // 타카 - 매의 발톱 (취약, 받피증)
            if (chkDebuffPassiveTaka.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffPassiveTaka);
                var taka = CharacterDb.GetByName("타카");
                var debuff = taka?.Passive?.GetDebuffModifier(isEnhanced, transcendLevel);
                if (debuff != null)
                {
                    total.MaxMerge(debuff);
                }
            }

            // 비스킷 - 대장장이의 강화 (방깎)
            if (chkDebuffPassiveBiscuit.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffPassiveBiscuit);
                var biscuit = CharacterDb.GetByName("비스킷");
                var debuff = biscuit?.Passive?.GetDebuffModifier(isEnhanced, transcendLevel);
                if (debuff != null)
                {
                    total.MaxMerge(debuff);
                }
            }

            return total;
        }

        private DebuffSet GetAllActiveDebuffs()
        {
            DebuffSet total = new DebuffSet();

            // 리나 - 따뜻한 울림 (방깎)
            if (chkDebuffActiveLina.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffActiveLina);
                var lina = CharacterDb.GetByName("리나");
                var skill = lina?.Skills?.FirstOrDefault(s => s.Name == "따뜻한 울림");
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.DebuffEffect != null)
                    {
                        total.MaxMerge(levelData.DebuffEffect);
                    }
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.DebuffModifier != null)
                    {
                        total.MaxMerge(transcendBonus.DebuffModifier);
                    }
                }
            }

            // 레이첼 - 염화 (공깎, 피해량감소)
            if (chkDebuffActiveRachelFlame.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffActiveRachel);
                var rachel = CharacterDb.GetByName("레이첼");
                var skill = rachel?.Skills?.FirstOrDefault(s => s.Name == "염화");
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.DebuffEffect != null)
                    {
                        total.MaxMerge(levelData.DebuffEffect);
                    }
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.DebuffModifier != null)
                    {
                        total.MaxMerge(transcendBonus.DebuffModifier);
                    }
                }
            }

            // 레이첼 - 불새 (방깎, 취약)
            if (chkDebuffActiveRachelPhoenix.IsChecked == true)
            {
                var (isEnhanced, transcendLevel) = GetBuffOption(btnDebuffActiveRachel);
                var rachel = CharacterDb.GetByName("레이첼");
                var skill = rachel?.Skills?.FirstOrDefault(s => s.Name == "불새");
                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.DebuffEffect != null)
                    {
                        total.MaxMerge(levelData.DebuffEffect);
                    }
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.DebuffModifier != null)
                    {
                        total.MaxMerge(transcendBonus.DebuffModifier);
                    }
                }
            }

            return total;
        }

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

        

        // region 프리셋 추가
        #region 프리셋

        private void RefreshPresetList()
        {
            cboPreset.Items.Clear();
            cboPreset.Items.Add("-- 프리셋 선택 --");
            foreach (var name in _presetManager.GetPresetNames())
            {
                cboPreset.Items.Add(name);
            }
            cboPreset.SelectedIndex = 0;
        }

        private Preset CreatePresetFromUI()
        {
            var preset = new Preset
            {
                // 캐릭터
                CharacterName = cboCharacter.SelectedIndex > 0 ? cboCharacter.SelectedItem.ToString() : "",
                SkillName = cboSkill.SelectedIndex >= 0 ? cboSkill.SelectedItem?.ToString() : "",
                TranscendLevel = cboTranscend.SelectedIndex,
                IsSkillEnhanced = chkSkillEnhanced.IsChecked == true,

                // 잠재능력
                PotentialAtk = cboPotentialAtk.SelectedIndex,
                PotentialDef = cboPotentialDef.SelectedIndex,
                PotentialHp = cboPotentialHp.SelectedIndex,

                // 세트
                EquipSet1 = cboEquipSet1.SelectedIndex > 0 ? cboEquipSet1.SelectedItem.ToString() : "",
                EquipSet2 = cboEquipSet2.SelectedIndex > 0 ? cboEquipSet2.SelectedItem.ToString() : "",
                SetCount1 = cboSetCount1.SelectedIndex,

                // 메인옵션
                MainWeapon1 = cboWeapon1Main.SelectedIndex > 0 ? cboWeapon1Main.SelectedItem.ToString() : "",
                MainWeapon2 = cboWeapon2Main.SelectedIndex > 0 ? cboWeapon2Main.SelectedItem.ToString() : "",
                MainArmor1 = cboArmor1Main.SelectedIndex > 0 ? cboArmor1Main.SelectedItem.ToString() : "",
                MainArmor2 = cboArmor2Main.SelectedIndex > 0 ? cboArmor2Main.SelectedItem.ToString() : "",

                // 장신구
                AccessoryGrade = cboAccessoryGrade.SelectedIndex,
                AccessoryOption = cboAccessoryMain.SelectedIndex > 0 ? cboAccessoryMain.SelectedItem.ToString() : "",
                AccessorySubOption = cboAccessorySub.SelectedIndex > 0 ? cboAccessorySub.SelectedItem.ToString() : "",

                // 진형
                Formation = cboFormation.SelectedIndex > 0 ? cboFormation.SelectedItem.ToString() : "",
                IsBackPosition = rbBack.IsChecked == true,

                // 펫
                PetName = cboPet.SelectedIndex > 0 ? cboPet.SelectedItem.ToString() : "",
                PetStar = cboPetStar.SelectedIndex,

                // 보스
                BossType = rbSiege.IsChecked == true ? "Siege" : (rbRaid.IsChecked == true ? "Raid" : "Descend"),
                BossName = cboBoss.SelectedIndex > 0 ? cboBoss.SelectedItem.ToString() : ""
            };

            // 서브옵션
            preset.SubOptions = new List<SubOptionData>
            {
                new SubOptionData { Type = cboSub1Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub1Tier.Text, out int t1) ? t1 : 0 },
                new SubOptionData { Type = cboSub2Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub2Tier.Text, out int t2) ? t2 : 0 },
                new SubOptionData { Type = cboSub3Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub3Tier.Text, out int t3) ? t3 : 0 },
                new SubOptionData { Type = cboSub4Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub4Tier.Text, out int t4) ? t4 : 0 },
                new SubOptionData { Type = cboSub5Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub5Tier.Text, out int t5) ? t5 : 0 },
                new SubOptionData { Type = cboSub6Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub6Tier.Text, out int t6) ? t6 : 0 },
                new SubOptionData { Type = cboSub7Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub7Tier.Text, out int t7) ? t7 : 0 },
                new SubOptionData { Type = cboSub8Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub8Tier.Text, out int t8) ? t8 : 0 },
                new SubOptionData { Type = cboSub9Type.SelectedItem?.ToString(), Tier = int.TryParse(txtSub9Tier.Text, out int t9) ? t9 : 0 }
            };

            // 펫옵션
            preset.PetOptions = new List<PetOptionData>
            {
                new PetOptionData { Type = (cboPetOpt1.SelectedItem as ComboBoxItem)?.Content?.ToString(), Value = ParseDouble(txtPetOpt1.Text) },
                new PetOptionData { Type = (cboPetOpt2.SelectedItem as ComboBoxItem)?.Content?.ToString(), Value = ParseDouble(txtPetOpt2.Text) },
                new PetOptionData { Type = (cboPetOpt3.SelectedItem as ComboBoxItem)?.Content?.ToString(), Value = ParseDouble(txtPetOpt3.Text) }
            };

            // 버프/디버프 체크 상태
            preset.BuffChecks = new Dictionary<string, bool>
            {
                { "BuffPassiveLion", chkBuffPassiveLion.IsChecked == true },
                { "BuffPassiveLina", chkBuffPassiveLina.IsChecked == true },
                { "BuffPassiveRachel", chkBuffPassiveRachel.IsChecked == true },
                { "BuffActiveBiscuit", chkBuffActiveBiscuit.IsChecked == true },
                { "BuffActiveLina", chkBuffActiveLina.IsChecked == true },
                { "DebuffPassiveTaka", chkDebuffPassiveTaka.IsChecked == true },
                { "DebuffPassiveBiscuit", chkDebuffPassiveBiscuit.IsChecked == true },
                { "DebuffActiveLina", chkDebuffActiveLina.IsChecked == true },
                { "DebuffActiveRachelFlame", chkDebuffActiveRachelFlame.IsChecked == true },
                { "DebuffActiveRachelPhoenix", chkDebuffActiveRachelPhoenix.IsChecked == true }
            };

            // 버프 버튼 상태
            preset.BuffStates = new Dictionary<string, int>
            {
                { "BuffPassiveLion", int.Parse(btnBuffPassiveLion.Tag?.ToString() ?? "0") },
                { "BuffPassiveLina", int.Parse(btnBuffPassiveLina.Tag?.ToString() ?? "0") },
                { "BuffPassiveRachel", int.Parse(btnBuffPassiveRachel.Tag?.ToString() ?? "0") },
                { "BuffActiveBiscuit", int.Parse(btnBuffActiveBiscuit.Tag?.ToString() ?? "0") },
                { "BuffActiveLina", int.Parse(btnBuffActiveLina.Tag?.ToString() ?? "0") },
                { "DebuffPassiveTaka", int.Parse(btnDebuffPassiveTaka.Tag?.ToString() ?? "0") },
                { "DebuffPassiveBiscuit", int.Parse(btnDebuffPassiveBiscuit.Tag?.ToString() ?? "0") },
                { "DebuffActiveLina", int.Parse(btnDebuffActiveLina.Tag?.ToString() ?? "0") },
                { "DebuffActiveRachel", int.Parse(btnDebuffActiveRachel.Tag?.ToString() ?? "0") }
            };

            return preset;
        }

        private void ApplyPresetToUI(Preset preset)
        {
            if (preset == null) return;

            _isInitialized = false;

            // 캐릭터
            SelectComboBoxItem(cboCharacter, preset.CharacterName);
            cboTranscend.SelectedIndex = preset.TranscendLevel;
            chkSkillEnhanced.IsChecked = preset.IsSkillEnhanced;

            // 스킬은 캐릭터 선택 후 업데이트되므로 잠시 대기
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    cboSkill.Items.Clear();
                    foreach (var skill in character.Skills)
                    {
                        cboSkill.Items.Add(skill.Name);
                    }
                }
            }
            SelectComboBoxItem(cboSkill, preset.SkillName);

            // 잠재능력
            cboPotentialAtk.SelectedIndex = preset.PotentialAtk;
            cboPotentialDef.SelectedIndex = preset.PotentialDef;
            cboPotentialHp.SelectedIndex = preset.PotentialHp;

            // 세트
            SelectComboBoxItem(cboEquipSet1, preset.EquipSet1);
            SelectComboBoxItem(cboEquipSet2, preset.EquipSet2);
            cboSetCount1.SelectedIndex = preset.SetCount1;

            // 메인옵션
            SelectComboBoxItem(cboWeapon1Main, preset.MainWeapon1);
            SelectComboBoxItem(cboWeapon2Main, preset.MainWeapon2);
            SelectComboBoxItem(cboArmor1Main, preset.MainArmor1);
            SelectComboBoxItem(cboArmor2Main, preset.MainArmor2);

            // 장신구
            cboAccessoryGrade.SelectedIndex = preset.AccessoryGrade;
            SelectComboBoxItem(cboAccessoryMain, preset.AccessoryOption);
            SelectComboBoxItem(cboAccessorySub, preset.AccessorySubOption);

            // 진형
            SelectComboBoxItem(cboFormation, preset.Formation);
            if (preset.IsBackPosition)
                rbBack.IsChecked = true;
            else
                rbFront.IsChecked = true;

            // 펫
            SelectComboBoxItem(cboPet, preset.PetName);
            cboPetStar.SelectedIndex = preset.PetStar;

            // 서브옵션
            if (preset.SubOptions != null && preset.SubOptions.Count >= 9)
            {
                SelectComboBoxItem(cboSub1Type, preset.SubOptions[0].Type); txtSub1Tier.Text = preset.SubOptions[0].Tier.ToString();
                SelectComboBoxItem(cboSub2Type, preset.SubOptions[1].Type); txtSub2Tier.Text = preset.SubOptions[1].Tier.ToString();
                SelectComboBoxItem(cboSub3Type, preset.SubOptions[2].Type); txtSub3Tier.Text = preset.SubOptions[2].Tier.ToString();
                SelectComboBoxItem(cboSub4Type, preset.SubOptions[3].Type); txtSub4Tier.Text = preset.SubOptions[3].Tier.ToString();
                SelectComboBoxItem(cboSub5Type, preset.SubOptions[4].Type); txtSub5Tier.Text = preset.SubOptions[4].Tier.ToString();
                SelectComboBoxItem(cboSub6Type, preset.SubOptions[5].Type); txtSub6Tier.Text = preset.SubOptions[5].Tier.ToString();
                SelectComboBoxItem(cboSub7Type, preset.SubOptions[6].Type); txtSub7Tier.Text = preset.SubOptions[6].Tier.ToString();
                SelectComboBoxItem(cboSub8Type, preset.SubOptions[7].Type); txtSub8Tier.Text = preset.SubOptions[7].Tier.ToString();
                SelectComboBoxItem(cboSub9Type, preset.SubOptions[8].Type); txtSub9Tier.Text = preset.SubOptions[8].Tier.ToString();
            }

            // 펫옵션
            if (preset.PetOptions != null && preset.PetOptions.Count >= 3)
            {
                SelectPetOptionComboBox(cboPetOpt1, preset.PetOptions[0].Type); txtPetOpt1.Text = preset.PetOptions[0].Value.ToString();
                SelectPetOptionComboBox(cboPetOpt2, preset.PetOptions[1].Type); txtPetOpt2.Text = preset.PetOptions[1].Value.ToString();
                SelectPetOptionComboBox(cboPetOpt3, preset.PetOptions[2].Type); txtPetOpt3.Text = preset.PetOptions[2].Value.ToString();
            }

            // 보스
            if (preset.BossType == "Siege") rbSiege.IsChecked = true;
            else if (preset.BossType == "Raid") rbRaid.IsChecked = true;
            else rbDescend.IsChecked = true;
            UpdateBossList();
            SelectComboBoxItem(cboBoss, preset.BossName);

            // 버프/디버프 체크 상태
            if (preset.BuffChecks != null)
            {
                chkBuffPassiveLion.IsChecked = preset.BuffChecks.GetValueOrDefault("BuffPassiveLion", false);
                chkBuffPassiveLina.IsChecked = preset.BuffChecks.GetValueOrDefault("BuffPassiveLina", false);
                chkBuffPassiveRachel.IsChecked = preset.BuffChecks.GetValueOrDefault("BuffPassiveRachel", false);
                chkBuffActiveBiscuit.IsChecked = preset.BuffChecks.GetValueOrDefault("BuffActiveBiscuit", false);
                chkBuffActiveLina.IsChecked = preset.BuffChecks.GetValueOrDefault("BuffActiveLina", false);
                chkDebuffPassiveTaka.IsChecked = preset.BuffChecks.GetValueOrDefault("DebuffPassiveTaka", false);
                chkDebuffPassiveBiscuit.IsChecked = preset.BuffChecks.GetValueOrDefault("DebuffPassiveBiscuit", false);
                chkDebuffActiveLina.IsChecked = preset.BuffChecks.GetValueOrDefault("DebuffActiveLina", false);
                chkDebuffActiveRachelFlame.IsChecked = preset.BuffChecks.GetValueOrDefault("DebuffActiveRachelFlame", false);
                chkDebuffActiveRachelPhoenix.IsChecked = preset.BuffChecks.GetValueOrDefault("DebuffActiveRachelPhoenix", false);
            }

            // 버프 버튼 상태
            if (preset.BuffStates != null)
            {
                ApplyBuffButtonState(btnBuffPassiveLion, "라이언", preset.BuffStates.GetValueOrDefault("BuffPassiveLion", 0));
                ApplyBuffButtonState(btnBuffPassiveLina, "리나", preset.BuffStates.GetValueOrDefault("BuffPassiveLina", 0));
                ApplyBuffButtonState(btnBuffPassiveRachel, "레이첼", preset.BuffStates.GetValueOrDefault("BuffPassiveRachel", 0));
                ApplyBuffButtonState(btnBuffActiveBiscuit, "비스킷", preset.BuffStates.GetValueOrDefault("BuffActiveBiscuit", 0));
                ApplyBuffButtonState(btnBuffActiveLina, "리나", preset.BuffStates.GetValueOrDefault("BuffActiveLina", 0));
                ApplyBuffButtonState(btnDebuffPassiveTaka, "타카", preset.BuffStates.GetValueOrDefault("DebuffPassiveTaka", 0));
                ApplyBuffButtonState(btnDebuffPassiveBiscuit, "비스킷", preset.BuffStates.GetValueOrDefault("DebuffPassiveBiscuit", 0));
                ApplyBuffButtonState(btnDebuffActiveLina, "리나", preset.BuffStates.GetValueOrDefault("DebuffActiveLina", 0));
                ApplyBuffButtonState(btnDebuffActiveRachel, "레이첼", preset.BuffStates.GetValueOrDefault("DebuffActiveRachel", 0));
            }

            _isInitialized = true;
            UpdateMainOptionDisplay();
            UpdateAccessoryDisplay();
            RecalculateStats();
        }

        private void ApplyBuffButtonState(Button btn, string baseName, int state)
        {
            btn.Tag = state;
            btn.Background = BuffBgColors[state];
            btn.Foreground = BuffFgColors[state];
            btn.Content = baseName + BuffOptionSuffix[state];
        }

        private void SelectComboBoxItem(ComboBox cbo, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                cbo.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < cbo.Items.Count; i++)
            {
                if (cbo.Items[i].ToString() == value)
                {
                    cbo.SelectedIndex = i;
                    return;
                }
            }
            cbo.SelectedIndex = 0;
        }

        private void SelectPetOptionComboBox(ComboBox cbo, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                cbo.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < cbo.Items.Count; i++)
            {
                if (cbo.Items[i] is ComboBoxItem item && item.Content?.ToString() == value)
                {
                    cbo.SelectedIndex = i;
                    return;
                }
            }
            cbo.SelectedIndex = 0;
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            if (cboPreset.SelectedIndex > 0)
            {
                // 기존 프리셋 덮어쓰기
                var preset = CreatePresetFromUI();
                preset.Name = cboPreset.SelectedItem.ToString();
                _presetManager.UpdatePreset(cboPreset.SelectedIndex - 1, preset);
                MessageBox.Show($"'{preset.Name}' 프리셋이 저장되었습니다.", "저장 완료");
            }
            else
            {
                // 새 프리셋
                BtnSaveAsPreset_Click(sender, e);
            }
        }

        private void BtnSaveAsPreset_Click(object sender, RoutedEventArgs e)
        {
            var inputWindow = new Window
            {
                Title = "프리셋 이름",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(39, 39, 39))
            };

            var panel = new StackPanel { Margin = new Thickness(15) };
            var label = new TextBlock { Text = "프리셋 이름을 입력하세요:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "확인", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            var btnCancel = new Button { Content = "취소", Width = 60 };

            btnOk.Click += (s, ev) => { inputWindow.DialogResult = true; };
            btnCancel.Click += (s, ev) => { inputWindow.DialogResult = false; };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(label);
            panel.Children.Add(textBox);
            panel.Children.Add(btnPanel);
            inputWindow.Content = panel;

            if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var preset = CreatePresetFromUI();
                preset.Name = textBox.Text;
                _presetManager.AddPreset(preset);
                RefreshPresetList();
                cboPreset.SelectedIndex = cboPreset.Items.Count - 1;
                MessageBox.Show($"'{preset.Name}' 프리셋이 저장되었습니다.", "저장 완료");
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (cboPreset.SelectedIndex <= 0) return;

            string name = cboPreset.SelectedItem.ToString();
            var result = MessageBox.Show($"'{name}' 프리셋을 삭제하시겠습니까?", "삭제 확인", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _presetManager.DeletePreset(cboPreset.SelectedIndex - 1);
                RefreshPresetList();
            }
        }

        private void CboPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (cboPreset.SelectedIndex <= 0) return;

            var preset = _presetManager.GetPreset(cboPreset.SelectedIndex - 1);
            ApplyPresetToUI(preset);
        }

        #endregion
    }
}

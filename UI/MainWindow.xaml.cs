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
        private bool _isInitialized = false;
        private readonly DamageCalculator _calculator;
        private PresetManager _presetManager;
        private DebuffSet _currentDebuffs = new DebuffSet();

        // 버프/디버프 설정 리스트
        private List<BuffConfig> _buffConfigs;

        // 버프 설정 클래스
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

        private void InitializeBuffConfigs()
        {
            _buffConfigs = new List<BuffConfig>
            {
                // 버프 패시브
                new BuffConfig { Key = "BuffPassiveLion", CheckBox = chkBuffPassiveLion, Button = btnBuffPassiveLion, BaseName = "라이언", CharacterName = "라이언", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLina", CheckBox = chkBuffPassiveLina, Button = btnBuffPassiveLina, BaseName = "리나", CharacterName = "리나", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRachel", CheckBox = chkBuffPassiveRachel, Button = btnBuffPassiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDelonz", CheckBox = chkBuffPassiveDelonz, Button = btnBuffPassiveDelonz, BaseName = "델론즈", CharacterName = "델론즈", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveMiho", CheckBox = chkBuffPassiveMiho, Button = btnBuffPassiveMiho, BaseName = "미호", CharacterName = "미호", SkillName = null, IsBuff = true },

                // 버프 액티브
                new BuffConfig { Key = "BuffActiveBiscuit", CheckBox = chkBuffActiveBiscuit, Button = btnBuffActiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = "장비 강화", IsBuff = true },
                new BuffConfig { Key = "BuffActiveLina", CheckBox = chkBuffActiveLina, Button = btnBuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = true },

                // 디버프 패시브
                new BuffConfig { Key = "DebuffPassiveTaka", CheckBox = chkDebuffPassiveTaka, Button = btnDebuffPassiveTaka, BaseName = "타카", CharacterName = "타카", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBiscuit", CheckBox = chkDebuffPassiveBiscuit, Button = btnDebuffPassiveBiscuit, BaseName = "비스킷", CharacterName = "비스킷", SkillName = null, IsBuff = false },

                // 디버프 액티브
                new BuffConfig { Key = "DebuffActiveLina", CheckBox = chkDebuffActiveLina, Button = btnDebuffActiveLina, BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelFlame", CheckBox = chkDebuffActiveRachelFlame, Button = btnDebuffActiveRachel, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "염화", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelPhoenix", CheckBox = chkDebuffActiveRachelPhoenix, Button = null, BaseName = "레이첼", CharacterName = "레이첼", SkillName = "불새", IsBuff = false }
            };
        }

        #region 초기화

        private void InitializeComboBoxes()
        {
             UpdateCharacterList();

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

            // 잡몹 리스트
            cboMob.Items.Clear();
            cboMob.Items.Add("선택");
            foreach (var mob in BossDb.Mobs)
            {
                cboMob.Items.Add(mob.Name);
            }
            cboMob.SelectedIndex = 0;
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

        private void FormationPosition_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string currentTag = btn.Tag.ToString();

            if (currentTag == "Back")
            {
                btn.Tag = "Front";
                btn.Content = "전열";
                rbBack.IsChecked = false;
                rbFront.IsChecked = true;
            }
            else
            {
                btn.Tag = "Back";
                btn.Content = "후열";
                rbBack.IsChecked = true;
                rbFront.IsChecked = false;
            }

            RecalculateStats();
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
                txtBossHp.Text = "0";
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
                txtBossHp.Text =  boss.Stats.Hp.ToString("N0");

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
                txtBossHp.Text = (ParseDouble(txtBossHp.Text) * 0.29).ToString("N0");
            }
            else
            {
                // 조건 미충족 → 방증 적용
                var boss = GetSelectedBoss();
                if (boss != null)
                {
                    txtBossDefInc.Text = (boss.DefenseIncrease * 100).ToString("F0");
                    txtBossHp.Text = boss.Stats.Hp.ToString("N0");
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

                // 버프 합산
                BuffSet passiveBuffs = GetAllPassiveBuffs();
                BuffSet activeBuffs = GetAllActiveBuffs();
                double weakDmgBuff = passiveBuffs.Wek_Dmg + activeBuffs.Wek_Dmg;

                BattleMode mode = BattleMode.Boss;
                if (rbMob.IsChecked == true) mode = BattleMode.Mob;
                else if (rbPvP.IsChecked == true) mode = BattleMode.PvP;

                // 버프% 계산 추가!
                double buffAtkRate = GetPetAtkRate() 
                    + GetAllPassiveBuffs().Atk_Rate 
                    + GetAllActiveBuffs().Atk_Rate;

                if (cboCharacter.SelectedIndex > 0)
                {
                    var charForBuff = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                    if (charForBuff?.Passive != null)
                    {
                        bool isConditionMet = chkPassiveCondition.IsChecked == true;
                        bool isEnhanced = chkSkillEnhanced.IsChecked == true;
                        int transcendLevel = cboTranscend.SelectedIndex;
                        var buff = charForBuff.Passive.GetTotalSelfBuff(isEnhanced, transcendLevel, isConditionMet);
                        if (buff != null)
                        {
                            buffAtkRate += buff.Atk_Rate;
                        }
                    }
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
                    WeakpointDmgBuff = weakDmgBuff,
                    Dmg1to3 = ParseStatValue(txtStatDmg1to3.Text),
                    Dmg4to5 = ParseStatValue(txtStatDmg4to5.Text),

                    // 디버프
                    DefReduction = _currentDebuffs.Def_Reduction,
                    DmgTakenIncrease = _currentDebuffs.Dmg_Taken_Increase,
                    Vulnerability = _currentDebuffs.Vulnerability,
                    HealReduction = _currentDebuffs.Heal_Reduction,
                    EffResReduction = _currentDebuffs.Eff_Red,

                    // 보스 정보
                    BossDef = ParseDouble(txtBossDef.Text),
                    BossDefIncrease = ParseDouble(txtBossDefInc.Text),
                    BossDmgReduction = ParseDouble(txtBossDmgRdc.Text),
                    BossTargetReduction = GetSelectedTargetReduction(),

                    // 전투 옵션
                    IsCritical = chkCritical.IsChecked == true,
                    IsWeakpoint = chkWeakpoint.IsChecked == true,
                    IsBlocked = chkBlock.IsChecked == true,
                    TargetStackCount = int.TryParse(txtTargetStackCount.Text, out int stacks) ? stacks : 0,
                    ForceStatusEffect = chkStatusEffect.IsChecked == true,
                    IsLostHpConditionMet = chkLostHpCondition.IsChecked == true,

                    // 조건
                    IsSkillConditionMet = chkSkillCondition.IsChecked == true,
                    AtkBuff = buffAtkRate,  // 버프% 합계,

                    Mode = mode
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

            // 서브옵션 초기화
            txtSubAtkRate.Text = "0";
            txtSubAtk.Text = "0";
            txtSubCri.Text = "0";
            txtSubCriDmg.Text = "0";
            txtSubWek.Text = "0";
            txtSubBlk.Text = "0";
            txtSubDmgRdc.Text = "0";
            txtSubDefRate.Text = "0";
            txtSubDef.Text = "0";
            txtSubHpRate.Text = "0";
            txtSubHp.Text = "0";
            txtSubEffHit.Text = "0";
            txtSubEffRes.Text = "0";
            txtSubSpd.Text = "0";

            // 진형
            cboFormation.SelectedIndex = 0;
            rbBack.IsChecked = true;

            // 펫
            cboPet.SelectedIndex = 0;
            cboPetStar.SelectedIndex = 2;
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
            txtBossHp.Text = "0";
            // 보스 인기감소 초기화
            txtBoss1TargetRdc.Text = "0";
            txtBoss3TargetRdc.Text = "0";
            txtBoss5TargetRdc.Text = "0";
            // 보스 조건 초기화
            panelBossCondition.Visibility = Visibility.Collapsed;
            chkBossCondition.IsChecked = false;

            // 버프/디버프 초기화
            foreach (var config in _buffConfigs)
            {
                config.CheckBox.IsChecked = false;
                if (config.Button != null)
                    ResetBuffOptionButton(config.Button, config.BaseName);
            }

            txtResult.Text = "계산 버튼을 눌러\n결과를 확인하세요.";
        }
        
        private void Tier_MouseLeft(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = border.Child as TextBox;

            if (txtBox != null && int.TryParse(txtBox.Text, out int val))
            {
                txtBox.Text = (val + 1).ToString();
                RecalculateStats();
            }
        }

        private void Tier_MouseRight(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var txtBox = border.Child as TextBox;

            if (txtBox != null && int.TryParse(txtBox.Text, out int val) && val > 0)
            {
                txtBox.Text = (val - 1).ToString();
                RecalculateStats();
            }
            e.Handled = true;
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

        private void CboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateCharacterList();
        }
        
        private void UpdateCharacterList()
        {
            string previousSelection = cboCharacter.SelectedItem?.ToString();
            
            cboCharacter.Items.Clear();
            cboCharacter.Items.Add("선택하세요");
            
            string gradeFilter = (cboGrade.SelectedItem as ComboBoxItem)?.Content.ToString();
            string typeFilter = (cboType.SelectedItem as ComboBoxItem)?.Content.ToString();
            
            foreach (var character in CharacterDb.Characters)
            {
                if (gradeFilter != "전체" && character.Grade != gradeFilter)
                    continue;
                
                if (typeFilter != "전체" && character.Type != typeFilter)
                    continue;
                
                cboCharacter.Items.Add(character.Name);
            }
            
            if (previousSelection != null && cboCharacter.Items.Contains(previousSelection))
                cboCharacter.SelectedItem = previousSelection;
            else
                cboCharacter.SelectedIndex = 0;
        }

        private void AccessoryGrade_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int currentTag = int.Parse(btn.Tag.ToString());
            
            // 0(없음) → 1(4성) → 2(5성) → 3(6성) → 0(없음) 순환
            int nextTag = (currentTag + 1) % 4;
            btn.Tag = nextTag.ToString();
            
            // 버튼 텍스트 변경
            string[] grades = { "없음", "4성", "5성", "6성" };
            btn.Content = grades[nextTag];
            
            // 기존 콤보박스도 동기화 (다른 코드 호환용)
            cboAccessoryGrade.SelectedIndex = nextTag;
            
            // 6성이면 부옵션 표시
            if (nextTag == 3)
            {
                cboAccessorySub.Visibility = Visibility.Visible;
                txtAccessorySubValue.Visibility = Visibility.Visible;
            }
            else
            {
                cboAccessorySub.Visibility = Visibility.Collapsed;
                txtAccessorySubValue.Visibility = Visibility.Collapsed;
            }
            
            RecalculateStats();
        }

        private void PetStar_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int currentTag = int.Parse(btn.Tag.ToString());
            
            // 4 → 5 → 6 → 4 순환
            int nextTag = currentTag == 6 ? 4 : currentTag + 1;
            btn.Tag = nextTag.ToString();
            btn.Content = $"{nextTag}성";
            
            RecalculateStats();
        }

        private void CboPetFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void UpdatePetList()
        {
            if (cboPet == null) return;

            cboPet.Items.Clear();
            cboPet.Items.Add("직접 입력하거나 골라주세요");

            string starFilter = (cboPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";
            string rarityFilter = (cboPetRarity.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";

            // 성급 문자열 → 숫자 변환
            int starValue = 0;
            if (starFilter == "4성") starValue = 4;
            else if (starFilter == "5성") starValue = 5;
            else if (starFilter == "6성") starValue = 6;

            foreach (var pet in PetDb.Pets)
            {
                // 희귀도 필터
                bool matchRarity = rarityFilter == "전체" || pet.Rarity == rarityFilter;

                // 성급 필터 (Skills 딕셔너리 키로 확인)
                bool matchStar = starFilter == "전체" || pet.Skills.ContainsKey(starValue);

                if (matchRarity && matchStar)
                {
                    cboPet.Items.Add(pet.Name);
                }
            }

            cboPet.SelectedIndex = 0;
        }
        private void BossOption_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        private void BattleMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            if (rbBoss.IsChecked == true)
            {
                // 보스 모드
                spBossType.Visibility = Visibility.Visible;
                cboBoss.Visibility = Visibility.Visible;
                cboMob.Visibility = Visibility.Collapsed;
            }
            else if (rbMob.IsChecked == true)
            {
                // 잡몹 모드
                spBossType.Visibility = Visibility.Collapsed;
                cboBoss.Visibility = Visibility.Collapsed;
                cboMob.Visibility = Visibility.Visible;
            }
            else if (rbPvP.IsChecked == true)
            {
                // PVP 모드
                spBossType.Visibility = Visibility.Collapsed;
                cboBoss.Visibility = Visibility.Collapsed;
                cboMob.Visibility = Visibility.Collapsed;
            }

            RecalculateStats();
        }

        private void CboMob_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (cboMob.SelectedIndex > 0)
            {
                var mob = BossDb.GetMobByName(cboMob.SelectedItem.ToString());
                if (mob != null)
                {
                    txtBossDef.Text = mob.Stats.Def.ToString();
                    txtBossHp.Text = mob.Stats.Hp.ToString("N0");
                }
            }

            RecalculateStats();
        }

        #endregion

        #region 스탯 계산 (UI 표시용)

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
                    bool isConditionMet = chkPassiveCondition.IsChecked == true;
                    bool isEnhanced = chkSkillEnhanced.IsChecked == true;
                    int transcendLevel = cboTranscend.SelectedIndex;
                    var buff = character.Passive.GetTotalSelfBuff(isEnhanced, transcendLevel, isConditionMet);
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

            // ========== 합연산% ==========
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

            // 패시브 깡스탯 보너스
            BaseStatSet passiveFlatBonus = new BaseStatSet();
            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character?.Passive != null)
                {
                    bool isEnhanced = chkSkillEnhanced.IsChecked == true;
                    var passiveData = character.Passive.GetLevelData(isEnhanced);
                    passiveFlatBonus = passiveData?.FlatBonus ?? new BaseStatSet();
                }
            }

            // ========== 깡스탯 합계 ==========
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk * 2;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def * 2;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            double petFlatAtk = GetPetFlatAtk();
            double petFlatDef = GetPetFlatDef();
            double petFlatHp = GetPetFlatHp();

            double flatAtk = equipFlatAtk + potentialStats.Atk + subStats.Atk + petFlatAtk + mainOptionStats.Atk;
            double flatDef = equipFlatDef + potentialStats.Def + subStats.Def + petFlatDef + mainOptionStats.Def;
            double flatHp = equipFlatHp + potentialStats.Hp + subStats.Hp + petFlatHp + mainOptionStats.Hp;
            
            // ========== 속공 계산 ==========
            double baseSpd = characterStats.Spd;  // 캐릭터 기본 속공
            double subSpd = subStats.Spd;         // GetSubOptionStats()에서 이미 4 * tier 계산됨
            double totalSpd = baseSpd + subSpd;
            

            double formationAtkRate = GetFormationAtkRate();
            double formationDefRate = GetFormationDefRate();

            double petOptionAtkRate = GetPetOptionAtkRate();
            double petOptionDefRate = GetPetOptionDefRate();
            double petOptionHpRate = GetPetOptionHpRate();

            double totalAtkRate = transcendStats.Atk_Rate + formationAtkRate
                    + setBonus.Atk_Rate + subStats.Atk_Rate 
                    + accessoryStats.Atk_Rate + petOptionAtkRate
                    + mainOptionStats.Atk_Rate + GetPetSkillAtkRate();

            double totalDefRate = transcendStats.Def_Rate + formationDefRate
                    + setBonus.Def_Rate + subStats.Def_Rate 
                    + accessoryStats.Def_Rate + petOptionDefRate
                    + mainOptionStats.Def_Rate;

            double totalHpRate = transcendStats.Hp_Rate 
                   + setBonus.Hp_Rate + subStats.Hp_Rate 
                   + accessoryStats.Hp_Rate + petOptionHpRate
                   + mainOptionStats.Hp_Rate;

            // ========== 버프% ==========
            double buffAtkRate = GetPetAtkRate() + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate = GetPetDefRate()+ totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate = GetPetHpRate()+ totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            // ========== 기본 스탯 (버프 적용 전) ==========
            double baseStatAtk = baseAtk * (1 + totalAtkRate / 100.0) + flatAtk;
            double baseStatDef = baseDef * (1 + totalDefRate / 100.0) + flatDef;
            double baseStatHp = baseHp * (1 + totalHpRate / 100.0) + flatHp;

            // ========== 스탯 비례 증가 (속공 비례 공격력 등) ==========
            double scalingFlatAtk = 0;
            double scalingFlatDef = 0;
            double scalingFlatHp = 0;
            double scalingCri = 0;

            if (cboCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character?.Passive != null)
                {
                    bool isEnhanced = chkSkillEnhanced.IsChecked == true;
                    var passiveData = character.Passive.GetLevelData(isEnhanced);

                    if (passiveData?.StatScalings != null)
                    {
                        foreach (var scaling in passiveData.StatScalings)
                        {
                            double sourceValue = scaling.SourceStat switch
                            {
                                StatType.Spd => totalSpd,
                                StatType.Hp => baseStatHp,
                                StatType.Def => baseStatDef,
                                StatType.Atk => baseStatAtk,
                                _ => 0
                            };

                            double bonus = CalcStatScaling(sourceValue, scaling);

                            switch (scaling.TargetStat)
                            {
                                case StatType.Atk:
                                    scalingFlatAtk += bonus;
                                    break;
                                case StatType.Def:
                                    scalingFlatDef += bonus;
                                    break;
                                case StatType.Hp:
                                    scalingFlatHp += bonus;
                                    break;
                                case StatType.Cri:
                                    scalingCri += bonus;
                                    break;
                            }
                        }
                    }
                }
            }

            // ========== 스탯 비례 보너스 적용 ==========
            baseStatAtk += scalingFlatAtk;
            baseStatDef += scalingFlatDef;
            baseStatHp += scalingFlatHp;

            // ========== 최종 스탯 (버프 적용 후) ==========
            double totalAtk = baseStatAtk * (1 + buffAtkRate / 100.0);
            double totalDef = baseStatDef * (1 + buffDefRate / 100.0);
            double totalHp = baseStatHp * (1 + buffHpRate / 100.0);

            // 순수 기본 = 캐릭터 기본 + 장비 + 초월 + 장신구
            double pureBaseAtk = baseAtk * (1+ (mainOptionStats.Atk_Rate+subStats.Atk_Rate+transcendStats.Atk_Rate+accessoryStats.Atk_Rate+setBonus.Atk_Rate)/100) + flatAtk;
            double pureBaseDef = baseDef * (1+ (mainOptionStats.Def_Rate+subStats.Def_Rate+transcendStats.Def_Rate+accessoryStats.Def_Rate+setBonus.Def_Rate)/100) + flatDef;
            double pureBaseHp = baseHp * (1+ (mainOptionStats.Hp_Rate+subStats.Hp_Rate+transcendStats.Hp_Rate+accessoryStats.Hp_Rate+setBonus.Hp_Rate)/100) + flatHp;

            // ========== UI 표시 ==========
            txtStatAtkBase.Text = pureBaseAtk.ToString("N0");
            txtStatDefBase.Text = pureBaseDef.ToString("N0");
            txtStatHpBase.Text = pureBaseHp.ToString("N0");

            txtStatAtk.Text = totalAtk.ToString("N0");
            txtStatDef.Text = totalDef.ToString("N0");
            txtStatHp.Text = totalHp.ToString("N0");
            txtStatSpd.Text = totalSpd.ToString("N0");

            // ========== 기타 스탯 ==========
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
                Dmg_Dealt_4to5 = characterStats.Dmg_Dealt_4to5 + transcendStats.Dmg_Dealt_4to5 + setBonus.Dmg_Dealt_4to5 + accessoryStats.Dmg_Dealt_4to5 + totalBuffs.Dmg_Dealt_4to5 + characterPassiveBuff.Dmg_Dealt_4to5,
                Atk_Rate = totalAtkRate
            };

            UpdateStatDisplay(displayStats);
            UpdateBossDebuffDisplay();
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
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
            txtStatAtkRate.Text = $"{stats.Atk_Rate}%";
        }

        /// <summary>
        /// 스탯 비례 증가 계산
        /// </summary>
        private double CalcStatScaling(double sourceValue, StatScaling scaling)
        {
            if (scaling == null || scaling.SourceUnit <= 0) return 0;

            // 기준 스탯 / 단위 = 배수
            double multiplier = Math.Floor(sourceValue / scaling.SourceUnit);

            // 증가량
            double bonus = multiplier * scaling.PerUnit;

            // 최대치 제한
            return Math.Min(bonus, scaling.MaxValue);
        }

        #endregion

        #region 펫 관련

        private int GetPetStar()
        {
            string starStr = (cboPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6성";

            if (starStr == "4성") return 4;
            else if (starStr == "5성") return 5;
            else if (starStr == "6성") return 6;
            else return 0;
        }

        private double GetPetFlatAtk()
        {
            if (cboPet.SelectedIndex <= 0) return 0;

            var pet = PetDb.GetByName(cboPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
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
                int star = GetPetStar();
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
                int star = GetPetStar();
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
                int star = GetPetStar();
                if (star == 0) return 0;
                return pet.GetSkillBonus(star).Atk_Rate;
            }
            return 0;
        }

        private double GetPetAtkRate()
        {
            if (double.TryParse(txtPetAtk.Text, out double val))
                return val;
            return 0;
        }
        
        private double GetPetDefRate()
        {
            if (double.TryParse(txtPetDef.Text, out double val))
                return val;
            return 0;
        }
        
        private double GetPetHpRate()
        {
            if (double.TryParse(txtPetHp.Text, out double val))
                return val;
            return 0;
        }

        private double GetPetOptionAtkRate()
        {
            if (double.TryParse(txtPetAtk.Text, out double val))
                return val;
            return 0;
        }

        private double GetPetOptionDefRate()
        {
            if (double.TryParse(txtPetDef.Text, out double val))
                return val;
            return 0;
        }

        private double GetPetOptionHpRate()
        {
            if (double.TryParse(txtPetHp.Text, out double val))
                return val;
            return 0;
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
        
            // 티어 값 파싱
            int atkRateTier = int.TryParse(txtSubAtkRate.Text, out int t1) ? t1 : 0;
            int atkTier = int.TryParse(txtSubAtk.Text, out int t2) ? t2 : 0;
            int criTier = int.TryParse(txtSubCri.Text, out int t3) ? t3 : 0;
            int criDmgTier = int.TryParse(txtSubCriDmg.Text, out int t4) ? t4 : 0;
            int wekTier = int.TryParse(txtSubWek.Text, out int t5) ? t5 : 0;
            int blkTier = int.TryParse(txtSubBlk.Text, out int t6) ? t6 : 0;
            int dmgRdcTier = int.TryParse(txtSubDmgRdc.Text, out int t7) ? t7 : 0;
            int defRateTier = int.TryParse(txtSubDefRate.Text, out int t8) ? t8 : 0;
            int defTier = int.TryParse(txtSubDef.Text, out int t9) ? t9 : 0;
            int hpRateTier = int.TryParse(txtSubHpRate.Text, out int t10) ? t10 : 0;
            int hpTier = int.TryParse(txtSubHp.Text, out int t11) ? t11 : 0;
            int effHitTier = int.TryParse(txtSubEffHit.Text, out int t12) ? t12 : 0;
            int effResTier = int.TryParse(txtSubEffRes.Text, out int t13) ? t13 : 0;
            int spdTier = int.TryParse(txtSubSpd.Text, out int t14) ? t14 : 0;
        
            // 티어당 스탯 적용
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
                {
                    if (mainOptions.TryGetValue(mainOpt, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

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

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

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
                CharacterName = cboCharacter.SelectedIndex > 0 ? cboCharacter.SelectedItem.ToString() : "",
                SkillName = cboSkill.SelectedIndex >= 0 ? cboSkill.SelectedItem?.ToString() : "",
                TranscendLevel = cboTranscend.SelectedIndex,
                IsSkillEnhanced = chkSkillEnhanced.IsChecked == true,

                PotentialAtk = cboPotentialAtk.SelectedIndex,
                PotentialDef = cboPotentialDef.SelectedIndex,
                PotentialHp = cboPotentialHp.SelectedIndex,

                EquipSet1 = cboEquipSet1.SelectedIndex > 0 ? cboEquipSet1.SelectedItem.ToString() : "",
                EquipSet2 = cboEquipSet2.SelectedIndex > 0 ? cboEquipSet2.SelectedItem.ToString() : "",
                SetCount1 = cboSetCount1.SelectedIndex,

                MainWeapon1 = cboWeapon1Main.SelectedIndex > 0 ? cboWeapon1Main.SelectedItem.ToString() : "",
                MainWeapon2 = cboWeapon2Main.SelectedIndex > 0 ? cboWeapon2Main.SelectedItem.ToString() : "",
                MainArmor1 = cboArmor1Main.SelectedIndex > 0 ? cboArmor1Main.SelectedItem.ToString() : "",
                MainArmor2 = cboArmor2Main.SelectedIndex > 0 ? cboArmor2Main.SelectedItem.ToString() : "",

                AccessoryGrade = cboAccessoryGrade.SelectedIndex,
                AccessoryOption = cboAccessoryMain.SelectedIndex > 0 ? cboAccessoryMain.SelectedItem.ToString() : "",
                AccessorySubOption = cboAccessorySub.SelectedIndex > 0 ? cboAccessorySub.SelectedItem.ToString() : "",

                Formation = cboFormation.SelectedIndex > 0 ? cboFormation.SelectedItem.ToString() : "",
                IsBackPosition = rbBack.IsChecked == true,

                PetName = cboPet.SelectedIndex > 0 ? cboPet.SelectedItem.ToString() : "",
                PetStar = cboPetStar.SelectedIndex,

                BossType = rbSiege.IsChecked == true ? "Siege" : (rbRaid.IsChecked == true ? "Raid" : "Descend"),
                BossName = cboBoss.SelectedIndex > 0 ? cboBoss.SelectedItem.ToString() : ""
            };

            // 프리셋 저장 (CreatePresetFromUI)
            preset.SubOptions = new Dictionary<string, int>
            {
                { "AtkRate", int.TryParse(txtSubAtkRate.Text, out int t1) ? t1 : 0 },
                { "Atk", int.TryParse(txtSubAtk.Text, out int t2) ? t2 : 0 },
                { "Cri", int.TryParse(txtSubCri.Text, out int t3) ? t3 : 0 },
                { "CriDmg", int.TryParse(txtSubCriDmg.Text, out int t4) ? t4 : 0 },
                { "Wek", int.TryParse(txtSubWek.Text, out int t5) ? t5 : 0 },
                { "Blk", int.TryParse(txtSubBlk.Text, out int t6) ? t6 : 0 },
                { "DmgRdc", int.TryParse(txtSubDmgRdc.Text, out int t7) ? t7 : 0 },
                { "DefRate", int.TryParse(txtSubDefRate.Text, out int t8) ? t8 : 0 },
                { "Def", int.TryParse(txtSubDef.Text, out int t9) ? t9 : 0 },
                { "HpRate", int.TryParse(txtSubHpRate.Text, out int t10) ? t10 : 0 },
                { "Hp", int.TryParse(txtSubHp.Text, out int t11) ? t11 : 0 },
                { "EffHit", int.TryParse(txtSubEffHit.Text, out int t12) ? t12 : 0 },
                { "EffRes", int.TryParse(txtSubEffRes.Text, out int t13) ? t13 : 0 }
            };

            preset.PetAtkRate = ParseDouble(txtPetAtk.Text);
            preset.PetDefRate = ParseDouble(txtPetDef.Text);
            preset.PetHpRate = ParseDouble(txtPetHp.Text);

            preset.BuffChecks = new Dictionary<string, bool>();
            preset.BuffStates = new Dictionary<string, int>();
            foreach (var config in _buffConfigs)
            {
                preset.BuffChecks[config.Key] = config.CheckBox.IsChecked == true;
                if (config.Button != null)
                    preset.BuffStates[config.Key] = int.Parse(config.Button.Tag?.ToString() ?? "0");
            }

            return preset;
        }

        private void ApplyPresetToUI(Preset preset)
        {
            if (preset == null) return;

            _isInitialized = false;

            SelectComboBoxItem(cboCharacter, preset.CharacterName);
            cboTranscend.SelectedIndex = preset.TranscendLevel;
            chkSkillEnhanced.IsChecked = preset.IsSkillEnhanced;

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

            cboPotentialAtk.SelectedIndex = preset.PotentialAtk;
            cboPotentialDef.SelectedIndex = preset.PotentialDef;
            cboPotentialHp.SelectedIndex = preset.PotentialHp;

            SelectComboBoxItem(cboEquipSet1, preset.EquipSet1);
            SelectComboBoxItem(cboEquipSet2, preset.EquipSet2);
            cboSetCount1.SelectedIndex = preset.SetCount1;

            SelectComboBoxItem(cboWeapon1Main, preset.MainWeapon1);
            SelectComboBoxItem(cboWeapon2Main, preset.MainWeapon2);
            SelectComboBoxItem(cboArmor1Main, preset.MainArmor1);
            SelectComboBoxItem(cboArmor2Main, preset.MainArmor2);

            cboAccessoryGrade.SelectedIndex = preset.AccessoryGrade;
            SelectComboBoxItem(cboAccessoryMain, preset.AccessoryOption);
            SelectComboBoxItem(cboAccessorySub, preset.AccessorySubOption);

            SelectComboBoxItem(cboFormation, preset.Formation);
            if (preset.IsBackPosition)
                rbBack.IsChecked = true;
            else
                rbFront.IsChecked = true;

            SelectComboBoxItem(cboPet, preset.PetName);
            cboPetStar.SelectedIndex = preset.PetStar;
            
            // 프리셋 불러오기 (ApplyPresetToUI)
            if (preset.SubOptions != null)
            {
                txtSubAtkRate.Text = preset.SubOptions.GetValueOrDefault("AtkRate", 0).ToString();
                txtSubAtk.Text = preset.SubOptions.GetValueOrDefault("Atk", 0).ToString();
                txtSubCri.Text = preset.SubOptions.GetValueOrDefault("Cri", 0).ToString();
                txtSubCriDmg.Text = preset.SubOptions.GetValueOrDefault("CriDmg", 0).ToString();
                txtSubWek.Text = preset.SubOptions.GetValueOrDefault("Wek", 0).ToString();
                txtSubBlk.Text = preset.SubOptions.GetValueOrDefault("Blk", 0).ToString();
                txtSubDmgRdc.Text = preset.SubOptions.GetValueOrDefault("DmgRdc", 0).ToString();
                txtSubDefRate.Text = preset.SubOptions.GetValueOrDefault("DefRate", 0).ToString();
                txtSubDef.Text = preset.SubOptions.GetValueOrDefault("Def", 0).ToString();
                txtSubHpRate.Text = preset.SubOptions.GetValueOrDefault("HpRate", 0).ToString();
                txtSubHp.Text = preset.SubOptions.GetValueOrDefault("Hp", 0).ToString();
                txtSubEffHit.Text = preset.SubOptions.GetValueOrDefault("EffHit", 0).ToString();
                txtSubEffRes.Text = preset.SubOptions.GetValueOrDefault("EffRes", 0).ToString();
            }

            txtPetAtk.Text = preset.PetAtkRate.ToString();
            txtPetDef.Text = preset.PetDefRate.ToString();
            txtPetHp.Text = preset.PetHpRate.ToString();

            if (preset.BossType == "Siege") rbSiege.IsChecked = true;
            else if (preset.BossType == "Raid") rbRaid.IsChecked = true;
            else rbDescend.IsChecked = true;
            UpdateBossList();
            SelectComboBoxItem(cboBoss, preset.BossName);

            if (preset.BuffChecks != null)
            {
                foreach (var config in _buffConfigs)
                {
                    config.CheckBox.IsChecked = preset.BuffChecks.GetValueOrDefault(config.Key, false);
                    if (config.Button != null && preset.BuffStates != null)
                    {
                        int state = preset.BuffStates.GetValueOrDefault(config.Key, 0);
                        ApplyBuffButtonState(config.Button, config.BaseName, state);
                    }
                }
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
                var preset = CreatePresetFromUI();
                preset.Name = cboPreset.SelectedItem.ToString();
                _presetManager.UpdatePreset(cboPreset.SelectedIndex - 1, preset);
                MessageBox.Show($"'{preset.Name}' 프리셋이 저장되었습니다.", "저장 완료");
            }
            else
            {
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
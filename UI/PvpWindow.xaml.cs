using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameDamageCalculator.Models;
using GameDamageCalculator.Database;
using GameDamageCalculator.Services;

namespace GameDamageCalculator.UI
{
    public partial class PvpWindow : Window
    {
        #region 필드

        private bool _isInitialized = false;
        private readonly StatCalculatorHelper _statHelper;
        private readonly PvpDamageCalculator _calculator;
        private List<BuffConfig> _buffConfigs;

        private class BuffConfig
        {
            public string Key { get; set; }
            public string BaseName { get; set; }
            public string CharacterName { get; set; }
            public string SkillName { get; set; }
            public bool IsBuff { get; set; }
        }

        // 버프 컨트롤 동적 접근 헬퍼
        private CheckBox GetBuffCheckBox(BuffConfig config, string prefix)
        {
            return FindName($"chk{prefix}{config.Key}") as CheckBox;
        }

        private Button GetBuffButton(BuffConfig config, string prefix)
        {
            return FindName($"btn{prefix}{config.Key}") as Button;
        }

        #endregion

        #region 생성자 및 초기화

        public PvpWindow()
        {
            InitializeComponent();
            _statHelper = new StatCalculatorHelper(this);
            _calculator = new PvpDamageCalculator();
            InitializeBuffConfigs();
            InitializeComboBoxes();
            _isInitialized = true;
            RecalculateStats("My");
            RecalculateStats("Enemy");
        }

        private void InitializeBuffConfigs()
        {
            _buffConfigs = new List<BuffConfig>
            {
                // 버프 지속
                new BuffConfig { Key = "BuffPassiveYeonhee", BaseName = "연희", CharacterName = "연희", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDazy", BaseName = "데이지", CharacterName = "데이지", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKiriel", BaseName = "키리엘", CharacterName = "키리엘", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveAilin", BaseName = "아일린", CharacterName = "아일린", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDelonz", BaseName = "델론즈", CharacterName = "델론즈", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLina", BaseName = "리나", CharacterName = "리나", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKagura", BaseName = "카구라", CharacterName = "카구라", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveOrly", BaseName = "오를리", CharacterName = "오를리", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveMiho", BaseName = "미호", CharacterName = "미호", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLion", BaseName = "라이언", CharacterName = "라이언", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRachel", BaseName = "레이첼", CharacterName = "레이첼", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveColt", BaseName = "콜트", CharacterName = "콜트", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassivePreiya", BaseName = "프레이야", CharacterName = "프레이야", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveYushin", BaseName = "유신", CharacterName = "유신", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRozy", BaseName = "로지", CharacterName = "로지", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKarma", BaseName = "카르마", CharacterName = "카르마", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRudi", BaseName = "루디", CharacterName = "루디", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRook", BaseName = "룩", CharacterName = "룩", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveSpike", BaseName = "스파이크", CharacterName = "스파이크", SkillName = null, IsBuff = true },
                new BuffConfig { Key = "BuffPassiveAriel", BaseName = "아리엘", CharacterName = "아리엘", SkillName = null, IsBuff = true },

                // 버프 턴제
                new BuffConfig { Key = "BuffActiveDazy", BaseName = "데이지", CharacterName = "데이지", SkillName = "불나비", IsBuff = true },
                new BuffConfig { Key = "BuffActiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", SkillName = "장비 강화", IsBuff = true },
                new BuffConfig { Key = "BuffActiveLina", BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = true },
                new BuffConfig { Key = "BuffActiveAlice", BaseName = "엘리스", CharacterName = "엘리스", SkillName = "비밀의 문", IsBuff = true },
                new BuffConfig { Key = "BuffActiveZik", BaseName = "지크", CharacterName = "지크", SkillName = "나만 믿어", IsBuff = true },
                new BuffConfig { Key = "BuffActiveGoku", BaseName = "손오공", CharacterName = "손오공", SkillName = "여의참난무", IsBuff = true },
                new BuffConfig { Key = "BuffActiveRudi", BaseName = "루디", CharacterName = "루디", SkillName = "방어 준비", IsBuff = true },
                new BuffConfig { Key = "BuffActiveAkila", BaseName = "아킬라", CharacterName = "아킬라", SkillName = "칠흑의 장막", IsBuff = true },
                new BuffConfig { Key = "BuffActiveNoho", BaseName = "노호", CharacterName = "노호", SkillName = "칼보다 강한 펜", IsBuff = true },
                new BuffConfig { Key = "BuffActiveYui", BaseName = "유이", CharacterName = "유이", SkillName = "축복의 선율", IsBuff = true },

                // 디버프 지속
                new BuffConfig { Key = "DebuffPassiveTaka", BaseName = "타카", CharacterName = "타카", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveMilia", BaseName = "밀리아", CharacterName = "밀리아", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBanesa", BaseName = "바네사", CharacterName = "바네사", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAce", BaseName = "에이스", CharacterName = "에이스", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveElisia", BaseName = "엘리시아", CharacterName = "엘리시아", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveMelkir", BaseName = "멜키르", CharacterName = "멜키르", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAragon", BaseName = "아라곤", CharacterName = "아라곤", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveChancellor", BaseName = "챈슬러", CharacterName = "챈슬러", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAkila", BaseName = "아킬라", CharacterName = "아킬라", SkillName = null, IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveNox", BaseName = "녹스", CharacterName = "녹스", SkillName = null, IsBuff = false },

                // 디버프 턴제
                new BuffConfig { Key = "DebuffActiveLina", BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveJuri", BaseName = "쥬리", CharacterName = "쥬리", SkillName = "천상의 심판", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveOrly", BaseName = "오를리", CharacterName = "오를리", SkillName = "고결한 유성", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveKagura", BaseName = "카구라", CharacterName = "카구라", SkillName = "해방-팔사검", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveMiho", BaseName = "미호", CharacterName = "미호", SkillName = "살율의 춤", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveVellica", BaseName = "벨리카", CharacterName = "벨리카", SkillName = "어둠의 환영", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveEspada", BaseName = "에스파다", CharacterName = "에스파다", SkillName = "정화탄", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveBanesa", BaseName = "바네사", CharacterName = "바네사", SkillName = "메마른 해일", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelFlame", BaseName = "레이첼", CharacterName = "레이첼", SkillName = "염화", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelPhoenix", BaseName = "레이첼", CharacterName = "레이첼", SkillName = "불새", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveColt", BaseName = "콜트", CharacterName = "콜트", SkillName = "어때, 화려하지?", IsBuff = false },
                new BuffConfig { Key = "DebuffActivePlaton", BaseName = "플라튼", CharacterName = "플라튼", SkillName = "평타", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveZik", BaseName = "지크", CharacterName = "지크", SkillName = "부숴버려!", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", SkillName = "평타", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRozy", BaseName = "로지", CharacterName = "로지", SkillName = "평타", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveYushinFlat", BaseName = "유신", CharacterName = "유신", SkillName = "평타", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveYushinS2", BaseName = "유신", CharacterName = "유신", SkillName = "번뇌", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveMelkir", BaseName = "멜키르", CharacterName = "멜키르", SkillName = "금지된 실험", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAceS1", BaseName = "에이스", CharacterName = "에이스", SkillName = "달빛 베기", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAceS2", BaseName = "에이스", CharacterName = "에이스", SkillName = "일도천화엽", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveChancellorS1", BaseName = "챈슬러", CharacterName = "챈슬러", SkillName = "분쇄", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveChancellorS2", BaseName = "챈슬러", CharacterName = "챈슬러", SkillName = "대지 파괴", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAragon", BaseName = "아라곤", CharacterName = "아라곤", SkillName = "포격 지원", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveKarma", BaseName = "카르마", CharacterName = "카르마", SkillName = "평타", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveNox", BaseName = "녹스", CharacterName = "녹스", SkillName = "지옥의 일격", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveGoku", BaseName = "손오공", CharacterName = "손오공", SkillName = "환.여의난참무", IsBuff = false },
                new BuffConfig { Key = "DebuffActivePungyeon", BaseName = "풍연", CharacterName = "풍연", SkillName = "구음검격", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAriel", BaseName = "아리엘", CharacterName = "아리엘", SkillName = "눈부신 빛", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveNoho", BaseName = "노호", CharacterName = "노호", SkillName = "파멸의 고서", IsBuff = true },
            };
        }

        private void InitializeComboBoxes()
        {
            // 캐릭터 목록
            UpdateCharacterList("My");
            UpdateCharacterList("Enemy");

            // 초월 단계
            for (int i = 0; i <= 12; i++)
            {
                cboMyTranscend.Items.Add($"{i}초월");
                cboEnemyTranscend.Items.Add($"{i}초월");
            }
            cboMyTranscend.SelectedIndex = 0;
            cboEnemyTranscend.SelectedIndex = 0;

            // 장비 세트
            InitializeEquipSetComboBox(cboMyEquipSet1);
            InitializeEquipSetComboBox(cboMyEquipSet2);
            InitializeEquipSetComboBox(cboEnemyEquipSet1);
            InitializeEquipSetComboBox(cboEnemyEquipSet2);

            // 메인옵션
            InitializeMainOptions("My");
            InitializeMainOptions("Enemy");

            // 장신구 옵션
            InitializeAccessoryOptions("My");
            InitializeAccessoryOptions("Enemy");

            // 진형
            InitializeFormationComboBox(cboMyFormation);
            InitializeFormationComboBox(cboEnemyFormation);

            // 펫
            InitializePetComboBox(cboMyPet);
            InitializePetComboBox(cboEnemyPet);
        }

        private void InitializeEquipSetComboBox(ComboBox cbo)
        {
            cbo.Items.Add("없음");
            foreach (var setName in EquipmentDb.SetEffects.Keys)
            {
                cbo.Items.Add(setName);
            }
            cbo.SelectedIndex = 0;
        }

        private void InitializeMainOptions(string prefix)
        {
            var cboWeapon1 = FindName($"cbo{prefix}Weapon1Main") as ComboBox;
            var cboWeapon2 = FindName($"cbo{prefix}Weapon2Main") as ComboBox;
            var cboArmor1 = FindName($"cbo{prefix}Armor1Main") as ComboBox;
            var cboArmor2 = FindName($"cbo{prefix}Armor2Main") as ComboBox;

            string[] weaponOptions = { "없음", "공격력", "치명타 확률", "약점 공격 확률" };
            foreach (var opt in weaponOptions)
            {
                cboWeapon1?.Items.Add(opt);
                cboWeapon2?.Items.Add(opt);
            }
            if (cboWeapon1 != null) cboWeapon1.SelectedIndex = 0;
            if (cboWeapon2 != null) cboWeapon2.SelectedIndex = 0;

            string[] armorOptions = { "없음", "방어력", "생명력", "막기 확률" };
            foreach (var opt in armorOptions)
            {
                cboArmor1?.Items.Add(opt);
                cboArmor2?.Items.Add(opt);
            }
            if (cboArmor1 != null) cboArmor1.SelectedIndex = 0;
            if (cboArmor2 != null) cboArmor2.SelectedIndex = 0;
        }

        private void InitializeAccessoryOptions(string prefix)
        {
            var cboMain = FindName($"cbo{prefix}AccessoryMain") as ComboBox;
            var cboSub = FindName($"cbo{prefix}AccessorySub") as ComboBox;

            string[] mainOptions = { "없음", "공격력%", "방어력%", "생명력%", "치명타 피해", "피해 증가", "1~3인기 피해 증가", "4~5인기 피해 증가" };
            string[] subOptions = { "없음", "공격력%", "방어력%", "생명력%", "치명타 피해", "피해 증가" };

            foreach (var opt in mainOptions)
                cboMain?.Items.Add(opt);
            foreach (var opt in subOptions)
                cboSub?.Items.Add(opt);

            if (cboMain != null) cboMain.SelectedIndex = 0;
            if (cboSub != null) cboSub.SelectedIndex = 0;
        }

        private void InitializeFormationComboBox(ComboBox cbo)
        {
            cbo.Items.Add("없음");
            foreach (var name in StatTable.FormationDb.Formations.Keys)
            {
                cbo.Items.Add(name);
            }
            cbo.SelectedIndex = 0;
        }

        private void InitializePetComboBox(ComboBox cbo)
        {
            if (cbo == null) 
            {
                System.Diagnostics.Debug.WriteLine("InitializePetComboBox: cbo is null");
                return;
            }
            
            cbo.Items.Clear();
            cbo.Items.Add("없음");
            
            // PetDb.Pets 직접 확인 후 로드
            var pets = PetDb.Pets;
            System.Diagnostics.Debug.WriteLine($"InitializePetComboBox: PetDb.Pets count = {pets?.Count ?? 0}");
            
            if (pets != null && pets.Count > 0)
            {
                foreach (var pet in pets)
                {
                    cbo.Items.Add(pet.Name);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"InitializePetComboBox: {cbo.Name} items count = {cbo.Items.Count}");
            cbo.SelectedIndex = 0;
        }

        #endregion

        #region 캐릭터 필터/선택

        private void UpdateCharacterList(string prefix)
        {
            var cboGrade = FindName($"cbo{prefix}Grade") as ComboBox;
            var cboType = FindName($"cbo{prefix}Type") as ComboBox;
            var cboCharacter = FindName($"cbo{prefix}Character") as ComboBox;

            if (cboCharacter == null) return;

            cboCharacter.Items.Clear();
            cboCharacter.Items.Add("선택");

            string gradeFilter = (cboGrade?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";
            string typeFilter = (cboType?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";

            foreach (var character in CharacterDb.Characters)
            {
                if (gradeFilter != "전체" && character.Grade != gradeFilter) continue;
                if (typeFilter != "전체" && character.Type != typeFilter) continue;
                cboCharacter.Items.Add(character.Name);
            }

            cboCharacter.SelectedIndex = 0;
        }

        private void CboMyFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateCharacterList("My");
        }

        private void CboEnemyFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateCharacterList("Enemy");
        }

        private void CboMyCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateSkillList("My");
            RecalculateStats("My");
        }

        private void CboEnemyCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateSkillList("Enemy");
            RecalculateStats("Enemy");
        }

        private void UpdateSkillList(string prefix)
        {
            var cboCharacter = FindName($"cbo{prefix}Character") as ComboBox;
            var cboSkill = FindName($"cbo{prefix}Skill") as ComboBox;

            if (cboSkill == null) return;
            cboSkill.Items.Clear();

            if (cboCharacter?.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    foreach (var skill in character.Skills)
                    {
                        cboSkill.Items.Add(skill.Name);
                    }
                }
            }

            if (cboSkill.Items.Count > 0)
                cboSkill.SelectedIndex = 0;
        }

        private void CboMySkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("My");
        }

        private void CboEnemySkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("Enemy");
        }

        private void CboMyTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("My");
        }

        private void CboEnemyTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("Enemy");
        }

        #endregion

        #region 옵션 변경 이벤트

        private void MyOption_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("My");
        }

        private void EnemyOption_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats("Enemy");
        }

        private void CboMySetCount1_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            gridMyEquipSet2.Visibility = cboMyEquipSet1.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            RecalculateStats("My");
        }

        private void CboEnemySetCount1_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            gridEnemyEquipSet2.Visibility = cboEnemyEquipSet1.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            RecalculateStats("Enemy");
        }

        private void CboMyAccessoryGrade_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            panelMyAccessorySub.Visibility = cboMyAccessoryGrade.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
            RecalculateStats("My");
        }

        private void CboEnemyAccessoryGrade_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            panelEnemyAccessorySub.Visibility = cboEnemyAccessoryGrade.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
            RecalculateStats("Enemy");
        }

        private void BtnMyFormationPosition_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormationPosition("My");
        }

        private void BtnEnemyFormationPosition_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormationPosition("Enemy");
        }

        private void ToggleFormationPosition(string prefix)
        {
            var btn = FindName($"btn{prefix}FormationPosition") as Button;
            var rbBack = FindName($"rb{prefix}Back") as RadioButton;
            var rbFront = FindName($"rb{prefix}Front") as RadioButton;

            if (btn == null) return;

            bool isBack = btn.Tag?.ToString() == "Back";
            btn.Tag = isBack ? "Front" : "Back";
            btn.Content = isBack ? "전열" : "후열";

            if (rbBack != null) rbBack.IsChecked = !isBack;
            if (rbFront != null) rbFront.IsChecked = isBack;

            RecalculateStats(prefix);
        }

        #endregion

        #region 버프/디버프 합산

        private (bool isEnhanced, int transcendLevel) GetBuffOption(Button btn, string prefix)
        {
            if (btn == null) return (false, 0);
            int tag = int.TryParse(btn.Tag?.ToString(), out int t) ? t : 0;
            bool isEnhanced = tag >= 4;
            int transcendLevel = tag % 4 * 4;
            return (isEnhanced, transcendLevel);
        }

        /// <summary>
        /// 상시 패시브 버프 수집 (PermanentBuff끼리 MaxMerge)
        /// </summary>
        private PermanentBuff GetAllPermanentPassiveBuffs(string prefix)
        {
            PermanentBuff total = new PermanentBuff();
            foreach (var config in _buffConfigs.Where(c => c.IsBuff && c.SkillName == null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;
                var btn = GetBuffButton(config, prefix);
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
                var character = CharacterDb.GetByName(config.CharacterName);
                var buff = character?.Passive?.GetPartyBuff(isEnhanced, transcendLevel);
                if (buff != null) total.MaxMerge(buff);
            }
            return total;
        }

        /// <summary>
        /// 턴제 패시브 버프 수집 (조건부 - TimedBuff끼리 MaxMerge)
        /// </summary>
        private TimedBuff GetAllTimedPassiveBuffs(string prefix)
        {
            TimedBuff total = new TimedBuff();
            foreach (var config in _buffConfigs.Where(c => c.IsBuff && c.SkillName == null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;
                var btn = GetBuffButton(config, prefix);
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
                var character = CharacterDb.GetByName(config.CharacterName);
                // 조건부 버프는 턴제
                var buff = character?.Passive?.GetConditionalPartyBuff(isEnhanced, transcendLevel);
                if (buff != null) total.MaxMerge(buff);
            }
            return total;
        }

        /// <summary>
        /// 액티브 스킬 버프 수집 (TimedBuff끼리 MaxMerge)
        /// </summary>
        private TimedBuff GetAllActiveBuffs(string prefix)
        {
            TimedBuff total = new TimedBuff();
            foreach (var config in _buffConfigs.Where(c => c.IsBuff && c.SkillName != null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;

                var btn = GetBuffButton(config, prefix);
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
                var character = CharacterDb.GetByName(config.CharacterName);
                var skill = character?.Skills?.FirstOrDefault(s => s.Name == config.SkillName);

                if (skill != null)
                {
                    var levelData = skill.GetLevelData(isEnhanced);
                    if (levelData?.PartyBuff != null)
                        total.MaxMerge(levelData.PartyBuff);
                    var transcendBonus = skill.GetTranscendBonus(transcendLevel);
                    if (transcendBonus?.PartyBuff != null)
                        total.MaxMerge(transcendBonus.PartyBuff);
                }
            }
            return total;
        }

        /// <summary>
        /// 전체 버프 합산 (상시 + 턴제 + 펫)
        /// 상시끼리 MaxMerge, 턴제끼리 MaxMerge, 그 결과를 Add
        /// </summary>
        private BuffSet GetTotalBuffs(string prefix)
        {
            // 상시 버프 (패시브 상시끼리 MaxMerge)
            PermanentBuff permanentBuffs = GetAllPermanentPassiveBuffs(prefix);

            // 턴제 버프 (패시브 조건부 + 액티브 스킬 끼리 MaxMerge)
            TimedBuff timedBuffs = new TimedBuff();
            timedBuffs.MaxMerge(GetAllTimedPassiveBuffs(prefix));
            timedBuffs.MaxMerge(GetAllActiveBuffs(prefix));

            // 펫 버프 (별도 합산 - 영웅 버프와 중첩 가능)
            BuffSet petBuffs = GetPetSkillBuff(prefix);

            // 최종 합산
            BuffSet total = new BuffSet();
            total.Add(permanentBuffs);  // 상시 (이미 MaxMerge됨)
            total.Add(timedBuffs);       // 턴제 (이미 MaxMerge됨)
            total.Add(petBuffs);         // 펫 (별도 합산)

            return total;
        }

        // 기존 메서드 유지 (하위 호환)
        private BuffSet GetAllPassiveBuffs(string prefix)
        {
            return GetAllPermanentPassiveBuffs(prefix);
        }

        /// <summary>
        /// 상시 패시브 디버프 수집 (PermanentDebuff끼리 MaxMerge)
        /// </summary>
        private PermanentDebuff GetAllPermanentPassiveDebuffs(string prefix)
        {
            PermanentDebuff total = new PermanentDebuff();
            foreach (var config in _buffConfigs.Where(c => !c.IsBuff && c.SkillName == null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;
                var btn = GetBuffButton(config, prefix);
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
                var character = CharacterDb.GetByName(config.CharacterName);
                var debuff = character?.Passive?.GetDebuff(isEnhanced, transcendLevel);
                if (debuff != null) total.MaxMerge(debuff);
            }
            return total;
        }

        /// <summary>
        /// 턴제 패시브 디버프 수집 (조건부 - TimedDebuff끼리 MaxMerge)
        /// </summary>
        private TimedDebuff GetAllTimedPassiveDebuffs(string prefix)
        {
            TimedDebuff total = new TimedDebuff();
            foreach (var config in _buffConfigs.Where(c => !c.IsBuff && c.SkillName == null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;
                var btn = GetBuffButton(config, prefix);
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
                var character = CharacterDb.GetByName(config.CharacterName);
                // 조건부 디버프는 턴제
                var debuff = character?.Passive?.GetConditionalDebuff(isEnhanced, transcendLevel);
                if (debuff != null) total.MaxMerge(debuff);
            }
            return total;
        }

        /// <summary>
        /// 액티브 스킬 디버프 수집 (TimedDebuff끼리 MaxMerge)
        /// </summary>
        private TimedDebuff GetAllActiveDebuffs(string prefix)
        {
            TimedDebuff total = new TimedDebuff();
            foreach (var config in _buffConfigs.Where(c => !c.IsBuff && c.SkillName != null))
            {
                var chk = GetBuffCheckBox(config, prefix);
                if (chk?.IsChecked != true) continue;
                var btn = GetBuffButton(config, prefix);
                if (btn == null)
                {
                    var otherConfig = _buffConfigs.FirstOrDefault(c => c.CharacterName == config.CharacterName && GetBuffButton(c, prefix) != null);
                    if (otherConfig != null) btn = GetBuffButton(otherConfig, prefix);
                }
                var (isEnhanced, transcendLevel) = GetBuffOption(btn, prefix);
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

        /// <summary>
        /// 전체 디버프 합산 (상시 + 턴제 + 펫)
        /// 상시끼리 MaxMerge, 턴제끼리 MaxMerge, 그 결과를 Add
        /// </summary>
        private DebuffSet GetTotalDebuffs(string prefix)
        {
            // 상시 디버프 (패시브 상시끼리 MaxMerge)
            PermanentDebuff permanentDebuffs = GetAllPermanentPassiveDebuffs(prefix);

            // 턴제 디버프 (패시브 조건부 + 액티브 스킬 끼리 MaxMerge)
            TimedDebuff timedDebuffs = new TimedDebuff();
            timedDebuffs.MaxMerge(GetAllTimedPassiveDebuffs(prefix));
            timedDebuffs.MaxMerge(GetAllActiveDebuffs(prefix));

            // 펫 디버프 (별도 합산 - 영웅 디버프와 중첩 가능)
            DebuffSet petDebuffs = GetPetSkillDebuff(prefix);

            // 최종 합산
            DebuffSet total = new DebuffSet();
            total.Add(permanentDebuffs);  // 상시 (이미 MaxMerge됨)
            total.Add(timedDebuffs);       // 턴제 (이미 MaxMerge됨)
            total.Add(petDebuffs);         // 펫 (별도 합산)

            return total;
        }

        // 기존 메서드 유지 (하위 호환)
        private DebuffSet GetAllPassiveDebuffs(string prefix)
        {
            return GetAllPermanentPassiveDebuffs(prefix);
        }

        /// <summary>
        /// 펫 스킬 버프 가져오기
        /// </summary>
        private BuffSet GetPetSkillBuff(string prefix)
        {
            return _statHelper.GetPetSkillBuff(prefix);
        }

        /// <summary>
        /// 펫 스킬 디버프 가져오기
        /// </summary>
        private DebuffSet GetPetSkillDebuff(string prefix)
        {
            return _statHelper.GetPetSkillDebuff(prefix);
        }

        #endregion

        #region 스탯 계산

        private void RecalculateStats(string prefix)
        {
            if (!_isInitialized) return;

            var stats = _statHelper.CalculateAllStats(prefix);
            var charInfo = _statHelper.GetCharacterInfo(prefix);

            // 본인 패시브 버프 - 새 로직: 상시/턴제 분리
            BuffSet characterPassiveBuff = new BuffSet();
            if (charInfo?.Character?.Passive != null)
            {
                // 상시 자버프
                var permanentBuff = charInfo.Character.Passive.GetTotalSelfBuff(
                    charInfo.IsSkillEnhanced, charInfo.TranscendLevel);
                if (permanentBuff != null) characterPassiveBuff.Add(permanentBuff);
                
                // 턴제 자버프 (조건 충족 시)
                if (charInfo.IsPassiveConditionMet)
                {
                    var timedBuff = charInfo.Character.Passive.GetConditionalSelfBuff(
                        charInfo.IsSkillEnhanced, charInfo.TranscendLevel);
                    if (timedBuff != null) characterPassiveBuff.Add(timedBuff);
                }
            }

            // 파티 버프 - 새 로직: 상시/턴제 타입별 MaxMerge
            BuffSet totalBuffs = GetTotalBuffs(prefix);

            // 펫 옵션
            var petRates = _statHelper.GetPetOptionRates(prefix);

            // 버프% 합계
            double buffAtkRate = petRates.AtkRate + totalBuffs.Atk_Rate + characterPassiveBuff.Atk_Rate;
            double buffDefRate = petRates.DefRate + totalBuffs.Def_Rate + characterPassiveBuff.Def_Rate;
            double buffHpRate = petRates.HpRate + totalBuffs.Hp_Rate + characterPassiveBuff.Hp_Rate;

            // 최종 스탯
            double totalAtk = stats.BaseStatAtk * (1 + buffAtkRate / 100.0);
            double totalDef = stats.BaseStatDef * (1 + buffDefRate / 100.0);
            double totalHp = stats.BaseStatHp * (1 + buffHpRate / 100.0);

            // UI 업데이트
            UpdateStatUI(prefix, stats, totalAtk, totalDef, totalHp, charInfo, characterPassiveBuff, totalBuffs);
        }

        private void UpdateStatUI(string prefix, CalculatedStats stats, double totalAtk, double totalDef, double totalHp,
            CharacterInfo charInfo, BuffSet characterPassiveBuff, BuffSet totalBuffs)
        {
            // 기본 공/방/생
            SetTextBoxText($"txt{prefix}StatAtk", totalAtk.ToString("N0"));
            SetTextBoxText($"txt{prefix}StatDef", totalDef.ToString("N0"));
            SetTextBoxText($"txt{prefix}StatHp", totalHp.ToString("N0"));
            SetTextBoxText($"txt{prefix}StatAtkBase", stats.PureBaseAtk.ToString("N0"));
            SetTextBoxText($"txt{prefix}StatDefBase", stats.PureBaseDef.ToString("N0"));
            SetTextBoxText($"txt{prefix}StatHpBase", stats.PureBaseHp.ToString("N0"));

            // 기타 스탯
            var charStats = stats.CharacterStats;
            var transcendStats = stats.TranscendStats;
            var setBonus = stats.SetBonus;
            var subStats = stats.SubStats;
            var mainOptionStats = stats.MainOptionStats;
            var accessoryStats = stats.AccessoryStats;

            double cri = charStats.Cri + transcendStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri + characterPassiveBuff.Cri + totalBuffs.Cri;
            double criDmg = charStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + characterPassiveBuff.Cri_Dmg;
            double wek = charStats.Wek + transcendStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek + totalBuffs.Wek + characterPassiveBuff.Wek;
            double wekDmg = charStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg + totalBuffs.Wek_Dmg + characterPassiveBuff.Wek_Dmg;
            double dmgDealt = charStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt + characterPassiveBuff.Dmg_Dealt;
            double armPen = charStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen + totalBuffs.Arm_Pen + characterPassiveBuff.Arm_Pen;
            double dmgRdc = charStats.Dmg_Rdc + transcendStats.Dmg_Rdc + setBonus.Dmg_Rdc + mainOptionStats.Dmg_Rdc + totalBuffs.Dmg_Rdc + characterPassiveBuff.Dmg_Rdc;
            double blk = charStats.Blk + transcendStats.Blk + setBonus.Blk + subStats.Blk + mainOptionStats.Blk + accessoryStats.Blk;
            double effHit = charStats.Eff_Hit + transcendStats.Eff_Hit + setBonus.Eff_Hit + subStats.Eff_Hit + mainOptionStats.Eff_Hit + accessoryStats.Eff_Hit + totalBuffs.Eff_Hit + characterPassiveBuff.Eff_Hit;
            double effRes = charStats.Eff_Res + transcendStats.Eff_Res + setBonus.Eff_Res + subStats.Eff_Res + mainOptionStats.Eff_Res + accessoryStats.Eff_Res + totalBuffs.Eff_Res + characterPassiveBuff.Eff_Res;

            SetTextBoxText($"txt{prefix}StatCri", $"{cri}%");
            SetTextBoxText($"txt{prefix}StatCriDmg", $"{criDmg}%");
            SetTextBoxText($"txt{prefix}StatWek", $"{wek}%");
            SetTextBoxText($"txt{prefix}StatWekDmg", $"{wekDmg}%");
            SetTextBoxText($"txt{prefix}StatDmgDealt", $"{dmgDealt}%");
            SetTextBoxText($"txt{prefix}StatArmPen", $"{armPen}%");
            SetTextBoxText($"txt{prefix}StatDmgRdc", $"{dmgRdc}%");
            SetTextBoxText($"txt{prefix}StatBlk", $"{blk}%");
            SetTextBoxText($"txt{prefix}StatEffHit", $"{effHit}%");
            SetTextBoxText($"txt{prefix}StatEffRes", $"{effRes}%");
            SetTextBoxText($"txt{prefix}StatSpd", stats.TotalSpd.ToString("N0"));
        }

        private void SetTextBoxText(string name, string text)
        {
            var textBox = FindName(name) as TextBox;
            if (textBox != null) textBox.Text = text;
        }

        #endregion

        #region 버튼 이벤트

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            // 스탯 계산
            var myCalcStats = _statHelper.CalculateAllStats("My");
            var enemyCalcStats = _statHelper.CalculateAllStats("Enemy");
            var myCharInfo = _statHelper.GetCharacterInfo("My");
            var enemyCharInfo = _statHelper.GetCharacterInfo("Enemy");

            // 버프 합산 (내 쪽) - 새 로직: 상시/턴제 타입별 MaxMerge
            BuffSet myTotalBuffs = GetTotalBuffs("My");
            BuffSet myCharPassiveBuff = new BuffSet();
            if (myCharInfo?.Character?.Passive != null)
            {
                // 상시 자버프
                var permanentBuff = myCharInfo.Character.Passive.GetTotalSelfBuff(
                    myCharInfo.IsSkillEnhanced, myCharInfo.TranscendLevel);
                if (permanentBuff != null) myCharPassiveBuff.Add(permanentBuff);
                
                // 턴제 자버프 (조건 충족 시)
                if (myCharInfo.IsPassiveConditionMet)
                {
                    var timedBuff = myCharInfo.Character.Passive.GetConditionalSelfBuff(
                        myCharInfo.IsSkillEnhanced, myCharInfo.TranscendLevel);
                    if (timedBuff != null) myCharPassiveBuff.Add(timedBuff);
                }
            }

            // 버프 합산 (적 쪽) - 새 로직: 상시/턴제 타입별 MaxMerge
            BuffSet enemyTotalBuffs = GetTotalBuffs("Enemy");
            BuffSet enemyCharPassiveBuff = new BuffSet();
            if (enemyCharInfo?.Character?.Passive != null)
            {
                // 상시 자버프
                var permanentBuff = enemyCharInfo.Character.Passive.GetTotalSelfBuff(
                    enemyCharInfo.IsSkillEnhanced, enemyCharInfo.TranscendLevel);
                if (permanentBuff != null) enemyCharPassiveBuff.Add(permanentBuff);
                
                // 턴제 자버프 (조건 충족 시)
                if (enemyCharInfo.IsPassiveConditionMet)
                {
                    var timedBuff = enemyCharInfo.Character.Passive.GetConditionalSelfBuff(
                        enemyCharInfo.IsSkillEnhanced, enemyCharInfo.TranscendLevel);
                    if (timedBuff != null) enemyCharPassiveBuff.Add(timedBuff);
                }
            }

            // 디버프 합산 (내가 적에게 건 디버프) - 새 로직
            DebuffSet myTotalDebuffs = GetTotalDebuffs("My");

            // 디버프 합산 (적이 나에게 건 디버프) - 새 로직
            DebuffSet enemyTotalDebuffs = GetTotalDebuffs("Enemy");

            // 스탯 조합
            var myStats = BuildPvpStats("My", myCalcStats, myTotalBuffs, myCharPassiveBuff);
            var enemyStats = BuildPvpStats("Enemy", enemyCalcStats, enemyTotalBuffs, enemyCharPassiveBuff);

            // 스킬 가져오기
            Skill mySkill = null;
            Skill enemySkill = null;
            if (myCharInfo?.Character != null && cboMySkill.SelectedIndex > 0)
                mySkill = myCharInfo.Character.Skills?.ElementAtOrDefault(cboMySkill.SelectedIndex - 1);
            if (enemyCharInfo?.Character != null && cboEnemySkill.SelectedIndex > 0)
                enemySkill = enemyCharInfo.Character.Skills?.ElementAtOrDefault(cboEnemySkill.SelectedIndex - 1);

            // PvpInput 구성
            var input = new PvpInput
            {
                MyStats = myStats,
                EnemyStats = enemyStats,
                MySkill = mySkill,
                EnemySkill = enemySkill,
                MySkillEnhanced = chkMySkillEnhanced.IsChecked == true,
                EnemySkillEnhanced = chkEnemySkillEnhanced.IsChecked == true,
                MyTranscendLevel = cboMyTranscend.SelectedIndex,
                EnemyTranscendLevel = cboEnemyTranscend.SelectedIndex,
                MyOptions = new PvpCombatOptions
                {
                    IsCritical = chkMyCritical.IsChecked == true,
                    IsWeakpoint = chkMyWeakpoint.IsChecked == true,
                    IsBlocked = chkMyBlock.IsChecked == true
                },
                EnemyOptions = new PvpCombatOptions
                {
                    IsCritical = chkEnemyCritical.IsChecked == true,
                    IsWeakpoint = chkEnemyWeakpoint.IsChecked == true,
                    IsBlocked = chkEnemyBlock.IsChecked == true
                },
                MyDebuffsOnEnemy = new PvpDebuffs
                {
                    DefReduction = myTotalDebuffs.Def_Reduction,
                    DmgTakenIncrease = myTotalDebuffs.Dmg_Taken_Increase,
                    Vulnerability = myTotalDebuffs.Vulnerability,
                    EffResReduction = myTotalDebuffs.Eff_Red,
                    CriDmgReduction = myTotalDebuffs.Cri_Dmg_Reduction
                },
                EnemyDebuffsOnMe = new PvpDebuffs
                {
                    DefReduction = enemyTotalDebuffs.Def_Reduction,
                    DmgTakenIncrease = enemyTotalDebuffs.Dmg_Taken_Increase,
                    Vulnerability = enemyTotalDebuffs.Vulnerability,
                    EffResReduction = enemyTotalDebuffs.Eff_Red,
                    CriDmgReduction = enemyTotalDebuffs.Cri_Dmg_Reduction
                }
            };

            // 계산 실행
            var result = _calculator.Calculate(input);

            // 결과 표시
            txtMyDamageResult.Text = result.MyDamageToEnemy.FinalDamage.ToString("N0");
            txtEnemyDamageResult.Text = result.EnemyDamageToMe.FinalDamage.ToString("N0");

            // 상세 정보
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══ 내가 적에게 주는 피해 ═══");
            sb.AppendLine(result.MyDamageToEnemy.Details ?? "스킬 미선택");
            sb.AppendLine();
            sb.AppendLine("═══ 적이 나에게 주는 피해 ═══");
            sb.AppendLine(result.EnemyDamageToMe.Details ?? "스킬 미선택");
            txtDetailResult.Text = sb.ToString();
        }

        /// <summary>
        /// PvpCharacterStats 빌드
        /// </summary>
        private PvpCharacterStats BuildPvpStats(string prefix, CalculatedStats stats, BuffSet totalBuffs, BuffSet charPassiveBuff)
        {
            var charInfo = _statHelper.GetCharacterInfo(prefix);
            var charStats = stats.CharacterStats;
            var transcendStats = stats.TranscendStats;
            var setBonus = stats.SetBonus;
            var subStats = stats.SubStats;
            var mainOptionStats = stats.MainOptionStats;
            var accessoryStats = stats.AccessoryStats;
            var petRates = _statHelper.GetPetOptionRates(prefix);  // 직접 호출

            // 버프% 합계
            double buffAtkRate = petRates.AtkRate + totalBuffs.Atk_Rate + charPassiveBuff.Atk_Rate;
            double buffDefRate = petRates.DefRate + totalBuffs.Def_Rate + charPassiveBuff.Def_Rate;

            // 최종 공/방/생
            double totalAtk = stats.BaseStatAtk * (1 + buffAtkRate / 100.0);
            double totalDef = stats.BaseStatDef * (1 + buffDefRate / 100.0);
            double totalHp = stats.BaseStatHp * (1 + (petRates.HpRate + totalBuffs.Hp_Rate + charPassiveBuff.Hp_Rate) / 100.0);

            // 스탯 합산
            double cri = charStats.Cri + transcendStats.Cri + setBonus.Cri + subStats.Cri + mainOptionStats.Cri + accessoryStats.Cri + charPassiveBuff.Cri + totalBuffs.Cri;
            double criDmg = charStats.Cri_Dmg + transcendStats.Cri_Dmg + setBonus.Cri_Dmg + subStats.Cri_Dmg + mainOptionStats.Cri_Dmg + accessoryStats.Cri_Dmg + totalBuffs.Cri_Dmg + charPassiveBuff.Cri_Dmg;
            double wek = charStats.Wek + transcendStats.Wek + setBonus.Wek + subStats.Wek + mainOptionStats.Wek + accessoryStats.Wek + totalBuffs.Wek + charPassiveBuff.Wek;
            double wekDmg = charStats.Wek_Dmg + transcendStats.Wek_Dmg + setBonus.Wek_Dmg + totalBuffs.Wek_Dmg + charPassiveBuff.Wek_Dmg;
            double dmgDealt = charStats.Dmg_Dealt + transcendStats.Dmg_Dealt + setBonus.Dmg_Dealt + accessoryStats.Dmg_Dealt + totalBuffs.Dmg_Dealt + charPassiveBuff.Dmg_Dealt;
            double armPen = charStats.Arm_Pen + transcendStats.Arm_Pen + setBonus.Arm_Pen + totalBuffs.Arm_Pen + charPassiveBuff.Arm_Pen;
            double dmgRdc = charStats.Dmg_Rdc + transcendStats.Dmg_Rdc + setBonus.Dmg_Rdc + mainOptionStats.Dmg_Rdc + totalBuffs.Dmg_Rdc + charPassiveBuff.Dmg_Rdc;
            double blk = charStats.Blk + transcendStats.Blk + setBonus.Blk + subStats.Blk + mainOptionStats.Blk + accessoryStats.Blk;
            double effHit = charStats.Eff_Hit + transcendStats.Eff_Hit + setBonus.Eff_Hit + subStats.Eff_Hit + mainOptionStats.Eff_Hit + accessoryStats.Eff_Hit + totalBuffs.Eff_Hit + charPassiveBuff.Eff_Hit;
            double effRes = charStats.Eff_Res + transcendStats.Eff_Res + setBonus.Eff_Res + subStats.Eff_Res + mainOptionStats.Eff_Res + accessoryStats.Eff_Res + totalBuffs.Eff_Res + charPassiveBuff.Eff_Res;

            return new PvpCharacterStats
            {
                Atk = totalAtk,
                Def = totalDef,
                Hp = totalHp,
                CritRate = cri,
                CritDamage = criDmg,
                WeakRate = wek,
                WeakDamage = wekDmg,
                WeakDamageBuff = totalBuffs.Wek_Dmg,  // 약피 버프만 따로
                DamageDealt = dmgDealt,
                ArmorPen = armPen,
                DefIncrease = totalBuffs.Def_Rate + charPassiveBuff.Def_Rate,  // 방증 버프
                DamageReduction = dmgRdc,
                BlockRate = blk,
                EffHit = effHit,
                EffRes = effRes
            };
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _isInitialized = false;

            // My 초기화
            cboMyGrade.SelectedIndex = 0;
            cboMyType.SelectedIndex = 0;
            cboMyCharacter.SelectedIndex = 0;
            cboMyTranscend.SelectedIndex = 0;
            chkMySkillEnhanced.IsChecked = false;
            chkMyPassiveCondition.IsChecked = false;

            // Enemy 초기화
            cboEnemyGrade.SelectedIndex = 0;
            cboEnemyType.SelectedIndex = 0;
            cboEnemyCharacter.SelectedIndex = 0;
            cboEnemyTranscend.SelectedIndex = 0;
            chkEnemySkillEnhanced.IsChecked = false;
            chkEnemyPassiveCondition.IsChecked = false;

            // 버프/디버프 초기화
            foreach (var config in _buffConfigs)
            {
                var myChk = GetBuffCheckBox(config, "My");
                var enemyChk = GetBuffCheckBox(config, "Enemy");
                if (myChk != null) myChk.IsChecked = false;
                if (enemyChk != null) enemyChk.IsChecked = false;
            }

            // 결과 초기화
            txtMyDamageResult.Text = "0";
            txtEnemyDamageResult.Text = "0";
            txtDetailResult.Text = "전투 계산 버튼을 눌러 결과를 확인하세요.";

            _isInitialized = true;
            RecalculateStats("My");
            RecalculateStats("Enemy");
        }

        private void BtnBackToPve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region 버프/디버프 버튼 UI

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

                // prefix 판별해서 해당 쪽만 재계산
                string prefix = btn.Name.Contains("My") ? "My" : "Enemy";
                RecalculateStats(prefix);
            }
        }

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            
            // prefix 판별
            string prefix = "My";
            if (sender is FrameworkElement element && element.Name.Contains("Enemy"))
                prefix = "Enemy";
            
            RecalculateStats(prefix);
        }

        private void ResetBuffOptionButton(Button btn, string baseName)
        {
            btn.Tag = 0;
            btn.Background = BuffBgColors[0];
            btn.Foreground = BuffFgColors[0];
            btn.Content = baseName;
        }

        #endregion

        #region 장비/펫/진형 이벤트

        private void CboEquipSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void CboSetCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            
            var grid = FindName($"grid{prefix}EquipSet2") as Grid;
            var cboSetCount = FindName($"cbo{prefix}SetCount1") as ComboBox;
            if (grid != null && cboSetCount != null)
                grid.Visibility = cboSetCount.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            
            RecalculateStats(prefix);
        }

        private void MainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void Tier_MouseLeft(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Border border)
            {
                var textBox = border.Child as TextBox;
                if (textBox != null)
                {
                    int current = int.TryParse(textBox.Text, out int val) ? val : 0;
                    textBox.Text = Math.Min(current + 1, 20).ToString();
                    string prefix = textBox.Name.Contains("Enemy") ? "Enemy" : "My";
                    RecalculateStats(prefix);
                }
            }
        }

        private void Tier_MouseRight(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Border border)
            {
                var textBox = border.Child as TextBox;
                if (textBox != null)
                {
                    int current = int.TryParse(textBox.Text, out int val) ? val : 0;
                    textBox.Text = Math.Max(current - 1, 0).ToString();
                    string prefix = textBox.Name.Contains("Enemy") ? "Enemy" : "My";
                    RecalculateStats(prefix);
                }
            }
        }

        private static readonly string[] AccessoryGradeLabels = { "없음", "4성", "5성", "6성" };

        private void AccessoryGrade_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Button btn)
            {
                int current = int.TryParse(btn.Tag?.ToString(), out int val) ? val : 0;
                int next = (current + 1) % 4;
                btn.Tag = next;
                btn.Content = AccessoryGradeLabels[next];

                string prefix = btn.Name.Contains("Enemy") ? "Enemy" : "My";
                
                // 6성일 때 부옵션 표시
                var cboSub = FindName($"cbo{prefix}AccessorySub") as ComboBox;
                var txtSubValue = FindName($"txt{prefix}AccessorySubValue") as TextBlock;
                if (cboSub != null) cboSub.Visibility = next == 3 ? Visibility.Visible : Visibility.Collapsed;
                if (txtSubValue != null) txtSubValue.Visibility = next == 3 ? Visibility.Visible : Visibility.Collapsed;

                RecalculateStats(prefix);
            }
        }

        private void CboAccessoryOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void CboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void CboPetFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            UpdatePetList(prefix);
        }

        private void UpdatePetList(string prefix)
        {
            var cboPet = FindName($"cbo{prefix}Pet") as ComboBox;
            var cboPetStar = FindName($"cbo{prefix}PetStar") as ComboBox;
            var cboPetRarity = FindName($"cbo{prefix}PetRarity") as ComboBox;
            
            if (cboPet == null) return;
            cboPet.Items.Clear();
            cboPet.Items.Add("없음");

            string starFilter = (cboPetStar?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";
            string rarityFilter = (cboPetRarity?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";

            // 성급 문자열 → 숫자 변환
            int starValue = 0;
            if (starFilter == "4성") starValue = 4;
            else if (starFilter == "5성") starValue = 5;
            else if (starFilter == "6성") starValue = 6;

            if (PetDb.Pets != null)
            {
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
            }
            cboPet.SelectedIndex = 0;
        }

        private void CboPet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void PetOption_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void CboFormation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        private void FormationPosition_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string prefix = btn.Name.Contains("Enemy") ? "Enemy" : "My";
                ToggleFormationPosition(prefix);
            }
        }

        private void Potential_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            string prefix = (sender as FrameworkElement)?.Name?.Contains("Enemy") == true ? "Enemy" : "My";
            RecalculateStats(prefix);
        }

        #endregion
    }
}
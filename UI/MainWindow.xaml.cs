using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        #region 필드 및 생성자

        private bool _isInitialized = false;
        private readonly DamageCalculator _calculator;
        private readonly StatCalculator _statCalculator;
        private readonly BuffCalculator _buffCalculator;
        private Formation _currentFormation = new Formation();
        private PresetManager _presetManager;
        private DebuffSet _currentDebuffs = new DebuffSet();
        private Accessory _currentAccessory = new Accessory();

        // 셋팅 슬롯 관리 (2셋팅 전환용)
        private Preset[] _characterSlots = new Preset[2];
        private int _currentSlotIndex = 0;

        // 각 셋팅의 최대 데미지 저장 (비교용)
        private double[] _slotMaxDamages = new double[2];
        private string[] _slotCharacterNames = new string[2];

        // MainWindow.xaml.cs
        public ObservableCollection<BuffConfig> BuffConfigs { get; private set; }

        public IEnumerable<BuffConfig> PassiveBuffs => 
            BuffConfigs.Where(c => c.IsBuff && c.SkillName == null)
                       .OrderBy(c => !string.IsNullOrEmpty(c.GroupKey) ? 1 : 0);

        public IEnumerable<BuffConfig> ActiveBuffs => 
            BuffConfigs.Where(c => c.IsBuff && c.SkillName != null)
                       .OrderBy(c => !string.IsNullOrEmpty(c.GroupKey) ? 1 : 0);

        public IEnumerable<BuffConfig> PassiveDebuffs => 
            BuffConfigs.Where(c => !c.IsBuff && c.SkillName == null)
                       .OrderBy(c => !string.IsNullOrEmpty(c.GroupKey) ? 1 : 0);

        public IEnumerable<BuffConfig> ActiveDebuffs => 
            BuffConfigs.Where(c => !c.IsBuff && c.SkillName != null)
                       .OrderBy(c => !string.IsNullOrEmpty(c.GroupKey) ? 1 : 0);

        

        public ObservableCollection<Equipment> Equipments { get; private set; }
        public List<string> AllSubStatNames => SubOptionDb.AllStatNames;
    

        // 버프 컨트롤 동적 접근 헬퍼
        private CheckBox GetBuffCheckBox(BuffConfig config, string prefix = "My")
        {
            return FindName($"chk{prefix}{config.Key}") as CheckBox;
        }

        private Button GetBuffButton(BuffConfig config, string prefix = "My")
        {
            return FindName($"btn{prefix}{config.Key}") as Button;
        }

        public MainWindow()
        {
            InitializeComponent();  // ✅ 반드시 첫 번째!
            InitializeEquipments();
            _calculator = new DamageCalculator();
            _statCalculator = new StatCalculator();
            _buffCalculator = new BuffCalculator();
            InitializeComboBoxes();  // 이 시점에 컨트롤들이 준비되어 있어야 함
            InitializeBuffConfigs();
            DataContext = this;
            _presetManager = new PresetManager();
            RefreshPresetList();
            _isInitialized = true;
            RecalculateStats();
        }

        #endregion

        #region 버프 설정 초기화

        private void InitializeBuffConfigs()
        {
            BuffConfigs = new ObservableCollection<BuffConfig>
            {
                // ==================== 지속 버프 ====================
                new BuffConfig { Key = "BuffPassiveYeonhee", BaseName = "연희", CharacterName = "연희", Label = "(마공증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDazy", BaseName = "데이지", CharacterName = "데이지", Label = "(마공증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKiriel", BaseName = "키리엘", CharacterName = "키리엘", Label = "(마피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveAriel", BaseName = "아리엘", CharacterName = "아리엘", Label = "(마피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveAilin", BaseName = "아일린", CharacterName = "아일린", Label = "(물공증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveDelonz", BaseName = "델론즈", CharacterName = "델론즈", Label = "(물피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLina", BaseName = "리나", CharacterName = "리나", Label = "(치피 @2초)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKagura", BaseName = "카구라", CharacterName = "카구라", Label = "(치피증/물피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveOrly", BaseName = "오를리", CharacterName = "오를리", Label = "(치확/치피)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveMiho", BaseName = "미호", CharacterName = "미호", Label = "(약피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveLion", BaseName = "라이언", CharacterName = "라이언", Label = "(1-3인기 피증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRachel", BaseName = "레이첼", CharacterName = "레이첼", Label = "(약공확)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveKarma", BaseName = "카르마", CharacterName = "카르마", Label = "(피증,댐감)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRudi", BaseName = "루디", CharacterName = "루디", Label = "(방증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRook", BaseName = "룩", CharacterName = "룩", Label = "(막기확률)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveColt", BaseName = "콜트", CharacterName = "콜트", Label = "(효과적중)", IsBuff = true },
                new BuffConfig { Key = "BuffPassivePreiya", BaseName = "프레이야", CharacterName = "프레이야", Label = "(효과적중)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveYushin", BaseName = "유신", CharacterName = "유신", Label = "(효과적중)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveRozy", BaseName = "로지", CharacterName = "로지", Label = "(효과적중(만/방))", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveSpike", BaseName = "스파이크", CharacterName = "스파이크", Label = "(효적,효저)", IsBuff = true },

                // ==================== 턴제 버프 ====================
                new BuffConfig { Key = "BuffActiveDazy", BaseName = "데이지", CharacterName = "데이지", SkillName = "불나비", Label = "(마공증)", IsBuff = true },
                new BuffConfig { Key = "BuffPassiveZik", BaseName = "지크", CharacterName = "지크", Label = "(물공증)", IsBuff = true },  // 패시브 턴제버프
                new BuffConfig { Key = "BuffActiveYui", BaseName = "유이", CharacterName = "유이", SkillName = "축복의 선율", Label = "(물피증)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", SkillName = "장비 강화", Label = "(보피증, 약공확)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveLina", BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", Label = "(피증)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveGoku", BaseName = "손오공", CharacterName = "손오공", SkillName = "여의참난무", Label = "(댐감)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveRudi", BaseName = "루디", CharacterName = "루디", SkillName = "방어 준비", Label = "(댐감@스강)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveAkila", BaseName = "아킬라", CharacterName = "아킬라", SkillName = "칠흑의 장막", Label = "(댐감@스강)", IsBuff = true },
                new BuffConfig { Key = "BuffActiveAlice", BaseName = "엘리스", CharacterName = "엘리스", SkillName = "비밀의 문", Label = "(방증)", IsBuff = true },

                // ==================== 지속 디버프 ====================
                new BuffConfig { Key = "DebuffPassiveTaka", BaseName = "타카", CharacterName = "타카", Label = "(받피증, 취약)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveMilia", BaseName = "밀리아", CharacterName = "밀리아", Label = "(마취약)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveBanesa", BaseName = "바네사", CharacterName = "바네사", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAce", BaseName = "에이스", CharacterName = "에이스", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveElisia", BaseName = "엘리시아", CharacterName = "엘리시아", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAragon", BaseName = "아라곤", CharacterName = "아라곤", Label = "(공깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveChancellor", BaseName = "챈슬러", CharacterName = "챈슬러", Label = "(피감)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveMelkir", BaseName = "멜키르", CharacterName = "멜키르", Label = "(효저깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveAkila", BaseName = "아킬라", CharacterName = "아킬라", Label = "(효저깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffPassiveNox", BaseName = "녹스", CharacterName = "녹스", Label = "(효저깎@2초)", IsBuff = false },

                // ==================== 턴제 디버프 ====================
                new BuffConfig { Key = "DebuffActiveLina", BaseName = "리나", CharacterName = "리나", SkillName = "따뜻한 울림", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveJuri", BaseName = "쥬리", CharacterName = "쥬리", SkillName = "천상의 심판", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveOrly", BaseName = "오를리", CharacterName = "오를리", SkillName = "고결한 유성", Label = "(방깎@스강)", IsBuff = false },

                // 챈슬러 - 그룹 (버튼 1개 공유)
                new BuffConfig { Key = "DebuffActiveChancellorS1", BaseName = "챈슬러", CharacterName = "챈슬러", SkillName = "분쇄", Label = "(방깎)", GroupKey = "Chancellor", ShowButton = true, IsBuff = false },
                new BuffConfig { Key = "DebuffActiveChancellorS2", BaseName = "챈슬러", CharacterName = "챈슬러", SkillName = "대지 파괴", Label = "(공깎)", GroupKey = "Chancellor", ShowButton = false, IsBuff = false },

                new BuffConfig { Key = "DebuffActivePungyeon", BaseName = "풍연", CharacterName = "풍연", SkillName = "구음검격", Label = "(방깎@2초)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAriel", BaseName = "아리엘", CharacterName = "아리엘", SkillName = "눈부신 빛", Label = "(방깎)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveKagura", BaseName = "카구라", CharacterName = "카구라", SkillName = "해방-팔사검", Label = "(받회감/물취약@6초)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveMiho", BaseName = "미호", CharacterName = "미호", SkillName = "살율의 춤", Label = "(마취약)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveVellica", BaseName = "벨리카", CharacterName = "벨리카", SkillName = "어둠의 환영", Label = "(마취약)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveEspada", BaseName = "에스파다", CharacterName = "에스파다", SkillName = "정화탄", Label = "(마취약)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveBanesa", BaseName = "바네사", CharacterName = "바네사", SkillName = "메마른 해일", Label = "(마취약@6초)", IsBuff = false },

                // 레이첼 - 그룹 (버튼 1개 공유)
                new BuffConfig { Key = "DebuffActiveRachelFlame", BaseName = "레이첼", CharacterName = "레이첼", SkillName = "염화", Label = "(염화)", GroupKey = "Rachel", ShowButton = true, IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRachelPhoenix", BaseName = "레이첼", CharacterName = "레이첼", SkillName = "불새", Label = "(불새)", GroupKey = "Rachel", ShowButton = false, IsBuff = false },

                new BuffConfig { Key = "DebuffActiveAragon", BaseName = "아라곤", CharacterName = "아라곤", SkillName = "포격 지원", Label = "(치피감@2초)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveColt", BaseName = "콜트", CharacterName = "콜트", SkillName = "어때, 화려하지?", Label = "(공깎@스강)", IsBuff = false },
                new BuffConfig { Key = "DebuffActivePlaton", BaseName = "플라튼", CharacterName = "플라튼", SkillName = "평타", Label = "(공깎@평타)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveNoho", BaseName = "노호", CharacterName = "노호", SkillName = "파멸의 고서", Label = "(공깎@2초월)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveZik", BaseName = "지크", CharacterName = "지크", SkillName = "부숴버려!", Label = "(뎀감)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveBiscuit", BaseName = "비스킷", CharacterName = "비스킷", SkillName = "평타", Label = "(뎀감@평타)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveKarma", BaseName = "카르마", CharacterName = "카르마", SkillName = "평타", Label = "(뎀감@평타)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveRozy", BaseName = "로지", CharacterName = "로지", SkillName = "평타", Label = "(효저깎@평타)", IsBuff = false },
                new BuffConfig { Key = "DebuffActiveNox", BaseName = "녹스", CharacterName = "녹스", SkillName = "지옥의 일격", Label = "(효저깎)", IsBuff = false },

                // 유신 - 그룹 (버튼 1개 공유)
                new BuffConfig { Key = "DebuffActiveYushinFlat", BaseName = "유신", CharacterName = "유신", SkillName = "평타", Label = "(효저깎@평타)", GroupKey = "Yushin", ShowButton = true, IsBuff = false },
                new BuffConfig { Key = "DebuffActiveYushinS2", BaseName = "유신", CharacterName = "유신", SkillName = "번뇌", Label = "(받회감)", GroupKey = "Yushin", ShowButton = false, IsBuff = false },

                new BuffConfig { Key = "DebuffActiveMelkir", BaseName = "멜키르", CharacterName = "멜키르", SkillName = "금지된 실험", Label = "(받회감)", IsBuff = false },

                // 에이스 - 그룹 (버튼 1개 공유)
                new BuffConfig { Key = "DebuffActiveAceS1", BaseName = "에이스", CharacterName = "에이스", SkillName = "달빛 베기", Label = "(받회감@2초)", GroupKey = "Ace", ShowButton = true, IsBuff = false },
                new BuffConfig { Key = "DebuffActiveAceS2", BaseName = "에이스", CharacterName = "에이스", SkillName = "일도천화엽", Label = "(취약,막감)", GroupKey = "Ace", ShowButton = false, IsBuff = false },

                new BuffConfig { Key = "DebuffActiveGoku", BaseName = "손오공", CharacterName = "손오공", SkillName = "환.여의난참무", Label = "(막감)", IsBuff = false },
            };

            // 체크 변경 이벤트 연결
            foreach (var config in BuffConfigs)
            {
                config.CheckedChanged += (s, e) =>
                {
                    if (_isInitialized) RecalculateStats();
                };
            }

            // 그룹 Level 변경 시 같은 그룹 UI 업데이트
            BuffConfig.GroupLevelChanged += (groupKey, level) =>
            {
                foreach (var config in BuffConfigs.Where(c => c.GroupKey == groupKey))
                {
                    config.NotifyLevelChanged();
                }
                if (_isInitialized) RecalculateStats();
            };

            // 그룹 파트너 연결 (ShowButton=true인 아이템에 ShowButton=false인 파트너 연결)
            LinkGroupPartners();

            DataContext = this;
        }

        /// <summary>
        /// 같은 GroupKey를 가진 아이템들을 서로 연결
        /// </summary>
        private void LinkGroupPartners()
        {
            var groups = BuffConfigs
                .Where(c => !string.IsNullOrEmpty(c.GroupKey))
                .GroupBy(c => c.GroupKey);

            foreach (var group in groups)
            {
                var primary = group.FirstOrDefault(c => c.ShowButton);
                var secondary = group.FirstOrDefault(c => !c.ShowButton);

                if (primary != null && secondary != null)
                {
                    primary.GroupPartner = secondary;
                }
            }
        }

        
        #endregion

        #region 콤보박스 초기화

        private void InitializeComboBoxes()
        {
             UpdateCharacterList();

            // 초월 단계
            for (int i = 0; i <= 12; i++)
            {
                cboMyTranscend.Items.Add($"{i}초월");
            }
            cboMyTranscend.SelectedIndex = 0;

            // 장비 세트
            cboMyEquipSet1.Items.Add("없음");
            foreach (var setName in EquipmentDb.SetEffects.Keys)
            {
                cboMyEquipSet1.Items.Add(setName);
            }
            cboMyEquipSet1.SelectedIndex = 0;

            // 진형 목록
            cboMyFormation.Items.Add("없음");
            foreach (var formationName in StatTable.FormationDb.Formations.Keys)
            {
                cboMyFormation.Items.Add(formationName);
            }
            cboMyFormation.SelectedIndex = 0;

            InitializeAccessoryComboBoxes();

            // 보스 목록 초기화
            UpdateBossList();

            // 펫 목록
            cboMyPet.Items.Add("없음");
            foreach (var pet in PetDb.Pets)
            {
                cboMyPet.Items.Add(pet.Name);
            }
            cboMyPet.SelectedIndex = 0;

            // 잡몹 리스트
            cboMob.Items.Clear();
            cboMob.Items.Add("선택");
            foreach (var mob in BossDb.Mobs)
            {
                cboMob.Items.Add(mob.Name);
            }
            cboMob.SelectedIndex = 0;
        }
        private void InitializeAccessoryComboBoxes()
        {
            // 성급
            cboMyAccessoryGrade.Items.Add("없음");
            cboMyAccessoryGrade.Items.Add("4성");
            cboMyAccessoryGrade.Items.Add("5성");
            cboMyAccessoryGrade.Items.Add("6성");
            cboMyAccessoryGrade.SelectedIndex = 0;

            // 메인옵션
            var mainOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                      "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                      "1-3인기%", "4-5인기%" };
            foreach (var opt in mainOptions)
                cboMyAccessoryMain.Items.Add(opt);
            cboMyAccessoryMain.SelectedIndex = 0;

            // 부옵션 (6성만)
            var subOptions = new[] { "없음", "피증%", "방어력%", "생명력%", "치명타확률%", "막기%",
                                     "약점공격확률%", "효과적중%", "효과저항%", "보피증%",
                                     "1-3인기%", "4-5인기%" };
            foreach (var opt in subOptions)
                cboMyAccessorySub.Items.Add(opt);
            cboMyAccessorySub.SelectedIndex = 0;
        }
        private void FormationPosition_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string currentTag = btn.Tag.ToString();

            if (currentTag == "Back")
            {
                btn.Tag = "Front";
                btn.Content = "전열";
                rbMyBack.IsChecked = false;
                rbMyFront.IsChecked = true;
            }
            else
            {
                btn.Tag = "Back";
                btn.Content = "후열";
                rbMyBack.IsChecked = true;
                rbMyFront.IsChecked = false;
            }

            RecalculateStats();
        }

        #endregion

        #region 캐릭터/스킬 이벤트

        private void BossType_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateBossList();
        }

        private void CboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (cboMyCharacter.SelectedIndex <= 0) 
            {
                cboMySkill.Items.Clear();
                cboMySkill.Items.Add("직접 입력하거나 골라주세요");
                cboMySkill.SelectedIndex = 0;
                RecalculateStats();
                return;
            }

            string charName = cboMyCharacter.SelectedItem.ToString();
            var character = CharacterDb.GetByName(charName);
            
            if (character != null)
            {
                cboMySkill.Items.Clear();
                foreach (var skill in character.Skills)
                {
                    cboMySkill.Items.Add(skill.Name);
                }
                if (cboMySkill.Items.Count > 0)
                {
                    cboMySkill.SelectedIndex = 0;
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

        private void CboAccessoryGrade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
        
            // 6성이면 부옵션 표시, 아니면 숨김
            if (cboMyAccessoryGrade.SelectedIndex == 3)  // 6성
            {
                panelMyAccessorySub.Visibility = Visibility.Visible;
            }
            else
            {
                panelMyAccessorySub.Visibility = Visibility.Collapsed;
                cboMyAccessorySub.SelectedIndex = 0;  // 부옵션 초기화
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
            int grade = cboMyAccessoryGrade.SelectedIndex > 0 
                ? cboMyAccessoryGrade.SelectedIndex + 3 : 0;

            string mainOpt = cboMyAccessoryMain.SelectedIndex > 0 
                ? cboMyAccessoryMain.SelectedItem.ToString() : null;
            string subOpt = cboMyAccessorySub.SelectedIndex > 0 
                ? cboMyAccessorySub.SelectedItem.ToString() : null;

            txtMyAccessoryMainValue.Text = Accessory.GetOptionDisplayValue(grade, mainOpt, true);
            txtMyAccessorySubValue.Text = grade == 6 
                ? Accessory.GetOptionDisplayValue(grade, subOpt, false) : "";
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
            else if (rbDescend.IsChecked == true)  // ✅ 강림 추가
            {
                boss = BossDb.ForestBosses.FirstOrDefault(b => selected.Contains(b.Name) && selected.Contains($"{b.Difficulty}단계"));
            }
            else if (rbGrowthDungeon.IsChecked == true)  // ✅ 성장던전 추가
            {
                boss = BossDb.GrowthDungeonBosses.FirstOrDefault(b => selected.Contains(b.Name));
            }

            if (boss != null)
            {
                txtBossDef.Text = boss.Stats.Def.ToString("N0");
                txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0");
                txtBossDmgRdc.Text = "0";
                txtBossHp.Text = boss.Stats.Hp.ToString("N0");
        
                txtBoss1TargetRdc.Text = boss.SingleTargetReduction.ToString("F0");
                txtBoss3TargetRdc.Text = boss.TripleTargetReduction.ToString("F0");
                txtBoss5TargetRdc.Text = boss.MultiTargetReduction.ToString("F0");
        
                // 조건부 방증 처리
                if (boss.IsStackableDefenseIncrease)
                {
                    // 스택형 방증 (카르마 등)
                    panelBossCondition.Visibility = Visibility.Collapsed;
                    panelBossStack.Visibility = Visibility.Visible;
                    btnBossStack.Content = "0";
                    btnBossStack.Tag = boss.MaxDefenseStack;  // 최대 스택 저장
                    txtBossStackInfo.Text = $"/ {boss.MaxDefenseStack}";
                    txtBossDefInc.Text = "0";  // 초기 스택 0
                }
                else if (!string.IsNullOrEmpty(boss.DefenseIncreaseCondition))
                {
                    // 기존 체력조건 방증
                    panelBossCondition.Visibility = Visibility.Visible;
                    panelBossStack.Visibility = Visibility.Collapsed;
                    txtBossCondition.Text = boss.DefenseIncreaseCondition;
                    chkBossCondition.IsChecked = false;
                    txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0");
                }
                else
                {
                    panelBossCondition.Visibility = Visibility.Collapsed;
                    panelBossStack.Visibility = Visibility.Collapsed;
                    txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0");
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
                    txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0");
                    txtBossHp.Text = boss.Stats.Hp.ToString("N0");
                }
            }
        }

        /// <summary>
        /// 스택 버튼 좌클릭 - 스택 감소
        /// </summary>
        private void BossStack_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isInitialized) return;

            int currentStack = int.Parse(btnBossStack.Content.ToString());
            if (currentStack > 0)
            {
                currentStack--;
                btnBossStack.Content = currentStack.ToString();
                UpdateBossStackDefense(currentStack);
            }
        }

        /// <summary>
        /// 스택 버튼 우클릭 - 스택 증가
        /// </summary>
        private void BossStack_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isInitialized) return;

            int maxStack = btnBossStack.Tag != null ? (int)btnBossStack.Tag : 8;
            int currentStack = int.Parse(btnBossStack.Content.ToString());
            
            if (currentStack < maxStack)
            {
                currentStack++;
                btnBossStack.Content = currentStack.ToString();
                UpdateBossStackDefense(currentStack);
            }
            e.Handled = true;
        }

        /// <summary>
        /// 스택에 따른 방증% 업데이트
        /// </summary>
        private void UpdateBossStackDefense(int stack)
        {
            var boss = GetSelectedBoss();
            if (boss != null && boss.IsStackableDefenseIncrease)
            {
                double totalDefIncrease = boss.DefenseIncrease * stack;
                txtBossDefInc.Text = totalDefIncrease.ToString("F0");
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 캐릭터 확인
                if (cboMyCharacter.SelectedIndex <= 0)
                {
                    txtResult.Text = "캐릭터를 선택해주세요.";
                    return;
                }

                var character = CharacterDb.GetByName(cboMyCharacter.SelectedItem.ToString());
                if (character == null)
                {
                    txtResult.Text = "캐릭터 정보를 찾을 수 없습니다.";
                    return;
                }

                // 스킬 확인
                Skill selectedSkill = null;
                if (cboMySkill.SelectedIndex >= 0 && cboMySkill.Items.Count > 0)
                {
                    string skillName = cboMySkill.SelectedItem?.ToString();
                    selectedSkill = character.Skills.FirstOrDefault(s => s.Name == skillName);
                }

                if (selectedSkill == null)
                {
                    txtResult.Text = "스킬을 선택해주세요.";
                    return;
                }

                // 버프 합산 - 새 로직
                Pet pet = cboMyPet.SelectedIndex > 0
                    ? PetDb.GetByName(cboMyPet.SelectedItem.ToString()) : null;
                int petStar = GetPetStar();
                BuffSet totalBuffs = _buffCalculator.CalculateTotalBuffs(BuffConfigs, pet, petStar);
                double weakDmgBuff = totalBuffs.Wek_Dmg;

                BattleMode mode = BattleMode.Boss;
                if (rbMob.IsChecked == true) mode = BattleMode.Mob;

                // 버프% 계산
                double buffAtkRate = GetPetOptionAtkRate() + totalBuffs.Atk_Rate;

                if (cboMyCharacter.SelectedIndex > 0)
                {
                    var charForBuff = CharacterDb.GetByName(cboMyCharacter.SelectedItem.ToString());
                    if (charForBuff?.Passive != null)
                    {
                        bool isEnhanced = chkMySkillEnhanced.IsChecked == true;
                        int transcendLevel = cboMyTranscend.SelectedIndex;

                        // 상시 자버프
                        var permanentBuff = charForBuff.Passive.GetTotalSelfBuff(isEnhanced, transcendLevel);
                        if (permanentBuff != null)
                        {
                            buffAtkRate += permanentBuff.Atk_Rate;
                        }

                        // 턴제 자버프 (조건 충족 시)
                        bool isConditionMet = chkMyPassiveCondition.IsChecked == true;
                        if (isConditionMet)
                        {
                            var timedBuff = charForBuff.Passive.GetConditionalSelfBuff(isEnhanced, transcendLevel);
                            if (timedBuff != null)
                            {
                                buffAtkRate += timedBuff.Atk_Rate;
                            }
                        }
                    }
                }

                // 기본 DamageInput 생성
                var baseInput = new DamageCalculator.DamageInput
                {
                    // 캐릭터/스킬
                    Character = character,
                    Skill = selectedSkill,
                    IsSkillEnhanced = chkMySkillEnhanced.IsChecked == true,
                    TranscendLevel = cboMyTranscend.SelectedIndex,

                    // UI에서 계산된 최종 스탯
                    FinalAtk = ParseStatValue(txtMyStatAtk.Text),
                    FinalDef = ParseStatValue(txtMyStatDef.Text),
                    FinalHp = ParseStatValue(txtMyStatHp.Text),
                    CritDamage = ParseStatValue(txtMyStatCriDmg.Text),
                    DmgDealt = ParseStatValue(txtMyStatDmgDealt.Text),
                    DmgDealtType = ParseStatValue(txtMyStatDmgDealtType.Text),
                    DmgDealtBoss = ParseStatValue(txtMyStatBossDmg.Text),
                    ArmorPen = ParseStatValue(txtMyStatArmPen.Text),
                    WeakpointDmg = ParseStatValue(txtMyStatWekDmg.Text),
                    WeakpointDmgBuff = weakDmgBuff,
                    Dmg1to3 = ParseStatValue(txtMyStatDmg1to3.Text),
                    Dmg4to5 = ParseStatValue(txtMyStatDmg4to5.Text),

                    // 디버프
                    DefReduction = _currentDebuffs.Def_Reduction,
                    DmgTakenIncrease = _currentDebuffs.Dmg_Taken_Increase,
                    Vulnerability = _currentDebuffs.Vulnerability,
                    BossVulnerability = _currentDebuffs.Boss_Vulnerability,
                    HealReduction = _currentDebuffs.Heal_Reduction,
                    EffResReduction = _currentDebuffs.Eff_Red,

                    // 보스 정보
                    BossDef = ParseDouble(txtBossDef.Text),
                    BossDefIncrease = ParseDouble(txtBossDefInc.Text),
                    BossDmgReduction = ParseDouble(txtBossDmgRdc.Text),
                    BossTargetReduction = GetSelectedTargetReduction(),
                    TargetHp = ParseDouble(txtBossHp.Text),

                    // 전투 옵션 - 빠른 비교에서는 4가지 시나리오로 계산
                    IsCritical = true,
                    IsWeakpoint = true,
                    IsBlocked = chkMyBlock.IsChecked == true,
                    TargetStackCount = int.TryParse(txtMyTargetStackCount.Text, out int stacks) ? stacks : 0,
                    ForceStatusEffect = chkMyStatusEffect.IsChecked == true,
                    IsLostHpConditionMet = chkMyLostHpCondition.IsChecked == true,

                    // 조건
                    IsSkillConditionMet = chkMySkillCondition.IsChecked == true,
                    AtkBuff = buffAtkRate,

                    // 자버프 타입피증 (스택소모 스킬용 - 자동 설정)
                    SelfBuffTypeDmg = GetSelfBuffTypeDmg(character, chkMySkillEnhanced.IsChecked == true),

                    Mode = mode
                };

                // ===== 빠른 비교: 4가지 시나리오 계산 =====
                // 1. 치명 + 약점 (디버그 파일 출력)
                baseInput.IsCritical = true;
                baseInput.IsWeakpoint = true;
                var resultCritWek = _calculator.Calculate(baseInput, writeDebugFile: true);

                // 2. 치명만
                baseInput.IsCritical = true;
                baseInput.IsWeakpoint = false;
                var resultCritOnly = _calculator.Calculate(baseInput);

                // 3. 약점만
                baseInput.IsCritical = false;
                baseInput.IsWeakpoint = true;
                var resultWekOnly = _calculator.Calculate(baseInput);

                // 4. 일반
                baseInput.IsCritical = false;
                baseInput.IsWeakpoint = false;
                var resultNormal = _calculator.Calculate(baseInput);

                // 현재 셋팅의 최대 데미지 저장 (치명+약점이 최대)
                _slotMaxDamages[_currentSlotIndex] = resultCritWek.FinalDamage;
                _slotCharacterNames[_currentSlotIndex] = character.Name;

                // 셋팅 비교 문자열 생성
                string comparisonHeader = GenerateSettingComparison();

                // 비교 결과 출력 (비교 헤더 + 상세 결과)
                txtResult.Text = comparisonHeader + _calculator.GenerateComparisonDetails(
                    resultCritWek, resultCritOnly, resultWekOnly, resultNormal, baseInput);

            }
            catch (Exception ex)
            {
                txtResult.Text = $"오류: {ex.Message}";
            }
        }

        /// <summary>
        /// 1번 셋팅과 2번 셋팅의 데미지 비교 문자열 생성
        /// </summary>
        private string GenerateSettingComparison()
        {
            // 둘 다 계산된 적이 없으면 빈 문자열
            if (_slotMaxDamages[0] == 0 && _slotMaxDamages[1] == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("         ⚔️  셋팅 비교  ⚔️");
            sb.AppendLine("═══════════════════════════════════════");

            // 1번 셋팅 정보
            string name1 = _slotCharacterNames[0] ?? "미설정";
            double dmg1 = _slotMaxDamages[0];
            string dmg1Str = dmg1 > 0 ? dmg1.ToString("N0") : "-";

            // 2번 셋팅 정보
            string name2 = _slotCharacterNames[1] ?? "미설정";
            double dmg2 = _slotMaxDamages[1];
            string dmg2Str = dmg2 > 0 ? dmg2.ToString("N0") : "-";

            // 현재 셋팅 표시
            string marker1 = _currentSlotIndex == 0 ? " ◀" : "";
            string marker2 = _currentSlotIndex == 1 ? " ◀" : "";

            sb.AppendLine($"  1번 셋팅 ({name1}): {dmg1Str,12}{marker1}");
            sb.AppendLine($"  2번 셋팅 ({name2}): {dmg2Str,12}{marker2}");

            // 둘 다 계산되었으면 차이 표시
            if (dmg1 > 0 && dmg2 > 0)
            {
                double diff = dmg1 - dmg2;
                double diffPercent = (diff / dmg2) * 100;
                string diffSign = diff >= 0 ? "+" : "";
                string winner = diff > 0 ? "1번" : (diff < 0 ? "2번" : "동일");

                sb.AppendLine("───────────────────────────────────────");
                sb.AppendLine($"  차이: {diffSign}{diff:N0} ({diffSign}{diffPercent:F1}%)");
                sb.AppendLine($"  → {winner} 셋팅이 더 높음");
            }
            else
            {
                sb.AppendLine("───────────────────────────────────────");
                sb.AppendLine("  ※ 두 셋팅 모두 계산해야 비교 가능");
            }

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();

            return sb.ToString();
        }

        private double GetSelectedTargetReduction()
        {
            return 0;
        }

        /// <summary>
        /// 캐릭터 패시브의 자버프 타입피증 가져오기
        /// 스택소모 스킬에서 스킬피해/스택소모피해 분리 계산용
        /// </summary>
        private double GetSelfBuffTypeDmg(Character character, bool isSkillEnhanced)
        {
            if (character?.Passive == null) return 0;

            var passiveData = character.Passive.GetLevelData(isSkillEnhanced);
            return passiveData?.SelfBuff?.Dmg_Dealt_Type ?? 0;
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
                    DefenseIncrease = ParseDouble(txtBossDefInc.Text)
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
            else if (rbDescend.IsChecked == true)
            {
                return BossDb.ForestBosses.FirstOrDefault(b => selected.Contains(b.Name));
            }
            else if (rbGrowthDungeon.IsChecked == true)
            {
                return BossDb.GrowthDungeonBosses.FirstOrDefault(b => selected.Contains(b.Name));
            }

            return new Boss { Name = "Unknown", Stats = new BaseStatSet { Def = 0 } };
        }
  
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // 캐릭터
            cboMyCharacter.SelectedIndex = 0;
            cboMyTranscend.SelectedIndex = 0;
            chkMySkillEnhanced.IsChecked = false;

            // 잠재능력
            cboMyPotentialAtk.SelectedIndex = 0;
            cboMyPotentialDef.SelectedIndex = 0;
            cboMyPotentialHp.SelectedIndex = 0;

            // 세트
            cboMyEquipSet1.SelectedIndex = 0;

            // 장신구
            cboMyAccessoryGrade.SelectedIndex = 0;

            // ⭐ 장비 초기화 (새 코드) - 기존 서브옵션 초기화 14줄 삭제하고 이걸로 교체
            foreach (var equip in Equipments)
            {
                equip.Reset();
            }

            // 진형
            cboMyFormation.SelectedIndex = 0;
            rbMyBack.IsChecked = true;

            // 펫
            cboMyPet.SelectedIndex = 0;
            cboMyPetStar.SelectedIndex = 2;
            txtMyPetOpt1.Text = "0";
            txtMyPetOpt2.Text = "0";
            txtMyPetOpt3.Text = "0";

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
            
            // 스택 패널 초기화
            panelBossStack.Visibility = Visibility.Collapsed;
            btnBossStack.Content = "0";

            // 버프/디버프 초기화
            foreach (var config in BuffConfigs)
            {
                config.IsChecked = false;
                config.Level = 0;
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

        private void CboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateCharacterList();
        }
        
        private void UpdateCharacterList()
        {
            string previousSelection = cboMyCharacter.SelectedItem?.ToString();
            
            cboMyCharacter.Items.Clear();
            cboMyCharacter.Items.Add("선택하세요");
            
            string gradeFilter = (cboMyGrade.SelectedItem as ComboBoxItem)?.Content.ToString();
            string typeFilter = (cboMyType.SelectedItem as ComboBoxItem)?.Content.ToString();
            
            foreach (var character in CharacterDb.Characters)
            {
                if (gradeFilter != "전체" && character.Grade != gradeFilter)
                    continue;
                
                if (typeFilter != "전체" && character.Type != typeFilter)
                    continue;
                
                cboMyCharacter.Items.Add(character.Name);
            }
            
            if (previousSelection != null && cboMyCharacter.Items.Contains(previousSelection))
                cboMyCharacter.SelectedItem = previousSelection;
            else
                cboMyCharacter.SelectedIndex = 0;
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
            cboMyAccessoryGrade.SelectedIndex = nextTag;
            
            // 6성이면 부옵션 표시
            if (nextTag == 3)
            {
                cboMyAccessorySub.Visibility = Visibility.Visible;
                txtMyAccessorySubValue.Visibility = Visibility.Visible;
            }
            else
            {
                cboMyAccessorySub.Visibility = Visibility.Collapsed;
                txtMyAccessorySubValue.Visibility = Visibility.Collapsed;
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
            if (cboMyPet == null) return;

            cboMyPet.Items.Clear();
            cboMyPet.Items.Add("직접 입력하거나 골라주세요");

            string starFilter = (cboMyPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";
            string rarityFilter = (cboMyPetRarity.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전체";

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
                    cboMyPet.Items.Add(pet.Name);
                }
            }

            cboMyPet.SelectedIndex = 0;
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

        #region 스탯 계산

        private void RecalculateStats()
        {
            if (!_isInitialized) return;

            // ========== 입력 데이터 구성 ==========
            Character character = null;
            if (cboMyCharacter.SelectedIndex > 0)
                character = CharacterDb.GetByName(cboMyCharacter.SelectedItem.ToString());

            // 장신구 상태 업데이트
            _currentAccessory.Grade = cboMyAccessoryGrade.SelectedIndex > 0 
                ? cboMyAccessoryGrade.SelectedIndex + 3 : 0;
            _currentAccessory.MainOption = cboMyAccessoryMain.SelectedIndex > 0 
                ? cboMyAccessoryMain.SelectedItem.ToString() : null;
            _currentAccessory.SubOption = cboMyAccessorySub.SelectedIndex > 0 
                ? cboMyAccessorySub.SelectedItem.ToString() : null;

            // 진형 상태 업데이트
            _currentFormation.Name = cboMyFormation.SelectedIndex > 0 
                ? cboMyFormation.SelectedItem.ToString() : null;
            _currentFormation.IsBackPosition = rbMyBack.IsChecked == true;

            // 펫 정보
            Pet pet = null;
            int petStar = GetPetStar();
            if (cboMyPet.SelectedIndex > 0)
                pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());

            // 버프/디버프 계산 (지속/턴제/펫 분리)
            var (partyPermanentBuffs, partyTimedBuffs, partyPetBuffs) = _buffCalculator.CalculateSeparatedPartyBuffs(BuffConfigs, pet, petStar);
            var totalBuffs = _buffCalculator.CalculateTotalBuffs(BuffConfigs, pet, petStar);
            _currentDebuffs = _buffCalculator.CalculateTotalDebuffs(BuffConfigs, pet, petStar);

            // 입력 데이터 생성
            var input = new StatCalculationInput
            {
                Character = character,
                TranscendLevel = cboMyTranscend.SelectedIndex,
                IsSkillEnhanced = chkMySkillEnhanced.IsChecked == true,
                IsPassiveConditionMet = chkMyPassiveCondition.IsChecked == true,

                Equipments = Equipments,
                EquipSetName = cboMyEquipSet1.SelectedIndex > 0 
                    ? cboMyEquipSet1.SelectedItem.ToString() : null,
                EquipSetCount = 4,

                PotentialAtkLevel = cboMyPotentialAtk.SelectedIndex,
                PotentialDefLevel = cboMyPotentialDef.SelectedIndex,
                PotentialHpLevel = cboMyPotentialHp.SelectedIndex,

                Accessory = _currentAccessory,
                Formation = _currentFormation,

                Pet = pet,
                PetStar = petStar,
                PetOptionAtkRate = GetPetOptionAtkRate(),
                PetOptionDefRate = GetPetOptionDefRate(),
                PetOptionHpRate = GetPetOptionHpRate(),

                TotalBuffs = totalBuffs,
                TotalDebuffs = _currentDebuffs,

                // 분리된 파티버프 (지속/턴제/펫)
                PartyPermanentBuffs = partyPermanentBuffs,
                PartyTimedBuffs = partyTimedBuffs,
                PartyPetBuffs = partyPetBuffs
            };

            // ========== 계산 실행 ==========
            var result = _statCalculator.Calculate(input);

            // ========== UI 표시 ==========
            txtMyStatAtkBase.Text = result.BaseAtk.ToString("N0");
            txtMyStatDefBase.Text = result.BaseDef.ToString("N0");
            txtMyStatHpBase.Text = result.BaseHp.ToString("N0");

            txtMyStatAtk.Text = result.FinalAtk.ToString("N0");
            txtMyStatDef.Text = result.FinalDef.ToString("N0");
            txtMyStatHp.Text = result.FinalHp.ToString("N0");
            txtMyStatSpd.Text = result.FinalSpd.ToString("N0");

            UpdateStatDisplay(result.DisplayStats);
            UpdateBossDebuffDisplay();
        }

        private void UpdateStatDisplay(BaseStatSet stats)
        {
            txtMyStatCri.Text = $"{stats.Cri}%";
            txtMyStatCriDmg.Text = $"{stats.Cri_Dmg}%";
            txtMyStatWek.Text = $"{stats.Wek}%";
            txtMyStatWekDmg.Text = $"{stats.Wek_Dmg}%";
            txtMyStatDmgDealt.Text = $"{stats.Dmg_Dealt}%";
            txtMyStatBossDmg.Text = $"{stats.Dmg_Dealt_Bos}%";
            txtMyStatDmg1to3.Text = $"{stats.Dmg_Dealt_1to3}%";
            txtMyStatDmg4to5.Text = $"{stats.Dmg_Dealt_4to5}%";
            txtMyStatArmPen.Text = $"{stats.Arm_Pen}%";
            txtMyStatBlk.Text = $"{stats.Blk}%";
            txtMyStatEffHit.Text = $"{stats.Eff_Hit}%";
            txtMyStatEffRes.Text = $"{stats.Eff_Res}%";
            txtMyStatEffAcc.Text = $"{stats.Eff_Acc}%";
            txtMyStatDmgRdc.Text = $"{stats.Dmg_Rdc}%";
            txtMyStatAtkRate.Text = $"{stats.Atk_Rate}%";
            txtMyStatDmgDealtType.Text = $"{stats.Dmg_Dealt_Type}%";
        }

        
        #endregion

        #region 펫 스탯 헬퍼

        private int GetPetStar()
        {
            string starStr = (cboMyPetStar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6성";

            if (starStr == "4성") return 4;
            else if (starStr == "5성") return 5;
            else if (starStr == "6성") return 6;
            else return 0;
        }

        private double GetPetFlatAtk()
        {
            if (cboMyPet.SelectedIndex <= 0) return 0;

            var pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
                return pet.GetBaseStats(star).Atk;
            }
            return 0;
        }

        private double GetPetFlatDef()
        {
            if (cboMyPet.SelectedIndex <= 0) return 0;

            var pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
                return pet.GetBaseStats(star).Def;
            }
            return 0;
        }

        private double GetPetFlatHp()
        {
            if (cboMyPet.SelectedIndex <= 0) return 0;

            var pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
                return pet.GetBaseStats(star).Hp;
            }
            return 0;
        }

        /// <summary>
        /// 펫 스킬 버프 전체 가져오기
        /// </summary>
        private BuffSet GetPetSkillBuff()
        {
            if (cboMyPet.SelectedIndex <= 0) return new BuffSet();
            var pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
                if (star == 0) return new BuffSet();
                return pet.GetSkillBuff(star);
            }
            return new BuffSet();
        }

        /// <summary>
        /// 펫 스킬 디버프 전체 가져오기
        /// </summary>
        private DebuffSet GetPetSkillDebuff()
        {
            if (cboMyPet.SelectedIndex <= 0) return new DebuffSet();
            var pet = PetDb.GetByName(cboMyPet.SelectedItem.ToString());
            if (pet != null)
            {
                int star = GetPetStar();
                if (star == 0) return new DebuffSet();
                return pet.GetSkillDebuff(star);
            }
            return new DebuffSet();
        }

        private double GetPetOptionAtkRate()
        {
            if (double.TryParse(txtMyPetAtk.Text, out double val))
                return val;
            return 0;
        }

        private double GetPetOptionDefRate()
        {
            if (double.TryParse(txtMyPetDef.Text, out double val))
                return val;
            return 0;
        }

        private double GetPetOptionHpRate()
        {
            if (double.TryParse(txtMyPetHp.Text, out double val))
                return val;
            return 0;
        }

        #endregion

        #region 보스 UI 헬퍼

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
            else if (rbDescend.IsChecked == true)  // ✅ 강림 추가
            {
                foreach (var boss in BossDb.ForestBosses)
                {
                    cboBoss.Items.Add($"{boss.Name} {boss.Difficulty}단계");
                }
            }
            else if (rbGrowthDungeon.IsChecked == true)  // ✅ 성장던전 추가
            {
                foreach (var boss in BossDb.GrowthDungeonBosses)
                {
                    cboBoss.Items.Add($"{boss.Name} {boss.Difficulty}단계");
                }
            }
            cboBoss.SelectedIndex = 0;
        }

        private void UpdateBossDebuffDisplay()
        {
            txtBossDefRed.Text = _currentDebuffs.Def_Reduction.ToString("F0");
            txtBossDmgTaken.Text = _currentDebuffs.Dmg_Taken_Increase.ToString("F0");
            // 취약 + 보스취약 합산 출력
            double totalVulnerability = _currentDebuffs.Vulnerability + _currentDebuffs.Boss_Vulnerability;
            txtBossVulnerable.Text = totalVulnerability.ToString("F0");
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

        #region 버프/디버프 버튼 UI

        private static readonly Brush[] BuffBgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(58, 58, 58)),    // 0: 기본
            new SolidColorBrush(Color.FromRgb(180, 150, 50)),  // 1: 스강
            new SolidColorBrush(Color.FromRgb(70, 130, 180)),  // 2: 초월
            new SolidColorBrush(Color.FromRgb(138, 43, 226))   // 3: 풀
        };

        private static readonly Brush[] BuffFgColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            Brushes.Black,
            Brushes.White,
            Brushes.White
        };

        private void BuffOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BuffConfig config)
            {
                // Level 순환: 0 → 1 → 2 → 3 → 0
                config.Level = (config.Level + 1) % 4;

                // 버튼 색상 업데이트
                btn.Background = BuffBgColors[config.Level];
                btn.Foreground = BuffFgColors[config.Level];

                if (_isInitialized) RecalculateStats();
            }
        }

        /// <summary>
        /// 모든 버프/디버프 버튼 색상을 현재 Level에 맞게 업데이트
        /// </summary>
        private void UpdateBuffButtonColors()
        {
            // ItemsControl에서 버튼 찾기
            UpdateBuffButtonColorsInItemsControl(icPassiveBuffs);
            UpdateBuffButtonColorsInItemsControl(icActiveBuffs);
            UpdateBuffButtonColorsInItemsControl(icPassiveDebuffs);
            UpdateBuffButtonColorsInItemsControl(icActiveDebuffs);
        }

        private void UpdateBuffButtonColorsInItemsControl(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                var config = itemsControl.Items[i] as BuffConfig;
                if (config == null) continue;

                // 버튼 찾기
                var button = FindVisualChild<Button>(container);
                if (button != null)
                {
                    button.Background = BuffBgColors[config.Level];
                    button.Foreground = BuffFgColors[config.Level];
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        #endregion

        #region 버프/디버프 합산

        private void PassiveBuff_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            RecalculateStats();
        }

        #endregion

        #region 장비 시스템

        private void InitializeEquipments()
        {
            Equipments = new ObservableCollection<Equipment>
            {
                new Equipment { Name = "무기1", Slot = "무기" },
                new Equipment { Name = "무기2", Slot = "무기" },
                new Equipment { Name = "방어구1", Slot = "방어구" },
                new Equipment { Name = "방어구2", Slot = "방어구" },
            };

            foreach (var equip in Equipments)
            {
                equip.EquipmentChanged += (s, e) =>
                {
                    if (_isInitialized) RecalculateStats();
                };
            }
        }

        // 티어 버튼 클릭
        private void SubOptionTier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubStatSlot slot)
            {
                slot.Tier = (slot.Tier + 1) % 7;
            }
        }

        // 티어 버튼 우클릭
        private void SubOptionTier_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubStatSlot slot)
            {
                slot.Tier = slot.Tier > 0 ? slot.Tier - 1 : 6;
                e.Handled = true;
            }
        }

        private void EquipmentMainOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (sender is ComboBox cbo && cbo.Tag is Equipment equip)
            {
                equip.MainStatName = cbo.SelectedIndex > 0 ? cbo.SelectedItem?.ToString() : null;
            }

            RecalculateStats();
        }

        // 새 코드 (이것만 유지)
        private BaseStatSet GetMainOptionStats()
        {
            BaseStatSet stats = new BaseStatSet();

            foreach (var equip in Equipments)
            {
                stats.Add(equip.GetMainStats());
            }

            return stats;
        }

        private BaseStatSet GetSubOptionStats()
        {
            BaseStatSet result = new BaseStatSet();

            foreach (var equip in Equipments)
            {
                result.Add(equip.GetSubStats());
            }

            return result;
        }

        // 리셋
        private void ResetEquipments()
        {
            foreach (var equip in Equipments)
            {
                equip.Reset();
            }
        }

        #endregion

        #region 캐릭터 슬롯 전환

        /// <summary>
        /// 캐릭터 슬롯 전환 버튼 클릭
        /// </summary>
        private void BtnSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int newIndex = int.Parse(btn.Tag.ToString());
                if (newIndex == _currentSlotIndex) return;

                SwitchCharacterSlot(newIndex);
            }
        }

        /// <summary>
        /// 캐릭터 슬롯 전환 실행
        /// </summary>
        private void SwitchCharacterSlot(int newIndex)
        {
            // 1. 현재 UI 값을 현재 슬롯에 저장
            _characterSlots[_currentSlotIndex] = CreatePresetFromUI();

            // 2. 인덱스 변경
            _currentSlotIndex = newIndex;

            // 3. 새 슬롯 데이터가 있으면 UI에 로드, 없으면 초기화
            if (_characterSlots[newIndex] != null)
            {
                ApplyPresetToUI(_characterSlots[newIndex]);
            }
            else
            {
                // 새 슬롯은 초기 상태로
                BtnReset_Click(null, null);
            }

            // 4. 버튼 UI 업데이트
            UpdateSlotButtonStyles();
        }

        /// <summary>
        /// 슬롯 버튼 스타일 업데이트 (선택된 슬롯 강조)
        /// </summary>
        private void UpdateSlotButtonStyles()
        {
            // 1번 슬롯 버튼
            if (_currentSlotIndex == 0)
            {
                btnSlot1.Background = new SolidColorBrush(Color.FromRgb(99, 102, 241));  // #6366f1
                btnSlot1.Foreground = Brushes.White;
                btnSlot2.Background = new SolidColorBrush(Color.FromRgb(54, 59, 71));    // #363b47
                btnSlot2.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94a3b8
            }
            else
            {
                btnSlot1.Background = new SolidColorBrush(Color.FromRgb(54, 59, 71));
                btnSlot1.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                btnSlot2.Background = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                btnSlot2.Foreground = Brushes.White;
            }
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
                CharacterName = cboMyCharacter.SelectedIndex > 0 ? cboMyCharacter.SelectedItem.ToString() : "",
                SkillName = cboMySkill.SelectedIndex >= 0 ? cboMySkill.SelectedItem?.ToString() : "",
                TranscendLevel = cboMyTranscend.SelectedIndex,
                IsSkillEnhanced = chkMySkillEnhanced.IsChecked == true,
        
                PotentialAtk = cboMyPotentialAtk.SelectedIndex,
                PotentialDef = cboMyPotentialDef.SelectedIndex,
                PotentialHp = cboMyPotentialHp.SelectedIndex,
        
                EquipSet1 = cboMyEquipSet1.SelectedIndex > 0 ? cboMyEquipSet1.SelectedItem.ToString() : "",
        
                AccessoryGrade = cboMyAccessoryGrade.SelectedIndex,
                AccessoryOption = cboMyAccessoryMain.SelectedIndex > 0 ? cboMyAccessoryMain.SelectedItem.ToString() : "",
                AccessorySubOption = cboMyAccessorySub.SelectedIndex > 0 ? cboMyAccessorySub.SelectedItem.ToString() : "",
        
                Formation = cboMyFormation.SelectedIndex > 0 ? cboMyFormation.SelectedItem.ToString() : "",
                IsBackPosition = rbMyBack.IsChecked == true,
        
                PetName = cboMyPet.SelectedIndex > 0 ? cboMyPet.SelectedItem.ToString() : "",
                PetStar = cboMyPetStar.SelectedIndex,
                PetAtkRate = double.TryParse(txtMyPetAtk.Text, out double atkRate) ? atkRate : 0,
                PetDefRate = double.TryParse(txtMyPetDef.Text, out double defRate) ? defRate : 0,
                PetHpRate = double.TryParse(txtMyPetHp.Text, out double hpRate) ? hpRate : 0,
        
                BossType = rbSiege.IsChecked == true ? "Siege" : (rbRaid.IsChecked == true ? "Raid" : (rbDescend.IsChecked == true ? "Descend" : "GrowthDungeon")),
                BossName = cboBoss.SelectedIndex > 0 ? cboBoss.SelectedItem.ToString() : ""
                // ⬆️ 여기서 객체 초기화 끝! (마지막 항목이라 콤마 없음)
            };
        
            // ⬇️ 객체 초기화 '이후'에 Dictionary들을 할당
            preset.EquipmentMainOptions = new Dictionary<string, string>();
            preset.EquipmentSubOptions = new Dictionary<string, string>();
            preset.EquipmentSubTiers = new Dictionary<string, int>();
        
            for (int i = 0; i < Equipments.Count; i++)
            {
                var equip = Equipments[i];
        
                // 메인옵션
                preset.EquipmentMainOptions[$"Equip{i}"] = equip.MainStatName ?? "";
        
                // 서브옵션 4개
                for (int j = 0; j < equip.SubSlots.Count; j++)
                {
                    var slot = equip.SubSlots[j];
                    string key = $"Equip{i}_Sub{j}";
                    preset.EquipmentSubOptions[key] = slot.StatName ?? "";
                    preset.EquipmentSubTiers[key] = slot.Tier;
                }
            }
        
            // 버프 저장
            preset.BuffChecks = new Dictionary<string, bool>();
            preset.BuffStates = new Dictionary<string, int>();
            foreach (var config in BuffConfigs)
            {
                preset.BuffChecks[config.Key] = config.IsChecked;
                preset.BuffStates[config.Key] = config.Level;
            }

            // ⭐ 상황 옵션 저장
            preset.IsCritical = chkMyCritical.IsChecked == true;
            preset.IsWeakpoint = chkMyWeakpoint.IsChecked == true;
            preset.IsBlocked = chkMyBlock.IsChecked == true;
            preset.IsLostHpCondition = chkMyLostHpCondition.IsChecked == true;
            preset.IsSkillCondition = chkMySkillCondition.IsChecked == true;
            preset.IsStatusEffect = chkMyStatusEffect.IsChecked == true;
            preset.TargetStackCount = int.TryParse(txtMyTargetStackCount.Text, out int stackCount) ? stackCount : 0;
        
            return preset;
        }

        private void ApplyPresetToUI(Preset preset)
        {
            if (preset == null) return;

            _isInitialized = false;

            SelectComboBoxItem(cboMyCharacter, preset.CharacterName);
            cboMyTranscend.SelectedIndex = preset.TranscendLevel;
            chkMySkillEnhanced.IsChecked = preset.IsSkillEnhanced;

            if (cboMyCharacter.SelectedIndex > 0)
            {
                var character = CharacterDb.GetByName(cboMyCharacter.SelectedItem.ToString());
                if (character != null)
                {
                    cboMySkill.Items.Clear();
                    foreach (var skill in character.Skills)
                    {
                        cboMySkill.Items.Add(skill.Name);
                    }
                }
            }
            SelectComboBoxItem(cboMySkill, preset.SkillName);

            cboMyPotentialAtk.SelectedIndex = preset.PotentialAtk;
            cboMyPotentialDef.SelectedIndex = preset.PotentialDef;
            cboMyPotentialHp.SelectedIndex = preset.PotentialHp;

            SelectComboBoxItem(cboMyEquipSet1, preset.EquipSet1);

            cboMyAccessoryGrade.SelectedIndex = preset.AccessoryGrade;
            // 장신구 버튼도 동기화
            btnMyAccessoryGrade.Tag = preset.AccessoryGrade.ToString();
            string[] accessoryGrades = { "없음", "4성", "5성", "6성" };
            btnMyAccessoryGrade.Content = accessoryGrades[preset.AccessoryGrade];
            SelectComboBoxItem(cboMyAccessoryMain, preset.AccessoryOption);
            SelectComboBoxItem(cboMyAccessorySub, preset.AccessorySubOption);

            SelectComboBoxItem(cboMyFormation, preset.Formation);
            if (preset.IsBackPosition)
                rbMyBack.IsChecked = true;
            else
                rbMyFront.IsChecked = true;

            SelectComboBoxItem(cboMyPet, preset.PetName);
            cboMyPetStar.SelectedIndex = preset.PetStar;

            // ⭐ 장비 데이터 불러오기 (새 코드) - 기존 서브옵션 불러오기 부분 교체
            if (preset.EquipmentMainOptions != null)
            {
                for (int i = 0; i < Equipments.Count; i++)
                {
                    var equip = Equipments[i];

                    // 메인옵션
                    if (preset.EquipmentMainOptions.TryGetValue($"Equip{i}", out var mainOpt))
                    {
                        equip.MainStatName = string.IsNullOrEmpty(mainOpt) ? null : mainOpt;
                    }

                    // 서브옵션 4개
                    for (int j = 0; j < equip.SubSlots.Count; j++)
                    {
                        var slot = equip.SubSlots[j];
                        string key = $"Equip{i}_Sub{j}";

                        if (preset.EquipmentSubOptions != null && 
                            preset.EquipmentSubOptions.TryGetValue(key, out var statName))
                        {
                            slot.StatName = string.IsNullOrEmpty(statName) ? null : statName;
                        }

                        if (preset.EquipmentSubTiers != null && 
                            preset.EquipmentSubTiers.TryGetValue(key, out var tier))
                        {
                            slot.Tier = tier;
                        }
                    }
                }
            }

            txtMyPetAtk.Text = preset.PetAtkRate.ToString();
            txtMyPetDef.Text = preset.PetDefRate.ToString();
            txtMyPetHp.Text = preset.PetHpRate.ToString();

            if (preset.BossType == "Siege") rbSiege.IsChecked = true;
            else if (preset.BossType == "Raid") rbRaid.IsChecked = true;
            else if (preset.BossType == "Descend") rbDescend.IsChecked = true;
            else if (preset.BossType == "GrowthDungeon") rbGrowthDungeon.IsChecked = true;
            UpdateBossList();
            SelectComboBoxItem(cboBoss, preset.BossName);

            // ⭐ 장신구 UI 업데이트 (6성이면 부옵션 표시)
            if (preset.AccessoryGrade == 3)  // 6성
            {
                panelMyAccessorySub.Visibility = Visibility.Visible;
            }
            else
            {
                panelMyAccessorySub.Visibility = Visibility.Collapsed;
            }

            // ⭐ 보스 스탯 로드
            if (cboBoss.SelectedIndex > 0)
            {
                string selected = cboBoss.SelectedItem?.ToString();
                Boss boss = null;

                if (preset.BossType == "Siege")
                {
                    boss = BossDb.SiegeBosses.FirstOrDefault(b => selected.Contains(b.Name));
                }
                else if (preset.BossType == "Raid")
                {
                    boss = BossDb.RaidBosses.FirstOrDefault(b => selected.Contains(b.Name) && selected.Contains($"{b.Difficulty}단계"));
                }
                else if (preset.BossType == "Descend")
                {
                    boss = BossDb.ForestBosses.FirstOrDefault(b => selected.Contains(b.Name) && selected.Contains($"{b.Difficulty}단계"));
                }
                else if (preset.BossType == "GrowthDungeon")
                {
                    boss = BossDb.GrowthDungeonBosses.FirstOrDefault(b => selected.Contains(b.Name));
                }

                if (boss != null)
                {
                    txtBossDef.Text = boss.Stats.Def.ToString("N0");
                    txtBossDefInc.Text = boss.DefenseIncrease.ToString("F0");
                    txtBossDmgRdc.Text = "0";
                    txtBossHp.Text = boss.Stats.Hp.ToString("N0");
                    txtBoss1TargetRdc.Text = boss.SingleTargetReduction.ToString("F0");
                    txtBoss3TargetRdc.Text = boss.TripleTargetReduction.ToString("F0");
                    txtBoss5TargetRdc.Text = boss.MultiTargetReduction.ToString("F0");

                    if (!string.IsNullOrEmpty(boss.DefenseIncreaseCondition))
                    {
                        panelBossCondition.Visibility = Visibility.Visible;
                        txtBossCondition.Text = boss.DefenseIncreaseCondition;
                        chkBossCondition.IsChecked = false;
                    }
                    else
                    {
                        panelBossCondition.Visibility = Visibility.Collapsed;
                    }
                }
            }

            // ⭐ 버프 불러오기 - 새 코드
            if (preset.BuffChecks != null)
            {
                foreach (var config in BuffConfigs)
                {
                    config.IsChecked = preset.BuffChecks.GetValueOrDefault(config.Key, false);

                    if (preset.BuffStates != null)
                    {
                        config.Level = preset.BuffStates.GetValueOrDefault(config.Key, 0);
                    }
                }
            }

            // ⭐ 버프/디버프 버튼 색상 업데이트
            UpdateBuffButtonColors();

            // ⭐ 상황 옵션 로드
            chkMyCritical.IsChecked = preset.IsCritical;
            chkMyWeakpoint.IsChecked = preset.IsWeakpoint;
            chkMyBlock.IsChecked = preset.IsBlocked;
            chkMyLostHpCondition.IsChecked = preset.IsLostHpCondition;
            chkMySkillCondition.IsChecked = preset.IsSkillCondition;
            chkMyStatusEffect.IsChecked = preset.IsStatusEffect;
            txtMyTargetStackCount.Text = preset.TargetStackCount.ToString();

            _isInitialized = true;
            UpdateAccessoryDisplay();
            RecalculateStats();
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
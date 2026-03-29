using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services;
using GameDamageCalculator.Services.BattleEngine;
using GameDamageCalculator.Services.Optimizer;

namespace GameDamageCalculator.UI
{
    public partial class SimulatorWindow : Window
    {
        private readonly ComboBox[] _charCombos;
        private readonly CheckBox[] _charBackChecks;

        public SimulatorWindow()
        {
            InitializeComponent();

            _charCombos = new[] { Char1Combo, Char2Combo, Char3Combo, Char4Combo, Char5Combo };
            _charBackChecks = new[] { Char1Back, Char2Back, Char3Back, Char4Back, Char5Back };

            InitializeUI();
        }

        private void InitializeUI()
        {
            // 보스 타입
            BossTypeCombo.Items.Add("공성전");
            BossTypeCombo.Items.Add("레이드");
            BossTypeCombo.Items.Add("강림");
            BossTypeCombo.Items.Add("성장던전");
            BossTypeCombo.Items.Add("잡몹");
            BossTypeCombo.SelectedIndex = 0;

            // 진형
            foreach (var formation in StatTable.FormationDb.Formations.Keys)
            {
                FormationCombo.Items.Add(formation);
            }
            FormationCombo.SelectedIndex = 0;

            // 펫
            PetCombo.Items.Add("없음");
            foreach (var pet in PetDb.Pets)
            {
                PetCombo.Items.Add(pet.Name);
            }
            PetCombo.SelectedIndex = 0;

            // 펫 성급
            for (int i = 1; i <= 5; i++)
                PetStarCombo.Items.Add(i);
            PetStarCombo.SelectedIndex = 4; // 기본 5성

            // 캐릭터
            var characters = CharacterDb.Characters.OrderBy(c => c.Name).ToList();
            foreach (var combo in _charCombos)
            {
                combo.Items.Add("없음");
                foreach (var ch in characters)
                {
                    combo.Items.Add(ch.Name);
                }
                combo.SelectedIndex = 0;
            }

            // 기본 후열 체크
            foreach (var check in _charBackChecks)
                check.IsChecked = true;
        }

        private void BossTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BossCombo == null) return;
            BossCombo.Items.Clear();

            var type = BossTypeCombo.SelectedItem?.ToString();
            List<Enemy> enemies = type switch
            {
                "공성전" => EnemyDb.SiegeBosses,
                "레이드" => EnemyDb.RaidBosses,
                "강림" => EnemyDb.ForestBosses,
                "성장던전" => EnemyDb.GrowthDungeonBosses,
                "잡몹" => EnemyDb.Mobs,
                _ => new List<Enemy>()
            };

            foreach (var en in enemies)
            {
                BossCombo.Items.Add(en.Name);
            }
            if (BossCombo.Items.Count > 0)
                BossCombo.SelectedIndex = 0;
        }

        private void BossCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 보스 정보 표시 (필요 시 확장)
        }

        #region 배틀 설정 구성

        private BattleConfig BuildBattleConfig()
        {
            var config = new BattleConfig();

            // 파티 구성
            for (int i = 0; i < 5; i++)
            {
                var charName = _charCombos[i].SelectedItem?.ToString();
                if (string.IsNullOrEmpty(charName) || charName == "없음") continue;

                var character = CharacterDb.Characters.FirstOrDefault(c => c.Name == charName);
                if (character == null) continue;

                config.AllyParty.Add(new BattleCharacter
                {
                    Character = character,
                    TranscendLevel = 12, // 기본 최대 초월
                    IsSkillEnhanced = true,
                    IsPassiveConditionMet = true,
                    IsBackPosition = _charBackChecks[i].IsChecked ?? true,
                    PotentialAtkLevel = 3,
                    PotentialDefLevel = 3,
                    PotentialHpLevel = 3,
                    Equipment = new EquipmentLoadout() // 빈 장비 (옵티마이저가 채움)
                });
            }

            // 적
            var enemyName = BossCombo.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(enemyName))
            {
                config.TargetEnemy = EnemyDb.GetByName(enemyName) ?? EnemyDb.GetMobByName(enemyName);
            }

            // 진형
            config.FormationName = FormationCombo.SelectedItem?.ToString() ?? "기본 진형";

            // 펫
            var petName = PetCombo.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(petName) && petName != "없음")
            {
                config.AllyPet = PetDb.Pets.FirstOrDefault(p => p.Name == petName);
                config.PetStar = (int)(PetStarCombo.SelectedItem ?? 5);
            }

            // 최대 턴
            if (int.TryParse(MaxTurnsBox.Text, out int maxTurns))
                config.MaxTurns = maxTurns;

            return config;
        }

        #endregion

        #region 시뮬레이션 실행

        private async void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBattleConfig();
            if (config.AllyParty.Count == 0)
            {
                MessageBox.Show("캐릭터를 1명 이상 선택해주세요.", "알림");
                return;
            }
            if (config.TargetEnemy == null)
            {
                MessageBox.Show("보스를 선택해주세요.", "알림");
                return;
            }

            SimulateButton.IsEnabled = false;
            SimulateButton.Content = "시뮬레이션 중...";

            try
            {
                var result = await Task.Run(() =>
                {
                    var sim = new BattleSimulator();
                    return sim.Simulate(config);
                });

                DisplayBattleResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시뮬레이션 오류: {ex.Message}", "오류");
            }
            finally
            {
                SimulateButton.IsEnabled = true;
                SimulateButton.Content = "배틀 시뮬레이션 실행";
            }
        }

        private void DisplayBattleResult(BattleResult result)
        {
            TotalDamageText.Text = $"{result.TotalDamage:N0}";
            ElapsedTimeText.Text = $"총 {result.TotalTurns}턴 | 보스 잔여 HP: {result.EnemyRemainingHp:N0}";

            ResultsPanel.Children.Clear();

            foreach (var charResult in result.CharacterResults.OrderByDescending(r => r.TotalDamage))
            {
                var card = CreateCharacterResultCard(charResult);
                ResultsPanel.Children.Add(card);
            }

            // 턴 로그
            AddTurnLogSection(result.TurnLogs);
        }

        #endregion

        #region 옵티마이저 실행

        private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBattleConfig();
            if (config.AllyParty.Count == 0)
            {
                MessageBox.Show("캐릭터를 1명 이상 선택해주세요.", "알림");
                return;
            }
            if (config.TargetEnemy == null)
            {
                MessageBox.Show("보스를 선택해주세요.", "알림");
                return;
            }

            OptimizeButton.IsEnabled = false;
            OptimizeButton.Content = "최적화 중...";

            try
            {
                int maxSubTiers = int.TryParse(MaxSubTiersBox.Text, out int t) ? t : 64;

                var result = await Task.Run(() =>
                {
                    var optimizer = new EquipmentOptimizer
                    {
                        MaxTotalSubTiers = maxSubTiers,
                        TopResultCount = 3
                    };
                    return optimizer.OptimizeParty(config);
                });

                DisplayOptimizerResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"최적화 오류: {ex.Message}", "오류");
            }
            finally
            {
                OptimizeButton.IsEnabled = true;
                OptimizeButton.Content = "장비 최적화 실행";
            }
        }

        private void DisplayOptimizerResult(OptimizerResult result)
        {
            TotalDamageText.Text = $"{result.EstimatedTotalDamage:N0}";
            ElapsedTimeText.Text = $"최적화 소요: {result.ElapsedMilliseconds}ms | 탐색 조합: {result.TotalCombinationsSearched:N0}";

            ResultsPanel.Children.Clear();

            foreach (var charResult in result.CharacterResults)
            {
                var card = CreateOptimizerResultCard(charResult);
                ResultsPanel.Children.Add(card);
            }
        }

        #endregion

        #region UI 생성 헬퍼

        private Border CreateCharacterResultCard(CharacterDamageResult result)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1c, 0x24)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var stack = new StackPanel();

            // 캐릭터 이름 + 데미지
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = result.CharacterName,
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0x8c, 0xf8)),
                FontSize = 14, FontWeight = FontWeights.Bold
            });
            header.Children.Add(new TextBlock
            {
                Text = $"  {result.TotalDamage:N0} ({result.DamageShare:F1}%)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xd3, 0x99)),
                FontSize = 14, Margin = new Thickness(8, 0, 0, 0)
            });
            stack.Children.Add(header);

            // 기본공격/스킬 분류
            var detail = new TextBlock
            {
                Text = $"기본공격: {result.NormalAttackDamage:N0} | 스킬: {result.SkillDamage:N0}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xa3, 0xb8)),
                FontSize = 11, Margin = new Thickness(0, 4, 0, 0)
            };
            stack.Children.Add(detail);

            // 데미지 바
            var barBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x22, 0x24, 0x2e)),
                CornerRadius = new CornerRadius(3),
                Height = 6, Margin = new Thickness(0, 4, 0, 0)
            };
            var bar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xf1)),
                CornerRadius = new CornerRadius(3),
                Height = 6,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(1, result.DamageShare / 100 * 300)
            };
            var barGrid = new Grid();
            barGrid.Children.Add(barBorder);
            barGrid.Children.Add(bar);
            stack.Children.Add(barGrid);

            border.Child = stack;
            return border;
        }

        private Border CreateOptimizerResultCard(CharacterOptimalEquipment result)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1c, 0x24)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var stack = new StackPanel();

            // 캐릭터 이름
            stack.Children.Add(new TextBlock
            {
                Text = $"{result.CharacterName} - 최적 장비",
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0x8c, 0xf8)),
                FontSize = 14, FontWeight = FontWeights.Bold
            });

            // 예상 데미지
            stack.Children.Add(new TextBlock
            {
                Text = $"예상 데미지: {result.EstimatedDamage:N0}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xd3, 0x99)),
                FontSize = 13, Margin = new Thickness(0, 4, 0, 0)
            });

            // 세트 조합
            if (result.BestSetConfig != null)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"세트: {result.BestSetConfig.Description}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b)),
                    FontSize = 12, Margin = new Thickness(0, 4, 0, 0)
                });
            }

            // 장비 상세
            if (result.BestLoadout != null)
            {
                var loadout = result.BestLoadout;
                var equipText = FormatEquipmentDetail(loadout);
                stack.Children.Add(new TextBlock
                {
                    Text = equipText,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xa3, 0xb8)),
                    FontSize = 11, Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            // 상위 조합 표시
            foreach (var ranked in result.TopLoadouts.Skip(1))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"  #{ranked.Rank}: {ranked.SetConfig?.Description} → {ranked.EstimatedDamage:N0}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8b)),
                    FontSize = 10, Margin = new Thickness(0, 2, 0, 0)
                });
            }

            border.Child = stack;
            return border;
        }

        private string FormatEquipmentDetail(EquipmentLoadout loadout)
        {
            var parts = new List<string>();

            if (loadout.Weapon1 != null)
                parts.Add($"무기1: {loadout.Weapon1.SetName} [{loadout.Weapon1.MainStatName}]");
            if (loadout.Weapon2 != null)
                parts.Add($"무기2: {loadout.Weapon2.SetName} [{loadout.Weapon2.MainStatName}]");
            if (loadout.Armor1 != null)
                parts.Add($"방어구1: {loadout.Armor1.SetName} [{loadout.Armor1.MainStatName}]");
            if (loadout.Armor2 != null)
                parts.Add($"방어구2: {loadout.Armor2.SetName} [{loadout.Armor2.MainStatName}]");
            if (loadout.Accessory != null)
                parts.Add($"장신구: {loadout.Accessory.Grade}성 [{loadout.Accessory.MainOption}]");

            // 서브옵션 요약
            foreach (var equip in loadout.GetEquipments())
            {
                var subs = equip.SubSlots?
                    .Where(s => !string.IsNullOrEmpty(s.StatName) && s.Tier > 0)
                    .Select(s => $"{s.StatName}({s.Tier})")
                    .ToList();
                if (subs != null && subs.Count > 0)
                    parts.Add($"  {equip.Name} 서브: {string.Join(", ", subs)}");
            }

            return string.Join("\n", parts);
        }

        private void AddTurnLogSection(List<BattleTurnLog> logs)
        {
            if (logs == null || logs.Count == 0) return;

            var expander = new Expander
            {
                Header = new TextBlock
                {
                    Text = $"턴 로그 ({logs.Count}개 행동)",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xa3, 0xb8)),
                    FontSize = 12
                },
                Margin = new Thickness(0, 8, 0, 0)
            };

            var logStack = new StackPanel();
            foreach (var log in logs.Where(l => l.IsAlly && l.DamageDealt > 0).Take(50))
            {
                logStack.Children.Add(new TextBlock
                {
                    Text = $"[턴 {log.Turn}] {log.Description}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8b)),
                    FontSize = 10
                });
            }

            expander.Content = logStack;
            ResultsPanel.Children.Add(expander);
        }

        #endregion
    }
}

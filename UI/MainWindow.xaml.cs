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
        
        public MainWindow()
        {
            InitializeComponent();
            _calculator = new DamageCalculator();
            InitializeComboBoxes();
        }

        #region 초기화

        /// <summary>
        /// 콤보박스 초기화
        /// </summary>
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

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// 보스 타입 변경
        /// </summary>
        private void BossType_Changed(object sender, RoutedEventArgs e)
        {
            if (cboBoss != null)
            {
                UpdateBossList();
            }
        }

        /// <summary>
        /// 캐릭터 선택 변경
        /// </summary>
        private void CboCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCharacter.SelectedIndex <= 0) 
            {
                cboSkill.Items.Clear();
                cboSkill.Items.Add("직접 입력하거나 골라주세요");
                cboSkill.SelectedIndex = 0;
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

        /// <summary>
        /// 스킬 선택 변경
        /// </summary>
        private void CboSkill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: 스킬 선택 시 스킬 배율 적용
        }

        /// <summary>
        /// 초월 단계 변경
        /// </summary>
        private void CboTranscend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        /// <summary>
        /// 장비 세트 선택 변경
        /// </summary>
        private void CboEquipSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        /// <summary>
        /// 세트1 개수 선택 변경
        /// </summary>
        private void CboSetCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridEquipSet2 == null) return;
            
            if (cboSetCount1.SelectedIndex == 0)
            {
                gridEquipSet2.Visibility = Visibility.Visible;
            }
            else
            {
                gridEquipSet2.Visibility = Visibility.Collapsed;
                if (cboEquipSet2 != null)
                    cboEquipSet2.SelectedIndex = 0;
            }
            
            RecalculateStats();
        }

        /// <summary>
        /// 잠재능력 선택 변경
        /// </summary>
        private void Potential_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        /// <summary>
        /// 펫 선택 변경
        /// </summary>
        private void CboPet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        /// <summary>
        /// 펫 옵션 변경
        /// </summary>
        private void PetOption_Changed(object sender, EventArgs e)
        {
            RecalculateStats();
        }

        /// <summary>
        /// 보스 선택 변경
        /// </summary>
        private void CboBoss_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

        /// <summary>
        /// 데미지 계산 버튼 클릭
        /// </summary>
        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double totalAtk = CalculateTotalAtk();
                txtResult.Text = $"총 공격력: {totalAtk:N0}";
                
                // TODO: 데미지 계산 추가
            }
            catch (Exception ex)
            {
                txtResult.Text = $"오류: {ex.Message}";
            }
        }

        /// <summary>
        /// 초기화 버튼 클릭
        /// </summary>
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

            // 펫
            cboPet.SelectedIndex = 0;
            cboPetStar.SelectedIndex = 2;
            cboPetOpt1.SelectedIndex = 0;
            cboPetOpt2.SelectedIndex = 0;
            cboPetOpt3.SelectedIndex = 0;
            cboPetOpt4.SelectedIndex = 0;
            txtPetOpt1.Text = "0";
            txtPetOpt2.Text = "0";
            txtPetOpt3.Text = "0";
            txtPetOpt4.Text = "0";

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
        /// 스탯 재계산 (UI 표시용 - 스탯공격력)
        /// 스탯공격력 = 기초공 × (1 + 장비공% + 초월공%) + 장비깡공 + 잠재능력깡공
        /// </summary>
        private void RecalculateStats()
        {
            if (cboCharacter == null || cboEquipSet1 == null || cboSetCount1 == null) return;

            // 기초 스탯 (LegendStats 또는 HeroStats)
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

            // 장비공% (세트 효과)
            double equipAtkRate = 0;
            double equipDefRate = 0;
            double equipHpRate = 0;

            // 세트1 효과
            if (cboEquipSet1.SelectedIndex > 0)
            {
                string setName = cboEquipSet1.SelectedItem.ToString();
                int setCount = cboSetCount1.SelectedIndex == 0 ? 2 : 4;
                var setBonus = GetSetBonus(setName, setCount);
                equipAtkRate += setBonus.Atk_Rate;
                equipDefRate += setBonus.Def_Rate;
                equipHpRate += setBonus.Hp_Rate;
            }

            // 세트2 효과
            if (cboSetCount1.SelectedIndex == 0 && cboEquipSet2 != null && cboEquipSet2.SelectedIndex > 0)
            {
                string setName = cboEquipSet2.SelectedItem.ToString();
                var setBonus = GetSetBonus(setName, 2);
                equipAtkRate += setBonus.Atk_Rate;
                equipDefRate += setBonus.Def_Rate;
                equipHpRate += setBonus.Hp_Rate;
            }

            // 초월공% (GetFullBonuses - Atk_Rate)
            double transcendAtkRate = 0;
            double transcendDefRate = 0;
            double transcendHpRate = 0;
            if (cboCharacter.SelectedIndex > 0 && cboTranscend != null)
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

            // 장비깡공 (CommonWeaponStat)
            double equipFlatAtk = EquipmentDb.EquipStatTable.CommonWeaponStat.Atk;
            double equipFlatDef = EquipmentDb.EquipStatTable.CommonArmorStat.Def;
            double equipFlatHp = EquipmentDb.EquipStatTable.CommonArmorStat.Hp;

            // 잠재능력깡공 (PotentialDb)
            var potentialStats = GetPotentialStats();

            // ========== 스탯 공격력 계산 ==========
            // 스탯공격력 = 기초공 × (1 + 장비공% + 초월공%) + 장비깡공 + 잠재능력깡공
            double statAtk = baseAtk * (1 + equipAtkRate + transcendAtkRate) + equipFlatAtk + potentialStats.Atk;
            double statDef = baseDef * (1 + equipDefRate + transcendDefRate) + equipFlatDef + potentialStats.Def;
            double statHp = baseHp * (1 + equipHpRate + transcendHpRate) + equipFlatHp + potentialStats.Hp;

            // UI에 표시할 스탯 세팅
            BaseStatSet displayStats = new BaseStatSet
            {
                Atk = statAtk,
                Def = statDef,
                Hp = statHp,
                Cri = characterStats.Cri,
                Cri_Dmg = characterStats.Cri_Dmg,
                Wek = characterStats.Wek,
                Wek_Dmg = characterStats.Wek_Dmg,
                Dmg_Dealt = characterStats.Dmg_Dealt,
                Dmg_Dealt_Bos = characterStats.Dmg_Dealt_Bos,
                Arm_Pen = characterStats.Arm_Pen,
                Blk = characterStats.Blk
            };

            UpdateStatDisplay(displayStats);
        }

        /// <summary>
        /// 스탯 표시 업데이트
        /// </summary>
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
        }

        #endregion

        #region 총 공격력 계산 (데미지 계산용)

        /// <summary>
        /// 총 공격력 계산 (데미지 계산용)
        /// 총공격력 = (스탯공격력 + 펫깡공 + 기초공 × 진형%) × (1 + 버프%)
        /// </summary>
        private double CalculateTotalAtk()
        {
            // 스탯 공격력 (UI에 표시된 값)
            double statAtk = ParseStatValue(txtStatAtk.Text);
            
            // 기초공 (버프 계산용)
            double baseAtk = GetBaseAtk();
            
            // 펫 깡공 (PetStatTable - Atk)
            double petFlatAtk = GetPetFlatAtk();
            
            // 진형 보너스 (Atk_Rate)
            double formationAtkRate = GetFormationAtkRate();
            
            // 버프% (펫 스킬 + 펫 옵션 + 파티 버프 등)
            double buffAtkRate = GetBuffAtkRate();
            
            // ========== 총 공격력 계산 ==========
            // 총공격력 = (스탯공격력 + 펫깡공 + 기초공 × 진형%) × (1 + 버프%)
            double totalAtk = (statAtk + petFlatAtk + baseAtk * formationAtkRate) * (1 + buffAtkRate);
            
            return totalAtk;
        }

        /// <summary>
        /// 기초 공격력 가져오기
        /// </summary>
        private double GetBaseAtk()
        {
            if (cboCharacter == null || cboCharacter.SelectedIndex <= 0) return 0;
            
            var character = CharacterDb.GetByName(cboCharacter.SelectedItem.ToString());
            if (character != null)
            {
                return character.GetBaseStats().Atk;
            }
            return 0;
        }

        #endregion

        #region 펫 관련

        /// <summary>
        /// 펫 깡공 가져오기 (PetStatTable - Atk)
        /// </summary>
        private double GetPetFlatAtk()
        {
            if (cboPet == null || cboPetStar == null) return 0;
            if (cboPet.SelectedIndex <= 0) return 0;

            string petName = cboPet.SelectedItem.ToString();
            var pet = PetDb.GetByName(petName);
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetBaseStats(star).Atk;
            }

            return 0;
        }

        /// <summary>
        /// 펫 옵션 공격력% 합산
        /// </summary>
        private double GetPetOptionAtkRate()
        {
            double rate = 0;
            
            rate += GetSinglePetOptionRate(cboPetOpt1, txtPetOpt1, "공격력%");
            rate += GetSinglePetOptionRate(cboPetOpt2, txtPetOpt2, "공격력%");
            rate += GetSinglePetOptionRate(cboPetOpt3, txtPetOpt3, "공격력%");
            rate += GetSinglePetOptionRate(cboPetOpt4, txtPetOpt4, "공격력%");
            
            return rate;
        }

        private double GetSinglePetOptionRate(ComboBox cbo, TextBox txt, string targetOption)
        {
            if (cbo == null || txt == null || cbo.SelectedIndex <= 0) return 0;
            
            string option = (cbo.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (option == targetOption)
            {
                return ParseDouble(txt.Text) / 100;
            }
            
            return 0;
        }

        #endregion

        #region 진형 관련

        /// <summary>
        /// 진형 공격력% 가져오기
        /// </summary>
        private double GetFormationAtkRate()
        {
            // TODO: 진형 UI 구현 후 연결
            return 0;
        }

        #endregion

        #region 버프 관련

        /// <summary>
        /// 버프 공격력% 가져오기 (펫 스킬 + 펫 옵션 + 파티 버프)
        /// </summary>
        private double GetBuffAtkRate()
        {
            double buffRate = 0;
            
            // 펫 스킬 버프
            buffRate += GetPetSkillAtkRate();
            
            // 펫 옵션 버프
            buffRate += GetPetOptionAtkRate();
            
            // TODO: 파티 버프 (델론즈, 에이린 등) 추가
            
            return buffRate;
        }

        /// <summary>
        /// 펫 스킬 공격력% 가져오기
        /// </summary>
        private double GetPetSkillAtkRate()
        {
            if (cboPet == null || cboPetStar == null) return 0;
            if (cboPet.SelectedIndex <= 0) return 0;

            string petName = cboPet.SelectedItem.ToString();
            var pet = PetDb.GetByName(petName);
            if (pet != null)
            {
                int star = cboPetStar.SelectedIndex + 4;
                return pet.GetSkillBonus(star).Atk_Rate;
            }

            return 0;
        }

        #endregion

        #region 보스 관련

        /// <summary>
        /// 보스 타입에 따라 보스 목록 업데이트
        /// </summary>
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

        /// <summary>
        /// 세트 효과 가져오기
        /// </summary>
        private BaseStatSet GetSetBonus(string setName, int setCount)
        {
            BaseStatSet total = new BaseStatSet();

            if (EquipmentDb.SetEffects.TryGetValue(setName, out var setData))
            {
                if (setCount >= 2 && setData.TryGetValue(2, out var bonus2))
                {
                    total.Add(bonus2);
                }
                if (setCount >= 4 && setData.TryGetValue(4, out var bonus4))
                {
                    total.Add(bonus4);
                }
            }

            return total;
        }

        /// <summary>
        /// 잠재능력 스탯 계산 (깡스탯)
        /// </summary>
        private BaseStatSet GetPotentialStats()
        {
            BaseStatSet stats = new BaseStatSet();
            
            if (cboPotentialAtk == null || cboPotentialDef == null || cboPotentialHp == null)
                return stats;
            
            int atkLevel = cboPotentialAtk.SelectedIndex;
            int defLevel = cboPotentialDef.SelectedIndex;
            int hpLevel = cboPotentialHp.SelectedIndex;
            
            if (atkLevel > 0)
            {
                stats.Atk = PotentialDb.Stats["공격력"][atkLevel - 1];
            }
            if (defLevel > 0)
            {
                stats.Def = PotentialDb.Stats["방어력"][defLevel - 1];
            }
            if (hpLevel > 0)
            {
                stats.Hp = PotentialDb.Stats["생명력"][hpLevel - 1];
            }
            
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
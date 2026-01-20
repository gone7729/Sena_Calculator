using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 장비 모델
    /// </summary>
    public class Equipment : INotifyPropertyChanged
    {
        public string Name { get; set; }            // 장비 이름 ("무기1", "무기2", "갑옷1", "갑옷2")
        public string SetName { get; set; }         // 세트 이름 (선봉장, 암살자 등)
        public string Slot { get; set; }            // 부위 (무기, 방어구)

        // 메인 옵션
        private string _mainStatName;
        public string MainStatName
        {
            get => _mainStatName;
            set
            {
                if (_mainStatName != value)
                {
                    _mainStatName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainStatValue));
                    EquipmentChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 메인 옵션 값 표시용
        public string MainStatValue
        {
            get
            {
                if (string.IsNullOrEmpty(MainStatName)) return "";
                if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(MainStatName, out var stats))
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
        }

        // 부옵션 슬롯 (최대 4개) - ObservableCollection으로 변경
        public ObservableCollection<SubStatSlot> SubSlots { get; set; }

        // 장비 강화 단계 (0~5)
        public int EnhanceLevel { get; set; } = 5;

        public Equipment()
        {
            SubSlots = new ObservableCollection<SubStatSlot>
            {
                new SubStatSlot(),
                new SubStatSlot(),
                new SubStatSlot(),
                new SubStatSlot()
            };

            // 서브슬롯 변경 시 이벤트 발생
            foreach (var slot in SubSlots)
            {
                slot.SlotChanged += (s, e) => EquipmentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // 변경 이벤트
        public event EventHandler EquipmentChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// 장비 기본 스탯 가져오기
        /// </summary>
        public BaseStatSet GetBaseStats()
        {
            if (Slot == "무기")
                return EquipmentDb.EquipStatTable.CommonWeaponStat.Clone();
            else if (Slot == "방어구")
                return EquipmentDb.EquipStatTable.CommonArmorStat.Clone();
            
            return new BaseStatSet();
        }

        /// <summary>
        /// 메인 옵션 스탯 가져오기
        /// </summary>
        public BaseStatSet GetMainStats()
        {
            // null 체크 추가!
            if (string.IsNullOrEmpty(MainStatName))
                return new BaseStatSet();

            if (EquipmentDb.MainStatDb.MainOptions.TryGetValue(MainStatName, out var mainStat))
            {
                return mainStat.Clone();
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 해당 장비 슬롯에서 선택 가능한 메인옵션 목록
        /// </summary>
        public List<string> AvailableMainOptions
        {
            get
            {
                var options = new List<string> { "" };  // 빈 값 (없음)
                
                if (Slot == "무기" && EquipmentDb.MainStatDb.AvailableOptions.ContainsKey("무기"))
                {
                    options.AddRange(EquipmentDb.MainStatDb.AvailableOptions["무기"]);
                }
                else if (Slot == "방어구" && EquipmentDb.MainStatDb.AvailableOptions.ContainsKey("방어구"))
                {
                    options.AddRange(EquipmentDb.MainStatDb.AvailableOptions["방어구"]);
                }
                
                return options;
            }
        }

        /// <summary>
        /// 부옵션 합산 스탯 가져오기
        /// </summary>
        public BaseStatSet GetSubStats()
        {
            BaseStatSet total = new BaseStatSet();
            foreach (var sub in SubSlots)
            {
                System.Diagnostics.Debug.WriteLine($"SubSlot: {sub.StatName}, Tier: {sub.Tier}, Cri: {sub.GetStats().Cri}, Atk%: {sub.GetStats().Atk_Rate}");
                total.Add(sub.GetStats());
            }
            return total;
        }

        /// <summary>
        /// 장비의 전체 스탯 계산 (기본 + 메인 + 부옵션)
        /// </summary>
        public BaseStatSet GetTotalStats()
        {
            BaseStatSet total = GetBaseStats();
            total.Add(GetMainStats());
            total.Add(GetSubStats());
            return total;
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Reset()
        {
            MainStatName = null;
            foreach (var slot in SubSlots)
            {
                slot.Reset();
            }
        }
    }

    /// <summary>
    /// 부옵션 슬롯
    /// </summary>
    public class SubStatSlot : INotifyPropertyChanged
    {
        private string _statName;
        private int _tier;

        public string StatName
        {
            get => _statName;
            set
            {
                if (_statName != value)
                {
                    _statName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayValue));
                    SlotChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public int Tier
        {
            get => _tier;
            set
            {
                int newValue = Math.Clamp(value, 0, 6);
                if (_tier != newValue)
                {
                    _tier = newValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayValue));
                    SlotChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 계산된 값 표시
        public string DisplayValue
        {
            get
            {
                if (string.IsNullOrEmpty(StatName) || Tier <= 0) return "0";
                if (SubOptionDb.TierValues.TryGetValue(StatName, out int perTier))
                {
                    return (perTier * Tier).ToString();
                }
                return "0";
            }
        }

        // 변경 이벤트
        public event EventHandler SlotChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// 티어에 따른 실제 값 계산
        /// </summary>
        public BaseStatSet GetStats()
        {
            // null 체크 추가!
            if (Tier <= 0 || string.IsNullOrEmpty(StatName)) 
                return new BaseStatSet();

            if (EquipmentDb.SubStatDb.SubStatBase.TryGetValue(StatName, out var baseStats))
            {
                return baseStats.Multiply(Tier);
            }
            return new BaseStatSet();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Reset()
        {
            StatName = null;
            Tier = 0;
        }
    }

    /// <summary>
    /// 서브옵션 데이터
    /// </summary>
    public static class SubOptionDb
    {
        // 서브옵션 목록 (드롭다운용)
        public static List<string> AllStatNames { get; } = new()
        {
            "",
            "공격력%", "공격력", "방어력%", "방어력", "생명력%", "생명력",
            "치명타확률%", "치명타피해%", "약점공격확률%", "막기확률%",
            "효과적중%", "효과저항%", "받피감%", "속공"
        };

        // 티어당 값
        public static Dictionary<string, int> TierValues { get; } = new()
        {
            { "공격력%", 5 },
            { "공격력", 50 },
            { "치명타확률%", 4 },
            { "치명타피해%", 6 },
            { "약점공격확률%", 5 },
            { "속공", 4 },
            { "막기확률%", 4 },
            { "효과적중%", 5 },
            { "효과저항%", 5 },
            { "방어력%", 5 },
            { "방어력", 30 },
            { "생명력%", 5 },
            { "생명력", 180 },
        };
    }

    /// <summary>
    /// 장비 세트 (2개 또는 4개 착용 시 효과)
    /// </summary>
    public class EquipmentSet
    {
        public string SetName { get; set; }
        public int PieceCount { get; set; }

        public BaseStatSet GetSetBonus()
        {
            if (EquipmentDb.SetEffects.TryGetValue(SetName, out var setData))
            {
                BaseStatSet total = new BaseStatSet();
                
                if (PieceCount >= 2 && setData.TryGetValue(2, out var bonus2))
                {
                    total.Add(bonus2);
                }
                if (PieceCount >= 4 && setData.TryGetValue(4, out var bonus4))
                {
                    total.Add(bonus4);
                }
                
                return total;
            }
            return new BaseStatSet();
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 버프/디버프 UI 설정 모델
    /// </summary>
    public class BuffConfig : INotifyPropertyChanged
    {
        // ===== 메타데이터 =====
        public string Key { get; set; }
        public string BaseName { get; set; }
        public string CharacterName { get; set; }
        public string SkillName { get; set; }
        public bool IsBuff { get; set; }
        public string Label { get; set; }

        // ===== 그룹 관련 =====
        public string GroupKey { get; set; }
        public bool ShowButton { get; set; } = true;
        
        /// <summary>
        /// 그룹 동료 (같은 GroupKey를 가진 다른 BuffConfig)
        /// </summary>
        public BuffConfig GroupPartner { get; set; }
        
        /// <summary>
        /// 그룹 아이템 여부 (체크박스 2개 표시용)
        /// </summary>
        public bool HasGroupPartner => GroupPartner != null;

        // ===== 그룹 Level 공유를 위한 static Dictionary =====
        private static readonly Dictionary<string, int> _groupLevels = new();

        // ===== UI 상태 =====
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private int _level;
        public int Level
        {
            get
            {
                if (!string.IsNullOrEmpty(GroupKey) && _groupLevels.TryGetValue(GroupKey, out int groupLevel))
                    return groupLevel;
                return _level;
            }
            set
            {
                if (!string.IsNullOrEmpty(GroupKey))
                {
                    _groupLevels[GroupKey] = value;
                    GroupLevelChanged?.Invoke(GroupKey, value);
                }
                else
                {
                    _level = value;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(LevelDisplay));
            }
        }

        // ===== 그룹 Level 변경 이벤트 =====
        public static event Action<string, int> GroupLevelChanged;

        public void NotifyLevelChanged()
        {
            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(LevelDisplay));
        }

        // ===== 표시용 =====
        public string LevelDisplay => Level switch
        {
            0 => BaseName,
            1 => $"{BaseName}(스강)",
            2 => $"{BaseName}(초월)",
            3 => $"{BaseName}(풀)",
            _ => BaseName
        };

        /// <summary>
        /// 버프 옵션 계산 (스킬강화 여부, 초월 레벨)
        /// </summary>
        public (bool isEnhanced, int transcendLevel) GetBuffOption()
        {
            return Level switch
            {
                0 => (false, 0),
                1 => (true, 0),
                2 => (false, 6),
                3 => (true, 6),
                _ => (false, 0)
            };
        }

        // ===== 이벤트 =====
        public event EventHandler CheckedChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
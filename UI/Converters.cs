using System;
using System.Globalization;
using System.Windows.Data;

namespace GameDamageCalculator.UI
{
    /// <summary>
    /// 그룹 아이템일 때 버튼 너비를 조정하는 컨버터
    /// 그룹이면 좁게, 아니면 넓게
    /// </summary>
    public class GroupButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasGroupPartner = value is bool b && b;
            return hasGroupPartner ? 36.0 : 52.0;  // 그룹이면 좁게
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 그룹 아이템일 때 Border 너비를 조정하는 컨버터 (UniformGrid에서는 무시됨)
    /// </summary>
    public class GroupBorderWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // UniformGrid에서는 Border 너비가 무시되므로 Auto 반환
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

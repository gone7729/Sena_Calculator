using System;
using System.Globalization;
using System.Windows.Data;

namespace GameDamageCalculator.UI
{
    /// <summary>
    /// 그룹 아이템일 때 버튼 너비를 조정하는 컨버터
    /// </summary>
    public class GroupButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasGroupPartner = value is bool b && b;
            return hasGroupPartner ? 42.0 : 58.0;  // 그룹이면 좁게, 아니면 넓게
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

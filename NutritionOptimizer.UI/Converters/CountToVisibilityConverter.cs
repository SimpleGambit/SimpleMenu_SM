using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

// 컬렉션 개수를 Visibility로 변환 (0이면 Visible, 그 외면 Collapsed)
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

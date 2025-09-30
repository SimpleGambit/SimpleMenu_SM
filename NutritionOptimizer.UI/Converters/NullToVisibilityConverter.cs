using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

/// <summary>
/// Null 값을 Visibility로 변환하는 컨버터
/// null -> Collapsed, not null -> Visible
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // null이거나 빈 문자열이면 Collapsed
        if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
        {
            return Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

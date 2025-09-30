using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

/// <summary>
/// Boolean 값을 반전시켜 Visibility로 변환하는 컨버터
/// true -> Collapsed, false -> Visible
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

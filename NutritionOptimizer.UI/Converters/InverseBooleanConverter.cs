using System;
using System.Globalization;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

/// <summary>
/// Boolean 값을 반전시키는 컨버터
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

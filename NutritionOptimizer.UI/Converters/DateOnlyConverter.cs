using System;
using System.Globalization;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

// DateOnly와 DateTime을 변환하는 Converter
public class DateOnlyToDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }
        return DateTime.Today;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }
        return DateOnly.FromDateTime(DateTime.Today);
    }
}

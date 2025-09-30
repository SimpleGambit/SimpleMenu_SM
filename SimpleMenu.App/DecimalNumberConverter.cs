using System.Globalization;
using System.Windows.Data;

namespace SimpleMenu.App;

public sealed class DecimalNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double number
            ? number.ToString("G15", culture)
            : "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return NumberInput.TryParseDecimal(value?.ToString(), out var number)
            ? Math.Max(0, number)
            : Binding.DoNothing;
    }
}

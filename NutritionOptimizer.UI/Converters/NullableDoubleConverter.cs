using System;
using System.Globalization;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

/// <summary>
/// Nullable double과 문자열 간 변환을 처리하는 컨버터
/// 빈 문자열을 null로, null을 빈 문자열로 변환합니다.
/// </summary>
public class NullableDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        if (value is double d)
            return d.ToString(CultureInfo.InvariantCulture);

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null or string { Length: 0 })
            return null;

        var str = value.ToString()?.Trim();
        
        if (string.IsNullOrWhiteSpace(str))
            return null;

        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;

        // 파싱 실패 시 null 반환
        return null;
    }
}

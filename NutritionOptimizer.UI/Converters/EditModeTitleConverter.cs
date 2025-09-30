using System;
using System.Globalization;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

// IsEditMode를 제목 텍스트로 변환
public class EditModeTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEditMode)
        {
            return isEditMode ? "음식 수정" : "음식 추가";
        }
        return "음식 편집";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

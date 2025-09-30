using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.Converters;

// Food 객체의 총 가격 계산
public class FoodTotalPriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Food food)
        {
            double quantity = ParseUnitQuantity(food.WeightUnit);
            return food.PricePer100g * (quantity / 100.0);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static double ParseUnitQuantity(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 100;
        
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            return quantity;
        }
        
        return 100;
    }
}

// Food 객체의 단위당 가격 표시
public class FoodUnitPriceDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Food food)
        {
            double quantity = ParseUnitQuantity(food.WeightUnit);
            
            if (quantity < 100)
            {
                // 100g 미만이면 10g당 가격
                double unitPrice = food.PricePer100g / 10.0;
                return $"{unitPrice:N0}원/10g";
            }
            else
            {
                // 100g 이상이면 100g당 가격
                return $"{food.PricePer100g:N0}원/100g";
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static double ParseUnitQuantity(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 100;
        
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            return quantity;
        }
        
        return 100;
    }
}

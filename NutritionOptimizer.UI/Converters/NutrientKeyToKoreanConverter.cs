using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace NutritionOptimizer.UI.Converters;

// 영양소 키를 한글로 변환하는 Converter
public class NutrientKeyToKoreanConverter : IValueConverter
{
    private static readonly Dictionary<string, string> NutrientNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // 기본 영양소
        ["kcal"] = "열량 (kcal)",
        ["moisture_g"] = "수분 (g)",
        ["protein_g"] = "단백질 (g)",
        ["fat_g"] = "지방 (g)",
        ["saturated_fat_g"] = "포화지방 (g)",
        ["trans_fat_g"] = "트랜스지방 (g)",
        ["carbs_g"] = "탄수화물 (g)",
        ["fiber_g"] = "식이섬유 (g)",
        ["sugar_g"] = "당류 (g)",
        ["sodium_mg"] = "나트륨 (mg)",
        ["cholesterol_mg"] = "콜레스테롤 (mg)",
        
        // 무기질 (10개)
        ["calcium_mg"] = "칼슘 (mg)",
        ["iron_mg"] = "철 (mg)",
        ["magnesium_mg"] = "마그네슘 (mg)",
        ["phosphorus_mg"] = "인 (mg)",
        ["potassium_mg"] = "칼륨 (mg)",
        ["zinc_mg"] = "아연 (mg)",
        ["copper_mg"] = "구리 (mg)",
        ["manganese_mg"] = "망간 (mg)",
        ["selenium_ug"] = "셀레늄 (μg)",
        ["molybdenum_ug"] = "몰리브덴 (μg)",
        ["iodine_ug"] = "요오드 (μg)",

        // 비타민 (13개)
        ["vitamin_a_ug"] = "비타민 A (μg RAE)",
        ["vitamin_c_mg"] = "비타민 C (mg)",
        ["vitamin_d_ug"] = "비타민 D (μg)",
        ["vitamin_e_mg"] = "비타민 E (mg α-TE)",
        ["vitamin_k_ug"] = "비타민 K (μg)",
        ["vitamin_b1_mg"] = "비타민 B1/티아민 (mg)",
        ["vitamin_b2_mg"] = "비타민 B2/리보플라빈 (mg)",
        ["vitamin_b3_mg"] = "비타민 B3/나이아신 (mg NE)",
        ["vitamin_b5_mg"] = "비타민 B5/판토텐산 (mg)",
        ["vitamin_b6_mg"] = "비타민 B6/피리독신 (mg)",
        ["vitamin_b7_ug"] = "비타민 B7/비오틴 (μg)",
        ["vitamin_b9_ug"] = "비타민 B9/엽산 (μg DFE)",
        ["vitamin_b12_ug"] = "비타민 B12/코발라민 (μg)"
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            if (NutrientNames.TryGetValue(key, out var koreanName))
            {
                return koreanName;
            }
            // 매핑이 없으면 원본 반환
            return key;
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

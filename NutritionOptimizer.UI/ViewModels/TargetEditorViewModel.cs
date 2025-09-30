using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 목표치 추가/수정을 위한 ViewModel
public sealed partial class TargetEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string nutrientKey = string.Empty;

    [ObservableProperty]
    private string minValue = string.Empty;

    [ObservableProperty]
    private string maxValue = string.Empty;

    [ObservableProperty]
    private string recommendedValue = string.Empty;

    [ObservableProperty]
    private string sufficientValue = string.Empty;

    [ObservableProperty]
    private string unit = string.Empty;

    public bool IsEditMode { get; }

    // 새 목표치 추가 모드
    public TargetEditorViewModel()
    {
        IsEditMode = false;
    }

    // 기존 목표치 수정 모드
    public TargetEditorViewModel(NutrientTarget target)
    {
        IsEditMode = true;
        NutrientKey = target.NutrientKey;
        MinValue = target.Min?.ToString() ?? string.Empty;
        MaxValue = target.Max?.ToString() ?? string.Empty;
        RecommendedValue = target.Recommended?.ToString() ?? string.Empty;
        SufficientValue = target.Sufficient?.ToString() ?? string.Empty;
        Unit = target.Unit;
    }

    // 입력 값을 NutrientTarget 객체로 변환
    public NutrientTarget ToTarget()
    {
        return new NutrientTarget(
            NutrientKey,
            ParseNullable(MinValue),
            ParseNullable(MaxValue),
            ParseNullable(RecommendedValue),
            ParseNullable(SufficientValue),
            Unit
        );
    }

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(NutrientKey) &&
               !string.IsNullOrWhiteSpace(Unit);
    }

    // 문자열을 double?로 변환
    private static double? ParseNullable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return double.TryParse(value, out var result) ? result : null;
    }
}

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 가격 이력 추가를 위한 ViewModel
public sealed partial class PriceHistoryEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string foodId = string.Empty;

    [ObservableProperty]
    private string foodName = string.Empty;

    [ObservableProperty]
    private DateOnly effectiveDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private double pricePer100g;

    public PriceHistoryEditorViewModel()
    {
    }

    // 특정 음식에 대한 가격 추가
    public PriceHistoryEditorViewModel(string foodId, string foodName)
    {
        FoodId = foodId;
        FoodName = foodName;
    }

    // 입력 값을 PriceHistory 객체로 변환
    public PriceHistory ToPriceHistory()
    {
        return new PriceHistory(
            FoodId,
            EffectiveDate,
            PricePer100g
        );
    }

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FoodId) && PricePer100g > 0;
    }
}

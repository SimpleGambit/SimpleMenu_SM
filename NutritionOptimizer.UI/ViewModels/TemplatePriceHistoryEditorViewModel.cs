using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 템플릿 가격 이력 추가/수정을 위한 ViewModel
public sealed partial class TemplatePriceHistoryEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string templateId = string.Empty;

    [ObservableProperty]
    private string templateName = string.Empty;

    [ObservableProperty]
    private string brand = string.Empty;

    [ObservableProperty]
    private DateOnly effectiveDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private string weightUnit = "1kg";

    [ObservableProperty]
    private double price;

    public bool IsEditMode { get; }

    // 새 가격 이력 추가
    public TemplatePriceHistoryEditorViewModel(string templateId, string templateName, string templateBrand)
    {
        IsEditMode = false;
        TemplateId = templateId;
        TemplateName = templateName;
        Brand = templateBrand ?? string.Empty;  // 템플릿의 브랜드를 사용
    }

    // 기존 가격 이력 수정
    public TemplatePriceHistoryEditorViewModel(TemplatePriceHistory priceHistory, string templateName)
    {
        IsEditMode = true;
        TemplateId = priceHistory.TemplateId;
        TemplateName = templateName;
        Brand = priceHistory.Brand;
        EffectiveDate = priceHistory.EffectiveDate;
        WeightUnit = priceHistory.WeightUnit;
        Price = priceHistory.Price;
    }

    // TemplatePriceHistory로 변환
    public TemplatePriceHistory ToPriceHistory()
    {
        return new TemplatePriceHistory(
            TemplateId,
            Brand,
            EffectiveDate,
            WeightUnit,
            Price
        );
    }

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(TemplateId) &&
               !string.IsNullOrWhiteSpace(WeightUnit) &&
               Price > 0;
    }
}

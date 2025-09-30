using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NutritionOptimizer.UI.ViewModels;

/// <summary>
/// 수동 식단 구성 항목 (음식 + 양)
/// </summary>
public sealed partial class ManualDietItem : ObservableObject
{
    [ObservableProperty]
    private string foodId = string.Empty;

    [ObservableProperty]
    private string foodName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayAmount))]
    private double amountG = 100; // 기본 100g

    [ObservableProperty]
    private double pricePer100g;

    [ObservableProperty]
    private double kcal;

    [ObservableProperty]
    private double proteinG;

    [ObservableProperty]
    private double fatG;

    [ObservableProperty]
    private double carbsG;

    [ObservableProperty]
    private double fiberG;

    [ObservableProperty]
    private double sodiumMg;

    [ObservableProperty]
    private double saturatedFatG;

    [ObservableProperty]
    private double transFatG;

    [ObservableProperty]
    private double sugarG;

    [ObservableProperty]
    private double cholesterolMg;

    [ObservableProperty]
    private double calciumMg;

    [ObservableProperty]
    private double ironMg;

    [ObservableProperty]
    private double magnesiumMg;

    [ObservableProperty]
    private double phosphorusMg;

    [ObservableProperty]
    private double potassiumMg;

    [ObservableProperty]
    private double zincMg;

    [ObservableProperty]
    private double copperMg;

    [ObservableProperty]
    private double manganeseMg;

    [ObservableProperty]
    private double seleniumUg;

    [ObservableProperty]
    private double molybdenumUg;

    [ObservableProperty]
    private double iodineUg;

    [ObservableProperty]
    private double vitaminAUg;

    [ObservableProperty]
    private double vitaminCMg;

    [ObservableProperty]
    private double vitaminDUg;

    [ObservableProperty]
    private double vitaminEMg;

    [ObservableProperty]
    private double vitaminKUg;

    [ObservableProperty]
    private double vitaminB1Mg;

    [ObservableProperty]
    private double vitaminB2Mg;

    [ObservableProperty]
    private double vitaminB3Mg;

    [ObservableProperty]
    private double vitaminB5Mg;

    [ObservableProperty]
    private double vitaminB6Mg;

    [ObservableProperty]
    private double vitaminB7Ug;

    [ObservableProperty]
    private double vitaminB9Ug;

    [ObservableProperty]
    private double vitaminB12Ug;

    public string DisplayAmount => $"{Math.Round(AmountG, 1)}g";

    // 양 변경 시 알림을 위한 이벤트
    partial void OnAmountGChanged(double value)
    {
        // 이벤트 발생 (MainViewModel에서 구독)
    }
}

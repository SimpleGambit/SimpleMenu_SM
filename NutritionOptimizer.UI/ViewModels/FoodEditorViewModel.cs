using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 음식 추가/수정을 위한 ViewModel
public sealed partial class FoodEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string brand = string.Empty;

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private double pricePer100g;

    [ObservableProperty]
    private string weightUnit = "100g";

    [ObservableProperty]
    private double kcal;

    [ObservableProperty]
    private double proteinG;

    [ObservableProperty]
    private double fatG;

    [ObservableProperty]
    private double saturatedFatG;

    [ObservableProperty]
    private double transFatG;

    [ObservableProperty]
    private double carbsG;

    [ObservableProperty]
    private double fiberG;

    [ObservableProperty]
    private double sugarG;

    [ObservableProperty]
    private double sodiumMg;

    [ObservableProperty]
    private double cholesterolMg;

    // 무기질
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

    // 비타민
    [ObservableProperty]
    private double vitaminAUg;

    [ObservableProperty]
    private double vitaminCMg;

    [ObservableProperty]
    private double vitaminDUg;

    [ObservableProperty]
    private double vitaminEMg;

    [ObservableProperty]
    private double vitaminB1Mg;

    [ObservableProperty]
    private double vitaminB2Mg;

    [ObservableProperty]
    private double vitaminB3Mg;

    [ObservableProperty]
    private double vitaminB6Mg;

    [ObservableProperty]
    private double vitaminB9Ug;

    [ObservableProperty]
    private double vitaminB12Ug;

    public bool IsEditMode { get; }

    // 새 음식 추가 모드
    public FoodEditorViewModel()
    {
        IsEditMode = false;
        // 자동으로 새 ID 생성
        Id = GenerateNewId();
    }

    // 기존 음식 수정 모드
    public FoodEditorViewModel(Food food)
    {
        IsEditMode = true;
        Id = food.Id;
        Name = food.Name;
        Brand = food.Brand ?? string.Empty;
        Category = food.Category ?? string.Empty;
        PricePer100g = food.PricePer100g;
        WeightUnit = food.WeightUnit;
        Kcal = food.Kcal;
        ProteinG = food.ProteinG;
        FatG = food.FatG;
        SaturatedFatG = food.SaturatedFatG;
        TransFatG = food.TransFatG;
        CarbsG = food.CarbsG;
        FiberG = food.FiberG;
        SugarG = food.SugarG;
        SodiumMg = food.SodiumMg;
        CholesterolMg = food.CholesterolMg;
        CalciumMg = food.CalciumMg;
        IronMg = food.IronMg;
        MagnesiumMg = food.MagnesiumMg;
        PhosphorusMg = food.PhosphorusMg;
        PotassiumMg = food.PotassiumMg;
        ZincMg = food.ZincMg;
        VitaminAUg = food.VitaminAUg;
        VitaminCMg = food.VitaminCMg;
        VitaminDUg = food.VitaminDUg;
        VitaminEMg = food.VitaminEMg;
        VitaminB1Mg = food.VitaminB1Mg;
        VitaminB2Mg = food.VitaminB2Mg;
        VitaminB3Mg = food.VitaminB3Mg;
        VitaminB6Mg = food.VitaminB6Mg;
        VitaminB9Ug = food.VitaminB9Ug;
        VitaminB12Ug = food.VitaminB12Ug;
    }

    // 입력 값을 Food 객체로 변환
    public Food ToFood()
    {
        return new Food(
            Id,
            Name,
            string.IsNullOrWhiteSpace(Brand) ? null : Brand,
            string.IsNullOrWhiteSpace(Category) ? null : Category,
            PricePer100g,
            WeightUnit,
            Kcal,
            ProteinG,
            FatG,
            SaturatedFatG,
            TransFatG,
            CarbsG,
            FiberG,
            SugarG,
            SodiumMg,
            CholesterolMg,
            CalciumMg,
            IronMg,
            MagnesiumMg,
            PhosphorusMg,
            PotassiumMg,
            ZincMg,
            VitaminAUg,
            VitaminCMg,
            VitaminDUg,
            VitaminEMg,
            VitaminB1Mg,
            VitaminB2Mg,
            VitaminB3Mg,
            VitaminB6Mg,
            VitaminB9Ug,
            VitaminB12Ug
        );
    }

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(WeightUnit);
    }

    // 새 ID 생성 (현재 시간 기반)
    private static string GenerateNewId()
    {
        return $"food_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }
}

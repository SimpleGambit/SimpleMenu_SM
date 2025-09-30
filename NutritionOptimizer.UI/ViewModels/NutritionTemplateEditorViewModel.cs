using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 영양성분 템플릿 추가/수정을 위한 ViewModel
public sealed partial class NutritionTemplateEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string type = "식재료";  // 기본값: "식재료"

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private string brand = string.Empty;  // 브랜드

    [ObservableProperty]
    private string weightUnit = "100g";  // 기본 단위: 100g

    [ObservableProperty]
    private double kcal;

    [ObservableProperty]
    private double moistureG;  // 수분

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

    public bool IsEditMode { get; }

    // 새 템플릿 추가 모드
    public NutritionTemplateEditorViewModel()
    {
        IsEditMode = false;
        Id = GenerateNewId();
    }

    // 기존 템플릿 수정 모드
    public NutritionTemplateEditorViewModel(NutritionTemplate template)
    {
        IsEditMode = true;
        Id = template.Id;
        Name = template.Name;
        Type = template.Type;
        Category = template.Category;
        Brand = template.Brand ?? string.Empty;
        WeightUnit = template.WeightUnit;
        Kcal = template.Kcal;
        MoistureG = template.MoistureG;
        ProteinG = template.ProteinG;
        FatG = template.FatG;
        SaturatedFatG = template.SaturatedFatG;
        TransFatG = template.TransFatG;
        CarbsG = template.CarbsG;
        FiberG = template.FiberG;
        SugarG = template.SugarG;
        SodiumMg = template.SodiumMg;
        CholesterolMg = template.CholesterolMg;
        CalciumMg = template.CalciumMg;
        IronMg = template.IronMg;
        MagnesiumMg = template.MagnesiumMg;
        PhosphorusMg = template.PhosphorusMg;
        PotassiumMg = template.PotassiumMg;
        ZincMg = template.ZincMg;
        CopperMg = template.CopperMg;
        ManganeseMg = template.ManganeseMg;
        SeleniumUg = template.SeleniumUg;
        MolybdenumUg = template.MolybdenumUg;
        IodineUg = template.IodineUg;
        VitaminAUg = template.VitaminAUg;
        VitaminCMg = template.VitaminCMg;
        VitaminDUg = template.VitaminDUg;
        VitaminEMg = template.VitaminEMg;
        VitaminKUg = template.VitaminKUg;
        VitaminB1Mg = template.VitaminB1Mg;
        VitaminB2Mg = template.VitaminB2Mg;
        VitaminB3Mg = template.VitaminB3Mg;
        VitaminB5Mg = template.VitaminB5Mg;
        VitaminB6Mg = template.VitaminB6Mg;
        VitaminB7Ug = template.VitaminB7Ug;
        VitaminB9Ug = template.VitaminB9Ug;
        VitaminB12Ug = template.VitaminB12Ug;
    }

    // ViewModel을 NutritionTemplate 객체로 변환
    // 가격 정보는 가격 이력에서 관리하므로 기본값 사용
    public NutritionTemplate ToTemplate()
    {
        return new NutritionTemplate(
            Id,
            Name,
            Type,
            Category,
            string.IsNullOrWhiteSpace(Brand) ? null : Brand,  // Brand
            WeightUnit,  // WeightUnit - 사용자가 선택한 단위 (100g 또는 100ml)
            0,           // TotalPrice - 가격 이력에서 관리
            Kcal,
            MoistureG,
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
            CopperMg,
            ManganeseMg,
            SeleniumUg,
            MolybdenumUg,
            IodineUg,
            VitaminAUg,
            VitaminCMg,
            VitaminDUg,
            VitaminEMg,
            VitaminKUg,
            VitaminB1Mg,
            VitaminB2Mg,
            VitaminB3Mg,
            VitaminB5Mg,
            VitaminB6Mg,
            VitaminB7Ug,
            VitaminB9Ug,
            VitaminB12Ug
        );
    }

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Type) &&
               !string.IsNullOrWhiteSpace(Category);
    }

    // 새 ID 생성
    private static string GenerateNewId()
    {
        return $"tmpl_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }
}

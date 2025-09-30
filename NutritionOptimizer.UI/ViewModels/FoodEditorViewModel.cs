using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 음식 추가/수정을 위한 ViewModel
public sealed partial class FoodEditorViewModel : ObservableObject
{
    private readonly INutritionTemplateRepository? _templateRepository;
    private NutritionTemplate? _currentTemplate;

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

    // 메모
    [ObservableProperty]
    private string notes = string.Empty;

    // 템플릿 목록
    [ObservableProperty]
    private ObservableCollection<NutritionTemplate> templates = new();

    [ObservableProperty]
    private NutritionTemplate? selectedTemplate;

    public bool IsEditMode { get; }

    // 새 음식 추가 모드
    public FoodEditorViewModel() : this(null)
    {
    }

    // Repository를 받는 생성자 (새 음식 추가)
    public FoodEditorViewModel(INutritionTemplateRepository? templateRepository)
    {
        IsEditMode = false;
        _templateRepository = templateRepository;
        Id = GenerateNewId();
        LoadTemplatesAsync();
    }

    // 기존 음식 수정 모드
    public FoodEditorViewModel(Food food, INutritionTemplateRepository? templateRepository = null)
    {
        IsEditMode = true;
        _templateRepository = templateRepository;
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
        Notes = food.Notes ?? string.Empty;
        LoadTemplatesAsync();
    }

    // 템플릿 목록 불러오기
    private async void LoadTemplatesAsync()
    {
        if (_templateRepository == null) return;
        
        var loadedTemplates = await _templateRepository.GetAllAsync();
        Templates.Clear();
        foreach (var template in loadedTemplates.OrderBy(t => t.Name))
        {
            Templates.Add(template);
        }
    }

    // 템플릿 선택 시 영양성분 적용
    partial void OnSelectedTemplateChanged(NutritionTemplate? value)
    {
        if (value == null) return;
        
        _currentTemplate = value;
        Name = value.Name;
        Category = value.Category;
        
        // 단위가 설정되어 있으면 계산, 없으면 100g 기준으로 적용
        ApplyTemplateWithUnit();
    }

    // 단위 변경 시 영양성분 재계산
    partial void OnWeightUnitChanged(string value)
    {
        if (_currentTemplate != null && !string.IsNullOrWhiteSpace(value))
        {
            ApplyTemplateWithUnit();
        }
    }

    // 템플릿 적용 (단위 고려)
    private void ApplyTemplateWithUnit()
    {
        if (_currentTemplate == null) return;

        double multiplier = CalculateMultiplier(WeightUnit);

        Kcal = _currentTemplate.Kcal * multiplier;
        ProteinG = _currentTemplate.ProteinG * multiplier;
        FatG = _currentTemplate.FatG * multiplier;
        SaturatedFatG = _currentTemplate.SaturatedFatG * multiplier;
        TransFatG = _currentTemplate.TransFatG * multiplier;
        CarbsG = _currentTemplate.CarbsG * multiplier;
        FiberG = _currentTemplate.FiberG * multiplier;
        SugarG = _currentTemplate.SugarG * multiplier;
        SodiumMg = _currentTemplate.SodiumMg * multiplier;
        CholesterolMg = _currentTemplate.CholesterolMg * multiplier;
        CalciumMg = _currentTemplate.CalciumMg * multiplier;
        IronMg = _currentTemplate.IronMg * multiplier;
        MagnesiumMg = _currentTemplate.MagnesiumMg * multiplier;
        PhosphorusMg = _currentTemplate.PhosphorusMg * multiplier;
        PotassiumMg = _currentTemplate.PotassiumMg * multiplier;
        ZincMg = _currentTemplate.ZincMg * multiplier;
        VitaminAUg = _currentTemplate.VitaminAUg * multiplier;
        VitaminCMg = _currentTemplate.VitaminCMg * multiplier;
        VitaminDUg = _currentTemplate.VitaminDUg * multiplier;
        VitaminEMg = _currentTemplate.VitaminEMg * multiplier;
        VitaminB1Mg = _currentTemplate.VitaminB1Mg * multiplier;
        VitaminB2Mg = _currentTemplate.VitaminB2Mg * multiplier;
        VitaminB3Mg = _currentTemplate.VitaminB3Mg * multiplier;
        VitaminB6Mg = _currentTemplate.VitaminB6Mg * multiplier;
        VitaminB9Ug = _currentTemplate.VitaminB9Ug * multiplier;
        VitaminB12Ug = _currentTemplate.VitaminB12Ug * multiplier;
    }

    // 단위에서 100g 대비 배수 계산
    private static double CalculateMultiplier(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        // 숫자 부분 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            // 단위 부분 추출
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            // g 단위면 100으로 나누고, ml도 동일하게 처리
            if (unit.StartsWith("g") || unit.StartsWith("ml"))
            {
                return quantity / 100.0;
            }
            // kg이면 1000g이므로 10배
            else if (unit.StartsWith("kg"))
            {
                return quantity * 10.0;
            }
            // l이면 1000ml이므로 10배
            else if (unit.StartsWith("l"))
            {
                return quantity * 10.0;
            }
        }
        
        return 1.0; // 기본값
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
            VitaminB12Ug,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes
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

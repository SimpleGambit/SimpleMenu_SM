using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 선택된 템플릿 항목 (단위, 브랜드와 함께)
public sealed partial class SelectedTemplateItem : ObservableObject
{
    private readonly ITemplatePriceHistoryRepository? _priceHistoryRepository;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Unit))]
    private NutritionTemplate template;

    [ObservableProperty]
    private double amountG = 100;  // 기본 100 (g 또는 ml)
    
    [ObservableProperty]
    private string? selectedBrand;
    
    [ObservableProperty]
    private ObservableCollection<string> availableBrands = new();
    
    [ObservableProperty]
    private double calculatedPrice;  // 계산된 가격 (브랜드 + 그램 기반)

    // 템플릿의 단위 (g 또는 ml)
    public string Unit => GetUnitFromTemplate();

    public SelectedTemplateItem(NutritionTemplate template, ITemplatePriceHistoryRepository? priceHistoryRepository, double amountG = 100)
    {
        this.template = template;
        this._priceHistoryRepository = priceHistoryRepository;
        this.amountG = amountG;
        LoadAvailableBrandsAsync();
    }
    
    // 템플릿의 WeightUnit에서 단위 추출 (g 또는 ml)
    private string GetUnitFromTemplate()
    {
        if (string.IsNullOrWhiteSpace(Template.WeightUnit))
            return "g";
        
        var unit = Template.WeightUnit.ToLower();
        if (unit.Contains("ml"))
            return "ml";
        else
            return "g";
    }
    
    // 브랜드 목록 로드
    private async void LoadAvailableBrandsAsync()
    {
        if (_priceHistoryRepository == null)
        {
            AvailableBrands.Clear();
            return;
        }
        
        var priceHistories = await _priceHistoryRepository.GetByTemplateIdAsync(Template.Id);
        var brands = priceHistories.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList();
        
        AvailableBrands.Clear();
        foreach (var brand in brands)
        {
            AvailableBrands.Add(brand);
        }
        
        // 첫 번째 브랜드를 기본 선택
        if (AvailableBrands.Any())
        {
            SelectedBrand = AvailableBrands.First();
        }
    }
    
    // 브랜드 변경 시 가격 재계산
    partial void OnSelectedBrandChanged(string? value)
    {
        CalculatePriceAsync();
    }
    
    // 그램 변경 시 가격 재계산
    partial void OnAmountGChanged(double value)
    {
        CalculatePriceAsync();
    }
    
    // 가격 계산
    private async void CalculatePriceAsync()
    {
        if (_priceHistoryRepository == null || string.IsNullOrWhiteSpace(SelectedBrand))
        {
            CalculatedPrice = 0;
            return;
        }
        
        var latestPrice = await _priceHistoryRepository.GetLatestPriceAsync(Template.Id, SelectedBrand);
        if (latestPrice != null)
        {
            // 가격 이력의 단위(예: 1kg, 500g)를 100g 기준으로 변환
            double priceHistoryMultiplier = CalculateMultiplierFromUnit(latestPrice.WeightUnit);
            double pricePer100g = latestPrice.Price / priceHistoryMultiplier;
            
            // 입력된 그램에 따라 가격 계산
            double multiplier = AmountG / 100.0;
            CalculatedPrice = Math.Round(pricePer100g * multiplier, 2);
        }
        else
        {
            CalculatedPrice = 0;
        }
    }
    
    // 단위 문자열에서 배수 계산 (100g 기준)
    private static double CalculateMultiplierFromUnit(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        // 숫자 부분 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            // 단위 부분 추출
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            // g 또는 ml 단위면 100으로 나누기
            if (unit.StartsWith("g") || unit.StartsWith("ml"))
            {
                return quantity / 100.0;
            }
            // kg 또는 l이면 1000g이므로 10배
            else if (unit.StartsWith("kg"))
            {
                return quantity * 10.0;
            }
            else if (unit.StartsWith("l") && !unit.StartsWith("lb")) // lb는 제외
            {
                return quantity * 10.0;
            }
        }
        
        return 1.0; // 기본값
    }

    public string DisplayName => $"{Template.Name} ({AmountG}{Unit})" + 
                                 (string.IsNullOrWhiteSpace(SelectedBrand) ? "" : $" - {SelectedBrand}");
}

// 음식 추가/수정을 위한 ViewModel
public sealed partial class FoodEditorViewModel : ObservableObject
{
    private readonly INutritionTemplateRepository? _templateRepository;
    private readonly ITemplatePriceHistoryRepository? _priceHistoryRepository;
    private readonly ICookingLossRateRepository? _cookingLossRateRepository;
    private readonly IFoodTemplateUsageRepository? _foodTemplateUsageRepository;

    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string type = "식재료";  // 템플릿에서 가져오기

    [ObservableProperty]
    private string category = string.Empty;  // 템플릿에서 가져오기

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeightUnit))]
    private string cookingMethod = "없음";  // 조리 방법: "없음", "가열조리", "튀김", "취사"

    [ObservableProperty]
    private double pricePer100g;

    [ObservableProperty]
    private double totalPrice;

    [ObservableProperty]
    private string weightUnit = "100g";

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

    // 메모
    [ObservableProperty]
    private string notes = string.Empty;

    // 템플릿 목록
    [ObservableProperty]
    private ObservableCollection<NutritionTemplate> templates = new();

    // 선택된 템플릿 목록 (여러 개)
    public ObservableCollection<SelectedTemplateItem> SelectedTemplates { get; } = new();

    [ObservableProperty]
    private NutritionTemplate? currentTemplate;  // 추가할 템플릿 선택용

    public bool IsEditMode { get; }

    // 새 음식 추가 모드 (매개변수 없는 기본 생성자)
    public FoodEditorViewModel() : this(null, null, null, null)
    {
    }

    // Repository를 받는 생성자 (새 음식 추가)
    public FoodEditorViewModel(INutritionTemplateRepository? templateRepository, ITemplatePriceHistoryRepository? priceHistoryRepository, ICookingLossRateRepository? cookingLossRateRepository, IFoodTemplateUsageRepository? foodTemplateUsageRepository = null)
    {
        IsEditMode = false;
        _templateRepository = templateRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _cookingLossRateRepository = cookingLossRateRepository;
        _foodTemplateUsageRepository = foodTemplateUsageRepository;
        Id = GenerateNewId();
        
        // 템플릿 목록 변경 시 영양성분 재계산
        SelectedTemplates.CollectionChanged += (s, e) => RecalculateNutrients();
        
        LoadTemplatesAsync();
    }

    // 기존 음식 수정 모드
    public FoodEditorViewModel(Food food, INutritionTemplateRepository? templateRepository = null, ITemplatePriceHistoryRepository? priceHistoryRepository = null, ICookingLossRateRepository? cookingLossRateRepository = null, IFoodTemplateUsageRepository? foodTemplateUsageRepository = null)
    {
        IsEditMode = true;
        _templateRepository = templateRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _cookingLossRateRepository = cookingLossRateRepository;
        _foodTemplateUsageRepository = foodTemplateUsageRepository;
        Id = food.Id;
        Name = food.Name;
        Type = food.Type;
        Category = food.Category;
        CookingMethod = food.CookingMethod;
        PricePer100g = food.PricePer100g;
        WeightUnit = food.WeightUnit;
        
        // 기존 데이터에서 총 가격 계산 (역계산)
        double multiplier = CalculateMultiplier(WeightUnit);
        TotalPrice = multiplier > 0 ? Math.Round(food.PricePer100g * multiplier, 2) : 0;
        
        // 저장된 영양성분은 100g 기준이므로, UI에 표시할 때는 WeightUnit에 맞게 곱함
        Kcal = Math.Round(food.Kcal * multiplier, 2);
        MoistureG = Math.Round(food.MoistureG * multiplier, 2);
        ProteinG = Math.Round(food.ProteinG * multiplier, 2);
        FatG = Math.Round(food.FatG * multiplier, 2);
        SaturatedFatG = Math.Round(food.SaturatedFatG * multiplier, 2);
        TransFatG = Math.Round(food.TransFatG * multiplier, 2);
        CarbsG = Math.Round(food.CarbsG * multiplier, 2);
        FiberG = Math.Round(food.FiberG * multiplier, 2);
        SugarG = Math.Round(food.SugarG * multiplier, 2);
        SodiumMg = Math.Round(food.SodiumMg * multiplier, 2);
        CholesterolMg = Math.Round(food.CholesterolMg * multiplier, 2);
        CalciumMg = Math.Round(food.CalciumMg * multiplier, 2);
        IronMg = Math.Round(food.IronMg * multiplier, 2);
        MagnesiumMg = Math.Round(food.MagnesiumMg * multiplier, 2);
        PhosphorusMg = Math.Round(food.PhosphorusMg * multiplier, 2);
        PotassiumMg = Math.Round(food.PotassiumMg * multiplier, 2);
        ZincMg = Math.Round(food.ZincMg * multiplier, 2);
        CopperMg = Math.Round(food.CopperMg * multiplier, 2);
        ManganeseMg = Math.Round(food.ManganeseMg * multiplier, 2);
        SeleniumUg = Math.Round(food.SeleniumUg * multiplier, 2);
        MolybdenumUg = Math.Round(food.MolybdenumUg * multiplier, 2);
        IodineUg = Math.Round(food.IodineUg * multiplier, 2);
        VitaminAUg = Math.Round(food.VitaminAUg * multiplier, 2);
        VitaminCMg = Math.Round(food.VitaminCMg * multiplier, 2);
        VitaminDUg = Math.Round(food.VitaminDUg * multiplier, 2);
        VitaminEMg = Math.Round(food.VitaminEMg * multiplier, 2);
        VitaminKUg = Math.Round(food.VitaminKUg * multiplier, 2);
        VitaminB1Mg = Math.Round(food.VitaminB1Mg * multiplier, 2);
        VitaminB2Mg = Math.Round(food.VitaminB2Mg * multiplier, 2);
        VitaminB3Mg = Math.Round(food.VitaminB3Mg * multiplier, 2);
        VitaminB5Mg = Math.Round(food.VitaminB5Mg * multiplier, 2);
        VitaminB6Mg = Math.Round(food.VitaminB6Mg * multiplier, 2);
        VitaminB7Ug = Math.Round(food.VitaminB7Ug * multiplier, 2);
        VitaminB9Ug = Math.Round(food.VitaminB9Ug * multiplier, 2);
        VitaminB12Ug = Math.Round(food.VitaminB12Ug * multiplier, 2);
        Notes = food.Notes ?? string.Empty;
        
        // 템플릿 목록 및 사용 정보 불러오기
        LoadTemplatesAsync();
        LoadTemplateUsagesAsync(food.Id);
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

    // 기존 음식의 템플릿 사용 정보 불러오기 (수정 모드)
    private async void LoadTemplateUsagesAsync(string foodId)
    {
        if (_foodTemplateUsageRepository == null || _templateRepository == null)
            return;

        try
        {
            var usages = await _foodTemplateUsageRepository.GetByFoodIdAsync(foodId);
            if (!usages.Any())
                return;

            // 템플릿 목록이 로드될 때까지 대기
            while (!Templates.Any())
            {
                await System.Threading.Tasks.Task.Delay(100);
            }

            // CollectionChanged 이벤트 일시 해제 (자동 재계산 방지)
            SelectedTemplates.CollectionChanged -= OnSelectedTemplatesChanged;

            foreach (var usage in usages)
            {
                var template = Templates.FirstOrDefault(t => t.Id == usage.TemplateId);
                if (template != null)
                {
                    var item = new SelectedTemplateItem(template, _priceHistoryRepository, usage.AmountG);
                    
                    // PropertyChanged 이벤트 등록
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectedTemplateItem.AmountG) || 
                            e.PropertyName == nameof(SelectedTemplateItem.SelectedBrand) ||
                            e.PropertyName == nameof(SelectedTemplateItem.CalculatedPrice))
                        {
                            RecalculateNutrients();
                        }
                    };
                    
                    SelectedTemplates.Add(item);
                }
            }

            // CollectionChanged 이벤트 다시 등록
            SelectedTemplates.CollectionChanged += OnSelectedTemplatesChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"템플릿 사용 정보 로드 실패: {ex.Message}");
        }
    }

    // CollectionChanged 이벤트 핸들러
    private void OnSelectedTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateNutrients();
    }

    // 조리 방법 변경 시 영양성분 재계산
    partial void OnCookingMethodChanged(string value)
    {
        RecalculateNutrients();
    }

    // 템플릿 추가
    [RelayCommand]
    private void AddTemplate()
    {
        if (CurrentTemplate == null)
        {
            System.Windows.MessageBox.Show("추가할 템플릿을 선택해주세요.");
            return;
        }

        // 이미 추가된 템플릿인지 확인
        if (SelectedTemplates.Any(st => st.Template.Id == CurrentTemplate.Id))
        {
            System.Windows.MessageBox.Show("이미 추가된 템플릿입니다.");
            return;
        }

        var item = new SelectedTemplateItem(CurrentTemplate, _priceHistoryRepository, 100);
        
        // AmountG 또는 SelectedBrand 변경 시 재계산
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedTemplateItem.AmountG) || 
                e.PropertyName == nameof(SelectedTemplateItem.SelectedBrand) ||
                e.PropertyName == nameof(SelectedTemplateItem.CalculatedPrice))
            {
                RecalculateNutrients();
            }
        };
        
        SelectedTemplates.Add(item);
        CurrentTemplate = null;  // 선택 초기화
    }

    // 템플릿 제거
    [RelayCommand]
    private void RemoveTemplate(SelectedTemplateItem item)
    {
        SelectedTemplates.Remove(item);
    }

    // 모든 선택된 템플릿의 영양성분 합산
    private void RecalculateNutrients()
    {
        if (!SelectedTemplates.Any())
        {
            // 템플릿이 없으면 모두 0으로 초기화
            ClearNutrients();
            return;
        }

        // 수정 모드가 아닐 때만 Type, Category, Name을 자동 설정
        if (!IsEditMode)
        {
            // 첫 번째 템플릿의 Type과 Category 사용
            var firstTemplate = SelectedTemplates.First().Template;
            
            // 템플릿 개수에 따라 Type 설정
            if (SelectedTemplates.Count == 1)
            {
                // 템플릿 1개: 템플릿의 Type을 그대로 사용
                Type = firstTemplate.Type;
                Category = firstTemplate.Category;  // 템플릿에서 가져오되 수정 가능
            }
            else
            {
                // 템플릿 2개 이상: 요리
                Type = "요리";
                // Category는 사용자가 직접 입력 (초기값만 설정)
                if (string.IsNullOrWhiteSpace(Category) || Category == firstTemplate.Category)
                {
                    Category = "복합 요리";  // 기본값
                }
            }
            
            // Name이 비어있으면 템플릿 이름들을 합쳐서 자동 설정
            if (string.IsNullOrWhiteSpace(Name) || Name == "혼합")
            {
                if (SelectedTemplates.Count == 1)
                {
                    Name = firstTemplate.Name;
                }
                else
                {
                    Name = string.Join(" + ", SelectedTemplates.Select(st => st.Template.Name));
                }
            }
        }

        // 총 가격 및 총 무게 계산
        double totalPrice = 0;
        double totalWeightG = 0;

        // 각 영양소 합계
        double sumKcal = 0, sumMoistureG = 0, sumProteinG = 0, sumFatG = 0, sumSaturatedFatG = 0, sumTransFatG = 0;
        double sumCarbsG = 0, sumFiberG = 0, sumSugarG = 0, sumSodiumMg = 0, sumCholesterolMg = 0;
        double sumCalciumMg = 0, sumIronMg = 0, sumMagnesiumMg = 0, sumPhosphorusMg = 0, sumPotassiumMg = 0;
        double sumZincMg = 0, sumCopperMg = 0, sumManganeseMg = 0, sumSeleniumUg = 0, sumMolybdenumUg = 0, sumIodineUg = 0;
        double sumVitaminAUg = 0, sumVitaminCMg = 0, sumVitaminDUg = 0, sumVitaminEMg = 0, sumVitaminKUg = 0;
        double sumVitaminB1Mg = 0, sumVitaminB2Mg = 0, sumVitaminB3Mg = 0, sumVitaminB5Mg = 0, sumVitaminB6Mg = 0;
        double sumVitaminB7Ug = 0, sumVitaminB9Ug = 0, sumVitaminB12Ug = 0;

        foreach (var item in SelectedTemplates)
        {
            var template = item.Template;
            double amountG = item.AmountG;
            double multiplier = amountG / 100.0;  // 100g 기준으로 변환

            totalWeightG += amountG;

            // 각 템플릿의 CalculatedPrice 사용 (브랜드별 가격 이력에서 계산됨)
            totalPrice += item.CalculatedPrice;

            // 영양성분 합산
            sumKcal += template.Kcal * multiplier;
            sumMoistureG += template.MoistureG * multiplier;
            sumProteinG += template.ProteinG * multiplier;
            sumFatG += template.FatG * multiplier;
            sumSaturatedFatG += template.SaturatedFatG * multiplier;
            sumTransFatG += template.TransFatG * multiplier;
            sumCarbsG += template.CarbsG * multiplier;
            sumFiberG += template.FiberG * multiplier;
            sumSugarG += template.SugarG * multiplier;
            sumSodiumMg += template.SodiumMg * multiplier;
            sumCholesterolMg += template.CholesterolMg * multiplier;
            sumCalciumMg += template.CalciumMg * multiplier;
            sumIronMg += template.IronMg * multiplier;
            sumMagnesiumMg += template.MagnesiumMg * multiplier;
            sumPhosphorusMg += template.PhosphorusMg * multiplier;
            sumPotassiumMg += template.PotassiumMg * multiplier;
            sumZincMg += template.ZincMg * multiplier;
            sumCopperMg += template.CopperMg * multiplier;
            sumManganeseMg += template.ManganeseMg * multiplier;
            sumSeleniumUg += template.SeleniumUg * multiplier;
            sumMolybdenumUg += template.MolybdenumUg * multiplier;
            sumIodineUg += template.IodineUg * multiplier;
            sumVitaminAUg += template.VitaminAUg * multiplier;
            sumVitaminCMg += template.VitaminCMg * multiplier;
            sumVitaminDUg += template.VitaminDUg * multiplier;
            sumVitaminEMg += template.VitaminEMg * multiplier;
            sumVitaminKUg += template.VitaminKUg * multiplier;
            sumVitaminB1Mg += template.VitaminB1Mg * multiplier;
            sumVitaminB2Mg += template.VitaminB2Mg * multiplier;
            sumVitaminB3Mg += template.VitaminB3Mg * multiplier;
            sumVitaminB5Mg += template.VitaminB5Mg * multiplier;
            sumVitaminB6Mg += template.VitaminB6Mg * multiplier;
            sumVitaminB7Ug += template.VitaminB7Ug * multiplier;
            sumVitaminB9Ug += template.VitaminB9Ug * multiplier;
            sumVitaminB12Ug += template.VitaminB12Ug * multiplier;
        }

        // WeightUnit 설정 (총 무게)
        // 취사 방법이고 쌀이 있으면 2.8배 증가
        bool isRiceCooking = CookingMethod == "취사" && SelectedTemplates.Any(st => st.Template.Category.Contains("쌀"));
        if (isRiceCooking)
        {
            WeightUnit = $"{Math.Round(totalWeightG * 2.8, 1)}g";
        }
        else
        {
            WeightUnit = $"{Math.Round(totalWeightG, 1)}g";
        }

        // TotalPrice 설정
        TotalPrice = Math.Round(totalPrice, 2);

        // 합산된 영양성분 설정 (이미 총량 기준)
        Kcal = Math.Round(sumKcal, 2);
        MoistureG = Math.Round(sumMoistureG, 2);
        ProteinG = Math.Round(sumProteinG, 2);
        FatG = Math.Round(sumFatG, 2);
        SaturatedFatG = Math.Round(sumSaturatedFatG, 2);
        TransFatG = Math.Round(sumTransFatG, 2);
        CarbsG = Math.Round(sumCarbsG, 2);
        FiberG = Math.Round(sumFiberG, 2);
        SugarG = Math.Round(sumSugarG, 2);
        SodiumMg = Math.Round(sumSodiumMg, 2);
        CholesterolMg = Math.Round(sumCholesterolMg, 2);
        CalciumMg = Math.Round(sumCalciumMg, 2);
        IronMg = Math.Round(sumIronMg, 2);
        MagnesiumMg = Math.Round(sumMagnesiumMg, 2);
        PhosphorusMg = Math.Round(sumPhosphorusMg, 2);
        PotassiumMg = Math.Round(sumPotassiumMg, 2);
        ZincMg = Math.Round(sumZincMg, 2);
        CopperMg = Math.Round(sumCopperMg, 2);
        ManganeseMg = Math.Round(sumManganeseMg, 2);
        SeleniumUg = Math.Round(sumSeleniumUg, 2);
        MolybdenumUg = Math.Round(sumMolybdenumUg, 2);
        IodineUg = Math.Round(sumIodineUg, 2);
        VitaminAUg = Math.Round(sumVitaminAUg, 2);
        VitaminCMg = Math.Round(sumVitaminCMg, 2);
        VitaminDUg = Math.Round(sumVitaminDUg, 2);
        VitaminEMg = Math.Round(sumVitaminEMg, 2);
        VitaminKUg = Math.Round(sumVitaminKUg, 2);
        VitaminB1Mg = Math.Round(sumVitaminB1Mg, 2);
        VitaminB2Mg = Math.Round(sumVitaminB2Mg, 2);
        VitaminB3Mg = Math.Round(sumVitaminB3Mg, 2);
        VitaminB5Mg = Math.Round(sumVitaminB5Mg, 2);
        VitaminB6Mg = Math.Round(sumVitaminB6Mg, 2);
        VitaminB7Ug = Math.Round(sumVitaminB7Ug, 2);
        VitaminB9Ug = Math.Round(sumVitaminB9Ug, 2);
        VitaminB12Ug = Math.Round(sumVitaminB12Ug, 2);

        // 취사 방법 처리 (쌀 전용)
        if (isRiceCooking)
        {
            ApplyRiceCookingEffects();
        }
        else if (CookingMethod != "없음")
        {
            // 일반 조리 손실률 적용 (동기적으로)
            ApplyCookingLossRatesSync();
        }

        // 100g당 가격 재계산
        CalculatePricePer100g();
    }

    // 영양성분 초기화
    private void ClearNutrients()
    {
        // 수정 모드가 아닐 때만 이름, 구분, 분류 초기화
        if (!IsEditMode)
        {
            Name = string.Empty;
            Type = "식재료";
            Category = string.Empty;
        }
        
        Kcal = 0;
        MoistureG = 0;
        ProteinG = 0;
        FatG = 0;
        SaturatedFatG = 0;
        TransFatG = 0;
        CarbsG = 0;
        FiberG = 0;
        SugarG = 0;
        SodiumMg = 0;
        CholesterolMg = 0;
        CalciumMg = 0;
        IronMg = 0;
        MagnesiumMg = 0;
        PhosphorusMg = 0;
        PotassiumMg = 0;
        ZincMg = 0;
        CopperMg = 0;
        ManganeseMg = 0;
        SeleniumUg = 0;
        MolybdenumUg = 0;
        IodineUg = 0;
        VitaminAUg = 0;
        VitaminCMg = 0;
        VitaminDUg = 0;
        VitaminEMg = 0;
        VitaminKUg = 0;
        VitaminB1Mg = 0;
        VitaminB2Mg = 0;
        VitaminB3Mg = 0;
        VitaminB5Mg = 0;
        VitaminB6Mg = 0;
        VitaminB7Ug = 0;
        VitaminB9Ug = 0;
        VitaminB12Ug = 0;
        TotalPrice = 0;
        WeightUnit = "0g";
    }

    // 조리 손실률 적용 (비타민 위주) - 동기 버전
    private void ApplyCookingLossRatesSync()
    {
        if (_cookingLossRateRepository == null || CookingMethod == "없음")
        {
            return; // 조리 안하면 손실 없음
        }

        try
        {
            // 영양소별 잔존률 가져오기 (동기 버전 - UI 데드락 방지)
            var vitaminCRate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminC");
            var vitaminB1Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB1");
            var vitaminB2Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB2");
            var vitaminB3Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB3");
            var vitaminB5Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB5");
            var vitaminB6Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB6");
            var vitaminB9Rate = _cookingLossRateRepository.GetRetentionRateSync(CookingMethod, "VitaminB9");

            // 손실률 적용 (잔존률 %를 곱함)
            VitaminCMg = Math.Round(VitaminCMg * vitaminCRate / 100.0, 2);
            VitaminB1Mg = Math.Round(VitaminB1Mg * vitaminB1Rate / 100.0, 2);
            VitaminB2Mg = Math.Round(VitaminB2Mg * vitaminB2Rate / 100.0, 2);
            VitaminB3Mg = Math.Round(VitaminB3Mg * vitaminB3Rate / 100.0, 2);
            VitaminB5Mg = Math.Round(VitaminB5Mg * vitaminB5Rate / 100.0, 2);
            VitaminB6Mg = Math.Round(VitaminB6Mg * vitaminB6Rate / 100.0, 2);
            VitaminB9Ug = Math.Round(VitaminB9Ug * vitaminB9Rate / 100.0, 2);
        }
        catch (Exception ex)
        {
            // 오류 발생 시 로그 출력
            System.Diagnostics.Debug.WriteLine($"[조리 손실률 오류] {ex.Message}");
        }
    }

    // 조리 손실률 적용 (비타민 위주) - 비동기 버전 (호환성을 위해 유지)
    [Obsolete("Use ApplyCookingLossRatesSync instead")]
    private async void ApplyCookingLossRates()
    {
        if (_cookingLossRateRepository == null || CookingMethod == "없음")
        {
            return; // 조리 안하면 손실 없음
        }

        try
        {
            // 영양소별 잔존률 가져오기 (비동기)
            var vitaminCRate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminC");
            var vitaminB1Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB1");
            var vitaminB2Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB2");
            var vitaminB3Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB3");
            var vitaminB5Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB5");
            var vitaminB6Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB6");
            var vitaminB9Rate = await _cookingLossRateRepository.GetRetentionRateAsync(CookingMethod, "VitaminB9");

            // 손실률 적용 (잔존률 %를 곱함)
            VitaminCMg = Math.Round(VitaminCMg * vitaminCRate / 100.0, 2);
            VitaminB1Mg = Math.Round(VitaminB1Mg * vitaminB1Rate / 100.0, 2);
            VitaminB2Mg = Math.Round(VitaminB2Mg * vitaminB2Rate / 100.0, 2);
            VitaminB3Mg = Math.Round(VitaminB3Mg * vitaminB3Rate / 100.0, 2);
            VitaminB5Mg = Math.Round(VitaminB5Mg * vitaminB5Rate / 100.0, 2);
            VitaminB6Mg = Math.Round(VitaminB6Mg * vitaminB6Rate / 100.0, 2);
            VitaminB9Ug = Math.Round(VitaminB9Ug * vitaminB9Rate / 100.0, 2);
        }
        catch
        {
            // 오류 발생 시 손실률 적용하지 않음 (그대로 유지)
        }
    }

    // 취사 효과 적용 (쌀 전용)
    // 취사 시:
    // - 무게는 2.8배 증가 (물 흡수)
    // - 영양성분은 변화 없음 (자동으로 100g 기준으로 변환됨)
    // - 수분만 증가: 밥의 수분 함량 약 65%
    // - 수용성 비타민 20% 감소
    private void ApplyRiceCookingEffects()
    {
        // 취사 후 총 무게 계산 (2.8배)
        double totalWeightG = SelectedTemplates.Sum(st => st.AmountG);
        double cookedWeightG = totalWeightG * 2.8;
        
        // 취사 후 수분 함량 65% 설정
        // 총 수분 = 취사 후 무게 * 0.65
        MoistureG = Math.Round(cookedWeightG * 0.65, 2);
        
        // 수용성 비타민 20% 감소 (80% 잔존)
        VitaminB1Mg = Math.Round(VitaminB1Mg * 0.8, 2);
        VitaminB2Mg = Math.Round(VitaminB2Mg * 0.8, 2);
        VitaminB3Mg = Math.Round(VitaminB3Mg * 0.8, 2);
        VitaminB5Mg = Math.Round(VitaminB5Mg * 0.8, 2);
        VitaminB6Mg = Math.Round(VitaminB6Mg * 0.8, 2);
        VitaminB9Ug = Math.Round(VitaminB9Ug * 0.8, 2);
        VitaminCMg = Math.Round(VitaminCMg * 0.8, 2);
    }

    // 단위 문자열에서 배수 계산 (100g 기준)
    private static double CalculateMultiplierFromUnit(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        // 숫자 부분 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            // 단위 부분 추출
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            // g 또는 ml 단위면 100으로 나누기
            if (unit.StartsWith("g") || unit.StartsWith("ml"))
            {
                return quantity / 100.0;
            }
            // kg 또는 l이면 1000g이므로 10배
            else if (unit.StartsWith("kg"))
            {
                return quantity * 10.0;
            }
            else if (unit.StartsWith("l") && !unit.StartsWith("lb")) // lb는 제외
            {
                return quantity * 10.0;
            }
        }
        
        return 1.0; // 기본값
    }

    // 단위 변경 시 영양성분 재계산 (제거 - 이제 템플릿 목록에서 자동 계산)
    partial void OnWeightUnitChanged(string value)
    {
        // 총 가격이 설정되어 있으면 100g당 가격 재계산
        if (TotalPrice > 0)
        {
            CalculatePricePer100g();
        }
    }

    // 총 가격 변경 시 100g당 가격 자동 계산
    partial void OnTotalPriceChanged(double value)
    {
        CalculatePricePer100g();
    }

    // 100g당 가격 계산
    private void CalculatePricePer100g()
    {
        if (TotalPrice <= 0 || string.IsNullOrWhiteSpace(WeightUnit))
        {
            PricePer100g = 0;
            return;
        }

        double multiplier = CalculateMultiplier(WeightUnit);
        if (multiplier > 0)
        {
            // 총 가격을 multiplier로 나누면 100g당 가격
            PricePer100g = Math.Round(TotalPrice / multiplier, 2);
        }
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

    // 입력 값을 Food 객체로 변환 (영양성분은 100g 기준, WeightUnit은 사용자 입력 유지)
    public Food ToFood()
    {
        // 사용자가 입력한 단위에서 100g 대비 배수 계산
        double multiplier = CalculateMultiplier(WeightUnit);
        
        // 영양성분은 100g 기준으로 변환하여 저장하되, WeightUnit은 사용자 입력 그대로 유지
        return new Food(
            Id,
            Name,
            Type,
            Category,
            PricePer100g,  // 이미 100g당 가격으로 계산됨
            WeightUnit,  // 사용자가 입력한 단위 유지 (예: "1.2kg")
            CookingMethod,  // 조리 방법
            Math.Round(Kcal / multiplier, 2),
            Math.Round(MoistureG / multiplier, 2),
            Math.Round(ProteinG / multiplier, 2),
            Math.Round(FatG / multiplier, 2),
            Math.Round(SaturatedFatG / multiplier, 2),
            Math.Round(TransFatG / multiplier, 2),
            Math.Round(CarbsG / multiplier, 2),
            Math.Round(FiberG / multiplier, 2),
            Math.Round(SugarG / multiplier, 2),
            Math.Round(SodiumMg / multiplier, 2),
            Math.Round(CholesterolMg / multiplier, 2),
            Math.Round(CalciumMg / multiplier, 2),
            Math.Round(IronMg / multiplier, 2),
            Math.Round(MagnesiumMg / multiplier, 2),
            Math.Round(PhosphorusMg / multiplier, 2),
            Math.Round(PotassiumMg / multiplier, 2),
            Math.Round(ZincMg / multiplier, 2),
            Math.Round(CopperMg / multiplier, 2),
            Math.Round(ManganeseMg / multiplier, 2),
            Math.Round(SeleniumUg / multiplier, 2),
            Math.Round(MolybdenumUg / multiplier, 2),
            Math.Round(IodineUg / multiplier, 2),
            Math.Round(VitaminAUg / multiplier, 2),
            Math.Round(VitaminCMg / multiplier, 2),
            Math.Round(VitaminDUg / multiplier, 2),
            Math.Round(VitaminEMg / multiplier, 2),
            Math.Round(VitaminKUg / multiplier, 2),
            Math.Round(VitaminB1Mg / multiplier, 2),
            Math.Round(VitaminB2Mg / multiplier, 2),
            Math.Round(VitaminB3Mg / multiplier, 2),
            Math.Round(VitaminB5Mg / multiplier, 2),
            Math.Round(VitaminB6Mg / multiplier, 2),
            Math.Round(VitaminB7Ug / multiplier, 2),
            Math.Round(VitaminB9Ug / multiplier, 2),
            Math.Round(VitaminB12Ug / multiplier, 2),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes
        );
    }

    // 템플릿 사용 정보 반환 (저장용)
    public IReadOnlyList<FoodTemplateUsage> GetTemplateUsages()
    {
        return SelectedTemplates
            .Select(st => new FoodTemplateUsage(Id, st.Template.Id, st.AmountG))
            .ToList();
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

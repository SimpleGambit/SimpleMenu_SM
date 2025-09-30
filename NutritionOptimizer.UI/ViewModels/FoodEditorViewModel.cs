using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 선택 가능한 항목 유형
public enum SelectableItemType
{
    Template,
    Food
}

// 선택된 항목 (템플릿 또는 음식)
public sealed partial class SelectedItem : ObservableObject
{
    private readonly ITemplatePriceHistoryRepository? _priceHistoryRepository;
    
    [ObservableProperty]
    private SelectableItemType itemType;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Unit))]
    private NutritionTemplate? template;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Unit))]
    private Food? food;

    [ObservableProperty]
    private double amountG = 100;  // 기본 100 (g 또는 ml)
    
    [ObservableProperty]
    private string? selectedBrand;
    
    [ObservableProperty]
    private ObservableCollection<string> availableBrands = new();
    
    [ObservableProperty]
    private double calculatedPrice;  // 계산된 가격 (브랜드 + 그램 기반)

    // 항목의 이름
    public string Name => ItemType == SelectableItemType.Template ? Template?.Name ?? "" : Food?.Name ?? "";
    
    // 항목의 타입 (식재료/요리)
    public string Type => ItemType == SelectableItemType.Template ? Template?.Type ?? "" : Food?.Type ?? "";
    
    // 항목의 카테고리
    public string Category => ItemType == SelectableItemType.Template ? Template?.Category ?? "" : Food?.Category ?? "";
    
    // 항목의 브랜드 (템플릿만)
    public string? ItemBrand => ItemType == SelectableItemType.Template ? Template?.Brand : null;
    
    // 단위 (g 또는 ml)
    public string Unit => GetUnitFromItem();

    // 템플릿용 생성자
    public SelectedItem(NutritionTemplate template, ITemplatePriceHistoryRepository? priceHistoryRepository, double amountG = 100)
    {
        this.itemType = SelectableItemType.Template;
        this.template = template;
        this._priceHistoryRepository = priceHistoryRepository;
        this.amountG = amountG;
        LoadAvailableBrandsAsync();
    }
    
    // 음식용 생성자
    public SelectedItem(Food food, double amountG = 100)
    {
        this.itemType = SelectableItemType.Food;
        this.food = food;
        this._priceHistoryRepository = null;
        this.amountG = amountG;
        
        // 음식의 경우 브랜드 정보가 없으므로 가격 직접 계산
        CalculateFoodPrice();
    }
    
    // 항목의 WeightUnit에서 단위 추출 (g 또는 ml)
    private string GetUnitFromItem()
    {
        string? weightUnit = ItemType == SelectableItemType.Template ? Template?.WeightUnit : Food?.WeightUnit;
        
        if (string.IsNullOrWhiteSpace(weightUnit))
            return "g";
        
        var unit = weightUnit.ToLower();
        if (unit.Contains("ml"))
            return "ml";
        else
            return "g";
    }
    
    // 브랜드 목록 로드 (템플릿만)
    private async void LoadAvailableBrandsAsync()
    {
        if (ItemType != SelectableItemType.Template || _priceHistoryRepository == null || Template == null)
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
        if (ItemType == SelectableItemType.Food)
        {
            CalculateFoodPrice();
            return;
        }
        
        if (_priceHistoryRepository == null || string.IsNullOrWhiteSpace(SelectedBrand) || Template == null)
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
    
    // 음식 가격 계산
    private void CalculateFoodPrice()
    {
        if (Food == null)
        {
            CalculatedPrice = 0;
            return;
        }
        
        // 음식의 경우 이미 100g당 가격이 있음
        double multiplier = AmountG / 100.0;
        CalculatedPrice = Math.Round(Food.PricePer100g * multiplier, 2);
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

    public string DisplayName => $"{Name} ({AmountG}{Unit})" + 
                                 (ItemType == SelectableItemType.Template && !string.IsNullOrWhiteSpace(SelectedBrand) ? $" - {SelectedBrand}" : "");
    
    // 영양성분 가져오기 메서드들
    public double GetKcal() => ItemType == SelectableItemType.Template ? Template?.Kcal ?? 0 : Food?.Kcal ?? 0;
    public double GetMoistureG() => ItemType == SelectableItemType.Template ? Template?.MoistureG ?? 0 : Food?.MoistureG ?? 0;
    public double GetProteinG() => ItemType == SelectableItemType.Template ? Template?.ProteinG ?? 0 : Food?.ProteinG ?? 0;
    public double GetFatG() => ItemType == SelectableItemType.Template ? Template?.FatG ?? 0 : Food?.FatG ?? 0;
    public double GetSaturatedFatG() => ItemType == SelectableItemType.Template ? Template?.SaturatedFatG ?? 0 : Food?.SaturatedFatG ?? 0;
    public double GetTransFatG() => ItemType == SelectableItemType.Template ? Template?.TransFatG ?? 0 : Food?.TransFatG ?? 0;
    public double GetCarbsG() => ItemType == SelectableItemType.Template ? Template?.CarbsG ?? 0 : Food?.CarbsG ?? 0;
    public double GetFiberG() => ItemType == SelectableItemType.Template ? Template?.FiberG ?? 0 : Food?.FiberG ?? 0;
    public double GetSugarG() => ItemType == SelectableItemType.Template ? Template?.SugarG ?? 0 : Food?.SugarG ?? 0;
    public double GetSodiumMg() => ItemType == SelectableItemType.Template ? Template?.SodiumMg ?? 0 : Food?.SodiumMg ?? 0;
    public double GetCholesterolMg() => ItemType == SelectableItemType.Template ? Template?.CholesterolMg ?? 0 : Food?.CholesterolMg ?? 0;
    public double GetCalciumMg() => ItemType == SelectableItemType.Template ? Template?.CalciumMg ?? 0 : Food?.CalciumMg ?? 0;
    public double GetIronMg() => ItemType == SelectableItemType.Template ? Template?.IronMg ?? 0 : Food?.IronMg ?? 0;
    public double GetMagnesiumMg() => ItemType == SelectableItemType.Template ? Template?.MagnesiumMg ?? 0 : Food?.MagnesiumMg ?? 0;
    public double GetPhosphorusMg() => ItemType == SelectableItemType.Template ? Template?.PhosphorusMg ?? 0 : Food?.PhosphorusMg ?? 0;
    public double GetPotassiumMg() => ItemType == SelectableItemType.Template ? Template?.PotassiumMg ?? 0 : Food?.PotassiumMg ?? 0;
    public double GetZincMg() => ItemType == SelectableItemType.Template ? Template?.ZincMg ?? 0 : Food?.ZincMg ?? 0;
    public double GetCopperMg() => ItemType == SelectableItemType.Template ? Template?.CopperMg ?? 0 : Food?.CopperMg ?? 0;
    public double GetManganeseMg() => ItemType == SelectableItemType.Template ? Template?.ManganeseMg ?? 0 : Food?.ManganeseMg ?? 0;
    public double GetSeleniumUg() => ItemType == SelectableItemType.Template ? Template?.SeleniumUg ?? 0 : Food?.SeleniumUg ?? 0;
    public double GetMolybdenumUg() => ItemType == SelectableItemType.Template ? Template?.MolybdenumUg ?? 0 : Food?.MolybdenumUg ?? 0;
    public double GetIodineUg() => ItemType == SelectableItemType.Template ? Template?.IodineUg ?? 0 : Food?.IodineUg ?? 0;
    public double GetVitaminAUg() => ItemType == SelectableItemType.Template ? Template?.VitaminAUg ?? 0 : Food?.VitaminAUg ?? 0;
    public double GetVitaminCMg() => ItemType == SelectableItemType.Template ? Template?.VitaminCMg ?? 0 : Food?.VitaminCMg ?? 0;
    public double GetVitaminDUg() => ItemType == SelectableItemType.Template ? Template?.VitaminDUg ?? 0 : Food?.VitaminDUg ?? 0;
    public double GetVitaminEMg() => ItemType == SelectableItemType.Template ? Template?.VitaminEMg ?? 0 : Food?.VitaminEMg ?? 0;
    public double GetVitaminKUg() => ItemType == SelectableItemType.Template ? Template?.VitaminKUg ?? 0 : Food?.VitaminKUg ?? 0;
    public double GetVitaminB1Mg() => ItemType == SelectableItemType.Template ? Template?.VitaminB1Mg ?? 0 : Food?.VitaminB1Mg ?? 0;
    public double GetVitaminB2Mg() => ItemType == SelectableItemType.Template ? Template?.VitaminB2Mg ?? 0 : Food?.VitaminB2Mg ?? 0;
    public double GetVitaminB3Mg() => ItemType == SelectableItemType.Template ? Template?.VitaminB3Mg ?? 0 : Food?.VitaminB3Mg ?? 0;
    public double GetVitaminB5Mg() => ItemType == SelectableItemType.Template ? Template?.VitaminB5Mg ?? 0 : Food?.VitaminB5Mg ?? 0;
    public double GetVitaminB6Mg() => ItemType == SelectableItemType.Template ? Template?.VitaminB6Mg ?? 0 : Food?.VitaminB6Mg ?? 0;
    public double GetVitaminB7Ug() => ItemType == SelectableItemType.Template ? Template?.VitaminB7Ug ?? 0 : Food?.VitaminB7Ug ?? 0;
    public double GetVitaminB9Ug() => ItemType == SelectableItemType.Template ? Template?.VitaminB9Ug ?? 0 : Food?.VitaminB9Ug ?? 0;
    public double GetVitaminB12Ug() => ItemType == SelectableItemType.Template ? Template?.VitaminB12Ug ?? 0 : Food?.VitaminB12Ug ?? 0;
}

// 음식 추가/수정을 위한 ViewModel
public sealed partial class FoodEditorViewModel : ObservableObject
{
    private readonly INutritionTemplateRepository? _templateRepository;
    private readonly IFoodRepository? _foodRepository;
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

    // 음식 목록
    [ObservableProperty]
    private ObservableCollection<Food> foods = new();

    // 선택된 항목 목록 (템플릿 + 음식)
    public ObservableCollection<SelectedItem> SelectedItems { get; } = new();

    [ObservableProperty]
    private NutritionTemplate? currentTemplate;  // 추가할 템플릿 선택용
    
    [ObservableProperty]
    private Food? currentFood;  // 추가할 음식 선택용
    
    [ObservableProperty]
    private bool isTemplateSelected = true;  // 템플릿/음식 탭 선택

    public bool IsEditMode { get; }

    // 새 음식 추가 모드 (매개변수 없는 기본 생성자)
    public FoodEditorViewModel() : this(null, null, null, null, null)
    {
    }

    // Repository를 받는 생성자 (새 음식 추가)
    public FoodEditorViewModel(INutritionTemplateRepository? templateRepository, IFoodRepository? foodRepository, ITemplatePriceHistoryRepository? priceHistoryRepository, ICookingLossRateRepository? cookingLossRateRepository, IFoodTemplateUsageRepository? foodTemplateUsageRepository = null)
    {
        IsEditMode = false;
        _templateRepository = templateRepository;
        _foodRepository = foodRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _cookingLossRateRepository = cookingLossRateRepository;
        _foodTemplateUsageRepository = foodTemplateUsageRepository;
        Id = GenerateNewId();
        
        // 항목 목록 변경 시 영양성분 재계산
        SelectedItems.CollectionChanged += (s, e) => RecalculateNutrients();
        
        LoadTemplatesAsync();
        LoadFoodsAsync();
    }

    // 기존 음식 수정 모드
    public FoodEditorViewModel(Food food, INutritionTemplateRepository? templateRepository = null, IFoodRepository? foodRepository = null, ITemplatePriceHistoryRepository? priceHistoryRepository = null, ICookingLossRateRepository? cookingLossRateRepository = null, IFoodTemplateUsageRepository? foodTemplateUsageRepository = null)
    {
        IsEditMode = true;
        _templateRepository = templateRepository;
        _foodRepository = foodRepository;
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
        
        // 항목 목록 및 사용 정보 불러오기
        LoadTemplatesAsync();
        LoadFoodsAsync();
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
    
    // 음식 목록 불러오기
    private async void LoadFoodsAsync()
    {
        if (_foodRepository == null) return;
        
        var loadedFoods = await _foodRepository.GetAllAsync();
        Foods.Clear();
        foreach (var food in loadedFoods.OrderBy(f => f.Name))
        {
            // 현재 편집 중인 음식은 제외
            if (!IsEditMode || food.Id != Id)
            {
                Foods.Add(food);
            }
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
            SelectedItems.CollectionChanged -= OnSelectedItemsChanged;

            foreach (var usage in usages)
            {
                var template = Templates.FirstOrDefault(t => t.Id == usage.TemplateId);
                if (template != null)
                {
                    var item = new SelectedItem(template, _priceHistoryRepository, usage.AmountG);
                    
                    // PropertyChanged 이벤트 등록
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectedItem.AmountG) || 
                            e.PropertyName == nameof(SelectedItem.SelectedBrand) ||
                            e.PropertyName == nameof(SelectedItem.CalculatedPrice))
                        {
                            RecalculateNutrients();
                        }
                    };
                    
                    SelectedItems.Add(item);
                }
            }

            // CollectionChanged 이벤트 다시 등록
            SelectedItems.CollectionChanged += OnSelectedItemsChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"템플릿 사용 정보 로드 실패: {ex.Message}");
        }
    }

    // CollectionChanged 이벤트 핸들러
    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
        if (SelectedItems.Any(si => si.ItemType == SelectableItemType.Template && si.Template?.Id == CurrentTemplate.Id))
        {
            System.Windows.MessageBox.Show("이미 추가된 템플릿입니다.");
            return;
        }

        var item = new SelectedItem(CurrentTemplate, _priceHistoryRepository, 100);
        
        // PropertyChanged 이벤트 등록
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedItem.AmountG) || 
                e.PropertyName == nameof(SelectedItem.SelectedBrand) ||
                e.PropertyName == nameof(SelectedItem.CalculatedPrice))
            {
                RecalculateNutrients();
            }
        };
        
        SelectedItems.Add(item);
        CurrentTemplate = null;  // 선택 초기화
    }
    
    // 음식 추가
    [RelayCommand]
    private void AddFood()
    {
        if (CurrentFood == null)
        {
            System.Windows.MessageBox.Show("추가할 음식을 선택해주세요.");
            return;
        }

        // 이미 추가된 음식인지 확인
        if (SelectedItems.Any(si => si.ItemType == SelectableItemType.Food && si.Food?.Id == CurrentFood.Id))
        {
            System.Windows.MessageBox.Show("이미 추가된 음식입니다.");
            return;
        }

        var item = new SelectedItem(CurrentFood, 100);
        
        // PropertyChanged 이벤트 등록
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedItem.AmountG) || 
                e.PropertyName == nameof(SelectedItem.CalculatedPrice))
            {
                RecalculateNutrients();
            }
        };
        
        SelectedItems.Add(item);
        CurrentFood = null;  // 선택 초기화
    }

    // 항목 제거
    [RelayCommand]
    private void RemoveItem(SelectedItem item)
    {
        SelectedItems.Remove(item);
    }

    // 모든 선택된 항목의 영양성분 합산
    private void RecalculateNutrients()
    {
        if (!SelectedItems.Any())
        {
            // 항목이 없으면 모두 0으로 초기화
            ClearNutrients();
            return;
        }

        // 수정 모드가 아닐 때만 Type, Category, Name을 자동 설정
        if (!IsEditMode)
        {
            // 첫 번째 항목의 Type과 Category 사용
            var firstItem = SelectedItems.First();
            
            // 항목 개수에 따라 Type 설정
            if (SelectedItems.Count == 1)
            {
                // 항목 1개: 해당 항목의 Type을 그대로 사용
                Type = firstItem.Type;
                Category = firstItem.Category;
            }
            else
            {
                // 항목 2개 이상: 요리
                Type = "요리";
                // Category는 사용자가 직접 입력 (초기값만 설정)
                if (string.IsNullOrWhiteSpace(Category) || Category == firstItem.Category)
                {
                    Category = "복합 요리";  // 기본값
                }
            }
            
            // Name이 비어있으면 항목 이름들을 합쳐서 자동 설정
            if (string.IsNullOrWhiteSpace(Name) || Name == "혼합")
            {
                if (SelectedItems.Count == 1)
                {
                    Name = firstItem.Name;
                }
                else
                {
                    Name = string.Join(" + ", SelectedItems.Select(si => si.Name));
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

        foreach (var item in SelectedItems)
        {
            double amountG = item.AmountG;
            double multiplier = amountG / 100.0;  // 100g 기준으로 변환

            totalWeightG += amountG;

            // 각 항목의 CalculatedPrice 사용
            totalPrice += item.CalculatedPrice;

            // 영양성분 합산 (항목 타입에 관계없이 동일한 메서드 사용)
            sumKcal += item.GetKcal() * multiplier;
            sumMoistureG += item.GetMoistureG() * multiplier;
            sumProteinG += item.GetProteinG() * multiplier;
            sumFatG += item.GetFatG() * multiplier;
            sumSaturatedFatG += item.GetSaturatedFatG() * multiplier;
            sumTransFatG += item.GetTransFatG() * multiplier;
            sumCarbsG += item.GetCarbsG() * multiplier;
            sumFiberG += item.GetFiberG() * multiplier;
            sumSugarG += item.GetSugarG() * multiplier;
            sumSodiumMg += item.GetSodiumMg() * multiplier;
            sumCholesterolMg += item.GetCholesterolMg() * multiplier;
            sumCalciumMg += item.GetCalciumMg() * multiplier;
            sumIronMg += item.GetIronMg() * multiplier;
            sumMagnesiumMg += item.GetMagnesiumMg() * multiplier;
            sumPhosphorusMg += item.GetPhosphorusMg() * multiplier;
            sumPotassiumMg += item.GetPotassiumMg() * multiplier;
            sumZincMg += item.GetZincMg() * multiplier;
            sumCopperMg += item.GetCopperMg() * multiplier;
            sumManganeseMg += item.GetManganeseMg() * multiplier;
            sumSeleniumUg += item.GetSeleniumUg() * multiplier;
            sumMolybdenumUg += item.GetMolybdenumUg() * multiplier;
            sumIodineUg += item.GetIodineUg() * multiplier;
            sumVitaminAUg += item.GetVitaminAUg() * multiplier;
            sumVitaminCMg += item.GetVitaminCMg() * multiplier;
            sumVitaminDUg += item.GetVitaminDUg() * multiplier;
            sumVitaminEMg += item.GetVitaminEMg() * multiplier;
            sumVitaminKUg += item.GetVitaminKUg() * multiplier;
            sumVitaminB1Mg += item.GetVitaminB1Mg() * multiplier;
            sumVitaminB2Mg += item.GetVitaminB2Mg() * multiplier;
            sumVitaminB3Mg += item.GetVitaminB3Mg() * multiplier;
            sumVitaminB5Mg += item.GetVitaminB5Mg() * multiplier;
            sumVitaminB6Mg += item.GetVitaminB6Mg() * multiplier;
            sumVitaminB7Ug += item.GetVitaminB7Ug() * multiplier;
            sumVitaminB9Ug += item.GetVitaminB9Ug() * multiplier;
            sumVitaminB12Ug += item.GetVitaminB12Ug() * multiplier;
        }

        // WeightUnit 설정 (총 무게)
        // 취사 방법이고 쌀이 있으면 2.8배 증가
        bool isRiceCooking = CookingMethod == "취사" && SelectedItems.Any(si => si.Category.Contains("쌀"));
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
        double totalWeightG = SelectedItems.Sum(st => st.AmountG);
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
        // 템플릿만 저장 (음식은 템플릿처럼 사용할 수 있지만 별도 저장하지 않음)
        return SelectedItems
            .Where(si => si.ItemType == SelectableItemType.Template && si.Template != null)
            .Select(si => new FoodTemplateUsage(Id, si.Template!.Id, si.AmountG))
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

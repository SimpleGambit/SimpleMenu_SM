using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutritionOptimizer.Domain;
using NutritionOptimizer.Optimization;

namespace NutritionOptimizer.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFoodRepository _foodRepository;
    private readonly ITargetsRepository _targetsRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ISavedDietRepository _savedDietRepository;
    private readonly IDietHistoryRepository _dietHistoryRepository;
    private readonly IFoodPreferenceRepository _foodPreferenceRepository;
    private readonly INutritionTemplateRepository _nutritionTemplateRepository;
    private readonly ITemplatePriceHistoryRepository _templatePriceHistoryRepository;
    private readonly ICookingLossRateRepository _cookingLossRateRepository;
    private readonly IFoodTemplateUsageRepository _foodTemplateUsageRepository;
    private readonly IOptimizationSettingsRepository _optimizationSettingsRepository;
    private readonly DietOptimizer _optimizer = new();

    public ObservableCollection<Food> Foods { get; } = new();
    public ObservableCollection<NutrientTarget> Targets { get; } = new();
    public ObservableCollection<PickItem> Picks { get; } = new();
    public ObservableCollection<NutrientSummaryItem> NutrientSummary { get; } = new();
    public ObservableCollection<PriceHistoryDisplayItem> PriceHistories { get; } = new();
    public ObservableCollection<SavedDiet> SavedDiets { get; } = new();
    public ObservableCollection<DietHistory> DietHistories { get; } = new();
    public ObservableCollection<FoodPreference> FoodPreferences { get; } = new();
    public ObservableCollection<CategoryConstraintItem> CategoryConstraints { get; } = new();
    public ObservableCollection<ManualDietItem> ManualDietItems { get; } = new();

    [ObservableProperty]
    private string? query;

    [ObservableProperty]
    private bool excludeDairy;

    [ObservableProperty]
    private bool excludeProcessed;

    [ObservableProperty]
    private double stepG = 50;

    [ObservableProperty]
    private string optimizationStatus = "Ready";

    [ObservableProperty]
    private double totalCost;

    [ObservableProperty]
    private double nutritionScore; // 영양 달성도 점수 (0-100)

    [ObservableProperty]
    private Food? selectedFood;

    public ObservableCollection<Food> SelectedFoods { get; } = new();

    [ObservableProperty]
    private NutrientTarget? selectedTarget;

    [ObservableProperty]
    private PriceHistoryDisplayItem? selectedPriceHistory;

    public ObservableCollection<PriceHistoryDisplayItem> SelectedPriceHistories { get; } = new();

    [ObservableProperty]
    private DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private SavedDiet? selectedSavedDiet;

    [ObservableProperty]
    private DietHistory? selectedDietHistory;

    [ObservableProperty]
    private DateOnly historyStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

    [ObservableProperty]
    private DateOnly historyEndDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private bool showFavoritesOnly;

    [ObservableProperty]
    private bool hideExcluded = true;

    [ObservableProperty]
    private string optimizationMode = "Balanced"; // "Cost", "Balanced", "Nutrition"

    [ObservableProperty]
    private bool useSelectedFoodsOnly; // 선택한 음식만 사용 여부

    [ObservableProperty]
    private double manualDietTotalCost; // 수동 식단 총 비용

    [ObservableProperty]
    private double manualDietNutritionScore; // 수동 식단 영양 점수

    public IEnumerable<Food> FilteredFoods => Foods.Where(MatchesFilter);

    // 데이터 개수 표시용 프로퍼티
    public string FoodsCountText => $"음식 목록 ({Foods.Count})";
    public string TargetsCountText => $"영양 목표치 ({Targets.Count})";
    public string PriceHistoriesCountText => $"가격 이력 ({PriceHistories.Count})";
    public string SavedDietsCountText => $"저장된 식단 ({SavedDiets.Count})";
    public string PicksCountText => Picks.Count > 0 ? $"최적화 결과 ({Picks.Count})" : "최적화 결과";

    public MainViewModel(IFoodRepository foodRepository, ITargetsRepository targetsRepository, IPriceHistoryRepository priceHistoryRepository, ISavedDietRepository savedDietRepository, IDietHistoryRepository dietHistoryRepository, IFoodPreferenceRepository foodPreferenceRepository, INutritionTemplateRepository nutritionTemplateRepository, ITemplatePriceHistoryRepository templatePriceHistoryRepository, ICookingLossRateRepository cookingLossRateRepository, IFoodTemplateUsageRepository foodTemplateUsageRepository, IOptimizationSettingsRepository optimizationSettingsRepository)
    {
        _foodRepository = foodRepository;
        _targetsRepository = targetsRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _savedDietRepository = savedDietRepository;
        _dietHistoryRepository = dietHistoryRepository;
        _foodPreferenceRepository = foodPreferenceRepository;
        _nutritionTemplateRepository = nutritionTemplateRepository;
        _templatePriceHistoryRepository = templatePriceHistoryRepository;
        _cookingLossRateRepository = cookingLossRateRepository;
        _foodTemplateUsageRepository = foodTemplateUsageRepository;
        _optimizationSettingsRepository = optimizationSettingsRepository;
        
        // 수동 식단 항목 변경 시 자동 계산
        ManualDietItems.CollectionChanged += (s, e) => RecalculateManualDiet();
    }

    // 프로그램 초기화 시 자동 데이터 로드
    public async Task InitializeAsync()
    {
        // 카테고리 제약 로드
        await LoadCategoryConstraints();
        
        await LoadAllData();
    }

    // 모든 데이터를 한 번에 불러오기
    [RelayCommand]
    private async Task LoadAllData()
    {
        OptimizationStatus = "데이터 로드 중...";
        
        try
        {
            // 음식 목록
            await LoadFoods();
            
            // 목표치
            await LoadTargets();
            
            // 가격 이력
            await LoadPriceHistories();
            
            // 선호도
            await LoadFoodPreferences();
            
            // 저장된 식단
            await LoadSavedDiets();
            
            OptimizationStatus = $"데이터 로드 완료 (음식: {Foods.Count}, 목표치: {Targets.Count})";
        }
        catch (Exception ex)
        {
            OptimizationStatus = $"데이터 로드 실패: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadFoods()
    {
        var foods = await _foodRepository.GetAllAsync();
        Foods.Clear();
        foreach (var food in foods)
        {
            Foods.Add(food);
        }

        OnPropertyChanged(nameof(FilteredFoods));
        OnPropertyChanged(nameof(FoodsCountText));
    }

    [RelayCommand]
    private async Task LoadTargets()
    {
        var targets = await _targetsRepository.GetAllAsync();
        Targets.Clear();
        foreach (var target in targets)
        {
            Targets.Add(target);
        }
        
        OnPropertyChanged(nameof(TargetsCountText));
    }

    [RelayCommand]
    private async Task Optimize()
    {
        if (Foods.Count == 0 || Targets.Count == 0)
        {
            OptimizationStatus = "데이터를 먼저 불러오세요";
            return;
        }

        OptimizationStatus = "Solving...";

        // 선택한 음식만 사용 여부에 따라 음식 목록 결정
        List<Food> foodsToOptimize;
        if (UseSelectedFoodsOnly && SelectedFoods.Any())
        {
            // 선택된 음식만 사용
            foodsToOptimize = SelectedFoods.ToList();
            OptimizationStatus = $"선택한 {foodsToOptimize.Count}개 음식으로 최적화 중...";
        }
        else if (UseSelectedFoodsOnly && !SelectedFoods.Any())
        {
            OptimizationStatus = "음식을 선택하거나 '선택한 음식만 사용' 옵션을 해제하세요";
            System.Windows.MessageBox.Show(
                "선택한 음식이 없습니다.\n\n등록된 모든 음식으로 최적화하려면 '선택한 음식만 사용' 체크박스를 해제하세요.",
                "선택 필요",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }
        else
        {
            // 필터링된 모든 음식 사용
            foodsToOptimize = FilteredFoods.ToList();
        }

        var targets = Targets.ToList();

        // 카테고리 제약 변환
        var categoryConstraints = CategoryConstraints
            .Where(c => !string.IsNullOrWhiteSpace(c.Category))
            .Select(c => new DietOptimizer.CategoryConstraint(c.Category, c.MinCount, c.MaxCountPerFood))
            .ToList();

        // 최적화 모드에 따라 가중치 설정
        double costWeight, nutritionWeight;
        switch (OptimizationMode)
        {
            case "Cost": // 비용 최우선
                costWeight = 100.0;
                nutritionWeight = 1.0;
                break;
            case "Nutrition": // 영양 최우선
                costWeight = 0.001;
                nutritionWeight = 1000.0;
                break;
            case "Balanced": // 균형 (기본값)
            default:
                costWeight = 1.0;
                nutritionWeight = 100.0;
                break;
        }

        // 균형 잡힌 영양 + 비용 최소화 목표로 최적화
        var result = await Task.Run(() => _optimizer.Optimize(
            foodsToOptimize, 
            targets, 
            StepG,
            DietOptimizer.OptimizationObjective.BalancedNutrition,
            costWeight: costWeight,
            nutritionWeight: nutritionWeight,
            defaultMaxItemsPerFood: 3,
            categoryConstraints: categoryConstraints
        ));

        Picks.Clear();
        foreach (var pick in result.Picks)
        {
            var cost = pick.Food.PricePer100g * pick.Amount100g;
            var amountG = pick.Amount100g * 100; // 100g 기준을 실제 g으로 변환
            Picks.Add(new PickItem(pick.Food.Name, Math.Round(amountG, 1), Math.Round(cost)));
        }

        NutrientSummary.Clear();
        foreach (var entry in result.NutrientSummary.OrderBy(e => e.Key))
        {
            var key = entry.Key;
            var (actual, target, min, max, achievementRate) = entry.Value;
            NutrientSummary.Add(new NutrientSummaryItem(
                key, 
                Math.Round(actual, 2), 
                target.HasValue ? Math.Round(target.Value, 2) : null,
                min, 
                max,
                Math.Round(achievementRate, 1)
            ));
        }

        TotalCost = Math.Round(result.TotalCost);
        NutritionScore = Math.Round(result.NutritionScore, 1);
        
        if (result.Feasible)
        {
            OptimizationStatus = $"완료 (영양 점수: {NutritionScore}/100, 비용: {TotalCost:N0}원)";
            
            // 최적화 결과 팝업 표시
            ShowOptimizationResultPopup();
        }
        else
        {
            OptimizationStatus = $"실패: {result.InfeasibleReason}";
            System.Windows.MessageBox.Show(
                $"최적화에 실패했습니다.\n\n원인: {result.InfeasibleReason}\n\n팁: 영양 목표치를 낮추거나, 음식 목록을 늘려보세요.",
                "최적화 실패",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        
        OnPropertyChanged(nameof(PicksCountText));
    }

    // 최적화 결과 팝업 표시
    private void ShowOptimizationResultPopup()
    {
        var window = new Views.OptimizationResultWindow(this);
        window.Show();
    }

    partial void OnQueryChanged(string? value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnExcludeDairyChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnExcludeProcessedChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnShowFavoritesOnlyChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnHideExcludedChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnOptimizationModeChanged(string value)
    {
        // 모드 변경 시 자동 저장
        _ = SaveCategoryConstraints();
    }

    // 음식 추가
    [RelayCommand]
    private async Task AddFood()
    {
        var viewModel = new FoodEditorViewModel(_nutritionTemplateRepository, _foodRepository, _templatePriceHistoryRepository, _cookingLossRateRepository, _foodTemplateUsageRepository);
        var dialog = new Views.FoodEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var newFood = dialog.ViewModel.ToFood();
            await _foodRepository.AddAsync(newFood);
            
            // 템플릿 사용 정보 저장
            var templateUsages = dialog.ViewModel.GetTemplateUsages();
            if (templateUsages.Any())
            {
                await _foodTemplateUsageRepository.SaveAsync(newFood.Id, templateUsages);
            }
            
            await LoadFoods();
        }
    }

    // 음식 수정
    [RelayCommand]
    private async Task EditFood()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("수정할 음식을 선택해주세요.");
            return;
        }

        var viewModel = new FoodEditorViewModel(SelectedFood, _nutritionTemplateRepository, _foodRepository, _templatePriceHistoryRepository, _cookingLossRateRepository, _foodTemplateUsageRepository);
        var dialog = new Views.FoodEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var updatedFood = dialog.ViewModel.ToFood();
            await _foodRepository.UpdateAsync(updatedFood);
            
            // 템플릿 사용 정보 저장
            var templateUsages = dialog.ViewModel.GetTemplateUsages();
            if (templateUsages.Any())
            {
                await _foodTemplateUsageRepository.SaveAsync(updatedFood.Id, templateUsages);
            }
            else
            {
                // 템플릿이 없으면 기존 사용 정보 삭제
                await _foodTemplateUsageRepository.DeleteByFoodIdAsync(updatedFood.Id);
            }
            
            await LoadFoods();
        }
    }

    // 음식 삭제
    [RelayCommand]
    private async Task DeleteFood()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("삭제할 음식을 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"'{SelectedFood.Name}'을(를) 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _foodRepository.DeleteAsync(SelectedFood.Id);
            await LoadFoods();
        }
    }

    // 음식 다중 삭제
    [RelayCommand]
    private async Task DeleteSelectedFoods()
    {
        if (SelectedFoods.Count == 0)
        {
            System.Windows.MessageBox.Show("삭제할 음식을 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"{SelectedFoods.Count}개의 음식을 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var foodsToDelete = SelectedFoods.ToList();
            foreach (var food in foodsToDelete)
            {
                await _foodRepository.DeleteAsync(food.Id);
            }
            SelectedFoods.Clear();
            await LoadFoods();
            OptimizationStatus = $"{foodsToDelete.Count}개 음식 삭제 완료";
        }
    }

    // 목표치 인라인 수정 (DataGrid 셀 편집 후 자동 저장)
    public async Task UpdateTargetAsync(NutrientTarget target)
    {
        await _targetsRepository.UpdateAsync(target);
        OptimizationStatus = $"{target.NutrientKey} 목표치 업데이트 완료";
    }

    // 목표치 수정 (삭제 기능 제거됨)
    [RelayCommand]
    private async Task EditTarget()
    {
        if (SelectedTarget == null)
        {
            System.Windows.MessageBox.Show("수정할 목표치를 선택해주세요.");
            return;
        }

        var viewModel = new TargetEditorViewModel(SelectedTarget);
        var dialog = new Views.TargetEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var updatedTarget = dialog.ViewModel.ToTarget();
            await _targetsRepository.UpdateAsync(updatedTarget);
            await LoadTargets();
        }
    }

    // 가격 이력 불러오기
    [RelayCommand]
    private async Task LoadPriceHistories()
    {
        var histories = await _priceHistoryRepository.GetAllAsync();
        PriceHistories.Clear();
        
        foreach (var history in histories.OrderByDescending(h => h.EffectiveDate))
        {
            var food = Foods.FirstOrDefault(f => f.Id == history.FoodId);
            if (food != null)
            {
                var displayItem = CreatePriceHistoryDisplayItem(history, food);
                PriceHistories.Add(displayItem);
            }
        }
        
        OnPropertyChanged(nameof(PriceHistoriesCountText));
    }

    // 선택된 음식의 가격 이력만 보기
    [RelayCommand]
    private async Task LoadPriceHistoriesForSelectedFood()
    {
        if (SelectedFood == null)
        {
            await LoadPriceHistories();
            return;
        }

        var histories = await _priceHistoryRepository.GetByFoodIdAsync(SelectedFood.Id);
        PriceHistories.Clear();
        
        foreach (var history in histories.OrderByDescending(h => h.EffectiveDate))
        {
            var displayItem = CreatePriceHistoryDisplayItem(history, SelectedFood);
            PriceHistories.Add(displayItem);
        }
        
        OnPropertyChanged(nameof(PriceHistoriesCountText));
    }

    // PriceHistory를 표시용 아이템으로 변환
    private PriceHistoryDisplayItem CreatePriceHistoryDisplayItem(PriceHistory history, Food food)
    {
        var weightUnit = food.WeightUnit;
        
        // 100g 대비 배수 계산
        double multiplier = CalculateMultiplier(weightUnit);
        
        // 총 가격 계산
        double totalPrice = Math.Round(history.PricePer100g * multiplier, 2);
        
        // 단위당 가격은 항상 100g 기준으로 표시
        double unitPrice = history.PricePer100g;
        string unitPriceDisplay = $"{unitPrice:N0}원/100g";
        
        return new PriceHistoryDisplayItem(
            history.FoodId,
            food.Name,
            history.EffectiveDate,
            totalPrice,
            weightUnit,
            unitPrice,
            unitPriceDisplay
        );
    }

    // 단위에서 100g 대비 배수 계산 (Food 모델과 동일한 로직)
    private static double CalculateMultiplier(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        var (quantity, unit) = ParseWeightUnit(weightUnit);
        
        // g 또는 ml 단위면 100으로 나누고
        if (unit == "g" || unit == "ml")
        {
            return quantity / 100.0;
        }
        // kg 또는 l이면 1000g이므로 10배
        else if (unit == "kg" || unit == "l")
        {
            return quantity * 10.0;
        }
        
        return 1.0; // 기본값
    }

    // 단위 파싱: 숫자와 단위 분리 (Food 모델과 동일한 로직)
    private static (double quantity, string unit) ParseWeightUnit(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return (100, "g");
        
        // 숫자 부분 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            // 단위 부분 추출
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            // 단위 정규화
            if (unit.StartsWith("kg"))
                return (quantity, "kg");
            else if (unit.StartsWith("l") && !unit.StartsWith("lb")) // lb는 제외
                return (quantity, "l");
            else if (unit.StartsWith("ml"))
                return (quantity, "ml");
            else // 기본은 g
                return (quantity, "g");
        }
        
        return (100, "g"); // 기본값
    }

    [Obsolete("Use CalculateMultiplier and ParseWeightUnit instead")]
    private double ParseUnitQuantity(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 100;
        
        // 숫자 부분만 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            return quantity;
        }
        
        return 100; // 기본값
    }

    // 가격 이력 추가
    [RelayCommand]
    private async Task AddPriceHistory()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("가격을 추가할 음식을 먼저 선택해주세요.");
            return;
        }

        var viewModel = new PriceHistoryEditorViewModel(SelectedFood.Id, SelectedFood.Name);
        var dialog = new Views.PriceHistoryEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var newHistory = dialog.ViewModel.ToPriceHistory();
            await _priceHistoryRepository.AddAsync(newHistory);
            await LoadPriceHistoriesForSelectedFood();
        }
    }

    // 가격 이력 삭제
    [RelayCommand]
    private async Task DeletePriceHistory()
    {
        if (SelectedPriceHistory == null)
        {
            System.Windows.MessageBox.Show("삭제할 가격 이력을 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"{SelectedPriceHistory.EffectiveDate:yyyy-MM-dd}의 가격 이력을 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _priceHistoryRepository.DeleteAsync(SelectedPriceHistory.FoodId, SelectedPriceHistory.EffectiveDate);
            await LoadPriceHistoriesForSelectedFood();
        }
    }

    // 가격 이력 다중 삭제
    [RelayCommand]
    private async Task DeleteSelectedPriceHistories()
    {
        if (SelectedPriceHistories.Count == 0)
        {
            System.Windows.MessageBox.Show("삭제할 가격 이력을 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"{SelectedPriceHistories.Count}개의 가격 이력을 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var historiesToDelete = SelectedPriceHistories.ToList();
            foreach (var history in historiesToDelete)
            {
                await _priceHistoryRepository.DeleteAsync(history.FoodId, history.EffectiveDate);
            }
            SelectedPriceHistories.Clear();
            await LoadPriceHistoriesForSelectedFood();
            OptimizationStatus = $"{historiesToDelete.Count}개 가격 이력 삭제 완료";
        }
    }

    // 현재 날짜 기준으로 음식 가격 업데이트
    [RelayCommand]
    private async Task ApplyCurrentPrices()
    {
        foreach (var food in Foods)
        {
            var price = await _priceHistoryRepository.GetPriceAtDateAsync(food.Id, CurrentDate);
            if (price.HasValue && price.Value != food.PricePer100g)
            {
                var updatedFood = food with { PricePer100g = price.Value };
                await _foodRepository.UpdateAsync(updatedFood);
            }
        }
        await LoadFoods();
        OptimizationStatus = $"{CurrentDate:yyyy-MM-dd} 기준 가격 적용 완료";
    }

    // 저장된 식단 목록 불러오기
    [RelayCommand]
    private async Task LoadSavedDiets()
    {
        var diets = await _savedDietRepository.GetAllAsync();
        SavedDiets.Clear();
        foreach (var diet in diets)
        {
            SavedDiets.Add(diet);
        }
        
        OnPropertyChanged(nameof(SavedDietsCountText));
    }

    // 현재 최적화 결과를 식단으로 저장
    [RelayCommand]
    private async Task SaveCurrentDiet()
    {
        if (Picks.Count == 0)
        {
            System.Windows.MessageBox.Show("저장할 식단이 없습니다. 먼저 최적화를 실행해주세요.");
            return;
        }

        var dialog = new Views.SaveDietWindow();
        if (dialog.ShowDialog() == true)
        {
            var dietId = $"diet_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var diet = new SavedDiet(
                dietId,
                dialog.ViewModel.Name,
                DateOnly.FromDateTime(DateTime.Today),
                TotalCost,
                dialog.ViewModel.Notes);

            var items = Picks.Select(p => new SavedDietItem(
                dietId,
                "", // FoodId는 나중에 조회 시 매칭
                p.FoodName,
                p.AmountG / 100.0, // g을 100g 기준으로 변환하여 저장
                p.Cost)).ToList();

            await _savedDietRepository.SaveAsync(diet, items);
            await LoadSavedDiets();
            OptimizationStatus = $"'{dialog.ViewModel.Name}' 식단 저장 완료";
        }
    }

    // 저장된 식단 불러오기
    [RelayCommand]
    private async Task LoadSavedDiet()
    {
        if (SelectedSavedDiet == null)
        {
            System.Windows.MessageBox.Show("불러올 식단을 선택해주세요.");
            return;
        }

        var items = await _savedDietRepository.GetItemsAsync(SelectedSavedDiet.Id);
        
        Picks.Clear();
        foreach (var item in items)
        {
            var amountG = item.Amount100g * 100; // 100g 기준을 g으로 변환
            Picks.Add(new PickItem(item.FoodName, Math.Round(amountG, 1), item.Cost));
        }

        TotalCost = SelectedSavedDiet.TotalCost;
        OptimizationStatus = $"'{SelectedSavedDiet.Name}' 식단 불러옴";
    }

    // 저장된 식단 삭제
    [RelayCommand]
    private async Task DeleteSavedDiet()
    {
        if (SelectedSavedDiet == null)
        {
            System.Windows.MessageBox.Show("삭제할 식단을 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"'{SelectedSavedDiet.Name}' 식단을 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _savedDietRepository.DeleteAsync(SelectedSavedDiet.Id);
            await LoadSavedDiets();
            OptimizationStatus = "식단 삭제 완료";
        }
    }

    // 식단 히스토리 불러오기 (기간별)
    [RelayCommand]
    private async Task LoadDietHistories()
    {
        var histories = await _dietHistoryRepository.GetByDateRangeAsync(HistoryStartDate, HistoryEndDate);
        DietHistories.Clear();
        foreach (var history in histories)
        {
            DietHistories.Add(history);
        }
    }

    // 식단 히스토리 추가
    [RelayCommand]
    private async Task AddDietHistory()
    {
        var savedDiets = await _savedDietRepository.GetAllAsync();
        var dialog = new Views.DietHistoryEditorWindow(savedDiets);
        if (dialog.ShowDialog() == true)
        {
            var historyId = $"history_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var history = dialog.ViewModel.ToHistory(historyId);
            await _dietHistoryRepository.AddAsync(history);
            await LoadDietHistories();
            OptimizationStatus = "히스토리 추가 완료";
        }
    }

    // 식단 히스토리 수정
    [RelayCommand]
    private async Task EditDietHistory()
    {
        if (SelectedDietHistory == null)
        {
            System.Windows.MessageBox.Show("수정할 히스토리를 선택해주세요.");
            return;
        }

        var savedDiets = await _savedDietRepository.GetAllAsync();
        var dialog = new Views.DietHistoryEditorWindow(SelectedDietHistory, savedDiets);
        if (dialog.ShowDialog() == true)
        {
            var updatedHistory = dialog.ViewModel.ToHistory(SelectedDietHistory.Id);
            await _dietHistoryRepository.UpdateAsync(updatedHistory);
            await LoadDietHistories();
            OptimizationStatus = "히스토리 수정 완료";
        }
    }

    // 식단 히스토리 삭제
    [RelayCommand]
    private async Task DeleteDietHistory()
    {
        if (SelectedDietHistory == null)
        {
            System.Windows.MessageBox.Show("삭제할 히스토리를 선택해주세요.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"{SelectedDietHistory.Date:yyyy-MM-dd} {SelectedDietHistory.MealType} 히스토리를 삭제하시겠습니까?",
            "삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _dietHistoryRepository.DeleteAsync(SelectedDietHistory.Id);
            await LoadDietHistories();
            OptimizationStatus = "히스토리 삭제 완료";
        }
    }

    // 히스토리에서 식단 불러오기
    [RelayCommand]
    private async Task LoadDietFromHistory()
    {
        if (SelectedDietHistory == null || string.IsNullOrEmpty(SelectedDietHistory.SavedDietId))
        {
            System.Windows.MessageBox.Show("선택한 히스토리에 저장된 식단이 없습니다.");
            return;
        }

        var diet = await _savedDietRepository.GetByIdAsync(SelectedDietHistory.SavedDietId);
        if (diet == null)
        {
            System.Windows.MessageBox.Show("해당 식단을 찾을 수 없습니다.");
            return;
        }

        var items = await _savedDietRepository.GetItemsAsync(diet.Id);
        
        Picks.Clear();
        foreach (var item in items)
        {
            var amountG = item.Amount100g * 100; // 100g 기준을 g으로 변환
            Picks.Add(new PickItem(item.FoodName, Math.Round(amountG, 1), item.Cost));
        }

        TotalCost = diet.TotalCost;
        OptimizationStatus = $"'{diet.Name}' 식단 불러옴 ({SelectedDietHistory.Date:yyyy-MM-dd} {SelectedDietHistory.MealType})";
    }

    // 히스토리 패널 열기
    [RelayCommand]
    private void OpenHistoryPanel()
    {
        var window = new Views.DietHistoryPanelWindow(this);
        window.Show();
    }

    // 템플릿 관리 창 열기
    [RelayCommand]
    private void OpenTemplateManager()
    {
        var window = new Views.NutritionTemplateManagerWindow(_nutritionTemplateRepository, _templatePriceHistoryRepository);
        window.ShowDialog();
    }

    // 영양소 차트 열기
    [RelayCommand]
    private void ShowNutrientChart()
    {
        if (NutrientSummary.Count == 0)
        {
            System.Windows.MessageBox.Show("차트를 표시할 데이터가 없습니다. 먼저 최적화를 실행해주세요.");
            return;
        }

        // NutrientSummary를 Dictionary로 변환
        var summary = NutrientSummary.ToDictionary(
            item => item.Key,
            item => (item.Actual, item.Min, item.Max));

        var viewModel = new NutrientChartViewModel(summary);
        var window = new Views.NutrientChartWindow
        {
            DataContext = viewModel
        };
        window.Show();
    }

    // 최적화 결과 창 열기
    [RelayCommand]
    private void ShowOptimizationResult()
    {
        if (Picks.Count == 0)
        {
            System.Windows.MessageBox.Show("표시할 최적화 결과가 없습니다. 먼저 최적화를 실행해주세요.");
            return;
        }

        var window = new Views.OptimizationResultWindow(this);
        window.Show();
    }

    // 선호도 목록 불러오기
    [RelayCommand]
    private async Task LoadFoodPreferences()
    {
        var preferences = await _foodPreferenceRepository.GetAllAsync();
        FoodPreferences.Clear();
        foreach (var preference in preferences)
        {
            FoodPreferences.Add(preference);
        }
        OnPropertyChanged(nameof(FilteredFoods));
    }

    // 즐겨찾기 토글
    [RelayCommand]
    private async Task ToggleFavorite()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("음식을 선택해주세요.");
            return;
        }

        var existing = await _foodPreferenceRepository.GetByFoodIdAsync(SelectedFood.Id);
        var isFavorite = existing?.IsFavorite ?? false;
        var isExcluded = existing?.IsExcluded ?? false;

        var preference = new FoodPreference(
            SelectedFood.Id,
            !isFavorite,
            isExcluded,
            existing?.Notes ?? string.Empty);

        await _foodPreferenceRepository.SaveAsync(preference);
        await LoadFoodPreferences();
        OptimizationStatus = isFavorite ? "즐겨찾기 해제" : "즐겨찾기 추가";
    }

    // 제외 목록 토글
    [RelayCommand]
    private async Task ToggleExcluded()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("음식을 선택해주세요.");
            return;
        }

        var existing = await _foodPreferenceRepository.GetByFoodIdAsync(SelectedFood.Id);
        var isFavorite = existing?.IsFavorite ?? false;
        var isExcluded = existing?.IsExcluded ?? false;

        var preference = new FoodPreference(
            SelectedFood.Id,
            isFavorite,
            !isExcluded,
            existing?.Notes ?? string.Empty);

        await _foodPreferenceRepository.SaveAsync(preference);
        await LoadFoodPreferences();
        OptimizationStatus = isExcluded ? "제외 목록 해제" : "제외 목록 추가";
    }

    // 선호도 삭제
    [RelayCommand]
    private async Task ClearPreference()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("음식을 선택해주세요.");
            return;
        }

        await _foodPreferenceRepository.DeleteAsync(SelectedFood.Id);
        await LoadFoodPreferences();
        OptimizationStatus = "선호도 초기화 완료";
    }

    // 필터 매칭 (선호도 포함)
    private bool MatchesFilter(Food food)
    {
        // 기본 필터
        if (!string.IsNullOrWhiteSpace(Query) && !food.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ExcludeDairy && string.Equals(food.Category, "유제품", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ExcludeProcessed && food.Category is not null && food.Category.Contains("가공", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 선호도 필터
        var preference = FoodPreferences.FirstOrDefault(p => p.FoodId == food.Id);
        
        // 제외 목록 숨기기
        if (HideExcluded && preference?.IsExcluded == true)
        {
            return false;
        }

        // 즐겨찾기만 보기
        if (ShowFavoritesOnly && preference?.IsFavorite != true)
        {
            return false;
        }

        return true;
    }

    // 선택된 음식이 즐겨찾기인지 확인
    public bool IsSelectedFoodFavorite()
    {
        if (SelectedFood == null) return false;
        var preference = FoodPreferences.FirstOrDefault(p => p.FoodId == SelectedFood.Id);
        return preference?.IsFavorite ?? false;
    }

    // 선택된 음식이 제외 목록에 있는지 확인
    public bool IsSelectedFoodExcluded()
    {
        if (SelectedFood == null) return false;
        var preference = FoodPreferences.FirstOrDefault(p => p.FoodId == SelectedFood.Id);
        return preference?.IsExcluded ?? false;
    }

    public sealed record PickItem(string FoodName, double AmountG, double Cost);
    public sealed record NutrientSummaryItem(
        string Key, 
        double Actual, 
        double? Target, 
        double? Min, 
        double? Max,
        double AchievementRate  // 달성률 (0-200%)
    );
    
    // 가격 이력 표시용 클래스
    public sealed record PriceHistoryDisplayItem(
        string FoodId,
        string FoodName,
        DateOnly EffectiveDate,
        double TotalPrice,
        string WeightUnit,
        double UnitPrice,
        string UnitPriceDisplay
    );
    
    // 카테고리 제약 아이템
    public sealed partial class CategoryConstraintItem : ObservableObject
    {
        [ObservableProperty]
        private string category = string.Empty;
        
        [ObservableProperty]
        private int minCount;
        
        [ObservableProperty]
        private int maxCountPerFood = 3;
        
        public CategoryConstraintItem() { }
        
        public CategoryConstraintItem(string category, int minCount, int maxCountPerFood = 3)
        {
            Category = category;
            MinCount = minCount;
            MaxCountPerFood = maxCountPerFood;
        }
    }
    
    // 카테고리 제약 설정 창 열기
    [RelayCommand]
    private void OpenCategoryConstraints()
    {
        var window = new Views.CategoryConstraintsWindow(this);
        window.ShowDialog();
    }
    
    // 카테고리 제약 로드
    private async Task LoadCategoryConstraints()
    {
        var settings = await _optimizationSettingsRepository.GetAllAsync();
        CategoryConstraints.Clear();
        
        // 전역 설정 로드 (특별한 카테고리 이름 사용)
        var globalSetting = settings.FirstOrDefault(s => s.Category == "__OptimizationMode__");
        if (globalSetting != null)
        {
            // MinCount 필드에 모드를 숫자로 저장 (0=Cost, 1=Balanced, 2=Nutrition)
            OptimizationMode = globalSetting.MinCount switch
            {
                0 => "Cost",
                2 => "Nutrition",
                _ => "Balanced"
            };
        }
        
        // 카테고리별 제약 로드
        var categorySettings = settings.Where(s => s.Category != "__OptimizationMode__").ToList();
        if (categorySettings.Count > 0)
        {
            foreach (var setting in categorySettings)
            {
                CategoryConstraints.Add(new CategoryConstraintItem(setting.Category, setting.MinCount, setting.MaxCountPerFood));
            }
        }
        else
        {
            // 기본값 설정
            CategoryConstraints.Add(new CategoryConstraintItem("밥", 1, 3));
            CategoryConstraints.Add(new CategoryConstraintItem("반찬", 2, 3));
            CategoryConstraints.Add(new CategoryConstraintItem("간식", 1, 2));
        }
    }
    
    // 카테고리 제약 저장
    public async Task SaveCategoryConstraints()
    {
        var settings = new List<OptimizationSettings>();
        
        // 전역 설정 저장 (최적화 모드)
        int modeValue = OptimizationMode switch
        {
            "Cost" => 0,
            "Nutrition" => 2,
            _ => 1 // Balanced
        };
        settings.Add(new OptimizationSettings("__OptimizationMode__", modeValue, 0));
        
        // 카테고리별 제약 저장
        settings.AddRange(CategoryConstraints
            .Where(c => !string.IsNullOrWhiteSpace(c.Category))
            .Select(c => new OptimizationSettings(c.Category, c.MinCount, c.MaxCountPerFood)));
        
        await _optimizationSettingsRepository.SaveAllAsync(settings);
    }
    
    // === 수동 식단 관리 ===
    
    // 선택한 음식을 수동 식단에 추가
    [RelayCommand]
    private void AddToManualDiet()
    {
        if (SelectedFood == null)
        {
            System.Windows.MessageBox.Show("추가할 음식을 선택해주세요.");
            return;
        }
        
        // 이미 추가된 음식인지 확인
        if (ManualDietItems.Any(item => item.FoodId == SelectedFood.Id))
        {
            System.Windows.MessageBox.Show("이미 추가된 음식입니다.");
            return;
        }
        
        var item = new ManualDietItem
        {
            FoodId = SelectedFood.Id,
            FoodName = SelectedFood.Name,
            AmountG = 100,
            PricePer100g = SelectedFood.PricePer100g,
            Kcal = SelectedFood.Kcal,
            ProteinG = SelectedFood.ProteinG,
            FatG = SelectedFood.FatG,
            CarbsG = SelectedFood.CarbsG,
            FiberG = SelectedFood.FiberG,
            SodiumMg = SelectedFood.SodiumMg,
            SaturatedFatG = SelectedFood.SaturatedFatG,
            TransFatG = SelectedFood.TransFatG,
            SugarG = SelectedFood.SugarG,
            CholesterolMg = SelectedFood.CholesterolMg,
            CalciumMg = SelectedFood.CalciumMg,
            IronMg = SelectedFood.IronMg,
            MagnesiumMg = SelectedFood.MagnesiumMg,
            PhosphorusMg = SelectedFood.PhosphorusMg,
            PotassiumMg = SelectedFood.PotassiumMg,
            ZincMg = SelectedFood.ZincMg,
            CopperMg = SelectedFood.CopperMg,
            ManganeseMg = SelectedFood.ManganeseMg,
            SeleniumUg = SelectedFood.SeleniumUg,
            MolybdenumUg = SelectedFood.MolybdenumUg,
            IodineUg = SelectedFood.IodineUg,
            VitaminAUg = SelectedFood.VitaminAUg,
            VitaminCMg = SelectedFood.VitaminCMg,
            VitaminDUg = SelectedFood.VitaminDUg,
            VitaminEMg = SelectedFood.VitaminEMg,
            VitaminKUg = SelectedFood.VitaminKUg,
            VitaminB1Mg = SelectedFood.VitaminB1Mg,
            VitaminB2Mg = SelectedFood.VitaminB2Mg,
            VitaminB3Mg = SelectedFood.VitaminB3Mg,
            VitaminB5Mg = SelectedFood.VitaminB5Mg,
            VitaminB6Mg = SelectedFood.VitaminB6Mg,
            VitaminB7Ug = SelectedFood.VitaminB7Ug,
            VitaminB9Ug = SelectedFood.VitaminB9Ug,
            VitaminB12Ug = SelectedFood.VitaminB12Ug
        };
        
        // PropertyChanged 이벤트 구독 (양 변경 시 재계산)
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ManualDietItem.AmountG))
            {
                RecalculateManualDiet();
            }
        };
        
        ManualDietItems.Add(item);
    }
    
    // 수동 식단에서 항목 제거
    [RelayCommand]
    private void RemoveFromManualDiet(ManualDietItem item)
    {
        ManualDietItems.Remove(item);
    }
    
    // 수동 식단 초기화
    [RelayCommand]
    private void ClearManualDiet()
    {
        if (ManualDietItems.Count == 0)
            return;
            
        var result = System.Windows.MessageBox.Show(
            "수동 식단을 모두 초기화하시겠습니까?",
            "초기화 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            ManualDietItems.Clear();
        }
    }
    
    // 수동 식단 재계산
    private void RecalculateManualDiet()
    {
        if (!ManualDietItems.Any())
        {
            ManualDietTotalCost = 0;
            ManualDietNutritionScore = 0;
            // NutrientSummary는 최적화 결과와 공유하므로 초기화하지 않음
            return;
        }
        
        // 총 비용 계산
        double totalCost = 0;
        foreach (var item in ManualDietItems)
        {
            double multiplier = item.AmountG / 100.0;
            totalCost += item.PricePer100g * multiplier;
        }
        ManualDietTotalCost = Math.Round(totalCost, 2);
        
        // 영양소 합계 계산
        var nutrientSummary = new Dictionary<string, (double actual, double? target, double? min, double? max)>();
        
        // 각 영양소별로 합산
        double totalKcal = 0, totalProtein = 0, totalFat = 0, totalCarbs = 0, totalFiber = 0;
        double totalSodium = 0, totalSaturatedFat = 0, totalTransFat = 0, totalSugar = 0, totalCholesterol = 0;
        double totalCalcium = 0, totalIron = 0, totalMagnesium = 0, totalPhosphorus = 0, totalPotassium = 0;
        double totalZinc = 0, totalCopper = 0, totalManganese = 0, totalSelenium = 0, totalMolybdenum = 0, totalIodine = 0;
        double totalVitaminA = 0, totalVitaminC = 0, totalVitaminD = 0, totalVitaminE = 0, totalVitaminK = 0;
        double totalVitaminB1 = 0, totalVitaminB2 = 0, totalVitaminB3 = 0, totalVitaminB5 = 0, totalVitaminB6 = 0;
        double totalVitaminB7 = 0, totalVitaminB9 = 0, totalVitaminB12 = 0;
        
        foreach (var item in ManualDietItems)
        {
            double multiplier = item.AmountG / 100.0;
            
            totalKcal += item.Kcal * multiplier;
            totalProtein += item.ProteinG * multiplier;
            totalFat += item.FatG * multiplier;
            totalCarbs += item.CarbsG * multiplier;
            totalFiber += item.FiberG * multiplier;
            totalSodium += item.SodiumMg * multiplier;
            totalSaturatedFat += item.SaturatedFatG * multiplier;
            totalTransFat += item.TransFatG * multiplier;
            totalSugar += item.SugarG * multiplier;
            totalCholesterol += item.CholesterolMg * multiplier;
            totalCalcium += item.CalciumMg * multiplier;
            totalIron += item.IronMg * multiplier;
            totalMagnesium += item.MagnesiumMg * multiplier;
            totalPhosphorus += item.PhosphorusMg * multiplier;
            totalPotassium += item.PotassiumMg * multiplier;
            totalZinc += item.ZincMg * multiplier;
            totalCopper += item.CopperMg * multiplier;
            totalManganese += item.ManganeseMg * multiplier;
            totalSelenium += item.SeleniumUg * multiplier;
            totalMolybdenum += item.MolybdenumUg * multiplier;
            totalIodine += item.IodineUg * multiplier;
            totalVitaminA += item.VitaminAUg * multiplier;
            totalVitaminC += item.VitaminCMg * multiplier;
            totalVitaminD += item.VitaminDUg * multiplier;
            totalVitaminE += item.VitaminEMg * multiplier;
            totalVitaminK += item.VitaminKUg * multiplier;
            totalVitaminB1 += item.VitaminB1Mg * multiplier;
            totalVitaminB2 += item.VitaminB2Mg * multiplier;
            totalVitaminB3 += item.VitaminB3Mg * multiplier;
            totalVitaminB5 += item.VitaminB5Mg * multiplier;
            totalVitaminB6 += item.VitaminB6Mg * multiplier;
            totalVitaminB7 += item.VitaminB7Ug * multiplier;
            totalVitaminB9 += item.VitaminB9Ug * multiplier;
            totalVitaminB12 += item.VitaminB12Ug * multiplier;
        }
        
        // 목표치와 비교하여 영양 점수 계산
        double totalScore = 0;
        int targetCount = 0;
        
        foreach (var target in Targets)
        {
            double actual = target.NutrientKey switch
            {
                "Kcal" => totalKcal,
                "ProteinG" => totalProtein,
                "FatG" => totalFat,
                "CarbsG" => totalCarbs,
                "FiberG" => totalFiber,
                "SodiumMg" => totalSodium,
                "SaturatedFatG" => totalSaturatedFat,
                "TransFatG" => totalTransFat,
                "SugarG" => totalSugar,
                "CholesterolMg" => totalCholesterol,
                "CalciumMg" => totalCalcium,
                "IronMg" => totalIron,
                "MagnesiumMg" => totalMagnesium,
                "PhosphorusMg" => totalPhosphorus,
                "PotassiumMg" => totalPotassium,
                "ZincMg" => totalZinc,
                "CopperMg" => totalCopper,
                "ManganeseMg" => totalManganese,
                "SeleniumUg" => totalSelenium,
                "MolybdenumUg" => totalMolybdenum,
                "IodineUg" => totalIodine,
                "VitaminAUg" => totalVitaminA,
                "VitaminCMg" => totalVitaminC,
                "VitaminDUg" => totalVitaminD,
                "VitaminEMg" => totalVitaminE,
                "VitaminKUg" => totalVitaminK,
                "VitaminB1Mg" => totalVitaminB1,
                "VitaminB2Mg" => totalVitaminB2,
                "VitaminB3Mg" => totalVitaminB3,
                "VitaminB5Mg" => totalVitaminB5,
                "VitaminB6Mg" => totalVitaminB6,
                "VitaminB7Ug" => totalVitaminB7,
                "VitaminB9Ug" => totalVitaminB9,
                "VitaminB12Ug" => totalVitaminB12,
                _ => 0
            };
            
            // 달성률 계산 (min-max 범위 내에서)
            double achievementRate = 0;
            if (target.Min.HasValue && target.Max.HasValue)
            {
                if (actual < target.Min.Value)
                {
                    achievementRate = (actual / target.Min.Value) * 100.0;
                }
                else if (actual > target.Max.Value)
                {
                    achievementRate = (target.Max.Value / actual) * 100.0;
                }
                else
                {
                    achievementRate = 100.0;
                }
            }
            else if (target.Min.HasValue)
            {
                achievementRate = Math.Min((actual / target.Min.Value) * 100.0, 100.0);
            }
            else if (target.Max.HasValue)
            {
                achievementRate = actual <= target.Max.Value ? 100.0 : (target.Max.Value / actual) * 100.0;
            }
            
            totalScore += achievementRate;
            targetCount++;
        }
        
        // 평균 영양 점수
        ManualDietNutritionScore = targetCount > 0 ? Math.Round(totalScore / targetCount, 1) : 0;
    }
}

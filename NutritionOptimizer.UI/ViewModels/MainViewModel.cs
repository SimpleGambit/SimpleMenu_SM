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
    private readonly DietOptimizer _optimizer = new();

    public ObservableCollection<Food> Foods { get; } = new();
    public ObservableCollection<NutrientTarget> Targets { get; } = new();
    public ObservableCollection<PickItem> Picks { get; } = new();
    public ObservableCollection<NutrientSummaryItem> NutrientSummary { get; } = new();
    public ObservableCollection<PriceHistoryDisplayItem> PriceHistories { get; } = new();
    public ObservableCollection<SavedDiet> SavedDiets { get; } = new();
    public ObservableCollection<DietHistory> DietHistories { get; } = new();
    public ObservableCollection<FoodPreference> FoodPreferences { get; } = new();

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
    private Food? selectedFood;

    [ObservableProperty]
    private NutrientTarget? selectedTarget;

    [ObservableProperty]
    private PriceHistoryDisplayItem? selectedPriceHistory;

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

    public IEnumerable<Food> FilteredFoods => Foods.Where(MatchesFilter);

    // 데이터 개수 표시용 프로퍼티
    public string FoodsCountText => $"음식 목록 ({Foods.Count})";
    public string TargetsCountText => $"영양 목표치 ({Targets.Count})";
    public string PriceHistoriesCountText => $"가격 이력 ({PriceHistories.Count})";
    public string SavedDietsCountText => $"저장된 식단 ({SavedDiets.Count})";
    public string PicksCountText => Picks.Count > 0 ? $"최적화 결과 ({Picks.Count})" : "최적화 결과";

    public MainViewModel(IFoodRepository foodRepository, ITargetsRepository targetsRepository, IPriceHistoryRepository priceHistoryRepository, ISavedDietRepository savedDietRepository, IDietHistoryRepository dietHistoryRepository, IFoodPreferenceRepository foodPreferenceRepository, INutritionTemplateRepository nutritionTemplateRepository)
    {
        _foodRepository = foodRepository;
        _targetsRepository = targetsRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _savedDietRepository = savedDietRepository;
        _dietHistoryRepository = dietHistoryRepository;
        _foodPreferenceRepository = foodPreferenceRepository;
        _nutritionTemplateRepository = nutritionTemplateRepository;
    }

    // 프로그램 초기화 시 자동 데이터 로드
    public async Task InitializeAsync()
    {
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

        var foods = FilteredFoods.ToList();
        var targets = Targets.ToList();

        var result = await Task.Run(() => _optimizer.Optimize(foods, targets, StepG));

        Picks.Clear();
        foreach (var pick in result.Picks)
        {
            var cost = pick.Food.PricePer100g * pick.Amount100g;
            Picks.Add(new PickItem(pick.Food.Name, Math.Round(pick.Amount100g, 2), Math.Round(cost)));
        }

        NutrientSummary.Clear();
        foreach (var (key, values) in result.NutrientSummary.OrderBy(entry => entry.Key))
        {
            var (actual, min, max) = values;
            NutrientSummary.Add(new NutrientSummaryItem(key, Math.Round(actual, 2), min, max));
        }

        TotalCost = Math.Round(result.TotalCost);
        OptimizationStatus = result.Feasible ? "Feasible" : $"Infeasible: {result.InfeasibleReason}";
        
        OnPropertyChanged(nameof(PicksCountText));
    }

    partial void OnQueryChanged(string? value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnExcludeDairyChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnExcludeProcessedChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnShowFavoritesOnlyChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    partial void OnHideExcludedChanged(bool value) => OnPropertyChanged(nameof(FilteredFoods));

    // 음식 추가
    [RelayCommand]
    private async Task AddFood()
    {
        var viewModel = new FoodEditorViewModel(_nutritionTemplateRepository);
        var dialog = new Views.FoodEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var newFood = dialog.ViewModel.ToFood();
            await _foodRepository.AddAsync(newFood);
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

        var viewModel = new FoodEditorViewModel(SelectedFood, _nutritionTemplateRepository);
        var dialog = new Views.FoodEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            var updatedFood = dialog.ViewModel.ToFood();
            await _foodRepository.UpdateAsync(updatedFood);
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
        // 음식의 실제 단위 정보 파싱 (예: "100g", "200ml", "50g" 등)
        var weightUnit = food.WeightUnit;
        double unitQuantity = ParseUnitQuantity(weightUnit);
        
        // 총 가격 계산 (해당 단위의 가격)
        double totalPrice = history.PricePer100g * (unitQuantity / 100.0);
        
        // 단위당 가격 표시 결정
        string unitPriceDisplay;
        double unitPrice;
        
        if (unitQuantity < 100)
        {
            // 100g 미만이면 10g당 가격으로 표시
            unitPrice = history.PricePer100g / 10.0;
            unitPriceDisplay = $"{unitPrice:N0}원/10g";
        }
        else
        {
            // 100g 이상이면 100g당 가격으로 표시
            unitPrice = history.PricePer100g;
            unitPriceDisplay = $"{unitPrice:N0}원/100g";
        }
        
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

    // 단위에서 수량 파싱 (예: "100g" -> 100, "200ml" -> 200)
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
                p.Amount100g,
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
            Picks.Add(new PickItem(item.FoodName, item.Amount100g, item.Cost));
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
            Picks.Add(new PickItem(item.FoodName, item.Amount100g, item.Cost));
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
        var window = new Views.NutritionTemplateManagerWindow(_nutritionTemplateRepository);
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

    public sealed record PickItem(string FoodName, double Amount100g, double Cost);
    public sealed record NutrientSummaryItem(string Key, double Actual, double? Min, double? Max);
    
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
}
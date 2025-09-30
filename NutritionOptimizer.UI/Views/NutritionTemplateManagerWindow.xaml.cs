using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

// 가격 이력 표시용 클래스
public sealed record PriceHistoryDisplayItem(
    string TemplateId,
    string Brand,
    DateOnly EffectiveDate,
    string WeightUnit,
    double Price,
    double PricePer100g
);

public partial class NutritionTemplateManagerWindow : Window
{
    private readonly INutritionTemplateRepository _repository;
    private readonly ITemplatePriceHistoryRepository _priceHistoryRepository;
    public ObservableCollection<NutritionTemplate> Templates { get; } = new();
    public ObservableCollection<NutritionTemplate> FilteredTemplates { get; } = new();
    public ObservableCollection<NutritionTemplate> SelectedTemplates { get; } = new();
    public ObservableCollection<PriceHistoryDisplayItem> PriceHistories { get; } = new();
    private string _currentSearchText = string.Empty;

    public NutritionTemplateManagerWindow(INutritionTemplateRepository repository, ITemplatePriceHistoryRepository priceHistoryRepository)
    {
        InitializeComponent();
        _repository = repository;
        _priceHistoryRepository = priceHistoryRepository;
        TemplatesDataGrid.ItemsSource = FilteredTemplates;
        TemplateComboBox.ItemsSource = Templates;
        PriceHistoriesDataGrid.ItemsSource = PriceHistories;
        LoadTemplates();
    }

    // 기존 생성자 (호환성 유지)
    public NutritionTemplateManagerWindow(INutritionTemplateRepository repository)
        : this(repository, null!)
    {
    }

    // 템플릿 목록 불러오기
    private async void LoadTemplates()
    {
        try
        {
            var templates = await _repository.GetAllAsync();
            Templates.Clear();
            foreach (var template in templates.OrderBy(t => t.Name))
            {
                Templates.Add(template);
            }
            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"템플릿 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 검색 필터 적용
    private void ApplySearchFilter()
    {
        FilteredTemplates.Clear();

        if (string.IsNullOrWhiteSpace(_currentSearchText))
        {
            // 검색어가 없으면 모든 템플릿 표시
            foreach (var template in Templates)
            {
                FilteredTemplates.Add(template);
            }
        }
        else
        {
            // 검색어로 필터링 (이름, 브랜드, 구분, 분류)
            var searchLower = _currentSearchText.ToLower();
            foreach (var template in Templates)
            {
                if (template.Name.ToLower().Contains(searchLower) ||
                    (template.Brand?.ToLower().Contains(searchLower) ?? false) ||
                    template.Type.ToLower().Contains(searchLower) ||
                    template.Category.ToLower().Contains(searchLower))
                {
                    FilteredTemplates.Add(template);
                }
            }
        }
    }

    // 검색 버튼 클릭
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _currentSearchText = SearchTextBox.Text;
        ApplySearchFilter();
    }

    // 검색 초기화 버튼 클릭
    private void ResetSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = string.Empty;
        _currentSearchText = string.Empty;
        ApplySearchFilter();
    }

    // 검색창에서 Enter 키 누르면 검색
    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchButton_Click(sender, e);
        }
    }

    // 추가 버튼
    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NutritionTemplateEditorWindow();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var newTemplate = dialog.ViewModel.ToTemplate();
                await _repository.AddAsync(newTemplate);
                LoadTemplates();
                MessageBox.Show("템플릿이 추가되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 추가 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 수정 버튼
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedTemplate = TemplatesDataGrid.SelectedItem as NutritionTemplate;
        if (selectedTemplate == null)
        {
            MessageBox.Show("수정할 템플릿을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var viewModel = new NutritionTemplateEditorViewModel(selectedTemplate);
        var dialog = new NutritionTemplateEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var updatedTemplate = dialog.ViewModel.ToTemplate();
                await _repository.UpdateAsync(updatedTemplate);
                LoadTemplates();
                MessageBox.Show("템플릿이 수정되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 수정 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 삭제 버튼
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedTemplate = TemplatesDataGrid.SelectedItem as NutritionTemplate;
        if (selectedTemplate == null)
        {
            MessageBox.Show("삭제할 템플릿을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"'{selectedTemplate.Name}' 템플릿을 삭제하시겠습니까?",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _repository.DeleteAsync(selectedTemplate.Id);
                LoadTemplates();
                MessageBox.Show("템플릿이 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 삭제 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 더블클릭으로 수정
    private void TemplatesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditButton_Click(sender, e);
    }

    // 닫기 버튼
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // 다중 삭제 버튼
    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTemplates.Count == 0)
        {
            MessageBox.Show("삭제할 템플릿을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"{SelectedTemplates.Count}개의 템플릿을 삭제하시겠습니까?",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var templatesToDelete = SelectedTemplates.ToList();
                foreach (var template in templatesToDelete)
                {
                    await _repository.DeleteAsync(template.Id);
                }
                SelectedTemplates.Clear();
                LoadTemplates();
                MessageBox.Show($"{templatesToDelete.Count}개의 템플릿이 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 삭제 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 다중 선택 동기화
    private void TemplatesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SelectedTemplates.Clear();
        foreach (var item in TemplatesDataGrid.SelectedItems)
        {
            if (item is NutritionTemplate template)
            {
                SelectedTemplates.Add(template);
            }
        }
    }

    // === 가격 이력 관련 메서드 ===

    // 템플릿 선택 변경 시 가격 이력 로드
    private async void TemplateComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TemplateComboBox.SelectedItem is not NutritionTemplate template || _priceHistoryRepository == null)
        {
            PriceHistories.Clear();
            // 헤더를 기본값으로 재설정
            UnitPriceColumn.Header = "100g/100ml당 가격";
            return;
        }

        // 템플릿의 WeightUnit에서 단위를 추출하여 컬럼 헤더 변경
        var unit = ExtractUnitType(template.WeightUnit);
        UnitPriceColumn.Header = $"100{unit}당 가격";

        try
        {
            var histories = await _priceHistoryRepository.GetByTemplateIdAsync(template.Id);
            PriceHistories.Clear();
            foreach (var history in histories.OrderByDescending(h => h.EffectiveDate))
            {
                var pricePer100g = CalculatePricePer100g(history.WeightUnit, history.Price);
                var displayItem = new PriceHistoryDisplayItem(
                    history.TemplateId,
                    history.Brand,
                    history.EffectiveDate,
                    history.WeightUnit,
                    history.Price,
                    pricePer100g
                );
                PriceHistories.Add(displayItem);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"가격 이력 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 100g당 가격 계산
    private static double CalculatePricePer100g(string weightUnit, double price)
    {
        var multiplier = CalculateMultiplierFromUnit(weightUnit);
        return Math.Round(price / multiplier, 2);
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

    // WeightUnit에서 단위 타입 추출 (g 또는 ml)
    private static string ExtractUnitType(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return "g";

        var unit = weightUnit.ToLower();

        // ml 또는 l (리터) 단위인 경우 "ml" 반환
        if (unit.Contains("ml") || (unit.Contains("l") && !unit.Contains("lb")))
        {
            return "ml";
        }

        // 기본은 g (g, kg 포함)
        return "g";
    }

    // 가격 추가 버튼
    private async void AddPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_priceHistoryRepository == null)
        {
            MessageBox.Show("가격 이력 기능을 사용할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TemplateComboBox.SelectedItem is not NutritionTemplate template)
        {
            MessageBox.Show("템플릿을 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new TemplatePriceHistoryEditorWindow(template.Id, template.Name, template.Brand ?? string.Empty);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var newHistory = dialog.ViewModel.ToPriceHistory();
                await _priceHistoryRepository.AddAsync(newHistory);
                TemplateComboBox_SelectionChanged(TemplateComboBox, null!);
                MessageBox.Show("가격 이력이 추가되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가격 이력 추가 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 가격 수정 버튼
    private async void EditPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_priceHistoryRepository == null)
        {
            MessageBox.Show("가격 이력 기능을 사용할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PriceHistoriesDataGrid.SelectedItem is not PriceHistoryDisplayItem selectedPrice)
        {
            MessageBox.Show("수정할 가격 이력을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (TemplateComboBox.SelectedItem is not NutritionTemplate template)
        {
            return;
        }

        var priceHistory = new TemplatePriceHistory(
            selectedPrice.TemplateId,
            selectedPrice.Brand,
            selectedPrice.EffectiveDate,
            selectedPrice.WeightUnit,
            selectedPrice.Price
        );

        var viewModel = new TemplatePriceHistoryEditorViewModel(priceHistory, template.Name);
        var dialog = new TemplatePriceHistoryEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var updatedHistory = dialog.ViewModel.ToPriceHistory();
                await _priceHistoryRepository.UpdateAsync(updatedHistory);
                TemplateComboBox_SelectionChanged(TemplateComboBox, null!);
                MessageBox.Show("가격 이력이 수정되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가격 이력 수정 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 가격 삭제 버튼
    private async void DeletePriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_priceHistoryRepository == null)
        {
            MessageBox.Show("가격 이력 기능을 사용할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PriceHistoriesDataGrid.SelectedItem is not PriceHistoryDisplayItem selectedPrice)
        {
            MessageBox.Show("삭제할 가격 이력을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"{selectedPrice.Brand} - {selectedPrice.EffectiveDate:yyyy-MM-dd} 가격 이력을 삭제하시겠습니까?",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _priceHistoryRepository.DeleteAsync(
                    selectedPrice.TemplateId,
                    selectedPrice.Brand,
                    selectedPrice.EffectiveDate);
                TemplateComboBox_SelectionChanged(TemplateComboBox, null!);
                MessageBox.Show("가격 이력이 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가격 이력 삭제 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 가격 이력 더블클릭으로 수정
    private void PriceHistoriesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditPriceButton_Click(sender, e);
    }
}

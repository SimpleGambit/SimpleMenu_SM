using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class FoodEditorWindow : Window
{
    public FoodEditorViewModel ViewModel { get; }
    private List<NutritionTemplate> _allTemplates = new();

    // 새 음식 추가
    public FoodEditorWindow()
    {
        InitializeComponent();
        ViewModel = new FoodEditorViewModel();
        DataContext = ViewModel;
        
        // 템플릿이 로드될 때까지 대기
        Loaded += (s, e) => LoadAllTemplates();
    }

    // 기존 음식 수정
    public FoodEditorWindow(FoodEditorViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // 템플릿이 로드될 때까지 대기
        Loaded += (s, e) => LoadAllTemplates();
    }

    // 전체 템플릿 목록 저장 (검색용)
    private void LoadAllTemplates()
    {
        _allTemplates = ViewModel.Templates.ToList();
    }

    // 템플릿 검색
    private void TemplateSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = TemplateSearchBox.Text;
        
        // 전체 목록이 비어있으면 현재 Templates를 저장
        if (_allTemplates.Count == 0)
        {
            _allTemplates = ViewModel.Templates.ToList();
        }
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // 검색어가 없으면 전체 목록 표시
            ViewModel.Templates.Clear();
            foreach (var template in _allTemplates)
            {
                ViewModel.Templates.Add(template);
            }
        }
        else
        {
            // 검색어가 있으면 필터링
            var filtered = _allTemplates
                .Where(t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           t.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            ViewModel.Templates.Clear();
            foreach (var template in filtered)
            {
                ViewModel.Templates.Add(template);
            }
        }
    }

    // 음식 검색
    private void FoodSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = FoodSearchBox.Text;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // 검색어가 없으면 전체 목록 표시 (필요시 구현)
        }
        else
        {
            // 검색어로 필터링 (필요시 구현)
        }
    }

    // 저장 버튼 클릭
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid())
        {
            MessageBox.Show("필수 항목(이름, 단위)을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    // 취소 버튼 클릭
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

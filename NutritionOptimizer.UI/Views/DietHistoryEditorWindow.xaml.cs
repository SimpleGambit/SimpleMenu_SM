using System.Collections.Generic;
using System.Windows;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class DietHistoryEditorWindow : Window
{
    public DietHistoryEditorViewModel ViewModel { get; }
    private readonly IReadOnlyList<SavedDiet> _savedDiets;

    public DietHistoryEditorWindow(IReadOnlyList<SavedDiet> savedDiets)
    {
        InitializeComponent();
        _savedDiets = savedDiets;
        ViewModel = new DietHistoryEditorViewModel();
        DataContext = ViewModel;
    }

    public DietHistoryEditorWindow(DietHistory history, IReadOnlyList<SavedDiet> savedDiets)
    {
        InitializeComponent();
        _savedDiets = savedDiets;
        ViewModel = new DietHistoryEditorViewModel(history);
        DataContext = ViewModel;
    }

    // 저장된 식단 선택
    private void SelectSavedDietButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SavedDietSelectorWindow(_savedDiets);
        if (dialog.ShowDialog() == true && dialog.SelectedDiet != null)
        {
            ViewModel.SetSavedDiet(dialog.SelectedDiet);
        }
    }

    // 저장 버튼 클릭
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.Views;

public partial class SavedDietSelectorWindow : Window
{
    public SavedDiet? SelectedDiet { get; private set; }

    public SavedDietSelectorWindow(IReadOnlyList<SavedDiet> savedDiets)
    {
        InitializeComponent();
        DietsDataGrid.ItemsSource = savedDiets;
    }

    // 더블클릭으로 선택
    private void DietsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DietsDataGrid.SelectedItem is SavedDiet diet)
        {
            SelectedDiet = diet;
            DialogResult = true;
            Close();
        }
    }

    // 선택 버튼 클릭
    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DietsDataGrid.SelectedItem is SavedDiet diet)
        {
            SelectedDiet = diet;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("식단을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // 취소 버튼 클릭
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class FoodEditorWindow : Window
{
    public FoodEditorViewModel ViewModel { get; }

    // 새 음식 추가
    public FoodEditorWindow()
    {
        InitializeComponent();
        ViewModel = new FoodEditorViewModel();
        DataContext = ViewModel;
    }

    // 기존 음식 수정
    public FoodEditorWindow(FoodEditorViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    // 저장 버튼 클릭
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid())
        {
            MessageBox.Show("필수 항목을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
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

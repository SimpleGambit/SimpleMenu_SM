using System.Windows;

namespace NutritionOptimizer.UI.Views;

public partial class CategoryConstraintsWindow : Window
{
    public ViewModels.MainViewModel MainViewModel { get; }

    public CategoryConstraintsWindow(ViewModels.MainViewModel mainViewModel)
    {
        InitializeComponent();
        MainViewModel = mainViewModel;
        DataContext = this;
    }

    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        MainViewModel.CategoryConstraints.Add(new ViewModels.MainViewModel.CategoryConstraintItem("", 1, 3));
    }

    private void ClearCategories_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "모든 카테고리 제약을 초기화하시겠습니까?",
            "초기화 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            MainViewModel.CategoryConstraints.Clear();
        }
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        // 설정 저장
        await MainViewModel.SaveCategoryConstraints();
        
        DialogResult = true;
        Close();
    }
}

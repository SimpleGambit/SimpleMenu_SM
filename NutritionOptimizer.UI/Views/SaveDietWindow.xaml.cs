using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class SaveDietWindow : Window
{
    public SaveDietViewModel ViewModel { get; }

    public SaveDietWindow()
    {
        InitializeComponent();
        ViewModel = new SaveDietViewModel();
        DataContext = ViewModel;
    }

    // 저장 버튼 클릭
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid())
        {
            MessageBox.Show("식단 이름을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
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

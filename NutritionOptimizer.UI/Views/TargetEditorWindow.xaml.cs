using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class TargetEditorWindow : Window
{
    public TargetEditorViewModel ViewModel { get; }

    // 새 목표치 추가
    public TargetEditorWindow()
    {
        InitializeComponent();
        ViewModel = new TargetEditorViewModel();
        DataContext = ViewModel;
    }

    // 기존 목표치 수정
    public TargetEditorWindow(TargetEditorViewModel viewModel) : this()
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

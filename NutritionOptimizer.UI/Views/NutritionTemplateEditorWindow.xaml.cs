using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class NutritionTemplateEditorWindow : Window
{
    public NutritionTemplateEditorViewModel ViewModel { get; }

    // 새 템플릿 추가
    public NutritionTemplateEditorWindow()
    {
        InitializeComponent();
        ViewModel = new NutritionTemplateEditorViewModel();
        DataContext = ViewModel;
    }

    // 기존 템플릿 수정
    public NutritionTemplateEditorWindow(NutritionTemplateEditorViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    // 저장 버튼
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid())
        {
            MessageBox.Show("필수 항목(이름, 카테고리)을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    // 취소 버튼
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

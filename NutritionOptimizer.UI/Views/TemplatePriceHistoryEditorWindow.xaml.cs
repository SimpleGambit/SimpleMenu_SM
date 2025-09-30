using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class TemplatePriceHistoryEditorWindow : Window
{
    public TemplatePriceHistoryEditorViewModel ViewModel { get; }

    // 새 가격 이력 추가
    public TemplatePriceHistoryEditorWindow(string templateId, string templateName, string templateBrand)
    {
        InitializeComponent();
        ViewModel = new TemplatePriceHistoryEditorViewModel(templateId, templateName, templateBrand);
        DataContext = ViewModel;
    }

    // 기존 가격 이력 수정
    public TemplatePriceHistoryEditorWindow(TemplatePriceHistoryEditorViewModel viewModel)
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
            MessageBox.Show("필수 항목(단위, 가격)을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
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

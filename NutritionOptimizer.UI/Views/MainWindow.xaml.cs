using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    // 창이 로드될 때 자동으로 데이터 불러오기
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    // 음식 목록 더블클릭 시 상세보기 창 열기
    private void FoodDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedFood != null)
        {
            var detailWindow = new FoodDetailWindow(viewModel.SelectedFood)
            {
                Owner = this
            };
            detailWindow.ShowDialog();
        }
    }

    // 목표치 셋 편집 종료 시 자동 저장
    private async void TargetsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && DataContext is MainViewModel viewModel)
        {
            // UI 업데이트가 완료될 때까지 대기
            await Dispatcher.InvokeAsync(async () =>
            {
                if (e.Row.Item is NutrientTarget target)
                {
                    // 편집된 값을 저장
                    await viewModel.UpdateTargetAsync(target);
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}

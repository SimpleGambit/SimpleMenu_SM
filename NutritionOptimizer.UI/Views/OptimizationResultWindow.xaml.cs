using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class OptimizationResultWindow : Window
{
    private readonly MainViewModel _viewModel;

    public OptimizationResultWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveCurrentDietCommand.ExecuteAsync(null);
    }
}

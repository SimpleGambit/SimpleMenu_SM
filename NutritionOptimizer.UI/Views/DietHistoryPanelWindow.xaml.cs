using System.Windows;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class DietHistoryPanelWindow : Window
{
    public DietHistoryPanelWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

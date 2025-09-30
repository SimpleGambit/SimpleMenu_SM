using System.Collections.Generic;
using System.Windows;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class FoodDetailWindow : Window
{
    public FoodDetailWindow(Food food, IReadOnlyList<NutrientTarget> targets)
    {
        InitializeComponent();
        DataContext = new FoodDetailViewModel(food, targets);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.Windows;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.Views;

public partial class FoodDetailWindow : Window
{
    public FoodDetailWindow(Food food)
    {
        InitializeComponent();
        DataContext = food;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

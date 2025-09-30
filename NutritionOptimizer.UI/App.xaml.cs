using Microsoft.Extensions.DependencyInjection;
using NutritionOptimizer.Domain;
using NutritionOptimizer.Infrastructure;
using NutritionOptimizer.UI.ViewModels;
using NutritionOptimizer.UI.Views;
using System;
using System.Windows;

namespace NutritionOptimizer.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var window = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFoodRepository>(_ => new CsvFoodRepository("data/foods.csv"));
        services.AddSingleton<ITargetsRepository>(_ => new CsvTargetsRepository("data/targets.csv"));
        services.AddSingleton<IPriceHistoryRepository>(_ => new CsvPriceHistoryRepository("data/prices.csv"));
        services.AddSingleton<ISavedDietRepository>(_ => new CsvSavedDietRepository("data/saved_diets.csv", "data/saved_diet_items.csv"));
        services.AddSingleton<IDietHistoryRepository>(_ => new CsvDietHistoryRepository("data/diet_history.csv"));
        services.AddSingleton<IFoodPreferenceRepository>(_ => new CsvFoodPreferenceRepository("data/food_preferences.csv"));
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>(provider =>
        {
            var window = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            };
            return window;
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
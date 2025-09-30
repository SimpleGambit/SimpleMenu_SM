using Microsoft.Extensions.DependencyInjection;
using NutritionOptimizer.Domain;
using NutritionOptimizer.Infrastructure;
using NutritionOptimizer.UI.ViewModels;
using NutritionOptimizer.UI.Views;
using System;
using System.IO;
using System.Windows;

namespace NutritionOptimizer.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "NutritionOptimizer",
        "data"
    );

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 사용자 데이터 폴더 초기화
        InitializeDataDirectory();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var window = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    private static void InitializeDataDirectory()
    {
        // 사용자 데이터 폴더 생성
        if (!Directory.Exists(DataDirectory))
        {
            Directory.CreateDirectory(DataDirectory);
        }

        // 기존 bin 폴더의 데이터 마이그레이션 (한 번만 실행)
        var migrationFlagFile = Path.Combine(DataDirectory, ".migrated");
        if (!File.Exists(migrationFlagFile))
        {
            var oldDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (Directory.Exists(oldDataDir))
            {
                var dataFiles = new[]
                {
                    "foods.csv",
                    "targets.csv",
                    "prices.csv",
                    "saved_diets.csv",
                    "saved_diet_items.csv",
                    "diet_history.csv",
                    "food_preferences.csv",
                    "nutrition_templates.csv",
                    "price_history.csv",
                    "template_price_history.csv",
                    "food_template_usage.csv",
                    "cooking_loss_rates.csv"
                };

                foreach (var file in dataFiles)
                {
                    var oldFile = Path.Combine(oldDataDir, file);
                    var newFile = Path.Combine(DataDirectory, file);

                    // bin 폴더의 파일을 새 위치로 복사
                    if (File.Exists(oldFile))
                    {
                        File.Copy(oldFile, newFile, overwrite: true);
                    }
                }

                // 마이그레이션 완료 플래그 파일 생성
                File.WriteAllText(migrationFlagFile, DateTime.Now.ToString());
            }
        }

        // 초기 데이터 파일들 복사 (파일이 없는 경우에만)
        var sourceDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        if (Directory.Exists(sourceDataDir))
        {
            var dataFiles = new[]
            {
                "foods.csv",
                "targets.csv",
                "prices.csv",
                "saved_diets.csv",
                "saved_diet_items.csv",
                "diet_history.csv",
                "food_preferences.csv",
                "nutrition_templates.csv",
                "price_history.csv",
                "template_price_history.csv",
                "food_template_usage.csv",
                "cooking_loss_rates.csv"
            };

            foreach (var file in dataFiles)
            {
                var sourceFile = Path.Combine(sourceDataDir, file);
                var destFile = Path.Combine(DataDirectory, file);

                // 목적지 파일이 없는 경우에만 복사
                if (File.Exists(sourceFile) && !File.Exists(destFile))
                {
                    File.Copy(sourceFile, destFile);
                }
            }
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFoodRepository>(_ => new CsvFoodRepository(Path.Combine(DataDirectory, "foods.csv")));
        services.AddSingleton<ITargetsRepository>(_ => new CsvTargetsRepository(Path.Combine(DataDirectory, "targets.csv")));
        services.AddSingleton<IPriceHistoryRepository>(_ => new CsvPriceHistoryRepository(Path.Combine(DataDirectory, "prices.csv")));
        services.AddSingleton<ISavedDietRepository>(_ => new CsvSavedDietRepository(
            Path.Combine(DataDirectory, "saved_diets.csv"),
            Path.Combine(DataDirectory, "saved_diet_items.csv")));
        services.AddSingleton<IDietHistoryRepository>(_ => new CsvDietHistoryRepository(Path.Combine(DataDirectory, "diet_history.csv")));
        services.AddSingleton<IFoodPreferenceRepository>(_ => new CsvFoodPreferenceRepository(Path.Combine(DataDirectory, "food_preferences.csv")));
        services.AddSingleton<INutritionTemplateRepository>(_ => new CsvNutritionTemplateRepository(Path.Combine(DataDirectory, "nutrition_templates.csv")));
        services.AddSingleton<ITemplatePriceHistoryRepository>(_ => new CsvTemplatePriceHistoryRepository(Path.Combine(DataDirectory, "template_price_history.csv")));
        services.AddSingleton<IFoodTemplateUsageRepository>(_ => new CsvFoodTemplateUsageRepository(Path.Combine(DataDirectory, "food_template_usage.csv")));
        services.AddSingleton<ICookingLossRateRepository>(_ => new CsvCookingLossRateRepository(Path.Combine(DataDirectory, "cooking_loss_rates.csv")));
        services.AddSingleton<IOptimizationSettingsRepository>(_ => new CsvOptimizationSettingsRepository(Path.Combine(DataDirectory, "optimization_settings.csv")));
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

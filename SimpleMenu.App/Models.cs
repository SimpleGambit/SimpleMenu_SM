using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleMenu.App;

public sealed class AppState
{
    public ObservableCollection<Ingredient> Ingredients { get; set; } = [];
    public ObservableCollection<Menu> Menus { get; set; } = [];
    public ObservableCollection<PlanItem> PlanItems { get; set; } = [];
    public ObservableCollection<WeeklyPlanGroup> WeeklyPlanGroups { get; set; } = [];
    public Guid? SelectedWeeklyPlanGroupId { get; set; }
    public NutritionGoal Goals { get; set; } = NutritionGoal.CreateDefault();
    public DateTime WeekStart { get; set; } = DateTime.Today.StartOfWeek();
}

public sealed class Ingredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Memo { get; set; } = "";
    public string BaseUnit { get; set; } = "g";
    public Dictionary<string, double> NutrientsPer100 { get; set; } = [];
    public ObservableCollection<IngredientPrice> Prices { get; set; } = [];
}

public sealed class IngredientPrice : INotifyPropertyChanged
{
    private DateTime _date = DateTime.Today;
    private double _totalAmount = 100;
    private string _unit = "g";
    private double _totalCount = 1;
    private double _cost;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime Date
    {
        get => _date;
        set => SetField(ref _date, value);
    }

    public double TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (SetField(ref _totalAmount, value))
            {
                OnPropertyChanged(nameof(PricePer100));
            }
        }
    }

    public string Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    public double TotalCount
    {
        get => _totalCount;
        set
        {
            if (SetField(ref _totalCount, value))
            {
                OnPropertyChanged(nameof(PricePer100));
            }
        }
    }

    public double Cost
    {
        get => _cost;
        set
        {
            if (SetField(ref _cost, value))
            {
                OnPropertyChanged(nameof(PricePer100));
            }
        }
    }

    public double PricePer100 => TotalAmount > 0 && TotalCount > 0
        ? Cost / (TotalAmount * TotalCount) * 100
        : 0;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class Menu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Recipe { get; set; } = "";
    public ObservableCollection<MenuIngredient> Ingredients { get; set; } = [];
}

public sealed class MenuIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IngredientId { get; set; }
    public double Amount { get; set; }
    public string Unit { get; set; } = "g";
}

public sealed class PlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public Guid MenuId { get; set; }
    public double Quantity { get; set; } = 1;
}

public sealed class WeeklyPlanGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public ObservableCollection<WeeklyPlanItem> Items { get; set; } = [];
}

public sealed class WeeklyPlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DayIndex { get; set; }
    public Guid MenuId { get; set; }
    public double Quantity { get; set; } = 1;
}

public sealed class NutritionGoal
{
    public Dictionary<string, double> DailyTargets { get; set; } = [];

    public static NutritionGoal CreateDefault()
    {
        return new NutritionGoal
        {
            DailyTargets = new Dictionary<string, double>
            {
                ["energy"] = 2000,
                ["carbohydrate"] = 300,
                ["protein"] = 60,
                ["fat"] = 50,
                ["saturatedFat"] = 15,
                ["fiber"] = 25,
                ["sugars"] = 50,
                ["sodium"] = 2000,
            }
        };
    }
}

public sealed record NutrientDefinition(string Key, string Name, string Unit, bool IsRequired);

public sealed class NutrientEditRow
{
    public string Key { get; init; } = "";
    public string Group { get; init; } = "";
    public string Name { get; init; } = "";
    public double Value { get; set; }
    public string Unit { get; init; } = "";
}

public sealed class GoalRow
{
    public string Key { get; init; } = "";
    public string Group { get; init; } = "";
    public string Name { get; init; } = "";
    public double DailyTarget { get; set; }
    public string Unit { get; init; } = "";
}

public sealed class IngredientRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string BaseUnit { get; init; } = "";
    public string LatestPriceDate { get; init; } = "";
    public string PricePer100 { get; init; } = "";
}

public sealed class MenuRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public int IngredientCount { get; init; }
    public string Price { get; init; } = "";
    public string Energy { get; init; } = "";
    public string Protein { get; init; } = "";
}

public sealed class MenuIngredientRow
{
    public Guid Id { get; init; }
    public Guid IngredientId { get; init; }
    public string IngredientName { get; init; } = "";
    public double Amount { get; set; }
    public string Unit { get; init; } = "";
    public string Price { get; init; } = "";
    public string Energy { get; init; } = "";
}

public sealed class NutrientSummaryRow
{
    public string Name { get; init; } = "";
    public string Amount { get; init; } = "";
    public string Unit { get; init; } = "";
}

public sealed class DayOption
{
    public int DayIndex { get; init; }
    public string Label { get; init; } = "";
}

public sealed class PlanRow
{
    public Guid Id { get; init; }
    public int DayIndex { get; init; }
    public string DayLabel { get; init; } = "";
    public string MenuName { get; init; } = "";
    public double Quantity { get; set; }
    public string Price { get; init; } = "";
    public string Energy { get; init; } = "";
    public string Carbohydrate { get; init; } = "";
    public string Protein { get; init; } = "";
    public string Fat { get; init; } = "";
    public string SaturatedFat { get; init; } = "";
    public string Fiber { get; init; } = "";
    public string Sugars { get; init; } = "";
    public string Sodium { get; init; } = "";
}

public sealed class DaySummaryRow
{
    public int DayIndex { get; init; }
    public string DayLabel { get; init; } = "";
    public int MenuCount { get; init; }
    public string Price { get; init; } = "";
    public string Energy { get; init; } = "";
    public string Carbohydrate { get; init; } = "";
    public string Protein { get; init; } = "";
    public string Fat { get; init; } = "";
    public string SaturatedFat { get; init; } = "";
    public string Fiber { get; init; } = "";
    public string Sugars { get; init; } = "";
    public string Sodium { get; init; } = "";
}

public sealed class WeeklyGoalRow
{
    public string Name { get; init; } = "";
    public string Goal { get; init; } = "";
    public string Average { get; init; } = "";
    public string Rate { get; init; } = "";
    public string Unit { get; init; } = "";
}

public static class DateExtensions
{
    public static DateTime StartOfWeek(this DateTime date)
    {
        var normalized = date.Date;
        var diff = (7 + (normalized.DayOfWeek - DayOfWeek.Monday)) % 7;
        return normalized.AddDays(-diff);
    }
}

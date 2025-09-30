using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SimpleMenu.App;

public static class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleMenu");

    public static string FilePath => Path.Combine(DirectoryPath, "simplemenu-data.json");

    public static AppState Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppState();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var state = JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
            Normalize(state);
            return state;
        }
        catch
        {
            return new AppState();
        }
    }

    public static void Save(AppState state)
    {
        Normalize(state);
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    private static void Normalize(AppState state)
    {
        state.WeekStart = state.WeekStart == default
            ? DateTime.Today.StartOfWeek()
            : state.WeekStart.Date.StartOfWeek();

        foreach (var ingredient in state.Ingredients)
        {
            if (ingredient.Id == Guid.Empty)
            {
                ingredient.Id = Guid.NewGuid();
            }

            ingredient.BaseUnit = ingredient.BaseUnit is "g" or "ml" ? ingredient.BaseUnit : "g";
            ingredient.Memo ??= "";
            ingredient.NutrientsPer100 ??= [];
            ingredient.Prices ??= [];

            foreach (var price in ingredient.Prices)
            {
                price.Date = price.Date == default ? DateTime.Today : price.Date.Date;
                price.Unit = price.Unit is "g" or "ml" ? price.Unit : ingredient.BaseUnit;
                price.TotalAmount = Math.Max(0, price.TotalAmount);
                price.TotalCount = Math.Max(0, price.TotalCount);
                price.Cost = Math.Max(0, price.Cost);
            }
        }

        foreach (var menu in state.Menus)
        {
            if (menu.Id == Guid.Empty)
            {
                menu.Id = Guid.NewGuid();
            }

            menu.Ingredients ??= [];
            foreach (var menuIngredient in menu.Ingredients)
            {
                if (menuIngredient.Id == Guid.Empty)
                {
                    menuIngredient.Id = Guid.NewGuid();
                }

                menuIngredient.Unit = menuIngredient.Unit is "g" or "ml" ? menuIngredient.Unit : "g";
                menuIngredient.Amount = Math.Max(0, menuIngredient.Amount);
            }
        }

        foreach (var item in state.PlanItems)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            item.Date = item.Date == default ? DateTime.Today.Date : item.Date.Date;
            item.Quantity = item.Quantity <= 0 ? 1 : item.Quantity;
        }

        state.WeeklyPlanGroups ??= [];
        if (state.WeeklyPlanGroups.Count == 0)
        {
            var defaultGroup = new WeeklyPlanGroup
            {
                Name = "기본 식단"
            };

            foreach (var legacyItem in state.PlanItems)
            {
                defaultGroup.Items.Add(new WeeklyPlanItem
                {
                    DayIndex = DayIndexFromDate(legacyItem.Date),
                    MenuId = legacyItem.MenuId,
                    Quantity = legacyItem.Quantity <= 0 ? 1 : legacyItem.Quantity
                });
            }

            state.WeeklyPlanGroups.Add(defaultGroup);
            state.SelectedWeeklyPlanGroupId = defaultGroup.Id;
        }

        foreach (var group in state.WeeklyPlanGroups)
        {
            if (group.Id == Guid.Empty)
            {
                group.Id = Guid.NewGuid();
            }

            group.Name = string.IsNullOrWhiteSpace(group.Name) ? "식단 그룹" : group.Name.Trim();
            group.Items ??= [];

            foreach (var item in group.Items)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                item.DayIndex = Math.Clamp(item.DayIndex, 0, 6);
                item.Quantity = item.Quantity <= 0 ? 1 : item.Quantity;
            }
        }

        if (state.SelectedWeeklyPlanGroupId is null
            || state.WeeklyPlanGroups.All(group => group.Id != state.SelectedWeeklyPlanGroupId))
        {
            state.SelectedWeeklyPlanGroupId = state.WeeklyPlanGroups[0].Id;
        }

        state.Goals ??= NutritionGoal.CreateDefault();
        state.Goals.DailyTargets ??= [];
        foreach (var target in NutritionGoal.CreateDefault().DailyTargets)
        {
            state.Goals.DailyTargets.TryAdd(target.Key, target.Value);
        }
    }

    private static int DayIndexFromDate(DateTime date)
    {
        return ((int)date.DayOfWeek + 6) % 7;
    }
}

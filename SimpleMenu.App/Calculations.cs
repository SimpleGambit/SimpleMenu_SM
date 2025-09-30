namespace SimpleMenu.App;

public static class Calculations
{
    public static IngredientPrice? LatestPrice(Ingredient ingredient)
    {
        return ingredient.Prices
            .Where(price => price.TotalAmount > 0 && price.Unit == ingredient.BaseUnit)
            .OrderByDescending(price => price.Date.Date)
            .FirstOrDefault();
    }

    public static double PricePer100(Ingredient ingredient)
    {
        return LatestPrice(ingredient)?.PricePer100 ?? 0;
    }

    public static double IngredientCost(Ingredient ingredient, double amount)
    {
        return PricePer100(ingredient) * amount / 100;
    }

    public static Dictionary<string, double> IngredientNutrients(Ingredient ingredient, double amount)
    {
        var factor = amount / 100;
        return NutritionCatalog.All.ToDictionary(
            nutrient => nutrient.Key,
            nutrient => ingredient.NutrientsPer100.GetValueOrDefault(nutrient.Key) * factor);
    }

    public static Dictionary<string, double> MenuNutrients(Menu menu, IEnumerable<Ingredient> ingredients)
    {
        var ingredientMap = ingredients.ToDictionary(ingredient => ingredient.Id);
        var totals = EmptyNutrients();

        foreach (var menuIngredient in menu.Ingredients)
        {
            if (!ingredientMap.TryGetValue(menuIngredient.IngredientId, out var ingredient))
            {
                continue;
            }

            var nutrients = IngredientNutrients(ingredient, menuIngredient.Amount);
            foreach (var nutrient in NutritionCatalog.All)
            {
                totals[nutrient.Key] += nutrients[nutrient.Key];
            }
        }

        return totals;
    }

    public static double MenuCost(Menu menu, IEnumerable<Ingredient> ingredients)
    {
        var ingredientMap = ingredients.ToDictionary(ingredient => ingredient.Id);
        var total = 0.0;

        foreach (var menuIngredient in menu.Ingredients)
        {
            if (ingredientMap.TryGetValue(menuIngredient.IngredientId, out var ingredient))
            {
                total += IngredientCost(ingredient, menuIngredient.Amount);
            }
        }

        return total;
    }

    public static Dictionary<string, double> DayNutrients(
        DateTime date,
        IEnumerable<PlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var menuMap = menus.ToDictionary(menu => menu.Id);
        var totals = EmptyNutrients();

        foreach (var item in planItems.Where(item => item.Date.Date == date.Date))
        {
            if (!menuMap.TryGetValue(item.MenuId, out var menu))
            {
                continue;
            }

            var menuNutrients = MenuNutrients(menu, ingredients);
            foreach (var nutrient in NutritionCatalog.All)
            {
                totals[nutrient.Key] += menuNutrients[nutrient.Key] * item.Quantity;
            }
        }

        return totals;
    }

    public static double DayCost(
        DateTime date,
        IEnumerable<PlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var menuMap = menus.ToDictionary(menu => menu.Id);
        var total = 0.0;

        foreach (var item in planItems.Where(item => item.Date.Date == date.Date))
        {
            if (menuMap.TryGetValue(item.MenuId, out var menu))
            {
                total += MenuCost(menu, ingredients) * item.Quantity;
            }
        }

        return total;
    }

    public static Dictionary<string, double> DayNutrients(
        int dayIndex,
        IEnumerable<WeeklyPlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var menuMap = menus.ToDictionary(menu => menu.Id);
        var totals = EmptyNutrients();

        foreach (var item in planItems.Where(item => item.DayIndex == dayIndex))
        {
            if (!menuMap.TryGetValue(item.MenuId, out var menu))
            {
                continue;
            }

            var menuNutrients = MenuNutrients(menu, ingredients);
            foreach (var nutrient in NutritionCatalog.All)
            {
                totals[nutrient.Key] += menuNutrients[nutrient.Key] * item.Quantity;
            }
        }

        return totals;
    }

    public static double DayCost(
        int dayIndex,
        IEnumerable<WeeklyPlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var menuMap = menus.ToDictionary(menu => menu.Id);
        var total = 0.0;

        foreach (var item in planItems.Where(item => item.DayIndex == dayIndex))
        {
            if (menuMap.TryGetValue(item.MenuId, out var menu))
            {
                total += MenuCost(menu, ingredients) * item.Quantity;
            }
        }

        return total;
    }

    public static Dictionary<string, double> WeekNutrients(
        DateTime weekStart,
        IEnumerable<PlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var totals = EmptyNutrients();
        for (var offset = 0; offset < 7; offset++)
        {
            var daily = DayNutrients(weekStart.AddDays(offset), planItems, menus, ingredients);
            foreach (var nutrient in NutritionCatalog.All)
            {
                totals[nutrient.Key] += daily[nutrient.Key];
            }
        }

        return totals;
    }

    public static double WeekCost(
        DateTime weekStart,
        IEnumerable<PlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var total = 0.0;
        for (var offset = 0; offset < 7; offset++)
        {
            total += DayCost(weekStart.AddDays(offset), planItems, menus, ingredients);
        }

        return total;
    }

    public static Dictionary<string, double> WeekNutrients(
        IEnumerable<WeeklyPlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var totals = EmptyNutrients();
        for (var dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            var daily = DayNutrients(dayIndex, planItems, menus, ingredients);
            foreach (var nutrient in NutritionCatalog.All)
            {
                totals[nutrient.Key] += daily[nutrient.Key];
            }
        }

        return totals;
    }

    public static double WeekCost(
        IEnumerable<WeeklyPlanItem> planItems,
        IEnumerable<Menu> menus,
        IEnumerable<Ingredient> ingredients)
    {
        var total = 0.0;
        for (var dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            total += DayCost(dayIndex, planItems, menus, ingredients);
        }

        return total;
    }

    public static Dictionary<string, double> EmptyNutrients()
    {
        return NutritionCatalog.All.ToDictionary(nutrient => nutrient.Key, _ => 0.0);
    }
}

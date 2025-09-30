using SimpleMenu.App;

var rice = new Ingredient
{
    Name = "밥",
    BaseUnit = "g",
    NutrientsPer100 =
    {
        ["energy"] = 130,
        ["carbohydrate"] = 28,
        ["protein"] = 2.7,
        ["fat"] = 0.3,
        ["sodium"] = 1
    },
    Prices =
    {
        new IngredientPrice
        {
            Date = new DateTime(2026, 1, 1),
            TotalAmount = 1000,
            Unit = "g",
            TotalCount = 1,
            Cost = 5000
        },
        new IngredientPrice
        {
            Date = new DateTime(2026, 2, 1),
            TotalAmount = 1000,
            Unit = "g",
            TotalCount = 1,
            Cost = 6000
        }
    }
};

var chicken = new Ingredient
{
    Name = "닭가슴살",
    BaseUnit = "g",
    NutrientsPer100 =
    {
        ["energy"] = 110,
        ["protein"] = 23,
        ["fat"] = 1.5,
        ["sodium"] = 45
    },
    Prices =
    {
        new IngredientPrice
        {
            Date = new DateTime(2026, 1, 1),
            TotalAmount = 1000,
            Unit = "g",
            TotalCount = 4,
            Cost = 12000
        }
    }
};

var menu = new Menu
{
    Name = "닭가슴살 덮밥",
    Ingredients =
    {
        new MenuIngredient { IngredientId = rice.Id, Amount = 200, Unit = "g" },
        new MenuIngredient { IngredientId = chicken.Id, Amount = 100, Unit = "g" }
    }
};

var planItems = new[]
{
    new WeeklyPlanItem { DayIndex = 0, MenuId = menu.Id, Quantity = 2 }
};

var ingredients = new[] { rice, chicken };
var menus = new[] { menu };

AssertClose("latest price per 100g", 600, Calculations.PricePer100(rice));
AssertClose("chicken price per 100g with count", 300, Calculations.PricePer100(chicken));
AssertClose("menu cost", 1500, Calculations.MenuCost(menu, ingredients));

var menuNutrients = Calculations.MenuNutrients(menu, ingredients);
AssertClose("menu energy", 370, menuNutrients["energy"]);
AssertClose("menu carbohydrate", 56, menuNutrients["carbohydrate"]);
AssertClose("menu protein", 28.4, menuNutrients["protein"]);

var weekNutrients = Calculations.WeekNutrients(planItems, menus, ingredients);
AssertClose("weekly total energy", 740, weekNutrients["energy"]);
AssertClose("weekly average energy", 740.0 / 7, weekNutrients["energy"] / 7);
AssertClose("weekday total cost", 3000, Calculations.DayCost(0, planItems, menus, ingredients));
AssertClose("weekly total cost", 3000, Calculations.WeekCost(planItems, menus, ingredients));

AssertParsed("decimal point", "12.5", 12.5);
AssertParsed("decimal comma", "12,5", 12.5);
AssertParsed("thousands comma with decimal", "1,234.5", 1234.5);
AssertParsed("thousands comma", "1,000", 1000);

var watchedPrice = new IngredientPrice
{
    TotalAmount = 250,
    Cost = 1000
};
var changedProperties = new List<string?>();
watchedPrice.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
watchedPrice.Cost = 1250;
AssertClose("automatic price per 100g after cost change", 500, watchedPrice.PricePer100);
AssertContains("price change notification after cost change", nameof(IngredientPrice.PricePer100), changedProperties);

changedProperties.Clear();
watchedPrice.TotalAmount = 500;
AssertClose("automatic price per 100g after amount change", 250, watchedPrice.PricePer100);
AssertContains("price change notification after amount change", nameof(IngredientPrice.PricePer100), changedProperties);

changedProperties.Clear();
watchedPrice.TotalCount = 2;
AssertClose("automatic price per 100g after count change", 125, watchedPrice.PricePer100);
AssertContains("price change notification after count change", nameof(IngredientPrice.PricePer100), changedProperties);

Console.WriteLine("Calculation checks passed.");

static void AssertClose(string name, double expected, double actual)
{
    if (Math.Abs(expected - actual) > 0.0001)
    {
        throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}");
    }
}

static void AssertParsed(string name, string text, double expected)
{
    if (!NumberInput.TryParseDecimal(text, out var actual))
    {
        throw new InvalidOperationException($"{name}: failed to parse {text}");
    }

    AssertClose(name, expected, actual);
}

static void AssertContains(string name, string expected, IEnumerable<string?> actual)
{
    if (!actual.Contains(expected))
    {
        throw new InvalidOperationException($"{name}: expected notification {expected}");
    }
}

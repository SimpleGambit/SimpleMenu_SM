using System.Windows;
using System.Windows.Controls;

namespace SimpleMenu.App;

public partial class MainWindow : Window
{
    private static readonly string[] DayNames = ["월요일", "화요일", "수요일", "목요일", "금요일", "토요일", "일요일"];

    private readonly AppState _state;
    private Ingredient? _selectedIngredient;
    private Menu? _selectedMenu;
    private List<NutrientEditRow> _ingredientNutrientRows = [];
    private List<GoalRow> _goalRows = [];
    private int _selectedPlanDayIndex;
    private bool _isRefreshing;

    public MainWindow()
    {
        InitializeComponent();

        _state = DataStore.Load();
        StoragePathText.Text = $"저장 위치: {DataStore.FilePath}";
        RefreshAll();
        SetStatus("준비됨");
    }

    private void RefreshAll()
    {
        _isRefreshing = true;
        RefreshIngredientList();
        RefreshIngredientEditor();
        RefreshMenuList();
        RefreshMenuEditor();
        RefreshPickers();
        RefreshGoals();
        RefreshWeek();
        _isRefreshing = false;
    }

    private void RefreshIngredientList()
    {
        if (_selectedIngredient is null && _state.Ingredients.Count > 0)
        {
            _selectedIngredient = _state.Ingredients[0];
        }

        var rows = _state.Ingredients
            .OrderBy(ingredient => ingredient.Name)
            .Select(ingredient =>
            {
                var latest = Calculations.LatestPrice(ingredient);
                return new IngredientRow
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name,
                    BaseUnit = ingredient.BaseUnit,
                    LatestPriceDate = latest is null ? "-" : latest.Date.ToString("yyyy-MM-dd"),
                    PricePer100 = $"{Calculations.PricePer100(ingredient):N0}원"
                };
            })
            .ToList();

        IngredientsGrid.ItemsSource = rows;
        IngredientsGrid.SelectedItem = rows.FirstOrDefault(row => row.Id == _selectedIngredient?.Id);
    }

    private void RefreshIngredientEditor()
    {
        if (_selectedIngredient is null)
        {
            IngredientNameBox.Text = "";
            IngredientMemoBox.Text = "";
            SetUnitCombo(IngredientUnitBox, "g");
            IngredientNutrientsGrid.ItemsSource = null;
            IngredientPricesGrid.ItemsSource = null;
            return;
        }

        IngredientNameBox.Text = _selectedIngredient.Name;
        IngredientMemoBox.Text = _selectedIngredient.Memo;
        SetUnitCombo(IngredientUnitBox, _selectedIngredient.BaseUnit);

        _ingredientNutrientRows = NutritionCatalog.All
            .Select(nutrient => new NutrientEditRow
            {
                Key = nutrient.Key,
                Group = nutrient.IsRequired ? "필수" : "상세",
                Name = nutrient.Name,
                Value = _selectedIngredient.NutrientsPer100.GetValueOrDefault(nutrient.Key),
                Unit = nutrient.Unit
            })
            .ToList();

        IngredientNutrientsGrid.ItemsSource = _ingredientNutrientRows;
        IngredientPricesGrid.ItemsSource = _selectedIngredient.Prices;
    }

    private void RefreshMenuList()
    {
        if (_selectedMenu is null && _state.Menus.Count > 0)
        {
            _selectedMenu = _state.Menus[0];
        }

        var rows = _state.Menus
            .OrderBy(menu => menu.Name)
            .Select(menu =>
            {
                var nutrients = Calculations.MenuNutrients(menu, _state.Ingredients);
                return new MenuRow
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    IngredientCount = menu.Ingredients.Count,
                    Price = FormatMoney(Calculations.MenuCost(menu, _state.Ingredients)),
                    Energy = FormatNumber(nutrients["energy"]),
                    Protein = FormatNumber(nutrients["protein"])
                };
            })
            .ToList();

        MenusGrid.ItemsSource = rows;
        MenusGrid.SelectedItem = rows.FirstOrDefault(row => row.Id == _selectedMenu?.Id);
    }

    private void RefreshMenuEditor()
    {
        if (_selectedMenu is null)
        {
            MenuNameBox.Text = "";
            RecipeBox.Text = "";
            MenuPriceText.Text = "";
            MenuIngredientsGrid.ItemsSource = null;
            MenuRequiredSummaryGrid.ItemsSource = null;
            MenuDetailSummaryGrid.ItemsSource = null;
            return;
        }

        MenuNameBox.Text = _selectedMenu.Name;
        RecipeBox.Text = _selectedMenu.Recipe;

        var ingredientMap = _state.Ingredients.ToDictionary(ingredient => ingredient.Id);
        var ingredientRows = _selectedMenu.Ingredients.Select(menuIngredient =>
        {
            ingredientMap.TryGetValue(menuIngredient.IngredientId, out var ingredient);
            var nutrients = ingredient is null
                ? Calculations.EmptyNutrients()
                : Calculations.IngredientNutrients(ingredient, menuIngredient.Amount);

            return new MenuIngredientRow
            {
                Id = menuIngredient.Id,
                IngredientId = menuIngredient.IngredientId,
                IngredientName = ingredient?.Name ?? "(삭제된 재료)",
                Amount = menuIngredient.Amount,
                Unit = menuIngredient.Unit,
                Price = ingredient is null ? "0원" : FormatMoney(Calculations.IngredientCost(ingredient, menuIngredient.Amount)),
                Energy = FormatWithUnit(nutrients["energy"], "kcal")
            };
        }).ToList();

        MenuIngredientsGrid.ItemsSource = ingredientRows;

        var menuNutrients = Calculations.MenuNutrients(_selectedMenu, _state.Ingredients);
        MenuPriceText.Text = $"1인분 예상 비용: {FormatMoney(Calculations.MenuCost(_selectedMenu, _state.Ingredients))}";
        MenuRequiredSummaryGrid.ItemsSource = BuildNutrientRows(menuNutrients, onlyRequired: true);
        MenuDetailSummaryGrid.ItemsSource = BuildNutrientRows(menuNutrients, onlyRequired: false);
    }

    private void RefreshPickers()
    {
        var ingredients = _state.Ingredients.OrderBy(ingredient => ingredient.Name).ToList();
        MenuIngredientPicker.ItemsSource = ingredients;
        if (ingredients.Count > 0 && MenuIngredientPicker.SelectedValue is null)
        {
            MenuIngredientPicker.SelectedValue = ingredients[0].Id;
        }
        UpdateMenuIngredientUnitFromSelection();

        var menus = _state.Menus.OrderBy(menu => menu.Name).ToList();
        PlanMenuPicker.ItemsSource = menus;
        if (menus.Count > 0 && PlanMenuPicker.SelectedValue is null)
        {
            PlanMenuPicker.SelectedValue = menus[0].Id;
        }
    }

    private void RefreshGoals()
    {
        _goalRows = NutritionCatalog.All
            .Select(nutrient => new GoalRow
            {
                Key = nutrient.Key,
                Group = nutrient.IsRequired ? "필수" : "상세",
                Name = nutrient.Name,
                DailyTarget = _state.Goals.DailyTargets.GetValueOrDefault(nutrient.Key),
                Unit = nutrient.Unit
            })
            .ToList();

        GoalsGrid.ItemsSource = _goalRows;
    }

    private void RefreshWeek()
    {
        var group = CurrentPlanGroup();
        var groups = _state.WeeklyPlanGroups.OrderBy(planGroup => planGroup.Name).ToList();
        PlanGroupPicker.ItemsSource = groups;
        PlanGroupPicker.SelectedValue = group?.Id;
        PlanGroupNameBox.Text = group?.Name ?? "";
        WeekTotalCostText.Text = group is null
            ? "식단 총 비용: 0원"
            : $"식단 총 비용: {FormatMoney(Calculations.WeekCost(group.Items, _state.Menus, _state.Ingredients))}";

        var days = Enumerable.Range(0, 7)
            .Select(dayIndex => new DayOption
            {
                DayIndex = dayIndex,
                Label = DayNames[dayIndex]
            })
            .ToList();

        _selectedPlanDayIndex = Math.Clamp(_selectedPlanDayIndex, 0, 6);
        PlanDisplayDayPicker.ItemsSource = days;
        PlanDisplayDayPicker.SelectedValue = _selectedPlanDayIndex;

        var menuMap = _state.Menus.ToDictionary(menu => menu.Id);
        var planRows = (group?.Items ?? [])
            .Where(item => item.DayIndex == _selectedPlanDayIndex)
            .OrderBy(item => item.DayIndex)
            .ThenBy(item => menuMap.TryGetValue(item.MenuId, out var menu) ? menu.Name : "")
            .Select(item =>
            {
                menuMap.TryGetValue(item.MenuId, out var menu);
                var nutrients = menu is null
                    ? Calculations.EmptyNutrients()
                    : Calculations.MenuNutrients(menu, _state.Ingredients);

                return new PlanRow
                {
                    Id = item.Id,
                    DayIndex = item.DayIndex,
                    DayLabel = FormatDay(item.DayIndex),
                    MenuName = menu?.Name ?? "(삭제된 메뉴)",
                    Quantity = item.Quantity,
                    Price = menu is null ? "0원" : FormatMoney(Calculations.MenuCost(menu, _state.Ingredients) * item.Quantity),
                    Energy = FormatWithUnit(nutrients["energy"] * item.Quantity, "kcal"),
                    Carbohydrate = FormatWithUnit(nutrients["carbohydrate"] * item.Quantity, "g"),
                    Protein = FormatWithUnit(nutrients["protein"] * item.Quantity, "g"),
                    Fat = FormatWithUnit(nutrients["fat"] * item.Quantity, "g"),
                    SaturatedFat = FormatWithUnit(nutrients["saturatedFat"] * item.Quantity, "g"),
                    Fiber = FormatWithUnit(nutrients["fiber"] * item.Quantity, "g"),
                    Sugars = FormatWithUnit(nutrients["sugars"] * item.Quantity, "g"),
                    Sodium = FormatWithUnit(nutrients["sodium"] * item.Quantity, "mg")
                };
            })
            .ToList();

        PlanItemsGrid.ItemsSource = planRows;
        DailySummaryGrid.ItemsSource = BuildDailySummaryRows(group);
        WeeklyGoalsGrid.ItemsSource = BuildWeeklyGoalRows(group);
    }

    private List<NutrientSummaryRow> BuildNutrientRows(Dictionary<string, double> nutrients, bool onlyRequired)
    {
        return NutritionCatalog.All
            .Where(nutrient => onlyRequired ? nutrient.IsRequired : !nutrient.IsRequired)
            .Select(nutrient => new NutrientSummaryRow
            {
                Name = nutrient.Name,
                Amount = FormatNumber(nutrients.GetValueOrDefault(nutrient.Key)),
                Unit = nutrient.Unit
            })
            .ToList();
    }

    private List<DaySummaryRow> BuildDailySummaryRows(WeeklyPlanGroup? group)
    {
        var rows = new List<DaySummaryRow>();
        var items = group?.Items ?? [];

        for (var dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            var nutrients = Calculations.DayNutrients(dayIndex, items, _state.Menus, _state.Ingredients);
            var menuCount = items.Count(item => item.DayIndex == dayIndex);

            rows.Add(new DaySummaryRow
            {
                DayIndex = dayIndex,
                DayLabel = FormatDay(dayIndex),
                MenuCount = menuCount,
                Price = FormatMoney(Calculations.DayCost(dayIndex, items, _state.Menus, _state.Ingredients)),
                Energy = FormatWithUnit(nutrients["energy"], "kcal"),
                Carbohydrate = FormatWithUnit(nutrients["carbohydrate"], "g"),
                Protein = FormatWithUnit(nutrients["protein"], "g"),
                Fat = FormatWithUnit(nutrients["fat"], "g"),
                SaturatedFat = FormatWithUnit(nutrients["saturatedFat"], "g"),
                Fiber = FormatWithUnit(nutrients["fiber"], "g"),
                Sugars = FormatWithUnit(nutrients["sugars"], "g"),
                Sodium = FormatWithUnit(nutrients["sodium"], "mg")
            });
        }

        return rows;
    }

    private List<WeeklyGoalRow> BuildWeeklyGoalRows(WeeklyPlanGroup? group)
    {
        var nutrients = Calculations.WeekNutrients(group?.Items ?? [], _state.Menus, _state.Ingredients);

        return NutritionCatalog.All
            .Where(nutrient => nutrient.IsRequired || _state.Goals.DailyTargets.GetValueOrDefault(nutrient.Key) > 0)
            .Select(nutrient =>
            {
                var average = nutrients.GetValueOrDefault(nutrient.Key) / 7;
                var goal = _state.Goals.DailyTargets.GetValueOrDefault(nutrient.Key);
                var rate = goal > 0 ? $"{average / goal * 100:N0}%" : "-";

                return new WeeklyGoalRow
                {
                    Name = nutrient.Name,
                    Goal = FormatNumber(goal),
                    Average = FormatNumber(average),
                    Rate = rate,
                    Unit = nutrient.Unit
                };
            })
            .ToList();
    }

    private void IngredientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || IngredientsGrid.SelectedItem is not IngredientRow row)
        {
            return;
        }

        _selectedIngredient = _state.Ingredients.FirstOrDefault(ingredient => ingredient.Id == row.Id);
        RefreshIngredientEditor();
    }

    private void NewIngredient_Click(object sender, RoutedEventArgs e)
    {
        var ingredient = new Ingredient
        {
            Name = UniqueName("새 재료", _state.Ingredients.Select(item => item.Name)),
            BaseUnit = "g"
        };

        foreach (var nutrient in NutritionCatalog.All)
        {
            ingredient.NutrientsPer100[nutrient.Key] = 0;
        }

        _state.Ingredients.Add(ingredient);
        _selectedIngredient = ingredient;
        SaveAndRefresh("새 재료를 추가했습니다.");
    }

    private void IngredientUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || _selectedIngredient is null)
        {
            return;
        }

        SyncIngredientPriceUnits(SelectedUnit(IngredientUnitBox));
    }

    private void SaveIngredient_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(IngredientNutrientsGrid);
        CommitGrid(IngredientPricesGrid);

        if (_selectedIngredient is null)
        {
            _selectedIngredient = new Ingredient();
            _state.Ingredients.Add(_selectedIngredient);
        }

        var name = IngredientNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("재료명을 입력하세요.");
            return;
        }

        _selectedIngredient.Name = name;
        _selectedIngredient.Memo = IngredientMemoBox.Text.Trim();
        _selectedIngredient.BaseUnit = SelectedUnit(IngredientUnitBox);
        SyncIngredientPriceUnits(_selectedIngredient.BaseUnit);
        _selectedIngredient.NutrientsPer100 = _ingredientNutrientRows.ToDictionary(row => row.Key, row => Math.Max(0, row.Value));

        foreach (var price in _selectedIngredient.Prices)
        {
            if (string.IsNullOrWhiteSpace(price.Unit))
            {
                price.Unit = _selectedIngredient.BaseUnit;
            }
        }

        SaveAndRefresh("재료 정보를 저장했습니다.");
    }

    private void SyncIngredientPriceUnits(string unit)
    {
        if (_selectedIngredient is null)
        {
            return;
        }

        _selectedIngredient.BaseUnit = unit;
        foreach (var price in _selectedIngredient.Prices)
        {
            price.Unit = unit;
        }

        IngredientPricesGrid.Items.Refresh();
    }

    private void DeleteIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIngredient is null)
        {
            return;
        }

        var usedMenus = _state.Menus
            .Where(menu => menu.Ingredients.Any(menuIngredient => menuIngredient.IngredientId == _selectedIngredient.Id))
            .Select(menu => menu.Name)
            .ToList();

        if (usedMenus.Count > 0)
        {
            ShowWarning($"이 재료를 사용하는 메뉴가 있습니다.\n먼저 메뉴에서 재료를 제거하세요.\n\n{string.Join(", ", usedMenus)}");
            return;
        }

        if (MessageBox.Show("선택한 재료를 삭제할까요?", "재료 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _state.Ingredients.Remove(_selectedIngredient);
        _selectedIngredient = _state.Ingredients.FirstOrDefault();
        SaveAndRefresh("재료를 삭제했습니다.");
    }

    private void AddPrice_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIngredient is null)
        {
            ShowWarning("가격을 추가할 재료를 선택하세요.");
            return;
        }

        CommitGrid(IngredientPricesGrid);
        _selectedIngredient.Prices.Add(new IngredientPrice
        {
            Date = DateTime.Today,
            Unit = _selectedIngredient.BaseUnit,
            TotalAmount = 100,
            TotalCount = 1,
            Cost = 0
        });

        IngredientPricesGrid.Items.Refresh();
        SetStatus("가격 이력 행을 추가했습니다. 값을 입력한 뒤 저장하세요.");
    }

    private void RemovePrice_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIngredient is null || IngredientPricesGrid.SelectedItem is not IngredientPrice price)
        {
            return;
        }

        _selectedIngredient.Prices.Remove(price);
        SaveAndRefresh("가격 이력을 삭제했습니다.");
    }

    private void MenusGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || MenusGrid.SelectedItem is not MenuRow row)
        {
            return;
        }

        _selectedMenu = _state.Menus.FirstOrDefault(menu => menu.Id == row.Id);
        RefreshMenuEditor();
    }

    private void NewMenu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new Menu
        {
            Name = UniqueName("새 메뉴", _state.Menus.Select(item => item.Name))
        };

        _state.Menus.Add(menu);
        _selectedMenu = menu;
        SaveAndRefresh("새 메뉴를 추가했습니다.");
    }

    private void SaveMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMenu is null)
        {
            _selectedMenu = new Menu();
            _state.Menus.Add(_selectedMenu);
        }

        var name = MenuNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("메뉴명을 입력하세요.");
            return;
        }

        CommitMenuEditorDraft();
        SaveAndRefresh("메뉴를 저장했습니다.");
    }

    private void DeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMenu is null)
        {
            return;
        }

        var usedCount = _state.WeeklyPlanGroups.Sum(group => group.Items.Count(item => item.MenuId == _selectedMenu.Id));
        if (usedCount > 0)
        {
            ShowWarning($"이 메뉴가 식단에 {usedCount}개 들어 있습니다.\n먼저 주간 식단에서 제거하세요.");
            return;
        }

        if (MessageBox.Show("선택한 메뉴를 삭제할까요?", "메뉴 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _state.Menus.Remove(_selectedMenu);
        _selectedMenu = _state.Menus.FirstOrDefault();
        SaveAndRefresh("메뉴를 삭제했습니다.");
    }

    private void MenuIngredientPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateMenuIngredientUnitFromSelection();
    }

    private void UpdateMenuIngredientUnitFromSelection()
    {
        if (MenuIngredientPicker.SelectedValue is Guid ingredientId)
        {
            var ingredient = _state.Ingredients.FirstOrDefault(item => item.Id == ingredientId);
            if (ingredient is not null)
            {
                SetUnitCombo(MenuIngredientUnitBox, ingredient.BaseUnit);
                return;
            }
        }

        SetUnitCombo(MenuIngredientUnitBox, "g");
    }

    private void AddMenuIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMenu is null)
        {
            ShowWarning("재료를 추가할 메뉴를 선택하세요.");
            return;
        }

        if (MenuIngredientPicker.SelectedValue is not Guid ingredientId)
        {
            ShowWarning("추가할 재료를 선택하세요.");
            return;
        }

        var ingredient = _state.Ingredients.FirstOrDefault(item => item.Id == ingredientId);
        if (ingredient is null)
        {
            ShowWarning("선택한 재료를 찾을 수 없습니다.");
            return;
        }

        if (!TryReadPositive(MenuIngredientAmountBox.Text, out var amount))
        {
            ShowWarning("사용량은 0보다 큰 숫자로 입력하세요.");
            return;
        }

        var unit = SelectedUnit(MenuIngredientUnitBox);
        if (unit != ingredient.BaseUnit)
        {
            ShowWarning($"이 재료의 기준 단위는 {ingredient.BaseUnit}입니다. 변환 정보가 없으므로 같은 단위로 입력하세요.");
            return;
        }

        CommitMenuEditorDraft();
        _selectedMenu.Ingredients.Add(new MenuIngredient
        {
            IngredientId = ingredientId,
            Amount = amount,
            Unit = unit
        });

        SaveAndRefresh("메뉴에 재료를 추가했습니다.");
    }

    private void RemoveMenuIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMenu is null || MenuIngredientsGrid.SelectedItem is not MenuIngredientRow row)
        {
            return;
        }

        var item = _selectedMenu.Ingredients.FirstOrDefault(menuIngredient => menuIngredient.Id == row.Id);
        if (item is null)
        {
            return;
        }

        CommitMenuEditorDraft();
        _selectedMenu.Ingredients.Remove(item);
        SaveAndRefresh("메뉴 재료를 삭제했습니다.");
    }

    private void MenuIngredientsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (_isRefreshing || e.EditAction != DataGridEditAction.Commit || e.Column != MenuIngredientAmountColumn)
        {
            return;
        }

        if (_selectedMenu is null || e.Row.Item is not MenuIngredientRow row || e.EditingElement is not TextBox editor)
        {
            return;
        }

        if (!TryReadPositive(editor.Text, out var amount))
        {
            e.Cancel = true;
            ShowWarning("사용량은 0보다 큰 숫자로 입력하세요.");
            editor.SelectAll();
            return;
        }

        var item = _selectedMenu.Ingredients.FirstOrDefault(menuIngredient => menuIngredient.Id == row.Id);
        if (item is null || Math.Abs(item.Amount - amount) < 0.000001)
        {
            return;
        }

        CommitMenuEditorDraft();
        item.Amount = amount;

        RefreshAfterGridEdit(() => SaveAndRefresh("메뉴 재료 사용량을 수정했습니다."));
    }

    private void PlanDisplayDayPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || PlanDisplayDayPicker.SelectedValue is not int dayIndex)
        {
            return;
        }

        _selectedPlanDayIndex = Math.Clamp(dayIndex, 0, 6);
        _isRefreshing = true;
        RefreshWeek();
        _isRefreshing = false;
        SetStatus($"{FormatDay(_selectedPlanDayIndex)} 식단 항목을 표시합니다.");
    }

    private void PlanGroupPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || PlanGroupPicker.SelectedValue is not Guid groupId)
        {
            return;
        }

        CommitPlanGroupEditorDraft();
        _state.SelectedWeeklyPlanGroupId = groupId;
        SaveAndRefreshWeek("식단 그룹을 선택했습니다.");
    }

    private void NewPlanGroup_Click(object sender, RoutedEventArgs e)
    {
        CommitPlanGroupEditorDraft();
        var group = new WeeklyPlanGroup
        {
            Name = UniqueName("새 식단", _state.WeeklyPlanGroups.Select(item => item.Name))
        };

        _state.WeeklyPlanGroups.Add(group);
        _state.SelectedWeeklyPlanGroupId = group.Id;
        SaveAndRefreshWeek("새 식단 그룹을 추가했습니다.");
    }

    private void SavePlanGroup_Click(object sender, RoutedEventArgs e)
    {
        var group = CurrentPlanGroup();
        if (group is null)
        {
            ShowWarning("저장할 식단 그룹이 없습니다.");
            return;
        }

        var name = PlanGroupNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("식단 그룹 이름을 입력하세요.");
            return;
        }

        group.Name = name;
        SaveAndRefreshWeek("식단 그룹을 저장했습니다.");
    }

    private void DeletePlanGroup_Click(object sender, RoutedEventArgs e)
    {
        var group = CurrentPlanGroup();
        if (group is null)
        {
            return;
        }

        if (MessageBox.Show("선택한 식단 그룹을 삭제할까요?", "식단 그룹 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _state.WeeklyPlanGroups.Remove(group);
        if (_state.WeeklyPlanGroups.Count == 0)
        {
            _state.WeeklyPlanGroups.Add(new WeeklyPlanGroup { Name = "기본 식단" });
        }

        _state.SelectedWeeklyPlanGroupId = _state.WeeklyPlanGroups[0].Id;
        SaveAndRefreshWeek("식단 그룹을 삭제했습니다.");
    }

    private void AddPlanItem_Click(object sender, RoutedEventArgs e)
    {
        var group = CurrentPlanGroup();
        if (group is null)
        {
            ShowWarning("메뉴를 추가할 식단 그룹을 선택하세요.");
            return;
        }

        var dayIndex = Math.Clamp(_selectedPlanDayIndex, 0, 6);

        if (PlanMenuPicker.SelectedValue is not Guid menuId)
        {
            ShowWarning("메뉴를 선택하세요.");
            return;
        }

        if (!TryReadPositive(PlanQuantityBox.Text, out var quantity))
        {
            ShowWarning("인분은 0보다 큰 숫자로 입력하세요.");
            return;
        }

        CommitPlanGroupEditorDraft();
        _selectedPlanDayIndex = dayIndex;
        group.Items.Add(new WeeklyPlanItem
        {
            DayIndex = dayIndex,
            MenuId = menuId,
            Quantity = quantity
        });

        SaveAndRefreshWeek("식단에 메뉴를 추가했습니다.");
    }

    private void RemovePlanItem_Click(object sender, RoutedEventArgs e)
    {
        var group = CurrentPlanGroup();
        if (group is null)
        {
            return;
        }

        var selectedIds = PlanItemsGrid.SelectedItems
            .OfType<PlanRow>()
            .Select(row => row.Id)
            .ToHashSet();
        if (selectedIds.Count == 0)
        {
            return;
        }

        var items = group.Items
            .Where(planItem => selectedIds.Contains(planItem.Id))
            .ToList();
        if (items.Count == 0)
        {
            return;
        }

        CommitPlanGroupEditorDraft();
        foreach (var item in items)
        {
            group.Items.Remove(item);
        }

        SaveAndRefreshWeek(items.Count == 1
            ? "식단에서 메뉴를 삭제했습니다."
            : $"식단에서 메뉴 {items.Count}개를 삭제했습니다.");
    }

    private void PlanItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (_isRefreshing || e.EditAction != DataGridEditAction.Commit || e.Column != PlanQuantityColumn)
        {
            return;
        }

        if (e.Row.Item is not PlanRow row || e.EditingElement is not TextBox editor)
        {
            return;
        }

        if (!TryReadPositive(editor.Text, out var quantity))
        {
            e.Cancel = true;
            ShowWarning("인분은 0보다 큰 숫자로 입력하세요.");
            editor.SelectAll();
            return;
        }

        var group = CurrentPlanGroup();
        var item = group?.Items.FirstOrDefault(planItem => planItem.Id == row.Id);
        if (item is null || Math.Abs(item.Quantity - quantity) < 0.000001)
        {
            return;
        }

        CommitPlanGroupEditorDraft();
        item.Quantity = quantity;
        DataStore.Save(_state);
        SetStatus("식단 인분을 수정했습니다.");

        RefreshAfterGridEdit(() =>
        {
            _isRefreshing = true;
            RefreshWeek();
            _isRefreshing = false;
        });
    }

    private void SaveGoals_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(GoalsGrid);
        _state.Goals.DailyTargets = _goalRows.ToDictionary(row => row.Key, row => Math.Max(0, row.DailyTarget));
        SaveAndRefreshWeek("영양 목표를 저장했습니다.");
        RefreshGoals();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        CommitGrid(IngredientNutrientsGrid);
        CommitGrid(IngredientPricesGrid);
        CommitGrid(GoalsGrid);

        if (_selectedIngredient is not null)
        {
            _selectedIngredient.Name = IngredientNameBox.Text.Trim();
            _selectedIngredient.Memo = IngredientMemoBox.Text.Trim();
            _selectedIngredient.BaseUnit = SelectedUnit(IngredientUnitBox);
            _selectedIngredient.NutrientsPer100 = _ingredientNutrientRows.ToDictionary(row => row.Key, row => Math.Max(0, row.Value));
        }

        if (_selectedMenu is not null)
        {
            CommitMenuEditorDraft();
        }

        CommitPlanGroupEditorDraft();
        _state.Goals.DailyTargets = _goalRows.ToDictionary(row => row.Key, row => Math.Max(0, row.DailyTarget));
        DataStore.Save(_state);
    }

    private void SaveAndRefresh(string message)
    {
        DataStore.Save(_state);
        RefreshAll();
        SetStatus(message);
    }

    private void SaveAndRefreshWeek(string message)
    {
        DataStore.Save(_state);
        _isRefreshing = true;
        RefreshPickers();
        RefreshWeek();
        _isRefreshing = false;
        SetStatus(message);
    }

    private void RefreshAfterGridEdit(Action refresh)
    {
        Dispatcher.BeginInvoke(refresh, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private WeeklyPlanGroup? CurrentPlanGroup()
    {
        if (_state.WeeklyPlanGroups.Count == 0)
        {
            return null;
        }

        var group = _state.WeeklyPlanGroups.FirstOrDefault(item => item.Id == _state.SelectedWeeklyPlanGroupId);
        if (group is not null)
        {
            return group;
        }

        group = _state.WeeklyPlanGroups[0];
        _state.SelectedWeeklyPlanGroupId = group.Id;
        return group;
    }

    private void CommitPlanGroupEditorDraft()
    {
        var group = CurrentPlanGroup();
        if (group is null)
        {
            return;
        }

        var name = PlanGroupNameBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            group.Name = name;
        }
    }

    private void CommitMenuEditorDraft()
    {
        if (_selectedMenu is null)
        {
            return;
        }

        _selectedMenu.Name = MenuNameBox.Text.Trim();
        _selectedMenu.Recipe = RecipeBox.Text.Trim();
    }

    private static List<NutrientSummaryRow> BuildNutrientRows(IEnumerable<NutrientDefinition> definitions, Dictionary<string, double> nutrients)
    {
        return definitions
            .Select(nutrient => new NutrientSummaryRow
            {
                Name = nutrient.Name,
                Amount = FormatNumber(nutrients.GetValueOrDefault(nutrient.Key)),
                Unit = nutrient.Unit
            })
            .ToList();
    }

    private static string FormatDay(int dayIndex)
    {
        return DayNames[Math.Clamp(dayIndex, 0, 6)];
    }

    private static string FormatMoney(double value)
    {
        return $"{Math.Max(0, value):N0}원";
    }

    private static string FormatWithUnit(double value, string unit)
    {
        return $"{FormatNumber(value)} {unit}";
    }

    private static string FormatNumber(double value)
    {
        return value switch
        {
            >= 100 => value.ToString("N0"),
            >= 10 => value.ToString("N1"),
            > 0 => value.ToString("N2"),
            _ => "0"
        };
    }

    private static bool TryReadPositive(string text, out double value)
    {
        if (!NumberInput.TryParseDecimal(text, out value))
        {
            value = 0;
            return false;
        }

        return value > 0;
    }

    private static string SelectedUnit(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item && item.Content is string content)
        {
            return content;
        }

        return comboBox.Text is "ml" ? "ml" : "g";
    }

    private static void SetUnitCombo(ComboBox comboBox, string unit)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if ((string)item.Content == unit)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static void CommitGrid(DataGrid grid)
    {
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private static string UniqueName(string baseName, IEnumerable<string> names)
    {
        var existing = names.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        if (!existing.Contains(baseName))
        {
            return baseName;
        }

        var index = 2;
        while (existing.Contains($"{baseName} {index}"))
        {
            index++;
        }

        return $"{baseName} {index}";
    }

    private void SetStatus(string message)
    {
        StatusText.Text = $"{DateTime.Now:HH:mm:ss}  {message}";
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(message, "SimpleMenu", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

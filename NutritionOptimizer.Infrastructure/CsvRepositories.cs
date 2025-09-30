using CsvHelper;
using CsvHelper.Configuration;
using NutritionOptimizer.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NutritionOptimizer.Infrastructure;

public sealed class CsvFoodRepository : IFoodRepository
{
    private readonly string _path;
    private readonly UnitNormalizer.UnitNormalizationOptions _unitOptions;
    public CsvFoodRepository(string path)
         : this(path, UnitNormalizer.UnitNormalizationOptions.Default)
    {
    }

    public CsvFoodRepository(string path, UnitNormalizer.UnitNormalizationOptions unitOptions)
    {
        _path = path;
        _unitOptions = unitOptions ?? throw new ArgumentNullException(nameof(unitOptions));
    }

    // 추가: 새 음식을 CSV에 추가
    public async Task AddAsync(Food food, CancellationToken ct = default)
    {
        var foods = (await GetAllAsync(ct)).ToList();
        foods.Add(food);
        await SaveAllAsync(foods, ct);
    }

    // 수정: 기존 음식을 업데이트
    public async Task UpdateAsync(Food food, CancellationToken ct = default)
    {
        var foods = (await GetAllAsync(ct)).ToList();
        var index = foods.FindIndex(f => f.Id == food.Id);
        if (index >= 0)
        {
            foods[index] = food;
            await SaveAllAsync(foods, ct);
        }
    }

    // 삭제: ID로 음식 제거
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var foods = (await GetAllAsync(ct)).ToList();
        foods.RemoveAll(f => f.Id == id);
        await SaveAllAsync(foods, ct);
    }

    // CSV 파일에 전체 리스트 저장
    private async Task SaveAllAsync(IEnumerable<Food> foods, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_path, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        
        // 매핑 등록
        csv.Context.RegisterClassMap<FoodRowMap>();
        
        var rows = foods.Select(f => new FoodRow
        {
            Id = f.Id,
            Name = f.Name,
            Brand = f.Brand ?? string.Empty,
            Category = f.Category ?? string.Empty,
            PricePer100g = f.PricePer100g.ToString(CultureInfo.InvariantCulture),
            WeightUnit = f.WeightUnit,
            Kcal = f.Kcal.ToString(CultureInfo.InvariantCulture),
            ProteinG = f.ProteinG.ToString(CultureInfo.InvariantCulture),
            FatG = f.FatG.ToString(CultureInfo.InvariantCulture),
            SaturatedFatG = f.SaturatedFatG.ToString(CultureInfo.InvariantCulture),
            TransFatG = f.TransFatG.ToString(CultureInfo.InvariantCulture),
            CarbsG = f.CarbsG.ToString(CultureInfo.InvariantCulture),
            FiberG = f.FiberG.ToString(CultureInfo.InvariantCulture),
            SugarG = f.SugarG.ToString(CultureInfo.InvariantCulture),
            SodiumMg = f.SodiumMg.ToString(CultureInfo.InvariantCulture),
            CholesterolMg = f.CholesterolMg.ToString(CultureInfo.InvariantCulture),
            CalciumMg = f.CalciumMg.ToString(CultureInfo.InvariantCulture),
            IronMg = f.IronMg.ToString(CultureInfo.InvariantCulture),
            MagnesiumMg = f.MagnesiumMg.ToString(CultureInfo.InvariantCulture),
            PhosphorusMg = f.PhosphorusMg.ToString(CultureInfo.InvariantCulture),
            PotassiumMg = f.PotassiumMg.ToString(CultureInfo.InvariantCulture),
            ZincMg = f.ZincMg.ToString(CultureInfo.InvariantCulture),
            VitaminAUg = f.VitaminAUg.ToString(CultureInfo.InvariantCulture),
            VitaminCMg = f.VitaminCMg.ToString(CultureInfo.InvariantCulture),
            VitaminDUg = f.VitaminDUg.ToString(CultureInfo.InvariantCulture),
            VitaminEMg = f.VitaminEMg.ToString(CultureInfo.InvariantCulture),
            VitaminB1Mg = f.VitaminB1Mg.ToString(CultureInfo.InvariantCulture),
            VitaminB2Mg = f.VitaminB2Mg.ToString(CultureInfo.InvariantCulture),
            VitaminB3Mg = f.VitaminB3Mg.ToString(CultureInfo.InvariantCulture),
            VitaminB6Mg = f.VitaminB6Mg.ToString(CultureInfo.InvariantCulture),
            VitaminB9Ug = f.VitaminB9Ug.ToString(CultureInfo.InvariantCulture),
            VitaminB12Ug = f.VitaminB12Ug.ToString(CultureInfo.InvariantCulture)
        });
        
        await csv.WriteRecordsAsync(rows, ct);
    }

    public async Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken ct = default)
    {
        using var reader = new StreamReader(_path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        
        // 매핑 등록
        csv.Context.RegisterClassMap<FoodRowMap>();

        var foods = new List<Food>();
        await foreach (var row in csv.GetRecordsAsync<FoodRow>().WithCancellation(ct))
        {
            var normalization = UnitNormalizer.GetNormalization(row.WeightUnit, _unitOptions);

            double Normalize(string value) => normalization.Normalize(Parse(value));
            foods.Add(new Food(
                row.Id,
                row.Name,
                string.IsNullOrWhiteSpace(row.Brand) ? null : row.Brand,
                string.IsNullOrWhiteSpace(row.Category) ? null : row.Category,
                Normalize(row.PricePer100g),
                normalization.NormalizedUnit,
                Normalize(row.Kcal),
                Normalize(row.ProteinG),
                Normalize(row.FatG),
                Normalize(row.SaturatedFatG),
                Normalize(row.TransFatG),
                Normalize(row.CarbsG),
                Normalize(row.FiberG),
                Normalize(row.SugarG),
                Normalize(row.SodiumMg),
                Normalize(row.CholesterolMg),
                Normalize(row.CalciumMg),
                Normalize(row.IronMg),
                Normalize(row.MagnesiumMg),
                Normalize(row.PhosphorusMg),
                Normalize(row.PotassiumMg),
                Normalize(row.ZincMg),
                Normalize(row.VitaminAUg),
                Normalize(row.VitaminCMg),
                Normalize(row.VitaminDUg),
                Normalize(row.VitaminEMg),
                Normalize(row.VitaminB1Mg),
                Normalize(row.VitaminB2Mg),
                Normalize(row.VitaminB3Mg),
                Normalize(row.VitaminB6Mg),
                Normalize(row.VitaminB9Ug),
                Normalize(row.VitaminB12Ug)));
        }

        return foods;

        static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    }

    private sealed class FoodRow
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Brand { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string PricePer100g { get; init; } = "0";
        public string WeightUnit { get; init; } = string.Empty;
        public string Kcal { get; init; } = "0";
        public string ProteinG { get; init; } = "0";
        public string FatG { get; init; } = "0";
        public string SaturatedFatG { get; init; } = "0";
        public string TransFatG { get; init; } = "0";
        public string CarbsG { get; init; } = "0";
        public string FiberG { get; init; } = "0";
        public string SugarG { get; init; } = "0";
        public string SodiumMg { get; init; } = "0";
        public string CholesterolMg { get; init; } = "0";
        // 무기질
        public string CalciumMg { get; init; } = "0";
        public string IronMg { get; init; } = "0";
        public string MagnesiumMg { get; init; } = "0";
        public string PhosphorusMg { get; init; } = "0";
        public string PotassiumMg { get; init; } = "0";
        public string ZincMg { get; init; } = "0";
        // 비타민
        public string VitaminAUg { get; init; } = "0";
        public string VitaminCMg { get; init; } = "0";
        public string VitaminDUg { get; init; } = "0";
        public string VitaminEMg { get; init; } = "0";
        public string VitaminB1Mg { get; init; } = "0";
        public string VitaminB2Mg { get; init; } = "0";
        public string VitaminB3Mg { get; init; } = "0";
        public string VitaminB6Mg { get; init; } = "0";
        public string VitaminB9Ug { get; init; } = "0";
        public string VitaminB12Ug { get; init; } = "0";
    }

    // CSV 헤더와 클래스 속성 매핑 설정
    private sealed class FoodRowMap : ClassMap<FoodRow>
    {
        public FoodRowMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Name).Name("name");
            Map(m => m.Brand).Name("brand");
            Map(m => m.Category).Name("category");
            Map(m => m.PricePer100g).Name("price_per_100g");
            Map(m => m.WeightUnit).Name("w_unit");
            Map(m => m.Kcal).Name("kcal");
            Map(m => m.ProteinG).Name("protein_g");
            Map(m => m.FatG).Name("fat_g");
            Map(m => m.SaturatedFatG).Name("saturated_fat_g");
            Map(m => m.TransFatG).Name("trans_fat_g");
            Map(m => m.CarbsG).Name("carbs_g");
            Map(m => m.FiberG).Name("fiber_g");
            Map(m => m.SugarG).Name("sugar_g");
            Map(m => m.SodiumMg).Name("sodium_mg");
            Map(m => m.CholesterolMg).Name("cholesterol_mg");
            // 무기질
            Map(m => m.CalciumMg).Name("calcium_mg");
            Map(m => m.IronMg).Name("iron_mg");
            Map(m => m.MagnesiumMg).Name("magnesium_mg");
            Map(m => m.PhosphorusMg).Name("phosphorus_mg");
            Map(m => m.PotassiumMg).Name("potassium_mg");
            Map(m => m.ZincMg).Name("zinc_mg");
            // 비타민
            Map(m => m.VitaminAUg).Name("vitamin_a_ug");
            Map(m => m.VitaminCMg).Name("vitamin_c_mg");
            Map(m => m.VitaminDUg).Name("vitamin_d_ug");
            Map(m => m.VitaminEMg).Name("vitamin_e_mg");
            Map(m => m.VitaminB1Mg).Name("vitamin_b1_mg");
            Map(m => m.VitaminB2Mg).Name("vitamin_b2_mg");
            Map(m => m.VitaminB3Mg).Name("vitamin_b3_mg");
            Map(m => m.VitaminB6Mg).Name("vitamin_b6_mg");
            Map(m => m.VitaminB9Ug).Name("vitamin_b9_ug");
            Map(m => m.VitaminB12Ug).Name("vitamin_b12_ug");
        }
    }
}

// 식단 히스토리를 관리하는 CSV Repository
public sealed class CsvDietHistoryRepository : IDietHistoryRepository
{
    private readonly string _path;

    public CsvDietHistoryRepository(string path) => _path = path;

    // 모든 히스토리 조회
    public async Task<IReadOnlyList<DietHistory>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<DietHistory>();

        using var reader = new StreamReader(_path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<DietHistoryRowMap>();

        var histories = new List<DietHistory>();
        await foreach (var row in csv.GetRecordsAsync<DietHistoryRow>().WithCancellation(ct))
        {
            histories.Add(new DietHistory(
                row.Id,
                DateOnly.Parse(row.Date),
                string.IsNullOrWhiteSpace(row.SavedDietId) ? null : row.SavedDietId,
                string.IsNullOrWhiteSpace(row.SavedDietName) ? null : row.SavedDietName,
                row.MealType,
                row.Notes));
        }

        return histories.OrderByDescending(h => h.Date).ToList();
    }

    // 기간별 히스토리 조회
    public async Task<IReadOnlyList<DietHistory>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(h => h.Date >= startDate && h.Date <= endDate)
                  .OrderByDescending(h => h.Date)
                  .ToList();
    }

    // 특정 날짜의 히스토리 조회
    public async Task<IReadOnlyList<DietHistory>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(h => h.Date == date).ToList();
    }

    // 히스토리 추가
    public async Task AddAsync(DietHistory history, CancellationToken ct = default)
    {
        var histories = (await GetAllAsync(ct)).ToList();
        histories.Add(history);
        await SaveAllAsync(histories, ct);
    }

    // 히스토리 수정
    public async Task UpdateAsync(DietHistory history, CancellationToken ct = default)
    {
        var histories = (await GetAllAsync(ct)).ToList();
        var index = histories.FindIndex(h => h.Id == history.Id);
        if (index >= 0)
        {
            histories[index] = history;
            await SaveAllAsync(histories, ct);
        }
    }

    // 히스토리 삭제
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var histories = (await GetAllAsync(ct)).ToList();
        histories.RemoveAll(h => h.Id == id);
        await SaveAllAsync(histories, ct);
    }

    // 모든 히스토리 저장
    private async Task SaveAllAsync(IEnumerable<DietHistory> histories, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_path, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<DietHistoryRowMap>();

        var rows = histories.Select(h => new DietHistoryRow
        {
            Id = h.Id,
            Date = h.Date.ToString("yyyy-MM-dd"),
            SavedDietId = h.SavedDietId ?? string.Empty,
            SavedDietName = h.SavedDietName ?? string.Empty,
            MealType = h.MealType,
            Notes = h.Notes
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    private sealed class DietHistoryRow
    {
        public string Id { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;
        public string SavedDietId { get; init; } = string.Empty;
        public string SavedDietName { get; init; } = string.Empty;
        public string MealType { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
    }

    private sealed class DietHistoryRowMap : ClassMap<DietHistoryRow>
    {
        public DietHistoryRowMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Date).Name("date");
            Map(m => m.SavedDietId).Name("saved_diet_id");
            Map(m => m.SavedDietName).Name("saved_diet_name");
            Map(m => m.MealType).Name("meal_type");
            Map(m => m.Notes).Name("notes");
        }
    }
}

// 저장된 식단을 관리하는 CSV Repository
public sealed class CsvSavedDietRepository : ISavedDietRepository
{
    private readonly string _dietsPath;
    private readonly string _itemsPath;

    public CsvSavedDietRepository(string dietsPath, string itemsPath)
    {
        _dietsPath = dietsPath;
        _itemsPath = itemsPath;
    }

    // 모든 저장된 식단 조회
    public async Task<IReadOnlyList<SavedDiet>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_dietsPath))
            return Array.Empty<SavedDiet>();

        using var reader = new StreamReader(_dietsPath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<SavedDietRowMap>();

        var diets = new List<SavedDiet>();
        await foreach (var row in csv.GetRecordsAsync<SavedDietRow>().WithCancellation(ct))
        {
            diets.Add(new SavedDiet(
                row.Id,
                row.Name,
                DateOnly.Parse(row.CreatedDate),
                double.Parse(row.TotalCost, CultureInfo.InvariantCulture),
                row.Notes));
        }

        return diets.OrderByDescending(d => d.CreatedDate).ToList();
    }

    // 특정 식단 조회
    public async Task<SavedDiet?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(d => d.Id == id);
    }

    // 특정 식단의 음식 항목들 조회
    public async Task<IReadOnlyList<SavedDietItem>> GetItemsAsync(string dietId, CancellationToken ct = default)
    {
        if (!File.Exists(_itemsPath))
            return Array.Empty<SavedDietItem>();

        using var reader = new StreamReader(_itemsPath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<SavedDietItemRowMap>();

        var items = new List<SavedDietItem>();
        await foreach (var row in csv.GetRecordsAsync<SavedDietItemRow>().WithCancellation(ct))
        {
            if (row.DietId == dietId)
            {
                items.Add(new SavedDietItem(
                    row.DietId,
                    row.FoodId,
                    row.FoodName,
                    double.Parse(row.Amount100g, CultureInfo.InvariantCulture),
                    double.Parse(row.Cost, CultureInfo.InvariantCulture)));
            }
        }

        return items;
    }

    // 식단 저장 (식단 정보 + 음식 항목들)
    public async Task SaveAsync(SavedDiet diet, IReadOnlyList<SavedDietItem> items, CancellationToken ct = default)
    {
        // 기존 식단 목록에 추가
        var diets = (await GetAllAsync(ct)).ToList();
        var existing = diets.FindIndex(d => d.Id == diet.Id);
        if (existing >= 0)
            diets[existing] = diet;
        else
            diets.Add(diet);

        await SaveAllDietsAsync(diets, ct);

        // 기존 항목들 중 이 식단 것만 제거하고 새로 추가
        var allItems = (await GetAllItemsAsync(ct)).ToList();
        allItems.RemoveAll(i => i.DietId == diet.Id);
        allItems.AddRange(items);

        await SaveAllItemsAsync(allItems, ct);
    }

    // 식단 삭제
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var diets = (await GetAllAsync(ct)).ToList();
        diets.RemoveAll(d => d.Id == id);
        await SaveAllDietsAsync(diets, ct);

        var items = (await GetAllItemsAsync(ct)).ToList();
        items.RemoveAll(i => i.DietId == id);
        await SaveAllItemsAsync(items, ct);
    }

    // 모든 항목 조회 (내부용)
    private async Task<IReadOnlyList<SavedDietItem>> GetAllItemsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_itemsPath))
            return Array.Empty<SavedDietItem>();

        using var reader = new StreamReader(_itemsPath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<SavedDietItemRowMap>();

        var items = new List<SavedDietItem>();
        await foreach (var row in csv.GetRecordsAsync<SavedDietItemRow>().WithCancellation(ct))
        {
            items.Add(new SavedDietItem(
                row.DietId,
                row.FoodId,
                row.FoodName,
                double.Parse(row.Amount100g, CultureInfo.InvariantCulture),
                double.Parse(row.Cost, CultureInfo.InvariantCulture)));
        }

        return items;
    }

    // 모든 식단 저장
    private async Task SaveAllDietsAsync(IEnumerable<SavedDiet> diets, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_dietsPath, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<SavedDietRowMap>();

        var rows = diets.Select(d => new SavedDietRow
        {
            Id = d.Id,
            Name = d.Name,
            CreatedDate = d.CreatedDate.ToString("yyyy-MM-dd"),
            TotalCost = d.TotalCost.ToString(CultureInfo.InvariantCulture),
            Notes = d.Notes
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    // 모든 항목 저장
    private async Task SaveAllItemsAsync(IEnumerable<SavedDietItem> items, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_itemsPath, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<SavedDietItemRowMap>();

        var rows = items.Select(i => new SavedDietItemRow
        {
            DietId = i.DietId,
            FoodId = i.FoodId,
            FoodName = i.FoodName,
            Amount100g = i.Amount100g.ToString(CultureInfo.InvariantCulture),
            Cost = i.Cost.ToString(CultureInfo.InvariantCulture)
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    private sealed class SavedDietRow
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string CreatedDate { get; init; } = string.Empty;
        public string TotalCost { get; init; } = "0";
        public string Notes { get; init; } = string.Empty;
    }

    private sealed class SavedDietRowMap : ClassMap<SavedDietRow>
    {
        public SavedDietRowMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Name).Name("name");
            Map(m => m.CreatedDate).Name("created_date");
            Map(m => m.TotalCost).Name("total_cost");
            Map(m => m.Notes).Name("notes");
        }
    }

    private sealed class SavedDietItemRow
    {
        public string DietId { get; init; } = string.Empty;
        public string FoodId { get; init; } = string.Empty;
        public string FoodName { get; init; } = string.Empty;
        public string Amount100g { get; init; } = "0";
        public string Cost { get; init; } = "0";
    }

    private sealed class SavedDietItemRowMap : ClassMap<SavedDietItemRow>
    {
        public SavedDietItemRowMap()
        {
            Map(m => m.DietId).Name("diet_id");
            Map(m => m.FoodId).Name("food_id");
            Map(m => m.FoodName).Name("food_name");
            Map(m => m.Amount100g).Name("amount_100g");
            Map(m => m.Cost).Name("cost");
        }
    }
}

public sealed class CsvTargetsRepository : ITargetsRepository
{
    private readonly string _path;

    public CsvTargetsRepository(string path) => _path = path;

    // 추가: 새 목표치를 CSV에 추가
    public async Task AddAsync(NutrientTarget target, CancellationToken ct = default)
    {
        var targets = (await GetAllAsync(ct)).ToList();
        targets.Add(target);
        await SaveAllAsync(targets, ct);
    }

    // 수정: 기존 목표치 업데이트
    public async Task UpdateAsync(NutrientTarget target, CancellationToken ct = default)
    {
        var targets = (await GetAllAsync(ct)).ToList();
        var index = targets.FindIndex(t => string.Equals(t.NutrientKey, target.NutrientKey, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            targets[index] = target;
            await SaveAllAsync(targets, ct);
        }
    }

    // 삭제: 영양소 키로 목표치 제거
    public async Task DeleteAsync(string nutrientKey, CancellationToken ct = default)
    {
        var targets = (await GetAllAsync(ct)).ToList();
        targets.RemoveAll(t => string.Equals(t.NutrientKey, nutrientKey, StringComparison.OrdinalIgnoreCase));
        await SaveAllAsync(targets, ct);
    }

    // CSV 파일에 전체 리스트 저장
    private async Task SaveAllAsync(IEnumerable<NutrientTarget> targets, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_path, false);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        
        // 매핑 등록
        csv.Context.RegisterClassMap<TargetRowMap>();
        
        var rows = targets.Select(t => new TargetRow
        {
            Nutrient = t.NutrientKey,
            Min = t.Min?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Max = t.Max?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Recommended = t.Recommended?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Sufficient = t.Sufficient?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Unit = t.Unit
        });
        
        await csv.WriteRecordsAsync(rows, ct);
    }

    public async Task<IReadOnlyList<NutrientTarget>> GetAllAsync(CancellationToken ct = default)
    {
        using var reader = new StreamReader(_path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        });

        // 매핑 등록
        csv.Context.RegisterClassMap<TargetRowMap>();

        var targets = new List<NutrientTarget>();
        await foreach (var row in csv.GetRecordsAsync<TargetRow>().WithCancellation(ct))
        {
            targets.Add(new NutrientTarget(
                row.Nutrient,
                ParseNullable(row.Min),
                ParseNullable(row.Max),
                ParseNullable(row.Recommended),
                ParseNullable(row.Sufficient),
                row.Unit));
        }

        return targets;

        static double? ParseNullable(string value)
            => string.IsNullOrWhiteSpace(value) ? null : double.Parse(value, CultureInfo.InvariantCulture);
    }

    private sealed class TargetRow
    {
        public string Nutrient { get; init; } = string.Empty;
        public string Min { get; init; } = string.Empty;
        public string Max { get; init; } = string.Empty;
        public string Recommended { get; init; } = string.Empty;
        public string Sufficient { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
    }

    // CSV 헤더와 클래스 속성 매핑 설정
    private sealed class TargetRowMap : ClassMap<TargetRow>
    {
        public TargetRowMap()
        {
            Map(m => m.Nutrient).Name("nutrient");
            Map(m => m.Min).Name("min");
            Map(m => m.Max).Name("max");
            Map(m => m.Recommended).Name("recommended");
            Map(m => m.Sufficient).Name("sufficient");
            Map(m => m.Unit).Name("unit");
        }
    }
}

// 가격 이력을 관리하는 CSV Repository
public sealed class CsvPriceHistoryRepository : IPriceHistoryRepository
{
    private readonly string _path;

    public CsvPriceHistoryRepository(string path) => _path = path;

    // 모든 가격 이력 조회
    public async Task<IReadOnlyList<PriceHistory>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<PriceHistory>();

        using var reader = new StreamReader(_path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        
        // 매핑 등록
        csv.Context.RegisterClassMap<PriceHistoryRowMap>();

        var histories = new List<PriceHistory>();
        await foreach (var row in csv.GetRecordsAsync<PriceHistoryRow>().WithCancellation(ct))
        {
            histories.Add(new PriceHistory(
                row.FoodId,
                DateOnly.Parse(row.EffectiveDate),
                double.Parse(row.PricePer100g, CultureInfo.InvariantCulture)));
        }

        return histories;
    }

    // 특정 음식의 가격 이력 조회
    public async Task<IReadOnlyList<PriceHistory>> GetByFoodIdAsync(string foodId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(p => p.FoodId == foodId)
                  .OrderByDescending(p => p.EffectiveDate)
                  .ToList();
    }

    // 특정 날짜의 유효한 가격 조회 (해당 날짜 이전의 가장 최근 가격)
    public async Task<double?> GetPriceAtDateAsync(string foodId, DateOnly date, CancellationToken ct = default)
    {
        var histories = await GetByFoodIdAsync(foodId, ct);
        var validPrice = histories
            .Where(p => p.EffectiveDate <= date)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault();

        return validPrice?.PricePer100g;
    }

    // 가격 이력 추가
    public async Task AddAsync(PriceHistory priceHistory, CancellationToken ct = default)
    {
        var histories = (await GetAllAsync(ct)).ToList();
        histories.Add(priceHistory);
        await SaveAllAsync(histories, ct);
    }

    // 가격 이력 삭제
    public async Task DeleteAsync(string foodId, DateOnly effectiveDate, CancellationToken ct = default)
    {
        var histories = (await GetAllAsync(ct)).ToList();
        histories.RemoveAll(p => p.FoodId == foodId && p.EffectiveDate == effectiveDate);
        await SaveAllAsync(histories, ct);
    }

    // CSV 파일에 전체 리스트 저장
    private async Task SaveAllAsync(IEnumerable<PriceHistory> histories, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_path, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        
        // 매핑 등록
        csv.Context.RegisterClassMap<PriceHistoryRowMap>();

        var rows = histories.Select(h => new PriceHistoryRow
        {
            FoodId = h.FoodId,
            EffectiveDate = h.EffectiveDate.ToString("yyyy-MM-dd"),
            PricePer100g = h.PricePer100g.ToString(CultureInfo.InvariantCulture)
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    private sealed class PriceHistoryRow
    {
        public string FoodId { get; init; } = string.Empty;
        public string EffectiveDate { get; init; } = string.Empty;
        public string PricePer100g { get; init; } = "0";
    }

    // CSV 헤더와 클래스 속성 매핑 설정
    private sealed class PriceHistoryRowMap : ClassMap<PriceHistoryRow>
    {
        public PriceHistoryRowMap()
        {
            Map(m => m.FoodId).Name("food_id");
            Map(m => m.EffectiveDate).Name("effective_date");
            Map(m => m.PricePer100g).Name("price_per_100g");
        }
    }
}

public static class UnitNormalizer
{
    private const string NormalizedUnit = "100g";

    private static readonly IReadOnlyDictionary<string, double> GramUnitFactors = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = 1d,
        ["gram"] = 1d,
        ["grams"] = 1d,
        ["kg"] = 1_000d,
        ["kilogram"] = 1_000d,
        ["kilograms"] = 1_000d,
        ["mg"] = 0.001d,
        ["milligram"] = 0.001d,
        ["milligrams"] = 0.001d,
    };

    private static readonly IReadOnlyDictionary<string, double> VolumeUnitToMilliliters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["ml"] = 1d,
        ["milliliter"] = 1d,
        ["milliliters"] = 1d,
        ["millilitre"] = 1d,
        ["millilitres"] = 1d,
        ["l"] = 1_000d,
        ["liter"] = 1_000d,
        ["liters"] = 1_000d,
        ["litre"] = 1_000d,
        ["litres"] = 1_000d,
    };

    public static double ToAmount100g(double userAmount, string weightUnit, UnitNormalizationOptions? options = null)
        => GetNormalization(weightUnit, options).Normalize(userAmount);

    public static UnitNormalizationResult GetNormalization(string weightUnit, UnitNormalizationOptions? options = null)
    {
        var unitOptions = options ?? UnitNormalizationOptions.Default;
        var (quantity, unitToken) = SplitQuantityAndUnit(weightUnit);

        if (unitToken.Length == 0)
        {
            var configured = unitOptions.TryGetUnitWeight(weightUnit.Trim());
            if (configured.HasValue)
            {
                return CreateResult(quantity * configured.Value);
            }

            throw new NotSupportedException($"Unsupported weight unit '{weightUnit}'. Add a unit-to-gram mapping to the unit normalization configuration.");
        }

        if (GramUnitFactors.TryGetValue(unitToken, out var gramFactor))
        {
            return CreateResult(quantity * gramFactor);
        }

        if (VolumeUnitToMilliliters.TryGetValue(unitToken, out var milliliterFactor))
        {
            var milliliters = quantity * milliliterFactor;
            var density = unitOptions.ResolveDensity(weightUnit.Trim(), unitToken);
            return CreateResult(milliliters * density);
        }

        var configuredUnit = unitOptions.TryGetUnitWeight(unitToken);
        if (configuredUnit.HasValue)
        {
            return CreateResult(quantity * configuredUnit.Value);
        }

        throw new NotSupportedException($"Unsupported weight unit '{weightUnit}'. Add a unit-to-gram mapping to the unit normalization configuration.");
    }

    private static UnitNormalizationResult CreateResult(double grams)
    {
        if (grams <= 0)
        {
            throw new InvalidOperationException("Weight units must resolve to a positive gram amount.");
        }

        return new UnitNormalizationResult(grams, NormalizedUnit);
    }

    private static (double Quantity, string UnitToken) SplitQuantityAndUnit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Weight unit cannot be empty.", nameof(value));
        }

        var trimmed = value.Trim();
        var index = 0;

        while (index < trimmed.Length && (char.IsDigit(trimmed[index]) || trimmed[index] == '.' || trimmed[index] == ','))
        {
            index++;
        }

        if (index == 0)
        {
            throw new FormatException($"Weight unit '{value}' does not start with a numeric quantity.");
        }

        var quantityToken = trimmed[..index].Replace(',', '.');
        if (!double.TryParse(quantityToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var quantity))
        {
            throw new FormatException($"Weight unit '{value}' contains an invalid quantity '{quantityToken}'.");
        }

        var unitPart = trimmed[index..].Trim().ToLowerInvariant();
        return (quantity, unitPart);
    }

    public sealed record UnitNormalizationResult(double GramsPerUnit, string NormalizedUnit)
    {
        public double ScaleTo100g => 100d / GramsPerUnit;

        public double Normalize(double value) => value * ScaleTo100g;
    }

    public sealed class UnitNormalizationOptions
    {
        public const string DefaultDensityKey = "default";

        private readonly IReadOnlyDictionary<string, double> _unitWeights;
        private readonly IReadOnlyDictionary<string, double> _densities;

        public static UnitNormalizationOptions Default { get; } = new UnitNormalizationOptions();

        public UnitNormalizationOptions(
            IReadOnlyDictionary<string, double>? unitWeightsInGrams = null,
            IReadOnlyDictionary<string, double>? milliliterDensities = null)
        {
            _unitWeights = CreateDictionary(unitWeightsInGrams);
            _densities = CreateDensityDictionary(milliliterDensities);
        }

        public double? TryGetUnitWeight(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return _unitWeights.TryGetValue(token, out var grams) ? grams : null;
        }

        public double ResolveDensity(string rawUnit, string normalizedUnit)
        {
            if (_densities.TryGetValue(rawUnit, out var density))
            {
                return density;
            }

            if (_densities.TryGetValue(normalizedUnit, out density))
            {
                return density;
            }

            if (_densities.TryGetValue(DefaultDensityKey, out density))
            {
                return density;
            }

            return 1d;
        }

        private static IReadOnlyDictionary<string, double> CreateDictionary(IReadOnlyDictionary<string, double>? source)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            if (source is null)
            {
                return new Dictionary<string, double>(comparer);
            }

            var dictionary = new Dictionary<string, double>(source.Count, comparer);
            foreach (var (key, value) in source)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                dictionary[key.Trim()] = value;
            }

            return dictionary;
        }

        private static IReadOnlyDictionary<string, double> CreateDensityDictionary(IReadOnlyDictionary<string, double>? source)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dictionary = new Dictionary<string, double>(comparer)
            {
                [DefaultDensityKey] = 1d,
                ["ml"] = 1d,
                ["milliliter"] = 1d,
                ["milliliters"] = 1d,
            };

            if (source is not null)
            {
                foreach (var (key, value) in source)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    dictionary[key.Trim()] = value;
                }
            }

            return dictionary;
        }
    }
}

// 음식 선호도를 관리하는 CSV Repository
public sealed class CsvFoodPreferenceRepository : IFoodPreferenceRepository
{
    private readonly string _path;

    public CsvFoodPreferenceRepository(string path) => _path = path;

    // 모든 선호도 조회
    public async Task<IReadOnlyList<FoodPreference>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<FoodPreference>();

        using var reader = new StreamReader(_path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<FoodPreferenceRowMap>();

        var preferences = new List<FoodPreference>();
        await foreach (var row in csv.GetRecordsAsync<FoodPreferenceRow>().WithCancellation(ct))
        {
            preferences.Add(new FoodPreference(
                row.FoodId,
                bool.Parse(row.IsFavorite),
                bool.Parse(row.IsExcluded),
                row.Notes));
        }

        return preferences;
    }

    // 특정 음식의 선호도 조회
    public async Task<FoodPreference?> GetByFoodIdAsync(string foodId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(p => p.FoodId == foodId);
    }

    // 즐겨찾기 목록 조회
    public async Task<IReadOnlyList<FoodPreference>> GetFavoritesAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(p => p.IsFavorite && !p.IsExcluded).ToList();
    }

    // 제외 목록 조회
    public async Task<IReadOnlyList<FoodPreference>> GetExcludedAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(p => p.IsExcluded).ToList();
    }

    // 선호도 저장 (추가 또는 수정)
    public async Task SaveAsync(FoodPreference preference, CancellationToken ct = default)
    {
        var preferences = (await GetAllAsync(ct)).ToList();
        var index = preferences.FindIndex(p => p.FoodId == preference.FoodId);
        
        if (index >= 0)
            preferences[index] = preference;
        else
            preferences.Add(preference);

        await SaveAllAsync(preferences, ct);
    }

    // 선호도 삭제
    public async Task DeleteAsync(string foodId, CancellationToken ct = default)
    {
        var preferences = (await GetAllAsync(ct)).ToList();
        preferences.RemoveAll(p => p.FoodId == foodId);
        await SaveAllAsync(preferences, ct);
    }

    // 모든 선호도 저장
    private async Task SaveAllAsync(IEnumerable<FoodPreference> preferences, CancellationToken ct)
    {
        await using var writer = new StreamWriter(_path, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<FoodPreferenceRowMap>();

        var rows = preferences.Select(p => new FoodPreferenceRow
        {
            FoodId = p.FoodId,
            IsFavorite = p.IsFavorite.ToString(),
            IsExcluded = p.IsExcluded.ToString(),
            Notes = p.Notes
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    private sealed class FoodPreferenceRow
    {
        public string FoodId { get; init; } = string.Empty;
        public string IsFavorite { get; init; } = "False";
        public string IsExcluded { get; init; } = "False";
        public string Notes { get; init; } = string.Empty;
    }

    private sealed class FoodPreferenceRowMap : ClassMap<FoodPreferenceRow>
    {
        public FoodPreferenceRowMap()
        {
            Map(m => m.FoodId).Name("food_id");
            Map(m => m.IsFavorite).Name("is_favorite");
            Map(m => m.IsExcluded).Name("is_excluded");
            Map(m => m.Notes).Name("notes");
        }
    }
}
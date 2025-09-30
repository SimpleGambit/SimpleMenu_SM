using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NutritionOptimizer.Domain;

public sealed record Food(
    string Id,
    string Name,
    string? Brand,
    string? Category,
    double PricePer100g,
    string WeightUnit,
    double Kcal,
    double ProteinG,
    double FatG,
    double SaturatedFatG,
    double TransFatG,
    double CarbsG,
    double FiberG,
    double SugarG,
    double SodiumMg,
    double CholesterolMg,
    // 무기질
    double CalciumMg,
    double IronMg,
    double MagnesiumMg,
    double PhosphorusMg,
    double PotassiumMg,
    double ZincMg,
    // 비타민
    double VitaminAUg,
    double VitaminCMg,
    double VitaminDUg,
    double VitaminEMg,
    double VitaminB1Mg,
    double VitaminB2Mg,
    double VitaminB3Mg,
    double VitaminB6Mg,
    double VitaminB9Ug,
    double VitaminB12Ug,
    string? Notes  // 메모 (요리법, 제철 정보 등)
)
{
    // 총 가격 (해당 제품의 실제 가격)
    public double TotalPrice
    {
        get
        {
            double quantity = ParseUnitQuantity(WeightUnit);
            return PricePer100g * (quantity / 100.0);
        }
    }

    // 단위당 가격 표시
    public string UnitPriceDisplay
    {
        get
        {
            double quantity = ParseUnitQuantity(WeightUnit);
            
            if (quantity < 100)
            {
                // 100g 미만이면 10g당 가격
                double unitPrice = PricePer100g / 10.0;
                return $"{unitPrice:N0}원/10g";
            }
            else
            {
                // 100g 이상이면 100g당 가격
                return $"{PricePer100g:N0}원/100g";
            }
        }
    }

    private static double ParseUnitQuantity(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 100;
        
        // 숫자 부분만 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            return quantity;
        }
        
        return 100;
    }
};

public sealed record NutrientTarget(
    string NutrientKey,
    double? Min,           // 최소
    double? Max,           // 최대
    double? Recommended,   // 권장 (RDA - Recommended Dietary Allowance)
    double? Sufficient,    // 충분 (AI - Adequate Intake)
    string Unit
);

public sealed record FoodQuantity(string FoodId, double Amount100g);

// 가격 이력 정보
public sealed record PriceHistory(
    string FoodId,
    DateOnly EffectiveDate,
    double PricePer100g
);

// 저장된 식단 정보
public sealed record SavedDiet(
    string Id,
    string Name,
    DateOnly CreatedDate,
    double TotalCost,
    string Notes
);

// 저장된 식단의 음식 항목
public sealed record SavedDietItem(
    string DietId,
    string FoodId,
    string FoodName,
    double Amount100g,
    double Cost
);

// 식단 히스토리 (날짜별 식단 기록)
public sealed record DietHistory(
    string Id,
    DateOnly Date,
    string? SavedDietId,
    string? SavedDietName,
    string MealType,
    string Notes
);

// 음식 선호도 (즐겨찾기 및 제외 목록)
public sealed record FoodPreference(
    string FoodId,
    bool IsFavorite,
    bool IsExcluded,
    string Notes
);

// 영양성분 템플릿 (100g 기준)
public sealed record NutritionTemplate(
    string Id,
    string Name,
    string Category,
    double Kcal,
    double ProteinG,
    double FatG,
    double SaturatedFatG,
    double TransFatG,
    double CarbsG,
    double FiberG,
    double SugarG,
    double SodiumMg,
    double CholesterolMg,
    double CalciumMg,
    double IronMg,
    double MagnesiumMg,
    double PhosphorusMg,
    double PotassiumMg,
    double ZincMg,
    double VitaminAUg,
    double VitaminCMg,
    double VitaminDUg,
    double VitaminEMg,
    double VitaminB1Mg,
    double VitaminB2Mg,
    double VitaminB3Mg,
    double VitaminB6Mg,
    double VitaminB9Ug,
    double VitaminB12Ug
);

public interface IFoodRepository
{
    Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Food food, CancellationToken ct = default);
    Task UpdateAsync(Food food, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface ITargetsRepository
{
    Task<IReadOnlyList<NutrientTarget>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(NutrientTarget target, CancellationToken ct = default);
    Task UpdateAsync(NutrientTarget target, CancellationToken ct = default);
    Task DeleteAsync(string nutrientKey, CancellationToken ct = default);
}

// 가격 이력 저장소
public interface IPriceHistoryRepository
{
    Task<IReadOnlyList<PriceHistory>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PriceHistory>> GetByFoodIdAsync(string foodId, CancellationToken ct = default);
    Task<double?> GetPriceAtDateAsync(string foodId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(PriceHistory priceHistory, CancellationToken ct = default);
    Task DeleteAsync(string foodId, DateOnly effectiveDate, CancellationToken ct = default);
}

// 저장된 식단 저장소
public interface ISavedDietRepository
{
    Task<IReadOnlyList<SavedDiet>> GetAllAsync(CancellationToken ct = default);
    Task<SavedDiet?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedDietItem>> GetItemsAsync(string dietId, CancellationToken ct = default);
    Task SaveAsync(SavedDiet diet, IReadOnlyList<SavedDietItem> items, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// 식단 히스토리 저장소
public interface IDietHistoryRepository
{
    Task<IReadOnlyList<DietHistory>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DietHistory>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<IReadOnlyList<DietHistory>> GetByDateAsync(DateOnly date, CancellationToken ct = default);
    Task AddAsync(DietHistory history, CancellationToken ct = default);
    Task UpdateAsync(DietHistory history, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// 음식 선호도 저장소
public interface IFoodPreferenceRepository
{
    Task<IReadOnlyList<FoodPreference>> GetAllAsync(CancellationToken ct = default);
    Task<FoodPreference?> GetByFoodIdAsync(string foodId, CancellationToken ct = default);
    Task<IReadOnlyList<FoodPreference>> GetFavoritesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FoodPreference>> GetExcludedAsync(CancellationToken ct = default);
    Task SaveAsync(FoodPreference preference, CancellationToken ct = default);
    Task DeleteAsync(string foodId, CancellationToken ct = default);
}

// 영양성분 템플릿 저장소
public interface INutritionTemplateRepository
{
    Task<IReadOnlyList<NutritionTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<NutritionTemplate?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<NutritionTemplate>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task AddAsync(NutritionTemplate template, CancellationToken ct = default);
    Task UpdateAsync(NutritionTemplate template, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
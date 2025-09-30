using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NutritionOptimizer.Domain;

public sealed record Food(
    string Id,
    string Name,
    string Type,       // 구분: "식재료" 또는 "요리" (템플릿에서 가져오기)
    string Category,   // 분류: 채소, 과일, 육류, 곡물 등 (템플릿에서 가져오기)
    double PricePer100g,
    string WeightUnit,
    string CookingMethod, // 조리 방법: "없음", "가열조리", "튀김"
    double Kcal,
    double MoistureG,  // 수분
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
    double CopperMg,
    double ManganeseMg,
    double SeleniumUg,
    double MolybdenumUg,
    double IodineUg,
    // 비타민
    double VitaminAUg,
    double VitaminCMg,
    double VitaminDUg,
    double VitaminEMg,
    double VitaminKUg,
    double VitaminB1Mg,
    double VitaminB2Mg,
    double VitaminB3Mg,
    double VitaminB5Mg,
    double VitaminB6Mg,
    double VitaminB7Ug,
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
            double multiplier = CalculateMultiplier(WeightUnit);
            return Math.Round(PricePer100g * multiplier, 2);
        }
    }

    // 단위당 가격 표시 (100g 미만이면 10g 기준, 100g 이상은 100g 기준)
    public string UnitPriceDisplay
    {
        get
        {
            var (quantity, unit) = ParseWeightUnit(WeightUnit);
            
            // 100g(또는 100ml) 미만이면 10g 기준으로 표시
            if (quantity < 100)
            {
                double pricePer10g = PricePer100g / 10.0;
                return $"{pricePer10g:N1}원/10{GetBaseUnit(unit)}";
            }
            else
            {
                return $"{PricePer100g:N0}원/100{GetBaseUnit(unit)}";
            }
        }
    }
    
    // 기본 단위 추출 (g 또는 ml)
    private static string GetBaseUnit(string unit)
    {
        if (unit == "ml" || unit == "l")
            return "ml";
        else
            return "g";
    }
    
    // 영양성분 표시용 속성들 (100g 미만이면 10g 기준, 100g 이상이면 100g 기준)
    private bool IsSmallPortion => ParseWeightUnit(WeightUnit).quantity < 100;
    private double DisplayMultiplier => IsSmallPortion ? 0.1 : 1.0; // 10g 기준이면 0.1배, 100g 기준이면 1배
    
    public string KcalDisplay => IsSmallPortion 
        ? $"{Kcal * DisplayMultiplier:N1}" 
        : $"{Kcal:N0}";
    
    public string CarbsGDisplay => $"{CarbsG * DisplayMultiplier:N1}";
    public string ProteinGDisplay => $"{ProteinG * DisplayMultiplier:N1}";
    public string FatGDisplay => $"{FatG * DisplayMultiplier:N1}";
    public string SaturatedFatGDisplay => $"{SaturatedFatG * DisplayMultiplier:N1}";
    public string SodiumMgDisplay => IsSmallPortion 
        ? $"{SodiumMg * DisplayMultiplier:N1}" 
        : $"{SodiumMg:N0}";
    public string FiberGDisplay => $"{FiberG * DisplayMultiplier:N1}";
    public string SugarGDisplay => $"{SugarG * DisplayMultiplier:N1}";
    public string CholesterolMgDisplay => IsSmallPortion 
        ? $"{CholesterolMg * DisplayMultiplier:N1}" 
        : $"{CholesterolMg:N0}";
    
    // 영양성분 헤더 (10g 또는 100g)
    public string NutrientUnit => IsSmallPortion ? "10g" : "100g";

    // 단위에서 100g 대비 배수 계산 (FoodEditorViewModel과 동일한 로직)
    private static double CalculateMultiplier(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        var (quantity, unit) = ParseWeightUnit(weightUnit);
        
        // g 또는 ml 단위면 100으로 나누고
        if (unit == "g" || unit == "ml")
        {
            return quantity / 100.0;
        }
        // kg 또는 l이면 1000g이므로 10배
        else if (unit == "kg" || unit == "l")
        {
            return quantity * 10.0;
        }
        
        return 1.0; // 기본값
    }

    // 단위 파싱: 숫자와 단위 분리
    private static (double quantity, string unit) ParseWeightUnit(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return (100, "g");
        
        // 숫자 부분 추출
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            // 단위 부분 추출
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            // 단위 정규화
            if (unit.StartsWith("kg"))
                return (quantity, "kg");
            else if (unit.StartsWith("l") && !unit.StartsWith("lb")) // lb는 제외
                return (quantity, "l");
            else if (unit.StartsWith("ml"))
                return (quantity, "ml");
            else // 기본은 g
                return (quantity, "g");
        }
        
        return (100, "g"); // 기본값
    }

    [Obsolete("Use CalculateMultiplier instead")]
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

// 음식에 사용된 템플릿 정보 (수정 시 복원용)
public sealed record FoodTemplateUsage(
    string FoodId,
    string TemplateId,
    double AmountG  // 해당 템플릿을 얼마나 사용했는지 (g)
);

// 가격 이력 정보
public sealed record PriceHistory(
    string FoodId,
    DateOnly EffectiveDate,
    double PricePer100g
);

// 템플릿 가격 이력 정보 (브랜드별)
public sealed record TemplatePriceHistory(
    string TemplateId,
    string Brand,           // 브랜드명 (예: "CJ", "동원", "이마트 노브랜드")
    DateOnly EffectiveDate, // 가격 적용 날짜
    string WeightUnit,      // 단위 (예: "1kg", "500g", "2kg")
    double Price            // 해당 단위의 가격
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
    string Type,       // 구분: "식재료" 또는 "요리"
    string Category,   // 분류: 채소, 과일, 육류, 곡물 등
    string? Brand,     // 브랜드
    string WeightUnit, // 단위 (예: 100g, 500g, 1kg)
    double TotalPrice, // 총 가격
    double Kcal,
    double MoistureG,  // 수분
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
    double CopperMg,
    double ManganeseMg,
    double SeleniumUg,
    double MolybdenumUg,
    double IodineUg,
    double VitaminAUg,
    double VitaminCMg,
    double VitaminDUg,
    double VitaminEMg,
    double VitaminKUg,
    double VitaminB1Mg,
    double VitaminB2Mg,
    double VitaminB3Mg,
    double VitaminB5Mg,
    double VitaminB6Mg,
    double VitaminB7Ug,
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

// 템플릿 가격 이력 저장소
public interface ITemplatePriceHistoryRepository
{
    Task<IReadOnlyList<TemplatePriceHistory>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TemplatePriceHistory>> GetByTemplateIdAsync(string templateId, CancellationToken ct = default);
    Task<IReadOnlyList<TemplatePriceHistory>> GetByTemplateAndBrandAsync(string templateId, string brand, CancellationToken ct = default);
    Task<TemplatePriceHistory?> GetLatestPriceAsync(string templateId, string brand, DateOnly? asOfDate = null, CancellationToken ct = default);
    Task AddAsync(TemplatePriceHistory priceHistory, CancellationToken ct = default);
    Task UpdateAsync(TemplatePriceHistory priceHistory, CancellationToken ct = default);
    Task DeleteAsync(string templateId, string brand, DateOnly effectiveDate, CancellationToken ct = default);
}

// 음식-템플릿 사용 정보 저장소
public interface IFoodTemplateUsageRepository
{
    Task<IReadOnlyList<FoodTemplateUsage>> GetByFoodIdAsync(string foodId, CancellationToken ct = default);
    Task SaveAsync(string foodId, IReadOnlyList<FoodTemplateUsage> usages, CancellationToken ct = default);
    Task DeleteByFoodIdAsync(string foodId, CancellationToken ct = default);
}

// 최적화 설정 (카테고리별 제약)
public sealed record OptimizationSettings(
    string Category,
    int MinCount,
    int MaxCountPerFood
);

// 최적화 설정 저장소
public interface IOptimizationSettingsRepository
{
    Task<IReadOnlyList<OptimizationSettings>> GetAllAsync(CancellationToken ct = default);
    Task SaveAllAsync(IReadOnlyList<OptimizationSettings> settings, CancellationToken ct = default);
}
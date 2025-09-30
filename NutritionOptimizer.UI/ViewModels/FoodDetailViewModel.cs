using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 음식 상세 정보 표시를 위한 ViewModel
public sealed class FoodDetailViewModel
{
    private readonly Food _food;
    private readonly double _multiplier;
    private readonly Dictionary<string, NutrientTarget> _targets;

    public FoodDetailViewModel(Food food, IReadOnlyList<NutrientTarget>? targets = null)
    {
        _food = food;
        _multiplier = CalculateMultiplier(food.WeightUnit);
        
        // 목표치를 Dictionary로 변환 (빠른 검색을 위해)
        _targets = targets?.ToDictionary(t => t.NutrientKey, StringComparer.OrdinalIgnoreCase) 
                   ?? new Dictionary<string, NutrientTarget>();
    }

    // 기본 정보
    public string Name => _food.Name;
    public string Type => _food.Type;
    public string Category => _food.Category;
    public string WeightUnit => _food.WeightUnit;
    public double TotalPrice => _food.TotalPrice;
    public string UnitPriceDisplay => _food.UnitPriceDisplay;
    public string? Notes => _food.Notes;

    // 총 영양성분 (실제 제품 단위 기준)
    public double TotalKcal => Math.Round(_food.Kcal * _multiplier, 2);
    public double TotalMoistureG => Math.Round(_food.MoistureG * _multiplier, 2);
    public double TotalProteinG => Math.Round(_food.ProteinG * _multiplier, 2);
    public double TotalFatG => Math.Round(_food.FatG * _multiplier, 2);
    public double TotalSaturatedFatG => Math.Round(_food.SaturatedFatG * _multiplier, 2);
    public double TotalTransFatG => Math.Round(_food.TransFatG * _multiplier, 2);
    public double TotalCarbsG => Math.Round(_food.CarbsG * _multiplier, 2);
    public double TotalFiberG => Math.Round(_food.FiberG * _multiplier, 2);
    public double TotalSugarG => Math.Round(_food.SugarG * _multiplier, 2);
    public double TotalSodiumMg => Math.Round(_food.SodiumMg * _multiplier, 2);
    public double TotalCholesterolMg => Math.Round(_food.CholesterolMg * _multiplier, 2);
    public double TotalCalciumMg => Math.Round(_food.CalciumMg * _multiplier, 2);
    public double TotalIronMg => Math.Round(_food.IronMg * _multiplier, 2);
    public double TotalMagnesiumMg => Math.Round(_food.MagnesiumMg * _multiplier, 2);
    public double TotalPhosphorusMg => Math.Round(_food.PhosphorusMg * _multiplier, 2);
    public double TotalPotassiumMg => Math.Round(_food.PotassiumMg * _multiplier, 2);
    public double TotalZincMg => Math.Round(_food.ZincMg * _multiplier, 2);
    public double TotalCopperMg => Math.Round(_food.CopperMg * _multiplier, 2);
    public double TotalManganeseMg => Math.Round(_food.ManganeseMg * _multiplier, 2);
    public double TotalSeleniumUg => Math.Round(_food.SeleniumUg * _multiplier, 2);
    public double TotalMolybdenumUg => Math.Round(_food.MolybdenumUg * _multiplier, 2);
    public double TotalIodineUg => Math.Round(_food.IodineUg * _multiplier, 2);
    public double TotalVitaminAUg => Math.Round(_food.VitaminAUg * _multiplier, 2);
    public double TotalVitaminCMg => Math.Round(_food.VitaminCMg * _multiplier, 2);
    public double TotalVitaminDUg => Math.Round(_food.VitaminDUg * _multiplier, 2);
    public double TotalVitaminEMg => Math.Round(_food.VitaminEMg * _multiplier, 2);
    public double TotalVitaminKUg => Math.Round(_food.VitaminKUg * _multiplier, 2);
    public double TotalVitaminB1Mg => Math.Round(_food.VitaminB1Mg * _multiplier, 2);
    public double TotalVitaminB2Mg => Math.Round(_food.VitaminB2Mg * _multiplier, 2);
    public double TotalVitaminB3Mg => Math.Round(_food.VitaminB3Mg * _multiplier, 2);
    public double TotalVitaminB5Mg => Math.Round(_food.VitaminB5Mg * _multiplier, 2);
    public double TotalVitaminB6Mg => Math.Round(_food.VitaminB6Mg * _multiplier, 2);
    public double TotalVitaminB7Ug => Math.Round(_food.VitaminB7Ug * _multiplier, 2);
    public double TotalVitaminB9Ug => Math.Round(_food.VitaminB9Ug * _multiplier, 2);
    public double TotalVitaminB12Ug => Math.Round(_food.VitaminB12Ug * _multiplier, 2);

    // 100g 기준 영양성분
    public double Per100gKcal => _food.Kcal;
    public double Per100gMoistureG => _food.MoistureG;
    public double Per100gProteinG => _food.ProteinG;
    public double Per100gFatG => _food.FatG;
    public double Per100gSaturatedFatG => _food.SaturatedFatG;
    public double Per100gTransFatG => _food.TransFatG;
    public double Per100gCarbsG => _food.CarbsG;
    public double Per100gFiberG => _food.FiberG;
    public double Per100gSugarG => _food.SugarG;
    public double Per100gSodiumMg => _food.SodiumMg;
    public double Per100gCholesterolMg => _food.CholesterolMg;
    public double Per100gCalciumMg => _food.CalciumMg;
    public double Per100gIronMg => _food.IronMg;
    public double Per100gMagnesiumMg => _food.MagnesiumMg;
    public double Per100gPhosphorusMg => _food.PhosphorusMg;
    public double Per100gPotassiumMg => _food.PotassiumMg;
    public double Per100gZincMg => _food.ZincMg;
    public double Per100gCopperMg => _food.CopperMg;
    public double Per100gManganeseMg => _food.ManganeseMg;
    public double Per100gSeleniumUg => _food.SeleniumUg;
    public double Per100gMolybdenumUg => _food.MolybdenumUg;
    public double Per100gIodineUg => _food.IodineUg;
    public double Per100gVitaminAUg => _food.VitaminAUg;
    public double Per100gVitaminCMg => _food.VitaminCMg;
    public double Per100gVitaminDUg => _food.VitaminDUg;
    public double Per100gVitaminEMg => _food.VitaminEMg;
    public double Per100gVitaminKUg => _food.VitaminKUg;
    public double Per100gVitaminB1Mg => _food.VitaminB1Mg;
    public double Per100gVitaminB2Mg => _food.VitaminB2Mg;
    public double Per100gVitaminB3Mg => _food.VitaminB3Mg;
    public double Per100gVitaminB5Mg => _food.VitaminB5Mg;
    public double Per100gVitaminB6Mg => _food.VitaminB6Mg;
    public double Per100gVitaminB7Ug => _food.VitaminB7Ug;
    public double Per100gVitaminB9Ug => _food.VitaminB9Ug;
    public double Per100gVitaminB12Ug => _food.VitaminB12Ug;

    // 목표치 대비 퍼센트 (총량 기준)
    public string PercentKcal => CalculatePercent(TotalKcal, "kcal");
    public string PercentMoistureG => CalculatePercentForWater(TotalMoistureG);
    public string PercentProteinG => CalculatePercent(TotalProteinG, "protein_g");
    public string PercentFatG => CalculatePercent(TotalFatG, "fat_g");
    public string PercentSaturatedFatG => CalculatePercent(TotalSaturatedFatG, "saturated_fat_g");
    public string PercentTransFatG => CalculatePercent(TotalTransFatG, "trans_fat_g");
    public string PercentCarbsG => CalculatePercent(TotalCarbsG, "carbs_g");
    public string PercentFiberG => CalculatePercent(TotalFiberG, "fiber_g");
    public string PercentSugarG => CalculatePercent(TotalSugarG, "sugar_g");
    public string PercentSodiumMg => CalculatePercent(TotalSodiumMg, "sodium_mg");
    public string PercentCholesterolMg => CalculatePercent(TotalCholesterolMg, "cholesterol_mg");
    public string PercentCalciumMg => CalculatePercent(TotalCalciumMg, "calcium_mg");
    public string PercentIronMg => CalculatePercent(TotalIronMg, "iron_mg");
    public string PercentMagnesiumMg => CalculatePercent(TotalMagnesiumMg, "magnesium_mg");
    public string PercentPhosphorusMg => CalculatePercent(TotalPhosphorusMg, "phosphorus_mg");
    public string PercentPotassiumMg => CalculatePercent(TotalPotassiumMg, "potassium_mg");
    public string PercentZincMg => CalculatePercent(TotalZincMg, "zinc_mg");
    public string PercentCopperMg => CalculatePercent(TotalCopperMg, "copper_mg");
    public string PercentManganeseMg => CalculatePercent(TotalManganeseMg, "manganese_mg");
    public string PercentSeleniumUg => CalculatePercent(TotalSeleniumUg, "selenium_ug");
    public string PercentMolybdenumUg => CalculatePercent(TotalMolybdenumUg, "molybdenum_ug");
    public string PercentIodineUg => CalculatePercent(TotalIodineUg, "iodine_ug");
    public string PercentVitaminAUg => CalculatePercent(TotalVitaminAUg, "vitamin_a_ug");
    public string PercentVitaminCMg => CalculatePercent(TotalVitaminCMg, "vitamin_c_mg");
    public string PercentVitaminDUg => CalculatePercent(TotalVitaminDUg, "vitamin_d_ug");
    public string PercentVitaminEMg => CalculatePercent(TotalVitaminEMg, "vitamin_e_mg");
    public string PercentVitaminKUg => CalculatePercent(TotalVitaminKUg, "vitamin_k_ug");
    public string PercentVitaminB1Mg => CalculatePercent(TotalVitaminB1Mg, "vitamin_b1_mg");
    public string PercentVitaminB2Mg => CalculatePercent(TotalVitaminB2Mg, "vitamin_b2_mg");
    public string PercentVitaminB3Mg => CalculatePercent(TotalVitaminB3Mg, "vitamin_b3_mg");
    public string PercentVitaminB5Mg => CalculatePercent(TotalVitaminB5Mg, "vitamin_b5_mg");
    public string PercentVitaminB6Mg => CalculatePercent(TotalVitaminB6Mg, "vitamin_b6_mg");
    public string PercentVitaminB7Ug => CalculatePercent(TotalVitaminB7Ug, "vitamin_b7_ug");
    public string PercentVitaminB9Ug => CalculatePercent(TotalVitaminB9Ug, "vitamin_b9_ug");
    public string PercentVitaminB12Ug => CalculatePercent(TotalVitaminB12Ug, "vitamin_b12_ug");

    // 목표치 대비 퍼센트 (100g 기준)
    public string Per100gPercentKcal => CalculatePercent(Per100gKcal, "kcal");
    public string Per100gPercentMoistureG => CalculatePercentForWater(Per100gMoistureG);
    public string Per100gPercentProteinG => CalculatePercent(Per100gProteinG, "protein_g");
    public string Per100gPercentFatG => CalculatePercent(Per100gFatG, "fat_g");
    public string Per100gPercentSaturatedFatG => CalculatePercent(Per100gSaturatedFatG, "saturated_fat_g");
    public string Per100gPercentTransFatG => CalculatePercent(Per100gTransFatG, "trans_fat_g");
    public string Per100gPercentCarbsG => CalculatePercent(Per100gCarbsG, "carbs_g");
    public string Per100gPercentFiberG => CalculatePercent(Per100gFiberG, "fiber_g");
    public string Per100gPercentSugarG => CalculatePercent(Per100gSugarG, "sugar_g");
    public string Per100gPercentSodiumMg => CalculatePercent(Per100gSodiumMg, "sodium_mg");
    public string Per100gPercentCholesterolMg => CalculatePercent(Per100gCholesterolMg, "cholesterol_mg");
    public string Per100gPercentCalciumMg => CalculatePercent(Per100gCalciumMg, "calcium_mg");
    public string Per100gPercentIronMg => CalculatePercent(Per100gIronMg, "iron_mg");
    public string Per100gPercentMagnesiumMg => CalculatePercent(Per100gMagnesiumMg, "magnesium_mg");
    public string Per100gPercentPhosphorusMg => CalculatePercent(Per100gPhosphorusMg, "phosphorus_mg");
    public string Per100gPercentPotassiumMg => CalculatePercent(Per100gPotassiumMg, "potassium_mg");
    public string Per100gPercentZincMg => CalculatePercent(Per100gZincMg, "zinc_mg");
    public string Per100gPercentCopperMg => CalculatePercent(Per100gCopperMg, "copper_mg");
    public string Per100gPercentManganeseMg => CalculatePercent(Per100gManganeseMg, "manganese_mg");
    public string Per100gPercentSeleniumUg => CalculatePercent(Per100gSeleniumUg, "selenium_ug");
    public string Per100gPercentMolybdenumUg => CalculatePercent(Per100gMolybdenumUg, "molybdenum_ug");
    public string Per100gPercentIodineUg => CalculatePercent(Per100gIodineUg, "iodine_ug");
    public string Per100gPercentVitaminAUg => CalculatePercent(Per100gVitaminAUg, "vitamin_a_ug");
    public string Per100gPercentVitaminCMg => CalculatePercent(Per100gVitaminCMg, "vitamin_c_mg");
    public string Per100gPercentVitaminDUg => CalculatePercent(Per100gVitaminDUg, "vitamin_d_ug");
    public string Per100gPercentVitaminEMg => CalculatePercent(Per100gVitaminEMg, "vitamin_e_mg");
    public string Per100gPercentVitaminKUg => CalculatePercent(Per100gVitaminKUg, "vitamin_k_ug");
    public string Per100gPercentVitaminB1Mg => CalculatePercent(Per100gVitaminB1Mg, "vitamin_b1_mg");
    public string Per100gPercentVitaminB2Mg => CalculatePercent(Per100gVitaminB2Mg, "vitamin_b2_mg");
    public string Per100gPercentVitaminB3Mg => CalculatePercent(Per100gVitaminB3Mg, "vitamin_b3_mg");
    public string Per100gPercentVitaminB5Mg => CalculatePercent(Per100gVitaminB5Mg, "vitamin_b5_mg");
    public string Per100gPercentVitaminB6Mg => CalculatePercent(Per100gVitaminB6Mg, "vitamin_b6_mg");
    public string Per100gPercentVitaminB7Ug => CalculatePercent(Per100gVitaminB7Ug, "vitamin_b7_ug");
    public string Per100gPercentVitaminB9Ug => CalculatePercent(Per100gVitaminB9Ug, "vitamin_b9_ug");
    public string Per100gPercentVitaminB12Ug => CalculatePercent(Per100gVitaminB12Ug, "vitamin_b12_ug");

    // 목표치 대비 퍼센트 계산
    private string CalculatePercent(double value, string nutrientKey)
    {
        if (!_targets.TryGetValue(nutrientKey, out var target))
            return "-";

        // Recommended가 있으면 사용, 없으면 Sufficient 사용
        double? targetValue = target.Recommended ?? target.Sufficient;
        
        if (!targetValue.HasValue || targetValue.Value <= 0)
            return "-";

        double percent = (value / targetValue.Value) * 100;
        return $"{percent:N0}%";
    }

    // 수분 전용 퍼센트 계산 (water_g 또는 water_ml 둘 다 지원)
    private string CalculatePercentForWater(double value)
    {
        // water_g 먼저 확인
        var percent = CalculatePercent(value, "water_g");
        if (percent != "-")
            return percent;
        
        // water_g가 없으면 water_ml 확인 (1ml = 1g)
        return CalculatePercent(value, "water_ml");
    }

    // 단위에서 100g 대비 배수 계산
    private static double CalculateMultiplier(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return 1.0;

        var (quantity, unit) = ParseWeightUnit(weightUnit);
        
        if (unit == "g" || unit == "ml")
        {
            return quantity / 100.0;
        }
        else if (unit == "kg" || unit == "l")
        {
            return quantity * 10.0;
        }
        
        return 1.0;
    }

    private static (double quantity, string unit) ParseWeightUnit(string weightUnit)
    {
        if (string.IsNullOrWhiteSpace(weightUnit))
            return (100, "g");
        
        var numStr = new string(weightUnit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(numStr, out double quantity))
        {
            var unit = weightUnit.Substring(numStr.Length).Trim().ToLower();
            
            if (unit.StartsWith("kg"))
                return (quantity, "kg");
            else if (unit.StartsWith("l") && !unit.StartsWith("lb"))
                return (quantity, "l");
            else if (unit.StartsWith("ml"))
                return (quantity, "ml");
            else
                return (quantity, "g");
        }
        
        return (100, "g");
    }
}

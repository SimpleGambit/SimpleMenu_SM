using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.Optimization;

public sealed class DietOptimizer
{
    // 영양소 우선순위 그룹 정의
    private static readonly HashSet<string> PrimaryNutrients = new(StringComparer.OrdinalIgnoreCase)
    {
        "kcal", "protein_g", "carbs_g", "fiber_g", "calcium_mg", "iron_mg", "magnesium_mg", "phosphorus_mg", "potassium_mg", "vitamin_c_mg"
    };

    private static readonly HashSet<string> SecondNutrients = new(StringComparer.OrdinalIgnoreCase)
    {
        "fat_g", "zinc_mg", "copper_mg",
        "vitamin_a_ug",  "vitamin_e_mg", "vitamin_k_ug", "vitamin_b12_ug",
        "vitamin_b1_mg", "vitamin_b2_mg", "vitamin_b3_mg", "vitamin_b5_mg", "vitamin_b6_mg", "vitamin_b7_ug", "vitamin_b9_ug", 
    };

    private static readonly HashSet<string> thirdNutrients = new(StringComparer.OrdinalIgnoreCase)
    {
        "manganese_mg", "selenium_ug", "molybdenum_ug", "iodine_ug"
    };

    // 우선순위별 가중치
    private const double PrimaryWeight = 100.0;   // 1순위
    private const double SecondWeight = 10.0;    // 2순위
    private const double thirdWeight = 5.0;     // 3순위
    private const double DefaultWeight = 1.0;     // 기타

    // 모든 영양소 선택자 (35개)
    private static readonly IReadOnlyDictionary<string, Func<Food, double>> NutrientSelectors =
    new Dictionary<string, Func<Food, double>>(StringComparer.OrdinalIgnoreCase)
    {
        // 기본 영양소
        ["kcal"] = food => food.Kcal,
        ["moisture_g"] = food => food.MoistureG,
        ["protein_g"] = food => food.ProteinG,
        ["fat_g"] = food => food.FatG,
        ["saturated_fat_g"] = food => food.SaturatedFatG,
        ["trans_fat_g"] = food => food.TransFatG,
        ["carbs_g"] = food => food.CarbsG,
        ["fiber_g"] = food => food.FiberG,
        ["sugar_g"] = food => food.SugarG,
        ["sodium_mg"] = food => food.SodiumMg,
        ["cholesterol_mg"] = food => food.CholesterolMg,
        // 무기질 (10개)
        ["calcium_mg"] = food => food.CalciumMg,
        ["iron_mg"] = food => food.IronMg,
        ["magnesium_mg"] = food => food.MagnesiumMg,
        ["phosphorus_mg"] = food => food.PhosphorusMg,
        ["potassium_mg"] = food => food.PotassiumMg,
        ["zinc_mg"] = food => food.ZincMg,
        ["copper_mg"] = food => food.CopperMg,
        ["manganese_mg"] = food => food.ManganeseMg,
        ["selenium_ug"] = food => food.SeleniumUg,
        ["molybdenum_ug"] = food => food.MolybdenumUg,
        ["iodine_ug"] = food => food.IodineUg,
        // 비타민 (13개)
        ["vitamin_a_ug"] = food => food.VitaminAUg,
        ["vitamin_c_mg"] = food => food.VitaminCMg,
        ["vitamin_d_ug"] = food => food.VitaminDUg,
        ["vitamin_e_mg"] = food => food.VitaminEMg,
        ["vitamin_k_ug"] = food => food.VitaminKUg,
        ["vitamin_b1_mg"] = food => food.VitaminB1Mg,
        ["vitamin_b2_mg"] = food => food.VitaminB2Mg,
        ["vitamin_b3_mg"] = food => food.VitaminB3Mg,
        ["vitamin_b5_mg"] = food => food.VitaminB5Mg,
        ["vitamin_b6_mg"] = food => food.VitaminB6Mg,
        ["vitamin_b7_ug"] = food => food.VitaminB7Ug,
        ["vitamin_b9_ug"] = food => food.VitaminB9Ug,
        ["vitamin_b12_ug"] = food => food.VitaminB12Ug,
    };

    public sealed record OptimizationGoal(
        string Name,
        OptimizationObjective Objective
    );

    public enum OptimizationObjective
    {
        MinimizeCost,              // 비용만 최소화
        BalancedNutrition,         // 영양 균형 우선, 비용은 부차적
        CostWithNutrition,         // 비용 우선, 영양은 제약으로만
        CustomWeighted             // 사용자 정의 가중치
    }

    public sealed record Result(
        IReadOnlyList<(Food Food, double Amount100g)> Picks,
        double TotalCost,
        IReadOnlyDictionary<string, (double value, double? target, double? min, double? max, double achievementRate)> NutrientSummary,
        bool Feasible,
        string? InfeasibleReason,
        double NutritionScore  // 영양 달성도 점수 (0-100)
    );

    // 카테고리별 제약 설정
    public sealed record CategoryConstraint(string Category, int MinCount, int MaxCountPerFood);

    // 최적화 실행 (기본: 균형 잡힌 영양 + 비용 최소화)
    public Result Optimize(
        IReadOnlyList<Food> foods, 
        IReadOnlyList<NutrientTarget> targets, 
        double stepG = 50,
        OptimizationObjective objective = OptimizationObjective.BalancedNutrition,
        double costWeight = 1.0,
        double nutritionWeight = 10.0,
        int defaultMaxItemsPerFood = 3,
        IReadOnlyList<CategoryConstraint>? categoryConstraints = null)
    {
        if (stepG <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepG), "stepG must be positive.");
        }

        var solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING")
                     ?? throw new InvalidOperationException("CBC solver not available");

        // 변수 생성: 각 음식의 선택 개수 (0 ~ maxCount)
        var variables = new Dictionary<string, Variable>(foods.Count);
        var foodMultipliers = new Dictionary<string, double>(foods.Count); // 각 음식의 100g 대비 배수
        
        // 카테고리별 최대 횟수 맵 생성
        var categoryMaxCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (categoryConstraints != null)
        {
            foreach (var constraint in categoryConstraints)
            {
                categoryMaxCountMap[constraint.Category] = constraint.MaxCountPerFood;
            }
        }
        
        foreach (var food in foods)
        {
            // 음식의 실제 단위를 100g 기준 배수로 계산
            double multiplier = CalculateMultiplier(food.WeightUnit);
            foodMultipliers[food.Id] = multiplier;
            
            // 카테고리에 해당하는 최대 횟수 찾기
            int maxCount = defaultMaxItemsPerFood;
            if (!string.IsNullOrEmpty(food.Category) && categoryMaxCountMap.TryGetValue(food.Category, out var categoryMaxCount))
            {
                maxCount = categoryMaxCount;
            }
            
            // 변수: 0 ~ maxCount 개
            variables[food.Id] = solver.MakeIntVar(0, maxCount, $"x_{food.Id}");
        }

        // 목표 함수 설정
        var objectiveFunc = solver.Objective();

        // 비용 최소화 부분 (음식 단위 기준)
        foreach (var food in foods)
        {
            double costCoefficient = food.PricePer100g * foodMultipliers[food.Id] * costWeight;
            objectiveFunc.SetCoefficient(variables[food.Id], costCoefficient);
        }

        // 영양 목표 달성을 위한 deviation 변수 추가
        Dictionary<string, Variable> deviationVarsPos = new();
        Dictionary<string, Variable> deviationVarsNeg = new();

        if (objective == OptimizationObjective.BalancedNutrition || objective == OptimizationObjective.CustomWeighted)
        {
            foreach (var target in targets)
            {
                if (!NutrientSelectors.ContainsKey(target.NutrientKey))
                    continue;

                // 목표 값 결정 (Recommended > (Min+Max)/2)
                double? targetValue = target.Recommended;
                if (targetValue == null && target.Min.HasValue && target.Max.HasValue)
                {
                    targetValue = (target.Min.Value + target.Max.Value) / 2.0;
                }
                else if (targetValue == null && target.Min.HasValue)
                {
                    targetValue = target.Min.Value;
                }
                else if (targetValue == null && target.Max.HasValue)
                {
                    targetValue = target.Max.Value;
                }

                if (!targetValue.HasValue)
                    continue;

                // deviation 변수 생성
                var devPos = solver.MakeNumVar(0, double.PositiveInfinity, $"dev_pos_{target.NutrientKey}");
                var devNeg = solver.MakeNumVar(0, double.PositiveInfinity, $"dev_neg_{target.NutrientKey}");
                deviationVarsPos[target.NutrientKey] = devPos;
                deviationVarsNeg[target.NutrientKey] = devNeg;

                // 제약: actual - target = devPos - devNeg
                var deviationConstraint = solver.MakeConstraint(targetValue.Value, targetValue.Value, $"deviation_{target.NutrientKey}");
                var selector = NutrientSelectors[target.NutrientKey];
                foreach (var food in foods)
                {
                    double coefficient = selector(food) * foodMultipliers[food.Id];
                    deviationConstraint.SetCoefficient(variables[food.Id], coefficient);
                }
                deviationConstraint.SetCoefficient(devPos, -1.0);
                deviationConstraint.SetCoefficient(devNeg, 1.0);

                // 목적 함수에 deviation 추가 (우선순위별 가중치 적용)
                double priorityWeight = GetNutrientPriorityWeight(target.NutrientKey);
                double normalizedWeight = (nutritionWeight * priorityWeight) / (targetValue.Value + 1e-6);
                objectiveFunc.SetCoefficient(devPos, normalizedWeight);
                objectiveFunc.SetCoefficient(devNeg, normalizedWeight);
            }
        }

        objectiveFunc.SetMinimization();

        // 기본 제약 조건: Min/Max 범위
        void AddBound(string key, Func<Food, double> selector, double? min, double? max)
        {
            if (min is not null)
            {
                var constraint = solver.MakeConstraint(min.Value, double.PositiveInfinity, $"min_{key}");
                foreach (var food in foods)
                {
                    constraint.SetCoefficient(variables[food.Id], selector(food) * foodMultipliers[food.Id]);
                }
            }

            if (max is not null)
            {
                var constraint = solver.MakeConstraint(double.NegativeInfinity, max.Value, $"max_{key}");
                foreach (var food in foods)
                {
                    constraint.SetCoefficient(variables[food.Id], selector(food) * foodMultipliers[food.Id]);
                }
            }
        }

        var unknownTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            if (NutrientSelectors.TryGetValue(target.NutrientKey, out var selector))
            {
                // BalancedNutrition 목표일 때는 Min/Max를 조금 느슨하게 적용
                if (objective == OptimizationObjective.BalancedNutrition)
                {
                    // Min은 90%, Max는 110%로 여유있게
                    double? relaxedMin = target.Min.HasValue ? target.Min.Value * 0.9 : (double?)null;
                    double? relaxedMax = target.Max.HasValue ? target.Max.Value * 1.1 : (double?)null;
                    AddBound(target.NutrientKey, selector, relaxedMin, relaxedMax);
                }
                else
                {
                    AddBound(target.NutrientKey, selector, target.Min, target.Max);
                }
            }
            else
            {
                unknownTargets.Add(target.NutrientKey);
            }
        }

        // 카테고리별 최소 개수 제약
        if (categoryConstraints != null && categoryConstraints.Count > 0)
        {
            foreach (var catConstraint in categoryConstraints)
            {
                var foodsInCategory = foods.Where(f => 
                    string.Equals(f.Category, catConstraint.Category, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (foodsInCategory.Count > 0)
                {
                    // 해당 카테고리에서 최소 N개 이상 선택
                    var constraint = solver.MakeConstraint(catConstraint.MinCount, double.PositiveInfinity, 
                        $"category_min_{catConstraint.Category}");
                    
                    foreach (var food in foodsInCategory)
                    {
                        constraint.SetCoefficient(variables[food.Id], 1.0);
                    }
                }
            }
        }

        if (unknownTargets.Count > 0)
        {
            Console.Error.WriteLine($"Unknown nutrient targets: {string.Join(", ", unknownTargets.OrderBy(k => k))}");
        }

        // 타임아웃 설정 (30초)
        solver.SetTimeLimit(30000);

        var status = solver.Solve();
        if (status is not Solver.ResultStatus.OPTIMAL and not Solver.ResultStatus.FEASIBLE)
        {
            return new Result(
               Array.Empty<(Food, double)>(),
               0,
               new Dictionary<string, (double, double?, double?, double?, double)>(),
               false,
               $"Solver status: {status}",
               0
           );
        }

        // 결과 추출
        var picks = new List<(Food Food, double Amount100g)>();
        foreach (var food in foods)
        {
            int count = (int)Math.Round(variables[food.Id].SolutionValue());
            if (count > 0)
            {
                double amount100g = count * foodMultipliers[food.Id];
                picks.Add((food, amount100g));
            }
        }

        var totalCost = picks.Sum(pick => pick.Food.PricePer100g * pick.Amount100g);
        var (summary, nutritionScore) = BuildSummary(picks, targets, unknownTargets);

        return new Result(picks, totalCost, summary, true, null, nutritionScore);
    }

    // 음식 단위를 100g 기준 배수로 계산
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
            else if (unit.StartsWith("l") && !unit.StartsWith("lb"))
                return (quantity, "l");
            else if (unit.StartsWith("ml"))
                return (quantity, "ml");
            else // 기본은 g
                return (quantity, "g");
        }
        
        return (100, "g"); // 기본값
    }

    // 영양소 우선순위별 가중치 반환
    private static double GetNutrientPriorityWeight(string nutrientKey)
    {
        if (PrimaryNutrients.Contains(nutrientKey))
            return PrimaryWeight;
        
        if (SecondNutrients.Contains(nutrientKey))
            return SecondWeight;
        
        if (thirdNutrients.Contains(nutrientKey))
            return thirdWeight;
        
        return DefaultWeight;
    }

    private static (IReadOnlyDictionary<string, (double value, double? target, double? min, double? max, double achievementRate)>, double nutritionScore) BuildSummary(
        IEnumerable<(Food Food, double Amount100g)> picks,
        IEnumerable<NutrientTarget> targets,
        IEnumerable<string> unknownTargets)
    {
        var totalByKey = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // 각 영양소의 총합 계산
        foreach (var (key, selector) in NutrientSelectors)
        {
            double total = picks.Sum(pick => selector(pick.Food) * pick.Amount100g);
            totalByKey[key] = total;
        }

        var summary = new Dictionary<string, (double, double?, double?, double?, double)>(StringComparer.OrdinalIgnoreCase);
        var targetList = targets.ToList();
        var weightedScores = new List<(double score, double weight)>();

        foreach (var target in targetList)
        {
            if (!NutrientSelectors.ContainsKey(target.NutrientKey))
                continue;

            totalByKey.TryGetValue(target.NutrientKey, out var actualValue);

            // 목표 값 결정
            double? targetValue = target.Recommended;
            if (targetValue == null && target.Min.HasValue && target.Max.HasValue)
            {
                targetValue = (target.Min.Value + target.Max.Value) / 2.0;
            }
            else if (targetValue == null && target.Min.HasValue)
            {
                targetValue = target.Min.Value;
            }
            else if (targetValue == null && target.Max.HasValue)
            {
                targetValue = target.Max.Value;
            }

            // 달성률 계산 (0-200%, 100%가 최적)
            double achievementRate = 0;
            if (targetValue.HasValue && targetValue.Value > 0)
            {
                achievementRate = (actualValue / targetValue.Value) * 100.0;
                
                // 달성률 점수 계산
                double score;
                if (achievementRate >= 80 && achievementRate <= 120)
                {
                    score = 100;
                }
                else if (achievementRate < 80)
                {
                    score = Math.Max(0, achievementRate / 0.8);
                }
                else // > 120%
                {
                    score = Math.Max(0, 100 - (achievementRate - 120) / 0.8);
                }
                
                // 우선순위별 가중치 적용
                double priorityWeight = GetNutrientPriorityWeight(target.NutrientKey);
                weightedScores.Add((score, priorityWeight));
            }

            summary[target.NutrientKey] = (actualValue, targetValue, target.Min, target.Max, achievementRate);
        }

        // 전체 영양소 추가 (목표 없는 것들)
        foreach (var (key, value) in totalByKey)
        {
            if (!summary.ContainsKey(key))
            {
                summary[key] = (value, null, null, null, 0);
            }
        }

        // 알 수 없는 목표 추가
        foreach (var key in unknownTargets)
        {
            if (!summary.ContainsKey(key))
            {
                var target = targetList.FirstOrDefault(t => string.Equals(t.NutrientKey, key, StringComparison.OrdinalIgnoreCase));
                summary[key] = (double.NaN, target?.Recommended, target?.Min, target?.Max, 0);
            }
        }

        // 전체 영양 점수 계산 (우선순위별 가중 평균)
        double nutritionScore = 0;
        if (weightedScores.Count > 0)
        {
            double totalWeightedScore = weightedScores.Sum(ws => ws.score * ws.weight);
            double totalWeight = weightedScores.Sum(ws => ws.weight);
            nutritionScore = totalWeightedScore / totalWeight;
        }

        return (summary, nutritionScore);
    }
}

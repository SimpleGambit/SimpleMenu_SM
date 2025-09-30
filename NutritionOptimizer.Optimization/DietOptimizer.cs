using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.Optimization;

public sealed class DietOptimizer
{
    private static readonly IReadOnlyDictionary<string, Func<Food, double>> NutrientSelectors =
    new Dictionary<string, Func<Food, double>>(StringComparer.OrdinalIgnoreCase)
    {
        ["kcal"] = food => food.Kcal,
        ["protein_g"] = food => food.ProteinG,
        ["fat_g"] = food => food.FatG,
        ["saturated_fat_g"] = food => food.SaturatedFatG,
        ["trans_fat_g"] = food => food.TransFatG,
        ["carbs_g"] = food => food.CarbsG,
        ["fiber_g"] = food => food.FiberG,
        ["sugar_g"] = food => food.SugarG,
        ["sodium_mg"] = food => food.SodiumMg,
        ["cholesterol_mg"] = food => food.CholesterolMg,
        // 무기질
        ["calcium_mg"] = food => food.CalciumMg,
        ["iron_mg"] = food => food.IronMg,
        ["magnesium_mg"] = food => food.MagnesiumMg,
        ["phosphorus_mg"] = food => food.PhosphorusMg,
        ["potassium_mg"] = food => food.PotassiumMg,
        ["zinc_mg"] = food => food.ZincMg,
        // 비타민
        ["vitamin_a_ug"] = food => food.VitaminAUg,
        ["vitamin_c_mg"] = food => food.VitaminCMg,
        ["vitamin_d_ug"] = food => food.VitaminDUg,
        ["vitamin_e_mg"] = food => food.VitaminEMg,
        ["vitamin_b1_mg"] = food => food.VitaminB1Mg,
        ["vitamin_b2_mg"] = food => food.VitaminB2Mg,
        ["vitamin_b3_mg"] = food => food.VitaminB3Mg,
        ["vitamin_b6_mg"] = food => food.VitaminB6Mg,
        ["vitamin_b9_ug"] = food => food.VitaminB9Ug,
        ["vitamin_b12_ug"] = food => food.VitaminB12Ug,
    };
    public sealed record Result(
        IReadOnlyList<(Food Food, double Amount100g)> Picks,
        double TotalCost,
        IReadOnlyDictionary<string, (double value, double? min, double? max)> NutrientSummary,
        bool Feasible,
        string? InfeasibleReason
    );

    public Result Optimize(IReadOnlyList<Food> foods, IReadOnlyList<NutrientTarget> targets, double stepG = 50)
    {
        if (stepG <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepG), "stepG must be positive.");
        }

        var solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING")
                     ?? throw new InvalidOperationException("CBC solver not available");

        var variables = new Dictionary<string, Variable>(foods.Count);
        foreach (var food in foods)
        {
            int maxSteps = (int)Math.Round(1000.0 / stepG);
            variables[food.Id] = solver.MakeIntVar(0, maxSteps, $"x_{food.Id}");
        }

        var objective = solver.Objective();
        foreach (var food in foods)
        {
            double coefficient = food.PricePer100g * (stepG / 100.0);
            objective.SetCoefficient(variables[food.Id], coefficient);
        }

        objective.SetMinimization();

        void AddBound(string key, Func<Food, double> selector, double? min, double? max)
        {
            if (min is not null)
            {
                var constraint = solver.MakeConstraint(min.Value, double.PositiveInfinity, $"min_{key}");
                foreach (var food in foods)
                {
                    constraint.SetCoefficient(variables[food.Id], selector(food) * (stepG / 100.0));
                }
            }

            if (max is not null)
            {
                var constraint = solver.MakeConstraint(double.NegativeInfinity, max.Value, $"max_{key}");
                foreach (var food in foods)
                {
                    constraint.SetCoefficient(variables[food.Id], selector(food) * (stepG / 100.0));
                }
            }
        }

        var unknownTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            if (NutrientSelectors.TryGetValue(target.NutrientKey, out var selector))
            {
                AddBound(target.NutrientKey, selector, target.Min, target.Max);
            }
            else
            {
                unknownTargets.Add(target.NutrientKey);
            }
        }

        if (unknownTargets.Count > 0)
        {
            Console.Error.WriteLine($"Unknown nutrient targets: {string.Join(", ", unknownTargets.OrderBy(k => k))}");
        }

        var status = solver.Solve();
        if (status is not Solver.ResultStatus.OPTIMAL and not Solver.ResultStatus.FEASIBLE)
        {
            return new Result(
               Array.Empty<(Food, double)>(),
               0,
               new Dictionary<string, (double, double?, double?)>(),
               false,
               $"Solver status: {status}"
           );
        }

        var picks = new List<(Food Food, double Amount100g)>();
        foreach (var food in foods)
        {
            double grams = variables[food.Id].SolutionValue() * stepG;
            if (grams > 0.0)
            {
                picks.Add((food, grams / 100.0));
            }
        }

        var totalCost = picks.Sum(pick => pick.Food.PricePer100g * pick.Amount100g);
        var summary = BuildSummary(picks, targets, unknownTargets);

        return new Result(picks, totalCost, summary, true, null);
    }

    private static IReadOnlyDictionary<string, (double value, double? min, double? max)> BuildSummary(
        IEnumerable<(Food Food, double Amount100g)> picks,
         IEnumerable<NutrientTarget> targets,
        IEnumerable<string> unknownTargets)
    {
        var totalByKey = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, selector) in NutrientSelectors)
        {
            double total = picks.Sum(pick => selector(pick.Food) * pick.Amount100g);
            totalByKey[key] = total;
        }

        var summary = new Dictionary<string, (double, double?, double?)>(StringComparer.OrdinalIgnoreCase);
        var targetList = targets.ToList();

        foreach (var target in targetList)
        {
            if (NutrientSelectors.ContainsKey(target.NutrientKey) && !summary.ContainsKey(target.NutrientKey))
            {
                totalByKey.TryGetValue(target.NutrientKey, out var value);
                summary[target.NutrientKey] = (value, target.Min, target.Max);
            }
        }

        foreach (var (key, value) in totalByKey)
        {
            if (!summary.ContainsKey(key))
            {
                summary[key] = (value, null, null);
            }
        }

        foreach (var key in unknownTargets)
        {
            if (!summary.ContainsKey(key))
            {
                var target = targetList.FirstOrDefault(t => string.Equals(t.NutrientKey, key, StringComparison.OrdinalIgnoreCase));
                summary[key] = (double.NaN, target?.Min, target?.Max);
            }
        }

        return summary;
    }
}
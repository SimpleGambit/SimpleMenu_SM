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

// 최적화 설정을 관리하는 CSV Repository
public sealed class CsvOptimizationSettingsRepository : IOptimizationSettingsRepository
{
    private readonly string _path;

    public CsvOptimizationSettingsRepository(string path) => _path = path;

    // 모든 설정 조회
    public async Task<IReadOnlyList<OptimizationSettings>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<OptimizationSettings>();

        using var reader = new StreamReader(_path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<OptimizationSettingsRowMap>();

        var settings = new List<OptimizationSettings>();
        await foreach (var row in csv.GetRecordsAsync<OptimizationSettingsRow>().WithCancellation(ct))
        {
            settings.Add(new OptimizationSettings(
                row.Category,
                int.Parse(row.MinCount, CultureInfo.InvariantCulture),
                int.Parse(row.MaxCountPerFood, CultureInfo.InvariantCulture)));
        }

        return settings;
    }

    // 모든 설정 저장
    public async Task SaveAllAsync(IReadOnlyList<OptimizationSettings> settings, CancellationToken ct = default)
    {
        await using var writer = new StreamWriter(_path, false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<OptimizationSettingsRowMap>();

        var rows = settings.Select(s => new OptimizationSettingsRow
        {
            Category = s.Category,
            MinCount = s.MinCount.ToString(CultureInfo.InvariantCulture),
            MaxCountPerFood = s.MaxCountPerFood.ToString(CultureInfo.InvariantCulture)
        });

        await csv.WriteRecordsAsync(rows, ct);
    }

    private sealed class OptimizationSettingsRow
    {
        public string Category { get; init; } = string.Empty;
        public string MinCount { get; init; } = "0";
        public string MaxCountPerFood { get; init; } = "3";
    }

    private sealed class OptimizationSettingsRowMap : ClassMap<OptimizationSettingsRow>
    {
        public OptimizationSettingsRowMap()
        {
            Map(m => m.Category).Name("category");
            Map(m => m.MinCount).Name("min_count");
            Map(m => m.MaxCountPerFood).Name("max_count_per_food");
        }
    }
}

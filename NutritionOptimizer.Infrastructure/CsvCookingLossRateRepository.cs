using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.Infrastructure;

public sealed class CsvCookingLossRateRepository : ICookingLossRateRepository
{
    private readonly string _filePath;
    private List<CookingLossRate> _cache = new();
    private bool _isLoaded;

    public CsvCookingLossRateRepository(string filePath)
    {
        _filePath = filePath;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        await Task.Run(() =>
        {
            if (!File.Exists(_filePath))
            {
                // 파일이 없으면 기본 데이터로 생성
                CreateDefaultFile();
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, config);
            
            csv.Context.RegisterClassMap<CookingLossRateMap>();
            _cache = csv.GetRecords<CookingLossRateRecord>()
                .Select(r => new CookingLossRate(r.CookingMethod, r.NutrientKey, r.RetentionRate))
                .ToList();
        });

        _isLoaded = true;
    }

    private void CreateDefaultFile()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var defaultData = new List<CookingLossRateRecord>
        {
            // 없음 (생식/조리 안함) - 모두 100%
            new("없음", "VitaminC", 100),
            new("없음", "VitaminB1", 100),
            new("없음", "VitaminB2", 100),
            new("없음", "VitaminB3", 100),
            new("없음", "VitaminB5", 100),
            new("없음", "VitaminB6", 100),
            new("없음", "VitaminB9", 100),
            
            // 가열조리 (볶음/찜/구이) - 대략적 평균값
            new("가열조리", "VitaminC", 60),
            new("가열조리", "VitaminB1", 75),
            new("가열조리", "VitaminB2", 80),
            new("가열조리", "VitaminB3", 85),
            new("가열조리", "VitaminB5", 80),
            new("가열조리", "VitaminB6", 75),
            new("가열조리", "VitaminB9", 70),
            
            // 튀김 - 더 높은 손실
            new("튀김", "VitaminC", 50),
            new("튀김", "VitaminB1", 70),
            new("튀김", "VitaminB2", 75),
            new("튀김", "VitaminB3", 80),
            new("튀김", "VitaminB5", 75),
            new("튀김", "VitaminB6", 70),
            new("튀김", "VitaminB9", 65),
        };

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var writer = new StreamWriter(_filePath);
        using var csv = new CsvWriter(writer, config);
        csv.Context.RegisterClassMap<CookingLossRateMap>();
        csv.WriteRecords(defaultData);
    }

    public async Task<IEnumerable<CookingLossRate>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _cache;
    }

    public async Task<IEnumerable<CookingLossRate>> GetByCookingMethodAsync(string cookingMethod)
    {
        await EnsureLoadedAsync();
        return _cache.Where(r => r.CookingMethod == cookingMethod);
    }

    public async Task<double> GetRetentionRateAsync(string cookingMethod, string nutrientKey)
    {
        await EnsureLoadedAsync();
        var rate = _cache.FirstOrDefault(r => 
            r.CookingMethod == cookingMethod && r.NutrientKey == nutrientKey);
        
        // 데이터가 없으면 100% (손실 없음)
        return rate?.RetentionRate ?? 100.0;
    }
    
    public double GetRetentionRateSync(string cookingMethod, string nutrientKey)
    {
        // 동기적 로드 (UI 데드락 방지)
        EnsureLoadedSync();
        var rate = _cache.FirstOrDefault(r => 
            r.CookingMethod == cookingMethod && r.NutrientKey == nutrientKey);
        
        // 데이터가 없으면 100% (손실 없음)
        return rate?.RetentionRate ?? 100.0;
    }
    
    private void EnsureLoadedSync()
    {
        if (_isLoaded) return;

        if (!File.Exists(_filePath))
        {
            // 파일이 없으면 기본 데이터로 생성
            CreateDefaultFile();
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, config);
        
        csv.Context.RegisterClassMap<CookingLossRateMap>();
        _cache = csv.GetRecords<CookingLossRateRecord>()
            .Select(r => new CookingLossRate(r.CookingMethod, r.NutrientKey, r.RetentionRate))
            .ToList();

        _isLoaded = true;
    }

    // CSV 매핑용 내부 클래스
    private sealed class CookingLossRateRecord
    {
        public string CookingMethod { get; set; } = string.Empty;
        public string NutrientKey { get; set; } = string.Empty;
        public double RetentionRate { get; set; }

        public CookingLossRateRecord() { }

        public CookingLossRateRecord(string cookingMethod, string nutrientKey, double retentionRate)
        {
            CookingMethod = cookingMethod;
            NutrientKey = nutrientKey;
            RetentionRate = retentionRate;
        }
    }

    private sealed class CookingLossRateMap : ClassMap<CookingLossRateRecord>
    {
        public CookingLossRateMap()
        {
            Map(m => m.CookingMethod).Name("조리방법");
            Map(m => m.NutrientKey).Name("영양소");
            Map(m => m.RetentionRate).Name("잔존률");
        }
    }
}

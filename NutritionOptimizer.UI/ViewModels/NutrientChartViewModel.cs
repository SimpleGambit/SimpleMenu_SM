using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NutritionOptimizer.UI.ViewModels;

// 차트 항목 하나를 나타내는 ViewModel
public sealed partial class ChartItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string nutrientName = string.Empty;

    [ObservableProperty]
    private double actualValue;

    [ObservableProperty]
    private string actualValueText = string.Empty;

    [ObservableProperty]
    private double? minTarget;

    [ObservableProperty]
    private double? maxTarget;

    [ObservableProperty]
    private string targetRangeText = string.Empty;

    [ObservableProperty]
    private double percentageValue; // 0~100 이상

    [ObservableProperty]
    private string percentageText = string.Empty;

    [ObservableProperty]
    private Brush barColor = Brushes.Green;

    [ObservableProperty]
    private string barWidthStar = "0*"; // Grid의 ColumnDefinition Width용

    // 생성자: 영양소 이름, 실제값, 최소/최대 목표치
    public ChartItemViewModel(string nutrientName, double actualValue, double? minTarget, double? maxTarget)
    {
        NutrientName = nutrientName;
        ActualValue = actualValue;
        MinTarget = minTarget;
        MaxTarget = maxTarget;

        CalculateChart();
    }

    // 차트 계산 로직
    private void CalculateChart()
    {
        // 실제값 텍스트 포맷팅
        ActualValueText = ActualValue >= 1000 
            ? $"{ActualValue:N0}" 
            : $"{ActualValue:N1}";

        // 목표 범위 텍스트
        if (MinTarget.HasValue && MaxTarget.HasValue)
        {
            TargetRangeText = $"{MinTarget.Value:N0}~{MaxTarget.Value:N0}";
        }
        else if (MinTarget.HasValue)
        {
            TargetRangeText = $"{MinTarget.Value:N0} 이상";
        }
        else if (MaxTarget.HasValue)
        {
            TargetRangeText = $"{MaxTarget.Value:N0} 이하";
        }
        else
        {
            TargetRangeText = "제한 없음";
        }

        // 달성률 계산 (최소값 기준)
        double referenceValue = MinTarget ?? MaxTarget ?? ActualValue;
        if (referenceValue > 0)
        {
            PercentageValue = (ActualValue / referenceValue) * 100.0;
        }
        else
        {
            PercentageValue = 0;
        }

        PercentageText = $"{PercentageValue:N0}%";

        // 막대 너비 (최대 100%로 제한하여 표시)
        double displayPercent = Math.Min(PercentageValue, 100);
        BarWidthStar = $"{displayPercent}*";

        // 색상 결정
        if (MinTarget.HasValue && ActualValue < MinTarget.Value * 0.95) // 5% 여유
        {
            BarColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 주황색 (부족)
        }
        else if (MaxTarget.HasValue && ActualValue > MaxTarget.Value * 1.05) // 5% 여유
        {
            BarColor = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 빨간색 (초과)
        }
        else
        {
            BarColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 녹색 (정상)
        }
    }
}

// 차트 전체를 관리하는 ViewModel
public sealed partial class NutrientChartViewModel : ObservableObject
{
    [ObservableProperty]
    private List<ChartItemViewModel> chartItems = new();

    public NutrientChartViewModel(IReadOnlyDictionary<string, (double value, double? min, double? max)> nutrientSummary)
    {
        // Converter를 사용해서 한글 이름 가져오기
        var converter = new Converters.NutrientKeyToKoreanConverter();

        ChartItems = nutrientSummary
            .Select(kv =>
            {
                string koreanName = converter.Convert(kv.Key, typeof(string), null!, System.Globalization.CultureInfo.CurrentCulture) as string ?? kv.Key;
                return new ChartItemViewModel(koreanName, kv.Value.value, kv.Value.min, kv.Value.max);
            })
            .OrderByDescending(item => item.PercentageValue) // 달성률 높은 순으로 정렬
            .ToList();
    }
}

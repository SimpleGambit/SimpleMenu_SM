using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NutritionOptimizer.UI.ViewModels;

// 식단 저장을 위한 ViewModel
public sealed partial class SaveDietViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    // 유효성 검사
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }
}

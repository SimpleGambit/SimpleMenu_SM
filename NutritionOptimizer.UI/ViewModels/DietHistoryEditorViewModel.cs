using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NutritionOptimizer.Domain;

namespace NutritionOptimizer.UI.ViewModels;

// 식단 히스토리 추가/수정을 위한 ViewModel
public sealed partial class DietHistoryEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private DateOnly date = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private string? selectedSavedDietId;

    [ObservableProperty]
    private string? selectedSavedDietName;

    [ObservableProperty]
    private string mealType = "아침";

    [ObservableProperty]
    private string notes = string.Empty;

    public List<string> MealTypes { get; } = new() { "아침", "점심", "저녁", "간식", "기타" };

    public bool IsEditMode { get; }

    // 새 히스토리 추가
    public DietHistoryEditorViewModel()
    {
        IsEditMode = false;
    }

    // 기존 히스토리 수정
    public DietHistoryEditorViewModel(DietHistory history)
    {
        IsEditMode = true;
        Date = history.Date;
        SelectedSavedDietId = history.SavedDietId;
        SelectedSavedDietName = history.SavedDietName;
        MealType = history.MealType;
        Notes = history.Notes;
    }

    // 저장된 식단 설정
    public void SetSavedDiet(SavedDiet? diet)
    {
        if (diet != null)
        {
            SelectedSavedDietId = diet.Id;
            SelectedSavedDietName = diet.Name;
        }
    }

    // DietHistory 객체로 변환
    public DietHistory ToHistory(string id)
    {
        return new DietHistory(
            id,
            Date,
            SelectedSavedDietId,
            SelectedSavedDietName,
            MealType,
            Notes
        );
    }
}

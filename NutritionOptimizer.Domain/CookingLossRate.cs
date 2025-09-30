namespace NutritionOptimizer.Domain;

/// <summary>
/// 조리 방법에 따른 영양소 손실률 (잔존률 %)
/// 예: 비타민C, 볶음, 60 -> 볶음 시 비타민C가 60%만 남음
/// </summary>
public sealed record CookingLossRate(
    string CookingMethod,  // 조리 방법 (예: "없음", "가열조리", "튀김")
    string NutrientKey,    // 영양소 키 (예: "VitaminC", "VitaminB1")
    double RetentionRate   // 잔존률 (0~100, 예: 60이면 60%만 남음)
);

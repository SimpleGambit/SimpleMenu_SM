using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutritionOptimizer.Domain;

/// <summary>
/// 조리 손실률 데이터 저장소 인터페이스
/// </summary>
public interface ICookingLossRateRepository
{
    /// <summary>
    /// 모든 조리 손실률 데이터 가져오기
    /// </summary>
    Task<IEnumerable<CookingLossRate>> GetAllAsync();
    
    /// <summary>
    /// 특정 조리 방법의 손실률 데이터 가져오기
    /// </summary>
    Task<IEnumerable<CookingLossRate>> GetByCookingMethodAsync(string cookingMethod);
    
    /// <summary>
    /// 특정 조리 방법과 영양소의 잔존률 가져오기
    /// </summary>
    Task<double> GetRetentionRateAsync(string cookingMethod, string nutrientKey);
    
    /// <summary>
    /// 특정 조리 방법과 영양소의 잔존률 가져오기 (동기 버전 - UI 데드락 방지용)
    /// </summary>
    double GetRetentionRateSync(string cookingMethod, string nutrientKey);
}

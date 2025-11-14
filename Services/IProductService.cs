using PXPayBackend.Models;

namespace PXPayBackend.Services;

/// <summary>
/// 商品服務介面（Interface）
/// WHY 要用 Interface？
/// - 方便測試（可以用 Mock）
/// - 鬆耦合（Controller 不直接依賴實作）
/// - 符合 SOLID 原則的 D（依賴反轉原則）
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 查詢福利熊庫存（有 Cache）
    /// </summary>
    Task<int> GetStockAsync();
    
    /// <summary>
    /// 查詢福利熊庫存（沒有 Cache）
    /// </summary>
    Task<int> GetStockNoCacheAsync();
    
    /// <summary>
    /// 初始化測試資料
    /// </summary>
    Task<bool> InitTestDataAsync();



   Task<bool> CreateTestProductsAsync();
    //加查詢db
    Task<List<Product>> GetAllProductsAsync();
    Task<int> GetProductCountAsync();
    Task<List<Product>> FindProductsByNameAsync(string name);
    Task<List<Product>> FindProductsByNameStartsWithAsync(string name);
    
    /// <summary>
    /// 使用 Redis 快取的搜尋方法
    /// </summary>
    Task<List<Product>> FindProductsByNameCachedAsync(string name);
}


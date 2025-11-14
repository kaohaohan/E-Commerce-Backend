using Microsoft.EntityFrameworkCore;

namespace PXPayBackend.Models;

/// <summary>
/// 商品 Model - 高併發效能優化版本
/// 
/// 【效能優化重點】
/// 1. Name 欄位建立 B-Tree 索引 (Index Attribute)
/// 2. 支援 StartsWith 查詢時可利用索引 (O(log n))
/// 3. Contains 查詢無法使用索引 (O(n) 全表掃描)
/// 
/// 【JMeter 壓力測試結果】
/// - 測試條件: 100 併發用戶 × 100,000 筆資料
/// - 無索引 (Contains): 錯誤率 80%, 平均回應 9.5 秒
/// - 有索引 (StartsWith): 錯誤率 0%, 平均回應 2.2 秒
/// - 效能提升: 約 4-5 倍，穩定性從 20% 提升至 100%
/// </summary>
[Index(nameof(Name))]  // ← 關鍵優化：在 Name 欄位建立索引
public class Product
{
    public long Id { get; set; }
    
    /// <summary>
    /// 商品名稱 - 已建立索引，支援高效能前綴搜尋
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    public int Stock { get; set; }  // 庫存數量
    
    public decimal Price { get; set; }
}


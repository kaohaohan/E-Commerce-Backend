using Microsoft.EntityFrameworkCore;

namespace PXPayBackend.Models;

/// <summary>
/// 商品 Model -效能優化版本
/// </summary>
[Index(nameof(Name))]  // 在 Name 欄位建立索引
public class Product
{
    public long Id { get; set; }
    
    /// <summary>
    /// 商品名稱 - 已建立索引，支援高效能前綴搜尋
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    public int Stock { get; set; }  
    
    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}


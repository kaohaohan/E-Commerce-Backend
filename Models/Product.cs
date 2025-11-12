using Microsoft.EntityFrameworkCore;

namespace PXPayBackend.Models;

/// <summary>
/// 商品 Model
/// </summary>
[Index(nameof(Name))]  
public class Product
{
    public long Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Stock { get; set; }  // 庫存數量
    
    public decimal Price { get; set; }
}


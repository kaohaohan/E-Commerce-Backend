using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using PXPayBackend.Data;
using PXPayBackend.Models;

namespace PXPayBackend.Services;

/// <summary>
/// 商品服務實作
/// </summary>
public class ProductService : IProductService
{
    private readonly IMemoryCache _cache;
    private readonly TodoContext _context;
    
    // TODO 任務 1：完成 Constructor（注入 Cache 和 DbContext）
    public ProductService(IMemoryCache cache, TodoContext context)
    {
        _cache = cache;
        _context = context;
    }
    
    // TODO 任務 2：
    // 提示：
    // 1. 使用 await Task.Delay(500) 模擬慢速查詢
    // 2. 使用 _context.Products.Where().FirstOrDefaultAsync()
    // 3. 回傳 product?.Stock ?? 0
    private async Task<int> GetStockFromDatabaseAsync()
    {

        // 模擬資料庫查詢需要時間（InMemory 太快，用 Task.Delay 讓它變慢）
        await Task.Delay(500);
        
        var product = await _context.Products
            .Where(p => p.Name == "福利熊玩偶")
            .FirstOrDefaultAsync();
        
        // 如果是 null 回傳 0
        return product?.Stock ?? 0;
       
    }
    
    // TODO 任務 3：查庫存（有 Cache 的版本）
    public async Task<int> GetStockAsync()
    {
        // 1. 先檢查 Cache 有沒有資料
        // 用key識別資料 
        var cacheKey = "product_stock";
        
        // 2.檢查cache 用 TryGetValue()取值 回傳bool裡有沒有這筆cacheKey 
        // out這個變數會被方法賦值  
        ////沒有cacheKey->執行查找資料庫, 存到cache ->回傳
        if (!_cache.TryGetValue(cacheKey, out int stock))
         {
             // Cache Miss：沒有快取，需要查詢資料庫
            stock = await GetStockFromDatabaseAsync();
        
            // 把資料存入 Cache（設定5 秒後過期）
            //因為cache不會自動更新 
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };
            _cache.Set(cacheKey, stock, cacheOptions);
         }
        
        
        return stock;
    }
    
    // TODO 任務 4：查庫存 沒有 Cache 的版本）
    // 提示：
    // 1. 直接呼叫 GetStockFromDatabaseAsync()
    // 2. 回傳結果
    public async Task<int> GetStockNoCacheAsync()
    {
        
        return await GetStockFromDatabaseAsync();
    }




    public async Task<bool> InitTestDataAsync()
    {
        var exists = await _context.Products.AnyAsync(p => p.Name == "福利熊玩偶");
        if (exists)
        {
            return false;
        }

        var product = new Product
        {
            Name = "福利熊玩偶",
            Stock = 1000,
            Price = 299m
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return true;
    }


    //創假資料到db裡
    public async Task<bool>  CreateTestProductsAsync()  // 建立測試商品
    {
        //  先判斷資料是否已存在
       var exists = await _context.Products.AnyAsync(p => p.Name.Contains("芭樂狗"));
        if (exists) return false;  // 已經建過了，不要重複建
    
    var characters = new List<string>
    {
        "芭樂狗",
        "旺來狗",
        "奇異狗",
        "香蕉狗",
        "蘋狗",
        "福利熊",
        "小福",
        "全聯先生"
    };
    var products = new List<Product>();
    var random = new Random();
    // 沒建過，就建立全部 8 個角色，每個 200 筆
    foreach (var character in characters)  
    {
   for (int i = 1; i <= 12500; i++)     
        {
    products.Add(new Product
    {
        Name = $"{character}玩偶 #{i}",
        Stock = random.Next(50, 500),
        Price = random.Next(199, 999)
    });
        }
       
    }
    await _context.Products.AddRangeAsync(products);
    await _context.SaveChangesAsync();
    return true;
    }
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<int> GetProductCountAsync()
    {
        return await _context.Products.CountAsync();
    }
    
    // 從10萬筆資料 找所有符合的產品 ex: 查找全聯先生
    //沒index情況會回傳幾秒?
    public async Task<List<Product>> FindProductsByNameAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(name))
            .ToListAsync();
           
    }

    // 使用 StartsWith 可以利用索引
    public async Task<List<Product>> FindProductsByNameStartsWithAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.StartsWith(name))
            .ToListAsync();
    }
}

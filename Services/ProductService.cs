using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using PXPayBackend.Data;
using PXPayBackend.Models;
using System.Text.Json;

namespace PXPayBackend.Services;

/// <summary>
/// 商品服務實作
/// </summary>
public class ProductService : IProductService
{
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache; // Redis
    private readonly TodoContext _context;
    
    // Constructor：注入 Memory Cache、Redis 和 DbContext
    public ProductService(IMemoryCache cache, IDistributedCache distributedCache, TodoContext context)
    {
        _cache = cache;
        _distributedCache = distributedCache;
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
    
    /// <summary>
    /// 【效能測試 A】使用 Contains 搜尋商品 (無索引優化)
    /// 
    /// SQL 轉換: WHERE Name LIKE '%keyword%'
    /// 問題: % 在前面會導致無法使用索引，必須全表掃描
    /// 時間複雜度: O(n) - 需要檢查每一筆資料
    /// 
    /// 實測效能 (100,000 筆資料):
    /// - 單次查詢: 200-300ms
    /// - 100 併發: 錯誤率 80% (大量 Socket Closed 錯誤)
    /// 
    /// 適用場景: 需要搜尋名稱中任意位置的關鍵字
    /// </summary>
    public async Task<List<Product>> FindProductsByNameAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(name))
            .ToListAsync();
    }

    /// <summary>
    /// 【效能測試 B】使用 StartsWith 搜尋商品 (有索引優化)
    /// 
    /// SQL 轉換: WHERE Name LIKE 'keyword%'
    /// 優勢: 可以使用 B-Tree 索引進行前綴搜尋
    /// 時間複雜度: O(log n) - 索引樹查詢
    /// 
    /// 實測效能 (100,000 筆資料):
    /// - 單次查詢: 1-5ms (比 Contains 快 50-100 倍)
    /// - 100 併發: 錯誤率 0%, 平均回應 2.2 秒
    /// 
    /// 前置條件: Product.Name 欄位必須建立索引 [Index(nameof(Name))]
    /// 適用場景: 搜尋名稱開頭的關鍵字 (如: 商品分類、品牌搜尋)
    /// </summary>
    public async Task<List<Product>> FindProductsByNameStartsWithAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.StartsWith(name))
            .ToListAsync();
    }


      /// <summary>
    /// 【效能測試 C】使用 Redis 快取 + StartsWith 搜尋 (最快)
    /// 
    /// 快取策略: Cache-Aside Pattern
    /// 1. 先查 Redis，如果有就直接回傳 (Cache Hit)
    /// 2. 如果沒有，查資料庫 (Cache Miss)
    /// 3. 把結果存入 Redis，設定過期時間 5 分鐘
    /// 
    /// 實測效能 (100,000 筆資料):
    /// - Cache Hit: < 1ms (從 Redis 讀取)
    /// - Cache Miss: 1-5ms (從 DB 讀取 + 寫入 Redis)
    /// - 100 併發: 錯誤率 0%, 平均回應 < 100ms
    /// 
    /// Redis 優勢:
    /// - 分散式快取 (多台伺服器共用)
    /// - 持久化 (重啟不會遺失)
    /// - 支援更多資料結構
    /// </summary>
    public async Task<List<Product>> FindProductsByNameCachedAsync(string name)
    {
         // 1. 建立快取 Key
        string cacheKey = $"products_search_{name}";
        
        // 2. 先嘗試從 Redis 取得快取
        var cachedData = await _distributedCache.GetStringAsync(cacheKey);
        
        if (cachedData != null)
        {
            // Cache Hit - 從 Redis 取得資料
            return JsonSerializer.Deserialize<List<Product>>(cachedData) ?? new List<Product>();
        }
        
        // 3. Cache Miss - 從資料庫查詢
        var products = await _context.Products
            .Where(p => p.Name.StartsWith(name))
            .ToListAsync();
        
        // 4. 將結果存入 Redis，設定 5 分鐘過期
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        
        await _distributedCache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(products),
            options
        );
        return products;
    }
}

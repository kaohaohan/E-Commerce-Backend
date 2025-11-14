using Microsoft.AspNetCore.Mvc;
using PXPayBackend.Services;

namespace PXPayBackend.Controllers;

[ApiController]
[Route("api/[controller]")]

public class ProductsController : ControllerBase
{
   private readonly IProductService _productService;

       public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock()
    {
        var stock = await _productService.GetStockAsync();
        return Ok(new { stock = stock }); 
    }

    [HttpGet("stock/no-cache")]
    public async Task<IActionResult> GetStockNoCache()
    {
        // 呼叫 GetStockNoCacheAsync()
         var stock = await _productService.GetStockNoCacheAsync();
        return Ok(new { stock = stock }); 
    }   

    [HttpPost("init")]
    public async Task<IActionResult> InitTestData()
    {
        // Hint 1: 呼叫 InitTestDataAsync()，回傳 bool記得加 await
        var success = await _productService.InitTestDataAsync();

        // Hint 2: 如果 true，回傳「測試資料已建立」
        if(success){
            return Ok(new { message = "成功" });
        }
        return Ok(new { message = "測試資料已存在" });
        // Hint 3: 如果 false，回傳「測試資料已存在」
    }
    [HttpPost("create-test-products")]
    public async Task<IActionResult> CreateTestProducts()
    {
        var success = await _productService.CreateTestProductsAsync();
        
        if (success)
        {
            return Ok(new { message = "成功建立 1600 筆測試資料！" });
        }
        return Ok(new { message = "測試資料已存在" });
    }
    [HttpGet("count")]
public async Task<IActionResult> GetCount()
{
    var count = await _productService.GetProductCountAsync();
    return Ok(new { 總數量 = count });
}

[HttpGet("all")]
public async Task<IActionResult> GetAll()
{
    var products = await _productService.GetAllProductsAsync();
    return Ok(products);
}



/// <summary>
/// 【效能測試 A】使用 Contains 搜尋商品名稱
/// - SQL: WHERE Name LIKE '%keyword%' (無法使用索引)
/// - 時間複雜度: O(n) - 全表掃描
/// - 適用場景: 需要模糊搜尋任意位置的關鍵字
/// - 效能: 100,000 筆資料約 200-300ms
/// </summary>
[HttpGet("search/{name}")]
public async Task<IActionResult> SearchByName(string name)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var products = await _productService.FindProductsByNameAsync(name);
    stopwatch.Stop();
    
    return Ok(new { 
        products = products,
        數量 = products.Count,
        查詢時間_毫秒 = stopwatch.ElapsedMilliseconds,
        說明 = "使用 Contains - 無法利用索引 (全表掃描)"
    });
}

/// <summary>
/// 【效能測試 B】使用 StartsWith 搜尋商品名稱 + 索引優化
/// - SQL: WHERE Name LIKE 'keyword%' (可使用 B-Tree 索引)
/// - 時間複雜度: O(log n) - 索引查詢
/// - 索引: Product.Name 欄位已建立索引 (見 Product.cs)
/// - 效能: 100,000 筆資料約 1-5ms (比 Contains 快 50-100 倍)
/// - JMeter 測試結果: 100 併發下錯誤率從 80% 降至 0%
/// </summary>
[HttpGet("search-starts-with/{name}")]
public async Task<IActionResult> SearchByNameStartsWith(string name)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var products = await _productService.FindProductsByNameStartsWithAsync(name);
    stopwatch.Stop();
    
    return Ok(new { 
        products = products,
        數量 = products.Count,
        查詢時間_毫秒 = stopwatch.ElapsedMilliseconds,
        說明 = "使用 StartsWith - 可利用索引 (B-Tree 查詢)"
    });
}
}
using Microsoft.EntityFrameworkCore;
using PXPayBackend.Data;
using PXPayBackend.Services;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 注册 DbContext (IOC/DI 的核心！)
// 当 Controller 需要 TodoContext 时，.NET 会自动注入
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册 Memory Cache（用於高流量優化）
builder.Services.AddMemoryCache();

// 注册 Redis 分散式快取
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ECommerceCache_"; // 快取 Key 的前綴
});

// 注册 Rate Limiting（限流）
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 10,
            Period = "1m"
        }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


// 注册 Service 層（業務邏輯）
// Scoped：每個 HTTP 請求會建立一個新的實例
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // 暫時關閉 HTTPS 重定向，方便測試

// 啟用 Rate Limiting 中間件
// app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

// 啟動時自動建立測試資料
using (var scope = app.Services.CreateScope())
{
    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
    await productService.CreateTestProductsAsync();
}

app.Run();

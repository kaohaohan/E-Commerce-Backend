using AspNetCoreRateLimit;
using PXPayBackend.Extensions;
using PXPayBackend.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== 服務註冊（使用 Extension Methods 保持簡潔）=====
builder.Services
    .AddDatabase(builder.Configuration)      // SQL Server + Connection Pool
    .AddCaching(builder.Configuration)       // Redis 分散式快取
    .AddRateLimiting(builder.Configuration)  // API 限流
    .AddCorsPolicy(builder.Configuration)    // CORS 跨域設定
    .AddBusinessServices();                  // 業務邏輯服務

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "E-Commerce API", 
        Version = "v1",
        Description = "高併發 API 優化範例：Database Indexing + Redis Cache + Connection Pool"
    });
});

var app = builder.Build();

// ===== Middleware Pipeline =====

// 全域例外處理（統一錯誤格式）
app.UseGlobalExceptionHandler();

// Swagger（所有環境都開啟，方便 Demo）
app.UseSwagger();
app.UseSwaggerUI();

// CORS（根據環境選擇策略）
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "Production";
app.UseCors(corsPolicy);

// Rate Limiting（透過設定檔控制開關）
var rateLimitEnabled = builder.Configuration.GetValue<bool>("RateLimitingEnabled");
if (rateLimitEnabled)
{
    app.UseIpRateLimiting();
}

app.UseAuthorization();
app.MapControllers();

// 使用 port 5000（監聽所有網絡接口）
app.Run("http://0.0.0.0:5000");

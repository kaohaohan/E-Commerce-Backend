using Microsoft.EntityFrameworkCore;
using PXPayBackend.Data;
using PXPayBackend.Services;
using AspNetCoreRateLimit;

namespace PXPayBackend.Extensions;

/// <summary>
/// 服務註冊擴充方法 - 讓 Program.cs 更簡潔
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// 註冊資料庫服務（SQL Server + Connection Pool）
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // 支援環境變數覆蓋（Production 使用）
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            connectionString = envConnectionString;
        }

        services.AddDbContext<TodoContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    /// <summary>
    /// 註冊 Redis 快取服務
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var redisConnection = configuration.GetConnectionString("Redis");
        
        // 支援環境變數覆蓋
        var envRedis = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
        if (!string.IsNullOrEmpty(envRedis))
        {
            redisConnection = envRedis;
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "ECommerceCache_";
        });

        return services;
    }

    /// <summary>
    /// 註冊 Rate Limiting（可透過設定檔/環境變數開關）
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Limit = 100,     // 每分鐘 100 次請求
                    Period = "1m"
                }
            };
        });

        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    /// <summary>
    /// 註冊 CORS（可配置允許的來源）
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        // 從設定檔讀取允許的 Origins
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() 
                             ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            // Development: 允許所有
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Production: 只允許指定來源
            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// 註冊業務邏輯服務
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}


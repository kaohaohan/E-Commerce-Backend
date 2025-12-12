# E-Commerce Backend API

ASP.NET Core Web API，展示高併發 API 優化技術。

## 技術棧

- .NET 8.0 + ASP.NET Core Web API
- SQL Server + Entity Framework Core
- Redis 分散式快取
- Docker + Docker Compose
- AWS ECS + ECR + Auto Scaling

## 快速開始

### 方式一：Docker Compose（推薦）

一鍵啟動完整環境：

```bash
docker compose up -d
```

包含：API + SQL Server + Redis

### 方式二：手動啟動

1. 複製設定檔並填入連線資訊：

```bash
cp appsettings.example.json appsettings.Development.json
# 編輯 appsettings.Development.json 填入密碼
```

2. 啟動資料庫：

```bash
docker start sqlserver redis
```

3. 啟動應用：

```bash
dotnet run
```

## 環境變數設定

本專案支援透過環境變數覆蓋連線資訊（適用於 Docker/AWS）：

| 變數名稱 | 說明 |
|---------|------|
| `ConnectionStrings__DefaultConnection` | SQL Server 連線字串 |
| `ConnectionStrings__Redis` | Redis 連線字串 |
| `RateLimitingEnabled` | 是否啟用 API 限流 |
| `AllowedOrigins__0` | CORS 允許的來源 |

## 核心功能

### Database Indexing

使用 B-Tree 索引優化查詢：
- `Contains()`: 全表掃描，200-300ms
- `StartsWith()` + Index: 前綴搜尋，1-5ms

### Redis Cache

Cache-Aside Pattern，TTL 5 分鐘。

### Connection Pooling

Min Pool Size=10, Max Pool Size=200

## API 端點

| 端點 | 說明 | 快取 |
|------|------|------|
| `GET /api/products/search/{keyword}` | Contains 搜尋（無優化） | 無 |
| `GET /api/products/search-starts-with/{keyword}` | StartsWith + 索引 | 無 |
| `GET /api/products/search-cached/{keyword}` | Redis 快取版本 | 5 分鐘 |
| `POST /api/products/init` | 初始化測試資料 | - |

## 效能測試

JMeter 壓測結果（100 併發，100,000 筆資料）：

| 版本 | 回應時間 | 錯誤率 |
|------|---------|--------|
| 無索引 | 200-300ms | 80% |
| 有索引 | 1-5ms | 0% |

## 專案結構

```
├── Controllers/       # API 端點
├── Services/         # 業務邏輯
├── Models/           # 資料模型
├── Data/             # DbContext
├── Extensions/       # 服務註冊擴充
├── Middleware/       # 全域例外處理
├── docker-compose.yml
└── Dockerfile
```

## License

MIT

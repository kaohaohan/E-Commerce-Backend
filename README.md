# E-Commerce Backend API

ASP.NET Core Web API 專案，展示高併發 API 優化技術。

## 技術棧

- .NET 8.0 + ASP.NET Core Web API
- SQL Server + Entity Framework Core
- Redis 分散式快取
- Docker 容器化
- AWS ECS + ECR + Auto Scaling

## 快速開始

### 1. 啟動 SQL Server

```bash
docker start sqlserver
```

### 2. 啟動應用

```bash
dotnet restore
dotnet ef database update
dotnet run
```

### 3. 測試

打開 demo.html 查看效能測試 Demo

## 核心功能

### Database Indexing 優化

使用 B-Tree 索引優化查詢效能：
- Contains(): 全表掃描，300ms
- StartsWith() + Index: 前綴搜尋，3ms

實測 100,000 筆資料，100 併發測試，錯誤率 0%。

### Redis Cache

實作 Cache-Aside Pattern，TTL 5 分鐘。

### Connection Pooling

配置資料庫連線池，避免重複建立連線。

## API 端點

### Products API
- `GET /api/products/search/{keyword}` - Contains 搜尋（無優化）
- `GET /api/products/search-starts-with/{keyword}` - StartsWith 搜尋（有索引）
- `GET /api/products/search-cached/{keyword}` - Redis 快取版本
- `POST /api/products/init` - 初始化測試資料

## 效能測試

使用 JMeter 壓測結果（100 併發）：

| 版本 | 回應時間 | 錯誤率 |
|------|---------|--------|
| 無索引 | 200-300ms | 80% |
| 有索引 | 1-5ms | 0% |

## Docker 部署

```bash
docker build -t ecommerce-backend .
docker run -d -p 5000:5000 ecommerce-backend
```

## AWS 雲端部署

本專案支援部署到 AWS，架構包含：

- ECR: 儲存 Docker 映像
- ECS: 運行容器化應用
- Application Load Balancer: 負載均衡
- Auto Scaling: 根據流量自動擴展

預計 11/20 前完成部署。詳細步驟參考：DEPLOYMENT_MANUAL.md

## License

MIT

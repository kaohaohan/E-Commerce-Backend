# 面試演示指南 - 完整流程

## 🎯 演示目標

展示 **Database Index** 和 **Redis Cache** 在高併發下的效能差異

---

## 📋 演示步驟（10 分鐘完整流程）

### Step 1: 啟動 Docker 環境（SQL Server + Redis）

```bash
# 確認 Docker Desktop 已啟動
docker info

# 啟動資料庫和快取服務
cd ~/E-Commerce-Backend
docker-compose up -d

# 確認服務已啟動（應該看到 sqlserver 和 redis）
docker ps
```

**檢查點**: 看到 2 個容器正在運行（sqlserver, redis）

---

### Step 2: 啟動 .NET 應用程式

```bash
# 更新資料庫結構（執行 Migrations）
dotnet ef database update

# 啟動應用
dotnet run
```

**檢查點**: 看到 `Now listening on: http://0.0.0.0:5000`

**開啟 Swagger**: http://localhost:5000/swagger

---

### Step 3: 建立測試資料（10 萬筆商品）

**方法 1: 使用 curl**

```bash
curl -X POST http://localhost:5000/api/products/init
```

**方法 2: 使用 Postman**

- Method: `POST`
- URL: `http://localhost:5000/api/products/init`
- 點擊 **Send**

**方法 3: 使用 Swagger**

- 展開 `POST /api/products/init`
- 點擊 **Try it out** → **Execute**

**檢查點**: 返回 `{"message": "成功"}`

---

### Step 4: 驗證資料已建立

```bash
curl http://localhost:5000/api/products/count
```

**預期結果**: `{"count": 100000}`

---

### Step 5: 測試三種方案的效能差異

#### 🔴 V1: 無優化（全表掃描）

```bash
curl "http://localhost:5000/api/products/search/Product_0001"
```

#### 🔵 V2: Database Index（索引查詢）⭐ **核心重點**

```bash
curl "http://localhost:5000/api/products/search-starts-with/Product_0001"
```

#### 🟢 V3: Redis Cache（快取命中）⭐ **核心重點**

```bash
# 第一次（Cache Miss）
curl "http://localhost:5000/api/products/search-cached/Product_0001"

# 第二次（Cache Hit）- 明顯變快
curl "http://localhost:5000/api/products/search-cached/Product_0001"
```

---

### Step 6: 前端高併發測試（視覺化效能差異）

```bash
# 在 Finder 中開啟 demo.html
open ~/E-Commerce-Backend/demo.html
```

**操作步驟**:

1. 點擊 **「執行 100 併發測試」** 按鈕
2. 等待約 30 秒
3. 觀察三條線的差異：
   - 🔴 **紅線（V1 無優化）**: 200-300ms，錯誤率高
   - 🔵 **藍線（V2 索引）**: 1-5ms，穩定
   - 🟢 **綠線（V3 快取）**: <1ms，最快

---

## 🎤 面試解說重點

### 1️⃣ 展示 Database Index 效能差異 ⭐ **核心**

**打開 Terminal 看 SQL Log**:

```bash
# V1: 全表掃描（慢）
curl "http://localhost:5000/api/products/search/Product_0001"
# 觀察 Terminal: WHERE [Name] LIKE '%Product_0001%'  ← 無法使用索引

# V2: 索引查詢（快）
curl "http://localhost:5000/api/products/search-starts-with/Product_0001"
# 觀察 Terminal: WHERE [Name] LIKE 'Product_0001%'  ← 使用 B-Tree 索引
```

**講解話術**:

> "你看 Terminal 的 SQL Log，V1 用的是 `%keyword%`（前後都有 %），這會導致全表掃描。
> 改用 `StartsWith` 後變成 `keyword%`（只有後面有 %），就能利用 B-Tree 索引，速度快 100 倍。"

**程式碼位置**: `Services/ProductService.cs`

```csharp
// Line 23-27: V1 無優化
.Where(p => p.Name.Contains(name))  // ❌ 全表掃描

// Line 31-35: V2 索引優化
.Where(p => p.Name.StartsWith(name))  // ✅ 使用索引
```

**Migration 位置**: `Migrations/20251112032100_AddIndexOnProductName.cs`

```csharp
// Line 10: 建立索引
migrationBuilder.CreateIndex("IX_Products_Name", "Products", "Name");
```

---

### 2️⃣ 展示 Redis Cache 效能提升 ⭐ **核心**

**連續呼叫兩次觀察差異**:

```bash
# 第一次：Cache Miss（需查 DB）
time curl "http://localhost:5000/api/products/search-cached/Product_0001"
# 觀察 Terminal: 有 SQL 查詢 Log

# 第二次：Cache Hit（直接返回）
time curl "http://localhost:5000/api/products/search-cached/Product_0001"
# 觀察 Terminal: 沒有 SQL Log，代表從 Redis 讀取
```

**講解話術**:

> "第一次請求會查資料庫並寫入 Redis（TTL 5 分鐘），
> 第二次就直接從 Redis 讀取，回應時間降到 1ms 以內。
> 這就是 **Cache-Aside Pattern**。"

**程式碼位置**: `Services/ProductService.cs`

```csharp
// Line 39-59: Cache-Aside Pattern
var cacheKey = $"search:{name}";

// 1. 先查 Redis
var cachedData = await _cache.GetStringAsync(cacheKey);
if (cachedData != null)  // Cache Hit
{
    return JsonSerializer.Deserialize<List<Product>>(cachedData);
}

// 2. Cache Miss：查 DB
var products = await _context.Products
    .Where(p => p.Name.StartsWith(name))
    .ToListAsync();

// 3. 寫入 Redis（TTL 5 分鐘）
await _cache.SetStringAsync(cacheKey, json, new()
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
});
```

---

### 3️⃣ 高併發場景問題定位

**講解話術**:

> "在 demo.html 的 100 併發測試中，V1 無優化版本會出現 80% 錯誤率，
> 原因是慢查詢導致 Connection Pool 耗盡（預設 Max Pool Size=200）。
> 加上索引後，查詢速度從 300ms 降到 5ms，錯誤率降為 0%。"

**程式碼位置**: `appsettings.json`

```json
// Line 3: Connection String 設定
"DefaultConnection": "Server=localhost;Database=TodoDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Max Pool Size=200"
```

---

## 🔧 如果出問題怎麼辦

### 問題 1: Docker 容器沒啟動

```bash
# 重新啟動
docker-compose down
docker-compose up -d
```

### 問題 2: 資料庫已有舊資料

```bash
# 刪除資料庫重建
dotnet ef database drop --force
dotnet ef database update
```

### 問題 3: Redis 快取沒清空

```bash
# 進入 Redis 容器清空快取
docker exec -it redis redis-cli
> FLUSHALL
> exit
```

### 問題 4: 應用程式未啟動

```bash
# Ctrl+C 停止，重新啟動
dotnet run
```

---

## 📊 預期效能數據

| 方案      | 單次請求  | 100 併發 | 錯誤率 | 關鍵技術            |
| --------- | --------- | -------- | ------ | ------------------- |
| V1 無優化 | 200-300ms | 500ms+   | 80%    | Contains + 全表掃描 |
| V2 索引   | 1-5ms     | 8ms      | 0%     | StartsWith + Index  |
| V3 快取   | <1ms      | 2ms      | 0%     | Cache-Aside + Redis |

---

## ⭐ 核心展示點總結

### 1. Database Index（最重要）

- **檔案**: `Services/ProductService.cs` Line 23-35
- **Migration**: `Migrations/20251112032100_AddIndexOnProductName.cs`
- **展示**: Terminal SQL Log 對比（`%keyword%` vs `keyword%`）

### 2. Redis Cache-Aside Pattern

- **檔案**: `Services/ProductService.cs` Line 39-59
- **展示**: 連續兩次請求，第二次沒有 SQL Log

### 3. 高併發測試視覺化

- **檔案**: `demo.html`
- **展示**: 三條線的效能差異圖表

---

## 🎯 面試時間分配建議

- **2 分鐘**: 啟動環境 + 建立測試資料
- **3 分鐘**: 展示 Index 效能差異 + SQL Log
- **2 分鐘**: 展示 Redis Cache 命中
- **3 分鐘**: 前端高併發測試視覺化
- **預留時間**: 回答問題

---

## 💡 可能被問的問題

### Q1: 為什麼 Contains 不能用索引？

> "因為 `%keyword%` 這種模式資料庫無法判斷從哪裡開始查，必須掃描整張表。
> 改用 `StartsWith` 變成 `keyword%` 後，可以利用 B-Tree 索引的前綴搜尋特性。"

### Q2: Redis TTL 為什麼設 5 分鐘？

> "這是在資料新鮮度和效能間的取捨。商品資料更新頻率不高，5 分鐘的延遲可接受。
> 如果是庫存這種高頻更新的資料，可能要搭配主動失效機制。"

### Q3: Connection Pool 耗盡怎麼辦？

> "根本解決是優化慢查詢（加索引）。如果調大 Pool Size 只是治標不治本，
> 而且會消耗更多資料庫資源。"

### Q4: Cache 一致性怎麼處理？

> "目前是簡單的 TTL 過期策略。如果需要強一致性，可以在更新資料時主動清除快取（Write-Through），
> 或使用 Redis Pub/Sub 通知所有節點清除。"

---

**祝面試順利！🚀**

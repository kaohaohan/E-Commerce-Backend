# Performance Optimization Lab: Database Index + Redis Cache

> 一個專注於學習效能優化技術的實驗專案  
> 使用商品搜尋場景驗證 Database Indexing 和 Redis Caching 的實際效果

## 🎯 專案目標

在高併發場景下驗證兩個核心優化技術的效能差異：

1. **Database Indexing** - B-Tree 索引優化查詢速度
2. **Redis Cache** - 分散式快取降低資料庫負擔

**技術棧**: .NET 8 + SQL Server + Redis + Docker

**測試場景**: 100,000 筆商品資料 × 100 併發請求

---

## 實驗結果

| 方案               | 技術實作                       | 回應時間  | 錯誤率 | 說明         |
| ------------------ | ------------------------------ | --------- | ------ | ------------ |
| V1: 無優化         | `.Contains()` - 全表掃描       | 200-300ms | 80%    | Pool 耗盡    |
| V2: Database Index | `.StartsWith()` + B-Tree Index | 1-5ms     | 0%     | 索引查詢     |
| V3: Redis Cache    | Cache-Aside Pattern (TTL 5min) | <1ms      | 0%     | 快取命中極快 |

---

## 核心技術

### 1️⃣ Database Index

```csharp
// ❌ Contains() - 無法使用索引
products.Where(p => p.Name.Contains(keyword))  // 全表掃描 O(n)

// ✅ StartsWith() + Index - 使用 B-Tree 索引
products.Where(p => p.Name.StartsWith(keyword))  // 索引查詢 O(log n)
```

**Migration 建立索引：**

```csharp
migrationBuilder.CreateIndex("IX_Products_Name", "Products", "Name");
```

### 2️⃣ Redis Cache-Aside Pattern

```csharp
// 先查 Redis
var cacheKey = $"search:{keyword}";
var cached = await _cache.GetStringAsync(cacheKey);
if (cached != null) return Deserialize(cached);  // Cache Hit

// Cache Miss：查 DB 並寫入 Redis
var result = await _dbQuery();
await _cache.SetStringAsync(cacheKey, Serialize(result), new() {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
});
return result;
```

---

## 快速開始

```bash
# 啟動環境 (SQL Server + Redis)
docker-compose up -d

# 執行應用
dotnet run

# 建立 10 萬筆測試資料
curl -X POST http://localhost:5000/api/products/init

# 開啟 demo.html 測試效能差異
```

**測試 API：**

- `GET /api/products/search/{keyword}` - V1 無優化
- `GET /api/products/search-starts-with/{keyword}` - V2 索引
- `GET /api/products/search-cached/{keyword}` - V3 快取

---

## 重點學習

### Database Index

- `.Contains()` 無法使用索引 → 全表掃描
- `.StartsWith()` 可使用 B-Tree 索引 → 查詢速度 100 倍差異
- 索引是解決慢查詢的第一步

### Redis Cache

- Cache-Aside Pattern：先查快取，Miss 才查 DB
- TTL 設定避免資料過期
- 適合熱門查詢，但需考慮快取一致性

---

## 🚀 延伸思考：進階場景

基於此專案的優化經驗，以下是對更複雜場景的思考：

### 場景 1: 即時稅率調整 API 設計

**需求**: 第三方提供即時稅率調整，需設計雙方 API 介面

**設計方案**:

1. **Pull Model（輪詢）**

   - 我方定期呼叫第三方 API 獲取最新稅率
   - 優點：實作簡單，我方控制更新頻率
   - 缺點：有延遲，且可能浪費 API 呼叫次數

2. **Push Model（Webhook）** ⭐ **推薦**

   ```
   第三方 → POST /api/webhook/tax-rate → 我方
   {
     "category_id": "123",
     "tax_rate": 0.15,
     "effective_at": "2025-12-18T10:00:00Z"
   }
   ```

   - 優點：即時性高、節省資源
   - 缺點：需處理重試機制、驗證來源

3. **混合方案**
   - 平時用 Webhook，定時輪詢作為備援
   - 使用 Redis 快取最新稅率 (TTL 1 小時)

---

### 場景 2: API Rate Limiting（每小時 5000 次）

**技術方案比較**:

| 方案                       | 實作               | 優點             | 缺點                             |
| -------------------------- | ------------------ | ---------------- | -------------------------------- |
| **Redis + Sliding Window** | 記錄每次請求時間戳 | 精確、分散式友善 | 記憶體消耗較高                   |
| **Token Bucket**           | 固定速率補充 Token | 允許短時突發流量 | 需要定時任務                     |
| **Fixed Window**           | 整點重置計數器     | 實作簡單         | 邊界問題（整點前後可能 2x 流量） |

**推薦實作（Redis Sliding Window）**:

```csharp
var key = $"rate_limit:{userId}:{hour}";
var count = await redis.IncrementAsync(key);
if (count == 1) await redis.ExpireAsync(key, TimeSpan.FromHours(1));

if (count > 5000) throw new TooManyRequestsException();
```

**延伸考量**:

- 不同用戶等級不同限額（Premium vs Free）
- 超過限額後的降級策略（返回快取資料）
- 監控與告警（接近限額時通知）

---

### 場景 3: 高併發訂單計算優化

**挑戰**: `totalCost = order_amount * unit_price * (1 + tax_rate)`

**優化策略**:

1. **預計算稅後價格** - 稅率變動時批次更新
2. **使用 Materialized View** - 資料庫層預先計算
3. **引入訊息佇列** - 非同步處理大量計算
4. **資料庫讀寫分離** - 計算查詢走 Read Replica

---

## 💭 技術取捨思考

這些延伸場景展示了：

- **沒有銀彈**：每個方案都有 trade-offs
- **由簡入繁**：先解決核心問題（本專案的 Index + Cache），再考慮進階場景
- **持續監控**：效能優化是持續的過程，需要 Metrics 和 Alerting

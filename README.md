# Performance Optimization Lab: Database Index + Redis Cache

> 📊 **實驗成果**：透過 Database Indexing + Redis Cache，將高併發查詢從 **80% 錯誤率降至 0%**，回應時間從 **300ms 降至 <1ms**

## 🎯 專案目標

在高併發場景下驗證兩個核心優化技術的效能差異：

1. **Database Indexing** - B-Tree 索引優化查詢速度（100x 提升）
2. **Redis Cache** - 分散式快取降低資料庫負擔（300x 提升）

**技術棧**: .NET 8 + SQL Server + Redis + Docker  
**測試場景**: 100,000 筆商品資料 × 100 併發請求

---

## 📈 實驗結果

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

### **步驟 1: 啟動環境**

```bash
docker-compose up -d
docker ps  # 確認 sqlserver 和 redis 都在運行
```

### **步驟 2: 執行應用**

```bash
dotnet ef database update  # 建立資料庫結構
dotnet run                 # 啟動 API (http://localhost:5000)
```

### **步驟 3: 建立測試資料**

```bash
curl -X POST http://localhost:5000/api/products/init
# 預期輸出: {"message":"成功建立 100000 筆測試資料"}
```

### **步驟 4: 測試效能差異**

**方式 1: 使用 curl**

```bash
# V1: 無優化 (預期 200-300ms)
curl "http://localhost:5000/api/products/search/Product_0001"

# V2: 索引優化 (預期 1-5ms)
curl "http://localhost:5000/api/products/search-starts-with/Product_0001"

# V3: Redis 快取 (第二次預期 <1ms)
curl "http://localhost:5000/api/products/search-cached/Product_0001"
```

**方式 2: 使用 demo.html**

- 開啟 `demo.html`
- 點擊三個按鈕，對比 100 併發請求的效能差異
- 觀察 V1 的錯誤率 vs V2/V3 的穩定性

---

## 💡 重點學習

### **關鍵發現**

1. **未優化的查詢在高併發下完全不可用**

   - 80% 錯誤率 → Connection Pool 耗盡
   - 第一步優化：加索引（0% 錯誤率，100x 速度提升）

2. **索引選擇很重要**

   - ❌ `.Contains()` 無法使用索引（全表掃描 O(n)）
   - ✅ `.StartsWith()` 可用 B-Tree 索引（O(log n)）

3. **快取適用場景**
   - ✅ 熱門查詢、讀多寫少
   - ⚠️ 需考慮：快取一致性、TTL 設定、命中率監控

### **優化順序**

```
1. 先加索引（解決根本問題）
2. 再加快取（錦上添花）
3. 持續監控（避免過度優化）
```

---

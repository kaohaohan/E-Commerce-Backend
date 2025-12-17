# E-Commerce Backend - API 高併發效能優化實驗

## 🎯 面試快速摘要

這個專案展示我在 **高併發場景下的效能分析與優化思路**：

- **識別問題**：在 10 萬筆資料 + 100 併發下，未優化查詢造成 80% 錯誤率、300ms 延遲
- **優化策略**：先用資料庫索引解決根本問題（錯誤率降至 0%），再用 Redis 降低 DB 壓力
- **驗證結果**：用實際數據對比三種方案（V1 無優化 / V2 索引 / V3 快取）

**技術棧**: .NET 8 + SQL Server + Redis + Docker + AWS ECS

---

## 專案背景

模擬電商商品搜尋場景，在高併發下測試不同優化策略的效能差異。

**測試條件**: 100,000 筆商品 × 100 併發請求

---

## 三種方案對比實驗

| 方案                      | 實作技術                         | 平均回應時間 | 錯誤率 | P95 延遲 | 備註                 |
| ------------------------- | -------------------------------- | ------------ | ------ | -------- | -------------------- |
| **Version 1: 無優化**     | EF Core `.Contains()` + 全表掃描 | 200-300ms    | 80%    | 500ms+   | Connection Pool 耗盡 |
| **Version 2: 資料庫索引** | `.StartsWith()` + B-Tree Index   | 1-5ms        | 0%     | 8ms      | 查詢改用前綴搜尋     |
| **Version 3: Redis 快取** | Cache-Aside Pattern (TTL 5min)   | <1ms         | 0%     | 2ms      | 熱門關鍵字快取       |

### 核心技術實作

**1. Database Index**

```csharp
// ❌ Contains() 無法使用索引 → 全表掃描
products.Where(p => p.Name.Contains(keyword))

// ✅ StartsWith() + Index → 索引查詢
products.Where(p => p.Name.StartsWith(keyword))
```

**2. Redis Cache-Aside Pattern**

```csharp
var cacheKey = $"search:{keyword}";
var cached = await _cache.GetStringAsync(cacheKey);
if (cached != null) return JsonSerializer.Deserialize<List<Product>>(cached);

var result = await _dbQuery();
await _cache.SetStringAsync(cacheKey, json, new() {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
});
```

**3. Connection Pooling**: `Max Pool Size=200`

---

## 快速開始

```bash
# 1. 啟動環境
docker-compose up -d

# 2. 執行應用
dotnet restore && dotnet ef database update && dotnet run

# 3. 建立測試資料
curl -X POST http://localhost:5000/api/products/init

# 4. 開啟 demo.html 執行 100 併發測試
```

### API 端點

| Endpoint                                         | 說明               | 版本 |
| ------------------------------------------------ | ------------------ | ---- |
| `GET /api/products/search/{keyword}`             | 無優化（全表掃描） | V1   |
| `GET /api/products/search-starts-with/{keyword}` | 資料庫索引         | V2   |
| `GET /api/products/search-cached/{keyword}`      | Redis 快取         | V3   |

---

## 核心發現

1. **未優化的查詢在高併發下不可用**（80% 錯誤率 → Connection Pool 耗盡）
2. **資料庫索引是第一步優化**（100x 效能提升，0% 錯誤率）
3. **Redis 適合熱門查詢場景**（進一步降低 DB 壓力）

---

## ⚖️ 設計取捨 (Trade-offs)

| 優化方案            | 優點               | 限制                         | 適用場景                     |
| ------------------- | ------------------ | ---------------------------- | ---------------------------- |
| **Database Index**  | 根本解決查詢慢問題 | 僅適用前綴搜尋（StartsWith） | 所有查詢場景                 |
| **Redis Cache**     | 極低延遲（<1ms）   | 需考慮快取一致性、TTL 設定   | 熱門關鍵字、可容忍短暫不一致 |
| **Connection Pool** | 避免重複建立連線   | Pool size 過大會消耗資源     | 高併發場景                   |

**關鍵思考**：

- 先解決根本問題（索引），再考慮快取
- Cache 不是銀彈：查詢條件多樣時命中率會下降
- 需監控 cache 命中率，避免無效快取佔用記憶體

---

## 部署

- **本機**: `docker-compose up -d`
- **AWS**: ECS Fargate + ALB + Auto Scaling（詳見 [DEPLOYMENT_MANUAL.md](./DEPLOYMENT_MANUAL.md)）

---

## License

MIT

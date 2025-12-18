# 快速演示小抄（1 頁紙）

## 🚀 啟動流程（5 個指令）

```bash
# 1. 啟動 Docker
docker-compose up -d

# 2. 更新資料庫
dotnet ef database update

# 3. 啟動應用
dotnet run

# 4. 建立測試資料（新開 Terminal）
curl -X POST http://localhost:5000/api/products/init

# 5. 驗證資料量
curl http://localhost:5000/api/products/count
# 預期: {"count": 100000}
```

---

## ⭐ 核心展示（3 個重點）

### 1️⃣ Database Index（必講）

```bash
# V1 無優化（慢 + 全表掃描）
curl "http://localhost:5000/api/products/search/Product_0001"

# V2 索引優化（快 100 倍）
curl "http://localhost:5000/api/products/search-starts-with/Product_0001"
```

**看 Terminal SQL Log**:

- V1: `WHERE [Name] LIKE '%Product%'` ❌ 無法用索引
- V2: `WHERE [Name] LIKE 'Product%'` ✅ 使用 B-Tree 索引

**程式碼**: `Services/ProductService.cs` Line 23-35

---

### 2️⃣ Redis Cache（必講）

```bash
# 第一次（Cache Miss）- 看 Terminal 有 SQL Log
curl "http://localhost:5000/api/products/search-cached/Product_0001"

# 第二次（Cache Hit）- 沒有 SQL Log，極快
curl "http://localhost:5000/api/products/search-cached/Product_0001"
```

**程式碼**: `Services/ProductService.cs` Line 39-59

- Cache-Aside Pattern
- TTL 5 分鐘

---

### 3️⃣ 高併發視覺化

```bash
open demo.html
# 點「執行 100 併發測試」
```

**觀察結果**:

- 🔴 V1: 200-300ms，錯誤率 80%（Pool 耗盡）
- 🔵 V2: 1-5ms，錯誤率 0%
- 🟢 V3: <1ms，錯誤率 0%

---

## 🎤 講解話術

### Index 優化

> "原本用 `.Contains()` 會全表掃描，10 萬筆資料要 300ms。
> 改用 `.StartsWith()` 配合 B-Tree 索引後，降到 5ms，速度快 60 倍。
> 在高併發下，V1 會導致 Connection Pool 耗盡（80% 錯誤率），加索引後完全解決。"

### Redis Cache

> "實作 Cache-Aside Pattern：先查 Redis，Miss 才查 DB 並寫入快取（TTL 5 分鐘）。
> 第二次請求直接從 Redis 讀取，回應時間降到 1ms 以內，而且減輕 DB 負擔。"

---

## 🔧 緊急救援指令

```bash
# Docker 有問題
docker-compose down && docker-compose up -d

# 資料庫有問題
dotnet ef database drop --force && dotnet ef database update

# 清空 Redis
docker exec -it redis redis-cli
> FLUSHALL

# 重啟應用
# Ctrl+C 停止，然後 dotnet run
```

---

## 📂 核心檔案位置

| 檔案                                                 | 行數    | 說明                  |
| ---------------------------------------------------- | ------- | --------------------- |
| `Services/ProductService.cs`                         | 23-27   | V1 Contains（無優化） |
| `Services/ProductService.cs`                         | 31-35   | V2 StartsWith（索引） |
| `Services/ProductService.cs`                         | 39-59   | V3 Redis Cache        |
| `Migrations/20251112032100_AddIndexOnProductName.cs` | 10      | 建立索引 Migration    |
| `appsettings.json`                                   | 3       | Connection Pool 設定  |
| `demo.html`                                          | 379-383 | 高併發測試邏輯        |

---

## 💡 常見面試問題

**Q: 為什麼 Contains 不能用索引？**

> B-Tree 索引只支援前綴搜尋，`%keyword%` 前後都有萬用字元無法利用。

**Q: Redis TTL 5 分鐘的考量？**

> 商品資料更新頻率低，5 分鐘延遲可接受。如果是高頻更新（如庫存），需要主動失效機制。

**Q: Connection Pool 耗盡怎麼辦？**

> 根本解決是優化慢查詢（加索引），不是調大 Pool Size。

**Q: Cache 一致性？**

> 目前用 TTL。如需強一致性，可在更新時主動清除快取（Write-Through）。

---

## 📊 預期效能

| 方案      | 回應時間  | 錯誤率 | 關鍵技術            |
| --------- | --------- | ------ | ------------------- |
| V1 無優化 | 200-300ms | 80%    | Contains + 全表掃描 |
| V2 索引   | 1-5ms     | 0%     | StartsWith + Index  |
| V3 快取   | <1ms      | 0%     | Cache-Aside + Redis |

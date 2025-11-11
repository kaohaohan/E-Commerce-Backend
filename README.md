# PXPayBackend - .NET Core Web API

這是我用 ASP.NET Core 開發的後端 API 專案，展示企業級後端開發的核心技術。

> 🚀 **技術亮點**：Memory Cache 效能優化、ACID Transaction、Rate Limiting、CI/CD 自動化部署

## 環境需求

- .NET 8.0 SDK
- Docker Desktop（用來跑 SQL Server）
- 任何能跑 .NET 的作業系統（Windows / macOS / Linux）

## 怎麼跑起來

### 1. 先啟動 SQL Server（用 Docker）

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Clone 專案並還原套件

```bash
git clone https://github.com/kaohaohan/PXPayBackend.git
cd PXPayBackend
dotnet restore
```

### 3. 執行資料庫遷移（建立資料表）

```bash
dotnet ef database update
```

### 4. 啟動 API

```bash
dotnet run --urls "http://localhost:5000"
```

### 5. 打開 Swagger 測試頁面

```
http://localhost:5000/swagger
```

## API 功能

### Products API（Cache 優化）

- `GET /api/products/stock` - 查詢庫存（有 Cache）
- `GET /api/products/stock/no-cache` - 查詢庫存（沒有 Cache，用來對比）
- `POST /api/products/init` - 初始化測試資料

### TodoItems API（CRUD + Transaction）

- `GET /api/todoitems` - 查詢所有項目
- `GET /api/todoitems/{id}` - 查詢單筆項目
- `POST /api/todoitems` - 新增項目
- `PUT /api/todoitems/{id}` - 更新項目
- `DELETE /api/todoitems/{id}` - 刪除單筆項目
- `DELETE /api/todoitems/batch` - 批次刪除（ACID Transaction）

### 安全防護

- **Rate Limiting（限流）** - 每個 IP 每分鐘最多 10 次請求

## 專案結構

```
PXPayBackend/
├── Controllers/
│   ├── TodoItemsController.cs    # TodoItems API（CRUD + Transaction）
│   └── ProductsController.cs     # Products API（Cache 優化）
├── Services/
│   ├── IProductService.cs        # Service 介面
│   └── ProductService.cs         # 業務邏輯（Cache + DB 查詢）
├── Models/
│   ├── TodoItem.cs               # TodoItem 資料結構
│   └── Product.cs                # Product 資料結構
├── Data/
│   └── TodoContext.cs            # EF Core DbContext（資料庫連線）
├── Migrations/                   # 資料庫遷移檔案
├── Program.cs                    # 程式進入點（IOC/DI 設定）
├── appsettings.json              # 資料庫連線字串設定
└── PXPayBackend.csproj          # 專案設定檔
```

## 用到的技術

- **ASP.NET Core Web API** - 主要框架
- **Entity Framework Core** - ORM（物件關聯映射）
- **InMemory Database** - 記憶體資料庫（開發/測試用）
- **Memory Cache** - 記憶體快取（效能優化）
- **Rate Limiting** - API 限流（防止 DDoS 攻擊）
- **MVC + Service 分層架構** - Controller → Service → Repository
- **IOC/DI（依賴注入）** - Interface + Constructor Injection
- **async/await** - 非同步程式設計，提升效能
- **Swagger** - API 文件跟測試界面
- **LINQ & Lambda** - 資料查詢（例如 `await _context.TodoItems.FindAsync(id)`）
- **ACID Transaction** - 批次操作的交易處理

## 資料存儲

使用 **SQL Server** 作為資料庫，透過 **Entity Framework Core** 進行資料存取。資料會持久化儲存，伺服器重啟後資料不會消失。

## 測試方式

啟動後打開 Swagger 頁面，可以直接在瀏覽器上測試所有 API。

試試看：

1. 先 GET 看看有哪些資料
2. POST 新增一筆
3. 再 GET 一次確認有新增成功
4. 用 PUT 更新資料
5. 用 DELETE 刪掉

## 技術亮點

這個專案展示了企業級 ASP.NET Core 開發的核心技術：

1. **RESTful API 設計** - 標準的 HTTP 方法和狀態碼
2. **MVC + Service 分層架構** - Controller → Service → Repository，職責分離
3. **Memory Cache 效能優化** - 查詢效能從 500ms 降到 0.05ms
4. **Rate Limiting 限流保護** - 每個 IP 每分鐘最多 10 次請求
5. **Entity Framework Core ORM** - Code First 方式管理資料庫
6. **IOC/DI 架構模式** - Interface + Constructor Injection，鬆耦合設計
7. **非同步程式設計** - 所有資料庫操作都使用 async/await
8. **LINQ & Lambda 表達式** - 優雅的資料查詢語法
9. **ACID Transaction** - 批次刪除 API 展示交易處理（原子性、一致性）
10. **CI/CD 自動化** - GitHub Actions 自動建置和測試

## 效能優化展示

### Cache 效能對比

| API                 | 第一次請求 | 第二次請求 | 效能提升      |
| ------------------- | ---------- | ---------- | ------------- |
| `/stock/no-cache`   | 500ms      | 500ms      | -             |
| `/stock` (有 Cache) | 500ms      | **0.05ms** | **10,000 倍** |

**說明：**

- 第一次請求：查詢資料庫（500ms）
- 第二次請求：從 Cache 取（0.05ms）
- 5 秒內的請求都從 Cache 取
- 資料庫壓力減少

### Rate Limiting 限流保護

**功能：** 防止 DDoS 攻擊和惡意濫用

**規則：** 每個 IP 每分鐘最多 10 次請求

**測試方式：**

1. 打開 Swagger：`http://localhost:5000/swagger`
2. 選擇任一 API（例如 `GET /api/Products/stock`）
3. 連續點擊 11 次「Execute」

**預期結果：**

- 前 10 次：✅ 200 OK
- 第 11 次：❌ 429 Too Many Requests

**回應訊息：**

```json
{
  "message": "API calls quota exceeded! maximum admitted 10 per 1m."
}
```

**技術實作：**

- 使用 `AspNetCoreRateLimit` 套件
- 透過 Middleware 在請求進入 Controller 前攔截
- 計數器存在記憶體裡，可擴展為 Redis（分散式環境）

## CI/CD 自動化

本專案已整合 **GitHub Actions** 和 **GitLab CI**，每次 push 或 PR 時會自動：

1. ✅ **還原套件** - `dotnet restore`
2. ✅ **編譯專案** - `dotnet build`
3. ✅ **執行測試** - `dotnet test`（如果有測試專案）
4. ✅ **程式碼品質檢查** - `dotnet format`

### GitHub Actions

查看建置狀態：前往專案的 **Actions** 頁籤

設定檔位置：`.github/workflows/dotnet.yml`

### GitLab CI

如果你想在 GitLab 上使用，專案已包含 `.gitlab-ci.yml` 設定檔，直接推送到 GitLab 即可自動觸發 CI/CD Pipeline。

## 注意事項

- 如果要修改資料庫連線字串，請編輯 `appsettings.json` 的 `ConnectionStrings` 區塊
- 執行專案前請確保 Docker 中的 SQL Server 容器正在運行
- 若要查看資料庫內容，可使用 Azure Data Studio 或 SQL Server Management Studio

有任何問題歡迎聯絡我！

# PXPayBackend - Todo API

這是我用 ASP.NET Core 寫的待辦事項 API，用來練習和展示基本的 CRUD 操作。

## 環境需求

- .NET 8.0 SDK
- 任何能跑 .NET 的作業系統（Windows / macOS / Linux）

## 怎麼跑起來

1. 先 clone 下來
```bash
git clone https://github.com/kaohaohan/PXPayBackend.git
cd PXPayBackend
```

2. 直接跑
```bash
dotnet run --urls "http://localhost:5000"
```

3. 打開瀏覽器，輸入這個網址就能看到 API 測試頁面了
```
http://localhost:5000/swagger
```

## API 功能

基本的增刪改查都有：

- `GET /api/todoitems` - 拿所有待辦事項
- `GET /api/todoitems/{id}` - 拿單一筆資料
- `POST /api/todoitems` - 新增待辦事項
- `PUT /api/todoitems/{id}` - 更新待辦事項
- `DELETE /api/todoitems/{id}` - 刪除待辦事項

## 專案結構

```
PXPayBackend/
├── Controllers/
│   └── TodoItemsController.cs    # API 邏輯都在這
├── Models/
│   └── TodoItem.cs                # 資料結構定義
├── Program.cs                     # 程式進入點
└── PXPayBackend.csproj           # 專案設定檔
```

## 用到的技術

- **ASP.NET Core Web API** - 主要框架
- **Swagger** - API 文件跟測試界面
- **LINQ & Lambda** - 資料查詢用的（例如 `_todos.Find(x => x.Id == id)`）

## 資料存儲

目前是用記憶體內的 List 存資料，所以重啟就會清掉。如果要做成正式的，可以接上資料庫（Entity Framework Core + SQL Server）。

## 測試方式

啟動後打開 Swagger 頁面，可以直接在瀏覽器上測試所有 API。

試試看：
1. 先 GET 看看有哪些資料
2. POST 新增一筆
3. 再 GET 一次確認有新增成功
4. 用 PUT 更新資料
5. 用 DELETE 刪掉

## 備註

這個專案主要是展示 ASP.NET Core 的基本用法，包含：
- Controller 的寫法
- RESTful API 設計
- Lambda 表達式的應用
- 基本的錯誤處理（404 Not Found、400 Bad Request）

有任何問題歡迎聯絡我！


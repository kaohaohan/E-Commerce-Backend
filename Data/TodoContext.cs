// 引入 EF Core 和我們自己的 Model
using Microsoft.EntityFrameworkCore;
using PXPayBackend.Models;

namespace PXPayBackend.Data
{
    // 讓我們的 TodoContext 繼承  .NET 內建的 DbContext
    // 就像 Mongoose 的 connection + schema 的集合體 
    
    public class TodoContext : DbContext
    {
        // Constructor (建構子)
        // 接收 DbContext Options，
        public TodoContext(DbContextOptions<TodoContext> options)
            : base(options)
        {
        }

        // DbSet 定義表
        // 就像 Mongoose 的 model('Todo', todoSchema)
        //TodoItems 型別是 DbSet<TodoItem>所以可以提供查詢方法
        public DbSet<TodoItem> TodoItems { get; set; } = null!;
    }
}



/*
Why 為什麼要EF CORE  ?
手寫SQL, 手動處理連線、執行、讀取資料、手動關閉 。 寫SQL、拼錯、型別轉換..
但有了 await _context.TodoItems.ToListAsync(); 翻譯成SQL語法 SELECT * FROM TodoItems WHERE Id = 1

What EF Core?
ORM 要建立DbContext連線 -> DbSet<T> 一張表 -> LINQ Methods 查詢方法

1. DbContext 資料庫入口1.管理連線 2.定義資料庫表 他像是之前專案寫的 mongoose.connect('mongodb://localhost/tododb');
2. DbSet<T> 資料庫裡TodoItems的表 像是Nongoose const Todo = mongoose.model('Todo', todoSchema);
3. LINQ Methods 查找語法 像是 await _context.TodoItems.ToListAsync()  ... 像是await Todo.find()

How怎麼設定 ？
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

public class TodoContext : DbContext...       
    // Constructor ..連接
    //定義表 TodoItems

3. 去 Program(Service) 註冊 DbContext 
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectio

告訴.NET 我有一個 TodoContext 用 SQL Server連 讀取字串從 appsettings.json
像是
// server.js
mongoose.connect('mongodb://localhost/tododb');

4.去controller 用
原先private readonly TodoContext _context; 改用

 public TodoItemsController(TodoContext context)
        {
            _context = context;
        }
*/

using Microsoft.EntityFrameworkCore;
using PXPayBackend.Models;

namespace PXPayBackend.Data
{
    // DbContext 就是 EF Core 的核心
    // 就像 Mongoose 的 connection + schema 的集合體
    public class TodoContext : DbContext
    {
        // Constructor (建構子)
        // 接收 DbContextOptions，這是 IOC/DI 注入的關鍵
        public TodoContext(DbContextOptions<TodoContext> options)
            : base(options)
        {
        }

        // DbSet 代表資料庫中的一張表
        // DbSet<TodoItem> 就是 TodoItems 表
        // 就像 Mongoose 的 model('Todo', todoSchema)
        public DbSet<TodoItem> TodoItems { get; set; } = null!;
    }
}


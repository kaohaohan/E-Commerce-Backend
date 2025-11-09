using Xunit;
using PXPayBackend.Controllers;
using PXPayBackend.Models;
using PXPayBackend.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PXPayBackend.Tests
{
    public class TodoItemsControllerTests
    {
        // 建立一個測試用的 InMemory DbContext
        private TodoContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TodoContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            
            return new TodoContext(options);
        }

        [Fact]
        public async Task GetTodoItem_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange (準備)
            using (var context = GetInMemoryDbContext())
            {
                var controller = new TodoItemsController(context);
                
                // Act (執行)
                var result = await controller.GetTodoItem(999);
                
                // Assert (驗證)
                Assert.IsType<NotFoundResult>(result.Result);
            }
        }

        [Fact]
        public async Task GetTodoItem_ReturnsItem_WhenItemExists()
        {
            // Arrange (準備)
            using (var context = GetInMemoryDbContext())
            {
                // 先新增一筆測試資料
                context.TodoItems.Add(new TodoItem 
                { 
                    Id = 1, 
                    Name = "測試項目", 
                    IsComplete = false 
                });
                await context.SaveChangesAsync();

                var controller = new TodoItemsController(context);
                
                // Act (執行)
                var result = await controller.GetTodoItem(1);
                
                // Assert (驗證)
                Assert.NotNull(result.Value);
                Assert.Equal("測試項目", result.Value.Name);
                Assert.False(result.Value.IsComplete);
            }
        }

        [Fact]
        public async Task PostTodoItem_CreatesNewItem()
        {
            // Arrange (準備)
            using (var context = GetInMemoryDbContext())
            {
                var controller = new TodoItemsController(context);
                var newItem = new TodoItem 
                { 
                    Name = "新增的項目", 
                    IsComplete = false 
                };
                
                // Act (執行)
                var result = await controller.PostTodoItem(newItem);
                
                // Assert (驗證)
                var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
                var returnedItem = Assert.IsType<TodoItem>(createdResult.Value);
                Assert.Equal("新增的項目", returnedItem.Name);
            }
        }
    }
}


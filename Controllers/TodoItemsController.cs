using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PXPayBackend.Data;
using PXPayBackend.Models;

namespace PXPayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodoItemsController : ControllerBase
    {
        // IOC/DI 核心！不再用 static List，改用 DbContext
        private readonly TodoContext _context;

        // Constructor- 接收 IOC 注入的 DbContext
        public TodoItemsController(TodoContext context)
        {
            _context = context;
        }
        
        // GET /api/todoitems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            return await _context.TodoItems.ToListAsync();
        }

        // GET /api/todoitems/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            // LINQ & Lambda（改成查詢資料庫）
            var todoItem = await _context.TodoItems.FindAsync(id);
            
            if (todoItem == null)
            {
                return NotFound();
            }
            
            return todoItem;
        }

        // POST /api/todoitems
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            // 直接加入 DbContext（Id 會自動生成）
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        // PUT /api/todoitems/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TodoItemExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE /api/todoitems/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        // DELETE: /api/todoitems/batch
        // 批次刪除 - 展示 Transaction (ACID)
        //從request body 接收 一包 id 
   [HttpDelete("batch")]
public async Task<IActionResult> DeleteBatch([FromBody] long[] ids)
{
    // 🖨️ 1. Print 收到的 ids
    Console.WriteLine($" 收到的 ids: {string.Join(", ", ids)}");
    //using 語法糖, 是用來自動釋放"非託管"資源(資料庫連線)，在這下面區塊 會自動呼叫物件 Dispose() 確保資源被釋放，換句話說transaction 可以被正確關閉釋放資料庫連線避免資源洩漏

    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
        //新增一個list裝 ids 
         var itemsToDelete = new List<TodoItem>();

        foreach (var id in ids)
        {
            var item = await _context.TodoItems.FindAsync(id);

            //找到每個id 的資料
          if (item != null)
        {
            // 🖨️ Print 找到的 item
            Console.WriteLine($" 找到 id={item.Id}, Name={item.Name}, IsComplete={item.IsComplete}");
            
            // 加入 List
            itemsToDelete.Add(item);
            
            // 🖨️ Print 目前 List 有幾筆
            Console.WriteLine($"📦 itemsToDelete 現在有 {itemsToDelete.Count} 筆資料");
        }
        else
        {
            Console.WriteLine($" id={id} 不存在");
        }

        }
        
        //刪除整包itemsToDelete
        _context.TodoItems.RemoveRange(itemsToDelete);
        //產生SQL語句  執行上面的刪除 但它其實沒真的刪除 目前還在   Transaction 暫存區
        //像是git commit 
        await _context.SaveChangesAsync();

        // 真正提交transaction 永久保存DB
        //像是git push 
        await transaction.CommitAsync();
         Console.WriteLine(" 批次删除成功！");
        return NoContent(); // 204
    
        }catch (Exception ex)
        {
              // Rollback 
              await transaction.RollbackAsync();
              Console.WriteLine($" 刪除失敗: {ex.Message}");
              return BadRequest(new { error = ex.Message });
              

        }
        
    }

    
}


        // Helper method
        private async Task<bool> TodoItemExists(long id)
        {
            return await _context.TodoItems.AnyAsync(e => e.Id == id);
        }

    }

}

/* 
筆記：
一開始用static List ？
因為沒有數據庫...不寫static 每次HTTP請求 都要創一新的controller 要確保這物件只要做一次就好(Singleton)

1. 複習node.js路由
router.get('/', (req, res) => {
    const data = userService.getAll();
    res.json(data);
});

2. 假資料庫
const todos = [
    { id: 1, name: "學習 C#", isComplete: false },
    { id: 2, name: "準備面試", isComplete: false }
];

3. IEnumerable<TodoItem> 像是Interface
不在乎傳的是List or array 
像是C++ template<typename Iterator>{
    for(auto it = begin; it != end; ++it) {
        std::cout << it->Name << std::endl;
    }
} 

4. Lambda 表達式
id 是 long 型別的變數
x 是 TodoItem 物件
var todoItem = _todos.Find(x => x.Id == id);

像是 JS 會這樣寫 const todoItem = todos.find(x => x.id === id); 
C++
auto it = std::find_if(todos.begin(), todos.end(), 
    [id](const TodoItem& x) { return x.Id == id; });
*/
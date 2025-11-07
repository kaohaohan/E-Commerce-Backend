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

        // Constructor（建構子）- 接收 IOC 注入的 DbContext
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

        // Helper method
        private async Task<bool> TodoItemExists(long id)
        {
            return await _context.TodoItems.AnyAsync(e => e.Id == id);
        }

    }

}

/* 
筆記：

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
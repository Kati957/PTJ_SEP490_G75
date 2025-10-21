// ================================================
// PTJ_API/Controllers/EmployerPostsController.cs
// ================================================
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.EmployerPostService;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployerPostsController : ControllerBase
    {
        private readonly IEmployerPostService _service;

        public EmployerPostsController(IEmployerPostService service)
        {
            _service = service;
        }

        // CRUD
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
            => Ok(await _service.GetByUserAsync(userId));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound("Không tìm thấy bài đăng.");
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound("Không tìm thấy bài đăng để xoá.");
            return Ok("Đã xoá bài đăng thành công.");
        }

        // AI
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployerPostDto dto)
        {
            var result = await _service.CreateEmployerPostAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id}/refresh")]
        public async Task<IActionResult> Refresh(int id)
        {
            var result = await _service.RefreshSuggestionsAsync(id);
            return Ok(result);
        }
    }
}

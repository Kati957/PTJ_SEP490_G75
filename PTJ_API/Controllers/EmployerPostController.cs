using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.EmployerPostService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployerPostController : ControllerBase
    {
        private readonly IEmployerPostService _service;

        public EmployerPostController(IEmployerPostService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] EmployerPostDto dto)
        {
            var post = await _service.CreateEmployerPostAsync(dto);
            return Ok(post);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _service.GetAllAsync();
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _service.GetByIdAsync(id);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var posts = await _service.GetByUserAsync(userId);
            if (posts == null || !posts.Any())
                return NotFound(new { message = "Không tìm thấy bài đăng nào cho người dùng này." });

            return Ok(posts);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}

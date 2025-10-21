// ================================================
// PTJ_API/Controllers/EmployerShortlistController.cs
// ================================================
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.EmployerPostService;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployerShortlistController : ControllerBase
    {
        private readonly IEmployerPostService _service;

        public EmployerShortlistController(IEmployerPostService service)
        {
            _service = _service = service;
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveCandidateDto dto)
        {
            await _service.SaveCandidateAsync(dto);
            return Ok("Đã lưu ứng viên vào danh sách phù hợp.");
        }

        [HttpDelete("unsave")]
        public async Task<IActionResult> Unsave([FromBody] SaveCandidateDto dto)
        {
            await _service.UnsaveCandidateAsync(dto);
            return Ok("Đã gỡ ứng viên khỏi danh sách phù hợp.");
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetByPost(int postId)
        {
            var list = await _service.GetShortlistedByPostAsync(postId);
            return Ok(list);
        }
    }
}

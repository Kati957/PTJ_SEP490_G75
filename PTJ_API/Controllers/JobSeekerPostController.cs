using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.JobSeekerPostService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSeekerPostController : ControllerBase
    {
        private readonly IJobSeekerPostService _service;

        public JobSeekerPostController(IJobSeekerPostService service)
        {
            _service = service;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId) =>
            Ok(await _service.GetByUserAsync(userId));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _service.GetByIdAsync(id);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] JobSeekerPostDto dto)
        {
            var result = await _service.CreateJobSeekerPostAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh/{id}")]
        public async Task<IActionResult> Refresh(int id)
        {
            var result = await _service.RefreshSuggestionsAsync(id);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool ok = await _service.DeleteAsync(id);
            return ok ? Ok("Đã xóa thành công.") : NotFound("Không tìm thấy bài đăng.");
        }

        [HttpPost("save-job")]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobDto dto)
        {
            await _service.SaveJobAsync(dto);
            return Ok("Đã lưu công việc.");
        }

        [HttpPost("unsave-job")]
        public async Task<IActionResult> UnsaveJob([FromBody] SaveJobDto dto)
        {
            await _service.UnsaveJobAsync(dto);
            return Ok("Đã bỏ lưu công việc.");
        }

        [HttpGet("saved/{jobSeekerId}")]
        public async Task<IActionResult> GetSavedJobs(int jobSeekerId)
        {
            var result = await _service.GetSavedJobsAsync(jobSeekerId);
            return Ok(result);
        }
    }
}

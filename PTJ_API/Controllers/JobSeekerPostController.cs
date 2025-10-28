using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;

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

        // =========================================================
        // CREATE
        // =========================================================
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] JobSeekerPostDto dto)
            {
            var result = await _service.CreateJobSeekerPostAsync(dto);
            return Ok(new { success = true, message = "Đăng bài tìm việc thành công.", data = result });
            }

        // =========================================================
        // READ
        // =========================================================
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
            {
            var result = await _service.GetAllAsync();
            return Ok(result);
            }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
            {
            var result = await _service.GetByUserAsync(userId);
            return Ok(result);
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });
            return Ok(result);
            }

        // =========================================================
        // UPDATE
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] JobSeekerPostDto dto)
            {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng để cập nhật." });
            return Ok(new { success = true, message = "Cập nhật thành công.", data = result });
            }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            var success = await _service.DeleteAsync(id);
            return Ok(new { success, message = success ? "Đã xóa bài đăng." : "Không tìm thấy bài đăng." });
            }

        // =========================================================
        // AI SUGGESTIONS
        // =========================================================
        [HttpPost("refresh/{postId}")]
        public async Task<IActionResult> Refresh(int postId)
            {
            var result = await _service.RefreshSuggestionsAsync(postId);
            return Ok(new { success = true, message = "Đã làm mới đề xuất việc làm.", data = result });
            }

        // =========================================================
        // SHORTLIST
        // =========================================================
        [HttpPost("save-job")]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobDto dto)
            {
            await _service.SaveJobAsync(dto);
            return Ok(new { success = true, message = "Đã lưu việc làm." });
            }

        [HttpPost("unsave-job")]
        public async Task<IActionResult> UnsaveJob([FromBody] SaveJobDto dto)
            {
            await _service.UnsaveJobAsync(dto);
            return Ok(new { success = true, message = "Đã bỏ lưu việc làm." });
            }

        [HttpGet("saved/{jobSeekerId}")]
        public async Task<IActionResult> GetSavedJobs(int jobSeekerId)
            {
            var result = await _service.GetSavedJobsAsync(jobSeekerId);
            return Ok(result);
            }
        }
    }

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.EmployerPostService;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employer,Admin")]
    public class EmployerPostController : ControllerBase
        {
        private readonly IEmployerPostService _service;

        public EmployerPostController(IEmployerPostService service)
            {
            _service = service;
            }

        // =========================================================
        // CREATE
        // =========================================================
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] EmployerPostDto dto)
            {
            var result = await _service.CreateEmployerPostAsync(dto);
            return Ok(new { success = true, message = "Đăng bài tuyển dụng thành công.", data = result });
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
        public async Task<IActionResult> Update(int id, [FromBody] EmployerPostDto dto)
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
            return Ok(new { success = true, message = "Đã làm mới đề xuất ứng viên.", data = result });
            }

        // =========================================================
        // SHORTLIST
        // =========================================================
        [HttpPost("save-candidate")]
        public async Task<IActionResult> SaveCandidate([FromBody] SaveCandidateDto dto)
            {
            await _service.SaveCandidateAsync(dto);
            return Ok(new { success = true, message = "Đã lưu ứng viên." });
            }

        [HttpPost("unsave-candidate")]
        public async Task<IActionResult> UnsaveCandidate([FromBody] SaveCandidateDto dto)
            {
            await _service.UnsaveCandidateAsync(dto);
            return Ok(new { success = true, message = "Đã bỏ lưu ứng viên." });
            }

        [HttpGet("shortlist/{postId}")]
        public async Task<IActionResult> GetShortlisted(int postId)
            {
            var result = await _service.GetShortlistedByPostAsync(postId);
            return Ok(result);
            }


        // =========================================================
        // AI SUGGESTIONS - GET (đã lưu trong AiMatchSuggestions)
        // =========================================================
        [HttpGet("{postId:int}/suggestions")]
        public async Task<IActionResult> GetSuggestions(int postId, [FromQuery] int take = 10, [FromQuery] int skip = 0)
            {
            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);

            // Nếu muốn total chính xác cho phân trang lớn, có thể tính riêng:
            // var total = await _service.CountSuggestionsByPostAsync(postId); // (tuỳ chọn)
            // return Ok(new { total, items });

            return Ok(new { total = items.Count(), items });
            }
        }
    }

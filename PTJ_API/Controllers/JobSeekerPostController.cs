using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;
using PTJ_Service.JobSeekerPostService;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "JobSeeker,Admin")]
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
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            // 🔒 Lấy userId từ JWT
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);

            if (!User.IsInRole("Admin") && dto.UserID != currentUserId)
                return Forbid("Bạn không thể đăng bài thay người khác.");

            // ✅ Kiểm tra logic đầu vào
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
                return BadRequest(new { success = false, message = "Tiêu đề phải có ít nhất 5 ký tự." });

            if (string.IsNullOrWhiteSpace(dto.PreferredLocation))
                return BadRequest(new { success = false, message = "Vui lòng chọn địa điểm mong muốn." });

            if (dto.Age is < 15 or > 65)
                return BadRequest(new { success = false, message = "Tuổi không hợp lệ." });

            var result = await _service.CreateJobSeekerPostAsync(dto);
            return Ok(new { success = true, message = "Đăng bài tìm việc thành công.", data = result });
            }

        // =========================================================
        // READ
        // =========================================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // chỉ admin mới xem tất cả
        public async Task<IActionResult> GetAll()
            {
            var result = await _service.GetAllAsync();
            return Ok(new { success = true, total = result.Count(), data = result });
            }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
            {
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);

            if (!User.IsInRole("Admin") && userId != currentUserId)
                return Forbid("Bạn không thể xem bài đăng của người khác.");

            var result = await _service.GetByUserAsync(userId);
            return Ok(new { success = true, total = result.Count(), data = result });
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.SeekerName != User.Identity!.Name)
                return Forbid("Bạn không thể xem bài đăng của người khác.");

            return Ok(new { success = true, data = post });
            }

        // =========================================================
        // UPDATE
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] JobSeekerPostDto dto)
            {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng để cập nhật." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && existing.SeekerName != User.Identity!.Name)
                return Forbid("Bạn không thể chỉnh sửa bài đăng của người khác.");

            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { success = true, message = "Cập nhật thành công.", data = result });
            }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.SeekerName != User.Identity!.Name)
                return Forbid("Bạn không thể xóa bài đăng của người khác.");

            var success = await _service.DeleteAsync(id);
            return Ok(new { success, message = success ? "Đã xóa bài đăng." : "Không thể xóa bài đăng." });
            }

        // =========================================================
        // AI SUGGESTIONS
        // =========================================================
        [HttpPost("refresh/{postId}")]
        public async Task<IActionResult> Refresh(int postId)
            {
            var post = await _service.GetByIdAsync(postId);
            if (post == null)
                return NotFound(new { success = false, message = "Bài đăng không tồn tại." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.SeekerName != User.Identity!.Name)
                return Forbid("Bạn không thể làm mới bài đăng của người khác.");

            var result = await _service.RefreshSuggestionsAsync(postId);
            return Ok(new { success = true, message = "Đã làm mới đề xuất việc làm.", data = result });
            }

        // =========================================================
        // SAVE JOB (Shortlist)
        // =========================================================
        [HttpPost("save-job")]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobDto dto)
            {
            if (dto.JobSeekerId <= 0 || dto.EmployerPostId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin việc làm hoặc ứng viên." });

            await _service.SaveJobAsync(dto);
            return Ok(new { success = true, message = "Đã lưu việc làm." });
            }

        [HttpPost("unsave-job")]
        public async Task<IActionResult> UnsaveJob([FromBody] SaveJobDto dto)
            {
            if (dto.JobSeekerId <= 0 || dto.EmployerPostId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin việc làm hoặc ứng viên." });

            await _service.UnsaveJobAsync(dto);
            return Ok(new { success = true, message = "Đã bỏ lưu việc làm." });
            }

        [HttpGet("saved/{jobSeekerId}")]
        public async Task<IActionResult> GetSavedJobs(int jobSeekerId)
            {
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbid("Bạn không thể xem danh sách việc làm đã lưu của người khác.");

            var result = await _service.GetSavedJobsAsync(jobSeekerId);
            return Ok(new { success = true, total = result.Count(), data = result });
            }

        // =========================================================
        // AI SUGGESTIONS - GET
        // =========================================================
        [HttpGet("{postId:int}/suggestions")]
        public async Task<IActionResult> GetSuggestions(int postId, [FromQuery] int take = 10, [FromQuery] int skip = 0)
            {
            var post = await _service.GetByIdAsync(postId);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.SeekerName != User.Identity!.Name)
                return Forbid("Bạn không thể xem gợi ý việc làm của bài đăng người khác.");

            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);
            return Ok(new { success = true, total = items.Count(), data = items });
            }
        }
    }

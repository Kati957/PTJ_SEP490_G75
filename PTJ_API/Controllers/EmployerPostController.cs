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
            // 🧩 Validate dữ liệu model
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            // 🧩 Lấy userId từ token (Claim "sub")
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);

            // 🧩 Nếu là Employer thì không cho đăng thay người khác
            if (!User.IsInRole("Admin") && dto.UserID != currentUserId)
                return Forbid("Bạn không thể đăng bài thay người khác.");

            // 🧩 Kiểm tra tiêu đề hợp lệ
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
                return BadRequest(new { success = false, message = "Tiêu đề phải có ít nhất 5 ký tự." });

            // 🧩 Kiểm tra lương
            if (dto.Salary < 0)
                return BadRequest(new { success = false, message = "Mức lương không hợp lệ." });

            var result = await _service.CreateEmployerPostAsync(dto);
            return Ok(new { success = true, message = "Đăng bài tuyển dụng thành công.", data = result });
            }

        // =========================================================
        // READ
        // =========================================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // chỉ admin mới được xem tất cả bài đăng
        public async Task<IActionResult> GetAll()
            {
            var result = await _service.GetAllAsync();
            return Ok(new { success = true, total = result.Count(), data = result });
            }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
            {
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);

            // 🧩 Employer chỉ xem được bài của chính mình
            if (!User.IsInRole("Admin") && currentUserId != userId)
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

            // 🧩 Nếu là employer, chỉ được xem bài của chính mình
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
                return Forbid("Bạn không thể xem bài đăng của người khác.");

            return Ok(new { success = true, data = post });
            }

        // =========================================================
        // UPDATE
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployerPostDto dto)
            {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng để cập nhật." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
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
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
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
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
                return Forbid("Bạn không thể làm mới bài đăng của người khác.");

            var result = await _service.RefreshSuggestionsAsync(postId);
            return Ok(new { success = true, message = "Đã làm mới đề xuất ứng viên.", data = result });
            }

        // =========================================================
        // SHORTLIST
        // =========================================================
        [HttpPost("save-candidate")]
        public async Task<IActionResult> SaveCandidate([FromBody] SaveCandidateDto dto)
            {
            if (dto.EmployerId <= 0 || dto.JobSeekerId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin ứng viên hoặc nhà tuyển dụng." });

            await _service.SaveCandidateAsync(dto);
            return Ok(new { success = true, message = "Đã lưu ứng viên." });
            }

        [HttpPost("unsave-candidate")]
        public async Task<IActionResult> UnsaveCandidate([FromBody] SaveCandidateDto dto)
            {
            if (dto.EmployerId <= 0 || dto.JobSeekerId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin ứng viên hoặc nhà tuyển dụng." });

            await _service.UnsaveCandidateAsync(dto);
            return Ok(new { success = true, message = "Đã bỏ lưu ứng viên." });
            }

        [HttpGet("shortlist/{postId}")]
        public async Task<IActionResult> GetShortlisted(int postId)
            {
            var post = await _service.GetByIdAsync(postId);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
                return Forbid("Bạn không thể xem danh sách ứng viên của bài đăng người khác.");

            var result = await _service.GetShortlistedByPostAsync(postId);
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
            if (!User.IsInRole("Admin") && post.EmployerName != User.Identity!.Name)
                return Forbid("Bạn không thể xem gợi ý của bài đăng người khác.");

            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);
            return Ok(new { success = true, total = items.Count(), data = items });
            }
        }
    }

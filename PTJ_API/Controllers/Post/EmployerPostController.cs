using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.EmployerPostService;

namespace PTJ_API.Controllers.Post
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

        // Helper method chuẩn hóa lỗi 403
        private IActionResult Forbidden(string message)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message
            });
        }

        // =========================================================
        // CREATE
        // =========================================================
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] EmployerPostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);

            if (!User.IsInRole("Admin") && dto.UserID != currentUserId)
                return Forbidden("Bạn không thể đăng bài thay người khác.");

            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
                return BadRequest(new { success = false, message = "Tiêu đề phải có ít nhất 5 ký tự." });

            if (dto.Salary < 0)
                return BadRequest(new { success = false, message = "Mức lương không hợp lệ." });

            if (dto.Salary == null && string.IsNullOrWhiteSpace(dto.SalaryText))
                return BadRequest(new { success = false, message = "Bạn phải nhập mức lương hoặc để 'thỏa thuận'." });

            if (dto.WorkHourStart != null && dto.WorkHourEnd != null)
                {
                if (TimeSpan.Parse(dto.WorkHourStart) >= TimeSpan.Parse(dto.WorkHourEnd))
                    return BadRequest(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu." });
                }


            var result = await _service.CreateEmployerPostAsync(dto);
            return Ok(new { success = true, message = "Đăng bài tuyển dụng thành công.", data = result });
        }

        // =========================================================
        // READ
        // =========================================================
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            {
            var result = await _service.GetAllAsync();

            // Nếu user đăng nhập và là admin → được xem tất cả
            bool isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

            if (!isAdmin)
                {
                result = result.Where(p => p.Status == "Active"); // Chỉ lấy bài active
                }

            return Ok(new { success = true, total = result.Count(), data = result });
            }


        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);

            if (!User.IsInRole("Admin") && currentUserId != userId)
                return Forbidden("Bạn không thể xem bài đăng của người khác.");

            var result = await _service.GetByUserAsync(userId);
            return Ok(new { success = true, total = result.Count(), data = result });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
            {
            //var currentUserId = int.Parse(sub.Value);
            //if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
            //    return Forbidden("Bạn không thể xem bài đăng của người khác.");
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            bool isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

            // Nếu bài đã bị xoá → chỉ admin được xem
            if (!isAdmin && post.Status == "Deleted")
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            return Ok(new { success = true, data = post });
            }


        // =========================================================
        // UPDATE
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployerPostDto dto)
            {
            // Validate model theo DataAnnotations
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            // Kiểm tra bài đăng tồn tại
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng để cập nhật." });

            // Lấy userId từ token
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);

            // Không phải admin → không được sửa bài của người khác
            if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể chỉnh sửa bài đăng của người khác.");

            // Validate chuyên sâu hơn
            if (dto.Salary == null && string.IsNullOrWhiteSpace(dto.SalaryText))
                return BadRequest(new { success = false, message = "Bạn phải nhập mức lương hoặc để 'thỏa thuận'." });

            if (dto.WorkHourStart != null && dto.WorkHourEnd != null)
                {
                if (TimeSpan.Parse(dto.WorkHourStart) >= TimeSpan.Parse(dto.WorkHourEnd))
                    return BadRequest(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu." });
                }

            // Thực hiện update
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

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể xóa bài đăng của người khác.");

            var success = await _service.DeleteAsync(id);
            return Ok(new { success, message = success ? "Đã xóa bài đăng." : "Không thể xóa bài đăng." });
        }

        // =========================================================
        // AI SUGGESTIONS + SHORTLIST
        // =========================================================
        [HttpPost("refresh/{postId}")]
        public async Task<IActionResult> Refresh(int postId)
        {
            var post = await _service.GetByIdAsync(postId);
            if (post == null)
                return NotFound(new { success = false, message = "Bài đăng không tồn tại." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể làm mới bài đăng của người khác.");

            var result = await _service.RefreshSuggestionsAsync(postId);
            return Ok(new { success = true, message = "Đã làm mới đề xuất ứng viên.", data = result });
        }

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

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể xem bài đăng của người khác.");

            var result = await _service.GetShortlistedByPostAsync(postId);
            return Ok(new { success = true, total = result.Count(), data = result });
        }

        [HttpGet("{postId:int}/suggestions")]
        public async Task<IActionResult> GetSuggestions(int postId, [FromQuery] int take = 10, [FromQuery] int skip = 0)
        {
            var post = await _service.GetByIdAsync(postId);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể xem bài đăng của người khác.");

            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);
            return Ok(new { success = true, total = items.Count(), data = items });
        }
    }
}

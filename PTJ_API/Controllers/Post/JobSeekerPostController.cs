using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;

namespace PTJ_API.Controllers.Post
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobSeekerPostController : ControllerBase
    {
        private readonly IJobSeekerPostService _service;

        public JobSeekerPostController(IJobSeekerPostService service)
        {
            _service = service;
        }

        // Helper: Chuẩn hóa trả lỗi 403
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
        public async Task<IActionResult> Create([FromBody] JobSeekerPostDto dto)
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

            if (!string.IsNullOrEmpty(dto.PreferredWorkHourStart) &&
                !string.IsNullOrEmpty(dto.PreferredWorkHourEnd))
                {
                if (TimeSpan.Parse(dto.PreferredWorkHourStart) >= TimeSpan.Parse(dto.PreferredWorkHourEnd))
                    return BadRequest(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu." });
                }


            if (dto.ProvinceId <= 0 || dto.DistrictId <= 0 || dto.WardId <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn Tỉnh/Quận/Huyện/Xã đầy đủ." });


            if (dto.Age is < 15 or > 65)
                return BadRequest(new { success = false, message = "Tuổi không hợp lệ." });

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

            bool isAdmin = User.IsInRole("Admin");
            bool isEmployer = User.IsInRole("Employer");
            bool isJobSeeker = User.IsInRole("JobSeeker");

            // JobSeeker không được xem tất cả
            if (isJobSeeker)
                return Forbid("JobSeeker không có quyền xem danh sách tất cả bài đăng.");

            // Employer chỉ được xem bài Active
            if (isEmployer)
                {
                result = result.Where(x => x.Status == "Active");
                return Ok(new
                    {
                    success = true,
                    total = result.Count(),
                    data = result
                    });
                }

            // Admin → xem tất cả
            return Ok(new
                {
                success = true,
                total = result.Count(),
                data = result
                });
            }


        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            int currentUserId = int.Parse(sub.Value);

            bool isAdmin = User.IsInRole("Admin");
            bool isEmployer = User.IsInRole("Employer");
            bool isJobSeeker = User.IsInRole("JobSeeker");

            var result = await _service.GetByUserAsync(userId);

            // JobSeeker chỉ xem bài của CHÍNH họ
            if (isJobSeeker && userId != currentUserId)
                return Forbid("Bạn không thể xem bài đăng của người khác.");

            // Employer → chỉ xem bài Active
            if (isEmployer)
                result = result.Where(x => x.Status == "Active");

            return Ok(new
                {
                success = true,
                total = result.Count(),
                data = result
                });
            }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            int currentUserId = int.Parse(sub.Value);

            bool isAdmin = User.IsInRole("Admin");
            bool isEmployer = User.IsInRole("Employer");
            bool isJobSeeker = User.IsInRole("JobSeeker");

            // JobSeeker → chỉ xem bài của họ
            if (isJobSeeker && post.UserID != currentUserId)
                return Forbid("Bạn không thể xem bài đăng của người khác.");

            // Employer → chỉ xem bài Active
            if (isEmployer && post.Status != "Active")
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            // Admin → xem tất cả
            return Ok(new { success = true, data = post });
            }




        // =========================================================
        // UPDATE
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] JobSeekerPostDto dto)
            {
            // Validate từ DataAnnotations
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng để cập nhật." });

            // Lấy user từ token
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            var currentUserId = int.Parse(sub.Value);

            // Không phải admin → không được sửa bài người khác
            if (!User.IsInRole("Admin") && existing.UserID != currentUserId)
                return Forbidden("Bạn không thể chỉnh sửa bài đăng của người khác.");

            // ====== VALIDATE NGHIỆP VỤ ======

            // Validate giờ làm
            if (!string.IsNullOrEmpty(dto.PreferredWorkHourStart) &&
                !string.IsNullOrEmpty(dto.PreferredWorkHourEnd))
                {
                if (TimeSpan.Parse(dto.PreferredWorkHourStart) >= TimeSpan.Parse(dto.PreferredWorkHourEnd))
                    return BadRequest(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu." });
                }

            // Validate địa chỉ
            if (dto.ProvinceId <= 0 || dto.DistrictId <= 0 || dto.WardId <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn Tỉnh/Huyện/Xã đầy đủ." });

            // Validate tuổi
            if (dto.Age is < 15 or > 65)
                return BadRequest(new { success = false, message = "Tuổi không hợp lệ." });

            // ====== Gọi service cập nhật ======
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
            if (!User.IsInRole("Admin") && post.UserID != currentUserId)
                return Forbidden("Bạn không thể xóa bài đăng của người khác.");

            var success = await _service.DeleteAsync(id);
            return Ok(new { success, message = success ? "Đã xóa bài đăng." : "Không thể xóa bài đăng." });
        }

        // =========================================================
        // REFRESH SUGGESTIONS
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
            if (!User.IsInRole("Admin") && post.UserID != currentUserId)
                return Forbidden("Bạn không thể làm mới bài đăng của người khác.");

            var result = await _service.RefreshSuggestionsAsync(postId);
            return Ok(new { success = true, message = "Đã làm mới đề xuất việc làm.", data = result });
        }

        // =========================================================
        // SAVE JOBS (SHORTLIST)
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
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbidden("Bạn không thể xem danh sách việc làm đã lưu của người khác.");

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

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            if (!User.IsInRole("Admin") && post.UserID != currentUserId)
                return Forbidden("Bạn không thể xem gợi ý việc làm của bài đăng người khác.");

            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);
            return Ok(new { success = true, total = items.Count(), data = items });
        }
        // =============================================
        // CLOSE
        // =============================================
        [HttpPut("{id}/close")]
        public async Task<IActionResult> Close(int id)
            {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            int currentUserId = int.Parse(sub.Value);

            // Chỉ owner hoặc admin mới được đóng bài
            if (!User.IsInRole("Admin") && post.UserID != currentUserId)
                return Forbid("Bạn không thể đóng bài đăng của người khác.");

            var ok = await _service.CloseJobSeekerPostAsync(id);
            return Ok(new { success = ok, message = "Đã đóng bài đăng." });
            }


        // =============================================
        // REOPEN
        // =============================================
        [HttpPut("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id)
            {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            int currentUserId = int.Parse(sub.Value);

            if (!User.IsInRole("Admin") && post.UserID != currentUserId)
                return Forbid("Bạn không thể mở lại bài đăng của người khác.");

            var ok = await _service.ReopenJobSeekerPostAsync(id);
            return Ok(new { success = ok, message = "Đã mở lại bài đăng." });
            }
        }
    }

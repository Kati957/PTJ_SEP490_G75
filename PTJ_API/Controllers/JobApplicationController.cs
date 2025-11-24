using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Service.JobApplicationService.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    public class JobApplicationController : ControllerBase
        {
        private readonly IJobApplicationService _service;

        public JobApplicationController(IJobApplicationService service)
            {
            _service = service;
            }

        // ỨNG VIÊN NỘP ĐƠN

        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationCreateDto dto)
            {
            if (!ModelState.IsValid)
                {
                return BadRequest(new
                    {
                    success = false,
                    message = "Dữ liệu không hợp lệ.",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            var currentUserId = int.Parse(sub.Value);
            dto.JobSeekerId = currentUserId;

            if (!User.IsInRole("Admin") && dto.JobSeekerId != currentUserId)
                return Forbid("Bạn không thể nộp đơn thay người khác.");

            var (success, error) = await _service.ApplyAsync(dto.JobSeekerId, dto.EmployerPostId, dto.Note, dto.Cvid);
            if (!success)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, message = "Ứng tuyển thành công." });
            }

        // =========================================================
        // ỨNG VIÊN RÚT ĐƠN
        // =========================================================
        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpPut("withdraw")]
        public async Task<IActionResult> Withdraw(int jobSeekerId, int employerPostId)
            {
            if (jobSeekerId <= 0 || employerPostId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin để rút đơn." });

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin user." });

            var currentUserId = int.Parse(sub.Value);

            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbid("Bạn không thể rút đơn của người khác.");

            var result = await _service.WithdrawAsync(jobSeekerId, employerPostId);
            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy đơn ứng tuyển hoặc đơn đã được rút." });

            return Ok(new { success = true, message = "Rút đơn thành công." });
            }

        // =========================================================
        // EMPLOYER XEM DANH SÁCH ỨNG VIÊN
        // =========================================================
        [Authorize(Roles = "Employer,Admin")]
        [HttpGet("by-post/{employerPostId}")]
        public async Task<ActionResult<IEnumerable<JobApplicationResultDto>>> GetByPost(int employerPostId)
            {
            if (employerPostId <= 0)
                return BadRequest(new { success = false, message = "ID bài đăng không hợp lệ." });

            var result = await _service.GetCandidatesByPostAsync(employerPostId);
            return Ok(new { success = true, total = result.Count(), data = result });
            }

        // =========================================================
        // JOBSEEKER XEM ĐƠN ĐÃ ỨNG TUYỂN CỦA CHÍNH HỌ
        // =========================================================
        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpGet("by-seeker/{jobSeekerId}")]
        public async Task<IActionResult> GetBySeeker(int jobSeekerId)
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc thiếu thông tin user." });

            var currentUserId = int.Parse(sub.Value);

            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbid("Bạn không thể xem đơn ứng tuyển của người khác.");

            var result = await _service.GetApplicationsBySeekerAsync(jobSeekerId);

            return Ok(new
                {
                success = true,
                total = result.Count(),
                data = result
                });
            }

        // =========================================================
        // EMPLOYER CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // =========================================================
        [Authorize(Roles = "Employer,Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] JobApplicationUpdateDto dto)
            {
            if (id <= 0)
                return BadRequest(new { success = false, message = "ID không hợp lệ." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { success = false, message = "Thiếu trạng thái cập nhật." });

            // ⭐ THÊM TRẠNG THÁI MỚI
            var validStatuses = new[] { "Interviewing", "Accepted", "Rejected" };

            if (!validStatuses.Contains(dto.Status, System.StringComparer.OrdinalIgnoreCase))
                return BadRequest(new
                    {
                    success = false,
                    message = "Trạng thái không hợp lệ. Chỉ chấp nhận: 'Interviewing', 'Accepted', 'Rejected'."
                    });

            var result = await _service.UpdateStatusAsync(id, dto.Status, dto.Note);
            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy ứng viên hoặc lỗi khi cập nhật." });

            return Ok(new { success = true, message = $"Cập nhật trạng thái thành công: {dto.Status}" });
            }

        // =========================================================
        // DANH SÁCH TRẠNG THÁI HỢP LỆ
        // =========================================================
        [HttpGet("valid-statuses")]
        public IActionResult GetValidStatuses()
            {
            // ⭐ cập nhật theo status mới
            var statuses = new[]
            {
                "Đang chờ xử lý",
                "Đang phỏng vấn",
                "Đã chấp nhận",
                "Đã từ chối",
                "Đã rút lại"
            };

            return Ok(new { success = true, data = statuses });
            }
        }
    }

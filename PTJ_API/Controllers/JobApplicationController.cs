using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Service.JobApplicationService.Interfaces;
using System.Collections.Generic;
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

        // =========================================================
        // ỨNG VIÊN NỘP ĐƠN ỨNG TUYỂN
        // =========================================================
        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationCreateDto dto)
            {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            if (dto.JobSeekerId <= 0 || dto.EmployerPostId <= 0)
                return BadRequest(new { success = false, message = "Thiếu thông tin ứng viên hoặc bài đăng." });

            // 🔒 Chỉ cho phép ứng viên tự nộp đơn của mình
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && dto.JobSeekerId != currentUserId)
                return Forbid("Bạn không thể nộp đơn thay người khác.");

            var result = await _service.ApplyAsync(dto.JobSeekerId, dto.EmployerPostId, dto.Note);

            if (!result)
                return BadRequest(new { success = false, message = "Bạn đã ứng tuyển bài này hoặc có lỗi xảy ra." });

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

            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbid("Bạn không thể rút đơn của người khác.");

            var result = await _service.WithdrawAsync(jobSeekerId, employerPostId);
            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy đơn ứng tuyển hoặc đã bị rút." });

            return Ok(new { success = true, message = "Rút đơn thành công." });
            }

        // =========================================================
        // EMPLOYER XEM DANH SÁCH ỨNG VIÊN CỦA BÀI ĐĂNG
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
        // JOBSEEKER XEM DANH SÁCH BÀI ĐÃ ỨNG TUYỂN
        // =========================================================
        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpGet("by-seeker/{jobSeekerId}")]
        public async Task<ActionResult<IEnumerable<JobApplicationResultDto>>> GetBySeeker(int jobSeekerId)
            {
            var currentUserId = int.Parse(User.FindFirst("sub")!.Value);
            if (!User.IsInRole("Admin") && jobSeekerId != currentUserId)
                return Forbid("Bạn không thể xem danh sách ứng tuyển của người khác.");

            var result = await _service.GetApplicationsBySeekerAsync(jobSeekerId);
            return Ok(new { success = true, total = result.Count(), data = result });
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

            var validStatuses = new[] { "Accepted", "Rejected" };
            if (!validStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Trạng thái không hợp lệ. Chỉ chấp nhận 'Accepted' hoặc 'Rejected'." });

            var result = await _service.UpdateStatusAsync(id, dto.Status, dto.Note);
            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy ứng viên hoặc lỗi khi cập nhật." });

            return Ok(new { success = true, message = $"Cập nhật trạng thái thành công: {dto.Status}" });
            }

        // =========================================================
        // LẤY DANH SÁCH TRẠNG THÁI HỢP LỆ (cho frontend render dropdown)
        // =========================================================
        [HttpGet("valid-statuses")]
        public IActionResult GetValidStatuses()
            {
            var statuses = new[] { "Pending", "Accepted", "Rejected", "Withdrawn" };
            return Ok(new { success = true, data = statuses });
            }
        }
    }

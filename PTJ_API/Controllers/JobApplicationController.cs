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
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationCreateDto dto)
            {
            var result = await _service.ApplyAsync(dto.JobSeekerId, dto.EmployerPostId, dto.Note);
            return result
                ? Ok(new { message = "Ứng tuyển thành công." })
                : BadRequest(new { message = "Ứng viên đã ứng tuyển hoặc lỗi xảy ra." });
            }

        // =========================================================
        // ỨNG VIÊN RÚT ĐƠN
        // =========================================================
        [HttpPut("withdraw")]
        public async Task<IActionResult> Withdraw(int jobSeekerId, int employerPostId)
            {
            var result = await _service.WithdrawAsync(jobSeekerId, employerPostId);
            return result
                ? Ok(new { message = "Rút đơn thành công." })
                : NotFound(new { message = "Không tìm thấy đơn ứng tuyển." });
            }

        // =========================================================
        // EMPLOYER XEM DANH SÁCH ỨNG VIÊN CỦA BÀI ĐĂNG
        // =========================================================
        [HttpGet("by-post/{employerPostId}")]
        public async Task<ActionResult<IEnumerable<JobApplicationResultDto>>> GetByPost(int employerPostId)
            {
            var result = await _service.GetCandidatesByPostAsync(employerPostId);
            return Ok(result);
            }

        // =========================================================
        // JOBSEEKER XEM DANH SÁCH BÀI ĐÃ ỨNG TUYỂN
        // =========================================================
        [HttpGet("by-seeker/{jobSeekerId}")]
        public async Task<ActionResult<IEnumerable<JobApplicationResultDto>>> GetBySeeker(int jobSeekerId)
            {
            var result = await _service.GetApplicationsBySeekerAsync(jobSeekerId);
            return Ok(result);
            }

        // =========================================================
        // EMPLOYER CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // =========================================================
        // =========================================================
        // EMPLOYER CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // =========================================================
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] JobApplicationUpdateDto dto)
            {
            // ✅ Danh sách trạng thái hợp lệ
            var validStatuses = new[] { "Accepted", "Rejected" };

            // ✅ Kiểm tra hợp lệ (không phân biệt hoa thường)
            if (!validStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
                {
                return BadRequest(new
                    {
                    message = "Trạng thái không hợp lệ. Chỉ cho phép 'Accepted' hoặc 'Rejected'."
                    });
                }

            var result = await _service.UpdateStatusAsync(id, dto.Status, dto.Note);
            if (!result)
                return NotFound(new { message = "Không tìm thấy ứng viên." });

            return Ok(new { message = $"Cập nhật trạng thái thành công: {dto.Status}" });
            }
        // =========================================================
        // LẤY DANH SÁCH TRẠNG THÁI HỢP LỆ (cho frontend render dropdown)
        // =========================================================
        [HttpGet("valid-statuses")]
        public IActionResult GetValidStatuses()
            {
            var statuses = new[] { "Accepted", "Rejected" };
            return Ok(statuses);
            }
        }
    }

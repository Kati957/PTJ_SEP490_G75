using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Interfaces.Admin;
using System.Security.Claims;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/system-reports")]
    [Authorize(Roles = "Admin")]
    public class AdminSystemReportController : ControllerBase
    {
        private readonly IAdminSystemReportService _service;

        public AdminSystemReportController(IAdminSystemReportService service)
        {
            _service = service;
        }

        private int GetAdminId()
        {
            return int.Parse(User.FindFirstValue("UserId"));
        }

        // 1️⃣ Danh sách report (có tìm kiếm + phân trang)
        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] string? status,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetSystemReportsAsync(status, keyword, page, pageSize);
            return Ok(result);
        }

        // 2️⃣ Chi tiết một báo cáo
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var detail = await _service.GetSystemReportDetailAsync(id);
            if (detail == null)
                return NotFound(new { message = "Không tìm thấy báo cáo." });

            return Ok(detail);
        }

        // 3️⃣ Xử lý báo cáo (Solved hoặc Rejected)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateSystemReportStatusDto dto)
        {
            int adminId = GetAdminId();

            if (dto.Status != "Solved" && dto.Status != "Rejected")
                return BadRequest(new { message = "Trạng thái không hợp lệ." });

            var success = await _service.UpdateReportStatusAsync(id, adminId, dto.Status, dto.AdminNote);

            if (!success)
                return NotFound(new { message = "Không tìm thấy báo cáo." });

            return Ok(new { message = "Cập nhật trạng thái báo cáo thành công." });
        }
    }
}

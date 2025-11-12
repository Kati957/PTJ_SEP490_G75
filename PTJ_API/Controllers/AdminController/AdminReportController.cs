using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Service.Interfaces.Admin;
using PTJ_Models.DTO.Admin;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : ControllerBase
    {
        private readonly IAdminReportService _svc;
        public AdminReportController(IAdminReportService svc) => _svc = svc;

        // 1️⃣ Danh sách report chưa xử lý (Pending)
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReports(
            [FromQuery] string? reportType = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _svc.GetPendingReportsAsync(reportType, keyword, page, pageSize);
            return Ok(data);
        }

        // 2️⃣ Danh sách report đã xử lý (Solved)
        [HttpGet("solved")]
        public async Task<IActionResult> GetSolvedReports(
            [FromQuery] string? adminEmail = null,
            [FromQuery] string? reportType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _svc.GetSolvedReportsAsync(adminEmail, reportType, page, pageSize);
            return Ok(data);
        }

        // 3️⃣ Xem chi tiết 1 report (View Report Detail)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReportDetail(int id)
        {
            var report = await _svc.GetReportDetailAsync(id);
            if (report == null)
                return NotFound(new { message = $"Report with ID {id} not found." });

            return Ok(report);
        }

        // 4️⃣ Xử lý report (BanUser / DeletePost / Warn / Ignore)
        [HttpPost("resolve/{reportId:int}")]
        public async Task<IActionResult> ResolveReport(int reportId, [FromBody] AdminResolveReportDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var result = await _svc.ResolveReportAsync(reportId, dto, adminId);

            return Ok(new
            {
                message = $"Report {reportId} resolved successfully.",
                result
            });
        }
    }
}

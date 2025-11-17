using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces.Admin;
using PTJ_Models.DTO.Admin;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/system-reports")]
    [Authorize(Roles = "Admin")]
    public class AdminSystemReportController : ControllerBase
    {
        private readonly IAdminSystemReportService _svc;
        public AdminSystemReportController(IAdminSystemReportService svc) => _svc = svc;

        // 1️⃣ Danh sách báo cáo hệ thống
        [HttpGet]
        public async Task<IActionResult> GetSystemReports(
            [FromQuery] string? status = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _svc.GetSystemReportsAsync(status, keyword, page, pageSize);
            return Ok(data);
        }

        // 2️⃣ Chi tiết báo cáo
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSystemReportDetail(int id)
        {
            var data = await _svc.GetSystemReportDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        // 3️⃣ Đánh dấu đã xử lý
        [HttpPost("{id:int}/resolve")]
        public async Task<IActionResult> ResolveSystemReport(int id, [FromBody] AdminResolveSystemReportDto dto)
        {
            var ok = await _svc.MarkReportSolvedAsync(id, dto.Note);
            return ok
                ? Ok(new { message = $"System report {id} marked as solved." })
                : NotFound(new { message = "Report not found." });
        }
    }
}

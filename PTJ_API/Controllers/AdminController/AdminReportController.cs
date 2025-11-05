using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Implementations.Admin;
using PTJ_Models.DTO.Admin;
using System.Security.Claims;
using PTJ_Service.Interfaces.Admin;


namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : ControllerBase
    {
        private readonly IAdminReportService _svc;
        public AdminReportController(IAdminReportService svc) => _svc = svc;

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReports()
            => Ok(await _svc.GetPendingReportsAsync());

        [HttpGet("solved")]
        public async Task<IActionResult> GetSolvedReports()
            => Ok(await _svc.GetSolvedReportsAsync());

        [HttpPost("resolve/{reportId}")]
        public async Task<IActionResult> ResolveReport(int reportId, [FromBody] AdminResolveReportDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
            await _svc.ResolveReportAsync(reportId, dto, adminId);
            return Ok(new { message = $"Report {reportId} resolved successfully." });
        }
    }
}

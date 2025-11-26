using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using System.Security.Claims;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : ControllerBase
    {
        private readonly IAdminReportService _svc;

        public AdminReportController(IAdminReportService svc)
        {
            _svc = svc;
        }


        //Danh sách report pending

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReports(
            [FromQuery] string? reportType = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _svc.GetPendingReportsAsync(reportType, keyword, page, pageSize);
            return Ok(result);
        }



        //Danh sách report đã xử lý

        [HttpGet("solved")]
        public async Task<IActionResult> GetSolvedReports(
            [FromQuery] string? adminEmail = null,
            [FromQuery] string? reportType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _svc.GetSolvedReportsAsync(adminEmail, reportType, page, pageSize);
            return Ok(result);
        }



        //Lấy chi tiết report

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportDetail(int id)
        {
            var detail = await _svc.GetReportDetailAsync(id);

            if (detail == null)
                return NotFound(new { message = "Không tìm thấy báo cáo." });

            return Ok(detail);
        }



        //Xử lý report

        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> ResolveReport(int id, [FromBody] AdminResolveReportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ActionTaken))
                return BadRequest(new { message = "ActionTaken là bắt buộc." });

            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _svc.ResolveReportAsync(id, dto, adminId);

            return Ok(result);
        }
    }
}

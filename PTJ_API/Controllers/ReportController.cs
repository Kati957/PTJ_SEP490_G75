using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Report;
using PTJ_Service.Interfaces;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize] // người dùng phải đăng nhập mới report được
    public class ReportController : ControllerBase
    {
        private readonly IReportService _svc;
        public ReportController(IReportService svc) => _svc = svc;

        private int CurrentUserId
        {
            get
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                return int.TryParse(id, out var uid) ? uid : 0;
            }
        }

        // POST /api/reports/employer-post
        [HttpPost("employer-post")]
        public async Task<IActionResult> ReportEmployerPost([FromBody] CreateEmployerPostReportDto dto)
        {
            var id = await _svc.ReportEmployerPostAsync(CurrentUserId, dto);
            return Ok(new { message = "Báo cáo đã được gửi thành công.", reportId = id });
        }

        // POST /api/reports/jobseeker-post
        [HttpPost("jobseeker-post")]
        public async Task<IActionResult> ReportJobSeekerPost([FromBody] CreateJobSeekerPostReportDto dto)
        {
            var id = await _svc.ReportJobSeekerPostAsync(CurrentUserId, dto);
            return Ok(new { message = "Báo cáo đã được gửi thành công.", reportId = id });
        }

        // GET /api/reports/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports()
            => Ok(await _svc.GetMyReportsAsync(CurrentUserId));
    }
}

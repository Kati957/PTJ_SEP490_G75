using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]   
    public class ReportController : ControllerBase
    {
        private readonly IReportService _svc;

        public ReportController(IReportService svc)
        {
            _svc = svc;
        }

        // 1️⃣ TẠO REPORT BÀI ĐĂNG (EmployerPost / JobSeekerPost)

        [HttpPost("post")]
        public async Task<IActionResult> ReportPost([FromBody] CreatePostReportDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Thiếu dữ liệu report." });

            if (dto.PostId <= 0)
                return BadRequest(new { message = "PostId không hợp lệ." });

            if (string.IsNullOrWhiteSpace(dto.PostType))
                return BadRequest(new { message = "PostType không hợp lệ." });

            int reporterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            int reportId = await _svc.ReportPostAsync(reporterId, dto);

            return Ok(new
            {
                message = "Đã gửi báo cáo thành công.",
                reportId = reportId
            });
        }



        // 2️⃣ LẤY DANH SÁCH REPORT CỦA USER

        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports()
        {
            int reporterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reports = await _svc.GetMyReportsAsync(reporterId);

            return Ok(reports);
        }
    }
}

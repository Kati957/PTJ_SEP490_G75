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

        // TẠO REPORT BÀI ĐĂNG
        [HttpPost("post")]
        public async Task<IActionResult> ReportPost([FromBody] CreatePostReportDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Thiếu dữ liệu báo cáo." });

            if (dto.PostId <= 0)
                return BadRequest(new { message = "PostId không hợp lệ." });

            if (string.IsNullOrWhiteSpace(dto.PostType) ||
                (dto.PostType != "EmployerPost" && dto.PostType != "JobSeekerPost"))
                return BadRequest(new { message = "PostType phải là EmployerPost hoặc JobSeekerPost." });

            if (string.IsNullOrWhiteSpace(dto.ReportType))
                return BadRequest(new { message = "Vui lòng chọn loại báo cáo (ReportType)." });

            // Validate ReportType tiếng Việt
            var allowedTypes = new[]
            {
                "Lừa đảo",
                "Sai thông tin",
                "Không phù hợp",
                "Spam",
                "Không phải tuyển dụng",
                "Khác"
            };

            if (!allowedTypes.Contains(dto.ReportType))
                return BadRequest(new { message = "ReportType không hợp lệ." });

            int reporterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            int reportId = await _svc.ReportPostAsync(reporterId, dto);

            return Ok(new
            {
                success = true,
                message = "Gửi báo cáo thành công.",
                reportId = reportId
            });
        }


        // XEM DANH SÁCH REPORT CỦA USER
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports()
        {
            int reporterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reports = await _svc.GetMyReportsAsync(reporterId);

            return Ok(new
            {
                success = true,
                data = reports
            });
        }
    }
}

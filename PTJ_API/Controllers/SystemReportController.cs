using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTOs;
using PTJ_Service.SystemReportService.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/system-reports")]
    [Authorize] // user nào cũng có thể gửi report
    public class SystemReportController : ControllerBase
    {
        private readonly ISystemReportService _service;

        public SystemReportController(ISystemReportService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue("UserId"));
        }

        // 1️⃣ USER TẠO SYSTEM REPORT
        [HttpPost]
        public async Task<IActionResult> Create(SystemReportCreateDto dto)
        {
            int userId = GetUserId();
            await _service.CreateReportAsync(userId, dto);

            return Ok(new { message = "Gửi báo cáo hệ thống thành công. Cảm ơn bạn đã phản hồi!" });
        }

        // 2️⃣ USER XEM REPORT CỦA CHÍNH MÌNH
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports()
        {
            int userId = GetUserId();
            var data = await _service.GetReportsByUserAsync(userId);

            return Ok(data);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.ReportDTO;
using PTJ_Service.SystemReportService.Interfaces;

namespace PTJ_API.Controllers.AdminController
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemReportController : ControllerBase
    {
        private readonly ISystemReportService _reportService;

        public SystemReportController(ISystemReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] SystemReportCreateDto dto)
        {
            await _reportService.CreateReportAsync(dto);
            return Ok(new { message = "Report submitted successfully" });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetReportsByUser(int userId)
        {
            var reports = await _reportService.GetReportsByUserAsync(userId);
            return Ok(reports);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }

        [HttpPut("{reportId}")]
        public async Task<IActionResult> UpdateStatus(int reportId, [FromBody] SystemReportUpdateDto dto)
        {
            var success = await _reportService.UpdateStatusAsync(reportId, dto.Status);
            if (!success) return NotFound(new { message = "Report not found" });

            return Ok(new { message = "Report status updated successfully" });
        }
    }
}

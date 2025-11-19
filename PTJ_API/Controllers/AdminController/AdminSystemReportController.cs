using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTOs;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/system-reports")]
public class AdminSystemReportController : ControllerBase
{
    private readonly IAdminSystemReportService _service;

    public AdminSystemReportController(IAdminSystemReportService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports(
        string? status, string? keyword, int page = 1, int pageSize = 10)
    {
        var result = await _service.GetSystemReportsAsync(status, keyword, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await _service.GetSystemReportDetailAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy báo cáo." });
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStatus(int id, SystemReportUpdateDto dto)
    {
        var success = await _service.UpdateStatusAsync(id, dto.Status);
        if (!success) return NotFound(new { message = "Không tìm thấy báo cáo." });
        return Ok(new { message = "Cập nhật trạng thái thành công." });
    }
}

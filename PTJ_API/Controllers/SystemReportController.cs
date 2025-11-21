using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTOs;
using PTJ_Models.Models;
using PTJ_Service.SystemReportService.Interfaces;

[Authorize]
[ApiController]
[Route("api/system-reports")]
public class SystemReportController : ControllerBase
{
    private readonly ISystemReportService _service;

    public SystemReportController(ISystemReportService service)
    {
        _service = service;
    }

    private int GetUserId() => int.Parse(User.FindFirst("UserId")!.Value);

    [HttpPost]
    public async Task<IActionResult> Create(SystemReportCreateDto dto)
    {
        await _service.CreateReportAsync(GetUserId(), dto);
        return Ok(new { message = "Gửi báo cáo thành công." });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyReports()
    {
        var result = await _service.GetReportsByUserAsync(GetUserId());
        return Ok(result);
    }
}

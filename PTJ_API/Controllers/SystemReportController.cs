using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.SystemReportService.Interfaces;
using System.Security.Claims;

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

    // Lấy UserId an toàn, không phụ thuộc vào string "UserId"
    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new Exception("Không thể xác định người dùng từ token.");

        return int.Parse(id);
    }

    //Tạo báo cáo hệ thống
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemReportCreateDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });

            await _service.CreateReportAsync(GetUserId(), dto);

            return Ok(new
            {
                success = true,
                message = "Gửi báo cáo thành công."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // Lấy các báo cáo của user hiện tại
    [HttpGet("my")]
    public async Task<IActionResult> MyReports()
    {
        try
        {
            var result = await _service.GetReportsByUserAsync(GetUserId());

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}

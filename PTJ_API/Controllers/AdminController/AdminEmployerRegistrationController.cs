using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Interfaces.Admin;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/employer-registrations")]
    [Authorize(Roles = "Admin")]
    public class AdminEmployerRegistrationController : ControllerBase
    {
        private readonly IAdminEmployerRegistrationService _svc;

        public AdminEmployerRegistrationController(IAdminEmployerRegistrationService svc)
        {
            _svc = svc;
        }
        [HttpGet]
        public async Task<IActionResult> GetRequests(
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            var data = await _svc.GetRequestsAsync(status, keyword, page, pageSize);
            return Ok(data);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var detail = await _svc.GetDetailAsync(id);
            if (detail == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ." });

            return Ok(detail);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = int.Parse(User.FindFirst("sub")?.Value ?? "0");

            await _svc.ApproveAsync(id, adminId);
            return Ok(new { message = "Duyệt hồ sơ thành công." });
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] AdminEmployerRegRejectDto dto)
        {
            await _svc.RejectAsync(id, dto);
            return Ok(new { message = "Đã từ chối hồ sơ." });
        }

        [HttpPost("google/{id:int}/approve")]
        public async Task<IActionResult> ApproveGoogle(int id)
        {
            var adminId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            await _svc.ApproveEmployerGoogleAsync(id, adminId);

            return Ok(new { message = "Duyệt hồ sơ Google thành công." });
        }

        [HttpPost("google/{id:int}/reject")]
        public async Task<IActionResult> RejectGoogle(int id, AdminEmployerRegRejectDto dto)
        {
            await _svc.RejectGoogleAsync(id, dto);
            return Ok(new { message = "Đã từ chối hồ sơ Google." });
        }
    }
}

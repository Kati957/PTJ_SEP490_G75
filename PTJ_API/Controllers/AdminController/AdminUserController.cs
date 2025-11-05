using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Admin.Interfaces;
using PTJ_Models.DTO.Admin;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _svc;
        public AdminUserController(IAdminUserService svc) => _svc = svc;

        //  GET: Danh sách người dùng (có filter + phân trang)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isVerified,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _svc.GetAllUsersAsync(role, isActive, isVerified, keyword, page, pageSize);
            return Ok(data);
        }

        //  GET: Chi tiết người dùng
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var data = await _svc.GetUserDetailAsync(id);
            return data is null ? NotFound(new { message = "User not found" }) : Ok(data);
        }

        //  POST: Khóa / Mở khóa tài khoản
        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            await _svc.ToggleUserActiveAsync(id);
            return Ok(new { message = "User active status toggled successfully." });
        }

        //  GET: Danh sách đầy đủ (dashboard)
        [HttpGet("full")]
        public async Task<IActionResult> GetAllUserFull()
        {
            var data = await _svc.GetAllUserFullAsync();
            return Ok(data);
        }
    }
}

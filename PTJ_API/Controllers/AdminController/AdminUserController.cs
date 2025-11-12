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

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isVerified = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
            => Ok(await _svc.GetUsersAsync(role, isActive, isVerified, keyword, page, pageSize));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var data = await _svc.GetUserDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            await _svc.ToggleActiveAsync(id);
            return Ok(new { message = "User active toggled successfully." });
        }
    }
}

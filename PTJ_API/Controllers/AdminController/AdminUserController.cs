using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Admin.Interfaces;
using PTJ_Models.DTO.Admin;
using System.Security.Claims;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _svc;
        public AdminUserController(IAdminUserService svc) => _svc = svc;

        //  Lấy AdminId từ JWT
        private int AdminId =>
            int.Parse(User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new Exception("Token missing userId"));

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
            return Ok(new { message = "Cập nhật trạng thái hoạt động của người dùng thành công." });
        }

        //  NEW: Admin khóa user + nhập lý do + gửi thông báo
        [HttpPost("{id:int}/ban")]
        public async Task<IActionResult> BanUser(int id, [FromBody] BanUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "Vui lòng cung cấp lý do." });

            await _svc.BanUserAsync(id, dto.Reason, AdminId);

            return Ok(new
            {
                message = "Tài khoản người dùng đã bị khóa.",
                userId = id,
                reason = dto.Reason
            });
        }
    }
}

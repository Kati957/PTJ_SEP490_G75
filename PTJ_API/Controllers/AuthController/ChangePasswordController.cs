using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Auth;
using PTJ_Service.AuthService.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers.AuthController
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // chỉ người đăng nhập mới gọi được
    public class ChangePasswordController : ControllerBase
    {
        private readonly IChangePasswordrService _userService;

        public ChangePasswordController(IChangePasswordrService userService)
        {
            _userService = userService;
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Mã token không hợp lệ.");

            int userId = int.Parse(userIdClaim);
            var result = await _userService.ChangePasswordAsync(userId, dto);

            if (result)
                return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });

            return BadRequest(new { message = "Thay đổi mật khẩu không thành công." });
        }
    }
}


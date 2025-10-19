using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Auth;
using PTJ_Service.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // chỉ người đăng nhập mới gọi được
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Invalid token.");

            int userId = int.Parse(userIdClaim);
            var result = await _userService.ChangePasswordAsync(userId, dto);

            if (result)
                return Ok(new { message = "Password changed successfully. Please log in again." });

            return BadRequest(new { message = "Failed to change password." });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PTJ_Models.DTO.Auth;
using PTJ_Service.AuthService.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers.AuthController
{
    [ApiController]
    [Route("api/change-password")]
    public class ChangePasswordController : ControllerBase
    {
        private readonly IChangePasswordService _svc;
        private readonly IConfiguration _cfg;

        public ChangePasswordController(IChangePasswordService svc, IConfiguration cfg)
        {
            _svc = svc;
            _cfg = cfg;
        }

        private int GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null) throw new Exception("Token không hợp lệ.");
            return int.Parse(id);
        }

        // 1️⃣ Gửi email xác nhận đổi mật khẩu
        [Authorize]
        [HttpPost("request")]
        public async Task<IActionResult> RequestChange([FromBody] RequestChangePasswordDto dto)
        {
            int userId = GetUserId();
            await _svc.RequestChangePasswordAsync(userId, dto);
            return Ok(new { message = "Đã gửi email xác nhận đổi mật khẩu." });
        }

        // 2️⃣ Người dùng bấm link trong email → BE verify → redirect sang FE
        [AllowAnonymous]
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyToken([FromQuery] string token)
        {
            var allowed = await _svc.VerifyChangePasswordTokenAsync(token);

            if (!allowed)
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");

            // URL FE để nhập mật khẩu mới
            var feUrl = $"{_cfg["Frontend:BaseUrl"]}/set-new-password?token={token}";
            return Redirect(feUrl);
        }

        // 3️⃣ FE gọi API này để đặt mật khẩu mới
        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmChangePasswordDto dto)
        {
            await _svc.ConfirmChangePasswordAsync(dto);
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
    }
}

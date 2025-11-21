using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Auth;
using PTJ_Service.AuthService.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PTJ_API.Controllers.AuthController
{
    [ApiController]
    [Route("api/change-password")]
    [Authorize] 
    public class ChangePasswordController : ControllerBase
    {
        private readonly IChangePasswordService _svc;

        public ChangePasswordController(IChangePasswordService svc)
        {
            _svc = svc;
        }

        private int GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null) throw new Exception("Không thể tìm thấy userId trong token.");
            return int.Parse(id);
        }

  
        // 1️⃣ API: Yêu cầu đổi mật khẩu -> gửi email xác nhận

        [HttpPost("request")]
        public async Task<IActionResult> RequestChangePassword([FromBody] RequestChangePasswordDto dto)
        {
            int userId = GetUserId();

            await _svc.RequestChangePasswordAsync(userId, dto.CurrentPassword);

            return Ok(new
            {
                message = "Đã gửi email xác nhận đổi mật khẩu. Vui lòng kiểm tra email của bạn."
            });
        }


        // 2️⃣ API: FE kiểm tra token (khi user bấm link email)
        // Không cần Authorize vì FE gọi không có token

        [AllowAnonymous]
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyChangePassword([FromQuery] string token)
        {
            bool allowed = await _svc.VerifyChangePasswordRequestAsync(token);
            return Ok(new { allowed });
        }


        // 3️⃣ API: Đổi mật khẩu sau khi user đã xác nhận email
        // Không Authorize vì user đổi qua link email
 
        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmChangePassword([FromBody] ConfirmChangePasswordDto dto)
        {
            bool success = await _svc.ChangePasswordAsync(dto);

            if (success)
                return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });

            return BadRequest(new { message = "Không thể đổi mật khẩu." });
        }
    }
}

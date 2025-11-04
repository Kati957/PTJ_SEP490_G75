using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Models.DTO.Auth;

namespace PTJ_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _svc;
    private readonly IConfiguration _cfg;

    private string? IP => HttpContext.Connection.RemoteIpAddress?.ToString();

    public AuthController(IAuthService svc, IConfiguration cfg)
    {
        _svc = svc;
        _cfg = cfg;
    }

    // 1️⃣ Đăng ký JobSeeker
    
    [HttpPost("register/jobseeker")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterJobSeeker([FromBody] RegisterJobSeekerDto dto)
    {
        try
        {
            var result = await _svc.RegisterJobSeekerAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
    // 2️⃣ Xác thực email (Swagger hoặc FE gọi POST)
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest dto)
    {
        try
        {
            await _svc.VerifyEmailAsync(dto.Token);
            return Ok(new { message = "Email verified successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class VerifyEmailRequest
    {
        public string Token { get; set; } = string.Empty;
    }

     
    // 3️⃣ Xác thực email qua link
     
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmailLink([FromQuery] string token)
    {
        try
        {
            var decoded = WebUtility.UrlDecode(token);
            await _svc.VerifyEmailAsync(decoded);

            var redirectUrl = $"{_cfg["Frontend:BaseUrl"]}/verify-success";
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            var redirectUrl = $"{_cfg["Frontend:BaseUrl"]}/verify-failed?error={Uri.EscapeDataString(ex.Message)}";
            return Redirect(redirectUrl);
        }
    }

    // 4️⃣ Gửi lại email xác thực
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerifyDto dto)
    {
        await _svc.ResendVerificationAsync(dto.Email);
        return Ok(new { message = "Verification email resent if account exists." });
    }

    // 5️⃣ Đăng nhập Email/Password
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _svc.LoginAsync(dto, IP);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 6️⃣ Refresh Token
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        try
        {
            var result = await _svc.RefreshAsync(dto.RefreshToken, dto.DeviceInfo, IP);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 7️⃣ Logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
    {
        await _svc.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    // 8️⃣ Lấy thông tin người dùng hiện tại (me)
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;
        var email = User.FindFirstValue("email");
        var username = User.FindFirst("username")?.Value;
        var verified = User.FindFirst("verified")?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        return Ok(new { id, email, username, verified, roles });
    }

    // 9️⃣ Nâng cấp Employer
    [Authorize]
    [HttpPost("register/employer")]
    public async Task<IActionResult> UpgradeToEmployer([FromBody] RegisterEmployerDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _svc.UpgradeToEmployerAsync(userId, dto, IP);
        return Ok(result);
    }

    // 🔟 Quên mật khẩu
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _svc.RequestPasswordResetAsync(dto.Email);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    // 11️⃣ Reset mật khẩu
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _svc.ResetPasswordAsync(dto);
        return Ok(new { message = "Password reset successfully." });
    }

    // 12️⃣ Đăng nhập bằng Google OAuth
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var result = await _svc.GoogleLoginAsync(dto, IP);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

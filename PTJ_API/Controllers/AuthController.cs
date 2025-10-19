using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Auth;
using System.Net; // dùng cho UrlDecode

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

    // ---------------------------
    // 1️⃣ Đăng ký Job Seeker
    // ---------------------------
    [HttpPost("register/jobseeker")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterJobSeeker(RegisterJobSeekerDto dto)
    {
        await _svc.RegisterJobSeekerAsync(dto);
        return Ok(new { message = "Please check your email to verify your account." });
    }

    // ---------------------------
    // 2️⃣ Xác thực email (Swagger hoặc FE gọi POST)
    // ---------------------------
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

    // ---------------------------
    // 3️⃣ Xác thực email (qua link trong email)
    // ---------------------------
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmailLink([FromQuery] string token)
    {
        try
        {
            // decode token trong query URL
            var decoded = WebUtility.UrlDecode(token);
            await _svc.VerifyEmailAsync(decoded);

            // redirect về FE (trang thành công)
            var redirectUrl = $"{_cfg["Frontend:BaseUrl"]}/verify-success";
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            // redirect về trang lỗi
            var redirectUrl = $"{_cfg["Frontend:BaseUrl"]}/verify-failed?error={Uri.EscapeDataString(ex.Message)}";
            return Redirect(redirectUrl);
        }
    }

    // ---------------------------
    // 4️⃣ Gửi lại email xác thực
    // ---------------------------
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerifyDto dto)
    {
        await _svc.ResendVerificationAsync(dto.Email);
        return Ok(new { message = "Verification email resent if account exists." });
    }

    // ---------------------------
    // 5️⃣ Đăng nhập
    // ---------------------------
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        => Ok(await _svc.LoginAsync(dto, IP));

    // ---------------------------
    // 6️⃣ Refresh Token
    // ---------------------------
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshDto dto)
        => Ok(await _svc.RefreshAsync(dto.RefreshToken, dto.DeviceInfo, IP));

    // ---------------------------
    // 7️⃣ Logout
    // ---------------------------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
    {
        await _svc.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    // ---------------------------
    // 8️⃣ Lấy thông tin user hiện tại
    // ---------------------------
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

    // ---------------------------
    // 9️⃣ Nâng cấp Employer
    // ---------------------------
    [Authorize]
    [HttpPost("register/employer")]
    public async Task<ActionResult<AuthResponseDto>> UpgradeToEmployer(RegisterEmployerDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        return Ok(await _svc.UpgradeToEmployerAsync(userId, dto, IP));
    }

    // ---------------------------
    // 🔟 Quên mật khẩu
    // ---------------------------
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Forgot(ForgotPasswordDto dto)
    {
        await _svc.RequestPasswordResetAsync(dto.Email);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    // ---------------------------
    // 11️⃣ Reset mật khẩu
    // ---------------------------
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Reset(ResetPasswordDto dto)
    {
        await _svc.ResetPasswordAsync(dto);
        return Ok(new { message = "Password reset successfully." });
    }

    // ---------------------------
    // 12️⃣ Đăng nhập Google
    // ---------------------------
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Google(GoogleLoginDto dto)
        => Ok(await _svc.GoogleLoginAsync(dto, IP));
}

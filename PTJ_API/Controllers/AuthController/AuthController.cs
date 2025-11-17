using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Models.DTO.Auth;
using System.Net;
using PTJ_Service.AuthService.Interfaces;

namespace PTJ_API.Controllers.AuthController;

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

    //  Đăng ký Job Seeker
    [HttpPost("register/jobseeker")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterJobSeeker(RegisterJobSeekerDto dto)
    {
        await _svc.RegisterJobSeekerAsync(dto);
        return Ok(new { message = "Please check your email to verify your account." });
    }

    //  Đăng ký Employer (chọn role ngay từ đầu)
    [HttpPost("register/employer")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterEmployer(RegisterEmployerDto dto)
    {
        await _svc.RegisterEmployerAsync(dto);
        return Ok(new { message = "Please check your email to verify your account." });
    }

    //  Xác thực email (Swagger hoặc FE gọi POST)
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

    //  Xác thực email (qua link trong email)
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

    //  Gửi lại email xác thực
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerifyDto dto)
    {
        await _svc.ResendVerificationAsync(dto.Email);
        return Ok(new { message = "Verification email resent if account exists." });
    }

    //  Đăng nhập
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        => Ok(await _svc.LoginAsync(dto, IP));

    //  Refresh Token
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshDto dto)
        => Ok(await _svc.RefreshAsync(dto.RefreshToken, dto.DeviceInfo, IP));

    //  Logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
    {
        await _svc.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    //  Lấy thông tin user hiện tại
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

    // Quên mật khẩu
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Forgot(ForgotPasswordDto dto)
    {
        await _svc.RequestPasswordResetAsync(dto.Email);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    // Reset mật khẩu
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Reset(ResetPasswordDto dto)
    {
        await _svc.ResetPasswordAsync(dto);
        return Ok(new { message = "Password reset successfully." });
    }

    //// Đăng nhập Google (role chọn ngay từ đầu)
    //[HttpPost("google")]
    //[AllowAnonymous]
    //public async Task<ActionResult<AuthResponseDto>> Google(
    //    [FromQuery] string? role,
    //    [FromBody] GoogleLoginDto dto)
    //    => Ok(await _svc.GoogleLoginAsync(dto, IP, role));
    // =====================================================

    // Đăng nhập Google (2 bước cho phép chọn role sau)
    // Xác thực Google token, kiểm tra user tồn tại hay chưa
    [HttpPost("google/prepare")]
    [AllowAnonymous]
    public async Task<IActionResult> GooglePrepare(GoogleLoginDto dto)
        => Ok(await _svc.GooglePrepareAsync(dto));

    // Hoàn tất đăng ký sau khi chọn role
    [HttpPost("google/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleComplete(GoogleCompleteDto dto)
        => Ok(await _svc.GoogleCompleteAsync(dto, IP));

}

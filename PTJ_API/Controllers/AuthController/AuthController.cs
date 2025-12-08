using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;
using PTJ_Models.DTO.Auth;
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

    [HttpPost("register/jobseeker")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterJobSeeker(RegisterJobSeekerDto dto)
    {
        await _svc.RegisterJobSeekerAsync(dto);
        return Ok(new { message = "Vui lòng kiểm tra email để xác minh tài khoản của bạn." });
    }

    [HttpPost("register/employer")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterEmployer(RegisterEmployerDto dto)
    {
        var result = await _svc.SubmitEmployerRegistrationAsync(dto);
        return Ok(result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest dto)
    {
        try
        {
            await _svc.VerifyEmailAsync(dto.Token);
            return Ok(new { message = "Xác minh email thành công." });
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

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmailLink([FromQuery] string token)
    {
        try
        {
            var decoded = WebUtility.UrlDecode(token);
            await _svc.VerifyEmailAsync(decoded);
            return Redirect($"{_cfg["Frontend:BaseUrl"]}/verify-success");
        }
        catch (Exception ex)
        {
            return Redirect($"{_cfg["Frontend:BaseUrl"]}/verify-failed?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerifyDto dto)
    {
        await _svc.ResendVerificationAsync(dto.Email);
        return Ok(new { message = "Email xác minh đã được gửi lại." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid request",
                errors = ModelState.Values.SelectMany(x => x.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var result = await _svc.LoginAsync(dto, IP);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshDto dto)
        => Ok(await _svc.RefreshAsync(dto.RefreshToken, dto.DeviceInfo, IP));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
    {
        await _svc.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Đăng xuất thành công." });
    }

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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Forgot(ForgotPasswordDto dto)
    {
        await _svc.RequestPasswordResetAsync(dto.Email);
        return Ok(new { message = "Đã gửi yêu cầu đặt lại mật khẩu (nếu email hợp lệ)." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> Reset(ResetPasswordDto dto)
    {
        await _svc.ResetPasswordAsync(dto);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập." });
    }

    [HttpPost("google/prepare")]
    [AllowAnonymous]
    public async Task<IActionResult> GooglePrepare(GoogleLoginDto dto)
        => Ok(await _svc.GooglePrepareAsync(dto));

    [HttpPost("google/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleComplete(GoogleCompleteDto dto)
        => Ok(await _svc.GoogleCompleteAsync(dto, IP));
}

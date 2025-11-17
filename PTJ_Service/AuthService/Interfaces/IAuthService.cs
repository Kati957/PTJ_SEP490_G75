using PTJ_Models.DTO.Auth;

namespace PTJ_Service.AuthService.Interfaces;

public interface IAuthService
{

    Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto);
    Task<AuthResponseDto> RegisterEmployerAsync(RegisterEmployerDto dto);
    Task VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip);
    Task LogoutAsync(string refreshToken);
    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);

    //Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ip, string? role = "JobSeeker");
    Task<object> GooglePrepareAsync(GoogleLoginDto dto);
    Task<AuthResponseDto> GoogleCompleteAsync(GoogleCompleteDto dto, string? ip);
}

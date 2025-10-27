using PTJ_Models.DTO.Auth;

namespace PTJ_Service.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto);
    Task VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email);

    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip);
    Task LogoutAsync(string refreshToken);

    Task<AuthResponseDto> UpgradeToEmployerAsync(int userId, RegisterEmployerDto dto, string? ip);

    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);

    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ip);
}

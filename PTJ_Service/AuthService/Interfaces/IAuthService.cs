using System.Threading.Tasks;
using PTJ_Models.DTO.Auth;

namespace PTJ_Service.AuthService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto);
        Task<object> SubmitEmployerRegistrationAsync(RegisterEmployerDto dto);
        Task<object> GooglePrepareAsync(GoogleLoginDto dto);
        Task<AuthResponseDto> GoogleCompleteAsync(GoogleCompleteDto dto, string? ip);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip);
        Task LogoutAsync(string refreshToken);
        Task VerifyEmailAsync(string token);
        Task ResendVerificationAsync(string email);
        Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip);
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}

using PTJ_Data;
using PTJ_Models.DTO.Auth;
using PTJ_Models.Models;

namespace PTJ_Service.AuthService.Interfaces;

public interface ITokenService
{
    Task<AuthResponseDto> IssueAsync(User user, string? deviceInfo, string? ip);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip);
    Task RevokeAsync(string refreshToken);
}

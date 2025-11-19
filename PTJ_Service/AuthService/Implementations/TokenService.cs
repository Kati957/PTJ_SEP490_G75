using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PTJ_Models.Models;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Data;
using PTJ_Models;

public sealed class TokenService : ITokenService
{
    private readonly JobMatchingDbContext _db;
    private readonly IConfiguration _cfg;

    public TokenService(JobMatchingDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    //  Lấy roles của user
    private async Task<List<string>> GetRolesAsync(int userId)
    {
        var user = await _db.Users.Include(u => u.Roles)
                                  .FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.Roles.Select(r => r.RoleName).ToList() ?? new();
    }

    //  Tạo AccessToken
    private (string token, string jti, DateTime exp) CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_cfg["Jwt:SigningKey"]!));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new("verified", user.IsVerified ? "true" : "false"),
            new("IsVerified", user.IsVerified.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var exp = DateTime.UtcNow.AddMinutes(int.Parse(_cfg["Jwt:AccessTokenMinutes"]!));
        var jwt = new JwtSecurityToken(
            _cfg["Jwt:Issuer"], _cfg["Jwt:Audience"], claims,
            notBefore: DateTime.UtcNow, expires: exp, signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, jti, exp);
    }

    private static string NewRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    //  1️⃣ Cấp token mới (login / register / google)
    public async Task<AuthResponseDto> IssueAsync(User user, string? deviceInfo, string? ip)
    {
        //  Xóa các refresh token cũ còn tồn tại (1 user = 1 token)
        var oldTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.UserId)
            .ToListAsync();

        if (oldTokens.Any())
        {
            _db.RefreshTokens.RemoveRange(oldTokens);
            await _db.SaveChangesAsync();
        }

        // Tạo access + refresh token
        var roles = await GetRolesAsync(user.UserId);
        var (access, jti, exp) = CreateAccessToken(user, roles);
        var refresh = NewRefreshToken();
        var days = int.Parse(_cfg["Jwt:RefreshTokenDays"]!);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            Token = refresh,
            JwtId = jti,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            DeviceInfo = deviceInfo,
            Ipaddress = ip,
            IsRevoked = false
        });
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = access,
            ExpiresIn = (int)(exp - DateTime.UtcNow).TotalSeconds,
            RefreshToken = refresh,
            User = new
            {
                id = user.UserId,
                email = user.Email,
                username = user.Username,
                roles,
                verified = user.IsVerified
            }
        };
    }

    //  2️⃣ Làm mới token
    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip)
    {
        var rt = await _db.RefreshTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (rt == null || rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn.");

        //  Xóa refresh token cũ trước khi cấp mới
        _db.RefreshTokens.Remove(rt);
        await _db.SaveChangesAsync();

        // Cấp token mới
        return await IssueAsync(rt.User, deviceInfo, ip);
    }

    //  3️⃣ Logout → xóa token khỏi DB
    public async Task RevokeAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (rt == null) return;

        _db.RefreshTokens.Remove(rt);
        await _db.SaveChangesAsync();
    }

    //  4️⃣ Logout toàn bộ → xóa tất cả refresh token của user
    public async Task RevokeAllAsync(int userId)
    {
        var tokens = _db.RefreshTokens.Where(t => t.UserId == userId);
        _db.RefreshTokens.RemoveRange(tokens);
        await _db.SaveChangesAsync();
    }
}

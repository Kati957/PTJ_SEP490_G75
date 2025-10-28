using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PTJ_Data;
using PTJ_Models.DTO.Auth;
using PTJ_Service.Helpers;
using PTJ_Models.Models;
using Microsoft.AspNetCore.WebUtilities;
using PTJ_Service.AuthService.Interfaces;

namespace PTJ_Service.AuthService.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly JobMatchingDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthService> _log;

    public AuthService(JobMatchingDbContext db, IPasswordHasher hasher, ITokenService tokens, IEmailSender email, IConfiguration cfg, ILogger<AuthService> log)
    { _db = db; _hasher = hasher; _tokens = tokens; _email = email; _cfg = cfg; _log = log; }

    public async Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // Kiểm tra trùng email
        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new Exception("Email already exists.");

        // Tạo user mới
        var user = new User
        {
            Email = email,
            Username = email.Split('@')[0],
            PasswordHash = _hasher.Hash(dto.Password),
            IsActive = true,
            IsVerified = false, // soft verify
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Gán 1 role duy nhất: JobSeeker
        await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "JobSeeker");


        // Tạo JobSeekerProfile (nếu chưa có)
        if (!await _db.JobSeekerProfiles.AnyAsync(p => p.UserId == user.UserId))
        {
            _db.JobSeekerProfiles.Add(new JobSeekerProfile
            {
                UserId = user.UserId,
                FullName = dto.FullName
            });
            await _db.SaveChangesAsync();
        }

        // Tạo token xác thực email (Base64UrlEncode)
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await _db.SaveChangesAsync();

        // Gửi email xác thực (async nền)
        _ = Task.Run(async () =>
        {
            try
            {
                var baseUrlApi = _cfg["App:BaseUrl"] ?? "https://localhost:7100";
                var verifyUrl = $"{baseUrlApi}/api/Auth/verify-email?token={token}";

                var body = $@"
                <h2>Welcome to PTJ!</h2>
                <p>Hi <b>{WebUtility.HtmlEncode(user.Username)}</b>,</p>
                <p>Thanks for registering. Please verify your email by clicking below:</p>
                <a href='{verifyUrl}' 
                   style='background:#007bff;color:#fff;padding:10px 20px;text-decoration:none;border-radius:4px;'>Verify Email</a>
                <p>This link will expire in 30 minutes.</p>";

                await _email.SendEmailAsync(user.Email, "Xác thực tài khoản PTJ", body);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Send verification email failed for {Email}", user.Email);
            }
        });

        // Sinh token đăng nhập (deviceInfo/ip không có trong method này → truyền null)
        var response = await _tokens.IssueAsync(user, deviceInfo: null, ip: null);

        // Gắn cảnh báo mềm nếu chưa verify
        if (!user.IsVerified)
            response.Warning = "Your email is not verified. Please check your inbox to verify your account.";

        return response;
    }



    public async Task VerifyEmailAsync(string token)
    {
        var decoded = WebUtility.UrlDecode(token);

        // Tìm token hợp lệ trong DB
        var ev = await _db.EmailVerificationTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == decoded && x.UsedAt == null);

        if (ev == null)
            throw new Exception("Token not found or already used.");

        if (ev.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Token has expired.");

        // Đánh dấu token đã sử dụng
        ev.UsedAt = DateTime.UtcNow;
        ev.User.IsVerified = true;
        ev.User.UpdatedAt = DateTime.UtcNow;

        // 🔹 Xóa hoặc đánh dấu token cũ chưa dùng của user để tránh rác DB
        var oldTokens = _db.EmailVerificationTokens
            .Where(x => x.UserId == ev.UserId && x.UsedAt == null && x.Token != decoded);
        foreach (var t in oldTokens)
            t.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _log.LogInformation("Email verified successfully for user {Email}", ev.User.Email);
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null || user.IsVerified) return;

        // Thu hồi tất cả token cũ chưa dùng
        var oldTokens = _db.EmailVerificationTokens
            .Where(x => x.UserId == user.UserId && x.UsedAt == null);
        foreach (var t in oldTokens)
            t.UsedAt = DateTime.UtcNow;

        // Tạo token mới (Base64UrlEncode)
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await _db.SaveChangesAsync();

        // Tạo link verify TRỎ VỀ API (đồng nhất với flow)
        var baseUrlApi = _cfg["App:BaseUrl"] ?? "https://localhost:7100";
        var verifyUrl = $"{baseUrlApi}/api/Auth/verify-email?token={token}";

        try
        {
            var body = $@"
            <p>Xin chào <b>{WebUtility.HtmlEncode(user.Username)}</b>,</p>
            <p>Nhấn vào link bên dưới để xác thực email của bạn:</p>
            <a href='{verifyUrl}' 
               style='background:#28a745;color:#fff;padding:10px 20px;text-decoration:none;border-radius:4px;'>Xác thực ngay</a>
            <p>Liên kết này có hiệu lực 30 phút.</p>";

            await _email.SendEmailAsync(user.Email, "Gửi lại email xác thực PTJ", body);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Resend verification email failed for {Email}", user.Email);
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip)
    {
        // 🔒 1. Kiểm tra dữ liệu đầu vào (tránh login rỗng)
        if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
            throw new Exception("Username/email and password are required.");

        var key = dto.UsernameOrEmail.Trim().ToLowerInvariant();

        // 🔍 2. Tìm user theo email hoặc username (không phân biệt hoa thường)
        var user = await _db.Users.FirstOrDefaultAsync(x =>
            x.Email.ToLower() == key || x.Username.ToLower() == key);

        // 🚫 3. Kiểm tra tình trạng khóa tài khoản (lockout)
        if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new Exception("Account is temporarily locked. Please try again later.");

        // ❌ 4. Kiểm tra sai mật khẩu hoặc user không tồn tại
        if (user == null || user.PasswordHash == null || !_hasher.Verify(user.PasswordHash, dto.Password))
        {
            if (user != null)
            {
                user.FailedLoginCount++;

                // Nếu sai >= 5 lần → khóa tạm 10 phút
                if (user.FailedLoginCount >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                    user.FailedLoginCount = 0;
                }

                await _db.SaveChangesAsync();
            }

            _db.LoginAttempts.Add(new LoginAttempt
            {
                UserId = user?.UserId,
                UsernameOrEmail = dto.UsernameOrEmail,
                IsSuccess = false,
                Message = "Invalid credentials",
                Ipaddress = ip,
                DeviceInfo = dto.DeviceInfo
            });
            await _db.SaveChangesAsync();

            throw new Exception("Invalid username/email or password.");
        }

        // ✅ 5. Nếu đăng nhập đúng → reset bộ đếm lỗi
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 📝 6. Ghi lại log đăng nhập thành công
        _db.LoginAttempts.Add(new LoginAttempt
        {
            UserId = user.UserId,
            UsernameOrEmail = user.Email,
            IsSuccess = true,
            Message = "Login successful",
            Ipaddress = ip,
            DeviceInfo = dto.DeviceInfo
        });
        await _db.SaveChangesAsync();

        // 🔑 7. Sinh token đăng nhập
        var response = await _tokens.IssueAsync(user, dto.DeviceInfo, ip);

        // ⚠️ 8. Nếu user chưa xác thực email → thêm cảnh báo
        if (!user.IsVerified)
            response.Warning = "Your email is not verified. Please check your inbox to verify your account.";

        return response;
    }



    public Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip)
        => _tokens.RefreshAsync(refreshToken, deviceInfo, ip);

    public Task LogoutAsync(string refreshToken) => _tokens.RevokeAsync(refreshToken);

    public async Task<AuthResponseDto> UpgradeToEmployerAsync(int userId, RegisterEmployerDto dto, string? ip)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new Exception("User not found");

        // Chỉ còn 1 role: Employer
        await RoleHelper.SetSingleRoleAsync(_db, userId, "Employer");

        // Tạo EmployerProfile nếu chưa có
        if (!await _db.EmployerProfiles.AnyAsync(p => p.UserId == userId))
        {
            _db.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = userId,
                DisplayName = dto.DisplayName ?? user.Username,
                ContactPhone = dto.PhoneNumber
            });
            await _db.SaveChangesAsync();
        }

        // Issue token mới để claims phản ánh role mới
        return await _tokens.IssueAsync(user, "web", ip);
    }


    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false
        });
        await _db.SaveChangesAsync();

        var link = $"{_cfg["Frontend:BaseUrl"]}/reset-password?token={WebUtility.UrlEncode(token)}";
        try { await _email.SendEmailAsync(user.Email, "Đặt lại mật khẩu", $"Nhấn <a href=\"{link}\">đây</a> để đặt lại mật khẩu (30’)."); }
        catch (Exception ex) { _log.LogWarning(ex, "Send reset email failed"); }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var prt = await _db.PasswordResetTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == dto.Token && !x.IsUsed);
        if (prt == null || prt.Expiration < DateTime.UtcNow) throw new Exception("Invalid/expired token.");

        prt.IsUsed = true;
        prt.User.PasswordHash = _hasher.Hash(dto.NewPassword);
        prt.User.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ip)
    {
        // 1) Xác thực IdToken với Google
        var payload = await GoogleJsonWebSignature.ValidateAsync(
            dto.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _cfg["Google:ClientId"] }
            });

        var email = payload.Email.Trim().ToLowerInvariant();

        // 2) Tìm user theo email
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
        {
            // 2a) Tạo user mới
            user = new User
            {
                Email = email,
                Username = email.Split('@')[0],
                PasswordHash = null,                    // đăng nhập qua Google, không có password local
                IsActive = true,
                IsVerified = payload.EmailVerified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Liên kết external login
            _db.ExternalLogins.Add(new ExternalLogin
            {
                UserId = user.UserId,
                Provider = "Google",
                ProviderKey = payload.Subject,          // sub
                Email = email,
                EmailVerified = payload.EmailVerified
            });
            await _db.SaveChangesAsync();

            // GÁN 1 ROLE DUY NHẤT: JobSeeker
            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "JobSeeker");

            // (Tuỳ chọn) tạo JobSeekerProfile nếu thiếu
            if (!await _db.JobSeekerProfiles.AnyAsync(p => p.UserId == user.UserId))
            {
                _db.JobSeekerProfiles.Add(new JobSeekerProfile
                {
                    UserId = user.UserId,
                    FullName = user.Username
                });
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            // 2b) User đã tồn tại: đảm bảo có external login Google
            var linked = await _db.ExternalLogins.AnyAsync(x =>
                x.UserId == user.UserId &&
                x.Provider == "Google" &&
                x.ProviderKey == payload.Subject);

            if (!linked)
            {
                _db.ExternalLogins.Add(new ExternalLogin
                {
                    UserId = user.UserId,
                    Provider = "Google",
                    ProviderKey = payload.Subject,
                    Email = email,
                    EmailVerified = payload.EmailVerified
                });
                await _db.SaveChangesAsync();
            }

            // Nếu Google xác nhận email verified mà user chưa verified → cập nhật
            if (payload.EmailVerified && !user.IsVerified)
            {
                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // ĐẢM BẢO user có role (không ghi đè nếu đã có, chỉ set nếu chưa có vai trò nào)
            await RoleHelper.EnsureRoleIfMissingAsync(_db, user.UserId, "JobSeeker");

            // (Tuỳ chọn) đảm bảo có JobSeekerProfile nếu user chưa nâng cấp Employer
            // (nếu đã có EmployerProfile thì bỏ qua)
            var hasEmployer = await _db.EmployerProfiles.AnyAsync(p => p.UserId == user.UserId);
            if (!hasEmployer && !await _db.JobSeekerProfiles.AnyAsync(p => p.UserId == user.UserId))
            {
                _db.JobSeekerProfiles.Add(new JobSeekerProfile
                {
                    UserId = user.UserId,
                    FullName = user.Username
                });
                await _db.SaveChangesAsync();
            }
        }

        // 3) Cấp token đăng nhập
        return await _tokens.IssueAsync(user, "google", ip);
    }
}

using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PTJ_Models.DTO.Auth;
using PTJ_Models.Models;
using Microsoft.AspNetCore.WebUtilities;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Data;
using PTJ_Service.Helpers.Implementations;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.AuthService.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly JobMatchingDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthService> _log;

    public AuthService(
        JobMatchingDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailSender email,
        IConfiguration cfg,
        ILogger<AuthService> log)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
        _cfg = cfg;
        _log = log;
    }

    // 1️⃣ Đăng ký JobSeeker

    public async Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // Kiểm tra email đã tồn tại
        var existing = await _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existing != null)
        {
            var role = existing.Roles.FirstOrDefault()?.RoleName ?? "User";
            throw new Exception($"Email này đã được sử dụng cho tài khoản {role}.");
        }

        using var tran = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                Email = email,
                Username = email.Split('@')[0],
                PasswordHash = _hasher.Hash(dto.Password),
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "JobSeeker");

            // Avatar mặc định
            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";
            const string DefaultPublicId = "avtDefaut_huflze";

            _db.JobSeekerProfiles.Add(new JobSeekerProfile
            {
                UserId = user.UserId,
                FullName = dto.FullName,
                ProfilePicture = DefaultAvatar,
                ProfilePicturePublicId = DefaultPublicId,
                IsPictureHidden = false,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Token xác thực email
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            await _db.SaveChangesAsync();

            await tran.CommitAsync();

            // Gửi email xác thực
            _ = Task.Run(async () =>
            {
                try
                {
                    var verifyUrl = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";
                    var body = $@"
                        <h2>Chào mừng bạn đến PTJ!</h2>
                        <p>Xin chào <b>{user.Username}</b>, vui lòng xác minh email của bạn:</p>
                        <a href='{verifyUrl}' style='background:#007bff;color:#fff;padding:10px 20px;border-radius:4px;'>Xác minh email</a>
                        <p>Liên kết này có hiệu lực trong 30 phút.</p>";

                    await _email.SendEmailAsync(user.Email, "PTJ - Xác minh tài khoản của bạn", body);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Gửi email xác minh thất bại cho {Email}", user.Email);
                }
            });

            var response = await _tokens.IssueAsync(user, null, null);
            response.Warning = "Email của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư.";

            return response;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            throw new Exception($"Đăng ký thất bại: {ex.Message}");
        }
    }



    // 2️⃣ Đăng ký Employer

    public async Task<AuthResponseDto> RegisterEmployerAsync(RegisterEmployerDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existing != null)
        {
            var role = existing.Roles.FirstOrDefault()?.RoleName ?? "User";
            throw new Exception($"Email này đã được sử dụng cho tài khoản {role}.");
        }

        using var tran = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                Email = email,
                Username = email.Split('@')[0],
                PasswordHash = _hasher.Hash(dto.Password),
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "Employer");

            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1762001123/default_company_logo.png";
            const string DefaultPublicId = "avtDefaut_huflze";

            _db.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = dto.DisplayName ?? user.Username,
                AvatarUrl = DefaultAvatar,
                AvatarPublicId = DefaultPublicId,
                ContactPhone = dto.ContactPhone,
                ContactEmail = null,
                Website = dto.Website,
                ProvinceId = 0,
                DistrictId = 0,
                WardId = 0,
                FullLocation = null,
                IsAvatarHidden = false,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            await _db.SaveChangesAsync();

            await tran.CommitAsync();

            // Email employer
            _ = Task.Run(async () =>
            {
                try
                {
                    var verifyUrl = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";
                    var body = $@"
                        <h2>Chào mừng bạn đến PTJ!</h2>
                        <p>Xin chào <b>{user.Username}</b>, vui lòng xác minh tài khoản nhà tuyển dụng của bạn:</p>
                        <a href='{verifyUrl}' style='background:#007bff;color:#fff;padding:10px 20px;border-radius:4px;'>Xác minh email</a>
                        <p>Liên kết này có hiệu lực trong 30 phút.</p>";

                    await _email.SendEmailAsync(user.Email, "PTJ - Xác minh tài khoản nhà tuyển dụng", body);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Gửi email xác minh thất bại cho {Email}", user.Email);
                }
            });

            var response = await _tokens.IssueAsync(user, null, null);
            response.Warning = "Email của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư.";

            return response;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            throw new Exception($"Đăng ký thất bại: {ex.Message}");
        }
    }


    // GOOGLE LOGIN — PREPARE

    public async Task<object> GooglePrepareAsync(GoogleLoginDto dto)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _cfg["Google:ClientId"] }
            });

        var email = payload.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existing != null)
        {
            if (!existing.IsActive)
                throw new Exception("Tài khoản này đã bị vô hiệu hóa.");

            return await _tokens.IssueAsync(existing, "google", null);
        }

        return new
        {
            needRoleSelection = true,
            email = payload.Email,
            name = payload.Name,
            picture = payload.Picture,
            availableRoles = new[] { "JobSeeker", "Employer" }
        };
    }


    // GOOGLE LOGIN — COMPLETE

    public async Task<AuthResponseDto> GoogleCompleteAsync(GoogleCompleteDto dto, string? ip)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _cfg["Google:ClientId"] }
            });

        var email = payload.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new Exception("Tài khoản Google này đã tồn tại.");

        var name = payload.Name ?? email.Split('@')[0];
        var picture = payload.Picture;

        var user = new User
        {
            Email = email,
            Username = email.Split('@')[0],
            IsActive = true,
            IsVerified = payload.EmailVerified,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var role = dto.Role.Equals("Employer", StringComparison.OrdinalIgnoreCase)
            ? "Employer" : "JobSeeker";

        await RoleHelper.SetSingleRoleAsync(_db, user.UserId, role);

        if (role == "Employer")
        {
            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1762001123/default_company_logo.png";

            _db.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = name,
                AvatarUrl = picture ?? DefaultAvatar,
                ContactEmail = email,
                IsAvatarHidden = false,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

            _db.JobSeekerProfiles.Add(new JobSeekerProfile
            {
                UserId = user.UserId,
                FullName = name,
                ProfilePicture = picture ?? DefaultAvatar,
                IsPictureHidden = false,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // External login link
        _db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.UserId,
            Provider = "Google",
            ProviderKey = payload.Subject,
            Email = email,
            EmailVerified = payload.EmailVerified
        });
        await _db.SaveChangesAsync();

        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await _tokens.IssueAsync(user, "google", ip);
    }



    // 3️⃣ LOGIN EMAIL + PASSWORD

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip)
    {
        if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new Exception("Email và mật khẩu là bắt buộc.");
        }

        var key = dto.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == key ||
                                      x.Username.ToLower() == key)
            ?? throw new Exception("Thông tin đăng nhập không hợp lệ.");

        if (!user.IsActive)
            throw new Exception("Tài khoản của bạn đã bị quản trị viên vô hiệu hóa.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new Exception("Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.");

        // Sai mật khẩu
        if (user.PasswordHash == null || !_hasher.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync();

            _db.LoginAttempts.Add(new LoginAttempt
            {
                UserId = user.UserId,
                UsernameOrEmail = dto.UsernameOrEmail,
                IsSuccess = false,
                Message = "Thông tin đăng nhập không hợp lệ",
                Ipaddress = ip,
                DeviceInfo = dto.DeviceInfo
            });
            await _db.SaveChangesAsync();

            throw new Exception("Email hoặc mật khẩu không đúng.");
        }

        // Reset trạng thái sau khi đăng nhập thành công
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _db.LoginAttempts.Add(new LoginAttempt
        {
            UserId = user.UserId,
            UsernameOrEmail = user.Email,
            IsSuccess = true,
            Message = "Đăng nhập thành công",
            Ipaddress = ip,
            DeviceInfo = dto.DeviceInfo
        });
        await _db.SaveChangesAsync();

        var role = user.Roles.FirstOrDefault()?.RoleName ?? "JobSeeker";

        // Avatar mặc định nếu rỗng
        if (role.Equals("JobSeeker", StringComparison.OrdinalIgnoreCase))
        {
            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";
            var profile = await _db.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == user.UserId);

            if (profile != null && string.IsNullOrEmpty(profile.ProfilePicture))
            {
                profile.ProfilePicture = DefaultAvatar;
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        else if (role.Equals("Employer", StringComparison.OrdinalIgnoreCase))
        {
            const string DefaultLogo = "https://res.cloudinary.com/do5rtjymt/image/upload/v1762001123/default_company_logo.png";
            var profile = await _db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == user.UserId);

            if (profile != null && string.IsNullOrEmpty(profile.AvatarUrl))
            {
                profile.AvatarUrl = DefaultLogo;
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        var response = await _tokens.IssueAsync(user, dto.DeviceInfo, ip);

        if (!user.IsVerified)
            response.Warning = "Email của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư.";

        response.Role = role;
        return response;
    }

    // 4️⃣ VERIFY / RESEND / REFRESH / LOGOUT / RESET PASSWORD

    public async Task VerifyEmailAsync(string token)
    {
        var decoded = WebUtility.UrlDecode(token);
        var ev = await _db.EmailVerificationTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == decoded && x.UsedAt == null);

        if (ev == null || ev.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Token không hợp lệ hoặc đã hết hạn.");

        ev.UsedAt = DateTime.UtcNow;
        ev.User.IsVerified = true;
        ev.User.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null || user.IsVerified) return;

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await _db.SaveChangesAsync();

        var link = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";

        await _email.SendEmailAsync(
            user.Email,
            "PTJ - Xác minh lại email của bạn",
            $"<p>Nhấn vào <a href='{link}'>đây</a> để xác minh email (hiệu lực 30 phút).</p>"
        );
    }

    public Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip)
        => _tokens.RefreshAsync(refreshToken, deviceInfo, ip);

    public Task LogoutAsync(string refreshToken)
        => _tokens.RevokeAsync(refreshToken);

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

        await _email.SendEmailAsync(
            user.Email,
            "PTJ - Đặt lại mật khẩu",
            $"Nhấn vào <a href='{link}'>đây</a> để đặt lại mật khẩu (hiệu lực 30 phút)."
        );
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var token = await _db.PasswordResetTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == dto.Token && !x.IsUsed);

        if (token == null || token.Expiration < DateTime.UtcNow)
            throw new Exception("Token không hợp lệ hoặc đã hết hạn.");
        token.IsUsed = true;
        token.User.PasswordHash = _hasher.Hash(dto.NewPassword);
        token.User.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

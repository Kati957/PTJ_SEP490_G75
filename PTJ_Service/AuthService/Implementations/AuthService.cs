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
using PTJ_Service.Helpers.Interfaces;
using PTJ_Service.Helpers.Implementations;

namespace PTJ_Service.AuthService.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly JobMatchingOpenAiDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateService _templates; 
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthService> _log;

    public AuthService(
        JobMatchingOpenAiDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailSender email,
        IEmailTemplateService templates,
        IConfiguration cfg,
        ILogger<AuthService> log)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
        _templates = templates;
        _cfg = cfg;
        _log = log;
    }

    // 1. Đăng ký JobSeeker
    public async Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new Exception("Email này đã được sử dụng.");

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

            const string DefaultAvatar = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

            _db.JobSeekerProfiles.Add(new JobSeekerProfile
            {
                UserId = user.UserId,
                FullName = dto.FullName,
                ProfilePicture = DefaultAvatar,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Tạo token verify
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            // Gửi email xác minh
            var verifyUrl = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";
            var html = _templates.CreateVerifyEmailTemplate(verifyUrl); 

            await _email.SendEmailAsync(user.Email, "PTJ - Xác minh tài khoản", html);

            var response = await _tokens.IssueAsync(user, null, null);
            response.Warning = "Vui lòng kiểm tra email để xác minh tài khoản.";

            return response;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    // 2. Đăng ký Employer → chờ duyệt
    public async Task<object> SubmitEmployerRegistrationAsync(RegisterEmployerDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new Exception("Email này đã được sử dụng.");

        if (await _db.EmployerRegistrationRequests.AnyAsync(x => x.Email == email && x.Status == "Pending"))
            throw new Exception("Email này đã gửi yêu cầu và đang chờ duyệt.");

        var username = email.Split('@')[0].ToLower();

        if (await _db.Users.AnyAsync(x => x.Username == username))
            throw new Exception("Tên đăng nhập đã tồn tại.");

        var req = new EmployerRegistrationRequest
        {
            Email = email,
            Username = username,
            PasswordHash = _hasher.Hash(dto.Password),
            CompanyName = dto.CompanyName,
            CompanyDescription = dto.CompanyDescription,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            Address = dto.Address,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.EmployerRegistrationRequests.Add(req);
        await _db.SaveChangesAsync();

        return new
        {
            message = "Gửi yêu cầu đăng ký thành công. Vui lòng chờ quản trị viên phê duyệt.",
            requestId = req.RequestId
        };
    }

    // 3. Google Login - Prepare
    public async Task<object> GooglePrepareAsync(GoogleLoginDto dto)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(
            dto.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _cfg["Google:ClientId"] }
            });

        var email = payload.Email.ToLowerInvariant();

        var existing = await _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existing != null)
        {
            if (!existing.IsActive)
                throw new Exception("Tài khoản đã bị vô hiệu hóa.");

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

    // 4. Google Login - Complete
    public async Task<AuthResponseDto> GoogleCompleteAsync(GoogleCompleteDto dto, string? ip)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(
            dto.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _cfg["Google:ClientId"] }
            });

        var email = payload.Email.ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new Exception("Tài khoản Google này đã tồn tại.");

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

        bool isEmployer = dto.Role.Equals("Employer", StringComparison.OrdinalIgnoreCase);

        if (isEmployer)
        {
            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "PendingEmployer");

            _db.GoogleEmployerRequests.Add(new GoogleEmployerRequest
            {
                UserId = user.UserId,
                DisplayName = payload.Name ?? user.Username,
                PictureUrl = payload.Picture,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                RequiresApproval = true,
                Message = "Tài khoản Google đã được tạo. Vui lòng chờ admin phê duyệt."
            };
        }

        await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "JobSeeker");

        const string DefaultAvatar =
            "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

        _db.JobSeekerProfiles.Add(new JobSeekerProfile
        {
            UserId = user.UserId,
            FullName = payload.Name ?? user.Username,
            ProfilePicture = payload.Picture ?? DefaultAvatar,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return await _tokens.IssueAsync(user, "google", ip);
    }

    // 5. Login
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ip)
    {
        var key = dto.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == key || x.Username.ToLower() == key);

        if (user == null || user.PasswordHash == null)
            throw new Exception("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new Exception("Tài khoản bị vô hiệu hóa.");

        // Sai mật khẩu
        if (!_hasher.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= 5) 
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync();
            throw new Exception("Email hoặc mật khẩu không đúng.");
        }

        // Login thành công
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var response = await _tokens.IssueAsync(user, dto.DeviceInfo, ip);

        if (!user.IsVerified)
            response.Warning = "Email của bạn chưa được xác minh.";

        return response;
    }

    // 6. Xác minh email
    public async Task VerifyEmailAsync(string token)
    {
        var ev = await _db.EmailVerificationTokens
     .Include(x => x.User)
     .FirstOrDefaultAsync(x => x.Token == token && x.UsedAt == null);
        if (ev == null || ev.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Token không hợp lệ hoặc đã hết hạn.");

        ev.UsedAt = DateTime.UtcNow;
        ev.User.IsVerified = true;
        ev.User.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    // 7. Gửi lại email xác minh
    public async Task ResendVerificationAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || user.IsVerified)
            return;

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });

        await _db.SaveChangesAsync();

        // Dùng template
        var link = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";
        var html = _templates.CreateVerifyEmailTemplate(link);

        await _email.SendEmailAsync(
            user.Email,
            "PTJ - Xác minh lại email",
            html
        );
    }

    // 8. Request reset password
    public async Task RequestPasswordResetAsync(string email)
    {
        try
        {
            var normalized = email.Trim().ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalized);

            if (user == null)
                return;

            if (!user.IsActive)
                return;

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                return;

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
            var html = _templates.CreateResetPasswordTemplate(link);

            await _email.SendEmailAsync(
                user.Email,
                "PTJ - Đặt lại mật khẩu",
                html
            );
        }
        catch (Exception ex)
        {
           
            Console.WriteLine($"[SECURITY] Forgot password error: {ex.Message}");
        }
    }


    // 9. Reset password
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var token = await _db.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == dto.Token && !x.IsUsed);

        if (token == null || token.Expiration < DateTime.UtcNow)
            throw new Exception("Token không hợp lệ hoặc đã hết hạn.");

        token.IsUsed = true;
        token.User.PasswordHash = _hasher.Hash(dto.NewPassword);
        token.User.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
    public Task<AuthResponseDto> RefreshAsync(string refreshToken, string? deviceInfo, string? ip)
    => _tokens.RefreshAsync(refreshToken, deviceInfo, ip);

    public Task LogoutAsync(string refreshToken)
        => _tokens.RevokeAsync(refreshToken);

}

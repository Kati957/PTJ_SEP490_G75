using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PTJ_Data;
using PTJ_Models.DTO.Auth;
using PTJ_Models.Models;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.AuthService.Implementations
{
    public class ChangePasswordService : IChangePasswordService
    {
        private readonly JobMatchingDbContext _context;
        private readonly IEmailSender _email;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ChangePasswordService> _log;

        public ChangePasswordService(
            JobMatchingDbContext context,
            IEmailSender email,
            IConfiguration cfg,
            ILogger<ChangePasswordService> log)
        {
            _context = context;
            _email = email;
            _cfg = cfg;
            _log = log;
        }

        // 1️⃣ Bước 1: YÊU CẦU ĐỔI MẬT KHẨU → gửi email xác nhận
        public async Task RequestChangePasswordAsync(int userId, RequestChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId)
                       ?? throw new Exception("Không tìm thấy người dùng.");

            // Tài khoản Google không có password
            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("Tài khoản đăng nhập Google không thể đổi mật khẩu.");

            // Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Mật khẩu hiện tại không đúng.");

            // Tạo token
            var token = Guid.NewGuid().ToString("N");

            // Lưu token vào bảng EmailVerificationTokens
            var entry = new EmailVerificationToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                UsedAt = null
            };

            _context.EmailVerificationTokens.Add(entry);
            await _context.SaveChangesAsync();

            // Link BE verify token (sẽ redirect sang FE)
            var baseUrl = _cfg["App:BaseUrl"]!;
            var confirmUrl = $"{baseUrl}/api/change-password/verify?token={token}";

            // Email HTML
            var body = $@"
                <h2>PTJ - Xác nhận đổi mật khẩu</h2>
                <p>Xin chào <b>{user.Username}</b>,</p>
                <p>Bạn vừa yêu cầu đổi mật khẩu. Nhấn nút bên dưới để xác nhận:</p>
                <a href='{confirmUrl}' 
                   style='background:#007bff;color:white;padding:10px 20px;
                          border-radius:4px;text-decoration:none'>
                    Xác nhận đổi mật khẩu
                </a>
                <p>Liên kết hết hạn sau 30 phút.</p>";

            try
            {
                await _email.SendEmailAsync(user.Email, "PTJ - Xác nhận đổi mật khẩu", body);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Lỗi gửi email đổi mật khẩu.");
            }
        }

        // 2️⃣ Bước 2: BE kiểm tra token (true/false)
        public async Task<bool> VerifyChangePasswordTokenAsync(string token)
        {
            var entry = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(e => e.Token == token);

            if (entry == null) return false;
            if (entry.UsedAt != null) return false;
            if (entry.ExpiresAt < DateTime.UtcNow) return false;

            return true;
        }

        // 3️⃣ Bước 3: đặt mật khẩu mới
        public async Task<bool> ConfirmChangePasswordAsync(ConfirmChangePasswordDto dto)
        {
            var entry = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(e => e.Token == dto.Token);

            if (entry == null || entry.UsedAt != null || entry.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Token không hợp lệ hoặc đã hết hạn.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == entry.UserId)
                       ?? throw new Exception("Không tìm thấy người dùng.");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new Exception("Mật khẩu xác nhận không khớp.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            entry.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

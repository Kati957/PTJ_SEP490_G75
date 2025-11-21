using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO.Auth;
using PTJ_Models.Models;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Service.Helpers.Interfaces;
using System;
using System.Threading.Tasks;

namespace PTJ_Service.AuthService.Implementations
{
    public class ChangePasswordService : IChangePasswordService
    {
        private readonly JobMatchingDbContext _context;
        private readonly IEmailSender _email;

        public ChangePasswordService(JobMatchingDbContext context, IEmailSender email)
        {
            _context = context;
            _email = email;
        }

     
        // 1️⃣ GỬI EMAIL XÁC NHẬN ĐỔI MẬT KHẨU

        public async Task RequestChangePasswordAsync(int userId, string currentPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new Exception("Không tìm thấy người dùng.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("Tài khoản Google không thể đổi mật khẩu theo cách này.");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                throw new Exception("Mật khẩu hiện tại không đúng.");

            // Tạo token xác nhận
            var token = Guid.NewGuid().ToString("N");

            var entry = new EmailVerificationToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                UsedAt = null
            };

            _context.EmailVerificationTokens.Add(entry);
            await _context.SaveChangesAsync();

            // Link FE xác nhận đổi mật khẩu
            var link = $"https://yourfrontend.com/account/confirm-change-password?token={token}";

            var subject = "Xác nhận đổi mật khẩu - Smart PTJ";
            var body = $@"
Bạn đã yêu cầu đổi mật khẩu tài khoản Smart PTJ.

Nếu đúng là bạn, nhấn vào link sau để xác nhận:
{link}

Nếu không phải bạn, vui lòng bỏ qua email này.
";

            await _email.SendEmailAsync(user.Email, subject, body);
        }



        // 2️⃣ FE GỌI API KIỂM TRA TOKEN
 
        public async Task<bool> VerifyChangePasswordRequestAsync(string token)
        {
            var entry = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (entry == null)
                return false;

            if (entry.UsedAt != null)
                return false;

            if (entry.ExpiresAt < DateTime.UtcNow)
                return false;

            return true;
        }


        // 3️⃣ ĐỔI MẬT KHẨU

        public async Task<bool> ChangePasswordAsync(ConfirmChangePasswordDto dto)
        {
            var entry = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (entry == null || entry.UsedAt != null || entry.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Token không hợp lệ hoặc đã hết hạn.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == entry.UserId)
                ?? throw new Exception("Không tìm thấy người dùng.");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new Exception("Mật khẩu xác nhận không khớp.");

            // Cập nhật mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            // Đánh dấu token đã dùng
            entry.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

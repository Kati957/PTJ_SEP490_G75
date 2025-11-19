using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;
using PTJ_Models.DTO.Auth;
using PTJ_Models.Models;
using PTJ_Service.AuthService.Interfaces;

namespace PTJ_Service.AuthService.Implementations
{
    public class ChangePasswordService : IChangePasswordrService
    {
        private readonly JobMatchingDbContext _context;

        public ChangePasswordService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("Bạn đang đăng nhập bằng Google, không thể đổi mật khẩu theo cách này.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO.Auth;

using PTJ_Models.Models;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly JobMatchingDbContext _context;

        public UserService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("User not found.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("You are using Google login, cannot change password this way.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

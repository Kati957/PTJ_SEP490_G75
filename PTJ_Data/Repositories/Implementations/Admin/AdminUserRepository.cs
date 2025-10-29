using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.Admin;
using PTJ_Data.Repositories.Interfaces.Admin;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminUserRepository(JobMatchingDbContext db) => _db = db;

        // ============= Danh sách người dùng =============
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null)
        {
            var query = _db.Users
                .Include(u => u.Roles)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (isVerified.HasValue)
                query = query.Where(u => u.IsVerified == isVerified.Value);

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(kw) ||
                    u.Username.ToLower().Contains(kw) ||
                    u.Address != null && u.Address.ToLower().Contains(kw));
            }

            var list = await query
                .Select(u => new UserDto
                {
                    Id = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Roles.Select(r => r.RoleName).FirstOrDefault() ?? "Unknown",
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            if (!string.IsNullOrEmpty(role))
                list = list
                    .Where(x => x.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return list.OrderByDescending(x => x.CreatedAt);
        }

        // ============= Chi tiết người dùng =============
        public async Task<UserDetailDto?> GetUserDetailAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return null;

            var dto = new UserDetailDto
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Roles.Select(r => r.RoleName).FirstOrDefault() ?? "Unknown",
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Address = user.Address ?? "",
                PhoneNumber = user.PhoneNumber?.ToString() ?? "",
            };

            if (dto.Role == "JobSeeker")
            {
                var profile = await _db.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == id);
                if (profile != null)
                {
                    dto.FullName = profile.FullName;
                    dto.Gender = profile.Gender;
                    dto.BirthYear = profile.BirthYear;
                    dto.PreferredLocation = profile.PreferredLocation;
                }
            }
            else if (dto.Role == "Employer")
            {
                var profile = await _db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == id);
                if (profile != null)
                {
                    dto.FullName = profile.DisplayName;
                    dto.Address = profile.Location ?? dto.Address;
                    dto.PhoneNumber = profile.ContactPhone?.ToString() ?? dto.PhoneNumber;
                    dto.PreferredLocation = profile.Website;
                }
            }

            return dto;
        }

        // ============= Khóa / Mở khóa =============
        public async Task<bool> ToggleUserActiveAsync(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

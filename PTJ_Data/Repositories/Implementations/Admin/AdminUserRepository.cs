using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.Admin;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.Models;


namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminUserRepository(JobMatchingDbContext db) => _db = db;

        public async Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
            string? role = null, bool? isActive = null, bool? isVerified = null, string? keyword = null,
            int page = 1, int pageSize = 10)
        {
            var query = _db.Users.Include(u => u.Roles).AsQueryable();

            if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
            if (isVerified.HasValue) query = query.Where(u => u.IsVerified == isVerified.Value);

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(kw) ||
                    u.Username.ToLower().Contains(kw) ||
                    (u.Address != null && u.Address.ToLower().Contains(kw)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Roles.Select(r => r.RoleName).FirstOrDefault() ?? "Unknown",
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            return new PagedResult<AdminUserDto>(items, total, page, pageSize);
        }

        public async Task<AdminUserDetailDto?> GetUserDetailAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.JobSeekerProfile)
                .Include(u => u.EmployerProfile)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return null;

            var dto = new AdminUserDetailDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Roles.Select(r => r.RoleName).FirstOrDefault() ?? "Unknown",
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber?.ToString()
            };

            if (dto.Role == "JobSeeker" && user.JobSeekerProfile != null)
            {
                dto.FullName = user.JobSeekerProfile.FullName;
                dto.Gender = user.JobSeekerProfile.Gender;
                dto.BirthYear = user.JobSeekerProfile.BirthYear;
                dto.PreferredLocation = user.JobSeekerProfile.PreferredLocation;
            }
            else if (dto.Role == "Employer" && user.EmployerProfile != null)
            {
                dto.FullName = user.EmployerProfile.DisplayName;
                dto.Address = user.EmployerProfile.Location ?? dto.Address;
                dto.PhoneNumber = user.EmployerProfile.ContactPhone;
            }

            return dto;
        }

        public Task<User?> GetUserEntityAsync(int id)
            => _db.Users.FirstOrDefaultAsync(x => x.UserId == id);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

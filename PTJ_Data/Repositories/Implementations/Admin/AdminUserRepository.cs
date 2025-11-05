using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.Admin;
using PTJ_Data.Repositories.Interfaces.Admin;


namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminUserRepository(JobMatchingDbContext db) => _db = db;

        //Danh sách người dùng (có phân trang) 
        public async Task<PagedResult<UserDto>> GetAllUsersAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
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
                    (u.Address != null && u.Address.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Roles.Any(r => r.RoleName == role));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedResult<UserDto>(data, total, page, pageSize);
        }

        //  Chi tiết người dùng 
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
                PhoneNumber = user.PhoneNumber?.ToString() ?? ""
            };

            // Lấy profile theo role
            switch (dto.Role)
            {
                case "JobSeeker":
                    var js = await _db.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == id);
                    if (js != null)
                    {
                        dto.FullName = js.FullName;
                        dto.Gender = js.Gender;
                        dto.BirthYear = js.BirthYear;
                        dto.PreferredLocation = js.PreferredLocation;
                    }
                    break;

                case "Employer":
                    var ep = await _db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == id);
                    if (ep != null)
                    {
                        dto.FullName = ep.DisplayName;
                        dto.Address = ep.Location ?? dto.Address;
                        dto.PhoneNumber = ep.ContactPhone?.ToString() ?? dto.PhoneNumber;
                        dto.PreferredLocation = ep.Website;
                    }
                    break;
            }

            return dto;
        }

        //  Danh sách đầy đủ (dashboard tổng hợp) 
        public async Task<IEnumerable<AdminUserFullDto>> GetAllUserFullAsync()
        {
            var rows = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.JobSeekerProfile)
                .Include(u => u.EmployerProfile)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    Role = u.Roles.Select(r => r.RoleName).FirstOrDefault(),
                    u.IsActive,
                    u.IsVerified,
                    u.CreatedAt,
                    u.LastLogin,
                    u.Address,
                    u.PhoneNumber,
                    JS = u.JobSeekerProfile,
                    EP = u.EmployerProfile
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return rows.Select(x => new AdminUserFullDto
            {
                UserId = x.UserId,
                Username = x.Username,
                Email = x.Email,
                Role = x.Role ?? "Unknown",
                IsActive = x.IsActive,
                IsVerified = x.IsVerified,
                CreatedAt = x.CreatedAt,
                LastLogin = x.LastLogin,
                Address = x.Address,
                PhoneNumber = x.PhoneNumber?.ToString(),
                FullName = x.JS?.FullName,
                Gender = x.JS?.Gender,
                BirthYear = x.JS?.BirthYear,
                PreferredLocation = x.JS?.PreferredLocation,
                CompanyName = x.EP?.DisplayName,
                Website = x.EP?.Website
            });
        }

        //  Khóa / Mở khóa 
        public async Task<bool> ToggleUserActiveAsync(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
    }
}

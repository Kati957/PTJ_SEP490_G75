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

        // 1️⃣ Danh sách user (phân trang)
        public async Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
            string? role = null, bool? isActive = null, bool? isVerified = null,
            string? keyword = null, int page = 1, int pageSize = 10)
            {
            var query = _db.Users
                .Include(u => u.Roles)
                .Include(u => u.JobSeekerProfile)
                .Include(u => u.EmployerProfile)
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
                    LastLogin = u.LastLogin,
                    AvatarUrl = u.JobSeekerProfile != null
                        ? u.JobSeekerProfile.ProfilePicture
                        : u.EmployerProfile != null
                            ? u.EmployerProfile.AvatarUrl
                            : null
                    })
                .ToListAsync();

            return new PagedResult<AdminUserDto>(items, total, page, pageSize);
            }

        // 2️⃣ Chi tiết người dùng
        public async Task<AdminUserDetailDto?> GetUserDetailAsync(int id)
            {
            var user = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.JobSeekerProfile)
                .Include(u => u.EmployerProfile)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return null;

            var role = user.Roles.Select(r => r.RoleName).FirstOrDefault() ?? "Unknown";

            var dto = new AdminUserDetailDto
                {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = role,
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber?.ToString(),
                };

            // JobSeeker
            if (role == "JobSeeker" && user.JobSeekerProfile != null)
                {
                dto.FullName = user.JobSeekerProfile.FullName;
                dto.Gender = user.JobSeekerProfile.Gender;
                dto.BirthYear = user.JobSeekerProfile.BirthYear;
                dto.ProvinceId = user.JobSeekerProfile.ProvinceId;
                dto.DistrictId = user.JobSeekerProfile.DistrictId;
                dto.WardId = user.JobSeekerProfile.WardId;

                dto.AvatarUrl = user.JobSeekerProfile.ProfilePicture;
                }
            // Employer
            else if (role == "Employer" && user.EmployerProfile != null)
                {
                dto.CompanyName = user.EmployerProfile.DisplayName ?? user.Username;
                dto.Website = user.EmployerProfile.Website;

                // 🔧 SỬA Ở ĐÂY: dùng EmployerProfile thay vì JobSeekerProfile
                dto.ProvinceId = user.EmployerProfile.ProvinceId;
                dto.DistrictId = user.EmployerProfile.DistrictId;
                dto.WardId = user.EmployerProfile.WardId;

                dto.PhoneNumber ??= user.EmployerProfile.ContactPhone;
                dto.AvatarUrl = user.EmployerProfile.AvatarUrl;
                }

            return dto;
            }

        public Task<User?> GetUserEntityAsync(int id)
            => _db.Users.FirstOrDefaultAsync(x => x.UserId == id);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
        }
    }

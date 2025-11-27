using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminJobPostRepository : IAdminJobPostRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminJobPostRepository(JobMatchingDbContext db) => _db = db;


        // EMPLOYER POSTS


        public async Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsPagedAsync(
            string? status, int? categoryId, string? keyword, int page, int pageSize)
        {
            var q = _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.User.EmployerProfile)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                q = q.Where(p => p.Status == status);

            if (categoryId.HasValue)
                q = q.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                q = q.Where(p =>
                    p.Title.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                    p.User.Email.ToLower().Contains(kw));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminEmployerPostDto
                {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    EmployerEmail = p.User.Email,
                    EmployerName = p.User.EmployerProfile != null
                        ? p.User.EmployerProfile.DisplayName
                        : null,
                    CategoryName = p.Category != null
                        ? p.Category.Name
                        : null,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminEmployerPostDto>(items, total, page, pageSize);
        }

        public async Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
        {
            // 1️⃣ Lấy dữ liệu bài đăng như cũ
            var dto = await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.User.EmployerProfile)
                .Where(p => p.EmployerPostId == id)
                .Select(p => new AdminEmployerPostDetailDto
                {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,

                    ProvinceId = p.ProvinceId,
                    DistrictId = p.DistrictId,
                    WardId = p.WardId,

                    PhoneContact = p.PhoneContact,
                    EmployerEmail = p.User.Email,
                    EmployerName = p.User.EmployerProfile != null
                        ? p.User.EmployerProfile.DisplayName
                        : p.User.Username,

                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,

                    ImageUrls = new List<string>()
                })
                .FirstOrDefaultAsync();

            if (dto == null)
                return null;

            // 2️⃣ Lấy ảnh từ bảng Images (pattern giống code CreatePost)
            dto.ImageUrls = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && i.EntityId == id)
                .Select(i => i.Url)
                .ToListAsync();

            return dto;
        }


        public async Task<bool> ToggleEmployerPostBlockedAsync(int id)
        {
            var post = await _db.EmployerPosts
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);

            if (post == null) return false;

            post.Status = post.Status == "Blocked" ? "Active" : "Blocked";
            post.UpdatedAt = System.DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        //  NEW — Lấy EmployerPost để gửi Notification 
        public async Task<EmployerPost?> GetEmployerPostByIdAsync(int id)
        {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);
        }


        // JOB SEEKER POSTS


        public async Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsPagedAsync(
            string? status, int? categoryId, string? keyword, int page, int pageSize)
        {
            var q = _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.User.JobSeekerProfile)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                q = q.Where(p => p.Status == status);

            if (categoryId.HasValue)
                q = q.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                q = q.Where(p =>
                    p.Title.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                    p.User.Email.ToLower().Contains(kw));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminJobSeekerPostDto
                {
                    JobSeekerPostId = p.JobSeekerPostId,
                    Title = p.Title,
                    JobSeekerEmail = p.User.Email,
                    FullName = p.User.JobSeekerProfile != null
                        ? p.User.JobSeekerProfile.FullName
                        : null,
                    CategoryName = p.Category != null
                        ? p.Category.Name
                        : null,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminJobSeekerPostDto>(items, total, page, pageSize);
        }

        public async Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.User.JobSeekerProfile)
                .Where(p => p.JobSeekerPostId == id)
                .Select(p => new AdminJobSeekerPostDetailDto
                {
                    JobSeekerPostId = p.JobSeekerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    JobSeekerEmail = p.User.Email,
                    FullName = p.User.JobSeekerProfile != null
                        ? p.User.JobSeekerProfile.FullName
                        : null,
                    CategoryName = p.Category != null
                        ? p.Category.Name
                        : null,

                    ProvinceId = p.ProvinceId,
                    DistrictId = p.DistrictId,
                    WardId = p.WardId,

                    PreferredWorkHours = p.PreferredWorkHours,
                    Gender = p.Gender,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ToggleJobSeekerPostArchivedAsync(int id)
        {
            var post = await _db.JobSeekerPosts
                .FirstOrDefaultAsync(p => p.JobSeekerPostId == id);

            if (post == null) return false;

            post.Status = post.Status == "Archived" ? "Active" : "Archived";
            post.UpdatedAt = System.DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        //  NEW — Lấy JobSeekerPost để gửi Notification 
        public async Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
        }
    }
}

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

        //  Employer Posts 

        public async Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.User.EmployerProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(kw) ||
                    (p.Description ?? "").ToLower().Contains(kw) ||
                    (p.Location ?? "").ToLower().Contains(kw) ||
                    p.User.Email.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminEmployerPostDto
                {
                    Id = p.EmployerPostId,
                    Title = p.Title,
                    EmployerUserId = p.UserId,
                    EmployerEmail = p.User.Email,
                    EmployerName = p.User.EmployerProfile != null ? p.User.EmployerProfile.DisplayName : null,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminEmployerPostDto>(data, total, page, pageSize);
        }

        public async Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
        {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.User.EmployerProfile)
                .Where(p => p.EmployerPostId == id)
                .Select(p => new AdminEmployerPostDetailDto
                {
                    Id = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,
                    Location = p.Location,
                    PhoneContact = p.PhoneContact,
                    EmployerUserId = p.UserId,
                    EmployerEmail = p.User.Email,
                    EmployerName = p.User.EmployerProfile != null ? p.User.EmployerProfile.DisplayName : null,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string?> ToggleEmployerPostBlockedAsync(int id)
        {
            var post = await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);
            if (post == null) return null;

            post.Status = post.Status == "Blocked" ? "Active" : "Blocked";
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return post.Status;
        }

        public Task<EmployerPost?> GetEmployerPostEntityAsync(int id)
            => _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);

        //  JobSeeker Posts 

        public async Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.User.JobSeekerProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(kw) ||
                    (p.Description ?? "").ToLower().Contains(kw) ||
                    (p.PreferredLocation ?? "").ToLower().Contains(kw) ||
                    p.User.Email.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminJobSeekerPostDto
                {
                    Id = p.JobSeekerPostId,
                    Title = p.Title,
                    UserId = p.UserId,
                    UserEmail = p.User.Email,
                    FullName = p.User.JobSeekerProfile != null ? p.User.JobSeekerProfile.FullName : null,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Gender = p.Gender,
                    PreferredLocation = p.PreferredLocation,
                    PreferredWorkHours = p.PreferredWorkHours,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminJobSeekerPostDto>(data, total, page, pageSize);
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
                    Id = p.JobSeekerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    UserId = p.UserId,
                    UserEmail = p.User.Email,
                    FullName = p.User.JobSeekerProfile != null ? p.User.JobSeekerProfile.FullName : null,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Gender = p.Gender,
                    PreferredLocation = p.PreferredLocation,
                    PreferredWorkHours = p.PreferredWorkHours,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string?> ToggleJobSeekerPostArchivedAsync(int id)
        {
            var post = await _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
            if (post == null) return null;

            post.Status = post.Status == "Archived" ? "Active" : "Archived";
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return post.Status;
        }

        public Task<JobSeekerPost?> GetJobSeekerPostEntityAsync(int id)
            => _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == id);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

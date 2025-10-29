using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Data.Repositories.Implementations
{
    public class JobSeekerSearchRepository : IJobSeekerSearchRepository
        {
        private readonly JobMatchingDbContext _db;

        public JobSeekerSearchRepository(JobMatchingDbContext db)
            {
            _db = db;
            }

        public async Task<IEnumerable<EmployerPostDtoOut>> SearchEmployerPostsAsync(JobSeekerSearchFilterDto filter)
            {
            var query = _db.EmployerPosts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Where(p => p.Status == "Active")
                .AsQueryable();

            // 🔍 Keyword
            if (!string.IsNullOrEmpty(filter.Keyword))
                {
                var key = filter.Keyword.ToLower();
                query = query.Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(key)) ||
                    (p.Description != null && p.Description.ToLower().Contains(key)) ||
                    (p.Requirements != null && p.Requirements.ToLower().Contains(key)));
                }

            // 🗂 Category
            if (filter.CategoryID.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryID.Value);

            // 📍 Location
            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(p => p.Location.Contains(filter.Location));

            // 🕐 Work Hours
            if (!string.IsNullOrEmpty(filter.WorkHours))
                query = query.Where(p => p.WorkHours.Contains(filter.WorkHours));

            // 💰 Salary
            if (filter.MinSalary.HasValue)
                query = query.Where(p => p.Salary >= filter.MinSalary.Value);

            if (filter.MaxSalary.HasValue)
                query = query.Where(p => p.Salary <= filter.MaxSalary.Value);

            // ⚡ Map sang DTO
            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new EmployerPostDtoOut
                    {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,
                    Location = p.Location,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    EmployerName = p.User != null ? p.User.Username : null,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                    })
                .ToListAsync();
            }
        }
    }

using PTJ_Models.DTO;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;

namespace PTJ_Service.HomeService
{
    public class HomeService : IHomeService
    {
        private readonly JobMatchingDbContext _context;

        public HomeService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<HomePostDto>> GetLatestPostsAsync(string? keyword, int page, int pageSize)
        {
            var employerPosts = await _context.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .Select(p => new HomePostDto
                {
                    PostId = p.EmployerPostId,
                    PostType = "Employer",
                    Title = p.Title,
                    Description = p.Description,
                    Location = p.Location,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Salary = p.Salary,
                    WorkHours = p.WorkHours,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User.Username
                })
                .ToListAsync();

            var jobSeekerPosts = await _context.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .Select(p => new HomePostDto
                {
                    PostId = p.JobSeekerPostId,
                    PostType = "JobSeeker",
                    Title = p.Title,
                    Description = p.Description,
                    Location = p.PreferredLocation,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Salary = null, // 👈 thêm dòng này để 2 bên khớp nhau
                    WorkHours = p.PreferredWorkHours,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User.Username
                })
                .ToListAsync();

            // Gộp hai danh sách trong bộ nhớ
            var combined = employerPosts.Concat(jobSeekerPosts);

            // Lọc theo keyword (nếu có)
            if (!string.IsNullOrEmpty(keyword))
            {
                combined = combined.Where(x =>
                    (x.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Location?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Phân trang & sắp xếp
            var result = combined
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return result;
        }
    }
}

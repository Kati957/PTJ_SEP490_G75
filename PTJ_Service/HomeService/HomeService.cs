using PTJ_Models.DTO;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;
using PTJ_Models.Models;
using PTJ_Models.DTO.HomePageDTO;
using PTJ_Data.Repositories.Interfaces.Home;

namespace PTJ_Service.HomeService
{
    public class HomeService : IHomeService
    {
        private readonly JobMatchingOpenAiDbContext _context;
        private readonly IHomeRepository _repo;
        public HomeService(JobMatchingOpenAiDbContext context, IHomeRepository repo)
        {
            _context = context;
            _repo = repo;
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

                    SalaryMin = p.SalaryMin,
                    SalaryMax = p.SalaryMax,
                    SalaryType = p.SalaryType,

                    SalaryDisplay =
                        (p.SalaryMin == null && p.SalaryMax == null)
                            ? "Thỏa thuận"
                            : (p.SalaryMin != null && p.SalaryMax != null)
                                ? $"{p.SalaryMin:#,###} - {p.SalaryMax:#,###}"
                                : (p.SalaryMin != null)
                                    ? $"Từ {p.SalaryMin:#,###}"
                                    : $"Đến {p.SalaryMax:#,###}",

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

                    SalaryMin = null,
                    SalaryMax = null,
                    SalaryType = null,
                    SalaryDisplay = "—", // hoặc "" tùy bạn

                    WorkHours = p.PreferredWorkHours,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User.Username
                    })
                .ToListAsync();

            // Gộp hai danh sách
            var combined = employerPosts.Concat(jobSeekerPosts);

            // Lọc keyword
            if (!string.IsNullOrEmpty(keyword))
                {
                combined = combined.Where(x =>
                    (x.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Location?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

            // Sort + paging
            var result = combined
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return result;
            }

        public Task<HomeStatisticsDto> GetHomeStatisticsAsync()
        {
            return _repo.GetHomeStatisticsAsync();
        }
    }
}

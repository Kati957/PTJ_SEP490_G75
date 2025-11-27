using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private readonly JobMatchingDbContext _db;
        public ReportRepository(JobMatchingDbContext db) => _db = db;

        public Task<bool> EmployerPostExistsAsync(int employerPostId)
            => _db.EmployerPosts.AnyAsync(p => p.EmployerPostId == employerPostId && p.Status != "Deleted");

        public Task<bool> JobSeekerPostExistsAsync(int jobSeekerPostId)
            => _db.JobSeekerPosts.AnyAsync(p => p.JobSeekerPostId == jobSeekerPostId && p.Status != "Deleted");

        // ADD REPORT

        public async Task AddAsync(PostReport report)
        {
            await _db.PostReports.AddAsync(report);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        // GET MY REPORTS UNIFIED VERSION

        public async Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
        {
            var list = await _db.PostReports
                .Where(r => r.ReporterId == reporterId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new MyReportDto
                {
                    ReportId = r.PostReportId,
                    ReportType = r.ReportType,
                    Status = r.Status,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,

                    PostId = r.AffectedPostId,
                    PostType = r.AffectedPostType,

                    PostTitle = r.AffectedPostType == "EmployerPost"
                        ? _db.EmployerPosts
                            .Where(p => p.EmployerPostId == r.AffectedPostId)
                            .Select(p => p.Title)
                            .FirstOrDefault()
                        : _db.JobSeekerPosts
                            .Where(p => p.JobSeekerPostId == r.AffectedPostId)
                            .Select(p => p.Title)
                            .FirstOrDefault()
                })
                .ToListAsync();

            return list;
        }


        // CHECK DUPLICATE REPORT (UNIFIED)

        public Task<bool> HasRecentDuplicateAsync(int reporterId, string reportType, int affectedPostId, int withinMinutes)
        {
            var since = System.DateTime.UtcNow.AddMinutes(-withinMinutes);

            return _db.PostReports.AnyAsync(r =>
                r.ReporterId == reporterId &&
                r.ReportType == reportType &&
                r.AffectedPostId == affectedPostId &&
                r.CreatedAt >= since &&
                r.Status == "Pending");
        }
        // SUPPORT TITLE FETCHING

        public async Task<string?> GetEmployerPostTitleAsync(int employerPostId)
        {
            return await _db.EmployerPosts
                .Where(p => p.EmployerPostId == employerPostId)
                .Select(p => p.Title)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetJobSeekerPostTitleAsync(int jobSeekerPostId)
        {
            return await _db.JobSeekerPosts
                .Where(p => p.JobSeekerPostId == jobSeekerPostId)
                .Select(p => p.Title)
                .FirstOrDefaultAsync();
        }

        // GET ADMIN USER ID (ROLE = ADMIN)

        public async Task<int> GetAdminUserIdAsync()
        {
            return await _db.Users
                .Where(u => u.IsActive && u.Roles.Any(r => r.RoleId == 1))
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repo.Interface;
using PTJ_Models;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repo.Implement
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminReportRepository(JobMatchingDbContext db) => _db = db;

        public async Task<IEnumerable<object>> GetPendingReportsAsync()
        {
            return await _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == "Pending")
                .Select(r => new
                {
                    r.PostReportId,
                    r.ReportType,
                    Reporter = new { r.Reporter.UserId, r.Reporter.Email },
                    TargetUser = r.TargetUser != null ? new { r.TargetUser.UserId, r.TargetUser.Email } : null,
                    r.Reason,
                    r.Status,
                    r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetSolvedReportsAsync()
        {
            return await _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.AffectedUser)
                .Include(s => s.PostReport)
                .Select(s => new
                {
                    s.SolvedPostReportId,
                    s.PostReportId,
                    s.ActionTaken,
                    Admin = new { s.Admin.UserId, s.Admin.Email },
                    AffectedUser = s.AffectedUser != null ? new { s.AffectedUser.UserId, s.AffectedUser.Email } : null,
                    Report = new
                    {
                        s.PostReport.ReportType,
                        s.PostReport.Reason,
                        s.PostReport.CreatedAt
                    },
                    s.Reason,
                    s.SolvedAt
                })
                .OrderByDescending(s => s.SolvedAt)
                .ToListAsync();
        }

        public Task<PostReport?> GetReportByIdAsync(int reportId)
            => _db.PostReports.FirstOrDefaultAsync(r => r.PostReportId == reportId);

        public Task<User?> GetUserByIdAsync(int userId)
            => _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        public Task<EmployerPost?> GetEmployerPostByIdAsync(int postId)
            => _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == postId);

        public Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int postId)
            => _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == postId);

        public async Task AddSolvedReportAsync(PostReportSolved solvedReport)
        {
            await _db.PostReportSolveds.AddAsync(solvedReport);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

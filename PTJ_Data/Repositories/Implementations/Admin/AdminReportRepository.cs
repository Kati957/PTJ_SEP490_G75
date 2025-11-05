using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Data.Repositories.Interfaces.Admin;


namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminReportRepository(JobMatchingDbContext db) => _db = db;

        //  1️⃣ Lấy danh sách report chưa xử lý (Pending)
        public async Task<IEnumerable<AdminReportDto>> GetPendingReportsAsync(string? reportType = null, string? keyword = null)
        {
            var query = _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == "Pending")
                .AsQueryable();

            if (!string.IsNullOrEmpty(reportType))
                query = query.Where(r => r.ReportType == reportType);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(r =>
                    (r.Reason != null && r.Reason.ToLower().Contains(kw)) ||
                    r.Reporter.Email.ToLower().Contains(kw) ||
                    (r.TargetUser != null && r.TargetUser.Email.ToLower().Contains(kw)));
            }

            var result = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdminReportDto
                {
                    ReportId = r.PostReportId,
                    ReportType = r.ReportType,
                    ReporterEmail = r.Reporter.Email,
                    TargetUserEmail = r.TargetUser != null ? r.TargetUser.Email : null,
                    Reason = r.Reason,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return result;
        }

        //  2️⃣ Lấy danh sách report đã xử lý (Resolved)
        public async Task<IEnumerable<AdminSolvedReportDto>> GetSolvedReportsAsync(string? adminKeyword = null)
        {
            var query = _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.AffectedUser)
                .Include(s => s.PostReport)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(adminKeyword))
            {
                var kw = adminKeyword.ToLower();
                query = query.Where(s => s.Admin.Email.ToLower().Contains(kw));
            }

            var result = await query
                .OrderByDescending(s => s.SolvedAt)
                .Select(s => new AdminSolvedReportDto
                {
                    SolvedReportId = s.SolvedPostReportId,
                    ReportId = s.PostReportId,
                    ActionTaken = s.ActionTaken,
                    AdminEmail = s.Admin.Email,
                    TargetUserEmail = s.AffectedUser != null ? s.AffectedUser.Email : null,
                    Reason = s.Reason,
                    SolvedAt = s.SolvedAt,
                    ReportType = s.PostReport.ReportType,
                    ReportReason = s.PostReport.Reason
                })
                .ToListAsync();

            return result;
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
            => await _db.PostReportSolveds.AddAsync(solvedReport);

        public Task SaveChangesAsync()
            => _db.SaveChangesAsync();
    }
}

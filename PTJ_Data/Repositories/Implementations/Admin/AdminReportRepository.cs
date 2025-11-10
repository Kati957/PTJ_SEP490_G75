using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminReportRepository(JobMatchingDbContext db) => _db = db;

        // 1️⃣ Danh sách report chưa xử lý (Pending)
        public async Task<PagedResult<AdminReportDto>> GetPendingReportsPagedAsync(
            string? reportType = null, string? keyword = null, int page = 1, int pageSize = 10)
        {
            var query = _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == "Pending")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(r => r.ReportType == reportType);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(r =>
                    r.Reporter.Email.ToLower().Contains(kw) ||
                    (r.TargetUser != null && r.TargetUser.Email.ToLower().Contains(kw)) ||
                    (r.Reason != null && r.Reason.ToLower().Contains(kw)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedResult<AdminReportDto>(items, total, page, pageSize);
        }

        // 2️⃣ Danh sách report đã xử lý (Solved)
        public async Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsPagedAsync(
            string? adminEmail = null, string? reportType = null, int page = 1, int pageSize = 10)
        {
            var query = _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.AffectedUser)
                .Include(s => s.PostReport)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(adminEmail))
                query = query.Where(s => s.Admin.Email.Contains(adminEmail));

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(s => s.PostReport.ReportType == reportType);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.SolvedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new AdminSolvedReportDto
                {
                    SolvedReportId = s.SolvedPostReportId,
                    ReportId = s.PostReportId,
                    ActionTaken = s.ActionTaken,
                    AdminEmail = s.Admin.Email,
                    TargetUserEmail = s.AffectedUser != null ? s.AffectedUser.Email : null,
                    ReportType = s.PostReport.ReportType,
                    ReportReason = s.PostReport.Reason,
                    Reason = s.Reason,
                    SolvedAt = s.SolvedAt
                })
                .ToListAsync();

            return new PagedResult<AdminSolvedReportDto>(items, total, page, pageSize);
        }

        // 3️⃣ Chi tiết từng report (dành cho GET /api/admin/reports/{id})
        public async Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
        {
            var report = await _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Include(r => r.EmployerPost)
                .Include(r => r.JobSeekerPost)
                .FirstOrDefaultAsync(r => r.PostReportId == reportId);

            if (report == null) return null;

            return new AdminReportDetailDto
            {
                ReportId = report.PostReportId,
                ReportType = report.ReportType,
                ReporterEmail = report.Reporter?.Email ?? "Unknown",
                TargetUserEmail = report.TargetUser?.Email,
                TargetUserRole = report.TargetUser?.Roles.FirstOrDefault()?.RoleName,
                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                EmployerPostId = report.EmployerPostId,
                EmployerPostTitle = report.EmployerPost?.Title,
                JobSeekerPostId = report.JobSeekerPostId,
                JobSeekerPostTitle = report.JobSeekerPost?.Title
            };
        }

        // 4️⃣ Các hàm phụ trợ xử lý
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

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

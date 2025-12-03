using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly JobMatchingDbContext _db;

        public AdminReportRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<AdminReportDto>> GetPendingReportsPagedAsync(
            string? reportType, string? keyword, int page, int pageSize)
        {
            var q = _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == "Pending")
                .AsQueryable();

            if (!string.IsNullOrEmpty(reportType))
                q = q.Where(r => r.ReportType == reportType);

            if (!string.IsNullOrEmpty(keyword))
                q = q.Where(r =>
                    (r.Reason != null && r.Reason.Contains(keyword)) ||
                    r.Reporter.Email.Contains(keyword));

            int total = await q.CountAsync();

            var data = await q
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminReportDto
                {
                    ReportId = r.PostReportId,
                    ReportType = r.ReportType,
                    ReporterEmail = r.Reporter.Email,
                    TargetUserEmail = r.TargetUser != null ? r.TargetUser.Email : null,
                    PostId = r.AffectedPostId,
                    PostType = r.AffectedPostType,
                    PostTitle = null,
                    Reason = r.Reason,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminReportDto>(data, total, page, pageSize);
        }

        public async Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsPagedAsync(
            string? adminEmail, string? reportType, int page, int pageSize)
        {
            var q = _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.PostReport)
                .ThenInclude(r => r.TargetUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(adminEmail))
                q = q.Where(s => s.Admin.Email.Contains(adminEmail));

            if (!string.IsNullOrEmpty(reportType))
                q = q.Where(s => s.PostReport.ReportType == reportType);

            int total = await q.CountAsync();

            var data = await q
                .OrderByDescending(s => s.SolvedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new AdminSolvedReportDto
                {
                    SolvedReportId = s.SolvedPostReportId,
                    ReportId = s.PostReportId,
                    ActionTaken = s.ActionTaken,
                    Reason = s.Reason,
                    SolvedAt = s.SolvedAt,
                    AdminEmail = s.Admin.Email,
                    PostId = s.AffectedPostId,
                    PostType = s.AffectedPostType,
                    PostTitle = null,
                    TargetUserId = s.AffectedUserId,
                    TargetUserEmail = s.PostReport.TargetUser != null
                        ? s.PostReport.TargetUser.Email : null
                })
                .ToListAsync();

            return new PagedResult<AdminSolvedReportDto>(data, total, page, pageSize);
        }

        public async Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
        {
            return await _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.PostReportId == reportId)
                .Select(r => new AdminReportDetailDto
                {
                    ReportId = r.PostReportId,
                    ReporterId = r.ReporterId,
                    ReporterEmail = r.Reporter.Email,
                    TargetUserId = r.TargetUserId,
                    TargetUserEmail = r.TargetUser != null ? r.TargetUser.Email : null,
                    PostId = r.AffectedPostId,
                    PostType = r.AffectedPostType,
                    PostTitle = null,
                    ReportType = r.ReportType,
                    Reason = r.Reason,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public Task<PostReport?> GetReportByIdAsync(int reportId)
        {
            return _db.PostReports
                .FirstOrDefaultAsync(r => r.PostReportId == reportId);
        }

        public Task<EmployerPost?> GetEmployerPostByIdAsync(int id)
        {
            return _db.EmployerPosts
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);
        }

        public Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int id)
        {
            return _db.JobSeekerPosts
                .FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
        }

        public async Task AddSolvedReportAsync(PostReportSolved solved)
        {
            await _db.PostReportSolveds.AddAsync(solved);
        }

        public Task<PostReportSolved?> GetSolvedReportByReportIdAsync(int reportId)
        {
            return _db.PostReportSolveds
                .FirstOrDefaultAsync(s => s.PostReportId == reportId);
        }

        public async Task<AdminSolvedReportDto?> GetSolvedReportDetailAsync(int solvedId)
        {
            return await _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.PostReport)
                .ThenInclude(r => r.TargetUser)
                .Where(s => s.SolvedPostReportId == solvedId)
                .Select(s => new AdminSolvedReportDto
                {
                    SolvedReportId = s.SolvedPostReportId,
                    ReportId = s.PostReportId,
                    ActionTaken = s.ActionTaken,
                    Reason = s.Reason,
                    SolvedAt = s.SolvedAt,
                    AdminEmail = s.Admin.Email,
                    PostId = s.AffectedPostId,
                    PostType = s.AffectedPostType,
                    PostTitle = null,
                    TargetUserId = s.AffectedUserId,
                    TargetUserEmail = s.PostReport.TargetUser != null
                        ? s.PostReport.TargetUser.Email : null
                })
                .FirstOrDefaultAsync();
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}

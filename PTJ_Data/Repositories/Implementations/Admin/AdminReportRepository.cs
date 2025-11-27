using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly JobMatchingDbContext _db;

        public AdminReportRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        // Lấy danh sách REPORT đang chờ xử lý (PENDING)

        public async Task<PagedResult<AdminReportDto>> GetPendingReportsPagedAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == "Pending")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(r => r.ReportType == reportType);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(r =>
                    r.Reporter.Email.Contains(keyword) ||
                    r.TargetUser.Email.Contains(keyword));

            int total = await query.CountAsync();

            var data = await query
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

                    PostTitle = r.AffectedPostType == "EmployerPost"
                        ? _db.EmployerPosts.Where(p => p.EmployerPostId == r.AffectedPostId).Select(p => p.Title).FirstOrDefault()
                        : _db.JobSeekerPosts.Where(p => p.JobSeekerPostId == r.AffectedPostId).Select(p => p.Title).FirstOrDefault(),

                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status
                })
                .ToListAsync();

            return new PagedResult<AdminReportDto>(data, total, page, pageSize);
        }

        // Lấy danh sách REPORT đã xử lý
        public async Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsPagedAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.PostReportSolveds
                .Include(s => s.PostReport)
                .Include(s => s.Admin)
                .Include(s => s.AffectedUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(adminEmail))
                query = query.Where(s => s.Admin.Email.Contains(adminEmail));

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(s => s.PostReport.ReportType == reportType);

            int total = await query.CountAsync();

            var data = await query
                .OrderByDescending(s => s.SolvedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new AdminSolvedReportDto
                {
                    SolvedReportId = s.SolvedPostReportId,
                    ReportId = s.PostReportId,
                    ActionTaken = s.ActionTaken,
                    AdminEmail = s.Admin.Email,

                    PostId = s.AffectedPostId,
                    PostType = s.AffectedPostType,
                    PostTitle = s.AffectedPostType == "EmployerPost"
                        ? _db.EmployerPosts.Where(p => p.EmployerPostId == s.AffectedPostId).Select(p => p.Title).FirstOrDefault()
                        : _db.JobSeekerPosts.Where(p => p.JobSeekerPostId == s.AffectedPostId).Select(p => p.Title).FirstOrDefault(),

                    TargetUserId = s.AffectedUserId,
                    TargetUserEmail = s.AffectedUser != null ? s.AffectedUser.Email : null,

                    Reason = s.Reason,
                    SolvedAt = s.SolvedAt
                })
                .ToListAsync();

            return new PagedResult<AdminSolvedReportDto>(data, total, page, pageSize);
        }

        // Chi tiết một REPORT

        public async Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
        {
            var r = await _db.PostReports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .FirstOrDefaultAsync(r => r.PostReportId == reportId);

            if (r == null) return null;

            string? title = null;

            if (r.AffectedPostType == "EmployerPost")
            {
                title = await _db.EmployerPosts
                    .Where(p => p.EmployerPostId == r.AffectedPostId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync();
            }
            else if (r.AffectedPostType == "JobSeekerPost")
            {
                title = await _db.JobSeekerPosts
                    .Where(p => p.JobSeekerPostId == r.AffectedPostId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync();
            }

            return new AdminReportDetailDto
            {
                ReportId = r.PostReportId,
                ReportType = r.ReportType,

                ReporterId = r.ReporterId,
                ReporterEmail = r.Reporter.Email,

                TargetUserId = r.TargetUserId,
                TargetUserEmail = r.TargetUser != null ? r.TargetUser.Email : null,

                PostId = r.AffectedPostId,
                PostType = r.AffectedPostType,
                PostTitle = title,

                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            };
        }
        // Lấy report theo ID

        public Task<PostReport?> GetReportByIdAsync(int id)
            => _db.PostReports.FirstOrDefaultAsync(r => r.PostReportId == id);

        //Lấy bài viết EmployerPost / JobSeekerPost theo ID

        public Task<EmployerPost?> GetEmployerPostByIdAsync(int id)
            => _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);

        public Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int id)
            => _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == id);

        // Lưu solved report

        public async Task AddSolvedReportAsync(PostReportSolved solved)
        {
            await _db.PostReportSolveds.AddAsync(solved);
        }
        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

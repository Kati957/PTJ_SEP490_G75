using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminSystemReportService : IAdminSystemReportService
    {
        private readonly JobMatchingDbContext _db;

        public AdminSystemReportService(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<SystemReportViewDto>> GetSystemReportsAsync(
            string? status, string? keyword, int page, int pageSize)
        {
            var query = _db.SystemReports
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(kw) ||
                    r.Description.ToLower().Contains(kw) ||
                    r.User.Email.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new SystemReportViewDto
                {
                    ReportId = r.SystemReportId,
                    UserEmail = r.User.Email,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<SystemReportViewDto>(items, total, page, pageSize);
        }

        public async Task<SystemReportViewDto?> GetSystemReportDetailAsync(int id)
        {
            return await _db.SystemReports
                .Include(r => r.User)
                .Where(r => r.SystemReportId == id)
                .Select(r => new SystemReportViewDto
                {
                    ReportId = r.SystemReportId,
                    UserEmail = r.User.Email,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var report = await _db.SystemReports.FindAsync(id);
            if (report == null) return false;

            report.Status = status;
            report.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
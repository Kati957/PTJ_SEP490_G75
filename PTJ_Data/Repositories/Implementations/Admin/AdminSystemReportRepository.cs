using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminSystemReportRepository : IAdminSystemReportRepository
    {
        private readonly JobMatchingOpenAiDbContext _db;

        public AdminSystemReportRepository(JobMatchingOpenAiDbContext db)
        {
            _db = db;
        }

        // 1️⃣ Danh sách report + filter + phân trang
        public async Task<PagedResult<AdminSystemReportDto>> GetSystemReportsPagedAsync(
            string? status = null, string? keyword = null,
            int page = 1, int pageSize = 10)
        {
            var query = _db.SystemReports
                .Include(r => r.User)
                .AsQueryable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            // Tìm kiếm
            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(kw) ||
                    r.Description.ToLower().Contains(kw) ||
                    r.User.Email.ToLower().Contains(kw) ||
                    r.User.Username.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminSystemReportDto
                {
                    ReportId = r.SystemReportId,
                    UserId = r.UserId,
                    UserEmail = r.User.Email,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminSystemReportDto>(items, total, page, pageSize);
        }

        // 2️⃣ Chi tiết
        public async Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id)
        {
            return await _db.SystemReports
                .Include(r => r.User)
                .Where(r => r.SystemReportId == id)
                .Select(r => new SystemReportDetailDto
                {
                    ReportId = r.SystemReportId,
                    UserId = r.UserId,
                    UserEmail = r.User.Email,
                    FullName = r.User.JobSeekerProfile != null
                        ? r.User.JobSeekerProfile.FullName
                        : r.User.EmployerProfile != null
                            ? r.User.EmployerProfile.DisplayName
                            : r.User.Username,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        // 3️⃣ Admin cập nhật trạng thái
        public async Task<bool> UpdateReportStatusAsync(int id, string status)
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

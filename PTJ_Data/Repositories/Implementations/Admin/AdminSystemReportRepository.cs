using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminSystemReportRepository : IAdminSystemReportRepository
    {
        private readonly JobMatchingDbContext _db;

        public AdminSystemReportRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        // 1️⃣ Danh sách hệ thống có filter + phân trang
        public async Task<PagedResult<AdminSystemReportDto>> GetSystemReportsPagedAsync(
            string? status = null, string? keyword = null,
            int page = 1, int pageSize = 10)
        {
            var query = _db.SystemReports
                .Include(r => r.User)
                .Include(r => r.ProcessedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

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
                    UpdatedAt = r.UpdatedAt,
                    AdminNote = r.AdminNote,
                    ProcessedBy = r.ProcessedByAdmin != null
                        ? r.ProcessedByAdmin.Username
                        : null
                })
                .ToListAsync();

            return new PagedResult<AdminSystemReportDto>(items, total, page, pageSize);
        }

        // 2️⃣ Chi tiết hệ thống
        public async Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id)
        {
            return await _db.SystemReports
                .Include(r => r.User)
                .Include(r => r.ProcessedByAdmin)
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
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    ProcessedBy = r.ProcessedByAdmin != null
                        ? r.ProcessedByAdmin.Username
                        : null
                })
                .FirstOrDefaultAsync(r => r.ReportId == id);
        }

        // 3️⃣ Admin cập nhật trạng thái + note + admin xử lý
        public async Task<bool> UpdateReportStatusAsync(int id, int adminId, string status, string? note)
        {
            var report = await _db.SystemReports.FindAsync(id);
            if (report == null) return false;

            report.Status = status;
            report.AdminNote = note;
            report.ProcessedByAdminId = adminId;
            report.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

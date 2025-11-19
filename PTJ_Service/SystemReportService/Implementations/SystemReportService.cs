using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTOs;
using PTJ_Models.Models;
using PTJ_Service.SystemReportService.Interfaces;

namespace PTJ_Service.SystemReportService.Implementations
{
    public class SystemReportService : ISystemReportService
    {
        private readonly JobMatchingDbContext _context;

        public SystemReportService(JobMatchingDbContext context)
        {
            _context = context;
        }

        // 1️⃣ USER TẠO REPORT
        public async Task<bool> CreateReportAsync(int userId, SystemReportCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
                throw new Exception("Tiêu đề phải có ít nhất 5 ký tự.");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 10)
                throw new Exception("Nội dung mô tả phải có ít nhất 10 ký tự.");

            var twoMinutesAgo = DateTime.UtcNow.AddMinutes(-2);
            bool recentlySent = await _context.SystemReports
                .AnyAsync(r => r.UserId == userId && r.CreatedAt >= twoMinutesAgo);

            if (recentlySent)
                throw new Exception("Bạn đang gửi báo cáo quá nhanh. Vui lòng đợi một lúc rồi thử lại.");

            var report = new SystemReport
            {
                UserId = userId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _context.SystemReports.AddAsync(report);
            await _context.SaveChangesAsync();

            return true;
        }


        // 2️⃣ USER XEM REPORT CỦA MÌNH
        public async Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId)
        {
            return await _context.SystemReports
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .Include(r => r.ProcessedByAdmin)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SystemReportViewDto
                {
                    SystemReportId = r.SystemReportId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Username : null
                })
                .ToListAsync();
        }

        // 3️⃣ ADMIN XEM TẤT CẢ REPORT
        public async Task<IEnumerable<SystemReportViewDto>> GetAllReportsAsync(string? status, string? keyword)
        {
            var query = _context.SystemReports
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.ProcessedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(r =>
                    r.Title.Contains(keyword) ||
                    r.Description.Contains(keyword) ||
                    r.User.Username.Contains(keyword) ||
                    r.UserId.ToString().Contains(keyword) // thêm search theo ID
                );

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SystemReportViewDto
                {
                    SystemReportId = r.SystemReportId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Username : null
                })
                .ToListAsync();
        }

        // 4️⃣ ADMIN UPDATE STATUS
        public async Task<bool> UpdateStatusAsync(int reportId, int adminId, SystemReportUpdateStatusDto dto)
        {
            var report = await _context.SystemReports.FindAsync(reportId);
            if (report == null)
                throw new Exception("Không tìm thấy báo cáo.");

            var validStatus = new[] { "Pending", "Solved", "Rejected" };

            if (!validStatus.Contains(dto.Status))
                throw new Exception("Trạng thái không hợp lệ.");

            if (report.Status == "Solved" && dto.Status == "Pending")
                throw new Exception("Không thể chuyển báo cáo đã xử lý về lại trạng thái chờ.");

            report.Status = dto.Status;
            report.AdminNote = dto.AdminNote;
            report.ProcessedByAdminId = adminId;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

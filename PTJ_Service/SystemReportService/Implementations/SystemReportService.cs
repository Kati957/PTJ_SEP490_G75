using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO;
using PTJ_Service.SystemReportService.Interfaces;

namespace PTJ_Service.SystemReportService.Implementations
{
    public class SystemReportService : ISystemReportService
    {
        private readonly JobMatchingOpenAiDbContext _db;

        public SystemReportService(JobMatchingOpenAiDbContext db)
        {
            _db = db;
        }

        //Tạo báo cáo hệ thống
        public async Task<bool> CreateReportAsync(int userId, SystemReportCreateDto dto)
        {
            // Kiểm tra DTO null
            if (dto == null)
                throw new Exception("Dữ liệu gửi lên không hợp lệ.");

            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length < 5)
                throw new Exception("Tiêu đề phải có ít nhất 5 ký tự.");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().Length < 10)
                throw new Exception("Mô tả phải có ít nhất 10 ký tự.");

            // Kiểm tra user tồn tại
            var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
                throw new Exception("Người dùng không tồn tại.");

            var newReport = new SystemReport
            {
                UserId = userId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _db.SystemReports.Add(newReport);
            await _db.SaveChangesAsync();
            return true;
        }

        //  Lấy danh sách report theo user
        public async Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId)
        {
            return await _db.SystemReports
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SystemReportViewDto
                {
                    ReportId = r.SystemReportId,
                    UserEmail = r.User != null ? r.User.Email : "(unknown)",
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();
        }

        // Admin cập nhật trạng thái report
        public async Task<bool> UpdateReportStatusAsync(int reportId, SystemReportUpdateDto dto)
        {
            if (dto == null)
                throw new Exception("Dữ liệu cập nhật không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                throw new Exception("Trạng thái không được để trống.");

            var report = await _db.SystemReports.FindAsync(reportId);
            if (report == null)
                throw new Exception("Không tìm thấy báo cáo.");

            report.Status = dto.Status.Trim();
            report.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

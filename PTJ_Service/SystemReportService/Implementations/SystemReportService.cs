using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTOs;
using PTJ_Models.Models;
using PTJ_Service.SystemReportService.Interfaces;


namespace PTJ_Service.SystemReportService.Implementations
{
    public class SystemReportService : ISystemReportService
    {
        private readonly JobMatchingDbContext _db;

        public SystemReportService(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CreateReportAsync(int userId, SystemReportCreateDto dto)
            {
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
                throw new Exception("Tiêu đề phải có ít nhất 5 ký tự.");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 10)
                throw new Exception("Mô tả phải có ít nhất 10 ký tự.");

            var newReport = new SystemReport
                {
                UserId = userId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.MinValue // Use DateTime.MinValue as a default value instead of null
                };

            _db.SystemReports.Add(newReport);
            await _db.SaveChangesAsync();
            return true;
            }

        public async Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId)
        {
            return await _db.SystemReports
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
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
        }
    }
}
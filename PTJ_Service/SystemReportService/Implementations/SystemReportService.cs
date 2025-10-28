using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> CreateReportAsync(SystemReportCreateDto dto)
        {
            var report = new SystemReport
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Description = dto.Description,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.SystemReports.Add(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId)
        {
            return await _context.SystemReports
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .Select(r => new SystemReportViewDto
                {
                    SystemReportId = r.SystemReportId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemReportViewDto>> GetAllReportsAsync()
        {
            return await _context.SystemReports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SystemReportViewDto
                {
                    SystemReportId = r.SystemReportId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Title = r.Title,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(int reportId, string status)
        {
            var report = await _context.SystemReports.FindAsync(reportId);
            if (report == null) return false;

            report.Status = status;
            report.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

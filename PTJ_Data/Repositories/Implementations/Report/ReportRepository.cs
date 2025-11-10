using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
using PTJ_Models.DTO.Report;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private readonly JobMatchingDbContext _db;
        public ReportRepository(JobMatchingDbContext db) => _db = db;

        public Task<bool> EmployerPostExistsAsync(int employerPostId)
            => _db.EmployerPosts.AnyAsync(p => p.EmployerPostId == employerPostId && p.Status != "Deleted");

        public Task<bool> JobSeekerPostExistsAsync(int jobSeekerPostId)
            => _db.JobSeekerPosts.AnyAsync(p => p.JobSeekerPostId == jobSeekerPostId && p.Status != "Deleted");

        public async Task AddAsync(PostReport report)
        {
            await _db.PostReports.AddAsync(report);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        public async Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
        {
            // Join sang 2 bảng để lấy Title (nếu có)
            var q = from r in _db.PostReports
                    where r.ReporterId == reporterId
                    orderby r.CreatedAt descending
                    select new MyReportDto
                    {
                        ReportId = r.PostReportId,
                        ReportType = r.ReportType,
                        ReportedItemId = r.ReportedItemId,
                        EmployerPostTitle = r.EmployerPost != null ? r.EmployerPost.Title : null,
                        JobSeekerPostTitle = r.JobSeekerPost != null ? r.JobSeekerPost.Title : null,
                        Status = r.Status,
                        Reason = r.Reason,
                        CreatedAt = r.CreatedAt
                    };

            return await q.ToListAsync();
        }

        public Task<bool> HasRecentDuplicateAsync(int reporterId, string reportType, int reportedItemId, int withinMinutes)
        {
            var since = System.DateTime.UtcNow.AddMinutes(-withinMinutes);
            return _db.PostReports.AnyAsync(r =>
                r.ReporterId == reporterId &&
                r.ReportType == reportType &&
                r.ReportedItemId == reportedItemId &&
                r.CreatedAt >= since &&
                r.Status == "Pending");
        }
    }
}

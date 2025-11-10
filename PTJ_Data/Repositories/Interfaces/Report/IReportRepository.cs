using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Report;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces
{
    public interface IReportRepository
    {
        // Validate entities
        Task<bool> EmployerPostExistsAsync(int employerPostId);
        Task<bool> JobSeekerPostExistsAsync(int jobSeekerPostId);

        // Create
        Task AddAsync(PostReport report);
        Task SaveChangesAsync();

        // Query
        Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId);

        // (optional) Chống spam: đã report cùng item trong X phút?
        Task<bool> HasRecentDuplicateAsync(int reporterId, string reportType, int reportedItemId, int withinMinutes);
    }
}

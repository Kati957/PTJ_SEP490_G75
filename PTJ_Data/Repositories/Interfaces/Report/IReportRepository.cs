using PTJ_Models.DTO;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces
{
    public interface IReportRepository
    {
        Task<bool> EmployerPostExistsAsync(int employerPostId);
        Task<bool> JobSeekerPostExistsAsync(int jobSeekerPostId);
        Task AddAsync(PostReport report);
        Task SaveChangesAsync();
        Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId);
        Task<bool> HasRecentDuplicateAsync(int reporterId, string reportType, int affectedPostId, int withinMinutes);
        Task<string?> GetEmployerPostTitleAsync(int employerPostId);
        Task<string?> GetJobSeekerPostTitleAsync(int jobSeekerPostId);
        Task<int> GetAdminUserIdAsync();
    }
}

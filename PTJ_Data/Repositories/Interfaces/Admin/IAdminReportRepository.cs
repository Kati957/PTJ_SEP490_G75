using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repo.Interface
{
    public interface IAdminReportRepository
    {
        Task<IEnumerable<object>> GetPendingReportsAsync();
        Task<IEnumerable<object>> GetSolvedReportsAsync();
        Task<PostReport?> GetReportByIdAsync(int reportId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<EmployerPost?> GetEmployerPostByIdAsync(int postId);
        Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int postId);
        Task AddSolvedReportAsync(PostReportSolved solvedReport);
        Task SaveChangesAsync();
    }
}

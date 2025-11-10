using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Models.DTO;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminReportRepository
    {
        Task<PagedResult<AdminReportDto>> GetPendingReportsPagedAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);
        Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsPagedAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10);
        Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId);
        Task<PostReport?> GetReportByIdAsync(int reportId);
        Task<EmployerPost?> GetEmployerPostByIdAsync(int postId);
        Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int postId);
        Task AddSolvedReportAsync(PostReportSolved solvedReport);
        Task SaveChangesAsync();
    }
}

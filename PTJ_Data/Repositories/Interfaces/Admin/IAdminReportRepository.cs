using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

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
        Task<PostReportSolved?> GetSolvedReportByReportIdAsync(int reportId);
        Task<AdminSolvedReportDto?> GetSolvedReportDetailAsync(int solvedId);
        Task<EmployerPost?> GetEmployerPostByIdAsync(int id);
        Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int id);
        Task AddSolvedReportAsync(PostReportSolved solved);
        Task SaveChangesAsync();
    }
}

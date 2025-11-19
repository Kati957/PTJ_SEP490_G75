using PTJ_Models.DTO.Admin;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminSystemReportRepository
    {
        Task<PagedResult<AdminSystemReportDto>> GetSystemReportsPagedAsync(
            string? status = null, string? keyword = null,
            int page = 1, int pageSize = 10);

        Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id);

        Task<bool> MarkReportSolvedAsync(int id, string? note = null);
    }
}

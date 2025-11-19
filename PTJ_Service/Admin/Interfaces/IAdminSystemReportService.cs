using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Interfaces.Admin
{
    public interface IAdminSystemReportService
    {
        Task<PagedResult<AdminSystemReportDto>> GetSystemReportsAsync(
            string? status, string? keyword, int page, int pageSize);

        Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id);

        Task<bool> MarkReportSolvedAsync(int id, string? note = null);
    }
}

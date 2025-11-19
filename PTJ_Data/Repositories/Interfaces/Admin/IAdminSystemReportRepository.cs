using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;

public interface IAdminSystemReportRepository
{
    Task<PagedResult<AdminSystemReportDto>> GetSystemReportsPagedAsync(
        string? status = null, string? keyword = null,
        int page = 1, int pageSize = 10);

    Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id);

    Task<bool> UpdateReportStatusAsync(int id, string status);
}

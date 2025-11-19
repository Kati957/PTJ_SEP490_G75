using PTJ_Models.DTO.Admin;
using PTJ_Models.DTOs;

public interface IAdminSystemReportService
{
    Task<PagedResult<SystemReportViewDto>> GetSystemReportsAsync(
        string? status, string? keyword, int page, int pageSize);

    Task<SystemReportViewDto?> GetSystemReportDetailAsync(int id);

    Task<bool> UpdateStatusAsync(int id, string status);
}

using PTJ_Models.DTO.Admin;

public interface IAdminSystemReportService
{
    Task<PagedResult<AdminSystemReportDto>> GetSystemReportsAsync(
        string? status, string? keyword, int page, int pageSize);

    Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id);

    Task<bool> UpdateReportStatusAsync(int reportId, int adminId, string status, string? note);
}

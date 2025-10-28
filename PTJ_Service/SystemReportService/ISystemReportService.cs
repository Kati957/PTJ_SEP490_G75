using PTJ_Models.DTOs;

namespace PTJ_Service.SystemReportService
{
    public interface ISystemReportService
    {
        Task<bool> CreateReportAsync(SystemReportCreateDto dto);
        Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId);
        Task<IEnumerable<SystemReportViewDto>> GetAllReportsAsync();
        Task<bool> UpdateStatusAsync(int reportId, string status);
    }
}

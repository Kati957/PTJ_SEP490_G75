using PTJ_Models.DTOs;

namespace PTJ_Service.SystemReportService.Interfaces
{
    public interface ISystemReportService
    {
        
        Task<bool> CreateReportAsync(int userId, SystemReportCreateDto dto);
        Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId);
        Task<IEnumerable<SystemReportViewDto>> GetAllReportsAsync(string? status = null, string? keyword = null);
        Task<bool> UpdateStatusAsync(int reportId, int adminId, SystemReportUpdateStatusDto dto);
    }
}

using PTJ_Models.DTO;
namespace PTJ_Service.SystemReportService.Interfaces
{
    public interface ISystemReportService
    {
        Task<bool> CreateReportAsync(int userId, SystemReportCreateDto dto);
        Task<IEnumerable<SystemReportViewDto>> GetReportsByUserAsync(int userId);
    }
}
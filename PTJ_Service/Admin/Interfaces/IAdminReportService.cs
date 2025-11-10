using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;

namespace PTJ_Service.Interfaces.Admin
{
    public interface IAdminReportService
    {
     
        Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10);

        Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId);

        Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId);
    }
}

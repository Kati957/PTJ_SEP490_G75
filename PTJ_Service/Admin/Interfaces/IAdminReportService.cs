using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Interface
{
    public interface IAdminReportService
    {
        Task<IEnumerable<object>> GetPendingReportsAsync();
        Task<IEnumerable<object>> GetSolvedReportsAsync();
        Task ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId);
    }
}

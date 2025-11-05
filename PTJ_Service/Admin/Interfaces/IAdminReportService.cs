using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO; 

namespace PTJ_Service.Interfaces.Admin
{
    public interface IAdminReportService
    {
        // 1️⃣ Danh sách report chưa xử lý (Pending)
        Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        // 2️⃣ Danh sách report đã xử lý (Solved)
        Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10);

        // 3️⃣ Xử lý report (BanUser / DeletePost / Warn / Ignore)
        Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId);
    }
}

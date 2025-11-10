using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Interfaces.Admin;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminSystemReportService : IAdminSystemReportService
    {
        private readonly IAdminSystemReportRepository _repo;
        public AdminSystemReportService(IAdminSystemReportRepository repo) => _repo = repo;

        public Task<PagedResult<AdminSystemReportDto>> GetSystemReportsAsync(
            string? status, string? keyword, int page, int pageSize)
            => _repo.GetSystemReportsPagedAsync(status, keyword, page, pageSize);

        public Task<SystemReportDetailDto?> GetSystemReportDetailAsync(int id)
            => _repo.GetSystemReportDetailAsync(id);

        public Task<bool> MarkReportSolvedAsync(int id, string? note = null)
            => _repo.MarkReportSolvedAsync(id, note);
    }
}

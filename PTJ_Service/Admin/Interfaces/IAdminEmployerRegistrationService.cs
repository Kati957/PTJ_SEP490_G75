using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Interfaces.Admin
{
    public interface IAdminEmployerRegistrationService
    {

        Task<PagedResult<AdminEmployerRegListItemDto>> GetRequestsAsync(
            string? status, string? keyword, int page, int pageSize);
        Task<AdminEmployerRegDetailDto?> GetDetailAsync(int requestId);
        Task ApproveAsync(int requestId, int adminId);
        Task ApproveEmployerGoogleAsync(int requestId, int adminId);
        Task RejectAsync(int requestId, AdminEmployerRegRejectDto dto);
        Task RejectGoogleAsync (int requestId, AdminEmployerRegRejectDto dto);
    }
}

using PTJ_Models.DTO.Admin;
using static PTJ_Models.DTO.Admin.GoogleEmployerRegListDto;

public interface IAdminEmployerRegistrationService
{
    // Normal
    Task<PagedResult<AdminEmployerRegListItemDto>> GetRequestsAsync(string? status, string? keyword, int page, int pageSize);
    Task<AdminEmployerRegDetailDto?> GetDetailAsync(int requestId);
    Task ApproveAsync(int requestId, int adminId);
    Task RejectAsync(int requestId, AdminEmployerRegRejectDto dto);

    // Google
    Task<IEnumerable<GoogleEmployerRegListDto>> GetGoogleRequestsAsync();
    Task<GoogleEmployerRegDetailDto?> GetGoogleDetailAsync(int id);
    Task ApproveEmployerGoogleAsync(int requestId, int adminId);
    Task RejectGoogleAsync(int requestId, AdminEmployerRegRejectDto dto);
}

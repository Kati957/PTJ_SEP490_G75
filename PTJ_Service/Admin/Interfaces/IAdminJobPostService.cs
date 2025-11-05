using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminJobPostService
    {
        // Employer
        Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id);
        Task<string> ToggleEmployerPostBlockedAsync(int id);

        // JobSeeker
        Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id);
        Task<string> ToggleJobSeekerPostArchivedAsync(int id);
    }
}

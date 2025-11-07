using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminJobPostService
    {
        // Employer Posts
        Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id);
        Task ToggleEmployerPostBlockedAsync(int id);

        // JobSeeker Posts
        Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id);
        Task ToggleJobSeekerPostArchivedAsync(int id);
    }
}

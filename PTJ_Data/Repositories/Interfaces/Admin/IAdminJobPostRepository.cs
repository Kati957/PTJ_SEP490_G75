using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminJobPostRepository
    {
        // Employer Posts
        Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsPagedAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id);
        Task<bool> ToggleEmployerPostBlockedAsync(int id);

        // JobSeeker Posts
        Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsPagedAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id);
        Task<bool> ToggleJobSeekerPostArchivedAsync(int id);
    }
}

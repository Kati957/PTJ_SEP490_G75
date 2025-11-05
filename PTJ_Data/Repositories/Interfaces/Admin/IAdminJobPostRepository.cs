using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminJobPostRepository
    {
        Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id);
        Task<string?> ToggleEmployerPostBlockedAsync(int id);
        Task<EmployerPost?> GetEmployerPostEntityAsync(int id);

        Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize);
        Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id);
        Task<string?> ToggleJobSeekerPostArchivedAsync(int id);
        Task<JobSeekerPost?> GetJobSeekerPostEntityAsync(int id);

        Task SaveChangesAsync();
    }
}

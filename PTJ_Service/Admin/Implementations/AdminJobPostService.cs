using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminJobPostService : IAdminJobPostService
    {
        private readonly IAdminJobPostRepository _repo;
        public AdminJobPostService(IAdminJobPostRepository repo) => _repo = repo;

        public Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize)
            => _repo.GetEmployerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

        public Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
            => _repo.GetEmployerPostDetailAsync(id);

        public async Task ToggleEmployerPostBlockedAsync(int id)
        {
            var ok = await _repo.ToggleEmployerPostBlockedAsync(id);
            if (!ok) throw new KeyNotFoundException("Employer post not found.");
        }

        public Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string? status, int? categoryId, string? keyword, int page, int pageSize)
            => _repo.GetJobSeekerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

        public Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id)
            => _repo.GetJobSeekerPostDetailAsync(id);

        public async Task ToggleJobSeekerPostArchivedAsync(int id)
        {
            var ok = await _repo.ToggleJobSeekerPostArchivedAsync(id);
            if (!ok) throw new KeyNotFoundException("JobSeeker post not found.");
        }
    }
}

using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminJobPostService : IAdminJobPostService
    {
        private readonly IAdminJobPostRepository _repo;
        public AdminJobPostService(IAdminJobPostRepository repo) => _repo = repo;

        //  Employer Posts 

        public Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
            => _repo.GetEmployerPostsAsync(status, categoryId, keyword, page, pageSize);

        public Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
            => _repo.GetEmployerPostDetailAsync(id);

        public async Task<string> ToggleEmployerPostBlockedAsync(int id)
        {
            var status = await _repo.ToggleEmployerPostBlockedAsync(id);
            if (status == null)
                throw new KeyNotFoundException($"Employer post with ID {id} not found.");
            return status;
        }

        //  JobSeeker Posts 

        public Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(
            string? status = null,
            int? categoryId = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
            => _repo.GetJobSeekerPostsAsync(status, categoryId, keyword, page, pageSize);

        public Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id)
            => _repo.GetJobSeekerPostDetailAsync(id);

        public async Task<string> ToggleJobSeekerPostArchivedAsync(int id)
        {
            var status = await _repo.ToggleJobSeekerPostArchivedAsync(id);
            if (status == null)
                throw new KeyNotFoundException($"JobSeeker post with ID {id} not found.");
            return status;
        }
    }
}

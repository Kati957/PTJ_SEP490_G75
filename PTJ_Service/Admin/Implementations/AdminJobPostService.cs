using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminJobPostService : IAdminJobPostService
    {
        private readonly IAdminJobPostRepository _repo;
        public AdminJobPostService(IAdminJobPostRepository repo) => _repo = repo;

        // Employer
        public Task<IEnumerable<AdminEmployerPostDto>> GetEmployerPostsAsync(string status = null, int? categoryId = null, string keyword = null)
            => _repo.GetEmployerPostsAsync(status, categoryId, keyword);

        public Task<AdminEmployerPostDetailDto> GetEmployerPostDetailAsync(int id)
            => _repo.GetEmployerPostDetailAsync(id);

        public async Task ToggleEmployerPostBlockedAsync(int id)
        {
            var ok = await _repo.ToggleEmployerPostBlockedAsync(id);
            if (!ok) throw new KeyNotFoundException("Employer post not found.");
        }

        // JobSeeker
        public Task<IEnumerable<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string status = null, int? categoryId = null, string keyword = null)
            => _repo.GetJobSeekerPostsAsync(status, categoryId, keyword);

        public Task<AdminJobSeekerPostDetailDto> GetJobSeekerPostDetailAsync(int id)
            => _repo.GetJobSeekerPostDetailAsync(id);

        public async Task ToggleJobSeekerPostArchivedAsync(int id)
        {
            var ok = await _repo.ToggleJobSeekerPostArchivedAsync(id);
            if (!ok) throw new KeyNotFoundException("JobSeeker post not found.");
        }
    }
}

using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_Service.SearchService.Implementations
{
    public class JobSeekerSearchService : IJobSeekerSearchService
    {
        private readonly IJobSeekerSearchRepository _repo;

        public JobSeekerSearchService(IJobSeekerSearchRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EmployerPostDtoOut>> SearchEmployerPostsAsync(JobSeekerSearchFilterDto filter)
        {
            return await _repo.SearchEmployerPostsAsync(filter);
        }
    }
}

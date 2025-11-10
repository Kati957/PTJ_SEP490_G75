using PTJ_Data.Repositories.Interfaces.EmployerPost;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_Service.SearchService.Implementations
{
    public class EmployerSearchService : IEmployerSearchService
    {
        private readonly IEmployerSearchRepository _repo;

        public EmployerSearchService(IEmployerSearchRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter)
        {
            return await _repo.SearchJobSeekersAsync(filter);
        }
    }
}

using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Service.SearchService
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

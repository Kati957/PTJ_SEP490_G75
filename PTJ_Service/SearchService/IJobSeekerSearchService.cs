using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Service.SearchService
    {
    public interface IJobSeekerSearchService
        {
        Task<IEnumerable<EmployerPostDtoOut>> SearchEmployerPostsAsync(JobSeekerSearchFilterDto filter);
        }
    }

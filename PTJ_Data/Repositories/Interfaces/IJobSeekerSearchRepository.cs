using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Data.Repositories.Interfaces
    {
    public interface IJobSeekerSearchRepository
        {
        Task<IEnumerable<EmployerPostDtoOut>> SearchEmployerPostsAsync(JobSeekerSearchFilterDto filter);
        }
    }

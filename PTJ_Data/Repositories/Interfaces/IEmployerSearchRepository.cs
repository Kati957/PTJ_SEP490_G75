using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Data.Repositories.Interfaces
    {
    public interface IEmployerSearchRepository
        {
        Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter);
        }
    }

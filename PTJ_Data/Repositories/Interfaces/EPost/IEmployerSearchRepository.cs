using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;

namespace PTJ_Data.Repositories.Interfaces.EmployerPost
{
    public interface IEmployerSearchRepository
    {
        Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter);
    }
}

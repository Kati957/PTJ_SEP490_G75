using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.ReportDTO.SearchDTO;

namespace PTJ_Data.Repositories.Interfaces.EPost
{
    public interface IEmployerSearchRepository
    {
        Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter);
    }
}

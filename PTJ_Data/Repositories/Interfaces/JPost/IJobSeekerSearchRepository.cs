using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.ReportDTO.SearchDTO;

namespace PTJ_Data.Repositories.Interfaces.JPost
{
    public interface IJobSeekerSearchRepository
    {
        Task<IEnumerable<EmployerPostDtoOut>> SearchEmployerPostsAsync(JobSeekerSearchFilterDto filter);
    }
}

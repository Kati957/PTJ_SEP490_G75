using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.ReportDTO.SearchDTO;

namespace PTJ_Service.SearchService.Interfaces
{
    public interface IEmployerSearchService
    {
        Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter);
    }
}

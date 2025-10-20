using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO;

namespace PTJ_Service.JobSeekerPostService
{
    public interface IJobSeekerPostService
    {
        Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto);
        Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync();
        Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId);

        Task<JobSeekerPostDtoOut?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}

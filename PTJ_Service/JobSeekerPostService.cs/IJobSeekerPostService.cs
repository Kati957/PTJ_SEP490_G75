using PTJ_Models.DTO;
using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.JobSeekerPostService
{
    public interface IJobSeekerPostService
    {
        Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto);
        Task<IEnumerable<JobSeekerPost>> GetAllAsync();
        Task<JobSeekerPost?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}

using PTJ_Models.DTO;
using PTJ_Models.Models;

namespace PTJ_Service.JobSeekerPostService
{
    public interface IJobSeekerPostService
    {
        // CRUD
        Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync();
        Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId);
        Task<JobSeekerPostDtoOut?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);

        // CREATE + REFRESH
        Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto);
        Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId);

        // SHORTLIST
        Task SaveJobAsync(SaveJobDto dto);
        Task UnsaveJobAsync(SaveJobDto dto);
        Task<IEnumerable<object>> GetSavedJobsAsync(int jobSeekerId);
    }
}

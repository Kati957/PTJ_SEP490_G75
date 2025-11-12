using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.JPost
    {
    public interface IJobSeekerPostRepository
        {
        Task<IEnumerable<JobSeekerPost>> GetAllAsync();
        Task<IEnumerable<JobSeekerPost>> GetByUserAsync(int userId);
        Task<JobSeekerPost?> GetByIdAsync(int id);
        Task AddAsync(JobSeekerPost post);
        Task UpdateAsync(JobSeekerPost post);
        Task SoftDeleteAsync(int id);
        Task SaveChangesAsync();
        }
    }

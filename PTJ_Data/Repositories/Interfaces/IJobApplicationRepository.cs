using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Interfaces
    {
    public interface IJobApplicationRepository
        {
        Task AddAsync(JobSeekerSubmission entity);
        Task<bool> ExistsAsync(int jobSeekerId, int employerPostId);
        Task<JobSeekerSubmission?> GetAsync(int jobSeekerId, int employerPostId);
        Task<JobSeekerSubmission?> GetByIdAsync(int id);
        Task<IEnumerable<JobSeekerSubmission>> GetByEmployerPostWithDetailAsync(int employerPostId);
        Task<IEnumerable<JobSeekerSubmission>> GetByJobSeekerWithPostDetailAsync(int jobSeekerId);
        Task UpdateAsync(JobSeekerSubmission entity);
        Task SaveChangesAsync();
        }
    }

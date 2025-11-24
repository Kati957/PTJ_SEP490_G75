using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Interfaces
    {
    public interface IJobSeekerCvRepository
        {
        Task<JobSeekerCv?> GetByIdAsync(int id);
        Task<IEnumerable<JobSeekerCv>> GetByJobSeekerAsync(int jobSeekerId);
        Task AddAsync(JobSeekerCv entity);
        Task UpdateAsync(JobSeekerCv entity);
        Task SoftDeleteAsync(int cvId);
        Task SaveChangesAsync();
        }
    }

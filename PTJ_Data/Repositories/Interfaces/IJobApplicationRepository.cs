using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Interfaces
    {
    public interface IJobApplicationRepository
        {
        Task AddAsync(EmployerCandidatesList entity);
        Task<bool> ExistsAsync(int jobSeekerId, int employerPostId);
        Task<EmployerCandidatesList?> GetAsync(int jobSeekerId, int employerPostId);
        Task<EmployerCandidatesList?> GetByIdAsync(int id);
        Task<IEnumerable<EmployerCandidatesList>> GetByEmployerPostWithDetailAsync(int employerPostId);

        Task<IEnumerable<EmployerCandidatesList>> GetByJobSeekerWithPostDetailAsync(int jobSeekerId);

        Task UpdateAsync(EmployerCandidatesList entity);
        Task SaveChangesAsync();
        }
    }

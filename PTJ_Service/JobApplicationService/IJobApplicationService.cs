using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.ApplicationDTO;

namespace PTJ_Service.JobApplicationService
    {
    public interface IJobApplicationService
        {
        Task<bool> ApplyAsync(int jobSeekerId, int employerPostId, string? note = null);
        Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId);
        Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId);
        Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId);
        Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null);
        }
    }

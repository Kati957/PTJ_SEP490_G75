using PTJ_Models.DTO.ApplicationDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.JobApplicationService.Interfaces
    {
    public interface IJobApplicationService
        {
        Task<(bool success, string? error)> ApplyAsync(int jobSeekerId, int employerPostId, string? note, int? cvid = null);
        Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId);
        Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId);
        Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId);
        Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null);
        }

    }

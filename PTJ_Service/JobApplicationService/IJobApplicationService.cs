using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.ApplicationDTO;

namespace PTJ_Service.JobApplicationService
    {
    public interface IJobApplicationService
        {
        // Ứng viên nộp đơn
        Task<bool> ApplyAsync(int jobSeekerId, int employerPostId, string? note = null);

        // Ứng viên rút đơn
        Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId);

        // Employer xem danh sách ứng viên trong bài đăng
        Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId);

        // Jobseeker xem danh sách bài đã ứng tuyển
        Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId);

        // Employer cập nhật trạng thái ứng viên (Accepted / Rejected / Pending / Withdrawn)
        Task<bool> UpdateStatusAsync(int candidateListId, string status, string? note = null);
        }
    }

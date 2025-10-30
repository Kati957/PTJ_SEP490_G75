using PTJ_Models.DTO.ApplicationDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.JobApplicationService.Interfaces
    {
    public interface IJobApplicationService
        {
        // Ứng viên nộp đơn (đã bao gồm validation)
        Task<(bool success, string? error)> ApplyAsync(int jobSeekerId, int employerPostId, string? note);

        // Ứng viên rút đơn
        Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId);

        // Employer xem danh sách ứng viên của bài đăng
        Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId);

        // JobSeeker xem danh sách bài đã ứng tuyển
        Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId);

        // Employer cập nhật trạng thái ứng viên
        Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null);
        }
    }

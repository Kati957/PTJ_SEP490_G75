using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Models.Models;
using PTJ_Service.JobApplicationService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Service.JobApplicationService.Implementations
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _repo;

        public JobApplicationService(IJobApplicationRepository repo)
        {
            _repo = repo;
        }

        // Ứng viên nộp đơn
        public async Task<bool> ApplyAsync(int jobSeekerId, int employerPostId, string? note = null)
        {
            var existing = await _repo.GetAsync(jobSeekerId, employerPostId);
            if (existing != null)
            {
                // Nếu đã từng rút đơn => cho phép nộp lại
                if (existing.Status == "Withdrawn")
                {
                    existing.Status = "Pending";
                    existing.Notes = note;
                    existing.UpdatedAt = DateTime.Now;
                    await _repo.UpdateAsync(existing);
                    return true;
                }
                return false;
            }

            var submission = new JobSeekerSubmission
            {
                JobSeekerId = jobSeekerId,
                EmployerPostId = employerPostId,
                AppliedAt = DateTime.Now,
                Status = "Pending",
                Notes = note,
                UpdatedAt = DateTime.Now
            };

            await _repo.AddAsync(submission);
            return true;
        }

        // Rút đơn
        public async Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId)
        {
            var app = await _repo.GetAsync(jobSeekerId, employerPostId);
            if (app == null)
                return false;

            app.Status = "Withdrawn";
            app.Notes = "Ứng viên đã rút đơn";
            app.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(app);
            return true;
        }

        // Employer xem danh sách ứng viên
        public async Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId)
        {
            var list = await _repo.GetByEmployerPostWithDetailAsync(employerPostId);

            return list.Select(x =>
            {
                var profile = x.JobSeeker.JobSeekerProfile;

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker.Username,
                    FullName = profile?.FullName,
                    Gender = profile?.Gender,
                    BirthYear = profile?.BirthYear,
                    ProfilePicture = profile?.ProfilePicture,
                    Skills = profile?.Skills,
                    Experience = profile?.Experience,
                    Education = profile?.Education,
                    PreferredJobType = profile?.PreferredJobType,
                    PreferredLocation = profile?.PreferredLocation,
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes
                };
            }).ToList();
        }

        // JobSeeker xem các bài đã ứng tuyển
        public async Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId)
        {
            var list = await _repo.GetByJobSeekerWithPostDetailAsync(jobSeekerId);

            return list.Select(x =>
            {
                var post = x.EmployerPost;
                var category = post?.Category;
                var employer = post?.User;

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker?.Username ?? "Unknown",

                    // 🔹 Trạng thái ứng tuyển
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,

                    // 🔹 Thông tin bài đăng tuyển dụng
                    EmployerPostId = post?.EmployerPostId ?? 0,
                    PostTitle = post?.Title,
                    CategoryName = category?.Name,
                    EmployerName = employer?.Username,
                    Location = post?.Location,
                    Salary = post?.Salary,
                    WorkHours = post?.WorkHours,
                    PhoneContact = post?.PhoneContact
                };
            }).ToList();
        }


        // Employer cập nhật trạng thái
        public async Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            // ✅ Chỉ chấp nhận Accepted / Rejected
            var validStatuses = new[] { "Accepted", "Rejected" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                return false;

            var entity = await _repo.GetByIdAsync(submissionId);
            if (entity == null)
                return false;

            entity.Status = status.Trim();
            entity.Notes = note;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);
            return true;
        }
    }
}

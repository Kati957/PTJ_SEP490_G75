using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Service.JobApplicationService
    {
    public class JobApplicationService : IJobApplicationService
        {
        private readonly IJobApplicationRepository _repo;

        public JobApplicationService(IJobApplicationRepository repo)
            {
            _repo = repo;
            }

        // =========================================================
        // ỨNG VIÊN NỘP ĐƠN ỨNG TUYỂN
        // =========================================================
        public async Task<bool> ApplyAsync(int jobSeekerId, int employerPostId, string? note = null)
            {
            var existing = await _repo.GetAsync(jobSeekerId, employerPostId);

            if (existing != null)
                {
                // Cho phép nộp lại nếu từng rút đơn
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

            var entity = new EmployerCandidatesList
                {
                EmployerPostId = employerPostId,
                JobSeekerId = jobSeekerId,
                ApplicationDate = DateTime.Now,
                Status = "Pending",
                Notes = note,
                UpdatedAt = DateTime.Now
                };

            await _repo.AddAsync(entity);
            return true;
            }

        // =========================================================
        // ỨNG VIÊN RÚT ĐƠN
        // =========================================================
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

        // =========================================================
        // EMPLOYER XEM DANH SÁCH ỨNG VIÊN CỦA BÀI ĐĂNG
        // =========================================================
        public async Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId)
            {
            var list = await _repo.GetByEmployerPostWithDetailAsync(employerPostId);

            return list.Select(x =>
            {
                var profile = x.JobSeeker.JobSeekerProfile;

                return new JobApplicationResultDto
                    {
                    CandidateListId = x.CandidateListId,
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
                    ApplicationDate = x.ApplicationDate,
                    Notes = x.Notes
                    };
            }).ToList();
            }

        // =========================================================
        // JOBSEEKER XEM DANH SÁCH BÀI ĐÃ ỨNG TUYỂN
        // =========================================================
        public async Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId)
            {
            var list = await _repo.GetByJobSeekerWithPostDetailAsync(jobSeekerId);

            return list.Select(x => new JobApplicationResultDto
                {
                CandidateListId = x.CandidateListId,
                JobSeekerId = x.JobSeekerId,
                Username = x.JobSeeker.Username,
                Status = x.Status,
                ApplicationDate = x.ApplicationDate,
                Notes = x.Notes
                });
            }

        // =========================================================
        // EMPLOYER CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // =========================================================
        public async Task<bool> UpdateStatusAsync(int candidateListId, string status, string? note = null)
            {
            var entity = await _repo.GetByIdAsync(candidateListId);
            if (entity == null)
                return false;

            entity.Status = status;
            entity.Notes = note;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);
            return true;
            }
        }
    }

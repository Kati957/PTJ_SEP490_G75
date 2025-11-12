using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
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
        private readonly JobMatchingDbContext _db;

        public JobApplicationService(IJobApplicationRepository repo, JobMatchingDbContext db)
            {
            _repo = repo;
            _db = db;
            }

        // =========================================================
        // ỨNG VIÊN NỘP ĐƠN (có validation)
        // =========================================================
        public async Task<(bool success, string? error)> ApplyAsync(int jobSeekerId, int employerPostId, string? note, int? cvid = null)
            {
            // 1️⃣ Kiểm tra user hợp lệ
            var seeker = await _db.Users.FirstOrDefaultAsync(u => u.UserId == jobSeekerId);
            if (seeker == null || !seeker.IsActive)
                return (false, "Tài khoản ứng viên không tồn tại hoặc đã bị khóa.");

            // 2️⃣ Kiểm tra bài đăng hợp lệ
            var post = await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == employerPostId);
            if (post == null)
                return (false, "Bài đăng không tồn tại.");
            if (post.Status == "Deleted" || post.Status == "Closed")
                return (false, "Bài đăng đã đóng tuyển.");

            // 3️⃣ Kiểm tra CV hợp lệ (nếu có)
            if (cvid.HasValue)
                {
                var cv = await _db.JobSeekerCvs.FirstOrDefaultAsync(c => c.Cvid == cvid && c.JobSeekerId == jobSeekerId);
                if (cv == null)
                    return (false, "CV không hợp lệ hoặc không thuộc về bạn.");
                }

            // 4️⃣ Kiểm tra đã ứng tuyển chưa
            var existing = await _repo.GetAsync(jobSeekerId, employerPostId);
            if (existing != null)
                {
                if (existing.Status == "Withdrawn")
                    {
                    existing.Status = "Pending";
                    existing.Notes = note;
                    existing.Cvid = cvid;
                    existing.UpdatedAt = DateTime.Now;
                    await _repo.UpdateAsync(existing);
                    return (true, null);
                    }
                return (false, "Bạn đã ứng tuyển bài này trước đó.");
                }

            // 5️⃣ Tạo đơn ứng tuyển mới
            var submission = new JobSeekerSubmission
                {
                JobSeekerId = jobSeekerId,
                EmployerPostId = employerPostId,
                AppliedAt = DateTime.Now,
                Status = "Pending",
                Notes = note,
                Cvid = cvid,
                UpdatedAt = DateTime.Now
                };

            await _repo.AddAsync(submission);
            return (true, null);
            }


        // =========================================================
        // RÚT ĐƠN
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
        // EMPLOYER XEM DANH SÁCH ỨNG VIÊN
        // =========================================================
        public async Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId)
            {
            var list = await _repo.GetByEmployerPostWithDetailAsync(employerPostId);

            return list.Select(x =>
            {
                var cv = x.Cv;  // ✅ Lấy thông tin CV
                var seeker = x.JobSeeker;

                return new JobApplicationResultDto
                    {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = seeker.Username,
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,

                    // ✅ CV chi tiết
                    CvId = cv?.Cvid,
                    CvTitle = cv?.Cvtitle,
                    SkillSummary = cv?.SkillSummary,
                    Skills = cv?.Skills,
                    PreferredJobType = cv?.PreferredJobType,
                    PreferredLocation = cv?.PreferredLocation,
                    ContactPhone = cv?.ContactPhone
                    };
            }).ToList();
            }


        // =========================================================
        // JOBSEEKER XEM CÁC BÀI ĐÃ ỨNG TUYỂN
        // =========================================================
        public async Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId)
            {
            var list = await _repo.GetByJobSeekerWithPostDetailAsync(jobSeekerId);

            return list.Select(x =>
            {
                var post = x.EmployerPost;
                var category = post?.Category;
                var employer = post?.User;
                var cv = x.Cv; // ✅ Thêm dòng này

                return new JobApplicationResultDto
                    {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker?.Username ?? "Unknown",
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,

                    // Thông tin bài đăng
                    EmployerPostId = post?.EmployerPostId ?? 0,
                    PostTitle = post?.Title,
                    CategoryName = category?.Name,
                    EmployerName = employer?.Username,
                    Location = post?.Location,
                    Salary = post?.Salary,
                    WorkHours = post?.WorkHours,
                    PhoneContact = post?.PhoneContact,

                    // ✅ Thông tin CV đã dùng
                    CvId = cv?.Cvid,
                    CvTitle = cv?.Cvtitle,
                    SkillSummary = cv?.SkillSummary,
                    Skills = cv?.Skills,
                    PreferredJobType = cv?.PreferredJobType,
                    PreferredLocation = cv?.PreferredLocation,
                    ContactPhone = cv?.ContactPhone
                    };
            }).ToList();
            }


        // =========================================================
        // EMPLOYER CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // =========================================================
        public async Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null)
            {
            var validStatuses = new[] { "Accepted", "Rejected" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ chấp nhận 'Accepted' hoặc 'Rejected'.");

            var entity = await _repo.GetByIdAsync(submissionId);
            if (entity == null)
                throw new Exception("Không tìm thấy đơn ứng tuyển.");
            if (entity.Status == "Withdrawn")
                throw new Exception("Không thể cập nhật đơn đã bị rút.");

            entity.Status = status.Trim();
            entity.Notes = note;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);
            return true;
            }
        }
    }

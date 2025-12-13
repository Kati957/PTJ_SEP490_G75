using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;
using PTJ_Service.JobApplicationService.Interfaces;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.JobApplicationService.Implementations
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly INotificationService _noti;
        private readonly IEmailSender _email;                 
        private readonly IEmailTemplateService _templates;    

        public JobApplicationService(
            IJobApplicationRepository repo,
            JobMatchingDbContext db,
            INotificationService noti,
            IEmailSender email,
            IEmailTemplateService templates)
        {
            _repo = repo;
            _db = db;
            _noti = noti;
            _email = email;
            _templates = templates;
        }

        // 1. ỨNG TUYỂN BÀI ĐĂNG

        public async Task<(bool success, string? error)> ApplyAsync(
            int jobSeekerId, int employerPostId, string? note, int? cvid = null)
        {
            var seeker = await _db.Users.FirstOrDefaultAsync(u => u.UserId == jobSeekerId);
            if (seeker == null || !seeker.IsActive)
                return (false, "Tài khoản ứng viên không tồn tại hoặc đã bị khóa.");

            var post = await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == employerPostId);
            if (post == null)
                return (false, "Bài đăng không tồn tại.");
            if (post.Status == "Deleted" || post.Status == "Inactive")
                return (false, "Bài đăng đã được đóng tuyển.");

            var employer = await _db.Users.FirstAsync(u => u.UserId == post.UserId);
            if (!employer.IsActive)
                return (false, "Nhà tuyển dụng đã bị khóa.");

            if (cvid.HasValue)
            {
                var cv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == cvid && c.JobSeekerId == jobSeekerId);
                if (cv == null)
                    return (false, "CV không hợp lệ hoặc không thuộc về bạn.");
            }

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

            var seekerName = await _db.JobSeekerProfiles
                .Where(x => x.UserId == jobSeekerId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync() ?? "Ứng viên";

            // Gửi thông báo cho employer
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = employer.UserId,
                NotificationType = "JobApplication",
                RelatedItemId = submission.SubmissionId,
                Data = new() { { "JobSeekerName", seekerName }, { "PostTitle", post.Title } }
            });

            // Gửi thông báo cho job seeker
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = jobSeekerId,
                NotificationType = "JobAppliedSuccess",
                RelatedItemId = submission.SubmissionId,
                Data = new() { { "PostTitle", post.Title } }
            });

            return (true, null);
        }

        // 2. RÚT ĐƠN
        public async Task<bool> WithdrawAsync(int jobSeekerId, int employerPostId)
        {
            var app = await _repo.GetAsync(jobSeekerId, employerPostId);
            if (app == null) return false;

            app.Status = "Withdrawn";
            app.Notes = "Ứng viên đã rút đơn";
            app.UpdatedAt = DateTime.Now;
            await _repo.UpdateAsync(app);

            return true;
        }

        // 3. EMPLOYER XEM DANH SÁCH ỨNG VIÊN
        public async Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId)
        {
           
            var list = await _repo.GetByEmployerPostWithDetailAsync(employerPostId);

            return list.Select(x =>
            {
                var cv = x.Cv;

                var seekerFullName = _db.JobSeekerProfiles
                    .Where(p => p.UserId == x.JobSeeker.UserId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? "Ứng viên";

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = seekerFullName,
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,
                    CvId = cv?.Cvid,
                    CvTitle = cv?.Cvtitle,
                    SkillSummary = cv?.SkillSummary,
                    Skills = cv?.Skills,
                    PreferredJobType = cv?.PreferredJobType,
                    PreferredLocation = cv?.PreferredLocation,
                    ContactPhone = cv?.ContactPhone,
                    EmployerId = x.EmployerPost?.UserId ?? 0
                };
            }).ToList();
        }
        // 4. JOBSEEKER XEM LỊCH SỬ ỨNG TUYỂN
        public async Task<IEnumerable<JobApplicationResultDto>> GetApplicationsBySeekerAsync(int jobSeekerId)
        {
          
            var list = await _repo.GetByJobSeekerWithPostDetailAsync(jobSeekerId);

            list = list.Where(x =>
                x.EmployerPost != null &&
                x.EmployerPost.Status == "Active" &&
                x.EmployerPost.User.IsActive == true
            ).ToList();

            return list.Select(x =>
            {
                var post = x.EmployerPost;
                var employer = post?.User;
                var cv = x.Cv;

                var jsFullName = _db.JobSeekerProfiles
                    .Where(js => js.UserId == x.JobSeekerId)
                    .Select(js => js.FullName)
                    .FirstOrDefault() ?? "Ứng viên";

                var employerName = _db.EmployerProfiles
                    .Where(e => e.UserId == employer.UserId)
                    .Select(e => e.DisplayName)
                    .FirstOrDefault() ?? "Nhà tuyển dụng";

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = jsFullName,
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,
                    EmployerPostId = post?.EmployerPostId ?? 0,
                    PostTitle = post?.Title,
                    CategoryName = post?.Category?.Name,
                    EmployerName = employerName,
                    Location = post?.Location,
                    SalaryMin = post?.SalaryMin,
                    SalaryMax = post?.SalaryMax,
                    SalaryType = post?.SalaryType,
                    SalaryDisplay =
                        (post?.SalaryMin == null && post?.SalaryMax == null)
                            ? "Thỏa thuận"
                            : (post?.SalaryMin != null && post?.SalaryMax != null)
                                ? $"{post.SalaryMin:#,###} - {post.SalaryMax:#,###}"
                                : post?.SalaryMin != null
                                    ? $"Từ {post?.SalaryMin:#,###}"
                                    : $"Đến {post?.SalaryMax:#,###}",
                    WorkHours = post?.WorkHours,
                    PhoneContact = post?.PhoneContact,
                    CvId = cv?.Cvid,
                    CvTitle = cv?.Cvtitle,
                    SkillSummary = cv?.SkillSummary,
                    Skills = cv?.Skills,
                    PreferredJobType = cv?.PreferredJobType,
                    PreferredLocation = cv?.PreferredLocation,
                    ContactPhone = cv?.ContactPhone,
                    EmployerId = employer?.UserId ?? 0
                };
            }).ToList();
        }
        // 5. EMPLOYER CẬP NHẬT TRẠNG THÁI ĐƠN
        public async Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null)
        {
            var allowedStatuses = new[] { "Interviewing", "Accepted", "Rejected" };
            if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            var entity = await _repo.GetByIdAsync(submissionId);
            if (entity == null)
                throw new Exception("Không tìm thấy đơn ứng tuyển.");

            var newStatus = status.Trim();

            // FINAL STATE → KHÔNG ĐƯỢC ĐỔI
            var finalStates = new[] { "Accepted", "Rejected", "Withdrawn" };
            if (finalStates.Contains(entity.Status, StringComparer.OrdinalIgnoreCase))
                throw new Exception($"Đơn đã chốt ({entity.Status}), không thể thay đổi.");

            // KIỂM SOÁT LUỒNG
            bool isValidTransition = entity.Status switch
            {
                "Pending" =>
                    new[] { "Interviewing", "Accepted", "Rejected" }
                        .Contains(newStatus, StringComparer.OrdinalIgnoreCase),

                "Interviewing" =>
                    new[] { "Accepted", "Rejected" }
                        .Contains(newStatus, StringComparer.OrdinalIgnoreCase),

                _ => false
            };

            if (!isValidTransition)
                throw new Exception($"Không thể chuyển từ {entity.Status} sang {newStatus}.");

            entity.Status = newStatus;
            entity.Notes = note;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);

            var seeker = await _db.Users.FirstAsync(u => u.UserId == entity.JobSeekerId);
            var seekerName = await _db.JobSeekerProfiles
                .Where(x => x.UserId == seeker.UserId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync() ?? seeker.Email;

            var post = await _db.EmployerPosts.FirstAsync(p => p.EmployerPostId == entity.EmployerPostId);
            var employerName = await _db.EmployerProfiles
                .Where(x => x.UserId == post.UserId)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync() ?? "Nhà tuyển dụng";

            // GỬI THÔNG BÁO + EMAIL TEMPLATE

            if (status == "Interviewing")
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = seeker.UserId,
                    NotificationType = "InterviewRequest",
                    RelatedItemId = submissionId,
                    Data = new() { { "PostTitle", post.Title } }
                });

                // Gửi email mời phỏng vấn
                var html = _templates.CreateInterviewInviteTemplate(seekerName, post.Title, employerName, note ?? "");
                await _email.SendEmailAsync(seeker.Email, $"Mời phỏng vấn – {post.Title}", html);

                return true;
            }

            if (status == "Accepted")
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = seeker.UserId,
                    NotificationType = "ApplicationAccepted",
                    RelatedItemId = submissionId,
                    Data = new() { { "PostTitle", post.Title } }
                });

                // Email ACCEPTED
                var html = _templates.CreateApplicationAcceptedTemplate(seekerName, post.Title, employerName);
                await _email.SendEmailAsync(seeker.Email, $"Bạn đã được nhận – {post.Title}", html);

                return true;
            }

            if (status == "Rejected")
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = seeker.UserId,
                    NotificationType = "ApplicationRejected",
                    RelatedItemId = submissionId,
                    Data = new() { { "PostTitle", post.Title } }
                });

                // Email REJECTED
                var html = _templates.CreateApplicationRejectedTemplate(seekerName, post.Title);
                await _email.SendEmailAsync(seeker.Email, $"Kết quả ứng tuyển – {post.Title}", html);

                return true;
            }

            return true;
        }
        public async Task<ApplicationSummaryDto> GetApplicationSummaryAsync(int userId, bool isAdmin)
        {
            int? employerId = isAdmin ? null : userId;
            return await _repo.GetFullSummaryAsync(employerId);
        }
    }
}

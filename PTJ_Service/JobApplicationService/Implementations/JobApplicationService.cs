using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.ApplicationDTO;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;
using PTJ_Service.JobApplicationService.Interfaces;

namespace PTJ_Service.JobApplicationService.Implementations
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly INotificationService _noti;   

        public JobApplicationService(
            IJobApplicationRepository repo,
            JobMatchingDbContext db,
            INotificationService noti) 
        {
            _repo = repo;
            _db = db;
            _noti = noti;
        }

       
        // 1️⃣ ỨNG VIÊN NỘP ĐƠN (APPLY)

        public async Task<(bool success, string? error)> ApplyAsync(
            int jobSeekerId,
            int employerPostId,
            string? note,
            int? cvid = null)
        {
            // 1. Kiểm tra user hợp lệ
            var seeker = await _db.Users.FirstOrDefaultAsync(u => u.UserId == jobSeekerId);
            if (seeker == null || !seeker.IsActive)
                return (false, "Tài khoản ứng viên không tồn tại hoặc đã bị khóa.");

            // 2. Kiểm tra bài đăng hợp lệ
            var post = await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == employerPostId);
            if (post == null)
                return (false, "Bài đăng không tồn tại.");
            if (post.Status == "Deleted" || post.Status == "Inactive")
                return (false, "Bài đăng đã được đóng tuyển.");

            var employer = await _db.Users.FirstAsync(u => u.UserId == post.UserId);
            if (!employer.IsActive)
                return (false, "Nhà tuyển dụng đã bị khóa, bài đăng này không khả dụng.");

            // Lấy thông tin Employer
            var employerId = post.UserId;

            // 3. Kiểm tra CV hợp lệ
            if (cvid.HasValue)
            {
                var cv = await _db.JobSeekerCvs.FirstOrDefaultAsync(c => c.Cvid == cvid && c.JobSeekerId == jobSeekerId);
                if (cv == null)
                    return (false, "CV không hợp lệ hoặc không thuộc về bạn.");
            }

            // 4. Kiểm tra đã ứng tuyển chưa
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

            // 5. Tạo đơn ứng tuyển mới
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

          
            //   GỬI THÔNG BÁO REALTIME CHO EMPLOYER
            
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = employerId,
                NotificationType = "JobApplication",
                RelatedItemId = submission.SubmissionId,
                Data = new Dictionary<string, string>
                {
                    { "Name", seeker.Username ?? "Ứng viên" },
                    { "PostTitle", post.Title }
                }
            });

            // GỬI THÔNG BÁO CHO JOB SEEKER (MỚI THÊM)
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = jobSeekerId,
                NotificationType = "JobAppliedSuccess",
                RelatedItemId = submission.SubmissionId,
                Data = new Dictionary<string, string>
    {
        { "PostTitle", post.Title }
    }
            });

            return (true, null);
        }


    
        // 2️⃣ RÚT ĐƠN
       
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


 
        // 3️⃣ EMPLOYER XEM DANH SÁCH ỨNG VIÊN
       
        public async Task<IEnumerable<JobApplicationResultDto>> GetCandidatesByPostAsync(int employerPostId)
        {
            var list = await _repo.GetByEmployerPostWithDetailAsync(employerPostId);

            return list.Select(x =>
            {
                var cv = x.Cv;
                var seeker = x.JobSeeker;

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = seeker.Username,
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
                    EmployerId = x.EmployerPost?.UserId ?? 0 // ✅ Added EmployerId
                };
            }).ToList();
        }


       
        // 4️⃣ JOBSEEKER XEM CÁC ĐƠN ỨNG TUYỂN CỦA MÌNH
        
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
                var category = post?.Category;
                var employer = post?.User;
                var cv = x.Cv;

                return new JobApplicationResultDto
                {
                    CandidateListId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker?.Username ?? "Unknown",
                    Status = x.Status,
                    ApplicationDate = x.AppliedAt,
                    Notes = x.Notes,
                    EmployerPostId = post?.EmployerPostId ?? 0,
                    PostTitle = post?.Title,
                    CategoryName = category?.Name,
                    EmployerName = employer?.Username,
                    Location = post?.Location,
                    Salary = post?.Salary,
                    WorkHours = post?.WorkHours,
                    PhoneContact = post?.PhoneContact,
                    CvId = cv?.Cvid,
                    CvTitle = cv?.Cvtitle,
                    SkillSummary = cv?.SkillSummary,
                    Skills = cv?.Skills,
                    PreferredJobType = cv?.PreferredJobType,
                    PreferredLocation = cv?.PreferredLocation,
                    ContactPhone = cv?.ContactPhone,
                    EmployerId = employer?.UserId ?? 0 // ✅ Added EmployerId
                };
            }).ToList();
        }



        // 5️⃣ EMPLOYER ACCEPT / REJECT APPLICATION

        public async Task<bool> UpdateStatusAsync(int submissionId, string status, string? note = null)
            {
            // ⭐ Step 1: Thêm Interviewing vào danh sách hợp lệ
            var validStatuses = new[] { "Interviewing", "Accepted", "Rejected" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            var entity = await _repo.GetByIdAsync(submissionId);
            if (entity == null)
                throw new Exception("Không tìm thấy đơn ứng tuyển.");

            if (entity.Status == "Withdrawn")
                throw new Exception("Không thể cập nhật đơn đã rút.");

            // ⭐ Step 2: Cập nhật trạng thái
            entity.Status = status.Trim();
            entity.Notes = note;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);

            // Lấy thông tin JS và bài post
            var seeker = await _db.Users.FirstAsync(u => u.UserId == entity.JobSeekerId);
            var post = await _db.EmployerPosts.FirstAsync(p => p.EmployerPostId == entity.EmployerPostId);

            // ⭐ Step 3: Gửi thông báo tùy theo trạng thái
            if (status.Equals("Interviewing", StringComparison.OrdinalIgnoreCase))
                {
                await _noti.SendAsync(new CreateNotificationDto
                    {
                    UserId = seeker.UserId,
                    NotificationType = "InterviewRequest",
                    RelatedItemId = submissionId,
                    Data = new()
            {
                { "PostTitle", post.Title }
            }
                    });

                return true;
                }

            if (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                await _noti.SendAsync(new CreateNotificationDto
                    {
                    UserId = seeker.UserId,
                    NotificationType = "ApplicationAccepted",
                    RelatedItemId = submissionId,
                    Data = new()
            {
                { "PostTitle", post.Title }
            }
                    });
                }
            else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                await _noti.SendAsync(new CreateNotificationDto
                    {
                    UserId = seeker.UserId,
                    NotificationType = "ApplicationRejected",
                    RelatedItemId = submissionId,
                    Data = new()
            {
                { "PostTitle", post.Title }
            }
                    });
                }

            return true;
            }

        public async Task<ApplicationSummaryDto> GetApplicationSummaryAsync(int userId, bool isAdmin)
            {
            // Nếu admin → xem toàn hệ thống → employerId = null
            // Nếu employer → employerId = userId
            int? employerId = isAdmin ? null : userId;
            return await _repo.GetFullSummaryAsync(employerId);
            }

        }
    }

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Notification;
using PTJ_Models.DTO.Report;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repo;
        private readonly INotificationService _noti;   // 🔔 ADD NOTIFICATION SERVICE

        public ReportService(IReportRepository repo, INotificationService noti)
        {
            _repo = repo;
            _noti = noti;
        }

        // -------------------------------------------------------------
        // 🔥 1. REPORT EMPLOYER POST
        // -------------------------------------------------------------
        public async Task<int> ReportEmployerPostAsync(int reporterId, CreateEmployerPostReportDto dto)
        {
            if (dto.EmployerPostId <= 0)
                throw new ArgumentException("Invalid EmployerPostId");

            // Validate tồn tại
            if (!await _repo.EmployerPostExistsAsync(dto.EmployerPostId))
                throw new KeyNotFoundException("Employer post not found.");

            // Chống spam report lặp trong 10 phút
            if (await _repo.HasRecentDuplicateAsync(reporterId, "EmployerPost", dto.EmployerPostId, withinMinutes: 10))
                throw new InvalidOperationException("You already reported this post recently.");

            var report = new PostReport
            {
                ReporterId = reporterId,
                ReportType = "EmployerPost",
                ReportedItemId = dto.EmployerPostId,
                EmployerPostId = dto.EmployerPostId,
                JobSeekerPostId = null,
                TargetUserId = null,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            await _repo.SaveChangesAsync();

            // Lấy tiêu đề bài đăng để hiển thị Notification
            var postTitle = await _repo.GetEmployerPostTitleAsync(dto.EmployerPostId);

            // 🔔 SEND NOTIFICATION TO ADMIN
            var adminId = await _repo.GetAdminUserIdAsync();
            if (adminId > 0)
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = adminId,
                    NotificationType = "ReportCreated",
                    RelatedItemId = report.PostReportId,
                    Data = new()
                    {
                        { "PostTitle", postTitle ?? "Unknown Post" }
                    }
                });
            }

            return report.PostReportId;
        }

        // -------------------------------------------------------------
        // 🔥 2. REPORT JOBSEEKER POST
        // -------------------------------------------------------------
        public async Task<int> ReportJobSeekerPostAsync(int reporterId, CreateJobSeekerPostReportDto dto)
        {
            if (dto.JobSeekerPostId <= 0)
                throw new ArgumentException("Invalid JobSeekerPostId");

            if (!await _repo.JobSeekerPostExistsAsync(dto.JobSeekerPostId))
                throw new KeyNotFoundException("Job seeker post not found.");

            if (await _repo.HasRecentDuplicateAsync(reporterId, "JobSeekerPost", dto.JobSeekerPostId, withinMinutes: 10))
                throw new InvalidOperationException("You already reported this post recently.");

            var report = new PostReport
            {
                ReporterId = reporterId,
                ReportType = "JobSeekerPost",
                ReportedItemId = dto.JobSeekerPostId,
                EmployerPostId = null,
                JobSeekerPostId = dto.JobSeekerPostId,
                TargetUserId = null,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            await _repo.SaveChangesAsync();

            // Lấy tiêu đề bài đăng
            var postTitle = await _repo.GetJobSeekerPostTitleAsync(dto.JobSeekerPostId);

            // 🔔 SEND NOTIFICATION TO ADMIN
            var adminId = await _repo.GetAdminUserIdAsync();
            if (adminId > 0)
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = adminId,
                    NotificationType = "ReportCreated",
                    RelatedItemId = report.PostReportId,
                    Data = new()
                    {
                        { "PostTitle", postTitle ?? "Unknown Post" }
                    }
                });
            }

            return report.PostReportId;
        }

        // -------------------------------------------------------------
        // 3️⃣ GET MY REPORTS
        // -------------------------------------------------------------
        public Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
            => _repo.GetMyReportsAsync(reporterId);
    }
}

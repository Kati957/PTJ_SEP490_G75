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
                    throw new ArgumentException("EmployerPostId không hợp lệ.");

                // Validate tồn tại
                if (!await _repo.EmployerPostExistsAsync(dto.EmployerPostId))
                    throw new KeyNotFoundException("Không tìm thấy bài đăng của nhà tuyển dụng.");

                // Chống spam report lặp trong 10 phút
                if (await _repo.HasRecentDuplicateAsync(reporterId, "EmployerPost", dto.EmployerPostId, withinMinutes: 10))
                    throw new InvalidOperationException("Bạn đã báo cáo bài đăng này gần đây.");

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

                // Lấy tiêu đề bài đăng để gửi Notification
                var postTitle = await _repo.GetEmployerPostTitleAsync(dto.EmployerPostId);

                // 🔔 GỬI NOTIFICATION CHO ADMIN
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
                            { "PostTitle", postTitle ?? "Không xác định" }
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
                    throw new ArgumentException("JobSeekerPostId không hợp lệ.");

                if (!await _repo.JobSeekerPostExistsAsync(dto.JobSeekerPostId))
                    throw new KeyNotFoundException("Không tìm thấy bài đăng của người tìm việc.");

                if (await _repo.HasRecentDuplicateAsync(reporterId, "JobSeekerPost", dto.JobSeekerPostId, withinMinutes: 10))
                    throw new InvalidOperationException("Bạn đã báo cáo bài đăng này gần đây.");

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

                // 🔔 GỬI THÔNG BÁO CHO ADMIN
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
                            { "PostTitle", postTitle ?? "Không xác định" }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Interfaces.Admin;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Notification;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _repo;
        private readonly INotificationService _noti;

        private const string ACTION_BLOCK = "BlockPost";
        private const string ACTION_WARN = "Warn";
        private const string ACTION_IGNORE = "Ignore";

        public AdminReportService(IAdminReportRepository repo, INotificationService noti)
        {
            _repo = repo;
            _noti = noti;
        }

        public Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(string? reportType, string? keyword, int page, int pageSize)
            => _repo.GetPendingReportsPagedAsync(reportType, keyword, page, pageSize);

        public Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(string? adminEmail, string? reportType, int page, int pageSize)
            => _repo.GetSolvedReportsPagedAsync(adminEmail, reportType, page, pageSize);

        public Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
            => _repo.GetReportDetailAsync(reportId);

        // RESOLVE REPORT

        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new Exception("Không tìm thấy báo cáo.");

            if (report.Status != "Pending")
                throw new Exception("Báo cáo này đã xử lý.");

            // Ngăn xử lý trùng
            if (await _repo.GetSolvedReportByReportIdAsync(reportId) != null)
                throw new Exception("Báo cáo đã có bản ghi xử lý.");

            string action = Normalize(dto.ActionTaken);

            switch (action)
            {
                case ACTION_BLOCK:
                    await HandleBlockPostAsync(report, dto);
                    break;

                case ACTION_WARN:
                    await HandleWarnUserAsync(report, dto);
                    break;

                case ACTION_IGNORE:
                    await HandleIgnoreAsync(report);
                    break;
            }

            report.Status = "Solved";

            var solved = new PostReportSolved
            {
                PostReportId = report.PostReportId,
                AdminId = adminId,
                ActionTaken = action,
                Reason = dto.Reason,
                SolvedAt = DateTime.UtcNow,

                AffectedUserId = report.TargetUserId,
                AffectedPostId = report.AffectedPostId,
                AffectedPostType = report.AffectedPostType
            };

            await _repo.AddSolvedReportAsync(solved);
            await _repo.SaveChangesAsync();

            return await _repo.GetSolvedReportDetailAsync(solved.SolvedPostReportId)
                ?? throw new Exception("Không lấy được thông tin solved.");
        }

        private string Normalize(string a)
        {
            return a.Trim() switch
            {
                ACTION_BLOCK => ACTION_BLOCK,
                ACTION_WARN => ACTION_WARN,
                ACTION_IGNORE => ACTION_IGNORE,
                _ => throw new Exception("Action không hợp lệ.")
            };
        }

        // BLOCK POST
        private async Task HandleBlockPostAsync(PostReport report, AdminResolveReportDto dto)
        {
            if (report.AffectedPostId == null || report.AffectedPostType == null)
                throw new Exception("Report không chứa thông tin bài đăng.");

            if (report.AffectedPostType == "EmployerPost")
            {
                var post = await _repo.GetEmployerPostByIdAsync(report.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Blocked";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = post.UserId,
                        NotificationType = "PostBlocked",
                        RelatedItemId = post.EmployerPostId,
                        Data = { { "Reason", dto.Reason ?? "" } }
                    });
                }
            }
            else if (report.AffectedPostType == "JobSeekerPost")
            {
                var post = await _repo.GetJobSeekerPostByIdAsync(report.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Blocked";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = post.UserId,
                        NotificationType = "PostBlocked",
                        RelatedItemId = post.JobSeekerPostId,
                        Data = { { "Reason", dto.Reason ?? "" } }
                    });
                }
            }
        }

        // HIDE POST
        private async Task HandleHidePostAsync(PostReport report, AdminResolveReportDto dto)
        {
            if (report.AffectedPostId == null || report.AffectedPostType == null)
                throw new Exception("Report không chứa thông tin bài đăng.");

            if (report.AffectedPostType == "EmployerPost")
            {
                var post = await _repo.GetEmployerPostByIdAsync(report.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Hidden";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = post.UserId,
                        NotificationType = "PostHidden",
                        RelatedItemId = post.EmployerPostId,
                        Data = { { "Reason", dto.Reason ?? "" } }
                    });
                }
            }
            else if (report.AffectedPostType == "JobSeekerPost")
            {
                var post = await _repo.GetJobSeekerPostByIdAsync(report.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Hidden";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = post.UserId,
                        NotificationType = "PostHidden",
                        RelatedItemId = post.JobSeekerPostId,
                        Data = { { "Reason", dto.Reason ?? "" } }
                    });
                }
            }
        }

        private async Task HandleWarnUserAsync(PostReport report, AdminResolveReportDto dto)
        {
            if (report.TargetUserId == null)
                throw new Exception("Không có TargetUserId.");

            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = report.TargetUserId.Value,
                NotificationType = "UserWarned",
                RelatedItemId = report.PostReportId,
                Data = { { "Reason", dto.Reason ?? "" } }
            });
        }

        private async Task HandleIgnoreAsync(PostReport report)
        {
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = report.ReporterId,
                NotificationType = "ReportIgnored",
                RelatedItemId = report.PostReportId,
                Data = { { "Message", "Report không hợp lệ." } }
            });
        }
    }

}

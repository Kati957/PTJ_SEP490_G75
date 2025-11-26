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

        public AdminReportService(IAdminReportRepository repo, INotificationService noti)
        {
            _repo = repo;
            _noti = noti;
        }


        // Lấy list report pending

        public Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
            => _repo.GetPendingReportsPagedAsync(reportType, keyword, page, pageSize);



        // Lấy list report đã xử lý

        public Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10)
            => _repo.GetSolvedReportsPagedAsync(adminEmail, reportType, page, pageSize);



        //  Lấy chi tiết report

        public Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
            => _repo.GetReportDetailAsync(reportId);



        // Xử lý report

        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Không tìm thấy báo cáo.");

            if (report.Status != "Pending")
                throw new InvalidOperationException("Báo cáo này đã được xử lý trước đó.");

            // Lấy thông tin bài đăng từ unified fields
            int? postId = report.AffectedPostId;
            string? postType = report.AffectedPostType;

            switch (dto.ActionTaken)
            {
                case "DeletePost":
                    await HandleDeletePostAsync(report, dto);
                    break;

                case "Warn":
                    await HandleWarnUserAsync(report, dto);
                    break;

                case "Ignore":
                    await HandleIgnoreAsync(report);
                    break;

                default:
                    throw new InvalidOperationException("Hành động xử lý không hợp lệ.");
            }

            // Cập nhật trạng thái report
            report.Status = "Resolved";

            var solved = new PostReportSolved
            {
                PostReportId = report.PostReportId,
                AdminId = adminId,

                AffectedUserId = report.TargetUserId ?? report.ReporterId,
                AffectedPostId = report.AffectedPostId,
                AffectedPostType = report.AffectedPostType,

                ActionTaken = dto.ActionTaken,
                Reason = dto.Reason,

                SolvedAt = DateTime.UtcNow
            };

            await _repo.AddSolvedReportAsync(solved);
            await _repo.SaveChangesAsync();

            // Lấy dữ liệu solved đầy đủ từ repository
            var solvedPaged = await _repo.GetSolvedReportsPagedAsync(null, null, 1, 1);
            var fullData = solvedPaged.Data.FirstOrDefault(x => x.SolvedReportId == solved.SolvedPostReportId);

            return fullData ?? new AdminSolvedReportDto
            {
                SolvedReportId = solved.SolvedPostReportId,
                ReportId = solved.PostReportId,
                ActionTaken = solved.ActionTaken,
                Reason = solved.Reason,
                SolvedAt = solved.SolvedAt,
                PostId = solved.AffectedPostId,
                PostType = solved.AffectedPostType
            };
        }

        //  Xử lý DeletePost

        private async Task HandleDeletePostAsync(PostReport report, AdminResolveReportDto dto)
        {
            if (report.AffectedPostId == null || string.IsNullOrEmpty(report.AffectedPostType))
                throw new InvalidOperationException("Thiếu thông tin bài đăng để xử lý.");

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
                        NotificationType = "PostRemoved",
                        RelatedItemId = post.EmployerPostId,
                        Data = new()
                        {
                            { "PostTitle", post.Title },
                            { "Reason", dto.Reason ?? "" }
                        }
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
                        NotificationType = "PostRemoved",
                        RelatedItemId = post.JobSeekerPostId,
                        Data = new()
                        {
                            { "PostTitle", post.Title },
                            { "Reason", dto.Reason ?? "" }
                        }
                    });
                }
            }
        }


        //  Warn user

        private async Task HandleWarnUserAsync(PostReport report, AdminResolveReportDto dto)
        {
            int userId = report.TargetUserId ?? report.ReporterId;

            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationType = "UserWarned",
                RelatedItemId = report.PostReportId,
                Data = new()
                {
                    { "Reason", dto.Reason ?? "Không có lý do cụ thể." }
                }
            });
        }

        //  Ignore

        private async Task HandleIgnoreAsync(PostReport report)
        {
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = report.ReporterId,
                NotificationType = "ReportIgnored",
                RelatedItemId = report.PostReportId,
                Data = new()
                {
                    { "Reason", "Báo cáo đã được xem xét và không phát hiện vấn đề." }
                }
            });
        }
    }
}

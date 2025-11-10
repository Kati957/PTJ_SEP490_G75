using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.Interfaces.Admin;
using PTJ_Service.Interfaces; 

namespace PTJ_Service.Implementations.Admin
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

        // 1️⃣ Danh sách report chưa xử lý
        public Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null, string? keyword = null, int page = 1, int pageSize = 10)
            => _repo.GetPendingReportsPagedAsync(reportType, keyword, page, pageSize);

        // 2️⃣ Danh sách report đã xử lý
        public Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null, string? reportType = null, int page = 1, int pageSize = 10)
            => _repo.GetSolvedReportsPagedAsync(adminEmail, reportType, page, pageSize);

        // 3️⃣ Chi tiết report
        public Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
            => _repo.GetReportDetailAsync(reportId);

        // 4️⃣ Xử lý report
        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Report not found.");

            if (report.Status != "Pending")
                throw new InvalidOperationException("This report has already been processed.");

            switch (dto.ActionTaken)
            {
                case "DeletePost":
                    await HandleDeletePostAsync(dto, report);
                    break;

                case "Warn":
                    await HandleWarnAsync(dto, report);
                    break;

                case "Ignore":
                    await HandleIgnoreAsync(report);
                    break;

                default:
                    throw new InvalidOperationException("Invalid action type.");
            }

            report.Status = "Resolved";

            var solved = new PostReportSolved
            {
                PostReportId = report.PostReportId,
                AdminId = adminId,
                AffectedUserId = report.TargetUserId,
                AffectedPostId = dto.AffectedPostId,
                AffectedPostType = dto.AffectedPostType,
                ActionTaken = dto.ActionTaken,
                Reason = dto.Reason,
                SolvedAt = DateTime.UtcNow
            };

            await _repo.AddSolvedReportAsync(solved);
            await _repo.SaveChangesAsync();

            return new AdminSolvedReportDto
            {
                SolvedReportId = solved.SolvedPostReportId,
                ReportId = solved.PostReportId,
                ActionTaken = solved.ActionTaken,
                Reason = solved.Reason,
                SolvedAt = solved.SolvedAt
            };
        }

        //  Ẩn bài đăng (DeletePost → Hidden)
        private async Task HandleDeletePostAsync(AdminResolveReportDto dto, PostReport report)
        {
            if (dto.AffectedPostId == null || string.IsNullOrEmpty(dto.AffectedPostType))
                throw new InvalidOperationException("Missing post information.");

            if (dto.AffectedPostType == "EmployerPost")
            {
                var post = await _repo.GetEmployerPostByIdAsync(dto.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Hidden";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendNotificationAsync(
                        post.UserId,
                        "Bài đăng bị ẩn",
                        "Bài đăng của bạn đã bị ẩn do vi phạm chính sách nội dung.",
                        "PostHidden",
                        post.EmployerPostId
                    );
                }
            }
            else if (dto.AffectedPostType == "JobSeekerPost")
            {
                var post = await _repo.GetJobSeekerPostByIdAsync(dto.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Hidden";
                    post.UpdatedAt = DateTime.UtcNow;

                    await _noti.SendNotificationAsync(
                        post.UserId,
                        "Bài viết bị ẩn",
                        "Bài viết giới thiệu bản thân của bạn đã bị ẩn do vi phạm quy định.",
                        "PostHidden",
                        post.JobSeekerPostId
                    );
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid post type.");
            }
        }

        //  Gửi cảnh báo
        private async Task HandleWarnAsync(AdminResolveReportDto dto, PostReport report)
        {
            int? targetUserId = report.TargetUserId ?? report.ReporterId;
            if (targetUserId.HasValue)
            {
                await _noti.SendNotificationAsync(
                    targetUserId.Value,
                    "Cảnh báo nội dung bài đăng",
                    "Bài đăng của bạn bị cảnh báo, vui lòng chỉnh sửa nội dung để tránh vi phạm.",
                    "PostWarning",
                    dto.AffectedPostId
                );
            }
        }

        //  Bỏ qua report
        private async Task HandleIgnoreAsync(PostReport report)
        {
            await _noti.SendNotificationAsync(
                report.ReporterId,
                "Báo cáo đã được xem xét",
                "Báo cáo của bạn đã được xem xét, không phát hiện vi phạm.",
                "ReportIgnored",
                report.PostReportId
            );
        }
    }
}

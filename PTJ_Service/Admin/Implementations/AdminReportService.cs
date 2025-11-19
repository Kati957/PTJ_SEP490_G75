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

        // 4️⃣ XỬ LÝ REPORT + GỬI NOTIFICATION

        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Không tìm thấy báo cáo.");

            if (report.Status != "Pending")
                throw new InvalidOperationException("Báo cáo đã được xử lý trước đó.");

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
                    throw new InvalidOperationException("Không thể thực hiện hành động này.");
            }

            // Cập nhật trạng thái report
            report.Status = "Resolved";

            // Lưu record solved
            var solved = new PostReportSolved
            {
                PostReportId = report.PostReportId,
                AdminId = adminId,
                AffectedUserId = report.TargetUserId ?? report.ReporterId,
                AffectedPostId = dto.AffectedPostId,
                AffectedPostType = dto.AffectedPostType,
                ActionTaken = dto.ActionTaken,
                Reason = dto.Reason,
                SolvedAt = DateTime.UtcNow
            };

            await _repo.AddSolvedReportAsync(solved);
            await _repo.SaveChangesAsync();

            // LẤY LẠI DỮ LIỆU ĐẦY ĐỦ SAU KHI SAVE (JOIN POST + ADMIN + USER)
            var solvedPaged = await _repo.GetSolvedReportsPagedAsync(null, null, 1, 1);

            var fullData = solvedPaged.Data.FirstOrDefault(s => s.SolvedReportId == solved.SolvedPostReportId);

            if (fullData == null)
            {
                // fallback nếu hiếm khi join không trả về
                return new AdminSolvedReportDto
                {
                    SolvedReportId = solved.SolvedPostReportId,
                    ReportId = solved.PostReportId,
                    ActionTaken = solved.ActionTaken,
                    Reason = solved.Reason,
                    SolvedAt = solved.SolvedAt
                };
            }

            return fullData;
        }



        // CASE 1️⃣: GỠ HOẶC ẨN BÀI ĐĂNG

        private async Task HandleDeletePostAsync(AdminResolveReportDto dto, PostReport report)
        {
            if (dto.AffectedPostId == null || dto.AffectedPostType == null)
                throw new InvalidOperationException("Thiếu thông tin bài đăng.");

            if (dto.AffectedPostType == "EmployerPost")
            {
                var post = await _repo.GetEmployerPostByIdAsync(dto.AffectedPostId.Value);
                if (post != null)
                {
                    post.Status = "Hidden";
                    post.UpdatedAt = DateTime.UtcNow;

                    // GỬI THÔNG BÁO
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
            else if (dto.AffectedPostType == "JobSeekerPost")
            {
                var post = await _repo.GetJobSeekerPostByIdAsync(dto.AffectedPostId.Value);
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


        // CASE 2️⃣: CẢNH CÁO USER

        private async Task HandleWarnAsync(AdminResolveReportDto dto, PostReport report)
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


        // CASE 3️⃣: BỎ QUA REPORT

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

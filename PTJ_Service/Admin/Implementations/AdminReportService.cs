using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.Interfaces.Admin;

namespace PTJ_Service.Implementations.Admin
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _repo;
        public AdminReportService(IAdminReportRepository repo) => _repo = repo;

        // 1️⃣ Danh sách report chưa xử lý
        public Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null, string? keyword = null, int page = 1, int pageSize = 10)
            => _repo.GetPendingReportsPagedAsync(reportType, keyword, page, pageSize);

        // 2️⃣ Danh sách report đã xử lý
        public Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null, string? reportType = null, int page = 1, int pageSize = 10)
            => _repo.GetSolvedReportsPagedAsync(adminEmail, reportType, page, pageSize);

        // 3️⃣ Chi tiết report (View Report Detail)
        public Task<AdminReportDetailDto?> GetReportDetailAsync(int reportId)
            => _repo.GetReportDetailAsync(reportId);

        // 4️⃣ Xử lý report
        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Report not found.");

            if (report.Status != "Pending")
                throw new InvalidOperationException("Report has already been processed.");

            switch (dto.ActionTaken)
            {
                case "BanUser":
                    if (report.TargetUserId == null)
                        throw new InvalidOperationException("No target user to ban.");
                    var userBan = await _repo.GetUserByIdAsync(report.TargetUserId.Value)
                        ?? throw new KeyNotFoundException("User not found.");
                    userBan.IsActive = false;
                    userBan.UpdatedAt = DateTime.UtcNow;
                    break;

                case "UnbanUser":
                    if (report.TargetUserId == null)
                        throw new InvalidOperationException("No target user to unban.");
                    var userUnban = await _repo.GetUserByIdAsync(report.TargetUserId.Value)
                        ?? throw new KeyNotFoundException("User not found.");
                    userUnban.IsActive = true;
                    userUnban.UpdatedAt = DateTime.UtcNow;
                    break;

                case "DeletePost":
                    if (dto.AffectedPostId == null || string.IsNullOrEmpty(dto.AffectedPostType))
                        throw new InvalidOperationException("Missing post information.");

                    if (dto.AffectedPostType == "EmployerPost")
                    {
                        var ep = await _repo.GetEmployerPostByIdAsync(dto.AffectedPostId.Value);
                        if (ep != null)
                        {
                            ep.Status = "Deleted";
                            ep.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else if (dto.AffectedPostType == "JobSeekerPost")
                    {
                        var jp = await _repo.GetJobSeekerPostByIdAsync(dto.AffectedPostId.Value);
                        if (jp != null)
                        {
                            jp.Status = "Deleted";
                            jp.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else throw new InvalidOperationException("Invalid post type.");
                    break;

                case "Warn":
                case "Ignore":
                    // Không thay đổi dữ liệu, chỉ ghi log xử lý
                    break;

                default:
                    throw new InvalidOperationException("Invalid action type.");
            }

            // Cập nhật trạng thái report
            report.Status = "Resolved";

            // Ghi log xử lý vào PostReportSolved
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
    }
}

using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.Interfaces.Admin;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;

namespace PTJ_Service.Implementations.Admin
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _repo;
        private readonly JobMatchingDbContext _db;

        public AdminReportService(IAdminReportRepository repo, JobMatchingDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        // 1️⃣ Danh sách report pending (phân trang)
        public async Task<PagedResult<AdminReportDto>> GetPendingReportsAsync(
            string? reportType = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10)
        {
            var allReports = await _repo.GetPendingReportsAsync(reportType, keyword);
            var total = allReports.Count();

            var pagedData = allReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AdminReportDto>(pagedData, total, page, pageSize);
        }

        // 2️⃣ Danh sách report đã xử lý (phân trang)
        public async Task<PagedResult<AdminSolvedReportDto>> GetSolvedReportsAsync(
            string? adminEmail = null,
            string? reportType = null,
            int page = 1,
            int pageSize = 10)
        {
            var allReports = await _repo.GetSolvedReportsAsync(adminEmail);

            if (!string.IsNullOrWhiteSpace(reportType))
                allReports = allReports.Where(r => r.ReportType == reportType);

            var total = allReports.Count();
            var pagedData = allReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AdminSolvedReportDto>(pagedData, total, page, pageSize);
        }

        // 3️⃣ Xử lý report
        public async Task<AdminSolvedReportDto> ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            var report = await _repo.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Report not found.");

            if (report.Status != "Pending")
                throw new InvalidOperationException("Report already processed.");

            // Xử lý hành động (Ban, Delete, Warn, ...)
            await HandleActionAsync(report, dto);

            // Cập nhật trạng thái
            report.Status = "Solved";

            // Ghi log xử lý
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
            await transaction.CommitAsync();

            return new AdminSolvedReportDto
            {
                ReportId = reportId,
                ActionTaken = dto.ActionTaken,
                AdminEmail = (await _repo.GetUserByIdAsync(adminId))?.Email ?? "",
                TargetUserEmail = report.TargetUser?.Email,
                Reason = dto.Reason,
                SolvedAt = solved.SolvedAt,
                ReportType = report.ReportType,
                ReportReason = report.Reason
            };
        }

        //  Helper xử lý hành động
        private async Task HandleActionAsync(PostReport report, AdminResolveReportDto dto)
        {
            switch (dto.ActionTaken)
            {
                case "BanUser":
                    var userBan = await _repo.GetUserByIdAsync(report.TargetUserId ?? 0)
                        ?? throw new KeyNotFoundException("User not found.");
                    userBan.IsActive = false;
                    break;

                case "UnbanUser":
                    var userUnban = await _repo.GetUserByIdAsync(report.TargetUserId ?? 0)
                        ?? throw new KeyNotFoundException("User not found.");
                    userUnban.IsActive = true;
                    break;

                case "DeletePost":
                    if (dto.AffectedPostType == "EmployerPost")
                    {
                        var ep = await _repo.GetEmployerPostByIdAsync(dto.AffectedPostId ?? 0);
                        if (ep != null) ep.Status = "Deleted";
                    }
                    else if (dto.AffectedPostType == "JobSeekerPost")
                    {
                        var jp = await _repo.GetJobSeekerPostByIdAsync(dto.AffectedPostId ?? 0);
                        if (jp != null) jp.Status = "Deleted";
                    }
                    break;

                case "Warn":
                case "Ignore":
                    // chỉ ghi log, không thay đổi data
                    break;

                default:
                    throw new InvalidOperationException("Invalid action type.");
            }
        }
    }
}

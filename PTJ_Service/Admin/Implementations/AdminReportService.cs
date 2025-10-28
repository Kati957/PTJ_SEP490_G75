using PTJ_Data.Repo.Interface;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Interface;

namespace PTJ_Service.Implement
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _repo;
        public AdminReportService(IAdminReportRepository repo) => _repo = repo;

        public Task<IEnumerable<object>> GetPendingReportsAsync() => _repo.GetPendingReportsAsync();
        public Task<IEnumerable<object>> GetSolvedReportsAsync() => _repo.GetSolvedReportsAsync();

        public async Task ResolveReportAsync(int reportId, AdminResolveReportDto dto, int adminId)
        {
            var report = await _repo.GetReportByIdAsync(reportId);
            if (report == null) throw new KeyNotFoundException("Report not found");
            if (report.Status != "Pending") throw new InvalidOperationException("Report already processed");

            switch (dto.ActionTaken)
            {
                case "BanUser":
                    if (report.TargetUserId == null) throw new InvalidOperationException("No target user to ban.");
                    var userBan = await _repo.GetUserByIdAsync(report.TargetUserId.Value);
                    if (userBan == null) throw new KeyNotFoundException("User not found.");
                    userBan.IsActive = false;
                    userBan.UpdatedAt = DateTime.UtcNow;
                    break;

                case "UnbanUser":
                    if (report.TargetUserId == null) throw new InvalidOperationException("No target user to unban.");
                    var userUnban = await _repo.GetUserByIdAsync(report.TargetUserId.Value);
                    if (userUnban == null) throw new KeyNotFoundException("User not found.");
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
                    // Không thay đổi dữ liệu, chỉ log
                    break;

                default:
                    throw new InvalidOperationException("Invalid action.");
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
        }
    }
}

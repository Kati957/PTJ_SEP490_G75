using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.Admin;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly JobMatchingDbContext _db;

        public AdminController(JobMatchingDbContext db)
        {
            _db = db;
        }

        // ✅ 1️⃣ Lấy danh sách các report chưa xử lý
        [HttpGet("reports/pending")]
        public async Task<IActionResult> GetPendingReports()
        {
            var reports = await _db.PostReports
                .Include(r => r.Reporter)
                .Where(r => r.Status == "Pending")
                .Select(r => new
                {
                    r.PostReportId,
                    r.ReportType,
                    Reporter = new
                    {
                        r.Reporter.UserId,
                        r.Reporter.Email
                    },
                    TargetUser = r.TargetUser != null
                        ? new { r.TargetUser.UserId, r.TargetUser.Email }
                        : null,
                    r.Reason,
                    r.Status,
                    r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reports);
        }

        // ✅ 2️⃣ Xử lý report (Ban / Unban / DeletePost / Warn / Ignore)
        [HttpPost("reports/resolve/{reportId}")]
        public async Task<IActionResult> ResolveReport(int reportId, [FromBody] AdminResolveReportDto dto)
        {
            // Lấy ID admin từ token
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

            var report = await _db.PostReports
                .FirstOrDefaultAsync(r => r.PostReportId == reportId);

            if (report == null)
                return NotFound(new { message = "Report not found." });

            if (report.Status != "Pending")
                return BadRequest(new { message = "Report already processed." });

            // ✅ Bắt đầu xử lý theo loại hành động
            switch (dto.ActionTaken)
            {
                case "BanUser":
                    if (report.TargetUserId == null)
                        return BadRequest(new { message = "This report is not for a user." });

                    var userToBan = await _db.Users.FindAsync(report.TargetUserId);
                    if (userToBan == null) return NotFound("User not found.");

                    userToBan.IsActive = false;
                    userToBan.UpdatedAt = DateTime.UtcNow;
                    break;

                case "UnbanUser":
                    if (report.TargetUserId == null)
                        return BadRequest(new { message = "This report is not for a user." });

                    var userToUnban = await _db.Users.FindAsync(report.TargetUserId);
                    if (userToUnban == null) return NotFound("User not found.");

                    userToUnban.IsActive = true;
                    userToUnban.UpdatedAt = DateTime.UtcNow;
                    break;

                case "DeletePost":
                    if (dto.AffectedPostId == null || string.IsNullOrEmpty(dto.AffectedPostType))
                        return BadRequest(new { message = "Missing post information (AffectedPostId or AffectedPostType)." });

                    if (dto.AffectedPostType == "EmployerPost")
                    {
                        var post = await _db.EmployerPosts.FindAsync(dto.AffectedPostId);
                        if (post != null)
                        {
                            post.Status = "Deleted";
                            post.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else if (dto.AffectedPostType == "JobSeekerPost")
                    {
                        var post = await _db.JobSeekerPosts.FindAsync(dto.AffectedPostId);
                        if (post != null)
                        {
                            post.Status = "Deleted";
                            post.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else return BadRequest(new { message = "Invalid AffectedPostType." });
                    break;

                case "Warn":
                case "Ignore":
                    // Không thay đổi dữ liệu, chỉ log lại
                    break;

                default:
                    return BadRequest(new { message = "Invalid ActionTaken value." });
            }

            // ✅ Cập nhật trạng thái report
            report.Status = "Resolved";

            // ✅ Ghi log xử lý vào bảng PostReport_Solved
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

            _db.PostReportSolveds.Add(solved);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Report {reportId} resolved with action '{dto.ActionTaken}'.",
                reportId,
                dto.ActionTaken
            });
        }

        // ✅ 3️⃣ Lấy danh sách report đã xử lý
        [HttpGet("reports/solved")]
        public async Task<IActionResult> GetSolvedReports()
        {
            var solvedReports = await _db.PostReportSolveds
                .Include(s => s.Admin)
                .Include(s => s.AffectedUser)
                .Include(s => s.PostReport)
                .Select(s => new
                {
                    s.SolvedPostReportId,
                    s.PostReportId,
                    s.ActionTaken,
                    Admin = new { s.Admin.UserId, s.Admin.Email },
                    AffectedUser = s.AffectedUser != null ? new { s.AffectedUser.UserId, s.AffectedUser.Email } : null,
                    Report = new
                    {
                        s.PostReport.ReportType,
                        s.PostReport.Reason,
                        s.PostReport.CreatedAt
                    },
                    s.Reason,
                    s.SolvedAt
                })
                .OrderByDescending(s => s.SolvedAt)
                .ToListAsync();

            return Ok(solvedReports);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using System.Security.Claims;
using PTJ_Models.DTO.Admin;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ✅ chỉ Admin có thể truy cập
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
                    r.ReportedItemId,
                    Reporter = new
                    {
                        r.Reporter.UserId,
                        r.Reporter.Email
                    },
                    r.Reason,
                    r.Status,
                    r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reports);
        }

        // ✅ 2️⃣ Xử lý report (ban user / bỏ qua / xóa bài)
        [HttpPost("reports/resolve/{reportId}")]
        public async Task<IActionResult> ResolveReport(int reportId, [FromBody] AdminResolveReportDto dto)
        {
            // Lấy thông tin Admin từ token
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

            var report = await _db.PostReports.FirstOrDefaultAsync(r => r.PostReportId == reportId);
            if (report == null)
                return NotFound(new { message = "Report not found." });

            if (report.Status != "Pending")
                return BadRequest(new { message = "This report has already been processed." });

            // Kiểm tra người bị ảnh hưởng
            var affectedUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.AffectedUserId);
            if (affectedUser == null)
                return NotFound(new { message = "Affected user not found." });

            // ✅ Xử lý hành động
            switch (dto.ActionTaken)
            {
                case "BanUser":
                    affectedUser.IsActive = false;
                    affectedUser.UpdatedAt = DateTime.UtcNow;
                    break;
                case "UnbanUser":
                    affectedUser.IsActive = true;
                    affectedUser.UpdatedAt = DateTime.UtcNow;
                    break;
                case "Ignore":
                    // Không thay đổi trạng thái user
                    break;
                case "DeletePost":
                    // Nếu bạn có bảng Posts thì xóa hoặc update tại đây
                    break;
                default:
                    return BadRequest(new { message = "Invalid action. Valid values: BanUser, UnbanUser, Ignore, DeletePost." });
            }

            // ✅ Cập nhật trạng thái báo cáo
            report.Status = "Resolved";

            // ✅ Ghi log xử lý vào PostReport_Solved
            var solved = new PostReportSolved
            {
                PostReportId = report.PostReportId,
                AdminId = adminId,
                AffectedUserId = dto.AffectedUserId,
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
                dto.AffectedUserId,
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
                    Action = s.ActionTaken,
                    Admin = new { s.Admin.UserId, s.Admin.Email },
                    AffectedUser = new { s.AffectedUser.UserId, s.AffectedUser.Email },
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

    // 🧩 DTO định nghĩa cho request xử lý report
   
}

using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class ReportService : IReportService
    {
        private readonly JobMatchingOpenAiDbContext _db;
        private readonly IReportRepository _repo;
        private readonly INotificationService _noti;

        public ReportService(
            JobMatchingOpenAiDbContext db,
            IReportRepository repo,
            INotificationService noti)
        {
            _db = db;
            _repo = repo;
            _noti = noti;
        }

        public async Task<int> ReportPostAsync(int reporterId, CreatePostReportDto dto)
        {
            if (dto == null)
                throw new Exception("Dữ liệu không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.ReportType))
                throw new Exception("Loại báo cáo không được để trống.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new Exception("Lý do báo cáo không được để trống.");

            string affectedPostType = dto.AffectedPostType;
            int? targetUserId = null;
            if (string.IsNullOrWhiteSpace(affectedPostType))
            {
                var employerPost = await _db.EmployerPosts
                    .Where(x => x.EmployerPostId == dto.PostId)
                    .Select(x => new { x.EmployerPostId, x.UserId })
                    .FirstOrDefaultAsync();

                if (employerPost != null)
                {
                    affectedPostType = "EmployerPost";
                    targetUserId = employerPost.UserId;
                }
                else
                {
                    var seekerPost = await _db.JobSeekerPosts
                        .Where(x => x.JobSeekerPostId == dto.PostId)
                        .Select(x => new { x.JobSeekerPostId, x.UserId })
                        .FirstOrDefaultAsync();

                    if (seekerPost != null)
                    {
                        affectedPostType = "JobSeekerPost";
                        targetUserId = seekerPost.UserId;
                    }
                }

                if (string.IsNullOrWhiteSpace(affectedPostType))
                    throw new Exception("Không tìm thấy bài đăng.");
            }
            else
            {
                if (affectedPostType != "EmployerPost" && affectedPostType != "JobSeekerPost")
                    throw new Exception("AffectedPostType không hợp lệ.");

                if (affectedPostType == "EmployerPost")
                {
                    targetUserId = await _db.EmployerPosts
                        .Where(x => x.EmployerPostId == dto.PostId)
                        .Select(x => x.UserId)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    targetUserId = await _db.JobSeekerPosts
                        .Where(x => x.JobSeekerPostId == dto.PostId)
                        .Select(x => x.UserId)
                        .FirstOrDefaultAsync();
                }

                if (targetUserId == null || targetUserId == 0)
                    throw new Exception("PostId không hợp lệ với loại bài đăng đã chọn.");
            }
            var report = new PostReport
            {
                ReporterId = reporterId,
                AffectedPostId = dto.PostId,
                AffectedPostType = affectedPostType,
                TargetUserId = targetUserId,
                ReportType = dto.ReportType.Trim(),
                Reason = dto.Reason.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.PostReports.Add(report);
            await _db.SaveChangesAsync();

            return report.PostReportId; 
        }

        public Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
            => _repo.GetMyReportsAsync(reporterId);
    }
}

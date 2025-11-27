using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repo;
        private readonly INotificationService _noti;

        public ReportService(IReportRepository repo, INotificationService noti)
        {
            _repo = repo;
            _noti = noti;
        }


        // 1) REPORT ANY POST (UNIFIED)

        public async Task<int> ReportPostAsync(int reporterId, CreatePostReportDto dto)
        {
            if (dto.PostId <= 0)
                throw new ArgumentException("PostId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.PostType))
                throw new ArgumentException("PostType không hợp lệ.");

            // PostType phải hợp lệ
            if (dto.PostType != "EmployerPost" && dto.PostType != "JobSeekerPost")
                throw new ArgumentException("PostType phải là EmployerPost hoặc JobSeekerPost.");

            // 1️⃣ Kiểm tra tồn tại
            bool exists = dto.PostType == "EmployerPost"
                ? await _repo.EmployerPostExistsAsync(dto.PostId)
                : await _repo.JobSeekerPostExistsAsync(dto.PostId);

            if (!exists)
                throw new KeyNotFoundException("Không tìm thấy bài đăng.");

            // 2️⃣ Chống spam báo cáo
            if (await _repo.HasRecentDuplicateAsync(reporterId, dto.PostType, dto.PostId, 10))
                throw new InvalidOperationException("Bạn đã báo cáo bài đăng này gần đây.");

            // 3️⃣ Tạo report
            var report = new PostReport
            {
                ReporterId = reporterId,
                ReportType = dto.PostType,      

                AffectedPostId = dto.PostId,
                AffectedPostType = dto.PostType,

                TargetUserId = null,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            await _repo.SaveChangesAsync();

            // 4️⃣ Lấy tiêu đề bài đăng
            string? postTitle = dto.PostType == "EmployerPost"
                ? await _repo.GetEmployerPostTitleAsync(dto.PostId)
                : await _repo.GetJobSeekerPostTitleAsync(dto.PostId);

            // 5️⃣ Gửi notification cho ADMIN
            int adminId = await _repo.GetAdminUserIdAsync();

            if (adminId > 0)
            {
                await _noti.SendAsync(new CreateNotificationDto
                {
                    UserId = adminId,
                    NotificationType = "ReportCreated",
                    RelatedItemId = report.PostReportId,
                    Data = new()
                    {
                        { "PostTitle", postTitle ?? "Không xác định" }
                    }
                });
            }

            return report.PostReportId;
        }

        // 2) GET MY REPORTS

        public Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
            => _repo.GetMyReportsAsync(reporterId);
    }
}

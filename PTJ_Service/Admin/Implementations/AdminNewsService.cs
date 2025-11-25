using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.ImageService;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Notification;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminNewsService : IAdminNewsService
    {
        private readonly IAdminNewsRepository _repo;
        private readonly IImageService _img;
        private readonly JobMatchingDbContext _db;
        private readonly INotificationService _noti;

        public AdminNewsService(
            IAdminNewsRepository repo,
            IImageService img,
            JobMatchingDbContext db,
            INotificationService noti)
        {
            _repo = repo;
            _img = img;
            _db = db;
            _noti = noti;
        }

        //  Danh sách
        public Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(bool? isPublished, string? keyword)
            => _repo.GetAllNewsAsync(isPublished, keyword);

        //  Chi tiết
        public Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id)
            => _repo.GetNewsDetailAsync(id);

        //  Tạo mới
        public async Task<int> CreateAsync(int adminId, AdminCreateNewsDto dto)
        {
            var entity = new News
            {
                AdminId = adminId,
                Title = dto.Title,
                Content = dto.Content,
                Category = dto.Category,
                IsFeatured = dto.IsFeatured,
                Priority = dto.Priority,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublished = dto.IsPublished,
                IsDeleted = false
            };

            //  Upload ảnh (nếu có)
            if (dto.CoverImage != null)
            {
                var (url, _) = await _img.UploadImageAsync(dto.CoverImage, "News");
                entity.ImageUrl = url;
            }

            var newsId = await _repo.CreateAsync(entity);

            // 🔔 Nếu news được publish ngay -> gửi notification
            if (dto.IsPublished)
            {
                // Lấy danh sách user cần nhận tin (JobSeeker + Employer, đang hoạt động)
                var targetUsers = await _db.Users
                    .Include(u => u.Roles)
                    .Where(u => u.IsActive &&
                                u.Roles.Any(r => r.RoleName == "JobSeeker" || r.RoleName == "Employer"))
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var userId in targetUsers)
                {
                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = userId,
                        NotificationType = "NewsPublished",
                        RelatedItemId = newsId,
                        Data = new()
                        {
                            { "Title", entity.Title }
                        }
                    });
                }
            }

            return newsId;
        }

        //  Cập nhật
        public async Task UpdateAsync(AdminUpdateNewsDto dto)
        {
            var detail = await _repo.GetNewsDetailAsync(dto.NewsId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin tức.");

            var entity = new News
            {
                NewsId = dto.NewsId,
                Title = dto.Title,
                Content = dto.Content,
                Category = dto.Category,
                IsFeatured = dto.IsFeatured,
                Priority = dto.Priority,
                ImageUrl = detail.ImageUrl,
                UpdatedAt = DateTime.UtcNow,
                IsPublished = dto.IsPublished ?? detail.IsPublished,
                IsDeleted = false
            };

            if (dto.CoverImage != null)
            {
                var (url, _) = await _img.UploadImageAsync(dto.CoverImage, "News");
                entity.ImageUrl = url;
            }

            await _repo.UpdateAsync(entity);
        }

        //  Publish / Unpublish
        public async Task TogglePublishStatusAsync(int id)
        {
            var success = await _repo.TogglePublishStatusAsync(id);
            if (!success)
                throw new KeyNotFoundException("Không tìm thấy tin tức hoặc tin tức đã bị xóa.");

            // (Nếu sau này bạn muốn gửi noti khi chuyển từ Unpublish -> Publish
            //  có thể load News tại đây và gửi giống CreateAsync)
        }

        //  Xóa mềm
        public async Task DeleteAsync(int id)
        {
            var success = await _repo.SoftDeleteAsync(id);
            if (!success)
                throw new KeyNotFoundException("Không tìm thấy tin tức hoặc tin tức đã bị xóa.");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.ImageService;
using PTJ_Service.Interfaces;



namespace PTJ_Service.Admin.Implementations
{
    public class AdminNewsService : IAdminNewsService
    {
        private readonly IAdminNewsRepository _repo;
        private readonly IImageService _img;
        private readonly INotificationService _noti;
        private readonly JobMatchingDbContext _db;


        public AdminNewsService(
     IAdminNewsRepository repo,
     IImageService img,
     INotificationService noti,
     JobMatchingDbContext db)
        {
            _repo = repo;
            _img = img;
            _noti = noti;
            _db = db;
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

            return await _repo.CreateAsync(entity);
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
        //  Publish / Unpublish + Notification
        public async Task TogglePublishStatusAsync(int id)
        {
            // 1️⃣ Lấy tin tức
            var news = await _db.News.FirstOrDefaultAsync(n => n.NewsId == id);
            if (news == null || news.IsDeleted)
                throw new KeyNotFoundException("Không tìm thấy tin tức hoặc tin tức đã bị xóa.");

            bool wasPublished = news.IsPublished;

            // 2️⃣ Toggle trạng thái
            news.IsPublished = !news.IsPublished;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Nếu chuyển sang unpublish → không bắn noti
            if (!news.IsPublished)
                return;

            // 3️⃣ Chỉ gửi noti khi publish = true và trước đó là false
            if (!wasPublished && news.IsPublished)
            {
                // 4️⃣ Lấy tất cả user đang active
                var users = await _db.Users
                    .Where(u => u.IsActive)
                    .Select(u => u.UserId)
                    .ToListAsync();

                string shortDesc = news.Content.Length > 80
                    ? news.Content.Substring(0, 80) + "..."
                    : news.Content;

                // 5️⃣ Gửi Notification cho tất cả user
                foreach (var uid in users)
                {
                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = uid,
                        NotificationType = "NewsPublished",
                        RelatedItemId = news.NewsId,
                        Data = new()
                {
                    { "Title", news.Title },
                    { "ShortDescription", shortDesc }
                }
                    });
                }
            }
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

using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminNewsRepository : IAdminNewsRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminNewsRepository(JobMatchingDbContext db) => _db = db;

        public async Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(bool? isPublished = null, string? keyword = null)
        {
            var q = _db.News.Include(n => n.Admin).Where(n => !n.IsDeleted);

            if (isPublished.HasValue)
                q = q.Where(n => n.IsPublished == isPublished.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));

            return await q.OrderByDescending(n => n.CreatedAt)
                          .Select(n => new AdminNewsDto
                          {
                              NewsId = n.NewsId,
                              Title = n.Title,
                              Category = n.Category,
                              IsPublished = n.IsPublished,
                              CreatedAt = n.CreatedAt,
                              UpdatedAt = n.UpdatedAt
                          })
                          .ToListAsync();
        }

        public async Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id)
        {
            return await _db.News.Include(n => n.Admin)
                .Where(n => n.NewsId == id && !n.IsDeleted)
                .Select(n => new AdminNewsDetailDto
                {
                    NewsId = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    ImageUrl = n.ImageUrl,
                    Category = n.Category,
                    IsFeatured = n.IsFeatured,
                    Priority = n.Priority,
                    IsPublished = n.IsPublished,
                    AdminId = n.AdminId,
                    AdminEmail = n.Admin.Email,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(News entity)
        {
            _db.News.Add(entity);
            await _db.SaveChangesAsync();
            return entity.NewsId;
        }

        public async Task<bool> UpdateAsync(News entity)
        {
            _db.News.Update(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> TogglePublishStatusAsync(int id)
        {
            var news = await _db.News.FirstOrDefaultAsync(n => n.NewsId == id && !n.IsDeleted);
            if (news == null)
                return false;

            news.IsPublished = !news.IsPublished;
            news.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var news = await _db.News.FindAsync(id);
            if (news == null) return false;

            news.IsDeleted = true;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

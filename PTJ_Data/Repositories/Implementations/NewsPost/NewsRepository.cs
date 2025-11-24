using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.NewsPost;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.NewsPost
    {
    public class NewsRepository : INewsRepository
        {
        private readonly JobMatchingDbContext _db;

        public NewsRepository(JobMatchingDbContext db) => _db = db;

        public async Task<(List<News> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc)
            {
            var q = _db.News
                .Where(n => n.IsPublished && !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(n => n.Category == category);

            q = desc ? q.OrderByDescending(n => n.CreatedAt)
                     : q.OrderBy(n => n.CreatedAt);

            var total = await q.CountAsync();
            var data = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Load images manually
            foreach (var item in data)
                {
                item.Images = await _db.Images
                    .Where(i => i.EntityType == "News" && i.EntityId == item.NewsId)
                    .ToListAsync();
                }

            return (data, total);
            }

        public async Task<News?> GetByIdAsync(int id)
            {
            var news = await _db.News
                .FirstOrDefaultAsync(n => n.NewsId == id && n.IsPublished && !n.IsDeleted);

            if (news == null) return null;

            news.Images = await _db.Images
                .Where(i => i.EntityType == "News" && i.EntityId == news.NewsId)
                .ToListAsync();

            return news;
            }
        }
    }

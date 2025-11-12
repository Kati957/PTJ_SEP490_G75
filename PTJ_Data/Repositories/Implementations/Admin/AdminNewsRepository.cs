using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(string? status = null, string? keyword = null)
        {
            var q = _db.News.Include(n => n.Admin).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(n => n.Status == status);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                q = q.Where(n =>
                    n.Title.ToLower().Contains(kw) ||
                    n.Content != null && n.Content.ToLower().Contains(kw));
            }

            return await q
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new AdminNewsDto
                {
                    NewsId = n.NewsId,
                    Title = n.Title,
                    Category = n.Category,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id)
        {
            return await _db.News
                .Include(n => n.Admin)
                .Where(n => n.NewsId == id)
                .Select(n => new AdminNewsDetailDto
                {
                    NewsId = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    ImageUrl = n.ImageUrl,
                    Category = n.Category,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt,
                    AdminId = n.AdminId,
                    AdminEmail = n.Admin.Email
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(News entity)
        {
            _db.News.Add(entity);
            await _db.SaveChangesAsync();
            return entity.NewsId;
        }

        public async Task<bool> UpdateAsync(int id, AdminUpdateNewsDto dto)
        {
            var news = await _db.News.FirstOrDefaultAsync(n => n.NewsId == id);
            if (news == null) return false;

            news.Title = dto.Title;
            news.Content = dto.Content;
            news.ImageUrl = dto.ImageUrl;
            news.Category = dto.Category;
            news.Status = dto.Status ?? news.Status;
            news.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var news = await _db.News.FirstOrDefaultAsync(n => n.NewsId == id);
            if (news == null) return false;

            news.Status = news.Status == "Active" ? "Hidden" : "Active";
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;
using PTJ_Models;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Data.Repositories.Interfaces.NewsPost
{
    public class NewsRepository : INewsRepository
    {
        private readonly JobMatchingDbContext _db;
        public NewsRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(News news)
        {
            await _db.News.AddAsync(news);
            await _db.SaveChangesAsync();
        }

        public async Task<News?> GetByIdAsync(int id)
        {
            return await _db.News.Include(n => n.Images)
                                 .FirstOrDefaultAsync(n => n.NewsId == id);
        }

        public async Task<(List<News> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc)
        {
            var query = _db.News
                .Include(n => n.Images)
                .Where(n => n.Status == "Active")
                .AsQueryable();


            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(n => n.Category == category);

            query = sortBy switch
            {
                "Priority" => desc
                    ? query.OrderByDescending(n => n.IsFeatured)
                            .ThenByDescending(n => n.Priority)
                            .ThenByDescending(n => n.CreatedAt)
                    : query.OrderByDescending(n => n.IsFeatured)
                            .ThenBy(n => n.Priority)
                            .ThenBy(n => n.CreatedAt),

                "CreatedAt" => desc
                    ? query.OrderByDescending(n => n.IsFeatured)
                            .ThenByDescending(n => n.CreatedAt)
                    : query.OrderByDescending(n => n.IsFeatured)
                            .ThenBy(n => n.CreatedAt),

                "Title" => desc
                    ? query.OrderByDescending(n => n.IsFeatured)
                            .ThenByDescending(n => n.Title)
                            .ThenByDescending(n => n.CreatedAt)
                    : query.OrderByDescending(n => n.IsFeatured)
                            .ThenBy(n => n.Title)
                            .ThenByDescending(n => n.CreatedAt),

                _ => query.OrderByDescending(n => n.IsFeatured)
                          .ThenByDescending(n => n.Priority)
                          .ThenByDescending(n => n.CreatedAt)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task UpdateAsync(News news)
        {
            _db.News.Update(news);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(News news)
        {
            _db.News.Remove(news);
            await _db.SaveChangesAsync();
        }
    }
}

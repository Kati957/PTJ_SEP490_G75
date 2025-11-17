using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTJ_Models.DTO.News;
using PTJ_Models.Models;
using PTJ_Data;
using PTJ_Service.ImageService;
using PTJ_Data.Repositories.Interfaces.NewsPost;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Service.NewsService
{
    public class NewsService : INewsService
    {
        private readonly JobMatchingDbContext _db;
        public NewsService(JobMatchingDbContext db) => _db = db;

        public async Task<(List<NewsReadDto> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc)
        {
            var q = _db.News.Include(n => n.Images)
                            .Where(n => n.IsPublished && !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(n => n.Category == category);

            q = desc ? q.OrderByDescending(n => n.CreatedAt) : q.OrderBy(n => n.CreatedAt);

            var total = await q.CountAsync();
            var data = await q.Skip((page - 1) * pageSize).Take(pageSize)
                              .Select(n => new NewsReadDto
                              {
                                  NewsID = n.NewsId,
                                  Title = n.Title,
                                  Content = n.Content,
                                  ImageUrl = n.ImageUrl,
                                  GalleryUrls = n.Images.Select(i => i.Url).ToList(),
                                  Category = n.Category,
                                  IsFeatured = n.IsFeatured,
                                  Priority = n.Priority,
                                  CreatedAt = n.CreatedAt
                              })
                              .ToListAsync();

            return (data, total);
        }

        public async Task<NewsReadDto?> GetDetailAsync(int id)
        {
            return await _db.News.Include(n => n.Images)
                .Where(n => n.NewsId == id && n.IsPublished && !n.IsDeleted)
                .Select(n => new NewsReadDto
                {
                    NewsID = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    ImageUrl = n.ImageUrl,
                    GalleryUrls = n.Images.Select(i => i.Url).ToList(),
                    Category = n.Category,
                    CreatedAt = n.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}

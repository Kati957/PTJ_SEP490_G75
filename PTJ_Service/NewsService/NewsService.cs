using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTJ_Models.DTO.News;
using PTJ_Models.Models;
using PTJ_Data;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Service.NewsService
    {
    public class NewsService : INewsService
        {
        private readonly JobMatchingOpenAiDbContext _db;
        public NewsService(JobMatchingOpenAiDbContext db) => _db = db;



        // 1️⃣ LẤY DANH SÁCH NEWS (PHÂN TRANG + TÌM KIẾM)

        public async Task<(List<NewsReadDto> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc)
            {
            var q = _db.News
                .Where(n => n.IsPublished && !n.IsDeleted);

            // Filter search
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(n => n.Category == category);

            // Sorting
            q = desc
                ? q.OrderByDescending(n => n.CreatedAt)
                : q.OrderBy(n => n.CreatedAt);

            var total = await q.CountAsync();

            //  Lấy ảnh bằng Image table (Không dùng Include)
            var data = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsReadDto
                    {
                    NewsID = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    ImageUrl = n.ImageUrl, // ảnh đại diện
                    GalleryUrls = _db.Images
                        .Where(i => i.EntityType == "News" && i.EntityId == n.NewsId)
                        .Select(i => i.Url)
                        .ToList(),
                    Category = n.Category,
                    IsFeatured = n.IsFeatured,
                    Priority = n.Priority,
                    CreatedAt = n.CreatedAt
                    })
                .ToListAsync();

            return (data, total);
            }



        // 2️⃣ LẤY CHI TIẾT NEWS

        public async Task<NewsReadDto?> GetDetailAsync(int id)
            {
            return await _db.News
                .Where(n => n.NewsId == id && n.IsPublished && !n.IsDeleted)
                .Select(n => new NewsReadDto
                    {
                    NewsID = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    ImageUrl = n.ImageUrl,

                    //  Lấy tất cả ảnh của News
                    GalleryUrls = _db.Images
                        .Where(i => i.EntityType == "News" && i.EntityId == n.NewsId)
                        .Select(i => i.Url)
                        .ToList(),

                    Category = n.Category,
                    CreatedAt = n.CreatedAt
                    })
                .FirstOrDefaultAsync();
            }
        }
    }

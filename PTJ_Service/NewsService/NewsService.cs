using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTJ_Models.DTO.News;
using PTJ_Models.Models;
using PTJ_Models;
using PTJ_Service.ImageService;
using PTJ_Data.Repositories.Interfaces;

namespace PTJ_Service.NewsService
{
    public class NewsService : INewsService
    {
        private readonly INewsRepository _repo;
        private readonly IImageService _imageService;
        private readonly JobMatchingDbContext _context;

        public NewsService(INewsRepository repo, IImageService imageService, JobMatchingDbContext context)
        {
            _repo = repo;
            _imageService = imageService;
            _context = context;
        }

        // ✅ CREATE
        public async Task<News> CreateAsync(NewsCreateDto dto)
        {
            var news = new News
            {
                AdminId = dto.AdminID,
                Title = dto.Title,
                Content = dto.Content,
                Category = dto.Category,
                IsFeatured = dto.IsFeatured,
                Priority = dto.Priority,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
            };

            // Lưu trước để có NewsID
            await _repo.AddAsync(news);

            // Upload ảnh cover
            if (dto.CoverImage != null)
            {
                var (url, publicId) = await _imageService.UploadImageAsync(dto.CoverImage, "News");
                news.ImageUrl = url;

                var cover = new Image
                {
                    EntityType = "News",
                    EntityID = news.NewsId,
                    Url = url,
                    PublicId = publicId,
                    Format = "jpg"
                };
                _context.Images.Add(cover);
            }

            // Upload gallery ảnh
            if (dto.GalleryImages != null && dto.GalleryImages.Any())
            {
                foreach (var img in dto.GalleryImages)
                {
                    var (url, publicId) = await _imageService.UploadImageAsync(img, "News");
                    var image = new Image
                    {
                        EntityType = "News",
                        EntityID = news.NewsId,
                        Url = url,
                        PublicId = publicId,
                        Format = "jpg"
                    };
                    _context.Images.Add(image);
                }
            }

            await _context.SaveChangesAsync();
            return news;
        }

        // ✅ READ (phân trang, lọc, sắp xếp)
        public async Task<(List<NewsReadDto> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc)
        {
            var (data, total) = await _repo.GetPagedAsync(keyword, category, page, pageSize, sortBy, desc);

            var result = data.Select(n => new NewsReadDto
            {
                NewsID = n.NewsId,
                Title = n.Title,
                Content = n.Content,
                ImageUrl = n.ImageUrl,
                GalleryUrls = n.Images?.Select(i => i.Url).ToList(),
                Category = n.Category,
                IsFeatured = n.IsFeatured,
                Priority = n.Priority,
                CreatedAt = n.CreatedAt
            }).ToList();

            return (result, total);
        }

        // ✅ UPDATE
        public async Task<News?> UpdateAsync(NewsUpdateDto dto)
        {
            var news = await _repo.GetByIdAsync(dto.NewsID);
            if (news == null) return null;

            news.Title = dto.Title;
            news.Content = dto.Content;
            news.Category = dto.Category;
            news.IsFeatured = dto.IsFeatured;
            news.Priority = dto.Priority;
            news.UpdatedAt = DateTime.Now;

            // Cập nhật ảnh cover nếu có
            if (dto.CoverImage != null)
            {
                var (url, publicId) = await _imageService.UploadImageAsync(dto.CoverImage, "News");
                news.ImageUrl = url;

                var image = new Image
                {
                    EntityType = "News",
                    EntityID = news.NewsId,
                    Url = url,
                    PublicId = publicId,
                    Format = "jpg"
                };
                _context.Images.Add(image);
            }

            // Cập nhật gallery nếu có
            if (dto.GalleryImages != null && dto.GalleryImages.Any())
            {
                foreach (var img in dto.GalleryImages)
                {
                    var (url, publicId) = await _imageService.UploadImageAsync(img, "News");
                    var image = new Image
                    {
                        EntityType = "News",
                        EntityID = news.NewsId,
                        Url = url,
                        PublicId = publicId,
                        Format = "jpg"
                    };
                    _context.Images.Add(image);
                }
            }

            await _repo.UpdateAsync(news);
            await _context.SaveChangesAsync();
            return news;
        }

        // ✅ DELETE
        public async Task<bool> DeleteAsync(int newsId)
        {
            var news = await _repo.GetByIdAsync(newsId);
            if (news == null) return false;

            // Xóa ảnh trong Cloudinary + DB
            var images = _context.Images.Where(i => i.EntityType == "News" && i.EntityID == newsId).ToList();
            foreach (var img in images)
            {
                await _imageService.DeleteImageAsync(img.PublicId);
                _context.Images.Remove(img);
            }

            await _repo.DeleteAsync(news);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

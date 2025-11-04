using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.News;
using PTJ_Models.Models;

namespace PTJ_Service.NewsService
{
    public interface INewsService
    {
        Task<News> CreateAsync(NewsCreateDto dto);
        Task<(List<NewsReadDto> Data, int Total)> GetPagedAsync(string? keyword, string? category, int page, int pageSize, string sortBy, bool desc);
        Task<News?> UpdateAsync(NewsUpdateDto dto);
        Task<bool> DeleteAsync(int newsId, bool isHardDelete = false);
        Task<News?> ToggleStatusAsync(int newsId);
    }
}

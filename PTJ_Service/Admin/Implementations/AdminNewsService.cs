using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminNewsService : IAdminNewsService
    {
        private readonly IAdminNewsRepository _repo;
        public AdminNewsService(IAdminNewsRepository repo) => _repo = repo;

        public Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(string? status = null, string? keyword = null)
            => _repo.GetAllNewsAsync(status, keyword);

        public Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id)
            => _repo.GetNewsDetailAsync(id);

        public async Task<int> CreateAsync(int adminId, AdminCreateNewsDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required.");

            var entity = new News
            {
                Title = dto.Title.Trim(),
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                Category = dto.Category,
                Status = "Active",
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _repo.CreateAsync(entity);
        }

        public async Task UpdateAsync(int id, AdminUpdateNewsDto dto)
        {
            var ok = await _repo.UpdateAsync(id, dto);
            if (!ok) throw new KeyNotFoundException("News not found.");
        }

        public async Task ToggleActiveAsync(int id)
        {
            var ok = await _repo.ToggleActiveAsync(id);
            if (!ok) throw new KeyNotFoundException("News not found.");
        }
    }
}

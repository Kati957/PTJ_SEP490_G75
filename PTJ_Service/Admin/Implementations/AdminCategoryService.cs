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
    public class AdminCategoryService : IAdminCategoryService
    {
        private readonly IAdminCategoryRepository _repo;
        public AdminCategoryService(IAdminCategoryRepository repo) => _repo = repo;

        public Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync(string? type = null, bool? isActive = null, string? keyword = null)
            => _repo.GetCategoriesAsync(type, isActive, keyword);

        public Task<AdminCategoryDto?> GetCategoryAsync(int id)
            => _repo.GetCategoryAsync(id);

        public async Task<int> CreateAsync(AdminCreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Category name is required.");

            var entity = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Type = dto.Type,
                IsActive = dto.IsActive
            };

            return await _repo.CreateAsync(entity);
        }

        public async Task UpdateAsync(int id, AdminUpdateCategoryDto dto)
        {
            var ok = await _repo.UpdateAsync(id, dto);
            if (!ok) throw new KeyNotFoundException("Category not found.");
        }

        public async Task ToggleActiveAsync(int id)
        {
            var ok = await _repo.ToggleActiveAsync(id);
            if (!ok) throw new KeyNotFoundException("Category not found.");
        }
    }
}

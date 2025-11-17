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
    public class AdminCategoryRepository : IAdminCategoryRepository
    {
        private readonly JobMatchingDbContext _db;
        public AdminCategoryRepository(JobMatchingDbContext db) => _db = db;

        public async Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync(string? type = null, bool? isActive = null, string? keyword = null)
        {
            var q = _db.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(c => c.Type == type);

            if (isActive.HasValue)
                q = q.Where(c => c.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                q = q.Where(c =>
                    c.Name.ToLower().Contains(kw) ||
                    (c.Description != null && c.Description.ToLower().Contains(kw)));
            }

            var data = await q
                .OrderByDescending(c => c.CategoryId)
                .Select(c => new AdminCategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    IsActive = c.IsActive,
                    CreatedAt = null // Db không có CreatedAt cho Category -> để null (hoặc thêm cột nếu cần)
                })
                .ToListAsync();

            return data;
        }

        public async Task<AdminCategoryDto?> GetCategoryAsync(int id)
        {
            return await _db.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new AdminCategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    IsActive = c.IsActive,
                    CreatedAt = null
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(Category entity)
        {
            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();
            return entity.CategoryId;
        }

        public async Task<bool> UpdateAsync(int id, AdminUpdateCategoryDto dto)
        {
            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (cat == null) return false;

            cat.Name = dto.Name.Trim();
            cat.Description = dto.Description;
            cat.Type = dto.Type;
            cat.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (cat == null) return false;

            cat.IsActive = !cat.IsActive;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<Category?> GetEntityAsync(int id)
            => _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

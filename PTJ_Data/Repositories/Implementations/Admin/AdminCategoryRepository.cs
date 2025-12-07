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

        // GET LIST + FILTER
        public async Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync(
            string? categoryGroup = null,
            bool? isActive = null,
            string? keyword = null)
        {
            var q = _db.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryGroup))
                q = q.Where(c => c.CategoryGroup == categoryGroup);

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
                    CategoryGroup = c.CategoryGroup,
                    IsActive = c.IsActive,
                    CreatedAt = null 
                })
                .ToListAsync();

            return data;
        }

        // GET BY ID
        public async Task<AdminCategoryDto?> GetCategoryAsync(int id)
        {
            return await _db.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new AdminCategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    CategoryGroup = c.CategoryGroup,
                    IsActive = c.IsActive,
                    CreatedAt = null
                })
                .FirstOrDefaultAsync();
        }

        // CREATE
        public async Task<int> CreateAsync(Category entity)
        {
            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();
            return entity.CategoryId;
        }

        // UPDATE
        public async Task<bool> UpdateAsync(int id, AdminUpdateCategoryDto dto)
        {
            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (cat == null) return false;

            cat.Name = dto.Name.Trim();
            cat.Description = dto.Description;
            cat.CategoryGroup = dto.CategoryGroup;
            cat.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        // TOGGLE ACTIVE
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

using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.CategoryService.Interfaces;

namespace PTJ_Service.CategoryService.Implementations
    {
    public class CategoryService : ICategoryService
        {
        private readonly JobMatchingDbContext _context;

        public CategoryService(JobMatchingDbContext context)
            {
            _context = context;
            }

        // GET ALL (admin: all, others: only active)
        public async Task<IEnumerable<Category>> GetCategoriesAsync(bool isAdmin)
            {
            var query = _context.Categories.AsQueryable();

            if (!isAdmin)
                query = query.Where(c => c.IsActive == true);

            return await query.OrderBy(c => c.Name).ToListAsync();
            }

        // GET BY ID (admin: all, others: only active)
        public async Task<Category?> GetByIdAsync(int id, bool isAdmin)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return null;

            // Nếu không phải admin thì chỉ trả về nếu IsActive == true
            if (!isAdmin && category.IsActive != true)
                return null;

            return category;
            }

        // CREATE CATEGORY (check duplicate name)
        public async Task<Category?> CreateAsync(CategoryDTO.CategoryCreateDto dto)
            {
            var normalizedName = dto.Name.Trim().ToLower();

            bool isNameExist = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == normalizedName);

            if (isNameExist)
                {
                return null;
                }

            var category = new Category
                {
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Description = dto.Description,
                IsActive = dto.IsActive
                };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
            }

        // UPDATE CATEGORY (check duplicate name)
        public async Task<bool> UpdateAsync(int id, CategoryDTO.CategoryUpdateDto dto)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                var normalizedName = dto.Name.Trim().ToLower();

                bool isNameExist = await _context.Categories
                    .AnyAsync(c => c.CategoryId != id && c.Name.ToLower() == normalizedName);

                if (isNameExist)
                    {
                    return false;
                    }

                category.Name = dto.Name.Trim();
                }

            category.Type = dto.Type ?? category.Type;
            category.Description = dto.Description ?? category.Description;

            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
            }

        // SOFT DELETE CATEGORY + related subcategories
        public async Task<bool> DeleteAsync(int id)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.IsActive = false;

            var subs = await _context.SubCategories
                .Where(s => s.CategoryId == id)
                .ToListAsync();

            foreach (var sub in subs)
                {
                sub.IsActive = false;
                }

            await _context.SaveChangesAsync();
            return true;
            }

        // FILTER CATEGORY (admin: all, others: only active)
        public async Task<IEnumerable<Category>> FilterAsync(CategoryDTO.CategoryFilterDto dto, bool isAdmin)
            {
            var query = _context.Categories.AsQueryable();

            if (!isAdmin)
                query = query.Where(c => c.IsActive == true);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                string keyword = dto.Name.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(keyword));
                }

            return await query.OrderBy(c => c.Name).ToListAsync();
            }
        }
    }

using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.CategoryService.Interfaces;
using static PTJ_Models.DTO.CategoryDTO.CategoryDTO;

namespace PTJ_Service.CategoryService.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly JobMatchingOpenAiDbContext _context;

        public CategoryService(JobMatchingOpenAiDbContext context)
        {
            _context = context;
        }

        // GET ALL 
        public async Task<IEnumerable<Category>> GetCategoriesAsync(bool isAdmin)
        {
            var query = _context.Categories.AsQueryable();

            if (!isAdmin)
                query = query.Where(c => c.IsActive == true);

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // GET BY ID
        public async Task<Category?> GetByIdAsync(int id, bool isAdmin)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return null;

            if (!isAdmin && category.IsActive != true)
                return null;

            return category;
        }

        // CREATE CATEGORY
        public async Task<Category?> CreateAsync(CategoryCreateDto dto)
        {
            var normalizedName = dto.Name.Trim().ToLower();

            bool isNameExist = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == normalizedName);

            if (isNameExist)
                return null;

            var category = new Category
            {
                Name = dto.Name.Trim(),
                CategoryGroup = dto.CategoryGroup,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return category;
        }

        // UPDATE CATEGORY
        public async Task<bool> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var normalizedName = dto.Name.Trim().ToLower();

                bool isNameExist = await _context.Categories
                    .AnyAsync(c => c.CategoryId != id && c.Name.ToLower() == normalizedName);

                if (isNameExist)
                    return false;

                category.Name = dto.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.CategoryGroup))
                category.CategoryGroup = dto.CategoryGroup;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                category.Description = dto.Description;

            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }

        // FILTER CATEGORY
        public async Task<IEnumerable<Category>> FilterAsync(CategoryFilterDto dto, bool isAdmin)
        {
            var query = _context.Categories.AsQueryable();

            if (!isAdmin)
                query = query.Where(c => c.IsActive == true);

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                string keyword = dto.Name.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(keyword));
            }

            // filter theo CategoryGroup
            if (!string.IsNullOrWhiteSpace(dto.CategoryGroup))
            {
                query = query.Where(c => c.CategoryGroup == dto.CategoryGroup);
            }

            return await query.OrderBy(c => c.Name).ToListAsync();
        }
    }
}

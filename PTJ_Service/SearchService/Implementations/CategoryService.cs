using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.SearchService.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.SearchService.Services
    {
    public class CategoryService : ICategoryService
        {
        private readonly JobMatchingDbContext _context;

        public CategoryService(JobMatchingDbContext context)
            {
            _context = context;
            }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
            {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            }

        public async Task<Category?> GetByIdAsync(int id)
            {
            return await _context.Categories.FindAsync(id);
            }

        public async Task<Category> CreateAsync(CategoryDTO.Create dto)
            {
            var category = new Category
                {
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                IsActive = dto.IsActive
                };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
            }

        public async Task<bool> UpdateAsync(int id, CategoryDTO.Update dto)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.Name = dto.Name ?? category.Name;
            category.Type = dto.Type ?? category.Type;
            category.Description = dto.Description ?? category.Description;
            if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
            }

        public async Task<bool> DeleteAsync(int id)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
            }
        }
    }

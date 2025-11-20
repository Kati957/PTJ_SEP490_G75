using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        // LẤY TẤT CẢ CATEGORY ACTIVE
        public async Task<IEnumerable<Category>> GetCategoriesAsync()
            {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            }

        // LẤY THEO ID
        public async Task<Category?> GetByIdAsync(int id)
            {
            return await _context.Categories.FindAsync(id);
            }

        // TẠO CATEGORY
        public async Task<Category> CreateAsync(CategoryDTO.CategoryCreateDto dto)
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

        // UPDATE CATEGORY
        public async Task<bool> UpdateAsync(int id, CategoryDTO.CategoryUpdateDto dto)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.Name = dto.Name ?? category.Name;
            category.Type = dto.Type ?? category.Type;
            category.Description = dto.Description ?? category.Description;
            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
            }

        // DELETE CATEGORY
        public async Task<bool> DeleteAsync(int id)
            {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
            }

        // FILTER CATEGORY
        public async Task<IEnumerable<Category>> FilterAsync(CategoryDTO.CategoryFilterDto dto)
            {
            var query = _context.Categories
                .Where(c => c.IsActive)  // 🔥 Mặc định chỉ lấy Active
                .AsQueryable();

            // 🔍 Filter theo tên
            if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                string keyword = dto.Name.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(keyword));
                }

            // 🔤 Sort theo tên
            return await query.OrderBy(c => c.Name).ToListAsync();
            }
        }
    }

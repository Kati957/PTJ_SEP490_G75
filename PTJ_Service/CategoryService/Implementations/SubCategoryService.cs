using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.SearchService.Interfaces;
using PTJ_Service.CategoryService.Interfaces;

namespace PTJ_Service.SearchService.Services
    {
    public class SubCategoryService : ISubCategoryService
        {
        private readonly JobMatchingDbContext _context;

        public SubCategoryService(JobMatchingDbContext context)
            {
            _context = context;
            }

        // GET ALL (only active)
        public async Task<IEnumerable<SubCategory>> GetAllAsync()
            {
            return await _context.SubCategories
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .ToListAsync();
            }

        // GET BY ID
        public async Task<SubCategory?> GetByIdAsync(int id)
            {
            return await _context.SubCategories
                .FirstOrDefaultAsync(s => s.SubCategoryId == id);
            }

        // LIST BY CATEGORY ID
        public async Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId)
            {
            return await _context.SubCategories
                .Where(s => s.CategoryId == categoryId && s.IsActive == true)
                .OrderBy(s => s.Name)
                .ToListAsync();
            }

        // FILTER BY NAME + CATEGORY
        public async Task<IEnumerable<SubCategory>> FilterAsync(SubCategoryDTO.SubCategoryFilterDto filter)
            {
            var query = _context.SubCategories
                .Where(s => s.IsActive == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
                {
                string keyword = filter.Name.Trim().ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(keyword));
                }

            if (filter.CategoryId.HasValue)
                {
                query = query.Where(s => s.CategoryId == filter.CategoryId.Value);
                }

            return await query.OrderBy(s => s.Name).ToListAsync();
            }

        // CREATE
        public async Task<SubCategory> CreateAsync(SubCategoryDTO.SubCategoryCreateDto dto)
            {
            // Check CategoryId tồn tại
            bool exists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!exists)
                throw new Exception("CategoryId không tồn tại.");

            var sub = new SubCategory
                {
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                Keywords = dto.Description, // model không có Description → map sang Keywords
                IsActive = dto.IsActive
                };

            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            return sub;
            }

        // UPDATE
        public async Task<bool> UpdateAsync(int id, SubCategoryDTO.SubCategoryUpdateDto dto)
            {
            var sub = await _context.SubCategories.FindAsync(id);
            if (sub == null)
                return false;

            if (dto.Name != null)
                sub.Name = dto.Name;

            if (dto.CategoryId.HasValue)
                sub.CategoryId = dto.CategoryId.Value;

            if (dto.Description != null)
                sub.Keywords = dto.Description;

            if (dto.IsActive.HasValue)
                sub.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
            }

        // DELETE
        public async Task<bool> DeleteAsync(int id)
            {
            var sub = await _context.SubCategories.FindAsync(id);
            if (sub == null)
                return false;

            _context.SubCategories.Remove(sub);
            await _context.SaveChangesAsync();
            return true;
            }
        }
    }

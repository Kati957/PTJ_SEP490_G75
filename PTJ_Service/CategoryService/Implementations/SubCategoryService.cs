using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_Service.SearchService.Services
    {
    public class SubCategoryService : ISubCategoryService
        {
        private readonly JobMatchingDbContext _context;

        public SubCategoryService(JobMatchingDbContext context)
            {
            _context = context;
            }

        // GET ALL (admin: all, others: only active)
        public async Task<IEnumerable<SubCategory>> GetAllAsync(bool isAdmin)
            {
            var query = _context.SubCategories.AsQueryable();

            if (!isAdmin)
                query = query.Where(s => s.IsActive == true);

            return await query.OrderBy(s => s.Name).ToListAsync();
            }

        // GET BY ID (admin: all, others: only active)
        public async Task<SubCategory?> GetByIdAsync(int id, bool isAdmin)
            {
            var sub = await _context.SubCategories.FindAsync(id);
            if (sub == null)
                return null;

            if (!isAdmin && sub.IsActive != true)
                return null;

            return sub;
            }

        // LIST BY CATEGORY ID
        public async Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId, bool isAdmin)
            {
            var query = _context.SubCategories
                .Where(s => s.CategoryId == categoryId);

            if (!isAdmin)
                query = query.Where(s => s.IsActive == true);

            return await query.OrderBy(s => s.Name).ToListAsync();
            }

        // FILTER BY NAME + CATEGORY
        public async Task<IEnumerable<SubCategory>> FilterAsync(SubCategoryDTO.SubCategoryFilterDto filter, bool isAdmin)
            {
            var query = _context.SubCategories.AsQueryable();

            if (!isAdmin)
                query = query.Where(s => s.IsActive == true);

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
        public async Task<SubCategory?> CreateAsync(SubCategoryDTO.SubCategoryCreateDto dto)
            {
            // Check CategoryId tồn tại + active
            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive == true);
            if (!exists)
                throw new Exception("CategoryId không tồn tại hoặc đã bị vô hiệu hoá.");

            // Check trùng tên trong cùng Category (chỉ check với SubCategory active)
            string normalized = dto.Name.Trim().ToLower();

            bool duplicate = await _context.SubCategories
                .AnyAsync(s => s.CategoryId == dto.CategoryId &&
                               s.Name.ToLower() == normalized &&
                               s.IsActive == true);

            if (duplicate)
                return null;

            var sub = new SubCategory
                {
                Name = dto.Name.Trim(),
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

            // Nếu đổi tên → check trùng
            if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                string normalized = dto.Name.Trim().ToLower();
                bool duplicate = await _context.SubCategories
                    .AnyAsync(s => s.SubCategoryId != id &&
                                   s.CategoryId == sub.CategoryId &&
                                   s.Name.ToLower() == normalized &&
                                   s.IsActive == true);

                if (duplicate)
                    return false;

                sub.Name = dto.Name.Trim();
                }

            if (dto.CategoryId.HasValue)
                sub.CategoryId = dto.CategoryId.Value;

            if (dto.Description != null)
                sub.Keywords = dto.Description;

            if (dto.IsActive.HasValue)
                sub.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
            }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(int id)
            {
            var sub = await _context.SubCategories.FindAsync(id);
            if (sub == null)
                return false;

            sub.IsActive = false; // soft delete

            await _context.SaveChangesAsync();
            return true;
            }
        }
    }

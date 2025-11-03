using Microsoft.EntityFrameworkCore;
using PTJ_Models;
using PTJ_Models.Models;
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
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.Name)
                .ToListAsync();
            }
        }
    }

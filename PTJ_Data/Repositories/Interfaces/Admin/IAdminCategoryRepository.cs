using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminCategoryRepository
    {
        Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync(string? type = null, bool? isActive = null, string? keyword = null);
        Task<AdminCategoryDto?> GetCategoryAsync(int id);

        Task<int> CreateAsync(Category entity);
        Task<bool> UpdateAsync(int id, AdminUpdateCategoryDto dto);
        Task<bool> ToggleActiveAsync(int id);

        Task<Category?> GetEntityAsync(int id);
        Task SaveChangesAsync();
    }
}

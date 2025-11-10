using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminCategoryService
    {
        Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync(string? type = null, bool? isActive = null, string? keyword = null);
        Task<AdminCategoryDto?> GetCategoryAsync(int id);

        Task<int> CreateAsync(AdminCreateCategoryDto dto);
        Task UpdateAsync(int id, AdminUpdateCategoryDto dto);
        Task ToggleActiveAsync(int id);
    }
}

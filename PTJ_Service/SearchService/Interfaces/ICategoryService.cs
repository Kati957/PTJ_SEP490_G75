using PTJ_Models.DTO.CategoryDTO;
using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.SearchService.Interfaces
    {
    public interface ICategoryService
        {
        /// <summary>
        /// Lấy tất cả danh mục đang hoạt động
        /// </summary>
        Task<IEnumerable<Category>> GetCategoriesAsync();

        /// <summary>
        /// Lấy danh mục theo ID
        /// </summary>
        Task<Category?> GetByIdAsync(int id);

        /// <summary>
        /// Tạo danh mục mới
        /// </summary>
        Task<Category> CreateAsync(CategoryDTO.Create dto);

        /// <summary>
        /// Cập nhật danh mục
        /// </summary>
        Task<bool> UpdateAsync(int id, CategoryDTO.Update dto);

        /// <summary>
        /// Xóa danh mục
        /// </summary>
        Task<bool> DeleteAsync(int id);
        }
    }

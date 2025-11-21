using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;

namespace PTJ_Service.CategoryService.Interfaces
    {
    public interface ICategoryService
        {
        Task<IEnumerable<Category>> GetCategoriesAsync(bool isAdmin);
        Task<Category?> GetByIdAsync(int id, bool isAdmin);
        Task<Category?> CreateAsync(CategoryDTO.CategoryCreateDto dto);
        Task<bool> UpdateAsync(int id, CategoryDTO.CategoryUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Category>> FilterAsync(CategoryDTO.CategoryFilterDto dto, bool isAdmin);
        }
    }

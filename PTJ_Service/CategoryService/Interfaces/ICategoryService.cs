using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;

namespace PTJ_Service.CategoryService.Interfaces
    {
    public interface ICategoryService
        {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category> CreateAsync(CategoryDTO.CategoryCreateDto dto);
        Task<bool> UpdateAsync(int id, CategoryDTO.CategoryUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Category>> FilterAsync(CategoryDTO.CategoryFilterDto dto);
        }
    }

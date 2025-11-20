using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;

namespace PTJ_Service.CategoryService.Interfaces
    {
    public interface ISubCategoryService
        {
        Task<IEnumerable<SubCategory>> GetAllAsync();
        Task<SubCategory?> GetByIdAsync(int id);
        Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId);
        Task<IEnumerable<SubCategory>> FilterAsync(SubCategoryDTO.SubCategoryFilterDto dto);
        Task<SubCategory> CreateAsync(SubCategoryDTO.SubCategoryCreateDto dto);
        Task<bool> UpdateAsync(int id, SubCategoryDTO.SubCategoryUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        }
    }

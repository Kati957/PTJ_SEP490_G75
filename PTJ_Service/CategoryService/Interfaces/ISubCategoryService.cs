using PTJ_Models.Models;
using PTJ_Models.DTO.CategoryDTO;

namespace PTJ_Service.SearchService.Interfaces
    {
    public interface ISubCategoryService
        {
        Task<IEnumerable<SubCategory>> GetAllAsync(bool isAdmin);
        Task<SubCategory?> GetByIdAsync(int id, bool isAdmin);
        Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId, bool isAdmin);
        Task<IEnumerable<SubCategory>> FilterAsync(SubCategoryDTO.SubCategoryFilterDto dto, bool isAdmin);
        Task<SubCategory?> CreateAsync(SubCategoryDTO.SubCategoryCreateDto dto);
        Task<bool> UpdateAsync(int id, SubCategoryDTO.SubCategoryUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        }
    }

using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminNewsRepository
    {
        Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(bool? isPublished = null, string? keyword = null);
        Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id);
        Task<int> CreateAsync(News entity);
        Task<bool> UpdateAsync(News entity);
        Task<bool> TogglePublishStatusAsync(int id);
        Task<bool> SoftDeleteAsync(int id);
    }
}

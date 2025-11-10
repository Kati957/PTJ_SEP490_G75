using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminNewsRepository
    {
        Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(string? status = null, string? keyword = null);
        Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id);
        Task<int> CreateAsync(News entity);
        Task<bool> UpdateAsync(int id, AdminUpdateNewsDto dto);
        Task<bool> ToggleActiveAsync(int id);
        Task SaveChangesAsync();
    }
}

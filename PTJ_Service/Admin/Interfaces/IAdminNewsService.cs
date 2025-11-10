using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminNewsService
    {
        Task<IEnumerable<AdminNewsDto>> GetAllNewsAsync(string? status = null, string? keyword = null);
        Task<AdminNewsDetailDto?> GetNewsDetailAsync(int id);
        Task<int> CreateAsync(int adminId, AdminCreateNewsDto dto);
        Task UpdateAsync(int id, AdminUpdateNewsDto dto);
        Task ToggleActiveAsync(int id);
    }
}

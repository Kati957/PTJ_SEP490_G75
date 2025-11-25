using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        Task<AdminUserDetailDto?> GetUserDetailAsync(int id);

        Task ToggleActiveAsync(int id);

        //  Khóa user + gửi thông báo có lý do (Admin nhập tay)
        Task<bool> BanUserAsync(int userId, string reason, int adminId);

    }
}

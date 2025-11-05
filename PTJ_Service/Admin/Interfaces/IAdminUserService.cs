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
        //  Danh sách người dùng (có filter + phân trang)
        Task<PagedResult<UserDto>> GetAllUsersAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        //  Chi tiết người dùng
        Task<UserDetailDto?> GetUserDetailAsync(int id);

        //  Khóa / Mở khóa tài khoản
        Task ToggleUserActiveAsync(int id);

        //  Danh sách đầy đủ (Dashboard)
        Task<IEnumerable<AdminUserFullDto>> GetAllUserFullAsync();
    }
}

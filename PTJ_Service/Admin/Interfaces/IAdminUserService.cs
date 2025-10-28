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
        Task<IEnumerable<UserDto>> GetAllUsersAsync(
            string role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string keyword = null);

        Task<UserDetailDto> GetUserDetailAsync(int id);

        Task ToggleUserActiveAsync(int id);
    }
}

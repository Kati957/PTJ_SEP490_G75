using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminUserRepository
    {
        Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null,
            int page = 1,
            int pageSize = 10);

        Task<AdminUserDetailDto?> GetUserDetailAsync(int id);
        Task<User?> GetUserEntityAsync(int id);
        Task SaveChangesAsync();
    }
}

using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using PTJ_Data.Repositories.Interfaces.Admin;

namespace PTJ_Service.Implementations.Admin
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _repo;
        public AdminUserService(IAdminUserRepository repo) => _repo = repo;

        public Task<PagedResult<UserDto>> GetAllUsersAsync(
            string? role, bool? isActive, bool? isVerified, string? keyword,
            int page = 1, int pageSize = 10)
            => _repo.GetAllUsersAsync(role, isActive, isVerified, keyword, page, pageSize);

        public Task<UserDetailDto?> GetUserDetailAsync(int id)
            => _repo.GetUserDetailAsync(id);

        public async Task ToggleUserActiveAsync(int id)
        {
            var ok = await _repo.ToggleUserActiveAsync(id);
            if (!ok)
                throw new KeyNotFoundException($"User with ID {id} not found");
        }

        public Task<IEnumerable<AdminUserFullDto>> GetAllUserFullAsync()
            => _repo.GetAllUserFullAsync();
    }
}

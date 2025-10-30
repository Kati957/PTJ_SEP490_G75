using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using PTJ_Data.Repositories.Interfaces.Admin;

namespace PTJ_Service.Implement
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _repo;
        public AdminUserService(IAdminUserRepository repo) => _repo = repo;

        public Task<IEnumerable<UserDto>> GetAllUsersAsync(
            string? role = null,
            bool? isActive = null,
            bool? isVerified = null,
            string? keyword = null)
            => _repo.GetAllUsersAsync(role, isActive, isVerified, keyword);

        public Task<UserDetailDto?> GetUserDetailAsync(int id)
            => _repo.GetUserDetailAsync(id);

        public async Task ToggleUserActiveAsync(int id)
        {
            var ok = await _repo.ToggleUserActiveAsync(id);
            if (!ok)
                throw new KeyNotFoundException($"Không tìm thấy người dùng có ID = {id}");
        }
        public Task<IEnumerable<AdminUserFullDto>> GetAllUserFullAsync()
            => _repo.GetAllUserFullAsync();
    }
}

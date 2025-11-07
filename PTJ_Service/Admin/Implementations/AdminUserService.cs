using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using PTJ_Data.Repositories.Interfaces.Admin;

namespace PTJ_Service.Implementations.Admin
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _repo;
        public AdminUserService(IAdminUserRepository repo) => _repo = repo;

        public Task<PagedResult<AdminUserDto>> GetUsersAsync(string? role, bool? isActive, bool? isVerified, string? keyword, int page, int pageSize)
            => _repo.GetUsersPagedAsync(role, isActive, isVerified, keyword, page, pageSize);

        public Task<AdminUserDetailDto?> GetUserDetailAsync(int id)
            => _repo.GetUserDetailAsync(id);

        public async Task ToggleActiveAsync(int id)
        {
            var user = await _repo.GetUserEntityAsync(id);
            if (user == null) throw new KeyNotFoundException("User not found.");
            user.IsActive = !user.IsActive;
            await _repo.SaveChangesAsync();
        }
    }
}

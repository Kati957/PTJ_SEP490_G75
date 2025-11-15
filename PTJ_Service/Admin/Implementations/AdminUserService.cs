using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

public class AdminUserService : IAdminUserService
    {
    private readonly IAdminUserRepository _repo;
    private readonly ILocationService _location;

    public AdminUserService(IAdminUserRepository repo, ILocationService location)
        {
        _repo = repo;
        _location = location;
        }

    public Task<PagedResult<AdminUserDto>> GetUsersAsync(
        string? role, bool? isActive, bool? isVerified, string? keyword, int page, int pageSize)
        => _repo.GetUsersPagedAsync(role, isActive, isVerified, keyword, page, pageSize);

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(int id)
        {
        var dto = await _repo.GetUserDetailAsync(id);
        if (dto == null) return null;

        dto.ProvinceName = await _location.GetProvinceName(dto.ProvinceId);
        dto.DistrictName = await _location.GetDistrictName(dto.DistrictId, dto.ProvinceId);
        dto.WardName = await _location.GetWardName(dto.WardId, dto.DistrictId);

        return dto;
        }


    public async Task ToggleActiveAsync(int id)
        {
        var user = await _repo.GetUserEntityAsync(id);
        if (user == null) throw new KeyNotFoundException("User not found.");

        user.IsActive = !user.IsActive;
        await _repo.SaveChangesAsync();
        }
    }

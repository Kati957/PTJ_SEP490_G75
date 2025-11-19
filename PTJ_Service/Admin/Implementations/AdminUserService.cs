using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _repo;
    private readonly ILocationService _location;
    private readonly INotificationService _noti;

    public AdminUserService(
        IAdminUserRepository repo,
        ILocationService location,
        INotificationService noti)
    {
        _repo = repo;
        _location = location;
        _noti = noti;
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
        if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.IsActive = !user.IsActive;
        await _repo.SaveChangesAsync();
    }

    //  NEW — KHÓA USER + GỬI NOTIFICATION CÓ LÝ DO 
    public async Task<bool> BanUserAsync(int userId, string reason, int adminId)
    {
        var user = await _repo.GetUserEntityAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        // 1️⃣ Khóa tài khoản
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;

        await _repo.SaveChangesAsync();

        // 2️⃣ Gửi Notification từ template "AccountSuspended"
        await _noti.SendAsync(new CreateNotificationDto
        {
            UserId = userId,
            NotificationType = "AccountSuspended",
            RelatedItemId = userId,
            Data = new Dictionary<string, string>
            {
                { "Reason", reason }
            }
        });

        return true;
    }
}

using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AdminJobPostService : IAdminJobPostService
{
    private readonly IAdminJobPostRepository _repo;
    private readonly ILocationService _location;
    private readonly INotificationService _noti;

    public AdminJobPostService(
        IAdminJobPostRepository repo,
        ILocationService location,
        INotificationService noti)
    {
        _repo = repo;
        _location = location;
        _noti = noti;
    }

    // ========================= EMPLOYER POSTS =========================

    public Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(
        string? status, int? categoryId, string? keyword, int page, int pageSize)
        => _repo.GetEmployerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

    public async Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
    {
        var dto = await _repo.GetEmployerPostDetailAsync(id);
        if (dto == null) return null;

        dto.ProvinceName = await _location.GetProvinceName(dto.ProvinceId);
        dto.DistrictName = await _location.GetDistrictName(dto.DistrictId, dto.ProvinceId);
        dto.WardName = await _location.GetWardName(dto.WardId, dto.DistrictId);

        return dto;
    }

    // ⭐⭐ Admin khóa/mở khóa EMPLOYER POST + gửi Notification ⭐⭐
    public async Task ToggleEmployerPostBlockedAsync(int id, string? reason, int adminId)
    {
        // 1️⃣ Lấy bài đăng
        var post = await _repo.GetEmployerPostByIdAsync(id);
        if (post == null)
            throw new KeyNotFoundException("Employer post not found.");

        bool wasBlocked = post.Status == "Blocked";

        // 2️⃣ Toggle trạng thái
        var ok = await _repo.ToggleEmployerPostBlockedAsync(id);
        if (!ok) throw new KeyNotFoundException("Unable to toggle employer post status.");

        string actionText = wasBlocked ? "được mở lại" : "đã bị khóa";

        // 3️⃣ Gửi Notification
        await _noti.SendAsync(new CreateNotificationDto
        {
            UserId = post.UserId,
            NotificationType = "PostHidden",
            RelatedItemId = post.EmployerPostId,
            Data = new()
            {
                { "PostTitle", post.Title },
                { "Reason", reason ?? $"Bài đăng {actionText} bởi quản trị viên." }
            }
        });
    }

    // ========================= JOB SEEKER POSTS =========================

    public Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(
        string? status, int? categoryId, string? keyword, int page, int pageSize)
        => _repo.GetJobSeekerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

    public async Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id)
    {
        var dto = await _repo.GetJobSeekerPostDetailAsync(id);
        if (dto == null) return null;

        dto.ProvinceName = await _location.GetProvinceName(dto.ProvinceId);
        dto.DistrictName = await _location.GetDistrictName(dto.DistrictId, dto.ProvinceId);
        dto.WardName = await _location.GetWardName(dto.WardId, dto.DistrictId);

        return dto;
    }

    // ⭐⭐ Admin lưu trữ/khôi phục JOB SEEKER POST + gửi Notification ⭐⭐
    public async Task ToggleJobSeekerPostArchivedAsync(int id, string? reason, int adminId)
    {
        // 1️⃣ Lấy bài đăng
        var post = await _repo.GetJobSeekerPostByIdAsync(id);
        if (post == null)
            throw new KeyNotFoundException("JobSeeker post not found.");

        bool wasArchived = post.Status == "Archived";

        // 2️⃣ Toggle
        var ok = await _repo.ToggleJobSeekerPostArchivedAsync(id);
        if (!ok) throw new KeyNotFoundException("Unable to toggle job seeker post status.");

        string actionText = wasArchived ? "được khôi phục" : "đã bị lưu trữ";

        // 3️⃣ Gửi Notification
        await _noti.SendAsync(new CreateNotificationDto
        {
            UserId = post.UserId,
            NotificationType = "PostHiddenJobSeeker",
            RelatedItemId = post.JobSeekerPostId,
            Data = new()
            {
                { "PostTitle", post.Title },
                { "Reason", reason ?? $"Bài viết {actionText} bởi quản trị viên." }
            }
        });
    }
}

using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO.Notification;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.Models;
using System.Linq;

public class AdminUserService : IAdminUserService
    {
    private readonly IAdminUserRepository _repo;
    private readonly ILocationService _location;
    private readonly INotificationService _noti;
    private readonly JobMatchingDbContext _db;

    public AdminUserService(
        IAdminUserRepository repo,
        ILocationService location,
        INotificationService noti,
        JobMatchingDbContext db)
        {
        _repo = repo;
        _location = location;
        _noti = noti;
        _db = db;
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
        user.UpdatedAt = DateTime.Now;

        await _repo.SaveChangesAsync();
        }

    // =======================================================
    // ⭐ KHÓA USER (EMPLOYER hoặc JOBSEEKER)
    // =======================================================
    public async Task<bool> BanUserAsync(int userId, string reason, int adminId)
        {
        var user = await _repo.GetUserEntityAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        // 1️⃣ Khóa tài khoản
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();

        // 2️⃣ Lấy Role của user (User có ICollection<Role>)
        var roleName = user.Roles.FirstOrDefault()?.RoleName;

        // =======================================================
        // 🔥 NẾU USER LÀ EMPLOYER
        // =======================================================
        if (roleName == "Employer")
            {
            // 2.1) Lấy tất cả bài đăng Active của Employer
            var posts = await _db.EmployerPosts
                .Where(p => p.UserId == userId && p.Status == "Active")
                .ToListAsync();

            var postIds = posts.Select(p => p.EmployerPostId).ToList();

            // 2.2) Lấy tất cả đơn ứng tuyển Pending vào các bài này
            var apps = await _db.JobSeekerSubmissions
                .Where(a => postIds.Contains(a.EmployerPostId) && a.Status == "Pending")
                .ToListAsync();

            foreach (var app in apps)
                {
                app.Status = "Cancelled";
                app.UpdatedAt = DateTime.Now;

                // Gửi thông báo tới JobSeeker
                await _noti.SendAsync(new CreateNotificationDto
                    {
                    UserId = app.JobSeekerId,
                    NotificationType = "ApplicationCancelled",
                    RelatedItemId = app.SubmissionId,
                    Data = new()
                    {
                        { "Message", "Nhà tuyển dụng đã bị khóa, đơn ứng tuyển của bạn không còn khả dụng." }
                    }
                    });
                }

            // 2.3) Xóa toàn bộ gợi ý AI liên quan đến các EmployerPost
            var employerAISuggestions = await _db.AiMatchSuggestions
                .Where(s =>
                    (s.SourceType == "EmployerPost" && postIds.Contains(s.SourceId)) ||
                    (s.TargetType == "EmployerPost" && postIds.Contains(s.TargetId)))
                .ToListAsync();

            _db.AiMatchSuggestions.RemoveRange(employerAISuggestions);

            await _db.SaveChangesAsync();
            }

        // =======================================================
        // 🔥 NẾU USER LÀ JOBSEEKER
        // =======================================================
        if (roleName == "JobSeeker")
            {
            // 3.1) Lấy tất cả bài JobSeekerPost Active của user này
            var jsPosts = await _db.JobSeekerPosts
                .Where(p => p.UserId == userId && p.Status == "Active")
                .ToListAsync();

            var jsPostIds = jsPosts.Select(x => x.JobSeekerPostId).ToList();

            // 3.2) Xóa toàn bộ gợi ý AI liên quan tới các JobSeekerPost
            var jsAISuggestions = await _db.AiMatchSuggestions
                .Where(s =>
                    (s.SourceType == "JobSeekerPost" && jsPostIds.Contains(s.SourceId)) ||
                    (s.TargetType == "JobSeekerPost" && jsPostIds.Contains(s.TargetId)))
                .ToListAsync();

            _db.AiMatchSuggestions.RemoveRange(jsAISuggestions);
            await _db.SaveChangesAsync();

            // 3.3) Gửi thông báo cho các Employer đã shortlist ứng viên này
            var employers = await _db.EmployerShortlistedCandidates
                .Where(x => x.JobSeekerId == userId)
                .Select(x => x.EmployerId)
                .Distinct()
                .ToListAsync();

            foreach (var empId in employers)
                {
                await _noti.SendAsync(new CreateNotificationDto
                    {
                    UserId = empId,
                    NotificationType = "JobSeekerSuspended",
                    RelatedItemId = userId,
                    Data = new()
                    {
                        { "Message", "Ứng viên bạn đã lưu/quan tâm đã bị khóa tài khoản và không còn khả dụng." }
                    }
                    });
                }
            }

        // =======================================================
        // 🔥 GỬI THÔNG BÁO CHO USER BỊ KHÓA
        // =======================================================
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

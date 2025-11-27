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
using System;

namespace PTJ_Service.Admin.Implementations
{
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

        
        //  Toggle Active / Inactive
        
        public async Task ToggleActiveAsync(int id)
        {
            var user = await _repo.GetUserEntityAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

            bool wasActive = user.IsActive;

            // Toggle
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;
            await _repo.SaveChangesAsync();

            // Nếu user được mở khóa -> khôi phục bài đăng
            if (!wasActive)
            {
                await RestorePostsAsync(user);
                return;
            }

            // Nếu user bị khóa -> xử lý ban
            await HandleUserDeactivationAsync(user, "Tài khoản của bạn đã bị khóa bởi quản trị viên.");
        }

        
        //   API BAN USER THỦ CÔNG
        
        public async Task<bool> BanUserAsync(int userId, string reason, int adminId)
        {
            var user = await _repo.GetUserEntityAsync(userId)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _repo.SaveChangesAsync();

            await HandleUserDeactivationAsync(user, reason);

            return true;
        }

        
        //  RESTORE POSTS KHI USER ĐƯỢC MỞ KHÓA
        
        private async Task RestorePostsAsync(User user)
        {
            var roleName = user.Roles
                .Select(r => r.RoleName.ToLower().Replace(" ", ""))
                .FirstOrDefault();

            int userId = user.UserId;

            // EMPLOYER
            if (roleName == "employer")
            {
                var posts = await _db.EmployerPosts
                    .Where(p => p.UserId == userId && p.Status.ToLower() == "blocked")
                    .ToListAsync();

                foreach (var p in posts)
                {
                    p.Status = "Active";
                    p.UpdatedAt = DateTime.Now;
                }

                await _db.SaveChangesAsync();
            }

            // JOB SEEKER
            if (roleName == "jobseeker")
            {
                var jsPosts = await _db.JobSeekerPosts
                    .Where(p => p.UserId == userId && p.Status.ToLower() == "blocked")
                    .ToListAsync();

                foreach (var p in jsPosts)
                {
                    p.Status = "Active";
                    p.UpdatedAt = DateTime.Now;
                }

                await _db.SaveChangesAsync();
            }
        }

        
        //   KHÓA ACCOUNT: Block bài + Xóa AI + Hủy đơn...
        
        private async Task HandleUserDeactivationAsync(User user, string? reason)
        {
            var roleName = user.Roles
                .Select(r => r.RoleName.ToLower().Replace(" ", ""))
                .FirstOrDefault();

            int userId = user.UserId;

            //  EMPLOYER 
            if (roleName == "employer")
            {
                var posts = await _db.EmployerPosts
                    .Where(p => p.UserId == userId &&
                                p.Status != null &&
                                p.Status.ToLower().Contains("active"))
                    .ToListAsync();

                foreach (var post in posts)
                {
                    post.Status = "Blocked";
                    post.UpdatedAt = DateTime.Now;
                }
                await _db.SaveChangesAsync();

                var postIds = posts.Select(p => p.EmployerPostId).ToList();

                var apps = await _db.JobSeekerSubmissions
                    .Where(a => postIds.Contains(a.EmployerPostId) &&
                                a.Status.ToLower() == "pending")
                    .ToListAsync();

                foreach (var app in apps)
                {
                    app.Status = "Cancelled";
                    app.UpdatedAt = DateTime.Now;

                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = app.JobSeekerId,
                        NotificationType = "ApplicationCancelled",
                        RelatedItemId = app.SubmissionId,
                        Data = new() { { "Message", "Nhà tuyển dụng đã bị khóa, đơn của bạn không còn hiệu lực." } }
                    });
                }

                var ai = await _db.AiMatchSuggestions
                    .Where(s =>
                        (s.SourceType == "EmployerPost" && postIds.Contains(s.SourceId)) ||
                        (s.TargetType == "EmployerPost" && postIds.Contains(s.TargetId)))
                    .ToListAsync();

                _db.AiMatchSuggestions.RemoveRange(ai);
                await _db.SaveChangesAsync();
            }

            //  JOB SEEKER 
            if (roleName == "jobseeker")
            {
                var posts = await _db.JobSeekerPosts
                    .Where(p => p.UserId == userId &&
                                p.Status != null &&
                                p.Status.ToLower().Contains("active"))
                    .ToListAsync();

                foreach (var post in posts)
                {
                    post.Status = "Blocked";
                    post.UpdatedAt = DateTime.Now;
                }
                await _db.SaveChangesAsync();

                var postIds = posts.Select(x => x.JobSeekerPostId).ToList();

                var ai = await _db.AiMatchSuggestions
                    .Where(s =>
                        (s.SourceType == "JobSeekerPost" && postIds.Contains(s.SourceId)) ||
                        (s.TargetType == "JobSeekerPost" && postIds.Contains(s.TargetId)))
                    .ToListAsync();

                _db.AiMatchSuggestions.RemoveRange(ai);
                await _db.SaveChangesAsync();

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
                        Data = new() { { "Message", "Ứng viên bạn lưu đã bị khóa tài khoản." } }
                    });
                }
            }

            //  NOTIFICATION TO USER 
            var data = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(reason))
                data["Reason"] = reason;
            else
                data["Message"] = "Tài khoản của bạn đã bị khóa.";

            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                NotificationType = "AccountSuspended",
                RelatedItemId = userId,
                Data = data
            });
        }
    }
}
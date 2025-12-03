using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Helpers.Interfaces;
using PTJ_Service.Interfaces.Admin;
using Microsoft.Extensions.Configuration;
using PTJ_Service.Interfaces;
using PTJ_Service.Helpers.Implementations;
using static PTJ_Models.DTO.Admin.GoogleEmployerRegListDto;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminEmployerRegistrationService : IAdminEmployerRegistrationService
    {
        private readonly JobMatchingDbContext _db;
        private readonly IEmailSender _email;
        private readonly IConfiguration _cfg;
        private readonly INotificationService _noti;

        public AdminEmployerRegistrationService(
            JobMatchingDbContext db,
            IEmailSender email,
            IConfiguration cfg,
            INotificationService noti)
        {
            _db = db;
            _email = email;
            _cfg = cfg;
            _noti = noti;
        }

        // 1. NORMAL EMPLOYER REQUESTS

        public async Task<PagedResult<AdminEmployerRegListItemDto>> GetRequestsAsync(
            string? status, string? keyword, int page, int pageSize)
        {
            var query = _db.EmployerRegistrationRequests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(r =>
                    r.CompanyName.Contains(keyword) ||
                    r.Email.Contains(keyword));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminEmployerRegListItemDto
                {
                    RequestId = r.RequestId,
                    Email = r.Email,
                    Username = r.Username,
                    CompanyName = r.CompanyName,
                    ContactPhone = r.ContactPhone,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminEmployerRegListItemDto>(data, total, page, pageSize);
        }

        public async Task<AdminEmployerRegDetailDto?> GetDetailAsync(int requestId)
        {
            return await _db.EmployerRegistrationRequests
                .Where(r => r.RequestId == requestId)
                .Select(r => new AdminEmployerRegDetailDto
                {
                    RequestId = r.RequestId,
                    Email = r.Email,
                    Username = r.Username,
                    CompanyName = r.CompanyName,
                    CompanyDescription = r.CompanyDescription,
                    ContactPerson = r.ContactPerson,
                    ContactPhone = r.ContactPhone,
                    ContactEmail = r.ContactEmail,
                    Website = r.Website,
                    Address = r.Address,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    ReviewedAt = r.ReviewedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task ApproveAsync(int requestId, int adminId)
        {
            var req = await _db.EmployerRegistrationRequests
                .FirstOrDefaultAsync(x => x.RequestId == requestId)
                ?? throw new Exception("Không tìm thấy hồ sơ đăng ký Employer.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ đã được xử lý.");

            var user = new User
            {
                Email = req.Email,
                Username = req.Username,
                PasswordHash = req.PasswordHash,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Gán role Employer
            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "Employer");

            // Tạo EmployerProfile
            const string DefaultLogo =
                "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

            _db.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = req.CompanyName,
                Description = req.CompanyDescription,
                ContactName = req.ContactPerson,
                ContactPhone = req.ContactPhone,
                ContactEmail = req.ContactEmail,
                FullLocation = req.Address,
                AvatarUrl = DefaultLogo,
                UpdatedAt = DateTime.UtcNow
            });

            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.AdminNote = "Đã xác minh và phê duyệt hồ sơ doanh nghiệp.";

            await _db.SaveChangesAsync();

            // Gửi email verify
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });

            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(
                user.Email,
                "PTJ - Xác minh tài khoản nhà tuyển dụng",
                $"Vui lòng xác minh email: <a href='{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}'>Xác minh</a>"
            );
        }

        public async Task RejectAsync(int requestId, AdminEmployerRegRejectDto dto)
        {
            var req = await _db.EmployerRegistrationRequests
                .FirstOrDefaultAsync(x => x.RequestId == requestId)
                ?? throw new Exception("Không tìm thấy hồ sơ.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ này đã được xử lý.");

            req.Status = "Rejected";
            req.AdminNote = dto.Reason;
            req.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(
                req.Email,
                "PTJ - Hồ sơ của bạn bị từ chối",
                $"<p>Hồ sơ đăng ký của bạn đã bị từ chối.</p><p>Lý do: {dto.Reason}</p>"
            );
        }

        // 2. GOOGLE EMPLOYER REQUESTS

        public async Task<IEnumerable<GoogleEmployerRegListDto>> GetGoogleRequestsAsync()
        {
            return await _db.GoogleEmployerRequests
                .Include(x => x.User)
                .Select(x => new GoogleEmployerRegListDto
                {
                    Id = x.Id,
                    DisplayName = x.DisplayName,
                    Email = x.User.Email,
                    PictureUrl = x.PictureUrl,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<GoogleEmployerRegDetailDto?> GetGoogleDetailAsync(int id)
        {
            return await _db.GoogleEmployerRequests
                .Include(x => x.User)
                .Where(x => x.Id == id)
                .Select(x => new GoogleEmployerRegDetailDto
                {
                    Id = x.Id,
                    DisplayName = x.DisplayName,
                    Email = x.User.Email,
                    PictureUrl = x.PictureUrl,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    ReviewedAt = x.ReviewedAt,
                    AdminNote = x.AdminNote
                })
                .FirstOrDefaultAsync();
        }

        public async Task ApproveEmployerGoogleAsync(int requestId, int adminId)
        {
            var req = await _db.GoogleEmployerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(x => x.Id == requestId)
                ?? throw new Exception("Không tìm thấy hồ sơ Google Employer.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ đã được xử lý.");

            var user = req.User;

            await RoleHelper.SetSingleRoleAsync(_db, user.UserId, "Employer");

            _db.EmployerProfiles.Add(new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = req.DisplayName,
                AvatarUrl = req.PictureUrl,
                UpdatedAt = DateTime.UtcNow
            });

            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.AdminNote = "Nhà tuyển dụng Google được duyệt.";

            user.IsVerified = true;

            await _db.SaveChangesAsync();

            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = user.UserId,
                NotificationType = "EmployerApproved",
                RelatedItemId = req.Id,
                Data = new()
                {
                    { "CompanyName", req.DisplayName },
                    { "Message", "Tài khoản nhà tuyển dụng Google của bạn đã được duyệt." }
                }
            });
        }

        public async Task RejectGoogleAsync(int requestId, AdminEmployerRegRejectDto dto)
        {
            var req = await _db.GoogleEmployerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(x => x.Id == requestId)
                ?? throw new Exception("Không tìm thấy hồ sơ Google.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ đã được xử lý.");

            req.Status = "Rejected";
            req.AdminNote = dto.Reason;
            req.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = req.UserId,
                NotificationType = "EmployerRejected",
                RelatedItemId = req.Id,
                Data = new()
                {
                    { "CompanyName", req.DisplayName },
                    { "Reason", dto.Reason },
                    { "Message", "Hồ sơ nhà tuyển dụng Google của bạn đã bị từ chối." }
                }
            });
        }
    }
}

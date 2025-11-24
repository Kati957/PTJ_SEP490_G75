using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
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

namespace PTJ_Service.Implementations.Admin
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
                ?? throw new Exception("Không tìm thấy hồ sơ.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ đã được xử lý trước đó.");

            // 1️⃣ Tạo User nhưng KHÔNG verify
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

            // 2️⃣ Gán role Employer
            var employerRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Employer")
                ?? throw new Exception("Không tìm thấy role Employer.");

            user.Roles.Add(employerRole);
            await _db.SaveChangesAsync();

            // 3️⃣ Tạo EmployerProfile
            const string DefaultLogo = "https://res.cloudinary.com/do5rtjymt/image/upload/v1762001123/default_company_logo.png";

            var profile = new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = req.CompanyName,
                Description = req.CompanyDescription,
                ContactName = req.ContactPerson,
                ContactPhone = req.ContactPhone,
                ContactEmail = req.ContactEmail,
                FullLocation = req.Address,
                AvatarUrl = DefaultLogo,
                AvatarPublicId = null,
                IsAvatarHidden = false,
                UpdatedAt = DateTime.UtcNow
            };

            _db.EmployerProfiles.Add(profile);

            // 4️⃣ Cập nhật trạng thái request
            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.AdminNote = $"Duyệt bởi admin {adminId}";
            await _db.SaveChangesAsync();

            // 5️⃣ Tạo verify token
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });

            await _db.SaveChangesAsync();

            // 6️⃣ Gửi email verify
            var verifyUrl = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";

            await _email.SendEmailAsync(
                user.Email,
                "PTJ - Xác minh tài khoản nhà tuyển dụng",
                $@"
                    <h3>Tài khoản của bạn đã được duyệt!</h3>
                    <p>Vui lòng nhấn nút bên dưới để xác minh email và kích hoạt tài khoản:</p>
                    <a href='{verifyUrl}' 
                        style='background:#007bff;color:#fff;padding:10px 14px;border-radius:5px;text-decoration:none;'>
                        Xác minh tài khoản
                    </a>
                    <p>Liên kết có hiệu lực trong 30 phút.</p>
                ");

            // 7️⃣ GỬI NOTIFICATION TRONG HỆ THỐNG
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = user.UserId,
                NotificationType = "EmployerApproved",
                RelatedItemId = req.RequestId,
                Data = new()
                {
                    { "CompanyName", req.CompanyName },
                    { "Message", "Hồ sơ nhà tuyển dụng của bạn đã được duyệt. Vui lòng xác minh email để kích hoạt tài khoản." }
                }
            });
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
    }
}

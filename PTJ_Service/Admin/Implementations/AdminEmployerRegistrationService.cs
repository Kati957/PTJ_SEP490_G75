using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Helpers.Interfaces;
using PTJ_Service.Interfaces.Admin;

namespace PTJ_Service.Implementations.Admin
{
    public class AdminEmployerRegistrationService : IAdminEmployerRegistrationService
    {
        private readonly JobMatchingDbContext _db;
        private readonly IEmailSender _email;

        public AdminEmployerRegistrationService(JobMatchingDbContext db, IEmailSender email)
        {
            _db = db;
            _email = email;
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
                throw new Exception("Hồ sơ này đã được xử lý.");

            var user = new User
            {
                Email = req.Email,
                Username = req.Username,
                PasswordHash = req.PasswordHash,
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Lấy role Employer
            var employerRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Employer")
                ?? throw new Exception("Không tìm thấy role Employer.");

            // Gán role thông qua navigation --> EF tự tạo UserRole
            user.Roles.Add(employerRole);
            await _db.SaveChangesAsync();

            const string DefaultLogo =
                "https://res.cloudinary.com/do5rtjymt/image/upload/v1762001123/default_company_logo.png";

            const string DefaultLogoPublicId = "default_company_logo";

            // Tạo profile Employer
            var profile = new EmployerProfile
            {
                UserId = user.UserId,
                DisplayName = req.CompanyName,
                Description = req.CompanyDescription,
                ContactName = req.ContactPerson,
                ContactPhone = req.ContactPhone,
                ContactEmail = req.ContactEmail,
                Website = req.Website,
                FullLocation = req.Address,
                AvatarUrl = DefaultLogo,
                AvatarPublicId = DefaultLogoPublicId,
                IsAvatarHidden = false,
                UpdatedAt = DateTime.UtcNow
            };

            _db.EmployerProfiles.Add(profile);

            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.AdminNote = req.AdminNote ?? $"Duyệt bởi admin #{adminId}";

            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(
                req.Email,
                "PTJ - Hồ sơ của bạn đã được duyệt",
                "<p>Hồ sơ đăng ký nhà tuyển dụng của bạn đã được phê duyệt.</p>" +
                "<p>Bạn có thể đăng nhập và sử dụng hệ thống.</p>"
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
    }
}

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
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.RequestId == requestId)
                ?? throw new Exception("Không tìm thấy hồ sơ.");

            if (req.Status != "Pending")
                throw new Exception("Hồ sơ đã được xử lý trước đó.");

            User user;

            // 1️⃣ PHÂN NHÁNH: GOOGLE hay ĐĂNG KÝ THƯỜNG?
            if (req.UserId != null)
            {
                //  LUỒNG GOOGLE — User đã tồn tại
                user = req.User!;
            }
            else
            {
                //  LUỒNG ĐĂNG KÝ THƯỜNG — tạo User mới
                user = new User
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

                // Gắn UserId vào request để đồng bộ
                req.UserId = user.UserId;
            }

            // GÁN ROLE EMPLOYER
            var employerRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Employer")
                ?? throw new Exception("Không tìm thấy role Employer.");

            user.Roles.Clear();            // Xóa PendingEmployer nếu có
            user.Roles.Add(employerRole);
            await _db.SaveChangesAsync();

            //TẠO EMPLOYER PROFILE
            const string DefaultLogo = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

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
                IsAvatarHidden = false,
                UpdatedAt = DateTime.UtcNow
            });

            // ĐỔI TRẠNG THÁI REQUEST
            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.AdminNote = "Hồ sơ đã được kiểm tra và phê duyệt";
            await _db.SaveChangesAsync();

            //TẠO VERIFY TOKEN (CHỈ LUỒNG EMAIL/PASS)
            if (req.User!.PasswordHash != "" && req.User!.PasswordHash != null)
            {
                // Với đăng ký thường — mới cần verify email
                var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

                _db.EmailVerificationTokens.Add(new EmailVerificationToken
                {
                    UserId = user.UserId,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                });

                await _db.SaveChangesAsync();

                var verifyUrl = $"{_cfg["App:BaseUrl"]}/api/Auth/verify-email?token={token}";

                await _email.SendEmailAsync(
                    user.Email,
                    "PTJ - Xác minh tài khoản nhà tuyển dụng",
                    $"Vui lòng xác minh email: <a href='{verifyUrl}'>Xác minh</a>"
                );
            }
            else
            {
                //GOOGLE KHÔNG CẦN RESET PASSWORD
                user.IsVerified = true;
                await _db.SaveChangesAsync();
            }

            // GỬI NOTIFICATION
            await _noti.SendAsync(new CreateNotificationDto
            {
                UserId = user.UserId,
                NotificationType = "EmployerApproved",
                RelatedItemId = req.RequestId,
                Data = new()
        {
            { "CompanyName", req.CompanyName },
            { "Message", "Hồ sơ nhà tuyển dụng của bạn đã được duyệt." }
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

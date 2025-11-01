using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PTJ_Models.DTO;
using PTJ_Models.DTO.ProfileDTO;
using PTJ_Repositories.Interfaces;
using PTJ_Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Services.Implementations
    {
    public class EmployerProfileService : IEmployerProfileService
        {
        private readonly IEmployerProfileRepository _repo;
        private readonly Cloudinary _cloudinary;

        private const string DefaultAvatarUrl = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";
        private const string DefaultPublicId = "avtDefaut_huflze";

        public EmployerProfileService(IEmployerProfileRepository repo, IConfiguration config)
            {
            _repo = repo;
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            }

        // 🟢 1️⃣ Lấy profile của chính Employer đăng nhập (đầy đủ)
        public async Task<EmployerProfileDto?> GetProfileAsync(int userId)
            {
            var p = await _repo.GetByUserIdAsync(userId);
            if (p == null) return null;

            return new EmployerProfileDto
                {
                ProfileId = p.ProfileId,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Description = p.Description,
                AvatarUrl = p.AvatarUrl,
                ContactName = p.ContactName,
                ContactPhone = p.ContactPhone,
                ContactEmail = p.ContactEmail,
                Website = p.Website,
                Location = p.Location
                };
            }

        // 🌐 2️⃣ Lấy danh sách public profiles (mọi người xem được)
        public async Task<IEnumerable<EmployerProfileDto>> GetAllProfilesAsync()
            {
            var list = await _repo.GetAllAsync();

            // Chỉ trả về các trường công khai
            return list.Select(p => new EmployerProfileDto
                {
                DisplayName = p.DisplayName,
                Description = p.Description,
                AvatarUrl = p.AvatarUrl,
                Website = p.Website,
                ContactPhone = p.ContactPhone,
                ContactEmail = p.ContactEmail,
                Location = p.Location
                });
            }

        // 🌐 3️⃣ Xem chi tiết profile của Employer khác (chỉ public info)
        public async Task<EmployerProfileDto?> GetProfileByUserIdAsync(int targetUserId)
            {
            var p = await _repo.GetByUserIdAsync(targetUserId);
            if (p == null) return null;

            return new EmployerProfileDto
                {
                DisplayName = p.DisplayName,
                Description = p.Description,
                AvatarUrl = p.AvatarUrl,
                Website = p.Website,
                ContactPhone = p.ContactPhone,
                ContactEmail = p.ContactEmail,
                Location = p.Location
                };
            }

        // ✏️ 4️⃣ Cập nhật thông tin + upload avatar (chính chủ)
        public async Task<bool> UpdateProfileAsync(int userId, EmployerProfileUpdateDto dto)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.DisplayName = dto.DisplayName;
            existing.Description = dto.Description;
            existing.ContactName = dto.ContactName;
            existing.ContactPhone = dto.ContactPhone;
            existing.ContactEmail = dto.ContactEmail;
            existing.Location = dto.Location;
            existing.Website = dto.Website;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
                {
                using var stream = dto.ImageFile.OpenReadStream();
                var uploadParams = new ImageUploadParams
                    {
                    File = new FileDescription(dto.ImageFile.FileName, stream),
                    Folder = "ptj_profiles/employers"
                    };
                var result = await _cloudinary.UploadAsync(uploadParams);

                existing.AvatarUrl = result.SecureUrl.ToString();
                existing.AvatarPublicId = result.PublicId;
                existing.IsAvatarHidden = false;
                }

            await _repo.UpdateAsync(existing);
            return true;
            }

        // ❌ 5️⃣ Gỡ avatar (chuyển về ảnh mặc định)
        public async Task<bool> DeleteAvatarAsync(int userId)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.AvatarUrl = DefaultAvatarUrl;
            existing.AvatarPublicId = DefaultPublicId;
            existing.IsAvatarHidden = false;

            await _repo.UpdateAsync(existing);
            return true;
            }
        }
    }

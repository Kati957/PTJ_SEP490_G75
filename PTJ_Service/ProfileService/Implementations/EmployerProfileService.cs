using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PTJ_Models.DTO;
using PTJ_Models.DTO.ProfileDTO;
using PTJ_Repositories.Interfaces;
using PTJ_Services.Interfaces;
using PTJ_Models.Models;
using PTJ_Service.LocationService;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;

namespace PTJ_Services.Implementations
    {
    public class EmployerProfileService : IEmployerProfileService
        {
        private readonly IEmployerProfileRepository _repo;
        private readonly Cloudinary _cloudinary;
        private readonly VnPostLocationService _locationService;
        private readonly JobMatchingDbContext _db;
        private const string DefaultAvatarUrl =
            "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

        private const string DefaultPublicId = "avtDefaut_huflze";

        public EmployerProfileService(
            IEmployerProfileRepository repo,
            IConfiguration config,
            JobMatchingDbContext db,
            VnPostLocationService locationService)
            {
            _db = db;
            _repo = repo;
            _locationService = locationService;

            _cloudinary = new Cloudinary(new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            ));
            }

        public async Task<EmployerProfileDto?> GetProfileAsync(int userId)
            {
            EmployerProfile? p = await _repo.GetByUserIdAsync(userId);
            if (p == null) return null;

            var dto = new EmployerProfileDto
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
                ProvinceId = p.ProvinceId,
                DistrictId = p.DistrictId,
                WardId = p.WardId
                };

            dto.Location = await BuildLocationStringAsync(p);

            return dto;
            }

        public async Task<EmployerProfileDto?> GetProfileByUserIdAsync(int targetUserId)
        {
           
            var user = await _db.Users
                .Include(u => u.Roles)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == targetUserId);

            if (user == null || user.IsActive == false)
                return null;
            var role = user.Roles.Select(r => r.RoleName).FirstOrDefault();
            if (role == null || role != "Employer")
                return null;
            var p = await _repo.GetByUserIdAsync(targetUserId);
            if (p == null)
                return null;

            var dto = new EmployerProfileDto
            {
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Description = p.Description,
                AvatarUrl = p.AvatarUrl,
                ContactPhone = p.ContactPhone,
                ContactEmail = p.ContactEmail,
                Website = p.Website,
                ProvinceId = p.ProvinceId,
                DistrictId = p.DistrictId,
                WardId = p.WardId
            };

            dto.Location = await BuildLocationStringAsync(p);

            return dto;
        }


        public async Task<bool> UpdateProfileAsync(int userId, EmployerProfileUpdateDto dto)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.DisplayName = dto.DisplayName;
            existing.Description = dto.Description;
            existing.ContactName = dto.ContactName;
            existing.ContactPhone = dto.ContactPhone;
            existing.ContactEmail = dto.ContactEmail;
            existing.Website = dto.Website;

            existing.ProvinceId = dto.ProvinceId;
            existing.DistrictId = dto.DistrictId;
            existing.WardId = dto.WardId;
            existing.FullLocation = dto.FullLocation;

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

        private async Task<string> BuildLocationStringAsync(EmployerProfile p)
            {
            var provinces = await _locationService.GetProvincesAsync();
            var province = provinces.FirstOrDefault(x => x.code == p.ProvinceId)?.name;

            var districts = await _locationService.GetDistrictsAsync(p.ProvinceId);
            var district = districts.FirstOrDefault(x => x.code == p.DistrictId)?.name;

            var wards = await _locationService.GetWardsAsync(p.DistrictId);
            var ward = wards.FirstOrDefault(x => x.code == p.WardId)?.name;

            string detail = p.FullLocation ?? "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(detail)) parts.Add(detail);
            if (!string.IsNullOrWhiteSpace(ward)) parts.Add(ward);
            if (!string.IsNullOrWhiteSpace(district)) parts.Add(district);
            if (!string.IsNullOrWhiteSpace(province)) parts.Add(province);

            return string.Join(", ", parts);
            }

        }
    }

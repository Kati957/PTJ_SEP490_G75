using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PTJ_Models.DTO;
using PTJ_Models.DTO.ProfileDTO;
using PTJ_Models.Models;
using PTJ_Repositories.Interfaces;
using PTJ_Services.Interfaces;
using PTJ_Service.LocationService;
using PTJ_Service.LocationService.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Services.Implementations
    {
    public class JobSeekerProfileService : IJobSeekerProfileService
        {
        private readonly IJobSeekerProfileRepository _repo;
        private readonly Cloudinary _cloudinary;
        private readonly VnPostLocationService _locationService;

        private const string DefaultPictureUrl =
            "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";

        private const string DefaultPublicId = "avtDefaut_huflze";

        public JobSeekerProfileService(
            IJobSeekerProfileRepository repo,
            IConfiguration config,
            VnPostLocationService locationService)
            {
            _repo = repo;
            _locationService = locationService;

            _cloudinary = new Cloudinary(new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            ));
            }

        // 🟢 Lấy profile của chính user đăng nhập
        public async Task<JobSeekerProfileDto?> GetProfileAsync(int userId)
            {
            JobSeekerProfile? p = await _repo.GetByUserIdAsync(userId);
            if (p == null) return null;

            var dto = new JobSeekerProfileDto
                {
                ProfileId = p.ProfileId,
                UserId = p.UserId,
                FullName = p.FullName,
                Gender = p.Gender,
                BirthYear = p.BirthYear,
                ProfilePicture = p.ProfilePicture,
                ContactPhone = p.ContactPhone,
                ProvinceId = p.ProvinceId,
                DistrictId = p.DistrictId,
                WardId = p.WardId
                };

            dto.Location = await BuildLocationStringAsync(p);

            return dto;
            }

        // 🌐 Xem public profile theo userId
        public async Task<JobSeekerProfileDto?> GetProfileByUserIdAsync(int targetUserId)
            {
            JobSeekerProfile? p = await _repo.GetByUserIdAsync(targetUserId);
            if (p == null) return null;

            var dto = new JobSeekerProfileDto
                {
                ProfileId = p.ProfileId,
                UserId = p.UserId,
                FullName = p.FullName,
                Gender = p.Gender,
                BirthYear = p.BirthYear,
                ProfilePicture = p.ProfilePicture,
                ContactPhone = p.ContactPhone,
                ProvinceId = p.ProvinceId,
                DistrictId = p.DistrictId,
                WardId = p.WardId
                };

            dto.Location = await BuildLocationStringAsync(p);

            return dto;
            }

        // ✏️ Cập nhật thông tin + upload ảnh
        public async Task<bool> UpdateProfileAsync(int userId, JobSeekerProfileUpdateDto dto)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.FullName = dto.FullName;
            existing.Gender = dto.Gender;
            existing.BirthYear = dto.BirthYear;
            existing.ContactPhone = dto.ContactPhone;

            existing.ProvinceId = dto.ProvinceId;
            existing.DistrictId = dto.DistrictId;
            existing.WardId = dto.WardId;

            if (dto.ImageFile is not null && dto.ImageFile.Length > 0)
                {
                using var stream = dto.ImageFile.OpenReadStream();
                var uploadParams = new ImageUploadParams
                    {
                    File = new FileDescription(dto.ImageFile.FileName, stream),
                    Folder = "ptj_profiles/jobseekers"
                    };

                var result = await _cloudinary.UploadAsync(uploadParams);
                existing.ProfilePicture = result.SecureUrl.ToString();
                existing.ProfilePicturePublicId = result.PublicId;
                existing.IsPictureHidden = false;
                }

            await _repo.UpdateAsync(existing);
            return true;
            }

        // ❌ Gỡ ảnh (về mặc định)
        public async Task<bool> DeleteProfilePictureAsync(int userId)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.ProfilePicture = DefaultPictureUrl;
            existing.ProfilePicturePublicId = DefaultPublicId;
            existing.IsPictureHidden = false;

            await _repo.UpdateAsync(existing);
            return true;
            }

        // 🔁 Helper: build LocationDto từ Id bằng VnPostLocationService
        private async Task<string> BuildLocationStringAsync(JobSeekerProfile p)
            {
            var provinces = await _locationService.GetProvincesAsync();
            var province = provinces.FirstOrDefault(x => x.code == p.ProvinceId)?.name;

            var districts = await _locationService.GetDistrictsAsync(p.ProvinceId);
            var district = districts.FirstOrDefault(x => x.code == p.DistrictId)?.name;

            var wards = await _locationService.GetWardsAsync(p.DistrictId);
            var ward = wards.FirstOrDefault(x => x.code == p.WardId)?.name;

            return $"{ward}, {district}, {province}".Trim().Trim(',');
            }
        }
    }

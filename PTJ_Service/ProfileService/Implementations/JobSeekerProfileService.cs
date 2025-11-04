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
    public class JobSeekerProfileService : IJobSeekerProfileService
        {
        private readonly IJobSeekerProfileRepository _repo;
        private readonly Cloudinary _cloudinary;

        private const string DefaultPictureUrl = "https://res.cloudinary.com/do5rtjymt/image/upload/v1761994164/avtDefaut_huflze.jpg";
        private const string DefaultPublicId = "avtDefaut_huflze";

        public JobSeekerProfileService(IJobSeekerProfileRepository repo, IConfiguration config)
            {
            _repo = repo;
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            }

        // 🟢 1️⃣ Lấy profile của chính user đăng nhập (đầy đủ thông tin)
        public async Task<JobSeekerProfileDto?> GetProfileAsync(int userId)
            {
            var p = await _repo.GetByUserIdAsync(userId);
            if (p == null) return null;

            return new JobSeekerProfileDto
                {
                ProfileId = p.ProfileId,
                UserId = p.UserId,
                FullName = p.FullName,
                Gender = p.Gender,
                BirthYear = p.BirthYear,
                ProfilePicture = p.ProfilePicture,
                Skills = p.Skills,
                Experience = p.Experience,
                Education = p.Education,
                PreferredJobType = p.PreferredJobType,
                PreferredLocation = p.PreferredLocation,
                ContactPhone = p.ContactPhone
                };
            }

        // 🌐 3️⃣ Xem chi tiết public profile của người khác
        public async Task<JobSeekerProfileDto?> GetProfileByUserIdAsync(int targetUserId)
            {
            var p = await _repo.GetByUserIdAsync(targetUserId);
            if (p == null) return null;

            // chỉ hiện thông tin công khai
            return new JobSeekerProfileDto
                {
                FullName = p.FullName,
                Gender = p.Gender,
                BirthYear = p.BirthYear,
                ProfilePicture = p.ProfilePicture,
                Skills = p.Skills,
                Experience = p.Experience,
                Education = p.Education,
                PreferredJobType = p.PreferredJobType,
                PreferredLocation = p.PreferredLocation,
                ContactPhone = p.ContactPhone
                };
            }

        // ✏️ 4️⃣ Cập nhật thông tin + upload ảnh (chính chủ)
        public async Task<bool> UpdateProfileAsync(int userId, JobSeekerProfileUpdateDto dto)
            {
            var existing = await _repo.GetByUserIdAsync(userId);
            if (existing == null) return false;

            existing.FullName = dto.FullName;
            existing.Gender = dto.Gender;
            existing.BirthYear = dto.BirthYear;
            existing.Skills = dto.Skills;
            existing.Experience = dto.Experience;
            existing.Education = dto.Education;
            existing.PreferredJobType = dto.PreferredJobType;
            existing.PreferredLocation = dto.PreferredLocation;
            existing.ContactPhone = dto.ContactPhone;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
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

        // ❌ 5️⃣ Gỡ ảnh (về ảnh mặc định)
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
        }
    }

using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.CvDTO;
using PTJ_Models.Models;
using PTJ_Service.JobSeekerCvService.Interfaces;
using PTJ_Service.LocationService;
using PTJ_Service.LocationService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Service.JobSeekerCvService.Implementations
    {
    public class JobSeekerCvService : IJobSeekerCvService
        {
        private readonly IJobSeekerCvRepository _repo;
        private readonly LocationDisplayService _location;
        private readonly JobMatchingDbContext _db;

        public JobSeekerCvService(IJobSeekerCvRepository repo, LocationDisplayService location, JobMatchingDbContext db)
            {
            _repo = repo;
            _location = location;
            _db = db;
            }

   
        // JobSeeker xem CV của chính mình
   
        public async Task<JobSeekerCvResultDto?> GetByIdAsync(int cvId, int jobSeekerId)
            {
            var cv = await _repo.GetByIdAsync(cvId);
            if (cv == null || cv.JobSeekerId != jobSeekerId)
                return null;

            return await ToDto(cv);
            }

   
        // Employer xem CV – KHÔNG check owner
   
        public async Task<JobSeekerCvResultDto?> GetByIdForEmployerAsync(int cvId)
            {
            var cv = await _repo.GetByIdAsync(cvId);
            if (cv == null)
                return null;

            return await ToDto(cv);
            }

   
        // Lấy toàn bộ CV của ứng viên
   
        public async Task<IEnumerable<JobSeekerCvResultDto>> GetByJobSeekerAsync(int jobSeekerId)
            {
            var list = await _repo.GetByJobSeekerAsync(jobSeekerId);
            var result = new List<JobSeekerCvResultDto>();

            foreach (var cv in list)
                {
                result.Add(await ToDto(cv));
                }

            return result;
            }


        // Tạo CV

        public async Task<JobSeekerCvResultDto> CreateAsync(int jobSeekerId, JobSeekerCvCreateDto dto)
            {
            var cv = new JobSeekerCv
                {
                JobSeekerId = jobSeekerId,
                Cvtitle = dto.CvTitle,
                SkillSummary = dto.SkillSummary,
                Skills = dto.Skills,
                PreferredJobType = dto.PreferredJobType,

                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardId = dto.WardId,

                ContactPhone = dto.ContactPhone,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
                };

            await _repo.AddAsync(cv);

            return await ToDto(cv);
            }


        // Cập nhật CV

        public async Task<bool> UpdateAsync(int jobSeekerId, int cvId, JobSeekerCvUpdateDto dto)
            {
            var cv = await _repo.GetByIdAsync(cvId);
            if (cv == null || cv.JobSeekerId != jobSeekerId)
                return false;

            bool hasActivePost = await _db.JobSeekerPosts
                .AnyAsync(x => x.SelectedCvId == cvId && x.Status == "Active");

            if (hasActivePost)
                {
                bool coreChanged =
                    cv.PreferredJobType != dto.PreferredJobType
                 || cv.ProvinceId != dto.ProvinceId
                 || cv.DistrictId != dto.DistrictId
                 || cv.WardId != dto.WardId;

                if (coreChanged)
                    {
                    throw new Exception(
                        "CV đang được sử dụng cho bài đăng tìm việc đang hoạt động. " +
                        "Bạn chỉ có thể cập nhật kỹ năng và mô tả, không thể thay đổi ngành hoặc khu vực."
                    );
                    }
                }

            // ✅ LUÔN CHO UPDATE
            cv.Cvtitle = dto.CvTitle;
            cv.SkillSummary = dto.SkillSummary;
            cv.Skills = dto.Skills;
            cv.ContactPhone = dto.ContactPhone;

            // ✅ CHỈ UPDATE CORE KHI KHÔNG CÓ POST ACTIVE
            if (!hasActivePost)
                {
                cv.PreferredJobType = dto.PreferredJobType;
                cv.ProvinceId = dto.ProvinceId;
                cv.DistrictId = dto.DistrictId;
                cv.WardId = dto.WardId;
                }

            cv.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(cv);
            return true;
            }

        // Xoá CV

        public async Task<bool> DeleteAsync(int jobSeekerId, int cvId)
            {
            var cv = await _repo.GetByIdAsync(cvId);

            // Không cho xóa nếu CV đang được dùng trong bài đăng còn hoạt động
            bool cvInUse = await _db.JobSeekerPosts
                .AnyAsync(x => x.SelectedCvId == cvId && x.Status != "Deleted");

            if (cvInUse)
                throw new Exception("Không thể xoá CV vì bạn đang sử dụng CV này cho một bài đăng. Hãy xoá hoặc cập nhật bài đăng trước.");


            // Nếu không tồn tại hoặc không phải CV của JobSeeker hiện tại
            if (cv == null || cv.JobSeekerId != jobSeekerId)
                return false;

            // Gọi Soft Delete
            await _repo.SoftDeleteAsync(cvId);
            return true;
            }


   
        // Helper convert entity → DTO
   
        private async Task<JobSeekerCvResultDto> ToDto(JobSeekerCv cv)
            {
            var fullAddress = await _location.BuildAddressAsync(cv.ProvinceId, cv.DistrictId, cv.WardId);

            return new JobSeekerCvResultDto
                {
                Cvid = cv.Cvid,
                JobSeekerId = cv.JobSeekerId,

                CvTitle = cv.Cvtitle,
                SkillSummary = cv.SkillSummary,
                Skills = cv.Skills,
                PreferredJobType = cv.PreferredJobType,

                PreferredLocationName = fullAddress,
                ProvinceId = cv.ProvinceId,
                DistrictId = cv.DistrictId,
                WardId = cv.WardId,
                ContactPhone = cv.ContactPhone,
                CreatedAt = (DateTime)cv.CreatedAt,
                UpdatedAt = (DateTime)cv.UpdatedAt
                };
            }
        }
    }

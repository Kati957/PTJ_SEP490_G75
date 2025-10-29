using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.DTO;
using PTJ_Service.ProfileService.Interfaces;

namespace PTJ_Service.ProfileService.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly JobMatchingDbContext _context;

        public ProfileService(JobMatchingDbContext context)
        {
            _context = context;
        }

        // ✅ Lấy thông tin hồ sơ
        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return null;

            var dto = new ProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            var jsProfile = await _context.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var empProfile = await _context.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (jsProfile != null)
            {
                dto.Role = "JobSeeker";
                dto.FullName = jsProfile.FullName;
                dto.Gender = jsProfile.Gender;
                dto.BirthYear = jsProfile.BirthYear;
                dto.Skills = jsProfile.Skills;
                dto.Experience = jsProfile.Experience;
                dto.Education = jsProfile.Education;
                dto.PreferredJobType = jsProfile.PreferredJobType;
                dto.PreferredLocation = jsProfile.PreferredLocation;
            }
            else if (empProfile != null)
            {
                dto.Role = "Employer";
                dto.DisplayName = empProfile.DisplayName;
                dto.Description = empProfile.Description;
                dto.ContactName = empProfile.ContactName;
                dto.ContactPhone = empProfile.ContactPhone;
                dto.ContactEmail = empProfile.ContactEmail;
                dto.Website = empProfile.Website;
                dto.Location = empProfile.Location;
            }
            else
            {
                dto.Role = "Unknown";
            }

            return dto;
        }

        // ✅ Cập nhật thông tin hồ sơ
        public async Task<bool> UpdateProfileAsync(int userId, ProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            // JobSeeker update
            var js = await _context.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (js != null)
            {
                js.FullName = dto.FullName;
                js.Gender = dto.Gender;
                js.BirthYear = dto.BirthYear;
                js.Skills = dto.Skills;
                js.Experience = dto.Experience;
                js.Education = dto.Education;
                js.PreferredJobType = dto.PreferredJobType;
                js.PreferredLocation = dto.PreferredLocation;
                js.UpdatedAt = DateTime.Now;
            }

            // Employer update
            var emp = await _context.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (emp != null)
            {
                emp.DisplayName = dto.DisplayName;
                emp.Description = dto.Description;
                emp.ContactName = dto.ContactName;
                emp.ContactPhone = dto.ContactPhone;
                emp.ContactEmail = dto.ContactEmail;
                emp.Website = dto.Website;
                emp.Location = dto.Location;
                emp.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

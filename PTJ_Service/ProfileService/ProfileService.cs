using PTJ_Models.DTO;
using PTJ_Models.Models;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Service.ProfileService
{
    public class ProfileService : IProfileService
    {
        private readonly JobMatchingDbContext _context;

        public ProfileService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return null;

            var dto = new ProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };

            if (user.Role == "JobSeeker")
            {
                var js = await _context.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (js != null)
                {
                    dto.FullName = js.FullName;
                    dto.Gender = js.Gender;
                    dto.BirthYear = js.BirthYear;
                    dto.Skills = js.Skills;
                    dto.Experience = js.Experience;
                    dto.Education = js.Education;
                    dto.PreferredJobType = js.PreferredJobType;
                    dto.PreferredLocation = js.PreferredLocation;
                    dto.AverageRating = js.AverageRating;
                }
            }
            else if (user.Role == "Employer")
            {
                var emp = await _context.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (emp != null)
                {
                    dto.DisplayName = emp.DisplayName;
                    dto.Description = emp.Description;
                    dto.ContactName = emp.ContactName;
                    dto.ContactPhone = emp.ContactPhone;
                    dto.ContactEmail = emp.ContactEmail;
                    dto.Website = emp.Website;
                    dto.Location = emp.Location;
                    dto.AverageRating = emp.AverageRating;
                }
            }

            return dto;
        }

        public async Task<bool> UpdateProfileAsync(int userId, ProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            if (user.Role == "JobSeeker")
            {
                var js = await _context.JobSeekerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (js == null) return false;

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
            else if (user.Role == "Employer")
            {
                var emp = await _context.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (emp == null) return false;

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

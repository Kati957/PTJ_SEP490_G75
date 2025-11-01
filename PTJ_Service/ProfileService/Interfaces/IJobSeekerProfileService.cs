using PTJ_Models.DTO;
using PTJ_Models.DTO.ProfileDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Services.Interfaces
    {
    public interface IJobSeekerProfileService
        {
        Task<JobSeekerProfileDto?> GetProfileAsync(int userId);
        Task<IEnumerable<JobSeekerProfileDto>> GetAllProfilesAsync();
        Task<JobSeekerProfileDto?> GetProfileByUserIdAsync(int targetUserId);
        Task<bool> UpdateProfileAsync(int userId, JobSeekerProfileUpdateDto dto);
        Task<bool> DeleteProfilePictureAsync(int userId);
        }
    }

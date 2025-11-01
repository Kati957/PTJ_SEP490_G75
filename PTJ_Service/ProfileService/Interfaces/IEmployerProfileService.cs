using PTJ_Models.DTO;
using PTJ_Models.DTO.ProfileDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Services.Interfaces
    {
    public interface IEmployerProfileService
        {
        Task<EmployerProfileDto?> GetProfileAsync(int userId);
        Task<IEnumerable<EmployerProfileDto>> GetAllProfilesAsync();
        Task<EmployerProfileDto?> GetProfileByUserIdAsync(int targetUserId);
        Task<bool> UpdateProfileAsync(int userId, EmployerProfileUpdateDto dto);
        Task<bool> DeleteAvatarAsync(int userId);
        }
    }

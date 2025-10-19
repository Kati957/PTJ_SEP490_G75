using PTJ_Models.DTO;
using System.Threading.Tasks;

namespace PTJ_Service.ProfileService
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, ProfileDto dto);
    }
}

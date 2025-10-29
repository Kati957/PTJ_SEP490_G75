using PTJ_Models.DTO;

namespace PTJ_Service.ProfileService.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, ProfileDto dto);
    }
}

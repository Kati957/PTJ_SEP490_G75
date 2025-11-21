using System.Threading.Tasks;
using PTJ_Models.DTO.Auth;

namespace PTJ_Service.AuthService.Interfaces
{
    public interface IChangePasswordService
    {
        Task RequestChangePasswordAsync(int userId, string currentPassword);
        Task<bool> VerifyChangePasswordRequestAsync(string token);
        Task<bool> ChangePasswordAsync(ConfirmChangePasswordDto dto);
    }
}

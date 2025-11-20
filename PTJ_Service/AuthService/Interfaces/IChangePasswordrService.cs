using System.Threading.Tasks;
using PTJ_Models.DTO.Auth;

namespace PTJ_Service.AuthService.Interfaces
{
    public interface IChangePasswordService
    {
        Task RequestChangePasswordAsync(int userId, RequestChangePasswordDto dto);
        Task<bool> VerifyChangePasswordTokenAsync(string token);
        Task<bool> ConfirmChangePasswordAsync(ConfirmChangePasswordDto dto);
    }
}

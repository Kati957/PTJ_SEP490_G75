using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Auth;

namespace PTJ_Service.AuthService.Interfaces
{
    public interface IChangePasswordrService
    {
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Auth
{
    public class RequestChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
    }
    public class ConfirmChangePasswordDto
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmNewPassword { get; set; } = null!;
    }
}

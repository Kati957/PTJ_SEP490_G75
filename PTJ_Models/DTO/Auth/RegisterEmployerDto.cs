using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Auth
{
    public class RegisterEmployerDto
    {
        public string DisplayName { get; set; } = string.Empty; // Tên công ty / hiển thị
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Website { get; set; }
    }
}

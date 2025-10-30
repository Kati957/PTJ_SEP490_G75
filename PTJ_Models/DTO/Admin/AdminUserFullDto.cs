using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminUserFullDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? PreferredLocation { get; set; }

        
        public string? CompanyName { get; set; }
        public string? Website { get; set; }
    }
}

using System;

namespace PTJ_Models.DTO.Admin
{
    // 1️⃣ Thông tin người dùng cơ bản (dùng cho danh sách)
    public class AdminUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class AdminUserDetailDto : AdminUserDto
    {
        // Thông tin cá nhân chung
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? PreferredLocation { get; set; }

        // Thông tin Employer (nếu là Employer)
        public string? CompanyName { get; set; }
        public string? Website { get; set; }
    }
}

namespace PTJ_Models.DTO
{
    public class ProfileDto
    {
        // Thông tin chung của user
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        // Loại user: "JobSeeker" hoặc "Employer"
        public string Role { get; set; } = string.Empty;

        // JobSeeker profile
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }

        // Employer profile
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
    }
}

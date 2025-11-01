namespace PTJ_Models.DTO.ProfileDTO
    {
    public class EmployerProfileDto
        {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        }
    }

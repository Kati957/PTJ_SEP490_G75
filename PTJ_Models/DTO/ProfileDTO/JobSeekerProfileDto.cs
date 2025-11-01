namespace PTJ_Models.DTO.ProfileDTO
    {
    public class JobSeekerProfileDto
        {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }
        public string? ContactPhone { get; set; }
        }
    }

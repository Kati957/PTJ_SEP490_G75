namespace PTJ_Models.DTO.PostDTO
    {
    public class JobSeekerPostDto
        {
        public int UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string? PreferredLocation { get; set; }
        public int? CategoryID { get; set; }
        public string? PhoneContact { get; set; }

        // 👇 NEW - thêm CV để JobSeekerPost dùng embedding từ CV
        public int? SelectedCvId { get; set; }
        }

    public class JobSeekerPostResultDto
        {
        public JobSeekerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedJobs { get; set; } = new();
        }
    }

namespace PTJ_Models.DTO.PostDTO
    {
    public class EmployerPostDto
        {
        public int UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Salary { get; set; }
        public string? Requirements { get; set; }
        public string? WorkHours { get; set; }
        public string? Location { get; set; }
        public int? CategoryID { get; set; }
        public string? PhoneContact { get; set; }
        }

    public class EmployerPostResultDto
        {
        public EmployerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedCandidates { get; set; } = new();
        }
    }

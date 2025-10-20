namespace PTJ_Models.DTO
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
        public PTJ_Models.Models.EmployerPost Post { get; set; } = new();
        public List<AIResultDto> SuggestedCandidates { get; set; } = new();
    }
}

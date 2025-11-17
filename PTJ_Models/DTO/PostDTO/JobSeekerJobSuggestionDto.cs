namespace PTJ_Models.DTO.PostDTO
{
    public class JobSeekerJobSuggestionDto
    {
        public int EmployerPostId { get; set; }
        public int EmployerUserId { get; set; }

        // Thông tin job
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public decimal? Salary { get; set; }
        public string? Location { get; set; }
        public string? WorkHours { get; set; }
        public string? PhoneContact { get; set; }
        public string? CategoryName { get; set; }

        // Tên nhà tuyển dụng
        public string EmployerName { get; set; } = string.Empty;

        // AI score
        public int MatchPercent { get; set; }
        public double RawScore { get; set; }

        public bool IsSaved { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

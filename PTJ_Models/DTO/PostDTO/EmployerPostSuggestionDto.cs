namespace PTJ_Models.DTO.PostDTO
    {
    public class EmployerPostSuggestionDto
        {
        public int JobSeekerPostId { get; set; }
        public int SeekerUserId { get; set; }

        // Thông tin bài seeker post
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? PreferredLocation { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string? PhoneContact { get; set; }
        public string? CategoryName { get; set; }

        // Tên ứng viên
        public string SeekerName { get; set; } = string.Empty;

        // Gợi ý AI
        public int MatchPercent { get; set; }
        public double RawScore { get; set; }

        public bool IsSaved { get; set; }

        public int? SelectedCvId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        }
    }

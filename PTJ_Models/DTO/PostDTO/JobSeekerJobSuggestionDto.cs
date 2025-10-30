using System;

namespace PTJ_Models.DTO.PostDTO
    {
    public class JobSeekerJobSuggestionDto
        {
        public int EmployerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? WorkHours { get; set; }
        public string EmployerName { get; set; } = string.Empty;

        public int MatchPercent { get; set; }
        public double RawScore { get; set; }

        // Ứng viên (chủ post) đã lưu job này chưa
        public bool IsSaved { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        }
    }

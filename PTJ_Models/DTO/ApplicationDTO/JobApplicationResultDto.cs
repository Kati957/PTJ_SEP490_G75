using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    public class JobApplicationResultDto
        {
        // =========================================================
        // THÔNG TIN ỨNG VIÊN
        // =========================================================
        public int CandidateListId { get; set; }
        public int JobSeekerId { get; set; }
        public string Username { get; set; } = string.Empty;

        // 🟡 Giữ lại tạm thời cho tương thích (Profile cũ)
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? ProfilePicture { get; set; }

        // =========================================================
        // THÔNG TIN CV ỨNG VIÊN
        // =========================================================
        public int? CvId { get; set; }
        public string? CvTitle { get; set; }
        public string? SkillSummary { get; set; }
        public string? Skills { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }
        public string? ContactPhone { get; set; }

        // =========================================================
        // TRẠNG THÁI & THỜI GIAN
        // =========================================================
        public string Status { get; set; } = "Pending";
        public DateTime ApplicationDate { get; set; }
        public string? Notes { get; set; }

        // =========================================================
        // THÔNG TIN BÀI ĐĂNG TUYỂN
        // =========================================================
        public int EmployerPostId { get; set; }
        public string? PostTitle { get; set; }
        public string? CategoryName { get; set; }
        public string? EmployerName { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? WorkHours { get; set; }
        public string? PhoneContact { get; set; }
        public int EmployerId { get; set; }
        }

    public class ApplicationSummaryDto
        {
        public int PendingTotal { get; set; }
        public int ReviewedTotal { get; set; }

        public List<ApplicationSimpleDto> PendingApplications { get; set; } = new();
        public List<ApplicationSimpleDto> ReviewedApplications { get; set; } = new();
        }

    public class ApplicationSimpleDto
        {
        public int SubmissionId { get; set; }
        public int JobSeekerId { get; set; }
        public string Username { get; set; } = string.Empty;

        public int PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        }

    }

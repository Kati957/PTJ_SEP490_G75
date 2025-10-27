using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    /// <summary>
    /// Dữ liệu trả về khi Employer hoặc JobSeeker xem danh sách ứng tuyển.
    /// </summary>
    public class JobApplicationResultDto
        {
        // ============================
        // Thông tin ứng tuyển
        // ============================
        public int CandidateListId { get; set; }
        public int JobSeekerId { get; set; }
        public string Username { get; set; } = string.Empty;

        // ============================
        // Thông tin hồ sơ ứng viên
        // ============================
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }

        // ============================
        // Trạng thái ứng tuyển
        // ============================
        public string Status { get; set; } = "Pending";
        public DateTime ApplicationDate { get; set; }
        public string? Notes { get; set; }

        // ============================
        // Thông tin bài đăng tuyển dụng
        // ============================
        public int EmployerPostId { get; set; }
        public string? PostTitle { get; set; }
        public string? CategoryName { get; set; }
        public string? EmployerName { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? WorkHours { get; set; }
        public int? PhoneContact { get; set; }
        }
    }

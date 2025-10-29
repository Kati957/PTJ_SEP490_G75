using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminEmployerPostDto
    {
        public int Id { get; set; }                 // EmployerPostID
        public string Title { get; set; } = string.Empty;
        public int EmployerUserId { get; set; }     // Users.UserID
        public string EmployerEmail { get; set; } = string.Empty;
        public string? EmployerName { get; set; }   // EmployerProfiles.DisplayName
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = "Active"; // Active|Archived|Deleted|Blocked
        public DateTime CreatedAt { get; set; }
    }

    // Dùng cho chi tiết bài đăng của Employer
    public class AdminEmployerPostDetailDto : AdminEmployerPostDto
    {
        public string? Description { get; set; }
        public decimal? Salary { get; set; }
        public string? Requirements { get; set; }
        public string? WorkHours { get; set; }
        public string? Location { get; set; }
        public int? PhoneContact { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Dùng cho danh sách bài đăng của JobSeeker (tìm việc)
    public class AdminJobSeekerPostDto
    {
        public int Id { get; set; }                 // JobSeekerPostID
        public string Title { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? FullName { get; set; }       // JobSeekerProfiles.FullName
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Gender { get; set; }
        public string? PreferredLocation { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string Status { get; set; } = "Active"; // Active|Archived|Deleted
        public DateTime CreatedAt { get; set; }
    }

    // Chi tiết JobSeeker post
    public class AdminJobSeekerPostDetailDto : AdminJobSeekerPostDto
    {
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

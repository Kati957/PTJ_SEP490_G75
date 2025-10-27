using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    /// <summary>
    /// Dùng khi seeker nộp đơn ứng tuyển vào bài đăng.
    /// </summary>
    public class JobApplicationCreateDto
        {
        public int JobSeekerId { get; set; }
        public int EmployerPostId { get; set; }
        public string? Note { get; set; }
        }
    }

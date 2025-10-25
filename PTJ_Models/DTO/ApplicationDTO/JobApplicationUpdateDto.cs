using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    /// <summary>
    /// Dùng khi employer cập nhật trạng thái ứng viên (Accepted / Rejected / Pending / Withdrawn).
    /// </summary>
    public class JobApplicationUpdateDto
        {
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        }
    }

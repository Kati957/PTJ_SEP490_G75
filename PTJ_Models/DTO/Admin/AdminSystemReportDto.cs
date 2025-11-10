using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminSystemReportDto
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty; // Pending / Solved
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // 2️⃣ Chi tiết System Report
    public class SystemReportDetailDto : AdminSystemReportDto
    {
        public string? FullName { get; set; }
    }

    // 3️⃣ Cập nhật trạng thái
    public class AdminResolveSystemReportDto
    {
        public string Action { get; set; } = "MarkSolved"; // MarkSolved / Ignore
        public string? Note { get; set; }
    }
}

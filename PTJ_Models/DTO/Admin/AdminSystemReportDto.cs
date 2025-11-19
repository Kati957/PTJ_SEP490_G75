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
        public string UserEmail { get; set; }
        public string? FullName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? AdminNote { get; set; }
        public string? ProcessedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    // 2️⃣ Chi tiết System Report
    public class SystemReportDetailDto
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? AdminNote { get; set; }
        public string? ProcessedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // 3️⃣ Cập nhật trạng thái
    public class UpdateSystemReportStatusDto
    {
        public string Status { get; set; }   
        public string? AdminNote { get; set; }
    }
}

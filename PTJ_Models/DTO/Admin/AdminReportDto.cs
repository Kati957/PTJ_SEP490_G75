using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    // Report chưa xử lý
    public class AdminReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;  // User / EmployerPost / JobSeekerPost
        public string ReporterEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Report đã xử lý
    public class AdminSolvedReportDto
    {
        public int SolvedReportId { get; set; }
        public int ReportId { get; set; }
        public string ActionTaken { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }
        public string? ReportType { get; set; }
        public string? ReportReason { get; set; }
        public string? Reason { get; set; } // Lý do admin xử lý
        public DateTime SolvedAt { get; set; }
    }

    // DTO xử lý report (input từ FE)
    public class AdminResolveReportDto
    {
        public int? AffectedUserId { get; set; }
        public int? AffectedPostId { get; set; }
        public string? AffectedPostType { get; set; } // EmployerPost / JobSeekerPost
        public string ActionTaken { get; set; } = string.Empty; // BanUser / UnbanUser / DeletePost / Warn / Ignore
        public string? Reason { get; set; }
    }
    public class AdminReportDetailDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }
        public string? TargetUserRole { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? EmployerPostId { get; set; }
        public string? EmployerPostTitle { get; set; }
        public int? JobSeekerPostId { get; set; }
        public string? JobSeekerPostTitle { get; set; }
    }
}

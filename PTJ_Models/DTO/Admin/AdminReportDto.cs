using System;

namespace PTJ_Models.DTO.Admin
{
    //Report chưa xử lý (Pending)
    public class AdminReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;

        public string ReporterEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }

        public int? PostId { get; set; }
        public string? PostType { get; set; }
        public string? PostTitle { get; set; }
        public string? ActionTaken { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }


    // Report đã xử lý (Solved)
    public class AdminSolvedReportDto
    {
        public int SolvedReportId { get; set; }
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;

        public int? PostId { get; set; }
        public string? PostType { get; set; }
        public string? PostTitle { get; set; }

        public int? TargetUserId { get; set; }
        public string? TargetUserEmail { get; set; }

        public string? Reason { get; set; }
        public DateTime SolvedAt { get; set; }
    }



    // 3️⃣ DTO khi admin xử lý report 
    public class AdminResolveReportDto
    {
        public string ActionTaken { get; set; } = string.Empty; // Warn / DeletePost / Ignore
        public string? Reason { get; set; }
    }


    // 4️⃣ Chi tiết report
    public class AdminReportDetailDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;

        public int ReporterId { get; set; }
        public string ReporterEmail { get; set; } = string.Empty;

        public int? TargetUserId { get; set; }
        public string? TargetUserEmail { get; set; }

        public int? PostId { get; set; }
        public string? PostType { get; set; }
        public string? PostTitle { get; set; }

        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

}

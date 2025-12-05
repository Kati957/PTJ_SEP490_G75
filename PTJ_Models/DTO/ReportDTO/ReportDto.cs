using System;

namespace PTJ_Models.DTO
{
    public class CreatePostReportDto
    {
        public int PostId { get; set; }
        public string? AffectedPostType { get; set; }
        public string ReportType { get; set; } = "";
        public string? Reason { get; set; }
    }

    public class MyReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;

        public int? PostId { get; set; }
        public string? PostType { get; set; }
        public string? PostTitle { get; set; }

        public string Status { get; set; } = "Pending";
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}

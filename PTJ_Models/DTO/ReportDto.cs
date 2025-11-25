using System;

namespace PTJ_Models.DTO.Report
{
    //  Request DTO 
    public class CreateEmployerPostReportDto
    {
        public int EmployerPostId { get; set; }
        public string? Reason { get; set; }
    }

    public class CreateJobSeekerPostReportDto
    {
        public int JobSeekerPostId { get; set; }
        public string? Reason { get; set; }
    }

    //  Response / Listing DTO 
    public class MyReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty; // "EmployerPost" | "JobSeekerPost"
        public int ReportedItemId { get; set; }

        public string? EmployerPostTitle { get; set; }
        public string? JobSeekerPostTitle { get; set; }

        public string Status { get; set; } = "Pending";
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

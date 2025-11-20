namespace PTJ_Models.DTOs
{
    public class SystemReportCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class SystemReportViewDto
    {
        public int ReportId { get; set; }
        public string UserEmail { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SystemReportUpdateDto
    {
        public string Status { get; set; }
    }
}


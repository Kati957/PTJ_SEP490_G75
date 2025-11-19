namespace PTJ_Models.DTOs
{
    public class SystemReportCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class SystemReportViewDto
    {
        public int SystemReportId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ProcessedBy { get; set; } 
    }
    public class SystemReportUpdateStatusDto
    {
        public string Status { get; set; } 
        public string? AdminNote { get; set; }
    }
}

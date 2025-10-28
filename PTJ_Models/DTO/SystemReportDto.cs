namespace PTJ_Models.DTOs
{
    public class SystemReportCreateDto
    {
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class SystemReportViewDto
    {
        public int SystemReportId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class SystemReportUpdateDto
    {
        public string Status { get; set; } = null!;
    }
}
